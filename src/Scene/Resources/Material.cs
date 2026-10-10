using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

/// <summary>A resource selecting how canvas geometry is shaded.</summary>
/// <remarks>Materials are borrowed by nodes; disposing a node
/// does not dispose shared materials. Three-dimensional render priority and next-pass chains are not supported.</remarks>
public abstract partial class Material : Resource
{
    private protected Material() { }
    internal abstract MaterialState? GetCanvasState();
    internal virtual BlendMode GetCanvasBlendMode() => BlendMode.Mix;
}

/// <summary>Applies a programmable fragment shader to a node's canvas commands.</summary>
/// <remarks>A null shader uses normal color drawing. An assigned shader requires the GPU backend, including
/// an identity shader. Shader resources are borrowed; resource duplication follows the requested graph policy.
/// Reserved TIME and SCREEN_PIXEL_SIZE uniforms and SCREEN_TEXTURE sampling are filled by the renderer
/// and excluded from parameter access, stored properties and value migration.</remarks>
public sealed class ShaderMaterial : Material
{
    private readonly object _gate = new();
    private Shader? _shader;
    private MaterialState? _state;

    /// <summary>Creates a material using ordinary canvas colors.</summary>
    public ShaderMaterial() { }

    /// <summary>Gets or sets the borrowed shader applied to canvas fragments.</summary>
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

    /// <summary>Sets a borrowed texture override for a sampled 2D shader parameter.</summary>
    /// <param name="name">The exact, case-sensitive texture parameter name.</param>
    /// <param name="texture">A live texture, or null to use the Shader default.</param>
    /// <remarks>An unbound parameter without a Shader default fails explicitly before drawing. Textures are sampled
    /// with linear filtering, clamped coordinates and LOD restricted to the base level, independently of canvas sampling
    /// properties. Configurable named sampler state remains pending. Changes emit Changed.</remarks>
    /// <exception cref="ArgumentException">The parameter is not a sampled texture.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material, shader or supplied texture is disposed.</exception>
    public void SetShaderParameter(string name, Texture? texture)
    {
        lock (_gate)
        {
            var state = RequireState();
            var slot = state.Program.FindTexture(name); state.Program.Textures[slot].RequireShape(false);
            if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
            state.Textures[slot] = texture;
        }
        EmitChanged();
    }

    /// <summary>Gets the explicitly assigned texture override for a sampled 2D shader parameter.</summary>
    /// <param name="name">The exact, case-sensitive texture parameter name.</param>
    /// <returns>The borrowed texture override, or null when the Shader default will be used.</returns>
    /// <exception cref="ArgumentException">The parameter is not a sampled texture.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public Texture? GetShaderParameter(string name)
    {
        lock (_gate) { var state = RequireState(); var slot = state.Program.FindTexture(name); state.Program.Textures[slot].RequireShape(false); return (Texture?)state.Textures[slot]; }
    }

    /// <summary>Sets a borrowed image-array override for a reflected array sampler.</summary>
    /// <param name="name">The exact sampler parameter name.</param>
    /// <param name="texture">A live array resource, or null to use the shader default.</param>
    /// <remarks>Sampling uses linear filtering, clamp addressing and base-level LOD. Changes need no geometry
    /// rerecording. An unbound parameter fails before drawing. Ownership remains with the caller.</remarks>
    /// <exception cref="ArgumentException">The parameter is not a layered sampler.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material, shader or array is disposed.</exception>
    public void SetShaderLayeredParameter(string name, TextureLayered? texture)
    {
        lock (_gate)
        {
            var state = RequireState(); var slot = state.Program.FindTexture(name); state.Program.Textures[slot].RequireShape(true);
            if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
            state.Textures[slot] = texture;
        }
        EmitChanged();
    }
    /// <summary>Gets the explicitly assigned image-array override.</summary>
    /// <param name="name">The exact sampler parameter name.</param>
    /// <returns>The borrowed override, or null when the shader default is used.</returns>
    /// <exception cref="ArgumentException">The parameter is not a layered sampler.</exception>
    /// <exception cref="InvalidOperationException">No shader is assigned.</exception>
    /// <exception cref="ObjectDisposedException">The material or shader is disposed.</exception>
    public TextureLayered? GetShaderLayeredParameter(string name)
    {
        lock (_gate) { var state = RequireState(); var slot = state.Program.FindTexture(name); state.Program.Textures[slot].RequireShape(true); return (TextureLayered?)state.Textures[slot]; }
    }
    internal Resource? GetSampledResource(string name)
    { lock (_gate) { var state = RequireState(); return state.Textures[state.Program.FindTexture(name)]; } }
    internal void SetSampledResource(string name, Resource? texture)
    {
        lock (_gate) { var state = RequireState(); var slot = state.Program.FindTexture(name); state.Program.Textures[slot].Validate(texture); state.Textures[slot] = texture; }
        EmitChanged();
    }

    private static void ValidateValue<T>(in T value) where T : unmanaged
    {
        if (typeof(T) != typeof(float) && typeof(T) != typeof(Vector2) && typeof(T) != typeof(Vector3) && typeof(T) != typeof(Vector4) && typeof(T) != typeof(Color) && typeof(T) != typeof(Rect2) && typeof(T) != typeof(Transform)) return;
        foreach (var component in MemoryMarshal.Cast<T, float>(MemoryMarshal.CreateReadOnlySpan(in value, 1)))
            if (!float.IsFinite(component)) throw new ArgumentException("Material floating-point values must be finite.", nameof(value));
    }

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
    internal readonly byte[][] Buffers;
    internal readonly Resource?[] Textures;
    internal readonly Shader? Shader;

    internal MaterialState(object gate, ShaderProgram program, MaterialState? previous, Shader? shader = null)
    {
        _gate = gate;
        Program = program;
        Shader = shader;
        Buffers = program.BufferSizes.Select(size => new byte[size]).ToArray();
        Textures = new Resource?[program.Textures.Length];
        foreach (var uniform in program.Uniforms.Values)
            if (uniform.Type == typeof(Transform))
                for (var i = 0; i < uniform.Count; i++)
                    uniform.Write(Buffers[uniform.Buffer].AsSpan(uniform.Offset + i * uniform.Stride, uniform.ElementSize), Transform.Identity);
        if (previous is null) return;
        foreach (var uniform in program.Uniforms.Values)
            if (previous.Program.Uniforms.TryGetValue(uniform.Name, out var old) && uniform.SameType(old))
                for (var i = 0; i < uniform.Count; i++)
                {
                    var source = previous.Buffers[old.Buffer].AsSpan(old.Offset + i * old.Stride, old.ElementSize);
                    var target = Buffers[uniform.Buffer].AsSpan(uniform.Offset + i * uniform.Stride, uniform.ElementSize);
                    if (uniform.Type == typeof(Transform)) uniform.Write(target, old.Read<Transform>(source));
                    else source.CopyTo(target);
                }
        for (var i = 0; i < Textures.Length; i++)
            for (var j = 0; j < previous.Textures.Length; j++)
                if (program.Textures[i].Name == previous.Program.Textures[j].Name && program.Textures[i].IsArray == previous.Program.Textures[j].IsArray) Textures[i] = previous.Textures[j];
    }

    internal void CopyTextures(Span<Resource?> target)
    {
        lock (_gate)
            for (var i = 0; i < Textures.Length; i++)
            {
                target[i] = Program.Textures[i].IsEngineTexture ? null : Textures[i] ?? Shader?.DefaultTexture(Program.Textures[i].Name)
                    ?? throw new InvalidOperationException($"Texture parameter '{Program.Textures[i].Name}' has no texture or Shader default.");
                Program.Textures[i].Validate(target[i]);
            }
    }

    internal void PushUniforms(nint command, float time, Vector2 screenPixelSize)
    {
        lock (_gate)
        {
            if (Program.TimeUniform is { } clock)
                MemoryMarshal.Write(Buffers[clock.Buffer].AsSpan(clock.Offset, sizeof(float)), in time);
            if (Program.ScreenPixelSizeUniform is { } pixels)
                MemoryMarshal.Write(Buffers[pixels.Buffer].AsSpan(pixels.Offset, sizeof(float) * 2), in screenPixelSize);
            for (var i = 0; i < Buffers.Length; i++)
                SDL.PushGPUFragmentUniformData(command, (uint)i, Buffers[i].AsSpan(), (uint)Buffers[i].Length);
        }
    }
}
