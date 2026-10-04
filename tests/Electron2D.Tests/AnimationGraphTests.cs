using Electron2D;

internal static class AnimationGraphTests
{
    private static readonly PropertyDescriptor<Probe, double> X = new("X", n => n.X, (n, v) => { n.X = v; n.Writes++; n.Hook?.Invoke(); });
    private static readonly PropertyDescriptor<Probe, double> Y = new("Y", n => n.Y, (n, v) => n.Y = v);
    internal static void Run() { Authoring(); Playback(); Arithmetic(); Timing(); Provider(); Custom(); Ownership(); Warm(); Console.WriteLine("Typed animation graph editing, per-tree parameters/clocks, filters, arithmetic, time controls, lifetime and warmed checks passed."); }
    private static void Authoring()
    {
        using var graph = new AnimationNodeBlendTree(); using var a = new AnimationNodeAnimation(); using var b = new AnimationNodeBlend2();
        Check(graph.GetNodeList().SequenceEqual(["output"]) && !a.AddInput("bad") && b.GetInputCount() == 2 && b.GetInputName(1) == "blend", "Reserved output, root input rejection and standard captions.");
        graph.AddNode("a", a, new(10, 20)); graph.AddNode("mix", b); graph.ConnectNode("output", 0, "mix"); graph.ConnectNode("mix", 0, "a"); Reject<ArgumentException>(() => graph.ConnectNode("mix", 1, "a")); Reject<ArgumentException>(() => graph.RemoveNode("output")); Reject<ArgumentException>(() => graph.AddNode("bad/name", a)); Reject<ArgumentException>(() => graph.AddNode("self", graph));
        using var scale = new AnimationNodeTimeScale(); graph.AddNode("scale", scale); Reject<ArgumentException>(() => graph.ConnectNode("scale", 0, "mix")); graph.DisconnectNode("output", 0); graph.ConnectNode("scale", 0, "mix"); Reject<InvalidOperationException>(() => graph.ConnectNode("mix", 1, "scale"));
        graph.RenameNode("a", "renamed"); Check(graph.GetNode("renamed") == a && graph.GetNodePosition("renamed") == new Vector2(10, 20), "Rename retains metadata and resource."); graph.RemoveNode("renamed"); Check(!a.IsDisposed, "Graph removal borrows resources.");
        Reject<InvalidOperationException>(() => b.GetParameter(AnimationNodeBlend2.BlendAmount)); Check(b.GetProcessingAnimationTreeInstanceID() == 0 && !b.IsProcessTesting(), "Processing helpers reject calls outside a tree.");
        using var deep = (AnimationNodeBlendTree)graph.Duplicate(true); Check(deep.GetNode("mix") is AnimationNodeBlend2 copyMix && copyMix != b && copyMix.GetInputCount() == 2, "Deep graph duplication recreates definitions and inputs."); foreach (var name in deep.GetNodeList()) if (name != "output") deep.GetNode(name).Dispose();
        using var clone = (AnimationNodeBlendTree)graph.Duplicate(); Check(clone.GetNode("mix") == b && clone.GetNode("output") != graph.GetNode("output"), "Copies share borrowed children and own separate output resources.");
    }
    private static void Playback()
    {
        using var graph = Basic(out var blend, out var a, out var b); using var scope = new Setup(graph, Clip(10), Clip(30)); using var other = new Setup(graph, Clip(100), Clip(300));
        Check(scope.Controller.Deterministic && scope.Controller.CallbackModeDiscrete == AnimationMixer.AnimationCallbackModeDiscrete.ForceContinuous, "Tree defaults.");
        scope.Controller.SetParameter("mix", AnimationNodeBlend2.BlendAmount, .25); other.Controller.SetParameter("mix", AnimationNodeBlend2.BlendAmount, .75);
        scope.Controller.Advance(0); other.Controller.Advance(0); Near(scope.Target.X, 15); Near(other.Target.X, 250); Near(scope.Controller.GetParameter("mix", AnimationNodeBlend2.BlendAmount), .25);
        scope.Controller.Advance(.5); Near(scope.Controller.GetParameter("a", AnimationNode.CurrentPosition), .5); Near(other.Controller.GetParameter("a", AnimationNode.CurrentPosition), 0);
        Reject<InvalidOperationException>(() => scope.Controller.SetParameter("a", AnimationNode.CurrentPosition, 9d)); Reject<ArgumentException>(() => scope.Controller.GetParameter("a", AnimationNodeBlend2.BlendAmount)); Reject<ArgumentOutOfRangeException>(() => scope.Controller.SetParameter("mix", AnimationNodeBlend2.BlendAmount, double.NaN));
        graph.RenameNode("mix", "blend"); scope.Controller.Advance(0); Near(scope.Controller.GetParameter("blend", AnimationNodeBlend2.BlendAmount), .25); Near(scope.Target.X, 15);
        graph.DisconnectNode("blend", 1); Reject<InvalidOperationException>(() => scope.Controller.Advance(.1)); graph.ConnectNode("blend", 1, "b"); scope.Controller.Advance(.1); Near(scope.Target.X, 15);
        a.Dispose(); Reject<ObjectDisposedException>(() => scope.Controller.Advance(.1));
        blend.Dispose(); b.Dispose();
    }
    private static void Arithmetic()
    {
        foreach (var kind in new[] { "Blend3", "Add2", "Add3", "Sub2" })
        {
            using var graph = new AnimationNodeBlendTree(); using AnimationNodeSync op = kind switch { "Blend3" => new AnimationNodeBlend3(), "Add2" => new AnimationNodeAdd2(), "Add3" => new AnimationNodeAdd3(), _ => new AnimationNodeSub2() }; graph.AddNode("op", op); graph.ConnectNode("output", 0, "op");
            var leaves = new List<AnimationNodeAnimation>(); for (var i = 0; i < op.GetInputCount(); i++) { var leaf = new AnimationNodeAnimation { Animation = ((char)('a' + i)).ToString() }; leaves.Add(leaf); var name = "n" + i; graph.AddNode(name, leaf); graph.ConnectNode("op", i, name); }
            using var scope = new Setup(graph, Clip(10), Clip(30), Clip(50));
            if (op is AnimationNodeBlend3) scope.Controller.SetParameter("op", AnimationNodeBlend3.BlendAmount, -.5);
            if (op is AnimationNodeAdd2) scope.Controller.SetParameter("op", AnimationNodeAdd2.AddAmount, .5);
            if (op is AnimationNodeAdd3) scope.Controller.SetParameter("op", AnimationNodeAdd3.AddAmount, .5);
            if (op is AnimationNodeSub2) scope.Controller.SetParameter("op", AnimationNodeSub2.SubAmount, .5);
            scope.Controller.Advance(0); Near(scope.Target.X, kind switch { "Blend3" => 20, "Add2" => 25, "Add3" => 55, _ => -5 });
            foreach (var leaf in leaves) leaf.Dispose();
        }
        using var filtered = Basic(out var mix, out var fa, out var fb); using var a = Clip(10); using var b = Clip(30); Add(b, Y, 80); Add(a, Y, 20); using var filteredScope = new Setup(filtered, a, b, borrow: true);
        mix.FilterEnabled = true; mix.SetFilterPath("target:X", true); filteredScope.Controller.SetParameter("mix", AnimationNodeBlend2.BlendAmount, .5); filteredScope.Controller.Advance(0); Near(filteredScope.Target.X, 20); Near(filteredScope.Target.Y, 20);
        fa.Dispose(); fb.Dispose(); mix.Dispose();
    }
    private static void Timing()
    {
        using var graph = Basic(out var mix, out var a, out var b); using var moving = Clip(0); moving.TrackInsertKey(0, 4, 40d); using var scope = new Setup(graph, moving, Clip(30));
        scope.Controller.SetParameter("mix", AnimationNodeBlend2.BlendAmount, 0d); scope.Controller.Advance(.5); Near(scope.Target.X, 0); scope.Controller.Advance(.5); Near(scope.Target.X, 5); Near(scope.Controller.GetParameter("b", AnimationNode.CurrentPosition), 0);
        mix.Sync = true; scope.Controller.Advance(.5); Near(scope.Controller.GetParameter("b", AnimationNode.CurrentPosition), .5);
        using var scale = new AnimationNodeTimeScale(); graph.AddNode("scale", scale); graph.DisconnectNode("output", 0); graph.ConnectNode("scale", 0, "mix"); graph.ConnectNode("output", 0, "scale"); scope.Controller.SetParameter("scale", AnimationNodeTimeScale.Scale, 2d); scope.Controller.Advance(.5); Near(scope.Target.X, 20);
        using var seek = new AnimationNodeTimeSeek(); graph.AddNode("seek", seek); graph.DisconnectNode("scale", 0); graph.ConnectNode("seek", 0, "mix"); graph.ConnectNode("scale", 0, "seek"); scope.Controller.SetParameter("seek", AnimationNodeTimeSeek.SeekRequest, 3d); scope.Controller.Advance(.1); Near(scope.Target.X, 30); Near(scope.Controller.GetParameter("seek", AnimationNodeTimeSeek.SeekRequest), -1);
        a.UseCustomTimeline = true; a.TimelineLength = 2; a.StartOffset = 0; a.StretchTimeScale = true; scope.Controller.SetParameter("seek", AnimationNodeTimeSeek.SeekRequest, 1d); scope.Controller.Advance(0); Near(scope.Target.X, 20);
        a.PlayMode = AnimationPlayMode.Backward; scope.Controller.SetParameter("seek", AnimationNodeTimeSeek.SeekRequest, .5); scope.Controller.Advance(0); Near(scope.Target.X, 30);
        a.PlayMode = AnimationPlayMode.Forward; a.UseCustomTimeline = false; moving.LoopMode = SpriteFrames.LoopMode.Linear; scope.Controller.SetParameter("seek", AnimationNodeTimeSeek.SeekRequest, 4.5); scope.Controller.Advance(0); Near(scope.Target.X, 5);
        moving.LoopMode = SpriteFrames.LoopMode.PingPong; scope.Controller.SetParameter("seek", AnimationNodeTimeSeek.SeekRequest, 4.5); scope.Controller.Advance(0); Near(scope.Target.X, 35); scope.Controller.Advance(.25); Near(scope.Target.X, 30);
        a.AdvanceOnStart = true; scope.Controller.TreeRoot = null; scope.Controller.TreeRoot = graph; scope.Controller.SetParameter("seek", AnimationNodeTimeSeek.SeekRequest, -1d); scope.Controller.Advance(.25); Near(scope.Target.X, 2.5);
        a.Dispose(); b.Dispose(); mix.Dispose();
    }
    private static void Provider()
    {
        using var clip = Clip(10); using var replacementClip = Clip(80); using var library = new AnimationLibrary(); library.AddAnimation("a", clip); using var replacementLibrary = new AnimationLibrary(); replacementLibrary.AddAnimation("a", replacementClip); using var leaf = new AnimationNodeAnimation { Animation = "a" };
        var root = new Node(); var target = new Probe { Name = "target" }; var provider = new AnimationPlayer { Name = "provider" }; provider.AddAnimationLibrary("", library); var controller = new AnimationTree { TreeRoot = leaf, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; root.AddChild(target); root.AddChild(provider); root.AddChild(controller); using var scene = new SceneTree(root); controller.AnimPlayer = "../provider"; controller.Advance(0); Near(target.X, 10); Check(!provider.Active, "Graph disables automatic provider playback.");
        root.RemoveChild(provider); provider.Dispose(); var next = new AnimationPlayer { Name = "provider" }; next.AddAnimationLibrary("", replacementLibrary); root.AddChild(next); controller.Advance(0); Near(target.X, 80); controller.AnimPlayer = ""; Check(controller.GetAnimationList().Length == 0 && controller.RootNode == "..", "Detaching provider clears copied library membership."); controller.AddAnimationLibrary("", library); controller.Advance(0); Near(target.X, 10);
    }
    private static void Custom()
    {
        using var node = new CustomNode(); using var scope = new Setup(node, Clip(10), Clip(30)); scope.Controller.Advance(0); Near(scope.Target.X, 10); scope.Controller.Advance(.5); Near(scope.Controller.GetParameter("", CustomNode.Memory), 2); Check(node.LastTree == scope.Controller.InstanceID, "Custom process helper context.");
        using var graph = Basic(out var mix, out var a, out var b); Action<ulong, string, string> failRename = (_, _, _) => throw new InvalidOperationException("rename fixture"); graph.AnimationNodeRenamed += failRename; using var editing = new Setup(graph, Clip(10), Clip(30)); editing.Controller.SetParameter("mix", AnimationNodeBlend2.BlendAmount, .5); editing.Controller.Advance(0); Reject<AggregateException>(() => graph.RenameNode("mix", "renamed")); Near(editing.Controller.GetParameter("renamed", AnimationNodeBlend2.BlendAmount), .5); graph.AnimationNodeRenamed -= failRename; graph.RenameNode("renamed", "mix");
        Action failed = () => throw new InvalidOperationException("fixture"); mix.TreeChanged += failed; Reject<AggregateException>(() => mix.FilterEnabled = true); mix.TreeChanged -= failed; mix.SetFilterPath("target:X", true); editing.Controller.Advance(0); Near(editing.Target.X, 20);
        mix.RemoveInput(0); mix.AddInput("replacement"); graph.ConnectNode("mix", 1, "a"); editing.Controller.Advance(0); Near(editing.Target.X, 20); a.Dispose(); b.Dispose(); mix.Dispose();
    }
    private sealed class CustomNode : AnimationRootNode
    {
        internal static readonly AnimationParameter<double> Memory = new("memory", 0);
        internal ulong LastTree;
        protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Append(Memory);
        protected override double OnProcess(double time, bool seek, bool external, bool testOnly) { LastTree = GetProcessingAnimationTreeInstanceID(); SetParameter(Memory, GetParameter(Memory) + 1); BlendAnimation("a", time, time, seek, external, 1); return 4; }
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_RENDERER") ?? "gpu"; var settings = ProjectSettings.Service; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var descriptor = new PropertyDescriptor<Entity, Vector2>(nameof(Entity.Position), n => n.Position, (n, v) => n.Position = v);
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var a = new Animation { Length = 4 }; var ta = a.AddTrack(descriptor); a.TrackSetPath(ta, "box:Position"); a.TrackInsertKey(ta, 0, new Vector2(16, 16)); using var b = new Animation { Length = 4 }; var tb = b.AddTrack(descriptor); b.TrackSetPath(tb, "box:Position"); b.TrackInsertKey(tb, 0, new Vector2(64, 16));
                using var graph = Basic(out var mix, out var first, out var second); using var library = new AnimationLibrary(); library.AddAnimation("a", a); library.AddAnimation("b", b); var window = new Window { Size = new(96, 64), Title = "Electron2D animation graph" }; var box = new Box { Name = "box" }; var tree = new AnimationTree { TreeRoot = graph, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; tree.AddAnimationLibrary("", library); window.AddChild(box); window.AddChild(tree); var stage = 0;
                box.Start = () => { var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black); tree.Advance(0); RenderingServer.FramePostDraw += () => { using var pixels = renderer.Readback(); var expected = 16 + stage * 12; Check(pixels.GetPixel(expected, 16).R > .9f && pixels.GetPixel(expected, 16).G < .1f, "Graph contribution reaches real rendered pixels."); if (stage > 0) Check(pixels.GetPixel(16, 16).R < .1f, "Old graph pose clears."); stage++; if (stage == 5) window.Tree!.Quit(); else { tree.SetParameter("mix", AnimationNodeBlend2.BlendAmount, stage * .25); tree.Advance(.1); } }; };
                Check(Engine.Run(window) == 0 && stage == 5 && window.IsDisposed && !graph.IsDisposed && !library.IsDisposed, "Graph host cleanup and borrowed resources."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "animation-graph-host", backend, run, stages = stage, cleaned = window.IsDisposed })); first.Dispose(); second.Dispose(); mix.Dispose();
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private sealed class Box : Entity { internal Action? Start; protected override void OnReady() => Start?.Invoke(); protected override void OnDraw() => DrawRect(new(-3, -3, 6, 6), Colors.Red); }
    private static void Ownership()
    {
        using var graph = Basic(out var mix, out var a, out var b); using var scope = new Setup(graph, Clip(10), Clip(30)); scope.Controller.Advance(0);
        scope.Target.Hook = () => scope.Controller.TreeRoot = null; scope.Controller.Advance(.1); Check(scope.Controller.TreeRoot is null, "Reentrant root replacement abandons stale evaluation."); scope.Target.Hook = null; scope.Controller.TreeRoot = graph; scope.Controller.Advance(.1);
        Reject<InvalidOperationException>(() => Task.Run(() => scope.Controller.SetParameter("mix", AnimationNodeBlend2.BlendAmount, .5)).GetAwaiter().GetResult());
        scope.Controller.Dispose(); Check(!graph.IsDisposed && !a.IsDisposed && !b.IsDisposed && !scope.Library.IsDisposed, "Tree borrows graph and library resources."); a.Dispose(); b.Dispose(); mix.Dispose();
    }
    private static void Warm()
    {
        using var graph = Basic(out var mix, out var a, out var b); using var scope = new Setup(graph, Clip(10), Clip(30)); scope.Controller.SetParameter("mix", AnimationNodeBlend2.BlendAmount, .5); for (var i = 0; i < 20; i++) scope.Tree.ProcessFrame(.01);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) scope.Tree.ProcessFrame(.01); Check(GC.GetAllocatedBytesForCurrentThread() == before, "256 warmed graph/property-mixer passes allocate zero bytes."); a.Dispose(); b.Dispose(); mix.Dispose();
    }
    private static AnimationNodeBlendTree Basic(out AnimationNodeBlend2 mix, out AnimationNodeAnimation a, out AnimationNodeAnimation b)
    { var graph = new AnimationNodeBlendTree(); a = new() { Animation = "a" }; b = new() { Animation = "b" }; mix = new(); graph.AddNode("a", a); graph.AddNode("b", b); graph.AddNode("mix", mix); graph.ConnectNode("mix", 0, "a"); graph.ConnectNode("mix", 1, "b"); graph.ConnectNode("output", 0, "mix"); return graph; }
    private static Animation Clip(double value) { var clip = new Animation { Length = 4 }; Add(clip, X, value); return clip; }
    private static void Add(Animation clip, PropertyDescriptor<Probe, double> property, double value) { var track = clip.AddTrack(property); clip.TrackSetPath(track, "target:" + property.Name); clip.TrackInsertKey(track, 0, value); }
    private sealed class Setup : IDisposable
    {
        internal readonly Probe Target = new() { Name = "target" }; internal readonly AnimationTree Controller = new(); internal readonly AnimationLibrary Library = new(); internal readonly SceneTree Tree; private readonly Animation[] _clips; private readonly bool _borrow;
        internal Setup(AnimationRootNode root, Animation a, Animation b, Animation? c = null, bool borrow = false) { _clips = c is null ? [a, b] : [a, b, c]; _borrow = borrow; for (var i = 0; i < _clips.Length; i++) Library.AddAnimation(((char)('a' + i)).ToString(), _clips[i]); var node = new Node(); node.AddChild(Target); node.AddChild(Controller); Tree = new(node); Controller.AddAnimationLibrary("", Library); Controller.TreeRoot = root; }
        public void Dispose() { Tree.Dispose(); Library.Dispose(); if (!_borrow) foreach (var clip in _clips) clip.Dispose(); }
    }
    private sealed class Probe : Node { internal double X, Y; internal int Writes; internal Action? Hook; }
    private static void Near(double a, double b) => Check(Math.Abs(a - b) < 1e-6, $"Expected {b}, got {a}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
