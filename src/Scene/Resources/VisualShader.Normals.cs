using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    private bool NeedsCanvasNormal(Type stage, IEnumerable<int> reachable)
    {
        if (stage != Type.Fragment || GetMode() != Mode.CanvasItem) return false;
        var graph = GetGraph(stage);
        var connected = graph.Connections.Where(edge => edge.ToPort == 0).Select(edge => edge.ToNode).ToHashSet();
        return reachable.Any(id => graph.Nodes[id].Node is VisualShaderNodeFresnel fresnel &&
            !fresnel.GetInputPortDefaultValue<Vector3>(0).HasValue &&
            !connected.Contains(id));
    }

    private static void EmitCanvasNormal(StringBuilder source, Dictionary<string, string> aliases)
    {
        var normal = aliases["e2d_normal"]; var flags = aliases["e2d_values"] + ".NORMAL_MAP_FLAGS";
        source.Append("    vec3 ").Append(normal).Append(" = vec3(texture(NORMAL_TEXTURE, ").Append(aliases["UV"])
            .AppendLine(").rg * vec2(2.0, -2.0) - vec2(1.0, -1.0), 0.0);")
            .Append("    ").Append(normal).Append(".xy = mix(").Append(normal).Append(".xy, ").Append(normal).Append(".yx, ").Append(flags).AppendLine(".z);")
            .Append("    ").Append(normal).Append(".xy *= vec2(1.0) - 2.0 * ").Append(flags).AppendLine(".xy;")
            .Append("    ").Append(normal).Append(".z = sqrt(max(0.0, 1.0 - dot(").Append(normal).Append(".xy, ").Append(normal).AppendLine(".xy)));");
    }
}
