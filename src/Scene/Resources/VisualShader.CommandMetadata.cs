using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    private bool NeedsCommandInput(Type stage, HashSet<int> reachable, CustomPlan? custom, string name) =>
        stage == Type.Fragment && GetMode() == Mode.CanvasItem &&
        (reachable.Any(id => GetNode(stage, id) is VisualShaderNodeInput input && input.GetInputRealName() == name) ||
        ExpressionSources(stage, reachable, custom).Any(s => SourceIdentifiers(s).Any(t => !t.Member && s.Substring(t.Start, t.Length) == name)));

    private static string SpecularStorage(Dictionary<string, string> aliases) => aliases.GetValueOrDefault("SPECULAR_SHININESS", aliases["e2d_specular"]);
    private static string SpecularDefinition(Dictionary<string, string> aliases)
    {
        var storage = SpecularStorage(aliases);
        return (aliases.ContainsKey("SPECULAR_SHININESS") ? "" : "vec4 " + storage + ";\n") +
            "vec4 " + aliases["e2d_specular_value"] + "() { return " + storage + "; }\n";
    }
    private static void EmitSpecularStart(StringBuilder source, Dictionary<string, string> aliases) => source.Append(SpecularStorage(aliases))
        .Append(" = texture(SPECULAR_SHININESS_TEXTURE, ").Append(aliases["UV"]).Append(") * ")
        .Append(aliases["e2d_values"]).AppendLine(".CANVAS_SPECULAR;");
}
