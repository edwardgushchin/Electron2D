using Electron2D;
using System.Buffers.Binary;

internal static class AnimationAudioTrackTests
{
    internal static void Run() { Authoring(); Console.WriteLine("Typed audio track authoring, keys, trims and copies passed."); }
    internal static void RunNative() { Authoring(); Execute(); Advanced(); Prepared(); Console.WriteLine("Audio animation tracks: typed keys/copies/trims, native start/seek/update-only/gain/cleanup, prepared voice capacity and warm checks passed."); }
    private static void Authoring()
    {
        using var wave = Tone(); using var clip = new Animation(); var track = clip.AddAudioTrack(); clip.TrackSetPath(track, "speaker"); clip.AudioTrackInsertKey(track, .2, wave, -1, -2); Check(clip.TrackGetType(track) == Animation.TrackType.Audio && clip.AudioTrackIsUseBlend(track), "Audio track identity/default."); Check(clip.AudioTrackGetKeyStartOffset(track, 0) == 0 && clip.AudioTrackGetKeyEndOffset(track, 0) == 0, "Negative trims clamp."); clip.AudioTrackSetKeyEndOffset(track, 0, .1); using var copy = (Animation)clip.Duplicate(); Check(ReferenceEquals(copy.AudioTrackGetKeyStream(track, 0), wave), "Shallow resource alias."); using var deep = (Animation)clip.Duplicate(true); using var copiedWave = deep.AudioTrackGetKeyStream(track, 0)!; Check(!ReferenceEquals(copiedWave, wave), "Deep source copy."); Reject<ArgumentException>(() => clip.TrackSetPath(track, "speaker:Stream")); Reject<ArgumentOutOfRangeException>(() => clip.AudioTrackSetKeyStartOffset(track, 0, double.NaN)); Reject<InvalidOperationException>(() => clip.ValueTrackGetUpdateMode(track)); Reject<InvalidCastException>(() => clip.TrackGetKeyValue<string>(track, 0));
    }
    private static void Execute()
    {
        using var wave = Tone(); using var clip = new Animation(); var track = clip.AddAudioTrack(); clip.TrackSetPath(track, "speaker"); clip.AudioTrackInsertKey(track, .2, wave); using var setup = new Setup(clip); setup.Player.Play("clip"); setup.Player.Advance(0); Check(!setup.Speaker.IsPlaying(), "No cue before first key."); setup.Player.Advance(.25); Check(setup.Speaker.Stream is AudioStreamPolyphonic && setup.Speaker.GetStreamPlayback() is AudioStreamPlaybackPolyphonic, "Prepared polyphonic receiver."); var native = AudioServer.Instance.Native; Check(Capture(native).Any(v => Math.Abs(v) > .03), "Cue reaches native PCM."); setup.Speaker.StreamPaused = true; Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Receiver pause reaches output."); setup.Speaker.StreamPaused = false; setup.Player.Stop(true); Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Controller stop cleans cue."); setup.Player.Play("clip"); setup.Player.Seek(.4, true, true); Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Update-only seek never starts sound."); setup.Player.Seek(.4, true); Check(Capture(native).Any(v => Math.Abs(v) > .03), "Seek starts preceding cue at elapsed offset."); setup.Player.ClearCaches(); Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Cache disposal releases audio transport.");
    }
    private static void Advanced()
    {
        using var wave = Tone(); using var clip = new Animation(); var track = clip.AddAudioTrack(); clip.TrackSetPath(track, "speaker"); clip.AudioTrackInsertKey(track, 0, wave, endOffset: .05); using var setup = new Setup(clip); setup.Player.Play("clip"); setup.Player.Advance(0); setup.Player.Advance(.06); var native = AudioServer.Instance.Native; Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "End trim expires even a looping source against clip time."); setup.Player.Stop(true); setup.Player.AudioMaxPolyphony = 0; setup.Player.Play("clip"); setup.Player.Advance(0); Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Zero polyphony emits no cue.");
        setup.Player.Stop(true); setup.Player.AudioMaxPolyphony = 2; setup.Speaker.PlaybackType = AudioServer.PlaybackType.Sample; clip.AudioTrackSetKeyEndOffset(track, 0, 0); setup.Player.Play("clip"); setup.Player.Advance(0); Check(Capture(native).Any(v => Math.Abs(v) > .03), "Prepared native-sample cue."); setup.Player.Pause(); Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Pause stops samples while retaining preparation.");
        using var random = new AudioStreamRandomizer { StreamsCount = 2, Mode = AudioStreamRandomizer.PlaybackMode.Sequential }; random.SetStream(0, wave); using var other = Tone(); random.SetStream(1, other); clip.AudioTrackSetKeyStream(track, 0, random); setup.Speaker.PlaybackType = AudioServer.PlaybackType.Stream; setup.Player.Play("clip"); setup.Player.Advance(0); var selected = random.GetLength(); setup.Player.Seek(0, true); Check(random.GetLength() == selected, "Prepared randomizer remains a live source with per-trigger selection."); setup.Player.Stop(true);
        using var synchronized = new AudioStreamSynchronized { StreamCount = 1 }; synchronized.SetSyncStream(0, random); clip.AudioTrackSetKeyStream(track, 0, synchronized); setup.Player.Play("clip"); setup.Player.Advance(0); Check(Capture(native).Any(v => Math.Abs(v) > .03), "Prepared composite contains a live per-start randomizer."); setup.Player.Seek(0, true); Check(Capture(native).Any(v => Math.Abs(v) > .03), "Prepared composite randomizer restarts without fixed preselection."); setup.Player.Stop(true);
        using var leaf = new AnimationNodeAnimation { Animation = "clip" }; setup.Player.Active = false; var tree = new AnimationTree { TreeRoot = leaf, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; tree.AddAnimationLibrary("", setup.Library); setup.Root.AddChild(tree); tree.Advance(0); Check(Capture(native).Any(v => Math.Abs(v) > .03), "Graph audio cue uses the same prepared transport."); tree.ClearCaches(); Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Graph cache cleanup."); tree.Dispose();
    }
    private static void Prepared()
    {
        using var wave = Tone(); using var clip = new Animation { LoopMode = SpriteFrames.LoopMode.Linear }; var track = clip.AddAudioTrack(); clip.TrackSetPath(track, "speaker"); clip.AudioTrackInsertKey(track, 0, wave); using var setup = new Setup(clip); setup.Player.AudioMaxPolyphony = 2; setup.Player.Play("clip"); setup.Player.Advance(0); var native = AudioServer.Instance.Native; for (var i = 0; i < 20; i++) { setup.Player.Seek(0, true); Wait(native, 1); }
        var bytes = GC.GetAllocatedBytesForCurrentThread(); var allocations = FAudioContext.AllocationCalls; var mixedBytes = native.MixManagedBytes; for (var i = 0; i < 64; i++) { setup.Player.Seek(0, true); Wait(native, 1); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes && FAudioContext.AllocationCalls == allocations && native.MixManagedBytes == mixedBytes, "Prepared repeated cue seek/start avoids owner managed/native storage allocation."); bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) setup.Player.Advance(0); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Idle audio controller frames allocate zero managed bytes.");
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_RENDERER") ?? "gpu"; var settings = ProjectSettings.Instance; var prior = settings.Get(ProjectSettings.RenderingMethod); settings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var wave = Tone(); using var clip = new Animation { Length = 2 }; var track = clip.AddAudioTrack(); clip.TrackSetPath(track, "speaker"); clip.AudioTrackInsertKey(track, 0, wave); using var library = new AnimationLibrary(); library.AddAnimation("cue", clip); var window = new Window { Size = new(240, 120), Title = "Electron2D audio animation tracks" }; var emitter = new AudioStreamEmitter { Name = "speaker", Position = new(120, 60), VolumeDB = -30 }; var player = new AnimationPlayer { PlaybackAutoCapture = false, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; player.AddAnimationLibrary("", library); var host = new Host(player); window.AddChild(emitter); window.AddChild(player); window.AddChild(host);
                Check(Engine.Instance.Run(window) == 0 && host.Completed && window.IsDisposed && !wave.IsDisposed && !clip.IsDisposed && !library.IsDisposed, "Audio track host cleanup preserves borrowed resources."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "animation-audio-tracks-host", backend, run, host.Completed, cleaned = window.IsDisposed }));
            }
        }
        finally { settings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private sealed class Host(AnimationPlayer player) : Entity
    {
        internal bool Completed;
        private int _stage;
        private bool _pixels;
        protected override void OnReady() { var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black); renderer.FramePostDraw += () => { using var pixels = renderer.Readback(); Check(pixels.GetPixel(20, 20).R > .9f, "Audio host draws actual canvas pixels."); _pixels = true; }; player.Play("cue"); player.Advance(0); ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            var native = AudioServer.Instance.Native;
            switch (_stage++)
            {
                case 0: Check(Capture(native).Any(v => Math.Abs(v) > .001), "Spatial cue reaches actual native output."); player.Pause(); break;
                case 1: Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Host pause stops output."); player.Play("cue"); player.Seek(.3, true); break;
                case 2: Check(Capture(native).Any(v => Math.Abs(v) > .001), "Host seek restarts output."); player.ClearCaches(); break;
                case 3: Check(Capture(native).All(v => Math.Abs(v) < 1e-6), "Host cache cleanup stops output."); Check(_pixels, "Renderer submitted audio-host canvas."); Completed = true; Tree!.Quit(); break;
            }
            QueueRedraw();
        }
        protected override void OnDraw() => DrawRect(new(16, 16, 24, 24), Colors.Red);
    }
    private static AudioStreamWAV Tone() { var data = new byte[8820]; for (var i = 0; i < data.Length / 2; i++) BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), (short)(Math.Sin(i * Math.PI * 2 * 440 / 44100) * 8000)); return new AudioStreamWAV { Data = data, SampleFormat = AudioStreamWAV.Format.PCM16, Loop = AudioLoopMode.Forward, LoopBegin = 0, LoopEnd = 4409 }; }
    private sealed class Setup : IDisposable
    {
        internal readonly Node Root = new(); internal readonly AudioStreamPlayer Speaker = new() { Name = "speaker" }; internal readonly AnimationPlayer Player = new() { PlaybackAutoCapture = false, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; internal readonly AnimationLibrary Library = new(); internal readonly SceneTree Scene;
        internal Setup(Animation clip) { Library.AddAnimation("clip", clip); Player.AddAnimationLibrary("", Library); Root.AddChild(Speaker); Root.AddChild(Player); Scene = new(Root); }
        public void Dispose() { Scene.Dispose(); Library.Dispose(); }
    }
    private static float[] Capture(FAudioContext context) { Wait(context, 4); context.PrepareCapture(context.QuantumFrames * context.Channels * 6); Wait(context, 8); return context.CapturedPCM(); }
    private static void Wait(FAudioContext context, int count) { var end = context.MixPasses + count; var started = System.Diagnostics.Stopwatch.GetTimestamp(); while (context.MixPasses < end) { if (System.Diagnostics.Stopwatch.GetElapsedTime(started) > TimeSpan.FromSeconds(5)) throw new InvalidOperationException("Native mixing timeout"); Thread.Sleep(1); } }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
