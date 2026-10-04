namespace Electron2D;

/// <summary>A named directed graph of borrowed animation resources with one owned output resource.</summary>
/// <remarks>Each source connects to at most one input. Cycles and resource self-containment fail before mutation.
/// Editing positions are metadata; connections and input edits notify attached trees. Copies own independent
/// graph containers and preserve resource aliases under the normal duplication policy.</remarks>
public sealed class AnimationNodeBlendTree : AnimationRootNode
{
    private static readonly PropertyDescriptor[] GraphProperties =
    [
        new PropertyDescriptor<AnimationNodeBlendTree, Vector2>(nameof(GraphOffset), n => n.GraphOffset, (n, v) => n.GraphOffset = v, _ => Vector2.Zero),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(GraphProperties);

    /// <summary>A valid connection.</summary>
    public const int ConnectionOK = 0;
    /// <summary>The destination does not exist.</summary>
    public const int ConnectionErrorNoInput = 1;
    /// <summary>The destination input index is invalid.</summary>
    public const int ConnectionErrorNoInputIndex = 2;
    /// <summary>The source is missing or is the reserved output resource.</summary>
    public const int ConnectionErrorNoOutput = 3;
    /// <summary>The source and destination are identical.</summary>
    public const int ConnectionErrorSameNode = 4;
    /// <summary>An input or source connection is already occupied.</summary>
    public const int ConnectionErrorConnectionExists = 5;
    private sealed class Entry(AnimationNode node, Vector2 position, Action<Resource> changed)
    { internal readonly AnimationNode Node = node; internal Vector2 Position = position; internal string[] Inputs = new string[node.GetInputCount()]; internal readonly Action<Resource> Changed = changed; internal Action<int>? InputRemoved; }
    private readonly Dictionary<string, Entry> _nodes = new(StringComparer.Ordinal);
    private Vector2 _graphOffset;
    /// <summary>Creates the owned reserved output node.</summary>
    public AnimationNodeBlendTree() { var output = new AnimationNodeOutput(); _nodes.Add("output", new(output, Vector2.Zero, _ => { })); }
    /// <summary>Gets or sets finite editor graph-offset metadata.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A numeric value is nonfinite or outside the stated domain.</exception>
    public Vector2 GraphOffset { get { ThrowIfDisposed(); return _graphOffset; } set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _graphOffset = value; } }
    /// <summary>Occurs when a named child's content or input definition changes.</summary>
    public event Action<string>? NodeChanged;
    /// <summary>Adds a borrowed graph resource.</summary>
    /// <param name="name">A nonempty unique local name without slash; output is reserved.</param>
    /// <param name="node">A live resource other than this graph.</param>
    /// <param name="position">Finite authoring position metadata.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="ArgumentException">The name/port is invalid or already occupied.</exception>
    public void AddNode(string name, AnimationNode node, Vector2 position = default)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(node); ObjectDisposedException.ThrowIf(node.IsDisposed, node);
        if (string.IsNullOrEmpty(name) || name.Contains('/') || _nodes.ContainsKey(name)) throw new ArgumentException("Invalid or occupied graph name.", nameof(name));
        if (Contains(node, this, new HashSet<AnimationNode>(ReferenceEqualityComparer.Instance))) throw new ArgumentException("Animation graph contains a resource cycle.", nameof(node));
        if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position));
        Action<Resource> changed = _ => ChildChanged(name); var entry = new Entry(node, position, changed); _nodes.Add(name, entry); AttachInputRemoval(entry); node.Changed += changed; node.AnimationNodeRemoved += ForwardRemoved; node.AnimationNodeRenamed += ForwardRenamed; node.NodeUpdated += ForwardUpdated; EmitGraphChanged();
    }
    internal static bool Contains(AnimationNode node, AnimationNode target, HashSet<AnimationNode> visited) { if (ReferenceEquals(node, target)) return true; if (!visited.Add(node)) return false; foreach (var child in node.Children()) if (Contains(child.Value, target, visited)) return true; return false; }
    private static void AttachInputRemoval(Entry entry) { entry.InputRemoved = index => { entry.Inputs = entry.Inputs.Where((_, i) => i != index).ToArray(); }; entry.Node.InputRemoved += entry.InputRemoved; }
    private void ForwardRemoved(ulong id, string name) => Removed(id, name);
    private void ForwardRenamed(ulong id, string oldName, string newName) => Renamed(id, oldName, newName);
    private void ForwardUpdated(ulong id) => Updated(id);
    private void ChildChanged(string name)
    {
        if (IsDisposed || !_nodes.TryGetValue(name, out var entry)) return;
        var count = entry.Node.GetInputCount(); if (entry.Inputs.Length != count) Array.Resize(ref entry.Inputs, count);
        EmitGraphChanged(); NodeChanged?.Invoke(name);
    }
    /// <summary>Returns a borrowed named resource, including the owned output resource.</summary>
    /// <param name="name">The exact local name.</param>
    /// <returns>The live graph resource.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public AnimationNode GetNode(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _nodes[name].Node; }
    /// <summary>Tests exact local name membership.</summary>
    /// <param name="name">The non-null name.</param>
    /// <returns>Whether the node exists.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public bool HasNode(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _nodes.ContainsKey(name); }
    /// <summary>Returns an independent ordinal-sorted array of local names.</summary>
    /// <returns>The graph names, including output.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public string[] GetNodeList() { ThrowIfDisposed(); var names = _nodes.Keys.ToArray(); Array.Sort(names, StringComparer.Ordinal); return names; }
    /// <summary>Returns finite authoring-position metadata.</summary>
    /// <param name="name">The existing name.</param>
    /// <returns>The stored position.</returns>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public Vector2 GetNodePosition(string name) { ThrowIfDisposed(); return _nodes[name].Position; }
    /// <summary>Changes authoring-position metadata without changing playback.</summary>
    /// <param name="name">The existing local name.</param>
    /// <param name="position">The finite position.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A numeric value is nonfinite or outside the stated domain.</exception>
    public void SetNodePosition(string name, Vector2 position) { ThrowIfDisposed(); if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position)); _nodes[name].Position = position; }
    /// <summary>Removes a borrowed node and every connection referencing it; output cannot be removed.</summary>
    /// <param name="name">The existing non-output name.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public void RemoveNode(string name)
    { ThrowIfDisposed(); if (name == "output") throw new ArgumentException("Output is reserved.", nameof(name)); var entry = _nodes[name]; Detach(entry); _nodes.Remove(name); foreach (var other in _nodes.Values) for (var i = 0; i < other.Inputs.Length; i++) if (other.Inputs[i] == name) other.Inputs[i] = ""; try { Removed(InstanceID, name); } finally { EmitGraphChanged(); } }
    /// <summary>Renames a borrowed node and updates its connection names.</summary>
    /// <param name="name">The existing non-output name.</param>
    /// <param name="newName">The valid unoccupied replacement name.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="ArgumentException">The name/port is invalid or already occupied.</exception>
    public void RenameNode(string name, string newName)
    {
        ThrowIfDisposed(); if (name == "output" || string.IsNullOrEmpty(newName) || newName.Contains('/') || _nodes.ContainsKey(newName)) throw new ArgumentException("Invalid rename.", nameof(newName));
        var old = _nodes[name]; Detach(old); _nodes.Remove(name); Action<Resource> changed = _ => ChildChanged(newName); var entry = new Entry(old.Node, old.Position, changed) { Inputs = old.Inputs }; _nodes.Add(newName, entry); AttachInputRemoval(entry); entry.Node.Changed += changed; entry.Node.AnimationNodeRemoved += ForwardRemoved; entry.Node.AnimationNodeRenamed += ForwardRenamed; entry.Node.NodeUpdated += ForwardUpdated;
        foreach (var other in _nodes.Values) for (var i = 0; i < other.Inputs.Length; i++) if (other.Inputs[i] == name) other.Inputs[i] = newName;
        try { Renamed(InstanceID, name, newName); } finally { EmitGraphChanged(); }
    }
    /// <summary>Connects a source node to one destination input, rejecting occupancy and cycles.</summary>
    /// <param name="inputNode">The destination name, including output.</param>
    /// <param name="inputIndex">The destination input index.</param>
    /// <param name="outputNode">The source name; output is not a source.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="ArgumentException">The name/port is invalid or already occupied.</exception>
    public void ConnectNode(string inputNode, int inputIndex, string outputNode)
    {
        ThrowIfDisposed(); var error = ConnectionError(inputNode, inputIndex, outputNode); if (error != ConnectionOK) throw new ArgumentException($"Connection rejected ({error}).");
        if (Reaches(outputNode, inputNode, new HashSet<string>(StringComparer.Ordinal))) throw new InvalidOperationException("Animation input connection would form a cycle.");
        _nodes[inputNode].Inputs[inputIndex] = outputNode; EmitGraphChanged(); Updated(_nodes[inputNode].Node.InstanceID);
    }
    private int ConnectionError(string input, int index, string output)
    {
        if (!_nodes.ContainsKey(output) || output == "output") return ConnectionErrorNoOutput;
        if (!_nodes.TryGetValue(input, out var entry)) return ConnectionErrorNoInput;
        if (input == output) return ConnectionErrorSameNode;
        if ((uint)index >= (uint)entry.Inputs.Length) return ConnectionErrorNoInputIndex;
        if (!string.IsNullOrEmpty(entry.Inputs[index])) return ConnectionErrorConnectionExists;
        foreach (var other in _nodes.Values) foreach (var source in other.Inputs) if (source == output) return ConnectionErrorConnectionExists;
        return ConnectionOK;
    }
    private bool Reaches(string from, string target, HashSet<string> visited) { if (from == target) return true; if (!visited.Add(from)) return false; foreach (var source in _nodes[from].Inputs) if (!string.IsNullOrEmpty(source) && Reaches(source, target, visited)) return true; return false; }
    /// <summary>Disconnects an existing destination input.</summary>
    /// <param name="inputNode">The existing destination name.</param>
    /// <param name="inputIndex">The input index.</param>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public void DisconnectNode(string inputNode, int inputIndex) { ThrowIfDisposed(); _nodes[inputNode].Inputs[inputIndex] = ""; EmitGraphChanged(); Updated(_nodes[inputNode].Node.InstanceID); }
    internal string Connection(string name, int index) { var entry = _nodes[name]; var count = entry.Node.GetInputCount(); if (entry.Inputs.Length != count) Array.Resize(ref entry.Inputs, count); return entry.Inputs[index] ?? ""; }
    /// <inheritdoc />
    protected override IEnumerable<KeyValuePair<string, AnimationNode>> OnGetChildNodes() { foreach (var node in _nodes) yield return new(node.Key, node.Value.Node); }
    /// <inheritdoc />
    protected override AnimationNode? OnGetChildByName(string name) => _nodes.TryGetValue(name, out var entry) ? entry.Node : null;
    /// <inheritdoc />
    protected override string OnGetCaption() => "BlendTree";
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly) => BlendNode("output", _nodes["output"].Node, time, seek, isExternalSeeking, 1, testOnly: testOnly);
    private void Detach(Entry entry) { entry.Node.InputRemoved -= entry.InputRemoved; entry.Node.Changed -= entry.Changed; entry.Node.AnimationNodeRemoved -= ForwardRemoved; entry.Node.AnimationNodeRenamed -= ForwardRenamed; entry.Node.NodeUpdated -= ForwardUpdated; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeBlendTree();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    {
        var copy = (AnimationNodeBlendTree)target; CopyNodeState(copy); foreach (var name in copy._nodes.Keys.Where(n => n != "output").ToArray()) copy.RemoveNode(name);
        foreach (var (name, entry) in _nodes) if (name != "output") copy.AddNode(name, deep ? (AnimationNode)duplicate(entry.Node)! : entry.Node, entry.Position);
        foreach (var (name, entry) in _nodes) { copy._nodes[name].Position = entry.Position; copy._nodes[name].Inputs = (string[])entry.Inputs.Clone(); }
        copy._graphOffset = _graphOffset;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { List<Exception>? errors = null; if (disposing) { foreach (var (name, entry) in _nodes) { try { if (name == "output") entry.Node.Dispose(); else Detach(entry); } catch (Exception error) { CollectException(ref errors, error); } } _nodes.Clear(); NodeChanged = null; } try { base.Dispose(disposing); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("Animation graph cleanup failed.", errors); }
}
/// <summary>The owned output pass-through of an AnimationNodeBlendTree.</summary>
public sealed class AnimationNodeOutput : AnimationNode
{
    /// <summary>Creates one output input named output.</summary>
    public AnimationNodeOutput() { AddInput("output"); }
    /// <inheritdoc />
    protected override string OnGetCaption() => "Output";
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly) => BlendInput(0, time, seek, isExternalSeeking, 1, testOnly: testOnly);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeOutput();
}
