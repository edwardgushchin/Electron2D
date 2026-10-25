using System.Globalization;
using System.Text;

namespace Electron2D;

internal sealed record CanvasLightSource(string Function, string State);

public sealed partial class VisualShader
{
    private static readonly (string Name, string Type)[] CanvasLightFields =
    [
        ("LIGHT", "vec4"), ("LIGHT_COLOR", "vec4"), ("LIGHT_DIRECTION", "vec3"),
        ("LIGHT_ENERGY", "float"), ("LIGHT_IS_DIRECTIONAL", "bool"), ("LIGHT_POSITION", "vec3"),
        ("LIGHT_VERTEX", "vec3"), ("NORMAL", "vec3"), ("SHADOW_MODULATE", "vec4"), ("SPECULAR_SHININESS", "vec4")
    ];

    private (string Source, CanvasLightSource Metadata) GenerateCanvasLight(Dictionary<string, VisualShaderNodeParameter> parameters,
        Dictionary<string, int> slots, List<VisualShaderImage> allImages, List<VisualShaderBlockName> blocks, CustomPlan? custom,
        Dictionary<string, VaryingWire> varyings, StagePlan light)
    {
        var domain = new DomainCompilation { Canvas = true };
        domain.Plans.Add(Type.Fragment, PlanStage(Type.Fragment)); domain.Plans.Add(Type.Light, light);
        var expressions = domain.Plans.SelectMany(p => ExpressionSources(p.Key, p.Value.Order.ToHashSet(), custom)).ToArray();
        var names = expressions.SelectMany(s => SourceIdentifiers(s).Select(t => s.Substring(t.Start, t.Length))).Concat(parameters.Keys).ToHashSet(StringComparer.Ordinal);
        var images = new List<VisualShaderImage>();
        foreach (var (stage, plan) in domain.Plans)
        {
            var imagePlan = ImagePlan(stage, plan.Order.ToHashSet(), parameters, custom); domain.ImagePlans.Add(stage, imagePlan);
            foreach (var image in imagePlan.Images)
            {
                var canonical = image with { Stage = Type.Fragment }; var previous = images.FirstOrDefault(i => i.Name == image.Name);
                if (previous is null) images.Add(canonical);
                else if (previous != canonical) throw new InvalidDataException("Conflicting fragment/light sampled-image declarations.");
            }
        }
        domain.Symbols = StageSymbols(images, names); names.UnionWith(domain.Symbols.Values);
        names.UnionWith(varyings.Values.SelectMany(w => w.Names));
        foreach (var role in new[] { "e2d_light_state", "Electron2D_CanvasLight", "e2d_fragment_color", "e2d_fragment_uv", "e2d_fragment_custom", "e2d_stage_1", "e2d_stage_2" })
        {
            var symbol = role;
            for (var suffix = 2; !names.Add(symbol); suffix++) symbol = role + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            domain.Symbols.Add(role, symbol);
        }
        foreach (var (name, _) in CanvasLightFields) domain.Symbols.Add(name, domain.Symbols["e2d_light_state"] + "." + name);
        var aliases = domain.Symbols;
        foreach (var role in new[] { "RendererValues", "Globals", "Instance" })
            if (aliases[role] != role) blocks.Add(new(Type.Fragment, aliases[role], role));
        allImages.AddRange(images);
        var header = ParameterHeader(Type.Fragment, domain.Plans[Type.Fragment].Order.ToHashSet(), parameters, slots, images, aliases, custom, domain);
        header = VaryingHeader(Type.Fragment, varyings, header);
        var source = new StringBuilder(header[..header.LastIndexOf("void main() {", StringComparison.Ordinal)]);
        source.Append(GenerateStage(Type.Fragment, parameters, slots, [], [], custom, domain, varyings));
        var lightSource = GenerateStage(Type.Light, parameters, slots, [], [], custom, domain, varyings);
        var entry = "void " + aliases["e2d_stage_2"] + "() {";
        var initialize = RewriteExpression("COLOR = e2d_fragment_color; UV = e2d_fragment_uv; INSTANCE_CUSTOM = e2d_fragment_custom; LIGHT = vec4(0.0); SHADOW_MODULATE = vec4(1.0);", aliases);
        source.Append(lightSource.Replace(entry, entry + "\n" + initialize, StringComparison.Ordinal));
        source.AppendLine(RewriteExpression("void main() { UV = inUV; COLOR = inColor * texture(TEXTURE, UV); INSTANCE_CUSTOM = inCustom; e2d_stage_1(); result = COLOR; e2d_fragment_color = COLOR; e2d_fragment_uv = UV; e2d_fragment_custom = INSTANCE_CUSTOM; e2d_stage_2(); }", aliases));
        CheckExpressionBudget(source);
        return (source.ToString(), new(aliases["e2d_stage_2"], aliases["e2d_light_state"]));
    }

    private static string CanvasLightState(Dictionary<string, string> aliases)
    {
        var source = new StringBuilder("struct " + aliases["Electron2D_CanvasLight"] + " {\n");
        foreach (var (name, type) in CanvasLightFields) source.Append(type).Append(' ').Append(name).AppendLine(";");
        source.Append("};\n").Append(aliases["Electron2D_CanvasLight"]).Append(' ').Append(aliases["e2d_light_state"]).AppendLine(";");
        source.AppendLine(RewriteExpression("vec2 UV, e2d_fragment_uv; vec4 COLOR, INSTANCE_CUSTOM, e2d_fragment_color, e2d_fragment_custom;", aliases));
        return source.ToString();
    }
}
