using System.Buffers.Binary;
using Electron2D;
using Path = System.IO.Path;
using static MovieWriterTests;

internal static class AVIMovieWriterTests
{
    internal static void Portable()
    {
        using (var chunks = new MemoryStream())
        {
            AVIMovieWriter.WriteChunk(chunks, "00dc"u8, [1, 2, 3]);
            AVIMovieWriter.WriteChunk(chunks, "01wb"u8, [4, 5]);
            Check(chunks.ToArray().AsSpan().SequenceEqual(new byte[] { 48, 48, 100, 99, 3, 0, 0, 0, 1, 2, 3, 0, 48, 49, 119, 98, 2, 0, 0, 0, 4, 5 }), "RIFF chunks retain unpadded lengths, one zero byte after odd payloads and exact next-chunk alignment.");
        }
        Check(AVIMovieWriter.CheckFrameSize(324, 0, 3, 4) == 348, "Odd JPEG padding is included before the index.");
        Check(AVIMovieWriter.CheckFrameSize(uint.MaxValue - 52L, 0, 0, 4) == uint.MaxValue - 32L, "Exact RIFF boundary includes both index entries.");
        Reject<IOException>(() => AVIMovieWriter.CheckFrameSize(uint.MaxValue - 51L, 0, 0, 4));
        using (var writer = new AVIMovieWriter())
            foreach (var size in new[] { new Vector2i(0, 48), new Vector2i(64, -1), new Vector2i(32768, 48), new Vector2i(64, 32768) })
                Reject<NotSupportedException>(() => writer.Begin(size, 60, "must-not-create.avi"));
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-avi-settings-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using (var settings = new ProjectSettingsRegistry(folder))
            {
                Check(settings.Get(ProjectSettings.MovieWriterVideoQuality) == .75f && settings.Get(ProjectSettings.MovieWriterAudioBitDepth) == 16, "AVI setting defaults.");
                foreach (var quality in new[] { -.01f, 1.01f, float.NaN, float.PositiveInfinity })
                    Reject<ArgumentException>(() => settings.Set(ProjectSettings.MovieWriterVideoQuality, quality));
                foreach (var bits in new[] { 0, 8, 24, 64 }) Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.MovieWriterAudioBitDepth, bits));
                settings.Set(ProjectSettings.MovieWriterVideoQuality, 0); settings.Set(ProjectSettings.MovieWriterVideoQuality, 1);
                settings.Set(ProjectSettings.MovieWriterAudioBitDepth, 32); settings.Save();
            }
            using var loaded = new ProjectSettingsRegistry(folder); loaded.Load();
            Check(loaded.Get(ProjectSettings.MovieWriterVideoQuality) == 1 && loaded.Get(ProjectSettings.MovieWriterAudioBitDepth) == 32, "AVI setting serialization.");
        }
        finally { Directory.Delete(folder, true); }
    }
    internal static void Native(string folder)
    {
        using var source = Source(); var lowSize = 0; var highSize = 0;
        try
        {
            foreach (var mode in Enum.GetValues<AudioServer.SpeakerMode>())
                foreach (var bits in new[] { 16, 32 })
                {
                    ProjectSettings.Set(ProjectSettings.MovieWriterAudioBitDepth, bits); ProjectSettings.Set(ProjectSettings.MovieWriterSpeakerMode, mode);
                    ProjectSettings.Set(ProjectSettings.MovieWriterMixRate, 44100); ProjectSettings.Set(ProjectSettings.MovieWriterVideoQuality, .75f);
                    var path = Path.Combine(folder, $"recording-{mode}-{bits}.AVI");
                    var window = Pattern(source); window.AddChild(new Finish(5)); Engine.Run(window, path, 59);
                    Validate(path, 4, 44100, 2 + (int)mode * 2, bits, 59);
                }
            ProjectSettings.Set(ProjectSettings.MovieWriterSpeakerMode, AudioServer.SpeakerMode.Stereo);
            foreach (var quality in new[] { 0f, 1f })
            {
                ProjectSettings.Set(ProjectSettings.MovieWriterVideoQuality, quality);
                var path = Path.Combine(folder, $"quality-{quality}.avi"); var window = Pattern(source); window.AddChild(new Finish(3));
                Engine.Run(window, path, 7); var result = Validate(path, 2, 44100, 2, 32, 7);
                if (quality == 0) lowSize = result; else highSize = result;
            }
            Check(highSize > lowSize, "Quality affects JPEG output.");
            var fastPath = Path.Combine(folder, "zero-audio-blocks.avi"); var fast = Pattern(source); fast.AddChild(new Finish(5));
            Engine.Run(fast, fastPath, 100000); Validate(fastPath, 4, 44100, 2, 32, 100000, expectSignal: false);
            var emptyPath = Path.Combine(folder, "empty.avi"); var empty = Pattern(source); empty.Ready += _ => empty.Tree!.Quit();
            Engine.Run(empty, emptyPath, 59); Validate(emptyPath, 0, 44100, 2, 32, 59);
            var failedPath = Path.Combine(folder, "failed.avi"); var failed = Pattern(source); failed.AddChild(new Finish(3, fail: true));
            try { Engine.Run(failed, failedPath, 59); throw new InvalidOperationException("Expected scene failure."); }
            catch (AggregateException error) when (error.InnerExceptions.Count == 1 && error.InnerException is FormatException { Message: "AVI scene failure" }) { }
            Validate(failedPath, 2, 44100, 2, 32, 59); Check(failed.IsDisposed, "Scene failure releases writer and window.");
            var snapshotPath = Path.Combine(folder, "snapshot.avi"); var snapshot = Pattern(source);
            snapshot.Ready += _ => { ProjectSettings.Set(ProjectSettings.MovieWriterAudioBitDepth, 16); ProjectSettings.Set(ProjectSettings.MovieWriterVideoQuality, 0); };
            snapshot.AddChild(new Finish(3)); Engine.Run(snapshot, snapshotPath, 59); Validate(snapshotPath, 2, 44100, 2, 32, 59);
            var missing = Pattern(source); Reject<DirectoryNotFoundException>(() => Engine.Run(missing, Path.Combine(folder, "absent", "fail.avi")));
            Check(missing.IsDisposed && Engine.GetWriteMoviePath() == "", "Begin IO failure releases recording ownership.");
            var customPath = Path.Combine(folder, "custom.avi"); File.Delete(customPath);
            var customWindow = Pattern(source); using var custom = new OverrideWriter(customWindow); MovieWriter.AddWriter(custom);
            Engine.Run(customWindow, customPath); Check(custom.Frames == 1 && !File.Exists(customPath), "Custom writers take precedence over AVI.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.MovieWriterAudioBitDepth, 16); ProjectSettings.Set(ProjectSettings.MovieWriterVideoQuality, .75f);
            ProjectSettings.Set(ProjectSettings.MovieWriterSpeakerMode, AudioServer.SpeakerMode.Stereo); ProjectSettings.Set(ProjectSettings.MovieWriterMixRate, 48000);
        }
        Console.WriteLine("AVI: MJPEG/PCM16/32, all channel modes, rational clocks, quality endpoints, index/padding, zero frames and failure cleanup passed.");
    }
    private static Window Pattern(AudioStream source)
    {
        var window = Scene(source); var pattern = new Entity { Name = "AVIPattern" };
        pattern.Draw += item => { for (var y = 0; y < 48; y += 3) for (var x = 32; x < 64; x += 3) item.DrawRect(new(x, y, 3, 3), new Color(x / 64f, y / 48f, ((x * 17 + y * 31) % 97) / 97f)); };
        window.AddChild(pattern); return window;
    }
    private static uint U(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
    private static int Validate(string path, int frames, int rate, int channels, int bits, int fps, bool expectSignal = true)
    {
        var b = File.ReadAllBytes(path); Check(b.AsSpan(0, 4).SequenceEqual("RIFF"u8) && b.AsSpan(8, 4).SequenceEqual("AVI "u8) && U(b, 4) == b.Length - 8, "RIFF file extent.");
        Check(U(b, 48) == frames && U(b, 56) == 2 && U(b, 140) == frames && U(b, 132) == fps && U(b, 128) == 1, "Two streams and exact video time base.");
        Check(U(b, 264) == (long)frames * rate / fps && U(b, 300) == rate && BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(298)) == channels && BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(310)) == bits, "PCM format and cumulative frame count.");
        var end = checked(320 + (int)U(b, 316)); var cursor = 324; var index = new List<(int Position, int Size)>(); var audioSamples = 0L; var nonzero = false; var first = 0;
        for (var frame = 0; frame < frames; frame++)
        {
            Check(b.AsSpan(cursor, 4).SequenceEqual("00dc"u8), "Compressed video stream chunk."); var size = checked((int)U(b, cursor + 4)); index.Add((cursor, size));
            using var image = new Image(); image.LoadJPGFromBuffer(b.AsSpan(cursor + 8, size)); Check(image.Size == new Vector2i(64, 48) && image.GetPixel(5, 5).R > .8f, "Actual MJPEG pixels decode.");
            if (frame == 0) first = size; cursor += 8 + size; if ((size & 1) != 0) { Check(b[cursor] == 0, "RIFF zero padding."); cursor++; }
            Check(b.AsSpan(cursor, 4).SequenceEqual("01wb"u8), "Interleaved audio chunk."); var audio = checked((int)U(b, cursor + 4)); index.Add((cursor, audio));
            var count = ((long)(frame + 1) * rate / fps - (long)frame * rate / fps) * channels;
            Check(audio == count * bits / 8, "Rational PCM chunk length.");
            for (var i = cursor + 8; i < cursor + 8 + audio; i += bits / 8)
            {
                var value = bits == 16 ? BinaryPrimitives.ReadInt16LittleEndian(b.AsSpan(i)) : BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(i)) / 65536;
                Check(value is >= -1024 and <= 10240, "Decoded PCM sample amplitude includes SRC startup ringing."); nonzero |= value > 100;
            }
            audioSamples += count / channels; cursor += 8 + audio;
        }
        Check(cursor == end && b.AsSpan(end, 4).SequenceEqual("idx1"u8) && U(b, end + 4) == frames * 32 && end + 8 + frames * 32 == b.Length, "Movie/index exact extents.");
        for (var i = 0; i < index.Count; i++)
        {
            var at = end + 8 + i * 16; var item = index[i];
            Check(b.AsSpan(at, 4).SequenceEqual(b.AsSpan(item.Position, 4)) && U(b, at + 4) == 16 && U(b, at + 8) + 320 == item.Position && U(b, at + 12) == item.Size, "Seek index uses unpadded payload sizes and relative chunk offsets.");
        }
        Check(audioSamples == (long)frames * rate / fps && (!expectSignal || frames == 0 || nonzero), "Complete native PCM clock and signal.");
        return first;
    }
    private sealed class Finish(int limit, bool fail = false) : Node
    {
        private int _frames;
        protected override void OnProcess(double delta) { if (++_frames != limit) return; if (fail) throw new FormatException("AVI scene failure"); Tree!.Quit(); }
    }
    private sealed class OverrideWriter(Window window) : MovieWriter
    {
        internal int Frames;
        protected override string[] OnGetSupportedExtensions() => ["avi"];
        protected override bool OnHandlesFile(string path) => Path.GetExtension(path) == ".avi";
        protected override void OnWriteBegin(Vector2i size, int fps, string path) { }
        protected override void OnWriteFrame(Image image, ReadOnlySpan<int> audio) { Frames++; window.Tree!.Quit(); }
        protected override void OnWriteEnd() { }
    }
}
