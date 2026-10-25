using System.Globalization;
using System.Text;
using P = Electron2D.VisualShaderNode.PortType;
using Q = Electron2D.VisualShaderNodeParameter.QualifierEnum;

namespace Electron2D;

public sealed partial class VisualShader
{
    internal IEnumerable<VisualShaderNodeParameter> ParameterDefinitions() => ActiveStages.SelectMany(stage => _graphs[(int)stage].Nodes.Values)
        .Select(e => e.Node).OfType<VisualShaderNodeParameter>().Distinct<VisualShaderNodeParameter>(ReferenceEqualityComparer.Instance);

    private P[] OutputTypes(VisualShaderNode node, Type stage)
    {
        if (node is VisualShaderNodeInput input) return input.OutputsFor(stage);
        if (node is not VisualShaderNodeParameterRef reference || reference.ParameterName == "[None]") return node.Outputs;
        var declaration = ParameterDefinitions().FirstOrDefault(p => p.ParameterName == reference.ParameterName);
        return declaration is VisualShaderNodeColorParameter ? [P.Vector3, P.Scalar] : declaration?.Outputs ?? [P.Scalar];
    }
    private string UniqueParameterName(VisualShaderNodeParameter parameter)
    {
        var name = parameter.ParameterName;
        var start = 0; while (start < name.Length && !char.IsAsciiLetter(name[start])) start++;
        name = new string(name[start..].Where(c => char.IsAsciiLetterOrDigit(c) || c == '_' || c == ' ').ToArray()).Replace(' ', '_');
        if (name.Length == 0) name = parameter.ParameterCaption.Replace("Vec", "Vector", StringComparison.Ordinal);
        var existing = _graphs.SelectMany(g => g.Nodes.Values).Select(e => e.Node).OfType<VisualShaderNodeParameter>().Where(p => !ReferenceEquals(p, parameter)).Select(p => p.ParameterName).ToHashSet(StringComparer.Ordinal);
        for (var attempt = 2; existing.Contains(name); attempt++)
        {
            name = name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9') + attempt.ToString(CultureInfo.InvariantCulture);
        }
        return name;
    }
    private Dictionary<string, VisualShaderNodeParameter> ParameterSymbols()
    {
        var result = new Dictionary<string, VisualShaderNodeParameter>(StringComparer.Ordinal);
        foreach (var parameter in ParameterDefinitions())
        {
            ObjectDisposedException.ThrowIf(parameter.IsDisposed, parameter);
            var name = parameter.ParameterName;
            if (name.Length == 0 || !char.IsAsciiLetter(name[0]) || name.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '_')) ||
                name.StartsWith("gl_", StringComparison.Ordinal) || name.Contains("__", StringComparison.Ordinal) ||
                name is "TIME" or "SCREEN_PIXEL_SIZE" or "SDF_COORDS" or "NORMAL_MAP_FLAGS" or "size" or "TEXTURE" or "NORMAL_TEXTURE" or "SPECULAR_SHININESS_TEXTURE" or "SCREEN_TEXTURE" or "SDF_TEXTURE")
                throw new InvalidDataException("A graph parameter requires a nonreserved ASCII shader identifier.");
            if (_mode == Mode.TextureBlit && name is "SOURCE_TEXTURE0" or "SOURCE_TEXTURE1" or "SOURCE_TEXTURE2" or "SOURCE_TEXTURE3")
                throw new InvalidDataException("Writable-texture source names are renderer-owned and cannot be material parameters.");
            if (!result.TryAdd(name, parameter)) throw new InvalidDataException($"Graph parameter '{name}' has duplicate declarations.");
            if (parameter is VisualShaderNodeMatrixParameter or VisualShaderNodeTextureParameter && parameter.Qualifier is Q.Instance or Q.InstanceIndex)
                throw new NotSupportedException("Matrix and sampled-image graph parameters cannot use per-item storage.");
            if (parameter.Qualifier == Q.Global && parameter.HasParameterDefault)
                throw new InvalidDataException("Global graph parameters receive defaults from the global registry.");
        }
        return result;
    }
    private static Dictionary<string, int> InstanceSlots(Dictionary<string, VisualShaderNodeParameter> symbols)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal); var used = new HashSet<int>();
        foreach (var p in symbols.Values.Where(p => p.Qualifier == Q.InstanceIndex).OrderBy(p => p.ParameterName, StringComparer.Ordinal))
        {
            if (!used.Add(p.InstanceIndex)) throw new InvalidDataException("Distinct graph parameters cannot occupy the same instance slot.");
            result.Add(p.ParameterName, p.InstanceIndex);
        }
        foreach (var p in symbols.Values.Where(p => p.Qualifier == Q.Instance).OrderBy(p => p.ParameterName, StringComparer.Ordinal))
        {
            var slot = Enumerable.Range(0, 16).FirstOrDefault(i => !used.Contains(i), -1);
            if (slot < 0) throw new NotSupportedException("A graph supports at most sixteen per-item parameter slots.");
            used.Add(slot); result.Add(p.ParameterName, slot);
        }
        return result;
    }
    private static string ParameterVariable(VisualShaderNodeParameter declaration, Dictionary<string, string> aliases) =>
        aliases[declaration.Qualifier == Q.Global ? "e2d_globals" : declaration.Qualifier is Q.Instance or Q.InstanceIndex ? "e2d_instance" : "e2d_values"] + "." + declaration.ParameterName;
    private static string ParameterExpression(VisualShaderNodeParameterRef reference, int output, Dictionary<string, VisualShaderNodeParameter> symbols, Dictionary<string, string> aliases)
    {
        if (reference.ParameterName == "[None]") return "0.0";
        if (!symbols.TryGetValue(reference.ParameterName, out var declaration)) throw new InvalidDataException($"Graph parameter reference '{reference.ParameterName}' has no declaration.");
        return ParameterVariable(declaration, aliases) + (declaration is VisualShaderNodeColorParameter ? output == 0 ? ".rgb" : ".a" : "");
    }
    private string ParameterHeader(Type stage, HashSet<int> reachable, Dictionary<string, VisualShaderNodeParameter> symbols, Dictionary<string, int> slots, List<VisualShaderImage> images, Dictionary<string, string> aliases, CustomPlan? custom)
    {
        var graph = GetGraph(stage); var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in reachable)
        {
            var node = graph.Nodes[id].Node;
            if (node is VisualShaderNodeParameter declaration) used.Add(declaration.ParameterName);
            else if (node is VisualShaderNodeParameterRef reference && reference.ParameterName != "[None]")
            {
                if (!symbols.ContainsKey(reference.ParameterName)) throw new InvalidDataException($"Graph parameter reference '{reference.ParameterName}' has no declaration.");
                used.Add(reference.ParameterName);
            }
        }
        var selected = used.OrderBy(n => n, StringComparer.Ordinal).Select(n => symbols[n]).Where(p => p is not VisualShaderNodeTextureParameter).ToArray();
        var header = StageExpression(stage == Type.Vertex ? VertexHeader : FragmentHeader, aliases)
            .Replace("#version 450", "#version 450\n#define ELECTRON2D_SHADER_STAGE " + ((int)stage).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        var screenUV = reachable.Any(id => graph.Nodes[id].Node is VisualShaderNodeTexture { Source: VisualShaderNodeTexture.SourceEnum.Screen }) && stage == Type.Fragment;
        var sdf = reachable.Any(id => UsesSDFImage(graph.Nodes[id].Node));
        var normal = NeedsCanvasNormal(stage, reachable);
        var rendererValues = normal || ReadsRendererValues(ExpressionSources(stage, reachable, custom)) || reachable.Any(id => IsSDFNode(graph.Nodes[id].Node)) || screenUV || selected.Any(p => p.Qualifier == Q.None) || reachable.Any(id => graph.Nodes[id].Node is VisualShaderNodeInput input &&
            (input.GenerateExpression(stage).Contains("TIME", StringComparison.Ordinal) || input.GenerateExpression(stage).Contains("SCREEN_PIXEL_SIZE", StringComparison.Ordinal)));
        var reserved = stage == Type.Vertex ? "float TIME;" : "float TIME; vec2 SCREEN_PIXEL_SIZE;";
        var sdfFields = (sdf ? " vec4 SDF_COORDS;" : "") + (normal ? " vec4 NORMAL_MAP_FLAGS;" : "");
        var fields = string.Join(" ", selected.Where(p => p.Qualifier == Q.None).Select(p => VisualShaderValues.Name(p.Outputs[0]) + " " + p.ParameterName + ";"));
        header = header.Replace(reserved + " };", reserved + sdfFields + " " + fields + " } " + aliases["e2d_values"] + ";", StringComparison.Ordinal);
        if (!rendererValues)
        {
            var at = header.IndexOf("uniform RendererValues", StringComparison.Ordinal);
            var start = header.LastIndexOf('\n', at) + 1; var end = header.IndexOf('\n', at);
            header = header.Remove(start, end - start + 1);
        }
        var extra = new StringBuilder(); var binding = (stage == Type.Vertex ? 1 : 0) + (rendererValues ? 1 : 0);
        foreach (var global in new[] { true, false })
        {
            var members = selected.Where(p => global ? p.Qualifier == Q.Global : p.Qualifier is Q.Instance or Q.InstanceIndex).ToArray();
            if (members.Length == 0) continue;
            extra.Append("layout(set=").Append(stage == Type.Vertex ? 1 : 3).Append(",binding=").Append(binding++).Append(",std140) uniform ").Append(aliases[global ? "Globals" : "Instance"]).AppendLine(" {");
            foreach (var p in global ? members : members.OrderBy(p => slots[p.ParameterName]).ToArray())
            {
                if (!global) extra.Append("layout(offset=").Append(slots[p.ParameterName] * 16).Append(") ");
                extra.Append(VisualShaderValues.Name(p.Outputs[0])).Append(' ').Append(p.ParameterName).AppendLine(";");
            }
            extra.Append("} ").Append(aliases[global ? "e2d_globals" : "e2d_instance"]).AppendLine(";");
        }
        var imageBinding = stage == Type.Fragment ? 1 : 0;
        foreach (var image in images.Where(i => stage != Type.Fragment || i.Name != "TEXTURE"))
            extra.Append("layout(set=").Append(stage == Type.Fragment ? 2 : 0).Append(",binding=").Append(imageBinding++).Append(") uniform ")
                .Append(image.IsArray ? "sampler2DArray " : "sampler2D ").Append(image.Name).AppendLine(";");
        if (reachable.Any(id => graph.Nodes[id].Node is VisualShaderNodeColorFunc { Function: VisualShaderNodeColorFunc.FunctionEnum.RGBToHSV }))
            extra.Append(VisualShaderNodeColorFunc.RGBToHSVDefinition(aliases["e2d_rgb_to_hsv"]));
        if (reachable.Any(id => graph.Nodes[id].Node is VisualShaderNodeRandomRange))
            extra.Append(VisualShaderNodeRandomRange.HashDefinition(aliases["e2d_random_range"]));
        if (reachable.Any(id => graph.Nodes[id].Node is VisualShaderNodeRotationByAxis))
            extra.Append(VisualShaderNodeRotationByAxis.RotationDefinition(aliases["e2d_axis_rotation"]));
        if (sdf) extra.Append(SDFDefinitions(reachable.Select(id => graph.Nodes[id].Node), aliases));
        extra.Append(CustomGlobals(custom, stage, aliases)).Append(GlobalExpressionHeader(stage, aliases));
        return header.Replace("void main() {", extra + "void main() {", StringComparison.Ordinal);
    }
}
