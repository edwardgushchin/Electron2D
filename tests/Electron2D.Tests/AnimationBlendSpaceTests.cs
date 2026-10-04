using Electron2D;

internal static class AnimationBlendSpaceTests
{
    private static readonly PropertyDescriptor<Probe, double> Value = new("Value", n => n.Value, (n, v) => n.Value = v);
    internal static void Run() { Authoring(); Linear(); Planar(); Clocks(); PingPongCarry(); Ownership(); Warm(); Console.WriteLine("Blend spaces: authoring/triangles, interpolation/projection, discrete/carry, cyclic clocks, shared resources, failure/reentry/copy and zero warmed managed allocations passed."); }
    private static void Authoring()
    {
        using var a = new AnimationNodeAnimation { Animation = "a" }; using var b = new AnimationNodeAnimation { Animation = "b" }; using var axis = new AnimationNodeBlendSpace1D(); axis.AddBlendPoint(a, 0); axis.AddBlendPoint(b, 1); axis.AddBlendPoint(a, -1, 1); Check(axis.GetBlendPointName(1) == "2" && axis.GetBlendPointName(2) == "1", "Safe inserted numeric names."); axis.SetBlendPointName(1, "1"); Check(axis.GetBlendPointName(1) == "1 2" && axis.FindBlendPointByName("missing") == -1, "Unique conflict names."); axis.ReorderBlendPoint(0, 2); Check(axis.GetBlendPointNode(0) == b, "Reorder swaps whole entries."); Reject<ArgumentException>(() => axis.AddBlendPoint(a, 0, name: "bad/name")); Reject<ArgumentException>(() => axis.AddBlendPoint(axis, 0)); Reject<ArgumentOutOfRangeException>(() => axis.SetBlendPointPosition(0, float.NaN)); axis.MinSpace = 2; Near(axis.MinSpace, 0); axis.MaxSpace = -2; Near(axis.MaxSpace, 1); axis.SyncMode = AnimationSyncMode.CyclicMutable; Check(axis.Sync, "Deprecated Sync getter reflects cyclic mode."); axis.Sync = false; Check(axis.SyncMode == AnimationSyncMode.None, "Sync setter projects policy.");
        using var plane = new AnimationNodeBlendSpace2D { AutoTriangles = false }; plane.AddBlendPoint(a, new(0, 0), name: "a"); plane.AddBlendPoint(b, new(1, 0), name: "b"); plane.AddBlendPoint(a, new(0, 1), name: "c"); plane.AddTriangle(2, 0, 1); Check(plane.GetTriangleCount() == 1 && plane.GetTrianglePoint(0, 0) == 0, "Canonical manual triangle."); Reject<ArgumentException>(() => plane.AddTriangle(1, 2, 0)); Reject<ArgumentException>(() => plane.AddTriangle(0, 0, 1)); plane.AddBlendPoint(a, new(-1, -1), 1, "inserted"); Check(plane.GetTrianglePoint(0, 1) == 2 && plane.GetTrianglePoint(0, 2) == 3, "Insertion shifts triangle indices."); plane.ReorderBlendPoint(0, 1); Check(plane.GetTrianglePoint(0, 0) == 1, "Swap remaps triangle vertices."); plane.RemoveBlendPoint(0); Check(plane.GetTrianglePoint(0, 0) == 0, "Nonmember removal shifts vertices."); plane.RemoveBlendPoint(1); Check(plane.GetTriangleCount() == 0 && !b.IsDisposed, "Member removal deletes triangle and borrows resource.");
        using var capacity = new AnimationNodeBlendSpace1D(); for (var i = 0; i < 64; i++) capacity.AddBlendPoint(a, i); Reject<InvalidOperationException>(() => capacity.AddBlendPoint(a, 65));
    }
    private static void Linear()
    {
        using var a = Leaf("a"); using var b = Leaf("b"); using var c = Leaf("c"); using var axis = new AnimationNodeBlendSpace1D(); axis.AddBlendPoint(a, -1, name: "a"); axis.AddBlendPoint(b, 0, name: "b"); axis.AddBlendPoint(c, 1, name: "c"); using var setup = new Setup(axis, Clip(10), Clip(30), Clip(70));
        foreach (var (pos, value) in new[] { (-2f, 10d), (-.5f, 20d), (0f, 30d), (.25f, 40d), (2f, 70d) }) { setup.Tree.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, pos); setup.Tree.Advance(0); Near(setup.Target.Value, value); }
        axis.BlendMode = AnimationBlendMode.Discrete; setup.Tree.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, .5f); setup.Tree.Advance(0); Near(setup.Target.Value, 30); axis.SetBlendPointPosition(2, 0); setup.Tree.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, 0f); axis.BlendMode = AnimationBlendMode.Interpolated; setup.Tree.Advance(0); Near(setup.Target.Value, 30);
        axis.ReorderBlendPoint(0, 1); setup.Tree.Advance(0); Near(setup.Target.Value, 30); axis.SetBlendPointName(0, "renamed"); Near(setup.Tree.GetParameter("renamed", AnimationNode.CurrentPosition), 0);
        using var empty = new AnimationNodeBlendSpace1D(); setup.Tree.TreeRoot = empty; Reject<InvalidOperationException>(() => setup.Tree.Advance(0));
    }
    private static void Planar()
    {
        using var a = Leaf("a"); using var b = Leaf("b"); using var c = Leaf("c"); using var plane = Plane(a, b, c); var updates = 0; plane.TrianglesUpdated += () => updates++; Check(plane.GetTriangleCount() == 1 && updates == 1, "Automatic Delaunay rebuild is synchronous on demand."); using var setup = new Setup(plane, Clip(10), Clip(30), Clip(70));
        setup.Tree.SetParameter("", AnimationNodeBlendSpace2D.BlendPosition, new Vector2(.25f, .25f)); setup.Tree.Advance(0); Near(setup.Target.Value, 30); setup.Tree.SetParameter("", AnimationNodeBlendSpace2D.BlendPosition, new Vector2(1, 1)); setup.Tree.Advance(0); Near(setup.Target.Value, 50); plane.BlendMode = AnimationBlendMode.Discrete; setup.Tree.Advance(0); Near(setup.Target.Value, 30);
        setup.Tree.SetParameter("", AnimationNodeBlendSpace2D.BlendPosition, new Vector2(float.NaN, 0)); Reject<ArgumentOutOfRangeException>(() => setup.Tree.Advance(0)); setup.Tree.SetParameter("", AnimationNodeBlendSpace2D.BlendPosition, Vector2.Zero);
        plane.AutoTriangles = false; plane.RemoveTriangle(0); plane.BlendMode = AnimationBlendMode.Interpolated; setup.Tree.Advance(0); Near(setup.Target.Value, 0); plane.AddTriangle(0, 1, 2); plane.SetBlendPointPosition(2, new(2, 0)); setup.Tree.SetParameter("", AnimationNodeBlendSpace2D.BlendPosition, new Vector2(.5f, 1)); setup.Tree.Advance(0); Near(setup.Target.Value, 20);
        plane.AutoTriangles = true; Check(plane.GetTriangleCount() == 0, "Collinear auto points have no triangle."); plane.SetBlendPointPosition(0, new(-float.MaxValue, -float.MaxValue)); plane.SetBlendPointPosition(1, new(float.MaxValue, -float.MaxValue)); plane.SetBlendPointPosition(2, new(0, float.MaxValue)); Check(plane.GetTriangleCount() == 1, "Finite extreme coordinates triangulate after normalization.");
    }
    private static void Clocks()
    {
        using var a = Leaf("a"); using var b = Leaf("b"); using var axis = new AnimationNodeBlendSpace1D { BlendMode = AnimationBlendMode.DiscreteCarry }; axis.AddBlendPoint(a, 0, name: "a"); axis.AddBlendPoint(b, 1, name: "b"); using var setup = new Setup(axis, Moving(4), Moving(8)); setup.Tree.Advance(0); setup.Tree.Advance(1); Near(setup.Tree.GetParameter("a", AnimationNode.CurrentPosition), 1); Near(setup.Tree.GetParameter("b", AnimationNode.CurrentPosition), 0);
        setup.Tree.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, 1f); setup.Tree.Advance(.5); Near(setup.Tree.GetParameter("b", AnimationNode.CurrentPosition), 1.5); axis.SetBlendPointName(1, "renamed"); axis.ReorderBlendPoint(0, 1); setup.Tree.Advance(.5); Near(setup.Tree.GetParameter("renamed", AnimationNode.CurrentPosition), 2);
        axis.BlendMode = AnimationBlendMode.Interpolated; axis.SyncMode = AnimationSyncMode.CyclicMutable; setup.Tree.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, .5f); setup.Tree.Advance(.6); Near(setup.Tree.GetParameter("a", AnimationNode.CurrentPosition), 1.4); Near(setup.Tree.GetParameter("renamed", AnimationNode.CurrentPosition), 2.8);
        axis.SyncMode = AnimationSyncMode.CyclicConstant; axis.CyclicLength = 2; setup.Tree.Advance(.25); Near(setup.Tree.GetParameter("a", AnimationNode.CurrentPosition), 1.9); Near(setup.Tree.GetParameter("renamed", AnimationNode.CurrentPosition), 3.8); axis.CyclicLength = 0; setup.Tree.Advance(1); Near(setup.Tree.GetParameter("a", AnimationNode.CurrentPosition), 1.9);
        using var other = new Setup(axis, Moving(2), Moving(6)); axis.CyclicLength = 4; other.Tree.Advance(0); other.Tree.Advance(1); Near(other.Tree.GetParameter("a", AnimationNode.CurrentPosition), .5); Near(other.Tree.GetParameter("renamed", AnimationNode.CurrentPosition), 1.5);
        setup.Clips[0].Length = 8; setup.Tree.Advance(.1); Near(setup.Tree.GetParameter("a", AnimationNode.CurrentPosition), 2.1);
        axis.SyncMode = AnimationSyncMode.Independent; axis.BlendMode = AnimationBlendMode.Discrete; setup.Tree.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, 1f); var frozen = setup.Tree.GetParameter("a", AnimationNode.CurrentPosition); setup.Tree.Advance(.1); Near(setup.Tree.GetParameter("a", AnimationNode.CurrentPosition), frozen + .1);
        using var nested = new AnimationNodeBlendSpace1D(); axis.SyncMode = AnimationSyncMode.CyclicMutable; nested.AddBlendPoint(a, 0); axis.SetBlendPointNode(0, nested); Reject<InvalidOperationException>(() => setup.Tree.Advance(0));
        using var invalidPlane = new AnimationNodeBlendSpace2D { SyncMode = AnimationSyncMode.CyclicConstant }; invalidPlane.AddBlendPoint(nested, Vector2.Zero); setup.Tree.TreeRoot = invalidPlane; Reject<InvalidOperationException>(() => setup.Tree.Advance(0));
    }
    private static void PingPongCarry()
    {
        using var a = Leaf("a"); using var b = Leaf("b"); using var axis = new AnimationNodeBlendSpace1D { BlendMode = AnimationBlendMode.DiscreteCarry }; axis.AddBlendPoint(a, 0, name: "a"); axis.AddBlendPoint(b, 1, name: "b"); using var setup = new Setup(axis, Moving(4), Moving(4)); foreach (var clip in setup.Clips) clip.LoopMode = SpriteFrames.LoopMode.PingPong;
        setup.Tree.Advance(0); setup.Tree.Advance(4.5); Near(setup.Tree.GetParameter("a", AnimationNode.CurrentPosition), 3.5); setup.Tree.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, 1f); setup.Tree.Advance(.1); Near(setup.Tree.GetParameter("b", AnimationNode.CurrentPosition), 3.4); setup.Tree.Advance(.1); Near(setup.Tree.GetParameter("b", AnimationNode.CurrentPosition), 3.3);
    }
    private static void Ownership()
    {
        using var a = Leaf("a"); using var b = Leaf("b"); using var c = Leaf("c"); using var plane = Plane(a, b, c); using var setup = new Setup(plane, Clip(10), Clip(30), Clip(70)); plane.TrianglesUpdated += () => { if (plane.GetBlendPointCount() == 3) plane.RemoveBlendPoint(2); }; setup.Tree.Advance(0); Near(setup.Target.Value, 0); setup.Tree.Advance(0); Near(setup.Target.Value, 0);
        using var axis = new AnimationNodeBlendSpace1D(); axis.AddBlendPoint(a, 0, name: "a"); axis.AddBlendPoint(a, 1, name: "same"); using var copy = (AnimationNodeBlendSpace1D)axis.Duplicate(true); Check(copy.GetBlendPointNode(0) == copy.GetBlendPointNode(1) && copy.GetBlendPointNode(0) != a, "Deep copies preserve definition aliases."); copy.GetBlendPointNode(0).Dispose();
        Action<ulong, string, string> fail = (_, _, _) => throw new InvalidOperationException("fixture"); axis.AnimationNodeRenamed += fail; Reject<AggregateException>(() => axis.SetBlendPointName(0, "renamed")); Check(axis.GetBlendPointName(0) == "renamed", "Rename commits before callback failure."); axis.AnimationNodeRenamed -= fail;
        using var failing = Plane(a, b, c); Action boom = () => throw new InvalidOperationException("triangle fixture"); failing.TrianglesUpdated += boom; Reject<InvalidOperationException>(() => failing.GetTriangleCount()); failing.TrianglesUpdated -= boom; Check(failing.GetTriangleCount() == 1, "Triangle publication survives observer failure."); using var reentrant = Plane(a, b, c); reentrant.TrianglesUpdated += () => reentrant.RemoveBlendPoint(2); Reject<InvalidOperationException>(() => reentrant.AddTriangle(0, 1, 2)); Check(reentrant.GetBlendPointCount() == 2, "Reentrant rebuild cannot publish stale triangle indices."); axis.Dispose(); Check(!a.IsDisposed, "Dispose borrows points.");
    }
    private static void Warm()
    {
        using var a = Leaf("a"); using var b = Leaf("b"); using var c = Leaf("c"); using var plane = Plane(a, b, c); plane.SyncMode = AnimationSyncMode.CyclicMutable; using var setup = new Setup(plane, Moving(4), Moving(8), Moving(12)); setup.Tree.SetParameter("", AnimationNodeBlendSpace2D.BlendPosition, new Vector2(.25f, .25f)); for (var i = 0; i < 20; i++) setup.Scene.ProcessFrame(.01);
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) setup.Scene.ProcessFrame(.01); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "256 warmed 2D cyclic graph/mixer frames allocate zero managed bytes.");
        using var axis = new AnimationNodeBlendSpace1D { BlendMode = AnimationBlendMode.DiscreteCarry, SyncMode = AnimationSyncMode.Independent }; axis.AddBlendPoint(a, 0, name: "a"); axis.AddBlendPoint(b, 1, name: "b"); using var carry = new Setup(axis, Moving(4), Moving(8)); for (var i = 0; i < 20; i++) { carry.Tree.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, (i & 1) == 0 ? .25f : .75f); carry.Tree.Advance(.001); }
        bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) { carry.Tree.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, (i & 1) == 0 ? .25f : .75f); carry.Tree.Advance(.001); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "256 warmed alternating carry changes allocate zero managed bytes.");
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_RENDERER") ?? "gpu"; var settings = ProjectSettings.Service; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var descriptor = new PropertyDescriptor<Entity, Vector2>(nameof(Entity.Position), n => n.Position, (n, v) => n.Position = v);
        Vector2[] expected = [new(16, 16), new(40, 16), new(28, 32), new(40, 32), new(64, 16), new(16, 48), new(40, 16)];
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var a = Leaf("a"); using var b = Leaf("b"); using var c = Leaf("c"); using var plane = Plane(a, b, c); using var axis = new AnimationNodeBlendSpace1D(); axis.AddBlendPoint(a, 0, name: "a"); axis.AddBlendPoint(b, 1, name: "b"); using var library = new AnimationLibrary(); var clips = new Animation[3];
                for (var i = 0; i < clips.Length; i++) { var clip = new Animation { Length = 4 }; var track = clip.AddTrack(descriptor); clip.TrackSetPath(track, "box:Position"); clip.TrackInsertKey(track, 0, i switch { 0 => new Vector2(16, 16), 1 => new Vector2(64, 16), _ => new Vector2(16, 48) }); clips[i] = clip; library.AddAnimation(((char)('a' + i)).ToString(), clip); }
                var window = new Window { Size = new(96, 64), Title = "Electron2D animation blend spaces" }; var box = new Box { Name = "box" }; var tree = new AnimationTree { TreeRoot = plane, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; tree.AddAnimationLibrary("", library); window.AddChild(box); window.AddChild(tree); var stage = 0;
                box.Start = () =>
                {
                    var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black); tree.Advance(0); RenderingServer.FramePostDraw += () =>
                    {
                        using var pixels = renderer.Readback(); var at = expected[stage]; Check(pixels.GetPixel((int)at.X, (int)at.Y).R > .9f && pixels.GetPixel((int)at.X, (int)at.Y).G < .1f, "Blend-space output reaches rendered pixels."); if (stage > 0 && expected[stage - 1] != at) { var old = expected[stage - 1]; Check(pixels.GetPixel((int)old.X, (int)old.Y).R < .1f, "Old blend-space pose clears."); }
                        switch (stage++) { case 0: tree.SetParameter("", AnimationNodeBlendSpace2D.BlendPosition, new Vector2(.5f, 0)); break; case 1: tree.SetParameter("", AnimationNodeBlendSpace2D.BlendPosition, new Vector2(.25f, .5f)); break; case 2: tree.SetParameter("", AnimationNodeBlendSpace2D.BlendPosition, Vector2.One); break; case 3: plane.BlendMode = AnimationBlendMode.Discrete; break; case 4: plane.BlendMode = AnimationBlendMode.DiscreteCarry; tree.SetParameter("", AnimationNodeBlendSpace2D.BlendPosition, new Vector2(0, 1)); break; case 5: tree.TreeRoot = axis; tree.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, .5f); break; case 6: window.Tree!.Quit(); return; }
                        tree.Advance(.1);
                    };
                };
                try { Check(Engine.Run(window) == 0 && stage == 7 && window.IsDisposed && !plane.IsDisposed && !axis.IsDisposed && !library.IsDisposed, "Blend-space host cleanup borrows definitions and clips."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "animation-blend-spaces-host", backend, run, stages = stage, cleaned = window.IsDisposed })); } finally { foreach (var clip in clips) clip.Dispose(); }
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private sealed class Box : Entity { internal Action? Start; protected override void OnReady() => Start?.Invoke(); protected override void OnDraw() => DrawRect(new(-3, -3, 6, 6), Colors.Red); }
    private static AnimationNodeAnimation Leaf(string name) => new() { Animation = name };
    private static AnimationNodeBlendSpace2D Plane(AnimationNodeAnimation a, AnimationNodeAnimation b, AnimationNodeAnimation c) { var p = new AnimationNodeBlendSpace2D(); p.AddBlendPoint(a, new(0, 0), name: "a"); p.AddBlendPoint(b, new(1, 0), name: "b"); p.AddBlendPoint(c, new(0, 1), name: "c"); return p; }
    private static Animation Clip(double value) { var clip = new Animation { Length = 4 }; var track = clip.AddTrack(Value); clip.TrackSetPath(track, "target:Value"); clip.TrackInsertKey(track, 0, value); return clip; }
    private static Animation Moving(double length) { var clip = Clip(0); clip.Length = length; clip.TrackInsertKey(0, length, length * 10); return clip; }
    private sealed class Setup : IDisposable
    {
        internal readonly AnimationTree Tree = new(); internal readonly Probe Target = new() { Name = "target" }; internal readonly AnimationLibrary Library = new(); internal readonly SceneTree Scene; internal readonly Animation[] Clips;
        internal Setup(AnimationRootNode root, params Animation[] clips) { Clips = clips; for (var i = 0; i < clips.Length; i++) Library.AddAnimation(((char)('a' + i)).ToString(), clips[i]); var scene = new Node(); scene.AddChild(Target); scene.AddChild(Tree); Scene = new(scene); Tree.AddAnimationLibrary("", Library); Tree.TreeRoot = root; }
        public void Dispose() { Scene.Dispose(); Library.Dispose(); foreach (var clip in Clips) clip.Dispose(); }
    }
    private sealed class Probe : Node { internal double Value; }
    private static void Near(double actual, double expected) => Check(Math.Abs(actual - expected) < 1e-5, $"Expected {expected}, got {actual}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
