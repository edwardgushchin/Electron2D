using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    private static bool UsesSDFImage(VisualShaderNode node) => node is VisualShaderNodeTextureSDF or VisualShaderNodeTextureSDFNormal or VisualShaderNodeSDFRaymarch;
    private static bool IsSDFNode(VisualShaderNode node) => UsesSDFImage(node) || node is VisualShaderNodeSDFToScreenUV or VisualShaderNodeScreenUVToSDF;
    private static string SDFExpression(string expression, Dictionary<string, string> aliases) =>
        StageExpression(expression, aliases).Replace("e2d_sdf_normal(", aliases["e2d_sdf_normal"] + "(", StringComparison.Ordinal)
            .Replace("e2d_sdf(", aliases["e2d_sdf"] + "(", StringComparison.Ordinal);

    private static string SDFDefinitions(IEnumerable<VisualShaderNode> nodes, Dictionary<string, string> aliases)
    {
        var result = new StringBuilder();
        var values = aliases["e2d_values"]; var sample = aliases["e2d_sdf"];
        result.Append("float ").Append(sample).Append("(vec2 p) { return textureLod(SDF_TEXTURE, p * ").Append(values).Append(".SDF_COORDS.xy + ")
            .Append(values).AppendLine(".SDF_COORDS.zw, 0.0).r; }");
        if (nodes.Any(n => n is VisualShaderNodeTextureSDFNormal))
            result.Append("vec2 ").Append(aliases["e2d_sdf_normal"]).Append("(vec2 p) { vec2 uv = p * ").Append(values).Append(".SDF_COORDS.xy + ").Append(values).AppendLine(".SDF_COORDS.zw;")
                .AppendLine("return normalize(vec2(textureLod(SDF_TEXTURE, uv + vec2(0.001,0), 0.0).r - textureLod(SDF_TEXTURE, uv - vec2(0.001,0), 0.0).r,")
                .AppendLine("textureLod(SDF_TEXTURE, uv + vec2(0,0.001), 0.0).r - textureLod(SDF_TEXTURE, uv - vec2(0,0.001), 0.0).r)); }");
        if (nodes.Any(n => n is VisualShaderNodeSDFRaymarch))
            result.Append("vec4 ").Append(aliases["e2d_sdf_raymarch"]).AppendLine("(vec2 start, vec2 end) {")
                .AppendLine("float limit = distance(start, end); if (limit == 0.0) return vec4(0,0,start);")
                .AppendLine("vec2 direction = (end - start) / limit; vec2 at = start; float accumulated = 0.0;")
                .AppendLine("while (accumulated < limit) {")
                .Append("float d = ").Append(sample).AppendLine("(at); float next = accumulated + d;")
                .AppendLine("if (d < 0.01) { accumulated = next; break; }")
                // Large derived float values can stop making progress even when the field is positive.
                .AppendLine("if (!(next > accumulated)) break; accumulated = next; at += d * direction;")
                .AppendLine("}")
                .AppendLine("float travelled = min(limit, accumulated); return vec4(travelled, accumulated < limit ? 1.0 : 0.0, start + direction * travelled); }");
        return result.ToString();
    }
}
