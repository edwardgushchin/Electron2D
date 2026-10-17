using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class PackedScene
{
    private static Dictionary<PackedScene, PackedSceneData> PrepareTemplates(PackedScene root, PackedSceneData data)
    {
        var snapshots = new Dictionary<PackedScene, PackedSceneData>(ReferenceEqualityComparer.Instance);
        var active = new HashSet<PackedScene>(ReferenceEqualityComparer.Instance);
        var counts = new Dictionary<PackedScene, long>(ReferenceEqualityComparer.Instance);
        Visit(root, data, 0);
        return snapshots;
        long Visit(PackedScene scene, PackedSceneData snapshot, int depth)
        {
            scene.ThrowIfDisposed();
            if (depth >= 128 || !active.Add(scene)) throw new InvalidOperationException("Linked scene templates contain a cycle or exceed the nesting budget.");
            try
            {
                if (counts.TryGetValue(scene, out var known)) return known;
                if (snapshot.Nodes.Length == 0) throw new InvalidOperationException("A linked scene template is empty.");
                snapshots.Add(scene, snapshot);
                long count = snapshot.Nodes.Count(n => n.Instance is null && n.InstanceRootIndex < 0);
                foreach (var node in snapshot.Nodes)
                    if (node.Instance is { } child)
                    {
                        count += Visit(child, child.FileData, depth + 1);
                        if (count > 65536) throw new InvalidOperationException("Linked scene expansion exceeds the node budget.");
                    }
                counts.Add(scene, count); return count;
            }
            finally { active.Remove(scene); }
        }
    }

    private Node InstantiateLinked(PackedSceneData data, string path, bool setupLocalResources,
        List<Node>? parentNodes, Dictionary<PackedScene, PackedSceneData> templates)
    {
        IDisposable? lease = RetainFileResources();
        var nodes = new Node[data.Nodes.Length];
        var created = new List<Node>();
        var resources = CreateSceneDuplicationScope(isolateAllResources: !setupLocalResources);
        var scopes = new Dictionary<Node, Resource.SceneDuplicationScope>(ReferenceEqualityComparer.Instance);
        var inherited = new Dictionary<int, Dictionary<string, Node>>();
        var topology = new SceneTopology();
        var connections = new HashSet<SceneConnectionRuntime>(ReferenceEqualityComparer.Instance);
        try
        {
            for (var i = 0; i < nodes.Length; i++)
            {
                var stored = data.Nodes[i]; Node node;
                if (stored.InstanceRootIndex >= 0)
                {
                    if (!inherited.TryGetValue(stored.InstanceRootIndex, out var original) || !original.TryGetValue(stored.InstancePath, out node!))
                        throw new InvalidOperationException("An overridden node is absent from its linked template.");
                    if (node.GetType() != stored.Factory.RuntimeType) throw new InvalidOperationException("A linked node's compiled type no longer matches its stored schema.");
                }
                else if (stored.Instance is { } instance)
                {
                    var before = created.Count;
                    node = instance.InstantiateCore(PackedSceneEditState.Disabled, setupLocalResources, created, templates);
                    if (node.GetType() != stored.Factory.RuntimeType) throw new InvalidOperationException("A linked root's compiled type no longer matches its stored schema.");
                    var original = new Dictionary<string, Node>(StringComparer.Ordinal);
                    for (var n = before; n < created.Count; n++)
                    {
                        var child = created[n]; topology.Track(child);
                        original.Add(node.GetPathTo(child), child);
                    }
                    inherited.Add(i, original);
                    if (node.SceneInstanceConnections is { } supplied) connections.UnionWith(supplied);
                }
                else
                {
                    node = stored.Factory.Create(); created.Add(node); node.BeginSceneInstantiation();
                    ValidateFactoryResult(stored, node); topology.Track(node);
                }
                nodes[i] = node;
                node.RestoreSceneName(stored.Name);
                var scope = resources;
                if (stored.Instance is not null || stored.InstanceRootIndex >= 0)
                {
                    var localRoot = stored.Instance is not null ? node : node.Owner ?? nodes[stored.InstanceRootIndex];
                    if (!scopes.TryGetValue(localRoot, out scope)) scopes.Add(localRoot, scope = CreateSceneDuplicationScope(isolateAllResources: !setupLocalResources));
                    RestoreProperties(node, stored, scope, localRoot: localRoot);
                }
                else RestoreProperties(node, stored, scope);
                foreach (var group in stored.Groups) node.AddToGroup(group, persistent: true);
                if (stored.ParentIndex >= 0)
                {
                    var parent = nodes[stored.ParentIndex];
                    topology.Place(node, parent, stored.SiblingIndex);
                    var restoredName = node.Name; var collision = false;
                    if (!ReferenceEquals(node.Parent, parent))
                        for (var c = 0; c < parent.GetChildCount(includeInternal: true); c++)
                            if (parent.GetChild(c, includeInternal: true).Name == restoredName) { collision = true; break; }
                    if (node.Parent is null) parent.AddChild(node);
                    else if (!ReferenceEquals(node.Parent, parent)) node.Reparent(parent, keepGlobalTransform: false);
                    // A later inherited record can release this name; final validation rejects real conflicts.
                    if (collision) node.RestoreSceneName(restoredName);
                    if (stored.SiblingIndex >= 0) parent.MoveChild(node, Math.Min(stored.SiblingIndex, parent.GetChildCount() - 1));
                }
            }
            for (var i = 0; i < nodes.Length; i++)
            {
                var owner = data.Nodes[i].OwnerIndex >= 0 ? nodes[data.Nodes[i].OwnerIndex] : null;
                topology.SetOwner(nodes[i], owner); nodes[i].Owner = owner;
            }
            for (var i = 0; i < nodes.Length; i++) RestoreProperties(nodes[i], data.Nodes[i], resources, nodeReferences: true);
            var root = nodes[0];
            if (!IsBuiltInPath(path)) root.SetSceneFilePath(path);
            foreach (var node in created) node.ValidateSceneInstance();
            foreach (var connection in data.Connections)
            {
                var publisher = nodes[connection.PublisherIndex];
                var token = connection.Schema.Subscribe(publisher, nodes[connection.SubscriberIndex], connection.Bindings.TransformResources(resources.Resolve));
                foreach (var runtime in publisher.CaptureSceneConnections()) if (ReferenceEquals(runtime.Token, token)) connections.Add(runtime);
            }
            resources.AssignLocalScene(root);
            foreach (var (localRoot, scope) in scopes) scope.AssignLocalScene(localRoot);
            if (setupLocalResources)
            {
                resources.SetupLocalResources();
                foreach (var scope in scopes.Values) scope.SetupLocalResources();
            }
            root.AdoptSceneResources(resources.ReleaseCreated());
            foreach (var (localRoot, scope) in scopes) localRoot.AppendSceneResources(scope.ReleaseCreated());
            root.AdoptSceneFileLease(lease); lease = null;
            topology.Validate(); root.SceneInstanceConnections = connections;
            if (setupLocalResources) root.Notify(Node.NotificationSceneInstantiated);
            topology.Validate();
            root.RecordSceneInstance(this, created);
            if (parentNodes is null) EndSceneInstantiation(created.ToArray(), created.Count);
            else parentNodes.AddRange(created);
            return root;
        }
        catch (Exception error)
        {
            var owned = created.ToArray(); EndSceneInstantiation(owned, owned.Length);
            var cleanup = CleanupFailedInstance(owned, owned.Length, resources);
            foreach (var scope in scopes.Values) cleanup.AddRange(scope.DisposeCreated());
            try { lease?.Dispose(); } catch (Exception caught) { cleanup.Add(caught); }
            if (cleanup.Count == 0) ExceptionDispatchInfo.Capture(error).Throw();
            cleanup.Insert(0, error); throw new AggregateException("Linked scene instantiation and rollback failed.", cleanup);
        }
        throw new InvalidOperationException("Unreachable linked scene instantiation state.");
    }

    private sealed class SceneTopology
    {
        private readonly Dictionary<Node, (Node? Parent, Node? Owner, List<Node> Children)> _nodes = new(ReferenceEqualityComparer.Instance);
        internal void Track(Node node) => _nodes.Add(node, (node.Parent, node.Owner, Children(node).ToList()));
        internal void Place(Node node, Node parent, int sibling)
        {
            var previous = _nodes[node];
            if (!ReferenceEquals(previous.Parent, parent) || sibling >= 0)
            {
                if (previous.Parent is { } old && _nodes.TryGetValue(old, out var oldState)) oldState.Children.Remove(node);
                var children = _nodes[parent].Children;
                children.Insert(sibling < 0 ? children.Count : Math.Min(sibling, children.Count), node);
            }
            _nodes[node] = (parent, previous.Owner, previous.Children);
        }
        internal void SetOwner(Node node, Node? owner) { var old = _nodes[node]; _nodes[node] = (old.Parent, owner, old.Children); }
        internal void Validate()
        {
            foreach (var (node, expected) in _nodes)
            {
                ObjectDisposedException.ThrowIf(node.IsDisposed, node);
                if (node.Parent != expected.Parent || node.Owner != expected.Owner || node.Tree is not null || node.IsQueuedForDeletion ||
                    !Children(node).SequenceEqual(expected.Children) || expected.Children.Select(n => n.Name).Distinct(StringComparer.Ordinal).Count() != expected.Children.Count)
                    throw new InvalidOperationException("A callback changed the hierarchy during linked scene instantiation.");
            }
        }
        private static IEnumerable<Node> Children(Node node)
        {
            for (var i = 0; i < node.GetChildCount(includeInternal: true); i++)
            {
                var child = node.GetChild(i, includeInternal: true);
                if (!child.IsInternalChild || node is FoldableContainer foldable && foldable.IsTitleControl(child)) yield return child;
            }
        }
    }
}
