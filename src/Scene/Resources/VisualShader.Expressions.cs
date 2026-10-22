using System.Globalization;
using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    private IEnumerable<string> GlobalExpressions() => ActiveStages.SelectMany(stage => _graphs[(int)stage].Nodes.Values).Select(e => e.Node)
        .OfType<VisualShaderNodeGlobalExpression>().Select(n => n.Expression);
    private IEnumerable<string> ExpressionSources(Type stage, HashSet<int> reachable, CustomPlan? custom = null) => GlobalExpressions().Concat(
        reachable.Select(id => GetNode(stage, id)).OfType<VisualShaderNodeExpression>().Where(n => n is not VisualShaderNodeGlobalExpression).Select(n => n.Expression)).Concat(custom?.Sources(stage) ?? []);
    private static IEnumerable<(int Start, int Length, bool Member)> SourceIdentifiers(string source)
    {
        var previous = '\0'; var structure = 0; var pendingStructure = false;
        for (var at = 0; at < source.Length;)
        {
            var c = source[at];
            if (char.IsWhiteSpace(c)) { at++; continue; }
            if (c == '/' && at + 1 < source.Length && source[at + 1] == '/')
            { at += 2; while (at < source.Length && source[at] != '\n') at++; continue; }
            if (c == '/' && at + 1 < source.Length && source[at + 1] == '*')
            { at += 2; while (at + 1 < source.Length && !(source[at] == '*' && source[at + 1] == '/')) at++; at = Math.Min(source.Length, at + 2); continue; }
            if (c is '\'' or '"')
            { var quote = c; at++; while (at < source.Length) { var next = source[at++]; if (next == '\\' && at < source.Length) at++; else if (next == quote) break; } previous = 's'; continue; }
            if (char.IsAsciiDigit(c) || c == '.' && at + 1 < source.Length && char.IsAsciiDigit(source[at + 1]))
            {
                var hexadecimal = c == '0' && at + 1 < source.Length && source[at + 1] is 'x' or 'X';
                at++;
                while (at < source.Length && (char.IsAsciiLetterOrDigit(source[at]) || source[at] == '.' ||
                    !hexadecimal && source[at] is '+' or '-' && source[at - 1] is 'e' or 'E')) at++;
                previous = 'n'; continue;
            }
            if (char.IsAsciiLetter(c) || c == '_')
            {
                var start = at++; while (at < source.Length && (char.IsAsciiLetterOrDigit(source[at]) || source[at] == '_')) at++;
                var keyword = source.AsSpan(start, at - start).SequenceEqual("struct");
                yield return (start, at - start, previous == '.' || structure > 0 || pendingStructure);
                if (keyword) pendingStructure = true;
                previous = 'i'; continue;
            }
            if (c == '{' && pendingStructure) { structure = 1; pendingStructure = false; }
            else if (structure > 0 && c == '{') structure++;
            else if (structure > 0 && c == '}') structure--;
            previous = c; at++;
        }
    }
    private static string RewriteExpression(string source, IReadOnlyDictionary<string, string> replacements)
    {
        var result = new StringBuilder(); var previous = 0;
        foreach (var token in SourceIdentifiers(source))
        {
            if (token.Member || !replacements.TryGetValue(source.Substring(token.Start, token.Length), out var value)) continue;
            result.Append(source, previous, token.Start - previous).Append(value); previous = token.Start + token.Length;
            CheckExpressionBudget(result);
        }
        result.Append(source, previous, source.Length - previous); CheckExpressionBudget(result); return result.ToString();
    }
    private static void CheckExpressionBudget(StringBuilder source)
    { if (source.Length > ShaderInclude.MaximumSourceBytes) throw new InvalidDataException("Generated expression source exceeds the import budget."); }
    private static bool ReadsRendererValues(IEnumerable<string> sources) => sources.Any(s => SourceIdentifiers(s).Any(t =>
        !t.Member && s.Substring(t.Start, t.Length) is "TIME" or "SCREEN_PIXEL_SIZE"));
    private static Dictionary<string, string> ExpressionBuiltins(Type stage, Dictionary<string, string> aliases, bool global = false)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal) { ["TIME"] = aliases["e2d_values"] + ".TIME" };
        if (IsParticleStage(stage) || stage == Type.TextureBlit)
        {
            foreach (var role in global ? Array.Empty<string>() : DomainBuiltins) if (aliases.TryGetValue(role, out var symbol)) result[role] = symbol;
            return result;
        }
        if (stage == Type.Fragment) result["SCREEN_PIXEL_SIZE"] = aliases["e2d_values"] + ".SCREEN_PIXEL_SIZE";
        if (!global)
        {
            foreach (var name in new[] { "UV", "COLOR", "INSTANCE_CUSTOM" }) result[name] = aliases[name];
            result["VERTEX"] = stage == Type.Vertex ? aliases["VERTEX"] : "gl_FragCoord.xy";
        }
        return result;
    }
    private string GlobalExpressionHeader(Type stage, Dictionary<string, string> aliases)
    {
        var result = new StringBuilder(); var builtins = ExpressionBuiltins(stage, aliases, true);
        foreach (var source in GlobalExpressions()) { result.Append(RewriteExpression(source, builtins)).AppendLine(); CheckExpressionBudget(result); }
        return result.ToString();
    }
    private static void WrapExpressionStage(StringBuilder source, string header, Type stage, Dictionary<string, string> aliases)
    {
        var entry = header.LastIndexOf("void main() {", StringComparison.Ordinal);
        var body = source.ToString(header.Length, source.Length - header.Length);
        var roles = stage == Type.Vertex ? new[] { "VERTEX", "UV", "COLOR", "INSTANCE_CUSTOM" } : new[] { "UV", "COLOR", "INSTANCE_CUSTOM" };
        var parameters = string.Join(", ", roles.Select(r => "inout " + (r is "VERTEX" or "UV" ? "vec2 " : "vec4 ") + aliases[r]));
        source.Clear().Append(header, 0, entry).Append("void ").Append(aliases["e2d_graph"]).Append('(').Append(parameters).AppendLine(") {")
            .Append(body).AppendLine("}").Append(header, entry, header.Length - entry)
            .Append("    ").Append(aliases["e2d_graph"]).Append('(').Append(string.Join(", ", roles.Select(r => aliases[r]))).AppendLine(");");
        CheckExpressionBudget(source);
    }
    private static void EmitExpression(VisualShaderNodeExpression expression, int id, IReadOnlyList<string> inputs,
        Type stage, Dictionary<string, string> aliases, HashSet<string> names, Dictionary<(int Node, int Port), string> values, StringBuilder source)
    {
        if (expression is VisualShaderNodeGlobalExpression) throw new InvalidDataException("Global definitions cannot supply local graph values.");
        var replacements = ExpressionBuiltins(stage, aliases);
        for (var port = 0; port < expression.InputPorts.Count; port++)
            replacements[expression.InputPorts[port].Name] = inputs[port].Length == 0 ? "" : expression.InputPorts[port].Type == VisualShaderNode.PortType.Sampler ? inputs[port] : "(" + inputs[port] + ")";
        for (var port = 0; port < expression.OutputPorts.Count; port++)
        {
            var type = expression.OutputPorts[port].Type;
            if (type == VisualShaderNode.PortType.Sampler) throw new NotSupportedException("GLSL expression outputs cannot declare local samplers.");
            var stem = "n" + id.ToString(CultureInfo.InvariantCulture) + "p" + port.ToString(CultureInfo.InvariantCulture); var name = stem;
            for (var suffix = 2; !names.Add(name); suffix++) name = stem + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            source.Append("    ").Append(VisualShaderValues.Name(type)).Append(' ').Append(name).Append(" = ").Append(VisualShaderValues.Literal(VisualShaderValues.Zero(type))).AppendLine(";");
            values.Add((id, port), name); replacements[expression.OutputPorts[port].Name] = name;
        }
        source.AppendLine("    {").Append(RewriteExpression(expression.Expression, replacements)).AppendLine().AppendLine("    }"); CheckExpressionBudget(source);
    }
}
