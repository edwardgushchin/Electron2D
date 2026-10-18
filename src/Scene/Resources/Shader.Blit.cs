namespace Electron2D;

public sealed partial class Shader
{
    /// <summary>Creates a compiled fragment program for a selected executing shader domain.</summary>
    /// <param name="bytecode">A copied fragment SPIR-V module, at most sixteen MiB.</param>
    /// <param name="mode">CanvasItem or TextureBlit.</param>
    /// <returns>A resource owning the validated program.</returns>
    /// <remarks>Blit fragments accept optional float4 MODULATE at location zero and float2 UV at one,
    /// with float4 outputs at zero through three. Reserved scalar float samplers SOURCE_TEXTURE0 through
    /// SOURCE_TEXTURE3 receive the call's sources. Other uniforms, sampled parameters and globals retain
    /// their typed material interfaces. There is no authored vertex, scene instance, screen or lighting input.
    /// Compile HLSL/GLSL with ShaderImport --texture-blit; runtime loading never compiles source.</remarks>
    /// <exception cref="ArgumentException">The module is malformed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="NotSupportedException">The module's interface is unsupported by the selected domain.</exception>
    public static Shader CreateFromSPIRV(ReadOnlySpan<byte> bytecode, Mode mode) => new() { _program = ShaderCompiler.ValidateInterface(bytecode, true, mode: mode) };

    /// <summary>Atomically replaces a compiled fragment program and its shader domain.</summary>
    /// <param name="bytecode">The copied compiled fragment module.</param>
    /// <param name="mode">CanvasItem or TextureBlit.</param>
    /// <remarks>Failure preserves the prior program. Matching typed material parameters migrate on next access.</remarks>
    /// <exception cref="ArgumentException">The module is malformed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="NotSupportedException">The reflected interface is unsupported.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public void SetSPIRV(ReadOnlySpan<byte> bytecode, Mode mode)
    { ThrowIfDisposed(); PublishProgram(ShaderCompiler.ValidateInterface(bytecode, true, mode: mode)); }
}
