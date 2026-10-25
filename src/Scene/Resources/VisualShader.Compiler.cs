using System.Globalization;
using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    internal (string Vertex, string Fragment, IReadOnlyList<VisualShaderImage> Images, IReadOnlyList<VisualShaderBlockName> Blocks, CanvasStageSource? Light, CanvasStageSource? Surface) GenerateSources()
    {
        ThrowIfDisposed(); ValidateFileState();
        if (GetMode() != Mode.CanvasItem) throw new NotSupportedException("This visual graph importer requires the canvas shader domain.");
        var parameters = ParameterSymbols(); var slots = InstanceSlots(parameters); var custom = PrepareCustomCode(); var varyings = PrepareVaryings(parameters, custom);
        var images = new List<VisualShaderImage>(); var blocks = new List<VisualShaderBlockName>();
        var vertex = GenerateStage(Type.Vertex, parameters, slots, images, blocks, custom, varyings: varyings);
        var lightPlan = PlanStage(Type.Light);
        var lightActive = lightPlan.Order.Count > 1 || GetNode(Type.Light, 0).GetDefaultInputValues().Count != 0 || custom?.Functions[Type.Light].Any(s => s.Length != 0) == true;
        var surface = PrepareSurface(custom);
        if (lightActive || surface is not null)
        {
            var module = GenerateCanvasModule(parameters, slots, images, blocks, custom, varyings, lightActive ? lightPlan : null, surface);
            return (vertex, module.Source, images, blocks, module.Light, module.Surface);
        }
        return (vertex, GenerateStage(Type.Fragment, parameters, slots, images, blocks, custom, varyings: varyings), images, blocks, null, null);
    }

    private string GenerateStage(Type stage, Dictionary<string, VisualShaderNodeParameter> parameters, Dictionary<string, int> slots, List<VisualShaderImage> allImages, List<VisualShaderBlockName> allBlocks, CustomPlan? custom, DomainCompilation? domain = null, Dictionary<string, VaryingWire>? varyings = null)
    {
        var graph = GetGraph(stage);
        var stagePlan = domain?.Plans[stage] ?? PlanStage(stage);
        var incoming = stagePlan.Incoming;
        var root = GetNode(stage, 0);
        var lastSupported = domain is null ? stage == Type.Vertex ? 3 : 1 : root.Inputs.Length - 1;
        for (var port = lastSupported + 1; port < root.Inputs.Length; port++)
            if (incoming.ContainsKey((0, port)) || root.GetDefaultInputValues().Any(v => v.Port == port))
                throw new NotSupportedException($"Canvas {stage} output {port} requires its separate graph output integration.");

        var reachable = stagePlan.Order.ToHashSet();
        if (stage is not (Type.Fragment or Type.Light) && reachable.Any(id => IsSDFNode(graph.Nodes[id].Node)))
            throw new NotSupportedException("Canvas SDF nodes require the fragment graph stage.");
        var remaining = reachable.ToDictionary(id => id, _ => 0);
        var dependents = reachable.ToDictionary(id => id, _ => new List<int>());
        foreach (var id in reachable)
            for (var port = 0; port < graph.Nodes[id].Node.Inputs.Length; port++)
                if (incoming.TryGetValue((id, port), out var edge)) { remaining[id]++; dependents[edge.FromNode].Add(id); }
        var ranks = domain is not null || reachable.Any(id => graph.Nodes[id].Node is VisualShaderNodeVaryingSetter)
            ? stagePlan.Order.Select((id, rank) => (id, rank)).ToDictionary(p => p.id, p => p.rank) : null;
        int Priority(int id) => ranks?.GetValueOrDefault(id) ?? id;
        var ready = new PriorityQueue<int, int>();
        foreach (var pair in remaining) if (pair.Value == 0) ready.Enqueue(pair.Key, Priority(pair.Key));
        var plan = domain?.ImagePlans[stage] ?? ImagePlan(stage, reachable, parameters, custom); if (domain is null) allImages.AddRange(plan.Images);
        var expressionSources = ExpressionSources(stage, reachable, custom).ToArray();
        var expressionNames = expressionSources.SelectMany(s => SourceIdentifiers(s).Select(t => s.Substring(t.Start, t.Length))).ToHashSet(StringComparer.Ordinal);
        var aliases = domain?.Symbols ?? StageSymbols(plan.Images, expressionNames);
        foreach (var role in domain is null ? new[] { "Frame", "RendererValues", "Globals", "Instance" } : Array.Empty<string>())
            if (aliases[role] != role) allBlocks.Add(new(stage, aliases[role], role));
        var header = domain is null ? ParameterHeader(stage, reachable, parameters, slots, plan.Images, aliases, custom) : domain.StageHeader(stage);
        if (varyings is not null) header = VaryingHeader(stage, varyings, header);
        var source = new StringBuilder(header);
        if (stage == Type.Fragment && domain?.Surface is { } surface) EmitSurfaceStart(source, aliases, surface);
        EmitCustomFunctions(custom, stage, aliases, source);
        if (domain?.Surface is null && NeedsCanvasNormal(stage, reachable)) EmitCanvasNormal(source, aliases);
        var names = parameters.Keys.Concat(plan.Images.Select(i => i.Name)).Concat(expressionNames).ToHashSet(StringComparer.Ordinal);
        if (varyings is not null) names.UnionWith(varyings.Values.SelectMany(v => v.Names));
        var values = new Dictionary<(int Node, int Port), string>(); var completed = 0;
        string Input(int id, int port)
        {
            var node = graph.Nodes[id].Node;
            if (!incoming.TryGetValue((id, port), out var edge))
            {
                if (node.Inputs[port] == VisualShaderNode.PortType.Sampler) return "";
                if (node is VisualShaderNodeCurveTexture or VisualShaderNodeCurveXYZTexture && !node.GetDefaultInputValues().Any(v => v.Port == port)) return "";
                if (node is VisualShaderNodeFresnel fresnel && port == 0 && !fresnel.GetInputPortDefaultValue<Vector3>(0).HasValue)
                    return stage == Type.Light ? aliases["NORMAL"] : domain is { Canvas: false } ? "vec3(0.0)" : stage == Type.Fragment ? aliases["e2d_normal"] :
                        throw new NotSupportedException("An implicit canvas normal requires the fragment graph stage; connect or author a vertex normal explicitly.");
                if (domain is not null && DomainInputDefault(node, port) is { } implicitValue && !node.GetDefaultInputValues().Any(v => v.Port == port))
                    return RewriteExpression(implicitValue, aliases);
                if (node is VisualShaderNodeScreenUVToSDF) return StageExpression("(gl_FragCoord.xy * e2d_values.SCREEN_PIXEL_SIZE)", aliases);
                if (domain is not null && IsParticleStage(stage) && port == 0 && node is VisualShaderNodeUVFunc or VisualShaderNodeUVPolarCoord or VisualShaderNodeTexture or VisualShaderNodeLayeredSample)
                    throw new NotSupportedException("Particle image and UV operations require an explicit coordinate connection.");
                if (port == 0 && node is VisualShaderNodeUVFunc or VisualShaderNodeUVPolarCoord) return StageExpression("UV", aliases);
                if (node is VisualShaderNodeTexture or VisualShaderNodeLayeredSample)
                {
                    if (port == 0) return StageExpression(node is VisualShaderNodeLayeredSample ? "vec3(UV, 0.0)" :
                        node is VisualShaderNodeTexture { Source: VisualShaderNodeTexture.SourceEnum.Screen } && stage is Type.Fragment or Type.Light ? "(gl_FragCoord.xy * e2d_values.SCREEN_PIXEL_SIZE)" : "UV", aliases);
                    if (port == 1) return "";
                }
                return node.InputDefaultExpression(port);
            }
            var from = graph.Nodes[edge.FromNode].Node;
            return VisualShaderValues.ConvertExpression(values[(edge.FromNode, edge.FromPort)], OutputTypes(from, stage)[edge.FromPort], node.Inputs[port]);
        }
        while (ready.TryDequeue(out var id, out _))
        {
            completed++; var node = graph.Nodes[id].Node;
            if (id == 0)
            {
                if (domain is not null)
                {
                    EmitDomainOutput(stage, root, incoming, Input, source, aliases, domain);
                    foreach (var next in dependents[id]) if (--remaining[next] == 0) ready.Enqueue(next, Priority(next));
                    continue;
                }
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
                if (node is VisualShaderNodeVaryingSetter setter)
                {
                    VaryingWrite(setter, stage, inputs[0], varyings, source);
                    foreach (var next in dependents[id]) if (--remaining[next] == 0) ready.Enqueue(next, Priority(next));
                    continue;
                }
                if (domain is not null && EmitDomainNode(domain, stage, node, id, inputs, incoming, names, values, source))
                {
                    foreach (var next in dependents[id]) if (--remaining[next] == 0) ready.Enqueue(next, Priority(next));
                    continue;
                }
                if (node is VisualShaderNodeCustom provider)
                {
                    var connected = Enumerable.Range(0, node.Inputs.Length).Where(p => incoming.ContainsKey((id, p))).ToHashSet();
                    EmitCustom(provider, custom!.Blocks[(stage, id)], id, inputs, connected, stage, aliases, names, values, source);
                    foreach (var next in dependents[id]) if (--remaining[next] == 0) ready.Enqueue(next, Priority(next));
                    continue;
                }
                if (node is VisualShaderNodeExpression block)
                {
                    EmitExpression(block, id, inputs, stage, aliases, names, values, source);
                    foreach (var next in dependents[id]) if (--remaining[next] == 0) ready.Enqueue(next, Priority(next));
                    continue;
                }
                string? raymarch = null;
                if (node is VisualShaderNodeSDFRaymarch)
                {
                    raymarch = "n" + id.ToString(CultureInfo.InvariantCulture) + "ray"; var stem = raymarch;
                    for (var suffix = 2; !names.Add(raymarch); suffix++) raymarch = stem + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                    source.Append("    vec4 ").Append(raymarch).Append(" = ").Append(aliases["e2d_sdf_raymarch"]).Append('(').Append(inputs[0]).Append(", ").Append(inputs[1]).AppendLine(");");
                }
                for (var output = 0; output < OutputTypes(node, stage).Length; output++)
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
                        VisualShaderNodeVaryingGetter getter => VaryingRead(getter, stage, varyings),
                        VisualShaderNodeInput input when domain is not null => RewriteExpression(input.GenerateExpression(stage), ExpressionBuiltins(stage, aliases)),
                        VisualShaderNodeInput input => StageExpression(input.GenerateExpression(stage), aliases).Replace("TIME", aliases["e2d_values"] + ".TIME", StringComparison.Ordinal).Replace("SCREEN_PIXEL_SIZE", aliases["e2d_values"] + ".SCREEN_PIXEL_SIZE", StringComparison.Ordinal).Replace("TEXTURE_PIXEL_SIZE", aliases["e2d_values"] + ".TEXTURE_PIXEL_SIZE", StringComparison.Ordinal)
                            .Replace("MODEL_MATRIX", aliases["e2d_values"] + ".MODEL_MATRIX", StringComparison.Ordinal).Replace("CANVAS_MATRIX", aliases["e2d_values"] + ".CANVAS_MATRIX", StringComparison.Ordinal).Replace("SCREEN_MATRIX", aliases["e2d_values"] + ".SCREEN_MATRIX", StringComparison.Ordinal),
                        VisualShaderNodeDerivativeFunc when stage is not (Type.Fragment or Type.Light or Type.TextureBlit) => throw new NotSupportedException("Screen-space derivative nodes require the fragment graph stage."),
                        VisualShaderNodeSDFRaymarch => raymarch + (output == 0 ? ".x" : output == 1 ? ".y != 0.0" : ".zw"),
                        VisualShaderNodeTextureSDF or VisualShaderNodeTextureSDFNormal or VisualShaderNodeSDFToScreenUV or VisualShaderNodeScreenUVToSDF => SDFExpression(node.GenerateExpression(output, inputs), aliases),
                        VisualShaderNodeTexture or VisualShaderNodeLayeredSample => SampleImage(sampler, inputs),
                        VisualShaderNodeCurveTexture => inputs[0].Length == 0 ? "0.0" : "texture(" + sampler + ", vec2(" + inputs[0] + ")).r",
                        VisualShaderNodeCurveXYZTexture => inputs[0].Length == 0 ? "vec3(0.0)" : "texture(" + sampler + ", vec2(" + inputs[0] + ")).rgb",
                        VisualShaderNodeColorFunc color => color.GenerateExpression(output, inputs).Replace("e2d_rgb_to_hsv(", aliases["e2d_rgb_to_hsv"] + "(", StringComparison.Ordinal),
                        VisualShaderNodeRotationByAxis => node.GenerateExpression(output, inputs).Replace("e2d_axis_rotation(", aliases["e2d_axis_rotation"] + "(", StringComparison.Ordinal),
                        VisualShaderNodeRandomRange => node.GenerateExpression(output, inputs).Replace("e2d_random_range(", aliases["e2d_random_range"] + "(", StringComparison.Ordinal),
                        VisualShaderNodeTextureParameter => plan.Nodes[id],
                        VisualShaderNodeParameterRef when OutputTypes(node, stage)[output] == VisualShaderNode.PortType.Sampler => plan.Nodes[id],
                        VisualShaderNodeParameter parameter => ParameterVariable(parameter, aliases),
                        VisualShaderNodeParameterRef reference => ParameterExpression(reference, output, parameters, aliases),
                        _ => node.GenerateExpression(output, inputs)
                    };
                    if (domain is not null) expression = RewriteExpression(expression, aliases);
                    if (OutputTypes(node, stage)[output] == VisualShaderNode.PortType.Sampler)
                    { values.Add((id, output), expression); continue; }
                    source.Append("    ").Append(VisualShaderValues.Name(OutputTypes(node, stage)[output])).Append(' ').Append(name).Append(" = ").Append(expression).AppendLine(";");
                    values.Add((id, output), name);
                }
            }
            foreach (var next in dependents[id]) if (--remaining[next] == 0) ready.Enqueue(next, Priority(next));
        }
        if (completed != reachable.Count) throw new InvalidDataException("The reachable visual shader graph contains a cycle.");
        if (domain is null && (custom is not null || reachable.Any(id => graph.Nodes[id].Node is VisualShaderNodeExpression))) WrapExpressionStage(source, header, stage, aliases);
        if (domain is null) source.Append(StageExpression(stage == Type.Vertex ? VertexFooter : FragmentFooter, aliases));
        else source.AppendLine("}");
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
        layout(set=1,binding=0,std140) uniform Frame { vec2 size; } e2d_frame;
        layout(set=1,binding=1,std140) uniform RendererValues { float TIME; };
        void main() {
            vec2 VERTEX = inPosition;
            vec2 UV = inUV;
            vec4 COLOR = inColor;
            vec4 INSTANCE_CUSTOM = inCustom;

        """;
    private const string VertexFooter = """
            gl_Position = vec4(VERTEX.x / e2d_frame.size.x * 2.0 - 1.0, 1.0 - VERTEX.y / e2d_frame.size.y * 2.0, 0.0, 1.0);
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
