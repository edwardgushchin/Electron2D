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
    private readonly List<FAudioSampleVoice> _samples = [];
    private readonly Dictionary<nint, (FAudioBusEffect Gain, int MeterIndex, FAudioBusEffect.Activity Activity)> _busEffects = [];
    private long _disableFrames;
    private float _disableThreshold;
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
                foreach (var bus in context._busEffects.Values) bus.Activity.Begin(context.QuantumFrames);
                foreach (var source in context._sources) if (context._busEffects.TryGetValue(source.ActiveSend, out var bus)) source.MarkActivity(bus.Activity);
                foreach (var sample in context._samples) { sample.CheckSource(); if (context._busEffects.TryGetValue(sample.ActiveSend, out var bus)) sample.MarkActivity(bus.Activity); }
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
    internal static void* Allocate(nuint size) => AllocateStorage(size);
    internal static void* AllocateStorage(nuint size) { Interlocked.Increment(ref _allocations); return NativeMemory.Alloc(size); }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Free(void* value) => FreeStorage(value);
    internal static void FreeStorage(void* value) => NativeMemory.Free(value);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void* Reallocate(void* value, nuint size) { Interlocked.Increment(ref _reallocations); return NativeMemory.Realloc(value, size); }
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
            Check(F.FAudioVoice_GetEffectParameters(voice, (uint)_busEffects[voice].MeterIndex, (nint)(&levels), (uint)sizeof(Levels)), "read bus meter"); return peaks[channel];
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
            var settings = ProjectSettings.Instance; var time = settings.GetWithOverride(ProjectSettings.AudioBusesChannelDisableTime); var threshold = settings.GetWithOverride(ProjectSettings.AudioBusesChannelDisableThresholdDB);
            if (!float.IsFinite(time) || time < 0 || time * (double)MixRate > long.MaxValue || !float.IsFinite(threshold)) throw new ArgumentOutOfRangeException(nameof(settings), "Bus activity settings require a finite threshold and nonnegative representable timeout.");
            _disableFrames = (long)(time * MixRate); _disableThreshold = (float)Mathf.DBToLinear(threshold);
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
    internal FAudioBusEffect.Activity CreateBusActivity() => new(Channels, _disableFrames, _disableThreshold);
    internal nint CreateBus(uint stage, nint send, FAudioBusEffect[]? publicEffects = null, bool[]? enabled = null, FAudioBusEffect.Activity? activity = null)
    {
        EnsureOwner(); var descriptor = new F.FAudioSendDescriptor { pOutputVoice = send }; var sends = new F.FAudioVoiceSends { SendCount = 1, pSends = (nint)(&descriptor) };
        lock (Gate)
        {
            publicEffects ??= []; activity ??= CreateBusActivity(); var gain = new FAudioBusEffect(Channels, QuantumFrames) { BusActivity = activity }; foreach (var effect in publicEffects) effect.BusActivity = activity;
            nint voice = 0;
            nint meter = 0;
            try
            {
                Check(CreateMeter(out meter, 0, &Allocate, &Free, &Reallocate), "create bus meter");
                var descriptors = new F.FAudioEffectDescriptor[publicEffects.Length + 2];
                for (var i = 0; i < publicEffects.Length; i++) descriptors[i] = new() { pEffect = publicEffects[i].DangerousGetHandle(), InitialState = enabled![i] ? 1 : 0, OutputChannels = (uint)Channels };
                descriptors[^2] = new() { pEffect = gain.DangerousGetHandle(), InitialState = 1, OutputChannels = (uint)Channels };
                descriptors[^1] = new() { pEffect = meter, InitialState = 1, OutputChannels = (uint)Channels };
                fixed (F.FAudioEffectDescriptor* pointer = descriptors)
                {
                    var effects = new F.FAudioEffectChain { EffectCount = (uint)descriptors.Length, pEffectDescriptors = (nint)pointer };
                    Check(F.FAudio_CreateSubmixVoice(_engine, out voice, (uint)Channels, (uint)MixRate, 0, stage, (nint)(&sends), (nint)(&effects)), "create bus voice");
                }
                _voices.Add(voice); _busEffects.Add(voice, (gain, descriptors.Length - 1, activity)); return voice;
            }
            catch { if (voice != 0) F.FAudioVoice_DestroyVoice(voice); gain.Dispose(); throw; }
            finally { if (meter != 0) F.FAPOBase_Release(meter); }
        }
    }
    internal void SetSend(nint voice, nint send)
    {
        EnsureOwner(); var descriptor = new F.FAudioSendDescriptor { pOutputVoice = send }; var sends = new F.FAudioVoiceSends { SendCount = 1, pSends = (nint)(&descriptor) };
        Check(F.FAudioVoice_SetOutputVoices(voice, ref sends), "set bus send");
        if (_busEffects.TryGetValue(voice, out var bus)) bus.Activity.Send = _busEffects.TryGetValue(send, out var target) ? target.Activity : null;
    }
    internal void SetVolume(nint voice, float value) { EnsureOwner(); Check(F.FAudioVoice_SetVolume(voice, value, 0), "set voice volume"); }
    internal void SetBusVolume(nint voice, float value) { EnsureOwner(); lock (Gate) _busEffects[voice].Gain.Gain = value; }
    internal void EnableBusEffect(nint voice, int index, bool enabled) { EnsureOwner(); lock (Gate) Check(enabled ? F.FAudioVoice_EnableEffect(voice, (uint)index, 0) : F.FAudioVoice_DisableEffect(voice, (uint)index, 0), "set bus effect state"); }
    internal void CollectBusGainErrors(ref List<Exception>? errors) { foreach (var bus in _busEffects.Values) if (bus.Gain.TakeError() is { } error) Node.CollectException(ref errors, error); }
    internal FAudioStreamVoice CreateStream(AudioStreamPlayback playback, nint send) { EnsureOwner(); lock (Gate) { var source = new FAudioStreamVoice(this, playback, send); _sources.Add(source); return source; } }
    internal FAudioSampleVoice CreateSample(AudioSamplePlayback request, AudioSample sample, nint send) { EnsureOwner(); lock (Gate) { var voice = new FAudioSampleVoice(this, request, sample, send); _samples.Add(voice); return voice; } }
    internal void ForgetSample(FAudioSampleVoice voice) => _samples.Remove(voice);
    internal bool HasStandaloneSamples => _samples.Any(sample => !sample.Wrapped && sample.Playing);
    internal void RefreshStandaloneSamplePitch() { foreach (var sample in _samples) if (!sample.Wrapped) sample.RefreshPitch(); }
    internal void RefreshStandaloneSampleGains() { foreach (var sample in _samples) sample.RefreshStandaloneGain(); }
    internal void RefreshStandaloneSampleRouting() { foreach (var sample in _samples) if (!sample.Wrapped) sample.RefreshBus(); }
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
    internal void DestroyBus(nint voice) { EnsureOwner(); lock (Gate) { if (!_voices.Remove(voice)) throw new InvalidOperationException("Unknown audio bus voice."); F.FAudioVoice_DestroyVoice(voice); _busEffects[voice].Gain.Dispose(); _busEffects.Remove(voice); } }
    public void Dispose()
    {
        if (_engine == 0) return;
        lock (Gate)
        {
            _closing = true; while (_sources.Count != 0) _sources[^1].Dispose();
            while (_samples.Count != 0) _samples[^1].Dispose();
            for (var i = _voices.Count - 1; i >= 0; i--) { F.FAudioVoice_DestroyVoice(_voices[i]); _busEffects[_voices[i]].Gain.Dispose(); }
            _voices.Clear(); _busEffects.Clear();
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
