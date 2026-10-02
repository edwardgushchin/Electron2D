using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using F = Electron2D.FAudioBindings.FAudio;

namespace Electron2D;

internal sealed unsafe partial class FAudioBusEffect : SafeHandle
{
    internal sealed class Activity(int channels, long disableFrames, float threshold)
    {
        internal readonly bool[] Active = new bool[channels / 2];
        private readonly bool[] _used = new bool[channels / 2];
        private readonly long[] _last = new long[channels / 2];
        private long _frame;
        internal Activity? Send;
        internal void Begin(int frames) { Array.Clear(_used); _frame += frames; }
        internal void Use(int pair) { Active[pair] = true; _used[pair] = true; _last[pair] = _frame; }
        internal void Finish(ReadOnlySpan<float> pcm, int channels)
        {
            for (var pair = 0; pair < Active.Length; pair++)
            {
                var peak = 0f; for (var i = pair * 2; i < pcm.Length; i += channels) peak = Math.Max(peak, Math.Max(Math.Abs(pcm[i]), Math.Abs(pcm[i + 1])));
                if (peak > threshold) { Active[pair] = true; _last[pair] = _frame; }
                else if (!_used[pair] && _frame - _last[pair] > disableFrames) Active[pair] = false;
                if (Active[pair]) Send?.Use(pair);
            }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct NativeEffect { internal F.FAPOBase Base; internal nint User; internal F.FAPORegistrationProperties Properties; }
    private sealed class State(int channels, int frames, AudioEffect? source)
    {
        internal readonly int Channels = channels;
        internal readonly Vector2[] Input = source is null ? [] : new Vector2[frames], Output = source is null ? [] : new Vector2[frames];
        internal readonly AudioEffectInstance[] Instances = source is null ? [] : new AudioEffectInstance[channels / 2];
        internal readonly AudioEffect? Source = source;
        internal float Gain = 1;
        internal Exception? Error;
        internal bool Reported;
        internal Activity? Activity;
    }
    private readonly State _state;
    internal AudioEffectInstance[] Instances => _state.Instances;
    internal float Gain { set { _state.Gain = value; _state.Error = null; _state.Reported = false; } }
    internal Activity? BusActivity { set => _state.Activity = value; }
    public override bool IsInvalid => handle == 0;
    [LibraryImport("FAudio", EntryPoint = "CreateFAPOBaseWithCustomAllocatorEXT")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void CreateBase(NativeEffect* value, F.FAPORegistrationProperties* properties, nint blocks, uint bytes, byte producer, delegate* unmanaged[Cdecl]<nuint, void*> malloc, delegate* unmanaged[Cdecl]<void*, void> free, delegate* unmanaged[Cdecl]<void*, nuint, void*> realloc);
    internal FAudioBusEffect(int channels, int frames, AudioEffect? source = null) : base(0, true)
    {
        _state = new(channels, frames, source);
        try
        {
            for (var i = 0; i < Instances.Length; i++) { var instance = AudioServer.Instance.CreateEffectInstance(source!); Instances[i] = instance; instance.Attach(i); }
            var value = (NativeEffect*)FAudioContext.AllocateStorage((nuint)sizeof(NativeEffect)); if (value is null) throw new OutOfMemoryException(); *value = default;
            SetHandle((nint)value);
            value->Properties = new F.FAPORegistrationProperties { clsid = new Guid("f478ec82-d6db-487c-80cf-b225b21a46ed"), MajorVersion = 1, Flags = 0x1F, MinInputBufferCount = 1, MaxInputBufferCount = 1, MinOutputBufferCount = 1, MaxOutputBufferCount = 1 };
            CreateBase(value, &value->Properties, 0, 0, 0, &FAudioContext.Allocate, &FAudioContext.Free, &FAudioContext.Reallocate);
            value->Base.Destructor = (nint)(delegate* unmanaged[Cdecl]<NativeEffect*, void>)&Destroy;
            value->Base.FAPO.Process = (nint)(delegate* unmanaged[Cdecl]<NativeEffect*, uint, F.FAPOProcessBufferParameters*, uint, F.FAPOProcessBufferParameters*, int, void>)&Process;
            value->User = GCHandle.ToIntPtr(GCHandle.Alloc(_state));
        }
        catch (Exception error)
        {
            Dispose(); try { ReleaseInstances(); } catch (Exception cleanup) { throw new AggregateException("Effect preparation and cleanup failed.", error, cleanup); }
            throw;
        }
    }
    protected override bool ReleaseHandle() { F.FAPOBase_Release(handle); handle = 0; return true; }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Destroy(NativeEffect* value)
    {
        if (value->User != 0) GCHandle.FromIntPtr(value->User).Free(); FAudioContext.FreeStorage(value);
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Process(NativeEffect* value, uint inputs, F.FAPOProcessBufferParameters* input, uint outputs, F.FAPOProcessBufferParameters* output, int enabled)
    {
        var state = (State)GCHandle.FromIntPtr(value->User).Target!;
        var count = (int)input->ValidFrameCount;
        var src = new ReadOnlySpan<float>((void*)input->pBuffer, count * state.Channels); var dst = new Span<float>((void*)output->pBuffer, src.Length);
        output->ValidFrameCount = input->ValidFrameCount;
        // FAudio's silence hint examines only part of a multichannel block. Determine silence per stereo pair below.
        output->BufferFlags = F.FAPOBufferFlags.FAPO_BUFFER_VALID;
        if (enabled == 0) { src.CopyTo(dst); return; }
        if (state.Error is not null) { dst.Clear(); return; }
        try
        {
            if (state.Source is null)
            {
                for (var i = 0; i < src.Length; i++) { var sample = src[i] * state.Gain; if (!float.IsFinite(sample)) throw new ArithmeticException("Bus gain produced nonfinite PCM."); dst[i] = sample; }
                state.Activity?.Finish(dst, state.Channels); return;
            }
            ObjectDisposedException.ThrowIf(state.Source.IsDisposed, state.Source);
            if (count > state.Input.Length) throw new InvalidOperationException("The effect quantum exceeds prepared storage.");
            var stereoInput = state.Input.AsSpan(0, count); var stereoOutput = state.Output.AsSpan(0, count);
            for (var pair = 0; pair < state.Instances.Length; pair++)
            {
                var silent = true;
                for (var i = 0; i < count; i++) { var frame = new Vector2(src[i * state.Channels + pair * 2], src[i * state.Channels + pair * 2 + 1]); stereoInput[i] = frame; silent &= frame == Vector2.Zero; }
                var instance = state.Instances[pair];
                if (!silent || state.Activity is { } activity && activity.Active[pair] || instance.ProcessSilence()) instance.ProcessNative(stereoInput, stereoOutput); else stereoInput.CopyTo(stereoOutput);
                for (var i = 0; i < count; i++) { dst[i * state.Channels + pair * 2] = stereoOutput[i].X; dst[i * state.Channels + pair * 2 + 1] = stereoOutput[i].Y; }
            }
        }
        catch (Exception error) { state.Error = error; dst.Clear(); }
    }
    internal Exception? TakeError() { if (_state.Reported || _state.Error is null) return null; _state.Reported = true; return _state.Error; }
    internal void ReleaseInstances()
    {
        List<Exception>? errors = null;
        for (var i = 0; i < Instances.Length; i++) if (Instances[i] is { } instance)
            {
                Instances[i] = null!; instance.Detach(); try { AudioServer.Instance.DisposeEffectInstance(instance); } catch (Exception error) { Node.CollectException(ref errors, error); }
            }
        Node.ThrowCollected("Effect instance teardown failed.", errors);
    }
}
