using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed class ShaderProgram(byte[] code, int[] bufferSizes, Dictionary<string, ShaderUniform> uniforms, ShaderTexture[]? textures = null, ShaderUniform? timeUniform = null, ShaderUniform? screenPixelSizeUniform = null, ShaderUniform? sdfCoordsUniform = null, Dictionary<string, ShaderUniform>? instanceUniforms = null)
{
    internal static readonly ShaderProgram Default = new(BuiltInShaders.Fragment, [], new(StringComparer.Ordinal), [new("TEXTURE", 0)]);
    internal readonly byte[] Code = code;
    internal readonly int[] BufferSizes = bufferSizes;
    internal readonly Dictionary<string, ShaderUniform> Uniforms = uniforms;
    internal readonly ShaderTexture[] Textures = textures ?? [];
    internal readonly Dictionary<string, ShaderUniform> InstanceUniforms = instanceUniforms ?? new(StringComparer.Ordinal);
    internal readonly bool HasInstanceUniforms = instanceUniforms is { Count: > 0 };
    internal readonly IReadOnlyList<PropertyDescriptor> InstanceDescriptors = Array.AsReadOnly((instanceUniforms ?? []).Values.Select(u => u.DescribeInstance(u.Name)).ToArray());
    internal readonly IReadOnlyList<PropertyDescriptor> CanvasInstanceDescriptors = Array.AsReadOnly((instanceUniforms ?? []).Values.Select(u => u.DescribeInstance("instance_shader_parameters/" + u.Name)).ToArray());
    internal readonly ShaderUniform? TimeUniform = timeUniform;
    internal readonly ShaderUniform? ScreenPixelSizeUniform = screenPixelSizeUniform;
    internal readonly ShaderUniform? SDFCoordsUniform = sdfCoordsUniform;
    internal readonly bool UsesSDFTexture = (textures ?? []).Any(t => t.IsSDFTexture);
    internal readonly bool UsesScreenTexture = (textures ?? []).Any(t => t.IsScreenTexture);
    internal readonly IReadOnlyList<PropertyDescriptor> Descriptors = Array.AsReadOnly(uniforms.Values.Select(u => u.Describe(u.Name))
        .Concat((textures ?? []).Where(t => !t.IsEngineTexture).Select(t => t.Describe(t.Name))).ToArray());
    internal readonly IReadOnlyList<PropertyDescriptor> MaterialDescriptors = Array.AsReadOnly(uniforms.Values.Select(u => u.Describe("shader_parameter/" + u.Name))
        .Concat((textures ?? []).Where(t => !t.IsEngineTexture).Select(t => t.Describe("shader_parameter/" + t.Name))).ToArray());

    internal int FindTexture(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        for (var i = 0; i < Textures.Length; i++) if (!Textures[i].IsEngineTexture && Textures[i].Name == name) return i;
        throw new ArgumentException($"Shader has no sampled texture named '{name}'.", nameof(name));
    }
}

internal sealed record ShaderTexture(string Name, int Binding, bool IsArray = false, int PixelType = 0)
{
    internal bool IsCanvasTexture => Name == "TEXTURE";
    internal bool IsScreenTexture => Name == "SCREEN_TEXTURE";
    internal bool IsSDFTexture => Name == "SDF_TEXTURE";
    internal bool IsEngineTexture => IsCanvasTexture || IsScreenTexture || IsSDFTexture;
    internal void RequireShape(bool array)
    {
        if (IsArray != array) throw new ArgumentException($"Texture parameter '{Name}' requires {(IsArray ? "layered" : "ordinary")} images.");
    }
    internal bool SameType(ShaderTexture other) => IsArray == other.IsArray && PixelType == other.PixelType;
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
        TextureRD { DeviceInfo: { } info } => RenderingDevice.PixelType(info.Format),
        TextureLayeredRD { DeviceInfo: { } info } => RenderingDevice.PixelType(info.Format),
        _ => 0
    };
    internal void Validate(Resource? texture)
    {
        if (texture is null) return;
        if (texture.IsDisposed) throw new ObjectDisposedException(nameof(texture));
        if (IsArray ? texture is not TextureLayered : texture is not Texture) throw new ArgumentException($"Texture parameter '{Name}' has a different sampled image shape.");
        if (ResourcePixelType(texture) != PixelType) throw new ArgumentException($"Texture parameter '{Name}' has a different sampled scalar type.");
    }
    internal PropertyDescriptor DescribeDefault() => IsArray
        ? new PropertyDescriptor<Shader, TextureLayered?>("default_texture_parameter/" + Name, shader => shader.GetDefaultLayeredTextureParameter(Name), (shader, value) => shader.SetDefaultLayeredTextureParameter(Name, value), _ => null, stored: true)
        : new PropertyDescriptor<Shader, Texture?>("default_texture_parameter/" + Name, shader => shader.GetDefaultTextureParameter(Name), (shader, value) => shader.SetDefaultTextureParameter(Name, value), _ => null, stored: true);
    internal PropertyDescriptor Describe(string propertyName) => IsArray
        ? new PropertyDescriptor<ShaderMaterial, TextureLayered?>(propertyName, m => m.GetShaderLayeredParameter(Name), (m, t) => m.SetShaderLayeredParameter(Name, t), _ => null, stored: true)
        : new PropertyDescriptor<ShaderMaterial, Texture?>(propertyName,
        m => m.GetShaderParameter(Name), (m, t) => m.SetShaderParameter(Name, t), _ => null, stored: true);
}

internal sealed record ShaderUniform(string Name, Type Type, int Buffer, int Offset, int ElementSize, int ArrayLength, int Stride, bool Unsigned, int MatrixStride, bool RowMajor, int BooleanWidth)
{
    internal int Count => Math.Max(1, ArrayLength);
    internal bool Accepts<T>() where T : unmanaged => typeof(T) == Type || Type == typeof(Vector3) && typeof(T) == typeof(Color) || Type == typeof(Vector4) && (typeof(T) == typeof(Color) || typeof(T) == typeof(Rect2));
    internal bool SameType(ShaderUniform other) => Type == other.Type && Unsigned == other.Unsigned && ArrayLength == other.ArrayLength && BooleanWidth == other.BooleanWidth;

    internal PropertyDescriptor Describe(string propertyName) => Type == typeof(float) ? Describe<float>(propertyName) :
        Type == typeof(bool) ? Describe<bool>(propertyName) : Type == typeof(int) ? Describe<int>(propertyName) : Type == typeof(uint) ? Describe<uint>(propertyName) :
        Type == typeof(Vector2) ? Describe<Vector2>(propertyName) : Type == typeof(Vector3) ? Describe<Vector3>(propertyName) : Type == typeof(Vector4) ? Describe<Vector4>(propertyName) :
        Type == typeof(Transform) ? Describe<Transform>(propertyName) : Type == typeof(Vector2i) ? Describe<Vector2i>(propertyName) : Type == typeof(Vector3i) ? Describe<Vector3i>(propertyName) : Describe<Vector4i>(propertyName);

    internal PropertyDescriptor DescribeInstance(string name) => Type == typeof(float) ? InstanceDescriptor<float>(name) : Type == typeof(bool) ? InstanceDescriptor<bool>(name) : Type == typeof(int) ? InstanceDescriptor<int>(name) : Type == typeof(uint) ? InstanceDescriptor<uint>(name) : Type == typeof(Vector2) ? InstanceDescriptor<Vector2>(name) : Type == typeof(Vector3) ? InstanceDescriptor<Vector3>(name) : Type == typeof(Vector4) ? InstanceDescriptor<Vector4>(name) : Type == typeof(Vector2i) ? InstanceDescriptor<Vector2i>(name) : Type == typeof(Vector3i) ? InstanceDescriptor<Vector3i>(name) : InstanceDescriptor<Vector4i>(name);
    private PropertyDescriptor InstanceDescriptor<T>(string propertyName) where T : unmanaged => new PropertyDescriptor<CanvasItem, T?>(propertyName, item => item.GetStoredInstanceParameter<T>(Name), (item, value) => item.SetInstanceShaderParameter<T>(Name, value), _ => null, stored: true) { StoredSetter = (item, value) => item.RestoreInstanceParameter<T>(Name, value) };

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
