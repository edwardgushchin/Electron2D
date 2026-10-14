using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2DBlockCodecs.Shared.ImageFiles;

namespace Electron2D;

public sealed partial class Image
{
    /// <summary>Loads the first image of a complete DDS source, preserving raw pixels and mip levels.</summary>
    /// <param name="buffer">Copied DDS bytes, bounded to sixty-four MiB.</param>
    /// <remarks>Supports BC1–BC7 and applicable raw color/HDR formats. Validates the complete payload before
    /// invoking the container reader. Arrays/cubes select their first face; volumes select the first slice at each mip. Failures preserve the previous image; observer failures retain publication.</remarks>
    /// <exception cref="InvalidDataException">The source is malformed, truncated or exceeds a source/dimension limit.</exception>
    /// <exception cref="NotSupportedException">The source format is unsupported.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadDDSFromBuffer(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        if (buffer.Length < 128 || buffer.Length > MaximumEncodedBytes) throw new InvalidDataException("DDS input must contain its header within sixty-four MiB.");
        var source = buffer.ToArray();
        State state;
        try { state = ReadDDS(source); }
        catch (OverflowException error) { throw new InvalidDataException("DDS layout exceeds the bounded image limits.", error); }
        Commit(state);
    }
    /// <summary>Saves this image's exact raw pixel and mip bytes in a DDS container.</summary>
    /// <returns>An independent DDS byte buffer bounded to sixty-four MiB.</returns>
    /// <remarks>Preserves BC payloads without recompression. Raw supported formats retain channel precision.</remarks>
    /// <exception cref="InvalidOperationException">The image is empty or encoded output exceeds the limit.</exception>
    /// <exception cref="NotSupportedException">The pixel format has no DDS representation.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public byte[] SaveDDSToBuffer()
    {
        var state = Snapshot(); if (state.Data.Length == 0) throw new InvalidOperationException("The image is empty.");
        if (state.Data.Length > MaximumEncodedBytes - 148) throw new InvalidOperationException("DDS output exceeds sixty-four MiB.");
        var format = DDSFormat(state.Format); DdsHeader header; DdsHeaderDx10 dx10;
        var legacy = state.Format is Format.Rgb8 or Format.Rgba4444 or Format.Rgb565 or Format.L8 or Format.La8;
        if (IsBCFormat(state.Format)) (header, dx10) = DdsHeader.InitializeCompressed(state.Width, state.Height, format, true);
        else
        {
            header = new DdsHeader
            {
                dwSize = 124,
                dwFlags = HeaderFlags.Required,
                dwWidth = (uint)state.Width,
                dwHeight = (uint)state.Height,
                dwDepth = 1,
                dwMipMapCount = 1,
                dwCaps = HeaderCaps.DdscapsTexture,
                ddsPixelFormat = new DdsPixelFormat { dwSize = 32, dwFlags = PixelFormatFlags.DdpfFourcc, dwFourCc = DdsPixelFormat.Dx10 }
            };
            dx10 = new() { dxgiFormat = format, arraySize = 1, resourceDimension = D3D10ResourceDimension.D3D10ResourceDimensionTexture2D };
        }
        if (legacy) { header.ddsPixelFormat = DDSLegacyPixels(state.Format); dx10 = default; }
        if (state.HasMIPMaps) header.dwFlags |= HeaderFlags.DdsdMipmapcount;
        if (IsBCFormat(state.Format)) { header.dwFlags |= HeaderFlags.DdsdLinearsize; header.dwPitchOrLinearSize = (uint)GetLevel(state, 0).Length; }
        else { header.dwFlags |= HeaderFlags.DdsdPitch; header.dwPitchOrLinearSize = (uint)(state.Width * GetBytesPerPixel(state.Format)); }
        var file = new DdsFile(header, dx10); var count = GetRequiredMIPMapCount(state.Width, state.Height, state.HasMIPMaps) + 1;
        var face = new DdsFace((uint)state.Width, (uint)state.Height, (uint)GetLevel(state, 0).Length, count);
        for (var mip = 0; mip < count; mip++) { var level = GetLevel(state, mip); face.MipMaps[mip] = new(DDSStoredPixels(state.Data.AsSpan(level.Offset, level.Length), state.Format), (uint)level.Width, (uint)level.Height); }
        file.Faces.Add(face); using var stream = new MemoryStream(); file.Write(stream); return stream.ToArray();
    }
    /// <summary>Atomically saves exact pixels and mip levels to a DDS file.</summary>
    /// <param name="path">An operating-system, res:// or user:// file path; parent directories must exist.</param>
    /// <exception cref="IOException">The destination cannot be replaced.</exception>
    /// <exception cref="InvalidOperationException">The image is empty or output exceeds the limit.</exception>
    /// <exception cref="NotSupportedException">The pixel format has no DDS representation.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void SaveDDS(string path) => SaveEncodedFile(path, SaveDDSToBuffer());
    private static State ReadDDS(byte[] source)
    {
        ReadOnlySpan<byte> input = source;
        if (input.Length < 128 || input.Length > MaximumEncodedBytes) throw new InvalidDataException("DDS input must contain its header within sixty-four MiB.");
        if (!BitConverter.IsLittleEndian) throw new NotSupportedException("The selected DDS container reader requires little-endian native records.");
        if (BinaryPrimitives.ReadUInt32LittleEndian(input) != 0x20534444) throw new InvalidDataException("The DDS signature is invalid.");
        var header = MemoryMarshal.Read<DdsHeader>(input[4..]);
        if (header.dwSize != 124 || header.ddsPixelFormat.dwSize != 32) throw new InvalidDataException("The DDS header sizes are invalid.");
        if (header.dwWidth > int.MaxValue || header.dwHeight > int.MaxValue) throw new InvalidDataException("DDS dimensions exceed the image limits.");
        var width = (int)header.dwWidth; var height = (int)header.dwHeight;
        try { ValidateDimensions(width, height); } catch (ArgumentOutOfRangeException error) { throw new InvalidDataException("DDS dimensions exceed the image limits.", error); }
        var dx10 = header.ddsPixelFormat.IsDxt10Format; var offset = dx10 ? 148 : 128;
        if (input.Length < offset) throw new InvalidDataException("The DDS extension header is truncated.");
        var extension = dx10 ? MemoryMarshal.Read<DdsHeaderDx10>(input[128..]) : default;
        var volume = (header.dwCaps2 & HeaderCaps2.Ddscaps2Volume) != 0 || dx10 && extension.resourceDimension == D3D10ResourceDimension.D3D10ResourceDimensionTexture3D;
        var cube = (header.dwCaps2 & HeaderCaps2.Ddscaps2Cubemap) != 0 || dx10 && (extension.miscFlag & 4) != 0;
        var layers = dx10 ? extension.arraySize : 1u;
        if (layers == 0 || layers > MaximumEncodedBytes || volume && (header.dwDepth == 0 || header.dwDepth > MaxWidth || layers != 1 || cube) ||
            dx10 && extension.resourceDimension is not (D3D10ResourceDimension.D3D10ResourceDimensionTexture2D or D3D10ResourceDimension.D3D10ResourceDimensionTexture3D))
            throw new InvalidDataException("DDS dimensions, layers or resource role are inconsistent.");
        if (cube) layers *= 6;
        var nativeFormat = dx10 ? extension.dxgiFormat : DDSFourCCFormat(header.ddsPixelFormat);
        var layout = !dx10 && (header.ddsPixelFormat.dwFlags & PixelFormatFlags.DdpfFourcc) == 0
            ? DDSLegacyLayout(header.ddsPixelFormat) : DDSNativeLayout(nativeFormat);
        var format = layout.Format;
        if (IsBCFormat(format) && (header.dwFlags & HeaderFlags.DdsdLinearsize) != 0 && header.dwPitchOrLinearSize != GetLevelByteCount(width, height, format))
            throw new InvalidDataException("DDS base-level linear size is inconsistent with its block layout.");
        var count = (header.dwFlags & HeaderFlags.DdsdMipmapcount) != 0 || (header.dwCaps & HeaderCaps.DdscapsMipmap) != 0 ? Math.Max(1, header.dwMipMapCount) : 1;
        if (count != 1 && count != GetRequiredMIPMapCount(width, height, true) + 1) throw new InvalidDataException("A DDS image must contain its complete mip chain.");
        int bytes; try { bytes = GetRequiredDataSize(width, height, format, count > 1); } catch (ArgumentOutOfRangeException error) { throw new InvalidDataException("DDS pixels exceed the managed image limit.", error); }
        if (bytes > MaximumEncodedBytes) throw new InvalidDataException("Decoded DDS pixels exceed sixty-four MiB.");
        long storedBytes = 0; var mipWidth = width; var mipHeight = height; var depth = volume ? header.dwDepth : 1u;
        for (var mip = 0; mip < count; mip++)
        {
            storedBytes = checked(storedBytes + (long)DDSLevelBytes(layout, mipWidth, mipHeight) * depth);
            mipWidth = Math.Max(1, mipWidth / 2); mipHeight = Math.Max(1, mipHeight / 2); depth = Math.Max(1, depth / 2);
        }
        storedBytes = checked(storedBytes * layers);
        if (storedBytes > MaximumEncodedBytes - offset || input.Length != offset + storedBytes) throw new InvalidDataException("The DDS payload length is inconsistent with its complete mip layout.");
        byte[] output;
        var standardReader = !volume && layers == 1 && layout.Conversion == DDSPixelConversion.Copy &&
            (count == 1 || (header.dwCaps & HeaderCaps.DdscapsMipmap) != 0) &&
            (dx10 || nativeFormat != DxgiFormat.DxgiFormatUnknown && header.ddsPixelFormat.DxgiFormat == nativeFormat);
        if (standardReader)
        {
            using var stream = new MemoryStream(source, false); var file = DdsFile.Load(stream); output = new byte[bytes]; var at = 0;
            foreach (var mip in file.Faces[0].MipMaps) { mip.Data.CopyTo(output, at); at += mip.Data.Length; }
            if (at != bytes) throw new InvalidDataException("The DDS reader returned an inconsistent mip layout.");
        }
        else
        {
            output = new byte[bytes]; var sourceOffset = offset; var destinationOffset = 0; mipWidth = width; mipHeight = height; depth = volume ? header.dwDepth : 1u;
            for (var mip = 0; mip < count; mip++)
            {
                var length = DDSLevelBytes(layout, mipWidth, mipHeight); var decodedLength = GetLevelByteCount(mipWidth, mipHeight, format);
                DecodeDDSPixels(input.Slice(sourceOffset, length), output.AsSpan(destinationOffset, decodedLength), layout);
                sourceOffset += checked(length * (int)depth); destinationOffset += decodedLength;
                mipWidth = Math.Max(1, mipWidth / 2); mipHeight = Math.Max(1, mipHeight / 2); depth = Math.Max(1, depth / 2);
            }
        }
        return new(width, height, format, count > 1, output);
    }
    private static DdsPixelFormat DDSLegacyPixels(Format format) => format switch
    {
        Format.Rgb8 => new() { dwSize = 32, dwFlags = PixelFormatFlags.DdpfRgb, dwRgbBitCount = 24, dwRBitMask = 0xFF, dwGBitMask = 0xFF00, dwBBitMask = 0xFF0000 },
        Format.Rgba4444 => new() { dwSize = 32, dwFlags = PixelFormatFlags.DdpfRgb | PixelFormatFlags.DdpfAlphaPixels, dwRgbBitCount = 16, dwRBitMask = 0xF00, dwGBitMask = 0xF0, dwBBitMask = 0xF, dwABitMask = 0xF000 },
        Format.Rgb565 => new() { dwSize = 32, dwFlags = PixelFormatFlags.DdpfRgb, dwRgbBitCount = 16, dwRBitMask = 0xF800, dwGBitMask = 0x7E0, dwBBitMask = 0x1F },
        Format.L8 => new() { dwSize = 32, dwFlags = PixelFormatFlags.DdpfLuminance, dwRgbBitCount = 8, dwRBitMask = 0xFF },
        Format.La8 => new() { dwSize = 32, dwFlags = PixelFormatFlags.DdpfLuminance | PixelFormatFlags.DdpfAlphaPixels, dwRgbBitCount = 16, dwRBitMask = 0xFF, dwABitMask = 0xFF00 },
        _ => throw new NotSupportedException("The raw pixel layout has no matching legacy DDS mask.")
    };
    private static byte[] DDSStoredPixels(ReadOnlySpan<byte> source, Format format, bool decode = false)
    {
        var bytes = source.ToArray();
        if (format == Format.Rgba4444) for (var i = 0; i < bytes.Length; i += 2)
            { var pixel = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(i)); BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i), (ushort)(decode ? (pixel << 4) | (pixel >> 12) : (pixel << 12) | (pixel >> 4))); }
        return bytes;
    }
    private static DxgiFormat DDSFormat(Format format) => format switch
    {
        Format.Rgb8 => DxgiFormat.DxgiFormatUnknown,
        Format.Rgba4444 => DxgiFormat.DxgiFormatB4G4R4A4Unorm,
        Format.Rgb565 => DxgiFormat.DxgiFormatB5G6R5Unorm,
        Format.R8 or Format.L8 => DxgiFormat.DxgiFormatR8Unorm,
        Format.Rg8 or Format.La8 => DxgiFormat.DxgiFormatR8G8Unorm,
        Format.Rgba8 => DxgiFormat.DxgiFormatR8G8B8A8Unorm,
        Format.Rf => DxgiFormat.DxgiFormatR32Float,
        Format.Rgf => DxgiFormat.DxgiFormatR32G32Float,
        Format.Rgbf => DxgiFormat.DxgiFormatR32G32B32Float,
        Format.Rgbaf => DxgiFormat.DxgiFormatR32G32B32A32Float,
        Format.Rh => DxgiFormat.DxgiFormatR16Float,
        Format.Rgh => DxgiFormat.DxgiFormatR16G16Float,
        Format.Rgbah => DxgiFormat.DxgiFormatR16G16B16A16Float,
        Format.R16 => DxgiFormat.DxgiFormatR16Unorm,
        Format.Rg16 => DxgiFormat.DxgiFormatR16G16Unorm,
        Format.Rgba16 => DxgiFormat.DxgiFormatR16G16B16A16Unorm,
        Format.R16I => DxgiFormat.DxgiFormatR16Uint,
        Format.Rg16I => DxgiFormat.DxgiFormatR16G16Uint,
        Format.Rgba16I => DxgiFormat.DxgiFormatR16G16B16A16Uint,
        Format.Rgbe9995 => DxgiFormat.DxgiFormatR9G9B9E5Sharedexp,
        Format.Dxt1 => DxgiFormat.DxgiFormatBc1Unorm,
        Format.Dxt3 => DxgiFormat.DxgiFormatBc2Unorm,
        Format.Dxt5 => DxgiFormat.DxgiFormatBc3Unorm,
        Format.RgtcR => DxgiFormat.DxgiFormatBc4Unorm,
        Format.RgtcRg => DxgiFormat.DxgiFormatBc5Unorm,
        Format.BptcRgba => DxgiFormat.DxgiFormatBc7Unorm,
        Format.BptcRgbf => DxgiFormat.DxgiFormatBc6HSf16,
        Format.BptcRgbfu => DxgiFormat.DxgiFormatBc6HUf16,
        _ => throw new NotSupportedException("The pixel format requires a different DDS storage mapping.")
    };
    private static Format ImageDDSFormat(DxgiFormat format) => format switch
    {
        DxgiFormat.DxgiFormatR8Unorm => Format.R8,
        DxgiFormat.DxgiFormatR8G8Unorm => Format.Rg8,
        DxgiFormat.DxgiFormatR8G8B8A8Unorm or DxgiFormat.DxgiFormatR8G8B8A8UnormSrgb or DxgiFormat.DxgiFormatB8G8R8A8Unorm or DxgiFormat.DxgiFormatB8G8R8A8UnormSrgb => Format.Rgba8,
        DxgiFormat.DxgiFormatR32Float => Format.Rf,
        DxgiFormat.DxgiFormatR32G32Float => Format.Rgf,
        DxgiFormat.DxgiFormatR32G32B32Float => Format.Rgbf,
        DxgiFormat.DxgiFormatR32G32B32A32Float => Format.Rgbaf,
        DxgiFormat.DxgiFormatR16Float => Format.Rh,
        DxgiFormat.DxgiFormatR16G16Float => Format.Rgh,
        DxgiFormat.DxgiFormatR16G16B16A16Float => Format.Rgbah,
        DxgiFormat.DxgiFormatR16Unorm => Format.R16,
        DxgiFormat.DxgiFormatR16G16Unorm => Format.Rg16,
        DxgiFormat.DxgiFormatR16G16B16A16Unorm => Format.Rgba16,
        DxgiFormat.DxgiFormatR16Uint => Format.R16I,
        DxgiFormat.DxgiFormatR16G16Uint => Format.Rg16I,
        DxgiFormat.DxgiFormatR16G16B16A16Uint => Format.Rgba16I,
        DxgiFormat.DxgiFormatR9G9B9E5Sharedexp => Format.Rgbe9995,
        DxgiFormat.DxgiFormatBc1Unorm or DxgiFormat.DxgiFormatBc1UnormSrgb => Format.Dxt1,
        DxgiFormat.DxgiFormatBc2Unorm or DxgiFormat.DxgiFormatBc2UnormSrgb => Format.Dxt3,
        DxgiFormat.DxgiFormatBc3Unorm or DxgiFormat.DxgiFormatBc3UnormSrgb => Format.Dxt5,
        DxgiFormat.DxgiFormatBc4Unorm => Format.RgtcR,
        DxgiFormat.DxgiFormatBc5Unorm => Format.RgtcRg,
        DxgiFormat.DxgiFormatBc7Unorm or DxgiFormat.DxgiFormatBc7UnormSrgb => Format.BptcRgba,
        DxgiFormat.DxgiFormatBc6HSf16 => Format.BptcRgbf,
        DxgiFormat.DxgiFormatBc6HUf16 => Format.BptcRgbfu,
        _ => throw new NotSupportedException("The DDS format requires a different portable pixel mapping.")
    };
}
