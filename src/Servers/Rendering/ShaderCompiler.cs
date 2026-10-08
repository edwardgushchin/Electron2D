using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal static unsafe class ShaderCompiler
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
                return ShaderCross.CompileComputePipelineFromSPIRV(device, in info, in metadata, 0);
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

    internal static ShaderProgram ValidateFragmentInterface(ReadOnlySpan<byte> bytecode) => ValidateInterface(bytecode, fragment: true);

    internal static ShaderProgram ValidateInterface(ReadOnlySpan<byte> bytecode, bool fragment,
        IReadOnlyDictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)>? sourceTypes = null)
    {
        if (bytecode.Length < 20 || bytecode.Length > MaximumBytecodeBytes || bytecode.Length % 4 != 0)
            throw new ArgumentException("SPIR-V must contain aligned words and a header, within 16 MiB.", nameof(bytecode));
        var code = bytecode.ToArray();
        ValidateStructure(code, fragment);
        var program = SpirvReflection.Read(code, fragment, sourceTypes);
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
                        r.NumUniformBuffers != program.BufferSizes.Length || !fragment && r.NumUniformBuffers != 1)
                        throw new NotSupportedException("The reflected resources do not match the canvas stage interface.");
                    if (metadata.NumInputs > (fragment ? 3 : 7) || (fragment ? metadata.NumOutputs != 1 : metadata.NumOutputs is < 2 or > 3))
                        throw new NotSupportedException("Canvas shaders use color at zero, UV at one, optional instance data at two and one fragment color output.");
                    if (fragment) ValidateVaryings(metadata.Inputs, metadata.NumInputs);
                    if (!fragment)
                    {
                        if (metadata.NumInputs is < 3 or > 7) throw new NotSupportedException("Canvas vertices require float2 position, float4 color and float2 UV.");
                        var locations = 0;
                        for (var i = 0; i < metadata.NumInputs; i++)
                        {
                            var field = Marshal.PtrToStructure<ShaderCross.IOVarMetadata>(metadata.Inputs + i * Marshal.SizeOf<ShaderCross.IOVarMetadata>());
                            if (field.VectorType != ShaderCross.IOVarType.Float32 || field.Location > 6 || field.VectorSize != (field.Location is 0 or 2 ? 2 : 4) ||
                                (locations & (1 << (int)field.Location)) != 0)
                                throw new NotSupportedException("Canvas vertex inputs require position at zero, color at one, UV at two, then raw instance data or the complete hardware instance layout.");
                            locations |= 1 << (int)field.Location;
                        }
                        if ((locations & 112) != 0 && (locations & 120) != 120) throw new NotSupportedException("Hardware instances require basis, translation, color and custom data at locations three through six.");
                        if ((locations & 7) != 7) throw new NotSupportedException("Canvas vertex inputs require position, color and UV.");
                    }
                    if (fragment) ValidateColor(metadata.Outputs);
                    else ValidateVaryings(metadata.Outputs, metadata.NumOutputs, requireBase: true);
                }
                finally { SDL.Free(memory); }
            }
            return true;
        });
        return program;
    }

    private static void ValidateVaryings(nint pointer, uint count, bool requireBase = false)
    {
        var locations = 0;
        for (var i = 0; i < count; i++)
        {
            var field = Marshal.PtrToStructure<ShaderCross.IOVarMetadata>(pointer + i * Marshal.SizeOf<ShaderCross.IOVarMetadata>());
            if (field.VectorType != ShaderCross.IOVarType.Float32 || field.Location > 2 || field.VectorSize != (field.Location == 1 ? 2 : 4) ||
                (locations & (1 << (int)field.Location)) != 0)
                throw new NotSupportedException("Canvas varyings require float4 color at zero, float2 UV at one and optional float4 instance data at two.");
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

    private static void ValidateStructure(byte[] code, bool fragment)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code);
        if (words[0] != 0x07230203 || words[1] is < 0x10000 or > 0x10600 || (words[1] & 0xff) != 0 ||
            words[3] is 0 or > 0x3fffff || words[4] != 0)
            throw new ArgumentException("Invalid or unsupported SPIR-V header.", nameof(code));
        var entrypoints = 0;
        for (var offset = 5; offset < words.Length;)
        {
            var count = (int)(words[offset] >> 16);
            var opcode = words[offset] & 0xffff;
            if (count == 0 || count > words.Length - offset)
                throw new ArgumentException("Truncated or zero-length SPIR-V instruction.", nameof(code));
            if (opcode == 15)
            {
                if (count < 5 || words[offset + 1] != (fragment ? 4u : 0u) || words[offset + 2] == 0 || words[offset + 2] >= words[3] ||
                    words[offset + 3] != 0x6e69616d || words[offset + 4] != 0)
                    throw new NotSupportedException("Canvas SPIR-V requires the selected stage with an entry point named main.");
                entrypoints++;
            }
            if (opcode == 17 && (count != 2 || words[offset + 1] != 1))
                throw new NotSupportedException("This canvas interface accepts only the baseline Shader capability.");
            if (opcode == 14 && (count != 3 || words[offset + 1] != 0 || words[offset + 2] != 1))
                throw new NotSupportedException("Canvas SPIR-V requires the logical GLSL450 memory model.");
            offset += count;
        }
        if (entrypoints != 1) throw new NotSupportedException("Canvas SPIR-V requires exactly one entry point.");
    }
}
