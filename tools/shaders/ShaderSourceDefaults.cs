using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Electron2D;

internal static unsafe partial class ShaderSourceTypes
{
    internal static byte[] AddConfiguredDefaults(byte[] code, string input, bool fragment, Shader.Mode mode)
    {
        var path = input + ".defaults.json";
        if (!File.Exists(path)) return code;
        try { return ReadConfiguredDefaults(code, path, fragment, mode); }
        catch (Exception error) when (error is ArgumentException or JsonException or InvalidOperationException or FormatException or OverflowException)
        { throw new ArgumentException($"{path}: {error.Message}", error); }
    }
    private static byte[] ReadConfiguredDefaults(byte[] code, string path, bool fragment, Shader.Mode mode)
    {
        if (new FileInfo(path).Length > 16 * 1024 * 1024) throw new ArgumentException("Shader defaults input exceeds sixteen MiB.");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
            !root.TryGetProperty("version", out var version) || !version.TryGetInt32(out var schema) || schema != 1 ||
            !root.TryGetProperty("parameters", out var parameters) || parameters.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Shader defaults require version one and a parameters object.");
        var program = ShaderCompiler.ValidateInterface(code, fragment, mode: mode);
        var names = new HashSet<string>(StringComparer.Ordinal); var records = new List<string>();
        foreach (var property in parameters.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            var name = property.Name;
            if (!names.Add(name) || !(program.Uniforms.TryGetValue(name, out var uniform) || program.InstanceUniforms.TryGetValue(name, out uniform)))
                throw new ArgumentException($"Default parameter '{name}' is duplicate, absent or renderer-owned.");
            var values = new uint[uniform.DefaultWordWidth * uniform.Count];
            if (uniform.ArrayLength != 0)
            {
                RequireArray(property.Value, uniform.Count);
                for (var i = 0; i < uniform.Count; i++) ReadElement(property.Value[i], uniform, values.AsSpan(i * uniform.DefaultWordWidth, uniform.DefaultWordWidth));
            }
            else ReadElement(property.Value, uniform, values);
            if (uniform.DefaultWords is not null)
            {
                if (!uniform.HasSameDefault(values)) throw new ArgumentException($"Default parameter '{name}' conflicts with an existing authored default.");
                continue;
            }
            records.Add(DefaultRecord(uniform, values));
        }
        return AppendMetadata(code, records);
    }
    private static void RequireArray(JsonElement value, int count)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != count)
            throw new ArgumentException($"Shader default requires exactly {count} array elements.");
    }
    private static void ReadElement(JsonElement value, ShaderUniform uniform, Span<uint> words)
    {
        if (words.Length > 1) RequireArray(value, words.Length);
        for (var i = 0; i < words.Length; i++)
        {
            var scalar = words.Length == 1 ? value : value[i];
            if (uniform.BooleanWidth != 0)
            {
                if (scalar.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new ArgumentException("Boolean defaults require JSON booleans.");
                words[i] = scalar.GetBoolean() ? 1u : 0;
            }
            else if (uniform.DefaultTypeCode[0] is 'f' or 'm')
            {
                var number = scalar.GetSingle();
                if (!float.IsFinite(number)) throw new ArgumentException("Floating defaults must be finite.");
                words[i] = BitConverter.SingleToUInt32Bits(number);
            }
            else words[i] = uniform.Unsigned ? scalar.GetUInt32() : unchecked((uint)scalar.GetInt32());
        }
    }
    private static string DefaultRecord(ShaderUniform uniform, ReadOnlySpan<uint> values)
    {
        var bytes = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 4), values[i]);
        return FormattableString.Invariant($"Electron2D:default:1:{uniform.DefaultTypeCode}:{uniform.ArrayLength}:{Convert.ToBase64String(Encoding.UTF8.GetBytes(uniform.Name))}:{Convert.ToBase64String(bytes)}");
    }
    private static byte[] AppendMetadata(byte[] code, IReadOnlyList<string> records)
    {
        if (records.Count == 0) return code;
        var words = MemoryMarshal.Cast<byte, uint>(code).ToArray(); var at = 5;
        while (at < words.Length && (words[at] & 0xffff) is 10 or 11 or 14 or 15 or 16 or 17 or 331) at += (int)(words[at] >> 16);
        using var output = new MemoryStream(); using var writer = new BinaryWriter(output); var id = words[3]; words[3] = checked(id + (uint)records.Count);
        writer.Write(MemoryMarshal.AsBytes(words.AsSpan(0, at)));
        foreach (var record in records)
        {
            var text = Encoding.UTF8.GetBytes(record + '\0'); var count = checked((text.Length + 3) / 4 + 2);
            if (count > ushort.MaxValue) throw new ArgumentException("Shader metadata exceeds the SPIR-V instruction limit.");
            writer.Write((uint)count << 16 | 7); writer.Write(id++); writer.Write(text);
            while (output.Position % 4 != 0) writer.Write((byte)0);
        }
        writer.Write(MemoryMarshal.AsBytes(words.AsSpan(at))); return output.ToArray();
    }
}
