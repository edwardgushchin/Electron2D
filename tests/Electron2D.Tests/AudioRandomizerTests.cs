using Electron2D;

internal static class AudioRandomizerTests
{
    internal static void Run(bool native = false)
    {
        VerifyPoolAndNotifications(); VerifySelection(); VerifyVariationAndWarm(); VerifyCopyAndScene(); VerifyFailureAndGraphs();
        if (native) VerifyNative();
        Console.WriteLine(native ? "Randomizer managed/native selection, lifetime and allocation checks passed." : "Randomizer pool/selection/variation/copy/failure and warmed CPU checks passed.");
    }
    private static void VerifyPoolAndNotifications()
    {
        using var pool = new AudioStreamRandomizer(); using var a = new Probe(.1f); using var b = new Probe(.2f);
        Check(pool.StreamsCount == 0 && pool.Mode == AudioStreamRandomizer.PlaybackMode.RandomNoRepeats && pool.RandomPitch == 1 && pool.RandomPitchSemitones == 0 && pool.RandomVolumeOffsetDB == 0, "Defaults.");
        Check(pool.IsMetaStream() && !pool.IsMonophonic() && pool.GetLength() == 0 && pool.GetParameterList().Length == 0, "Inherited concrete policies.");
        var changed = 0; var properties = 0; pool.Changed += _ => changed++; pool.PropertyListChanged += _ => properties++;
        pool.StreamsCount = 2; Check(pool.GetStream(0) is null && pool.GetStreamProbabilityWeight(1) == 1 && changed == 0 && properties == 0, "Silent resize/default entries.");
        pool.SetStream(0, a); pool.SetStream(1, b); pool.SetStreamProbabilityWeight(1, -2); pool.AddStream(-999, null); pool.MoveStream(0, 3);
        Check(ReferenceEquals(pool.GetStream(0), b) && pool.GetStream(1) is null && ReferenceEquals(pool.GetStream(2), a), "Move targets the original insertion boundary.");
        pool.RemoveStream(1); Check(pool.StreamsCount == 2 && changed == 6 && properties == 3, "Pool notifications.");
        pool.MoveStream(1, 1); Check(changed == 7 && properties == 4, "Equal move still reports structural authoring.");
        pool.RandomPitch = .5f; pool.RandomPitchSemitones = 12; Check(pool.RandomPitch == 2 && Math.Abs(pool.RandomPitchSemitones - 12) < .0001f, "Pitch representations.");
        pool.RandomPitchSemitones = -12; Check(pool.RandomPitch == .5f && pool.RandomPitchSemitones == 0, "Negative exponent/raw multiplier semantics."); pool.RandomVolumeOffsetDB = -10; Check(pool.RandomVolumeOffsetDB == 0, "Volume clamps.");
        Reject<ArgumentOutOfRangeException>(() => pool.StreamsCount = -1); Reject<ArgumentOutOfRangeException>(() => pool.GetStream(-1)); Reject<ArgumentOutOfRangeException>(() => pool.MoveStream(0, 3));
        Reject<ArgumentOutOfRangeException>(() => pool.Mode = (AudioStreamRandomizer.PlaybackMode)3); Reject<ArgumentOutOfRangeException>(() => pool.RandomPitch = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => pool.RandomPitchSemitones = 2000); Reject<ArgumentOutOfRangeException>(() => pool.SetStreamProbabilityWeight(0, float.PositiveInfinity));
        using var disposed = new Probe(.3f); disposed.Dispose(); Reject<ObjectDisposedException>(() => pool.SetStream(0, disposed));
        var count = pool.StreamsCount; Action<Resource> failChanged = _ => throw new ApplicationException("Changed failure"); Action<ElectronObject> failProperties = _ => throw new ApplicationException("Property-list failure");
        pool.Changed += failChanged; pool.PropertyListChanged += failProperties;
        try { Reject<AggregateException>(() => pool.AddStream(-1, a)); Check(pool.StreamsCount == count + 1 && properties == 5, "Structural state commits and both notifications run after failures."); }
        finally { pool.Changed -= failChanged; pool.PropertyListChanged -= failProperties; }
        var slot = pool.GetPropertyList().OfType<PropertyDescriptor<AudioStreamRandomizer, AudioStream?>>().Single(p => p.Name == "Stream_0/Stream");
        slot.SetValue(pool, a); Check(ReferenceEquals(pool.GetStream(0), a), "Typed indexed descriptor executes.");
    }
    private static void VerifySelection()
    {
        using var a = new Probe(.1f, length: 3); using var b = new Probe(.2f, length: 7); using var pool = new AudioStreamRandomizer();
        pool.AddStream(-1, a, 2); pool.AddStream(-1, a, 9); pool.AddStream(-1, null); pool.AddStream(-1, b, 1);
        // Deterministic no-repeat evidence after an explicit preceding A selection.
        pool.Mode = AudioStreamRandomizer.PlaybackMode.Sequential;
        using (var first = pool.InstantiatePlayback()) { first.Start(); }
        pool.Mode = AudioStreamRandomizer.PlaybackMode.RandomNoRepeats; pool.SetStreamProbabilityWeight(3, 1);
        var preceding = pool.GetLength();
        for (var i = 0; i < 64; i++) { using var p = pool.InstantiatePlayback(); p.Start(); Check(pool.GetLength() != preceding, "Shared identity history alternates eligible identities."); preceding = pool.GetLength(); }
        pool.Mode = AudioStreamRandomizer.PlaybackMode.Sequential; pool.SetStreamProbabilityWeight(0, 0); pool.SetStreamProbabilityWeight(1, -3); pool.SetStreamProbabilityWeight(3, 0);
        var prior = pool.GetLength();
        for (var i = 0; i < 32; i++) { using var p = pool.InstantiatePlayback(); p.Start(); Check(pool.GetLength() != prior, "Sequential deduplicates and ignores weights."); prior = pool.GetLength(); }
        using var captured = pool.InstantiatePlayback(); var capturedValue = pool.GetLength() == 3 ? .1f : .2f; pool.StreamsCount = 0; captured.Start(); Check(captured.MixAudio(1, 1)[0].X == capturedValue, "Pool edits do not retarget captured playback.");
        using var empty = pool.InstantiatePlayback(); empty.Start(); Check(!empty.IsPlaying() && empty.MixAudio(1, 8).All(v => v == Vector2.Zero), "Empty playback supplies requested silence with inactive state.");
        pool.AddStream(-1, a, 1); pool.Mode = AudioStreamRandomizer.PlaybackMode.RandomNoRepeats;
        for (var i = 0; i < 10; i++) { using var p = pool.InstantiatePlayback(); p.Start(); Check(p.MixAudio(1, 1)[0].X == .1f, "Single identity permits repeats."); }
        pool.AddStream(-1, b, 3); pool.Mode = AudioStreamRandomizer.PlaybackMode.Random; var bCount = 0;
        for (var i = 0; i < 4000; i++) { using var p = pool.InstantiatePlayback(); p.Start(); if (p.MixAudio(1, 1)[0].X == .2f) bCount++; }
        Check(bCount is > 2800 and < 3200, "1:3 weighted frequency has a broad independent confidence bound.");
        pool.SetStreamProbabilityWeight(0, float.MaxValue); pool.SetStreamProbabilityWeight(1, float.MaxValue); using var huge = pool.InstantiatePlayback(); huge.Start(); Check(huge.MixAudio(1, 1).Length == 1, "Weights sum in double precision.");
        a.Monophonic = true; pool.SetStreamProbabilityWeight(0, 0); Check(pool.IsMonophonic(), "Monophony includes zero-weight pool members.");
    }
    private static void VerifyVariationAndWarm()
    {
        using var child = new Probe(.25f); using var pool = new AudioStreamRandomizer { RandomPitch = 2, RandomVolumeOffsetDB = 6 }; pool.AddStream(-1, child);
        using var playback = pool.InstantiatePlayback(); var target = new Vector2[32]; double pitchSum = 0, volumeSum = 0;
        for (var i = 0; i < 1000; i++)
        {
            playback.Start(.2); playback.MixInto(target, 1.5f); var pitch = child.LastRate / 1.5f; var volumeDB = 20 * Math.Log10(target[0].X / .25f);
            Check(pitch is >= .5f and <= 2 && volumeDB is >= -6.0001 and <= 6.0001, "Log-frequency and DB ranges."); pitchSum += Math.Log(pitch); volumeSum += volumeDB;
        }
        Check(Math.Abs(pitchSum / 1000) < .08 && Math.Abs(volumeSum / 1000) < 1, "Pitch is symmetric in log space and volume in DB.");
        playback.Stop(); playback.Seek(.4); Check(playback.GetPlaybackPosition() == .4, "Started wrapper delegates seek even after Stop.");
        pool.RandomPitch = 1; pool.RandomVolumeOffsetDB = 0; playback.Start(.1); playback.MixInto(target, 1); Check(child.LastRate == 1 && target.All(v => v.X == .25f), "Start reads current variation, without selecting again.");
        for (var i = 0; i < 20; i++) { playback.MixInto(target, 1); pool.GetLength(); pool.IsMonophonic(); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { playback.MixInto(target, 1); pool.GetLength(); pool.IsMonophonic(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Active mix/metadata queries allocate zero after warmup.");
        playback.Stop(); for (var i = 0; i < 20; i++) playback.MixInto(target, 1); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) playback.MixInto(target, 1); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Idle mix allocates zero.");
    }
    private static void VerifyCopyAndScene()
    {
        using var child = new Probe(.2f); using var pool = new AudioStreamRandomizer { Mode = AudioStreamRandomizer.PlaybackMode.Sequential, RandomPitch = 2, RandomVolumeOffsetDB = 3, ResourceLocalToScene = true };
        pool.AddStream(-1, child, .7f); pool.AddStream(-1, child, 2); using var first = pool.InstantiatePlayback();
        using var shallow = (AudioStreamRandomizer)pool.Duplicate(); Check(ReferenceEquals(shallow.GetStream(0), child) && shallow.GetLength() == 0, "Shallow copy borrows streams with independent history.");
        using var deep = (AudioStreamRandomizer)pool.DuplicateDeep(DeepDuplicateMode.All); Check(!ReferenceEquals(deep.GetStream(0), child) && ReferenceEquals(deep.GetStream(0), deep.GetStream(1)) && deep.RandomPitch == 2 && deep.RandomVolumeOffsetDB == 3, "Deep graph identity and configuration.");
        using var player = new AudioStreamPlayer { Stream = pool }; using var scene = new PackedScene(); scene.Pack(player); using var instance = (AudioStreamPlayer)scene.Instantiate();
        var local = (AudioStreamRandomizer)instance.Stream!; Check(!ReferenceEquals(local, pool) && local.StreamsCount == 2 && local.GetStreamProbabilityWeight(0) == .7f, "PackedScene local resource storage.");
        shallow.Dispose(); Check(!child.IsDisposed, "Borrowed child resources survive pool disposal."); deep.GetStream(0)!.Dispose();
    }
    private static void VerifyFailureAndGraphs()
    {
        using var a = new AudioStreamRandomizer(); using var b = new AudioStreamRandomizer(); a.AddStream(-1, b); Reject<InvalidOperationException>(() => b.AddStream(-1, a)); Reject<InvalidOperationException>(() => a.AddStream(-1, a));
        using var child = new Probe(.1f); b.AddStream(-1, child); child.FactoryHook = () => a.InstantiatePlayback(); Reject<InvalidOperationException>(() => a.InstantiatePlayback()); child.FactoryHook = null;
        using (var recovered = a.InstantiatePlayback()) { recovered.Start(); Check(recovered.MixAudio(1, 2).Length == 2, "Preparation guard recovers after callback failure."); }
        a.StreamsCount = 0; b.StreamsCount = 0; using var historicalA = a.InstantiatePlayback(); b.AddStream(-1, a); using var historicalB = b.InstantiatePlayback(); Reject<InvalidOperationException>(() => a.GetLength());
        b.StreamsCount = 0;
        using var concurrentA = new AudioStreamRandomizer(); using var concurrentB = new AudioStreamRandomizer();
        var left = Task.Run(() => { try { concurrentA.AddStream(-1, concurrentB); return true; } catch (InvalidOperationException) { return false; } });
        var right = Task.Run(() => { try { concurrentB.AddStream(-1, concurrentA); return true; } catch (InvalidOperationException) { return false; } });
        Check(left.Result != right.Result, "Concurrent graph edits permit one link and reject the cycle atomically.");
        var deepChain = Enumerable.Range(0, 257).Select(_ => new AudioStreamRandomizer()).ToArray();
        try { for (var i = deepChain.Length - 2; i >= 0; i--) deepChain[i].AddStream(-1, deepChain[i + 1]); Reject<InvalidOperationException>(() => deepChain[0].InstantiatePlayback()); }
        finally { foreach (var item in deepChain) item.Dispose(); }
        using var victim = new AudioStreamRandomizer(); victim.AddStream(-1, child); child.AfterFactory = _ => victim.Dispose(); Reject<ObjectDisposedException>(() => victim.InstantiatePlayback()); Check(child.LastPlayback!.IsDisposed, "Dispose-during-factory releases a prepared child."); child.AfterFactory = null;
        using var deadParent = new AudioStreamRandomizer(); deadParent.AddStream(-1, child); using var p = deadParent.InstantiatePlayback(); deadParent.Dispose(); Reject<ObjectDisposedException>(() => p.Start()); p.Stop();
        using var failParent = new AudioStreamRandomizer(); failParent.AddStream(-1, child); using var failPlayback = failParent.InstantiatePlayback(); child.FailDispose = true; Reject<ApplicationException>(() => failPlayback.Dispose()); Check(failPlayback.IsDisposed && child.LastPlayback!.IsDisposed, "Child disposal failure still finalizes both playbacks."); child.FailDispose = false;
    }
    private static void VerifyNative()
    {
        using var a = new Probe(.15f); using var b = new Probe(-.25f); using var pool = new AudioStreamRandomizer { Mode = AudioStreamRandomizer.PlaybackMode.Sequential };
        pool.AddStream(-1, a); pool.AddStream(-1, b); var root = new Node(); var player = new AudioStreamPlayer { Stream = pool, MaxPolyphony = 3 }; root.AddChild(player); using var tree = new SceneTree(root);
        player.Play(); Check(a.Stops == 0, "Fresh native Play does not call child Stop before its first Start."); Check(a.Instances == 1 && b.Instances == 0, "One child preparation per actual Play, independent of configured polyphony."); var first = player.GetStreamPlayback(); player.Stop();
        player.Play(); Check(a.Stops == 1 && b.Stops == 0, "Reusing a stopped native slot does not stop either child redundantly."); Check(a.Instances == 1 && b.Instances == 1 && first.IsDisposed, "Pooled native slot receives fresh playback/next selection.");
        var native = AudioServer.Service.Native; Wait(native, 20); native.PrepareCapture(16000); Wait(native, 20); Check(native.CapturedPCM().Any(v => v < -.1f), "Actual FAudio PCM contains the second child.");
        var bytes = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 active native passes allocate zero measured bytes/calls.");
        player.StreamPaused = true; Wait(native, 20); bytes = native.MixManagedBytes; calls = FAudioContext.AllocationCalls; Wait(native, 64); Check(native.MixManagedBytes == bytes && FAudioContext.AllocationCalls == calls, "64 paused native passes allocate zero measured bytes/calls."); player.StreamPaused = false;
        player.Play(); player.Play(); Check(player.HasStreamPlayback() && a.Instances + b.Instances == 4, "Polyphony prepares one fresh independent child at a time.");
        a.FactoryHook = () => player.Stream = null; Reject<InvalidOperationException>(() => player.Play()); Check(ReferenceEquals(player.Stream, pool), "Reentrant factory mutation rejects before changing player state."); a.FactoryHook = null;
        player.Stop(); player.Play();
        // Both potential outgoing selections can fail cleanup; replacement still owns its fresh child.
        a.FailDispose = b.FailDispose = true;
        Reject<ApplicationException>(() => player.Play());
        Check(!a.LastPlayback!.IsDisposed || !b.LastPlayback!.IsDisposed, "Cleanup failure leaves the committed replacement child live.");
        a.FailDispose = b.FailDispose = false; player.Play();
        a.FailMix = b.FailMix = true; Wait(native, 5); Reject<Exception>(() => tree.ProcessFrame(.01)); Check(!player.IsPlaying(), "Child mix failure reports/stops all failed voices."); a.FailMix = b.FailMix = false;
        player.Play(); a.StartHook = b.StartHook = () => player.Dispose();
        Reject<InvalidOperationException>(() => player.Play()); Check(!player.IsDisposed, "Reentrant Start disposal rejects before losing player ownership."); a.StartHook = b.StartHook = null;
        player.Play(); a.StopHook = b.StopHook = () => player.Stream = null;
        Reject<Exception>(() => player.Stop()); Check(ReferenceEquals(player.Stream, pool), "Reentrant Stop mutation rejects before stream changes."); a.StopHook = b.StopHook = null;
        player.Play(); player.Stop(); tree.Dispose(); Check(!a.IsDisposed && !b.IsDisposed && !pool.IsDisposed, "Native cleanup retains all borrowed resources.");
    }
    private static void Wait(FAudioContext native, long passes)
    {
        var goal = native.MixPasses + passes; var time = System.Diagnostics.Stopwatch.GetTimestamp();
        while (native.MixPasses < goal) { if (System.Diagnostics.Stopwatch.GetElapsedTime(time).TotalSeconds > 5) throw new InvalidOperationException("Native mix deadline."); Thread.Sleep(1); }
    }
    private sealed class Probe(float sample, double length = 1000) : AudioStream
    {
        internal bool Monophonic, FailMix, FailDispose;
        internal int Instances, Stops;
        internal float LastRate;
        internal Action? FactoryHook;
        internal Action? StartHook, StopHook;
        internal Action<AudioStreamPlayback>? AfterFactory;
        internal AudioStreamPlayback? LastPlayback;
        protected override AudioStreamPlayback OnInstantiatePlayback() { FactoryHook?.Invoke(); Instances++; var p = new Cursor(this, sample); LastPlayback = p; AfterFactory?.Invoke(p); return p; }
        protected override double OnGetLength() => length;
        protected override bool OnIsMonophonic() => Monophonic;
        protected override Resource CreateDuplicateInstance() => new Probe(sample, length);
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> forceDuplicate) { ((Probe)target).Monophonic = Monophonic; }
        private sealed class Cursor(Probe source, float sample) : AudioStreamPlayback
        {
            private bool _active; private double _position;
            protected override void OnStart(double time) { source.StartHook?.Invoke(); _position = time; _active = true; }
            protected override void OnStop() { source.StopHook?.Invoke(); source.Stops++; _active = false; }
            protected override bool OnIsPlaying() => _active;
            protected override double OnGetPlaybackPosition() => _position;
            protected override void OnSeek(double time) => _position = time;
            protected override int OnMix(Span<Vector2> buffer, float rate)
            {
                if (source.FailMix) throw new ApplicationException("Child mix failure");
                if (!_active) { buffer.Clear(); return 0; }
                source.LastRate = rate; buffer.Fill(new(sample, sample)); _position += buffer.Length * rate / AudioServer.GetMixRate(); return buffer.Length;
            }
            protected override void Dispose(bool disposing) { base.Dispose(disposing); if (source.FailDispose) throw new ApplicationException("Child dispose failure"); }
        }
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
