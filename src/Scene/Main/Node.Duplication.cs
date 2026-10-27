using System.Runtime.ExceptionServices;

namespace Electron2D;

public partial class Node
{
    /// <summary>Selects which authored state a node hierarchy duplicate retains.</summary>
    [Flags]
    public enum DuplicateFlags
    {
        /// <summary>Copies authored persistent typed event connections.</summary>
        Signals = 1,
        /// <summary>Copies persistent and runtime group memberships.</summary>
        Groups = 2,
        /// <summary>Constructs the original compiled gameplay types through their scene factories.</summary>
        Scripts = 4,
        /// <summary>Uses the original linked scene as the starting hierarchy when available.</summary>
        UseInstantiation = 8,
        /// <summary>Also copies writable nonstored typed properties.</summary>
        InternalState = 16,
        /// <summary>Copies signals, groups and compiled types, using linked templates when available.</summary>
        Default = Signals | Groups | Scripts | UseInstantiation
    }

    /// <summary>Creates an independent detached copy of this node and its ordinary descendants.</summary>
    /// <param name="flags">Authored state and linked-template construction policy.</param>
    /// <returns>A caller-owned detached root, with fresh node identities and no Ready delivery.</returns>
    /// <remarks>Copies writable stored typed properties by default; InternalState includes nonstored properties.
    /// Internal children come from constructors/templates. Arrays are copied, internal node references remap,
    /// external references and ordinary resources stay borrowed. Owned resource/file leases remain valid after
    /// source disposal. Always-duplicate resource properties receive independent shallow copies. Signals copies
    /// registered authored subscriptions, preserving external receivers; ordinary CLR subscriptions remain local.
    /// Without Scripts, freshly constructed nodes use their nearest constructible engine base. A linked
    /// template retains its own compiled types unless Scripts replaces them with the source types. Derived
    /// classes require static CreateSceneInstanceFactory hooks. Source mutation/reentry is rejected while copying.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Flags contain an unknown bit.</exception>
    /// <exception cref="ObjectDisposedException">A source or required resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">The factory, schema, hierarchy or scene-owner access is invalid.</exception>
    /// <exception cref="NotSupportedException">A required construction factory or typed property value is unavailable.</exception>
    /// <exception cref="Exception">A constructor, property, subscription or cleanup callback fails.</exception>
    public Node Duplicate(DuplicateFlags flags = DuplicateFlags.Default)
    {
        if ((flags & ~(DuplicateFlags.Default | DuplicateFlags.InternalState)) != 0) throw new ArgumentOutOfRangeException(nameof(flags));
        EnsureSceneActivationAvailable();
        var captured = BeginSceneCapture();
        try { return new NodeDuplication(this, captured, flags).Run(); }
        finally { EndSceneCapture(captured); }
    }

    private sealed class NodeDuplication(Node sourceRoot, Node[] sources, DuplicateFlags flags)
    {
        private readonly HashSet<Node> _sources = new(sources, ReferenceEqualityComparer.Instance);
        private readonly HashSet<Node> _created = new(ReferenceEqualityComparer.Instance);
        private readonly List<Node> _construction = [];
        private readonly Dictionary<Node, Node> _copies = new(ReferenceEqualityComparer.Instance);
        private readonly List<(Node Source, Node Target, ScenePropertyData[] Properties, bool Stripped)> _pairs = [];
        private readonly List<(Node Source, Node Target, Dictionary<ulong, Node> Origins)> _instances = [];
        private readonly List<Resource> _forced = [];
        private readonly List<IDisposable> _leases = [];
        private Node? _root;
        private bool Has(DuplicateFlags bit) => (flags & bit) != 0;

        internal Node Run()
        {
            try
            {
                RetainSourceOwnership();
                var pending = new Stack<(Node Source, Node? Parent, int Index)>(); pending.Push((sourceRoot, null, 0));
                while (pending.TryPop(out var entry))
                {
                    var original = entry.Source; var target = Reuse(original); var linked = target is not null;
                    var stripped = false;
                    if (target is null)
                    {
                        var scene = Has(DuplicateFlags.UseInstantiation) && original is not InstancePlaceholder
                            ? original.SpawnSceneIdentity ?? (original.SceneFilePath.Length != 0 ? ResourceLoader.Load<PackedScene>(original.SceneFilePath) : null) : null;
                        if (scene is not null) { target = scene.Instantiate(); Own(target); linked = true; }
                        else { target = Fresh(original, Has(DuplicateFlags.Scripts)); stripped = target.GetType() != original.GetType(); }
                        if (_root is null) _root = target;
                        if (entry.Parent is not null) entry.Parent.AddChild(target);
                    }
                    if (Has(DuplicateFlags.Scripts) && target.GetType() != original.GetType())
                    {
                        var replacement = Fresh(original, true);
                        target.ReplaceBy(replacement, keepGroups: true);
                        if (_root == target) _root = replacement;
                        foreach (var instance in _instances)
                        {
                            foreach (var key in instance.Origins.Where(pair => ReferenceEquals(pair.Value, target)).Select(pair => pair.Key).ToArray()) instance.Origins[key] = replacement;
                        }
                        target.EndSceneInstantiation(); target.Dispose(); target = replacement;
                    }
                    if (entry.Parent is not null)
                    {
                        if (target.Parent != entry.Parent) target.Reparent(entry.Parent, keepGlobalTransform: false);
                        entry.Parent.MoveChild(target, Math.Min(entry.Index, entry.Parent.GetChildCount() - 1));
                    }
                    stripped = !Has(DuplicateFlags.Scripts) && target.GetType() != original.GetType();
                    _copies.Add(original, target);
                    if (Has(DuplicateFlags.UseInstantiation) && original.SceneInstancePaths is { } paths && linked)
                        _instances.Add((original, target, paths.Where(pair => target.GetNodeOrNull(pair.Value) is not null)
                            .ToDictionary(pair => pair.Key, pair => target.GetNode(pair.Value))));
                    target.RestoreSceneName(original.Name);
                    target._sceneFilePath = original._sceneFilePath;
                    target._editableInstance = original._editableInstance;
                    target._sceneInstanceLoadPlaceholder = original._sceneInstanceLoadPlaceholder;
                    if (original is MissingNode oldMissing && target is MissingNode newMissing)
                        newMissing.CopyRecoveryControls(oldMissing, Has(DuplicateFlags.InternalState));
                    if (original is InstancePlaceholder oldPlaceholder && target is InstancePlaceholder newPlaceholder)
                        newPlaceholder.InitializePending(oldPlaceholder.GetInstancePath(), oldPlaceholder.PendingFields.Copy(static r => r).Fields.Values);
                    if (Has(DuplicateFlags.Groups)) foreach (var group in original._groups) target.AddToGroup(group.Key, group.Value);
                    var properties = original.GetPropertyList().Where(property => !property.IsReadOnly &&
                        (property.IsStored || Has(DuplicateFlags.InternalState)) && property.Name != nameof(Name) &&
                        !(original is MissingNode && MissingNode.IsRecoveryControl(property.Name)) &&
                        !(property.Name == nameof(TranslationDomain) && original.IsTranslationDomainInherited) &&
                        !(original is Window window && property.Name == nameof(Window.Position) && !window.HasAuthoredPosition) &&
                        !(original is FoldableContainer && property.Name.StartsWith("__TitleControl/", StringComparison.Ordinal)))
                        .Concat(original is FoldableContainer foldable ? foldable.CapturedTitleProperties(sourceRoot) : [])
                        .Select(property => new ScenePropertyData(property.Name, property.CaptureStoredValue(original))).ToArray();
                    if (original is MissingNode missing) properties = missing.CaptureRecoveryProperties(properties);
                    _pairs.Add((original, target, properties, stripped));
                    for (var i = original.GetChildCount() - 1; i >= 0; i--) pending.Push((original.GetChild(i), target, i));
                }
                var root = _root!;
                var topology = _created.Where(node => !node.IsDisposed).Select(node => (Node: node, node.Parent, Children: node.GetChildren())).ToArray();
                var scope = Resource.CreateSceneDuplicationScope();
                foreach (var pair in _pairs)
                    foreach (var references in new[] { false, true })
                        PackedScene.RestoreProperties(pair.Target, pair.Properties, scope, references,
                            externalReference: path => ResolveReference(pair.Source, pair.Target, path),
                            includeInternal: Has(DuplicateFlags.InternalState), skipUnavailable: pair.Stripped,
                            resolveResource: static r => { ObjectDisposedException.ThrowIf(r.IsDisposed, r); return r; }, resolveForcedResource: Force);
                if (Has(DuplicateFlags.Signals)) CopyConnections();
                foreach (var pair in _pairs) pair.Target.ValidateSceneInstance();
                foreach (var pair in topology)
                    if (pair.Node.IsDisposed || pair.Node.Parent != pair.Parent || !pair.Node.GetChildren().AsSpan().SequenceEqual(pair.Children))
                        throw new InvalidOperationException("A callback changed the duplicated hierarchy.");
                foreach (var lease in _leases) root.AdoptSceneFileLease(lease); _leases.Clear();
                root.AppendSceneResources(_forced); _forced.Clear();
                foreach (var node in _created) if (!node.IsDisposed) node.EndSceneInstantiation();
                return root;
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                foreach (var node in _created) if (!node.IsDisposed) node.EndSceneInstantiation();
                foreach (var node in _created)
                    foreach (var child in node.IsDisposed ? [] : node.GetChildren())
                        if (!_created.Contains(child)) try { node.RemoveChild(child); } catch (Exception cleanup) { errors.Add(cleanup); }
                foreach (var node in _construction.AsEnumerable().Reverse()) if (!node.IsDisposed)
                        try { node.Dispose(); } catch (Exception cleanup) { errors.Add(cleanup); }
                foreach (var resource in _forced) try { resource.Dispose(); } catch (Exception cleanup) { errors.Add(cleanup); }
                foreach (var lease in _leases) try { lease.Dispose(); } catch (Exception cleanup) { errors.Add(cleanup); }
                if (errors.Count > 1) throw new AggregateException(errors);
                ExceptionDispatchInfo.Capture(error).Throw(); throw;
            }
        }

        private void RetainSourceOwnership()
        {
            var owners = new HashSet<Node>(sources, ReferenceEqualityComparer.Instance);
            for (var parent = sourceRoot.Parent; parent is not null; parent = parent.Parent) owners.Add(parent);
            var pending = new Queue<Node>(owners);
            while (pending.TryDequeue(out var node))
                if (node.SceneOwner is { } owner && owners.Add(owner)) pending.Enqueue(owner);
            foreach (var owner in owners)
            {
                if (owner._sceneFileLeases is { } leases)
                    foreach (var lease in leases.ToArray()) _leases.Add(ResourceFileOwnership.RetainLease(lease));
                if (owner._sharedSceneResourceOwnership is { } resources) _leases.Add(resources.Retain());
            }
        }
        private Node? Reuse(Node source)
        {
            for (var i = _instances.Count - 1; i >= 0; i--)
            {
                var instance = _instances[i]; var owner = source.Owner;
                if (owner is not null && (owner == instance.Source || instance.Source.IsAncestorOf(owner) &&
                    (owner.SpawnSceneIdentity is not null || owner.SceneFilePath.Length != 0)))
                {
                    if (instance.Origins.TryGetValue(source.InstanceID, out var node)) return node;
                    if (instance.Target.GetNodeOrNull(instance.Source.GetPathTo(source)) is { } byPath) return byPath;
                }
            }
            return null;
        }
        private Node Fresh(Node original, bool script)
        {
            Type type = original.GetType();
            if (!script) while (type.Assembly != typeof(Node).Assembly) type = type.BaseType!;
            var expected = type;
            var factory = script || type == original.GetType() ? original.CaptureSceneInstanceFactory() : () => (Node)ResourceFileTypes.Get(expected).Factory();
            var node = InvokeSceneInstanceFactory(factory, script ? original.ConstructionScript : null) ?? throw new InvalidOperationException("A node duplication factory returned null.");
            if (_sources.Contains(node) || _created.Contains(node)) throw new InvalidOperationException("A node duplication factory reused a borrowed or issued node.");
            if (node.Parent is not null || node.Tree is not null || node.Owner is not null) throw new InvalidOperationException("A node duplication factory returned an attached node.");
            Own(node);
            if (node.GetType() != type || node.GetChildCount() != 0 || node.IsQueuedForDeletion || node.SceneFilePath.Length != 0)
                throw new InvalidOperationException("A node duplication factory must return a fresh exact default type.");
            return node;
        }
        private void Own(Node root)
        {
            foreach (var node in root.EnumerateDepthFirst())
            {
                if (_sources.Contains(node)) throw new InvalidOperationException("A factory returned a source node in its hierarchy.");
                if (_created.Add(node)) { _construction.Add(node); node.BeginSceneInstantiation(); }
            }
        }
        private Resource Force(Resource source)
        { var copy = source.Duplicate(); _forced.Add(copy); return copy; }
        private Node? ResolveReference(Node source, Node target, string path)
        {
            var original = source.GetNodeOrNull(path);
            if (original is null) return null;
            if (_copies.TryGetValue(original, out var copy)) return copy;
            return sourceRoot == original || sourceRoot.IsAncestorOf(original) ? target.GetNodeOrNull(path) : original;
        }
        private void CopyConnections()
        {
            var baseline = _pairs.ToDictionary(pair => pair.Target, pair => pair.Target.CaptureSceneConnections());
            var matched = new HashSet<SceneConnectionRuntime>(ReferenceEqualityComparer.Instance);
            foreach (var pair in _pairs)
                foreach (var connection in pair.Source.CaptureSceneConnections())
                {
                    var subscriber = _copies.GetValueOrDefault(connection.Subscriber) ?? connection.Subscriber;
                    if (pair.Target is not MissingNode && !connection.Schema.PublisherType.IsInstanceOfType(pair.Target) || subscriber is not MissingNode && !connection.Schema.SubscriberType.IsInstanceOfType(subscriber)) continue;
                    var existing = baseline[pair.Target].FirstOrDefault(candidate => !matched.Contains(candidate) &&
                        candidate.Schema == connection.Schema && candidate.Subscriber == subscriber && SameBindings(candidate.Bindings, connection.Bindings));
                    if (existing is not null) { matched.Add(existing); continue; }
                    connection.Schema.Restore(pair.Target, subscriber, connection.Bindings);
                }
        }
        private static bool SameBindings(StoredPropertyValue a, StoredPropertyValue b)
        {
            var left = a.CaptureField("Bindings"); var right = b.CaptureField("Bindings");
            return left.Schema == right.Schema && left.Role == right.Role && left.NullArray == right.NullArray &&
                left.Bytes.AsSpan().SequenceEqual(right.Bytes) && left.References.AsSpan().SequenceEqual(right.References);
        }
    }
}
