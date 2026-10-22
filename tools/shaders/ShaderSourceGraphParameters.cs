using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using Electron2D;

internal static unsafe partial class ShaderSourceTypes
{
    internal static byte[] CanonicalizeGraphBlocks(byte[] code, IReadOnlyList<VisualShaderBlockName> blocks, bool fragment, Shader.Mode mode = Shader.Mode.CanvasItem)
    {
        var names = blocks.Where(b => b.Stage == GraphStage(fragment, mode)).ToDictionary(b => b.Name, b => b.Canonical, StringComparer.Ordinal);
        if (names.Count == 0) return code;
        var words = MemoryMarshal.Cast<byte, uint>(code); var structures = new HashSet<uint>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            if ((words[at] & 0xffff) == 30) structures.Add(words[at + 1]);
        using var output = new MemoryStream(); using var writer = new BinaryWriter(output); writer.Write(code.AsSpan(0, 20));
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16);
            if ((words[at] & 0xffff) == 5 && count > 2 && structures.Contains(words[at + 1]))
            {
                var text = MemoryMarshal.AsBytes(words.Slice(at + 2, count - 2)); var end = text.IndexOf((byte)0);
                if (end >= 0 && names.TryGetValue(new UTF8Encoding(false, true).GetString(text[..end]), out var canonical))
                {
                    var bytes = Encoding.UTF8.GetBytes(canonical + '\0'); writer.Write((uint)((bytes.Length + 3) / 4 + 2) << 16 | 5u); writer.Write(words[at + 1]); writer.Write(bytes);
                    while (output.Position % 4 != 0) writer.Write((byte)0);
                    continue;
                }
            }
            writer.Write(MemoryMarshal.AsBytes(words.Slice(at, count)));
        }
        return output.ToArray();
    }

    internal static byte[] AddGraphParameters(byte[] code, VisualShader graph, bool fragment)
    {
        var program = ShaderCompiler.ValidateInterface(code, fragment, mode: graph.GetMode()); var records = new List<string>();
        foreach (var declaration in graph.ParameterDefinitions().OrderBy(p => p.ParameterName, StringComparer.Ordinal))
        {
            var name = declaration.ParameterName;
            if (!(program.Uniforms.TryGetValue(name, out var uniform) || program.InstanceUniforms.TryGetValue(name, out uniform))) continue;
            if (declaration.HasParameterDefault)
            {
                var values = GraphDefaultWords(declaration.ParameterDefault ?? throw new InvalidDataException("A numeric graph default is absent."));
                if (values.Length != uniform.DefaultWordWidth) throw new InvalidDataException("Graph default differs from its compiled parameter role.");
                records.Add(DefaultRecord(uniform, values));
            }
            var hint = PropertyHint.None; var text = "";
            if (declaration is VisualShaderNodeFloatParameter f && f.Hint != VisualShaderNodeFloatParameter.HintEnum.None)
            {
                hint = PropertyHint.Range; text = f.Min.ToString("R", CultureInfo.InvariantCulture) + "," + f.Max.ToString("R", CultureInfo.InvariantCulture);
                if (f.Hint == VisualShaderNodeFloatParameter.HintEnum.RangeStep) text += "," + f.Step.ToString("R", CultureInfo.InvariantCulture);
            }
            if (declaration is VisualShaderNodeIntParameter i)
            {
                if (i.Hint == VisualShaderNodeIntParameter.HintEnum.Enum) { hint = PropertyHint.Enum; text = string.Join(',', i.EnumNames); }
                else if (i.Hint != VisualShaderNodeIntParameter.HintEnum.None)
                {
                    hint = PropertyHint.Range; text = i.Min.ToString(CultureInfo.InvariantCulture) + "," + i.Max.ToString(CultureInfo.InvariantCulture);
                    if (i.Hint == VisualShaderNodeIntParameter.HintEnum.RangeStep) text += "," + i.Step.ToString(CultureInfo.InvariantCulture);
                }
            }
            records.Add(FormattableString.Invariant($"Electron2D:hint:1:{Convert.ToBase64String(Encoding.UTF8.GetBytes(name))}:{(int)hint}:{Convert.ToBase64String(new UTF8Encoding(false, true).GetBytes(text))}:{(declaration is VisualShaderNodeColorParameter ? 1 : 0)}"));
        }
        return AppendMetadata(code, records);
    }
    internal static byte[] AddGraphImages(byte[] code, IReadOnlyList<VisualShaderImage> images, bool fragment, Shader.Mode mode = Shader.Mode.CanvasItem)
    {
        var program = ShaderCompiler.ValidateInterface(code, fragment, mode: mode); var records = new List<string>();
        foreach (var image in images.Where(i => i.Stage == GraphStage(fragment, mode) && i.Policy is not null))
        {
            if (!program.Textures.Any(t => t.Name == image.Name)) continue;
            var policy = image.Policy!.Value;
            records.Add(FormattableString.Invariant($"Electron2D:image:2:{Convert.ToBase64String(Encoding.UTF8.GetBytes(image.Name))}:{Convert.ToBase64String(Encoding.UTF8.GetBytes(image.PublicName ?? image.Name))}:{Convert.ToBase64String(Encoding.UTF8.GetBytes(policy.GlobalName ?? ""))}:{(image.IsArray ? 1 : 0)}:{(policy.SourceColor ? 1 : 0)}:{policy.Fallback}:{policy.Filter}:{policy.Repeat}:{(policy.Screen ? 1 : 0)}"));
        }
        return AppendMetadata(code, records);
    }
    private static VisualShader.Type GraphStage(bool fragment, Shader.Mode mode) => mode switch
    { Shader.Mode.Particles => VisualShader.Type.Process, Shader.Mode.TextureBlit => VisualShader.Type.TextureBlit, _ => fragment ? VisualShader.Type.Fragment : VisualShader.Type.Vertex };
    private static uint[] GraphDefaultWords(object value)
    {
        static uint F(float number) => BitConverter.SingleToUInt32Bits(number);
        if (value is System.Numerics.Matrix4x4 matrix)
        {
            var words = new uint[16];
            for (var column = 0; column < 4; column++) for (var row = 0; row < 4; row++) words[column * 4 + row] = F(matrix[row, column]);
            return words;
        }
        return value switch
        {
            float f => [F(f)],
            int i => [unchecked((uint)i)],
            uint u => [u],
            bool b => [b ? 1u : 0],
            Vector2 v => [F(v.X), F(v.Y)],
            Vector3 v => [F(v.X), F(v.Y), F(v.Z)],
            Vector4 v => [F(v.X), F(v.Y), F(v.Z), F(v.W)],
            Color c => [F(c.R), F(c.G), F(c.B), F(c.A)],
            _ => throw new InvalidDataException("Unsupported graph parameter default.")
        };
    }
}
