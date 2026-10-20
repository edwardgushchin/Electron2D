using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private const uint TexelHeaderSize = 32;
    private readonly record struct TexelLayout(DataFormat Format, uint Bytes, uint Widths, uint Offsets, uint Encoding, int PixelType);

    private static TexelLayout TexelEncoding(DataFormat format)
    {
        if (!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format));
        if (format == DataFormat.E5B9G9R9UFLOATPACK32) return new(format, 4, 0x00090909, 0x00120900, 8, 0);
        var name = format.ToString(); var packed = name.IndexOf("PACK", StringComparison.Ordinal);
        var end = packed < 0 ? name.Length : packed;
        Span<uint> widths = stackalloc uint[4]; Span<uint> offsets = stackalloc uint[4];
        uint bits = 0; var cursor = 0;
        while (cursor < end && "RGBA".IndexOf(name[cursor]) is var lane && lane >= 0)
        {
            cursor++; var start = cursor;
            while (cursor < end && char.IsAsciiDigit(name[cursor])) cursor++;
            if (cursor == start || !uint.TryParse(name.AsSpan(start, cursor - start), out var width) || width is 0 or > 32 || widths[lane] != 0)
                throw new NotSupportedException("The texel format requires an unsupported component precision or layout.");
            widths[lane] = width; offsets[lane] = bits; bits += width;
        }
        var kind = name.AsSpan(cursor, end - cursor) switch
        {
            "UNORM" => 0u,
            "SNORM" => 1u,
            "USCALED" => 2u,
            "SSCALED" => 3u,
            "UINT" => 4u,
            "SINT" => 5u,
            "SFLOAT" => 6u,
            "UFLOAT" => 7u,
            "SRGB" => 9u,
            _ => throw new NotSupportedException("The format is not an uncompressed portable texel encoding.")
        };
        if (bits == 0 || bits % 8 != 0 || bits > 128 || packed >= 0 && (bits > 32 || !uint.TryParse(name.AsSpan(packed + 4), out var storage) || storage != bits))
            throw new NotSupportedException("The format has no portable texel-buffer layout.");
        uint widthWord = 0, offsetWord = 0;
        for (var lane = 0; lane < 4; lane++)
        {
            if (widths[lane] != 0 && packed >= 0) offsets[lane] = bits - offsets[lane] - widths[lane];
            widthWord |= widths[lane] << (lane * 8); offsetWord |= offsets[lane] << (lane * 8);
        }
        return new(format, bits / 8, widthWord, offsetWord, kind, kind == 4 ? 2 : kind == 5 ? 1 : 0);
    }

    /// <summary>Creates a zero-initialized, formatted GPU texel buffer.</summary>
    /// <param name="sizeElements">Positive number of formatted elements, rather than a byte count.</param>
    /// <param name="format">Uncompressed color encoding with at most 32 bits per component.</param>
    /// <param name="data">Empty, or exactly the complete encoded element data; copied before returning.</param>
    /// <returns>A device-owned buffer identity usable by typed shader descriptors and ordinary buffer transfers.</returns>
    /// <remarks>Indexed shader reads and compute writes share the original encoded bytes. Internal format metadata is excluded
    /// from public byte offsets and readback. Creation is cold owner-thread work; end an active command list first.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The format value, element count or encoded capacity is invalid.</exception>
    /// <exception cref="ArgumentException">Nonempty data does not cover the complete formatted buffer.</exception>
    /// <exception cref="NotSupportedException">The format requires a compressed, depth, planar or wider-precision adapter.</exception>
    public RID TextureBufferCreate(uint sizeElements, DataFormat format, ReadOnlySpan<byte> data = default)
    {
        IdleList(); var layout = TexelEncoding(format);
        if (sizeElements == 0 || (ulong)sizeElements * layout.Bytes > int.MaxValue - TexelHeaderSize - 3) throw new ArgumentOutOfRangeException(nameof(sizeElements));
        var size = sizeElements * layout.Bytes;
        if (!data.IsEmpty && data.Length != size) throw new ArgumentException("Initial texel data must cover the complete formatted buffer.", nameof(data));
        var physical = (size + TexelHeaderSize + 3) & ~3u;
        var entry = new Entry { Kind = Kind.Texel, Size = size, BufferOffset = TexelHeaderSize, Texel = layout };
        try
        {
            entry.Handle = new(SDL.CreateGPUBuffer(Device, new SDL.GPUBufferCreateInfo { Size = physical, Usage = SDL.GPUBufferUsageFlags.ComputeStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageWrite | SDL.GPUBufferUsageFlags.GraphicsStorageRead }), h => SDL.ReleaseGPUBuffer(Device, h), _context.Handle);
            entry.Upload = Transfer(physical, SDL.GPUTransferBufferUsage.Upload);
            var initial = new byte[physical]; var header = MemoryMarshal.Cast<byte, uint>(initial.AsSpan(0, (int)TexelHeaderSize));
            header[0] = sizeElements; header[1] = layout.Bytes; header[2] = layout.Widths; header[3] = layout.Offsets; header[4] = layout.Encoding;
            data.CopyTo(initial.AsSpan((int)TexelHeaderSize)); UploadRaw(entry, 0, initial);
            return Add(entry);
        }
        catch { entry.Handle?.Dispose(); entry.Upload?.Dispose(); throw; }
    }

    private static bool TexelBinding(UniformType type) => type is UniformType.TextureBuffer or UniformType.SamplerWithTextureBuffer or UniformType.ImageBuffer;
    private static bool StorageBinding(UniformType type) => type == UniformType.StorageBuffer || TexelBinding(type);
    private static int BufferID(UniformType type) => type == UniformType.SamplerWithTextureBuffer ? 1 : 0;
}
