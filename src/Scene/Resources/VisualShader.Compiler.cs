using System.Globalization;
using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    internal (string Vertex, string Fragment) GenerateSources()
    {
        ThrowIfDisposed(); ValidateFileState();
        if (GetMode() != Mode.CanvasItem) throw new NotSupportedException("This visual graph importer requires the canvas shader domain.");
        return (GenerateStage(Type.Vertex), GenerateStage(Type.Fragment));
    }

    private string GenerateStage(Type stage)
    {
        var graph = GetGraph(stage);
        var incoming = new Dictionary<(int Node, int Port), VisualShaderConnection>();
        foreach (var edge in graph.Connections)
        {
            if (!PortsExist(graph, edge)) throw new InvalidDataException("A graph connection no longer has live matching ports.");
            if (!PortsCompatible(graph, edge)) throw new InvalidDataException("Matrix graph ports require a matrix connection.");
            incoming[(edge.ToNode, edge.ToPort)] = edge;
        }
        var root = GetNode(stage, 0);
        var lastSupported = stage == Type.Vertex ? 3 : 1;
        for (var port = lastSupported + 1; port < root.Inputs.Length; port++)
            if (incoming.ContainsKey((0, port)) || root.GetDefaultInputValues().Any(v => v.Port == port))
                throw new NotSupportedException($"Canvas {stage} output {port} requires its separate graph output integration.");

        var reachable = new HashSet<int>(); var stack = new Stack<int>(); stack.Push(0);
        while (stack.TryPop(out var id))
        {
            if (!reachable.Add(id)) continue;
            var node = GetNode(stage, id);
            for (var port = 0; port < node.Inputs.Length; port++)
                if (incoming.TryGetValue((id, port), out var edge)) stack.Push(edge.FromNode);
        }
        var remaining = reachable.ToDictionary(id => id, _ => 0);
        var dependents = reachable.ToDictionary(id => id, _ => new List<int>());
        foreach (var id in reachable)
            for (var port = 0; port < graph.Nodes[id].Node.Inputs.Length; port++)
                if (incoming.TryGetValue((id, port), out var edge)) { remaining[id]++; dependents[edge.FromNode].Add(id); }
        var ready = new PriorityQueue<int, int>();
        foreach (var pair in remaining) if (pair.Value == 0) ready.Enqueue(pair.Key, pair.Key);
        var source = new StringBuilder(stage == Type.Vertex ? VertexHeader : FragmentHeader);
        var values = new Dictionary<(int Node, int Port), string>(); var completed = 0;
        string Input(int id, int port)
        {
            var node = graph.Nodes[id].Node;
            if (!incoming.TryGetValue((id, port), out var edge)) return node.InputDefaultExpression(port);
            var from = graph.Nodes[edge.FromNode].Node;
            return VisualShaderValues.ConvertExpression(values[(edge.FromNode, edge.FromPort)], from.Outputs[edge.FromPort], node.Inputs[port]);
        }
        while (ready.TryDequeue(out var id, out _))
        {
            completed++; var node = graph.Nodes[id].Node;
            if (id == 0)
            {
                var defaults = node.GetDefaultInputValues();
                for (var port = 0; port <= lastSupported; port++)
                {
                    if (!incoming.ContainsKey((0, port)) && !defaults.Any(d => d.Port == port)) continue;
                    var destination = stage == Type.Vertex ? port switch { 0 => "VERTEX", 1 => "UV", 2 => "COLOR.rgb", _ => "COLOR.a" }
                        : port == 0 ? "COLOR.rgb" : "COLOR.a";
                    source.Append("    ").Append(destination).Append(" = ").Append(Input(0, port)).AppendLine(";");
                }
            }
            else
            {
                var inputs = Enumerable.Range(0, node.Inputs.Length).Select(port => Input(id, port)).ToArray();
                for (var output = 0; output < node.Outputs.Length; output++)
                {
                    var name = "n" + id.ToString(CultureInfo.InvariantCulture) + "p" + output.ToString(CultureInfo.InvariantCulture);
                    var expression = node is VisualShaderNodeInput input ? input.Expression(stage) : node.Expression(output, inputs);
                    source.Append("    ").Append(VisualShaderValues.Name(node.Outputs[output])).Append(' ').Append(name).Append(" = ").Append(expression).AppendLine(";");
                    values.Add((id, output), name);
                }
            }
            foreach (var next in dependents[id]) if (--remaining[next] == 0) ready.Enqueue(next, next);
        }
        if (completed != reachable.Count) throw new InvalidDataException("The reachable visual shader graph contains a cycle.");
        source.Append(stage == Type.Vertex ? VertexFooter : FragmentFooter);
        var result = source.ToString();
        if (Encoding.UTF8.GetByteCount(result) > ShaderInclude.MaximumSourceBytes) throw new InvalidDataException("Generated shader source exceeds the import budget.");
        return result;
    }

    private const string VertexHeader = """
        #version 450
        layout(location=0) in vec2 inPosition;
        layout(location=1) in vec4 inColor;
        layout(location=2) in vec2 inUV;
        layout(location=3) in vec4 inCustom;
        layout(location=0) out vec4 outColor;
        layout(location=1) out vec2 outUV;
        layout(location=2) out vec4 outCustom;
        layout(set=1,binding=0,std140) uniform Frame { vec2 size; };
        layout(set=1,binding=1,std140) uniform RendererValues { float TIME; };
        void main() {
            vec2 VERTEX = inPosition;
            vec2 UV = inUV;
            vec4 COLOR = inColor;
            vec4 INSTANCE_CUSTOM = inCustom;

        """;
    private const string VertexFooter = """
            gl_Position = vec4(VERTEX.x / size.x * 2.0 - 1.0, 1.0 - VERTEX.y / size.y * 2.0, 0.0, 1.0);
            outColor = COLOR; outUV = UV; outCustom = INSTANCE_CUSTOM;
        }

        """;
    private const string FragmentHeader = """
        #version 450
        layout(location=0) in vec4 inColor;
        layout(location=1) in vec2 inUV;
        layout(location=2) in vec4 inCustom;
        layout(location=0) out vec4 result;
        layout(set=2,binding=0) uniform sampler2D TEXTURE;
        layout(set=3,binding=0,std140) uniform RendererValues { float TIME; vec2 SCREEN_PIXEL_SIZE; };
        void main() {
            vec2 UV = inUV;
            vec4 COLOR = inColor * texture(TEXTURE, UV);
            vec4 INSTANCE_CUSTOM = inCustom;

        """;
    private const string FragmentFooter = """
            result = COLOR;
        }

        """;
}
