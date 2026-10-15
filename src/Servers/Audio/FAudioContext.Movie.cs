using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class FAudioContext
{
    private float[]? _offlinePCM;
    private int _offlinePosition;
    [LibraryImport(NativeLibraries.AudioLibrary, EntryPoint = "e2d_audio_create_offline_master")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint CreateOfflineMaster(nint engine, out nint master, uint channels, uint rate);
    [LibraryImport(NativeLibraries.AudioLibrary, EntryPoint = "e2d_audio_mix_offline")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void MixOffline(nint engine, float* output);

    internal void ReadMoviePCM(Span<int> output)
    {
        EnsureOwner();
        var pcm = _offlinePCM ?? throw new InvalidOperationException("Audio is not in movie mode.");
        lock (Gate)
        {
            for (var i = 0; i < output.Length; i++)
            {
                if (_offlinePosition == pcm.Length)
                {
                    fixed (float* data = pcm) MixOffline(_engine, data);
                    _offlinePosition = 0;
                    foreach (var source in _sources) _ = source.Finished;
                }
                var value = pcm[_offlinePosition++];
                if (!float.IsFinite(value)) throw new ArithmeticException("Movie output contains nonfinite audio.");
                output[i] = value <= -1 ? int.MinValue : value >= 1 ? int.MaxValue : (int)(value * 2147483648d);
            }
        }
    }
}
