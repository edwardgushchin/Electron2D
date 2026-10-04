using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Electron2D;

/// <summary>Evaluates reusable typed animation graphs through the inherited property mixer.</summary>
/// <remarks>Graph assets are borrowed. Parameter cells, clocks, connections and scratch belong to each tree/path.
/// Defaults are deterministic blending and nearest continuous sampling of discrete tracks. Topology edits prepare
/// a new graph before evaluation. No scene factory or disk format is supplied for graph state.</remarks>
public class AnimationTree : AnimationMixer
{
    private static readonly PropertyDescriptor[] GraphProperties =
    [
        new PropertyDescriptor<AnimationTree, AnimationRootNode?>(nameof(TreeRoot), n => n.TreeRoot, (n, v) => n.TreeRoot = v, _ => null),
        new PropertyDescriptor<AnimationTree, string>(nameof(AnimPlayer), n => n.AnimPlayer, (n, v) => n.AnimPlayer = v, _ => ""),
        new PropertyDescriptor<AnimationTree, bool>(nameof(Deterministic), n => n.Deterministic, (n, v) => n.Deterministic = v, _ => true),
        new PropertyDescriptor<AnimationTree, AnimationCallbackModeDiscrete>(nameof(CallbackModeDiscrete), n => n.CallbackModeDiscrete, (n, v) => n.CallbackModeDiscrete = v, _ => AnimationCallbackModeDiscrete.ForceContinuous),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(p => p.Name != nameof(Deterministic) && p.Name != nameof(CallbackModeDiscrete)).Concat(GraphProperties);

    private AnimationRootNode? _root;
    private string _animPlayer = "";
    private AnimationPlayer? _player;
    private bool _dirty = true, _started = true, _playerDirty = true;
    private AnimationGraphInstance? _rootInstance;
    private readonly Dictionary<string, AnimationGraphInstance> _instances = new(StringComparer.Ordinal);
    private readonly Dictionary<Animation, (int[] Map, long Revision)> _clipMaps = new(ReferenceEqualityComparer.Instance);
    private readonly List<string> _trackPaths = [];
    private readonly List<AnimationMixFrame> _frames = [];
    private readonly List<double[]> _frameWeights = [];
    private readonly HashSet<AnimationNode> _watched = new(ReferenceEqualityComparer.Instance);
    private long _graphGeneration;
    private SceneTree? _graphTree;
    /// <summary>Initializes deterministic mixing and ForceContinuous discrete evaluation.</summary>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public AnimationTree() { Deterministic = true; CallbackModeDiscrete = AnimationCallbackModeDiscrete.ForceContinuous; AnimationLibrariesUpdated += GraphChanged; AnimationListChanged += GraphChanged; }
    /// <summary>Gets or sets the borrowed graph root; null disables graph output.</summary>
    public AnimationRootNode? TreeRoot
    {
        get { ThrowIfDisposed(); return _root; }
        set { EnsureAnimationMutable(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(_root, value)) return; UnsubscribeRoot(); _root = value; SubscribeRoot(); GraphChanged(); _started = true; }
    }
    /// <summary>Gets or sets the relative scene path to an optional AnimationPlayer library provider.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public string AnimPlayer { get { ThrowIfDisposed(); return _animPlayer; } set { EnsureAnimationMutable(); ArgumentNullException.ThrowIfNull(value); _animPlayer = value; _playerDirty = true; GraphChanged(); ReconcilePlayer(); AnimationPlayerChanged?.Invoke(); } }
    /// <summary>Occurs after assigning the animation-player path.</summary>
    public event Action? AnimationPlayerChanged;
    /// <summary>Returns the inherited automatic animation update phase.</summary>
    /// <returns>The current phase.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public AnimationCallbackModeProcess GetProcessCallback() => CallbackModeProcess;
    /// <summary>Sets the inherited automatic animation update phase.</summary>
    /// <param name="mode">A defined phase.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public void SetProcessCallback(AnimationCallbackModeProcess mode) => CallbackModeProcess = mode;
    /// <summary>Reads a declared typed parameter at a graph-node path.</summary>
    /// <typeparam name="TValue">The exact parameter type.</typeparam>
    /// <param name="nodePath">The relative graph path, empty for the root.</param>
    /// <param name="parameter">A parameter definition declared by that node.</param>
    /// <returns>The per-tree value.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Processing context is absent, an input is disconnected, or an external write targets a read-only slot.</exception>
    /// <exception cref="KeyNotFoundException">The graph-node path is absent.</exception>
    /// <exception cref="ArgumentException">The typed key is absent from the selected node schema.</exception>
    public TValue GetParameter<TValue>(string nodePath, AnimationParameter<TValue> parameter) { EnsureAnimationMutable(); Prepare(); return Instance(nodePath).Get(parameter); }
    /// <summary>Writes a declared, externally writable typed graph parameter.</summary>
    /// <typeparam name="TValue">The exact parameter type.</typeparam>
    /// <param name="nodePath">The relative graph path.</param>
    /// <param name="parameter">The node's exact parameter definition.</param>
    /// <param name="value">The typed value; floating point values must be finite.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Processing context is absent, an input is disconnected, or an external write targets a read-only slot.</exception>
    /// <exception cref="ArgumentException">The typed key is absent from the selected node schema.</exception>
    public void SetParameter<TValue>(string nodePath, AnimationParameter<TValue> parameter, TValue value) { EnsureAnimationMutable(); Prepare(); Instance(nodePath).Set(parameter, value, false); }
    private AnimationGraphInstance Instance(string path) { ArgumentNullException.ThrowIfNull(path); return _instances[path]; }
    private void SubscribeRoot() { if (_root is not null) Watch(_root); }
    private void Watch(AnimationNode node) { if (!_watched.Add(node)) return; node.TreeChanged += GraphChanged; node.AnimationNodeRenamed += GraphRenamed; node.AnimationNodeRemoved += GraphRemoved; }
    private void UnsubscribeRoot() { foreach (var node in _watched) { node.TreeChanged -= GraphChanged; node.AnimationNodeRenamed -= GraphRenamed; node.AnimationNodeRemoved -= GraphRemoved; } _watched.Clear(); }
    private void GraphChanged() { _dirty = true; _graphGeneration++; InvalidateBindings(); }
    private void GraphRemoved(ulong id, string name) => GraphChanged();
    private void GraphRenamed(ulong id, string oldName, string newName)
    {
        foreach (var instance in _instances.Values.Where(i => i.Parent?.Definition.InstanceID == id && i.Name == oldName).ToArray())
        { var oldPrefix = instance.Path; var prefix = instance.Parent!.Path.Length == 0 ? newName : instance.Parent.Path + "/" + newName; foreach (var entry in _instances.Values) if (entry.Path == oldPrefix || entry.Path.StartsWith(oldPrefix + "/", StringComparison.Ordinal)) entry.Path = prefix + entry.Path[oldPrefix.Length..]; }
        GraphChanged();
    }
    private void SceneChanged(SceneTree tree) { _playerDirty = true; GraphChanged(); }
    private void ProviderChanged() { _playerDirty = true; GraphChanged(); }
    private void DetachPlayer() { if (_player is null) return; _player.AnimationLibrariesUpdated -= ProviderChanged; _player.AnimationListChanged -= ProviderChanged; _player.CachesCleared -= ProviderChanged; _player = null; }
    private void ReconcilePlayer()
    {
        if (!_playerDirty && _player?.IsDisposed != true) return;
        _playerDirty = false; var hadPlayer = _player is not null; DetachPlayer();
        if (_animPlayer.Length == 0) { if (hadPlayer) { foreach (var name in GetAnimationLibraryList()) RemoveAnimationLibrary(name); RootNode = ".."; } return; }
        var player = GetNodeOrNull(_animPlayer) as AnimationPlayer;
        foreach (var name in GetAnimationLibraryList()) RemoveAnimationLibrary(name);
        if (player is null || player.IsDisposed) return;
        _player = player; player.AnimationLibrariesUpdated += ProviderChanged; player.AnimationListChanged += ProviderChanged; player.CachesCleared += ProviderChanged;
        foreach (var name in player.GetAnimationLibraryList()) AddAnimationLibrary(name, player.GetAnimationLibrary(name));
        var target = player.RootNode.Length == 0 ? player : player.GetNodeOrNull(player.RootNode); if (target is not null) RootNode = GetPathTo(target);
        player.Active = false;
        _dirty = true;
    }
    private void Prepare()
    {
        ReconcilePlayer();
        foreach (var (clip, state) in _clipMaps) if (!clip.IsDisposed && clip.ChangeRevision != state.Revision) { _dirty = true; break; }
        if (!_dirty) return;
        _rootInstance = null;
        var prior = _instances.Values.ToArray(); _instances.Clear(); _clipMaps.Clear(); _trackPaths.Clear();
        var trackMap = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var name in GetAnimationList())
        {
            var clip = GetAnimation(name); if (clip.IsDisposed || _clipMaps.ContainsKey(clip)) continue;
            var map = new int[clip.GetTrackCount()]; for (var i = 0; i < map.Length; i++) { var track = clip.Get(i); var path = track.Path.Contains(':') ? track.Path : track.Path + ":" + track.PropertyName; if (!trackMap.TryGetValue(path, out var index)) { index = _trackPaths.Count; _trackPaths.Add(path); trackMap.Add(path, index); } map[i] = index; }
            _clipMaps.Add(clip, (map, clip.ChangeRevision));
        }
        if (_root is not null) _rootInstance = Build(_root, "", "", null, new HashSet<AnimationNode>(ReferenceEqualityComparer.Instance), prior);
        var live = _instances.Values.Select(i => i.Definition).ToHashSet(ReferenceEqualityComparer.Instance);
        foreach (var node in _watched.ToArray()) if (!live.Contains(node)) { node.TreeChanged -= GraphChanged; node.AnimationNodeRenamed -= GraphRenamed; node.AnimationNodeRemoved -= GraphRemoved; _watched.Remove(node); }
        _dirty = false;
    }
    private AnimationGraphInstance Build(AnimationNode node, string path, string name, AnimationGraphInstance? parent, HashSet<AnimationNode> stack, AnimationGraphInstance[] prior)
    {
        ObjectDisposedException.ThrowIf(node.IsDisposed, node); if (!stack.Add(node)) throw new InvalidOperationException("Animation graph contains a resource cycle.");
        Watch(node); var instance = new AnimationGraphInstance(this, node, path, name, parent, _trackPaths.Count);
        var old = prior.FirstOrDefault(p => p.Path == path && ReferenceEquals(p.Definition, node));
        foreach (var parameter in node.Parameters())
        { ArgumentNullException.ThrowIfNull(parameter); if (instance.Parameters.ContainsKey(parameter.Name)) throw new InvalidOperationException("Duplicate graph parameter name."); if (old?.Parameters.TryGetValue(parameter.Name, out var slot) == true && ReferenceEquals(slot.Parameter, parameter)) instance.Parameters.Add(parameter.Name, slot); else instance.Parameters.Add(parameter.Name, parameter.CreateSlot(node)); }
        _instances.Add(path, instance);
        foreach (var child in node.Children())
        { if (child.Key.Contains('/') || !instance.Children.TryAdd(child.Key, Build(child.Value, path.Length == 0 ? child.Key : path + "/" + child.Key, child.Key, instance, stack, prior))) throw new InvalidOperationException("Invalid or duplicate child path."); }
        if (node is AnimationNodeBlendTree blendTree) foreach (var child in instance.Children.Values) for (var i = 0; i < child.Inputs.Length; i++) { var source = blendTree.Connection(child.Name, i); if (source.Length != 0) child.Inputs[i] = instance.Children[source]; }
        stack.Remove(node); return instance;
    }
    internal AnimationGraphTime BlendChild(AnimationGraphContext parent, AnimationGraphInstance child, double time, bool seek, bool external, double blend, AnimationNode.FilterAction filter, bool sync, bool test)
    {
        Animation.Finite(time); Animation.Finite(blend); Animation.Valid(filter); var any = false;
        for (var i = 0; i < child.Weights.Length; i++)
        {
            var selected = parent.Instance.Definition.HasFilter && parent.Instance.Definition.FilterEnabled && parent.Instance.Definition.IsPathFiltered(_trackPaths[i]);
            var weight = parent.Instance.Weights[i] * blend;
            if (parent.Instance.Definition.HasFilter && parent.Instance.Definition.FilterEnabled)
                weight = filter switch { AnimationNode.FilterAction.Pass => selected ? weight : 0, AnimationNode.FilterAction.Stop => selected ? 0 : weight, AnimationNode.FilterAction.Blend => selected ? weight : parent.Instance.Weights[i], _ => weight };
            child.Weights[i] = weight; any |= Math.Abs(weight) > 1e-12;
        }
        if (!seek && !sync && !any) time = 0;
        return Evaluate(child, time, seek, external, test || parent.TestOnly, seek ? parent.Delta : time);
    }
    private AnimationGraphTime Evaluate(AnimationGraphInstance instance, double time, bool seek, bool external, bool test, double? inheritedDelta = null)
    {
        if (instance.Evaluating) throw new InvalidOperationException("Animation graph contains an input connection cycle.");
        instance.Evaluating = true; var definition = instance.Definition; var previous = definition.Context; var context = instance.Context;
        context.Time = time; context.Delta = inheritedDelta ?? time; context.Seek = seek; context.External = external; context.TestOnly = test; context.Result = default; context.Position = seek ? time : instance.Get(AnimationNode.CurrentPosition) + time;
        definition.Context = context;
        try
        {
            var remaining = definition.Process(time, seek, external, test); Animation.Finite(remaining);
            var result = context.Result;
            if (!context.HasTime) result = new(Math.Max(0, remaining), 0, 0, SpriteFrames.LoopMode.None);
            if (!test) { instance.Set(AnimationNode.CurrentLength, result.Length, true); instance.Set(AnimationNode.CurrentPosition, result.Position, true); instance.Set(AnimationNode.CurrentDelta, result.Delta, true); }
            return result;
        }
        finally { definition.Context = previous; instance.Evaluating = false; context.HasTime = false; }
    }
    internal void AddClip(AnimationGraphInstance instance, string name, double time, double delta, bool seeked, double blend, double start = 0, double end = -1)
    {
        Animation.Finite(time); Animation.Finite(delta); Animation.Finite(blend); var clip = GetAnimation(name); ObjectDisposedException.ThrowIf(clip.IsDisposed, clip);
        var map = _clipMaps[clip].Map; var index = _frames.Count;
        if (index == _frameWeights.Count) _frameWeights.Add(new double[map.Length]); else if (_frameWeights[index].Length != map.Length) _frameWeights[index] = new double[map.Length];
        var weights = _frameWeights[index]; for (var i = 0; i < weights.Length; i++) weights[i] = instance.Weights[map[i]];
        _frames.Add(new(clip, time, delta < 0, seeked ? null : time - delta, blend, start, end, delta, weights));
    }
    internal void GraphStarted(string name) { if (Tree is not null) Tree.Defer(() => { if (!IsDisposed) Started(name); }); else Started(name); }
    internal void GraphFinished(string name) { if (Tree is not null) Tree.Defer(() => { if (!IsDisposed) Finished(name); }); else Finished(name); }
    /// <inheritdoc />
    internal override void AdvanceAnimation(double delta)
    {
        Prepare(); if (_rootInstance is null) return; var generation = _graphGeneration; _frames.Clear(); Array.Fill(_rootInstance.Weights, 1d);
        Evaluate(_rootInstance, _started ? 0 : delta, _started, false, false, delta); _started = false;
        if (!IsDisposed && generation == _graphGeneration) ApplyBlend(CollectionsMarshal.AsSpan(_frames), delta);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    { if (what == NotificationEnterTree) { _graphTree = Tree; if (_graphTree is not null) _graphTree.TreeChanged += SceneChanged; } else if (what == NotificationExitTree) { if (_graphTree is not null) _graphTree.TreeChanged -= SceneChanged; _graphTree = null; } if (what is NotificationEnterTree or NotificationReady) { _playerDirty = true; ReconcilePlayer(); } base.OnNotification(what); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    { if (disposing) { if (_graphTree is not null) _graphTree.TreeChanged -= SceneChanged; _graphTree = null; UnsubscribeRoot(); _root = null; DetachPlayer(); _instances.Clear(); _clipMaps.Clear(); _frames.Clear(); _frameWeights.Clear(); AnimationPlayerChanged = null; } base.Dispose(disposing); }
}
internal readonly record struct AnimationGraphTime(double Length, double Position, double Delta, SpriteFrames.LoopMode Loop)
{ internal double Remaining => Loop == SpriteFrames.LoopMode.None ? Math.Max(0, Length - Position) : 1e20; }
internal sealed class AnimationGraphContext(AnimationTree tree, AnimationGraphInstance instance)
{
    internal readonly AnimationTree Tree = tree; internal readonly AnimationGraphInstance Instance = instance;
    internal double Time, Position, Delta; internal bool Seek, External, TestOnly, HasTime;
    internal AnimationGraphTime Result;
}
internal sealed class AnimationGraphInstance
{
    internal readonly AnimationNode Definition; internal readonly AnimationGraphInstance? Parent; internal readonly string Name;
    internal string Path; internal readonly double[] Weights; internal readonly AnimationGraphInstance?[] Inputs;
    internal readonly Dictionary<string, AnimationGraphInstance> Children = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, AnimationParameterSlot> Parameters = new(StringComparer.Ordinal);
    internal readonly AnimationGraphContext Context; internal bool Evaluating;
    internal AnimationGraphInstance(AnimationTree tree, AnimationNode definition, string path, string name, AnimationGraphInstance? parent, int tracks)
    { Definition = definition; Path = path; Name = name; Parent = parent; Weights = new double[tracks]; Inputs = new AnimationGraphInstance?[definition.GetInputCount()]; Context = new(tree, this); }
    internal T Get<T>(AnimationParameter<T> key) { ArgumentNullException.ThrowIfNull(key); return (Parameters.TryGetValue(key.Name, out var slot) && ReferenceEquals(slot.Parameter, key) && slot is AnimationParameterSlot<T> typed) ? typed.Value : throw new ArgumentException("Parameter is not declared by this node.", nameof(key)); }
    internal void Set<T>(AnimationParameter<T> key, T value, bool internalWrite)
    { ArgumentNullException.ThrowIfNull(key); if (!internalWrite && Definition.ParameterReadOnly(key)) throw new InvalidOperationException("Parameter is read-only."); if (typeof(T) == typeof(double)) Animation.Finite(Unsafe.As<T, double>(ref value)); if (typeof(T) == typeof(float)) Animation.Finite(Unsafe.As<T, float>(ref value)); if (!Parameters.TryGetValue(key.Name, out var slot) || !ReferenceEquals(slot.Parameter, key) || slot is not AnimationParameterSlot<T> typed) throw new ArgumentException("Parameter is not declared by this node.", nameof(key)); typed.Value = value; }
}
