using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class Image
{
    private static State CompressNativePixels(State state, CompressMode mode, UsedChannels channels, ASTCFormat astcFormat)
    {
        if (mode is CompressMode.S3tc or CompressMode.Bptc) return CompressBCPixels(state, mode, channels);
        RequireReadablePixels(state); CheckCodecWork(state);
        var hdr = mode == CompressMode.Astc && state.Format is >= Format.Rf and <= Format.Rgbe9995;
        var format = mode switch
        {
            CompressMode.Etc => Format.Etc,
            CompressMode.Etc2 => channels switch { UsedChannels.Red => Format.Etc2R11, UsedChannels.RedGreen => Format.Etc2Rg11, UsedChannels.Rgba or UsedChannels.LuminanceAlpha => Format.Etc2Rgba8, _ => Format.Etc2Rgb8 },
            _ => astcFormat == ASTCFormat.Format4X4 ? hdr ? Format.Astc4X4Hdr : Format.Astc4X4 : hdr ? Format.Astc8X8Hdr : Format.Astc8X8
        };
        var kind = format switch { Format.Etc or Format.Etc2Rgb8 => 1u, Format.Etc2Rgba8 => 2u, Format.Etc2R11 => 3u, Format.Etc2Rg11 => 4u, Format.Astc4X4 or Format.Astc4X4Hdr => 5u, _ => 6u };
        var data = EncodeNativePixels(state, kind, hdr, channels is UsedChannels.Red or UsedChannels.RedGreen, 2, 0);
        if (data.Length != GetRequiredDataSize(state.Width, state.Height, format, state.HasMIPMaps)) throw new InvalidDataException("The block encoder returned an inconsistent mip-chain size.");
        return new(state.Width, state.Height, format, state.HasMIPMaps, data);
    }

    internal byte[] EncodeBasis(int quality, float rdo, bool normal)
    {
        return Read(state =>
        {
            RequireReadablePixels(state); CheckCodecWork(state);
            return EncodeNativePixels(state, 0, state.Format is >= Format.Rf and <= Format.Rgbe9995, normal, quality, rdo);
        });
    }

    private static unsafe byte[] EncodeNativePixels(State source, uint kind, bool hdr, bool normal, int quality, float rdo)
    {
        var state = ConvertState(source, hdr ? Format.Rgbaf : Format.Rgba8);
        fixed (byte* input = state.Data)
        {
            var status = NativeImageEncode(kind, (uint)state.Width, (uint)state.Height,
                (uint)(1 + GetRequiredMIPMapCount(state.Width, state.Height, state.HasMIPMaps)), hdr ? 1u : 0u, normal ? 1u : 0u,
                (uint)quality, rdo, input, (ulong)state.Data.Length, out var output, out var length);
            try
            {
                if (status == -3) throw new IOException("The native image encoder could not allocate working memory.");
                if (status != 0 || output == 0 || length is 0 or > MaximumEncodedBytes) throw new InvalidDataException("The image encoder rejected its input or output budget.");
                var result = new byte[(int)length]; Marshal.Copy(output, result, 0, result.Length); return result;
            }
            finally { NativeImageEncodedFree(output); }
        }
    }
    [LibraryImport(NativeLibraries.KTXLibrary, EntryPoint = "e2d_image_encode")]
    private static unsafe partial int NativeImageEncode(uint kind, uint width, uint height, uint levels,
        uint hdr, uint normal, uint quality, float rdo, byte* source, ulong sourceSize, out nint output, out ulong outputSize);
    [LibraryImport(NativeLibraries.KTXLibrary, EntryPoint = "e2d_image_encoded_free")]
    private static partial void NativeImageEncodedFree(nint output);
}
