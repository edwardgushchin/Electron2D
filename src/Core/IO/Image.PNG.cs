using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class Image
{
    private static unsafe State DecodePNG(byte[] bytes, Vector2i size)
    {
        NativeAllocation.Prepare();
        byte* pixels = null;
        try
        {
            int channels;
            fixed (byte* input = bytes)
            {
                var status = DecodePNGNative(input, (nuint)bytes.Length, size.X, size.Y, &pixels, &channels);
                if (status == -2)
                    throw new IOException("The PNG decoder could not allocate working memory.");
                if (status != 0)
                    throw new InvalidDataException("Invalid PNG data or decoded pixels exceed 64 MiB.");
            }
            var format = channels switch
            {
                1 => Format.L8,
                2 => Format.La8,
                3 => Format.Rgb8,
                4 => Format.Rgba8,
                _ => throw new InvalidDataException("Invalid PNG channel count.")
            };
            var length = GetRequiredDataSize(size.X, size.Y, format, false);
            return new(size.X, size.Y, format, false, new ReadOnlySpan<byte>(pixels, length).ToArray());
        }
        finally { NativeAllocation.Free(pixels); }
    }

    private unsafe byte[] EncodePNG()
    {
        var state = CopyBaseLevelForEncoding("PNG");
        var format = state.Format is Format.L8 or Format.La8 or Format.Rgb8 or Format.Rgba8
            ? state.Format : DetectAlpha(state) == AlphaMode.None ? Format.Rgb8 : Format.Rgba8;
        var channels = GetBytesPerPixel(format);
        if ((long)state.Width * state.Height * channels > MaximumEncodedBytes)
            throw new IOException("PNG input pixels exceed 64 MiB.");
        state = ConvertState(state, format);

        NativeAllocation.Prepare();
        byte* bytes = null;
        try
        {
            nuint length;
            fixed (byte* pixels = state.Data)
                if (EncodePNGNative(pixels, state.Width, state.Height, channels, &bytes, &length) != 0)
                    throw new IOException("PNG encoding failed or exceeded 64 MiB.");
            return new ReadOnlySpan<byte>(bytes, checked((int)length)).ToArray();
        }
        finally { NativeAllocation.Free(bytes); }
    }

    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_png_decode")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int DecodePNGNative(byte* input, nuint length, int width, int height, byte** pixels, int* channels);

    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_png_encode")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int EncodePNGNative(byte* pixels, int width, int height, int channels, byte** output, nuint* length);
}
