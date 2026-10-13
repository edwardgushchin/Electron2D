using Electron2D;
using System.Diagnostics;
using Path = System.IO.Path;

internal static class AnimationGraphPersistenceTests
{
    private static readonly PropertyDescriptor<Entity, Vector2> Position = new(nameof(Entity.Position), n => n.Position, (n, v) => n.Position = v);
    private static readonly AnimationParameter<bool> Flag = new("advance", false);
    private static readonly Func<AnimationTree, bool> Predicate = static t => t.GetParameter("", Flag);
    private static bool _registered;
    private static void Register()
    {
        if (_registered) return;
        ResourceFileTypes.RegisterNode("test.graphfiles.Box", CreateBox);
        ResourceFileTypes.RegisterResource("test.graphfiles.Extension", CreateExtension);
        ResourceFileTypes.RegisterAnimationProperty("test.graphfiles.position", Position);
        ResourceFileTypes.RegisterAnimationParameter("test.graphfiles.flag", Flag);
        ResourceFileTypes.RegisterAnimationPredicate("test.graphfiles.predicate", Predicate); _registered = true;
    }
    private static Box CreateBox() => new();
    private static Extension CreateExtension() => new();
    private sealed class Extension : AnimationNodeExtension
    {
        protected override AnimationNodeTimeInfo OnProcessAnimationNode(in AnimationNodePlaybackInfo info, bool testOnly)
        { BlendAnimation("a", info.Time, info.Delta, info.Seeked, info.IsExternalSeeking, 1); return new(1, info.Time, info.Delta); }
    }
    private sealed class Box : Entity
    {
        protected override Func<Node> CreateSceneInstanceFactory() => CreateBox;
        protected override void OnDraw() => DrawRect(new(0, 0, 8, 8), Colors.Red);
    }
    internal static void Run()
    {
        Register(); var directory = Path.Combine(Path.GetTempPath(), "e2d-graphfile-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var scenePath = Path.Combine(directory, "scene.e2dscene"); WriteFixture(scenePath);
            using var scene = ResourceLoader.Load<PackedScene>(scenePath, ResourceLoader.CacheMode.IgnoreDeep); VerifyScene(scene);
            Variants(directory); Validate(); Child(scenePath, false); Child(scenePath, true);
        }
        finally { Directory.Delete(directory, true); }
        Console.WriteLine("Graph files: definitions, topology, aliases, parameters, action/space/machine variants, fresh scenes and failures passed.");
    }
    internal static void WriteFixture(string path)
    {
        Register(); using var a = Clip(new(16, 16), new(32, 16)); using var b = Clip(new(32, 16), new(48, 16)); using var library = new AnimationLibrary(); library.AddAnimation("a", a); library.AddAnimation("b", b);
        using var left = new AnimationNodeAnimation { Animation = "a" }; using var right = new AnimationNodeAnimation { Animation = "b" };
        using var blend = new AnimationNodeBlend2 { Sync = true, FilterEnabled = true }; blend.SetFilterPath("target:Position", true);
        using var scale = new AnimationNodeTimeScale(); using var seek = new AnimationNodeTimeSeek { ExplicitElapse = false }; using var graph = new AnimationNodeBlendTree { GraphOffset = new(7, 9) };
        graph.AddNode("left", left, new(10, 20)); graph.AddNode("right", right, new(10, 50)); graph.AddNode("blend", blend, new(40, 30)); graph.AddNode("scale", scale); graph.AddNode("seek", seek);
        graph.ConnectNode("blend", 0, "left"); graph.ConnectNode("blend", 1, "right"); graph.ConnectNode("scale", 0, "blend"); graph.ConnectNode("seek", 0, "scale"); graph.ConnectNode("output", 0, "seek");
        using var root = new Node { Name = "root" }; var box = new Box { Name = "target" }; var tree = new AnimationTree { Name = "tree", TreeRoot = graph, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual };
        var other = new AnimationTree { Name = "other", TreeRoot = graph, Active = false }; tree.AddAnimationLibrary("", library); other.AddAnimationLibrary("", library); root.AddChild(box); root.AddChild(tree); root.AddChild(other); box.Owner = tree.Owner = other.Owner = root;
        tree.SetParameter("blend", AnimationNodeBlend2.BlendAmount, .25); other.SetParameter("blend", AnimationNodeBlend2.BlendAmount, .75); tree.SetParameter("scale", AnimationNodeTimeScale.Scale, 1d);
        tree.Advance(.2); using var packed = new PackedScene(); packed.Pack(root); ResourceSaver.Save(packed, path, SaverFlags.BundleResources | SaverFlags.SaveBigEndian);
    }
    private static Animation Clip(Vector2 from, Vector2 to)
    {
        var a = new Animation { Length = 1 }; var t = a.AddTrack(Position); a.TrackSetPath(t, "target:Position"); a.TrackInsertKey(t, 0, from); a.TrackInsertKey(t, 1, to); return a;
    }
    private static void VerifyScene(PackedScene packed)
    {
        using var root = packed.Instantiate(); var tree = (AnimationTree)root.GetNode("tree"); var other = (AnimationTree)root.GetNode("other"); var box = (Box)root.GetNode("target");
        Check(ReferenceEquals(tree.TreeRoot, other.TreeRoot) && ReferenceEquals(tree.GetAnimationLibrary(""), other.GetAnimationLibrary("")), "Shared definitions and libraries survive scene files.");
        Check(tree.GetParameter("blend", AnimationNodeBlend2.BlendAmount) == .25 && other.GetParameter("blend", AnimationNodeBlend2.BlendAmount) == .75, "Per-tree authored parameter cells stay independent.");
        var graph = (AnimationNodeBlendTree)tree.TreeRoot!; Check(graph.GraphOffset == new Vector2(7, 9) && graph.GetNodePosition("left") == new Vector2(10, 20) && graph.GetNode("blend").IsPathFiltered("target:Position"), "Definition metadata and filters survive.");
        tree.Advance(0); Near(box.Position.X, 20); tree.Advance(.5); Near(box.Position.X, 28); tree.Advance(.5); Near(box.Position.X, 36);
        Check(tree.GetParameter("left", AnimationNode.CurrentPosition) == 1, "Loaded graph clocks execute from startup.");
        packed.Dispose(); tree.Advance(0); Check(!graph.IsDisposed, "Instantiated graph retains file ownership after template disposal.");
    }
    private static void Variants(string directory)
    {
        using var extension = new Extension(); var extensionPath = Path.Combine(directory, "extension.e2dres"); ResourceSaver.Save(extension, extensionPath, SaverFlags.BundleResources);
        using (var copy = ResourceLoader.Load<Extension>(extensionPath, ResourceLoader.CacheMode.IgnoreDeep)) EqualTrace(extension, copy);
        using var a = new AnimationNodeAnimation { Animation = "a", UseCustomTimeline = true, TimelineLength = 3, StartOffset = .25, StretchTimeScale = true };
        using var b = new AnimationNodeAnimation { Animation = "b" }; using var curve = new Curve(); curve.AddPoint(new(0, 0)); curve.AddPoint(new(1, 1));
        using var action = new AnimationNodeOneShot { FadeInTime = .2, FadeOutTime = .3, FadeInCurve = curve, FadeOutCurve = curve, Autorestart = true, AutorestartDelay = .4, AutorestartRandomDelay = .1, MixMode = AnimationMixMode.Add };
        var path = Path.Combine(directory, "action.e2dres"); ResourceSaver.Save(action, path, SaverFlags.BundleResources);
        using (var copy = ResourceLoader.Load<AnimationNodeOneShot>(path, ResourceLoader.CacheMode.IgnoreDeep)) Check(copy.FadeInTime == .2 && copy.Autorestart && ReferenceEquals(copy.FadeInCurve, copy.FadeOutCurve), "One-shot definitions and curve aliases survive.");
        using (var copy = ResourceLoader.Load<AnimationNodeOneShot>(path, ResourceLoader.CacheMode.IgnoreDeep)) EqualTrace(action, copy);
        using var transition = new AnimationNodeTransition { InputCount = 2, XFadeTime = .25, XFadeCurve = curve }; transition.SetInputName(0, "a"); transition.SetInputName(1, "b"); transition.SetInputAsAutoAdvance(0, true); transition.SetInputBreakLoopAtEnd(1, true); transition.SetInputReset(1, false);
        path = Path.Combine(directory, "transition.e2dres"); ResourceSaver.Save(transition, path, SaverFlags.BundleResources);
        using (var copy = ResourceLoader.Load<AnimationNodeTransition>(path, ResourceLoader.CacheMode.IgnoreDeep)) Check(copy.GetInputName(0) == "a" && copy.IsInputSetAsAutoAdvance(0) && copy.IsInputLoopBrokenAtEnd(1) && !copy.IsInputReset(1), "Named input policies survive.");
        using (var copy = ResourceLoader.Load<AnimationNodeTransition>(path, ResourceLoader.CacheMode.IgnoreDeep)) EqualTrace(transition, copy);
        using var line = new AnimationNodeBlendSpace1D { SyncMode = AnimationSyncMode.CyclicMutable, CyclicLength = 2, ValueLabel = "speed" }; line.AddBlendPoint(a, -1, name: "a"); line.AddBlendPoint(b, 1, name: "b");
        path = Path.Combine(directory, "line.e2dres"); ResourceSaver.Save(line, path, SaverFlags.BundleResources);
        using (var copy = ResourceLoader.Load<AnimationNodeBlendSpace1D>(path, ResourceLoader.CacheMode.IgnoreDeep)) Check(copy.GetBlendPointName(0) == "a" && copy.GetBlendPointPosition(1) == 1 && copy.SyncMode == AnimationSyncMode.CyclicMutable && copy.CyclicLength == 2 && copy.ValueLabel == "speed", "Scalar space definitions survive.");
        using (var copy = ResourceLoader.Load<AnimationNodeBlendSpace1D>(path, ResourceLoader.CacheMode.IgnoreDeep)) EqualTrace(line, copy);
        using var plane = new AnimationNodeBlendSpace2D { AutoTriangles = false, SyncMode = AnimationSyncMode.CyclicConstant, CyclicLength = 2, XLabel = "horizontal" }; plane.AddBlendPoint(a, default, name: "a"); plane.AddBlendPoint(b, Vector2.Right, name: "b"); plane.AddBlendPoint(a, Vector2.Down, name: "alias"); plane.AddTriangle(0, 1, 2);
        path = Path.Combine(directory, "plane.e2dres"); ResourceSaver.Save(plane, path, SaverFlags.BundleResources);
        using (var copy = ResourceLoader.Load<AnimationNodeBlendSpace2D>(path, ResourceLoader.CacheMode.IgnoreDeep)) Check(copy.GetTriangleCount() == 1 && copy.GetTrianglePoint(0, 2) == 2 && !copy.AutoTriangles && copy.SyncMode == AnimationSyncMode.CyclicConstant && ReferenceEquals(copy.GetBlendPointNode(0), copy.GetBlendPointNode(2)), "Planar authored triangles and definition aliases survive.");
        using (var copy = ResourceLoader.Load<AnimationNodeBlendSpace2D>(path, ResourceLoader.CacheMode.IgnoreDeep)) EqualTrace(plane, copy);
        using var machine = new AnimationNodeStateMachine(); machine.AddNode("a", a); machine.AddNode("b", b); using var start = new AnimationNodeStateMachineTransition { AdvanceMode = AnimationAdvanceMode.Auto }; using var edge = new AnimationNodeStateMachineTransition { AdvanceMode = AnimationAdvanceMode.Auto, AdvanceCondition = Flag, AdvanceExpression = Predicate, XFadeTime = .1, XFadeCurve = curve };
        machine.AddTransition("Start", "a", start); machine.AddTransition("a", "b", edge); machine.AddTransition("b", "a", edge);
        path = Path.Combine(directory, "machine.e2dres"); ResourceSaver.Save(machine, path, SaverFlags.BundleResources);
        using (var copy = ResourceLoader.Load<AnimationNodeStateMachine>(path, ResourceLoader.CacheMode.IgnoreDeep))
        {
            Check(copy.GetTransitionCount() == 3 && ReferenceEquals(copy.GetTransition(1), copy.GetTransition(2)) && ReferenceEquals(copy.GetTransition(1).AdvanceCondition, Flag) && Equals(copy.GetTransition(1).AdvanceExpression, Predicate), "State-machine edge aliases and compiled identities survive."); EqualTrace(machine, copy);
            using var deep = (AnimationNodeStateMachine)copy.Duplicate(true); Check(ReferenceEquals(deep.GetTransition(1), deep.GetTransition(2)) && !ReferenceEquals(deep.GetNode("Start"), copy.GetNode("Start")), "Deep machine copies retain policy aliases and own independent boundaries.");
            using var target = new AnimationNodeStateMachine(); target.AddNode("old", new AnimationRootNode()); target.CopyFromResource(copy); Check(target.HasNode("a") && !target.HasNode("old"), "Machine copy replaces existing authored state.");
        }
        AnimationNode[] leaves = [new AnimationNode(), new AnimationRootNode(), new AnimationNodeSync(), new AnimationNodeOutput(), new AnimationNodeBlend2(), new AnimationNodeBlend3(), new AnimationNodeAdd2(), new AnimationNodeAdd3(), new AnimationNodeSub2(), new AnimationNodeTimeScale(), new AnimationNodeTimeSeek()];
        foreach (var node in leaves) using (node) { path = Path.Combine(directory, node.GetType().Name + ".e2dres"); ResourceSaver.Save(node, path); using var copy = ResourceLoader.Load<AnimationNode>(path, ResourceLoader.CacheMode.IgnoreDeep); Check(copy.GetType() == node.GetType() && copy.GetInputCount() == node.GetInputCount(), "Exact leaf factory and inputs."); }
    }
    private static void EqualTrace(AnimationNode original, AnimationNode loaded)
    {
        var a = Trace(original); var b = Trace(loaded);
        Check(a.Length == b.Length && a.Zip(b).All(p => p.First.DistanceTo(p.Second) < .001f), "Loaded definition executes the same active trace.");
        Check(a.Any(p => p != a[0]), "Definition trace contains actual movement.");
    }
    private static Vector2[] Trace(AnimationNode definition)
    {
        using var a = Clip(new(16, 16), new(32, 16)); using var b = Clip(new(32, 16), new(48, 16)); using var library = new AnimationLibrary(); library.AddAnimation("a", a); library.AddAnimation("b", b);
        using var left = new AnimationNodeAnimation { Animation = "a" }; using var right = new AnimationNodeAnimation { Animation = "b" }; using var wrapper = new AnimationNodeBlendTree();
        var nodePath = ""; AnimationRootNode rootDefinition;
        if (definition is AnimationRootNode rootNode) rootDefinition = rootNode;
        else { wrapper.AddNode("a", left); wrapper.AddNode("b", right); wrapper.AddNode("operation", definition); if (definition.GetInputCount() > 0) wrapper.ConnectNode("operation", 0, "a"); if (definition.GetInputCount() > 1) wrapper.ConnectNode("operation", 1, "b"); wrapper.ConnectNode("output", 0, "operation"); rootDefinition = wrapper; nodePath = "operation"; }
        using var root = new Node(); var box = new Box { Name = "target" }; var tree = new AnimationTree { TreeRoot = rootDefinition, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual }; root.AddChild(box); root.AddChild(tree); tree.AddAnimationLibrary("", library);
        if (definition is AnimationNodeOneShot) tree.SetParameter(nodePath, AnimationNodeOneShot.Request, AnimationOneShotRequest.Fire);
        if (definition is AnimationNodeBlendSpace1D) tree.SetParameter("", AnimationNodeBlendSpace1D.BlendPosition, .25f);
        if (definition is AnimationNodeBlendSpace2D) tree.SetParameter("", AnimationNodeBlendSpace2D.BlendPosition, new Vector2(.25f, .25f));
        var trace = new Vector2[8];
        for (var i = 0; i < trace.Length; i++) { if (definition is AnimationNodeStateMachine && i == 2) tree.SetParameter("", Flag, true); tree.Advance(i == 0 ? 0 : .25); trace[i] = box.Position; }
        return trace;
    }
    private static void Validate()
    {
        Reject<InvalidOperationException>(() => ResourceFileTypes.RegisterAnimationParameter("test.graphfiles.flag", Flag)); Reject<InvalidOperationException>(() => ResourceFileTypes.RegisterAnimationPredicate("test.graphfiles.other", Predicate));
        using var node = new AnimationNodeBlend2(); var inputs = (PropertyDescriptor<AnimationNode, string[]>)node.GetPropertyList().Single(p => p.Name == "InputNames"); Reject<InvalidDataException>(() => inputs.SetValue(node, ["bad/name"])); Check(node.GetInputCount() == 2, "Invalid port names preserve state.");
        using var graph = new AnimationNodeBlendTree(); var names = (PropertyDescriptor<AnimationNodeBlendTree, string[]>)graph.GetPropertyList().Single(p => p.Name == "NodeNames"); Reject<InvalidDataException>(() => names.SetValue(graph, ["a", "a"]));
        var connections = (PropertyDescriptor<AnimationNodeBlendTree, string[]>)graph.GetPropertyList().Single(p => p.Name == "Connections");
        Reject<InvalidDataException>(() => connections.SetValue(graph, ["output", "NaN", "missing"]));
        connections.SetValue(graph, ["output", "0", "missing"]); Reject<ArgumentException>(() => graph.ValidateFileState());
        using var edge = new AnimationNodeStateMachineTransition(); var condition = (PropertyDescriptor<AnimationNodeStateMachineTransition, string>)edge.GetPropertyList().Single(p => p.Name == "ConditionID");
        Reject<InvalidDataException>(() => condition.SetValue(edge, "engine.AnimationNodeBlend2.BlendAmount"));
        var predicate = (PropertyDescriptor<AnimationNodeStateMachineTransition, string>)edge.GetPropertyList().Single(p => p.Name == "PredicateID"); Reject<InvalidDataException>(() => predicate.SetValue(edge, "unknown"));
        using var tree = new AnimationTree(); var schemas = (PropertyDescriptor<AnimationTree, string[]>)tree.GetPropertyList().Single(p => p.Name == "ParameterSchemas"); Reject<InvalidDataException>(() => schemas.SetValue(tree, ["", "unknown"]));
        schemas.SetValue(tree, ["missing", "engine.AnimationNodeBlend2.BlendAmount"]); tree.SetFileParameter("missing", AnimationNodeBlend2.BlendAmount, .5); Reject<InvalidDataException>(() => tree.ValidateSceneInstance());
        using var boundaryMachine = new AnimationNodeStateMachine(); boundaryMachine.GetNode("Start").ResourceName = "owned start"; using var boundaryCopy = (AnimationNodeStateMachine)boundaryMachine.Duplicate(); Check(boundaryCopy.GetNode("Start").ResourceName == "owned start", "Owned shallow boundary metadata copies.");
        using var outputGraph = new AnimationNodeBlendTree(); outputGraph.GetNode("output").ResourceName = "owned output"; using var outputCopy = (AnimationNodeBlendTree)outputGraph.Duplicate(true); Check(outputCopy.GetNode("output").ResourceName == "owned output" && !ReferenceEquals(outputCopy.GetNode("output"), outputGraph.GetNode("output")), "Owned output metadata and independent identity copy.");
    }
    private static void Child(string path, bool unknown)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; start.ArgumentList.Add(typeof(AnimationGraphPersistenceTests).Assembly.Location);
        start.Environment.Remove("ELECTRON2D_TEST_GRAPH_FILE"); start.Environment["ELECTRON2D_TEST_GRAPH_FILE_CHILD"] = path; start.Environment["ELECTRON2D_TEST_GRAPH_FILE_UNKNOWN"] = unknown ? "1" : "0";
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Graph file child."); }
        Check(child.ExitCode == 0, output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string path)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_GRAPH_FILE_UNKNOWN") == "1") { Reject<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep)); return; }
        Register(); using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); VerifyScene(scene);
        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(path)!, "*.e2dres"))
        { using var resource = ResourceLoader.Load<Resource>(file, ResourceLoader.CacheMode.IgnoreDeep); if (resource is Extension or AnimationNodeOneShot or AnimationNodeTransition or AnimationNodeBlendSpace1D or AnimationNodeBlendSpace2D or AnimationNodeStateMachine) _ = Trace((AnimationNode)resource); }
    }
    internal static void RunNative(string path)
    {
        Register(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATION_RENDERER") ?? "gpu"; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); var root = packed.Instantiate(); var tree = (AnimationTree)root.GetNode("tree"); var window = new Window { Size = new(80, 64), Title = "Electron2D loaded graph scene" }; window.AddChild(root); var stage = 0;
            root.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Actual requested renderer."); RenderingServer.SetDefaultClearColor(Colors.Black); tree.Advance(0); RenderingServer.FramePostDraw += () => { using var image = RenderingServer.Service!.Readback(); Check(image.GetPixel(20 + stage * 8, 16).R > .9f, "Loaded graph pose reaches actual pixels."); stage++; if (stage == 3) window.Tree!.Quit(); else tree.Advance(.5); }; };
            Check(Engine.Run(window) == 0 && stage == 3 && window.IsDisposed, "Loaded graph host cleanup."); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "loaded-graph-scene", backend, stages = stage, cleaned = window.IsDisposed }));
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private static void Near(float value, float expected) => Check(Math.Abs(value - expected) < .001, $"Expected {expected}, got {value}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
