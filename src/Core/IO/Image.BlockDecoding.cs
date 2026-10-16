using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class Image
{
    internal static unsafe State DecompressPixels(State state)
    {
        if (IsBCFormat(state.Format)) return DecodeBCPixels(state);
        var code = state.Format switch
        {
            Format.Etc => 0,
            Format.Etc2Rgb8 => 1,
            Format.Etc2Rgba8 or Format.Etc2RaAsRg => 2,
            Format.Etc2Rgb8A1 => 3,
            Format.Etc2R11 => 4,
            Format.Etc2R11S => 5,
            Format.Etc2Rg11 => 6,
            Format.Etc2Rg11S => 7,
            Format.Astc4X4 => 8,
            Format.Astc4X4Hdr => 9,
            Format.Astc8X8 => 10,
            Format.Astc8X8Hdr => 11,
            _ => throw new NotSupportedException("This pixel format is not block-compressed.")
        };
        CheckCodecWork(state);
        var format = code is 5 or 7 ? Format.Rgbaf : code is 9 or 11 ? Format.Rgbah : Format.Rgba8;
        var output = new byte[GetRequiredDataSize(state.Width, state.Height, format, state.HasMIPMaps)];
        var offset = 0;
        for (var mip = 0; mip <= GetRequiredMIPMapCount(state.Width, state.Height, state.HasMIPMaps); mip++)
        {
            var level = GetLevel(state, mip); var bytes = GetLevelByteCount(level.Width, level.Height, format);
            fixed (byte* input = state.Data)
            fixed (byte* destination = output)
            {
                var status = DecodeNativeBlock((uint)code, (uint)level.Width, (uint)level.Height,
                    input + level.Offset, (ulong)level.Length, destination + offset, (ulong)bytes);
                if (status == -3) throw new IOException("The block decoder could not allocate working memory.");
                if (status != 0) throw new InvalidDataException("The compressed pixel blocks are invalid.");
            }
            offset += bytes;
        }
        if (state.Format == Format.Etc2RaAsRg)
            for (var i = 0; i < output.Length; i += 4) { output[i + 1] = output[i + 3]; output[i + 2] = 0; output[i + 3] = 255; }
        return new(state.Width, state.Height, format, state.HasMIPMaps, output);
    }

    [LibraryImport(NativeLibraries.KTXLibrary, EntryPoint = "e2d_block_decode")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int DecodeNativeBlock(uint format, uint width, uint height, byte* source,
        ulong sourceSize, byte* output, ulong outputSize);
}
