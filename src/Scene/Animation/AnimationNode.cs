namespace Electron2D;

/// <summary>A reusable animation-graph resource with typed parameters, inputs and track filters.</summary>
/// <remarks>This resource is independent of scene Node. Process-local helpers operate only while a tree evaluates
/// the resource; parameters and playback state belong to the tree and path, allowing resource sharing.</remarks>
public class AnimationNode : Resource
{
    private static readonly PropertyDescriptor[] GraphProperties =
    [
        new PropertyDescriptor<AnimationNode, bool>(nameof(FilterEnabled), n => n.FilterEnabled, (n, v) => n.FilterEnabled = v, _ => false),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(GraphProperties);

    /// <summary>Controls how an enabled track filter affects a child contribution.</summary>
    public enum FilterAction
    {
        /// <summary>Multiply all incoming track weights.</summary>
        Ignore = 0,
        /// <summary>Only selected paths pass.</summary>
        Pass = 1,
        /// <summary>Selected paths are stopped.</summary>
        Stop = 2,
        /// <summary>Selected paths blend; other paths pass unchanged.</summary>
        Blend = 3,
    }
    /// <summary>Read-only length of the previously processed node timeline in seconds.</summary>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public static readonly AnimationParameter<double> CurrentLength = new("current_length", 0, true);
    /// <summary>Read-only position of the previously processed node timeline in seconds.</summary>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public static readonly AnimationParameter<double> CurrentPosition = new("current_position", 0, true);
    /// <summary>Read-only effective delta of the previously processed node timeline in seconds.</summary>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public static readonly AnimationParameter<double> CurrentDelta = new("current_delta", 0, true);
    private static readonly AnimationParameter[] TimeParameters = [CurrentLength, CurrentPosition, CurrentDelta];
    private readonly List<string> _inputs = [];
    private readonly HashSet<string> _filters = new(StringComparer.Ordinal);
    private bool _filterEnabled;
    internal AnimationGraphContext? Context;
    internal event Action<int>? InputRemoved;
    /// <summary>Gets or sets whether supported child blends use this node's path filter.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public bool FilterEnabled { get { ThrowIfDisposed(); return _filterEnabled; } set { ThrowIfDisposed(); _filterEnabled = value; EmitGraphChanged(); } }
    /// <summary>Occurs after graph structure or parameter definitions change.</summary>
    public event Action? TreeChanged;
    /// <summary>Occurs after a named graph node is removed.</summary>
    public event Action<ulong, string>? AnimationNodeRemoved;
    /// <summary>Occurs after a named graph node is renamed.</summary>
    public event Action<ulong, string, string>? AnimationNodeRenamed;
    /// <summary>Occurs after input definitions or a named child connection changes.</summary>
    public event Action<ulong>? NodeUpdated;
    /// <summary>Adds an input; root resources and names with slash/dot are rejected.</summary>
    /// <param name="name">The non-null input caption; duplicates and empty captions are accepted.</param>
    /// <returns>Whether the input was added.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <remarks>Overrides preserve associated input policy storage before delivering committed edit notifications.</remarks>
    public virtual bool AddInput(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); if (this is AnimationRootNode || name.Contains('/') || name.Contains('.')) return false; _inputs.Add(name); EmitGraphChanged(); Updated(InstanceID); return true; }
    /// <summary>Removes an existing input.</summary>
    /// <param name="index">The zero-based input index.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <remarks>Overrides remove associated policy storage before invoking this committed input removal.</remarks>
    public virtual void RemoveInput(int index) { ThrowIfDisposed(); _inputs.RemoveAt(index); InputRemoved?.Invoke(index); EmitGraphChanged(); Updated(InstanceID); }
    /// <summary>Renames an input, rejecting slash/dot captions.</summary>
    /// <param name="input">The existing zero-based input index.</param>
    /// <param name="name">The non-null caption.</param>
    /// <returns>Whether the caption was accepted.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public virtual bool SetInputName(int input, string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); if (name.Contains('/') || name.Contains('.')) return false; _inputs[input] = name; EmitGraphChanged(); Updated(InstanceID); return true; }
    /// <summary>Returns an input caption.</summary>
    /// <param name="input">The zero-based index.</param>
    /// <returns>The exact caption.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public string GetInputName(int input) { ThrowIfDisposed(); return _inputs[input]; }
    /// <summary>Returns the number of inputs.</summary>
    /// <returns>The input count.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public int GetInputCount() { ThrowIfDisposed(); return _inputs.Count; }
    /// <summary>Returns the first input matching an ordinal caption, or minus one.</summary>
    /// <param name="name">The exact non-null caption.</param>
    /// <returns>The first index or minus one.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public int FindInput(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _inputs.IndexOf(name); }
    /// <summary>Enables or removes a path from the filter.</summary>
    /// <param name="path">An exact track path including the typed property suffix.</param>
    /// <param name="enable">Whether to include the path.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public void SetFilterPath(string path, bool enable) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(path); if (enable) _filters.Add(path); else _filters.Remove(path); EmitGraphChanged(); }
    /// <summary>Tests exact filter path membership.</summary>
    /// <param name="path">The non-null track path.</param>
    /// <returns>Whether the path is selected.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public bool IsPathFiltered(string path) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(path); return _filters.Contains(path); }
    /// <summary>Reads one typed parameter of the currently processing instance.</summary>
    /// <typeparam name="TValue">The exact parameter type.</typeparam>
    /// <param name="parameter">A definition declared by this node.</param>
    /// <returns>The current typed value.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Processing context is absent, an input is disconnected, or an external write targets a read-only slot.</exception>
    /// <exception cref="ArgumentException">The typed key is absent from the selected node schema.</exception>
    public TValue GetParameter<TValue>(AnimationParameter<TValue> parameter) => Current().Instance.Get(parameter);
    /// <summary>Writes processing-local memory, including read-only diagnostic slots.</summary>
    /// <typeparam name="TValue">The exact parameter type.</typeparam>
    /// <param name="parameter">A definition declared by this node.</param>
    /// <param name="value">The typed value.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Processing context is absent, an input is disconnected, or an external write targets a read-only slot.</exception>
    /// <exception cref="ArgumentException">The typed key is absent from the selected node schema.</exception>
    public void SetParameter<TValue>(AnimationParameter<TValue> parameter, TValue value) { var context = Current(); if (!context.TestOnly) context.Instance.Set(parameter, value, true); }
    /// <summary>Returns the tree instance ID while processing, or zero outside processing.</summary>
    /// <returns>The current tree identity or zero.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public ulong GetProcessingAnimationTreeInstanceID() { ThrowIfDisposed(); return Context?.Tree.InstanceID ?? 0; }
    /// <summary>Tests whether the active evaluation suppresses writes and playback state changes.</summary>
    /// <returns>Whether the current evaluation is a test.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public bool IsProcessTesting() { ThrowIfDisposed(); return Context?.TestOnly == true; }
    /// <summary>Processes an input of the current blend-tree instance.</summary>
    /// <param name="inputIndex">The existing input index.</param>
    /// <param name="time">Relative seconds, or an absolute seek position.</param>
    /// <param name="seek">Whether time is absolute.</param>
    /// <param name="isExternalSeeking">Whether seeking is external to graph startup.</param>
    /// <param name="blend">The finite signed contribution.</param>
    /// <param name="filter">The child path filtering action.</param>
    /// <param name="sync">Whether zero-weight branches advance.</param>
    /// <param name="testOnly">Whether to suppress persistent changes.</param>
    /// <returns>The selected child's remaining timeline duration.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Processing context is absent, an input is disconnected, or an external write targets a read-only slot.</exception>
    public double BlendInput(int inputIndex, double time, bool seek, bool isExternalSeeking, double blend, FilterAction filter = FilterAction.Ignore, bool sync = true, bool testOnly = false)
    { var c = Current(); var child = c.Instance.Inputs[inputIndex] ?? throw new InvalidOperationException($"Nothing connected to '{c.Instance.Path}' input {inputIndex}."); var info = c.Tree.BlendChild(c, child, time, seek, isExternalSeeking, blend, filter, sync, testOnly); c.Result = info; c.HasTime = true; return info.Remaining; }
    /// <summary>Processes a named child resource of the active graph instance.</summary>
    /// <param name="name">The exact local child name.</param>
    /// <param name="node">The live declared child resource.</param>
    /// <param name="time">Relative seconds, or an absolute seek position.</param>
    /// <param name="seek">Whether time is absolute.</param>
    /// <param name="isExternalSeeking">Whether seeking is external.</param>
    /// <param name="blend">The signed contribution.</param>
    /// <param name="filter">The filtering action.</param>
    /// <param name="sync">Whether zero-weight branches advance.</param>
    /// <param name="testOnly">Whether persistent changes are suppressed.</param>
    /// <returns>The child's remaining duration.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Processing context is absent, an input is disconnected, or an external write targets a read-only slot.</exception>
    public double BlendNode(string name, AnimationNode node, double time, bool seek, bool isExternalSeeking, double blend, FilterAction filter = FilterAction.Ignore, bool sync = true, bool testOnly = false)
    { var c = Current(); var child = c.Instance.Children[name]; if (!ReferenceEquals(child.Definition, node)) throw new ArgumentException("Child resource differs from the prepared graph.", nameof(node)); var info = c.Tree.BlendChild(c, child, time, seek, isExternalSeeking, blend, filter, sync, testOnly); c.Result = info; c.HasTime = true; return info.Remaining; }
    /// <summary>Adds a named clip to the active tree's typed property mixer.</summary>
    /// <param name="animation">The exact qualified clip name.</param>
    /// <param name="time">The clip position in seconds.</param>
    /// <param name="delta">The signed clip delta in seconds.</param>
    /// <param name="seeked">Whether the position was explicitly sought.</param>
    /// <param name="isExternalSeeking">Whether the seek is external.</param>
    /// <param name="blend">The finite signed contribution.</param>
    /// <param name="loopedFlag">The crossed endpoint flag.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Processing context is absent, an input is disconnected, or an external write targets a read-only slot.</exception>
    public void BlendAnimation(string animation, double time, double delta, bool seeked, bool isExternalSeeking, double blend, Animation.LoopedFlag loopedFlag = Animation.LoopedFlag.None)
    { var c = Current(); Animation.Valid(loopedFlag); if (!c.TestOnly) c.Tree.AddClip(c.Instance, animation, time, delta, seeked, blend); }
    /// <summary>Returns the display caption of this resource.</summary>
    /// <returns>The typed caption.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public string GetCaption() { ThrowIfDisposed(); return OnGetCaption(); }
    /// <summary>Supplies the graph resource's caption.</summary>
    /// <returns>The caption; defaults to Node.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    protected virtual string OnGetCaption() => "Node";
    /// <summary>Returns ordered named child definitions; no scene nodes are created.</summary>
    /// <returns>The graph's borrowed children.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    protected virtual IEnumerable<KeyValuePair<string, AnimationNode>> OnGetChildNodes() => [];
    /// <summary>Finds a named child definition.</summary>
    /// <param name="name">The exact local name.</param>
    /// <returns>The borrowed child or null.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    protected virtual AnimationNode? OnGetChildByName(string name) { foreach (var child in OnGetChildNodes()) if (child.Key == name) return child.Value; return null; }
    /// <summary>Supplies immutable typed parameter definitions.</summary>
    /// <returns>The node's parameter schema, including inherited timing slots.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    protected virtual IEnumerable<AnimationParameter> OnGetParameterList() => TimeParameters;
    /// <summary>Supplies one parameter's typed default for a newly prepared tree instance.</summary>
    /// <typeparam name="TValue">The exact parameter type.</typeparam>
    /// <param name="parameter">The schema definition.</param>
    /// <returns>The initial typed value.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    protected virtual TValue OnGetParameterDefaultValue<TValue>(AnimationParameter<TValue> parameter) => parameter.GetDefaultValue();
    /// <summary>Reports external write protection for a parameter.</summary>
    /// <param name="parameter">The immutable schema definition.</param>
    /// <returns>Whether external writes are blocked.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    protected virtual bool OnIsParameterReadOnly(AnimationParameter parameter) => parameter.IsReadOnly;
    /// <summary>Reports whether child blend helpers apply track filters.</summary>
    /// <returns>False for the base resource.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    protected virtual bool OnHasFilter() => false;
    /// <summary>Evaluates custom graph behavior; helpers can blend inputs, children and clips.</summary>
    /// <param name="time">Relative delta seconds, or absolute time when seeking.</param>
    /// <param name="seek">Whether time is absolute.</param>
    /// <param name="isExternalSeeking">Whether the seek originated outside startup.</param>
    /// <param name="testOnly">Whether persistent changes and mixer writes are suppressed.</param>
    /// <returns>The selected remaining duration, or zero when no input is evaluated.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    protected virtual double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly) => 0;
    internal AnimationGraphContext Current() { ThrowIfDisposed(); return Context ?? throw new InvalidOperationException("Animation-node processing helpers require an active AnimationTree evaluation."); }
    internal IEnumerable<KeyValuePair<string, AnimationNode>> Children() => OnGetChildNodes();
    internal IEnumerable<AnimationParameter> Parameters() => OnGetParameterList();
    internal TValue ParameterDefault<TValue>(AnimationParameter<TValue> key) => OnGetParameterDefaultValue(key);
    internal bool ParameterReadOnly(AnimationParameter key) => OnIsParameterReadOnly(key);
    internal bool HasFilter => OnHasFilter();
    internal double Process(double time, bool seek, bool external, bool test) => OnProcess(time, seek, external, test);
    internal static void CollectException(ref List<Exception>? errors, Exception error) => (errors ??= []).Add(error);
    internal static void ThrowCollected(string message, List<Exception>? errors) { if (errors is not null) throw new AggregateException(message, errors); }
    internal void EmitGraphChanged()
    {
        List<Exception>? errors = null;
        try { EmitChanged(); } catch (Exception error) { CollectException(ref errors, error); }
        if (TreeChanged is { } handlers) foreach (Action handler in handlers.GetInvocationList()) try { handler(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Animation graph notification failed.", errors);
    }
    internal void Removed(ulong id, string name)
    { List<Exception>? errors = null; if (AnimationNodeRemoved is { } handlers) foreach (Action<ulong, string> handler in handlers.GetInvocationList()) try { handler(id, name); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("Animation graph removal notification failed.", errors); }
    internal void Renamed(ulong id, string oldName, string newName)
    { List<Exception>? errors = null; if (AnimationNodeRenamed is { } handlers) foreach (Action<ulong, string, string> handler in handlers.GetInvocationList()) try { handler(id, oldName, newName); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("Animation graph rename notification failed.", errors); }
    internal void Updated(ulong id)
    { List<Exception>? errors = null; if (NodeUpdated is { } handlers) foreach (Action<ulong> handler in handlers.GetInvocationList()) try { handler(id); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("Animation graph input notification failed.", errors); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNode();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (AnimationNode)target; CopyNodeState(copy); }
    internal void CopyNodeState(AnimationNode copy) { copy._inputs.Clear(); copy._inputs.AddRange(_inputs); copy._filters.Clear(); foreach (var path in _filters) copy._filters.Add(path); copy._filterEnabled = _filterEnabled; }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _inputs.Clear(); _filters.Clear(); TreeChanged = null; InputRemoved = null; AnimationNodeRemoved = null; AnimationNodeRenamed = null; NodeUpdated = null; Context = null; } base.Dispose(disposing); }
}
/// <summary>Marks a resource that can serve as the root of an AnimationTree or named blend subtree.</summary>
public class AnimationRootNode : AnimationNode
{
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationRootNode();
}
/// <summary>Shared zero-weight input-clock synchronization policy for multi-input graph resources.</summary>
public class AnimationNodeSync : AnimationNode
{
    private static readonly PropertyDescriptor[] GraphProperties =
    [
        new PropertyDescriptor<AnimationNodeSync, bool>(nameof(Sync), n => n.Sync, (n, v) => n.Sync = v, _ => false),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(GraphProperties);

    private bool _sync;
    /// <summary>Gets or sets whether zero-weight inputs advance their clocks; defaults to false.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public bool Sync { get { ThrowIfDisposed(); return _sync; } set { ThrowIfDisposed(); _sync = value; EmitGraphChanged(); } }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeSync();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force) { CopyNodeState((AnimationNode)target); ((AnimationNodeSync)target)._sync = _sync; }
}
