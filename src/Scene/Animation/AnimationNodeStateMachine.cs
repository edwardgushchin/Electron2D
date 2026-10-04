namespace Electron2D;

/// <summary>Selects state-machine startup, completion and ancestry semantics.</summary>
public enum AnimationStateMachineType
{
    /// <summary>Internal seek to zero restarts at Start; End exits the machine.</summary>
    Root = 0,
    /// <summary>Internal seek resets the current state; a terminal state also reports completion.</summary>
    Nested = 1,
    /// <summary>A parent machine controls this group through its Start and End boundary edges.</summary>
    Grouped = 2,
}
/// <summary>A reusable graph of animation states with per-tree playback and borrowed edge policies.</summary>
/// <remarks>Start and End are owned boundary resources. Other states and transitions remain borrowed.
/// Names are ordinal and cannot contain slash. Graph positions affect geometric travel cost.
/// In-memory authoring runs on the owner thread; there is no disk persistence factory.</remarks>
public sealed class AnimationNodeStateMachine : AnimationRootNode
{
    /// <summary>The read-only per-tree/path playback controller, owned by the tree.</summary>
    public static readonly AnimationParameter<AnimationNodeStateMachinePlayback> Playback = new("playback", null!, true);
    internal sealed class State(AnimationNode node, Vector2 position)
    { internal AnimationNode Node = node; internal Vector2 Position = position; }
    internal sealed class Edge(string from, string to, AnimationNodeStateMachineTransition policy)
    { internal string From = from, To = to; internal readonly AnimationNodeStateMachineTransition Policy = policy; }
    internal readonly Dictionary<string, State> States = new(StringComparer.Ordinal);
    internal readonly List<Edge> Edges = [];
    private readonly Dictionary<AnimationNode, int> _children = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<AnimationNodeStateMachineTransition, int> _policies = new(ReferenceEqualityComparer.Instance);
    private readonly AnimationRootNode _start = new(), _end = new();
    private AnimationStateMachineType _type;
    private bool _self, _resetEnds;
    private Vector2 _offset;
    /// <summary>Creates the owned Start and End boundary states.</summary>
    public AnimationNodeStateMachine() { States.Add("Start", new(_start, new(200, 100))); States.Add("End", new(_end, new(900, 100))); }
    /// <summary>Gets or sets root, nested or grouped execution semantics.</summary>
    /// <value>Initially Root.</value>
    /// <exception cref="ArgumentOutOfRangeException">The enumeration is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public AnimationStateMachineType StateMachineType { get { ThrowIfDisposed(); return _type; } set { ThrowIfDisposed(); Animation.Valid(value); _type = value; EmitGraphChanged(); } }
    /// <summary>Gets or sets whether travel to the current state may teleport and reset it.</summary>
    /// <value>Initially false.</value>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public bool AllowTransitionToSelf { get { ThrowIfDisposed(); return _self; } set { ThrowIfDisposed(); _self = value; EmitGraphChanged(); } }
    /// <summary>Gets or sets whether fades through Start/End blend against the mixer rest pose.</summary>
    /// <value>Initially false; grouped boundaries always preserve the surrounding pose.</value>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public bool ResetEnds { get { ThrowIfDisposed(); return _resetEnds; } set { ThrowIfDisposed(); _resetEnds = value; EmitGraphChanged(); } }
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AnimationNodeStateMachine, AnimationStateMachineType>(nameof(StateMachineType), n => n.StateMachineType, (n, v) => n.StateMachineType = v, _ => AnimationStateMachineType.Root),
        new PropertyDescriptor<AnimationNodeStateMachine, bool>(nameof(AllowTransitionToSelf), n => n.AllowTransitionToSelf, (n, v) => n.AllowTransitionToSelf = v, _ => false),
        new PropertyDescriptor<AnimationNodeStateMachine, bool>(nameof(ResetEnds), n => n.ResetEnds, (n, v) => n.ResetEnds = v, _ => false),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    private static void Name(string name) { ArgumentException.ThrowIfNullOrWhiteSpace(name); if (name.Contains('/')) throw new ArgumentException("State names cannot contain slash.", nameof(name)); }
    private void Editable(string name) { if (name is "Start" or "End") throw new ArgumentException("Boundary states are owned by the machine.", nameof(name)); _ = States[name]; }
    private void Validate(AnimationNode node) { ArgumentNullException.ThrowIfNull(node); ObjectDisposedException.ThrowIf(node.IsDisposed, node); if (node is not AnimationRootNode || AnimationNodeBlendTree.Contains(node, this, new(ReferenceEqualityComparer.Instance))) throw new ArgumentException("States require root resources without containment cycles.", nameof(node)); }
    private void Attach(AnimationNode node)
    {
        if (_children.TryGetValue(node, out var count)) { _children[node] = count + 1; return; }
        _children.Add(node, 1); node.TreeChanged += ChildChanged; node.AnimationNodeRemoved += ForwardRemoved; node.AnimationNodeRenamed += ForwardRenamed; node.NodeUpdated += ForwardUpdated;
    }
    private void Detach(AnimationNode node)
    {
        if (_children[node] > 1) { _children[node]--; return; }
        _children.Remove(node); node.TreeChanged -= ChildChanged; node.AnimationNodeRemoved -= ForwardRemoved; node.AnimationNodeRenamed -= ForwardRenamed; node.NodeUpdated -= ForwardUpdated;
    }
    private void ChildChanged() => EmitGraphChanged();
    private void ForwardRemoved(ulong id, string name) => Removed(id, name);
    private void ForwardRenamed(ulong id, string from, string to) => Renamed(id, from, to);
    private void ForwardUpdated(ulong id) => Updated(id);
    private void PolicyChanged() => EmitGraphChanged();
    private void Attach(AnimationNodeStateMachineTransition policy) { if (_policies.TryGetValue(policy, out var count)) { _policies[policy] = count + 1; return; } _policies.Add(policy, 1); policy.PolicyUpdated += PolicyChanged; }
    private void Detach(AnimationNodeStateMachineTransition policy) { if (_policies[policy] > 1) { _policies[policy]--; return; } _policies.Remove(policy); policy.PolicyUpdated -= PolicyChanged; }
    /// <summary>Adds a borrowed root resource as a named state.</summary>
    /// <param name="name">A unique nonblank local name without slash.</param>
    /// <param name="node">A live root resource without containment cycles.</param>
    /// <param name="position">Finite authoring coordinates, used by travel cost.</param>
    /// <exception cref="ArgumentException">The name or resource is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The position is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The machine or child is disposed.</exception>
    public void AddNode(string name, AnimationNode node, Vector2 position = default) { ThrowIfDisposed(); Name(name); Validate(node); if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position)); States.Add(name, new(node, position)); Attach(node); EmitGraphChanged(); }
    /// <summary>Replaces an ordinary state's borrowed resource while preserving position and edges.</summary>
    /// <param name="name">An existing ordinary state.</param>
    /// <param name="node">A live root resource without containment cycles.</param>
    /// <exception cref="ArgumentException">The state is reserved or the child is invalid.</exception>
    /// <exception cref="KeyNotFoundException">The state is absent.</exception>
    /// <exception cref="ObjectDisposedException">The machine or child is disposed.</exception>
    public void ReplaceNode(string name, AnimationNode node) { ThrowIfDisposed(); Editable(name); Validate(node); var state = States[name]; Detach(state.Node); state.Node = node; Attach(node); EmitGraphChanged(); }
    /// <summary>Returns the borrowed state resource, including owned boundaries.</summary>
    /// <param name="name">An existing local name.</param>
    /// <returns>The state resource.</returns>
    /// <exception cref="KeyNotFoundException">The state is absent.</exception>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public AnimationNode GetNode(string name) { ThrowIfDisposed(); return States[name].Node; }
    /// <summary>Tests exact local state membership.</summary>
    /// <param name="name">The non-null name.</param>
    /// <returns>Whether the state exists.</returns>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public bool HasNode(string name) { ThrowIfDisposed(); return States.ContainsKey(name); }
    /// <summary>Returns an independent ordinal-sorted state-name snapshot.</summary>
    /// <returns>Names including Start and End.</returns>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public string[] GetNodeList() { ThrowIfDisposed(); var result = States.Keys.ToArray(); Array.Sort(result, StringComparer.Ordinal); return result; }
    /// <summary>Returns the first name using this exact resource, or an empty string.</summary>
    /// <param name="node">The borrowed resource identity.</param>
    /// <returns>A local name or empty string.</returns>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public string GetNodeName(AnimationNode node) { ThrowIfDisposed(); foreach (var state in States) if (ReferenceEquals(state.Value.Node, node)) return state.Key; return ""; }
    /// <summary>Removes an ordinary state and all incident edges, retaining borrowed resources.</summary>
    /// <param name="name">An existing ordinary state.</param>
    /// <exception cref="ArgumentException">The state is reserved.</exception>
    /// <exception cref="KeyNotFoundException">The state is absent.</exception>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public void RemoveNode(string name)
    {
        ThrowIfDisposed(); Editable(name); var node = States[name].Node; States.Remove(name); Detach(node);
        for (var i = Edges.Count - 1; i >= 0; i--) if (Edges[i].From == name || Edges[i].To == name) { Detach(Edges[i].Policy); Edges.RemoveAt(i); }
        try { Removed(InstanceID, name); } finally { EmitGraphChanged(); }
    }
    /// <summary>Renames an ordinary state and its edges; prepared playback retains the state's identity.</summary>
    /// <param name="name">An existing ordinary state.</param>
    /// <param name="newName">A unique valid local name.</param>
    /// <exception cref="ArgumentException">A name is invalid, occupied or reserved.</exception>
    /// <exception cref="KeyNotFoundException">The source is absent.</exception>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public void RenameNode(string name, string newName) { ThrowIfDisposed(); Editable(name); Name(newName); var state = States[name]; States.Add(newName, state); States.Remove(name); foreach (var edge in Edges) { if (edge.From == name) edge.From = newName; if (edge.To == name) edge.To = newName; } try { Renamed(InstanceID, name, newName); } finally { EmitGraphChanged(); } }
    /// <summary>Sets finite state coordinates used by travel.</summary>
    /// <param name="name">An existing state, including boundaries.</param>
    /// <param name="position">Finite coordinates.</param>
    /// <exception cref="ArgumentOutOfRangeException">The coordinates are nonfinite.</exception>
    /// <exception cref="KeyNotFoundException">The state is absent.</exception>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public void SetNodePosition(string name, Vector2 position) { ThrowIfDisposed(); if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position)); States[name].Position = position; EmitGraphChanged(); }
    /// <summary>Returns a state's authoring coordinates.</summary>
    /// <param name="name">An existing state.</param>
    /// <returns>The finite coordinates.</returns>
    /// <exception cref="KeyNotFoundException">The state is absent.</exception>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public Vector2 GetNodePosition(string name) { ThrowIfDisposed(); return States[name].Position; }
    /// <summary>Stores finite graph-view offset metadata.</summary>
    /// <param name="offset">Finite coordinates.</param>
    /// <exception cref="ArgumentOutOfRangeException">The coordinates are nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public void SetGraphOffset(Vector2 offset) { ThrowIfDisposed(); if (!offset.IsFinite()) throw new ArgumentOutOfRangeException(nameof(offset)); _offset = offset; EmitChanged(); }
    /// <summary>Returns graph-view offset metadata.</summary>
    /// <returns>The stored coordinates.</returns>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public Vector2 GetGraphOffset() { ThrowIfDisposed(); return _offset; }
    /// <summary>Adds a unique directed borrowed policy between local states.</summary>
    /// <param name="from">An existing source other than End.</param>
    /// <param name="to">An existing destination other than Start or the source.</param>
    /// <param name="transition">A live borrowed edge policy.</param>
    /// <exception cref="ArgumentException">The edge is invalid or occupied.</exception>
    /// <exception cref="KeyNotFoundException">A state is absent.</exception>
    /// <exception cref="ObjectDisposedException">The machine or policy is disposed.</exception>
    public void AddTransition(string from, string to, AnimationNodeStateMachineTransition transition) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(transition); ObjectDisposedException.ThrowIf(transition.IsDisposed, transition); _ = States[from]; _ = States[to]; if (from == to || from == "End" || to == "Start" || HasTransition(from, to)) throw new ArgumentException("Invalid or occupied state edge."); Edges.Add(new(from, to, transition)); Attach(transition); EmitGraphChanged(); }
    /// <summary>Tests exact directed edge membership.</summary>
    /// <param name="from">The source name.</param>
    /// <param name="to">The destination name.</param>
    /// <returns>Whether the edge exists.</returns>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public bool HasTransition(string from, string to) { ThrowIfDisposed(); return Edges.Any(e => e.From == from && e.To == to); }
    /// <summary>Returns the current edge count.</summary>
    /// <returns>The count.</returns>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public int GetTransitionCount() { ThrowIfDisposed(); return Edges.Count; }
    /// <summary>Returns a borrowed edge policy.</summary>
    /// <param name="idx">An existing edge index.</param>
    /// <returns>The borrowed policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the edge list.</exception>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public AnimationNodeStateMachineTransition GetTransition(int idx) { ThrowIfDisposed(); return Edges[idx].Policy; }
    /// <summary>Returns an edge's source name.</summary>
    /// <param name="idx">An existing edge index.</param>
    /// <returns>The exact local source.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the edge list.</exception>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public string GetTransitionFrom(int idx) { ThrowIfDisposed(); return Edges[idx].From; }
    /// <summary>Returns an edge's destination name.</summary>
    /// <param name="idx">An existing edge index.</param>
    /// <returns>The exact local destination.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the edge list.</exception>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public string GetTransitionTo(int idx) { ThrowIfDisposed(); return Edges[idx].To; }
    /// <summary>Removes an edge while retaining its borrowed policy.</summary>
    /// <param name="idx">An existing edge index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the edge list.</exception>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public void RemoveTransitionByIndex(int idx) { ThrowIfDisposed(); var edge = Edges[idx]; Edges.RemoveAt(idx); Detach(edge.Policy); EmitGraphChanged(); }
    /// <summary>Removes an exact edge.</summary>
    /// <param name="from">The source name.</param>
    /// <param name="to">The destination name.</param>
    /// <exception cref="ArgumentException">The edge is absent.</exception>
    /// <exception cref="ObjectDisposedException">The machine is disposed.</exception>
    public void RemoveTransition(string from, string to) { ThrowIfDisposed(); var index = Edges.FindIndex(e => e.From == from && e.To == to); if (index < 0) throw new ArgumentException("State edge is absent."); RemoveTransitionByIndex(index); }
    /// <inheritdoc />
    protected override IEnumerable<KeyValuePair<string, AnimationNode>> OnGetChildNodes() { foreach (var state in States) yield return new(state.Key, state.Value.Node); }
    /// <inheritdoc />
    protected override AnimationNode? OnGetChildByName(string name) => States.TryGetValue(name, out var state) ? state.Node : null;
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList()
    {
        foreach (var key in base.OnGetParameterList()) yield return key; yield return Playback;
        var keys = new HashSet<AnimationParameter<bool>>(ReferenceEqualityComparer.Instance);
        foreach (var edge in Edges) if (edge.Policy.AdvanceCondition is { } key && keys.Add(key)) yield return key;
    }
    /// <inheritdoc />
    protected override TValue OnGetParameterDefaultValue<TValue>(AnimationParameter<TValue> parameter) => ReferenceEquals(parameter, Playback) ? (TValue)(object)new AnimationNodeStateMachinePlayback() : base.OnGetParameterDefaultValue(parameter);
    /// <inheritdoc />
    protected override string OnGetCaption() => "StateMachine";
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly)
    { var c = Current(); var playback = c.Instance.Get(Playback); var result = playback.Process(c); c.Result = result; c.HasTime = true; return result.Remaining; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeStateMachine();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    {
        base.CopyCustomStateTo(target, deep, mode, duplicate, force); var copy = (AnimationNodeStateMachine)target; copy._type = _type; copy._self = _self; copy._resetEnds = _resetEnds; copy._offset = _offset;
        copy.States["Start"].Position = States["Start"].Position; copy.States["End"].Position = States["End"].Position;
        foreach (var state in States) if (state.Key is not "Start" and not "End") copy.AddNode(state.Key, deep ? (AnimationNode)duplicate(state.Value.Node)! : state.Value.Node, state.Value.Position);
        foreach (var edge in Edges) copy.AddTransition(edge.From, edge.To, deep ? (AnimationNodeStateMachineTransition)duplicate(edge.Policy)! : edge.Policy);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { foreach (var node in _children.Keys.ToArray()) { _children[node] = 1; Detach(node); } foreach (var policy in _policies.Keys.ToArray()) { _policies[policy] = 1; Detach(policy); } States.Clear(); Edges.Clear(); _start.Dispose(); _end.Dispose(); }
        base.Dispose(disposing);
    }
}
