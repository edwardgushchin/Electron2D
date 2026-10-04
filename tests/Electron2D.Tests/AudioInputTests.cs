using System.Runtime.InteropServices;
using Electron2D;
using SDL3;
using RateMode = Electron2D.AudioStreamGenerator.AudioStreamGeneratorMixRate;

internal static class AudioInputTests
{
    internal static void Run(bool native = false)
    {
        using var settings = new ProjectSettingsRegistry(Environment.CurrentDirectory, System.IO.Path.GetTempPath()); Check(!settings.Get(ProjectSettings.AudioDriverEnableInput), "Input is disabled by default."); settings.Set(ProjectSettings.AudioDriverEnableInput, true); Check(settings.Get(ProjectSettings.AudioDriverEnableInput), "Typed input setting.");
        using var source = new AudioStreamMicrophone { ResourceLocalToScene = true }; using var copy = (AudioStreamMicrophone)source.Duplicate(true); using var p = source.InstantiatePlayback();
        Check(copy.ResourceLocalToScene && source.IsMonophonic() && source.GetLength() == 0 && !source.IsMetaStream() && source.GetParameterList().Length == 0, "Microphone resource/graph contract.");
        Check(p is AudioStreamPlaybackResampled && !p.IsPlaying() && p.GetLoopCount() == 0 && p.GetPlaybackPosition() == 0 && p.MixAudio(1, 128).Length == 0, "Inactive microphone.");
        p.Seek(12); Reject<ArgumentOutOfRangeException>(() => p.Start(double.NaN)); Reject<ArgumentOutOfRangeException>(() => p.Seek(double.PositiveInfinity));
        using var workerPlayback = Task.Run(source.InstantiatePlayback).GetAwaiter().GetResult();
        if (native) VerifyNative();
        Console.WriteLine("Microphone resource and input checks passed.");
    }
    private static void VerifyNative()
    {
        var server = AudioServer.Service; var settings = ProjectSettings.Service; var enabled = ProjectSettings.Get(ProjectSettings.AudioDriverEnableInput);
        server.CloseNative(); ProjectSettings.Set(ProjectSettings.AudioDriverEnableInput, false);
        try
        {
            Check(AudioServer.InputDevice == "Default" && AudioServer.GetInputBufferLengthFrames() == 0 && AudioServer.GetInputFramesAvailable() == 0, "Cold input state.");
            Reject<InvalidOperationException>(() => AudioServer.SetInputDeviceActive(true));
            using var mic = new AudioStreamMicrophone(); using var disabled = mic.InstantiatePlayback(); Reject<InvalidOperationException>(() => disabled.Start()); Check(!disabled.IsPlaying(), "Disabled start is transactional.");
            var devices = AudioServer.GetInputDeviceList(); Check(devices.Length > 1 && devices[0] == "Default", "Native dummy input enumeration.");
            var rate = AudioServer.GetInputMixRate(); var input = server.PreparedInput; Check(rate > 0 && !input.Active && AudioServer.GetInputBufferLengthFrames() == input.Capacity, "Actual paused recording format.");
            Check(Task.Run(AudioServer.GetInputMixRate).GetAwaiter().GetResult() == rate, "Prepared frequency read is passive.");
            Task.Run(() => Reject<InvalidOperationException>(() => AudioServer.SetInputDeviceActive(true))).GetAwaiter().GetResult();
            Reject<ArgumentNullException>(() => AudioServer.InputDevice = null!); Reject<ArgumentException>(() => AudioServer.InputDevice = "missing capture fixture"); Check(ReferenceEquals(input, server.PreparedInput) && AudioServer.InputDevice == "Default", "Failed replacement retains device.");
            Reject<ArgumentOutOfRangeException>(() => AudioServer.GetInputFrames(-1));
            VerifyRing(input); VerifyGenerator(input); VerifyCallbackFailure(input);
            ProjectSettings.Set(ProjectSettings.AudioDriverEnableInput, true); VerifyActivationFailure(input, mic); AudioServer.SetInputDeviceActive(true); Check(input.Active, "Manual activation."); var pass = Interlocked.Read(ref input.CallbackPasses); Wait(() => Interlocked.Read(ref input.CallbackPasses) >= pass + 20);
            Check(AudioServer.GetInputFramesAvailable() > 0 && AudioServer.GetInputFrames(AudioServer.GetInputFramesAvailable()).All(v => v == Vector2.Zero), "Actual recording callback supplies dummy silence.");
            var bytes = Interlocked.Read(ref input.CallbackManagedBytes); pass = Interlocked.Read(ref input.CallbackPasses); Wait(() => Interlocked.Read(ref input.CallbackPasses) >= pass + 64);
            Check(Interlocked.Read(ref input.CallbackManagedBytes) == bytes, "64 warmed actual input callbacks allocate zero managed bytes.");
            AudioServer.SetInputDeviceActive(true); AudioServer.SetInputDeviceActive(false); var held = AudioServer.GetInputFramesAvailable(); Check(!input.Active && held > 0 && AudioServer.GetInputFrames(held).Length == held, "Pause retains readable capture.");
            VerifyMicrophones(input, mic); VerifyOutput(mic);
            AudioServer.InputDevice = devices[1]; Check(AudioServer.InputDevice == devices[1] && AudioServer.GetInputFramesAvailable() == 0 && !server.PreparedInput.Active, "Native explicit device switch resets capture."); AudioServer.InputDevice = "Default";
            using var standalone = mic.InstantiatePlayback(); standalone.Start(); server.CloseNative(); Check(!standalone.IsPlaying() && AudioServer.GetInputBufferLengthFrames() == 0, "Engine closure stops standalone requests and releases input."); standalone.Start(); standalone.Stop();
            var borrowed = new AudioStreamMicrophone(); using var borrowedPlayback = borrowed.InstantiatePlayback(); borrowedPlayback.Start(); borrowed.Dispose(); Reject<ObjectDisposedException>(() => borrowedPlayback.MixAudio(1, 128)); borrowedPlayback.Dispose(); Check(!server.PreparedInput.Active, "Disposed source rejects mixing while playback cleanup still releases capture.");
            VerifyStopFailure(mic);
        }
        finally { server.CloseNative(); ProjectSettings.Set(ProjectSettings.AudioDriverEnableInput, enabled); }
    }
    private static void VerifyRing(AudioInputDevice input)
    {
        var server = AudioServer.Service; var frames = new Vector2[input.Capacity + 17]; for (var i = 0; i < frames.Length; i++) frames[i] = new(i / (float)frames.Length, -i / (float)frames.Length);
        Put(input, frames); Check(AudioServer.GetInputFramesAvailable() == input.Capacity, "Exact/full wrapped ring does not look empty.");
        Check(AudioServer.GetInputFrames(int.MaxValue).Length == 0 && AudioServer.GetInputFramesAvailable() == input.Capacity && AudioServer.GetInputFrames(0).Length == 0, "Insufficient/empty reads do not consume.");
        Check(AudioServer.GetInputFrames(input.Capacity).SequenceEqual(frames.AsSpan(17).ToArray()), "Overflow preserves the newest complete ring in FIFO order.");
        Put(input, [new(.25f, -.5f), new(2, -2), new(float.NaN, 0)]); var sanitized = AudioServer.GetInputFrames(3); Check(sanitized.SequenceEqual(new[] { new Vector2(.25f, -.5f), new(1, -1), Vector2.Zero }), "Capture finite/range boundary.");
        Check(SDL.GetAudioStreamFormat(input.Stream, out _, out var target), "Read native conversion format."); var mono = target; mono.Channels = 1;
        // A bound recording stream's source format is device-owned; an unbound native stream supplies the mono fixture.
        var converter = SDL.CreateAudioStream(in mono, in target); Check(converter != 0, "Prepare mono converter.");
        var callback = typeof(AudioInputDevice).GetMethod("Capture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.CreateDelegate<SDL.AudioStreamCallback>(input);
        try { Check(SDL.SetAudioStreamPutCallback(converter, callback, 0), "Install the production capture callback."); PutRaw(converter, new float[] { .3f, -.2f }); Check(AudioServer.GetInputFrames(2).SequenceEqual(new[] { new Vector2(.3f, .3f), new(-.2f, -.2f) }), "Native mono-to-stereo conversion."); }
        finally { SDL.DestroyAudioStream(converter); GC.KeepAlive(callback); }
        var threshold = Math.Min((int)(50 * (long)input.MixRate / 1000), input.Capacity / 2); var cursor = 0L; var generation = -1; var output = new Vector2[128];
        // Ring already contains capture; a fresh paused device isolates the 50 ms priming boundary.
        using var fresh = new AudioInputDevice("Default"); threshold = Math.Min((int)(50 * (long)fresh.MixRate / 1000), fresh.Capacity / 2);
        var prime = new Vector2[threshold - 1]; prime.AsSpan().Fill(new(.4f, -.1f)); Put(fresh, prime); fresh.Mix(output, ref cursor, ref generation); Check(output.All(v => v == Vector2.Zero) && cursor == 0, "Capture priming shortage is silent.");
        Put(fresh, [new(.4f, -.1f)]); fresh.Mix(output, ref cursor, ref generation); Check(output.All(v => v == new Vector2(.4f, -.1f)), "Capture begins at the priming boundary.");
        Put(fresh, new Vector2[fresh.Capacity]); fresh.Mix(output, ref cursor, ref generation); Check(cursor == threshold + output.Length && output.All(v => v == Vector2.Zero), "Lagging microphone catches up instead of replaying overwritten data.");
    }
    private static void VerifyGenerator(AudioInputDevice input)
    {
        using var source = new AudioStreamGenerator { MixRate = 123, MixRateMode = RateMode.Input, BufferLength = .01f }; using var copy = (AudioStreamGenerator)source.Duplicate(true); using var p = (AudioStreamGeneratorPlayback)copy.InstantiatePlayback();
        var count = (uint)(input.MixRate * .01f); var expected = 1 << (System.Numerics.BitOperations.Log2(count) + 1);
        Check(copy.MixRateMode == RateMode.Input && p.GetFramesAvailable() == expected - 1 && !input.Active, "Input generator uses actual frequency and never activates recording.");
        var feed = new Vector2[expected - 1]; feed.AsSpan().Fill(new(.2f, -.3f)); p.PushBuffer(feed); p.Start(); p.MixAudio(1, 256); Check(p.GetPlaybackPosition() > 0 && p.IsPlaying(), "Input-rate generator resamples continuously.");
        source.MixRateMode = RateMode.Custom; using var live = (AudioStreamGeneratorPlayback)source.InstantiatePlayback(); source.MixRateMode = RateMode.Input; live.Start(); live.MixAudio(1, 256); Check(live.IsPlaying() && Math.Abs(live.GetPlaybackPosition() - 256d / input.MixRate) < 1e-12, "Live Custom-to-Input mode uses actual frequency before mixing.");
    }
    private static int FailedAvailable(nint stream) => -1;
    private static bool FailedResume(nint stream) => false;
    private static void VerifyStopFailure(AudioStreamMicrophone mic)
    {
        var field = typeof(SDL).GetField("PauseAudioStreamDeviceNativeFunction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var original = field.GetValue(null); var method = typeof(AudioInputTests).GetMethod(nameof(FailedResume), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        using var playback = mic.InstantiatePlayback(); playback.Start();
        try { field.SetValue(null, method.CreateDelegate(field.FieldType)); Reject<InvalidOperationException>(playback.Stop); Check(!playback.IsPlaying() && AudioServer.GetInputBufferLengthFrames() == 0, "Failed last-request pause destroys the failed device and releases playback state."); }
        finally { field.SetValue(null, original); }
        playback.Start(); playback.Stop();
        playback.Start();
        try { field.SetValue(null, method.CreateDelegate(field.FieldType)); Reject<InvalidOperationException>(playback.Dispose); Check(playback.IsDisposed && AudioServer.GetInputBufferLengthFrames() == 0, "Failed native pause during disposal retains no dead capture request."); }
        finally { field.SetValue(null, original); }
    }
    private static void VerifyActivationFailure(AudioInputDevice input, AudioStreamMicrophone mic)
    {
        var field = typeof(SDL).GetField("ResumeAudioStreamDeviceNativeFunction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var original = field.GetValue(null); var method = typeof(AudioInputTests).GetMethod(nameof(FailedResume), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        using var playback = mic.InstantiatePlayback();
        try { field.SetValue(null, method.CreateDelegate(field.FieldType)); Reject<InvalidOperationException>(() => AudioServer.SetInputDeviceActive(true)); Reject<InvalidOperationException>(() => playback.Start()); Check(!input.Active && !playback.IsPlaying(), "Failed activation does not publish a request or active playback."); }
        finally { field.SetValue(null, original); }
    }
    private static void VerifyCallbackFailure(AudioInputDevice input)
    {
        var field = typeof(SDL).GetField("GetAudioStreamAvailableNativeFunction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var original = field.GetValue(null); var method = typeof(AudioInputTests).GetMethod(nameof(FailedAvailable), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        try { field.SetValue(null, method.CreateDelegate(field.FieldType)); Put(input, [Vector2.One]); }
        finally { field.SetValue(null, original); }
        Reject<InvalidOperationException>(() => AudioServer.GetInputFramesAvailable());
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        var settings = ProjectSettings.Service; var rendering = ProjectSettings.Get(ProjectSettings.RenderingMethod); var enabled = ProjectSettings.Get(ProjectSettings.AudioDriverEnableInput); var fps = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.AudioDriverEnableInput, true); Engine.MaxFPS = 60;
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var mic = new AudioStreamMicrophone(); var window = new Window { Size = new(160, 96), Title = "Electron2D microphone" }; var player = new AudioStreamPlayer { Stream = mic, Autoplay = true }; var scenario = new Scenario(player);
                window.AddChild(player); window.AddChild(scenario); Check(Engine.Run(window) == 0 && scenario.Completed && window.IsDisposed && !mic.IsDisposed && AudioServer.GetInputBufferLengthFrames() == 0, "Public microphone host/cleanup.");
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "audio-input-host", backend, run, scenario.Rate, scenario.Frames, cleaned = window.IsDisposed }));
            }
            using var failingMic = new AudioStreamMicrophone(); var failingWindow = new Window { Size = new(160, 96), Title = "Electron2D microphone cleanup" }; failingWindow.AddChild(new AudioStreamPlayer { Stream = failingMic, Autoplay = true }); failingWindow.AddChild(new FailedScenario());
            try { Engine.Run(failingWindow); throw new InvalidOperationException("Expected host process failure."); }
            catch (AggregateException error) { Check(error.Flatten().InnerExceptions.Any(e => e.Message == "Input host failure fixture."), "Scene frame aggregates the injected process failure."); }
            Check(failingWindow.IsDisposed && AudioServer.GetInputBufferLengthFrames() == 0 && !failingMic.IsDisposed, "Failed public host releases recording and borrows stream.");
        }
        finally { Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.AudioDriverEnableInput, enabled); ProjectSettings.Set(ProjectSettings.RenderingMethod, rendering); }
    }
    private sealed class Scenario(AudioStreamPlayer player) : Node
    {
        private double _elapsed;
        internal bool Completed;
        internal float Rate;
        internal int Frames;
        protected override void OnReady() => ProcessEnabled = true;
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; if (_elapsed < .3) return; Rate = AudioServer.GetInputMixRate(); Frames = AudioServer.GetInputFramesAvailable(); Completed = player.IsPlaying() && player.GetPlaybackPosition() == 0 && Rate > 0 && Frames > 0; Tree!.Quit();
        }
    }
    private sealed class FailedScenario : Node
    {
        protected override void OnReady() => ProcessEnabled = true;
        protected override void OnProcess(double delta) => throw new InvalidOperationException("Input host failure fixture.");
    }
    private static void VerifyMicrophones(AudioInputDevice input, AudioStreamMicrophone mic)
    {
        var server = AudioServer.Service; using var a = (AudioStreamPlaybackResampled)mic.InstantiatePlayback(); using var b = (AudioStreamPlaybackResampled)mic.InstantiatePlayback(); a.Start(20); b.Start();
        Check(SDL.PauseAudioStreamDevice(input.Stream), "Pause hardware for deterministic PCM injection."); var feed = new Vector2[input.Capacity]; feed.AsSpan().Fill(new(.2f, -.4f)); Put(input, feed); a.BeginResample(); b.BeginResample();
        Check(AudioServer.GetInputFrames(AudioServer.GetInputFramesAvailable()).Length > 0, "Server independently consumes its reader.");
        var aa = a.MixAudio(1, 256); var bb = b.MixAudio(1, 256); Check(aa.SequenceEqual(bb) && aa.Skip(2).Take(100).All(v => v == new Vector2(.2f, -.4f)), "Independent microphone cursors produce the same stereo PCM.");
        Check(a.GetPlaybackPosition() == 0 && a.GetLoopCount() == 0, "Microphone has no time/loop cursor."); a.Seek(50); a.Start(100); a.Stop(); Check(input.Active && b.IsPlaying(), "Stopping one microphone retains the other's capture.");
        AudioServer.SetInputDeviceActive(true); b.Stop(); Check(input.Active, "Manual request outlives microphone stop."); AudioServer.SetInputDeviceActive(false); Check(!input.Active, "Last request pauses native input.");
        b.Start(); AudioServer.SetInputDeviceActive(false); Check(!input.Active && b.IsPlaying(), "Explicit server pause stops capture even with an active microphone."); b.Stop();
        b.Start(); Check(SDL.PauseAudioStreamDevice(input.Stream), "Pause fixture."); var small = new Vector2[128]; small.AsSpan().Fill(new(.15f, -.1f)); var output = new Vector2[128];
        for (var i = 0; i < 20; i++) { Put(input, small); b.MixInto(output, 1); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { Put(input, small); b.MixInto(output, 1); AudioServer.GetInputFramesAvailable(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 prepared SDL callback/ring/resampling cycles allocate zero managed bytes.");
        var concurrent = Task.Run(() => { for (var i = 0; i < 200; i++) { Put(input, small); b.BeginResample(); } }); for (var i = 0; i < 500; i++) b.MixInto(output, 1); concurrent.GetAwaiter().GetResult(); Check(output.All(v => v.IsFinite()), "Concurrent capture/history/mix preserves finite frames.");
        Task.Run(() => Reject<InvalidOperationException>(b.Stop)).GetAwaiter().GetResult(); Task.Run(() => Reject<InvalidOperationException>(b.Dispose)).GetAwaiter().GetResult(); Check(!b.IsDisposed, "Foreign active disposal rejects before transition.");
        var current = server.PreparedInput; Reject<ArgumentException>(() => AudioServer.InputDevice = "missing active capture fixture"); Check(ReferenceEquals(current, server.PreparedInput) && current.Active && b.IsPlaying(), "Failed active switch preserves recording and playback.");
        var devices = AudioServer.GetInputDeviceList(); AudioServer.InputDevice = devices[1]; Check(server.PreparedInput.Active && b.IsPlaying(), "Active microphone follows switched device."); Check(b.MixAudio(1, 128).Length == 128, "Device switch resets history without ending playback."); AudioServer.InputDevice = "Default"; b.Stop();
        before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) b.MixInto(output, 1); Check(GC.GetAllocatedBytesForCurrentThread() == before && output.All(v => v == Vector2.Zero), "Stopped microphone mixing stays silent and allocation-free.");
    }
    private static void VerifyOutput(AudioStreamMicrophone mic)
    {
        var server = AudioServer.Service; var root = new Node(); var player = new AudioStreamPlayer { Stream = mic }; root.AddChild(player); using var tree = new SceneTree(root); player.Play();
        var native = server.Native; var input = server.PreparedInput; Check(SDL.PauseAudioStreamDevice(input.Stream), "Pause capture for native output fixture."); var feed = new Vector2[512]; feed.AsSpan().Fill(new(.2f, -.35f));
        void Refill() => Put(input, feed);
        WaitPasses(native, 20, Refill); native.PrepareCapture(16000); WaitPasses(native, 20, Refill); var pcm = native.CapturedPCM();
        Check(pcm.Where((_, i) => i % native.Channels == 0).Any(v => Math.Abs(v - .2f) < .01f) && pcm.Where((_, i) => i % native.Channels == 1).Any(v => Math.Abs(v + .35f) < .01f), "Actual FAudio receives converted microphone stereo.");
        var bytes = native.MixManagedBytes; var inputBytes = Interlocked.Read(ref input.CallbackManagedBytes); var calls = FAudioContext.AllocationCalls; WaitPasses(native, 64, Refill);
        Check(native.MixManagedBytes == bytes && Interlocked.Read(ref input.CallbackManagedBytes) == inputBytes && FAudioContext.AllocationCalls == calls, "64 native capture/output passes allocate zero measured managed bytes and FAudio allocator calls.");
        WaitPasses(native, 30); tree.ProcessFrame(.01); Check(player.IsPlaying(), "Input underrun does not finish native microphone output.");
        player.StreamPaused = true; WaitPasses(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls; WaitPasses(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 paused output passes allocate zero measured bytes/calls.");
        player.StreamPaused = false; VerifyCallbackFailure(input); WaitPasses(native, 4); Reject<AggregateException>(() => tree.ProcessFrame(.01)); Check(!player.IsPlaying() && !input.Active, "Capture callback error stops native output before owner-frame reporting.");
        player.Stop();
        AudioServer.SetInputDeviceActive(true); tree.Dispose(); Check(server.PreparedInput.Active, "Last output detachment retains an independent manual recording request."); AudioServer.SetInputDeviceActive(false);
    }
    private static unsafe void Put(AudioInputDevice input, ReadOnlySpan<Vector2> frames)
    {
        fixed (Vector2* data = frames) Check(SDL.PutAudioStreamData(input.Stream, (nint)data, frames.Length * 8), "Native stereo input injection.");
    }
    private static unsafe void PutRaw(nint stream, ReadOnlySpan<float> frames)
    {
        fixed (float* data = frames) Check(SDL.PutAudioStreamData(stream, (nint)data, frames.Length * 4), "Native mono input injection.");
    }
    private static void Wait(Func<bool> ready)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp(); while (!ready()) { if (System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalSeconds > 5) throw new InvalidOperationException("Input callback deadline."); Thread.Sleep(1); }
    }
    private static void WaitPasses(FAudioContext native, long passes, Action? refill = null)
    {
        var target = native.MixPasses + passes; Wait(() => { refill?.Invoke(); return native.MixPasses >= target; });
    }
    private static void Check(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
