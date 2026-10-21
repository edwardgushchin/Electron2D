using System.Buffers.Binary;

namespace Electron2D;

public sealed partial class Shader
{
    /// <summary>Creates a canvas program from copied vertex and fragment SPIR-V modules.</summary>
    /// <param name="vertexBytecode">A vertex module using the canvas attribute and renderer-owned Frame interface.</param>
    /// <param name="fragmentBytecode">A fragment module consuming matching canvas varyings.</param>
    /// <returns>A shader owning both validated modules and one shared typed parameter namespace.</returns>
    /// <remarks>Each module is bounded to sixteen MiB. Shared parameter names require matching logical types.
    /// Hardware instance, lighting and glyph variants belong to the renderer. VertexIndex (GLSL gl_VertexIndex or
    /// HLSL SV_VertexID) identifies the original surface vertex for mesh and triangle-array drawing, including
    /// indices, LODs, expanded primitives and instances. Other canvas geometry retains native stream indices.
    /// Position-free mesh input is zero; authored local pixel positions use Frame size for clip conversion,
    /// then the renderer applies homogeneous canvas, command, instance and skin transforms and optional pixel snapping.
    /// Supplied-position geometry keeps framebuffer-pixel input. Vertex deformation does not update retained CPU bounds.
    /// No active renderer is required.</remarks>
    /// <exception cref="ArgumentException">A module or cross-stage parameter/interface is invalid.</exception>
    /// <exception cref="NotSupportedException">A module uses an unsupported canvas interface.</exception>
    public static Shader CreateFromSPIRV(ReadOnlySpan<byte> vertexBytecode, ReadOnlySpan<byte> fragmentBytecode)
    {
        var program = ShaderCompiler.ValidateCanvasProgram(vertexBytecode, fragmentBytecode);
        return new Shader { _program = program };
    }

    /// <summary>Atomically replaces both canvas stages after copying and validating their complete interface.</summary>
    /// <param name="vertexBytecode">The replacement vertex SPIR-V module.</param>
    /// <param name="fragmentBytecode">The replacement fragment SPIR-V module.</param>
    /// <remarks>Failure preserves both old stages. Success publishes one immutable version and emits Changed once
    /// unless signals are blocked. Material and per-item values still migrate by name and logical type,
    /// independently of stage bindings or event delivery. Vertex builtins preserve the original surface index
    /// for mesh/triangle-array drawing, as in the paired factory.</remarks>
    /// <exception cref="ArgumentException">A module or shared parameter/interface is invalid.</exception>
    /// <exception cref="NotSupportedException">A module uses an unsupported canvas interface.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public void SetSPIRV(ReadOnlySpan<byte> vertexBytecode, ReadOnlySpan<byte> fragmentBytecode)
    { ThrowIfDisposed(); PublishProgram(ShaderCompiler.ValidateCanvasProgram(vertexBytecode, fragmentBytecode)); }

    /// <summary>Returns an independent copy of a selected authored shader stage.</summary>
    /// <param name="stage">Vertex or Fragment for canvas/blit, Compute for particles.</param>
    /// <returns>The selected module; an empty vertex payload selects the renderer's built-in vertex program.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage is undefined.</exception>
    /// <exception cref="NotSupportedException">The selected stage does not belong to the current shader domain.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public byte[] GetSPIRV(RenderingDevice.ShaderStage stage)
    {
        var program = GetProgram();
        if (!Enum.IsDefined(stage)) throw new ArgumentOutOfRangeException(nameof(stage));
        if (program.Mode == Mode.Particles)
        {
            if (stage != RenderingDevice.ShaderStage.Compute) throw new NotSupportedException("Particle programs contain one compute stage.");
            return (byte[])program.Code.Clone();
        }
        if (stage is not (RenderingDevice.ShaderStage.Vertex or RenderingDevice.ShaderStage.Fragment))
        {
            if (!Enum.IsDefined(stage)) throw new ArgumentOutOfRangeException(nameof(stage));
            throw new NotSupportedException("Canvas programs contain vertex and fragment stages.");
        }
        return stage == RenderingDevice.ShaderStage.Vertex ? program.Vertex is { } vertex ? (byte[])vertex.Code.Clone() : [] : (byte[])program.Code.Clone();
    }

    private const uint StoredStageMagic = 0x32534845;
    private byte[] GetStoredSPIRV()
    {
        var program = GetProgram();
        if (ReferenceEquals(program, ShaderProgram.Default))
        {
            var marker = new byte[8]; BinaryPrimitives.WriteUInt32LittleEndian(marker, 0x44534845);
            BinaryPrimitives.WriteUInt32LittleEndian(marker.AsSpan(4), 1); return marker;
        }
        if (program.Mode is Mode.TextureBlit or Mode.Particles)
        {
            var payload = new byte[8 + program.Code.Length];
            BinaryPrimitives.WriteUInt32LittleEndian(payload, program.Mode == Mode.Particles ? 0x50534845u : 0x42534845u);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4), 1);
            program.Code.CopyTo(payload, 8); return payload;
        }
        if (program.Vertex is not { } vertex) return (byte[])program.Code.Clone();
        var data = new byte[checked(16 + vertex.Code.Length + program.Code.Length)];
        BinaryPrimitives.WriteUInt32LittleEndian(data, StoredStageMagic);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), 1);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(8), vertex.Code.Length);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(12), program.Code.Length);
        vertex.Code.CopyTo(data, 16); program.Code.CopyTo(data, 16 + vertex.Code.Length);
        return data;
    }
    private void SetStoredSPIRV(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(data) == 0x44534845)
        {
            if (data.Length != 8 || BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)) != 1) throw new InvalidDataException("Invalid built-in shader identity.");
            PublishProgram(ShaderProgram.Default); return;
        }
        // Older archives stored the current built-in module despite its private varying interface.
        if (data.AsSpan().SequenceEqual(ShaderProgram.Default.Code)) { PublishProgram(ShaderProgram.Default); return; }
        if (data.Length >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(data) is 0x42534845 or 0x50534845)
        {
            if (data.Length < 28 || BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)) != 1) throw new InvalidDataException("Unsupported stored shader domain header.");
            SetSPIRV(data.AsSpan(8), BinaryPrimitives.ReadUInt32LittleEndian(data) == 0x50534845 ? Mode.Particles : Mode.TextureBlit); return;
        }
        if (data.Length < 4 || BinaryPrimitives.ReadUInt32LittleEndian(data) != StoredStageMagic) { SetSPIRV(data); return; }
        if (data.Length < 16 || BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)) != 1)
            throw new InvalidDataException("Unsupported stored canvas stage header.");
        var vertex = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8)); var fragment = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(12));
        if (vertex < 20 || fragment < 20 || 16L + vertex + fragment != data.Length)
            throw new InvalidDataException("Stored canvas stage lengths are inconsistent.");
        SetSPIRV(data.AsSpan(16, vertex), data.AsSpan(16 + vertex, fragment));
    }
}
