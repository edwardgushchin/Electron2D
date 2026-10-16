using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Electron2D;

internal sealed partial class NativeVideoDecoder : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Metadata
    {
        internal int Width, Height, FPSNumerator, FPSDenominator, Channels, Rate, AudioTracks, Reserved;
        internal long VideoFrames, AudioFrames;
        internal double Duration;
    }
    private sealed class DecoderHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal DecoderHandle(nint value) : base(true) => SetHandle(value);
        protected override bool ReleaseHandle() { Destroy(handle); return true; }
    }
    private readonly DecoderHandle _handle;
    internal readonly Metadata Info;
    internal unsafe NativeVideoDecoder(byte[] bytes, int track)
    {
        nint pointer;
        fixed (byte* data = bytes) Check(Create(data, (nuint)bytes.Length, track, out Info, out pointer));
        try { _handle = new(pointer); } catch { Destroy(pointer); throw; }
    }
    private static void Check(int status)
    {
        if (status >= 0) return;
        if (status == -2) throw new OutOfMemoryException("Video decoder could not reserve working storage.");
        if (status == -3) throw new NotSupportedException("The video picture or audio format is not supported.");
        throw new InvalidDataException("Video source contains malformed or unsupported Ogg codec packets.");
    }
    internal unsafe ReadOnlySpan<byte> Frame(long frame)
    {
        ObjectDisposedException.ThrowIf(_handle.IsClosed, this);
        var status = GetFrame(_handle, frame, out var pixels); Check(status);
        return new((void*)pixels, checked(Info.Width * Info.Height * 4));
    }
    internal unsafe int ReadAudio(float[] pcm, int frames)
    {
        ObjectDisposedException.ThrowIf(_handle.IsClosed, this);
        if (frames < 0 || (long)frames * Info.Channels > pcm.Length) throw new ArgumentOutOfRangeException(nameof(frames));
        fixed (float* data = pcm) { var count = GetAudio(_handle, data, frames); Check(count); return count; }
    }
    internal void SeekAudio(long frame) { ObjectDisposedException.ThrowIf(_handle.IsClosed, this); Check(Seek(_handle, frame)); }
    public void Dispose() => _handle.Dispose();
    [LibraryImport(NativeLibraries.OGVLibrary, EntryPoint = "e2d_video_create")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int Create(byte* data, nuint bytes, int track, out Metadata info, out nint result);
    [LibraryImport(NativeLibraries.OGVLibrary, EntryPoint = "e2d_video_frame")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int GetFrame(DecoderHandle decoder, long frame, out nint pixels);
    [LibraryImport(NativeLibraries.OGVLibrary, EntryPoint = "e2d_video_audio")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int GetAudio(DecoderHandle decoder, float* pcm, int frames);
    [LibraryImport(NativeLibraries.OGVLibrary, EntryPoint = "e2d_video_seek_audio")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Seek(DecoderHandle decoder, long frame);
    [LibraryImport(NativeLibraries.OGVLibrary, EntryPoint = "e2d_video_destroy")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void Destroy(nint decoder);
}
