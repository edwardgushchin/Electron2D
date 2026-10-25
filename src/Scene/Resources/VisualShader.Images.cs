using System.Globalization;
using P = Electron2D.VisualShaderNode.PortType;
using T = Electron2D.VisualShaderNodeTexture;
using TP = Electron2D.VisualShaderNodeTextureParameter;
using Q = Electron2D.VisualShaderNodeParameter.QualifierEnum;

namespace Electron2D;

internal sealed record VisualShaderBlockName(VisualShader.Type Stage, string Name, string Canonical);

internal sealed record VisualShaderImage(VisualShader.Type Stage, string Name, bool IsArray, ShaderImagePolicy? Policy = null, Resource? Default = null, string? PublicName = null);

public sealed partial class VisualShader
{
    private static Dictionary<string, string> ImageNames(Dictionary<string, VisualShaderNodeParameter> parameters)
    {
        var reserved = parameters.Keys.ToHashSet(StringComparer.Ordinal); var used = new HashSet<string>(StringComparer.Ordinal);
        var result = new Dictionary<string, string>(StringComparer.Ordinal); var suffix = 0;
        foreach (var p in parameters.Values.OfType<TP>().OrderBy(p => p.ParameterName, StringComparer.Ordinal))
        {
            var global = p.Qualifier == Q.Global; var name = (global ? "GLOBAL_" : "") + p.ParameterName;
            if (!global && name.StartsWith("GLOBAL_", StringComparison.Ordinal) || global && reserved.Contains(name) || !used.Add(name))
            {
                do { name = (global ? "GLOBAL_" : "") + "e2d_param_image_" + (suffix++).ToString(CultureInfo.InvariantCulture); }
                while (reserved.Contains(name) || !used.Add(name));
            }
            result.Add(p.ParameterName, name);
        }
        return result;
    }

    private (List<VisualShaderImage> Images, Dictionary<int, string> Nodes) ImagePlan(Type stage, HashSet<int> reachable,
        Dictionary<string, VisualShaderNodeParameter> parameters, CustomPlan? custom)
    {
        var images = new List<VisualShaderImage>(); var nodes = new Dictionary<int, string>();
        var nativeNames = ImageNames(parameters);
        var reserved = nativeNames.Values.Concat(parameters.Keys).ToHashSet(StringComparer.Ordinal);
        reserved.UnionWith(ExpressionSources(stage, reachable, custom).SelectMany(s => SourceIdentifiers(s).Select(t => s.Substring(t.Start, t.Length))));
        reserved.UnionWith(["TEXTURE", "NORMAL_TEXTURE", "SPECULAR_SHININESS_TEXTURE", "SCREEN_TEXTURE", "SDF_TEXTURE"]);
        if (stage == Type.Fragment) images.Add(new(stage, "TEXTURE", false));
        string Anonymous(int id)
        {
            var stem = "e2d_image_" + (int)stage + "_" + id.ToString(CultureInfo.InvariantCulture); var name = stem;
            for (var suffix = 2; !reserved.Add(name); suffix++) name = stem + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            return name;
        }
        void Add(int id, VisualShaderImage image)
        {
            var existing = images.FirstOrDefault(i => i.Name == image.Name);
            if (existing is null) images.Add(image);
            else if (existing != image) throw new InvalidDataException("Conflicting graph image declarations.");
            nodes.Add(id, image.Name);
        }
        foreach (var id in reachable.Order())
        {
            var node = GetNode(stage, id);
            var parameter = node as VisualShaderNodeParameter;
            if (node is VisualShaderNodeParameterRef reference && parameters.TryGetValue(reference.ParameterName, out var referenced)) parameter = referenced;
            if (parameter is TP texture)
            {
                var array = texture is VisualShaderNodeTextureArrayParameter;
                var screen = texture.TextureSource == TP.TextureSourceEnum.Screen;
                if (screen && (array || stage != Type.Fragment || texture.TextureType is TP.TextureTypeEnum.NormalMap or TP.TextureTypeEnum.Anisotropy || texture.ColorDefault != TP.ColorDefaultEnum.White))
                    throw new NotSupportedException("Screen image parameters require a fragment ordinary sampler with a data/color interpretation and white default.");
                var fallback = texture.TextureType switch { TP.TextureTypeEnum.NormalMap => array ? 0 : 3, TP.TextureTypeEnum.Anisotropy => array ? 0 : 4, _ => (int)texture.ColorDefault };
                Add(id, new(stage, nativeNames[texture.ParameterName], array,
                    new(texture.TextureType == TP.TextureTypeEnum.Color && !screen, fallback, (int)texture.TextureFilter, (int)texture.TextureRepeat, screen, texture.Qualifier == Q.Global ? texture.ParameterName : null),
                    PublicName: texture.Qualifier == Q.Global ? nativeNames[texture.ParameterName] : texture.ParameterName));
            }
            else if (node is T ordinary)
            {
                switch (ordinary.Source)
                {
                    case T.SourceEnum.Texture:
                        Add(id, new(stage, Anonymous(id), false, new(ordinary.TextureType == T.TextureTypeEnum.Color, ordinary.TextureType == T.TextureTypeEnum.NormalMap ? 3 : 0, 0, 0, false), ordinary.Texture)); break;
                    case T.SourceEnum.Screen when stage == Type.Fragment:
                        Add(id, new(stage, Anonymous(id), false, new(false, 0, 0, 0, true))); break;
                    case T.SourceEnum.Canvas when stage == Type.Fragment:
                        Add(id, new(stage, "TEXTURE", false)); break;
                    case T.SourceEnum.CanvasNormal when stage == Type.Fragment:
                        Add(id, new(stage, "NORMAL_TEXTURE", false)); break;
                }
            }
            else if (node is VisualShaderNodeTextureArray layered && layered.Source == VisualShaderNodeLayeredSample.SourceEnum.Texture)
                Add(id, new(stage, Anonymous(id), true, new(false, 0, 0, 0, false), layered.TextureArray));
            else if (node is VisualShaderNodeInput input && input.OutputsFor(stage)[0] == P.Sampler)
            {
                _ = input.GenerateExpression(stage);
                Add(id, new(stage, input.GetInputRealName(), false));
            }
        }
        if (reachable.Any(id => UsesSDFImage(GetNode(stage, id)))) images.Add(new(stage, "SDF_TEXTURE", false));
        if (NeedsCanvasNormal(stage, reachable) && images.All(image => image.Name != "NORMAL_TEXTURE")) images.Add(new(stage, "NORMAL_TEXTURE", false));
        return (images, nodes);
    }

    private static Dictionary<string, string> StageSymbols(IEnumerable<VisualShaderImage> images, IEnumerable<string> sourceNames)
    {
        var used = images.Select(i => i.Name).Concat(sourceNames).ToHashSet(StringComparer.Ordinal); var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var symbol in new[] { "VERTEX", "UV", "COLOR", "INSTANCE_CUSTOM", "e2d_values", "e2d_globals", "e2d_instance", "Frame", "RendererValues", "Globals", "Instance", "e2d_rgb_to_hsv", "e2d_random_range", "e2d_axis_rotation", "e2d_sdf", "e2d_sdf_normal", "e2d_sdf_raymarch", "e2d_graph", "e2d_frame", "result", "inPosition", "inColor", "inUV", "inCustom", "outColor", "outUV", "outCustom", "e2d_normal" })
        {
            var name = symbol;
            for (var suffix = 2; !used.Add(name); suffix++) name = symbol + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            aliases.Add(symbol, name);
        }
        return aliases;
    }
    private static string StageExpression(string expression, Dictionary<string, string> aliases) =>
        System.Text.RegularExpressions.Regex.Replace(expression, @"\b(VERTEX|UV|COLOR|INSTANCE_CUSTOM|e2d_values|e2d_globals|e2d_instance|Frame|RendererValues|Globals|Instance|e2d_frame|result|inPosition|inColor|inUV|inCustom|outColor|outUV|outCustom)\b", m => aliases[m.Value]);

    private static string SampleImage(string sampler, IReadOnlyList<string> inputs)
    {
        if (sampler.Length == 0) return "vec4(0.0)";
        return inputs[1].Length == 0 ? "texture(" + sampler + ", " + inputs[0] + ")" :
            "textureLod(" + sampler + ", " + inputs[0] + ", " + inputs[1] + ")";
    }
}
