using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using SDL3;

namespace Electron2D;

internal static unsafe partial class ShaderCompiler
{
    private static bool NeedsBufferSizes(byte[] code)
    {
        ValidateBytecodeLength(code);
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan());
        if (words[0] != 0x07230203 || words[3] is 0 or > 1_048_576 || words[4] != 0)
            throw new ArgumentException("Invalid compute module header.", nameof(code));
        var needed = false;
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16);
            if (count == 0 || count > words.Length - at) throw new ArgumentException("Truncated compute instruction.", nameof(code));
            needed |= (words[at] & 65535) == 68;
            at += count;
        }
        return needed;
    }

    internal static (nint Handle, ComputePipeline.BufferLayout Layout) CreateComputePipelineWithBufferSizes(nint device, byte[] code)
    {
        if ((SDL.GetGPUShaderFormats(device) & SDL.GPUShaderFormat.MSL) == 0 || !NeedsBufferSizes(code))
            return (CreateComputePipeline(device, code), default);
        var translated = PrepareComputeMSL(code);
        var bytes = Encoding.UTF8.GetBytes(translated.Source);
        fixed (byte* source = bytes)
        fixed (byte* entry = Encoding.UTF8.GetBytes(translated.Entrypoint + "\0"))
        {
            var m = translated.Metadata;
            var info = new SDL.GPUComputePipelineCreateInfo
            {
                Code = (nint)source,
                CodeSize = (nuint)bytes.Length,
                Entrypoint = (nint)entry,
                Format = SDL.GPUShaderFormat.MSL,
                NumSamplers = m.NumSamplers,
                NumReadonlyStorageTextures = m.NumReadOnlyStorageTextures,
                NumReadonlyStorageBuffers = m.NumReadOnlyStorageBuffers,
                NumReadwriteStorageTextures = m.NumReadWriteStorageTextures,
                NumReadwriteStorageBuffers = m.NumReadWriteStorageBuffers,
                NumUniformBuffers = m.NumUniformBuffers,
                ThreadcountX = m.ThreadCountX,
                ThreadcountY = m.ThreadCountY,
                ThreadcountZ = m.ThreadCountZ
            };
            return (SDL.CreateGPUComputePipeline(device, in info), translated.Layout);
        }
    }

    internal static (string Source, string Entrypoint, ShaderCross.ComputePipelineMetadata Metadata, ComputePipeline.BufferLayout Layout) PrepareComputeMSL(byte[] code) => Run(() =>
    {
        ValidateBytecodeLength(code);
        fixed (byte* pointer = code)
        fixed (byte* entry = "main\0"u8)
        {
            var reflection = ShaderCross.ReflectComputeSPIRV((nint)pointer, (nuint)code.Length, 0);
            if (reflection == 0) throw new ArgumentException("Compute reflection failed: " + SDL.GetError(), nameof(code));
            try
            {
                var metadata = Marshal.PtrToStructure<ShaderCross.ComputePipelineMetadata>(reflection);
                ValidateComputeMetadata(in metadata);
                var info = new ShaderCross.SPIRVInfo { ByteCode = (nint)pointer, ByteCodeSize = (nuint)code.Length, Entrypoint = (nint)entry, ShaderStage = ShaderCross.ShaderStage.Compute };
                var native = ShaderCross.TranspileMSLFromSPIRV(in info);
                if (native == 0) throw new ArgumentException("Metal compute translation failed: " + SDL.GetError(), nameof(code));
                string source;
                try { source = Marshal.PtrToStringUTF8(native)!; }
                finally { SDL.Free(native); }
                var entries = Regex.Matches(source, @"^kernel\s+void\s+([A-Za-z_][A-Za-z_0-9]*)\s*\(", RegexOptions.Multiline);
                if (entries.Count != 1) throw new NotSupportedException("The compute translation must have one native entry point.");
                var entrypoint = entries[0].Groups[1].Value;
                if (!source.Contains("[[buffer(25)]]", StringComparison.Ordinal)) return (source, entrypoint, metadata, default(ComputePipeline.BufferLayout));
                if (!Regex.IsMatch(source, @"\bconstant\s+uint\s*\*\s*\w+\s+\[\[buffer\(25\)\]\]"))
                    throw new NotSupportedException("The compute translation has no supported buffer-size table.");
                if (metadata.NumReadOnlyStorageBuffers == 8) throw new NotSupportedException("Compute buffer lengths require one private readonly binding.");
                var layout = new ComputePipeline.BufferLayout(true, metadata.NumUniformBuffers, metadata.NumReadOnlyStorageBuffers, metadata.NumReadWriteStorageBuffers);
                var firstWrite = layout.Uniforms + layout.ReadOnly;
                var bindings = new HashSet<uint>();
                source = Regex.Replace(source, @"\[\[buffer\((\d+)\)\]\]", match =>
                {
                    var slot = uint.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                    if (slot != 25 && slot >= firstWrite + layout.ReadWrite) throw new NotSupportedException("Unexpected native compute buffer binding.");
                    if (!bindings.Add(slot)) throw new NotSupportedException("Repeated native compute buffer binding.");
                    // Array-length indices retain their original byte-size table positions.
                    var nativeSlot = slot == 25 ? firstWrite : slot >= firstWrite ? slot + 1 : slot;
                    return "[[buffer(" + nativeSlot.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")]]";
                });
                if (!bindings.Contains(25)) throw new NotSupportedException("Native compute buffer sizes have no binding.");
                metadata.NumReadOnlyStorageBuffers++;
                return (source, entrypoint, metadata, layout);
            }
            finally { SDL.Free(reflection); }
        }
    });

    private static void ValidateComputeMetadata(in ShaderCross.ComputePipelineMetadata metadata)
    {
        if (metadata.NumReadOnlyStorageBuffers > 8 || metadata.NumReadWriteStorageBuffers > 8 ||
            metadata.NumReadOnlyStorageTextures > 8 || metadata.NumReadWriteStorageTextures > 8 ||
            metadata.NumSamplers > 16 || metadata.NumUniformBuffers > 4)
            throw new NotSupportedException("Compute resources exceed the native pipeline binding limits.");
    }
}
