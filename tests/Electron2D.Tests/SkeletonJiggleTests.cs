using Electron2D;
using System.Diagnostics;
using IOPath = System.IO.Path;

internal static class SkeletonJiggleTests
{
    private static Bone Bone() { var bone = new Bone { Name = "A", Rest = Transform.Identity }; bone.SetAutocalculateLengthAndAngle(false); bone.SetLength(45); return bone; }
    private static SkeletonModificationJiggle Jiggle() { var mod = new SkeletonModificationJiggle { JiggleDataChainLength = 1, TargetNodePath = "../Target" }; mod.SetJiggleJointBoneNode(0, "A"); return mod; }
    private sealed class Setup : IDisposable
    {
        internal readonly Entity Root = new();
        internal readonly Skeleton Rig = new() { Name = "Rig" };
        internal readonly Bone A = Bone();
        internal readonly Entity Target = new() { Name = "Target", Position = new(30, 0) };
        internal readonly SkeletonModificationStack Stack = new() { Enabled = true };
        internal readonly SceneTree Tree;
        internal Setup(SkeletonModification mod) { Rig.AddChild(A); Root.AddChild(Rig); Root.AddChild(Target); Stack.AddModification(mod); Rig.SetModificationStack(Stack); Tree = new(Root); }
        internal void Step(double delta = .1) => Rig.ExecuteModifications(delta, ProcessPhase.Idle);
        public void Dispose() { Tree.Dispose(); Root.Dispose(); Stack.Dispose(); }
    }
    internal static void Run() { Dynamics(); Settings(); Collision(); Holders(); Failures(); Storage(); DepthBudget(); Warm(); Console.WriteLine("Jiggle simulation/collision and nested stack graph checks passed."); }
    private static void Dynamics()
    {
        using var mod = Jiggle(); using var setup = new Setup(mod); setup.Step(); var state = mod.GetSimulationState(0); Near(state.Force, new(9, 0), .0001f, "Force from target displacement/delta."); Near(state.Velocity, new(3, 0), .0001f, "Mass and damping acceleration."); Near(state.Dynamic, new(12, 0), .0001f, "Dynamic force/velocity advance."); setup.Target.Position = new(0, 30); setup.Step(); Near(setup.A.Rotation, new Vector2(10.2f, 12).Angle(), .0001f, "Actual spring lag rotation.");
        mod.Reset(); state = mod.GetSimulationState(0); Check(state.Dynamic == setup.A.GlobalPosition && state.Velocity == Vector2.Zero && state.Force == Vector2.Zero, "Reset current origins and accumulated terms."); setup.Rig.Position = new(100, 50); mod.Reset(); setup.Target.Position = new(130, 50); setup.Step(); Near(mod.GetSimulationState(0).Dynamic, new(112, 50), .0002f, "Nonzero world origin seeds without startup translation impulse.");
        setup.Rig.Position = new(105, 50); var before = mod.GetSimulationState(0).Dynamic; setup.Step(0); Near(mod.GetSimulationState(0).Dynamic, before + new Vector2(8, 0), .0002f, "Zero delta retains velocity and compensates root translation.");
        setup.Rig.Scale = new(-1, 1); setup.Target.Position = new(75, 80); mod.Reset(); setup.Step(); Near(SkeletonIKBinding.Axis(setup.A).Angle(), new Vector2(-30, 30).Angle(), .0002f, "Reflected endpoint aiming."); mod.Enabled = false; setup.Step(); Check(setup.A.Transform == setup.A.AuthoredPose, "Disable restores authored pose."); mod.Enabled = true; setup.Target.Name = "Moved"; setup.Step(); Check(setup.A.Transform == setup.A.AuthoredPose, "Missing target skips."); mod.TargetNodePath = "../Moved"; setup.Step(); Check(setup.A.Transform.IsFinite(), "Reconnected target resumes finite history.");
        mod.SetJiggleJointBoneIndex(0, 0); Check(mod.GetJiggleJointBoneNode(0) == "A" && mod.GetJiggleJointBoneIndex(0) == 0, "Attached numeric selection authors path."); setup.A.Name = "Renamed"; setup.Step(); Check(mod.GetJiggleJointBoneIndex(0) == -1, "Renamed bone invalidates selection."); mod.SetJiggleJointBoneNode(0, "Renamed"); setup.Step(); Check(mod.GetJiggleJointBoneIndex(0) == 0, "Bone reconnects.");
    }
    private static void Settings()
    {
        using var mod = new SkeletonModificationJiggle { Stiffness = 7, Mass = 2, Damping = .2f, UseGravity = true, Gravity = new(4, 9), JiggleDataChainLength = 2 }; Check(mod.GetJiggleJointStiffness(0) == 7 && mod.GetJiggleJointMass(0) == 2 && mod.GetJiggleJointUseGravity(1), "New joints receive current defaults."); mod.SetJiggleJointOverride(0, true); mod.SetJiggleJointStiffness(0, 11); mod.SetJiggleJointGravity(0, new(1, 2)); mod.Stiffness = 5; Check(mod.GetJiggleJointStiffness(0) == 11 && mod.GetJiggleJointStiffness(1) == 5 && mod.GetJiggleJointGravity(0) == new Vector2(1, 2), "Override survives default propagation."); mod.SetJiggleJointOverride(0, false); Check(mod.GetJiggleJointStiffness(0) == 5 && mod.GetJiggleJointGravity(0) == mod.Gravity, "Override false copies all defaults.");
        Reject<ArgumentOutOfRangeException>(() => mod.Mass = 0); Reject<ArgumentOutOfRangeException>(() => mod.SetJiggleJointMass(0, -1)); Reject<ArgumentOutOfRangeException>(() => mod.Damping = 1.1f); Reject<ArgumentOutOfRangeException>(() => mod.SetJiggleJointStiffness(0, float.NaN)); Reject<ArgumentException>(() => mod.Gravity = new(float.PositiveInfinity, 0)); Reject<ArgumentOutOfRangeException>(() => mod.SetJiggleJointBoneIndex(0, -1)); Reject<ArgumentOutOfRangeException>(() => mod.GetJiggleJointOverride(2)); Reject<ArgumentOutOfRangeException>(() => mod.JiggleDataChainLength = 4097);
        mod.JiggleDataChainLength = 1; Check(mod.GetJiggleJointDamping(0) == .2f, "Resize preserves configuration prefix."); mod.Reset(); Check(mod.GetSimulationState(0).Velocity == Vector2.Zero, "Detached reset is safe.");
        using var gravity = Jiggle(); gravity.Stiffness = 0; gravity.UseGravity = true; gravity.Gravity = new(0, 6); using var setup = new Setup(gravity); setup.Step(); Near(gravity.GetSimulationState(0).Dynamic, new(0, .8f), .0001f, "Gravity contributes actual force.");
        var stored = (PropertyDescriptor<SkeletonModificationJiggle, byte[]>)mod.GetPropertyList().First(p => p.Name == "_joints"); Reject<InvalidDataException>(() => stored.SetValue(mod, new byte[8])); Check(mod.JiggleDataChainLength == 1, "Malformed archive preserves joints.");
    }
    private static void Collision()
    {
        using var mod = Jiggle(); mod.ExecutionMode = ProcessPhase.Physics; mod.SetUseColliders(true); using var setup = new Setup(mod); setup.Target.Position = new(30, 30); using var shape = new RectangleShape { Size = new(100, 2) }; var wall = new StaticBody { Name = "Wall", Position = new(0, 5) }; wall.AddChild(new CollisionShape { Shape = shape }); setup.Root.AddChild(wall);
        setup.Rig.ExecuteModifications(.1, ProcessPhase.Physics); Check(mod.GetSimulationState(0).Dynamic == Vector2.Zero && mod.GetSimulationState(0).Velocity == Vector2.Zero, "Actual world ray hit reverts last clear point and velocity."); setup.Step(); Check(setup.A.Rotation == 0, "Blocked physics override preserves clear pose."); mod.SetCollisionMask(0); setup.Rig.ExecuteModifications(.1, ProcessPhase.Physics); setup.Step(); Near(setup.A.Rotation, Mathf.Pi / 4, .0001f, "Zero mask ignores collider and produces pose."); Check(mod.GetCollisionMask() == 0 && mod.GetUseColliders(), "Collider API roundtrip."); mod.ExecutionMode = ProcessPhase.Idle; var old = mod.GetSimulationState(0).Dynamic; Reject<InvalidOperationException>(() => setup.Step()); Check(mod.GetSimulationState(0).Dynamic == old, "Idle collider misuse rejects before state advances.");
    }
    private static void Holders()
    {
        using var look = new SkeletonModificationLookAt { BoneNode = "A", TargetNodePath = "../Target" }; using var child = new SkeletonModificationStack { Enabled = true, Strength = .5f }; child.AddModification(look); using var holder = new SkeletonModificationStackHolder(); holder.SetHeldModificationStack(child); using var setup = new Setup(holder); setup.Stack.Strength = .1f; setup.Target.Position = new(30, 30); setup.Step(); Near(setup.A.Rotation, Mathf.Pi / 8, .0001f, "Child strength independent of parent."); Check(ReferenceEquals(child.GetSkeleton(), setup.Rig) && holder.GetHeldModificationStack() == child, "Held child actual binding.");
        using var alias = new SkeletonModificationStackHolder(); alias.SetHeldModificationStack(child); setup.Stack.AddModification(alias); setup.Stack.DeleteModification(0); setup.Step(); Near(setup.A.Rotation, Mathf.Pi / 8, .0001f, "Removing one holder preserves shared child."); using var rootProbe = new Probe(); var rootCalls = 0; rootProbe.Action = () => rootCalls++; setup.Stack.AddModification(rootProbe); child.Execute(.01, ProcessPhase.Idle); Check(rootCalls == 0, "Direct child Execute does not enter a root sibling."); setup.Step(); Check(rootCalls == 1, "Root Execute still enters its own sibling."); setup.Stack.DeleteModification(1); Near(setup.A.Rotation, Mathf.Pi / 8, .0001f, "Direct child Execute selects child instead of root graph."); holder.SetHeldModificationStack(null); setup.Stack.DeleteModification(0); Check(child.GetSkeleton() is null && !look.GetIsSetup(), "Final holder removal detaches child.");
        holder.SetHeldModificationStack(child); setup.Stack.AddModification(holder); Reject<InvalidOperationException>(() => holder.SetHeldModificationStack(setup.Stack)); Check(holder.GetHeldModificationStack() == child, "Root cycle rejects before replacement."); using var inner = new SkeletonModificationStackHolder(); using var nested = new SkeletonModificationStack { Enabled = true }; nested.AddModification(inner); Reject<InvalidOperationException>(() => inner.SetHeldModificationStack(nested));
        holder.Enabled = false; setup.Step(); Check(setup.A.Rotation == 0, "Disabled holder skips subtree."); holder.Enabled = true; child.Enabled = false; setup.Step(); Check(setup.A.Rotation == 0, "Disabled child skips list."); child.Enabled = true; holder.ExecutionMode = ProcessPhase.Physics; setup.Rig.ExecuteModifications(.01, ProcessPhase.Physics); setup.Step(); Check(setup.A.Rotation == 0, "Holder and child phases must both match."); look.ExecutionMode = ProcessPhase.Physics; setup.Rig.ExecuteModifications(.01, ProcessPhase.Physics); setup.Step(); Near(setup.A.Rotation, Mathf.Pi / 8, .0001f, "Matching held physics stages an actual child pose."); holder.ExecutionMode = look.ExecutionMode = ProcessPhase.Idle;
        using var copy = (SkeletonModificationStack)setup.Stack.Duplicate(); var copiedHolder = (SkeletonModificationStackHolder)copy.GetModification(0)!; Check(!ReferenceEquals(copiedHolder.GetHeldModificationStack(), child) && !copiedHolder.GetHeldModificationStack()!.GetIsSetup(), "Always duplicate child graph without live binding.");
        setup.Rig.SetModificationStack(null); Check(child.GetSkeleton() is null, "Rig detachment propagates child lifecycle.");
    }
    private sealed class Probe : SkeletonModification
    {
        internal Action? SetupAction, Action;
        protected override void OnSetupModification(SkeletonModificationStack stack) => SetupAction?.Invoke();
        protected override void OnExecute(double delta) => Action?.Invoke();
    }
    private static void Failures()
    {
        using var jiggle = Jiggle(); using var setup = new Setup(jiggle); var before = jiggle.GetSimulationState(0); jiggle.Stiffness = float.MaxValue; setup.Target.Position = new(float.MaxValue, float.MaxValue); Reject<InvalidOperationException>(() => setup.Step(100)); Check(jiggle.GetSimulationState(0) == before && setup.A.Rotation == 0, "Overflow preserves last valid joint state and authored pose.");
        using var probe = new Probe(); using var child = new SkeletonModificationStack { Enabled = true }; child.AddModification(probe); using var holder = new SkeletonModificationStackHolder(); setup.Stack.SetModification(0, holder); probe.SetupAction = () => throw new ApplicationException("Injected nested setup failure."); Reject<AggregateException>(() => holder.SetHeldModificationStack(child)); Check(!setup.Stack.GetIsSetup() && holder.GetHeldModificationStack() == child, "Failed child setup retains assignment and invalidates parent for retry."); probe.SetupAction = null; setup.Stack.Setup(); probe.Action = () => holder.SetHeldModificationStack(null); Reject<InvalidOperationException>(() => setup.Step()); Check(holder.GetHeldModificationStack() == child, "Child replacement during execution rejects."); probe.Action = () => child.Dispose(); Reject<InvalidOperationException>(() => setup.Step()); Check(!child.IsDisposed, "Running child disposal rejects before terminal state."); probe.Action = () => probe.Dispose(); Reject<InvalidOperationException>(() => setup.Step()); Check(!probe.IsDisposed, "Running modifier disposal rejects before terminal state."); probe.Action = null; setup.Step(); holder.SetHeldModificationStack(null); Check(child.GetSkeleton() is null, "Failed/retried attachment releases correctly.");
    }
    private static void Storage()
    {
        using var jiggle = Jiggle(); using var child = new SkeletonModificationStack { Enabled = true, Strength = .4f }; child.AddModification(jiggle); using var first = new SkeletonModificationStackHolder(); using var second = new SkeletonModificationStackHolder(); first.SetHeldModificationStack(child); second.SetHeldModificationStack(child); using var setup = new Setup(first); setup.Stack.AddModification(second); jiggle.SetJiggleJointOverride(0, true); jiggle.SetJiggleJointMass(0, 1.25f); jiggle.SetJiggleJointGravity(0, new(1, 8)); setup.A.Owner = setup.Rig; setup.Target.Reparent(setup.Rig); setup.Target.Owner = setup.Rig; jiggle.TargetNodePath = "Target";
        using var packed = new PackedScene(); packed.Pack(setup.Rig); var path = IOPath.Combine(IOPath.GetTempPath(), "e2d-jiggle-" + Guid.NewGuid().ToString("N") + ".e2dscene"); try { ResourceSaver.Save(packed, path); RunChild(path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(SkeletonJiggleTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_JIGGLE_CHILD"] = path; using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Jiggle child."); } Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh Jiggle scene passed"), error.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var rig = (Skeleton)packed.Instantiate(); using var tree = new SceneTree(rig); var root = rig.GetModificationStack()!; var first = (SkeletonModificationStackHolder)root.GetModification(0)!; var second = (SkeletonModificationStackHolder)root.GetModification(1)!; var child = first.GetHeldModificationStack()!; var jiggle = (SkeletonModificationJiggle)child.GetModification(0)!; Check(ReferenceEquals(child, second.GetHeldModificationStack()) && jiggle.GetJiggleJointMass(0) == 1.25f && jiggle.GetJiggleJointGravity(0) == new Vector2(1, 8) && child.Strength == .4f, "Fresh factories/settings preserve child aliases."); rig.ExecuteModifications(.1, ProcessPhase.Idle); Check(jiggle.GetSimulationState(0).Dynamic.X > 0 && ReferenceEquals(child.GetSkeleton(), rig), "Fresh nested simulation actually executes."); Console.WriteLine("Fresh Jiggle scene passed.");
    }
    private static void DepthBudget()
    {
        var stacks = new List<SkeletonModificationStack>(); var holders = new List<SkeletonModificationStackHolder>();
        try
        {
            var child = new SkeletonModificationStack(); stacks.Add(child);
            for (var i = 0; i < 129; i++) { var holder = new SkeletonModificationStackHolder(); holders.Add(holder); var parent = new SkeletonModificationStack(); stacks.Add(parent); if (i == 128) { Reject<InvalidOperationException>(() => holder.SetHeldModificationStack(child)); break; } holder.SetHeldModificationStack(child); parent.AddModification(holder); child = parent; }
        }
        finally { foreach (var stack in stacks) stack.Dispose(); foreach (var holder in holders) holder.Dispose(); }
    }
    private static void Warm()
    {
        using var jiggle = Jiggle(); using var child = new SkeletonModificationStack { Enabled = true }; child.AddModification(jiggle); using var holder = new SkeletonModificationStackHolder(); holder.SetHeldModificationStack(child); using var setup = new Setup(holder); for (var i = 0; i < 32; i++) setup.Step(.01); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) { setup.Target.Position = new(30, 20 + (i & 1) * 10); setup.Step(.01); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "128 prepared nested Jiggle simulations allocate zero managed bytes.");
    }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_JIGGLE_RENDERER") ?? "gpu"; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        using var jiggle = Jiggle(); using var aim = new SkeletonModificationLookAt { BoneNode = "A", TargetNodePath = "../Target1" }; using var collider = Jiggle(); collider.ExecutionMode = ProcessPhase.Physics; collider.SetUseColliders(true); using var child = new SkeletonModificationStack { Enabled = true, Strength = .5f }; child.AddModification(aim); using var holder = new SkeletonModificationStackHolder(); holder.SetHeldModificationStack(child); using var wallShape = new RectangleShape { Size = new(80, 2) };
        var stacks = new SkeletonModificationStack[3];
        try
        {
            var window = new Window { Size = new(360, 180) }; var rigs = new Skeleton[3]; var targets = new Entity[3]; var mods = new SkeletonModification[] { jiggle, holder, collider };
            for (var i = 0; i < 3; i++)
            {
                rigs[i] = new Skeleton { Name = "Rig" + i, Position = new(40 + i * 110, 55) }; rigs[i].AddChild(Bone()); window.AddChild(rigs[i]); targets[i] = new Entity { Name = "Target" + i, Position = rigs[i].Position + new Vector2(30, 30) }; window.AddChild(targets[i]); if (mods[i] is SkeletonModificationJiggle j) j.TargetNodePath = "../Target" + i; stacks[i] = new() { Enabled = true }; stacks[i].AddModification(mods[i]); rigs[i].SetModificationStack(stacks[i]); var polygon = new Polygon { Name = "Body" + i, Position = rigs[i].Position, Skeleton = "../Rig" + i, Vertices = [new(0, -4), new(45, -4), new(45, 4), new(0, 4)], Color = new(.1f, .7f, 1) }; polygon.AddBone("A", [1, 1, 1, 1]); window.AddChild(polygon);
            }
            var wall = new StaticBody { Name = "Wall", Position = rigs[2].Position + new Vector2(0, 2) }; wall.AddChild(new CollisionShape { Shape = wallShape }); window.AddChild(wall);
            var frame = 0; long before = 0, bytes = 0; window.Ready += _ => { RenderingServer.SetDefaultClearColor(new(.04f, .05f, .08f)); rigs[2].ExecuteModifications(.1, ProcessPhase.Physics); rigs[2].ExecuteModifications(.01, ProcessPhase.Idle); RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread(); RenderingServer.FramePostDraw += () => { var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 64) bytes += after - before; if (frame == 4) { using var image = RenderingServer.Service!.Readback(); Check(image.GetPixel(61, 76).B > .7f && image.GetPixel(83, 55).B < .3f, "Native Jiggle changes skin pixels."); Check(image.GetPixel(178, 66).B > .7f && image.GetPixel(193, 55).B < .3f, "Native held stack strength changes skin pixels."); Check(collider.GetSimulationState(0).Dynamic == rigs[2].GlobalPosition && image.GetPixel(290, 55).B > .7f, "Native physics ray blocks candidate and publishes clear pose."); if (Environment.GetEnvironmentVariable("ELECTRON2D_JIGGLE_CAPTURE") is { } path) image.SavePNG(path); } rigs[2].ExecuteModifications(.1, ProcessPhase.Physics); rigs[2].ExecuteModifications(.01, ProcessPhase.Idle); if (frame >= 32) targets[0].Position = rigs[0].Position + new Vector2(30, 20 + (frame & 1) * 10); if (frame >= 64) bytes += GC.GetAllocatedBytesForCurrentThread() - after; if (++frame == 128) window.Tree!.Quit(); }; };
            Check(Engine.Run(window) == 0 && frame == 128 && bytes == 0, "64 native prepared Jiggle/collision/nested intervals: " + bytes); Console.WriteLine("64 native prepared Jiggle/collision/nested intervals: " + bytes + " managed bytes; backend=" + backend);
        }
        finally { foreach (var stack in stacks) stack?.Dispose(); ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private static void Near(Vector2 a, Vector2 b, float tolerance, string message) => Check(a.DistanceTo(b) <= tolerance, message + " actual=" + a + " expected=" + b);
    private static void Near(float a, float b, float tolerance, string message) => Check(Math.Abs(a - b) <= tolerance, message + " actual=" + a + " expected=" + b);
    private static void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
