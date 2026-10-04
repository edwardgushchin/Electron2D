using System.Buffers.Binary;
using Electron2D;

internal static class AudioResourceTests
{
    internal static void Run()
    {
        VerifyFormats(); VerifyMatrices(); VerifyInterpolationVectors(); VerifyImportAndSave(); VerifyLoopAndWarm(); VerifyGuardsAndPacking();
        Console.WriteLine("Audio resource format, import/save, loop, lifetime, packing and warmed CPU checks passed.");
    }
    private static void VerifyMatrices()
    {
        var matrix = new float[12]; FAudioStreamVoice.FillOutputMatrix(matrix, 6, AudioStreamPlayer.MixTarget.Center, .25f, 1);
        Check(matrix.SequenceEqual(new float[] { 0, 0, 0, 0, .25f, 0, 0, 1, 0, 0, 0, 0 }), "Center/LFE independent gain vector.");
        FAudioStreamVoice.FillOutputMatrix(matrix, 6, AudioStreamPlayer.MixTarget.Surround, .25f, 1);
        Check(matrix.SequenceEqual(new float[] { .25f, 0, 0, .25f, .25f, 0, 0, 1, .25f, 0, 0, .25f }), "Surround stereo-pair vector.");
        FAudioStreamVoice.FillOutputMatrix(matrix, 6, AudioStreamPlayer.MixTarget.Surround, .25f, 0); Check(matrix.All(value => value == 0), "Solo source gating removes LFE too.");
    }
    private static void VerifyFormats()
    {
        using var defaults = new AudioStreamWAV(); Check(defaults.SampleFormat == AudioStreamWAV.Format.PCM8 && defaults.MixRate == 44100 && !defaults.Stereo && defaults.Loop == AudioLoopMode.Disabled && defaults.GetLength() == 0, "WAV defaults.");
        var input = Enumerable.Range(0, 10240).Select(i => MathF.Sin(i * .05f) * .5f).ToArray();
        foreach (var format in Enum.GetValues<AudioStreamWAV.Format>())
        {
            var bytes = AudioPCMCodec.Encode(input, format, 1, 44100); var decoded = AudioPCMCodec.Decode(bytes, format, 1);
            Check(decoded.Length >= input.Length && decoded.All(float.IsFinite), "Each internal codec produces finite PCM.");
            var error = input.Zip(decoded).Average(pair => MathF.Abs(pair.First - pair.Second));
            Check(error < (format == AudioStreamWAV.Format.IMAADPCM ? .02 : .01), $"Codec error bound {format}: {error}.");
            using var resource = new AudioStreamWAV { SampleFormat = format, Data = bytes };
            Check(Math.Abs(resource.GetLength() - input.Length / 44100d) < .0001, "Codec resource duration.");
            using var playback = resource.InstantiatePlayback(); playback.Start(); var mixed = playback.MixAudio(1, 256); Check(mixed.Length == 256 && mixed.Any(v => MathF.Abs(v.X) > .1), "Codec playback executes cubic mixing.");
        }
        Check(AudioPCMCodec.Decode([0x77, 0x77, 0x77, 0x77], AudioStreamWAV.Format.IMAADPCM, 1).SequenceEqual(new float[] { 11, 41, 104, 240, 533, 1164, 2521, 5431 }.Select(v => v / 32767)), "IMA independent nibble vector.");
        Reject<FormatException>(() => AudioPCMCodec.Decode("qoaf"u8.ToArray(), AudioStreamWAV.Format.QOA, 1));
    }
    private static void VerifyInterpolationVectors()
    {
        var bytes = new byte[1024]; for (var i = 0; i < 512; i++) BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i * 2), (short)((i % 5 + 1) * 1000));
        using var source = new AudioStreamWAV { Data = bytes, SampleFormat = AudioStreamWAV.Format.PCM16, MixRate = (int)AudioServer.GetMixRate() };
        using var playback = source.InstantiatePlayback(); playback.Start(); var half = playback.MixAudio(.5f, 6);
        float[] expected = [0, -62.5f, 0, 437.5f, 1000, 1500];
        for (var i = 0; i < expected.Length; i++) Check(MathF.Abs(half[i].X - expected[i] / 32767) < .000001f, "Independent cubic history/fraction vector.");
        source.LoopBegin = 1; source.LoopEnd = 3;
        foreach (var (mode, sequence) in new[] { (AudioLoopMode.Forward, new float[] { 0, 0, 1000, 2000, 3000, 4000, 3000, 4000 }), (AudioLoopMode.PingPong, new float[] { 0, 0, 1000, 2000, 3000, 4000, 3000, 2000 }), (AudioLoopMode.Backward, new float[] { 0, 0, 3000, 2000, 3000, 2000, 3000, 2000 }) })
        {
            source.Loop = mode; playback.Start(); var output = playback.MixAudio(1, sequence.Length);
            for (var i = 0; i < sequence.Length; i++) Check(MathF.Abs(output[i].X - sequence[i] / 32767) < .000001f, "Independent loop boundary/history vector: " + mode);
        }
        Reject<FormatException>(() => AudioPCMCodec.Decode([1, 2, 3], AudioStreamWAV.Format.IMAADPCM, 2));
    }
    private static void VerifyImportAndSave()
    {
        using var source = AudioRuntimeTests.Tone(44100); var path = "/tmp/e2d-audio-resource-test.wav"; source.SaveToWAV(path); using var loaded = AudioStreamWAV.LoadFromFile(path);
        Check(loaded.SampleFormat == AudioStreamWAV.Format.PCM16 && loaded.Data.SequenceEqual(source.Data) && loaded.GetLength() == 1, "PCM WAVE fresh load preserves bytes and duration.");
        var bytes = System.IO.File.ReadAllBytes(path);
        using var normalized = AudioStreamWAV.LoadFromBuffer(bytes, new() { Normalize = true, Force8Bit = true }); Check(normalized.SampleFormat == AudioStreamWAV.Format.PCM8, "Typed normalize/force-width options execute.");
        using var limited = AudioStreamWAV.LoadFromBuffer(bytes, new() { LimitRate = true, MaxRate = 22050, Compression = AudioStreamWAV.Format.QOA }); Check(limited.MixRate == 22050 && limited.SampleFormat == AudioStreamWAV.Format.QOA, "Cubic import resampling and compressed storage execute.");
        using var pcm8 = new AudioStreamWAV { Data = [0, 127, 128, 255], MixRate = 44100 }; pcm8.SaveToWAV(path); using var loaded8 = AudioStreamWAV.LoadFromFile(path); Check(loaded8.Data.SequenceEqual(pcm8.Data), "Signed internal/unsigned RIFF eight-bit boundary round-trips.");
        var before = System.IO.File.ReadAllBytes(path); using var compressed = new AudioStreamWAV { SampleFormat = AudioStreamWAV.Format.QOA }; Reject<NotSupportedException>(() => compressed.SaveToWAV(path)); Check(System.IO.File.ReadAllBytes(path).SequenceEqual(before), "Rejected save preserves existing data.");
        Reject<FormatException>(() => AudioStreamWAV.LoadFromBuffer("not a WAVE file"u8)); Reject<ArgumentOutOfRangeException>(() => AudioStreamWAV.LoadFromBuffer(bytes, new() { LimitRate = true }));
    }
    private static void VerifyLoopAndWarm()
    {
        using var source = AudioRuntimeTests.Tone(44100); source.Loop = AudioLoopMode.Forward; source.LoopBegin = 10; source.LoopEnd = 1000;
        using var playback = source.InstantiatePlayback(); playback.Start(); var buffer = new Vector2[256];
        for (var i = 0; i < 64; i++) playback.MixInto(buffer, 1);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Check(playback.MixInto(buffer, 1) == 256, "Prepared loop continues."); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared cubic/loop mixing allocates zero managed bytes.");
        source.Loop = AudioLoopMode.PingPong; playback.Start(); Check(playback.MixAudio(1, 512).Length == 512, "Ping-pong traversal executes."); source.Loop = AudioLoopMode.Backward; playback.Start(.02); Check(playback.MixAudio(1, 512).Length == 512, "Backward traversal executes.");
        source.LoopBegin = -1; Reject<InvalidOperationException>(() => playback.Start());
    }
    private static void VerifyGuardsAndPacking()
    {
        using var source = AudioRuntimeTests.Tone(44100); var bytes = source.Data; bytes[0] = 255; Check(source.Data[0] != 255, "Encoded data getter copies.");
        Reject<ArgumentOutOfRangeException>(() => source.MixRate = 0); source.MixRate = -1; Check(source.MixRate == -1, "Signed nonzero rate metadata is retained."); source.MixRate = 44100;
        using var root = new AudioStreamPlayer { Stream = source, Autoplay = true, Bus = "Music", MaxPolyphony = 3, PitchScale = 2, VolumeDB = -6 }; using var scene = new PackedScene(); scene.Pack(root); using var copy = (AudioStreamPlayer)scene.Instantiate();
        Check(ReferenceEquals(copy.Stream, source) && copy.Autoplay && copy.MaxPolyphony == 3 && copy.PitchScale == 2 && copy.VolumeDB == -6, "Typed player scene configuration/factory.");
        using var playback = source.InstantiatePlayback(); source.Dispose(); Reject<ObjectDisposedException>(() => playback.Start());
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
