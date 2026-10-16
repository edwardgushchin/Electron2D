using System.Runtime.InteropServices;
using System.Text.Json;
using Electron2D;
using Path = System.IO.Path;
using static MovieWriterTests;

internal static class VideoRenderingTests
{
    internal static void Run(string backend, string folder)
    {
        Directory.CreateDirectory(folder);
        var sound = VideoPlaybackTests.Fixture(folder, "theora-vorbis.ogv");
        var silent = VideoPlaybackTests.Fixture(folder, "theora-silent.ogv");
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using (var source = new VideoStreamTheora { File = sound })
        using (var writer = new Writer(folder))
        {
            MovieWriter.AddWriter(writer);
            var window = new Window { Size = new(256, 192), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
            var view = new VideoStreamPlayer { Stream = source, Autoplay = true, Loop = true, Expand = true, Position = new(4, 4), Size = new(68, 52) };
            writer.View = view; window.AddChild(view);
            writer.Alias = new TextureRect { Texture = view.GetVideoTexture(), Position = new(100, 4), Size = new(68, 52), ExpandMode = TextureRectExpandMode.IgnoreSize };
            window.AddChild(writer.Alias);
            window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); FaultCallbacks(); };
            Check(Engine.Run(window, Path.Combine(folder, "playback.videoprobe"), 60) == 0 && window.IsDisposed, "Video-to-movie public lifecycle completes.");
            Check(writer.Count == 100 && writer.Audible && writer.Silent && writer.Peak > .02 && writer.Peak < .5, "Actual FAudio soundtrack, gain, track changes and silence execute.");
        }
        Warm(backend, silent);
        Console.WriteLine($"Video native {backend}: 100 real pixel/PCM recording frames and 64 active/64 idle prepared render frames passed.");
    }
    private static void FaultCallbacks()
    {
        using var source = new FaultStream();
        var player = new VideoStreamPlayer { Stream = source, Autoplay = true };
        using var tree = new SceneTree(player);
        tree.ProcessFrame(.01);
        var audio = player.GetChildren(true).OfType<AudioStreamPlayer>().Single();
        source.FailStop = true;
        var stopped = Fails(player.Stop); source.FailStop = false;
        Check(stopped && audio.StreamPaused, "A failing decoder Stop still pauses its native soundtrack.");
        player.Play(); source.FailPlay = true;
        var started = Fails(player.Play); source.FailPlay = false;
        Check(started && audio.StreamPaused && !player.IsPlaying(), "A failing decoder Play attempts complete transport cleanup.");
        player.Play(); source.FailPause = true;
        var paused = Fails(() => player.Paused = true);
        source.FailPause = false;
        Check(paused && audio.StreamPaused, "A failing decoder pause still pauses its native soundtrack.");
    }
    private static bool Fails(Action action) { try { action(); return false; } catch (Exception) { return true; } }
    private sealed class FaultStream : VideoStream
    {
        internal bool FailPlay, FailStop, FailPause;
        protected override VideoStreamPlayback OnInstantiatePlayback() => new FaultPlayback(this);
    }
    private sealed class FaultPlayback(FaultStream source) : VideoStreamPlayback
    {
        private bool _active, _paused;
        protected override int OnGetChannels() => 2;
        protected override int OnGetMixRate() => 48000;
        protected override double OnGetLength() => 0;
        protected override double OnGetPlaybackPosition() => 0;
        protected override Texture? OnGetTexture() => null;
        protected override bool OnIsPlaying() => _active;
        protected override bool OnIsPaused() => _paused;
        protected override void OnPlay() { if (source.FailPlay) throw new ApplicationException("video play"); _active = true; }
        protected override void OnStop() { if (source.FailStop) throw new ApplicationException("video stop"); _active = false; }
        protected override void OnSetPaused(bool paused) { if (source.FailPause) throw new ApplicationException("video pause"); _paused = paused; }
        protected override void OnSetAudioTrack(int index) { }
        protected override void OnSeek(double time) { }
        protected override void OnUpdate(double delta) { }
    }
    private sealed class Writer(string folder) : MovieWriter
    {
        internal VideoStreamPlayer View = null!;
        internal TextureRect Alias = null!;
        internal int Count;
        internal bool Audible, Silent;
        internal double Peak;
        private FileStream? _pcm;
        private readonly List<(int Frame, int ReferenceFrame, double Position, double RMS, double Peak, int Samples)> _stats = [];
        private readonly byte[] _reference = VideoPlaybackTests.Expected("theora-vorbis.ogv");
        private double _previousPosition;
        protected override string[] OnGetSupportedExtensions() => ["videoprobe"];
        protected override bool OnHandlesFile(string path) => Path.GetExtension(path) == ".videoprobe";
        protected override int OnGetAudioMixRate() => 48000;
        protected override AudioServer.SpeakerMode OnGetAudioSpeakerMode() => AudioServer.SpeakerMode.Stereo;
        protected override void OnWriteBegin(Vector2i size, int fps, string path)
        {
            Check(fps == 60 && AudioServer.GetMixRate() == 48000, "Video uses the recording graph frequency.");
            _pcm = File.Create(Path.Combine(folder, "soundtrack.s32le"));
        }
        protected override void OnWriteFrame(Image frameImage, ReadOnlySpan<int> audioFrameBlock)
        {
            Check(View.GetViewport()!.GetTexture().GetSize() == new Vector2(frameImage.Width, frameImage.Height), "Video pixel fixture requires stable native viewport dimensions.");
            using var source = View.GetVideoTexture()!.GetImage()!;
            Check(ReferenceEquals(Alias.Texture, View.GetVideoTexture()), "External texture consumers retain the same live frame through track changes.");
            var position = View.StreamPosition;
            var referenceFrame = position == 0 && _previousPosition > 0 ? 8 : Math.Clamp((int)Math.Floor(position * 30000 / 1001), 0, 8);
            for (var y = 0; y < 26; y++) for (var x = 0; x < 34; x++)
                {
                    var offset = (referenceFrame * 34 * 26 + y * 34 + x) * 4;
                    var expected = new Color(_reference[offset] / 255f, _reference[offset + 1] / 255f, _reference[offset + 2] / 255f);
                    var actual = frameImage.GetPixel(4 + x * 2, 4 + y * 2);
                    Check(frameImage.GetPixel(100 + x * 2, 4 + y * 2).IsEqualApprox(actual), "An independent TextureRect observes every prepared video revision.");
                    if (Math.Abs((int)MathF.Round(actual.R * 255) - _reference[offset]) > 3 || Math.Abs((int)MathF.Round(actual.G * 255) - _reference[offset + 1]) > 3 || Math.Abs((int)MathF.Round(actual.B * 255) - _reference[offset + 2]) > 3)
                    {
                        frameImage.SavePNG(Path.Combine(folder, "failed-frame.png")); source.SavePNG(Path.Combine(folder, "failed-source.png"));
                        throw new InvalidOperationException($"Video frame {Count}, source {x},{y}: expected {expected}, actual {actual}; output {frameImage.Width}x{frameImage.Height}, view {View.Position}/{View.Size}, viewport {View.GetViewport()!.GetTexture().GetSize()}.");
                    }
                }
            Check(frameImage.GetPixel(0, 0).IsEqualApprox(Colors.Black), "Video preserves the background outside its control.");
            double energy = 0, peak = 0;
            foreach (var sample in audioFrameBlock) { var value = sample / 2147483648d; energy += value * value; peak = Math.Max(peak, Math.Abs(value)); }
            var rms = Math.Sqrt(energy / audioFrameBlock.Length); Peak = Math.Max(Peak, peak);
            if (Count is >= 4 and < 20) Audible |= rms > .02;
            if (Count is >= 13 and < 17) Check(rms > .02, "An ignored out-of-range seek preserves the queued soundtrack.");
            if (Count >= 84) { Check(peak == 0, "Zero video volume silences every output sample."); Silent = true; }
            _stats.Add((Count, referenceFrame, position, rms, peak, audioFrameBlock.Length));
            _previousPosition = position;
            _pcm!.Write(MemoryMarshal.AsBytes(audioFrameBlock));
            if (Count is 0 or 8 or 16 or 40 or 80) frameImage.SavePNG(Path.Combine(folder, $"frame-{Count}.png"));
            if (Count == 9) View.StreamPosition = View.GetStreamLength() + 1;
            if (Count == 29) View.AudioTrack = -1;
            if (Count == 39) View.AudioTrack = 0;
            if (Count == 59) { View.Bus = "missing-video-bus"; View.Volume = .5f; }
            if (Count == 79) View.Volume = 0;
            if (++Count == 100) View.Tree!.Quit();
        }
        protected override void OnWriteEnd()
        {
            _pcm?.Dispose(); _pcm = null;
            using var output = File.Create(Path.Combine(folder, "pcm-frames.json"));
            using var json = new Utf8JsonWriter(output);
            json.WriteStartArray();
            foreach (var item in _stats)
            {
                json.WriteStartObject(); json.WriteNumber("frame", item.Frame); json.WriteNumber("referenceFrame", item.ReferenceFrame);
                json.WriteNumber("position", item.Position); json.WriteNumber("rms", item.RMS); json.WriteNumber("peak", item.Peak); json.WriteNumber("samples", item.Samples); json.WriteEndObject();
            }
            json.WriteEndArray();
        }
    }
    private static void Warm(string backend, string path)
    {
        using var source = new VideoStreamTheora { File = path };
        var window = new Window { Size = new(256, 192), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var view = new VideoStreamPlayer { Stream = source, Autoplay = true, Loop = true, Expand = true, Size = new(68, 52) }; window.AddChild(view);
        var frames = 0; long before = 0, active = 0, idle = 0;
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frames < 84) view.StreamPosition = (frames % 9 + .01) * 1001 / 30000;
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                if (frames is >= 20 and < 84) active += bytes;
                if (frames >= 84) idle += bytes;
                if (++frames == 84) view.Stop();
                if (frames == 148) window.Tree!.Quit();
            };
        };
        Engine.Run(window);
        Check(active == 0 && idle == 0, $"Prepared video seek/process/render allocated {active}/{idle} managed bytes on {backend}.");
    }
}
