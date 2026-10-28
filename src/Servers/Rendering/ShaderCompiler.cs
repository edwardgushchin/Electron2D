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
        ShaderPrecision.ValidateNative(device, code);
        if ((SDL.GetGPUShaderFormats(device) & SDL.GPUShaderFormat.MSL) != 0 && NeedsBufferSizes(code))
            throw new NotSupportedException("This compute consumer must bind its physical buffer-size table.");
        fixed (byte* pointer = code)
        fixed (byte* entrypoint = "main\0"u8)
        {
            var reflection = ShaderCross.ReflectComputeSPIRV((nint)pointer, (nuint)code.Length, 0);
            if (reflection == 0) throw new ArgumentException("Compute reflection failed: " + SDL.GetError(), nameof(code));
            try
            {
                var metadata = Marshal.PtrToStructure<ShaderCross.ComputePipelineMetadata>(reflection);
                ValidateComputeMetadata(in metadata);
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

    internal static nint CreateShader(nint device, byte[] code, bool fragment, bool sizedPoints = false) => Run(() =>
    {
        ShaderPrecision.ValidateNative(device, code);
        CanvasPointProgram.ValidateNative(code, fragment, SDL.GetGPUDeviceDriver(device) ?? "", sizedPoints);
        fixed (byte* pointer = code)
        {
            var metadata = ShaderCross.ReflectGraphicsSPIRV((nint)pointer, (nuint)code.Length, 0);
            if (metadata == 0) throw new ArgumentException("Shader reflection failed: " + SDL.GetError(), nameof(code));
            try
            {
                var resources = Marshal.PtrToStructure<ShaderCross.GraphicsShaderMetadata>(metadata).ResourceInfo;
                resources.NumSamplers = Math.Max(resources.NumSamplers, SamplerCount(code));
                resources.NumUniformBuffers = Math.Max(resources.NumUniformBuffers, UniformBufferCount(code, fragment));
                return ShaderCross.CompileGraphicsShaderFromSPIRV(device, (nint)pointer, (nuint)code.Length, "main",
                    fragment ? ShaderCross.ShaderStage.Fragment : ShaderCross.ShaderStage.Vertex, in resources);
            }
            finally { SDL.Free(metadata); }
        }
    });

    internal static uint SamplerCount(byte[] code)
    {
        var source = new MaterialLightingProgram.Module(code).Instructions;
        var types = source.Where(i => MaterialLightingProgram.Op(i) is >= 19 and < 39).ToDictionary(i => i[1]);
        var bindings = source.Where(i => MaterialLightingProgram.Op(i) == 71 && i.Length == 4 && i[2] == 33).ToDictionary(i => i[1], i => i[3]);
        var samplers = source.Where(i => MaterialLightingProgram.Op(i) == 59 && i[3] == 0 && MaterialLightingProgram.Op(types[types[i[1]][3]]) == 26).Select(i => bindings[i[2]]).ToHashSet();
        uint count = 0;
        foreach (var variable in source.Where(i => MaterialLightingProgram.Op(i) == 59 && i[3] == 0))
        {
            var type = types[types[variable[1]][3]];
            var combined = MaterialLightingProgram.Op(type) == 27;
            if (combined) type = types[type[2]];
            if (MaterialLightingProgram.Op(type) != 25 || type[7] != 1 || !combined && !samplers.Contains(bindings[variable[2]])) continue;
            count = Math.Max(count, checked(bindings[variable[2]] + 1));
        }
        // Composed libraries retain unused slots before storage buffers; native layouts must retain that extent.
        if (count > 16) throw new NotSupportedException("Native sampled images exceed sixteen slots per stage.");
        return count;
    }

    private static uint UniformBufferCount(byte[] code, bool fragment)
    {
        var source = new MaterialLightingProgram.Module(code).Instructions;
        var types = source.Where(i => MaterialLightingProgram.Op(i) is >= 19 and < 39).ToDictionary(i => i[1]);
        var blocks = source.Where(i => MaterialLightingProgram.Op(i) == 71 && i.Length == 3 && i[2] == 2).Select(i => i[1]).ToHashSet();
        var bindings = source.Where(i => MaterialLightingProgram.Op(i) == 71 && i.Length == 4 && i[2] == 33).ToDictionary(i => i[1], i => i[3]);
        var sets = source.Where(i => MaterialLightingProgram.Op(i) == 71 && i.Length == 4 && i[2] == 34).ToDictionary(i => i[1], i => i[3]);
        uint count = 0;
        foreach (var variable in source.Where(i => MaterialLightingProgram.Op(i) == 59 && i[3] == 2))
            if (blocks.Contains(types[variable[1]][3]) && sets.GetValueOrDefault(variable[2]) == (fragment ? 3u : 1u) && bindings.TryGetValue(variable[2], out var binding))
                count = Math.Max(count, checked(binding + 1));
        // Lighting variants can use uniform slots absent from the unlit entry's active resources.
        if (count > 4) throw new NotSupportedException("Native uniform buffers exceed four slots per stage.");
        return count;
    }

    internal readonly record struct DeviceIO(uint Location, int Type, uint Width, uint BitWidth = 32);
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
                            var (type, bits) = field.VectorType switch
                            {
                                ShaderCross.IOVarType.Float16 => (0, 16u),
                                ShaderCross.IOVarType.Int16 => (1, 16u),
                                ShaderCross.IOVarType.UInt16 => (2, 16u),
                                ShaderCross.IOVarType.Float32 => (0, 32u),
                                ShaderCross.IOVarType.Int32 => (1, 32u),
                                ShaderCross.IOVarType.UInt32 => (2, 32u),
                                ShaderCross.IOVarType.Float64 => (0, 64u),
                                ShaderCross.IOVarType.Int64 => (1, 64u),
                                ShaderCross.IOVarType.UInt64 => (2, 64u),
                                _ => throw new NotSupportedException("Graphics interfaces require supported scalar or vector fields.")
                            };
                            var locations = bits == 64 && field.VectorSize > 2 ? 2u : 1u;
                            if (field.Location > 16 - locations || field.VectorSize is < 1 or > 4) throw new ArgumentException("Invalid graphics interface location.");
                            var occupied = ((1u << (int)locations) - 1) << (int)field.Location;
                            if ((mask & occupied) != 0) throw new ArgumentException("Graphics interface locations overlap.");
                            mask |= occupied; result[i] = new(field.Location, type, field.VectorSize, bits);
                        }
                        return result;
                    }
                    return (Read(metadata.Inputs, metadata.NumInputs), Read(metadata.Outputs, metadata.NumOutputs));
                }
                finally { SDL.Free(memory); }
            }
        }
        var v = Reflect(vertex); var f = Reflect(fragment);
        if (v.Inputs.Any(i => i.BitWidth != 32) || f.Outputs.Any(i => i.BitWidth != 32)) throw new NotSupportedException("Non-32-bit vertex attributes and color outputs require their native format adapters.");
        foreach (var input in f.Inputs)
            if (!v.Outputs.Contains(input)) throw new ArgumentException("Fragment inputs require matching vertex outputs.");
        return (v.Inputs, f.Outputs);
    });

    internal static ShaderProgram ValidateFragmentInterface(ReadOnlySpan<byte> bytecode) => ValidateInterface(bytecode, fragment: true, customVaryings: false);

    internal static ShaderProgram ValidateInterface(ReadOnlySpan<byte> bytecode, bool fragment,
        IReadOnlyDictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)>? sourceTypes = null, Shader.Mode mode = Shader.Mode.CanvasItem, bool customVaryings = true)
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
        code = program.ValidationCode ?? program.NativeCode;
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
                    if (fragment && mode == Shader.Mode.CanvasItem) ValidateVaryings(metadata.Inputs, metadata.NumInputs, customVaryings: customVaryings);
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
        var fragment = ValidateInterface(fragmentBytecode, fragment: true);
        var graphics = ValidateDeviceGraphics(vertex.NativeCode, fragment.NativeCode);
        if (graphics.Inputs.Any(input => input.Location is >= 4 and <= 6))
            throw new NotSupportedException("Canvas programs receive raw instance custom data at location three; hardware instance transforms and colors belong to the renderer.");
        var program = ShaderProgram.WithVertex(fragment, vertex); program.PrepareVertexVariants(); return program;
    }

    private static void ValidateVaryings(nint pointer, uint count, bool requireBase = false, bool customVaryings = true)
    {
        var locations = 0;
        for (var i = 0; i < count; i++)
        {
            var field = Marshal.PtrToStructure<ShaderCross.IOVarMetadata>(pointer + i * Marshal.SizeOf<ShaderCross.IOVarMetadata>());
            if (!customVaryings && field.Location is >= 12 and < 16) throw new NotSupportedException("Custom varying inputs require a matching authored vertex module.");
            var valid = field.Location switch
            {
                >= 12 and < 16 => field.VectorType is ShaderCross.IOVarType.Float32 or ShaderCross.IOVarType.Int32 or ShaderCross.IOVarType.UInt32 && field.VectorSize is >= 1 and <= 4,
                0 or 1 or 2 or 4 or 5 or 6 or 7 or 8 or 9 or 10 => field.VectorType == ShaderCross.IOVarType.Float32 && field.VectorSize == (field.Location == 5 ? 3 : field.Location is 1 or 4 ? 2 : 4),
                _ => false
            };
            if (!valid || (locations & (1 << (int)field.Location)) != 0)
                throw new NotSupportedException("Canvas varyings require their fixed color/UV/instance/mesh roles at locations zero through ten; custom float32/int32/uint32 scalars or vectors use twelve through fifteen. Location eleven belongs to glyph transport.");
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
