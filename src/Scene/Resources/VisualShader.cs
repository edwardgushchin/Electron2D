namespace Electron2D;

/// <summary>A typed canvas graph and its last successfully imported shader program.</summary>
/// <remarks>Graph edits are source authoring. Save the resource and run ShaderImport with --visual-shader to
/// produce a new compiled resource. Existing materials keep the previous immutable program until reloaded.
/// Added nodes are borrowed; reserved outputs and factory-created replacements belong to the graph.
/// Authoring is single-threaded cold work.</remarks>
public sealed partial class VisualShader : Shader
{
    /// <summary>The canvas stage containing a graph.</summary>
    public enum Type
    {
        /// <summary>Transforms supplied canvas vertices before rasterization.</summary>
        Vertex = 0,
        /// <summary>Computes canvas fragment color.</summary>
        Fragment = 1,
        /// <summary>The nonselectable upper bound of all shader graph stage identities.</summary>
        Max = 11,
    }
    /// <summary>The sentinel for an absent node.</summary>
    public const int NodeIDInvalid = -1;
    /// <summary>The reserved output node in each stage.</summary>
    public const int NodeIDOutput = 0;
    private const int MaximumNodes = 4096;
    private sealed class Entry(VisualShaderNode node, Vector2 position)
    { internal VisualShaderNode Node = node; internal Vector2 Position = position; }
    private sealed class Graph
    {
        internal readonly SortedDictionary<int, Entry> Nodes = [];
        internal readonly List<VisualShaderConnection> Connections = [];
    }
    private readonly Graph[] _graphs = [new(), new()];
    private readonly Dictionary<VisualShaderNode, int> _watched = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<VisualShaderNode> _ownedDefinitions = new(ReferenceEqualityComparer.Instance);
    private readonly Action<Resource> _childChanged;

    /// <summary>Creates two empty canvas graphs with independent owned output nodes.</summary>
    public VisualShader()
    {
        _childChanged = _ => EmitChanged();
        for (var i = 0; i < _graphs.Length; i++)
        {
            var output = new VisualShaderNodeOutput { Stage = (Type)i };
            _graphs[i].Nodes.Add(NodeIDOutput, new(output, Vector2.Zero)); Watch(output); _ownedDefinitions.Add(output);
        }
    }

    /// <summary>Adds a borrowed numeric operation at a finite authoring position.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <param name="node">A live operation; output nodes cannot be added.</param>
    /// <param name="position">Position metadata in graph coordinates.</param>
    /// <param name="id">A unique ID of at least two.</param>
    /// <exception cref="ArgumentException">The node, position or ID is invalid or already occupied.</exception>
    /// <exception cref="InvalidOperationException">The graph has exhausted its node budget.</exception>
    /// <exception cref="ObjectDisposedException">This graph or the supplied node is disposed.</exception>
    public void AddNode(Type type, VisualShaderNode node, Vector2 position, int id)
    {
        var graph = GetGraph(type); ArgumentNullException.ThrowIfNull(node); ObjectDisposedException.ThrowIf(node.IsDisposed, node);
        if (id < 2 || graph.Nodes.ContainsKey(id) || !position.IsFinite() || node is VisualShaderNodeOutput)
            throw new ArgumentException("Invalid node, position or graph ID.");
        if (graph.Nodes.Count >= MaximumNodes) throw new InvalidOperationException("Visual shader node capacity exceeded.");
        graph.Nodes.Add(id, new(node, position)); Watch(node); EmitChanged();
    }
    /// <summary>Returns a borrowed operation in a stage.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <param name="id">An existing node ID, including zero for the output.</param>
    /// <returns>The live borrowed operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage or node does not exist.</exception>
    /// <exception cref="ObjectDisposedException">This graph or the node is disposed.</exception>
    public VisualShaderNode GetNode(Type type, int id)
    { var node = GetEntry(type, id).Node; ObjectDisposedException.ThrowIf(node.IsDisposed, node); return node; }
    /// <summary>Copies the stage's node IDs in ascending order.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <returns>An independent ID array, including the reserved output.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public int[] GetNodeList(Type type) => GetGraph(type).Nodes.Keys.ToArray();
    /// <summary>Returns the next unused ID after the largest stage ID, with two as the minimum.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <returns>The next ID; holes below the largest ID are preserved.</returns>
    /// <exception cref="OverflowException">The largest ID is Int32.MaxValue.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public int GetValidNodeID(Type type) => Math.Max(2, checked(GetGraph(type).Nodes.Keys.Last() + 1));
    /// <summary>Reads a node's finite authoring position.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <param name="id">An existing node ID.</param>
    /// <returns>The graph-coordinate position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage or node does not exist.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public Vector2 GetNodePosition(Type type, int id) => GetEntry(type, id).Position;
    /// <summary>Changes a node's finite authoring position without changing compiled code.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <param name="id">An existing node ID.</param>
    /// <param name="position">Finite graph coordinates.</param>
    /// <exception cref="ArgumentException">The position is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The stage or node does not exist.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void SetNodePosition(Type type, int id, Vector2 position)
    { var entry = GetEntry(type, id); if (!position.IsFinite()) throw new ArgumentException("Graph position must be finite.", nameof(position)); entry.Position = position; EmitChanged(); }
    /// <summary>Removes a borrowed operation and all incident connections.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <param name="id">An existing non-output ID.</param>
    /// <exception cref="ArgumentException">The reserved output was selected.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The stage or node does not exist.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void RemoveNode(Type type, int id)
    {
        var graph = GetGraph(type); var entry = GetEntry(type, id);
        if (id == NodeIDOutput) throw new ArgumentException("The output node is reserved.", nameof(id));
        graph.Nodes.Remove(id); Unwatch(entry.Node); graph.Connections.RemoveAll(c => c.FromNode == id || c.ToNode == id);
        PublishAndRetire(TakeRetired(entry.Node));
    }
    /// <summary>Replaces an operation using an explicitly registered compiled resource factory.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <param name="id">An existing node ID of at least two.</param>
    /// <param name="newClass">The exact registered type ID, for example VisualShaderNodeVec3Constant.</param>
    /// <remarks>An unchanged runtime type is a no-op. Positions and stored edges remain; import rejects any
    /// edges whose ports no longer exist. The graph owns the replacement until its final graph alias is removed.
    /// No assembly loading, member reflection or runtime source compilation occurs.</remarks>
    /// <exception cref="ArgumentException">The ID or registered class does not identify an ordinary graph operation.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The stage or node does not exist.</exception>
    /// <exception cref="InvalidOperationException">The compiled factory violates its freshness/type contract.</exception>
    /// <exception cref="ObjectDisposedException">The graph is disposed.</exception>
    /// <exception cref="AggregateException">Independent observer and retirement failures occur after commitment.</exception>
    public void ReplaceNode(Type type, int id, string newClass)
    {
        var entry = GetEntry(type, id); ArgumentException.ThrowIfNullOrWhiteSpace(newClass);
        var factory = ResourceFileTypes.Find(newClass);
        if (id < 2 || factory is null || !typeof(VisualShaderNode).IsAssignableFrom(factory.Type) || factory.Type == typeof(VisualShaderNodeOutput))
            throw new ArgumentException("A registered graph operation and non-output ID are required.");
        if (entry.Node.GetType() == factory.Type) return;
        var replacement = (VisualShaderNode)factory.Factory();
        try { ThrowIfDisposed(); }
        catch (Exception error)
        { try { replacement.Dispose(); } catch (Exception cleanup) { throw new AggregateException(error, cleanup); } throw; }
        var previous = entry.Node; Unwatch(previous); entry.Node = replacement; Watch(replacement); _ownedDefinitions.Add(replacement);
        PublishAndRetire(TakeRetired(previous));
    }
    /// <summary>Tests whether a new numeric connection is valid and acyclic.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <param name="fromNode">The source ID.</param>
    /// <param name="fromPort">The source output.</param>
    /// <param name="toNode">The destination ID.</param>
    /// <param name="toPort">The destination input.</param>
    /// <returns>False for absent/disposed nodes, invalid ports, duplicates, self edges or cycles.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public bool CanConnectNodes(Type type, int fromNode, int fromPort, int toNode, int toPort)
    {
        var graph = GetGraph(type); var edge = new VisualShaderConnection(fromNode, fromPort, toNode, toPort);
        if (!PortsExist(graph, edge) || !PortsCompatible(graph, edge) || fromNode == toNode || graph.Connections.Contains(edge)) return false;
        var pending = new Stack<int>(); var visited = new HashSet<int>(); pending.Push(toNode);
        while (pending.TryPop(out var current))
        {
            if (current == fromNode) return false;
            if (!visited.Add(current)) continue;
            foreach (var connection in graph.Connections) if (connection.FromNode == current) pending.Push(connection.ToNode);
        }
        return true;
    }
    /// <summary>Adds a validated numeric connection.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <param name="fromNode">The source ID.</param>
    /// <param name="fromPort">The source output.</param>
    /// <param name="toNode">The destination ID.</param>
    /// <param name="toPort">The destination input.</param>
    /// <remarks>Several edges may target an input; the last authored edge supplies its value at import.</remarks>
    /// <exception cref="ArgumentException">The connection is invalid, duplicated or cyclic.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void ConnectNodes(Type type, int fromNode, int fromPort, int toNode, int toPort)
    {
        if (!CanConnectNodes(type, fromNode, fromPort, toNode, toPort)) throw new ArgumentException("Invalid visual shader connection.");
        ConnectNodesForced(type, fromNode, fromPort, toNode, toPort);
    }
    /// <summary>Records an edge between existing ports without rejecting graph cycles.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <param name="fromNode">The source ID.</param>
    /// <param name="fromPort">The source output.</param>
    /// <param name="toNode">The destination ID.</param>
    /// <param name="toPort">The destination input.</param>
    /// <remarks>Duplicate edges are unchanged. This bypasses type compatibility; import rejects incompatible ports and reachable cycles atomically.</remarks>
    /// <exception cref="ArgumentException">A selected node or port is absent or disposed.</exception>
    /// <exception cref="InvalidOperationException">The connection budget is exhausted.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void ConnectNodesForced(Type type, int fromNode, int fromPort, int toNode, int toPort)
    {
        var graph = GetGraph(type); var edge = new VisualShaderConnection(fromNode, fromPort, toNode, toPort);
        if (!PortsExist(graph, edge)) throw new ArgumentException("A graph node or port does not exist.");
        if (graph.Connections.Contains(edge)) return;
        if (graph.Connections.Count >= MaximumNodes * 8) throw new InvalidOperationException("Visual shader connection capacity exceeded.");
        graph.Connections.Add(edge); EmitChanged();
    }
    /// <summary>Removes an exact edge if present.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <param name="fromNode">The source ID.</param>
    /// <param name="fromPort">The source output.</param>
    /// <param name="toNode">The destination ID.</param>
    /// <param name="toPort">The destination input.</param>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void DisconnectNodes(Type type, int fromNode, int fromPort, int toNode, int toPort)
    { var graph = GetGraph(type); if (graph.Connections.Remove(new(fromNode, fromPort, toNode, toPort))) EmitChanged(); }
    /// <summary>Tests whether an exact edge is stored.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <param name="fromNode">The source ID.</param>
    /// <param name="fromPort">The source output.</param>
    /// <param name="toNode">The destination ID.</param>
    /// <param name="toPort">The destination input.</param>
    /// <returns>True only for the exact stored edge.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public bool IsNodeConnection(Type type, int fromNode, int fromPort, int toNode, int toPort) => GetGraph(type).Connections.Contains(new(fromNode, fromPort, toNode, toPort));
    /// <summary>Copies connections in authoring order.</summary>
    /// <param name="type">The canvas stage.</param>
    /// <returns>An independent typed edge array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public VisualShaderConnection[] GetNodeConnections(Type type) => GetGraph(type).Connections.ToArray();

    private Graph GetGraph(Type type)
    { ThrowIfDisposed(); if (type is not (Type.Vertex or Type.Fragment)) throw new ArgumentOutOfRangeException(nameof(type)); return _graphs[(int)type]; }
    private Entry GetEntry(Type type, int id) => GetGraph(type).Nodes.TryGetValue(id, out var entry) ? entry : throw new ArgumentOutOfRangeException(nameof(id));
    private static bool PortsExist(Graph graph, VisualShaderConnection c) =>
        graph.Nodes.TryGetValue(c.FromNode, out var from) && graph.Nodes.TryGetValue(c.ToNode, out var to) && !from.Node.IsDisposed && !to.Node.IsDisposed &&
        (uint)c.FromPort < (uint)from.Node.Outputs.Length && (uint)c.ToPort < (uint)to.Node.Inputs.Length;
    private static bool PortsCompatible(Graph graph, VisualShaderConnection c) =>
        (graph.Nodes[c.FromNode].Node.Outputs[c.FromPort] == VisualShaderNode.PortType.Matrix) == (graph.Nodes[c.ToNode].Node.Inputs[c.ToPort] == VisualShaderNode.PortType.Matrix);
    private void Watch(VisualShaderNode node)
    { if (_watched.TryGetValue(node, out var count)) _watched[node] = count + 1; else { _watched.Add(node, 1); node.Changed += _childChanged; } }
    private void Unwatch(VisualShaderNode node)
    { if (--_watched[node] == 0) { _watched.Remove(node); node.Changed -= _childChanged; } }
    private VisualShaderNode? TakeRetired(VisualShaderNode node) => !_watched.ContainsKey(node) && _ownedDefinitions.Remove(node) ? node : null;
    private void PublishAndRetire(VisualShaderNode? retired)
    {
        Exception? observerError = null;
        try { EmitChanged(); } catch (Exception error) { observerError = error; }
        try { retired?.Dispose(); }
        catch (Exception error) { if (observerError is not null) throw new AggregateException(observerError, error); throw; }
        if (observerError is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(observerError).Throw();
    }
}
