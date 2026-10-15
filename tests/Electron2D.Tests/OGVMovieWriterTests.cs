using Electron2D;
using System.Buffers.Binary;
using Path = System.IO.Path;
using static MovieWriterTests;

internal static class OGVMovieWriterTests
{
    internal static void Portable()
    {
        using (var writer = new OGVMovieWriter())
            foreach (var size in new[] { new Vector2i(65, 48), new Vector2i(64, 49), new Vector2i(0, 48), new Vector2i(64, -2) })
                Reject<NotSupportedException>(() => writer.Begin(size, 60, "must-not-create.ogv"));
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-ogv-settings-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using (var settings = new ProjectSettingsRegistry(folder))
            {
                Check(settings.Get(ProjectSettings.MovieWriterOGVAudioQuality) == .5f && settings.Get(ProjectSettings.MovieWriterOGVEncodingSpeed) == 4 && settings.Get(ProjectSettings.MovieWriterOGVKeyframeInterval) == 64, "OGV defaults.");
                foreach (var value in new[] { -.11f, 1.01f, float.NaN, float.PositiveInfinity }) Reject<ArgumentException>(() => settings.Set(ProjectSettings.MovieWriterOGVAudioQuality, value));
                foreach (var value in new[] { 0, 5 }) Reject<ArgumentException>(() => settings.Set(ProjectSettings.MovieWriterOGVEncodingSpeed, value));
                foreach (var value in new[] { 0, 1025 }) Reject<ArgumentException>(() => settings.Set(ProjectSettings.MovieWriterOGVKeyframeInterval, value));
                settings.Set(ProjectSettings.MovieWriterOGVAudioQuality, -.1f); settings.Set(ProjectSettings.MovieWriterOGVAudioQuality, 1);
                settings.Set(ProjectSettings.MovieWriterOGVEncodingSpeed, 1); settings.Set(ProjectSettings.MovieWriterOGVKeyframeInterval, 1024); settings.Save();
            }
            using var loaded = new ProjectSettingsRegistry(folder); loaded.Load();
            Check(loaded.Get(ProjectSettings.MovieWriterOGVAudioQuality) == 1 && loaded.Get(ProjectSettings.MovieWriterOGVEncodingSpeed) == 1 && loaded.Get(ProjectSettings.MovieWriterOGVKeyframeInterval) == 1024, "OGV settings persist.");
        }
        finally { Directory.Delete(folder, true); }
    }
    internal static void Native(string backend, string folder)
    {
        Directory.CreateDirectory(folder); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var source = Source();
        try
        {
            foreach (var mode in Enum.GetValues<AudioServer.SpeakerMode>())
                foreach (var quality in new[] { 0f, 1f })
                {
                    ProjectSettings.Set(ProjectSettings.MovieWriterSpeakerMode, mode); ProjectSettings.Set(ProjectSettings.MovieWriterMixRate, 44100);
                    ProjectSettings.Set(ProjectSettings.MovieWriterVideoQuality, quality); ProjectSettings.Set(ProjectSettings.MovieWriterOGVAudioQuality, quality == 0 ? -.1f : 1);
                    ProjectSettings.Set(ProjectSettings.MovieWriterOGVEncodingSpeed, (int)mode + 1); ProjectSettings.Set(ProjectSettings.MovieWriterOGVKeyframeInterval, quality == 0 ? 3 : 1);
                    var path = Path.Combine(folder, $"recording-{mode}-{quality}.OGV"); var window = AnimatedScene(source); window.Size = new(66, 50); window.AddChild(new Finish(18));
                    Engine.Run(window, path, 59); Validate(path, 17, 44100, mode == AudioServer.SpeakerMode.Surround31 ? 6 : 2 + (int)mode * 2, 59, (int)(quality * 63));
                }
            ProjectSettings.Set(ProjectSettings.MovieWriterSpeakerMode, AudioServer.SpeakerMode.Stereo); ProjectSettings.Set(ProjectSettings.MovieWriterVideoQuality, .75f); ProjectSettings.Set(ProjectSettings.MovieWriterOGVAudioQuality, .5f);
            ProjectSettings.Set(ProjectSettings.MovieWriterOGVKeyframeInterval, 1024);
            foreach (var (name, frames, fps) in new[] { ("short", 1, 59), ("empty", 0, 59), ("low-fps", 2, 7), ("zero-audio-blocks", 4, 100000) })
            {
                var window = AnimatedScene(source); window.Size = new(66, 50); if (frames == 0) window.Ready += _ => window.Tree!.Quit(); else window.AddChild(new Finish(frames + 1));
                var path = Path.Combine(folder, name + ".ogv"); Engine.Run(window, path, fps); Validate(path, frames, 44100, 2, fps, 47);
            }
            var failure = AnimatedScene(source); failure.Size = new(66, 50); failure.AddChild(new Finish(3, true)); var failedPath = Path.Combine(folder, "failed.ogv");
            try { Engine.Run(failure, failedPath, 59); throw new InvalidOperationException("Expected scene failure."); }
            catch (AggregateException error) when (error.InnerExceptions.Count == 1 && error.InnerException is FormatException { Message: "OGV scene failure" }) { }
            Validate(failedPath, 2, 44100, 2, 59, 47); Check(failure.IsDisposed, "Failure closes OGV recording.");
            var snapshot = AnimatedScene(source); snapshot.Size = new(66, 50); snapshot.AddChild(new Finish(3)); snapshot.Ready += _ => { ProjectSettings.Set(ProjectSettings.MovieWriterVideoQuality, 0); ProjectSettings.Set(ProjectSettings.MovieWriterOGVAudioQuality, -.1f); };
            var snapshotPath = Path.Combine(folder, "snapshot.ogv"); Engine.Run(snapshot, snapshotPath, 59); Validate(snapshotPath, 2, 44100, 2, 59, 47);
            var odd = AnimatedScene(source); odd.Size = new(65, 49); Reject<NotSupportedException>(() => Engine.Run(odd, Path.Combine(folder, "odd.ogv"))); Check(odd.IsDisposed, "Odd-size failure consumes reserved scene.");
            if (OperatingSystem.IsLinux())
            {
                var fullPath = Path.Combine(folder, "full.ogv"); File.Delete(fullPath); File.CreateSymbolicLink(fullPath, "/dev/full");
                try
                {
                    var full = AnimatedScene(source); full.Size = new(66, 50);
                    try { Engine.Run(full, fullPath); throw new InvalidOperationException("Expected output failure."); }
                    catch (IOException) { }
                    catch (AggregateException error) when (error.Flatten().InnerExceptions.All(e => e is IOException)) { }
                    Check(full.IsDisposed, "Native write callback or final flush failure cleans the scene.");
                }
                finally { File.Delete(fullPath); }
            }
            Check(Engine.GetWriteMoviePath() == "", "Recording state resets after OGV failures.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.MovieWriterSpeakerMode, AudioServer.SpeakerMode.Stereo); ProjectSettings.Set(ProjectSettings.MovieWriterMixRate, 48000);
            ProjectSettings.Set(ProjectSettings.MovieWriterVideoQuality, .75f); ProjectSettings.Set(ProjectSettings.MovieWriterOGVAudioQuality, .5f);
            ProjectSettings.Set(ProjectSettings.MovieWriterOGVEncodingSpeed, 4); ProjectSettings.Set(ProjectSettings.MovieWriterOGVKeyframeInterval, 64);
        }
        Console.WriteLine("OGV: real capture, all channel modes, quality/speed/keyframes, exact clocks, empty blocks and failure cleanup passed.");
    }
    private static Window AnimatedScene(AudioStream source)
    { var window = Scene(source); window.AddChild(new AnimatedMarker()); return window; }
    private sealed class AnimatedMarker : Entity
    {
        private int _frame;
        internal AnimatedMarker() { Name = "OGVMarker"; Draw += item => item.DrawRect(new(32, 0, 34, 50), (_frame & 1) != 0 ? Colors.Green : Colors.Blue); }
        protected override void OnProcess(double delta) { _frame++; QueueRedraw(); }
    }
    private static void Validate(string path, int frames, int rate, int channels, int fps, int quality)
    {
        var bytes = File.ReadAllBytes(path); var streams = new Dictionary<uint, StreamState>(); var offset = 0; var bos = 0;
        while (offset < bytes.Length)
        {
            var header = bytes.AsSpan(offset); Check(header.Length >= 27 && header[..4].SequenceEqual("OggS"u8) && header[4] == 0, "Ogg page header.");
            var segments = header[26]; var length = 27 + segments; for (var i = 0; i < segments; i++) length += header[27 + i]; Check(length <= header.Length, "Ogg page extent.");
            uint crc = 0; for (var i = 0; i < length; i++) { crc ^= (uint)(i is >= 22 and < 26 ? 0 : header[i]) << 24; for (var bit = 0; bit < 8; bit++) crc = (crc & 0x80000000) != 0 ? (crc << 1) ^ 0x04c11db7 : crc << 1; }
            Check(crc == BinaryPrimitives.ReadUInt32LittleEndian(header[22..]), "Ogg page checksum.");
            var serial = BinaryPrimitives.ReadUInt32LittleEndian(header[14..]); var sequence = BinaryPrimitives.ReadUInt32LittleEndian(header[18..]);
            if (!streams.TryGetValue(serial, out var state)) { Check(sequence == 0 && (header[5] & 2) != 0, "First page is BOS."); streams.Add(serial, state = new()); bos++; }
            Check(sequence == state.Pages++, "Logical page sequence.");
            var granule = BinaryPrimitives.ReadInt64LittleEndian(header[6..]); if (granule >= 0) { Check(granule >= state.Granule, "Monotonic granules."); state.Granule = granule; }
            state.EOS |= (header[5] & 4) != 0; var body = 27 + segments;
            for (var i = 0; i < segments; i++)
            {
                var count = header[27 + i]; if (state.Packets == 0) for (var j = 0; j < count; j++) state.Identification.Add(header[body + j]); body += count;
                if (count < 255) state.Packets++;
            }
            offset += length;
        }
        Check(bos == 2 && streams.Count == 2 && streams.Values.All(s => s.EOS), "Two terminated streams.");
        var video = streams.Values.Single(s => s.Identification.Count >= 7 && s.Identification.Skip(1).Take(6).SequenceEqual("theora"u8.ToArray())); var audio = streams.Values.Single(s => !ReferenceEquals(s, video));
        var v = video.Identification.ToArray(); var a = audio.Identification.ToArray();
        Check(v[14] * 65536 + v[15] * 256 + v[16] == 66 && v[17] * 65536 + v[18] * 256 + v[19] == 50 && BinaryPrimitives.ReadUInt32BigEndian(v.AsSpan(22)) == fps, "Picture dimensions and fixed cadence.");
        var packed = BinaryPrimitives.ReadUInt16BigEndian(v.AsSpan(40)); var shift = (packed >> 5) & 31;
        Check((packed >> 10) == quality && video.Packets == frames + 3 && (frames == 0 || (video.Granule >> shift) + (video.Granule & ((1L << shift) - 1)) == frames), "Quality and exact video granule clock.");
        Check(a[11] == channels && BinaryPrimitives.ReadUInt32LittleEndian(a.AsSpan(12)) == rate && audio.Granule == (long)frames * rate / fps, "Vorbis channel layout/rate and exact PCM granule clock.");
        File.WriteAllText(path + ".expect.json", $"{{\"frames\":{frames},\"rate\":{rate},\"channels\":{channels},\"fps\":{fps},\"width\":66,\"height\":50,\"keyframes\":{ProjectSettings.Get(ProjectSettings.MovieWriterOGVKeyframeInterval)}}}");
    }
    private sealed class StreamState { internal uint Pages; internal int Packets; internal long Granule; internal bool EOS; internal readonly List<byte> Identification = []; }
    private sealed class Finish(int frames, bool fail = false) : Node
    { private int _count; protected override void OnProcess(double delta) { if (++_count != frames) return; if (fail) throw new FormatException("OGV scene failure"); Tree!.Quit(); } }
}
