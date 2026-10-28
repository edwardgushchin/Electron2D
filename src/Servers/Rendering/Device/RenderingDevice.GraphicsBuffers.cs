using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private sealed record VertexLayout(SDL.GPUVertexAttribute[] Attributes, SDL.GPUVertexBufferDescription[] Buffers, uint[] Ends);
    private sealed record VertexArray(uint Count, int Format, RID[] Buffers, uint[] Offsets);
    private sealed record IndexArray(RID Buffer, uint Offset, uint Count);
    private readonly Dictionary<int, VertexLayout> _vertexFormats = [];
    private static int _nextGraphicsFormat;
    private static int NextFormatID(ref int sequence) { var id = Interlocked.Increment(ref sequence); return id > 0 ? id : throw new InvalidOperationException("Device format identities are exhausted."); }
    private static (SDL.GPUVertexElementFormat Format, uint Bytes) VertexEncoding(DataFormat format) => format switch
    {
        DataFormat.R32SFLOAT => (SDL.GPUVertexElementFormat.Float, 4),
        DataFormat.R32G32SFLOAT => (SDL.GPUVertexElementFormat.Float2, 8),
        DataFormat.R32G32B32SFLOAT => (SDL.GPUVertexElementFormat.Float3, 12),
        DataFormat.R32G32B32A32SFLOAT => (SDL.GPUVertexElementFormat.Float4, 16),
        DataFormat.R32SINT => (SDL.GPUVertexElementFormat.Int, 4),
        DataFormat.R32G32SINT => (SDL.GPUVertexElementFormat.Int2, 8),
        DataFormat.R32G32B32SINT => (SDL.GPUVertexElementFormat.Int3, 12),
        DataFormat.R32G32B32A32SINT => (SDL.GPUVertexElementFormat.Int4, 16),
        DataFormat.R32UINT => (SDL.GPUVertexElementFormat.Uint, 4),
        DataFormat.R32G32UINT => (SDL.GPUVertexElementFormat.Uint2, 8),
        DataFormat.R32G32B32UINT => (SDL.GPUVertexElementFormat.Uint3, 12),
        DataFormat.R32G32B32A32UINT => (SDL.GPUVertexElementFormat.Uint4, 16),
        DataFormat.R8G8B8A8UNORM => (SDL.GPUVertexElementFormat.Ubyte4Norm, 4),
        DataFormat.R8G8UNORM => (SDL.GPUVertexElementFormat.Ubyte2Norm, 2),
        DataFormat.R8G8B8A8SNORM => (SDL.GPUVertexElementFormat.Byte4Norm, 4),
        DataFormat.R8G8SNORM => (SDL.GPUVertexElementFormat.Byte2Norm, 2),
        DataFormat.R8G8UINT => (SDL.GPUVertexElementFormat.Ubyte2, 2),
        DataFormat.R8G8B8A8UINT => (SDL.GPUVertexElementFormat.Ubyte4, 4),
        DataFormat.R8G8SINT => (SDL.GPUVertexElementFormat.Byte2, 2),
        DataFormat.R8G8B8A8SINT => (SDL.GPUVertexElementFormat.Byte4, 4),
        DataFormat.R16G16SFLOAT => (SDL.GPUVertexElementFormat.Half2, 4),
        DataFormat.R16G16B16A16SFLOAT => (SDL.GPUVertexElementFormat.Half4, 8),
        DataFormat.R16G16UNORM => (SDL.GPUVertexElementFormat.Ushort2Norm, 4),
        DataFormat.R16G16B16A16UNORM => (SDL.GPUVertexElementFormat.Ushort4Norm, 8),
        DataFormat.R16G16SNORM => (SDL.GPUVertexElementFormat.Short2Norm, 4),
        DataFormat.R16G16B16A16SNORM => (SDL.GPUVertexElementFormat.Short4Norm, 8),
        DataFormat.R16G16UINT => (SDL.GPUVertexElementFormat.Ushort2, 4),
        DataFormat.R16G16B16A16UINT => (SDL.GPUVertexElementFormat.Ushort4, 8),
        DataFormat.R16G16SINT => (SDL.GPUVertexElementFormat.Short2, 4),
        DataFormat.R16G16B16A16SINT => (SDL.GPUVertexElementFormat.Short4, 8),
        _ => throw new NotSupportedException("The data format has no portable vertex encoding.")
    };
    /// <summary>Creates a copied, validated vertex layout.</summary>
    /// <param name="vertexDescriptions">Locations, buffer bindings, strides and formats.</param>
    /// <returns>A device-local format identifier.</returns>
    /// <exception cref="ArgumentException">Bindings, locations or ranges are inconsistent.</exception>
    /// <exception cref="NotSupportedException">An encoding is not supported.</exception>
    /// <remarks>Effective native attribute, binding, byte-offset and stride limits also apply; query LimitGet before preparing adaptive layouts.</remarks>
    public int VertexFormatCreate(ReadOnlySpan<RDVertexAttribute> vertexDescriptions)
    {
        IdleList(); var limits = ValidationLimits; if (vertexDescriptions.Length > 16 || limits is not null && (ulong)vertexDescriptions.Length > limits[23]) throw new ArgumentOutOfRangeException(nameof(vertexDescriptions));
        var attributes = new SDL.GPUVertexAttribute[vertexDescriptions.Length]; var descriptions = new Dictionary<uint, SDL.GPUVertexBufferDescription>(); var ends = new Dictionary<uint, uint>(); var locations = new HashSet<uint>();
        for (var i = 0; i < vertexDescriptions.Length; i++)
        {
            var source = vertexDescriptions[i]; ArgumentNullException.ThrowIfNull(source);
            var binding = source.Binding; var location = source.Location; var offset = source.Offset; var stride = source.Stride; var frequency = source.Frequency;
            var (format, bytes) = VertexEncoding(source.Format);
            if (binding >= 16 || location >= 16 || stride == 0 || stride > 2048 || offset > stride || bytes > stride - offset || !Enum.IsDefined(frequency) || !locations.Add(location)) throw new ArgumentException("Invalid vertex attribute layout.", nameof(vertexDescriptions));
            if (limits is not null && (binding >= limits[24] || location >= limits[23] || offset > limits[22] || stride > limits[25])) throw new ArgumentException("Vertex input exceeds device limits.", nameof(vertexDescriptions));
            var descriptor = new SDL.GPUVertexBufferDescription { Slot = binding, Pitch = stride, InputRate = frequency == VertexFrequency.Instance ? SDL.GPUVertexInputRate.Instance : SDL.GPUVertexInputRate.Vertex };
            if (descriptions.TryGetValue(binding, out var prior) && (prior.Pitch != stride || prior.InputRate != descriptor.InputRate)) throw new ArgumentException("Attributes sharing a buffer require identical stride and frequency.", nameof(vertexDescriptions));
            descriptions[binding] = descriptor; ends[binding] = Math.Max(ends.GetValueOrDefault(binding), offset + bytes);
            attributes[i] = new() { Location = location, BufferSlot = binding, Format = format, Offset = offset };
        }
        var buffers = descriptions.OrderBy(p => p.Key).Select(p => p.Value).ToArray();
        for (var i = 0; i < buffers.Length; i++) if (buffers[i].Slot != i) throw new ArgumentException("Vertex buffer bindings must be contiguous from zero.", nameof(vertexDescriptions));
        var id = NextFormatID(ref _nextGraphicsFormat); _vertexFormats.Add(id, new(attributes, buffers, buffers.Select(b => ends[b.Slot]).ToArray())); return id;
    }
    private VertexLayout VertexFormat(int format) => _vertexFormats.TryGetValue(format, out var layout) ? layout : throw new ArgumentException("Unknown vertex format.", nameof(format));
    private RID GraphicsBuffer(uint size, ReadOnlySpan<byte> data, Kind kind, BufferCreationBits creation, IndexBufferFormat index = default)
    {
        IdleList(); if (size == 0 || size > int.MaxValue || data.Length > size || (creation & ~(BufferCreationBits.AsStorage | BufferCreationBits.DeviceAddress)) != 0) throw new ArgumentOutOfRangeException(nameof(size));
        var usage = kind == Kind.Vertex ? SDL.GPUBufferUsageFlags.Vertex : SDL.GPUBufferUsageFlags.Index;
        if ((creation & BufferCreationBits.AsStorage) != 0) usage |= SDL.GPUBufferUsageFlags.GraphicsStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageWrite;
        var entry = new Entry { Kind = kind, Size = size, GraphicsStorage = (creation & BufferCreationBits.AsStorage) != 0, IndexFormat = index };
        try
        {
            entry.Handle = CreateBuffer(entry, usage, creation);
            entry.Upload = Transfer(size, SDL.GPUTransferBufferUsage.Upload); var bytes = new byte[size]; data.CopyTo(bytes); Upload(entry, 0, bytes); return Add(entry);
        }
        catch { entry.Handle?.Dispose(); entry.Upload?.Dispose(); throw; }
    }
    /// <summary>Creates a zero-filled vertex buffer with a copied initial prefix.</summary>
    /// <param name="sizeBytes">Positive byte capacity.</param>
    /// <param name="data">Initial byte prefix.</param>
    /// <param name="creationBits">Optional storage and stable device-address roles.</param>
    /// <returns>A device-owned buffer identity.</returns>
    public RID VertexBufferCreate(uint sizeBytes, ReadOnlySpan<byte> data = default, BufferCreationBits creationBits = 0) => GraphicsBuffer(sizeBytes, data, Kind.Vertex, creationBits);
    /// <summary>Creates a copied unsigned index buffer.</summary>
    /// <param name="sizeIndices">Positive index capacity.</param>
    /// <param name="format">Unsigned 16 or 32-bit indices.</param>
    /// <param name="data">Initial byte prefix.</param>
    /// <param name="useRestartIndices">Enables maximum unsigned index values as strip restart markers.</param>
    /// <param name="creationBits">Optional storage and stable device-address roles.</param>
    /// <returns>A device-owned index buffer identity.</returns>
    public RID IndexBufferCreate(uint sizeIndices, IndexBufferFormat format, ReadOnlySpan<byte> data = default, bool useRestartIndices = false, BufferCreationBits creationBits = 0)
    { if (!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format)); var rid = GraphicsBuffer(checked(sizeIndices * (format == IndexBufferFormat.UInt16 ? 2u : 4u)), data, Kind.Index, creationBits, format); Get(rid).RestartIndices = useRestartIndices; return rid; }
    /// <summary>Creates a borrowed vertex-buffer array.</summary>
    /// <param name="vertexCount">Number of available vertices.</param>
    /// <param name="vertexFormat">A live vertex layout.</param>
    /// <param name="sourceBuffers">One device-owned vertex buffer per layout binding.</param>
    /// <param name="offsets">Optional byte offset per buffer.</param>
    /// <returns>A device-owned array identity invalidated by its source buffers.</returns>
    public RID VertexArrayCreate(uint vertexCount, int vertexFormat, ReadOnlySpan<RID> sourceBuffers, ReadOnlySpan<uint> offsets = default)
    {
        IdleList(); var layout = VertexFormat(vertexFormat);
        if (vertexCount == 0 || sourceBuffers.Length != layout.Buffers.Length || !offsets.IsEmpty && offsets.Length != sourceBuffers.Length) throw new ArgumentException("Vertex array dimensions do not match the format.");
        var actualOffsets = offsets.IsEmpty ? new uint[sourceBuffers.Length] : offsets.ToArray();
        for (var i = 0; i < sourceBuffers.Length; i++)
        {
            var buffer = Get(sourceBuffers[i], Kind.Vertex); var count = layout.Buffers[i].InputRate == SDL.GPUVertexInputRate.Vertex ? vertexCount : 1;
            if ((ulong)actualOffsets[i] + (ulong)(count - 1) * layout.Buffers[i].Pitch + layout.Ends[i] > buffer.Size) throw new ArgumentException("The vertex array exceeds its buffer.");
        }
        return Add(new Entry { Kind = Kind.VertexArray, Vertices = new(vertexCount, vertexFormat, sourceBuffers.ToArray(), actualOffsets) });
    }
    /// <summary>Creates a borrowed index range.</summary>
    /// <param name="indexBuffer">A live index buffer.</param>
    /// <param name="indexOffset">First index.</param>
    /// <param name="indexCount">Positive index count.</param>
    /// <returns>A device-owned range identity invalidated by the source buffer.</returns>
    public RID IndexArrayCreate(RID indexBuffer, uint indexOffset, uint indexCount)
    {
        IdleList(); var buffer = Get(indexBuffer, Kind.Index); var bytes = buffer.IndexFormat == IndexBufferFormat.UInt16 ? 2u : 4u;
        if (indexCount == 0 || (ulong)indexOffset + indexCount > buffer.Size / bytes) throw new ArgumentOutOfRangeException(nameof(indexCount));
        return Add(new Entry { Kind = Kind.IndexArray, Indices = new(indexBuffer, checked(indexOffset * bytes), indexCount) });
    }
}
