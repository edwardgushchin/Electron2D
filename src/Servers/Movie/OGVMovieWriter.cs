using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed partial class OGVMovieWriter : MovieWriter
{
    private FileAccess? _file;
    private nint _encoder;
    private GCHandle _handle;
    private Exception? _failure;
    private int _channels;
    private bool _failed;
    protected override int OnGetAudioMixRate() => ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterMixRate);
    protected override AudioServer.SpeakerMode OnGetAudioSpeakerMode() => ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterSpeakerMode);
    protected override string[] OnGetSupportedExtensions() => ["ogv"];
    protected override bool OnHandlesFile(string path) => System.IO.Path.GetExtension(path).Equals(".ogv", StringComparison.OrdinalIgnoreCase);
    protected override unsafe void OnWriteBegin(Vector2i movieSize, int fps, string basePath)
    {
        if (movieSize.X <= 0 || movieSize.Y <= 0 || (movieSize.X & 1) != 0 || (movieSize.Y & 1) != 0)
            throw new NotSupportedException("OGV recording requires positive even pixel dimensions for chroma subsampling.");
        _channels = 2 + (int)AudioServer.GetSpeakerMode() * 2;
        var quality = ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterVideoQuality);
        var audioQuality = ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterOGVAudioQuality);
        var speed = ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterOGVEncodingSpeed);
        var keyframes = ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterOGVKeyframeInterval);
        _file = FileAccess.Open(basePath, FileAccessModeFlags.Write);
        try
        {
            _handle = GCHandle.Alloc(this);
            Check(Create(movieSize.X, movieSize.Y, fps, (int)AudioServer.GetMixRate(), _channels, quality, audioQuality, speed, keyframes,
            &WritePage, GCHandle.ToIntPtr(_handle), out _encoder));
        }
        catch { _failed = true; throw; }
    }
    protected override unsafe void OnWriteFrame(Image frameImage, ReadOnlySpan<int> audioFrameBlock)
    {
        try
        {
            var rgb = frameImage.GetData();
            fixed (byte* pixels = rgb)
            fixed (int* audio = audioFrameBlock)
                Check(Frame(_encoder, pixels, (nuint)rgb.Length, audio, audioFrameBlock.Length / _channels));
        }
        catch { _failed = true; throw; }
    }
    private void Check(int status)
    {
        if (status == 0) return;
        _failed = true;
        throw new IOException($"OGV encoding failed with codec status {status}.", _failure);
    }
    protected override void OnWriteEnd()
    {
        try { if (_encoder != 0 && !_failed) Check(Finish(_encoder)); }
        catch { _failed = true; throw; }
        finally
        {
            try { if (_encoder != 0) { Destroy(_encoder); _encoder = 0; } }
            finally
            {
                if (_handle.IsAllocated) _handle.Free();
                var file = _file; _file = null; if (_failed) file?.CancelPendingSave(); file?.Dispose();
            }
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int WritePage(byte* bytes, nuint length, nint context)
    {
        var writer = (OGVMovieWriter)GCHandle.FromIntPtr(context).Target!;
        try { writer._file!.BorrowStream.Write(new ReadOnlySpan<byte>(bytes, checked((int)length))); return 1; }
        catch (Exception failure) { writer._failure = failure; return 0; }
    }
    [LibraryImport(NativeLibraries.OGVLibrary, EntryPoint = "e2d_ogv_create")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int Create(int width, int height, int fps, int rate, int channels, float videoQuality, float audioQuality,
        int speed, int keyframes, delegate* unmanaged[Cdecl]<byte*, nuint, nint, int> writer, nint user, out nint encoder);
    [LibraryImport(NativeLibraries.OGVLibrary, EntryPoint = "e2d_ogv_frame")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int Frame(nint encoder, byte* rgb, nuint bytes, int* pcm, int samples);
    [LibraryImport(NativeLibraries.OGVLibrary, EntryPoint = "e2d_ogv_finish")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Finish(nint encoder);
    [LibraryImport(NativeLibraries.OGVLibrary, EntryPoint = "e2d_ogv_destroy")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void Destroy(nint encoder);
}
