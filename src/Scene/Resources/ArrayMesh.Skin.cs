using System.Buffers.Binary;

namespace Electron2D;

public sealed partial class ArrayMesh
{
    /// <summary>Updates copied bytes in the packed skin buffer, including partial records.</summary>
    /// <param name="surfaceIndex">Existing surface with bone/weight channels.</param>
    /// <param name="offset">Byte offset into 16-byte four-slot or 32-byte eight-slot records.</param>
    /// <param name="data">Little-endian uint16 indices followed by UNORM16 weights for each vertex.</param>
    /// <remarks>Retained mesh draws observe edits without rerecording; configured palette bounds are validated at draw time.</remarks>
    /// <exception cref="InvalidOperationException">No skin buffer exists.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The surface or byte region is invalid.</exception>
    public void SurfaceUpdateSkinRegion(int surfaceIndex, int offset, ReadOnlySpan<byte> data)
    {
        lock (Gate)
        {
            var surface = Get(surfaceIndex); var slots = surface.Data.SkinSlots; if (slots == 0) throw new InvalidOperationException("The surface has no skin buffer."); var stride = slots * 4; Region(offset, data.Length, surface.Data.Vertices.Length, stride); Span<byte> record = stackalloc byte[32]; var end = (long)offset + data.Length;
            for (var index = offset / stride; index * (long)stride < end; index++)
            {
                var buffer = record[..stride];
                for (var slot = 0; slot < slots; slot++) { var entry = index * slots + slot; BinaryPrimitives.WriteUInt16LittleEndian(buffer[(slot * 2)..], (ushort)surface.Data.Bones[entry]); BinaryPrimitives.WriteUInt16LittleEndian(buffer[(slots * 2 + slot * 2)..], EncodeWeight(surface.Data.Weights[entry])); }
                Overlay(buffer, index * (long)stride, offset, data);
                for (var slot = 0; slot < slots; slot++) { var entry = index * slots + slot; surface.Data.Bones[entry] = BinaryPrimitives.ReadUInt16LittleEndian(buffer[(slot * 2)..]); surface.Data.Weights[entry] = BinaryPrimitives.ReadUInt16LittleEndian(buffer[(slots * 2 + slot * 2)..]) / 65535f; }
            }
        }
        EmitChanged();
    }
    internal static ushort EncodeWeight(float value) => (ushort)Math.Clamp(Math.Round(value * 65535d), 0, 65535);
}
