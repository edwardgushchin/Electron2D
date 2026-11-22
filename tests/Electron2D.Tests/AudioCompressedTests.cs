using Electron2D;

internal static class AudioCompressedTests
{
    internal static void Run(bool native = true)
    {
        VerifyMP3ReservoirWrap();
        using var trace = PhysicsAllocationTrace.Open(PhysicsServer.Backend.CPU, 7UL << 32);
        using var mp3 = AudioStreamMP3.LoadFromBuffer(Fixture("tone.mp3")); using var ogg = AudioStreamOggVorbis.LoadFromBuffer(Fixture("tone.ogg"));
        Console.WriteLine($"Compressed durations: MP3={mp3.GetLength()}, Ogg={ogg.GetLength()}, tags={ogg.Tags.Count}.");
        Check(Math.Abs(mp3.GetLength() - .4) < .005 && Math.Abs(ogg.GetLength() - .4) < .005, "Compressed durations.");
        var mp3PCM = AudioFileDecoder.DecodeMP3(mp3.Data); var oggPCM = AudioFileDecoder.DecodeVorbis(ogg.PacketSequence!).PCM;
        Compare(mp3PCM.Samples, "tone-mp3.f32", .0001f); Compare(oggPCM.Samples, "tone-ogg.f32", .0001f);
        foreach (var stream in new AudioStream[] { mp3, ogg })
        {
            using var playback = stream.InstantiatePlayback(); playback.Start(.1); var frames = playback.MixAudio(1, 512); Check(frames.Any(v => MathF.Abs(v.X) > .03f), "Actual compressed PCM playback.");
            playback.LoopingOverride = true; playback.Start(); var target = new Vector2[512];
            var format = stream is AudioStreamMP3 ? "MP3" : "Ogg";
            var tag = trace?.Tag(stream is AudioStreamMP3 ? 1UL : 2UL) ?? 0;
            AllocationWarmupCheck.Collect();
            for (var i = 0; i < 50; i++) playback.MixInto(target, 1); Check(playback.GetLoopCount() > 0, "Compressed EOF looping.");
            long allocated;
            trace?.Begin(tag);
            try
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) playback.MixInto(target, 1);
                allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            }
            finally { trace?.End(); }
            Console.WriteLine($"Compressed active {format}: 50 warm/64 measured blocks, owner={allocated} B, trace={tag}.");
            Check(allocated == 0, $"Warmed compressed {format} mixing allocated {allocated} owner-thread B, trace={tag}.");
        }
        VerifyMetadataAndCorruption(mp3, ogg);
        VerifyBoundaries(mp3, ogg, trace);
        using var surround = VerifyMultichannel();
        VerifyIdentificationFraming(surround);
        VerifyCommentFraming(surround);
        VerifySetupTimeTransform(surround);
        VerifyLoaderAndNative(mp3, surround, native);
        Console.WriteLine(native ? "Compressed format/PCM/cursor/loop/loader/native and 64-pass allocation checks passed." : "Compressed CPU/cursor/loop/loader checks passed; native checks run separately.");
    }
    private static void VerifyMP3ReservoirWrap()
    {
        // 48 kHz, 320 kbps, MPEG-1 Layer III stereo without padding or CRC.
        // Each frame carries 924 main-data bytes, so the 8192-byte ring wraps exactly every 2048 frames.
        ReadOnlySpan<byte> header = [0xff, 0xfb, 0xe4, 0x00];
        const int frameBytes = 960;
        const int frameCount = 4099;
        const int samplesPerFrame = 1152 * 2;
        var encoded = new byte[frameCount * frameBytes];

        foreach (var overlap in new[] { 0, 100, 511 })
        {
            for (var frame = 0; frame < frameCount; frame++)
            {
                var offset = frame * frameBytes;
                header.CopyTo(encoded.AsSpan(offset));
                if (frame > 0)
                {
                    encoded[offset + 4] = (byte)(overlap >> 1);
                    encoded[offset + 5] = (byte)((overlap & 1) << 7);
                }
            }

            using var resource = AudioStreamMP3.LoadFromBuffer(encoded);
            Check(Math.Abs(resource.GetLength() - frameCount * 1152.0 / 48000) < 1e-8,
                $"MP3 import retains every frame across both reservoir wraps (overlap {overlap}).");

            using var source = new MemoryStream(encoded, writable: false);
            using var mpeg = new Electron2D.NLayerBindings.MpegFile(source);
            Check(CountMPEGSamples(mpeg) == frameCount * samplesPerFrame,
                $"MP3 decoder retains every frame (overlap {overlap}).");
            mpeg.Time = TimeSpan.Zero;
            Check(CountMPEGSamples(mpeg) == frameCount * samplesPerFrame,
                $"MP3 rewind resets the reservoir (overlap {overlap}).");

            using var forwardOnly = new ForwardOnlyMP3Stream(encoded);
            using var streamed = new Electron2D.NLayerBindings.MpegFile(forwardOnly);
            Check(!streamed.CanSeek && CountMPEGSamples(streamed) == frameCount * samplesPerFrame,
                $"Forward-only MP3 retains every frame (overlap {overlap}).");
        }

        var lengthWithPartialHeader = (frameCount - 1) * frameBytes + 3;
        using var truncatedSource = new MemoryStream(encoded, 0, lengthWithPartialHeader, writable: false);
        using var truncated = new Electron2D.NLayerBindings.MpegFile(truncatedSource);
        Check(CountMPEGSamples(truncated) == (frameCount - 1) * samplesPerFrame,
            "MP3 omits a final frame with an incomplete header after a reservoir wrap.");

        Console.WriteLine("MP3 reservoir wraps, rewind, forward-only decoding and incomplete final header passed.");
    }

    private static long CountMPEGSamples(Electron2D.NLayerBindings.MpegFile mpeg)
    {
        var buffer = new float[4096];
        long total = 0;
        int read;
        while ((read = mpeg.ReadSamples(buffer, 0, buffer.Length)) > 0)
        {
            Check(!buffer.AsSpan(0, read).ContainsAnyExcept(0f), "Synthetic MP3 produces silent PCM.");
            total += read;
        }

        return total;
    }

    private sealed class ForwardOnlyMP3Stream(byte[] data) : MemoryStream(data, writable: false)
    {
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => base.Position;
            set => throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
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
    private static void VerifyBoundaries(AudioStreamMP3 mp3, AudioStreamOggVorbis ogg, PhysicsAllocationTrace? trace)
    {
        using var empty = new AudioStreamMP3(); Check(empty.GetLength() == 0, "Empty stream duration."); Reject<InvalidOperationException>(() => empty.InstantiatePlayback());
        foreach (var source in new AudioStream[] { mp3, ogg })
        {
            using var playback = source.InstantiatePlayback(); playback.Seek(.2); Check(playback.GetPlaybackPosition() == 0, "Inactive seek preserves cursor.");
            Reject<ArgumentOutOfRangeException>(() => playback.Start(double.NaN)); Reject<ArgumentOutOfRangeException>(() => playback.Seek(double.PositiveInfinity));
            playback.Start(source.GetLength()); Check(playback.GetPlaybackPosition() < .01, "Seek at EOF restarts from zero with bounded prefetch.");
            playback.MixAudio(1, 40000); Check(!playback.IsPlaying(), "Natural completion.");
            playback.Start(); Check(playback.MixAudio(1, 512).Length == 512, "Restart resets the first-silence marker.");
            playback.Stop(); var target = new Vector2[512];
            var format = source is AudioStreamMP3 ? "MP3" : "Ogg";
            var tag = trace?.Tag(source is AudioStreamMP3 ? 3UL : 4UL) ?? 0;
            AllocationWarmupCheck.Collect();
            for (var i = 0; i < 20; i++) playback.MixInto(target, 1);
            long allocated;
            trace?.Begin(tag);
            try
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) playback.MixInto(target, 1);
                allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            }
            finally { trace?.End(); }
            Console.WriteLine($"Compressed idle {format}: 20 warm/64 measured blocks, owner={allocated} B, trace={tag}.");
            Check(allocated == 0, $"Idle compressed {format} mixing allocated {allocated} owner-thread B, trace={tag}.");
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
        Action<Resource> failure = _ => throw new ApplicationException("Expected metadata listener failure."); mp3.Changed += failure;
        try { Reject<ApplicationException>(() => mp3.BPM = 123); Check(mp3.BPM == 123, "Metadata commits before callback failure."); } finally { mp3.Changed -= failure; mp3.BPM = 0; }
    }
    private static AudioStreamOggVorbis VerifyMultichannel()
    {
        var encoded = Fixture("surround.ogg");
        using var fromBuffer = AudioStreamOggVorbis.LoadFromBuffer(encoded);
        encoded.AsSpan().Clear();
        var pcm = AudioFileDecoder.DecodeVorbis(fromBuffer.PacketSequence!).PCM;
        Check(pcm.Channels == 6 && pcm.Rate == 48000 && pcm.Samples.Length == 5760, "Vorbis retains all six encoded channels and the final frame count.");
        Compare(pcm.Samples, "surround-ogg.f32", .0001f);

        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-surround-" + Guid.NewGuid() + ".ogg");
        AudioStreamOggVorbis fromFile;
        try
        {
            System.IO.File.WriteAllBytes(file, Fixture("surround.ogg"));
            fromFile = AudioStreamOggVorbis.LoadFromFile(file);
        }
        finally { System.IO.File.Delete(file); }
        try
        {
            using var copy = (AudioStreamOggVorbis)fromBuffer.DuplicateDeep(DeepDuplicateMode.All);
            using var packets = copy.PacketSequence!;
            Check(!ReferenceEquals(packets, fromBuffer.PacketSequence), "Deep duplication copies Vorbis packet sequences.");
            fromBuffer.Dispose();
            using var holder = new AudioStreamOggVorbis { PacketSequence = packets };
            using var ownedSample = holder.GenerateSample();
            using var fileSample = fromFile.GenerateSample();
            var samples = ownedSample.Data;
            Check(ownedSample.NumChannels == 2 && fileSample.Data.SequenceEqual(samples), "File import and direct assignment share the stereo sample projection.");
            for (var i = 0; i < pcm.Samples.Length / pcm.Channels; i++)
                Check(samples[i * 2] == pcm.Samples[i * 6] && samples[i * 2 + 1] == pcm.Samples[i * 6 + 1], "Vorbis sample uses encoded channels 0 and 1.");

            holder.Loop = true;
            holder.LoopOffset = .005;
            using var playback = (AudioStreamPlaybackOggVorbis)holder.InstantiatePlayback();
            playback.Start();
            var mixed = playback.MixAudio(AudioServer.GetMixRate() / 48000f, 96);
            for (var i = 2; i < mixed.Length; i++)
                Check(Math.Abs(mixed[i].X - samples[(i - 2) * 2]) < .0001f &&
                    Math.Abs(mixed[i].Y - samples[(i - 2) * 2 + 1]) < .0001f, "Playback projects the same two encoded channels as samples.");
            playback.Seek(.01);
            Check(playback.GetPlaybackPosition() == .01, "Vorbis seek is in frames rather than individual channel samples.");
            playback.MixAudio(1, 2048);
            Check(playback.GetLoopCount() > 0, "Multichannel Vorbis reaches and loops at the final granule.");

            var tags = holder.Tags;
            using var malformed = (OggPacketSequence)packets.Duplicate();
            var damagedPages = malformed.PacketData;
            damagedPages.SelectMany(page => page).ElementAt(2)[0] = 0;
            malformed.PacketData = damagedPages;
            Reject<FormatException>(() => holder.PacketSequence = malformed);
            malformed.PacketData = packets.PacketData;
            var granules = malformed.GranulePositions;
            granules[^1] += 8192;
            malformed.GranulePositions = granules;
            Reject<FormatException>(() => holder.PacketSequence = malformed);
            Check(ReferenceEquals(holder.PacketSequence, packets) && holder.Tags.SequenceEqual(tags) && holder.GetLength() == .02,
                "Malformed replacement preserves published packets, tags and duration.");
            using var afterFailure = holder.GenerateSample();
            Check(afterFailure.Data.SequenceEqual(samples), "Malformed replacement preserves published PCM.");
            var broken = Fixture("surround.ogg");
            broken[^1] ^= 1;
            Reject<FormatException>(() => AudioStreamOggVorbis.LoadFromBuffer(broken));
            holder.Dispose();
            Check(!packets.IsDisposed && ownedSample.Data.SequenceEqual(samples), "Borrowed sequence and owned sample outlive the holder.");
            Console.WriteLine("Multichannel Vorbis file/buffer/packets, stereo sample projection, seek/loop, lifetime and rollback passed.");
            return fromFile;
        }
        catch
        {
            fromFile.Dispose();
            throw;
        }
    }
    private static void VerifyIdentificationFraming(AudioStreamOggVorbis stream)
    {
        const int framingByte = 29;
        var encoded = Fixture("surround.ogg");
        Check(encoded[26] == 1 && encoded[27] == 30 && (encoded[28 + framingByte] & 1) == 1,
            "Surround fixture begins with one complete, framed identification packet.");
        encoded[28 + framingByte] &= 0xfe;
        VerifyRejectedHeader(stream, encoded, 0, 58, 0, framingByte, 1, "identification framing");
    }

    private static void VerifyCommentFraming(AudioStreamOggVorbis stream)
    {
        // The pinned surround fixture stores the complete comments packet at the start of page two.
        const int pageStart = 58;
        const int packetSize = 62;
        var encoded = Fixture("surround.ogg");
        var packetStart = pageStart + 27 + encoded[pageStart + 26];
        var framingOffset = packetStart + packetSize - 1;
        Check(encoded[pageStart + 27] == packetSize &&
            encoded.AsSpan(packetStart, 7).SequenceEqual("\x03vorbis"u8) && (encoded[framingOffset] & 1) == 1,
            "Surround fixture begins its second page with a complete, framed comments packet.");
        encoded[framingOffset] &= 0xfe;
        VerifyRejectedHeader(stream, encoded, pageStart, 7412, 1, packetSize - 1, 1, "comment framing");
    }

    private static void VerifySetupTimeTransform(AudioStreamOggVorbis stream)
    {
        // The pinned surround fixture has one time-domain transform after its setup codebooks.
        const int firstTransformBit = 56141;
        var encoded = Fixture("surround.ogg");
        var setup = encoded.AsSpan().IndexOf("\x05vorbis"u8);
        var offset = firstTransformBit / 8;
        var mask = (byte)(1 << (firstTransformBit % 8));
        Check(setup == 177 && (encoded[setup + offset] & mask) == 0,
            "Surround fixture has the expected zero setup transform.");
        encoded[setup + offset] |= mask;
        VerifyRejectedHeader(stream, encoded, 58, 7412, 2, offset, mask, "setup time-domain transform");
    }

    private static void VerifyRejectedHeader(AudioStreamOggVorbis stream, byte[] encoded,
        int pageStart, int pageEnd, int packetIndex, int packetOffset, byte mask, string field)
    {
        var page = encoded.AsSpan(pageStart, pageEnd - pageStart);
        // A valid page CRC lets the loaders reach the Vorbis header check.
        uint crc = 0;
        page.Slice(22, 4).Clear();
        foreach (var value in page)
        {
            crc ^= (uint)value << 24;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc << 1) ^ ((crc & 0x80000000u) == 0 ? 0 : 0x04c11db7u);
        }
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(page.Slice(22, 4), crc);
        Reject<FormatException>(() => AudioStreamOggVorbis.LoadFromBuffer(encoded).Dispose());

        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-vorbis-header-" + Guid.NewGuid() + ".ogg");
        var original = stream.PacketSequence!;
        using var before = stream.GenerateSample();
        var tags = stream.Tags;
        var duration = stream.GetLength();
        try
        {
            System.IO.File.WriteAllBytes(path, Fixture("surround.ogg"));
            using var cached = ResourceLoader.Load<AudioStreamOggVorbis>(path);
            var cachedPackets = cached.PacketSequence;
            System.IO.File.WriteAllBytes(path, encoded);
            Reject<FormatException>(() => AudioStreamOggVorbis.LoadFromFile(path).Dispose());
            Reject<FormatException>(() => ResourceLoader.Load<AudioStreamOggVorbis>(path, ResourceLoader.CacheMode.Replace));
            Check(ReferenceEquals(ResourceLoader.GetCachedRef<AudioStreamOggVorbis>(path), cached) &&
                ReferenceEquals(cached.PacketSequence, cachedPackets), $"Invalid {field} preserves the cached resource and packets.");
            using var cachedSample = cached.GenerateSample();
            Check(cachedSample.Data.SequenceEqual(before.Data), $"Invalid {field} preserves cached PCM.");
        }
        finally { System.IO.File.Delete(path); }

        using var malformed = (OggPacketSequence)original.Duplicate();
        var pages = malformed.PacketData;
        pages.SelectMany(packets => packets).ElementAt(packetIndex)[packetOffset] ^= mask;
        malformed.PacketData = pages;
        malformed.SamplingRate = 123;
        Reject<FormatException>(() => stream.PacketSequence = malformed);
        Check(ReferenceEquals(stream.PacketSequence, original) && stream.Tags.SequenceEqual(tags) &&
            stream.GetLength() == duration && malformed.SamplingRate == 123,
            $"Invalid {field} preserves the published resource and candidate metadata.");
        using var after = stream.GenerateSample();
        Check(after.Data.SequenceEqual(before.Data), $"Invalid {field} preserves published PCM.");
        Console.WriteLine($"Vorbis {field}: valid Ogg CRC, both loaders, cache replacement, and PacketSequence rollback passed.");
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void VerifyLoaderAndNative(AudioStreamMP3 mp3, AudioStreamOggVorbis ogg, bool runNative)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-compressed-loader-" + Guid.NewGuid() + ".ogg"); System.IO.File.WriteAllBytes(path, Fixture("surround.ogg"));
        using var loaded = ResourceLoader.Load<AudioStream>(path); Check(loaded is AudioStreamOggVorbis && ResourceLoader.Exists<AudioStream>(path), "Generic audio loader routes Ogg.");
        using var captured = loaded.InstantiatePlayback();
        var replaced = ResourceLoader.Load<AudioStream>(path, ResourceLoader.CacheMode.Replace); Check(ReferenceEquals(loaded, replaced), "Audio cache Replace retains identity.");
        captured.Start(); Check(captured.MixAudio(1, 256).Length == 256, "Imported retired sequence remains valid for captured playback after reload.");
        Check(!ResourceLoader.Exists<AudioStreamMP3>(path) && ResourceLoader.GetRecognizedExtensionsForType<AudioStream>().SequenceEqual(new[] { "wav", "mp3", "ogg", "e2dres" }), "Typed extension discovery and mismatched audio existence.");
        Reject<InvalidOperationException>(() => ResourceLoader.Load<AudioStreamMP3>(path));
        Reject<NotSupportedException>(() => ResourceLoader.Load<AudioStreamMP3>(path, ResourceLoader.CacheMode.Ignore));
        using (var ignored = ResourceLoader.Load<AudioStream>(path, ResourceLoader.CacheMode.Ignore)) Check(!ReferenceEquals(ignored, loaded) && ReferenceEquals(ResourceLoader.GetCachedRef<AudioStream>(path), loaded), "Ignore keeps cache identity.");
        System.IO.File.WriteAllBytes(path, "broken"u8.ToArray()); Reject<FormatException>(() => ResourceLoader.Load<AudioStream>(path, ResourceLoader.CacheMode.Replace));
        Check(ReferenceEquals(ResourceLoader.GetCachedRef<AudioStream>(path), loaded) && loaded.GetLength() == .02, "Malformed replacement preserves cached audio.");
        System.IO.File.WriteAllBytes(path, Fixture("surround.ogg"));
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
        Console.WriteLine($"{path}: {actual.Length} samples, max error {error:R}.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
