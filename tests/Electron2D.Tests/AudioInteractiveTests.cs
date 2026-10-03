using Electron2D;
using From = Electron2D.AudioStreamInteractive.TransitionFromTime;
using To = Electron2D.AudioStreamInteractive.TransitionToTime;
using Fade = Electron2D.AudioStreamInteractive.FadeMode;
using Advance = Electron2D.AudioStreamInteractive.AutoAdvanceMode;

internal static class AudioInteractiveTests
{
    internal static void Run(bool native = false)
    {
        Resources(); Timing(); Fades(); Progression(); Lifetime(); CopiesAndGraphs(); Edges(); Warm();
        if (native) Native();
        Console.WriteLine("Interactive clip rules, timing/fades/filler/hold, lifetime/copies/parameters and warmed CPU" + (native ? "/native/input" : "") + " checks passed.");
    }
    private static double Rate => AudioServer.Instance.GetMixRate();
    private static void Resources()
    {
        using var s = new AudioStreamInteractive();
        Check(s.ClipCount == 0 && s.InitialClip == 0 && s.GetLength() == 0 && s.IsMetaStream() && s.IsMonophonic() && s.GetClipName(-1) == "All Clips", "Resource defaults.");
        Check(s.GetClipStream(62) is null && s.GetClipName(62) == "" && s.GetClipAutoAdvance(62) == Advance.Disabled && s.GetClipAutoAdvanceNextClip(62) == 0, "All capacity slots exist independently of count.");
        Reject<ArgumentOutOfRangeException>(() => s.ClipCount = 64); Reject<ArgumentOutOfRangeException>(() => s.ClipCount = -1); Reject<ArgumentOutOfRangeException>(() => s.InitialClip = 0); Reject<ArgumentOutOfRangeException>(() => s.GetClipName(63)); Reject<ArgumentNullException>(() => s.SetClipName(0, null!));
        var properties = 0; var parameters = 0; var changed = 0; s.PropertyListChanged += _ => properties++; s.ParameterListChanged += () => parameters++; s.Changed += _ => changed++;
        s.SetClipName(62, "hidden"); s.ClipCount = 63; s.InitialClip = 62; s.ClipCount = 63;
        s.SetClipAutoAdvance(1, Advance.Enabled); s.SetClipAutoAdvanceNextClip(1, 62);
        s.AddTransition(1, 2, From.NextBar, To.PreviousPosition, Fade.Cross, .5f, true, 62, true);
        s.AddTransition(-1, -1, From.Immediate, To.Start, Fade.Disabled, 0);
        Check(s.HasTransition(1, 2) && s.GetTransitionFromTime(1, 2) == From.NextBar && s.GetTransitionToTime(1, 2) == To.PreviousPosition && s.GetTransitionFadeMode(1, 2) == Fade.Cross && s.GetTransitionFadeBeats(1, 2) == .5f && s.IsTransitionUsingFillerClip(1, 2) && s.GetTransitionFillerClip(1, 2) == 62 && s.IsTransitionHoldingPrevious(1, 2), "All exact transition queries.");
        Check(s.GetTransitionList().SequenceEqual(new[] { 1, 2, -1, -1 }), "Insertion ordering.");
        s.AddTransition(1, 2, From.End, To.Start, Fade.Out, 1); Check(s.GetTransitionList().SequenceEqual(new[] { 1, 2, -1, -1 }), "Replacement retains order.");
        Reject<ArgumentOutOfRangeException>(() => s.AddTransition(63, 2, From.End, To.Start, Fade.Out, 1)); Reject<ArgumentOutOfRangeException>(() => s.AddTransition(1, 2, (From)4, To.Start, Fade.Out, 1)); Reject<ArgumentOutOfRangeException>(() => s.AddTransition(1, 2, From.End, (To)3, Fade.Out, 1)); Reject<ArgumentOutOfRangeException>(() => s.AddTransition(1, 2, From.End, To.Start, (Fade)5, 1));
        foreach (var value in new[] { float.NaN, float.PositiveInfinity, -1 }) Reject<ArgumentOutOfRangeException>(() => s.AddTransition(1, 2, From.End, To.Start, Fade.Out, value));
        s.AddTransition(1, 2, From.End, To.Start, Fade.Out, 1, true, 62); s.AddTransition(62, 0, From.Immediate, To.Start, Fade.In, 0); s.ClipCount = 3;
        Check(s.InitialClip == 0 && s.GetClipAutoAdvance(1) == Advance.Disabled && s.GetClipAutoAdvanceNextClip(1) == 0 && !s.IsTransitionUsingFillerClip(1, 2) && !s.HasTransition(62, 0) && s.HasTransition(-1, -1) && s.GetClipName(62) == "hidden", "Shrink sanitizes dependent indices and retains hidden configuration.");
        s.EraseTransition(1, 2); Reject<KeyNotFoundException>(() => s.EraseTransition(1, 2)); Reject<KeyNotFoundException>(() => s.GetTransitionFadeBeats(1, 2));
        Check(properties == 4 && parameters == 3 && changed == 0, "Count/advance notifications only.");
        var descriptor = s.GetPropertyList().OfType<PropertyDescriptor<AudioStreamInteractive, string>>().Single(p => p.Name == "Clip_1/Name"); descriptor.SetValue(s, "typed"); Check(s.GetClipName(1) == "typed", "Typed authoring.");
        Action<ElectronObject> fail = _ => throw new ApplicationException(); s.PropertyListChanged += fail; Reject<ApplicationException>(() => s.ClipCount = 2); Check(s.ClipCount == 2 && parameters == 4, "Parameter notification still runs after property observer failure."); s.PropertyListChanged -= fail;
        Check(s.GetParameterList().Single() == AudioStreamPlaybackInteractive.SwitchToClipParameter, "Typed music parameter.");
    }
    private static void Timing()
    {
        using var a = new Probe(new(1, 0)); using var b = new Probe(new(0, 1)); using var s = Pair(a, b); using var p = (AudioStreamPlaybackInteractive)s.InstantiatePlayback();
        var frames = new Vector2[100]; Check(p.GetCurrentClipIndex() == -1 && p.MixInto(frames, 1) == 0, "Inactive selection."); p.SwitchToClip(1); Check(p.MixInto(frames, 1) == 0 && a.Last is null, "Inactive requests do not start orphan children.");
        p.Start(9); Check(a.Last!.Position == 0 && p.GetCurrentClipIndex() == 0, "Initial clip always starts at zero.");
        s.AddTransition(0, 1, From.Immediate, To.Start, Fade.Out, 0); p.MixInto(frames, 0); Check(p.GetCurrentClipIndex() == 1 && b.Last!.LastRate == 1, "Preset request applies at output clock even with rate zero."); Near(frames[0], new(0, 1), "Instant outgoing cut.");
        p.Stop(); p.Start(); p.Seek(200); Check(p.GetPlaybackPosition() == 0 && p.GetLoopCount() == 0 && a.Last.Position == 0 && p.GetCurrentClipIndex() == 0, "Restart and validated no-op seek.");
        a.BPM = 600; a.Bars = 2; a.Beats = 5; a.Length = 100;
        foreach (var (mode, seconds) in new[] { (From.NextBeat, .1), (From.NextBar, .2), (From.End, .5) })
        {
            p.Stop(); p.Start(); s.AddTransition(0, 1, mode, To.Start, Fade.Out, 0); p.SwitchToClipByName("B");
            var before = new Vector2[(int)Math.Round(seconds * Rate)]; p.MixInto(before, 1); Check(p.GetCurrentClipIndex() == 0 && before.All(v => v.X == 1 && v.Y == 0), "Musical wait " + mode + $" current={p.GetCurrentClipIndex()} firstbad={Array.FindIndex(before, v => v.X != 1 || v.Y != 0)} last={before[^1]} rate={Rate}");
            p.MixInto(frames.AsSpan(0, 1), 1); Check(p.GetCurrentClipIndex() == 1, "Destination begins at timing boundary " + mode);
        }
        a.BPM = 0; a.Length = 10; p.Stop(); p.Start(); p.MixInto(frames, 1); var pos = a.Last.Position;
        s.AddTransition(0, 1, From.NextBeat, To.SamePosition, Fade.Out, 0); p.SwitchToClip(1); p.MixInto(frames, 1); Check(Math.Abs(b.Last!.StartedAt - pos) < 1e-10, "Same-position seek without BPM.");
        var previous = b.Last.Position; s.AddTransition(1, 0, From.Immediate, To.Start, Fade.Out, 0); p.SwitchToClip(0); p.MixInto(frames, 1); s.AddTransition(0, 1, From.Immediate, To.PreviousPosition, Fade.Out, 0); p.SwitchToClip(1); p.MixInto(frames, 1); Check(Math.Abs(b.Last.StartedAt - previous) < 1e-9, "Previous mixed cursor resumes.");
        p.Stop(); p.Start(); b.Length = .00001; p.MixInto(frames, 1); s.AddTransition(0, 1, From.Immediate, To.SamePosition, Fade.Out, 0); p.SwitchToClip(1); p.MixInto(frames, 1); Check(b.Last.StartedAt == 0, "Same-position overflow returns to start.");
        Reject<ArgumentException>(() => p.SwitchToClipByName("absent")); Reject<ArgumentNullException>(() => p.SwitchToClipByName(null!)); Reject<ArgumentOutOfRangeException>(() => p.SwitchToClip(2)); p.SwitchToClipByName(""); p.SwitchToClip(-1);
        using var empty = new AudioStreamInteractive(); using var ep = empty.InstantiatePlayback(); ep.Start(); Check(!ep.IsPlaying(), "Zero/null clip starts safely.");
        s.SetClipStream(0, null); p.Start(); Check(!p.IsPlaying(), "Null initial clip is a safe inactive state.");
    }
    private static void Fades()
    {
        using var a = new Probe(new(1, 0)); using var b = new Probe(new(0, 1)); using var s = Pair(a, b); using var p = (AudioStreamPlaybackInteractive)s.InstantiatePlayback();
        const int length = 128; var duration = (float)(length / Rate); var frames = new Vector2[length];
        foreach (var mode in Enum.GetValues<Fade>())
        {
            p.Stop(); p.Start(); s.AddTransition(0, 1, From.Immediate, To.Start, mode, duration); p.SwitchToClip(1); p.MixInto(frames, 1);
            for (var i = 0; i < length; i++)
            {
                var t = Math.Min(1, (i + 1) / (duration * Rate)); var outGain = mode is Fade.Disabled or Fade.In ? Math.Max(0, 1 - (i + 1) / (.001 * Rate)) : Math.Max(0, 1 - t); var inGain = mode is Fade.In or Fade.Cross ? t : 1;
                Near(frames[i], new((float)outGain, (float)inGain), "Every fade coefficient " + mode);
            }
        }
        a.Frames = 4; a.Length = 4 / Rate; s.AddTransition(0, 1, From.End, To.Start, Fade.Disabled, 0); p.Stop(); p.Start(); p.SwitchToClip(1); p.MixInto(frames, 1); Check(frames.Take(4).All(v => v == new Vector2(1, 0)) && frames.Skip(4).All(v => v == new Vector2(0, 1)), "Finite end is padded and destination starts after it.");
        a.Frames = int.MaxValue; a.Length = 100; s.AddTransition(-1, -1, From.Immediate, To.Start, Fade.Out, 0); s.EraseTransition(0, 1); p.Stop(); p.Start(); p.SwitchToClip(1); p.MixInto(frames, 1); Check(frames.All(v => v == new Vector2(0, 1)), "Any/Any fallback.");
        s.AddTransition(-1, 1, From.End, To.Start, Fade.Out, 0); s.AddTransition(0, -1, From.Immediate, To.Start, Fade.Out, 0); p.Stop(); p.Start(); p.SwitchToClip(1); p.MixInto(frames, 1); Check(p.GetCurrentClipIndex() == 1, "Source wildcard precedes destination wildcard.");
        s.AddTransition(0, 1, From.End, To.Start, Fade.Out, 0); p.Stop(); p.Start(); p.SwitchToClip(1); p.MixInto(frames, 1); Check(p.GetCurrentClipIndex() == 0, "Exact rule precedes wildcard rules.");
        s.EraseTransition(0, 1); s.EraseTransition(0, -1); p.Stop(); p.Start(); p.SwitchToClip(1); p.MixInto(frames, 1); Check(p.GetCurrentClipIndex() == 0, "Destination wildcard precedes Any/Any.");
    }
    private static void Progression()
    {
        using var a = new Probe(new(1, 0)); using var b = new Probe(new(0, 1)) { Frames = 4, Length = 4 / Rate }; using var filler = new Probe(new(.25f, .25f)) { Frames = 8, Length = 8 / Rate }; using var s = Pair(a, b); s.ClipCount = 3; s.SetClipStream(2, filler);
        using var p = (AudioStreamPlaybackInteractive)s.InstantiatePlayback(); var frames = new Vector2[32];
        s.AddTransition(0, 1, From.Immediate, To.Start, Fade.Out, 0, true, 2, true); s.SetClipAutoAdvance(1, Advance.ReturnToHold); s.AddTransition(1, 0, From.End, To.Start, Fade.Out, 0);
        p.Start(); p.SwitchToClip(1); p.MixInto(frames, 1);
        Check(frames.Take(8).All(v => v == new Vector2(.25f, .25f)) && frames.Skip(8).Take(4).All(v => v == new Vector2(0, 1)) && frames.Skip(12).All(v => v == new Vector2(1, 0)), "Filler, short full-gain destination and held return have exact frame boundaries without a silent planning block.");
        p.MixInto(frames, 1); Check(p.GetCurrentClipIndex() == 0 && frames.All(v => v == new Vector2(1, 0)), "Filler destination auto returns to source remembered by hold.");
        // Reverse the same clip identities to ensure auto scheduling is independent of slot order.
        s.SetClipStream(0, b); s.SetClipStream(1, a); s.InitialClip = 1; s.SetClipAutoAdvance(0, Advance.Disabled); s.SetClipAutoAdvance(1, Advance.Enabled); s.SetClipAutoAdvanceNextClip(1, 0); a.Length = 8 / Rate; a.Frames = 8;
        s.AddTransition(1, 0, From.Immediate, To.SamePosition, Fade.Out, 0); p.Start(); p.MixInto(frames.AsSpan(0, 4), 1); p.MixInto(frames, 1); Check(frames.Take(4).All(v => v == new Vector2(1, 0)) && frames.Skip(4).Take(4).All(v => v == new Vector2(0, 1)), "Automatic end overrides timing/cursor across reverse slots.");
        p.Stop(); p.Start(); s.SetClipAutoAdvanceNextClip(1, 1); p.MixInto(frames, 1); p.MixInto(frames, 1); Check(p.IsPlaying(), "Finite ended graph remains active waiting for requests.");
        a.Frames = int.MaxValue; a.Length = 100; s.SetClipAutoAdvance(1, Advance.Disabled); s.InitialClip = 1; p.Start(); s.AddTransition(1, 0, From.End, To.Start, Fade.Out, 0); p.SwitchToClip(0); p.MixInto(frames, 1); s.AddTransition(1, 2, From.Immediate, To.Start, Fade.Out, 0); p.SwitchToClip(2); p.MixInto(frames, 1); Check(p.GetCurrentClipIndex() == 2 && !b.Last!.Active, "Replacing a delayed destination stops the canceled child.");
    }
    private static void Lifetime()
    {
        using var a = new Probe(new(.1f, .2f)); using var b = new Probe(new(.2f, .1f)); using var bad = new Probe(Vector2.Zero) { FailFactory = true }; using var s = Pair(a, b); using var p = (AudioStreamPlaybackInteractive)s.InstantiatePlayback(); p.Start();
        var first = a.Last; s.SetClipStream(1, bad); Reject<ApplicationException>(() => p.Start()); Check(p.IsPlaying() && !first!.IsDisposed, "Failed refresh preserves prior prepared ownership/playing latch."); s.SetClipStream(1, b); p.MixInto(new Vector2[8], 1); Check(!p.IsPlaying() && !first!.Active, "Changed configuration invalidates all cached child states at mix."); p.Start();
        var frames = new Vector2[8]; a.Hook = () => p.Dispose(); Reject<InvalidOperationException>(() => p.MixInto(frames, 1)); Check(!p.IsDisposed && !p.IsPlaying(), "Callback disposal rejects before consuming parent ownership and failed mix stops states.");
        a.Hook = () => s.SetClipName(0, "reenter"); p.Start(); Reject<InvalidOperationException>(() => p.MixInto(frames, 1)); a.Hook = null;
        using var fresh = new Probe(Vector2.Zero) { FactoryHook = () => p.Dispose() }; s.SetClipStream(0, fresh); Reject<InvalidOperationException>(() => p.Start()); Check(!p.IsDisposed && fresh.Last!.IsDisposed, "Factory callback disposal rejects and rolls back new ownership."); s.SetClipStream(0, a);
        a.FailStart = true; Reject<ApplicationException>(() => p.Start()); Check(!p.IsPlaying() && !b.Last!.Active, "Failed start closes all children."); a.FailStart = false; p.Start(); a.Nonfinite = true; Reject<ArithmeticException>(() => p.MixInto(frames, 1)); a.Nonfinite = false;
        p.Start(); a.FailStop = true; Reject<ApplicationException>(() => p.Stop()); Check(!p.IsPlaying() && !b.Last!.Active, "Stop failure attempts all states."); a.FailStop = false;
        s.SetClipStream(1, null); p.Start(); Check(b.Last!.IsDisposed, "Refresh closes removed child ownership.");
        a.FailDispose = true; Reject<ApplicationException>(() => p.Dispose()); Check(p.IsDisposed && a.Last!.IsDisposed, "Dispose failure still consumes owned graph."); a.FailDispose = false;
    }
    private static void CopiesAndGraphs()
    {
        using var s = new AudioStreamInteractive(); using var r = new AudioStreamRandomizer(); using var sync = new AudioStreamSynchronized(); s.SetClipStream(62, r); Reject<InvalidOperationException>(() => r.AddStream(-1, s)); s.SetClipStream(62, null); r.AddStream(-1, sync); sync.SetSyncStream(31, s); Reject<InvalidOperationException>(() => s.SetClipStream(0, r)); sync.SetSyncStream(31, null); Reject<InvalidOperationException>(() => s.SetClipStream(0, s));
        using var child = new AudioStreamWAV { Data = new byte[128] }; s.ClipCount = 2; s.SetClipStream(0, child); s.SetClipStream(1, child); s.SetClipName(0, "A"); s.SetClipName(1, "B"); s.SetClipStream(62, child); s.AddTransition(-1, -1, From.Immediate, To.Start, Fade.Out, 0, false, -1, true);
        using var shallow = (AudioStreamInteractive)s.Duplicate(); using var deep = (AudioStreamInteractive)s.Duplicate(true); Check(ReferenceEquals(shallow.GetClipStream(0), child) && !ReferenceEquals(deep.GetClipStream(0), child) && ReferenceEquals(deep.GetClipStream(0), deep.GetClipStream(1)) && shallow.GetClipStream(62) is null && deep.IsTransitionHoldingPrevious(-1, -1), "Resource copies retain typed rule table/active aliases and exclude hidden slots.");
        s.ResourceLocalToScene = true; using var player = new AudioStreamPlayer { Stream = s }; player.SetParameter(AudioStreamPlaybackInteractive.SwitchToClipParameter, "B"); using var packed = new PackedScene(); packed.Pack(player); using var instance = (AudioStreamPlayer)packed.Instantiate(); Check(instance.Stream is AudioStreamInteractive local && !ReferenceEquals(local, s) && local.ClipCount == 2 && instance.GetParameter(AudioStreamPlaybackInteractive.SwitchToClipParameter) == "B", "Typed scene-local resource and music parameter restore.");
        using var emitter = new AudioStreamEmitter { Stream = s }; emitter.SetParameter(AudioStreamPlaybackInteractive.SwitchToClipParameter, "A"); packed.Pack(emitter); using var emitterCopy = (AudioStreamEmitter)packed.Instantiate(); Check(emitterCopy.GetParameter(AudioStreamPlaybackInteractive.SwitchToClipParameter) == "A", "Spatial sibling preserves its typed clip parameter.");
        using var playback = s.InstantiatePlayback(); s.Dispose(); Reject<ObjectDisposedException>(() => playback.Start()); playback.Stop(); Check(!child.IsDisposed, "Disposal retains borrowed resources and permits cleanup."); deep.GetClipStream(0)!.Dispose();
        using var empty = new AudioStreamInteractive(); using var emptyCopy = empty.Duplicate();
    }
    private static void Edges()
    {
        using var a = new Probe(new(1, 0)); using var b = new Probe(new(0, 1)); using var s = Pair(a, b); using var p = (AudioStreamPlaybackInteractive)s.InstantiatePlayback(); using var second = (AudioStreamPlaybackInteractive)s.InstantiatePlayback(); var frames = new Vector2[8];
        s.AddTransition(-1, -1, From.Immediate, To.Start, Fade.Out, 0, true, 99); p.Start(); second.Start(); p.SwitchToClip(1); p.MixInto(frames, 1); Check(p.GetCurrentClipIndex() == 1 && second.GetCurrentClipIndex() == 0, "Independent playbacks and ignored invalid filler.");
        s.SetClipName(0, "same"); s.SetClipName(1, "same"); p.SwitchToClipByName("same"); p.MixInto(frames, 1); Check(p.GetCurrentClipIndex() == 0, "Duplicate names resolve to first active index.");
        s.EraseTransition(-1, -1); s.AddTransition(0, 1, From.Immediate, To.Start, Fade.Out, 0); s.AddTransition(1, 0, From.Immediate, To.Start, Fade.Out, 0); s.EraseTransition(0, 1); s.AddTransition(0, 1, From.Immediate, To.Start, Fade.Out, 0); Check(s.GetTransitionList().SequenceEqual(new[] { 1, 0, 0, 1 }), "Erase/reinsert appends rule order.");
        a.BPM = 120; a.Bars = 0; s.AddTransition(0, 1, From.NextBar, To.Start, Fade.Cross, 0); p.Start(); p.SwitchToClip(1); p.MixInto(frames, 1); Check(p.GetCurrentClipIndex() == 1 && frames.All(v => v == new Vector2(0, 1)), "Missing bar metadata and zero crossfade switch safely.");
        a.BPM = double.NaN; p.Start(); p.SwitchToClip(1); Reject<InvalidOperationException>(() => p.MixInto(frames, 1)); Check(!p.IsPlaying(), "Nonfinite tempo stops the graph safely."); a.BPM = double.Epsilon; p.Start(); p.SwitchToClip(1); Reject<InvalidOperationException>(() => p.MixInto(frames, 1)); a.BPM = 0;
        s.SetClipStream(1, null); p.Start(); p.SwitchToClip(1); p.MixInto(frames, 1); Check(p.GetCurrentClipIndex() == 0 && p.IsPlaying(), "Null destination is ignored."); s.SetClipStream(1, b); Check(p.MixInto(frames, 1) == 0 && !p.IsPlaying(), "Null-to-stream edit also invalidates cached playback."); p.Start(); p.SwitchToClip(1); p.MixInto(frames, 1); Check(p.GetCurrentClipIndex() == 1, "Owner restart refreshes the newly supplied clip.");
        s.ClipCount = 3; s.SetClipStream(2, a); Check(p.MixInto(frames, 1) == 0, "Count growth invalidates prepared active prefix."); p.Start(); p.SwitchToClip(2); p.MixInto(frames, 1); Check(p.GetCurrentClipIndex() == 2, "Grown prefix is executable after owner refresh.");
        using var maximum = new AudioStreamInteractive { ClipCount = 63 }; for (var i = 0; i < 63; i++) maximum.SetClipStream(i, a); maximum.AddTransition(-1, -1, From.Immediate, To.Start, Fade.Out, 0); using var mp = (AudioStreamPlaybackInteractive)maximum.InstantiatePlayback(); mp.Start(); mp.SwitchToClip(62); mp.MixInto(frames, 1); Check(mp.GetCurrentClipIndex() == 62, "Maximum capacity prepares and switches every legal slot.");
    }
    private static void Warm()
    {
        using var a = new Probe(new(.1f, .2f)); using var b = new Probe(new(.2f, .1f)); using var s = Pair(a, b); s.AddTransition(-1, -1, From.Immediate, To.Start, Fade.Out, 0); using var p = (AudioStreamPlaybackInteractive)s.InstantiatePlayback(); p.Start(); var buffer = new Vector2[1025];
        void Cycle(int i) { p.SwitchToClip(i % 2); p.MixInto(buffer, 1); p.GetCurrentClipIndex(); p.GetPlaybackPosition(); }
        for (var i = 0; i < 20; i++) Cycle(i); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Cycle(i); Check(before == GC.GetAllocatedBytesForCurrentThread(), "64 warmed switch/mix/query cycles allocate zero bytes."); p.Stop();
        for (var i = 0; i < 20; i++) p.MixInto(buffer, 1); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) p.MixInto(buffer, 1); Check(before == GC.GetAllocatedBytesForCurrentThread(), "64 warmed inactive mixes allocate zero bytes.");
    }
    private static AudioStreamInteractive Pair(AudioStream a, AudioStream b) { var s = new AudioStreamInteractive { ClipCount = 2 }; s.SetClipStream(0, a); s.SetClipStream(1, b); s.SetClipName(0, "A"); s.SetClipName(1, "B"); return s; }
    private sealed class Probe(Vector2 sample) : AudioStream
    {
        internal double BPM, Length = 100; internal int Bars, Beats, Frames = int.MaxValue; internal bool FailFactory, FailStart, FailStop, FailDispose, Nonfinite, Wave; internal Action? Hook, FactoryHook; internal Playback? Last;
        protected override AudioStreamPlayback OnInstantiatePlayback() { if (FailFactory) throw new ApplicationException(); Last = new(this, sample); try { FactoryHook?.Invoke(); return Last; } catch { Last.Dispose(); throw; } }
        protected override double OnGetLength() => Length;
        protected override double OnGetBPM() => BPM;
        protected override int OnGetBarBeats() => Bars;
        protected override int OnGetBeatCount() => Beats;
        internal sealed class Playback(Probe source, Vector2 sample) : AudioStreamPlayback
        {
            internal bool Active; internal double Position, StartedAt; internal float LastRate; private int _remaining, _frames;
            protected override void OnStart(double time) { Position = StartedAt = time; _remaining = source.Frames; _frames = 0; Active = true; if (source.FailStart) throw new ApplicationException(); }
            protected override void OnStop() { Active = false; if (source.FailStop) throw new ApplicationException(); }
            protected override bool OnIsPlaying() => Active;
            protected override double OnGetPlaybackPosition() => Position;
            protected override void OnSeek(double time) => Position = time;
            protected override int OnMix(Span<Vector2> output, float rateScale) { source.Hook?.Invoke(); LastRate = rateScale; var count = Active ? Math.Min(output.Length, _remaining) : 0; for (var i = 0; i < count; i++) output[i] = source.Nonfinite ? new(float.NaN, 0) : source.Wave ? sample * MathF.Sin((_frames + i) * (2 * MathF.PI / 64)) : sample; output[count..].Fill(new(999, 999)); _frames += count; _remaining -= count; Position += count / Rate; if (_remaining == 0) Active = false; return count; }
            protected override void Dispose(bool disposing) { Active = false; base.Dispose(disposing); if (source.FailDispose) throw new ApplicationException(); }
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Near(Vector2 actual, Vector2 expected, string message) { if ((actual - expected).Length() > 2e-6f) throw new InvalidOperationException(message + $": {actual} != {expected}"); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Native()
    {
        using var a = new Probe(new(.2f, .2f)) { Wave = true }; using var b = new Probe(new(-.2f, -.2f)) { Wave = true }; using var s = Pair(a, b); s.AddTransition(-1, -1, From.Immediate, To.Start, Fade.Out, 0);
        var root = new Node(); var player = new AudioStreamPlayer { Stream = s }; root.AddChild(player); using var tree = new SceneTree(root); player.Play(); var native = AudioServer.Instance.Native;
        Wait(native, 20); native.PrepareCapture(native.QuantumFrames * native.Channels * 8); Wait(native, 10); Check(native.CapturedPCM().Any(v => Math.Abs(v) > .1f), "Interactive clip reaches actual FAudio PCM.");
        var handle = (AudioStreamPlaybackInteractive)player.GetStreamPlayback(); player.SetParameter(AudioStreamPlaybackInteractive.SwitchToClipParameter, "B"); Wait(native, 5); Check(handle.GetCurrentClipIndex() == 1 && player.GetParameter(AudioStreamPlaybackInteractive.SwitchToClipParameter) == "B", "Live typed player parameter reaches the native scheduler.");
        player.StreamPaused = true; var cursor = b.Last!.Position; Wait(native, 5); Check(b.Last.Position == cursor, "Paused child cursor remains fixed."); player.StreamPaused = false;
        for (var i = 0; i < 20; i++) { handle.SwitchToClip(i % 2); Wait(native, 1); }
        var before = native.MixManagedBytes; var calls = FAudioContext.AllocationCalls; for (var i = 0; i < 64; i++) { handle.SwitchToClip(i % 2); Wait(native, 1); }
        Check(native.MixManagedBytes == before && FAudioContext.AllocationCalls == calls, "64 warmed native scheduled switches allocate zero measured bytes/calls.");
        player.StreamPaused = true; Wait(native, 20); before = native.MixManagedBytes; calls = FAudioContext.AllocationCalls; Wait(native, 64); Check(native.MixManagedBytes == before && FAudioContext.AllocationCalls == calls, "64 warmed paused native passes allocate zero measured bytes/calls."); player.StreamPaused = false;
        s.SetClipStream(0, a); Wait(native, 5); tree.ProcessFrame(.01); Check(!player.IsPlaying(), "Structural version edit stops the attached native voice.");
        var settings = ProjectSettings.Instance; var enabled = settings.Get(ProjectSettings.AudioDriverEnableInput);
        try
        {
            settings.Set(ProjectSettings.AudioDriverEnableInput, true);
            using var microphone = new AudioStreamMicrophone(); using var random = new AudioStreamRandomizer(); random.AddStream(-1, microphone);
            using var sync = new AudioStreamSynchronized { StreamCount = 1 }; sync.SetSyncStream(0, random);
            using var nested = Pair(sync, a); using var monitored = Pair(a, nested); monitored.AddTransition(-1, -1, From.Immediate, To.Start, Fade.Out, 0);
            player.Stream = monitored; player.Play(); handle = (AudioStreamPlaybackInteractive)player.GetStreamPlayback(); Check(AudioServer.Instance.CurrentInput?.Active != true, "Preparing a nested microphone clip opens paused input without recording.");
            Task.Run(() => { Reject<InvalidOperationException>(() => handle.Start()); Reject<InvalidOperationException>(() => handle.Dispose()); }).GetAwaiter().GetResult(); Check(!handle.IsDisposed && handle.IsPlaying(), "Prepared nested microphone controls retain public owner checks.");
            handle.SwitchToClip(1); Wait(native, 8); tree.ProcessFrame(.01); Check(handle.GetCurrentClipIndex() == 1 && AudioServer.Instance.CurrentInput?.Active == true, "Native scheduler starts nested interactive/synchronized/randomized microphone recording safely.");
            handle.SwitchToClip(0); Wait(native, 8); tree.ProcessFrame(.01); Check(AudioServer.Instance.CurrentInput?.Active != true, "Outgoing scheduled stop releases the final nested microphone capture request.");
            for (var i = 0; i < 20; i++) { handle.SwitchToClip(i % 2); Wait(native, 2); }
            before = native.MixManagedBytes; calls = FAudioContext.AllocationCalls;
            for (var i = 0; i < 64; i++) { handle.SwitchToClip(i % 2); Wait(native, 2); }
            Check(native.MixManagedBytes == before && FAudioContext.AllocationCalls == calls, "64 warmed nested scheduled recording switches allocate zero measured bytes/calls.");
            handle.SwitchToClip(1); Wait(native, 5); monitored.SetClipStream(1, nested); Wait(native, 5); tree.ProcessFrame(.01); Check(!player.IsPlaying() && AudioServer.Instance.CurrentInput?.Active != true, "Version invalidation releases nested recording on the native thread.");
            settings.Set(ProjectSettings.AudioDriverEnableInput, false); player.Play(); handle = (AudioStreamPlaybackInteractive)player.GetStreamPlayback(); handle.SwitchToClip(1); Wait(native, 5); Reject<Exception>(() => tree.ProcessFrame(.01)); Check(!player.IsPlaying() && AudioServer.Instance.CurrentInput?.Active != true, "Disabled scheduled input fails through native containment without a retained request.");
        }
        finally { player.Stop(); settings.Set(ProjectSettings.AudioDriverEnableInput, enabled); }
    }
    private static void Wait(FAudioContext context, long count) { var end = context.MixPasses + count; var watch = System.Diagnostics.Stopwatch.StartNew(); while (context.MixPasses < end) { if (watch.Elapsed > TimeSpan.FromSeconds(5)) throw new InvalidOperationException("Native audio did not advance."); Thread.Sleep(1); } }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu"; var settings = ProjectSettings.Instance; var old = settings.Get(ProjectSettings.RenderingMethod); var fps = Engine.Instance.MaxFPS;
        settings.Set(ProjectSettings.RenderingMethod, backend); Engine.Instance.MaxFPS = 60;
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var a = HostTone(440); using var b = HostTone(660); using var s = Pair(a, b); s.AddTransition(-1, -1, From.Immediate, To.Start, Fade.Cross, .05f);
                var window = new Window { Title = "Electron2D interactive audio", Size = new(240, 120) }; var player = new AudioStreamPlayer { Stream = s, Autoplay = true, VolumeDB = -24 }; player.SetParameter(AudioStreamPlaybackInteractive.SwitchToClipParameter, "B"); var scenario = new HostScenario(player); window.AddChild(player); window.AddChild(scenario);
                if (Engine.Instance.Run(window) != 0 || !scenario.Completed || !window.IsDisposed || s.IsDisposed || a.IsDisposed || b.IsDisposed) throw new InvalidOperationException("Interactive public host lifecycle failed.");
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "audio-interactive-host", backend, run, scenario.Selected, scenario.Paused, scenario.Restarted, cleaned = window.IsDisposed }));
            }
        }
        finally { Engine.Instance.MaxFPS = fps; settings.Set(ProjectSettings.RenderingMethod, old); }
    }
    private static AudioStreamWAV HostTone(double hz)
    {
        var data = new byte[48000 * 2]; for (var i = 0; i < 48000; i++) System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), (short)(Math.Sin(i * 2 * Math.PI * hz / 48000) * 8000));
        return new AudioStreamWAV { Data = data, SampleFormat = AudioStreamWAV.Format.PCM16, MixRate = 48000, Loop = AudioStreamWAV.LoopMode.Forward, LoopEnd = 47999 };
    }
    private sealed class HostScenario(AudioStreamPlayer player) : Node
    {
        private double _elapsed, _phaseAt; private int _phase; private AudioStreamPlaybackInteractive? _old; internal bool Completed, Selected, Paused, Restarted;
        protected override void OnReady() { ProcessEnabled = true; if (!player.IsPlaying() || player.GetStreamPlayback() is not AudioStreamPlaybackInteractive) throw new InvalidOperationException("Interactive autoplay."); }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta; var p = (AudioStreamPlaybackInteractive)player.GetStreamPlayback();
            if (_phase == 0 && _elapsed > .15) { Selected = p.GetCurrentClipIndex() == 1; if (!Selected) throw new InvalidOperationException("Restored typed clip parameter."); player.StreamPaused = true; _phaseAt = _elapsed; _phase = 1; }
            else if (_phase == 1 && _elapsed - _phaseAt > .1) { Paused = player.StreamPaused && p.GetCurrentClipIndex() == 1; player.SetParameter(AudioStreamPlaybackInteractive.SwitchToClipParameter, "A"); player.StreamPaused = false; _phaseAt = _elapsed; _phase = 2; }
            else if (_phase == 2 && _elapsed - _phaseAt > .15) { if (p.GetCurrentClipIndex() != 0) throw new InvalidOperationException("Live public parameter did not switch."); _old = p; player.Stop(); player.Play(); _phaseAt = _elapsed; _phase = 3; }
            else if (_phase == 3 && _elapsed - _phaseAt > .15) { Restarted = _old!.IsDisposed && ((AudioStreamPlaybackInteractive)player.GetStreamPlayback()).GetCurrentClipIndex() == 0; if (!Restarted || !Paused) throw new InvalidOperationException("Public restart did not replace prepared ownership."); Completed = true; Tree!.Quit(); }
            if (_elapsed > 5) throw new InvalidOperationException("Interactive host did not finish.");
        }
    }
}
