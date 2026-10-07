using Electron2D;
using System.Diagnostics;
using IOPath = System.IO.Path;

internal static class SkeletonTests
{
    private static Bone Bone(string name, Vector2 point) { var b = new Bone { Name = name, Rest = new Transform(0, point), Position = point }; b.SetAutocalculateLengthAndAngle(false); return b; }
    private static Polygon Body(string path = "../Rig") => new() { Name = "Body", Skeleton = path, Vertices = [new(0, -5), new(40, -5), new(40, 5), new(0, 5)], Color = new(.1f, .7f, 1) };
    internal static void Run() { Hierarchy(); Overrides(); Skinning(); Interpolation(); AnimationIntegration(); Modifications(); Storage(); Failures(); Warm(); Console.WriteLine("Skeleton/bone/weighted polygon/modification storage checks passed."); }
    private static void Hierarchy()
    {
        using var root = new Entity(); var rig = new Skeleton { Name = "Rig" }; var a = new Bone { Name = "A", Rest = Transform.Identity }; var b = Bone("B", new(20, 10)); var c = Bone("C", new(2, 0)); rig.AddChild(a); a.AddChild(b); rig.AddChild(c); root.AddChild(rig);
        Check(rig.GetBoneCount() == 0 && a.GetIndexInSkeleton() == -1, "Detached indices/count."); using var tree = new SceneTree(root);
        Check(rig.GetBoneCount() == 3 && ReferenceEquals(rig.GetBone(1), b) && a.GetIndexInSkeleton() == 0 && c.GetIndexInSkeleton() == 2, "Attached depth-first order."); Near(a.GetLength(), MathF.Sqrt(500), .001f, "Automatic first-child length."); Near(a.GetBoneAngle(), new Vector2(20, 10).Angle(), .0001f, "Automatic endpoint angle."); Check(b.GetSkeletonRest() == b.Rest, "Accumulated rest chain.");
        var events = 0; rig.BoneSetupChanged += () => events++; rig.MoveChild(c, 0); Check(ReferenceEquals(rig.GetBone(0), c) && b.GetIndexInSkeleton() == 2 && events == 1, "Reorder updates indices and commits setup once."); a.RemoveChild(b); Check(rig.GetBoneCount() == 2 && b.GetIndexInSkeleton() == -1, "Removal drops palette membership."); b.Dispose();
        var neutral = new Node { Name = "Neutral" }; var orphan = Bone("Orphan", Vector2.Zero); neutral.AddChild(orphan); rig.AddChild(neutral); Check(rig.GetBoneCount() == 2 && orphan.GetIndexInSkeleton() == -1, "Neutral nodes break bone chains.");
        var rid = rig.GetSkeleton(); Check(rid.IsValid() && rig.GetSkeleton() == rid && ReferenceEquals(RenderingSkeletonRegistry.Resolve(rid), rig), "Stable weak palette RID.");
        a.SetLength(-3); a.SetBoneAngle(.4f); a.ApplyRest(); Check(a.Transform == a.Rest && a.GetLength() == -3 && a.GetBoneAngle() == .4f, "Authored endpoint values and ApplyRest."); Reject<ArgumentException>(() => a.Rest = new Transform(new(float.NaN, 0), Vector2.Down, Vector2.Zero)); Reject<ArgumentOutOfRangeException>(() => a.SetLength(float.PositiveInfinity));
    }
    private static void Overrides()
    {
        using var root = new Node(); var rig = new Skeleton(); var bone = Bone("A", Vector2.Zero); rig.AddChild(bone); root.AddChild(rig); using var tree = new SceneTree(root);
        rig.SetBoneLocalPoseOverride(0, new Transform(0, new(10, 0)), .5f, true); tree.ProcessFrame(.01); Near(bone.Position.X, 5, .0001f, "Override executes without a stack."); tree.ProcessFrame(.01); Near(bone.Position.X, 5, .0001f, "Persistent override remains.");
        rig.SetBoneLocalPoseOverride(0, new Transform(0, new(20, 0)), .5f, false); tree.ProcessFrame(.01); Near(bone.Position.X, 10, .0001f, "Transient override applies once."); tree.ProcessFrame(.01); Check(bone.Position == Vector2.Zero, "Transient override restores authored pose.");
        rig.SetBoneLocalPoseOverride(0, new Transform(.5f, Vector2.Zero), 1, false); rig.ExecuteModifications(.01, ProcessPhase.Physics); Check(bone.Rotation == 0, "Physics preserves authored pose while staging."); rig.ExecuteModifications(.01, ProcessPhase.Idle); Near(bone.Rotation, .5f, .0001f, "Idle consumes staged override.");
        Reject<ArgumentOutOfRangeException>(() => rig.SetBoneLocalPoseOverride(0, Transform.Identity, 2, true)); Reject<ArgumentOutOfRangeException>(() => rig.ExecuteModifications(double.NaN, ProcessPhase.Idle));
    }
    private static void Skinning()
    {
        using var root = new Entity(); var rig = new Skeleton { Name = "Rig" }; var a = Bone("A", Vector2.Zero); var b = Bone("B", new(20, 0)); a.AddChild(b); rig.AddChild(a); root.AddChild(rig); var polygon = Body(); polygon.AddBone("A", [1, 0, 0, 1]); polygon.AddBone("A/B", [0, 1, 1, 0]); root.AddChild(polygon); using var tree = new SceneTree(root);
        var vertices = new List<CanvasVertex>(128); var batches = new List<CanvasBatch>(4); void Replay() { polygon.PrepareCanvas(); vertices.Clear(); batches.Clear(); polygon.AppendCanvas(vertices, batches, Transform.Identity); }
        Replay(); Check(vertices.Count == 6 && vertices.Any(v => v.Position == new Vector2(40, 5)), "Rest geometry."); b.Rotation = Mathf.Pi / 2; Replay(); Check(vertices.Any(v => v.Position.DistanceTo(new(15, 20)) < .001f), "Live child pose bends retained vertices without redraw.");
        polygon.Polygons = [[3, 2, 1, 0]]; Replay(); Check(vertices.Any(v => v.Position.DistanceTo(new(15, 20)) < .001f), "Indexed contours retain original weight indices."); polygon.Polygons = [];
        polygon.AddBone("Ignored", [float.MaxValue]); Check(polygon.GetBonePath(2) == "Ignored", "Authored missing/mismatched record remains queryable."); polygon.EraseBone(2); Reject<ArgumentOutOfRangeException>(() => polygon.EraseBone(2));
        var stored = (PropertyDescriptor<Polygon, byte[]>)polygon.GetPropertyList().First(p => p.Name == "_bone_weights"); Reject<InvalidDataException>(() => stored.SetValue(polygon, new byte[8])); Check(polygon.GetBoneCount() == 2, "Malformed skin archive preserves records.");
        var weights = polygon.GetBoneWeights(1); weights[1] = 0; Check(polygon.GetBoneWeights(1)[1] == 1, "Copied weights."); Reject<ArgumentException>(() => polygon.SetBoneWeights(1, [float.NaN])); Check(polygon.GetBoneWeights(1).Length == 4, "Invalid weights preserve state.");
        polygon.SetBonePath(1, "Missing"); Replay(); Check(vertices.Any(v => v.Position == new Vector2(40, 5)), "Missing bone leaves unpainted points unchanged."); polygon.SetBonePath(1, "A/B"); b.Name = "Renamed"; Replay(); Check(vertices.Any(v => v.Position == new Vector2(40, 5)), "Renamed paths invalidate prepared bindings."); polygon.SetBonePath(1, "A/Renamed"); Replay(); Check(vertices.Any(v => v.Position.DistanceTo(new(15, 20)) < .001f), "Retarget reconnects.");
        polygon.ClearBones(); for (var i = 0; i < 5; i++) { var extra = Bone("E" + i, Vector2.Zero); extra.Position = new(i * 10, 0); rig.AddChild(extra); polygon.AddBone("E" + i, Enumerable.Repeat((float)(i + 1), 4).ToArray()); }
        Replay(); var min = vertices.Min(v => v.Position.X); Near(min, (10 * 2 + 20 * 3 + 30 * 4 + 40 * 5) / 14f, .001f, "Four strongest normalized influences.");
        polygon.InvertEnabled = true; Replay(); Check(vertices.Count > 6, "Inversion remains an unskinned ordinary contour."); polygon.InvertEnabled = false; polygon.ClearBones(); Replay(); Check(vertices.Any(v => v.Position == new Vector2(40, 5)), "Cleared skin returns original points.");
    }
    private sealed class MovingBone : Bone
    {
        protected override void OnPhysicsProcess(double delta) => Position += new Vector2(10, 0);
    }
    private static void Interpolation()
    {
        using var root = new Entity(); var rig = new Skeleton { Name = "Rig" }; var bone = new MovingBone { Name = "A", Rest = Transform.Identity, PhysicsProcessEnabled = true }; bone.SetAutocalculateLengthAndAngle(false); rig.AddChild(bone); root.AddChild(rig); var polygon = Body(); polygon.AddBone("A", [1, 1, 1, 1]); root.AddChild(polygon); using var tree = new SceneTree(root); tree.PhysicsInterpolation = true; tree.PhysicsFrame(.01); tree.PhysicsFrame(.01); Near(bone.GetInterpolatedVisualTransform(.5f).Origin.X, 15, .0001f, "Bone presentation uses physics history."); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); polygon.PrepareCanvas(); polygon.AppendCanvas(vertices, batches, Transform.Identity); var expected = 10 + 10 * (float)Engine.PhysicsInterpolationFraction; Near(vertices.Min(v => v.Position.X), expected, .0001f, "Skin palette uses the actual presentation fraction.");
    }
    private static void AnimationIntegration()
    {
        using var root = new Entity(); var rig = new Skeleton { Name = "Rig", Position = new(30, 40), Rotation = .3f, Scale = new(2, 1) }; var bone = Bone("A", Vector2.Zero); rig.AddChild(bone); root.AddChild(rig); var polygon = Body(); polygon.Position = new(10, 20); polygon.AddBone("A", [1, 1, 1, 1]); root.AddChild(polygon);
        var rotation = new PropertyDescriptor<Bone, float>(nameof(Entity.Rotation), b => b.Rotation, (b, v) => b.Rotation = v);
        using var clip = new Animation { Length = 1 }; var track = clip.AddBezierTrack(rotation); clip.TrackSetPath(track, "Rig/A:Rotation"); clip.BezierTrackInsertKey(track, 0, 0, outHandle: new(1f / 3, 0)); clip.BezierTrackInsertKey(track, 1, .8f, inHandle: new(-1f / 3, 0)); using var library = new AnimationLibrary(); library.AddAnimation("bend", clip); var player = new AnimationPlayer { Name = "Player" }; player.AddAnimationLibrary("", library); root.AddChild(player); using var tree = new SceneTree(root); player.Play("bend"); player.Advance(.5); Near(bone.Rotation, .4f, .0001f, "AnimationPlayer binds inherited Bone rotation."); rig.ExecuteModifications(.01, ProcessPhase.Idle); Near(bone.Rotation, .4f, .0001f, "Skeleton retains the animated authored pose.");
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); polygon.PrepareCanvas(); polygon.AppendCanvas(vertices, batches, Transform.Identity); var expected = polygon.GetGlobalTransform().AffineInverse() * rig.GetGlobalTransform() * bone.Transform * rig.GetGlobalTransform().AffineInverse() * polygon.GetGlobalTransform() * new Vector2(40, 5); Check(vertices.Any(v => v.Position.DistanceTo(expected) < .001f), "Noncommuting skeleton/polygon transforms preserve bind coordinates."); bone.TopLevel = true; vertices.Clear(); batches.Clear(); polygon.AppendCanvas(vertices, batches, Transform.Identity); expected = polygon.GetGlobalTransform().AffineInverse() * bone.GetGlobalTransform() * rig.GetGlobalTransform().AffineInverse() * polygon.GetGlobalTransform() * new Vector2(40, 5); Check(vertices.Any(v => v.Position.DistanceTo(expected) < .001f), "Top-level bone respects its actual canvas basis.");
    }
    private static void Modifications()
    {
        using var root = new Entity(); var rig = new Skeleton { Name = "Rig" }; var a = Bone("A", Vector2.Zero); rig.AddChild(a); root.AddChild(rig); var target = new Entity { Name = "Target", Position = new(30, 30) }; root.AddChild(target);
        using var look = new SkeletonModificationLookAt { BoneNode = "A", TargetNodePath = "../Target" }; using var stack = new SkeletonModificationStack { Enabled = true }; stack.AddModification(look); rig.SetModificationStack(stack); using var tree = new SceneTree(root);
        Check(stack.GetIsSetup() && look.GetIsSetup() && ReferenceEquals(look.GetModificationStack(), stack), "Concrete setup."); tree.ProcessFrame(.01); Near(a.Rotation, Mathf.Pi / 4, .0001f, "Actual look-at pose."); Near(a.AuthoredPose.X.Angle(), 0, .0001f, "Modified pose does not overwrite animation state."); stack.Strength = .5f; tree.ProcessFrame(.01); Near(a.Rotation, Mathf.Pi / 8, .0001f, "Stack strength blends the final pose.");
        look.Enabled = false; tree.ProcessFrame(.01); Near(a.Rotation, 0, .0001f, "Disabled modification restores the authored pose."); look.Enabled = true; stack.Strength = 1; look.SetEnableConstraint(true); look.SetConstraintAngleMin(0); look.SetConstraintAngleMax(.2f); tree.ProcessFrame(.01); Near(a.Rotation, .2f, .0001f, "Local angle constraints.");
        Near(look.ClampAngle(Mathf.Tau * 3 + .1f, 0, .2f, false), .1f, .00001f, "Multiple-turn angles normalize."); look.SetEnableConstraint(false); target.Name = "Gone"; tree.ProcessFrame(.01); Near(a.Rotation, 0, .0001f, "Stale target path is skipped."); look.TargetNodePath = "../Gone"; tree.ProcessFrame(.01); Near(a.Rotation, Mathf.Pi / 4, .0001f, "New target path reconnects immediately.");
        using var copy = (SkeletonModificationStack)stack.Duplicate(); Check(!ReferenceEquals(copy.GetModification(0), look) && !copy.GetIsSetup() && copy.Enabled && copy.Strength == 1, "Stack always duplicates its modification graph without scene binding.");
        stack.ModificationCount = 3; Check(stack.GetModification(2) is null, "Null slots."); stack.SetModification(2, look); stack.DeleteModification(0); Check(ReferenceEquals(stack.GetModification(1), look), "Removal preserves a still-aliased modification."); stack.EnableAllModifications(false); Check(!look.Enabled, "Enable all controls actual modifiers.");
    }
    private sealed class ProbeModification : SkeletonModification
    {
        internal Action? Action, SetupAction;
        protected override void OnSetupModification(SkeletonModificationStack stack) => SetupAction?.Invoke();
        protected override void OnExecute(double delta) => Action?.Invoke();
    }
    private static void Failures()
    {
        using var root = new Node(); var rig = new Skeleton(); var bone = Bone("A", Vector2.Zero); rig.AddChild(bone); root.AddChild(rig); using var probe = new ProbeModification(); using var stack = new SkeletonModificationStack { Enabled = true }; stack.AddModification(probe); rig.SetModificationStack(stack); using var tree = new SceneTree(root);
        probe.Action = () => { bone.Rotation = .7f; throw new ApplicationException("Injected modifier failure."); }; Reject<AggregateException>(() => tree.ProcessFrame(.01)); Near(bone.Rotation, 0, .0001f, "Modifier failure restores authored pose."); probe.Action = () => stack.AddModification(probe); Reject<InvalidOperationException>(() => rig.ExecuteModifications(.01, ProcessPhase.Idle)); Check(stack.ModificationCount == 1, "Structural reentry rejects before append."); probe.Action = null; tree.ProcessFrame(.01);
        using var setupProbe = new ProbeModification(); setupProbe.SetupAction = () => stack.AddModification(probe); Reject<InvalidOperationException>(() => stack.AddModification(setupProbe)); Check(!stack.GetIsSetup() && stack.ModificationCount == 2, "Failed setup commits one slot and blocks callback restructuring."); setupProbe.SetupAction = null; stack.Setup(); tree.ProcessFrame(.01); Check(stack.GetIsSetup(), "Setup retries after callback failure.");
        Exception? offOwner = null; var thread = new Thread(() => { try { bone.Rest = Transform.Identity; } catch (Exception error) { offOwner = error; } }); thread.Start(); thread.Join(); Check(offOwner is InvalidOperationException, "Scene owner guards.");
        using var second = new Skeleton { Name = "Second" }; Reject<InvalidOperationException>(() => second.SetModificationStack(stack)); Check(second.GetModificationStack() is null, "Live stack ownership rejects hijacking."); using var otherStack = new SkeletonModificationStack(); root.AddChild(second); second.SetModificationStack(otherStack); Reject<InvalidOperationException>(() => otherStack.AddModification(probe)); Check(otherStack.ModificationCount == 0 && ReferenceEquals(probe.GetModificationStack(), stack), "Live modification binding cannot be stolen by another stack.");
        rig.SetModificationStack(null); Check(!probe.GetIsSetup() && probe.GetModificationStack() is null, "Detachment clears modifier setup/binding."); rig.SetModificationStack(stack); stack.Dispose(); Check(!probe.GetIsSetup() && probe.GetModificationStack() is null, "Disposed stack clears borrowed live modifiers even after its disposed flag commits."); rig.SetModificationStack(null); tree.ProcessFrame(.01);
    }
    private static void Storage()
    {
        using var rig = new Skeleton { Name = "Rig" }; var a = Bone("A", Vector2.Zero); rig.AddChild(a); a.Owner = rig; var polygon = Body(".."); polygon.AddBone("A", [1, 1, 1, 1]); rig.AddChild(polygon); polygon.Owner = rig; var target = new Entity { Name = "Target", Position = new(50, 50) }; rig.AddChild(target); target.Owner = rig;
        using var look = new SkeletonModificationLookAt { BoneNode = "A", TargetNodePath = "Target" }; using var stack = new SkeletonModificationStack { Enabled = true }; stack.AddModification(look); rig.SetModificationStack(stack);
        using var packed = new PackedScene(); packed.Pack(rig); using var first = (Skeleton)packed.Instantiate(); using var second = (Skeleton)packed.Instantiate(); Check(!ReferenceEquals(first.GetModificationStack(), stack) && !ReferenceEquals(first.GetModificationStack(), second.GetModificationStack()), "Node stack descriptors force unique scene copies with owned lifetime.");
        var path = IOPath.Combine(IOPath.GetTempPath(), "e2d-skeleton-" + Guid.NewGuid().ToString("N") + ".e2dscene"); try { ResourceSaver.Save(packed, path); RunChild(path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(SkeletonTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_SKELETON_CHILD"] = path; using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Skeleton child."); } Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh skeleton scene passed"), error.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var rig = (Skeleton)packed.Instantiate(); using var tree = new SceneTree(rig); tree.ProcessFrame(.01); Check(rig.GetBoneCount() == 1 && rig.GetBone(0).Rest == Transform.Identity && rig.GetModificationStack()!.GetModification(0) is SkeletonModificationLookAt && rig.GetNode<Polygon>("Body").GetBoneWeights(0).Length == 4, "Fresh exact factories/configuration."); Near(rig.GetBone(0).Rotation, Mathf.Pi / 4, .0001f, "Fresh actual modification execution."); Console.WriteLine("Fresh skeleton scene passed.");
    }
    private static void Warm()
    {
        using var root = new Entity(); var rig = new Skeleton { Name = "Rig" }; var bone = Bone("A", Vector2.Zero); rig.AddChild(bone); root.AddChild(rig); var polygon = Body(); polygon.AddBone("A", [1, 1, 1, 1]); root.AddChild(polygon); using var tree = new SceneTree(root);
        var vertices = new List<CanvasVertex>(128); var batches = new List<CanvasBatch>(4); void Cycle(int i) { bone.Rotation = (i & 1) * .2f; polygon.QueueRedraw(); polygon.PrepareCanvas(); vertices.Clear(); batches.Clear(); polygon.AppendCanvas(vertices, batches, Transform.Identity); }
        for (var i = 0; i < 32; i++) Cycle(i); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Cycle(i); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "128 warmed pose/record/skin/replay iterations allocate zero bytes.");
    }
    internal static void RunHost()
    {
        Run(); var method = Environment.GetEnvironmentVariable("ELECTRON2D_SKELETON_RENDERER") ?? "gpu"; var old = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, method);
        try
        {
            var window = new Window { Size = new(260, 190) }; var rig = new Skeleton { Name = "Rig", Position = new(60, 60) }; var a = Bone("A", Vector2.Zero); var b = Bone("B", new(20, 0)); rig.AddChild(a); a.AddChild(b); window.AddChild(rig); var polygon = Body(); polygon.Position = rig.Position; polygon.AddBone("A", [1, 0, 0, 1]); polygon.AddBone("A/B", [0, 1, 1, 0]); window.AddChild(polygon);
            var frame = 0; long before = 0, allocations = 0;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(new(.04f, .05f, .08f)); Reject<InvalidOperationException>(() => RenderingServer.FreeRID(rig.GetSkeleton())); RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread(); RenderingServer.FramePostDraw += () =>
            {
                var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 64) allocations += after - before;
                if (frame == 2) { b.Rotation = Mathf.Pi / 2; }
                if (frame == 4) { using var image = RenderingServer.Service!.Readback(); Check(image.GetPixel(79, 76).B > .7f && image.GetPixel(98, 60).B < .3f, "Actual child deformation reaches native pixels."); if (Environment.GetEnvironmentVariable("ELECTRON2D_SKELETON_CAPTURE") is { } path) image.SavePNG(path); }
                if (frame >= 32) { b.Rotation = (frame & 1) == 0 ? .5f : .7f; polygon.QueueRedraw(); }
                if (frame >= 64) allocations += GC.GetAllocatedBytesForCurrentThread() - after; if (++frame == 128) window.Tree!.Quit();
            };
            };
            Check(Engine.Run(window) == 0 && frame == 128 && allocations == 0, "64 native prepared skeleton frames: " + allocations); Console.WriteLine("64 native prepared skeleton intervals: " + allocations + " managed bytes; backend=" + method);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, old); }
    }
    private static void Near(float a, float b, float tolerance, string message) => Check(Math.Abs(a - b) <= tolerance, message + " actual=" + a + " expected=" + b);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
