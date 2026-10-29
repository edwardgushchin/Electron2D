using System.Globalization;
using Electron2D;

internal static class AnimationEndpointTests
{
    private static readonly double[] KeyTimes = [0, .000001, .249999, .25, .250001, .5, .75, .999999, 1, 1.000001, 1.25];
    private static int _checks;
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }

    internal static void Run()
    {
        var vectors = File.ReadAllLines(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures/AnimationEndpoints/reference.tsv"));
        using var setup = new Setup();
        setup.Set(1, SpriteFrames.LoopMode.Linear, Animation.LoopedFlag.End, .25, .25);
        setup.Advance();
        Check(setup.Receiver.Keys.SequenceEqual([0, 2, 3, 4]), "A custom loop flag must include the crossed linear endpoint and its ordered range.");
        var vector = 0;
        foreach (var line in vectors)
        {
            var fields = line.Split('\t');
            var length = double.Parse(fields[0], CultureInfo.InvariantCulture);
            var loop = (SpriteFrames.LoopMode)int.Parse(fields[1], CultureInfo.InvariantCulture);
            var flag = (Animation.LoopedFlag)int.Parse(fields[2], CultureInfo.InvariantCulture);
            var time = double.Parse(fields[3], CultureInfo.InvariantCulture);
            var delta = double.Parse(fields[4], CultureInfo.InvariantCulture);
            var expected = fields[5] == "-" ? [] : fields[5].Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            setup.Set(length, loop, flag, time, delta); setup.Advance();
            Check(setup.Receiver.Keys.SequenceEqual(expected), $"Method endpoint vector {vector}: {line}; actual {string.Join(',', setup.Receiver.Keys)}.");
            Check(setup.Child.AssignedAnimation == (expected.Length == 0 ? "sentinel" : "k" + expected[^1]), $"Nested endpoint vector {vector}: {line}; actual {setup.Child.AssignedAnimation}.");
            vector++;
        }
        Check(vector == 3564, "Every independent endpoint vector executes through a public custom graph.");
        Policies(setup);
        using var disposed = new Setup(); disposed.Set(1, SpriteFrames.LoopMode.Linear, Animation.LoopedFlag.End, .25, .25);
        disposed.Receiver.Hook = () => { disposed.Receiver.Hook = null; disposed.Clip.Dispose(); };
        disposed.Advance(); Check(disposed.Receiver.Keys.SequenceEqual([0]), "Disposing the source in a callback abandons later event ranges.");
        Console.WriteLine($"Animation endpoints PASS {_checks}; {vector} independent vectors for method and nested tracks.");
    }

    private static void Policies(Setup setup)
    {
        setup.Set(1, SpriteFrames.LoopMode.Linear, Animation.LoopedFlag.End, .25, .25);
        setup.Cue.Seek = true; setup.Advance();
        Check(setup.Receiver.Keys.SequenceEqual([3]), "Internal seek selects one exact key rather than traversing a reported boundary.");
        setup.Set(1, SpriteFrames.LoopMode.PingPong, Animation.LoopedFlag.End, .3, .1);
        setup.Cue.Seek = setup.Cue.External = true; setup.Advance();
        Check(setup.Receiver.Keys.SequenceEqual([4]), "External seek selects the nearest preceding key, ignoring endpoint traversal.");
        setup.Set(1, SpriteFrames.LoopMode.Linear, Animation.LoopedFlag.End, .25, .25); setup.Cue.Weight = 0; setup.Advance();
        Check(setup.Receiver.Keys.Count == 0 && setup.Child.AssignedAnimation == "sentinel", "Zero-weight frames have no event side effects.");
        setup.Set(1, SpriteFrames.LoopMode.Linear, Animation.LoopedFlag.End, .25, .25);
        setup.Tree.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Deferred; setup.Tree.PrepareMethodCallbacks(64);
        setup.Advance(); Check(setup.Receiver.Keys.Count == 0, "Custom endpoint calls retain the deferred safe point.");
        setup.Scene.ProcessFrame(0); Check(setup.Receiver.Keys.SequenceEqual([0, 2, 3, 4]), "Deferred calls retain traversal order.");
        setup.Tree.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate;
        setup.Set(1, SpriteFrames.LoopMode.Linear, Animation.LoopedFlag.End, .25, .25);
        setup.Receiver.Hook = () => { setup.Receiver.Hook = null; setup.Clip.TrackSetEnabled(setup.Method, false); };
        setup.Advance(); Check(setup.Receiver.Keys.SequenceEqual([0]), "A callback resource edit abandons the remaining stale ranges.");
        setup.Clip.TrackSetEnabled(setup.Method, true);
        setup.Set(1, SpriteFrames.LoopMode.Linear, Animation.LoopedFlag.End, .25, .25);
        setup.Receiver.Hook = () => throw new ApplicationException("endpoint callback");
        Reject<ApplicationException>(setup.Advance); setup.Receiver.Hook = null;
        setup.Set(1, SpriteFrames.LoopMode.Linear, Animation.LoopedFlag.End, .25, .25); setup.Advance();
        Check(setup.Receiver.Keys.SequenceEqual([0, 2, 3, 4]), "A failed callback does not leave the graph processing context attached.");
        setup.Cue.Flag = (Animation.LoopedFlag)123; Reject<ArgumentOutOfRangeException>(setup.Advance);
        setup.Cue.Flag = Animation.LoopedFlag.None; setup.Cue.Time = double.MaxValue; setup.Cue.Delta = -double.MaxValue;
        Reject<ArgumentOutOfRangeException>(setup.Advance);
        setup.TestOnly();
        setup.Set(1, SpriteFrames.LoopMode.PingPong, Animation.LoopedFlag.Start, .25, .5);
        for (var i = 0; i < 100; i++) { setup.Receiver.Keys.Clear(); setup.Tree.Advance(0); }
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) { setup.Receiver.Keys.Clear(); setup.Tree.Advance(0); }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared custom endpoint evaluation allocates zero caller-thread managed bytes.");
    }

    internal static void Sparse()
    {
        using var setup = new Setup(); setup.Set(1, SpriteFrames.LoopMode.Linear, Animation.LoopedFlag.None, .500025, .000001);
        setup.Clip.TrackSetEnabled(setup.Method, false);
        for (var i = setup.Clip.TrackGetKeyCount(1) - 1; i >= 0; i--) setup.Clip.TrackRemoveKey(1, i);
        for (var i = 0; i < 20000; i++) setup.Clip.AnimationTrackInsertKey(1, i / 20000d, "sentinel");
        for (var i = 0; i < 100; i++) setup.Tree.Advance(0);
        var allocated = GC.GetAllocatedBytesForCurrentThread(); var start = System.Diagnostics.Stopwatch.GetTimestamp();
        for (var i = 0; i < 4000; i++) setup.Tree.Advance(0);
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start); var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Check(setup.Child.AssignedAnimation == "sentinel" && !setup.Child.IsPlaying(), "A sparse custom interval does not cross a key.");
        Check(bytes == 0, "Sparse custom evaluation has no warmed caller-thread allocation.");
        Console.WriteLine(FormattableString.Invariant($"Sparse custom endpoints: keys=20000 frames=4000 milliseconds={elapsed.TotalMilliseconds:F3} allocated={bytes}"));
    }

    internal static void RunAudio(SpriteFrames.LoopMode loop)
    {
        var bytes = new byte[8820];
        for (var i = 0; i < bytes.Length; i += 2) System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i, 2), 8000);
        using var wave = new AudioStreamWAV { Data = bytes, SampleFormat = AudioStreamWAV.Format.PCM16, Loop = AudioLoopMode.Forward, LoopEnd = 4409 };
        using var clip = new Animation { LoopMode = loop }; var track = clip.AddAudioTrack(); clip.TrackSetPath(track, "speaker"); clip.AudioTrackInsertKey(track, 1, wave);
        using var library = new AnimationLibrary(); library.AddAnimation("clip", clip);
        using var cue = new Cue { Time = .75, Delta = loop == SpriteFrames.LoopMode.Linear ? -.25 : .25 };
        using var graph = new AnimationNodeBlendTree(); graph.AddNode("cue", cue); graph.ConnectNode("output", 0, "cue");
        var root = new Node(); var speaker = new AudioStreamPlayer { Name = "speaker" };
        var controller = new AnimationTree { TreeRoot = graph, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual };
        controller.AddAnimationLibrary("", library); root.AddChild(speaker); root.AddChild(controller);
        using var scene = new SceneTree(root); Engine.Start(scene);
        using var capture = new AudioEffectCapture { BufferLength = .2f };
        var effect = AudioServer.GetBusEffectCount(0); AudioServer.AddBusEffect(0, capture);
        try
        {
            controller.Advance(0); Check(!speaker.IsPlaying(), "No audio key is crossed without the reported endpoint.");
            cue.Flag = loop == SpriteFrames.LoopMode.Linear ? Animation.LoopedFlag.Start : Animation.LoopedFlag.End;
            if (loop == SpriteFrames.LoopMode.None)
            {
                controller.Advance(0); Check(!speaker.IsPlaying(), "Non-looping clips ignore a reported loop endpoint.");
                cue.Time = 1;
            }
            controller.Advance(0); Check(speaker.GetStreamPlayback() is AudioStreamPlaybackPolyphonic, "An endpoint cue reaches the prepared native audio receiver.");
            var heard = false; var started = System.Diagnostics.Stopwatch.GetTimestamp();
            while (System.Diagnostics.Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(5))
            {
                var available = capture.GetFramesAvailable();
                if (available > 0 && capture.GetBuffer(available).Any(f => Math.Abs(f.X) > .01f || Math.Abs(f.Y) > .01f)) { heard = true; break; }
                Thread.Sleep(2);
            }
            Check(heard, "The custom endpoint selected cue produces PCM through the native callback and capture effect.");
            controller.ClearCaches();
        }
        finally { AudioServer.RemoveBusEffect(0, effect); Engine.Stop(); }
        Console.WriteLine($"Animation endpoint native audio {loop} PASS {_checks}");
    }

    private sealed class TestingNode : AnimationNodeExtension
    {
        internal TestingNode() { AddInput("source"); }
        protected override AnimationNodeTimeInfo OnProcessAnimationNode(in AnimationNodePlaybackInfo info, bool testOnly)
        { BlendInput(0, info.Delta, info.Seeked, info.IsExternalSeeking, 1, testOnly: true); return new(1, 0, 0); }
    }

    private sealed class Cue : AnimationNodeExtension
    {
        internal double Time, Delta, Weight = 1;
        internal bool Seek, External;
        internal Animation.LoopedFlag Flag;
        protected override AnimationNodeTimeInfo OnProcessAnimationNode(in AnimationNodePlaybackInfo info, bool testOnly)
        { BlendAnimation("clip", Time, Delta, Seek, External, Weight, Flag); return new(1, 0, 0); }
    }
    private sealed class Receiver : Node
    {
        internal readonly List<int> Keys = new(64);
        internal Action? Hook;
        internal void Receive(int key) { Keys.Add(key); Hook?.Invoke(); }
    }
    private sealed class Setup : IDisposable
    {
        internal readonly Animation Clip = new();
        internal readonly Receiver Receiver = new() { Name = "receiver" };
        internal readonly Cue Cue = new();
        internal readonly AnimationTree Tree;
        internal readonly AnimationPlayer Child = new() { Name = "child", CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual };
        internal readonly SceneTree Scene;
        internal readonly int Method;
        private readonly Node _root = new();
        private readonly AnimationNodeBlendTree _graph = new();
        private readonly AnimationLibrary _library = new(), _children = new();
        private readonly List<Animation> _childClips = [];
        private double _length;
        internal Setup()
        {
            Method = Clip.AddMethodTrack<Receiver>(); Clip.TrackSetPath(Method, "receiver");
            var nested = Clip.AddAnimationTrack(); Clip.TrackSetPath(nested, "child");
            for (var i = 0; i < KeyTimes.Length; i++)
            {
                Clip.TrackInsertKey(Method, KeyTimes[i], new AnimationMethodKey<Receiver, int>("Receive", static (r, value) => r.Receive(value), i));
                Clip.AnimationTrackInsertKey(nested, KeyTimes[i], "k" + i);
                var child = new Animation { Length = 20 }; _childClips.Add(child); _children.AddAnimation("k" + i, child);
            }
            var sentinel = new Animation { Length = 20 }; _childClips.Add(sentinel); _children.AddAnimation("sentinel", sentinel);
            _library.AddAnimation("clip", Clip); Child.AddAnimationLibrary("", _children);
            _graph.AddNode("cue", Cue); _graph.ConnectNode("output", 0, "cue");
            Tree = new() { Name = "tree", TreeRoot = _graph, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual, CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate };
            Tree.AddAnimationLibrary("", _library); _root.AddChild(Receiver); _root.AddChild(Child); _root.AddChild(Tree); Scene = new(_root);
        }
        internal void Set(double length, SpriteFrames.LoopMode loop, Animation.LoopedFlag flag, double time, double delta)
        {
            if (_length != length)
            {
                Clip.Length = length;
                for (var i = KeyTimes.Length - 1; i >= 0; i--) { Clip.TrackRemoveKey(Method, i); Clip.TrackRemoveKey(1, i); }
                for (var i = 0; i < KeyTimes.Length; i++)
                {
                    Clip.TrackInsertKey(Method, KeyTimes[i] * length, new AnimationMethodKey<Receiver, int>("Receive", static (r, value) => r.Receive(value), i));
                    Clip.AnimationTrackInsertKey(1, KeyTimes[i] * length, "k" + i);
                }
                _length = length;
            }
            Clip.LoopMode = loop; Cue.Flag = flag; Cue.Time = time; Cue.Delta = delta; Cue.Weight = 1; Cue.Seek = Cue.External = false;
            Receiver.Keys.Clear(); Child.Play("sentinel"); Child.Stop(true);
        }
        internal void TestOnly()
        {
            using var testing = new TestingNode(); _graph.AddNode("testing", testing);
            _graph.DisconnectNode("output", 0); _graph.ConnectNode("testing", 0, "cue"); _graph.ConnectNode("output", 0, "testing");
            Set(1, SpriteFrames.LoopMode.Linear, Animation.LoopedFlag.End, .25, .25); Advance();
            Check(Receiver.Keys.Count == 0 && Child.AssignedAnimation == "sentinel", "Predictive custom processing never delivers endpoint events.");
            _graph.DisconnectNode("output", 0); _graph.DisconnectNode("testing", 0); _graph.ConnectNode("output", 0, "cue"); _graph.RemoveNode("testing");
        }
        internal void Advance() => Tree.Advance(0);
        public void Dispose() { Scene.Dispose(); _graph.Dispose(); Cue.Dispose(); _library.Dispose(); _children.Dispose(); Clip.Dispose(); foreach (var clip in _childClips) clip.Dispose(); }
    }
}
