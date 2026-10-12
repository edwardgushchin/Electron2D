using Electron2D;
using System.Buffers.Binary;
using System.Diagnostics;
using Path = System.IO.Path;

internal static class AnimationCuePersistenceTests
{
    private static readonly PropertyDescriptor<Entity, Vector2> Position = new(nameof(Entity.Position), n => n.Position, (n, v) => n.Position = v);
    private static readonly Action<Box, Color> Paint = SetColor;
    private static readonly Action<Box, Gradient> Asset = SetGradient;
    private static readonly Action<Box, int> Number = SetNumber;
    private static readonly Action<Box, int> Failure = Fail;
    private static bool _registered;
    private static void SetColor(Box box, Color color) { box.Color = color; box.Calls++; box.QueueRedraw(); }
    private static void SetGradient(Box box, Gradient gradient) { box.Color = gradient.GetColor(0); box.Calls++; box.QueueRedraw(); }
    private static void SetNumber(Box box, int number) { box.Number = number; box.Calls++; }
    private static void Fail(Box box, int value) => throw new InvalidOperationException("loaded callback failure");
    private static void Register()
    {
        if (_registered) return;
        ResourceFileTypes.RegisterAnimationProperty("test.cues.position", Position);
        ResourceFileTypes.RegisterAnimationMethod("test.cues.color", Paint);
        ResourceFileTypes.RegisterAnimationMethod("test.cues.asset", Asset);
        ResourceFileTypes.RegisterAnimationMethod("test.cues.number", Number);
        ResourceFileTypes.RegisterAnimationMethod("test.cues.failure", Failure);
        ResourceFileTypes.RegisterAnimationMethod<Node, Color>("test.cues.other", static (_, _) => { });
        _registered = true;
    }
    internal static void Run()
    {
        Register(); var directory = Path.Combine(Path.GetTempPath(), "e2d-cues-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "cues.e2dres"); WriteFixture(path);
            using (var loaded = ResourceLoader.Load<AnimationLibrary>(path, ResourceLoader.CacheMode.IgnoreDeep)) Verify(loaded);
            Child(path, "verify"); Child(path, "unknown"); WAVFormats(directory);
            using var loadedAgain = ResourceLoader.Load<AnimationLibrary>(path, ResourceLoader.CacheMode.IgnoreDeep); Validate(loadedAgain.GetAnimation("control"));
            var prior = File.ReadAllBytes(path); using var bad = new Animation(); var t = bad.AddMethodTrack<Box>();
            bad.TrackInsertKey<AnimationMethodKey<Box>>(t, 0, new AnimationMethodKey<Box, int>("unregistered", static (_, _) => { }, 1));
            Reject<AggregateException>(() => ResourceSaver.Save(bad, path)); Check(File.ReadAllBytes(path).SequenceEqual(prior), "Unknown callback cannot replace the file.");
            using var empty = new Animation(); empty.AddMethodTrack<Box>(); var emptyPath = Path.Combine(directory, "empty.e2dres"); ResourceSaver.Save(empty, emptyPath);
            using var emptyLoaded = ResourceLoader.Load<Animation>(emptyPath, ResourceLoader.CacheMode.Ignore); Check(emptyLoaded.GetTrackCount() == 1 && emptyLoaded.TrackGetKeyCount(0) == 0, "Empty registered receiver track persists.");
        }
        finally { Directory.Delete(directory, true); }
        Console.WriteLine("Animation cue files: heterogeneous methods, resource/audio aliases, nested playback, fresh schemas and atomic failures passed.");
    }
    internal static void WriteFixture(string path)
    {
        Register(); using var wave = Tone(); wave.Tags = new() { ["artist"] = "cue fixture" };
        using var gradient = new Gradient { Colors = [Colors.Green, Colors.Blue] };
        using var control = new Animation { Length = 1 }; var method = control.AddMethodTrack<Box>(); control.TrackSetPath(method, "target");
        control.TrackInsertKey<AnimationMethodKey<Box>>(method, 0, new AnimationMethodKey<Box, Color>("paint", Paint, Colors.Red));
        control.TrackInsertKey<AnimationMethodKey<Box>>(method, .5, new AnimationMethodKey<Box, Gradient>("asset color", Asset, gradient));
        control.TrackInsertKey<AnimationMethodKey<Box>>(method, .75, new AnimationMethodKey<Box, int>("number", Number, 7));
        control.TrackInsertKey<AnimationMethodKey<Box>>(method, 1, new AnimationMethodKey<Box, Color>("final paint", Paint, Colors.Blue));
        var audio = control.AddAudioTrack(); control.TrackSetPath(audio, "speaker"); control.AudioTrackSetUseBlend(audio, false); control.AudioTrackInsertKey(audio, 0, wave, .01, .02); control.AudioTrackInsertKey(audio, .5, wave, .01, .02);
        var nested = control.AddAnimationTrack(); control.TrackSetPath(nested, "child"); control.AnimationTrackInsertKey(nested, 0, "left"); control.AnimationTrackInsertKey(nested, .5, "right"); control.AnimationTrackInsertKey(nested, 1, "[stop]");
        using var left = Pose(new Vector2(16, 16)); using var right = Pose(new Vector2(32, 16)); using var library = new AnimationLibrary();
        using var failure = new Animation(); var failureTrack = failure.AddMethodTrack<Box>(); failure.TrackSetPath(failureTrack, "target"); failure.TrackInsertKey<AnimationMethodKey<Box>>(failureTrack, 0, new AnimationMethodKey<Box, int>("throw", Failure, 1)); library.AddAnimation("failure", failure);
        library.AddAnimation("control", control); library.AddAnimation("alias", control); library.AddAnimation("left", left); library.AddAnimation("right", right);
        ResourceSaver.Save(library, path, SaverFlags.BundleResources | SaverFlags.SaveBigEndian);
    }
    private static Animation Pose(Vector2 position) { var clip = new Animation(); var t = clip.AddTrack(Position); clip.TrackSetPath(t, "target:Position"); clip.TrackInsertKey(t, 0, position); return clip; }
    private static void Verify(AnimationLibrary library)
    {
        var control = library.GetAnimation("control"); Check(ReferenceEquals(control, library.GetAnimation("alias")), "Control alias persists.");
        Check(control.TrackGetType(0) == Animation.TrackType.Method && control.TrackGetType(1) == Animation.TrackType.Audio && control.TrackGetType(2) == Animation.TrackType.Animation, "All cue track kinds restore.");
        Check(control.MethodTrackGetName(0, 1) == "asset color" && control.MethodTrackGetParams<int>(0, 2) == 7, "Diagnostic names and mixed typed arguments persist.");
        Check(control.MethodTrackGetParams<Gradient>(0, 1).GetColor(0) == Colors.Green, "Resource method arguments persist.");
        var wave = (AudioStreamWAV)control.AudioTrackGetKeyStream(1, 0)!;
        Check(ReferenceEquals(wave, control.AudioTrackGetKeyStream(1, 1)) && !control.AudioTrackIsUseBlend(1) && control.AudioTrackGetKeyStartOffset(1, 0) == .01 && control.AudioTrackGetKeyEndOffset(1, 1) == .02, "Audio aliases, blend and trim metadata persist.");
        Check(wave.SampleFormat == AudioStreamWAV.Format.PCM16 && wave.MixRate == 44100 && !wave.Stereo && wave.Loop == AudioLoopMode.Forward && wave.LoopBegin == 0 && wave.LoopEnd == 4409 && wave.Data.Length == 8820 && wave.Tags["artist"] == "cue fixture", "WAV data and all authored metadata persist.");
        control.TrackSetEnabled(1, false);
        var root = new Node(); var box = new Box { Name = "target" }; var child = Player("child", library); var parent = Player("parent", library); root.AddChild(box); root.AddChild(child); root.AddChild(parent);
        using var tree = new SceneTree(root); parent.Play("control"); parent.Advance(0); child.Advance(0); Check(box.Color == Colors.Red && box.Position == new Vector2(16, 16) && child.IsPlaying(), "Loaded initial callback and nested pose execute.");
        parent.Advance(.5); child.Advance(0); Check(box.Color == Colors.Green && box.Position == new Vector2(32, 16), "Loaded resource callback and child selection execute.");
        parent.Advance(.5); Check(box.Color == Colors.Blue && box.Number == 7 && !child.IsPlaying() && box.Calls == 4, "Loaded mixed callbacks and child stop execute in order.");
        parent.Stop(true); box.Calls = 0; parent.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Deferred; parent.Play("control"); parent.Advance(0);
        Check(box.Calls == 0, "Loaded deferred key waits for the safe point."); tree.FlushDeferred(); Check(box.Calls == 1, "Loaded deferred key delivers.");
        parent.Advance(.5); parent.ClearCaches(); tree.FlushDeferred(); Check(box.Calls == 2 && box.Color == Colors.Green, "Accepted loaded resource arguments survive cache clearing.");
        parent.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate; parent.Play("failure"); Reject<InvalidOperationException>(() => parent.Advance(0)); Check(!parent.IsPlaying(), "Loaded callback failure pauses unchanged playback.");
    }
    private static void WAVFormats(string directory)
    {
        using var source = Tone(); var riffPath = Path.Combine(directory, "source.wav"); source.SaveToWAV(riffPath); var riff = File.ReadAllBytes(riffPath);
        foreach (var format in new[] { AudioStreamWAV.Format.PCM8, AudioStreamWAV.Format.PCM16, AudioStreamWAV.Format.IMAADPCM, AudioStreamWAV.Format.QOA })
        {
            var options = new AudioWAVImportOptions { Force8Bit = format == AudioStreamWAV.Format.PCM8, Compression = format is AudioStreamWAV.Format.IMAADPCM or AudioStreamWAV.Format.QOA ? format : null };
            using var wave = AudioStreamWAV.LoadFromBuffer(riff, options); wave.Tags = new() { ["title"] = format.ToString() };
            var path = Path.Combine(directory, format + ".e2dres"); var bytes = wave.Data; var length = wave.GetLength(); ResourceSaver.Save(wave, path, SaverFlags.SaveBigEndian);
            using var loaded = ResourceLoader.Load<AudioStreamWAV>(path, ResourceLoader.CacheMode.Ignore);
            Check(loaded.SampleFormat == format && loaded.Data.SequenceEqual(bytes) && Math.Abs(loaded.GetLength() - length) < 1e-8 && loaded.Tags["title"] == format.ToString(), "Every WAV storage format round-trips and decodes.");
            var copy = loaded.Data; if (copy.Length != 0) copy[0] ^= 1; Check(loaded.Data.SequenceEqual(bytes), "Loaded bytes stay independently owned.");
        }
    }
    private static void Validate(Animation control)
    {
        Reject<InvalidOperationException>(() => ResourceFileTypes.RegisterAnimationMethod("test.cues.color", Paint));
        Reject<InvalidOperationException>(() => ResourceFileTypes.RegisterAnimationMethod("test.cues.duplicate", Paint));
        Reject<ArgumentException>(() => ResourceFileTypes.RegisterAnimationMethod("bad\nmethod", Paint));
        var schemas = (PropertyDescriptor<Animation, string[]>)control.GetPropertyList().Single(p => p.Name == "Track[0].MethodSchemas");
        var before = control.MethodTrackGetName(0, 0);
        Reject<InvalidDataException>(() => schemas.SetValue(control, ["test.cues.other", "test.cues.asset", "test.cues.number", "test.cues.color"]));
        Reject<InvalidDataException>(() => schemas.SetValue(control, ["unknown", "test.cues.asset", "test.cues.number", "test.cues.color"]));
        Reject<InvalidDataException>(() => schemas.SetValue(control, [])); Check(control.MethodTrackGetName(0, 0) == before, "Wrong schema/receiver is transactional.");
        var trimmed = (PropertyDescriptor<Animation, double>)control.GetPropertyList().Single(p => p.Name == "Track[1].Key[0].StartOffset");
        Reject<ArgumentOutOfRangeException>(() => trimmed.SetValue(control, double.NaN));
    }
    private static void Child(string path, string mode)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; start.ArgumentList.Add(typeof(AnimationCuePersistenceTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_CUE_FILE_CHILD"] = path; start.Environment["ELECTRON2D_TEST_CUE_FILE_MODE"] = mode; start.Environment.Remove("ELECTRON2D_TEST_CUE_FILE");
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Cue file child."); }
        Check(child.ExitCode == 0, "Fresh cue process: " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string path)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_CUE_FILE_MODE") == "unknown") { Reject<InvalidDataException>(() => ResourceLoader.Load<AnimationLibrary>(path, ResourceLoader.CacheMode.IgnoreDeep)); return; }
        Register(); using var library = ResourceLoader.Load<AnimationLibrary>(path, ResourceLoader.CacheMode.IgnoreDeep); Verify(library); Console.WriteLine("Fresh cues restored and executed.");
    }
    internal static void RunNative(string path)
    {
        Register(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_RENDERER") ?? "gpu"; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            using var library = ResourceLoader.Load<AnimationLibrary>(path, ResourceLoader.CacheMode.IgnoreDeep);
            var window = new Window { Size = new(80, 64), Title = "Electron2D loaded cues" }; var box = new Box { Name = "target" }; var speaker = new AudioStreamPlayer { Name = "speaker", VolumeDB = -30 }; var child = Player("child", library); var parent = Player("parent", library);
            window.AddChild(box); window.AddChild(speaker); window.AddChild(child); window.AddChild(parent); var stage = 0;
            box.Start = () =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Actual requested renderer."); RenderingServer.SetDefaultClearColor(Colors.Black); parent.Play("control"); parent.Advance(0); child.Advance(0);
                RenderingServer.FramePostDraw += () =>
                {
                    using var pixels = RenderingServer.Service!.Readback(); var color = stage == 0 ? Colors.Red : stage == 1 ? Colors.Green : Colors.Blue; var x = stage == 0 ? 16 : 32;
                    Check(pixels.GetPixel(x, 16) == color, "Loaded callback/nested pose reaches real pixels.");
                    if (stage < 2) Check(Capture(AudioServer.Service.Native).Any(v => Math.Abs(v) > .0001), "Loaded audio cue reaches native PCM.");
                    else { Check(box.Number == 7 && !child.IsPlaying(), "Loaded method payload and stop."); parent.ClearCaches(); Check(Capture(AudioServer.Service.Native).All(v => Math.Abs(v) < 1e-6), "Loaded cue cleanup reaches PCM silence."); }
                    stage++; if (stage == 3) window.Tree!.Quit(); else { parent.Advance(.5); child.Advance(0); }
                };
            };
            Check(Engine.Run(window) == 0 && stage == 3 && window.IsDisposed && !library.IsDisposed, "Cue host borrowing/cleanup."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "loaded-cues", backend, stages = stage, cleaned = window.IsDisposed }));
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private static AnimationPlayer Player(string name, AnimationLibrary library) { var player = new AnimationPlayer { Name = name, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual, CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate, PlaybackAutoCapture = false }; player.AddAnimationLibrary("", library); return player; }
    private static AudioStreamWAV Tone() { var data = new byte[8820]; for (var i = 0; i < data.Length / 2; i++) BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), (short)(Math.Sin(i * Math.PI * 2 * 440 / 44100) * 8000)); return new AudioStreamWAV { Data = data, SampleFormat = AudioStreamWAV.Format.PCM16, Loop = AudioLoopMode.Forward, LoopBegin = 0, LoopEnd = 4409 }; }
    private static float[] Capture(FAudioContext context) { Wait(context, 4); context.PrepareCapture(context.QuantumFrames * context.Channels * 6); Wait(context, 8); return context.CapturedPCM(); }
    private static void Wait(FAudioContext context, int count) { var end = context.MixPasses + count; var started = Stopwatch.GetTimestamp(); while (context.MixPasses < end) { if (Stopwatch.GetElapsedTime(started) > TimeSpan.FromSeconds(5)) throw new TimeoutException("Native cue PCM."); Thread.Sleep(1); } }
    private sealed class Box : Entity { internal Color Color; internal int Number, Calls; internal Action? Start; protected override void OnReady() => Start?.Invoke(); protected override void OnDraw() => DrawRect(new(-3, -3, 6, 6), Color); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
