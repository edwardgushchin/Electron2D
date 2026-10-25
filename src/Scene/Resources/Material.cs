using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

/// <summary>A resource selecting how canvas geometry, writable texture pixels or particle data are shaded.</summary>
/// <remarks>Materials are borrowed by nodes; disposing a node
/// does not dispose shared materials. Three-dimensional render priority and next-pass chains are not supported.</remarks>
public abstract partial class Material : Resource
{
    private protected Material() { }
    internal abstract MaterialState? GetCanvasState();
    internal MaterialState? RequireCanvasState()
    {
        var state = GetCanvasState();
        if (state is not null && state.Program.Mode != Shader.Mode.CanvasItem || this is BlitMaterial) throw new NotSupportedException("Canvas drawing requires a canvas material program.");
        return state;
    }
    internal virtual BlendMode GetCanvasBlendMode() => BlendMode.Mix;
}

/// <summary>Applies a compiled shader to canvas commands, writable-texture blit passes or particle data.</summary>
/// <remarks>A null shader uses normal color drawing. An assigned shader requires the GPU backend, including
/// an identity shader. Shader resources are borrowed; resource duplication follows the requested graph policy.
/// Reserved TIME and SCREEN_PIXEL_SIZE uniforms and SCREEN_TEXTURE sampling are filled by the renderer
/// and excluded from parameter access, stored properties and value migration. Scene lights compose the authored fragment color before blend while preserving discard and parameters.
/// Normal/specular channels and point masks share the native sixteen-sampler stage capacity; overflow rejects
/// before drawing. All four fragment uniform-buffer slots remain available; an authored vertex stage has three material slots after its renderer-owned Frame. Matching parameter names share typed values across both stages. Particle shaders retain the same typed material schema,
/// with three user buffers after ParticleFrame; they reject canvas and writable-texture draw submission.
/// Explicit RenderingDevice execution binds its own frame, storage and parameter buffers.</remarks>
public sealed partial class ShaderMaterial : Material
{
    private readonly object _gate = new();
    private Shader? _shader;
    private MaterialState? _state;

    /// <summary>Creates a material using ordinary canvas colors.</summary>
    public ShaderMaterial() { }

    /// <summary>Gets or sets the borrowed program selecting canvas, writable-texture fragments or particle compute.</summary>
    /// <value>Null by default.</value>
    /// <exception cref="ObjectDisposedException">The material or supplied shader is disposed.</exception>
    public Shader? Shader
    {
        get { lock (_gate) { ThrowIfDisposed(); return _shader; } }
        set
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
                if (ReferenceEquals(_shader, value)) return;
                _shader = value;
                if (value is null) _state = null;
            }
            EmitChanged();
        }
    }

    /// <summary>Sets a typed scalar, vector or matrix material uniform.</summary>
    /// <typeparam name="T">Bool, float, int, uint, Vector2, Vector3, Vector4, Color, Rect2, Transform, Vector2i, Vector3i or Vector4i as required by the shader.</typeparam>
    /// <param name="name">The exact, case-sensitive uniform member name.</param>
    /// <param name="value">The new value. Color maps RGB to float3 or RGBA to float4 without color-space conversion.
    /// Rect2 maps position and size to float4. Integer vectors preserve component bits for signed or unsigned shader vectors.
    /// Logical bool uses bool; bool2/3/4 use an int mask whose low bits select true components. Higher bits are ignored.
    /// Transform supplies its X/Y basis to float2x2; Origin is not stored.</param>
    /// <remarks>Matrices start at identity; other stored components start at zero. Updates affect every node sharing the material, without QueueRedraw or
    /// a managed allocation after initialization. Floating-point values, including unused Color alpha and Transform Origin, must be finite. Changed is emitted after
    /// mutation. Shader replacement retains values whose names, logical element types (including boolean width) and array lengths still match.</remarks>
    /// <exception cref="ArgumentException">The name, element type or scalar/array shape does not match, or a value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public void SetShaderParameter<T>(string name, T value) where T : unmanaged
    {
        lock (_gate)
        {
            var state = RequireState();
            var uniform = Find<T>(state, name, array: false);
            ValidateValue(in value);
            state.UniformOverrides.Add(name);
            uniform.Write(state.Buffers[uniform.Buffer].AsSpan(uniform.Offset, uniform.ElementSize), in value);
        }
        EmitChanged();
    }

    /// <summary>Replaces every element of a fixed-size array uniform.</summary>
    /// <typeparam name="T">The supported scalar, vector or matrix type matching the reflected element type.</typeparam>
    /// <param name="name">The exact, case-sensitive array member name.</param>
    /// <param name="values">Values copied into the material, with exactly the reflected array length. Boolean vectors use one int mask per vector.</param>
    /// <remarks>Validation completes before mutation; a failure preserves all prior elements. The renderer handles
    /// reflected padding. A successful replacement emits Changed.</remarks>
    /// <exception cref="ArgumentException">The name, element type, array length or a floating-point value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public void SetShaderParameter<T>(string name, ReadOnlySpan<T> values) where T : unmanaged
    {
        lock (_gate)
        {
            var state = RequireState();
            var uniform = Find<T>(state, name, array: true);
            if (values.Length != uniform.ArrayLength) throw new ArgumentException($"Uniform '{name}' requires {uniform.ArrayLength} elements.", nameof(values));
            foreach (ref readonly var value in values) ValidateValue(in value);
            state.UniformOverrides.Add(name);
            for (var i = 0; i < values.Length; i++)
                uniform.Write(state.Buffers[uniform.Buffer].AsSpan(uniform.Offset + i * uniform.Stride, uniform.ElementSize), in values[i]);
        }
        EmitChanged();
    }

    /// <summary>Reads a typed scalar, vector or matrix material uniform.</summary>
    /// <typeparam name="T">The supported type matching the reflected value; Color aliases float3 and float4, and Rect2 aliases float4.</typeparam>
    /// <param name="name">The exact, case-sensitive uniform member name.</param>
    /// <returns>The current value; initially identity for matrices and zero for other stored components.
    /// Boolean vectors return only their represented low mask bits. Color mapped from float3 always has alpha one. Transform mapped from float2x2 always has zero Origin.</returns>
    /// <exception cref="ArgumentException">The name, type or scalar/array shape does not match.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public T GetShaderParameter<T>(string name) where T : unmanaged
    {
        lock (_gate)
        {
            var state = RequireState();
            var uniform = Find<T>(state, name, array: false);
            return uniform.Read<T>(state.Buffers[uniform.Buffer].AsSpan(uniform.Offset, uniform.ElementSize));
        }
    }

    /// <summary>Returns a copy of a fixed-size array uniform.</summary>
    /// <typeparam name="T">The supported type matching the reflected array element type.</typeparam>
    /// <param name="name">The exact, case-sensitive array member name.</param>
    /// <returns>An independent array with the reflected length; initially identity matrices or zero components.
    /// Boolean scalar arrays use bool elements; boolean vector arrays use int masks. Float3 Color elements always have alpha one; float2x2 Transform elements always have zero Origin.</returns>
    /// <exception cref="ArgumentException">The name, element type or scalar/array shape does not match.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public T[] GetShaderParameterArray<T>(string name) where T : unmanaged
    {
        lock (_gate)
        {
            var state = RequireState();
            var uniform = Find<T>(state, name, array: true);
            var values = new T[uniform.ArrayLength];
            for (var i = 0; i < values.Length; i++)
                values[i] = uniform.Read<T>(state.Buffers[uniform.Buffer].AsSpan(uniform.Offset + i * uniform.Stride, uniform.ElementSize));
            return values;
        }
    }

    /// <summary>Copies a reflected fixed-size uniform array into caller-owned storage.</summary>
    /// <typeparam name="T">The unmanaged type matching the reflected array element.</typeparam>
    /// <param name="name">Exact case-sensitive array parameter name.</param>
    /// <param name="destination">Storage with exactly the reflected array length.</param>
    /// <remarks>Initializes/migrates state by the same policy as other typed getters; prepared copies allocate no managed memory.</remarks>
    /// <exception cref="ArgumentException">Name, type, array shape or destination length is invalid.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public void CopyShaderParameterArray<T>(string name, Span<T> destination) where T : unmanaged
    {
        lock (_gate)
        {
            var state = RequireState(); var uniform = Find<T>(state, name, array: true);
            if (destination.Length != uniform.ArrayLength) throw new ArgumentException("Destination length must match the reflected array.", nameof(destination));
            for (var i = 0; i < destination.Length; i++) destination[i] = uniform.Read<T>(state.Buffers[uniform.Buffer].AsSpan(uniform.Offset + i * uniform.Stride, uniform.ElementSize));
        }
    }

    private static ShaderUniform Find<T>(MaterialState state, string name, bool array) where T : unmanaged
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!state.Program.Uniforms.TryGetValue(name, out var uniform) || !uniform.Accepts<T>() || (uniform.ArrayLength != 0) != array)
            throw new ArgumentException($"Uniform '{name}' does not match the requested {typeof(T).Name} {(array ? "array" : "value")}.", nameof(name));
        return uniform;
    }

    /// <summary>Sets a borrowed ordinary image override, or clears a named parameter override.</summary>
    /// <param name="name">The exact, case-sensitive material parameter name.</param>
    /// <param name="texture">A live ordinary scalar texture, or null to reset a numeric, matrix, array or image parameter to its shader default.</param>
    /// <remarks>An unbound parameter without a Shader default fails explicitly before drawing. Textures are sampled
    /// with linear float or nearest integer filtering, clamped coordinates and LOD restricted to the base level, independently of canvas sampling
    /// properties. A numeric reset removes explicit ownership, so later compatible reloads follow new shader defaults.
    /// An image-array reset clears every element. Changes emit Changed.</remarks>
    /// <exception cref="ArgumentException">The parameter is absent, or a nonnull image does not match an ordinary scalar sampled descriptor.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned, or texture validation reenters mutation or replaces the program.</exception>
    /// <exception cref="ObjectDisposedException">The material, shader or supplied texture is disposed.</exception>
    public void SetShaderParameter(string name, Texture? texture) => SetSampledResource(name, texture, false);

    /// <summary>Gets the explicit ordinary image override for a scalar sampled descriptor.</summary>
    /// <param name="name">The exact, case-sensitive texture parameter name.</param>
    /// <returns>The borrowed texture override, or null when the Shader default will be used.</returns>
    /// <exception cref="ArgumentException">The parameter is not an ordinary scalar sampled descriptor or the supplied sampled scalar type differs.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public Texture? GetShaderParameter(string name)
    {
        lock (_gate) { var state = RequireState(); var slot = state.Program.FindTexture(name); state.Program.Textures[slot].RequireShape(false); state.Program.Textures[slot].RequireBindingArray(false); return (Texture?)state.Textures[slot]; }
    }

    /// <summary>Sets a borrowed layered image override for a scalar sampled descriptor.</summary>
    /// <param name="name">The exact sampler parameter name.</param>
    /// <param name="texture">A live layered resource, or null to clear any named material parameter override.</param>
    /// <remarks>Sampling uses linear float or nearest integer filtering, clamp addressing and base-level LOD. Changes need no geometry
    /// rerecording. An unbound parameter fails before drawing. Ownership remains with the caller.</remarks>
    /// <exception cref="ArgumentException">The parameter is not a layered scalar sampled descriptor or the supplied sampled scalar type differs.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned, or texture validation reenters mutation or replaces the program.</exception>
    /// <exception cref="ObjectDisposedException">The material, shader or array is disposed.</exception>
    public void SetShaderLayeredParameter(string name, TextureLayered? texture) => SetSampledResource(name, texture, true);

    /// <summary>Gets the explicit layered image override for a scalar sampled descriptor.</summary>
    /// <param name="name">The exact sampler parameter name.</param>
    /// <returns>The borrowed override, or null when the shader default is used.</returns>
    /// <exception cref="ArgumentException">The parameter is not a layered scalar sampled descriptor or the supplied sampled scalar type differs.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public TextureLayered? GetShaderLayeredParameter(string name)
    {
        lock (_gate) { var state = RequireState(); var slot = state.Program.FindTexture(name); state.Program.Textures[slot].RequireShape(true); state.Program.Textures[slot].RequireBindingArray(false); return (TextureLayered?)state.Textures[slot]; }
    }
    internal Resource? GetSampledResource(string name)
    { lock (_gate) { var state = RequireState(); var slot = state.Program.FindTexture(name); state.Program.Textures[slot].RequireBindingArray(false); return state.Textures[slot]; } }
    internal void SetSampledResource(string name, Resource? texture, bool? layered = null)
    {
        if (texture is null) { ResetParameter(name); return; }
        lock (_gate)
        {
            var state = RequireState(); var slot = state.Program.FindTexture(name); var descriptor = state.Program.Textures[slot]; descriptor.RequireBindingArray(false);
            if (layered.HasValue) descriptor.RequireShape(layered.Value);
            if (_validatingTextures) throw new InvalidOperationException("Texture validation cannot reenter a texture update.");
            _validatingTextures = true;
            try
            {
                descriptor.Validate(texture);
                if (!ReferenceEquals(state, RequireState())) throw new InvalidOperationException("Texture validation replaced the material program.");
                if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
                state.Textures[slot] = texture;
            }
            finally { _validatingTextures = false; }
        }
        EmitChanged();
    }

    internal static void ValidateValue<T>(in T value) where T : unmanaged
    {
        if (typeof(T) != typeof(float) && typeof(T) != typeof(Vector2) && typeof(T) != typeof(Vector3) && typeof(T) != typeof(Vector4) && typeof(T) != typeof(Color) && typeof(T) != typeof(Rect2) && typeof(T) != typeof(Transform)) return;
        foreach (var component in MemoryMarshal.Cast<T, float>(MemoryMarshal.CreateReadOnlySpan(in value, 1)))
            if (!float.IsFinite(component)) throw new ArgumentException("Material floating-point values must be finite.", nameof(value));
    }

    internal bool HasStoredUniform(string name) { lock (_gate) return RequireState().UniformOverrides.Contains(name); }

    private MaterialState RequireState() => GetCanvasState() ?? throw new InvalidOperationException("Shader parameters require an assigned shader.");

    internal override MaterialState? GetCanvasState()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_shader is null) return null;
            var program = _shader.GetProgram();
            if (!ReferenceEquals(_state?.Program, program) || !ReferenceEquals(_state?.Shader, _shader)) _state = new MaterialState(_gate, program, _state, _shader);
            return _state;
        }
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ShaderMaterial();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        Shader? shader;
        MaterialState? state;
        lock (_gate)
        {
            shader = Shader;
            var current = GetCanvasState();
            state = current is null ? null : new MaterialState(new object(), current.Program, current, current.Shader);
        }
        var copy = (ShaderMaterial)target;
        var copiedShader = deep ? (Shader?)duplicateSubresource(shader) : shader;
        if (deep && state is not null)
            for (var i = 0; i < state.Textures.Length; i++) state.Textures[i] = duplicateSubresource(state.Textures[i]);
        lock (copy._gate)
        {
            copy._shader = copiedShader;
            copy._state = state is null ? null : new MaterialState(copy._gate, state.Program, state, copiedShader);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<ShaderMaterial, Shader?>(nameof(Shader), m => m.Shader, (m, v) => m.Shader = v, _ => null, stored: true)])
        .Concat(Shader?.GetProgram().MaterialDescriptors ?? []);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) { _shader = null; _state = null; }
        base.Dispose(disposing);
    }
}

internal sealed class MaterialState
{
    private readonly object _gate;
    internal readonly ShaderProgram Program;
    internal readonly HashSet<string> UniformOverrides = new(StringComparer.Ordinal);
    internal readonly byte[][] Buffers;
    internal readonly Resource?[] Textures;
    internal readonly SamplerConfiguration?[] Samplers;
    internal readonly Shader? Shader;
    internal long ParticleConfigurationVersion;
    internal MaterialState? ParticleMotion;

    internal MaterialState(object gate, ShaderProgram program, MaterialState? previous, Shader? shader = null)
    {
        _gate = gate;
        Program = program;
        Shader = shader;
        Buffers = program.BufferSizes.Select(size => new byte[size]).ToArray();
        Textures = new Resource?[program.Textures.Length];
        Samplers = new SamplerConfiguration?[program.Textures.Length];
        foreach (var uniform in program.Uniforms.Values)
            uniform.InitializeDefault(Buffers[uniform.Buffer].AsSpan(uniform.Offset, uniform.StorageSize));
        foreach (var uniform in program.InstanceUniforms.Values)
            uniform.InitializeDefault(Buffers[uniform.Buffer].AsSpan(uniform.Offset, uniform.StorageSize));
        if (previous is null) return;
        foreach (var uniform in program.Uniforms.Values)
            if (previous.UniformOverrides.Contains(uniform.Name) && previous.Program.Uniforms.TryGetValue(uniform.Name, out var old) && uniform.SameType(old))
            {
                UniformOverrides.Add(uniform.Name);
                for (var i = 0; i < uniform.Count; i++)
                {
                    var source = previous.Buffers[old.Buffer].AsSpan(old.Offset + i * old.Stride, old.ElementSize);
                    var target = Buffers[uniform.Buffer].AsSpan(uniform.Offset + i * uniform.Stride, uniform.ElementSize);
                    old.CopyValue(source, uniform, target);
                }
            }
        for (var i = 0; i < Textures.Length; i++)
            for (var j = 0; j < previous.Textures.Length; j++)
                if (program.Textures[i].IsMaterialTexture && previous.Program.Textures[j].IsMaterialTexture && program.Textures[i].Name == previous.Program.Textures[j].Name && program.Textures[i].Element == previous.Program.Textures[j].Element && program.Textures[i].SameType(previous.Program.Textures[j])) Textures[i] = previous.Textures[j];
    }

    internal void CopyTextures(Span<Resource?> target)
    {
        lock (_gate)
        {
            RenderingServer.ActiveGlobalParameters?.Validate(Program);
            CaptureSamplerStates();
            for (var i = 0; i < Textures.Length; i++)
            {
                var texture = Program.Textures[i];
                target[i] = texture.IsEngineTexture ? null : texture.IsGlobal ? (RenderingServer.ActiveGlobalParameters ?? throw new InvalidOperationException("Global images require an active renderer.")).Resolve(texture) : Textures[i] ?? Shader?.DefaultTexture(texture.Name, texture.Element);
                if (target[i] is null && !texture.IsEngineTexture && texture.ImagePolicy is null)
                    throw new InvalidOperationException($"Texture parameter '{texture.Name}' has no texture or Shader default.");
                texture.Validate(target[i]);
            }
        }
    }

    internal void CaptureSamplerStates()
    {
        if (Shader is null) Array.Clear(Samplers); else Shader.CopySamplerStates(Program, Samplers);
    }

    internal void SynchronizeSharedUniforms(byte[]?[]? instanceBuffers = null)
    {
        foreach (var copy in Program.UniformCopies)
        {
            var source = instanceBuffers is not null && instanceBuffers[copy.Source.Buffer] is { } ownSource ? ownSource : Buffers[copy.Source.Buffer];
            var target = instanceBuffers is not null && instanceBuffers[copy.Target.Buffer] is { } ownTarget ? ownTarget : Buffers[copy.Target.Buffer];
            for (var i = 0; i < copy.Source.Count; i++)
            {
                var from = source.AsSpan(copy.Source.Offset + i * copy.Source.Stride, copy.Source.ElementSize);
                var to = target.AsSpan(copy.Target.Offset + i * copy.Target.Stride, copy.Target.ElementSize);
                copy.Source.CopyValue(from, copy.Target, to);
            }
        }
    }

    internal void PushUniforms(nint command, float time, Vector2 screenPixelSize, Vector4 sdfCoords = default, CanvasItem? instance = null, bool compute = false, Vector4 normalMapFlags = default, Vector2 texturePixelSize = default, CanvasMatrices? matrices = null)
    {
        lock (_gate)
        {
            if (Program.TimeUniform is { } clock)
                MemoryMarshal.Write(Buffers[clock.Buffer].AsSpan(clock.Offset, sizeof(float)), in time);
            if (Program.ScreenPixelSizeUniform is { } pixels)
                MemoryMarshal.Write(Buffers[pixels.Buffer].AsSpan(pixels.Offset, sizeof(float) * 2), in screenPixelSize);
            if (Program.SDFCoordsUniform is { } sdf)
                MemoryMarshal.Write(Buffers[sdf.Buffer].AsSpan(sdf.Offset, sizeof(float) * 4), in sdfCoords);
            if (Program.NormalMapFlagsUniform is { } normal)
                MemoryMarshal.Write(Buffers[normal.Buffer].AsSpan(normal.Offset, sizeof(float) * 4), in normalMapFlags);
            if (Program.NormalModelBasisUniform is { } normalModel)
            {
                var basis = CanvasMatrices.NormalBasis(matrices?.NormalModel ?? Transform.Identity);
                MemoryMarshal.Write(Buffers[normalModel.Buffer].AsSpan(normalModel.Offset, sizeof(float) * 4), in basis);
            }
            if (Program.NormalCanvasBasisUniform is { } normalCanvas)
            {
                var basis = CanvasMatrices.NormalBasis(matrices?.Canvas ?? Transform.Identity);
                MemoryMarshal.Write(Buffers[normalCanvas.Buffer].AsSpan(normalCanvas.Offset, sizeof(float) * 4), in basis);
            }
            if (Program.TexturePixelSizeUniform is { } texturePixels)
                MemoryMarshal.Write(Buffers[texturePixels.Buffer].AsSpan(texturePixels.Offset, sizeof(float) * 2), in texturePixelSize);
            if (Program.ModelMatrixUniform is { } model) CanvasMatrices.Write(model, Buffers, matrices?.Model ?? Transform.Identity);
            if (Program.CanvasMatrixUniform is { } canvas) CanvasMatrices.Write(canvas, Buffers, matrices?.Canvas ?? Transform.Identity);
            if (Program.ScreenMatrixUniform is { } screen) CanvasMatrices.Write(screen, Buffers,
                new(new(2 * screenPixelSize.X, 0), new(0, -2 * screenPixelSize.Y), new(-1, 1)));
            RenderingServer.ActiveGlobalParameters?.Write(Program, Buffers);
            var instanceBuffer = Program.HasInstanceUniforms ? instance?.InstanceUniforms.Prepare(Program) : null;
            SynchronizeSharedUniforms(instanceBuffer);
            for (var i = 0; i < Buffers.Length; i++)
            {
                var bytes = instanceBuffer is not null && instanceBuffer[i] is { } own ? own : Buffers[i];
                if (compute) { if (i != 0) SDL.PushGPUComputeUniformData(command, (uint)i, bytes.AsSpan(), (uint)bytes.Length); }
                else if (i < Program.FragmentBufferCount) SDL.PushGPUFragmentUniformData(command, (uint)i, bytes.AsSpan(), (uint)bytes.Length);
                else SDL.PushGPUVertexUniformData(command, (uint)(i - Program.FragmentBufferCount + 1), bytes.AsSpan(), (uint)bytes.Length);
            }
        }
    }
}
