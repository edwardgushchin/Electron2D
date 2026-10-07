using Electron2D;
using System.Diagnostics;
using IOPath = System.IO.Path;

internal static class PhysicalBoneTests
{
    private static Bone Bone(string name, Vector2 position) { var bone = new Bone { Name = name, Position = position, Rest = new Transform(0, position) }; bone.SetAutocalculateLengthAndAngle(false); bone.SetLength(40); return bone; }
    private sealed class Setup : IDisposable
    {
        internal readonly Entity Root = new();
        internal readonly Skeleton Rig = new() { Name = "Rig" };
        internal readonly Bone A = Bone("A", Vector2.Zero), B = Bone("B", new(40, 0));
        internal readonly PhysicalBone P = new() { Name = "P", BoneNodePath = "../A", GravityScale = 0, CanSleep = false };
        internal readonly PhysicalBone Q = new() { Name = "Q", BoneNodePath = "../../A/B", GravityScale = 0, CanSleep = false };
        internal readonly CircleShape Shape = new() { Radius = 4 };
        internal readonly SkeletonModificationPhysicalBones Mod = new() { PhysicalBoneChainLength = 2 };
        internal readonly SkeletonModificationStack Stack = new() { Enabled = true };
        internal readonly SceneTree Tree;
        internal Setup(bool joint = false)
        {
            A.AddChild(B); Rig.AddChild(A); P.AddChild(new CollisionShape { Name = "Shape", Shape = Shape }); Q.AddChild(new CollisionShape { Name = "Shape", Shape = Shape }); P.AddChild(Q); Rig.AddChild(P); if (joint) Q.AddChild(new PinJoint { Name = "Pin" }); Mod.SetPhysicalBoneNode(0, "P"); Mod.SetPhysicalBoneNode(1, "P/Q"); Stack.AddModification(Mod); Rig.SetModificationStack(Stack); Root.AddChild(Rig); Tree = new(Root);
        }
        internal void Step(double delta = 1d / 60) { Tree.PhysicsFrame(delta); Tree.ProcessFrame(delta); }
        public void Dispose() { Tree.Dispose(); Root.Dispose(); Stack.Dispose(); Mod.Dispose(); Shape.Dispose(); }
    }
    internal static void Run() { Motion(); Binding(); Joints(); Commands(); Failure(); PhasesAndGuards(); Storage(); Warm(); Console.WriteLine("Physical bone motion, pose transfer, joints, filters and fresh scene checks passed."); }
    private static void Motion()
    {
        using var s = new Setup(); Check(!s.P.SimulatePhysics && !s.P.IsSimulatingPhysics() && s.P.GetJoint() is null && s.P.BoneIndex == 0 && s.Q.BoneIndex == 1, "Defaults, distinct borrowed Bone selection, no generated joint."); Near(s.Q.GlobalPosition, s.B.GlobalPosition, .0001f, "Child Ready bootstraps parent before following.");
        s.P.CollisionLayer = 8; s.P.CollisionMask = 16; using var ray = PhysicsRayQueryParameters.Create(new(-10, 0), new(10, 0), 8); Check(s.P.GetWorld()!.DirectSpaceState.IntersectRay(ray) is null, "Follower fixtures are noncolliding.");
        s.Mod.StartSimulation(["P"]); s.P.LinearVelocity = new(60, 0); for (var i = 0; i < 6; i++) s.Step(); Near(s.A.GlobalPosition, s.P.GlobalPosition, .0001f, "Solved body drives actual scene bone."); Check(s.P.GlobalPosition.X > 5 && !s.Q.SimulatePhysics && s.P.CollisionLayer == 8 && s.P.CollisionMask == 16, "Inherited velocity and configured filters preserved."); ray.From = s.P.GlobalPosition + new Vector2(-10, 0); ray.To = s.P.GlobalPosition + new Vector2(10, 0); Check(s.P.GetWorld()!.DirectSpaceState.IntersectRay(ray) is not null, "Active actual fixture responds to world query.");
        s.Stack.Strength = .5f; s.Step(); Near(s.A.Position.X, s.P.GlobalPosition.X * .5f, .001f, "Strength blends against authored local pose."); s.Mod.Enabled = false; s.Tree.ProcessFrame(.01); Near(s.A.Position, Vector2.Zero, .0001f, "Disabled transfer releases transient override."); s.Mod.Enabled = true; s.Stack.Strength = 1;
        s.P.Freeze = true; var frozen = s.P.GlobalPosition; s.Step(); Near(s.P.GlobalPosition, frozen, .0001f, "Inherited Freeze retains actual active identity and suppresses motion."); Check(s.P.IsSimulatingPhysics(), "Freeze does not erase request."); s.P.Freeze = false;
        s.P.FollowBoneWhenSimulating = true; s.A.Position = new(20, 0); s.P.LinearVelocity = Vector2.Zero; s.Step(); Near(s.P.GlobalPosition, new(20, 0), .0001f, "Follow seeds before physics."); Near(s.A.Position, new(20, 0), .0001f, "Follow skips physical pose transfer."); s.P.FollowBoneWhenSimulating = false;
        s.Mod.StopSimulation(); s.Step(); Check(!s.P.IsSimulatingPhysics() && s.P.CollisionLayer == 8 && s.P.CollisionMask == 16 && s.P.GetWorld()!.DirectSpaceState.IntersectRay(ray) is null, "Stop keeps configured filters and removes actual collision response."); Near(s.P.GlobalPosition, s.A.GlobalPosition, .0001f, "Stopped body follows authored bone.");
    }
    private static void Binding()
    {
        using var detached = new PhysicalBone(); Check(detached.BoneIndex == -1 && detached.GetJoint() is null && !detached.IsSimulatingPhysics(), "Detached safe reads."); detached.SimulatePhysics = true; Check(!detached.IsSimulatingPhysics(), "Detached request cannot simulate."); Reject<ArgumentOutOfRangeException>(() => detached.BoneIndex = -2); Reject<ArgumentException>(() => detached.BoneNodePath = "bad\0path");
        using var s = new Setup(); s.P.BoneIndex = 1; Check(s.P.BoneNodePath == "../A/B", "Numeric setter authors stable path."); s.P.BoneIndex = -1; Check(s.P.BoneNodePath == "" && s.P.BoneIndex == -1, "Minus one clears selection."); s.P.BoneNodePath = "../A"; s.P.SimulatePhysics = true; s.A.Name = "Changed"; s.Step(); Check(s.P.BoneIndex == -1 && !s.P.IsSimulatingPhysics() && s.P.SimulatePhysics, "Rename invalidates weak binding while retaining request."); s.P.BoneNodePath = "../Changed"; s.Step(); Check(s.P.IsSimulatingPhysics(), "Corrected path resumes simulation.");
        var neutral = new Entity { Name = "Neutral" }; s.Rig.AddChild(neutral); s.P.Reparent(neutral); s.Step(); Check(!s.P.IsSimulatingPhysics() && s.P.GetConfigurationWarnings().Length > 0, "Neutral parent breaks physical rig ownership."); s.P.Reparent(s.Rig); s.P.BoneNodePath = "../Changed"; s.Step(); Check(s.P.IsSimulatingPhysics(), "Reparenting valid rig recovers.");
        Reject<InvalidOperationException>(() => Task.Run(() => s.P.SimulatePhysics = false).GetAwaiter().GetResult()); Check(s.P.SimulatePhysics, "Off-owner mutation rejects before request changes.");
    }
    private static void Joints()
    {
        using var s = new Setup(true); var joint = (PinJoint)s.Q.GetJoint()!; Check(joint.NodeA == "../.." && joint.NodeB == ".." && joint.GlobalPosition == s.Q.GlobalPosition, "First authored joint automatically connects physical parent/body."); s.P.Freeze = true; ((CollisionShape)s.Q.GetChild(0)).Position = new(20, 0); s.Mod.StartSimulation(); s.Q.GravityScale = 1; for (var i = 0; i < 45; i++) s.Step(); Check(joint.GetConfigurationWarnings().Length == 0 && Math.Abs(s.Q.GlobalPosition.DistanceTo(s.P.GlobalPosition) - 40) < 2 && Math.Abs(s.Q.GlobalRotation) > .1f, "Actual pin solver swings a physical bone without stretching. P=" + s.P.GlobalPosition + " Q=" + s.Q.GlobalPosition + " warnings=" + string.Join(";", joint.GetConfigurationWarnings())); Near(s.B.GlobalPosition, s.Q.GlobalPosition, .001f, "Nested bone receives solved global pose.");
        joint.NodeA = "../../.."; joint.NodeB = ""; s.Q.AutoConfigureJoint = false; var point = joint.Position; s.Step(); Check(joint.NodeA == "../../.." && joint.NodeB == "" && joint.Position == point, "Manual configuration remains authored."); s.Q.AutoConfigureJoint = true; s.Step(); Check(joint.NodeB == "..", "Reenabled configuration reconnects.");
        var rootJoint = new PinJoint { NodeA = "../Shape", NodeB = "../Q" }; s.P.AddChild(rootJoint); s.Step(); Check(s.P.GetJoint() == rootJoint && rootJoint.NodeA == "../Shape", "Skeleton-parent joint endpoints remain authored."); s.P.RemoveChild(rootJoint); Check(s.P.GetJoint() is null && !rootJoint.IsDisposed, "Borrowed joint removal is independent of ownership."); rootJoint.Dispose();
    }
    private static void Commands()
    {
        using var mod = new SkeletonModificationPhysicalBones { PhysicalBoneChainLength = 1 }; mod.SetPhysicalBoneNode(0, "P"); var names = new[] { "P" }; mod.StartSimulation(names); names[0] = "Other"; var rig = new Skeleton(); var bone = Bone("A", Vector2.Zero); rig.AddChild(bone); var p = new PhysicalBone { Name = "P", BoneNodePath = "../A" }; rig.AddChild(p); using var stack = new SkeletonModificationStack { Enabled = true }; stack.AddModification(mod); rig.SetModificationStack(stack); using var tree = new SceneTree(rig); tree.ProcessFrame(.01); Check(p.SimulatePhysics, "Detached copied name request applies at actual setup."); mod.StopSimulation(["A"]); Check(p.SimulatePhysics, "Names select physical nodes rather than bound bones."); mod.StopSimulation(["P"]); Check(!p.SimulatePhysics, "Named stop applies immediately.");
        using var s = new Setup(); var foreign = new Skeleton { Name = "Foreign" }; foreign.AddChild(Bone("A", Vector2.Zero)); foreign.AddChild(new PhysicalBone { Name = "F", BoneNodePath = "../A" }); s.Rig.AddChild(foreign); s.Mod.FetchPhysicalBones(); Check(s.Mod.PhysicalBoneChainLength == 2 && s.Mod.GetPhysicalBoneNode(0) == "P" && s.Mod.GetPhysicalBoneNode(1) == "P/Q", "Breadth-first fetch omits nested foreign rigs."); s.Mod.PhysicalBoneChainLength = 3; Check(s.Mod.GetPhysicalBoneNode(2) == "", "Resize retains prefix and empty new slots."); Reject<ArgumentOutOfRangeException>(() => s.Mod.GetPhysicalBoneNode(3)); Reject<ArgumentOutOfRangeException>(() => s.Mod.PhysicalBoneChainLength = 4097); Reject<ArgumentNullException>(() => s.Mod.StartSimulation([null!])); using var copy = (SkeletonModificationPhysicalBones)s.Mod.Duplicate(); Check(copy.GetPhysicalBoneNode(1) == "P/Q" && !copy.GetIsSetup(), "Resource copy retains configuration without live ownership.");
    }
    private static void Failure()
    {
        using var s = new Setup(); s.Mod.StartSimulation(); s.P.Scale = new(2, 1); Reject<AggregateException>(() => s.Step()); s.P.Scale = Vector2.One; s.Step(); Check(s.P.IsSimulatingPhysics(), "Invalid inherited body geometry rejects and recovers."); s.Mod.SetPhysicalBoneNode(0, "Missing"); s.Step(); s.Mod.SetPhysicalBoneNode(0, "P"); s.Step(); Check(s.A.GlobalTransform.IsFinite(), "Missing consumer paths skip and reconnect.");
    }
    private sealed class Probe : SkeletonModification
    {
        internal Action? Action;
        protected override void OnExecute(double delta) => Action?.Invoke();
    }
    private sealed class Hook : PhysicalBone
    {
        internal bool Called;
        protected override void IntegrateForces(PhysicsDirectBodyState state)
        {
            Called = true; Check(IsSimulatingPhysics(), "Active state read in a solver callback is pure."); SimulatePhysics = false; Check(!IsSimulatingPhysics(), "Post-solver callback can stop a completed simulation."); SimulatePhysics = true; Check(IsSimulatingPhysics(), "Post-solver callback can reactivate its retained body.");
        }
    }
    private static void PhasesAndGuards()
    {
        using var s = new Setup(); s.Mod.StartSimulation(["P"]); s.P.Freeze = true; s.P.GlobalPosition = new(20, 0); s.Mod.ExecutionMode = ProcessPhase.Physics; s.Rig.ExecuteModifications(.01, ProcessPhase.Physics); Check(s.A.Position == Vector2.Zero, "Physics stages without replacing authoring."); s.Rig.ExecuteModifications(.01, ProcessPhase.Idle); Near(s.A.Position, new(20, 0), .0001f, "Idle consumes staged physical pose.");
        s.Mod.ExecutionMode = ProcessPhase.Idle; using var probe = new Probe(); s.Stack.AddModification(probe); probe.Action = () => throw new ApplicationException("Physical pose failure."); Reject<ApplicationException>(() => s.Rig.ExecuteModifications(.01, ProcessPhase.Idle)); Check(s.A.Position == Vector2.Zero, "Failed later modifier restores authored pose."); probe.Enabled = false; s.Mod.Enabled = false; s.Rig.ExecuteModifications(.01, ProcessPhase.Idle); Check(s.A.Position == Vector2.Zero, "Failed physical request does not leak through disable.");
        s.Mod.Enabled = true; s.P.ProcessMode = ProcessMode.Disabled; Check(!s.P.IsSimulatingPhysics() && s.P.SimulatePhysics, "Inherited Remove disable retains request but removes backend."); s.P.ProcessMode = ProcessMode.Inherit; s.Step(); Check(s.P.IsSimulatingPhysics(), "Reenabled inherited participation resumes."); s.P.Freeze = false; s.P.CanSleep = true; s.P.Sleeping = true; Check(s.P.Sleeping && s.P.IsSimulatingPhysics(), "Sleeping retains active request.");
        var rig = new Skeleton(); rig.AddChild(Bone("A", Vector2.Zero)); var body = new Hook { Name = "Hook", BoneNodePath = "../A", SimulatePhysics = true, GravityScale = 0, CanSleep = false }; using var shape = new CircleShape(); body.AddChild(new CollisionShape { Shape = shape }); rig.AddChild(body); using var tree = new SceneTree(rig); tree.PhysicsFrame(.01); Check(body.Called, "Inherited force integration callback executes.");
    }
    private static void Storage()
    {
        using var s = new Setup(true); ((CollisionShape)s.Q.GetChild(0)).Position = new(20, 0); s.Mod.StartSimulation(); s.P.Freeze = true; foreach (var node in new Node[] { s.A, s.B, s.P, s.Q, s.P.GetChild(0), s.Q.GetChild(0), s.Q.GetJoint()! }) node.Owner = s.Rig;
        using var packed = new PackedScene(); packed.Pack(s.Rig); var path = IOPath.Combine(IOPath.GetTempPath(), "e2d-physical-" + Guid.NewGuid().ToString("N") + ".e2dscene"); try { ResourceSaver.Save(packed, path); RunChild(path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(PhysicalBoneTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_PHYSICAL_BONE_HOST"); start.Environment["ELECTRON2D_TEST_PHYSICAL_BONE_CHILD"] = path; using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Physical bone child."); } Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh physical scene passed"), error.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var rig = (Skeleton)packed.Instantiate(); using var tree = new SceneTree(rig); var p = rig.GetNode<PhysicalBone>("P"); var q = rig.GetNode<PhysicalBone>("P/Q"); var mod = (SkeletonModificationPhysicalBones)rig.GetModificationStack()!.GetModification(0)!; Check(p.Freeze && p.SimulatePhysics && q.SimulatePhysics && mod.GetPhysicalBoneNode(1) == "P/Q" && q.GetJoint() is PinJoint, "Fresh exact node/resource/joint factories and settings."); q.GravityScale = 1; for (var i = 0; i < 12; i++) { tree.PhysicsFrame(1d / 60); tree.ProcessFrame(1d / 60); }
        Check(Math.Abs(q.GlobalRotation) > .01f && rig.GetBone(1).GlobalPosition.DistanceTo(q.GlobalPosition) < .01f, "Fresh actual physics to bone consumer executes."); Console.WriteLine("Fresh physical scene passed.");
    }
    private static void Warm()
    {
        using var s = new Setup(true); s.Mod.StartSimulation(); s.P.Freeze = true; ((CollisionShape)s.Q.GetChild(0)).Position = new(20, 0); s.Q.GravityScale = 1; var skin = new Polygon { Name = "Skin", Skeleton = "../Rig", Vertices = [new(40, -4), new(80, -4), new(80, 4), new(40, 4)] }; skin.AddBone("A/B", [1, 1, 1, 1]); s.Root.AddChild(skin); var vertices = new List<CanvasVertex>(128); var batches = new List<CanvasBatch>(4); void Step() { s.Step(); skin.PrepareCanvas(); vertices.Clear(); batches.Clear(); skin.AppendCanvas(vertices, batches, Transform.Identity); }
        for (var i = 0; i < 40; i++) Step(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Step(); var bytes = GC.GetAllocatedBytesForCurrentThread() - before; Check(bytes == 0 && vertices.Count == 6, "128 prepared physics/pose/weighted Polygon steps: " + bytes + " managed bytes.");
    }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_PHYSICAL_BONE_RENDERER") ?? "gpu"; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var previousFPS = Engine.MaxFPS; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); Engine.MaxFPS = 120;
        using var shape = new CircleShape { Radius = 4 }; using var mod = new SkeletonModificationPhysicalBones { PhysicalBoneChainLength = 1 }; mod.SetPhysicalBoneNode(0, "P"); using var stack = new SkeletonModificationStack { Enabled = true }; stack.AddModification(mod);
        try
        {
            var window = new Window { Size = new(240, 140) }; var rig = new Skeleton { Name = "Rig", Position = new(40, 45) }; var bone = Bone("A", Vector2.Zero); rig.AddChild(bone); var p = new PhysicalBone { Name = "P", BoneNodePath = "../A", GravityScale = 0, CanSleep = false, SimulatePhysics = true }; p.AddChild(new CollisionShape { Shape = shape }); rig.AddChild(p); rig.SetModificationStack(stack); window.AddChild(rig); var skin = new Polygon { Skeleton = "../Rig", Position = rig.Position, Vertices = [new(0, -4), new(40, -4), new(40, 4), new(0, 4)], Color = new(.1f, .7f, 1) }; skin.AddBone("A", [1, 1, 1, 1]); window.AddChild(skin);
            var frame = 0; var prepared = -1; var captured = false; long before = 0, bytes = 0;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(new(.04f, .05f, .08f)); p.LinearVelocity = new(60, 0);
                RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread();
                RenderingServer.FramePostDraw += () =>
                {
                    var after = GC.GetAllocatedBytesForCurrentThread(); if (prepared >= 64) bytes += after - before;
                    if (!captured && p.GlobalPosition.X >= 70)
                    {
                        using var image = RenderingServer.Service!.Readback(); Check(image.GetPixel((int)p.GlobalPosition.X + 15, 45).B > .7f && image.GetPixel(45, 45).B < .3f && p.GlobalPosition.DistanceTo(bone.GlobalPosition) < .01f, "Actual scheduled solver translation changes weighted skin pixels."); if (Environment.GetEnvironmentVariable("ELECTRON2D_PHYSICAL_BONE_CAPTURE") is { } path) image.SavePNG(path); captured = true; prepared = 0;
                    }
                    if (prepared >= 0) { p.LinearVelocity = new((prepared & 1) == 0 ? 15 : -15, 0); if (prepared >= 64) bytes += GC.GetAllocatedBytesForCurrentThread() - after; if (++prepared == 128) window.Tree!.Quit(); }
                    if (++frame > 2000) throw new TimeoutException("Scheduled physical bone did not produce rendered motion.");
                };
            };
            Check(Engine.Run(window) == 0 && captured && prepared == 128 && bytes == 0, "64 native prepared physical skin intervals: " + bytes); Console.WriteLine("64 native prepared physical skin intervals: " + bytes + " managed bytes; backend=" + backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); Engine.MaxFPS = previousFPS; }
    }
    private static void Near(Vector2 a, Vector2 b, float tolerance, string text) => Check(a.DistanceTo(b) <= tolerance, text + " actual=" + a + " expected=" + b);
    private static void Near(float a, float b, float tolerance, string text) => Check(Math.Abs(a - b) <= tolerance, text + " actual=" + a + " expected=" + b);
    private static void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
