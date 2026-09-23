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

internal sealed record ShaderUniform(string Name, Type Type, int Buffer, int Offset, int ElementSize, int ArrayLength, int Stride)
{
    internal int Count => Math.Max(1, ArrayLength);
    internal bool Accepts<T>() where T : unmanaged => typeof(T) == Type || Type == typeof(Vector4) && typeof(T) == typeof(Color);
    internal bool SameType(ShaderUniform other) => Type == other.Type && ArrayLength == other.ArrayLength;

    internal PropertyDescriptor Describe(string propertyName) => Type == typeof(float) ? Describe<float>(propertyName) :
        Type == typeof(int) ? Describe<int>(propertyName) : Type == typeof(uint) ? Describe<uint>(propertyName) :
        Type == typeof(Vector2) ? Describe<Vector2>(propertyName) : Type == typeof(Vector4) ? Describe<Vector4>(propertyName) :
        Type == typeof(Vector2I) ? Describe<Vector2I>(propertyName) : Describe<Vector4I>(propertyName);

    private PropertyDescriptor Describe<T>(string propertyName) where T : unmanaged => ArrayLength == 0
        ? new PropertyDescriptor<ShaderMaterial, T>(propertyName, m => m.GetShaderParameter<T>(Name), (m, v) => m.SetShaderParameter(Name, v), _ => default, stored: true)
        : new PropertyDescriptor<ShaderMaterial, T[]>(propertyName, m => m.GetShaderParameterArray<T>(Name),
            (m, v) => { ArgumentNullException.ThrowIfNull(v); m.SetShaderParameter<T>(Name, v.AsSpan()); }, _ => new T[ArrayLength], stored: true);
}
