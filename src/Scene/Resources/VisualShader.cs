namespace Electron2D;

/// <summary>Typed editable shader graphs and their last successfully imported program.</summary>
/// <remarks>Graph edits are source authoring. Save the resource and run ShaderImport with --visual-shader to
/// produce a new compiled resource. Existing materials keep the previous immutable program until reloaded.
/// Added nodes are borrowed; reserved outputs and factory-created replacements belong to the graph.
/// Authoring is single-threaded cold work.</remarks>
public sealed partial class VisualShader : Shader
{
    /// <summary>The source stage containing a graph.</summary>
    public enum Type
    {
        /// <summary>Transforms supplied canvas vertices before rasterization.</summary>
        Vertex = 0,
        /// <summary>Computes canvas fragment color.</summary>
        Fragment = 1,
        /// <summary>Computes the contribution and shadow modulation of each selected canvas light.</summary>
        Light = 2,
        /// <summary>Initializes a newly born particle.</summary>
        Start = 3,
        /// <summary>Processes an active particle outside a contact.</summary>
        Process = 4,
        /// <summary>Processes an active particle touching the canvas distance field.</summary>
        Collide = 5,
        /// <summary>Applies custom channels after particle initialization.</summary>
        StartCustom = 6,
        /// <summary>Applies custom channels after ordinary particle processing.</summary>
        ProcessCustom = 7,
        /// <summary>Computes up to four writable-texture color outputs.</summary>
        TextureBlit = 10,
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
    private sealed class Graph(Type stage)
    {
        internal readonly Type Stage = stage;
        internal readonly SortedDictionary<int, Entry> Nodes = [];
        internal readonly List<VisualShaderConnection> Connections = [];
    }
    private static readonly Type[] GraphStages = [Type.Vertex, Type.Fragment, Type.Light, Type.Start, Type.Process, Type.Collide, Type.StartCustom, Type.ProcessCustom, Type.TextureBlit];
    private readonly Graph[] _graphs = Enumerable.Range(0, (int)Type.Max).Select(i => new Graph((Type)i)).ToArray();
    private Mode _mode = Mode.CanvasItem;
    private static bool IsGraphStage(Type stage) => stage is Type.Vertex or Type.Fragment or Type.Light or Type.Start or Type.Process or Type.Collide or Type.StartCustom or Type.ProcessCustom or Type.TextureBlit;
    private static bool IsParticleStage(Type stage) => stage is >= Type.Start and <= Type.ProcessCustom;
    private Type[] ActiveStages => _mode switch { Mode.Particles => [Type.Start, Type.Process, Type.Collide, Type.StartCustom, Type.ProcessCustom], Mode.TextureBlit => [Type.TextureBlit], _ => [Type.Vertex, Type.Fragment, Type.Light] };
    private readonly Dictionary<VisualShaderNode, int> _watched = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<VisualShaderNode> _ownedDefinitions = new(ReferenceEqualityComparer.Instance);
    private readonly Action<Resource> _childChanged;

    /// <summary>Creates independent owned outputs for the supported source stages; canvas mode is initially selected.</summary>
    public VisualShader()
    {
        _childChanged = _ => EmitChanged();
        foreach (var stage in GraphStages)
        {
            VisualShaderNodeOutput output = IsParticleStage(stage) ? new VisualShaderNodeParticleOutput { Stage = stage } : new VisualShaderNodeOutput { Stage = stage };
            _graphs[(int)stage].Nodes.Add(NodeIDOutput, new(output, Vector2.Zero)); Watch(output); _ownedDefinitions.Add(output);
        }
    }

    /// <summary>Returns the authored domain, independently of the retained compiled program.</summary>
    /// <returns>The selected CanvasItem, Particles or TextureBlit source domain.</returns>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public override Mode GetMode() { ThrowIfDisposed(); return _mode; }

    /// <summary>Selects the source domain and removes input/output boundary connections.</summary>
    /// <param name="mode">A supported shader domain.</param>
    /// <remarks>An equal mode is unchanged. All nodes, positions, numeric defaults and ordinary links remain,
    /// including inactive stages. The prior compiled program remains executable until a successful import.
    /// Schema and Changed observers run after commitment, even if another observer fails.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The domain is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void SetMode(Mode mode)
    {
        ThrowIfDisposed(); if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (_mode == mode) return;
        _mode = mode;
        foreach (var graph in _graphs)
            graph.Connections.RemoveAll(c => graph.Nodes[c.FromNode].Node is VisualShaderNodeInput or VisualShaderNodeOutput || graph.Nodes[c.ToNode].Node is VisualShaderNodeInput or VisualShaderNodeOutput);
        Exception? failure = null;
        try { NotifyPropertyListChanged(); } catch (Exception error) { failure = error; }
        try { EmitChanged(); } catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    /// <summary>Adds a borrowed numeric operation at a finite authoring position.</summary>
    /// <param name="type">The source stage.</param>
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
        if (node is VisualShaderNodeParameter parameter) parameter.ParameterName = UniqueParameterName(parameter);
        graph.Nodes.Add(id, new(node, position)); Watch(node); EmitChanged();
    }
    /// <summary>Returns a borrowed operation in a stage.</summary>
    /// <param name="type">The source stage.</param>
    /// <param name="id">An existing node ID, including zero for the output.</param>
    /// <returns>The live borrowed operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage or node does not exist.</exception>
    /// <exception cref="ObjectDisposedException">This graph or the node is disposed.</exception>
    public VisualShaderNode GetNode(Type type, int id)
    { var node = GetEntry(type, id).Node; ObjectDisposedException.ThrowIf(node.IsDisposed, node); return node; }
    /// <summary>Copies the stage's node IDs in ascending order.</summary>
    /// <param name="type">The source stage.</param>
    /// <returns>An independent ID array, including the reserved output.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public int[] GetNodeList(Type type) => GetGraph(type).Nodes.Keys.ToArray();
    /// <summary>Returns the next unused ID after the largest stage ID, with two as the minimum.</summary>
    /// <param name="type">The source stage.</param>
    /// <returns>The next ID; holes below the largest ID are preserved.</returns>
    /// <exception cref="OverflowException">The largest ID is Int32.MaxValue.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public int GetValidNodeID(Type type) => Math.Max(2, checked(GetGraph(type).Nodes.Keys.Last() + 1));
    /// <summary>Reads a node's finite authoring position.</summary>
    /// <param name="type">The source stage.</param>
    /// <param name="id">An existing node ID.</param>
    /// <returns>The graph-coordinate position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage or node does not exist.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public Vector2 GetNodePosition(Type type, int id) => GetEntry(type, id).Position;
    /// <summary>Changes a node's finite authoring position without changing compiled code.</summary>
    /// <param name="type">The source stage.</param>
    /// <param name="id">An existing node ID.</param>
    /// <param name="position">Finite graph coordinates.</param>
    /// <exception cref="ArgumentException">The position is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The stage or node does not exist.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void SetNodePosition(Type type, int id, Vector2 position)
    { var entry = GetEntry(type, id); if (!position.IsFinite()) throw new ArgumentException("Graph position must be finite.", nameof(position)); entry.Position = position; EmitChanged(); }
    /// <summary>Removes a borrowed operation and all incident connections.</summary>
    /// <param name="type">The source stage.</param>
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
    /// <param name="type">The source stage.</param>
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
        if (id < 2 || factory is null || !typeof(VisualShaderNode).IsAssignableFrom(factory.Type) || typeof(VisualShaderNodeOutput).IsAssignableFrom(factory.Type))
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
    /// <param name="type">The source stage.</param>
    /// <param name="fromNode">The source ID.</param>
    /// <param name="fromPort">The source output.</param>
    /// <param name="toNode">The destination ID.</param>
    /// <param name="toPort">The destination input.</param>
    /// <returns>False for absent/disposed nodes, invalid ports, duplicates, self edges, cycles or incompatible
    /// reroute terminals. This query never changes port roles or defaults.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public bool CanConnectNodes(Type type, int fromNode, int fromPort, int toNode, int toPort)
    {
        var graph = GetGraph(type); var edge = new VisualShaderConnection(fromNode, fromPort, toNode, toPort);
        if (!PortsExist(graph, edge) || fromNode == toNode || graph.Connections.Contains(edge) || !CanConnectReroutes(graph, edge, out _)) return false;
        var pending = new Stack<int>(); var visited = new HashSet<int>(); pending.Push(toNode);
        while (pending.TryPop(out var current))
        {
            if (current == fromNode) return false;
            if (!visited.Add(current)) continue;
            foreach (var connection in graph.Connections) if (connection.FromNode == current) pending.Push(connection.ToNode);
        }
        return true;
    }
    /// <summary>Adds a validated typed connection, inferring downstream reroute roles.</summary>
    /// <param name="type">The source stage.</param>
    /// <param name="fromNode">The source ID.</param>
    /// <param name="fromPort">The source output.</param>
    /// <param name="toNode">The destination ID.</param>
    /// <param name="toPort">The destination input.</param>
    /// <remarks>Several edges may target an input; the last authored edge supplies its value at import.
    /// Reroutes adopt the source role across their downstream branches after terminal compatibility checks.
    /// An unconnected source reroute may adopt its destination role. Changed roles reset defaults to typed
    /// zero/identity or sampler absence. The complete edit commits before change notifications.</remarks>
    /// <exception cref="ArgumentException">The connection is invalid, duplicated or cyclic.</exception>
    /// <exception cref="AggregateException">Reroute observers failed after the complete edit committed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void ConnectNodes(Type type, int fromNode, int fromPort, int toNode, int toPort)
    {
        if (!CanConnectNodes(type, fromNode, fromPort, toNode, toPort)) throw new ArgumentException("Invalid visual shader connection.");
        ConnectWithReroutes(GetGraph(type), new(fromNode, fromPort, toNode, toPort));
    }
    /// <summary>Records an edge between existing ports without rejecting graph cycles.</summary>
    /// <param name="type">The source stage.</param>
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
    /// <param name="type">The source stage.</param>
    /// <param name="fromNode">The source ID.</param>
    /// <param name="fromPort">The source output.</param>
    /// <param name="toNode">The destination ID.</param>
    /// <param name="toPort">The destination input.</param>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void DisconnectNodes(Type type, int fromNode, int fromPort, int toNode, int toPort)
    { var graph = GetGraph(type); if (graph.Connections.Remove(new(fromNode, fromPort, toNode, toPort))) EmitChanged(); }
    /// <summary>Tests whether an exact edge is stored.</summary>
    /// <param name="type">The source stage.</param>
    /// <param name="fromNode">The source ID.</param>
    /// <param name="fromPort">The source output.</param>
    /// <param name="toNode">The destination ID.</param>
    /// <param name="toPort">The destination input.</param>
    /// <returns>True only for the exact stored edge.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public bool IsNodeConnection(Type type, int fromNode, int fromPort, int toNode, int toPort) => GetGraph(type).Connections.Contains(new(fromNode, fromPort, toNode, toPort));
    /// <summary>Copies connections in authoring order.</summary>
    /// <param name="type">The source stage.</param>
    /// <returns>An independent typed edge array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public VisualShaderConnection[] GetNodeConnections(Type type) => GetGraph(type).Connections.ToArray();

    private Graph GetGraph(Type type)
    { ThrowIfDisposed(); if (!IsGraphStage(type)) throw new ArgumentOutOfRangeException(nameof(type)); return _graphs[(int)type]; }
    private Entry GetEntry(Type type, int id) => GetGraph(type).Nodes.TryGetValue(id, out var entry) ? entry : throw new ArgumentOutOfRangeException(nameof(id));
    private bool PortsExist(Graph graph, VisualShaderConnection c) =>
        graph.Nodes.TryGetValue(c.FromNode, out var from) && graph.Nodes.TryGetValue(c.ToNode, out var to) && !from.Node.IsDisposed && !to.Node.IsDisposed &&
        (uint)c.FromPort < (uint)OutputTypes(from.Node, graph.Stage).Length && (uint)c.ToPort < (uint)to.Node.Inputs.Length;
    private bool PortsCompatible(Graph graph, VisualShaderConnection c) =>
        CompatiblePorts(OutputTypes(graph.Nodes[c.FromNode].Node, graph.Stage)[c.FromPort], graph.Nodes[c.ToNode].Node.Inputs[c.ToPort]);
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
