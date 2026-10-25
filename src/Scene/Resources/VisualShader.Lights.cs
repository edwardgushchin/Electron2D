using System.Globalization;
using System.Text;

namespace Electron2D;

internal sealed record CanvasStageSource(string Function, string State);

public sealed partial class VisualShader
{
    private static readonly (string Name, string Type)[] CanvasLightFields =
    [
        ("LIGHT", "vec4"), ("LIGHT_COLOR", "vec4"), ("LIGHT_DIRECTION", "vec3"),
        ("LIGHT_ENERGY", "float"), ("LIGHT_IS_DIRECTIONAL", "bool"), ("LIGHT_POSITION", "vec3"),
        ("LIGHT_VERTEX", "vec3"), ("NORMAL", "vec3"), ("SHADOW_MODULATE", "vec4"), ("SPECULAR_SHININESS", "vec4")
    ];

    private (string Source, CanvasStageSource? Light, CanvasStageSource? Surface) GenerateCanvasModule(Dictionary<string, VisualShaderNodeParameter> parameters,
        Dictionary<string, int> slots, List<VisualShaderImage> allImages, List<VisualShaderBlockName> blocks, CustomPlan? custom,
        Dictionary<string, VaryingWire> varyings, StagePlan? light, SurfacePlan? surface)
    {
        var domain = new DomainCompilation { Canvas = true, Surface = surface };
        domain.Plans.Add(Type.Fragment, PlanStage(Type.Fragment)); if (light is not null) domain.Plans.Add(Type.Light, light);
        var expressions = domain.Plans.SelectMany(p => ExpressionSources(p.Key, p.Value.Order.ToHashSet(), custom)).ToArray();
        var names = expressions.SelectMany(s => SourceIdentifiers(s).Select(t => s.Substring(t.Start, t.Length))).Concat(parameters.Keys).ToHashSet(StringComparer.Ordinal);
        var images = new List<VisualShaderImage>();
        foreach (var (stage, plan) in domain.Plans)
        {
            var imagePlan = ImagePlan(stage, plan.Order.ToHashSet(), parameters, custom); domain.ImagePlans.Add(stage, imagePlan);
            if (surface is not null && stage == Type.Fragment && imagePlan.Images.All(i => i.Name != "NORMAL_TEXTURE"))
                imagePlan.Images.Add(new(Type.Fragment, "NORMAL_TEXTURE", false));
            foreach (var image in imagePlan.Images)
            {
                var canonical = image with { Stage = Type.Fragment }; var previous = images.FirstOrDefault(i => i.Name == image.Name);
                if (previous is null) images.Add(canonical);
                else if (previous != canonical) throw new InvalidDataException("Conflicting fragment/light sampled-image declarations.");
            }
        }
        if (images.Count > 16) throw new NotSupportedException("A shader stage supports at most sixteen sampled-image descriptors.");
        domain.Symbols = StageSymbols(images, names); names.UnionWith(domain.Symbols.Values);
        names.UnionWith(varyings.Values.SelectMany(w => w.Names));
        foreach (var role in new[] { "e2d_light_state", "Electron2D_CanvasLight", "e2d_fragment_color", "e2d_fragment_uv", "e2d_fragment_custom", "e2d_stage_1", "e2d_stage_2" }.Concat(surface is null ? [] : new[] { "e2d_surface", "Electron2D_CanvasSurface", "e2d_surface_finalize" }))
        {
            var symbol = role;
            for (var suffix = 2; !names.Add(symbol); suffix++) symbol = role + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            domain.Symbols.Add(role, symbol);
        }
        foreach (var (name, _) in CanvasLightFields) domain.Symbols.Add(name, domain.Symbols["e2d_light_state"] + "." + name);
        if (surface is not null)
        {
            foreach (var (name, _) in SurfaceFields) domain.Symbols[name] = domain.Symbols["e2d_surface"] + "." + name;
            domain.Symbols["e2d_normal"] = domain.Symbols["NORMAL"];
        }
        var aliases = domain.Symbols;
        foreach (var role in new[] { "RendererValues", "Globals", "Instance" })
            if (aliases[role] != role) blocks.Add(new(Type.Fragment, aliases[role], role));
        allImages.AddRange(images);
        var header = ParameterHeader(Type.Fragment, domain.Plans[Type.Fragment].Order.ToHashSet(), parameters, slots, images, aliases, custom, domain);
        header = VaryingHeader(Type.Fragment, varyings, header);
        var source = new StringBuilder(header[..header.LastIndexOf("void main() {", StringComparison.Ordinal)]);
        source.Append(GenerateStage(Type.Fragment, parameters, slots, [], [], custom, domain, varyings));
        if (surface is not null) source.Append(SurfaceFinalize(aliases, surface));
        if (light is not null)
        {
            var lightSource = GenerateStage(Type.Light, parameters, slots, [], [], custom, domain, varyings);
            var entry = "void " + aliases["e2d_stage_2"] + "() {";
            var initialize = RewriteExpression("COLOR = e2d_fragment_color; UV = e2d_fragment_uv; INSTANCE_CUSTOM = e2d_fragment_custom; LIGHT = vec4(0.0); SHADOW_MODULATE = vec4(1.0);", aliases);
            if (surface is not null) initialize += aliases["NORMAL"] + " = " + aliases["e2d_light_state"] + ".NORMAL;\n" + aliases["LIGHT_VERTEX"] + " = " + aliases["e2d_light_state"] + ".LIGHT_VERTEX;\n";
            source.Append(lightSource.Replace(entry, entry + "\n" + initialize, StringComparison.Ordinal));
        }
        var body = "void main() { UV = inUV; COLOR = inColor * texture(TEXTURE, UV); INSTANCE_CUSTOM = inCustom; e2d_stage_1(); result = COLOR; e2d_fragment_color = COLOR; e2d_fragment_uv = UV; e2d_fragment_custom = INSTANCE_CUSTOM; " +
            (surface is null ? "" : "e2d_surface_finalize(); ") + (light is null ? "" : "e2d_stage_2(); ") + "}";
        source.AppendLine(RewriteExpression(body, aliases));
        CheckExpressionBudget(source);
        return (source.ToString(), light is null ? null : new(aliases["e2d_stage_2"], aliases["e2d_light_state"]), surface is null ? null : new(aliases["e2d_surface_finalize"], aliases["e2d_surface"]));
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
