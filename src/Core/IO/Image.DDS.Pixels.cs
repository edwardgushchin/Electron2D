using System.Buffers.Binary;
using Electron2DBlockCodecs.Shared.ImageFiles;

namespace Electron2D;

public sealed partial class Image
{
    private enum DDSPixelConversion { Copy, Bgr, Rgbx, Bgrx, Rgba4444, Rgba5551, Rgb332, Rgba3328, Rgb10A2, Bgr10A2, La4 }
    private readonly record struct DDSPixelLayout(Format Format, int StoredBytes, DDSPixelConversion Conversion = DDSPixelConversion.Copy);
    private static int DDSLevelBytes(DDSPixelLayout layout, int width, int height) => layout.StoredBytes == 0 ? GetLevelByteCount(width, height, layout.Format) : checked(width * height * layout.StoredBytes);
    private static DxgiFormat DDSFourCCFormat(DdsPixelFormat pixels) => pixels.dwFourCc switch
    {
        36 => DxgiFormat.DxgiFormatR16G16B16A16Unorm,
        111 => DxgiFormat.DxgiFormatR16Float,
        112 => DxgiFormat.DxgiFormatR16G16Float,
        113 => DxgiFormat.DxgiFormatR16G16B16A16Float,
        114 => DxgiFormat.DxgiFormatR32Float,
        115 => DxgiFormat.DxgiFormatR32G32Float,
        116 => DxgiFormat.DxgiFormatR32G32B32A32Float,
        0x59583241 => DxgiFormat.DxgiFormatBc5Unorm,
        _ when pixels.dwFourCc == DdsPixelFormat.Bc4S || pixels.dwFourCc == DdsPixelFormat.Bc5S => throw new NotSupportedException("Signed RGTC has no matching portable image storage role."),
        _ => pixels.DxgiFormat
    };
    private static DDSPixelLayout DDSNativeLayout(DxgiFormat format) => format switch
    {
        DxgiFormat.DxgiFormatB4G4R4A4Unorm => new(Format.Rgba4444, 2, DDSPixelConversion.Rgba4444),
        DxgiFormat.DxgiFormatB5G6R5Unorm => new(Format.Rgb565, 2),
        DxgiFormat.DxgiFormatB5G5R5A1Unorm => new(Format.Rgba8, 2, DDSPixelConversion.Rgba5551),
        DxgiFormat.DxgiFormatR10G10B10A2Unorm => new(Format.Rgba8, 4, DDSPixelConversion.Rgb10A2),
        DxgiFormat.DxgiFormatA8Unorm => new(Format.L8, 1),
        DxgiFormat.DxgiFormatB8G8R8A8Unorm or DxgiFormat.DxgiFormatB8G8R8A8UnormSrgb => new(Format.Rgba8, 4, DDSPixelConversion.Bgr),
        DxgiFormat.DxgiFormatB8G8R8X8Unorm or DxgiFormat.DxgiFormatB8G8R8X8UnormSrgb => new(Format.Rgb8, 4, DDSPixelConversion.Bgrx),
        _ => new(ImageDDSFormat(format), 0)
    };
    private static DDSPixelLayout DDSLegacyLayout(DdsPixelFormat p)
    {
        var alpha = (p.dwFlags & PixelFormatFlags.DdpfAlphaPixels) != 0;
        var rgb = (p.dwFlags & PixelFormatFlags.DdpfRgb) != 0;
        var bits = p.dwRgbBitCount; var r = p.dwRBitMask; var g = p.dwGBitMask; var b = p.dwBBitMask; var a = p.dwABitMask;
        if (rgb)
        {
            if (bits == 32 && r == 0xFFFF && g == 0xFFFF0000) return new(Format.Rg16, 4);
            if (alpha)
            {
                if (bits == 32 && g == 0xFF00 && a == 0xFF000000)
                {
                    if (r == 0xFF && b == 0xFF0000) return new(Format.Rgba8, 4);
                    if (r == 0xFF0000 && b == 0xFF) return new(Format.Rgba8, 4, DDSPixelConversion.Bgr);
                }
                if (bits == 16 && r == 0x7C00 && g == 0x3E0 && b == 0x1F && a == 0x8000) return new(Format.Rgba8, 2, DDSPixelConversion.Rgba5551);
                if (bits == 16 && r == 0xF00 && g == 0xF0 && b == 0xF && a == 0xF000) return new(Format.Rgba4444, 2, DDSPixelConversion.Rgba4444);
                if (bits == 16 && r == 0xE0 && g == 0x1C && b == 3 && a == 0xFF00) return new(Format.Rgba8, 2, DDSPixelConversion.Rgba3328);
                if (bits == 32 && g == 0xFFC00 && a == 0xC0000000)
                {
                    if (r == 0x3FF && b == 0x3FF00000) return new(Format.Rgba8, 4, DDSPixelConversion.Rgb10A2);
                    if (r == 0x3FF00000 && b == 0x3FF) return new(Format.Rgba8, 4, DDSPixelConversion.Bgr10A2);
                }
            }
            else
            {
                if (bits is 24 or 32 && g == 0xFF00)
                {
                    if (r == 0xFF && b == 0xFF0000) return new(Format.Rgb8, (int)bits / 8, bits == 24 ? DDSPixelConversion.Copy : DDSPixelConversion.Rgbx);
                    if (r == 0xFF0000 && b == 0xFF) return new(Format.Rgb8, (int)bits / 8, bits == 24 ? DDSPixelConversion.Bgr : DDSPixelConversion.Bgrx);
                }
                if (bits == 16 && r == 0xF800 && g == 0x7E0 && b == 0x1F) return new(Format.Rgb565, 2);
                if (bits == 8 && r == 0xE0 && g == 0x1C && b == 3) return new(Format.Rgb8, 1, DDSPixelConversion.Rgb332);
                if (bits == 8 && r == 0xFF && g == 0 && b == 0) return new(Format.R8, 1);
                if (bits == 16 && r == 0xFF && g == 0xFF00 && b == 0) return new(Format.Rg8, 2);
            }
        }
        if ((p.dwFlags & PixelFormatFlags.DdpfAlpha) != 0 && bits == 8 && a == 0xFF) return new(Format.L8, 1);
        if (alpha)
        {
            if (bits == 16 && r == 0xFF && a == 0xFF00) return new(Format.La8, 2);
            if (bits == 8 && r == 0xF && a == 0xF0) return new(Format.La8, 1, DDSPixelConversion.La4);
        }
        else
        {
            if (bits == 8 && r == 0xFF) return new(Format.L8, 1);
            if (bits == 16 && r == 0xFFFF) return new(Format.R16, 2);
        }
        throw new NotSupportedException("The legacy DDS color layout is unsupported.");
    }
    private static void DecodeDDSPixels(ReadOnlySpan<byte> input, Span<byte> output, DDSPixelLayout layout)
    {
        if (layout.Conversion == DDSPixelConversion.Copy) { input.CopyTo(output); return; }
        var outputBytes = GetBytesPerPixel(layout.Format);
        for (var i = 0; i < output.Length / outputBytes; i++)
        {
            var src = input.Slice(i * layout.StoredBytes, layout.StoredBytes); var dst = output.Slice(i * outputBytes, outputBytes);
            switch (layout.Conversion)
            {
                case DDSPixelConversion.Bgr:
                case DDSPixelConversion.Bgrx:
                    dst[0] = src[2]; dst[1] = src[1]; dst[2] = src[0]; if (outputBytes == 4) dst[3] = src[3]; break;
                case DDSPixelConversion.Rgbx: src[..3].CopyTo(dst); break;
                case DDSPixelConversion.Rgba4444:
                    var packed = BinaryPrimitives.ReadUInt16LittleEndian(src); BinaryPrimitives.WriteUInt16LittleEndian(dst, (ushort)((packed << 4) | (packed >> 12))); break;
                case DDSPixelConversion.Rgba5551:
                    var v = BinaryPrimitives.ReadUInt16LittleEndian(src); dst[0] = (byte)((v >> 10 & 31) << 3); dst[1] = (byte)((v >> 5 & 31) << 3); dst[2] = (byte)((v & 31) << 3); dst[3] = (v & 0x8000) != 0 ? (byte)255 : (byte)0; break;
                case DDSPixelConversion.Rgb332:
                case DDSPixelConversion.Rgba3328:
                    dst[0] = (byte)(src[0] & 0xE0); dst[1] = (byte)((src[0] & 0x1C) << 3); dst[2] = (byte)((src[0] & 3) << 6); if (outputBytes == 4) dst[3] = src[1]; break;
                case DDSPixelConversion.Rgb10A2:
                case DDSPixelConversion.Bgr10A2:
                    var value = BinaryPrimitives.ReadUInt32LittleEndian(src); var red = (byte)((value & 0x3FF) >> 2); var blue = (byte)((value >> 20 & 0x3FF) >> 2);
                    dst[0] = layout.Conversion == DDSPixelConversion.Rgb10A2 ? red : blue; dst[1] = (byte)((value >> 10 & 0x3FF) >> 2); dst[2] = layout.Conversion == DDSPixelConversion.Rgb10A2 ? blue : red;
                    dst[3] = value >> 30 == 3 ? (byte)255 : (byte)((value >> 30) << 6); break;
                case DDSPixelConversion.La4: dst[0] = (byte)((src[0] & 15) * 17); dst[1] = (byte)((src[0] >> 4) * 17); break;
            }
        }
    }
}
