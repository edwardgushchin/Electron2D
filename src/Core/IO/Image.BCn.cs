using System.Runtime.InteropServices;
using Electron2DBlockCodecs.Decoder;
using Electron2DBlockCodecs.Encoder;
using Electron2DBlockCodecs.Shared;
using Electron2DBufferTools;

namespace Electron2D;

public sealed partial class Image
{
    /// <summary>Compresses every stored mip level into a supported GPU block format.</summary>
    /// <param name="mode">S3TC or BPTC compression family.</param>
    /// <param name="source">Color/data or normal-map channel policy.</param>
    /// <param name="astcFormat">Validated ASTC footprint; ASTC encoding requires a separate codec.</param>
    /// <remarks>Preserves dimensions and existing mip levels. Unsupported families and codec budget failures
    /// preserve the previous image. Successful publication emits Changed outside the image locks.</remarks>
    /// <exception cref="NotSupportedException">The requested family has no installed codec.</exception>
    /// <exception cref="InvalidOperationException">The image is empty, already compressed or exceeds the codec work budget.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An enum value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void Compress(CompressMode mode, CompressSource source = CompressSource.Generic, ASTCFormat astcFormat = ASTCFormat.Format4X4)
    {
        ValidateBCCompression(mode, astcFormat);
        if (!Enum.IsDefined(source)) throw new ArgumentOutOfRangeException(nameof(source));
        Mutate(state => CompressBCPixels(state, mode, DetectBCChannels(state, source)));
    }
    /// <summary>Compresses stored mip levels with an explicit channel role.</summary>
    /// <param name="mode">S3TC or BPTC compression family.</param>
    /// <param name="channels">Channels that the encoder must retain.</param>
    /// <param name="astcFormat">Validated ASTC footprint; ASTC encoding requires a separate codec.</param>
    /// <remarks>Uses the same atomic publication, mip and work-budget rules as Compress.</remarks>
    /// <exception cref="NotSupportedException">The requested family has no installed codec.</exception>
    /// <exception cref="InvalidOperationException">The image is empty, already compressed or exceeds the codec work budget.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An enum value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void CompressFromChannels(CompressMode mode, UsedChannels channels, ASTCFormat astcFormat = ASTCFormat.Format4X4)
    {
        ValidateBCCompression(mode, astcFormat);
        if (!Enum.IsDefined(channels)) throw new ArgumentOutOfRangeException(nameof(channels));
        Mutate(state => CompressBCPixels(state, mode, channels));
    }
    /// <summary>Decompresses BC1–BC7 pixels while retaining dimensions and every stored mip level.</summary>
    /// <remarks>An uncompressed image is unchanged and emits no notification. Unsupported block families
    /// and decode failures preserve the previous image. Signed/unsigned BC6 becomes RGB floating-point pixels;
    /// other BC formats become RGBA8. Codec work is bounded before expanded pixel allocation.</remarks>
    /// <exception cref="NotSupportedException">The compressed format has no installed decoder.</exception>
    /// <exception cref="InvalidOperationException">Expanded pixels exceed the codec work budget.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void Decompress()
    {
        ThrowIfDisposed();
        lock (_mutationGate)
        {
            var state = Snapshot(); if (!IsCompressedFormat(state.Format)) return;
            SetStateWithoutNotification(DecompressBCPixels(state));
        }
        EmitChanged();
    }
    private static void ValidateBCCompression(CompressMode mode, ASTCFormat astcFormat)
    {
        if (mode < 0 || mode >= CompressMode.Max) throw new ArgumentOutOfRangeException(nameof(mode));
        if (!Enum.IsDefined(astcFormat)) throw new ArgumentOutOfRangeException(nameof(astcFormat));
        if (mode is not (CompressMode.S3tc or CompressMode.Bptc)) throw new NotSupportedException("This compression family requires another block codec.");
    }
    private static UsedChannels DetectBCChannels(State state, CompressSource source)
    {
        RequireReadablePixels(state);
        return DetectUsedChannels(state, source);
    }
    private static State CompressBCPixels(State state, CompressMode mode, UsedChannels channels)
    {
        RequireReadablePixels(state); CheckBCWork(state);
        var hdr = mode == CompressMode.Bptc && channels is not (UsedChannels.Rgba or UsedChannels.LuminanceAlpha) && state.Format is >= Format.Rf and <= Format.Rgbah;
        var format = mode == CompressMode.Bptc ? hdr ? HasNegativeBCPixels(state) ? Format.BptcRgbf : Format.BptcRgbfu : Format.BptcRgba
            : channels switch { UsedChannels.Red => Format.RgtcR, UsedChannels.RedGreen => Format.RgtcRg, UsedChannels.Rgba or UsedChannels.LuminanceAlpha => Format.Dxt5, _ => Format.Dxt1 };
        var encoder = new BcEncoder(format == Format.Dxt1 ? CompressionFormat.Bc1 : BCFormat(format)); encoder.Options.IsParallel = false; encoder.OutputOptions.GenerateMipMaps = false;
        encoder.OutputOptions.Quality = CompressionQuality.Balanced;
        var output = new byte[GetRequiredDataSize(state.Width, state.Height, format, state.HasMIPMaps)]; var offset = 0;
        var converted = ConvertState(state, hdr ? Format.Rgbf : Format.Rgba8);
        for (var mip = 0; mip <= GetRequiredMIPMapCount(state.Width, state.Height, state.HasMIPMaps); mip++)
        {
            var level = GetLevel(converted, mip); byte[] bytes;
            if (hdr)
            {
                var colors = MemoryMarshal.Cast<byte, ColorRgbFloat>(converted.Data.AsSpan(level.Offset, level.Length)).ToArray();
                bytes = encoder.EncodeToRawBytesHdr(colors.AsMemory().AsMemory2D(level.Height, level.Width))[0];
            }
            else bytes = encoder.EncodeToRawBytes(converted.Data.AsSpan(level.Offset, level.Length), level.Width, level.Height, Electron2DBlockCodecs.Encoder.PixelFormat.Rgba32)[0];
            if (bytes.Length != GetLevelByteCount(level.Width, level.Height, format)) throw new InvalidDataException("The block encoder returned an inconsistent mip size.");
            bytes.CopyTo(output, offset); offset += bytes.Length;
        }
        return new(state.Width, state.Height, format, state.HasMIPMaps, output);
    }
    internal static bool IsBCFormat(Format format) => format is >= Format.Dxt1 and <= Format.BptcRgbfu || format == Format.Dxt5RaAsRg;
    private static CompressionFormat BCFormat(Format format) => format switch
    {
        Format.Dxt1 => CompressionFormat.Bc1WithAlpha,
        Format.Dxt3 => CompressionFormat.Bc2,
        Format.Dxt5 or Format.Dxt5RaAsRg => CompressionFormat.Bc3,
        Format.RgtcR => CompressionFormat.Bc4,
        Format.RgtcRg => CompressionFormat.Bc5,
        Format.BptcRgba => CompressionFormat.Bc7,
        Format.BptcRgbf => CompressionFormat.Bc6S,
        Format.BptcRgbfu => CompressionFormat.Bc6U,
        _ => throw new NotSupportedException("This block format requires another decoder.")
    };
    internal static State DecompressBCPixels(State state)
    {
        var codec = BCFormat(state.Format); var hdr = state.Format is Format.BptcRgbf or Format.BptcRgbfu; var format = hdr ? Format.Rgbf : Format.Rgba8;
        CheckBCWork(state); var output = new byte[GetRequiredDataSize(state.Width, state.Height, format, state.HasMIPMaps)]; var offset = 0; var decoder = new BcDecoder(); decoder.Options.IsParallel = false;
        for (var mip = 0; mip <= GetRequiredMIPMapCount(state.Width, state.Height, state.HasMIPMaps); mip++)
        {
            var level = GetLevel(state, mip); var input = state.Data.AsSpan(level.Offset, level.Length).ToArray();
            if (hdr) MemoryMarshal.AsBytes(decoder.DecodeRawHdr(input, level.Width, level.Height, codec).AsSpan()).CopyTo(output.AsSpan(offset));
            else
            {
                var colors = decoder.DecodeRaw(input, level.Width, level.Height, codec); var bytes = MemoryMarshal.AsBytes(colors.AsSpan());
                if (state.Format == Format.Dxt5RaAsRg) for (var i = 0; i < colors.Length; i++) { colors[i].g = colors[i].a; colors[i].b = 0; colors[i].a = 255; }
                bytes.CopyTo(output.AsSpan(offset));
            }
            offset += GetLevelByteCount(level.Width, level.Height, format);
        }
        return new(state.Width, state.Height, format, state.HasMIPMaps, output);
    }
    private static void CheckBCWork(State state)
    {
        long pixels = 0; var width = state.Width; var height = state.Height;
        do { pixels += (long)width * height; if (!state.HasMIPMaps || width == 1 && height == 1) break; width = Math.Max(1, width / 2); height = Math.Max(1, height / 2); } while (true);
        if (state.Data.Length > MaximumEncodedBytes || pixels * 16 > MaximumEncodedBytes)
            throw new InvalidOperationException("Source and expanded codec pixels must each fit within sixty-four MiB.");
    }
    private static bool HasNegativeBCPixels(State state)
    {
        var values = ConvertState(state, Format.Rgbf); var pixels = MemoryMarshal.Cast<byte, float>(values.Data);
        var negative = false; foreach (var value in pixels) { if (!float.IsFinite(value)) throw new InvalidDataException("HDR block compression requires finite colors."); negative |= value < 0; }
        return negative;
    }
}
