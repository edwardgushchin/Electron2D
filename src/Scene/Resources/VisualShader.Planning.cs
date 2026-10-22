namespace Electron2D;

public sealed partial class VisualShader
{
    private sealed record StagePlan(Dictionary<(int Node, int Port), VisualShaderConnection> Incoming, List<int> Order);

    private StagePlan PlanStage(Type stage)
    {
        var graph = GetGraph(stage); var incoming = new Dictionary<(int Node, int Port), VisualShaderConnection>();
        foreach (var edge in graph.Connections)
        {
            if (!PortsExist(graph, edge) || !PortsCompatible(graph, edge)) throw new InvalidDataException("Graph connections require live compatible ports.");
            incoming[(edge.ToNode, edge.ToPort)] = edge;
        }
        var order = new List<int>(); var visited = new HashSet<int>(); var visiting = new HashSet<int>();
        var roots = new[] { 0 }.Concat(IsParticleStage(stage) ? graph.Nodes.Where(p => p.Value.Node is VisualShaderNodeParticleEmit).Select(p => p.Key) : []);
        foreach (var root in roots)
        {
            var stack = new Stack<(int ID, bool Exit)>(); stack.Push((root, false));
            while (stack.TryPop(out var step))
            {
                if (visited.Contains(step.ID)) continue;
                var node = GetNode(stage, step.ID);
                if (!IsParticleStage(stage) && node is VisualShaderNodeParticleEmitter or VisualShaderNodeParticleRandomness or VisualShaderNodeParticleMultiplyByAxisAngle or VisualShaderNodeParticleConeVelocity or VisualShaderNodeParticleAccelerator or VisualShaderNodeParticleEmit)
                    throw new NotSupportedException("Particle graph operations require a particle lifecycle stage.");
                if (step.Exit) { visiting.Remove(step.ID); visited.Add(step.ID); order.Add(step.ID); continue; }
                if (!visiting.Add(step.ID)) throw new InvalidDataException("The reachable visual shader graph contains a cycle.");
                stack.Push((step.ID, true));
                for (var port = node.Inputs.Length - 1; port >= 0; port--)
                    if (incoming.TryGetValue((step.ID, port), out var edge)) stack.Push((edge.FromNode, false));
            }
        }
        return new(incoming, order);
    }
}
