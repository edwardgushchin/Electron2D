using Electron2D;
using System.Diagnostics;
using IOPath = System.IO.Path;

internal static class SkeletonIKTests
{
    private static Bone MakeBone(string name, Vector2 position, float length = 30) { var bone = new Bone { Name = name, Position = position, Rest = new Transform(0, position) }; bone.SetAutocalculateLengthAndAngle(false); bone.SetLength(length); return bone; }
    private sealed class Setup : IDisposable
    {
        internal readonly Entity Root = new();
        internal readonly Skeleton Rig = new() { Name = "Rig" };
        internal readonly Bone A = MakeBone("A", Vector2.Zero), B = MakeBone("B", new(30, 0));
        internal readonly Entity Tip = new() { Name = "Tip", Position = new(30, 0) }, Target = new() { Name = "Target", Position = new(30, 30) };
        internal readonly SkeletonModificationStack Stack = new() { Enabled = true };
        internal readonly SceneTree Tree;
        internal Setup(SkeletonModification modification) { Rig.AddChild(A); A.AddChild(B); B.AddChild(Tip); Root.AddChild(Rig); Root.AddChild(Target); Stack.AddModification(modification); Rig.SetModificationStack(Stack); Tree = new(Root); }
        internal void Step() => Rig.ExecuteModifications(.01, ProcessPhase.Idle);
        public void Dispose() { Tree.Dispose(); Stack.Dispose(); Root.Dispose(); }
    }
    private static SkeletonModificationTwoBoneIK Two() { var m = new SkeletonModificationTwoBoneIK { TargetNodePath = "../Target" }; m.SetJointOneBoneNode("A"); m.SetJointTwoBoneNode("A/B"); return m; }
    private static SkeletonModificationCCDIK CCD() { var m = new SkeletonModificationCCDIK { CCDIKDataChainLength = 2, TargetNodePath = "../Target", TipNodePath = "A/B/Tip" }; m.SetCCDIKJointBoneNode(0, "A/B"); m.SetCCDIKJointBoneNode(1, "A"); return m; }
    private static SkeletonModificationFABRIK FAB() { var m = new SkeletonModificationFABRIK { FABRIKDataChainLength = 2, TargetNodePath = "../Target" }; m.SetFABRIKJointBoneNode(0, "A"); m.SetFABRIKJointBoneNode(1, "A/B"); return m; }
    internal static void Run() { TwoBone(); CCDIK(); FABRIK(); Storage(); Failures(); DeferredIndices(); Warm(); Console.WriteLine("IK law-of-cosines, CCD, FABRIK, storage and prepared checks passed."); }
    private static void TwoBone()
    {
        using var mod = Two(); using var setup = new Setup(mod); setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .001f, "Two-bone reaches reachable target."); var bend = setup.B.GlobalPosition; mod.FlipBendDirection = true; setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .001f, "Flipped limb reaches target."); Check(setup.B.GlobalPosition.DistanceTo(bend) > 30, "Bend side changes."); mod.FlipBendDirection = false;
        setup.Target.Position = new(100, 0); setup.Step(); Near(setup.Tip.GlobalPosition, new(60, 0), .001f, "Unreachable limb extends."); setup.Target.Position = Vector2.Zero; setup.Step(); Near(setup.Tip.GlobalPosition, Vector2.Zero, .001f, "Coincident equal-length target folds finitely.");
        mod.TargetMinimumDistance = 20; setup.Step(); Near(setup.Tip.GlobalPosition.DistanceTo(setup.A.GlobalPosition), 20, .001f, "Minimum solve distance."); mod.TargetMinimumDistance = 0; mod.TargetMaximumDistance = 15; setup.Target.Position = new(50, 0); setup.Step(); Near(setup.Tip.GlobalPosition.DistanceTo(setup.A.GlobalPosition), 15, .001f, "Maximum solve distance."); mod.TargetMaximumDistance = 0;
        setup.Target.Position = new(30, 30); setup.Stack.Strength = .5f; setup.Step(); Near(setup.B.Rotation, Mathf.Pi / 4, .0001f, "Stack strength blends the solved local pose."); setup.Stack.Strength = 1; mod.ExecutionMode = ProcessPhase.Physics; setup.Rig.ExecuteModifications(.01, ProcessPhase.Physics); Check(setup.B.Rotation == 0, "Physics IK preserves the authored pose."); setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .001f, "Idle applies physics-staged IK."); mod.ExecutionMode = ProcessPhase.Idle;
        setup.A.SetLength(40); setup.B.Position = new(40, 0); setup.B.SetLength(20); setup.Tip.Position = new(20, 0); setup.Target.Position = Vector2.Zero; setup.Step(); Near(setup.Tip.GlobalPosition.DistanceTo(setup.A.GlobalPosition), 20, .001f, "Inside inner reach folds to closest endpoint."); setup.A.SetLength(0); setup.Step(); Check(setup.A.Transform == setup.A.AuthoredPose && setup.B.Transform == setup.B.AuthoredPose, "Zero length leaves authored pose."); setup.A.SetLength(30); setup.B.SetLength(30); setup.B.Position = new(30, 0); setup.Tip.Position = new(30, 0);
        setup.Rig.Scale = new(-2, 2); setup.Target.Position = new(-60, 60); setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .002f, "Reflected uniform basis reaches target."); setup.Rig.Scale = Vector2.One; setup.Target.Position = new(30, 30); setup.A.SetBoneAngle(Mathf.Pi / 2); setup.B.Position = new(0, 30); setup.B.SetBoneAngle(Mathf.Pi / 2); setup.Tip.Position = new(0, 30); setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .002f, "Bone endpoint angles participate.");
        setup.Target.Name = "Renamed"; setup.Step(); Check(setup.A.Transform == setup.A.AuthoredPose, "Rename invalidates target."); mod.TargetNodePath = "."; setup.Step(); Check(setup.A.Transform == setup.A.AuthoredPose, "Skeleton itself is not a target binding."); mod.TargetNodePath = "../Renamed"; setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .002f, "Target path reconnects."); mod.SetJointOneBoneIndex(0); Check(mod.GetJointOneBoneNode() == "A" && mod.GetJointOneBoneIndex() == 0, "Bound numeric selection authors a path."); Reject<ArgumentOutOfRangeException>(() => mod.SetJointTwoBoneIndex(2)); Reject<ArgumentOutOfRangeException>(() => mod.SetJointOneBoneIndex(-1)); Reject<ArgumentOutOfRangeException>(() => mod.TargetMaximumDistance = -1); Reject<ArgumentOutOfRangeException>(() => mod.TargetMinimumDistance = float.NaN); var indexDescriptor = (PropertyDescriptor<SkeletonModificationTwoBoneIK, int>)mod.GetPropertyList().First(p => p.Name == "_joint_one_index"); Reject<ArgumentOutOfRangeException>(() => indexDescriptor.SetValue(mod, -2));
    }
    private static void CCDIK()
    {
        using var mod = CCD(); using var setup = new Setup(mod); setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .001f, "Authored reverse-order CCD pass reaches target."); mod.SetCCDIKJointRotateFromJoint(0, true); mod.SetCCDIKJointEnableConstraint(1, true); mod.SetCCDIKJointConstraintAngleMin(1, 0); mod.SetCCDIKJointConstraintAngleMax(1, .1f); setup.Step(); Check(setup.A.Rotation >= 0 && setup.A.Rotation <= .10001f, "Local CCD limits.");
        mod.SetCCDIKJointConstraintAngleInvert(1, true); setup.Step(); Check(float.IsFinite(setup.A.Rotation), "Inverted interval remains finite."); mod.SetCCDIKJointConstraintAngleInvert(1, false); mod.SetCCDIKJointConstraintInLocalSpace(1, false); setup.Rig.Rotation = .2f; setup.Step(); Check(setup.A.GlobalRotation >= -.0001f && setup.A.GlobalRotation <= .1001f, "Global CCD limits.");
        mod.SetCCDIKJointEnableConstraint(1, false); mod.SetCCDIKJointRotateFromJoint(0, false); setup.Rig.Rotation = 0; mod.TipNodePath = "Missing"; setup.Step(); Check(setup.A.Rotation == 0 && setup.B.Rotation == 0, "Missing explicit tip skips pass."); mod.TipNodePath = "A/B/Tip"; setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .001f, "Tip path reconnects."); mod.SetCCDIKJointBoneIndex(0, 1); Check(mod.GetCCDIKJointBoneNode(0) == "A/B" && mod.GetCCDIKJointBoneIndex(0) == 1, "Joint index/path query.");
        Reject<ArgumentOutOfRangeException>(() => mod.GetCCDIKJointRotateFromJoint(2)); Reject<ArgumentOutOfRangeException>(() => mod.SetCCDIKJointConstraintAngleMax(1, float.PositiveInfinity)); Reject<ArgumentOutOfRangeException>(() => mod.CCDIKDataChainLength = 4097); using var copy = (SkeletonModificationCCDIK)mod.Duplicate(); Check(copy.CCDIKDataChainLength == 2 && !copy.GetCCDIKJointConstraintInLocalSpace(1) && !copy.GetIsSetup(), "Copied hidden CCD coordinate policy without live binding.");
        mod.CCDIKDataChainLength = 3; Check(mod.GetCCDIKJointBoneIndex(2) == -1 && mod.GetCCDIKJointConstraintAngleMax(2) == Mathf.Tau, "New CCD slot defaults."); mod.CCDIKDataChainLength = 2;
    }
    private static void FABRIK()
    {
        using var mod = FAB(); using var setup = new Setup(mod); setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .02f, "FABRIK reaches target."); Near(setup.A.GlobalPosition, Vector2.Zero, .0001f, "Root anchored."); Near(setup.B.GlobalPosition.DistanceTo(setup.A.GlobalPosition), 30, .001f, "Segment length retained.");
        var elbow = setup.B.GlobalPosition; mod.SetFABRIKJointMagnetPosition(1, new(-50, 30)); setup.Step(); Check(setup.B.GlobalPosition.DistanceTo(elbow) > 20 && setup.A.Transform.IsFinite(), "Magnet changes the actual bend solution."); mod.SetFABRIKJointMagnetPosition(1, Vector2.Zero); setup.Target.Rotation = Mathf.Pi / 2; mod.SetFABRIKJointUseTargetRotation(1, true); setup.Step(); Near(setup.B.GlobalRotation, setup.Target.GlobalRotation, .0001f, "Final target orientation."); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .02f, "Orientation-constrained endpoint.");
        mod.SetFABRIKJointUseTargetRotation(1, false); setup.Target.Position = new(200, 0); setup.Step(); Check(setup.Tip.GlobalPosition.X > 59.9f && setup.A.Transform.IsFinite(), "Unreachable FABRIK approaches closest finite reach."); setup.Target.Position = Vector2.Zero; setup.Step(); Check(setup.A.Transform.IsFinite() && setup.B.Transform.IsFinite(), "Coincident/folded FABRIK finite."); setup.B.Position = Vector2.Zero; setup.Step(); Check(setup.A.Transform.IsFinite() && setup.B.Transform.IsFinite(), "Coincident joint seeds use fallback direction."); setup.B.Position = new(30, 0);
        setup.Rig.Scale = new(-2, 2); setup.Target.Position = new(-60, 60); setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .04f, "FABRIK reflected uniform scale."); setup.Rig.Scale = Vector2.One; var third = MakeBone("C", new(30, 0)); setup.B.AddChild(third); setup.Tip.Reparent(third, false); setup.Tip.Position = new(30, 0); mod.FABRIKDataChainLength = 3; mod.SetFABRIKJointBoneNode(2, "A/B/C"); setup.Target.Position = new(50, 35); setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .04f, "Multi-joint iterative reach."); setup.Tip.Reparent(setup.B, false); setup.Tip.Position = new(30, 0); setup.B.RemoveChild(third); third.Dispose(); mod.FABRIKDataChainLength = 2;
        mod.SetFABRIKJointBoneIndex(0, 0); Check(mod.GetFABRIKJointBoneNode(0) == "A", "FABRIK numeric selection authors path."); mod.SetFABRIKJointBoneNode(1, "A"); setup.Step(); Check(setup.A.Transform == setup.A.AuthoredPose, "Duplicate joints skip full solve."); mod.SetFABRIKJointBoneNode(1, "A/B"); setup.B.SetLength(0); setup.Step(); Check(setup.A.Transform == setup.A.AuthoredPose, "Nonpositive segment skips full solve.");
        Reject<ArgumentException>(() => mod.SetFABRIKJointMagnetPosition(0, new(float.NaN, 0))); Reject<ArgumentOutOfRangeException>(() => mod.SetFABRIKJointBoneIndex(2, 0)); using var copy = (SkeletonModificationFABRIK)mod.Duplicate(); Check(copy.FABRIKDataChainLength == 2 && copy.GetFABRIKJointMagnetPosition(1) == Vector2.Zero && !copy.GetIsSetup(), "FABRIK copy independent.");
        var stored = (PropertyDescriptor<SkeletonModificationFABRIK, byte[]>)mod.GetPropertyList().First(p => p.Name == "_joints"); Reject<InvalidDataException>(() => stored.SetValue(mod, new byte[8])); Check(mod.FABRIKDataChainLength == 2, "Malformed joint archive preserves configuration.");
    }
    private sealed class ThrowAfterRequest : SkeletonModification
    {
        protected override void OnExecute(double delta) { var rig = GetModificationStack()!.GetSkeleton()!; rig.SetBoneLocalPoseOverride(0, new Transform(.7f, Vector2.Zero), 1, false); throw new ApplicationException("Injected after override request."); }
    }
    private static void Failures()
    {
        using var mod = Two(); using var setup = new Setup(mod); using var fail = new ThrowAfterRequest(); setup.Stack.AddModification(fail); Reject<ApplicationException>(setup.Step); setup.Stack.Enabled = false; setup.Step(); Check(setup.A.Rotation == 0 && setup.B.Rotation == 0, "Failed modifier pass does not leak staged overrides into disabled execution."); setup.Rig.SetBoneLocalPoseOverride(0, new Transform(.2f, Vector2.Zero), 1, true); setup.Stack.Enabled = true; Reject<ApplicationException>(setup.Step); setup.Stack.Enabled = false; setup.Step(); Near(setup.A.Rotation, .2f, .0001f, "Failure preserves the prior authored persistent request.");
    }
    private static void Storage()
    {
        using var two = Two(); using var ccd = CCD(); using var fab = FAB(); using var setup = new Setup(two); setup.Stack.AddModification(ccd); setup.Stack.AddModification(fab); ccd.Enabled = false; fab.Enabled = false; ccd.SetCCDIKJointConstraintInLocalSpace(0, false); fab.SetFABRIKJointMagnetPosition(1, new(2, -3));
        foreach (var child in new Node[] { setup.A, setup.B, setup.Tip }) child.Owner = setup.Rig; setup.Target.Reparent(setup.Rig); setup.Target.Owner = setup.Rig; two.TargetNodePath = ccd.TargetNodePath = fab.TargetNodePath = "Target";
        using var packed = new PackedScene(); packed.Pack(setup.Rig); var path = IOPath.Combine(IOPath.GetTempPath(), "e2d-ik-" + Guid.NewGuid().ToString("N") + ".e2dscene"); try { ResourceSaver.Save(packed, path); RunChild(path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(SkeletonIKTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_IK_CHILD"] = path; using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("IK child."); } Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh IK scene passed"), error.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var rig = (Skeleton)packed.Instantiate(); using var tree = new SceneTree(rig); rig.ExecuteModifications(.01, ProcessPhase.Idle); var stack = rig.GetModificationStack()!; Check(stack.GetModification(0) is SkeletonModificationTwoBoneIK && stack.GetModification(1) is SkeletonModificationCCDIK c && !c.GetCCDIKJointConstraintInLocalSpace(0) && stack.GetModification(2) is SkeletonModificationFABRIK f && f.GetFABRIKJointMagnetPosition(1) == new Vector2(2, -3), "Fresh exact factories and joint policies."); Near(rig.GetNode<Entity>("A/B/Tip").GlobalPosition, rig.GetNode<Entity>("Target").GlobalPosition, .002f, "Fresh two-bone execution."); Console.WriteLine("Fresh IK scene passed.");
    }
    private static void DeferredIndices()
    {
        using var two = new SkeletonModificationTwoBoneIK { TargetNodePath = "../Target" }; two.SetJointOneBoneIndex(0); two.SetJointTwoBoneIndex(1); using (var setup = new Setup(two)) { setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .001f, "Detached numeric two-bone selection binds."); Exception? error = null; var worker = new Thread(() => { try { two.TargetMinimumDistance = 1; } catch (Exception caught) { error = caught; } }); worker.Start(); worker.Join(); Check(error is InvalidOperationException, "Bound IK configuration rejects another thread."); }
        using var ccd = new SkeletonModificationCCDIK { CCDIKDataChainLength = 2, TargetNodePath = "../Target", TipNodePath = "A/B/Tip" }; ccd.SetCCDIKJointBoneIndex(0, 1); ccd.SetCCDIKJointBoneIndex(1, 0); using (var setup = new Setup(ccd)) { setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .001f, "Detached numeric CCD selection binds."); }
        using var fab = new SkeletonModificationFABRIK { FABRIKDataChainLength = 2, TargetNodePath = "../Target" }; fab.SetFABRIKJointBoneIndex(0, 0); fab.SetFABRIKJointBoneIndex(1, 1); using (var setup = new Setup(fab)) { setup.Step(); Near(setup.Tip.GlobalPosition, setup.Target.GlobalPosition, .02f, "Detached numeric FABRIK selection binds."); }
    }
    private static void Warm()
    {
        foreach (var kind in new[] { 0, 1, 2 })
        {
            using SkeletonModification mod = kind == 0 ? Two() : kind == 1 ? CCD() : FAB(); using var setup = new Setup(mod); for (var i = 0; i < 24; i++) setup.Step(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) { setup.Target.Position = new(30, 25 + (i & 1) * 5); setup.Step(); }
            Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Prepared IK " + kind + " allocates zero managed bytes.");
        }
    }
    internal static void RunHost()
    {
        Run(); var renderer = Environment.GetEnvironmentVariable("ELECTRON2D_IK_RENDERER") ?? "gpu"; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
        try
        {
            var window = new Window { Size = new(340, 180) }; var mods = new SkeletonModification[3]; var stacks = new SkeletonModificationStack[3]; var targets = new Entity[3];
            for (var i = 0; i < 3; i++)
            {
                var rig = new Skeleton { Name = "Rig" + i, Position = new(30 + i * 100, 65) }; var a = MakeBone("A", Vector2.Zero); var b = MakeBone("B", new(30, 0)); rig.AddChild(a); a.AddChild(b); b.AddChild(new Entity { Name = "Tip", Position = new(30, 0) }); window.AddChild(rig); targets[i] = new Entity { Name = "Target" + i, Position = rig.Position + new Vector2(30, 30) }; window.AddChild(targets[i]);
                mods[i] = i == 0 ? Two() : i == 1 ? CCD() : FAB(); if (mods[i] is SkeletonModificationTwoBoneIK t) t.TargetNodePath = "../Target" + i; if (mods[i] is SkeletonModificationCCDIK c) c.TargetNodePath = "../Target" + i; if (mods[i] is SkeletonModificationFABRIK f) f.TargetNodePath = "../Target" + i; stacks[i] = new() { Enabled = true }; stacks[i].AddModification(mods[i]); rig.SetModificationStack(stacks[i]);
                var body = new Polygon { Name = "Body" + i, Position = rig.Position, Skeleton = "../Rig" + i, Vertices = [new(0, -5), new(30, -5), new(60, -5), new(60, 5), new(30, 5), new(0, 5)], Color = new(.1f, .7f, 1) }; body.AddBone("A", [1, 1, 0, 0, 1, 1]); body.AddBone("A/B", [0, 0, 1, 1, 0, 0]); window.AddChild(body);
            }
            var frame = 0; long before = 0, bytes = 0; window.Ready += _ => { RenderingServer.SetDefaultClearColor(new(.04f, .05f, .08f)); RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread(); RenderingServer.FramePostDraw += () => { var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 64) bytes += after - before; if (frame == 4) { using var image = RenderingServer.Service!.Readback(); for (var i = 0; i < 3; i++) Check(image.GetPixel(58 + i * 100, 90).B > .7f && image.GetPixel(87 + i * 100, 65).B < .3f, "Native IK " + i + " skin reaches bent pixels."); if (Environment.GetEnvironmentVariable("ELECTRON2D_IK_CAPTURE") is { } capture) image.SavePNG(capture); } if (frame >= 32) for (var i = 0; i < 3; i++) targets[i].Position = new(60 + i * 100, 90 + (frame & 1) * 5); if (frame >= 64) bytes += GC.GetAllocatedBytesForCurrentThread() - after; if (++frame == 128) window.Tree!.Quit(); }; };
            try { Check(Engine.Run(window) == 0 && frame == 128 && bytes == 0, "64 native IK intervals: " + bytes); Console.WriteLine("64 native prepared IK intervals: " + bytes + " managed bytes; backend=" + renderer); } finally { foreach (var stack in stacks) stack.Dispose(); foreach (var mod in mods) mod.Dispose(); }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private static void Near(Vector2 a, Vector2 b, float tolerance, string text) => Check(a.DistanceTo(b) <= tolerance, text + " actual=" + a + " expected=" + b);
    private static void Near(float a, float b, float tolerance, string text) => Check(Math.Abs(a - b) <= tolerance, text + " actual=" + a + " expected=" + b);
    private static void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
