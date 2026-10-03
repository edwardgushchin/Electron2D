using Electron2D;

internal static class AudioSynchronizedTests
{
    internal static void Run(bool native = false)
    {
        Resources(); Mixing(); Configuration(); Failures(); GraphsAndCopies(); Warm();
        if (native) Native();
        Console.WriteLine("Synchronized resources, coherent child mixing/cursors, live gain, transactional cohort replacement, callbacks/cycles/copies and warmed CPU" + (native ? "/native PCM" : "") + " checks passed.");
    }
    private static void Resources()
    {
        using var stream = new AudioStreamSynchronized(); using var a = new Probe(new(.1f, .2f)) { Length = 3, BPM = 0, BarBeats = 3, BeatCount = 4 }; using var b = new Probe(new(.2f, .1f)) { Length = 7, BPM = 140, BarBeats = 4, BeatCount = 10, Loop = true };
        Check(AudioStreamSynchronized.MaxStreams == 32 && stream.StreamCount == 0 && stream.IsMetaStream() && stream.IsMonophonic() && stream.GetLength() == 0 && stream.GetParameterList().Length == 0, "Defaults and inherited policy.");
        var changed = 0; var properties = 0; stream.Changed += _ => changed++; stream.PropertyListChanged += _ => properties++;
        stream.SetSyncStream(31, b); stream.SetSyncStreamVolume(31, -6); Check(ReferenceEquals(stream.GetSyncStream(31), b) && stream.GetLength() == 0 && stream.GetSyncStreamVolume(31) == -6, "Hidden slots persist independently of the active prefix.");
        stream.SetSyncStream(0, a); stream.SetSyncStream(1, b); stream.StreamCount = 2;
        Check(stream.GetLength() == 7 && stream.ReadBPM() == 140 && stream.ReadBarBeats() == 3 && stream.ReadBeatCount() == 10 && stream.ReadLoop(), "Maximum duration/beats, first nonzero tempo/bar and any loop.");
        stream.StreamCount = 2; Check(changed == 0 && properties == 2, "Only count reports the property list; equal count also reports.");
        Reject<ArgumentOutOfRangeException>(() => stream.StreamCount = -1); Reject<ArgumentOutOfRangeException>(() => stream.StreamCount = 33); Reject<ArgumentOutOfRangeException>(() => stream.GetSyncStream(32)); Reject<ArgumentOutOfRangeException>(() => stream.GetSyncStreamVolume(-1));
        Reject<ArgumentOutOfRangeException>(() => stream.SetSyncStreamVolume(0, float.NaN)); Reject<ArgumentOutOfRangeException>(() => stream.SetSyncStreamVolume(0, 1000)); Check(stream.GetSyncStreamVolume(0) == 0, "Invalid gains do not change storage.");
        stream.SetSyncStreamVolume(31, float.NegativeInfinity); Check(float.IsNegativeInfinity(stream.GetSyncStreamVolume(31)), "Negative infinity is stored as exact mute dB.");
        var descriptor = stream.GetPropertyList().OfType<PropertyDescriptor<AudioStreamSynchronized, float>>().Single(d => d.Name == "Stream_1/Volume"); descriptor.SetValue(stream, -3); Check(stream.GetSyncStreamVolume(1) == -3, "Typed indexed authoring.");
        Action<ElectronObject> fail = _ => throw new ApplicationException("Property-list failure"); stream.PropertyListChanged += fail;
        Reject<ApplicationException>(() => stream.StreamCount = 1); Check(stream.StreamCount == 1, "Property notification fails after committed count."); stream.PropertyListChanged -= fail;
        a.BPM = 80; Check(stream.ReadBPM() == 80 && !stream.ReadLoop(), "Metadata reads the live prefix.");
        stream.StreamCount = 32; Check(stream.GetLength() == 7 && stream.GetSyncStream(31) == b, "Maximum count activates the retained final slot.");
    }
    private static void Mixing()
    {
        using var a = new Probe(new(.1f, .4f)) { Frames = 5, Loops = 3 }; using var b = new Probe(new(.2f, .6f)) { Frames = 9, Loops = 7 }; using var stream = new AudioStreamSynchronized { StreamCount = 2 };
        stream.SetSyncStream(0, a); stream.SetSyncStream(1, b); stream.SetSyncStreamVolume(1, (float)Mathf.LinearToDB(.5));
        using var playback = (AudioStreamPlaybackSynchronized)stream.InstantiatePlayback(); var frames = new Vector2[7];
        Check(!playback.IsPlaying() && playback.MixInto(frames, 1) == 0, "Inactive aggregate reports no frames."); playback.Start(2);
        Check(a.Last!.Position == 2 && b.Last!.Position == 2 && playback.GetLoopCount() == 3, "Every child starts at the same time; loop count is active minimum.");
        Check(playback.MixInto([], 1) == 0 && playback.IsPlaying(), "Zero request preserves active state.");
        Check(playback.MixInto(frames, 2) == 7, "Active aggregate fills the request.");
        for (var i = 0; i < 7; i++) Near(frames[i], i < 5 ? new(.2f, .7f) : new(.1f, .3f), "Only reported child frames contribute; independent left/right gain.");
        Check(a.Last!.LastRate == 2 && b.Last!.LastRate == 2 && playback.GetLoopCount() == 7 && playback.GetPlaybackPosition() == b.Last!.Position, "Rate/cursor queries use active children.");
        Check(playback.MixInto(frames, 1) == 7 && playback.IsPlaying(), "Last child's end retains active latch for the request.");
        Near(frames[0], new(.1f, .3f), "Remaining last child PCM."); Check(frames.Skip(2).All(v => v == Vector2.Zero), "Short reads pad with silence.");
        Check(playback.MixInto(frames, 1) == 7 && !playback.IsPlaying() && frames.All(v => v == Vector2.Zero), "One silent requested block clears the aggregate latch.");
        Check(playback.MixInto(frames, 1) == 0 && playback.GetPlaybackPosition() == 0, "Following inactive mix reports zero and no playing cursor.");
        playback.Seek(-1); Check(a.Last!.Position == -1 && b.Last!.Position == -1, "Seek forwards even to stopped children.");
        a.Frames = b.Frames = int.MaxValue; playback.Start(); stream.SetSyncStreamVolume(1, 0); playback.MixInto(frames, 1); Near(frames[0], new(.3f, 1), "Live gain does not restart or clip PCM.");
        playback.Stop(); Check(!playback.IsPlaying() && !a.Last!.Active && !b.Last!.Active, "Stop reaches every child.");
        using var empty = new AudioStreamSynchronized(); using var ep = empty.InstantiatePlayback(); ep.Start(); Check(!ep.IsPlaying() && ep.MixInto(frames, 1) == 0, "Empty start remains inactive.");
        using var wave = new AudioStreamWAV { Data = new byte[512], Loop = AudioStreamWAV.LoopMode.Forward, LoopBegin = 0, LoopEnd = 255 }; using var looping = new AudioStreamSynchronized { StreamCount = 1 }; looping.SetSyncStream(0, wave); using var lp = looping.InstantiatePlayback(); lp.Start();
        Check(lp.MixAudio(1, 10000).Length == 10000 && lp.IsPlaying() && !looping.ReadLoop(), "WAV sample loops keep output indefinite while retaining their separate base music-loop metadata.");
    }
    private static void Configuration()
    {
        using var a = new Probe(new(.1f, .1f)); using var b = new Probe(new(.2f, .2f)); using var bad = new Probe(Vector2.Zero) { FailFactory = true }; using var stream = new AudioStreamSynchronized { StreamCount = 2 };
        stream.SetSyncStream(0, a); stream.SetSyncStream(1, b); using var first = stream.InstantiatePlayback(); using var second = stream.InstantiatePlayback(); first.Start(); second.Start();
        var old = a.Instances.Concat(b.Instances).ToArray(); Reject<ApplicationException>(() => stream.SetSyncStream(1, bad));
        Check(ReferenceEquals(stream.GetSyncStream(1), b) && first.IsPlaying() && second.IsPlaying() && old.All(p => !p.IsDisposed), "Factory failure preserves all prior ownership/configuration and active states.");
        Check(a.Last!.IsDisposed, "Factory rollback disposes the earlier newly prepared child.");
        stream.SetSyncStream(31, a); Check(!first.IsPlaying() && !second.IsPlaying() && old.All(p => p.IsDisposed), "Even hidden slot replacement rebuilds the full live cohort.");
        first.Start(); second.Start(); old = a.Instances.Concat(b.Instances).Where(p => !p.IsDisposed).ToArray(); stream.StreamCount = 1;
        Check(old.All(p => p.IsDisposed) && !first.IsPlaying() && ReferenceEquals(stream.GetSyncStream(1), b), "Shrinking closes hidden playback ownership and retains resource slots.");
        stream.StreamCount = 2; first.Start(); var current = b.Last; stream.StreamCount = 2; Check(first.IsPlaying() && ReferenceEquals(current, b.Last), "Equal count leaves prepared states untouched.");
        stream.SetSyncStream(0, a); Check(!first.IsPlaying() && !ReferenceEquals(current, b.Last), "Equal slot assignment also recreates child state.");
        a.FailStop = true; Reject<Exception>(() => stream.SetSyncStream(0, b)); Check(ReferenceEquals(stream.GetSyncStream(0), b) && !first.IsPlaying() && !second.IsPlaying(), "Old cleanup errors report after the new stopped cohort commits."); a.FailStop = false;
    }
    private static void Failures()
    {
        using var a = new Probe(new(.1f, .1f)); using var b = new Probe(new(.2f, .2f)); using var stream = new AudioStreamSynchronized { StreamCount = 2 }; stream.SetSyncStream(0, a); stream.SetSyncStream(1, b);
        using var playback = stream.InstantiatePlayback(); a.FailStart = true; Reject<ApplicationException>(() => playback.Start()); Check(!playback.IsPlaying() && !a.Last!.Active && !b.Last!.Active && b.Last!.Starts == 1, "Start failure attempts later children, then stops every state."); a.FailStart = false;
        playback.Start(); a.FailSeek = true; Reject<ApplicationException>(() => playback.Seek(4)); Check(b.Last!.Position == 4, "Seek failure continues later children."); a.FailSeek = false;
        a.FailStop = true; Reject<ApplicationException>(() => playback.Stop()); Check(!playback.IsPlaying() && !b.Last!.Active, "Stop failure continues later children."); a.FailStop = false;
        playback.Start(); var target = new Vector2[256]; a.MixHook = () => playback.Dispose(); Reject<InvalidOperationException>(() => playback.MixInto(target, 1)); Check(!playback.IsDisposed, "Mix reentry rejects before disposal starts."); a.MixHook = () => stream.SetSyncStream(0, b); Reject<InvalidOperationException>(() => playback.MixInto(target, 1)); Check(ReferenceEquals(stream.GetSyncStream(0), a), "Structural callback reentry preserves slots."); a.MixHook = null;
        a.Nonfinite = true; Reject<ArithmeticException>(() => playback.MixInto(target, 1)); a.Nonfinite = false;
        a.FactoryHook = () => stream.SetSyncStream(0, b); Reject<InvalidOperationException>(() => stream.InstantiatePlayback()); a.FactoryHook = null;
        a.FactoryHook = () => stream.InstantiatePlayback(); Reject<InvalidOperationException>(() => stream.InstantiatePlayback()); a.FactoryHook = null;
        // A rejected recursive factory must not strand a recursive audio monitor entry.
        Check(Task.Run(() => { AudioServer.Instance.Lock(); AudioServer.Instance.Unlock(); }).Wait(TimeSpan.FromSeconds(2)), "Recursive rejection releases the audio gate.");
        a.CursorHook = () => playback.GetPlaybackPosition(); Reject<InvalidOperationException>(() => playback.GetPlaybackPosition()); a.CursorHook = null;
        a.FailDispose = b.FailDispose = true; Reject<Exception>(() => playback.Dispose()); Check(playback.IsDisposed && a.Instances.All(p => p.IsDisposed) && b.Instances.All(p => p.IsDisposed), "Cleanup failures finalize the aggregate and all owned children."); a.FailDispose = b.FailDispose = false;
        using var victim = new AudioStreamSynchronized { StreamCount = 1 }; victim.SetSyncStream(0, a); a.FactoryHook = () => victim.Dispose(); Reject<ObjectDisposedException>(() => victim.InstantiatePlayback()); Check(a.Last!.IsDisposed, "Resource disposal during factory releases the prepared child."); a.FactoryHook = null;
    }
    private static void GraphsAndCopies()
    {
        using var a = new AudioStreamSynchronized(); using var b = new AudioStreamSynchronized(); using var random = new AudioStreamRandomizer();
        a.SetSyncStream(31, b); Reject<InvalidOperationException>(() => b.SetSyncStream(0, a)); Reject<InvalidOperationException>(() => a.SetSyncStream(0, a));
        a.SetSyncStream(31, null); a.SetSyncStream(0, random); Reject<InvalidOperationException>(() => random.AddStream(-1, a)); a.SetSyncStream(0, null); random.AddStream(-1, a); Reject<InvalidOperationException>(() => a.SetSyncStream(0, random)); random.RemoveStream(0);
        using var child = new Probe(new(.2f, .3f)); a.StreamCount = 2; a.SetSyncStream(0, child); a.SetSyncStream(1, child); a.SetSyncStream(31, child); a.SetSyncStreamVolume(1, -3);
        using var shallow = (AudioStreamSynchronized)a.Duplicate(); using var deep = (AudioStreamSynchronized)a.Duplicate(true);
        Check(ReferenceEquals(shallow.GetSyncStream(0), child) && !ReferenceEquals(deep.GetSyncStream(0), child) && ReferenceEquals(deep.GetSyncStream(0), deep.GetSyncStream(1)) && shallow.GetSyncStream(31) is null && deep.GetSyncStreamVolume(1) == -3, "Stored-prefix shallow/deep copy preserves alias and excludes hidden transient assignments.");
        a.ResourceLocalToScene = true; using var player = new AudioStreamPlayer { Stream = a }; using var packed = new PackedScene(); packed.Pack(player); using var instance = (AudioStreamPlayer)packed.Instantiate(); Check(instance.Stream is AudioStreamSynchronized local && !ReferenceEquals(local, a) && local.StreamCount == 2, "Typed scene-local resource graph.");
        using var playback = a.InstantiatePlayback(); a.Dispose(); Reject<ObjectDisposedException>(() => playback.Start()); playback.Stop(); Check(!child.IsDisposed, "Source disposal invalidates borrowing playback and retains child resources."); deep.GetSyncStream(0)!.Dispose();
    }
    private static void Warm()
    {
        using var child = new Probe(new(.1f, .2f)); using var stream = new AudioStreamSynchronized { StreamCount = 32 }; for (var i = 0; i < 32; i++) stream.SetSyncStream(i, child); using var playback = stream.InstantiatePlayback(); playback.Start(); var buffer = new Vector2[1025];
        void Active(int i) { stream.SetSyncStreamVolume(0, i % 2 == 0 ? 0 : -3); playback.MixInto(buffer, 1); playback.GetLoopCount(); playback.GetPlaybackPosition(); stream.GetLength(); stream.ReadBPM(); }
        for (var i = 0; i < 20; i++) Active(i); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Active(i); Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 warmed live gain/mix/cursor/metadata cycles allocate zero bytes.");
        playback.Stop(); for (var i = 0; i < 20; i++) playback.MixInto(buffer, 1); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) playback.MixInto(buffer, 1); Check(GC.GetAllocatedBytesForCurrentThread() == before, "64 warmed inactive mixes allocate zero bytes.");
    }
    private static void Native()
    {
        using var positive = new Probe(new(.2f, .2f)) { Wave = true }; using var negative = new Probe(new(-.2f, -.2f)) { Wave = true }; using var stream = new AudioStreamSynchronized { StreamCount = 2 }; stream.SetSyncStream(0, positive); stream.SetSyncStream(1, negative);
        var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player); using var tree = new SceneTree(root); player.Play(); var native = AudioServer.Instance.Native;
        Wait(native, 20); native.PrepareCapture(native.QuantumFrames * native.Channels * 8); Wait(native, 10); Check(native.CapturedPCM().All(v => Math.Abs(v) < 1e-6), "Opposite coherent children cancel through actual FAudio PCM.");
        stream.SetSyncStreamVolume(1, -200); Wait(native, 10); native.PrepareCapture(native.QuantumFrames * native.Channels * 8); Wait(native, 10); Check(native.CapturedPCM().Any(v => Math.Abs(v) > .1f), "Live gain exposes the real waveform without restarting.");
        Wait(native, 20); var before = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; Wait(native, 64); Check(native.MixManagedBytes == before && FAudioContext.AllocationCalls == calls, "64 warmed native mix passes allocate zero measured bytes/calls.");
        player.StreamPaused = true; Wait(native, 20); before = native.MixManagedBytes; calls = FAudioContext.AllocationCalls; Wait(native, 64); Check(native.MixManagedBytes == before && FAudioContext.AllocationCalls == calls, "64 warmed paused native passes allocate zero measured bytes/calls."); player.StreamPaused = false;
        stream.SetSyncStream(1, negative); Check(!player.GetStreamPlayback().IsPlaying(), "Owner structural edit stops and replaces attached child states."); player.Stop(); player.Play(); positive.MixHook = () => stream.SetSyncStream(0, negative); Wait(native, 5); Reject<Exception>(() => tree.ProcessFrame(.01)); Check(!player.IsPlaying(), "Native reentrant structural mutation is contained and reported at owner frame."); positive.MixHook = null;
        player.Play(); player.Stop(); Check(!positive.IsDisposed && !negative.IsDisposed && !stream.IsDisposed, "Player owns playbacks and borrows resources.");
        var settings = ProjectSettings.Instance; var enabled = settings.Get(ProjectSettings.AudioDriverEnableInput); settings.Set(ProjectSettings.AudioDriverEnableInput, true);
        try
        {
            using var microphone = new AudioStreamMicrophone(); using var random = new AudioStreamRandomizer(); random.AddStream(-1, microphone);
            using var monitored = new AudioStreamSynchronized { StreamCount = 1 }; monitored.SetSyncStream(0, random); player.Stream = monitored; player.Play(); var handle = player.GetStreamPlayback();
            Task.Run(() => { Reject<InvalidOperationException>(() => handle.Dispose()); Reject<InvalidOperationException>(() => monitored.SetSyncStream(0, positive)); }).GetAwaiter().GetResult();
            Check(!handle.IsDisposed && ReferenceEquals(monitored.GetSyncStream(0), random) && handle.IsPlaying(), "Microphone ownership propagates through mixed composites before off-owner disposal/edit can consume state.");
            Task.Run(() => Reject<InvalidOperationException>(() => monitored.InstantiatePlayback())).GetAwaiter().GetResult();
            monitored.SetSyncStream(0, positive); Check(AudioServer.Instance.CurrentInput?.Active != true, "Owner cohort replacement releases nested microphone capture.");
            player.Stop();
        }
        finally { settings.Set(ProjectSettings.AudioDriverEnableInput, enabled); }
    }
    private static void Wait(FAudioContext context, long count) { var end = context.MixPasses + count; var watch = System.Diagnostics.Stopwatch.StartNew(); while (context.MixPasses < end) { if (watch.Elapsed > TimeSpan.FromSeconds(5)) throw new InvalidOperationException("Native audio did not advance."); Thread.Sleep(1); } }
    private sealed class Probe(Vector2 sample) : AudioStream
    {
        internal double Length = 1000, BPM; internal int BarBeats, BeatCount, Loops, Frames = int.MaxValue;
        internal bool Loop, FailFactory, FailStart, FailSeek, FailStop, FailDispose, Nonfinite, Wave;
        internal Action? FactoryHook, MixHook, CursorHook;
        internal readonly List<ProbePlayback> Instances = []; internal ProbePlayback? Last => Instances.LastOrDefault();
        protected override AudioStreamPlayback OnInstantiatePlayback() { if (FailFactory) throw new ApplicationException("Factory failure"); var p = new ProbePlayback(this, sample); Instances.Add(p); try { FactoryHook?.Invoke(); return p; } catch (Exception error) { Exception? cleanup = null; try { p.Dispose(); } catch (Exception failure) { cleanup = failure; } Resource.ThrowCombined(error, cleanup); throw; } }
        protected override double OnGetLength() => Length;
        protected override double OnGetBPM() => BPM;
        protected override int OnGetBarBeats() => BarBeats;
        protected override int OnGetBeatCount() => BeatCount;
        protected override bool OnHasLoop() => Loop;
        protected override bool OnIsMonophonic() => false;
        protected override Resource CreateDuplicateInstance() => new Probe(sample) { Length = Length, BPM = BPM, BarBeats = BarBeats, BeatCount = BeatCount, Loop = Loop, Frames = Frames };
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force) { }
        internal sealed class ProbePlayback(Probe source, Vector2 sample) : AudioStreamPlayback
        {
            internal bool Active; internal double Position; internal int Starts, Stops; internal float LastRate; private int _frames, _remaining;
            protected override void OnStart(double position) { Starts++; Position = position; _remaining = source.Frames; _frames = 0; Active = true; if (source.FailStart) throw new ApplicationException("Start failure"); }
            protected override void OnStop() { Stops++; Active = false; if (source.FailStop) throw new ApplicationException("Stop failure"); }
            protected override bool OnIsPlaying() => Active;
            protected override int OnGetLoopCount() => source.Loops;
            protected override double OnGetPlaybackPosition() { source.CursorHook?.Invoke(); return Position; }
            protected override void OnSeek(double time) { Position = time; if (source.FailSeek) throw new ApplicationException("Seek failure"); }
            protected override int OnMix(Span<Vector2> buffer, float rateScale)
            {
                source.MixHook?.Invoke(); LastRate = rateScale; var count = Active ? Math.Min(_remaining, buffer.Length) : 0;
                for (var i = 0; i < count; i++) buffer[i] = source.Nonfinite ? new(float.NaN, 0) : source.Wave ? sample * MathF.Sin((_frames + i) * (2 * MathF.PI / 64)) : sample;
                buffer[count..].Fill(new(999, 999)); _frames += count; _remaining -= count; Position += count * rateScale / 48000; if (_remaining == 0) Active = false; return count;
            }
            protected override void Dispose(bool disposing) { Active = false; base.Dispose(disposing); if (source.FailDispose) throw new ApplicationException("Dispose failure"); }
        }
    }
    private static void Near(Vector2 actual, Vector2 expected, string message) { if ((actual - expected).Length() > 1e-6f) throw new InvalidOperationException(message + $": {actual} != {expected}"); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
