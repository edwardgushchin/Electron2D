using Path = System.IO.Path;
using System.Buffers.Binary;
using Electron2D;

internal static class MovieWriterTests
{
    internal static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    internal static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal sealed class Writer : MovieWriter
    {
        internal int Rate = 48000, FPS = 59, Limit = 8, Frames, Begins, Ends, Handles;
        internal AudioServer.SpeakerMode Mode;
        internal long Samples;
        internal double Sum, Max;
        internal string Fail = "", Extension = "movieprobe";
        internal Window? Window;
        internal Image? Borrowed;
        internal bool CheckGuards, Measure, Resize;
        internal long MeasuredBytes = -1, NativeAllocations = -1, IdleBytes = -1, IdleNative = -1;
        internal readonly List<int> Blocks = [];
        protected override int OnGetAudioMixRate() => Rate;
        protected override AudioServer.SpeakerMode OnGetAudioSpeakerMode() => Mode;
        protected override string[] OnGetSupportedExtensions()
        {
            if (Fail == "extensions") throw new FormatException("extensions failure");
            if (Fail == "reenter")
            {
                using var other = new Writer(); Reject<InvalidOperationException>(() => AddWriter(other));
                using var nested = new Window(); Reject<InvalidOperationException>(() => Engine.Run(nested, "nested.png"));
                using var loop = new HookLoop(); Reject<InvalidOperationException>(() => Engine.Start(loop));
            }
            return [Extension];
        }
        protected override bool OnHandlesFile(string path)
        {
            Handles++; if (Fail == "handles") throw new FormatException("handles failure");
            return Path.GetExtension(path).Equals("." + Extension, StringComparison.OrdinalIgnoreCase);
        }
        protected override void OnWriteBegin(Vector2i size, int fps, string path)
        {
            Begins++; Frames = 0; Samples = 0; Sum = Max = 0; Blocks.Clear();
            Check(size == new Vector2i(64, 48) && fps == FPS && Engine.GetWriteMoviePath() == path, "Begin size, cadence and path.");
            Check(AudioServer.GetMixRate() == Rate && AudioServer.GetSpeakerMode() == Mode, "Writer audio configuration reaches native graph.");
            if (Fail == "begin") throw new FormatException("begin failure");
        }
        protected override void OnWriteFrame(Image image, ReadOnlySpan<int> pcm)
        {
            Check(Borrowed is null || Borrowed.IsDisposed, "Previous borrowed frame is retired."); Borrowed = image;
            Check(image.Size == new Vector2i(64, 48) && image.PixelFormat == Image.Format.Rgb8, "Capture has stable RGB dimensions.");
            var color = image.GetPixel(5, 5); Check(color.R > .9f && color.G < .02f, "Real canvas pixel reaches writer.");
            if (Resize)
            {
                if (Frames == 0) { Window!.ContentScaleSize = new(96, 48); Window.ContentScaleMode = ContentScaleMode.Viewport; }
                else if (Frames == 1)
                {
                    using var full = Window!.GetTexture().GetImage()!; Check(full.Size == new Vector2i(96, 48), "Horizontal resize actually changed render target.");
                    Check(image.GetPixel(55, 5).R < .05f, "Horizontal centered cover crop."); Window.ContentScaleSize = new(32, 96);
                }
                else { using var full = Window!.GetTexture().GetImage()!; Check(full.Size == new Vector2i(32, 96) && image.GetPixel(5, 40).R < .05f, "Vertical centered cover crop and bilinear resize."); }
            }
            var channels = 2 + (int)Mode * 2;
            var expected = (int)((long)(Frames + 1) * Rate / FPS - (long)Frames * Rate / FPS);
            Check(pcm.Length == expected * channels, "Rational audio block frame count.");
            Blocks.Add(pcm.Length); Samples += pcm.Length / channels;
            for (var i = 0; i < pcm.Length; i++) { Sum += pcm[i] / 2147483648d; Max = Math.Max(Max, Math.Abs(pcm[i] / 2147483648d)); }
            Check(Engine.GetWriteMoviePath().EndsWith(".movieprobe", StringComparison.OrdinalIgnoreCase), "Output path remains live.");
            if (CheckGuards)
            {
                Reject<InvalidOperationException>(Dispose);
                Reject<InvalidOperationException>(() => AddWriter(this));
                using var nested = new Window(); Reject<InvalidOperationException>(() => Engine.Run(nested, "nested.png"));
                Reject<InvalidOperationException>(() => AudioServer.OutputDevice = "not-a-device");
            }
            if (Measure)
            {
                Span<int> block = stackalloc int[512];
                for (var n = 0; n < 128; n++) AudioServer.Service.ReadMoviePCM(block);
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true); GC.WaitForPendingFinalizers();
                var allocations = FAudioContext.AllocationCalls; var before = GC.GetAllocatedBytesForCurrentThread();
                for (var n = 0; n < 128; n++) AudioServer.Service.ReadMoviePCM(block);
                MeasuredBytes = GC.GetAllocatedBytesForCurrentThread() - before; NativeAllocations = FAudioContext.AllocationCalls - allocations;
                ((AudioStreamPlayer)Window!.GetChild(1)).Stop();
                for (var n = 0; n < 128; n++) AudioServer.Service.ReadMoviePCM(block);
                allocations = FAudioContext.AllocationCalls; before = GC.GetAllocatedBytesForCurrentThread();
                for (var n = 0; n < 128; n++) AudioServer.Service.ReadMoviePCM(block);
                IdleBytes = GC.GetAllocatedBytesForCurrentThread() - before; IdleNative = FAudioContext.AllocationCalls - allocations;
                foreach (var sample in block) Check(sample == 0, "Stopped graph emits silence.");
            }
            Frames++;
            if (Fail is "frame" or "both") throw new FormatException("frame failure");
            if (Frames == Limit) Window!.Tree!.Quit(17);
        }
        protected override void OnWriteEnd() { Ends++; if (Fail is "end" or "both") throw new FormatException("end failure"); }
    }
    internal static void Portable()
    {
        using var window = new Window();
        Reject<ArgumentException>(() => Engine.Run(window, "")); Reject<ArgumentOutOfRangeException>(() => Engine.Run(window, "x.png", 0));
        Check(!window.IsDisposed && Engine.GetWriteMoviePath() == "", "Invalid arguments do not consume root.");
        Reject<ArgumentNullException>(() => MovieWriter.AddWriter(null!));
        using var invalid = new Writer { Extension = ".bad" }; Reject<ArgumentException>(() => MovieWriter.AddWriter(invalid));
        using var failure = new Writer { Fail = "extensions" }; Reject<FormatException>(() => MovieWriter.AddWriter(failure));
        using var guarded = new Writer { Fail = "reenter" }; MovieWriter.AddWriter(guarded); guarded.Dispose();
        using var writer = new Writer(); MovieWriter.AddWriter(writer); Reject<InvalidOperationException>(() => MovieWriter.AddWriter(writer));
        Task.Run(() => Reject<InvalidOperationException>(() => MovieWriter.AddWriter(writer))).GetAwaiter().GetResult();
        var writers = Enumerable.Range(0, 7).Select(_ => new Writer()).ToArray();
        try { foreach (var item in writers) MovieWriter.AddWriter(item); using var ninth = new Writer(); Reject<InvalidOperationException>(() => MovieWriter.AddWriter(ninth)); }
        finally { foreach (var item in writers) item.Dispose(); }
        using var replacement = new Writer(); MovieWriter.AddWriter(replacement);
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-movie-settings-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using (var settings = new ProjectSettingsRegistry(folder))
            {
                Check(settings.Get(ProjectSettings.MovieWriterMixRate) == 48000 && settings.Get(ProjectSettings.MovieWriterSpeakerMode) == AudioServer.SpeakerMode.Stereo, "Built-in setting defaults.");
                Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.MovieWriterMixRate, 999));
                Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.MovieWriterSpeakerMode, (AudioServer.SpeakerMode)99));
                settings.Set(ProjectSettings.MovieWriterMixRate, 44100); settings.Set(ProjectSettings.MovieWriterSpeakerMode, AudioServer.SpeakerMode.Surround71); settings.Save();
            }
            using var loaded = new ProjectSettingsRegistry(folder); loaded.Load();
            Check(loaded.Get(ProjectSettings.MovieWriterMixRate) == 44100 && loaded.Get(ProjectSettings.MovieWriterSpeakerMode) == AudioServer.SpeakerMode.Surround71, "Independent project settings round-trip.");
        }
        finally { Directory.Delete(folder, recursive: true); }
        Console.WriteLine("Movie writer portable: arguments, registry ownership/capacity/duplicates, failure and reclamation passed.");
    }
    private static AudioStreamWAV Source()
    {
        var data = new byte[48000 * 2]; for (var i = 0; i < 48000; i++) BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), 8192);
        return new AudioStreamWAV { SampleFormat = AudioStreamWAV.Format.PCM16, Stereo = false, MixRate = 48000, Data = data, Loop = AudioLoopMode.Forward, LoopEnd = 47999 };
    }
    private static Window Scene(AudioStream source, bool sample = false, bool failReady = false)
    {
        var window = new Window { Size = new(64, 48), Title = "Electron2D offline movie" };
        var canvas = new Entity(); canvas.Draw += item => item.DrawRect(new(0, 0, 64, 48), Colors.Red); window.AddChild(canvas);
        var player = new AudioStreamPlayer { Stream = source, Autoplay = true, PlaybackType = sample ? AudioServer.PlaybackType.Sample : AudioServer.PlaybackType.Stream };
        window.AddChild(player);
        window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); if (failReady) throw new FormatException("ready failure"); };
        return window;
    }
    internal static void Native(string backend, string folder)
    {
        Directory.CreateDirectory(folder);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var source = Source(); using var writer = new Writer { CheckGuards = true }; MovieWriter.AddWriter(writer);
        foreach (var mode in Enum.GetValues<AudioServer.SpeakerMode>())
        {
            writer.Mode = mode; writer.Window = Scene(source, sample: mode == AudioServer.SpeakerMode.Surround51);
            var deltas = new List<double>(); var node = new DeltaNode(deltas); writer.Window.AddChild(node);
            Check(Engine.Run(writer.Window, Path.Combine(folder, "capture.movieprobe"), writer.FPS) == 17, "Recorded quit code.");
            Check(writer.Frames == 8 && writer.Samples == 8L * writer.Rate / writer.FPS && writer.Sum > 20 && writer.Max > .1, "Native PCM and drift-free audio clock.");
            Check(writer.Borrowed!.IsDisposed && writer.Window.IsDisposed && Engine.GetWriteMoviePath() == "", "Movie lifetime cleanup.");
            Check(deltas.Count >= 8 && deltas.All(v => Math.Abs(v - 1d / writer.FPS) < 1e-12), "Fixed process cadence.");
        }
        writer.Mode = AudioServer.SpeakerMode.Stereo; writer.Measure = true; writer.Limit = 1; writer.Window = Scene(source);
        Engine.Run(writer.Window, Path.Combine(folder, "allocation.movieprobe"), writer.FPS);
        Check(writer.MeasuredBytes == 0 && writer.NativeAllocations == 0 && writer.IdleBytes == 0 && writer.IdleNative == 0, "Prepared native movie PCM bytes=" + writer.MeasuredBytes + ", native calls=" + writer.NativeAllocations); writer.Measure = false;
        foreach (var stage in new[] { "handles", "begin", "frame", "end", "ready" })
        {
            writer.Fail = stage; writer.Limit = 1; var beforeEnds = writer.Ends; writer.Window = Scene(source, failReady: stage == "ready");
            ExpectFailure(stage + " failure", () => Engine.Run(writer.Window, Path.Combine(folder, "failure.movieprobe"), writer.FPS));
            Check(writer.Window.IsDisposed && Engine.GetWriteMoviePath() == "", "Failure closes the root and clears path: " + stage);
            Check(writer.Ends == beforeEnds + (stage == "handles" ? 0 : 1), "Balanced end hook: " + stage);
        }
        writer.Fail = ""; writer.Rate = 999; writer.Window = Scene(source); var ends = writer.Ends;
        Reject<InvalidOperationException>(() => Engine.Run(writer.Window, Path.Combine(folder, "invalid.movieprobe"), writer.FPS)); Check(writer.Ends == ends && writer.Window.IsDisposed, "Bad format is rejected before begin."); writer.Rate = 48000;
        writer.FPS = 1; writer.Limit = 2; writer.Window = Scene(source); var physicsBefore = Engine.PhysicsFrames;
        Engine.Run(writer.Window, Path.Combine(folder, "slow.movieprobe"), writer.FPS);
        Check(Engine.PhysicsFrames - physicsBefore == 120 && writer.Samples == 96000, "Low recording FPS retains all due physics and audio steps.");
        writer.FPS = 7; writer.Limit = 7; writer.Window = Scene(source); physicsBefore = Engine.PhysicsFrames;
        Engine.Run(writer.Window, Path.Combine(folder, "rational-physics.movieprobe"), writer.FPS);
        Check(Engine.PhysicsFrames - physicsBefore == 60 && writer.Samples == 48000, "One second at 7 FPS must contain exactly 60 physics ticks and 48000 audio frames.");
        writer.FPS = 59; writer.Limit = 2; writer.Window = Scene(source); var scaledDeltas = new List<double>(); writer.Window.AddChild(new DeltaNode(scaledDeltas));
        Engine.TimeScale = .25;
        try { Engine.Run(writer.Window, Path.Combine(folder, "scaled.movieprobe"), writer.FPS); }
        finally { Engine.TimeScale = 1; }
        Check(scaledDeltas.All(v => Math.Abs(v - .25 / 59) < 1e-12) && writer.Samples == 2L * 48000 / 59, "TimeScale changes scene deltas independently of audio duration.");
        using (var effect = new BrokenEffect())
        {
            AudioServer.AddBusEffect(0, effect); writer.Window = Scene(source); var frameBefore = writer.Frames;
            try { ExpectFailure("native effect failure", () => Engine.Run(writer.Window, Path.Combine(folder, "effect.movieprobe"), 59)); }
            finally { AudioServer.RemoveBusEffect(0, 0); }
            Check(writer.Window.IsDisposed, "Native effect error closes recording.");
        }
        writer.Fail = "both"; writer.Window = Scene(source);
        try { Engine.Run(writer.Window, Path.Combine(folder, "both.movieprobe"), 59); throw new InvalidOperationException("Expected aggregated frame/end failures."); }
        catch (AggregateException error) { Check(error.ToString().Contains("frame failure") && error.ToString().Contains("end failure"), "Both callback failures survive cleanup."); }
        writer.Fail = ""; writer.Resize = true; writer.Limit = 3; writer.Window = Scene(source);
        Engine.Run(writer.Window, Path.Combine(folder, "resize.movieprobe"), 59); writer.Resize = false;
        writer.Limit = 2; writer.Window = Scene(source);
        var priorContext = SynchronizationContext.Current; var context = new PumpContext(); SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var pending = Engine.RunAsync(writer.Window, Path.Combine(folder, "async.movieprobe"), 59);
            var deadline = System.Diagnostics.Stopwatch.StartNew();
            while (!pending.IsCompleted) { if (deadline.Elapsed.TotalSeconds > 10) throw new TimeoutException("Async movie host did not finish."); context.Pump(); }
            Check(pending.GetAwaiter().GetResult() == 17 && writer.Frames == 2, "Yielding recording uses the same owner and frame/audio flow.");
        }
        finally { SynchronizationContext.SetSynchronizationContext(priorContext); }
        writer.Dispose();
        using (var unknown = new Window()) { Reject<NotSupportedException>(() => Engine.Run(unknown, Path.Combine(folder, "unknown.not-a-writer"))); Check(unknown.IsDisposed, "Unsupported output consumes/cleans a reserved root."); }
        File.WriteAllText(Path.Combine(folder, "sequence00000009.png"), "old-frame");
        File.WriteAllText(Path.Combine(folder, "sequence-backup.png"), "unrelated");
        var pngWindow = Scene(source); pngWindow.AddChild(new QuitNode());
        Engine.Run(pngWindow, Path.Combine(folder, "sequence.png"), 59);
        Check(!File.Exists(Path.Combine(folder, "sequence00000009.png")) && File.Exists(Path.Combine(folder, "sequence-backup.png")), "Only the exact prior sequence is replaced.");
        var wav = File.ReadAllBytes(Path.Combine(folder, "sequence.wav"));
        Check(wav.AsSpan(0, 4).SequenceEqual("RIFF"u8) && BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(4)) == wav.Length - 8 && BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(40)) == wav.Length - 44, "Finalized real WAV header.");
        Check(BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(24)) == 48000 && wav.Length == 44 + 4 * 48000 / 59 * 8, "PNG writer audio rate/count.");
        using var image = Image.LoadFromFile(Path.Combine(folder, "sequence00000003.png")); Check(image.GetPixel(5, 5).R > .9f, "Independent PNG image load.");
        ProjectSettings.Set(ProjectSettings.MovieWriterMixRate, 44100); ProjectSettings.Set(ProjectSettings.MovieWriterSpeakerMode, AudioServer.SpeakerMode.Surround31);
        try
        {
            var alternative = Scene(source); alternative.AddChild(new QuitNode()); Engine.Run(alternative, Path.Combine(folder, "alternative.png"), 59);
            var bytes = File.ReadAllBytes(Path.Combine(folder, "alternative.wav"));
            Check(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(24)) == 44100 && BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(22)) == 4 && bytes.Length == 44 + (4 * 44100 / 59) * 16, "Project-backed built-in rate and speaker settings reach the actual WAV.");
        }
        finally { ProjectSettings.Set(ProjectSettings.MovieWriterMixRate, 48000); ProjectSettings.Set(ProjectSettings.MovieWriterSpeakerMode, AudioServer.SpeakerMode.Stereo); }
        var empty = new Window { Size = new(64, 48) }; empty.Ready += _ => empty.Tree!.Quit(); Engine.Run(empty, Path.Combine(folder, "empty.png"));
        Check(File.ReadAllBytes(Path.Combine(folder, "empty.wav")).Length == 44, "Ready quit produces a finalized empty recording.");
        Console.WriteLine($"Movie writer {backend}: 2/4/6/8 channel PCM, stream/sample voices, fixed deltas, rational clock, actual pixels, hooks/failures, PNG/WAV and zero-allocation native PCM passed.");
    }
    private static void ExpectFailure(string message, Action action)
    {
        try { action(); }
        catch (Exception error) { Check(error.ToString().Contains(message, StringComparison.Ordinal), "Wrong failure: " + error); return; }
        throw new InvalidOperationException("Expected failure: " + message);
    }
    private sealed class BrokenEffect : AudioEffect
    {
        protected override AudioEffectInstance OnInstantiate() => new BrokenInstance();
        private sealed class BrokenInstance : AudioEffectInstance
        { protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination) => throw new FormatException("native effect failure"); }
    }
    private sealed class HookLoop : MainLoop { }
    private sealed class PumpContext : SynchronizationContext
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _queue = new();
        public override void Post(SendOrPostCallback callback, object? state) => _queue.Enqueue((callback, state));
        internal void Pump() { if (_queue.TryDequeue(out var work)) work.Callback(work.State); else Thread.Sleep(1); }
    }
    private sealed class QuitNode : Node
    { private int _frames; protected override void OnProcess(double delta) { if (++_frames == 5) Tree!.Quit(); } }
    private sealed class DeltaNode(List<double> deltas) : Node
    { protected override void OnProcess(double delta) => deltas.Add(delta); }
}
