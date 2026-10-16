using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Electron2D;

public sealed partial class Image
{
    /// <summary>Loads a KTX or KTX2 image, retaining its complete mip chain and channel precision.</summary>
    /// <param name="buffer">Copied container bytes, bounded to sixty-four MiB.</param>
    /// <remarks>Accepts a flat image with one base level or a complete mip chain. Arrays, cubes and volumes
    /// are rejected. Basis Universal LDR and HDR data is transcoded to portable RGBA8 and RGB half pixels.
    /// Raw block formats retain their source bytes unless orientation or swizzle requires decoding.
    /// Uncompressed RGB8 sRGB content is converted to linear pixels. Source, raw output and conservative
    /// RGBA-float expansion are each bounded to sixty-four MiB. Key/value metadata is bounded to one MiB and 4096 entries. Failure preserves the previous image;
    /// success emits Changed after publication.</remarks>
    /// <exception cref="InvalidDataException">The container is malformed, incomplete or exceeds image limits.</exception>
    /// <exception cref="NotSupportedException">The image role, storage format or required block transformation is unsupported.</exception>
    /// <exception cref="IOException">The native decoder cannot allocate its working data.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadKTXFromBuffer(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        if (buffer.Length is < 64 or > MaximumEncodedBytes)
            throw new InvalidDataException("KTX input must contain its complete header within sixty-four MiB.");
        var source = buffer.ToArray();
        Commit(ReadKTX(source));
    }

    private static State ReadKTX(byte[] source) => ReadNativeTexture(source, legacyBasis: false, requireBasis: false, linearize: true);

    private static unsafe State ReadNativeTexture(byte[] source, bool legacyBasis, bool requireBasis, bool linearize)
    {
        KTXInfo info;
        nint pointer;
        fixed (byte* input = source)
        {
            var status = legacyBasis ? BasisCreate(input, (ulong)source.Length, out info, out pointer) :
                KTXCreate(input, (ulong)source.Length, out info, out pointer);
            if (status == -2) throw new NotSupportedException("KTX arrays, cubes and volumes are not flat images.");
            if (status is -3 or 13) throw new IOException("KTX decoding could not allocate its working data.");
            if (status is 16 or 17) throw new NotSupportedException("The KTX codec does not support the declared storage feature.");
            if (status != 0) throw new InvalidDataException($"KTX decoding failed with codec status {status}.");
        }
        using var texture = new KTXHandle(pointer);
        if (requireBasis && (info.Version != 2 || (info.Flags & 8) == 0))
            throw new InvalidDataException("Imported Basis data requires a transcodable image.");
        var format = KTXFormat(info.Version, info.Format);
        var width = checked((int)info.Width); var height = checked((int)info.Height);
        ValidateDimensions(width, height);
        var mipmaps = info.Levels > 1;
        if (info.Levels != GetRequiredMIPMapCount(width, height, mipmaps) + 1)
            throw new InvalidDataException("KTX mip levels are incomplete.");
        var length = GetRequiredDataSize(width, height, format, mipmaps);
        if (length > MaximumEncodedBytes) throw new InvalidDataException("KTX pixels exceed sixty-four MiB.");
        var state = new State(width, height, format, mipmaps, new byte[length]);
        fixed (byte* output = state.Data)
        {
            for (var mip = 0; mip < info.Levels; ++mip)
            {
                var level = GetLevel(state, mip);
                var rows = IsCompressedFormat(format) ? (level.Height + GetCompressedBlock(format).Height - 1) / GetCompressedBlock(format).Height : level.Height;
                if (KTXLevel(texture, (uint)mip, output + level.Offset, (ulong)level.Length, (uint)(level.Length / rows), (uint)rows) != 0)
                    throw new InvalidDataException("KTX image bytes disagree with the declared pixel layout.");
            }
        }
        if (info.Swizzle != 0x61626772 || (info.Flags & 3) != 0)
        {
            if (IsCompressedFormat(format)) state = DecompressPixels(state);
            if ((info.Flags & 1) != 0) state = FlipState(state, horizontal: true);
            if ((info.Flags & 2) != 0) state = FlipState(state, horizontal: false);
            if (info.Swizzle != 0x61626772) state = SwizzleKTX(state, info.Swizzle);
        }
        if (linearize && (info.Flags & 4) != 0 && state.Format is Format.Rgb8 or Format.Rgba8)
        {
            var stride = GetBytesPerPixel(state.Format);
            for (var offset = 0; offset < state.Data.Length; offset += stride)
                WriteColor(state.Data, offset, state.Format, ReadColor(state.Data, offset, state.Format).SRGBToLinear());
        }
        return state;
    }

    private static State SwizzleKTX(State source, uint swizzle)
    {
        Span<byte> channels = stackalloc byte[4];
        for (var i = 0; i < 4; ++i)
        {
            channels[i] = (byte)(swizzle >> (i * 8));
            if (channels[i] is not ((byte)'r' or (byte)'g' or (byte)'b' or (byte)'a' or (byte)'0' or (byte)'1'))
                throw new InvalidDataException("KTX channel swizzle is invalid.");
        }
        var destination = ConvertState(source, source.Format is Format.Rf or Format.Rgf or Format.Rgbf or Format.Rgbaf or
            Format.Rh or Format.Rgh or Format.Rgbh or Format.Rgbah or Format.Rgbe9995 ? Format.Rgbaf :
            source.Format >= Format.R16I ? Format.Rgba16I : source.Format >= Format.R16 ? Format.Rgba16 : Format.Rgba8);
        var stride = GetBytesPerPixel(destination.Format);
        for (var offset = 0; offset < destination.Data.Length; offset += stride)
        {
            var color = ReadColor(destination.Data, offset, destination.Format);
            WriteColor(destination.Data, offset, destination.Format, new Color(KTXChannel(color, channels[0]), KTXChannel(color, channels[1]),
                KTXChannel(color, channels[2]), KTXChannel(color, channels[3])));
        }
        return destination;
    }

    private static float KTXChannel(Color color, byte channel) => channel switch
    {
        (byte)'r' => color.R,
        (byte)'g' => color.G,
        (byte)'b' => color.B,
        (byte)'a' => color.A,
        (byte)'0' => 0,
        (byte)'1' => 1,
        _ => throw new InvalidDataException("KTX channel swizzle is invalid.")
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct KTXInfo { internal uint Width, Height, Levels, Version, Format, Flags, Swizzle, Reserved; }

    private sealed class KTXHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal KTXHandle(nint pointer) : base(true) => SetHandle(pointer);
        protected override bool ReleaseHandle() { KTXDestroy(handle); return true; }
    }

    [LibraryImport(NativeLibraries.KTXLibrary, EntryPoint = "e2d_ktx_create")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int KTXCreate(byte* source, ulong length, out KTXInfo info, out nint result);
    [LibraryImport(NativeLibraries.KTXLibrary, EntryPoint = "e2d_ktx_level")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int KTXLevel(KTXHandle texture, uint level, byte* output, ulong length, uint rowBytes, uint rows);
    [LibraryImport(NativeLibraries.KTXLibrary, EntryPoint = "e2d_ktx_destroy")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void KTXDestroy(nint texture);
}
