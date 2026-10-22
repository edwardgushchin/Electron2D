using System.Globalization;
using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    internal (string Vertex, string Fragment, IReadOnlyList<VisualShaderImage> Images, IReadOnlyList<VisualShaderBlockName> Blocks) GenerateSources()
    {
        ThrowIfDisposed(); ValidateFileState();
        if (GetMode() != Mode.CanvasItem) throw new NotSupportedException("This visual graph importer requires the canvas shader domain.");
        var parameters = ParameterSymbols(); var slots = InstanceSlots(parameters);
        var images = new List<VisualShaderImage>(); var blocks = new List<VisualShaderBlockName>();
        return (GenerateStage(Type.Vertex, parameters, slots, images, blocks), GenerateStage(Type.Fragment, parameters, slots, images, blocks), images, blocks);
    }

    private string GenerateStage(Type stage, Dictionary<string, VisualShaderNodeParameter> parameters, Dictionary<string, int> slots, List<VisualShaderImage> allImages, List<VisualShaderBlockName> allBlocks)
    {
        var graph = GetGraph(stage);
        var incoming = new Dictionary<(int Node, int Port), VisualShaderConnection>();
        foreach (var edge in graph.Connections)
        {
            if (!PortsExist(graph, edge)) throw new InvalidDataException("A graph connection no longer has live matching ports.");
            if (!PortsCompatible(graph, edge)) throw new InvalidDataException("Graph ports require compatible numeric or sampled-image roles.");
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
        if (stage != Type.Fragment && reachable.Any(id => IsSDFNode(graph.Nodes[id].Node)))
            throw new NotSupportedException("Canvas SDF nodes require the fragment graph stage.");
        var remaining = reachable.ToDictionary(id => id, _ => 0);
        var dependents = reachable.ToDictionary(id => id, _ => new List<int>());
        foreach (var id in reachable)
            for (var port = 0; port < graph.Nodes[id].Node.Inputs.Length; port++)
                if (incoming.TryGetValue((id, port), out var edge)) { remaining[id]++; dependents[edge.FromNode].Add(id); }
        var ready = new PriorityQueue<int, int>();
        foreach (var pair in remaining) if (pair.Value == 0) ready.Enqueue(pair.Key, pair.Key);
        var plan = ImagePlan(stage, reachable, parameters); allImages.AddRange(plan.Images);
        var aliases = StageSymbols(plan.Images);
        foreach (var role in new[] { "Frame", "RendererValues", "Globals", "Instance" })
            if (aliases[role] != role) allBlocks.Add(new(stage, aliases[role], role));
        var source = new StringBuilder(ParameterHeader(stage, reachable, parameters, slots, plan.Images, aliases));
        var names = parameters.Keys.Concat(plan.Images.Select(i => i.Name)).ToHashSet(StringComparer.Ordinal);
        var values = new Dictionary<(int Node, int Port), string>(); var completed = 0;
        string Input(int id, int port)
        {
            var node = graph.Nodes[id].Node;
            if (!incoming.TryGetValue((id, port), out var edge))
            {
                if (node.Inputs[port] == VisualShaderNode.PortType.Sampler) return "";
                if (node is VisualShaderNodeScreenUVToSDF) return StageExpression("(gl_FragCoord.xy * e2d_values.SCREEN_PIXEL_SIZE)", aliases);
                if (port == 0 && node is VisualShaderNodeUVFunc or VisualShaderNodeUVPolarCoord) return StageExpression("UV", aliases);
                if (node is VisualShaderNodeTexture or VisualShaderNodeLayeredSample)
                {
                    if (port == 0) return StageExpression(node is VisualShaderNodeLayeredSample ? "vec3(UV, 0.0)" :
                        node is VisualShaderNodeTexture { Source: VisualShaderNodeTexture.SourceEnum.Screen } && stage == Type.Fragment ? "(gl_FragCoord.xy * e2d_values.SCREEN_PIXEL_SIZE)" : "UV", aliases);
                    if (port == 1) return "";
                }
                return node.InputDefaultExpression(port);
            }
            var from = graph.Nodes[edge.FromNode].Node;
            return VisualShaderValues.ConvertExpression(values[(edge.FromNode, edge.FromPort)], OutputTypes(from)[edge.FromPort], node.Inputs[port]);
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
                    source.Append("    ").Append(StageExpression(destination, aliases)).Append(" = ").Append(Input(0, port)).AppendLine(";");
                }
            }
            else
            {
                var inputs = Enumerable.Range(0, node.Inputs.Length).Select(port => Input(id, port)).ToArray();
                string? raymarch = null;
                if (node is VisualShaderNodeSDFRaymarch)
                {
                    raymarch = "n" + id.ToString(CultureInfo.InvariantCulture) + "ray"; var stem = raymarch;
                    for (var suffix = 2; !names.Add(raymarch); suffix++) raymarch = stem + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                    source.Append("    vec4 ").Append(raymarch).Append(" = ").Append(aliases["e2d_sdf_raymarch"]).Append('(').Append(inputs[0]).Append(", ").Append(inputs[1]).AppendLine(");");
                }
                for (var output = 0; output < OutputTypes(node).Length; output++)
                {
                    var stem = "n" + id.ToString(CultureInfo.InvariantCulture) + "p" + output.ToString(CultureInfo.InvariantCulture); var name = stem;
                    for (var suffix = 2; !names.Add(name); suffix++) name = stem + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                    var sample = node is VisualShaderNodeTexture or VisualShaderNodeLayeredSample;
                    var sampler = plan.Nodes.GetValueOrDefault(id, node is VisualShaderNodeTexture { Source: VisualShaderNodeTexture.SourceEnum.Port } or VisualShaderNodeLayeredSample { Source: VisualShaderNodeLayeredSample.SourceEnum.Port } ? inputs[2] : "");
                    if (sample && sampler.Length != 0)
                    {
                        var image = plan.Images.First(i => i.Name == sampler);
                        if (image.IsArray != (node is VisualShaderNodeLayeredSample)) throw new InvalidDataException("A connected graph sampler has the wrong image shape.");
                    }
                    var expression = node switch
                    {
                        VisualShaderNodeInput input => StageExpression(input.Expression(stage), aliases).Replace("TIME", aliases["e2d_values"] + ".TIME", StringComparison.Ordinal).Replace("SCREEN_PIXEL_SIZE", aliases["e2d_values"] + ".SCREEN_PIXEL_SIZE", StringComparison.Ordinal),
                        VisualShaderNodeDerivativeFunc when stage != Type.Fragment => throw new NotSupportedException("Screen-space derivative nodes require the fragment graph stage."),
                        VisualShaderNodeSDFRaymarch => raymarch + (output == 0 ? ".x" : output == 1 ? ".y != 0.0" : ".zw"),
                        VisualShaderNodeTextureSDF or VisualShaderNodeTextureSDFNormal or VisualShaderNodeSDFToScreenUV or VisualShaderNodeScreenUVToSDF => SDFExpression(node.Expression(output, inputs), aliases),
                        VisualShaderNodeTexture or VisualShaderNodeLayeredSample => SampleImage(sampler, inputs),
                        VisualShaderNodeColorFunc color => color.Expression(output, inputs).Replace("e2d_rgb_to_hsv(", aliases["e2d_rgb_to_hsv"] + "(", StringComparison.Ordinal),
                        VisualShaderNodeTextureParameter => plan.Nodes[id],
                        VisualShaderNodeParameterRef when OutputTypes(node)[output] == VisualShaderNode.PortType.Sampler => plan.Nodes[id],
                        VisualShaderNodeParameter parameter => ParameterVariable(parameter, aliases),
                        VisualShaderNodeParameterRef reference => ParameterExpression(reference, output, parameters, aliases),
                        _ => node.Expression(output, inputs)
                    };
                    if (OutputTypes(node)[output] == VisualShaderNode.PortType.Sampler)
                    { values.Add((id, output), expression); continue; }
                    source.Append("    ").Append(VisualShaderValues.Name(OutputTypes(node)[output])).Append(' ').Append(name).Append(" = ").Append(expression).AppendLine(";");
                    values.Add((id, output), name);
                }
            }
            foreach (var next in dependents[id]) if (--remaining[next] == 0) ready.Enqueue(next, next);
        }
        if (completed != reachable.Count) throw new InvalidDataException("The reachable visual shader graph contains a cycle.");
        source.Append(StageExpression(stage == Type.Vertex ? VertexFooter : FragmentFooter, aliases));
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
