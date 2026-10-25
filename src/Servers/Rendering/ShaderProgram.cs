using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed partial class ShaderProgram(byte[] code, int[] bufferSizes, Dictionary<string, ShaderUniform> uniforms, ShaderTexture[]? textures = null, ShaderUniform? timeUniform = null, ShaderUniform? screenPixelSizeUniform = null, ShaderUniform? sdfCoordsUniform = null, Dictionary<string, ShaderUniform>? instanceUniforms = null, Dictionary<string, ShaderUniform>? globalUniforms = null)
{
    internal static readonly ShaderProgram Default = new(BuiltInShaders.Fragment, [], new(StringComparer.Ordinal), [new("TEXTURE", 0)]);
    internal Shader.Mode Mode { get; set; } = Shader.Mode.CanvasItem;
    internal int ColorOutputs { get; set; } = 1;
    internal int ParticleStorageBuffers { get; set; } = 1;
    internal readonly byte[] Code = code;
    internal byte[] NativeCode { get; init; } = code;
    internal byte[]? ValidationCode { get; init; }
    internal CanvasLightEntry? AuthoredLight { get; init; }
    internal readonly int[] BufferSizes = bufferSizes;
    internal readonly Dictionary<string, ShaderUniform> Uniforms = uniforms;
    internal readonly ShaderTexture[] Textures = textures ?? [];
    internal readonly Dictionary<string, ShaderUniform> InstanceUniforms = instanceUniforms ?? new(StringComparer.Ordinal);
    internal readonly Dictionary<string, ShaderUniform> GlobalUniforms = globalUniforms ?? new(StringComparer.Ordinal);
    internal readonly bool HasInstanceUniforms = instanceUniforms is { Count: > 0 };
    internal readonly IReadOnlyList<PropertyDescriptor> InstanceDescriptors = Array.AsReadOnly((instanceUniforms ?? []).Values.Select(u => u.DescribeInstance(u.Name)).ToArray());
    internal readonly IReadOnlyList<PropertyDescriptor> CanvasInstanceDescriptors = Array.AsReadOnly((instanceUniforms ?? []).Values.Select(u => u.DescribeInstance("instance_shader_parameters/" + u.Name)).ToArray());
    internal readonly ShaderUniform? TimeUniform = timeUniform;
    internal readonly ShaderUniform? ScreenPixelSizeUniform = screenPixelSizeUniform;
    internal readonly ShaderUniform? SDFCoordsUniform = sdfCoordsUniform;
    internal ShaderUniform? NormalMapFlagsUniform { get; init; }
    internal ShaderUniform? TexturePixelSizeUniform { get; init; }
    internal ShaderUniform? ModelMatrixUniform { get; init; }
    internal ShaderUniform? CanvasMatrixUniform { get; init; }
    internal ShaderUniform? ScreenMatrixUniform { get; init; }
    internal readonly bool UsesSDFTexture = (textures ?? []).Any(t => t.IsSDFTexture);
    internal readonly bool UsesScreenTexture = (textures ?? []).Any(t => t.IsScreenTexture);
    internal readonly IReadOnlyList<PropertyDescriptor> Descriptors = Array.AsReadOnly(uniforms.Values.Select(u => u.Describe(u.Name))
        .Concat((textures ?? []).Where(t => t.IsMaterialTexture && t.Element == 0).Select(t => t.Describe(t.Name))).ToArray());
    internal readonly IReadOnlyList<PropertyDescriptor> MaterialDescriptors = Array.AsReadOnly(uniforms.Values.Select(u => u.Describe("shader_parameter/" + u.Name))
        .Concat((textures ?? []).Where(t => t.IsMaterialTexture && t.Element == 0).Select(t => t.Describe("shader_parameter/" + t.Name))).ToArray());

    internal int FindTexture(string name, int index = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        for (var i = 0; i < Textures.Length; i++) if (Textures[i].IsMaterialTexture && Textures[i].Name == name)
            {
                if ((uint)index >= (uint)Textures[i].Count) throw new ArgumentOutOfRangeException(nameof(index));
                return i + index;
            }
        throw new ArgumentException($"Shader has no sampled texture named '{name}'.", nameof(name));
    }
}

internal sealed record ShaderTexture(string Name, int Binding, bool IsArray = false, int PixelType = 0, int ArrayLength = 0, int Element = 0, bool BlitSource = false)
{
    internal ShaderImagePolicy? ImagePolicy { get; init; }
    internal string NativeName { get; init; } = Name;
    private readonly string _legacyGlobalName = Name.StartsWith("GLOBAL_", StringComparison.Ordinal) ? Name[7..] : string.Empty;
    internal int Count => Math.Max(1, ArrayLength);
    internal bool IsGlobal => ImagePolicy is { } policy ? policy.GlobalName is not null : Name.StartsWith("GLOBAL_", StringComparison.Ordinal);
    internal string GlobalName => ImagePolicy is { } policy ? policy.GlobalName ?? string.Empty : _legacyGlobalName;
    internal bool IsMaterialTexture => !IsEngineTexture && !IsGlobal;
    internal bool IsCanvasTexture => Name == "TEXTURE";
    internal bool IsScreenTexture => Name == "SCREEN_TEXTURE" || ImagePolicy is { Screen: true };
    internal bool IsNormalTexture => Name == "NORMAL_TEXTURE";
    internal bool IsSpecularTexture => Name == "SPECULAR_SHININESS_TEXTURE";
    internal bool IsSDFTexture => Name == "SDF_TEXTURE";
    internal bool IsEngineTexture => IsCanvasTexture || IsNormalTexture || IsSpecularTexture || IsScreenTexture || IsSDFTexture || BlitSource;
    internal void RequireShape(bool array)
    {
        if (IsArray != array) throw new ArgumentException($"Texture parameter '{Name}' requires {(IsArray ? "layered" : "ordinary")} images.");
    }
    internal void RequireBindingArray(bool array)
    {
        if ((ArrayLength != 0) != array) throw new ArgumentException($"Texture parameter '{Name}' requires {(ArrayLength != 0 ? "an array" : "a scalar binding")}.");
    }
    internal bool SameType(ShaderTexture other) => IsArray == other.IsArray && PixelType == other.PixelType && ArrayLength == other.ArrayLength;
    internal static Resource? Source(Resource? texture)
    {
        if (texture is ServerTexture { IsProxy: true } proxy) texture = RenderingTextureRegistry.ResolveProxySource(proxy.ProxyTarget);
        if (texture is LayeredTextureProxy layered) texture = RenderingTextureRegistry.ResolveLayeredProxySource(layered.ProxyTarget);
        if (texture is AtlasTexture atlas) texture = atlas.RenderingTexture;
        if (texture is CanvasTexture channels) texture = Source(channels.DiffuseTexture);
        return texture;
    }
    internal static int ResourcePixelType(Resource? texture) => Source(texture) switch
    {
        TextureRD deviceTexture => deviceTexture.DeviceInfo is { } info ? RenderingDevice.PixelType(info.Format) : 0,
        TextureLayeredRD deviceArray => deviceArray.DeviceInfo is { } info ? RenderingDevice.PixelType(info.Format) : 0,
        Texture ordinary => ordinary.CapturePixels()?.PixelType ?? 0,
        TextureLayered layered => layered.CaptureLayers()?.First.PixelType ?? 0,
        _ => 0
    };
    internal void Validate(Resource? texture)
    {
        if (texture is null) return;
        if (texture.IsDisposed) throw new ObjectDisposedException(nameof(texture));
        if (IsArray ? texture is not TextureLayered : texture is not Texture) throw new ArgumentException($"Texture parameter '{Name}' has a different sampled image shape.");
        if (ResourcePixelType(texture) != PixelType) throw new ArgumentException($"Texture parameter '{Name}' has a different sampled scalar type.");
    }
    internal PropertyDescriptor DescribeDefault() => ArrayLength != 0
        ? IsArray
            ? new PropertyDescriptor<Shader, TextureLayered?[]>("default_texture_parameter/" + Name, shader => shader.GetDefaultSampledArray<TextureLayered>(Name), (shader, value) => shader.SetDefaultSampledArray(Name, value, true), _ => new TextureLayered?[Count], stored: true)
            : new PropertyDescriptor<Shader, Texture?[]>("default_texture_parameter/" + Name, shader => shader.GetDefaultSampledArray<Texture>(Name), (shader, value) => shader.SetDefaultSampledArray(Name, value, false), _ => new Texture?[Count], stored: true)
        : IsArray
        ? new PropertyDescriptor<Shader, TextureLayered?>("default_texture_parameter/" + Name, shader => shader.GetDefaultLayeredTextureParameter(Name), (shader, value) => shader.SetDefaultLayeredTextureParameter(Name, value), _ => null, stored: true)
        : new PropertyDescriptor<Shader, Texture?>("default_texture_parameter/" + Name, shader => shader.GetDefaultTextureParameter(Name), (shader, value) => shader.SetDefaultTextureParameter(Name, value), _ => null, stored: true);
    internal PropertyDescriptor Describe(string propertyName) => ArrayLength != 0
        ? IsArray
            ? new PropertyDescriptor<ShaderMaterial, TextureLayered?[]>(propertyName, m => m.GetShaderLayeredParameterArray(Name), (m, value) => m.SetShaderLayeredParameterArray(Name, value), _ => new TextureLayered?[Count], stored: true)
            : new PropertyDescriptor<ShaderMaterial, Texture?[]>(propertyName, m => m.GetShaderTextureParameterArray(Name), (m, value) => m.SetShaderTextureParameterArray(Name, value), _ => new Texture?[Count], stored: true)
        : IsArray
        ? new PropertyDescriptor<ShaderMaterial, TextureLayered?>(propertyName, m => m.GetShaderLayeredParameter(Name), (m, t) => m.SetShaderLayeredParameter(Name, t), _ => null, stored: true)
        : new PropertyDescriptor<ShaderMaterial, Texture?>(propertyName,
        m => m.GetShaderParameter(Name), (m, t) => m.SetShaderParameter(Name, t), _ => null, stored: true);
}

internal sealed partial record ShaderUniform(string Name, Type Type, int Buffer, int Offset, int ElementSize, int ArrayLength, int Stride, bool Unsigned, int MatrixStride, bool RowMajor, int BooleanWidth, int MatrixColumns = 0, int MatrixRows = 0)
{
    internal int MatrixComponents => MatrixColumns * MatrixRows;
    internal int Count => Math.Max(1, ArrayLength);
    internal bool Accepts<T>() where T : unmanaged => typeof(T) == Type || Type == typeof(Vector3) && typeof(T) == typeof(Color) || Type == typeof(Vector4) && (typeof(T) == typeof(Color) || typeof(T) == typeof(Rect2));
    internal bool SameType(ShaderUniform other) => Type == other.Type && Unsigned == other.Unsigned && ArrayLength == other.ArrayLength && BooleanWidth == other.BooleanWidth && MatrixColumns == other.MatrixColumns && MatrixRows == other.MatrixRows;

    internal PropertyDescriptor Describe(string propertyName) => MatrixColumns != 0 ? new PropertyDescriptor<ShaderMaterial, float[]>(propertyName,
        m => m.GetShaderMatrixParameter(Name), (m, v) => { ArgumentNullException.ThrowIfNull(v); m.SetShaderMatrixParameter(Name, v); }, _ => MatrixDefaults(), stored: true)
    { Hint = Hint, HintString = HintString, StoreCondition = m => m.HasStoredUniform(Name) } : SourceColor ? Describe<Color>(propertyName) : Type == typeof(float) ? Describe<float>(propertyName) :
        Type == typeof(bool) ? Describe<bool>(propertyName) : Type == typeof(int) ? Describe<int>(propertyName) : Type == typeof(uint) ? Describe<uint>(propertyName) :
        Type == typeof(Vector2) ? Describe<Vector2>(propertyName) : Type == typeof(Vector3) ? Describe<Vector3>(propertyName) : Type == typeof(Vector4) ? Describe<Vector4>(propertyName) :
        Type == typeof(Transform) ? Describe<Transform>(propertyName) : Type == typeof(Vector2i) ? Describe<Vector2i>(propertyName) : Type == typeof(Vector3i) ? Describe<Vector3i>(propertyName) : Describe<Vector4i>(propertyName);

    internal PropertyDescriptor DescribeInstance(string name) => SourceColor ? InstanceDescriptor<Color>(name) : Type == typeof(float) ? InstanceDescriptor<float>(name) : Type == typeof(bool) ? InstanceDescriptor<bool>(name) : Type == typeof(int) ? InstanceDescriptor<int>(name) : Type == typeof(uint) ? InstanceDescriptor<uint>(name) : Type == typeof(Vector2) ? InstanceDescriptor<Vector2>(name) : Type == typeof(Vector3) ? InstanceDescriptor<Vector3>(name) : Type == typeof(Vector4) ? InstanceDescriptor<Vector4>(name) : Type == typeof(Vector2i) ? InstanceDescriptor<Vector2i>(name) : Type == typeof(Vector3i) ? InstanceDescriptor<Vector3i>(name) : InstanceDescriptor<Vector4i>(name);
    private PropertyDescriptor InstanceDescriptor<T>(string propertyName) where T : unmanaged => new PropertyDescriptor<CanvasItem, T?>(propertyName, item => item.GetStoredInstanceParameter<T>(Name), (item, value) => item.SetInstanceShaderParameter<T>(Name, value), _ => null, stored: true) { Hint = Hint, HintString = HintString, StoredSetter = (item, value) => item.RestoreInstanceParameter<T>(Name, value) };

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

    internal void WriteMatrix(Span<byte> target, ReadOnlySpan<float> values)
    {
        var rows = MatrixRows;
        var stored = MemoryMarshal.Cast<byte, float>(target);
        for (var column = 0; column < MatrixColumns; column++)
            for (var row = 0; row < rows; row++)
                stored[(RowMajor ? row : column) * MatrixStride / 4 + (RowMajor ? column : row)] = values[column * rows + row];
    }

    internal void ReadMatrix(ReadOnlySpan<byte> source, Span<float> values)
    {
        var stored = MemoryMarshal.Cast<byte, float>(source);
        for (var column = 0; column < MatrixColumns; column++)
            for (var row = 0; row < MatrixRows; row++)
                values[column * MatrixRows + row] = stored[(RowMajor ? row : column) * MatrixStride / 4 + (RowMajor ? column : row)];
    }

    private float[] MatrixDefaults()
    {
        var values = new float[Count * MatrixComponents];
        Span<byte> storage = stackalloc byte[ElementSize];
        for (var i = 0; i < Count; i++)
        {
            storage.Clear(); WriteDefaultElement(storage, i);
            ReadMatrix(storage, values.AsSpan(i * MatrixComponents, MatrixComponents));
        }
        return values;
    }

    internal void CopyValue(ReadOnlySpan<byte> source, ShaderUniform destination, Span<byte> target)
    {
        if (Type == typeof(Transform)) destination.Write(target, Read<Transform>(source));
        else if (MatrixColumns != 0)
        {
            Span<float> values = stackalloc float[16];
            ReadMatrix(source, values);
            destination.WriteMatrix(target, values);
        }
        else source.CopyTo(target);
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
        var initial = ReadDefault<T>();
        return ArrayLength == 0
            ? new PropertyDescriptor<ShaderMaterial, T>(propertyName, m => m.GetShaderParameter<T>(Name), (m, v) => m.SetShaderParameter(Name, v), _ => initial, stored: true) { Hint = Hint, HintString = HintString, StoreCondition = m => m.HasStoredUniform(Name) }
            : new PropertyDescriptor<ShaderMaterial, T[]>(propertyName, m => m.GetShaderParameterArray<T>(Name),
                (m, v) => { ArgumentNullException.ThrowIfNull(v); m.SetShaderParameter<T>(Name, v.AsSpan()); }, _ => DefaultArray<T>(), stored: true)
            { Hint = Hint, HintString = HintString, StoreCondition = m => m.HasStoredUniform(Name) };
    }
}
