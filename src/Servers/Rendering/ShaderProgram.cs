using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed class ShaderProgram(byte[] code, int[] bufferSizes, Dictionary<string, ShaderUniform> uniforms, ShaderTexture[]? textures = null, ShaderUniform? timeUniform = null)
{
    internal static readonly ShaderProgram Default = new(BuiltInShaders.Fragment, [], new(StringComparer.Ordinal), [new("TEXTURE", 0)]);
    internal readonly byte[] Code = code;
    internal readonly int[] BufferSizes = bufferSizes;
    internal readonly Dictionary<string, ShaderUniform> Uniforms = uniforms;
    internal readonly ShaderTexture[] Textures = textures ?? [];
    internal readonly ShaderUniform? TimeUniform = timeUniform;
    internal readonly IReadOnlyList<PropertyDescriptor> Descriptors = Array.AsReadOnly(uniforms.Values.Select(u => u.Describe(u.Name))
        .Concat((textures ?? []).Where(t => !t.IsCanvasTexture).Select(t => t.Describe(t.Name))).ToArray());
    internal readonly IReadOnlyList<PropertyDescriptor> MaterialDescriptors = Array.AsReadOnly(uniforms.Values.Select(u => u.Describe("shader_parameter/" + u.Name))
        .Concat((textures ?? []).Where(t => !t.IsCanvasTexture).Select(t => t.Describe("shader_parameter/" + t.Name))).ToArray());

    internal int FindTexture(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        for (var i = 0; i < Textures.Length; i++) if (!Textures[i].IsCanvasTexture && Textures[i].Name == name) return i;
        throw new ArgumentException($"Shader has no sampled 2D texture named '{name}'.", nameof(name));
    }
}

internal sealed record ShaderTexture(string Name, int Binding)
{
    internal bool IsCanvasTexture => Name == "TEXTURE";
    internal PropertyDescriptor Describe(string propertyName) => new PropertyDescriptor<ShaderMaterial, Texture?>(propertyName,
        m => m.GetShaderParameter(Name), (m, t) => m.SetShaderParameter(Name, t), _ => null, stored: true);
}

internal sealed record ShaderUniform(string Name, Type Type, int Buffer, int Offset, int ElementSize, int ArrayLength, int Stride, bool Unsigned, int MatrixStride, bool RowMajor, int BooleanWidth)
{
    internal int Count => Math.Max(1, ArrayLength);
    internal bool Accepts<T>() where T : unmanaged => typeof(T) == Type || Type == typeof(Vector3) && typeof(T) == typeof(Color) || Type == typeof(Vector4) && (typeof(T) == typeof(Color) || typeof(T) == typeof(Rect));
    internal bool SameType(ShaderUniform other) => Type == other.Type && Unsigned == other.Unsigned && ArrayLength == other.ArrayLength && BooleanWidth == other.BooleanWidth;

    internal PropertyDescriptor Describe(string propertyName) => Type == typeof(float) ? Describe<float>(propertyName) :
        Type == typeof(bool) ? Describe<bool>(propertyName) : Type == typeof(int) ? Describe<int>(propertyName) : Type == typeof(uint) ? Describe<uint>(propertyName) :
        Type == typeof(Vector2) ? Describe<Vector2>(propertyName) : Type == typeof(Vector3) ? Describe<Vector3>(propertyName) : Type == typeof(Vector4) ? Describe<Vector4>(propertyName) :
        Type == typeof(Transform) ? Describe<Transform>(propertyName) : Type == typeof(Vector2I) ? Describe<Vector2I>(propertyName) : Type == typeof(Vector3I) ? Describe<Vector3I>(propertyName) : Describe<Vector4I>(propertyName);

    internal void Write<T>(Span<byte> target, in T value) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in value, 1));
        if (BooleanWidth != 0)
        {
            var bits = Type == typeof(bool) ? (bytes[0] != 0 ? 1 : 0) : MemoryMarshal.Read<int>(bytes);
            var stored = MemoryMarshal.Cast<byte, int>(target);
            for (var i = 0; i < BooleanWidth; i++) stored[i] = (bits >> i) & 1;
        }
        else if (Type == typeof(Transform))
        {
            var basis = MemoryMarshal.Cast<byte, float>(bytes); var stored = MemoryMarshal.Cast<byte, float>(target);
            stored[0] = basis[0]; stored[RowMajor ? MatrixStride / 4 : 1] = basis[1];
            stored[RowMajor ? 1 : MatrixStride / 4] = basis[2]; stored[MatrixStride / 4 + 1] = basis[3];
        }
        else bytes[..ElementSize].CopyTo(target);
    }

    internal T Read<T>(ReadOnlySpan<byte> source) where T : unmanaged
    {
        T value = default;
        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
        if (BooleanWidth != 0)
        {
            var stored = MemoryMarshal.Cast<byte, int>(source); var bits = 0;
            for (var i = 0; i < BooleanWidth; i++) if (stored[i] != 0) bits |= 1 << i;
            if (Type == typeof(bool)) bytes[0] = (byte)bits;
            else MemoryMarshal.Write(bytes, in bits);
        }
        else if (Type == typeof(Transform))
        {
            var basis = MemoryMarshal.Cast<byte, float>(bytes); var stored = MemoryMarshal.Cast<byte, float>(source);
            basis[0] = stored[0]; basis[1] = stored[RowMajor ? MatrixStride / 4 : 1];
            basis[2] = stored[RowMajor ? 1 : MatrixStride / 4]; basis[3] = stored[MatrixStride / 4 + 1];
        }
        else source.CopyTo(bytes);
        if (Type == typeof(Vector3) && typeof(T) == typeof(Color)) MemoryMarshal.Cast<byte, float>(bytes)[3] = 1;
        return value;
    }

    private PropertyDescriptor Describe<T>(string propertyName) where T : unmanaged
    {
        var defaults = new byte[ElementSize];
        if (Type == typeof(Transform)) Write(defaults, Transform.Identity);
        var initial = Read<T>(defaults);
        return ArrayLength == 0
            ? new PropertyDescriptor<ShaderMaterial, T>(propertyName, m => m.GetShaderParameter<T>(Name), (m, v) => m.SetShaderParameter(Name, v), _ => initial, stored: true)
            : new PropertyDescriptor<ShaderMaterial, T[]>(propertyName, m => m.GetShaderParameterArray<T>(Name),
                (m, v) => { ArgumentNullException.ThrowIfNull(v); m.SetShaderParameter<T>(Name, v.AsSpan()); }, _ => Enumerable.Repeat(initial, ArrayLength).ToArray(), stored: true);
    }
}
