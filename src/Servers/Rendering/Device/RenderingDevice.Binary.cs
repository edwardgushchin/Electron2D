using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Electron2D;

public sealed partial class RenderingDevice
{
    private const int BinaryHeaderBytes = 48, BinaryModuleLimit = 16 * 1024 * 1024, BinaryNameLimit = 4096;
    private const int BinarySizeLimit = BinaryHeaderBytes + 12 + BinaryNameLimit + 2 * (4 + BinaryModuleLimit);
    private static readonly UTF8Encoding BinaryText = new(false, true);

    /// <summary>Validates and packages compiled compute, vertex-only or paired graphics modules for later shader creation.</summary>
    /// <param name="spirvData">Copied successful stage modules accepted by the ordinary shader factory.</param>
    /// <param name="name">A strict UTF-8 diagnostic label of at most 4096 encoded bytes.</param>
    /// <returns>An independent, versioned Electron2D shader container with integrity-checked stage bytes.</returns>
    /// <remarks>This cold operation validates concrete native programs, then releases them. Symbolic specialization
    /// templates retain pipeline-time validation. The container stores SPIR-V, not driver pipeline-cache objects;
    /// loading repeats device validation and native creation, without invoking a source compiler. Preserve the
    /// original modules for future container-format changes. Version one remains readable and is emitted for compute/paired programs;
    /// vertex-only programs use version two with an explicit missing-fragment flag.</remarks>
    /// <exception cref="ArgumentException">Modules, diagnostics or the label are invalid.</exception>
    /// <exception cref="NotSupportedException">The program exceeds the executing backend profile.</exception>
    public byte[] ShaderCompileBinaryFromSPIRV(RDShaderSPIRV spirvData, string name = "")
    {
        IdleList(); ArgumentNullException.ThrowIfNull(name);
        int nameBytes;
        try { nameBytes = BinaryText.GetByteCount(name); }
        catch (EncoderFallbackException e) { throw new ArgumentException("The shader name must contain valid Unicode.", nameof(name), e); }
        if (nameBytes > BinaryNameLimit) throw new ArgumentException("The shader name exceeds 4096 UTF-8 bytes.", nameof(name));
        var program = CaptureDeviceProgram(spirvData, name);
        try
        {
            if (program.Sources.Any(s => s.Length > BinaryModuleLimit)) throw new ArgumentException("A shader stage exceeds 16 MiB.", nameof(spirvData));
            var result = new byte[BinaryHeaderBytes + 12 + nameBytes + program.Sources.Sum(s => 4 + s.Length)];
            "E2DSBIN\0"u8.CopyTo(result); BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), program.VertexOnly ? 2u : 1u);
            BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), result.Length);
            var cursor = BinaryHeaderBytes;
            Write(program.Sources.Length); Write(nameBytes); BinaryText.GetBytes(name, result.AsSpan(cursor, nameBytes)); cursor += nameBytes;
            Write(program.VertexOnly ? 1 : 0); // Version two marks an intentionally absent fragment module.
            foreach (var source in program.Sources) { Write(source.Length); source.CopyTo(result, cursor); cursor += source.Length; }
            SHA256.HashData(result.AsSpan(BinaryHeaderBytes), result.AsSpan(16, 32)); return result;
            void Write(int value) { BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(cursor), value); cursor += 4; }
        }
        finally { ReleaseProgram(program); }
    }

    /// <summary>Reserves a shader identity for later successful bytecode loading.</summary>
    /// <returns>An owned, uninitialized RID accepted by ShaderCreateFromBytecode and FreeRID.</returns>
    /// <remarks>Pipeline, descriptor-set and shader-interface operations reject this identity until it is populated.</remarks>
    public RID ShaderCreatePlaceholder() { IdleList(); return Add(new Entry { Kind = Kind.ShaderPlaceholder }); }

    /// <summary>Creates a shader from a copied validated Electron2D binary container.</summary>
    /// <param name="binaryData">The complete output of ShaderCompileBinaryFromSPIRV.</param>
    /// <param name="placeholderRID">An optional uninitialized shader RID owned by this device.</param>
    /// <returns>A new shader RID, or the supplied placeholder RID populated with the validated program.</returns>
    /// <remarks>Parsing, validation and native creation finish before identity publication. Failure leaves a placeholder
    /// reusable; initialized shaders cannot be replaced. Specialization, graphics variants and borrowed lifetimes use
    /// the ordinary shader path. Containers are engine-owned, have no external binary compatibility and are not an
    /// authentication boundary. Native validation still applies after their corruption check.</remarks>
    /// <exception cref="ArgumentException">The container, placeholder ownership/kind or program is invalid.</exception>
    /// <exception cref="NotSupportedException">The container version or shader exceeds the current profile.</exception>
    public RID ShaderCreateFromBytecode(ReadOnlySpan<byte> binaryData, RID placeholderRID = default)
    {
        IdleList(); if (placeholderRID != default) Get(placeholderRID, Kind.ShaderPlaceholder);
        if (binaryData.Length < BinaryHeaderBytes || binaryData.Length > BinarySizeLimit || !binaryData[..8].SequenceEqual("E2DSBIN\0"u8) || BinaryPrimitives.ReadInt32LittleEndian(binaryData[12..]) != binaryData.Length)
            throw new ArgumentException("The shader container header or length is invalid.", nameof(binaryData));
        var version = BinaryPrimitives.ReadUInt32LittleEndian(binaryData[8..]);
        if (version is not (1 or 2)) throw new NotSupportedException("The shader container version is unsupported.");
        Span<byte> digest = stackalloc byte[32]; SHA256.HashData(binaryData[BinaryHeaderBytes..], digest);
        if (!CryptographicOperations.FixedTimeEquals(digest, binaryData.Slice(16, 32))) throw new ArgumentException("The shader container checksum is invalid.", nameof(binaryData));
        var payload = binaryData[BinaryHeaderBytes..]; var stages = BinaryRead(ref payload); var nameBytes = BinaryRead(ref payload);
        if (stages is not (1 or 2) || nameBytes < 0 || nameBytes > BinaryNameLimit || nameBytes > payload.Length) throw new ArgumentException("The shader container stage count or name length is invalid.", nameof(binaryData));
        string name;
        try { name = BinaryText.GetString(payload[..nameBytes]); }
        catch (DecoderFallbackException e) { throw new ArgumentException("The shader container name must be valid UTF-8.", nameof(binaryData), e); }
        payload = payload[nameBytes..]; var flags = BinaryRead(ref payload);
        if (flags != 0 && (flags != 1 || version != 2 || stages != 2)) throw new ArgumentException("The shader container flags are invalid.", nameof(binaryData));
        using var spirv = new RDShaderSPIRV();
        if (stages == 1) spirv.BytecodeCompute = BinaryReadModule(ref payload);
        else
        {
            spirv.BytecodeVertex = BinaryReadModule(ref payload); spirv.BytecodeFragment = BinaryReadModule(ref payload, flags == 1);
            if ((flags == 1) != (spirv.BytecodeFragment.Length == 0)) throw new ArgumentException("The shader container fragment flags disagree with its module.", nameof(binaryData));
        }
        if (!payload.IsEmpty) throw new ArgumentException("The shader container has trailing data.", nameof(binaryData));
        var program = CaptureDeviceProgram(spirv, name);
        try { if (placeholderRID == default) return Add(program); _entries[placeholderRID] = program; return placeholderRID; }
        catch { ReleaseProgram(program); throw; }
    }
    private static int BinaryRead(ref ReadOnlySpan<byte> data)
    {
        if (data.Length < 4) throw new ArgumentException("The shader container is truncated.", "binaryData");
        var result = BinaryPrimitives.ReadInt32LittleEndian(data); data = data[4..]; return result;
    }
    private static byte[] BinaryReadModule(ref ReadOnlySpan<byte> data, bool allowEmpty = false)
    {
        var size = BinaryRead(ref data);
        if (allowEmpty && size == 0) return [];
        if (size < 20 || size > BinaryModuleLimit || size % 4 != 0 || size > data.Length) throw new ArgumentException("The shader stage length is invalid.", "binaryData");
        var result = data[..size].ToArray(); data = data[size..]; return result;
    }
}
