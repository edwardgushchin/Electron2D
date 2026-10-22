using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal static unsafe partial class ShaderCompiler
{
    private static readonly object Gate = new();
    private const int MaximumBytecodeBytes = 16 * 1024 * 1024;

    // ponytail: serialize the cold compiler path because shadercross Init/Quit owns process-wide libraries.
    // Use a shared lifetime with concurrent compilation only when import throughput requires it.
    private static T Run<T>(Func<T> action)
    {
        lock (Gate)
        {
            if (!ShaderCross.Init()) throw CanvasBackend.Failure("initialize the shader toolchain");
            try { return action(); }
            finally { ShaderCross.Quit(); }
        }
    }

    internal static SDL.GPUShaderFormat GetFormats() => Run(ShaderCross.GetSPIRVShaderFormats);

    internal static nint CreateComputePipeline(nint device, byte[] code) => Run(() =>
    {
        fixed (byte* pointer = code)
        fixed (byte* entrypoint = "main\0"u8)
        {
            var reflection = ShaderCross.ReflectComputeSPIRV((nint)pointer, (nuint)code.Length, 0);
            if (reflection == 0) throw new ArgumentException("Compute reflection failed: " + SDL.GetError(), nameof(code));
            try
            {
                var metadata = Marshal.PtrToStructure<ShaderCross.ComputePipelineMetadata>(reflection);
                if (metadata.NumReadOnlyStorageBuffers > 8 || metadata.NumReadWriteStorageBuffers > 8 ||
                    metadata.NumReadOnlyStorageTextures > 8 || metadata.NumReadWriteStorageTextures > 8 ||
                    metadata.NumSamplers > 16 || metadata.NumUniformBuffers > 4)
                    throw new NotSupportedException("Compute resources exceed the native pipeline binding limits.");
                var info = new ShaderCross.SPIRVInfo
                {
                    ByteCode = (nint)pointer,
                    ByteCodeSize = (nuint)code.Length,
                    Entrypoint = (nint)entrypoint,
                    ShaderStage = ShaderCross.ShaderStage.Compute
                };
                if ((SDL.GetGPUShaderFormats(device) & SDL.GPUShaderFormat.DXIL) == 0)
                    return ShaderCross.CompileComputePipelineFromSPIRV(device, in info, in metadata, 0);
                // The native convenience path prefers FXC/DXBC even on DXIL-capable devices.
                var compiled = ShaderCross.CompileDXILFromSPIRV(in info, out var size);
                if (compiled == 0) return 0;
                try
                {
                    var pipeline = new SDL.GPUComputePipelineCreateInfo
                    {
                        Code = compiled,
                        CodeSize = size,
                        Entrypoint = (nint)entrypoint,
                        Format = SDL.GPUShaderFormat.DXIL,
                        NumSamplers = metadata.NumSamplers,
                        NumReadonlyStorageTextures = metadata.NumReadOnlyStorageTextures,
                        NumReadonlyStorageBuffers = metadata.NumReadOnlyStorageBuffers,
                        NumReadwriteStorageTextures = metadata.NumReadWriteStorageTextures,
                        NumReadwriteStorageBuffers = metadata.NumReadWriteStorageBuffers,
                        NumUniformBuffers = metadata.NumUniformBuffers,
                        ThreadcountX = metadata.ThreadCountX,
                        ThreadcountY = metadata.ThreadCountY,
                        ThreadcountZ = metadata.ThreadCountZ
                    };
                    return SDL.CreateGPUComputePipeline(device, in pipeline);
                }
                finally { SDL.Free(compiled); }
            }
            finally { SDL.Free(reflection); }
        }
    });

    internal static nint CreateShader(nint device, byte[] code, bool fragment) => Run(() =>
    {
        fixed (byte* pointer = code)
        {
            var metadata = ShaderCross.ReflectGraphicsSPIRV((nint)pointer, (nuint)code.Length, 0);
            if (metadata == 0) throw new ArgumentException("Shader reflection failed: " + SDL.GetError(), nameof(code));
            try
            {
                var resources = Marshal.PtrToStructure<ShaderCross.GraphicsShaderMetadata>(metadata).ResourceInfo;
                return ShaderCross.CompileGraphicsShaderFromSPIRV(device, (nint)pointer, (nuint)code.Length, "main",
                    fragment ? ShaderCross.ShaderStage.Fragment : ShaderCross.ShaderStage.Vertex, in resources);
            }
            finally { SDL.Free(metadata); }
        }
    });

    internal static uint SamplerCount(byte[] code) => Run(() =>
    {
        fixed (byte* pointer = code)
        {
            var metadata = ShaderCross.ReflectGraphicsSPIRV((nint)pointer, (nuint)code.Length, 0);
            if (metadata == 0) throw new ArgumentException("Shader reflection failed: " + SDL.GetError(), nameof(code));
            try { return Marshal.PtrToStructure<ShaderCross.GraphicsShaderMetadata>(metadata).ResourceInfo.NumSamplers; }
            finally { SDL.Free(metadata); }
        }
    });

    internal readonly record struct DeviceIO(uint Location, int Type, uint Width);
    internal static (DeviceIO[] Inputs, DeviceIO[] Outputs) ValidateDeviceGraphics(byte[] vertex, byte[] fragment) => Run(() =>
    {
        (DeviceIO[] Inputs, DeviceIO[] Outputs) Reflect(byte[] code)
        {
            fixed (byte* pointer = code)
            {
                var memory = ShaderCross.ReflectGraphicsSPIRV((nint)pointer, (nuint)code.Length, 0);
                if (memory == 0) throw new ArgumentException("Graphics reflection failed: " + SDL.GetError(), nameof(code));
                try
                {
                    var metadata = Marshal.PtrToStructure<ShaderCross.GraphicsShaderMetadata>(memory);
                    var resources = metadata.ResourceInfo;
                    if (resources.NumSamplers > 16 || resources.NumStorageBuffers > 8 || resources.NumStorageTextures > 8 || resources.NumUniformBuffers > 4)
                        throw new NotSupportedException("Graphics resources exceed per-stage native binding limits.");
                    DeviceIO[] Read(nint fields, uint count)
                    {
                        if (count > 16) throw new NotSupportedException("Graphics interfaces support sixteen locations per stage.");
                        var result = new DeviceIO[count]; var mask = 0u;
                        for (var i = 0; i < count; i++)
                        {
                            var field = Marshal.PtrToStructure<ShaderCross.IOVarMetadata>(fields + i * Marshal.SizeOf<ShaderCross.IOVarMetadata>());
                            var type = field.VectorType switch { ShaderCross.IOVarType.Float32 => 0, ShaderCross.IOVarType.Int32 => 1, ShaderCross.IOVarType.UInt32 => 2, _ => throw new NotSupportedException("Graphics interfaces require 32-bit scalar or vector fields.") };
                            if (field.Location >= 16 || field.VectorSize is < 1 or > 4 || (mask & (1u << (int)field.Location)) != 0) throw new ArgumentException("Invalid or duplicate graphics interface location.");
                            mask |= 1u << (int)field.Location; result[i] = new(field.Location, type, field.VectorSize);
                        }
                        return result;
                    }
                    return (Read(metadata.Inputs, metadata.NumInputs), Read(metadata.Outputs, metadata.NumOutputs));
                }
                finally { SDL.Free(memory); }
            }
        }
        var v = Reflect(vertex); var f = Reflect(fragment);
        foreach (var input in f.Inputs)
            if (!v.Outputs.Contains(input)) throw new ArgumentException("Fragment inputs require matching vertex outputs.");
        return (v.Inputs, f.Outputs);
    });

    internal static ShaderProgram ValidateFragmentInterface(ReadOnlySpan<byte> bytecode) => ValidateInterface(bytecode, fragment: true);

    internal static ShaderProgram ValidateInterface(ReadOnlySpan<byte> bytecode, bool fragment,
        IReadOnlyDictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)>? sourceTypes = null, Shader.Mode mode = Shader.Mode.CanvasItem)
    {
        if (mode == Shader.Mode.Particles) return ValidateParticleInterface(bytecode, sourceTypes);
        if (mode is not (Shader.Mode.CanvasItem or Shader.Mode.TextureBlit)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (!fragment && mode != Shader.Mode.CanvasItem) throw new NotSupportedException("Blit programs use the built-in vertex stage.");
        ValidateBytecodeLength(bytecode);
        var code = bytecode.ToArray();
        ValidateStructure(code, fragment);
        var program = SpirvReflection.Read(code, fragment, sourceTypes, mode == Shader.Mode.TextureBlit);
        program.Mode = mode;
        if (mode == Shader.Mode.TextureBlit && (program.HasInstanceUniforms || program.UsesScreenTexture || program.UsesSDFTexture || program.ScreenPixelSizeUniform is not null || program.SDFCoordsUniform is not null || program.Textures.Any(t => t.IsCanvasTexture)))
            throw new NotSupportedException("Blit shaders have no scene, screen or command-texture interface; use SOURCE_TEXTURE0 through SOURCE_TEXTURE3.");
        if (!fragment) program.ValidateVertexFrame();
        code = program.NativeCode;
        Run(() =>
        {
            fixed (byte* pointer = code)
            {
                var memory = ShaderCross.ReflectGraphicsSPIRV((nint)pointer, (nuint)code.Length, 0);
                if (memory == 0) throw new ArgumentException("SPIR-V reflection failed: " + SDL.GetError(), nameof(code));
                try
                {
                    var metadata = Marshal.PtrToStructure<ShaderCross.GraphicsShaderMetadata>(memory);
                    var r = metadata.ResourceInfo;
                    if (r.NumSamplers != program.Textures.Length || r.NumStorageTextures != 0 || r.NumStorageBuffers != 0 ||
                        r.NumUniformBuffers != program.BufferSizes.Length)
                        throw new NotSupportedException($"The reflected resources do not match the canvas stage interface (samplers={r.NumSamplers}/{program.Textures.Length}, storage images={r.NumStorageTextures}, buffers={r.NumStorageBuffers}, uniforms={r.NumUniformBuffers}/{program.BufferSizes.Length}).");
                    if (mode == Shader.Mode.TextureBlit)
                    {
                        if (metadata.NumInputs > 2 || metadata.NumOutputs is < 1 or > 4) throw new NotSupportedException("Blit fragments support MODULATE, UV and one through four color outputs.");
                        for (var i = 0; i < metadata.NumInputs; i++)
                        {
                            var field = Marshal.PtrToStructure<ShaderCross.IOVarMetadata>(metadata.Inputs + i * Marshal.SizeOf<ShaderCross.IOVarMetadata>());
                            if (field.VectorType != ShaderCross.IOVarType.Float32 || field.Location > 1 || field.VectorSize != (field.Location == 0 ? 4 : 2)) throw new NotSupportedException("Blit inputs require float4 MODULATE at zero and float2 UV at one.");
                        }
                        var mask = 0;
                        for (var i = 0; i < metadata.NumOutputs; i++)
                        {
                            var field = Marshal.PtrToStructure<ShaderCross.IOVarMetadata>(metadata.Outputs + i * Marshal.SizeOf<ShaderCross.IOVarMetadata>());
                            if (field.VectorType != ShaderCross.IOVarType.Float32 || field.VectorSize != 4 || field.Location > 3 || (mask & (1 << (int)field.Location)) != 0) throw new NotSupportedException("Blit outputs require unique float4 locations zero through three.");
                            mask |= 1 << (int)field.Location; program.ColorOutputs = Math.Max(program.ColorOutputs, (int)field.Location + 1);
                        }
                        if (mask != (1 << (int)metadata.NumOutputs) - 1) throw new NotSupportedException("Blit color outputs must be contiguous from zero.");
                    }
                    else if (metadata.NumInputs > (fragment ? 10 : 14) || (fragment ? metadata.NumOutputs != 1 : metadata.NumOutputs is < 2 or > 10))
                        throw new NotSupportedException("Canvas shaders use color at zero, UV at one, optional instance data at two, secondary UV at four, normal/tangent at five/six, mesh custom float4 at seven through ten and one fragment color output.");
                    if (fragment && mode == Shader.Mode.CanvasItem) ValidateVaryings(metadata.Inputs, metadata.NumInputs);
                    if (!fragment)
                    {
                        if (metadata.NumInputs is < 3 or > 14) throw new NotSupportedException("Canvas vertices require float2 position, float4 color and float2 UV.");
                        var locations = 0;
                        for (var i = 0; i < metadata.NumInputs; i++)
                        {
                            var field = Marshal.PtrToStructure<ShaderCross.IOVarMetadata>(metadata.Inputs + i * Marshal.SizeOf<ShaderCross.IOVarMetadata>());
                            if (field.VectorType != ShaderCross.IOVarType.Float32 || field.Location > 13 || field.VectorSize != (field.Location == 8 ? 3 : field.Location is 0 or 2 or 7 ? 2 : 4) ||
                                (locations & (1 << (int)field.Location)) != 0)
                                throw new NotSupportedException("Canvas vertex inputs require position at zero, color at one, UV at two, then raw instance data or the complete hardware instance layout, with optional secondary UV at seven, normal/tangent at eight/nine and mesh custom float4 at ten through thirteen.");
                            locations |= 1 << (int)field.Location;
                        }
                        if ((locations & 112) != 0 && (locations & 120) != 120) throw new NotSupportedException("Hardware instances require basis, translation, color and custom data at locations three through six.");
                        if ((locations & 7) != 7) throw new NotSupportedException("Canvas vertex inputs require position, color and UV.");
                    }
                    if (fragment) { if (mode == Shader.Mode.CanvasItem) ValidateColor(metadata.Outputs); }
                    else ValidateVaryings(metadata.Outputs, metadata.NumOutputs, requireBase: true);
                }
                finally { SDL.Free(memory); }
            }
            return true;
        });
        return program;
    }

    internal static void ValidateBytecodeLength(ReadOnlySpan<byte> bytecode)
    {
        if (bytecode.Length < 20 || bytecode.Length > MaximumBytecodeBytes || bytecode.Length % 4 != 0)
            throw new ArgumentException("SPIR-V must contain aligned words and a header, within 16 MiB.", nameof(bytecode));
    }

    internal static ShaderProgram ValidateCanvasProgram(ReadOnlySpan<byte> vertexBytecode, ReadOnlySpan<byte> fragmentBytecode)
    {
        ValidateBytecodeLength(vertexBytecode); ValidateBytecodeLength(fragmentBytecode);
        var vertex = ValidateInterface(vertexBytecode, fragment: false);
        var fragment = ValidateFragmentInterface(fragmentBytecode);
        var graphics = ValidateDeviceGraphics(vertex.NativeCode, fragment.NativeCode);
        if (graphics.Inputs.Any(input => input.Location is >= 4 and <= 6))
            throw new NotSupportedException("Canvas programs receive raw instance custom data at location three; hardware instance transforms and colors belong to the renderer.");
        var program = ShaderProgram.WithVertex(fragment, vertex); program.PrepareVertexVariants(); return program;
    }

    private static void ValidateVaryings(nint pointer, uint count, bool requireBase = false)
    {
        var locations = 0;
        for (var i = 0; i < count; i++)
        {
            var field = Marshal.PtrToStructure<ShaderCross.IOVarMetadata>(pointer + i * Marshal.SizeOf<ShaderCross.IOVarMetadata>());
            if (field.VectorType != ShaderCross.IOVarType.Float32 || field.Location is not (0 or 1 or 2 or 4 or 5 or 6 or 7 or 8 or 9 or 10) || field.VectorSize != (field.Location == 5 ? 3 : field.Location is 1 or 4 ? 2 : 4) ||
                (locations & (1 << (int)field.Location)) != 0)
                throw new NotSupportedException("Canvas varyings require float4 color at zero, float2 UV at one and optional float4 instance data at two float2 secondary UV at four, float3 normal at five and float4 tangent at six, plus mesh custom float4 at seven through ten.");
            locations |= 1 << (int)field.Location;
        }
        if (requireBase && (locations & 3) != 3) throw new NotSupportedException("Canvas vertex outputs require color and UV.");
    }

    private static void ValidateColor(nint pointer)
    {
        var field = Marshal.PtrToStructure<ShaderCross.IOVarMetadata>(pointer);
        if (field.Location != 0 || field.VectorType != ShaderCross.IOVarType.Float32 || field.VectorSize != 4)
            throw new NotSupportedException("Canvas shader color interfaces must be float4 at location zero.");
    }

    private static void ValidateStructure(byte[] code, bool fragment, bool particles = false)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code);
        if (words[0] != 0x07230203 || words[1] is < 0x10000 or > 0x10600 || (words[1] & 0xff) != 0 ||
            words[3] is 0 or > 0x3fffff || words[4] != 0)
            throw new ArgumentException("Invalid or unsupported SPIR-V header.", nameof(code));
        var entrypoints = 0; var derivativeControl = false;
        for (var offset = 5; offset < words.Length;)
        {
            var count = (int)(words[offset] >> 16);
            var opcode = words[offset] & 0xffff;
            if (count == 0 || count > words.Length - offset)
                throw new ArgumentException("Truncated or zero-length SPIR-V instruction.", nameof(code));
            if (opcode == 15)
            {
                if (count < 5 || words[offset + 1] != (particles ? 5u : fragment ? 4u : 0u) || words[offset + 2] == 0 || words[offset + 2] >= words[3] ||
                    words[offset + 3] != 0x6e69616d || words[offset + 4] != 0)
                    throw new NotSupportedException("Canvas SPIR-V requires the selected stage with an entry point named main.");
                entrypoints++;
            }
            if (opcode == 17)
            {
                if (count != 2 || words[offset + 1] is not (1 or 29 or 50 or 5301 or 5307) && !(fragment && !particles && words[offset + 1] == 51))
                    throw new NotSupportedException("Canvas shaders require supported graphics, fragment derivative, image query or sampled descriptor indexing capabilities.");
                if (words[offset + 1] == 51) derivativeControl = true;
            }
            if (opcode is >= 207 and <= 215)
            {
                if (!fragment || particles) throw new NotSupportedException("Screen-space derivatives require the fragment stage.");
                if (count != 4) throw new ArgumentException("Invalid derivative instruction.", nameof(code));
                if (opcode >= 210 && !derivativeControl) throw new ArgumentException("Fine/coarse derivatives require the derivative-control capability.", nameof(code));
            }
            if (opcode == 14 && (count != 3 || words[offset + 1] != 0 || words[offset + 2] != 1))
                throw new NotSupportedException("Canvas SPIR-V requires the logical GLSL450 memory model.");
            offset += count;
        }
        if (entrypoints != 1) throw new NotSupportedException("Canvas SPIR-V requires exactly one entry point.");
    }
}
