namespace Electron2D;

/// <summary>A canvas fragment program loaded from compiled SPIR-V bytecode.</summary>
/// <remarks>HLSL and GLSL source is compiled during import or build. Runtime loading checks module structure and
/// reflected interfaces without compiling source; it does not perform full instruction-level semantic validation.
/// This interface accepts optional float4 color at location zero,
/// float2 UV at location one, raw float4 instance data at location two, framebuffer position, and one float4 color output. TEXTURE at set two, binding zero
/// samples the current canvas command, or opaque white for untextured geometry; it is not a material parameter.
/// SCREEN_TEXTURE at any contiguous sampled binding consumes the current viewport screen snapshot;
/// an optional float2 SCREEN_PIXEL_SIZE uniform supplies inverse native target dimensions. Both are renderer-owned
/// and excluded from material/default parameter access. The first ordinary screen-reading draw snapshots the canvas
/// unless BackBufferCopy already supplied a region; a group material reads its completed transparent group image.
/// An optional non-array float32 uniform named TIME receives render seconds, scaled by Engine.TimeScale and wrapped
/// by ProjectSettings.RenderingTimeRolloverSeconds. It continues during scene pause and is not a material parameter.
/// Logical boolean types are retained by validated metadata in the imported SPIR-V payload. External modules may
/// supply the same metadata; unannotated unsigned storage retains its numeric type.
/// Material uniforms use validated std140 buffers at descriptor
/// set three and sampled 2D or image-array textures at set two. Named textures use linear/base-level/clamp sampling, independently
/// of canvas properties. Sampler configuration, matrices other than float2x2, nested uniform structs and user vertex programs remain pending.</remarks>
public sealed partial class Shader : Resource
{
    /// <summary>Identifies the supported two-dimensional shader domain.</summary>
    public enum Mode
    {
        /// <summary>A shader applied to canvas geometry.</summary>
        CanvasItem = 1,
    }

    private readonly object _codeGate = new();
    private ShaderProgram _program = ShaderProgram.Default;
    private readonly Dictionary<string, Resource> _defaultTextures = new(StringComparer.Ordinal);

    /// <summary>Creates a canvas shader multiplying command texture samples by drawing color.</summary>
    public Shader() { }

    /// <summary>Gets this shader's canvas domain.</summary>
    /// <returns>Mode.CanvasItem.</returns>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public Mode GetMode() { ThrowIfDisposed(); return Mode.CanvasItem; }

    /// <summary>Creates a canvas fragment shader from a copied SPIR-V module.</summary>
    /// <param name="bytecode">A little-endian SPIR-V module, at most 16 MiB, with one fragment entry point named main.</param>
    /// <returns>A shader owning an immutable copy of the validated bytecode.</returns>
    /// <remarks>No active renderer is required. The selected platform package supplies native reflection and shader toolchain libraries.</remarks>
    /// <exception cref="ArgumentException">The bytecode is malformed or reflection fails.</exception>
    /// <exception cref="NotSupportedException">The module uses unsupported stages, capabilities or interfaces.</exception>
    public static Shader CreateFromSPIRV(ReadOnlySpan<byte> bytecode)
    {
        return new Shader { _program = ShaderCompiler.ValidateFragmentInterface(bytecode) };
    }

    /// <summary>Replaces the program after copying the input and checking its structure and reflected interface.</summary>
    /// <param name="bytecode">Compiled SPIR-V with the supported canvas fragment interface.</param>
    /// <remarks>A failure preserves the prior program. A successful replacement emits Changed. Native pipelines
    /// are rebuilt by the renderer on demand; the resource itself owns no device handles. Native reflection
    /// resolves the selected platform package independently of renderer startup.</remarks>
    /// <exception cref="ArgumentException">The bytecode is malformed or reflection fails.</exception>
    /// <exception cref="NotSupportedException">The module uses unsupported stages, capabilities or interfaces.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public void SetSPIRV(ReadOnlySpan<byte> bytecode)
    {
        ThrowIfDisposed();
        var program = ShaderCompiler.ValidateFragmentInterface(bytecode);
        lock (_codeGate)
        {
            ThrowIfDisposed(); _program = program;
            foreach (var entry in _defaultTextures)
                if (!program.Textures.Any(t => !t.IsEngineTexture && t.Name == entry.Key && t.IsArray == (entry.Value is TextureLayered))) _defaultTextures.Remove(entry.Key);
        }
        EmitChanged();
    }

    /// <summary>Returns an independent copy of this program's compiled SPIR-V bytecode.</summary>
    /// <returns>The compiled shader payload, suitable for later binary loading.</returns>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public byte[] GetSPIRV() => (byte[])GetProgram().Code.Clone();

    /// <summary>Returns typed material property descriptors for this program's reflected uniforms.</summary>
    /// <returns>An immutable list with case-sensitive member names. Float4 values use Vector4 descriptors;
    /// material access also accepts Color and Rect2. Float2x2 uses Transform with an identity revert value. Float3 uses Vector3; material access also accepts Color with alpha one on read. Signed/unsigned integer vectors
    /// use Vector2i, Vector3i or Vector4i with preserved component bits. Logical bool uses bool; boolean vectors use int component masks. Fixed arrays use array-valued descriptors; sampled images use Texture or TextureLayered descriptors.
    /// The built-in command TEXTURE and render TIME are omitted.</returns>
    /// <remarks>Descriptors access ShaderMaterial values through its typed parameter methods. Buffer padding and
    /// resource handles remain internal. The returned list describes this program version and does not change after reload.</remarks>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public IReadOnlyList<PropertyDescriptor> GetShaderUniformList() => GetProgram().Descriptors;

    /// <summary>Sets the borrowed default texture for a reflected sampled 2D parameter.</summary>
    /// <param name="name">The exact texture parameter name.</param>
    /// <param name="texture">The default texture, or null to clear it.</param>
    /// <param name="index">Zero; arrays of texture bindings are not integrated.</param>
    /// <remarks>A material override takes precedence. Changes emit Changed; texture ownership remains with the caller.</remarks>
    /// <exception cref="ArgumentException">The parameter is not a sampled texture.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is not zero.</exception>
    /// <exception cref="ObjectDisposedException">The shader or supplied texture is disposed.</exception>
    public void SetDefaultTextureParameter(string name, Texture? texture, int index = 0)
    {
        lock (_codeGate)
        {
            ThrowIfDisposed();
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            _program.Textures[_program.FindTexture(name)].RequireShape(false);
            if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
            if (texture is null) _defaultTextures.Remove(name); else _defaultTextures[name] = texture;
        }
        EmitChanged();
    }

    /// <summary>Gets the borrowed default texture for a reflected sampled 2D parameter.</summary>
    /// <param name="name">The exact texture parameter name.</param>
    /// <param name="index">Zero; arrays of texture bindings are not integrated.</param>
    /// <returns>The assigned default, or null.</returns>
    /// <exception cref="ArgumentException">The parameter is not a sampled texture.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is not zero.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public Texture? GetDefaultTextureParameter(string name, int index = 0)
    {
        lock (_codeGate)
        {
            ThrowIfDisposed();
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            _program.Textures[_program.FindTexture(name)].RequireShape(false);
            return (Texture?)_defaultTextures.GetValueOrDefault(name);
        }
    }

    /// <summary>Sets a borrowed default resource for an image-array sampler.</summary>
    /// <param name="name">The exact reflected array sampler name.</param>
    /// <param name="texture">A live array resource, or null to clear its default.</param>
    /// <param name="index">Zero; descriptor binding arrays are not integrated.</param>
    /// <remarks>Material overrides take precedence. Ownership remains with the caller; Changed follows mutation.</remarks>
    /// <exception cref="ArgumentException">The parameter is not a layered sampler.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is not zero.</exception>
    /// <exception cref="ObjectDisposedException">The shader or resource is disposed.</exception>
    public void SetDefaultLayeredTextureParameter(string name, TextureLayered? texture, int index = 0)
    {
        lock (_codeGate)
        {
            ThrowIfDisposed(); if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            _program.Textures[_program.FindTexture(name)].RequireShape(true);
            if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
            if (texture is null) _defaultTextures.Remove(name); else _defaultTextures[name] = texture;
        }
        EmitChanged();
    }
    /// <summary>Gets the borrowed default image-array resource.</summary>
    /// <param name="name">The exact reflected array sampler name.</param>
    /// <param name="index">Zero; descriptor binding arrays are not integrated.</param>
    /// <returns>The assigned resource, or null.</returns>
    /// <exception cref="ArgumentException">The parameter is not a layered sampler.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is not zero.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public TextureLayered? GetDefaultLayeredTextureParameter(string name, int index = 0)
    {
        lock (_codeGate)
        {
            ThrowIfDisposed(); if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            _program.Textures[_program.FindTexture(name)].RequireShape(true);
            return (TextureLayered?)_defaultTextures.GetValueOrDefault(name);
        }
    }
    internal void SetDefaultSampledResource(string name, Resource? texture, int index)
    {
        var program = GetProgram(); var slot = program.FindTexture(name); program.Textures[slot].Validate(texture);
        if (program.Textures[slot].IsArray) SetDefaultLayeredTextureParameter(name, (TextureLayered?)texture, index);
        else SetDefaultTextureParameter(name, (Texture?)texture, index);
    }

    internal Resource? GetDefaultSampledResource(string name, int index)
    {
        lock (_codeGate) { ThrowIfDisposed(); if (index != 0) throw new ArgumentOutOfRangeException(nameof(index)); _program.FindTexture(name); return _defaultTextures.GetValueOrDefault(name); }
    }
    internal Resource? DefaultTexture(string name) { lock (_codeGate) { ThrowIfDisposed(); return _defaultTextures.GetValueOrDefault(name); } }

    internal ShaderProgram GetProgram()
    {
        lock (_codeGate) { ThrowIfDisposed(); return _program; }
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new Shader();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ShaderProgram program;
        KeyValuePair<string, Resource>[] defaults;
        lock (_codeGate) { program = GetProgram(); defaults = _defaultTextures.ToArray(); }
        if (deep)
            for (var i = 0; i < defaults.Length; i++) defaults[i] = new(defaults[i].Key, duplicateSubresource(defaults[i].Value)!);
        var copy = (Shader)target;
        lock (copy._codeGate)
        {
            copy._program = program; copy._defaultTextures.Clear();
            foreach (var pair in defaults) copy._defaultTextures.Add(pair.Key, pair.Value);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_codeGate) { ReleaseRenderingRID(); _program = ShaderProgram.Default; _defaultTextures.Clear(); }
        base.Dispose(disposing);
    }
}
