using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;
using static MovieWriterTests;

internal static class VideoPlaybackTests
{
    internal static string Fixture(string folder, string name)
    {
        using var input = typeof(VideoPlaybackTests).Assembly.GetManifestResourceStream("TestVideo." + name)!;
        using var output = File.Create(Path.Combine(folder, name)); input.CopyTo(output); return output.Name;
    }
    internal static byte[] Expected(string name)
    {
        using var input = typeof(VideoPlaybackTests).Assembly.GetManifestResourceStream("TestVideo." + name + ".rgba")!;
        using var output = new MemoryStream(); input.CopyTo(output); return output.ToArray();
    }
    internal static void Managed()
    {
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-video-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var silent = Fixture(folder, "theora-silent.ogv"); var sound = Fixture(folder, "theora-vorbis.ogv");
            Defaults(); Decode(silent, "theora-silent.ogv"); Decode(sound, "theora-vorbis.ogv");
            Scene(silent); ColdTransfer(silent); Delay(silent, folder); Extensions(); AudioQueue(); LoadingAndStorage(sound, silent, folder); Warm(silent);
        }
        finally { Directory.Delete(folder, true); }
        Console.WriteLine("Video managed: independent pixels, decoder state, scene clocks, callbacks, loading/storage and zero-byte prepared playback passed.");
    }
    private static void Defaults()
    {
        using var player = new VideoStreamPlayer();
        Check(player.Stream is null && !player.Autoplay && !player.Expand && !player.Loop && !player.Paused && player.SpeedScale == 1 && player.AudioTrack == 0 && player.BufferingMSEC == 500 && player.Bus == "Master" && player.Volume == 1 && player.VolumeDB == 0, "Video defaults.");
        Check(!player.IsPlaying() && player.GetStreamName() == "<No Stream>" && player.GetStreamLength() == 0 && player.GetVideoTexture() is null && player.GetMinimumSize() == Vector2.Zero, "Unassigned video observations.");
        Reject<InvalidOperationException>(player.Play);
        Reject<ArgumentOutOfRangeException>(() => player.SpeedScale = float.NaN); Reject<ArgumentOutOfRangeException>(() => player.SpeedScale = -1);
        Reject<ArgumentOutOfRangeException>(() => player.Volume = float.PositiveInfinity); Reject<ArgumentOutOfRangeException>(() => player.Volume = -1);
        Reject<ArgumentOutOfRangeException>(() => player.StreamPosition = double.NaN);
        Reject<ArgumentOutOfRangeException>(() => player.AudioTrack = -2); Reject<ArgumentOutOfRangeException>(() => player.BufferingMSEC = 0);
        player.VolumeDB = float.NegativeInfinity; Check(player.Volume == 0, "Zero gain and negative-infinity dB agree.");
        player.Stop(); player.Dispose(); Reject<ObjectDisposedException>(() => _ = player.Stream);
    }
    private static void Decode(string path, string name)
    {
        using var source = new VideoStreamTheora { File = path };
        using var playback = source.InstantiatePlayback(); using var other = source.InstantiatePlayback();
        Check(!ReferenceEquals(playback, other) && playback.GetLength() == .3003, "Independent playback and rational video duration.");
        var texture = playback.GetTexture()!; var expected = Expected(name); var extent = 34 * 26 * 4;
        Check(texture.GetSize() == new Vector2(34, 26), "Visible cropped picture dimensions.");
        foreach (var frame in new[] { 0, 1, 2, 8, 3, 0, 7 })
        {
            playback.Seek((frame + .01) * 1001 / 30000);
            using var image = texture.GetImage()!; var pixels = image.GetData();
            Check(pixels.Length == extent && pixels.Where((value, i) => Math.Abs(value - expected[frame * extent + i]) > 3).Count() == 0, "Decoded pixels agree with the independent oracle, including rewinds.");
        }
        Check(ReferenceEquals(texture, playback.GetTexture()) && other.GetPlaybackPosition() == 0, "Seek preserves borrowed texture identity and independent cursors.");
        using var frozen = (ImageTexture)texture.Duplicate(); using var frozenBefore = frozen.GetImage()!; var frozenBytes = frozenBefore.GetData();
        playback.Play(); playback.SetPaused(true); playback.Update(.2); Check(playback.IsPlaying() && playback.GetPlaybackPosition() == 0, "Paused playback remains active.");
        playback.SetPaused(false); playback.Update(.12); Check(Math.Abs(playback.GetPlaybackPosition() - .12) < 1e-12, "Source delta advances the timeline.");
        using var frozenAfter = frozen.GetImage()!; Check(frozenAfter.GetData().SequenceEqual(frozenBytes), "Duplicated video image freezes the current pixels independently of later decoding.");
        var position = playback.GetPlaybackPosition(); playback.Seek(1); Check(playback.GetPlaybackPosition() == position, "Built-in seek beyond duration is ignored.");
        using var before = texture.GetImage()!; var bytes = before.GetData(); playback.Stop();
        using var after = texture.GetImage()!; Check(!playback.IsPlaying() && playback.GetPlaybackPosition() == 0 && bytes.SequenceEqual(after.GetData()), "Stop rewinds time while retaining pixels.");
        playback.Play(); playback.Update(1); Check(!playback.IsPlaying() && playback.GetPlaybackPosition() == 0, "Natural completion stops and resets time.");
        playback.Play(); playback.Seek(-.05); playback.Update(.02); Check(Math.Abs(playback.GetPlaybackPosition() + .03) < 1e-12, "Negative source time provides a real first-picture preroll."); playback.Stop();
        using (var temporary = Image.CreateEmpty(34, 26, false, Image.Format.Rgba8)) ((ImageTexture)texture).Update(temporary);
        playback.Seek(.2); using (var restored = texture.GetImage()!) Check(restored.GetData().Any(value => value != 0), "A matching public image update preserves later prepared video publication.");
        playback.SetAudioTrack(-1); Check(playback.GetChannels() == 0 && playback.GetMixRate() == 0, "Audio can be explicitly disabled.");
        Reject<ArgumentOutOfRangeException>(() => playback.Update(-1)); Reject<ArgumentOutOfRangeException>(() => playback.Seek(double.PositiveInfinity));
        Check(playback.MixAudio(0) == -1, "Standalone playback has no audio sink.");
        Check(Task.Run(() => Capture(playback.Play)).Result is InvalidOperationException, "Decoder control is owner-affine.");
    }
    private static void Scene(string path)
    {
        using var source = new VideoStreamTheora { File = path };
        var player = new VideoStreamPlayer { Stream = source, Autoplay = true };
        using var tree = new SceneTree(player); var finished = 0; player.Finished += () => finished++;
        Check(player.GetMinimumSize() == new Vector2(34, 26), "Video owns an intrinsic minimum."); player.Expand = true; Check(player.GetMinimumSize() == Vector2.Zero, "Expand removes the intrinsic minimum.");
        tree.ProcessFrame(.1); Check(player.IsPlaying() && player.StreamPosition == 0, "First process frame uses zero video delta.");
        tree.ProcessFrame(.1); Check(Math.Abs(player.StreamPosition - .1) < 1e-12, "Scene process advances source time.");
        player.Paused = true; tree.ProcessFrame(.1); Check(player.IsPlaying() && player.StreamPosition == .1, "Explicit pause preserves active time.");
        player.Paused = false; player.SpeedScale = 0; tree.ProcessFrame(.1); Check(player.StreamPosition == .1, "Zero speed freezes time.");
        player.SpeedScale = 2; tree.Paused = true; tree.ProcessFrame(.1); Check(player.StreamPosition == .1, "Scene pause freezes time.");
        tree.Paused = false; tree.ProcessFrame(.11); Check(!player.IsPlaying() && finished == 1, "Speed and natural completion execute once.");
        player.Loop = true; player.Play(); for (var i = 0; i < 8; i++) tree.ProcessFrame(.2); Check(player.IsPlaying() && finished == 1, "Prepared looping stays active without Finished.");
        player.Stop(); tree.ProcessFrame(.1); Check(!player.IsPlaying() && finished == 1 && !source.IsDisposed, "Explicit stop preserves borrowed source ownership.");
        player.Loop = false; player.SpeedScale = 1; player.Finished += player.Play;
        player.Play(); tree.ProcessFrame(.01); tree.ProcessFrame(1);
        Check(player.IsPlaying() && player.StreamPosition == 0 && finished == 2, "Finished runs after stopping and permits immediate restart.");
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void ColdTransfer(string path)
    {
        using var source = new VideoStreamTheora { File = path };
        var player = Task.Run(() => new VideoStreamPlayer { Stream = source, Autoplay = true }).Result;
        using var tree = new SceneTree(player);
        tree.ProcessFrame(.01); tree.ProcessFrame(.1);
        Check(player.IsPlaying() && player.StreamPosition == .1, "A detached prepared scene can enter its initial owner on another thread.");
        Check(Task.Run(() => Capture(() => _ = player.StreamPosition)).Result is InvalidOperationException, "Active scene playback rejects cross-thread reads.");
    }
    private static void Delay(string path, string folder)
    {
        using (var registry = new ProjectSettingsRegistry(folder))
        {
            Check(registry.Get(ProjectSettings.AudioVideoDelayCompensationMSEC) == 0, "Video compensation defaults to zero.");
            registry.Set(ProjectSettings.AudioVideoDelayCompensationMSEC, -35); registry.SetFeatureOverride(ProjectSettings.AudioVideoDelayCompensationMSEC, "dotnet", 50); registry.Save();
        }
        using (var registry = new ProjectSettingsRegistry(folder)) { registry.Load(); Check(registry.GetWithOverride(ProjectSettings.AudioVideoDelayCompensationMSEC) == 50, "Video compensation persists with feature overrides."); }
        ProjectSettings.SetFeatureOverride(ProjectSettings.AudioVideoDelayCompensationMSEC, "dotnet", 50);
        try
        {
            using var source = new VideoStreamTheora { File = path }; using var playback = source.InstantiatePlayback();
            playback.Play(); Check(playback.GetPlaybackPosition() == -.05, "Presentation delay exposes the compensated initial timestamp.");
            ProjectSettings.SetFeatureOverride(ProjectSettings.AudioVideoDelayCompensationMSEC, "dotnet", 100);
            playback.Update(.1); Check(Math.Abs(playback.GetPlaybackPosition() - .05) < 1e-12, "Delay is sampled at Play.");
            var player = new VideoStreamPlayer { Stream = source, Autoplay = true }; using var tree = new SceneTree(player); tree.ProcessFrame(.01); tree.ProcessFrame(.05);
            var position = player.StreamPosition; var texture = player.GetVideoTexture(); player.AudioTrack = -1; player.BufferingMSEC = 100;
            Check(player.IsPlaying() && player.StreamPosition == position && ReferenceEquals(texture, player.GetVideoTexture()) && !texture!.IsDisposed, "Track and queue replacement preserve sampled compensated time and borrowed frame identity.");
            playback.Seek(.2); Check(Math.Abs(playback.GetPlaybackPosition() - .15) < 1e-12, "Explicit seek uses uncompensated source seconds.");
        }
        finally { ProjectSettings.ClearFeatureOverride(ProjectSettings.AudioVideoDelayCompensationMSEC, "dotnet"); }
    }
    private static void Extensions()
    {
        using (var inner = new ProbeStream())
        using (var wrapper = new DelegatingStream(inner))
        using (var delegated = wrapper.InstantiatePlayback()) Check(delegated.GetLength() == 1, "Custom factories can delegate to another video source.");
        using var source = new ProbeStream(); var player = new VideoStreamPlayer { Stream = source, Autoplay = true }; using var tree = new SceneTree(player);
        var playback = source.Last!;
        playback.UpdateHook = () => { Reject<InvalidOperationException>(() => player.Stream = null); Reject<InvalidOperationException>(player.Dispose); Reject<InvalidOperationException>(playback.Dispose); Reject<InvalidOperationException>(() => playback.Update(0)); };
        tree.ProcessFrame(.01); playback.UpdateHook = null;
        var accepted = 0; playback.Channels = 2; playback.GetChannels(); playback.AudioSink = (samples, frames) => { accepted += frames; Check(samples.SequenceEqual(new float[] { 2, 3, 4, 5 }), "PCM offset is a scalar float index."); return frames; };
        Check(playback.MixAudio(2, new float[] { 1, 2, 3, 4, 5 }, 1) == 2 && accepted == 2, "Custom decoder PCM reaches the attached sink.");
        Reject<ArgumentException>(() => playback.MixAudio(3, new float[2])); Reject<ArgumentException>(() => playback.MixAudio(1, new[] { float.NaN, 0f })); playback.AudioSink = null;
        using var invalid = new ProbeStream { FailFactory = true };
        Check(Capture(() => player.Stream = invalid) is not null && ReferenceEquals(player.Stream, source) && player.IsPlaying(), "Factory failure preserves the previous active source.");
        using var invalidFrame = new InvalidTexture(); using var invalidPicture = new ProbeStream { Frame = invalidFrame };
        Check(Capture(() => player.Stream = invalidPicture) is not null && ReferenceEquals(player.Stream, source) && player.IsPlaying(), "Invalid prepared texture metadata is rejected before replacing the active cohort.");
        playback.FailDispose = true; using var replacement = new ProbeStream();
        Check(Capture(() => player.Stream = replacement) is not null && ReferenceEquals(player.Stream, replacement), "Cleanup failure reports after replacement commits.");
        player.Play(); Check(player.IsPlaying(), "Replacement remains usable after old cleanup failed.");
        replacement.Last!.UpdateHook = () => throw new ApplicationException("video update");
        Check(Capture(() => tree.ProcessFrame(.01)) is not null && !player.IsPlaying(), "A failing custom update leaves both transports stopped.");
    }
    private static void AudioQueue()
    {
        Vector2[][] weights =
        [
            [new(1, 1)], [new(1, 0), new(0, 1)], [new(1, 0), new(.70710678f, .70710678f), new(0, 1)],
            [new(1, 0), new(0, 1), new(.5f, 0), new(0, .5f)],
            [new(1, 0), new(.70710678f, .70710678f), new(0, 1), new(.5f, 0), new(0, .5f)],
            [new(1, 0), new(.70710678f, .70710678f), new(0, 1), new(.5f, 0), new(0, .5f), new(0, 0)],
            [new(1, 0), new(.70710678f, .70710678f), new(0, 1), new(.70710678f, 0), new(0, .70710678f), new(.5f, .5f), new(0, 0)],
            [new(1, 0), new(.70710678f, .70710678f), new(0, 1), new(.70710678f, 0), new(0, .70710678f), new(.5f, 0), new(0, .5f), new(0, 0)]
        ];
        var previous = AudioServer.PlaybackSpeedScale;
        try
        {
            foreach (var speed in new[] { float.Epsilon, 3f })
            {
                AudioServer.PlaybackSpeedScale = speed;
                for (var channels = 1; channels <= 8; channels++)
                    for (var channel = 0; channel < channels; channel++)
                    {
                        using var queue = new VideoAudioStream.QueuePlayback((int)AudioServer.GetMixRate(), channels, 2048);
                        queue.Start(); var samples = new float[1024 * channels];
                        for (var i = channel; i < samples.Length; i += channels) samples[i] = 1;
                        Check(queue.Push(samples, 1024) == 1024, "Video queue accepts available source frames.");
                        var output = new Vector2[1024]; queue.MixInto(output, 1);
                        Check(output[600].IsEqualApprox(weights[channels - 1][channel]), "Vorbis channel downmix and video's independent audio clock.");
                    }
            }
            using var bounded = new VideoAudioStream.QueuePlayback(48000, 2, 16);
            bounded.Start(); Check(bounded.Push(new float[40], 20) == 15 && bounded.Push(new float[2], 1) == 0, "A bounded video queue reports partial acceptance and full backpressure.");
        }
        finally { AudioServer.PlaybackSpeedScale = previous; }
    }
    private static void LoadingAndStorage(string sound, string silent, string folder)
    {
        Check(ResourceLoader.Exists<VideoStream>(sound) && ResourceLoader.GetRecognizedExtensionsForType<VideoStream>().Contains("ogv"), "Loader recognizes video resource families.");
        using var source = ResourceLoader.Load<VideoStreamTheora>(sound);
        Check(ReferenceEquals(source, ResourceLoader.Load<VideoStream>(sound)), "Video path cache retains typed identity.");
        using var copy = (VideoStreamTheora)source.Duplicate(); Check(copy.File == source.File, "Resource duplication preserves configured source.");
        using var silentSource = new VideoStreamTheora { File = silent };
        using var player = new VideoStreamPlayer { Name = "Video", Stream = silentSource, Autoplay = true, Expand = true, Loop = true, SpeedScale = 1.5f, BufferingMSEC = 100, VolumeDB = -6 };
        using var packed = new PackedScene(); packed.Pack(player); var path = Path.Combine(folder, "video.e2dscene"); ResourceSaver.Save(packed, path, SaverFlags.BundleResources);
        ReadArchive(path);
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        if (Path.GetFileNameWithoutExtension(start.FileName) == "dotnet") start.ArgumentList.Add(typeof(VideoPlaybackTests).Assembly.Location);
        start.ArgumentList.Add("--video-archive"); start.ArgumentList.Add(path);
        using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(20000)) { child.Kill(true); throw new TimeoutException("Video archive child did not finish."); }
        Check(child.ExitCode == 0, "Fresh process video storage: " + stdout.Result + stderr.Result);
        var oldBytes = File.ReadAllBytes(sound); File.WriteAllBytes(sound, [1, 2, 3]);
        try { Check(Capture(() => ResourceLoader.Load<VideoStreamTheora>(sound, ResourceLoader.CacheMode.Replace)) is not null && ReferenceEquals(source, ResourceLoader.GetCachedRef<VideoStream>(sound)), "Failed reload preserves cache identity."); }
        finally { File.WriteAllBytes(sound, oldBytes); }
        var oversized = Path.Combine(folder, "oversized.ogv");
        using (var file = File.Create(oversized)) file.SetLength(536870913);
        using var invalid = new VideoStreamTheora { File = oversized };
        Reject<InvalidDataException>(() => invalid.InstantiatePlayback());
    }
    internal static void ReadArchive(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        using var instance = packed.Instantiate(); Check(instance is VideoStreamPlayer { Stream: VideoStreamTheora, Autoplay: true, Expand: true, Loop: true, BufferingMSEC: 100, SpeedScale: 1.5f } player && player.GetVideoTexture()?.GetSize() == new Vector2(34, 26), "Archive restores exact node, video configuration and an executable first frame.");
    }
    private static void Warm(string path)
    {
        using var source = new VideoStreamTheora { File = path }; using var playback = source.InstantiatePlayback();
        for (var i = 0; i < 128; i++) { playback.Play(); playback.Update(.12); playback.Update(.13); playback.Stop(); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { playback.Play(); playback.Update(.12); playback.Update(.13); playback.Stop(); }
        var active = GC.GetAllocatedBytesForCurrentThread() - before; before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) playback.Update(.01);
        var idle = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(active == 0 && idle == 0, $"Prepared decoder active/idle managed bytes: {active}/{idle}.");
    }
    private sealed class ProbeStream : VideoStream
    {
        internal ProbePlayback? Last;
        internal bool FailFactory;
        internal Texture? Frame;
        protected override VideoStreamPlayback OnInstantiatePlayback() => FailFactory ? throw new ApplicationException("video factory") : Last = new() { Frame = Frame };
    }
    private sealed class InvalidTexture : Texture
    {
        public override int GetWidth() => -1;
        public override int GetHeight() => 26;
    }
    private sealed class DelegatingStream(VideoStream inner) : VideoStream
    {
        protected override VideoStreamPlayback OnInstantiatePlayback() => inner.InstantiatePlayback();
    }
    private sealed class ProbePlayback : VideoStreamPlayback
    {
        private bool _active, _paused;
        private double _time;
        internal Action? UpdateHook;
        internal bool FailDispose;
        internal int Channels;
        internal Texture? Frame;
        protected override int OnGetChannels() => Channels;
        protected override int OnGetMixRate() => 48000;
        protected override double OnGetLength() => 1;
        protected override double OnGetPlaybackPosition() => _time;
        protected override Texture? OnGetTexture() => Frame;
        protected override bool OnIsPlaying() => _active;
        protected override bool OnIsPaused() => _paused;
        protected override void OnPlay() { _time = 0; _active = true; }
        protected override void OnStop() { _time = 0; _active = false; }
        protected override void OnSeek(double time) => _time = time;
        protected override void OnSetAudioTrack(int index) { }
        protected override void OnSetPaused(bool paused) => _paused = paused;
        protected override void OnUpdate(double delta) { UpdateHook?.Invoke(); if (_active && !_paused) _time += delta; }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (FailDispose) throw new ApplicationException("video disposal"); }
    }
}
