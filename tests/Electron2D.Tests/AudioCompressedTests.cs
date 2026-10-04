using Electron2D;

internal static class AudioCompressedTests
{
    internal static void Run(bool native = true)
    {
        using var mp3 = AudioStreamMP3.LoadFromBuffer(Fixture("tone.mp3")); using var ogg = AudioStreamOggVorbis.LoadFromBuffer(Fixture("tone.ogg"));
        Console.WriteLine($"Compressed durations: MP3={mp3.GetLength()}, Ogg={ogg.GetLength()}, tags={ogg.Tags.Count}.");
        Check(Math.Abs(mp3.GetLength() - .4) < .005 && Math.Abs(ogg.GetLength() - .4) < .005, "Compressed durations.");
        var mp3PCM = AudioFileDecoder.DecodeMP3(mp3.Data); var oggPCM = AudioFileDecoder.DecodeVorbis(ogg.PacketSequence!).PCM;
        Compare(mp3PCM.Samples, "tone-mp3.f32", .0001f); Compare(oggPCM.Samples, "tone-ogg.f32", .0001f);
        foreach (var stream in new AudioStream[] { mp3, ogg })
        {
            using var playback = stream.InstantiatePlayback(); playback.Start(.1); var frames = playback.MixAudio(1, 512); Check(frames.Any(v => MathF.Abs(v.X) > .03f), "Actual compressed PCM playback.");
            playback.LoopingOverride = true; playback.Start(); var target = new Vector2[512]; for (var i = 0; i < 50; i++) playback.MixInto(target, 1); Check(playback.GetLoopCount() > 0, "Compressed EOF looping.");
            var allocation = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) playback.MixInto(target, 1); Check(GC.GetAllocatedBytesForCurrentThread() == allocation, "Warmed compressed mixing zero allocation.");
        }
        VerifyMetadataAndCorruption(mp3, ogg);
        VerifyBoundaries(mp3, ogg);
        VerifyLoaderAndNative(mp3, ogg, native);
        Console.WriteLine(native ? "Compressed format/PCM/cursor/loop/loader/native and 64-pass allocation checks passed." : "Compressed CPU/cursor/loop/loader checks passed; native checks run separately.");
    }
    private static void VerifyMetadataAndCorruption(AudioStreamMP3 mp3, AudioStreamOggVorbis ogg)
    {
        Check(mp3.BPM == 0 && mp3.BarBeats == 4 && !mp3.Loop && ogg.Tags["title"] == "Electron2D test tone", "Compressed defaults/tags.");
        var original = mp3.Data; Reject<FormatException>(() => mp3.Data = "bad mp3"u8.ToArray()); Check(mp3.Data.SequenceEqual(original), "Corrupt replacement preserves old MP3.");
        var broken = Fixture("tone.ogg"); broken[^1] ^= 1; Reject<FormatException>(() => AudioStreamOggVorbis.LoadFromBuffer(broken));
        Reject<ArgumentOutOfRangeException>(() => mp3.BPM = -1); Reject<ArgumentOutOfRangeException>(() => ogg.BarBeats = 1);
        using var mp3Copy = (AudioStreamMP3)mp3.Duplicate(); Check(mp3Copy.Data.SequenceEqual(original), "MP3 duplication.");
        using var oggCopy = (AudioStreamOggVorbis)ogg.DuplicateDeep(DeepDuplicateMode.All); Check(!ReferenceEquals(oggCopy.PacketSequence, ogg.PacketSequence), "Ogg graph duplication.");
        mp3.Loop = true; mp3.BPM = 600; mp3.BeatCount = 1; mp3.LoopOffset = .025;
        using (var playback = mp3.InstantiatePlayback()) { playback.Start(); playback.MixAudio(1, 10000); Check(playback.GetLoopCount() > 1, "Beat-based loop and crossfade."); playback.LoopingOverride = false; playback.Start(); playback.MixAudio(1, 30000); Check(!playback.IsPlaying(), "Typed false override disables resource loop."); }
        mp3.Loop = false; mp3.BPM = 0; mp3.BeatCount = 0; mp3.LoopOffset = 0;
        using var invalidated = oggCopy.InstantiatePlayback(); invalidated.Start(); var sequence = oggCopy.PacketSequence!; sequence.GranulePositions = sequence.GranulePositions; Reject<InvalidOperationException>(() => invalidated.MixAudio(1, 256));
    }
    private static void VerifyBoundaries(AudioStreamMP3 mp3, AudioStreamOggVorbis ogg)
    {
        using var empty = new AudioStreamMP3(); Check(empty.GetLength() == 0, "Empty stream duration."); Reject<InvalidOperationException>(() => empty.InstantiatePlayback());
        foreach (var source in new AudioStream[] { mp3, ogg })
        {
            using var playback = source.InstantiatePlayback(); playback.Seek(.2); Check(playback.GetPlaybackPosition() == 0, "Inactive seek preserves cursor.");
            Reject<ArgumentOutOfRangeException>(() => playback.Start(double.NaN)); Reject<ArgumentOutOfRangeException>(() => playback.Seek(double.PositiveInfinity));
            playback.Start(source.GetLength()); Check(playback.GetPlaybackPosition() < .01, "Seek at EOF restarts from zero with bounded prefetch.");
            playback.MixAudio(1, 40000); Check(!playback.IsPlaying(), "Natural completion.");
            playback.Start(); Check(playback.MixAudio(1, 512).Length == 512, "Restart resets the first-silence marker.");
            playback.Stop(); var target = new Vector2[512]; for (var i = 0; i < 20; i++) playback.MixInto(target, 1);
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) playback.MixInto(target, 1); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Idle compressed mixing zero allocation.");
        }
        using var captured = mp3.InstantiatePlayback(); mp3.Data = mp3.Data; captured.Start(); Check(captured.MixAudio(1, 256).Length == 256, "MP3 captured immutable samples survive replacement.");
        var bytes = Fixture("tone.mp3"); Reject<FormatException>(() => AudioStreamMP3.LoadFromBuffer(bytes.AsSpan(0, bytes.Length / 2)));
        var oggBytes = Fixture("tone.ogg"); Reject<FormatException>(() => AudioStreamOggVorbis.LoadFromBuffer(oggBytes.AsSpan(0, oggBytes.Length - 1)));
        using var multiplex = AudioStreamOggVorbis.LoadFromBuffer(Fixture("multiplex.ogg")); Check(multiplex.GetLength() == .4, "Importer finds the first Vorbis stream after unrelated logical pages.");
        using var badPackets = new OggPacketSequence { PacketData = [[], [], []], GranulePositions = [0, 0, 0] };
        Reject<FormatException>(() => ogg.PacketSequence = badPackets); Check(ogg.GetLength() == .4, "Incomplete headers reject before replacing the old sequence.");
        using var borrowed = (OggPacketSequence)ogg.PacketSequence!.Duplicate(); using var holder = new AudioStreamOggVorbis { PacketSequence = borrowed };
        using var old = holder.InstantiatePlayback(); var granules = borrowed.GranulePositions; granules[^1] -= 100; borrowed.GranulePositions = granules;
        Reject<InvalidOperationException>(() => old.Start()); using var refreshed = holder.InstantiatePlayback(); refreshed.Start();
        Check(Math.Abs(holder.GetLength() - (19200 - 100) / 48000d) < .000001, "New playback redecodes edited packets.");
        using var packets = borrowed.InstantiatePlayback(); borrowed.PacketData = borrowed.PacketData; Reject<InvalidOperationException>(() => packets.Peek());
        borrowed.SamplingRate = 0; Check(double.IsPositiveInfinity(borrowed.GetLength()), "Sequence preserves literal zero-rate metadata.");
        using var zeroRatePlayback = holder.InstantiatePlayback(); Reject<InvalidOperationException>(() => zeroRatePlayback.Start()); holder.Dispose(); Check(!borrowed.IsDisposed, "Caller-owned sequences remain borrowed.");
        using var shortOgg = AudioStreamOggVorbis.LoadFromBuffer(Fixture("short.ogg")); using var shortPlayback = shortOgg.InstantiatePlayback(); shortPlayback.Start();
        Check(shortPlayback.MixAudio(1, 32).Length == (int)Math.Ceiling(shortOgg.GetLength() * AudioServer.GetMixRate()), "Initial short resample block reports its end.");
        using var mono = AudioStreamMP3.LoadFromBuffer(Fixture("mono.mp3")); var monoPCM = AudioFileDecoder.DecodeMP3(mono.Data); Compare(monoPCM.Samples, "mono-mp3.f32", .0001f);
        using var monoPlayback = mono.InstantiatePlayback(); monoPlayback.Start(); Check(monoPlayback.MixAudio(1, 128).All(frame => frame.X == frame.Y), "Mono expands into both channels.");
        Reject<FormatException>(() => AudioStreamOggVorbis.LoadFromBuffer(Fixture("surround.ogg")));
        Action<Resource> failure = _ => throw new ApplicationException("Expected metadata listener failure."); mp3.Changed += failure;
        try { Reject<ApplicationException>(() => mp3.BPM = 123); Check(mp3.BPM == 123, "Metadata commits before callback failure."); } finally { mp3.Changed -= failure; mp3.BPM = 0; }
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void VerifyLoaderAndNative(AudioStreamMP3 mp3, AudioStreamOggVorbis ogg, bool runNative)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-compressed-loader-" + Guid.NewGuid() + ".ogg"); System.IO.File.WriteAllBytes(path, Fixture("tone.ogg"));
        using var loaded = ResourceLoader.Load<AudioStream>(path); Check(loaded is AudioStreamOggVorbis && ResourceLoader.Exists<AudioStream>(path), "Generic audio loader routes Ogg.");
        using var captured = loaded.InstantiatePlayback();
        var replaced = ResourceLoader.Load<AudioStream>(path, ResourceLoader.CacheMode.Replace); Check(ReferenceEquals(loaded, replaced), "Audio cache Replace retains identity.");
        captured.Start(); Check(captured.MixAudio(1, 256).Length == 256, "Imported retired sequence remains valid for captured playback after reload.");
        Check(!ResourceLoader.Exists<AudioStreamMP3>(path) && ResourceLoader.GetRecognizedExtensionsForType<AudioStream>().SequenceEqual(new[] { "wav", "mp3", "ogg" }), "Typed extension discovery and mismatched audio existence.");
        Reject<InvalidOperationException>(() => ResourceLoader.Load<AudioStreamMP3>(path));
        Reject<NotSupportedException>(() => ResourceLoader.Load<AudioStreamMP3>(path, ResourceLoader.CacheMode.Ignore));
        using (var ignored = ResourceLoader.Load<AudioStream>(path, ResourceLoader.CacheMode.Ignore)) Check(!ReferenceEquals(ignored, loaded) && ReferenceEquals(ResourceLoader.GetCachedRef<AudioStream>(path), loaded), "Ignore keeps cache identity.");
        System.IO.File.WriteAllBytes(path, "broken"u8.ToArray()); Reject<FormatException>(() => ResourceLoader.Load<AudioStream>(path, ResourceLoader.CacheMode.Replace));
        Check(ReferenceEquals(ResourceLoader.GetCachedRef<AudioStream>(path), loaded) && loaded.GetLength() == .4, "Malformed replacement preserves cached audio.");
        System.IO.File.WriteAllBytes(path, Fixture("tone.ogg"));
        using (var playback = loaded.InstantiatePlayback()) { playback.Start(); Check(playback.MixAudio(1, 512).Any(f => Math.Abs(f.X) > .03), "Reloaded Ogg owns live cloned sequence."); }
        if (!runNative) { System.IO.File.Delete(path); return; }
        var root = new Node(); var player = new AudioStreamPlayer { Stream = mp3 }; root.AddChild(player); using var tree = new SceneTree(root);
        Check(player.GetParameter(AudioStreamPlayback.LoopingParameter) is null, "Typed looping default."); player.SetParameter(AudioStreamPlayback.LoopingParameter, (bool?)true); player.Play();
        Check(player.GetStreamPlayback().LoopingOverride == true, "Typed parameter reaches actual playback.");
        var native = AudioServer.Service.Native; native.PrepareCapture(24000); Thread.Sleep(80); Check(native.CapturedPCM().Any(v => Math.Abs(v) > .03), "Native MP3 produces actual PCM.");
        VerifyNativeWarm(native, player);
        player.Stream = ogg; player.SetParameter(AudioStreamPlayback.LoopingParameter, (bool?)true); player.Play(); native.PrepareCapture(24000); Thread.Sleep(80); Check(native.CapturedPCM().Any(v => Math.Abs(v) > .03), "Native Ogg produces actual PCM.");
        VerifyNativeWarm(native, player);
        using (var scene = new PackedScene())
        {
            player.SetParameter(AudioStreamPlayback.LoopingParameter, (bool?)false); scene.Pack(player); using var copy = (AudioStreamPlayer)scene.Instantiate();
            Check(copy.GetParameter(AudioStreamPlayback.LoopingParameter) == false, "Packed player preserves typed looping override.");
        }
        var sequence = ogg.PacketSequence!; var packets = sequence.PacketData; packets[0][0][0] = 255; Check(sequence.PacketData[0][0][0] == 1, "Packet copies are deep.");
        player.Stop(); tree.Dispose(); System.IO.File.Delete(path);
    }
    private static void VerifyNativeWarm(FAudioContext native, AudioStreamPlayer player)
    {
        WaitMixPasses(native, 20);
        var managed = native.MixManagedBytes; var allocations = FAudioContext.AllocationCalls;
        WaitMixPasses(native, 64);
        Check(native.MixManagedBytes == managed && FAudioContext.AllocationCalls == allocations, "64 warmed active native passes allocate zero managed bytes and zero custom native allocator calls.");
        Check(player.GetStreamPlayback().GetLoopCount() > 0, "Native compressed stream remains active through loops.");
        player.StreamPaused = true; WaitMixPasses(native, 20); managed = native.MixManagedBytes; allocations = FAudioContext.AllocationCalls;
        WaitMixPasses(native, 64);
        Check(native.MixManagedBytes == managed && FAudioContext.AllocationCalls == allocations, "64 warmed paused native passes allocate zero measured bytes/calls.");
        player.StreamPaused = false;
    }
    private static void WaitMixPasses(FAudioContext native, long passes)
    {
        var target = native.MixPasses + passes; var started = System.Diagnostics.Stopwatch.GetTimestamp();
        while (native.MixPasses < target) { if (System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalSeconds > 5) throw new InvalidOperationException("Native mix deadline."); Thread.Sleep(1); }
    }
    private static byte[] Fixture(string name) { using var stream = typeof(AudioCompressedTests).Assembly.GetManifestResourceStream("TestAudio." + name) ?? throw new InvalidOperationException(name); using var memory = new MemoryStream(); stream.CopyTo(memory); return memory.ToArray(); }
    private static void Compare(float[] actual, string path, float tolerance)
    {
        var bytes = Fixture(path); var reference = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(bytes);
        Console.WriteLine($"PCM samples {actual.Length} vs independent {reference.Length}."); Check(actual.Length == reference.Length, "Independent decoder sample counts.");
        var error = 0f; for (var i = 0; i < actual.Length; i++) error = Math.Max(error, Math.Abs(actual[i] - reference[i])); Check(error < tolerance, $"Independent decoder maximum error: {error}.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
