namespace Electron2D;

/// <summary>A canvas program loaded from compiled fragment SPIR-V with an optional authored vertex stage.</summary>
/// <remarks>HLSL and GLSL source is compiled during import or build. Runtime loading checks module structure and
/// reflected interfaces without compiling source; it does not perform full instruction-level semantic validation.
/// Paired vertex programs preserve original mesh surface indices through gl_VertexIndex or SV_VertexID,
/// including expanded primitives, LODs and instances. Other canvas geometry retains native stream indices.
/// This interface accepts optional float4 color at location zero,
/// float2 UV at location one, raw float4 instance data at location two, float2 secondary UV at location four, mesh-local float3 normal at five and float4 tangent at six, framebuffer position, and one float4 color output. TEXTURE at set two, binding zero
/// samples the current canvas command, or opaque white for untextured geometry; it is not a material parameter.
/// SCREEN_TEXTURE at any contiguous sampled binding consumes the current viewport screen snapshot;
/// an optional float2 SCREEN_PIXEL_SIZE uniform supplies inverse native target dimensions. Both are renderer-owned
/// and excluded from material/default parameter access. The first ordinary screen-reading draw snapshots the canvas
/// unless BackBufferCopy already supplied a region; a group material reads its completed transparent group image.
/// An optional non-array float32 uniform named TIME receives render seconds, scaled by Engine.TimeScale and wrapped
/// by ProjectSettings.RenderingTimeRolloverSeconds. It continues during scene pause and is not a material parameter.
/// Logical boolean types are retained by validated metadata in the imported SPIR-V payload. External modules may
/// supply the same metadata; unannotated unsigned storage retains its numeric type.
/// A stage uniform block named Instance (type.Instance in imported HLSL) owns at most sixteen scalar/vector
/// per-item fields. CanvasItem and RenderingServer instance access is independent of shared material values.
/// Instance arrays, matrices and renderer-owned fields are rejected; supported declaration defaults are zero.
/// Material uniforms use validated std140 buffers at descriptor
/// set three and sampled 2D or image-array textures at set two. Named textures use linear/base-level/clamp sampling, independently
/// of canvas properties. The paired-stage API adds vertex Frame at set one/binding zero, material blocks at set one/bindings one through three and sampled textures at set zero. Shared names use one typed parameter namespace across stages. Sampler configuration, matrices other than float2x2 and nested uniform structs remain pending.</remarks>
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
    private readonly Dictionary<(string Name, int Index), Resource> _defaultTextures = [];
    private bool _validatingDefaultTextures;
    private Resource?[]? _defaultTextureScratch;

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
        var program = ShaderCompiler.ValidateFragmentInterface(bytecode);
        return new Shader { _program = program };
    }

    /// <summary>Replaces the program after copying the input and checking its structure and reflected interface.</summary>
    /// <param name="bytecode">Compiled SPIR-V with the supported canvas fragment interface.</param>
    /// <remarks>A failure preserves the prior program. Success selects the built-in vertex stage and emits Changed. Native pipelines
    /// are rebuilt by the renderer on demand; the resource itself owns no device handles. Native reflection
    /// resolves the selected platform package independently of renderer startup.</remarks>
    /// <exception cref="ArgumentException">The bytecode is malformed or reflection fails.</exception>
    /// <exception cref="NotSupportedException">The module uses unsupported stages, capabilities or interfaces.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public void SetSPIRV(ReadOnlySpan<byte> bytecode)
    {
        ThrowIfDisposed();
        var program = ShaderCompiler.ValidateFragmentInterface(bytecode);
        PublishProgram(program);
    }

    private void PublishProgram(ShaderProgram program)
    {
        lock (_codeGate)
        {
            ThrowIfDisposed(); var previous = _program; _program = program;
            foreach (var entry in _defaultTextures)
            {
                var old = previous.Textures.FirstOrDefault(t => t.Name == entry.Key.Name && t.Element == entry.Key.Index);
                if (old is null || !program.Textures.Any(t => t.IsMaterialTexture && t.Name == entry.Key.Name && t.Element == entry.Key.Index && t.SameType(old))) _defaultTextures.Remove(entry.Key);
            }
        }
        EmitChanged();
    }

    /// <summary>Returns an independent copy of this program's compiled SPIR-V bytecode.</summary>
    /// <returns>The copied fragment payload, suitable for fragment-only loading. Use the stage overload to export an authored vertex module.</returns>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public byte[] GetSPIRV() => (byte[])GetProgram().Code.Clone();

    /// <summary>Returns typed material property descriptors for this program's reflected uniforms.</summary>
    /// <returns>An immutable list with case-sensitive member names. Float4 values use Vector4 descriptors;
    /// material access also accepts Color and Rect2. Float2x2 uses Transform with an identity revert value. Float3 uses Vector3; material access also accepts Color with alpha one on read. Signed/unsigned integer vectors
    /// use Vector2i, Vector3i or Vector4i with preserved component bits. Logical bool uses bool; boolean vectors use int component masks. Fixed numeric and sampled descriptor arrays use array-valued descriptors; scalar sampled images use Texture or TextureLayered, matched by image role and float/signed/unsigned scalar type.
    /// The built-in command TEXTURE and render TIME are omitted.</returns>
    /// <remarks>Descriptors access ShaderMaterial values through its typed parameter methods. Buffer padding and
    /// resource handles remain internal. The returned list describes this program version and does not change after reload.</remarks>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public IReadOnlyList<PropertyDescriptor> GetShaderUniformList() => GetProgram().Descriptors;

    /// <summary>Sets the borrowed default texture for one reflected sampled 2D parameter element.</summary>
    /// <param name="name">The exact texture parameter name.</param>
    /// <param name="texture">The default texture, or null to clear it.</param>
    /// <param name="index">Zero for scalar bindings, otherwise the zero-based fixed descriptor array element.</param>
    /// <remarks>A material override takes precedence per element. Changes emit Changed after mutation; ownership remains with the caller.</remarks>
    /// <exception cref="ArgumentException">The parameter is not an ordinary sampled image or its sampled scalar type differs.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the reflected element count.</exception>
    /// <exception cref="InvalidOperationException">Texture validation replaces the program or reenters a default update.</exception>
    /// <exception cref="ObjectDisposedException">The shader or supplied texture is disposed.</exception>
    public void SetDefaultTextureParameter(string name, Texture? texture, int index = 0) => SetDefaultSampledResource(name, texture, index, false);

    /// <summary>Gets the borrowed default texture for one reflected sampled 2D parameter element.</summary>
    /// <param name="name">The exact texture parameter name.</param>
    /// <param name="index">Zero for scalar bindings, otherwise the zero-based fixed descriptor array element.</param>
    /// <returns>The assigned default, or null.</returns>
    /// <exception cref="ArgumentException">The parameter is not an ordinary sampled image.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the reflected element count.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public Texture? GetDefaultTextureParameter(string name, int index = 0) => (Texture?)GetDefaultSampledResource(name, index, false);

    /// <summary>Sets the borrowed default layered image for one reflected sampled parameter element.</summary>
    /// <param name="name">The exact parameter name.</param>
    /// <param name="texture">A live layered resource, or null to clear its default.</param>
    /// <param name="index">Zero for scalar bindings, otherwise the zero-based fixed descriptor array element.</param>
    /// <remarks>Material overrides take precedence per element. Changed follows mutation. Reload drops defaults whose descriptor shape, length or scalar type changes.</remarks>
    /// <exception cref="ArgumentException">The parameter requires a different image shape or sampled scalar type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the reflected element count.</exception>
    /// <exception cref="InvalidOperationException">Texture validation replaces the program or reenters a default update.</exception>
    /// <exception cref="ObjectDisposedException">The shader or supplied resource is disposed.</exception>
    public void SetDefaultLayeredTextureParameter(string name, TextureLayered? texture, int index = 0) => SetDefaultSampledResource(name, texture, index, true);

    /// <summary>Gets the borrowed default layered image for one reflected sampled parameter element.</summary>
    /// <param name="name">The exact parameter name.</param>
    /// <param name="index">Zero for scalar bindings, otherwise the zero-based fixed descriptor array element.</param>
    /// <returns>The assigned resource, or null.</returns>
    /// <exception cref="ArgumentException">The parameter is not a layered sampled image.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the reflected element count.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public TextureLayered? GetDefaultLayeredTextureParameter(string name, int index = 0) => (TextureLayered?)GetDefaultSampledResource(name, index, true);

    internal void SetDefaultSampledResource(string name, Resource? texture, int index, bool? layered = null)
    {
        lock (_codeGate)
        {
            ThrowIfDisposed(); var program = _program; var descriptor = program.Textures[program.FindTexture(name, index)];
            if (layered.HasValue) descriptor.RequireShape(layered.Value);
            if (_validatingDefaultTextures) throw new InvalidOperationException("Texture validation cannot reenter a default update.");
            _validatingDefaultTextures = true;
            try
            {
                descriptor.Validate(texture); ThrowIfDisposed();
                if (!ReferenceEquals(program, _program)) throw new InvalidOperationException("Texture validation replaced the shader program.");
                if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
                if (texture is null) _defaultTextures.Remove((name, index)); else _defaultTextures[(name, index)] = texture;
            }
            finally { _validatingDefaultTextures = false; }
        }
        EmitChanged();
    }
    internal Resource? GetDefaultSampledResource(string name, int index, bool? layered = null)
    {
        lock (_codeGate)
        {
            ThrowIfDisposed(); var descriptor = _program.Textures[_program.FindTexture(name, index)];
            if (layered.HasValue) descriptor.RequireShape(layered.Value);
            return _defaultTextures.GetValueOrDefault((name, index));
        }
    }
    internal T?[] GetDefaultSampledArray<T>(string name) where T : Resource
    {
        lock (_codeGate)
        {
            ThrowIfDisposed(); var descriptor = _program.Textures[_program.FindTexture(name)]; descriptor.RequireBindingArray(true); descriptor.RequireShape(typeof(T) == typeof(TextureLayered));
            var values = new T?[descriptor.Count]; for (var i = 0; i < values.Length; i++) values[i] = (T?)_defaultTextures.GetValueOrDefault((name, i)); return values;
        }
    }
    internal void SetDefaultSampledArray(string name, Resource?[] values, bool layered)
    {
        ArgumentNullException.ThrowIfNull(values);
        lock (_codeGate)
        {
            ThrowIfDisposed(); var program = _program; var descriptor = program.Textures[program.FindTexture(name)]; descriptor.RequireBindingArray(true);
            descriptor.RequireShape(layered);
            if (values.Length != descriptor.Count) throw new ArgumentException("Default texture array length must match the reflected descriptor array.", nameof(values));
            if (_validatingDefaultTextures) throw new InvalidOperationException("Texture validation cannot reenter a default update.");
            var scratch = _defaultTextureScratch ??= new Resource?[16]; _validatingDefaultTextures = true;
            try
            {
                for (var i = 0; i < values.Length; i++) scratch[i] = values[i];
                foreach (var texture in scratch.AsSpan(0, values.Length)) descriptor.Validate(texture);
                ThrowIfDisposed(); if (!ReferenceEquals(program, _program)) throw new InvalidOperationException("Texture validation replaced the shader program.");
                foreach (var texture in scratch.AsSpan(0, values.Length)) if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(values));
                for (var i = 0; i < values.Length; i++) if (scratch[i] is { } texture) _defaultTextures[(name, i)] = texture; else _defaultTextures.Remove((name, i));
            }
            finally { Array.Clear(scratch); _validatingDefaultTextures = false; }
        }
        EmitChanged();
    }
    internal Resource? DefaultTexture(string name, int index = 0) { lock (_codeGate) { ThrowIfDisposed(); return _defaultTextures.GetValueOrDefault((name, index)); } }

    internal ShaderProgram GetProgram()
    {
        lock (_codeGate) { ThrowIfDisposed(); return _program; }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<Shader, byte[]>("SPIRV", shader => shader.GetStoredSPIRV(), (shader, code) => shader.SetStoredSPIRV(code), stored: true)])
        .Concat(GetProgram().Textures.Where(texture => texture.IsMaterialTexture && texture.Element == 0).Select(texture => texture.DescribeDefault()));

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new Shader();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ShaderProgram program;
        KeyValuePair<(string Name, int Index), Resource>[] defaults;
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
    protected override void RemapResourceReferences(Func<Resource, Resource> remap)
    {
        base.RemapResourceReferences(remap);
        lock (_codeGate)
        {
            foreach (var name in _defaultTextures.Keys.ToArray())
            {
                var replacement = remap(_defaultTextures[name]);
                if (replacement is not Texture && replacement is not TextureLayered) throw new InvalidDataException("A shader default requires a sampled texture resource.");
                _defaultTextures[name] = replacement;
            }
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_codeGate) { ReleaseRenderingRID(); _program = ShaderProgram.Default; _defaultTextures.Clear(); }
        base.Dispose(disposing);
    }
}
