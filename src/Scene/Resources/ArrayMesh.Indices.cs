using System.Buffers.Binary;

namespace Electron2D;

public sealed partial class ArrayMesh
{
    internal static int IndexStride(int vertexCount) => vertexCount is > 0 and <= 65536 ? 2 : 4;

    internal void UpdateIndexRegion(int surfaceIndex, int offset, ReadOnlySpan<byte> data)
    {
        lock (Gate)
        {
            var surface = Get(surfaceIndex); var indices = surface.Data.Indices;
            if (indices.Length == 0) throw new InvalidOperationException("The surface has no index buffer.");
            var count = surface.Data.VertexCount; var stride = IndexStride(count);
            Region(offset, data.Length, indices.Length, stride);
            Span<byte> record = stackalloc byte[4]; var end = (long)offset + data.Length;
            for (var pass = 0; pass < 2; pass++)
                for (var index = offset / stride; index * (long)stride < end; index++)
                {
                    var bytes = record[..stride];
                    if (stride == 2) BinaryPrimitives.WriteUInt16LittleEndian(bytes, (ushort)indices[index]);
                    else BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)indices[index]);
                    Overlay(bytes, index * (long)stride, offset, data);
                    var next = stride == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt32LittleEndian(bytes);
                    if (count != 0 && next >= count) throw new ArgumentOutOfRangeException(nameof(data), "Every index must address an existing vertex.");
                    if (pass != 0) indices[index] = (int)next;
                }
        }
        EmitChanged();
    }
}
