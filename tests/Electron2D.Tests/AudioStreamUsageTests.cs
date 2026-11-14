using Electron2D;

internal static class AudioStreamUsageTests
{
    internal static void Run()
    {
        RunOffline();
        RunRealtime();
    }

    private static void RunOffline()
    {
        const int frames = 480;
        var pcm = new int[frames * 2];
        var server = AudioServer.Service;
        using var reported = new TestStream();
        using var stream = new TestStream(reported);
        using var relay = new RelayStream(stream);
        using var leaf = new AudioStreamRandomizer();
        using var branch = new AudioStreamRandomizer();
        leaf.AddStream(0, relay);
        branch.AddStream(0, leaf);
        using var polyphonic = new AudioStreamPolyphonic { Polyphony = 10 };
        using var sample = AudioEffectTests.Constant();
        var root = new Node();
        var player = new AudioStreamPlayer { Stream = polyphonic };
        var samplePlayer = new AudioStreamPlayer { Stream = sample, PlaybackType = AudioServer.PlaybackType.Sample };
        root.AddChild(player);
        root.AddChild(samplePlayer);
        try
        {
            server.PrepareMovie(48000, 2);
            using var tree = new SceneTree(root);
            player.Play();
            samplePlayer.Play();
            var playback = (AudioStreamPlaybackPolyphonic)player.GetStreamPlayback();
            for (var i = 0; i < 10; i++)
                Check(playback.PlayStream(branch, i, playbackType: AudioServer.PlaybackType.Stream) >= 0, "Queued nested stream.");
            server.ReadMoviePCM(pcm);
            Check(stream.GetTaggedPlaybackOffsets().Length == 0 && stream.TagCalls == 0, "Tagging is disabled by default.");

            AudioServer.SetEnableTaggingUsedAudioStreams(true);
            using (var manual = stream.InstantiatePlayback())
            {
                manual.Start(20);
                manual.MixAudio(1, frames);
                Check(stream.GetTaggedPlaybackOffsets().Length == 0 && stream.TagCalls == 0, "Manual mixing stays outside the output quantum.");
            }
            server.ReadMoviePCM(pcm);
            var offsets = stream.GetTaggedPlaybackOffsets();
            Check(offsets.Length == 8, $"Each nested playback contributes one slot, capped at eight per resource. Leaf offsets: {offsets.Length}, tag calls: {stream.TagCalls}, parent offsets: {polyphonic.GetTaggedPlaybackOffsets().Length}, peak: {pcm.Max()}.");
            for (var i = 0; i < offsets.Length; i++)
                Check(Math.Abs(offsets[i] - (i + .02)) < 1e-9, "Repeated child mixing updates its offset to the latest cursor.");
            Check(branch.GetTaggedPlaybackOffsets().SequenceEqual(offsets) && leaf.GetTaggedPlaybackOffsets().SequenceEqual(offsets), "All randomizer wrappers report usage.");
            Check(relay.GetTaggedPlaybackOffsets().SequenceEqual(offsets), "A delegated factory reports both the concrete and requested streams.");
            Check(polyphonic.GetTaggedPlaybackOffsets().Length == 1 && stream.TagCalls == 10, "Composite and custom hooks each run once per playback.");
            Check(reported.GetTaggedPlaybackOffsets().Length == 8 && reported.GetTaggedPlaybackOffsets()[0] == .5, "Custom hooks tag an additional resource.");
            Check(samplePlayer.IsPlaying() && sample.GetTaggedPlaybackOffsets().Length == 0, "Native sample transport bypasses managed tagging.");
            offsets[0] = 100;
            Check(stream.GetTaggedPlaybackOffsets()[0] < 1, "A snapshot is caller-owned.");
            Check(Task.Run(stream.GetTaggedPlaybackOffsets).GetAwaiter().GetResult().Length == 8, "A worker can inspect completed offsets.");

            player.StreamPaused = true;
            server.ReadMoviePCM(pcm);
            Check(stream.GetTaggedPlaybackOffsets().Length == 0 && polyphonic.GetTaggedPlaybackOffsets().Length == 0 && stream.TagCalls == 10, "A pause tail does not tag usage.");
            player.StreamPaused = false;
            server.ReadMoviePCM(pcm);
            Check(stream.GetTaggedPlaybackOffsets().Length == 8, "Resumed streams tag usage again.");
            Reject<InvalidOperationException>(() => Task.Run(() => AudioServer.SetEnableTaggingUsedAudioStreams(false)).GetAwaiter().GetResult());
            Check(stream.GetTaggedPlaybackOffsets().Length == 8, "A worker cannot change the owner's collection mode.");
            AudioServer.SetEnableTaggingUsedAudioStreams(false);
            Check(stream.GetTaggedPlaybackOffsets().Length == 0 && reported.GetTaggedPlaybackOffsets().Length == 0, "Disabling clears the last published quantum immediately.");
            AudioServer.SetEnableTaggingUsedAudioStreams(true);
            Check(stream.GetTaggedPlaybackOffsets().Length == 0, "Reenabling does not republish old offsets.");
            server.ReadMoviePCM(pcm);
            player.Stop();
            server.ReadMoviePCM(pcm);
            Check(stream.GetTaggedPlaybackOffsets().Length == 0, "A stop tail does not tag usage.");
            reported.Dispose();
            Reject<ObjectDisposedException>(() => reported.GetTaggedPlaybackOffsets());
        }
        finally
        {
            AudioServer.SetEnableTaggingUsedAudioStreams(false);
            AudioServer.CloseForEngine();
        }
        Console.WriteLine("Offline audio stream usage passed.");
    }

    private static void RunRealtime()
    {
        using var stream = new TestStream();
        var root = new Node();
        var player = new AudioStreamPlayer { Stream = stream };
        root.AddChild(player);
        using var tree = new SceneTree(root);
        AudioServer.SetEnableTaggingUsedAudioStreams(true);
        try
        {
            player.Play(1);
            var native = AudioServer.Service.Native;
            AudioEffectTests.Wait(native, 24);
            var snapshot = stream.GetTaggedPlaybackOffsets();
            Check(snapshot.Length == 1 && snapshot[0] > 1, "Realtime native output publishes managed stream usage.");
            var allocated = native.MixManagedBytes;
            var nativeAllocations = FAudioContext.AllocationCalls;
            AudioEffectTests.Wait(native, 64);
            Check(native.MixManagedBytes == allocated && FAudioContext.AllocationCalls == nativeAllocations,
                "64 warmed realtime quanta collect usage without measured managed or FAudio allocations.");
            tree.Dispose();
            Check(stream.GetTaggedPlaybackOffsets().Length == 0, "Closing the last output clears the snapshot.");
        }
        finally
        {
            AudioServer.SetEnableTaggingUsedAudioStreams(false);
            AudioServer.CloseForEngine();
        }
        Console.WriteLine("Realtime audio stream usage passed.");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class RelayStream(AudioStream source) : AudioStream
    {
        protected override AudioStreamPlayback OnInstantiatePlayback() => source.InstantiatePlayback();
    }

    private sealed class TestStream(AudioStream? reported = null) : AudioStream
    {
        internal int TagCalls;
        protected override AudioStreamPlayback OnInstantiatePlayback() => new Playback(this, reported);

        private sealed class Playback(TestStream source, AudioStream? reported) : AudioStreamPlayback
        {
            private double _position;
            private bool _active;
            protected override void OnStart(double fromPosition) { _active = true; _position = fromPosition; }
            protected override void OnStop() => _active = false;
            protected override bool OnIsPlaying() => _active;
            protected override double OnGetPlaybackPosition() => _position;
            protected override void OnSeek(double time) => _position = time;
            protected override int OnMix(Span<Vector2> buffer, float rateScale)
            {
                if (!_active) return 0;
                buffer.Fill(new Vector2(.1f, -.1f));
                _position += buffer.Length * (double)rateScale / 48000;
                return buffer.Length;
            }

            protected override void OnTagUsedStreams()
            {
                source.TagCalls++;
                if (reported is not null) TagUsedStream(reported, .5);
            }
        }
    }
}
