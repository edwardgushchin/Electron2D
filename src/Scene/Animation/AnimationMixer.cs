namespace Electron2D;

/// <summary>Owns scene animation libraries, typed property bindings and update-phase selection.</summary>
/// <remarks>Playback controllers derive from this node. Idle and physics use Node's internal scheduling;
/// manual Advance also works while detached. Tree mutation, resource edits and explicit ClearCaches rebuild
/// typed target bindings on their next use. Libraries and animations remain caller-owned.</remarks>
public class AnimationMixer : Node
{
    private static readonly PropertyDescriptor[] AnimationProperties =
    [
        new PropertyDescriptor<AnimationMixer, bool>(nameof(Active), n => n.Active, (n, v) => n.Active = v, _ => true),
        new PropertyDescriptor<AnimationMixer, string>(nameof(RootNode), n => n.RootNode, (n, v) => n.RootNode = v, _ => ".."),
        new PropertyDescriptor<AnimationMixer, AnimationCallbackModeProcess>(nameof(CallbackModeProcess), n => n.CallbackModeProcess, (n, v) => n.CallbackModeProcess = v, _ => AnimationCallbackModeProcess.Idle),
    ];
    /// <summary>Selects the automatic animation update phase or explicit manual updates.</summary>
    public enum AnimationCallbackModeProcess
    {
        /// <summary>Use fixed physics updates.</summary>
        Physics = 0,
        /// <summary>Use variable idle updates.</summary>
        Idle = 1,
        /// <summary>Advance only when explicitly requested.</summary>
        Manual = 2,
    }
    private readonly Dictionary<string, (AnimationLibrary Library, Action<Resource> Changed)> _libraries = new(StringComparer.Ordinal);
    private Animation? _cachedAnimation;
    private long _cachedRevision;
    private AnimationBinding?[] _bindings = [];
    private bool _active = true;
    private string _rootNode = "..";
    private AnimationCallbackModeProcess _callbackModeProcess = AnimationCallbackModeProcess.Idle;
    private SceneTree? _observedTree;
    private long _bindingGeneration;
    /// <summary>Gets or sets whether evaluation is active; defaults to true.</summary>
    public bool Active { get { ThrowIfDisposed(); return _active; } set { EnsureAnimationMutable(); _active = value; InvalidateBindings(); UpdateScheduling(); } }
    /// <summary>Gets or sets the relative root used for all track paths; defaults to the parent.</summary>
    public string RootNode { get { ThrowIfDisposed(); return _rootNode; } set { EnsureAnimationMutable(); ArgumentNullException.ThrowIfNull(value); _rootNode = value; InvalidateBindings(); } }
    /// <summary>Gets or sets the internal update phase, initially idle.</summary>
    public AnimationCallbackModeProcess CallbackModeProcess { get { ThrowIfDisposed(); return _callbackModeProcess; } set { EnsureAnimationMutable(); Animation.Valid(value); _callbackModeProcess = value; InvalidateBindings(); UpdateScheduling(); } }
    /// <summary>Occurs after library membership changes.</summary>
    public event Action? AnimationLibrariesUpdated;
    /// <summary>Occurs after a library's named animation collection changes.</summary>
    public event Action? AnimationListChanged;
    /// <summary>Occurs when playback starts.</summary>
    public event Action<string>? AnimationStarted;
    /// <summary>Occurs after non-looping playback reaches its endpoint.</summary>
    public event Action<string>? AnimationFinished;
    /// <summary>Occurs after explicit cache clearing.</summary>
    public event Action? CachesCleared;
    /// <summary>Adds a borrowed library; empty names form the default namespace.</summary>
    /// <param name="name">The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.</param>
    /// <param name="library">The live borrowed animation library.</param>
    public void AddAnimationLibrary(string name, AnimationLibrary library)
    {
        EnsureAnimationMutable(); ArgumentNullException.ThrowIfNull(library); ObjectDisposedException.ThrowIf(library.IsDisposed, library); if (!AnimationLibrary.ValidName(name, true) || _libraries.ContainsKey(name)) throw new ArgumentException("Invalid or occupied library namespace.", nameof(name));
        foreach (var item in _libraries.Values) if (ReferenceEquals(item.Library, library)) throw new ArgumentException("The library is already registered.", nameof(library));
        Action<Resource> changed = _ => { InvalidateBindings(); if (!IsDisposed) AnimationListChanged?.Invoke(); };
        _libraries.Add(name, (library, changed)); library.Changed += changed; InvalidateBindings(); AnimationListChanged?.Invoke(); if (!IsDisposed) AnimationLibrariesUpdated?.Invoke();
    }
    /// <summary>Returns a borrowed library by exact namespace.</summary>
    /// <param name="name">The exact ordinal name.</param>
    public AnimationLibrary GetAnimationLibrary(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _libraries[name].Library; }
    /// <summary>Tests exact library namespace membership.</summary>
    /// <param name="name">The exact ordinal name.</param>
    public bool HasAnimationLibrary(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _libraries.ContainsKey(name); }
    /// <summary>Returns an independent ordinal-sorted library namespace array.</summary>
    public string[] GetAnimationLibraryList() { ThrowIfDisposed(); return _libraries.Keys.Order(StringComparer.Ordinal).ToArray(); }
    /// <summary>Removes a library without disposing it.</summary>
    /// <param name="name">The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.</param>
    public void RemoveAnimationLibrary(string name) { EnsureAnimationMutable(); var item = _libraries[name]; _libraries.Remove(name); item.Library.Changed -= item.Changed; InvalidateBindings(); AnimationListChanged?.Invoke(); if (!IsDisposed) AnimationLibrariesUpdated?.Invoke(); }
    /// <summary>Renames a library namespace without changing its resources.</summary>
    /// <param name="newName">An unoccupied valid replacement name.</param>
    /// <param name="name">The exact ordinal name.</param>
    public void RenameAnimationLibrary(string name, string newName)
    { EnsureAnimationMutable(); if (!AnimationLibrary.ValidName(newName, true) || _libraries.ContainsKey(newName)) throw new ArgumentException("Invalid or occupied library namespace.", nameof(newName)); var item = _libraries[name]; _libraries.Remove(name); _libraries.Add(newName, item); InvalidateBindings(); AnimationListChanged?.Invoke(); if (!IsDisposed) AnimationLibrariesUpdated?.Invoke(); }
    /// <summary>Tests a qualified library/name or an unqualified default-library animation.</summary>
    /// <param name="name">The exact ordinal name.</param>
    public bool HasAnimation(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); var slash = name.IndexOf('/'); return _libraries.TryGetValue(slash < 0 ? "" : name[..slash], out var item) && !item.Library.IsDisposed && item.Library.HasAnimation(slash < 0 ? name : name[(slash + 1)..]); }
    /// <summary>Returns a borrowed animation by its qualified name.</summary>
    /// <param name="name">The exact ordinal name.</param>
    public Animation GetAnimation(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); var slash = name.IndexOf('/'); return GetAnimationLibrary(slash < 0 ? "" : name[..slash]).GetAnimation(slash < 0 ? name : name[(slash + 1)..]); }
    /// <summary>Returns all qualified animation names in ordinal order.</summary>
    public string[] GetAnimationList() { ThrowIfDisposed(); return _libraries.SelectMany(item => item.Value.Library.GetAnimationList().Select(name => item.Key.Length == 0 ? name : item.Key + "/" + name)).Order(StringComparer.Ordinal).ToArray(); }
    /// <summary>Finds the first qualified name of a borrowed animation, or empty.</summary>
    /// <param name="animation">The live borrowed animation resource.</param>
    public string FindAnimation(Animation animation) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(animation); foreach (var name in GetAnimationList()) if (ReferenceEquals(GetAnimation(name), animation)) return name; return ""; }
    /// <summary>Finds an animation's first library namespace, or empty for the default or absent library.</summary>
    /// <param name="animation">The borrowed animation resource.</param>
    public string FindAnimationLibrary(Animation animation) { var name = FindAnimation(animation); var slash = name.IndexOf('/'); return slash < 0 ? "" : name[..slash]; }
    /// <summary>Advances the controller by finite signed seconds when active.</summary>
    /// <param name="delta">Finite signed elapsed seconds; automatic updates use the inherited scaled delta.</param>
    public void Advance(double delta) { EnsureAnimationMutable(); Animation.Finite(delta); if (_active) AdvanceAnimation(delta); }
    /// <summary>Clears all cached typed target bindings.</summary>
    public void ClearCaches() { EnsureAnimationMutable(); InvalidateBindings(); CachesCleared?.Invoke(); }
    /// <summary>Evaluates a controller update. The base mixer has no playback source.</summary>
    internal virtual void AdvanceAnimation(double delta) { }
    internal void ApplyAnimation(Animation animation, double time, bool backward, double? previous = null)
    {
        if (animation.IsDisposed) return;
        if (!ReferenceEquals(_cachedAnimation, animation) || _cachedRevision != animation.ChangeRevision)
        {
            var root = _rootNode.Length == 0 ? this : GetNodeOrNull(_rootNode);
            _bindings = new AnimationBinding?[animation.GetTrackCount()];
            if (root is not null) for (var i = 0; i < _bindings.Length; i++) _bindings[i] = animation.Get(i).Bind(root);
            _cachedAnimation = animation; _cachedRevision = animation.ChangeRevision;
        }
        var bindings = _bindings; var revision = animation.ChangeRevision; var generation = _bindingGeneration;
        for (var i = 0; i < bindings.Length; i++) { if (IsDisposed || animation.IsDisposed || animation.ChangeRevision != revision || !ReferenceEquals(_cachedAnimation, animation)) break; bindings[i]?.Apply(animation, time, backward, previous, this, generation); }
    }
    internal bool IsBindingCurrent(Animation animation, long generation) => !IsDisposed && _active && _bindingGeneration == generation && ReferenceEquals(_cachedAnimation, animation);
    internal void EnsureAnimationMutable() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    internal void InvalidateBindings() { _bindingGeneration++; _cachedAnimation = null; _bindings = []; }
    internal void Started(string name) => AnimationStarted?.Invoke(name);
    internal void Finished(string name) => AnimationFinished?.Invoke(name);
    private void TreeMutated(SceneTree tree) => InvalidateBindings();
    private void UpdateScheduling() => SetInternalProcessing(_active && _callbackModeProcess == AnimationCallbackModeProcess.Idle, _active && _callbackModeProcess == AnimationCallbackModeProcess.Physics);
    /// <summary>Initializes internal idle animation scheduling.</summary>
    public AnimationMixer() { UpdateScheduling(); }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what); if (IsDisposed) return;
        if (what == NotificationEnterTree) { _observedTree = Tree; if (_observedTree is not null) _observedTree.TreeChanged += TreeMutated; InvalidateBindings(); }
        else if (what == NotificationExitTree) { if (_observedTree is not null) _observedTree.TreeChanged -= TreeMutated; _observedTree = null; InvalidateBindings(); }
        else if (what == NotificationInternalProcess) Advance(ProcessDeltaTime);
        else if (what == NotificationInternalPhysicsProcess) Advance(PhysicsProcessDeltaTime);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    { if (disposing) { if (_observedTree is not null) _observedTree.TreeChanged -= TreeMutated; _observedTree = null; foreach (var item in _libraries.Values) item.Library.Changed -= item.Changed; _libraries.Clear(); InvalidateBindings(); AnimationLibrariesUpdated = null; AnimationListChanged = null; AnimationStarted = null; AnimationFinished = null; CachesCleared = null; } base.Dispose(disposing); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(AnimationProperties);

}
