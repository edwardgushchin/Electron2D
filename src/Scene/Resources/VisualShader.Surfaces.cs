using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    private sealed record SurfacePlan(bool Normal, bool Map);
    private static readonly (string Name, string Type)[] SurfaceFields =
    [
        ("NORMAL", "vec3"), ("NORMAL_MAP", "vec3"), ("NORMAL_MAP_DEPTH", "float"),
        ("NORMAL_USED", "bool"), ("LIGHT_VERTEX", "vec3"), ("SHADOW_VERTEX", "vec2")
    ];
    private SurfacePlan? PrepareSurface(CustomPlan? custom)
    {
        var plan = PlanStage(Type.Fragment); var root = GetNode(Type.Fragment, 0);
        bool Selected(int port) => plan.Incoming.ContainsKey((0, port)) || root.GetDefaultInputValues().Any(d => d.Port == port);
        var identifiers = ExpressionSources(Type.Fragment, plan.Order.ToHashSet(), custom)
            .SelectMany(s => SourceIdentifiers(s).Where(t => !t.Member).Select(t => s.Substring(t.Start, t.Length))).ToHashSet(StringComparer.Ordinal);
        if (!Enumerable.Range(2, 5).Any(Selected) && !SurfaceFields.Any(f => f.Name != "NORMAL_USED" && identifiers.Contains(f.Name))) return null;
        return new(Selected(2) || identifiers.Contains("NORMAL") || NeedsCanvasNormal(Type.Fragment, plan.Order), Selected(3) || identifiers.Contains("NORMAL_MAP"));
    }
    private static string SurfaceState(Dictionary<string, string> aliases)
    {
        var source = new StringBuilder("struct " + aliases["Electron2D_CanvasSurface"] + " {\n");
        foreach (var (name, type) in SurfaceFields) source.Append(type).Append(' ').Append(name).AppendLine(";");
        return source.Append("};\n").Append(aliases["Electron2D_CanvasSurface"]).Append(' ').Append(aliases["e2d_surface"]).AppendLine(";").ToString();
    }
    private static void EmitSurfaceStart(StringBuilder source, Dictionary<string, string> aliases, SurfacePlan plan)
    {
        var flags = aliases["e2d_values"] + ".NORMAL_MAP_FLAGS";
        var start = "NORMAL_USED = " + (plan.Normal || plan.Map ? "true" : flags + ".w != 0.0") + ";\n" +
            "NORMAL = vec3(0,0,1); NORMAL_MAP = vec3(0,0,1); NORMAL_MAP_DEPTH = 1.0;\n" +
            "LIGHT_VERTEX = vec3(gl_FragCoord.xy,0); SHADOW_VERTEX = gl_FragCoord.xy;\n" +
            "if (NORMAL_USED) { NORMAL.xy = texture(NORMAL_TEXTURE, UV).rg * vec2(2,-2) - vec2(1,-1);\n" +
            "NORMAL.xy = mix(NORMAL.xy,NORMAL.yx," + flags + ".z); NORMAL.xy *= vec2(1)-2.0*" + flags + ".xy;\n" +
            "NORMAL.z=sqrt(max(0.0,1.0-dot(NORMAL.xy,NORMAL.xy))); }\n";
        source.AppendLine(RewriteExpression(start, aliases));
    }
    private static string SurfaceFinalize(Dictionary<string, string> aliases, SurfacePlan plan)
    {
        var values = aliases["e2d_values"];
        var function = "void " + aliases["e2d_surface_finalize"] + "() {\n";
        if (plan.Map) function += "NORMAL = mix(vec3(0,0,1), NORMAL_MAP*vec3(2,-2,1)-vec3(1,-1,0), NORMAL_MAP_DEPTH);\n";
        function += "if (NORMAL_USED) { NORMAL.xy = mat2(" + values + ".NORMAL_MODEL_BASIS.xy," + values + ".NORMAL_MODEL_BASIS.zw)*NORMAL.xy;\n" +
            "NORMAL = normalize(vec3(mat2(" + values + ".NORMAL_CANVAS_BASIS.xy," + values + ".NORMAL_CANVAS_BASIS.zw)*NORMAL.xy,NORMAL.z)); }\n}\n";
        return RewriteExpression(function, aliases);
    }
}
