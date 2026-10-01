using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using F = Electron2D.FAudioBindings.FAudio;

namespace Electron2D;

internal sealed unsafe partial class FAudioContext : IDisposable
{
    private nint _engine, _master;
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private readonly List<nint> _voices = [];
    private readonly List<FAudioStreamVoice> _sources = [];
    private static long _allocations, _reallocations;
    internal readonly object Gate;
    private GCHandle _self;
    private bool _closing;
    private float[]? _capture;
    private int _captureOffset;
    internal void PrepareCapture(int samples) { EnsureOwner(); lock (Gate) { _capture = new float[samples]; _captureOffset = 0; } }
    internal float[] CapturedPCM() { EnsureOwner(); lock (Gate) return _capture is null ? [] : _capture.AsSpan(0, _captureOffset).ToArray(); }
    private long _lastMix, _mixPasses, _mixManagedBytes;
    internal int QuantumFrames { get; private set; }
    internal long MixPasses => Interlocked.Read(ref _mixPasses);
    internal long MixManagedBytes => Interlocked.Read(ref _mixManagedBytes);
    internal double SinceMix => _lastMix == 0 ? 0 : System.Diagnostics.Stopwatch.GetElapsedTime(Interlocked.Read(ref _lastMix)).TotalSeconds;
    [LibraryImport("FAudio", EntryPoint = "FAudio_SetEngineProcedureEXT")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void SetProcedure(nint engine, delegate* unmanaged[Cdecl]<delegate* unmanaged[Cdecl]<nint, float*, void>, nint, float*, nint, void> procedure, nint user);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Process(delegate* unmanaged[Cdecl]<nint, float*, void> original, nint engine, float* output, nint user)
    {
        var context = (FAudioContext)GCHandle.FromIntPtr(user).Target!;
        var before = GC.GetAllocatedBytesForCurrentThread();
        lock (context.Gate)
        {
            if (context._closing) new Span<float>(output, context.QuantumFrames * context.Channels).Clear();
            else
            {
                original(engine, output);
                if (context._capture is { } capture)
                {
                    var count = Math.Min(capture.Length - context._captureOffset, context.QuantumFrames * context.Channels);
                    new ReadOnlySpan<float>(output, count).CopyTo(capture.AsSpan(context._captureOffset)); context._captureOffset += count;
                }
            }
            Interlocked.Exchange(ref context._lastMix, System.Diagnostics.Stopwatch.GetTimestamp()); Interlocked.Increment(ref context._mixPasses);
        }
        Interlocked.Add(ref context._mixManagedBytes, GC.GetAllocatedBytesForCurrentThread() - before);
    }
    internal nint Engine => _engine;
    internal nint Master => _master;
    internal int MixRate { get; private set; }
    internal int Channels { get; private set; }
    internal static long AllocationCalls => Interlocked.Read(ref _allocations) + Interlocked.Read(ref _reallocations);
    [LibraryImport("FAudio", EntryPoint = "FAudioCreateWithCustomAllocatorEXT")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint Create(out nint engine, uint flags, uint processor, delegate* unmanaged[Cdecl]<nuint, void*> malloc, delegate* unmanaged[Cdecl]<void*, void> free, delegate* unmanaged[Cdecl]<void*, nuint, void*> realloc);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* Allocate(nuint size) { Interlocked.Increment(ref _allocations); return NativeMemory.Alloc(size); }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Free(void* value) => NativeMemory.Free(value);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* Reallocate(void* value, nuint size) { Interlocked.Increment(ref _reallocations); return NativeMemory.Realloc(value, size); }
    [LibraryImport("FAudio", EntryPoint = "FAudioCreateVolumeMeterWithCustomAllocatorEXT")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint CreateMeter(out nint meter, uint flags, delegate* unmanaged[Cdecl]<nuint, void*> malloc, delegate* unmanaged[Cdecl]<void*, void> free, delegate* unmanaged[Cdecl]<void*, nuint, void*> realloc);
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Levels { internal nint Peaks, RMS; internal uint Channels; }
    internal float BusPeak(nint voice, int channel)
    {
        EnsureOwner(); if ((uint)channel >= (uint)Channels) throw new ArgumentOutOfRangeException(nameof(channel));
        lock (Gate)
        {
            var peaks = stackalloc float[Channels]; var rms = stackalloc float[Channels]; var levels = new Levels { Peaks = (nint)peaks, RMS = (nint)rms, Channels = (uint)Channels };
            Check(F.FAudioVoice_GetEffectParameters(voice, 0, (nint)(&levels), (uint)sizeof(Levels)), "read bus meter"); return peaks[channel];
        }
    }
    internal FAudioContext(uint deviceIndex = 0, object? gate = null)
    {
        Gate = gate ?? new();
        if (!OperatingSystem.IsLinux()) throw new NotSupportedException("Native audio output is currently verified only by the Linux FAudio profile.");
        try
        {
            Check(Create(out _engine, 0, F.FAUDIO_DEFAULT_PROCESSOR, &Allocate, &Free, &Reallocate), "create engine");
            _self = GCHandle.Alloc(this); SetProcedure(_engine, &Process, GCHandle.ToIntPtr(_self));
            Check(F.FAudio_GetDeviceDetails(_engine, deviceIndex, out var device), "query output format");
            MixRate = checked((int)device.OutputFormat.Format.nSamplesPerSec); Channels = Math.Max(2, checked((int)device.OutputFormat.Format.nChannels)); if (Channels is not (2 or 4 or 6 or 8)) throw new NotSupportedException("The output layout has no supported stereo-pair profile."); QuantumFrames = MixRate / 100;
            Check(F.FAudio_CreateMasteringVoice(_engine, out _master, (uint)Channels, (uint)MixRate, 0, deviceIndex, 0), "create output voice");
            F.FAudio_GetProcessingQuantum(_engine, out var frames, out _); QuantumFrames = checked((int)frames); if (QuantumFrames == 0) throw new InvalidOperationException("The native output has an empty processing quantum.");
        }
        catch { Dispose(); throw; }
    }
    internal void EnsureOwner()
    {
        ObjectDisposedException.ThrowIf(_engine == 0, this);
        if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("Audio configuration requires its owner thread.");
    }
    internal static void Check(uint result, string operation)
    {
        if (result != 0) throw new InvalidOperationException($"FAudio {operation} failed (0x{result:X8}).");
    }
    internal nint CreateBus(uint stage, nint send)
    {
        EnsureOwner(); var descriptor = new F.FAudioSendDescriptor { pOutputVoice = send }; var sends = new F.FAudioVoiceSends { SendCount = 1, pSends = (nint)(&descriptor) };
        lock (Gate)
        {
            Check(CreateMeter(out var meter, 0, &Allocate, &Free, &Reallocate), "create bus meter");
            try
            {
                var effect = new F.FAudioEffectDescriptor { pEffect = meter, InitialState = 1, OutputChannels = (uint)Channels }; var effects = new F.FAudioEffectChain { EffectCount = 1, pEffectDescriptors = (nint)(&effect) };
                Check(F.FAudio_CreateSubmixVoice(_engine, out var voice, (uint)Channels, (uint)MixRate, 0, stage, (nint)(&sends), (nint)(&effects)), "create bus voice");
                _voices.Add(voice); return voice;
            }
            finally { F.FAPOBase_Release(meter); }
        }
    }
    internal void SetSend(nint voice, nint send)
    {
        EnsureOwner(); var descriptor = new F.FAudioSendDescriptor { pOutputVoice = send }; var sends = new F.FAudioVoiceSends { SendCount = 1, pSends = (nint)(&descriptor) };
        Check(F.FAudioVoice_SetOutputVoices(voice, ref sends), "set bus send");
    }
    internal void SetVolume(nint voice, float value) { EnsureOwner(); Check(F.FAudioVoice_SetVolume(voice, value, 0), "set voice volume"); }
    internal FAudioStreamVoice CreateStream(AudioStreamPlayback playback, nint send) { var source = new FAudioStreamVoice(this, playback, send); _sources.Add(source); return source; }
    internal nint[] BusVoices() => _voices.ToArray();
    internal void ForgetSource(FAudioStreamVoice source) => _sources.Remove(source);
    internal string[] Devices()
    {
        EnsureOwner(); Check(F.FAudio_GetDeviceCount(_engine, out var count), "enumerate output devices"); var result = new string[count];
        for (uint i = 0; i < count; i++)
        {
            Check(F.FAudio_GetDeviceDetails(_engine, i, out var details), "query output device"); var length = 0; while (length < 256 && details.DisplayName[length] != 0) length++;
            result[i] = new string((char*)details.DisplayName, 0, length);
        }
        return result;
    }
    internal void DestroyBus(nint voice) { EnsureOwner(); if (!_voices.Remove(voice)) throw new InvalidOperationException("Unknown audio bus voice."); F.FAudioVoice_DestroyVoice(voice); }
    public void Dispose()
    {
        if (_engine == 0) return;
        lock (Gate)
        {
            _closing = true; while (_sources.Count != 0) _sources[^1].Dispose();
            for (var i = _voices.Count - 1; i >= 0; i--) F.FAudioVoice_DestroyVoice(_voices[i]); _voices.Clear();
        }
        // The backend joins its SDL worker; closing state protects teardown while caller-held mix locks are released.
        var locks = 0;
        while (Monitor.IsEntered(Gate)) { Monitor.Exit(Gate); locks++; }
        try
        {
            if (_master != 0) { F.FAudioVoice_DestroyVoice(_master); _master = 0; }
            F.FAudio_Release(_engine); _engine = 0;
            if (_self.IsAllocated) _self.Free();
        }
        finally { while (locks-- > 0) Monitor.Enter(Gate); }
    }
}
