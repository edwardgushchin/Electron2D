namespace Electron2D;

public sealed partial class VisualShader
{
    private static bool CompatiblePorts(VisualShaderNode.PortType from, VisualShaderNode.PortType to) =>
        (from == VisualShaderNode.PortType.Matrix) == (to == VisualShaderNode.PortType.Matrix) &&
        (from == VisualShaderNode.PortType.Sampler) == (to == VisualShaderNode.PortType.Sampler);

    private bool CanConnectReroutes(Graph graph, VisualShaderConnection edge, out Dictionary<VisualShaderNodeReroute, VisualShaderNode.PortType> changes)
    {
        changes = [];
        var from = graph.Nodes[edge.FromNode].Node; var to = graph.Nodes[edge.ToNode].Node;
        var type = OutputTypes(from, graph.Stage)[edge.FromPort];
        VisualShaderNodeReroute? root = to as VisualShaderNodeReroute;
        if (root is null && from is VisualShaderNodeReroute source &&
            !graph.Connections.Any(c => ReferenceEquals(graph.Nodes[c.ToNode].Node, source)))
        { root = source; type = to.Inputs[edge.ToPort]; }
        if (root is null) return PortsCompatible(graph, edge);
        var outgoing = graph.Connections.ToLookup(c => graph.Nodes[c.FromNode].Node);
        var pending = new Stack<VisualShaderNodeReroute>(); pending.Push(root);
        while (pending.TryPop(out var current))
        {
            if (!changes.TryAdd(current, type)) continue;
            if (current.IsDisposed) return false;
            foreach (var connection in outgoing[current])
            {
                var destination = graph.Nodes[connection.ToNode].Node;
                if (destination.IsDisposed) return false;
                if (destination is VisualShaderNodeReroute next) pending.Push(next);
                else if ((uint)connection.ToPort >= (uint)destination.Inputs.Length || !CompatiblePorts(type, destination.Inputs[connection.ToPort])) return false;
            }
        }
        foreach (var connection in graph.Connections)
        {
            if (graph.Nodes[connection.ToNode].Node is not VisualShaderNodeReroute target || !changes.TryGetValue(target, out var targetType)) continue;
            var previousSource = graph.Nodes[connection.FromNode].Node;
            if (previousSource.IsDisposed) return false;
            var outputs = OutputTypes(previousSource, graph.Stage);
            if (connection.ToPort != 0 || (uint)connection.FromPort >= (uint)outputs.Length) return false;
            var sourceType = previousSource is VisualShaderNodeReroute reroute && changes.TryGetValue(reroute, out var selected) ? selected : outputs[connection.FromPort];
            if (!CompatiblePorts(sourceType, targetType)) return false;
        }
        return true;
    }

    private void ConnectWithReroutes(Graph graph, VisualShaderConnection edge)
    {
        if (!CanConnectReroutes(graph, edge, out var changes)) throw new ArgumentException("Incompatible reroute destinations.");
        if (graph.Connections.Count >= MaximumNodes * 8) throw new InvalidOperationException("Visual shader connection capacity exceeded.");
        var changed = changes.Where(p => p.Key.SetPortType(p.Value)).Select(p => p.Key).ToArray();
        graph.Connections.Add(edge);
        if (changed.Length == 0) { EmitChanged(); return; }
        List<Exception>? errors = null;
        foreach (var node in changed)
            try { node.EmitChanged(); } catch (Exception error) { (errors ??= []).Add(error); }
        try { EmitChanged(); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Connection observers failed after the complete graph edit.", errors);
    }
}
