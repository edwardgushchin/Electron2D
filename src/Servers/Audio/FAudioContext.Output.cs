using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class FAudioContext
{
    private bool _switchingOutput;
    private double _outputLatency;
    [LibraryImport("FAudio", EntryPoint = "e2d_audio_output_latency")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial double OutputLatency(nint audio);
    [LibraryImport("FAudio", EntryPoint = "e2d_audio_select_output")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int SelectOutput(nint audio, uint device);
    internal double BufferedOutputLatency => AtomicFloatingPoint.Read(ref _outputLatency);
    internal void SetOutput(string name)
    {
        EnsureOwner(); var id = SDL.AudioDeviceDefaultPlayback;
        if (name != "Default")
        {
            var ids = SDL.GetAudioPlaybackDevices(out _) ?? throw new InvalidOperationException("Audio output enumeration failed: " + SDL.GetError()); var found = false;
            foreach (var candidate in ids) if (SDL.GetAudioDeviceName(candidate) == name) { id = candidate; found = true; break; }
            if (!found) throw new ArgumentException("The output device name is not available.", nameof(name));
        }
        RunOutputOperation(id);
    }
    private void UpdateOutputSnapshot() => RunOutputOperation(null);
    private void RunOutputOperation(uint? selected)
    {
        EnsureOwner(); var locks = 0;
        lock (Gate) _switchingOutput = true;
        while (Monitor.IsEntered(Gate)) { Monitor.Exit(Gate); locks++; }
        try
        {
            if (selected is { } device && SelectOutput(_engine, device) == 0)
                throw new InvalidOperationException("Audio output selection failed: " + SDL.GetError());
            var latency = OutputLatency(_engine);
            if (!double.IsFinite(latency) || latency < 0) throw new InvalidOperationException("Audio output buffering is unavailable: " + SDL.GetError());
            AtomicFloatingPoint.Write(ref _outputLatency, latency);
        }
        finally
        {
            while (locks-- > 0) Monitor.Enter(Gate);
            lock (Gate) _switchingOutput = false;
        }
    }
}
