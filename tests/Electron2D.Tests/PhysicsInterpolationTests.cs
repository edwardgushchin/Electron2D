using Electron2D;
using IOPath = System.IO.Path;

internal static class PhysicsInterpolationTests
{
    internal static void Run()
    {
        var settings = ProjectSettings.Service;
        Check(ProjectSettings.PhysicsInterpolation.Name == "physics/common/physics_interpolation" &&
              !ProjectSettings.PhysicsInterpolation.DefaultValue &&
              ProjectSettings.HasSetting(ProjectSettings.PhysicsInterpolation) &&
              (int)PhysicsInterpolationMode.Inherit == 0 &&
              (int)PhysicsInterpolationMode.On == 1 && (int)PhysicsInterpolationMode.Off == 2,
            "The typed setting and mode identities retain their pinned defaults and values.");
        ProjectSettingRoundTrip();
        var previous = ProjectSettings.Get(ProjectSettings.PhysicsInterpolation);
        try
        {
            ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true);
            NodePolicyAndCanvas();
            ControlDefaultAndOptIn();
            CameraCanvas();
            FailureAndPacking();
        }
        finally { ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, previous); }
        Console.WriteLine("Physics interpolation policy, canvas, camera and reset checks passed.");
    }

    private static void NodePolicyAndCanvas()
    {
        using var root = new Node { Name = "Root" };
        var mover = new MovingEntity { Name = "Mover", PhysicsProcessEnabled = true };
        var control = new Control { Name = "Control" };
        var child = new Entity { Name = "Child" };
        root.AddChild(mover); root.AddChild(control); control.AddChild(child);
        using var tree = new SceneTree(root);
        Check(tree.PhysicsInterpolation && root.IsPhysicsInterpolatedAndEnabled() &&
              mover.PhysicsInterpolationMode == PhysicsInterpolationMode.Inherit &&
              control.PhysicsInterpolationMode == PhysicsInterpolationMode.Off &&
              !control.IsPhysicsInterpolated() && !child.IsPhysicsInterpolated(),
            "A tree samples the project setting; ordinary nodes inherit On and controls default Off.");
        child.PhysicsInterpolationMode = PhysicsInterpolationMode.On;
        Check(child.IsPhysicsInterpolatedAndEnabled(), "An explicit child On overrides the parent Control Off.");
        tree.PhysicsFrame(1d / 60);
        Check(mover.Position.X == 10 && mover.GetInterpolatedVisualTransform(.5f).Origin.X == 10,
            $"A first physics tick initializes presentation history without drawing from identity (logical={mover.Position.X}, presented={mover.GetInterpolatedVisualTransform(.5f).Origin.X}).");
        tree.PhysicsFrame(1d / 60);
        Check(mover.Position.X == 20 && mover.GetGlobalTransform().Origin.X == 20 &&
              mover.GetInterpolatedVisualTransform(.5f).Origin.X == 15 &&
              mover.GetInterpolatedVisualTransform(0).Origin.X == 10 &&
              mover.GetInterpolatedVisualTransform(1).Origin.X == 20,
            "A second tick interpolates only presentation while logical geometry remains current.");
        tree.Paused = true;
        Check(mover.GetInterpolatedVisualTransform(.5f).Origin.X == 20 && mover.ResetCount > 0,
            "Pausing resets the displayed physics pose.");
        tree.Paused = false;
        var beforeReset = mover.ResetCount;
        mover.ResetPhysicsInterpolation();
        Check(mover.GetInterpolatedVisualTransform(.5f).Origin.X == 20 && mover.ResetCount == beforeReset + 1,
            "An explicit reset removes the historical pose before presentation.");
        tree.PhysicsFrame(1d / 60);
        mover.Position = new(60, 0);
        Check(mover.GetInterpolatedVisualTransform(.5f).Origin.X == 60,
            "A process-time edit presents immediately instead of replaying a stale physics pose.");
        tree.PhysicsInterpolation = false;
        Check(!mover.IsPhysicsInterpolatedAndEnabled() && mover.GetInterpolatedVisualTransform(.5f).Origin.X == 60,
            "Disabling the tree returns logical presentation immediately.");
        tree.PhysicsInterpolation = true;
        Check(mover.GetInterpolatedVisualTransform(.5f).Origin.X == 60,
            "Re-enabling the tree resets interpolation history.");
        Reject<ArgumentOutOfRangeException>(() => mover.PhysicsInterpolationMode = (PhysicsInterpolationMode)99);
        Check(mover.PhysicsInterpolationMode == PhysicsInterpolationMode.Inherit,
            "Invalid modes reject before state mutation.");
        Exception? wrongThread = null;
        Task.Run(() =>
        {
            try { mover.PhysicsInterpolationMode = PhysicsInterpolationMode.Off; }
            catch (Exception error) { wrongThread = error; }
        }).GetAwaiter().GetResult();
        Check(wrongThread is InvalidOperationException,
            "Attached interpolation policy follows the scene owner thread.");
        for (var index = 0; index < 32; index++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 128; index++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() == before,
            "Warmed active physics snapshots allocate no managed memory on the owner thread.");
    }

    private static void ProjectSettingRoundTrip()
    {
        var root = IOPath.Combine(IOPath.GetTempPath(), "electron2d-interpolation-" + Guid.NewGuid().ToString("N"));
        var project = IOPath.Combine(root, "project");
        var user = IOPath.Combine(root, "user");
        Directory.CreateDirectory(project); Directory.CreateDirectory(user);
        try
        {
            using (var saved = new ProjectSettingsRegistry(project, user))
            {
                Check(!saved.Get(ProjectSettings.PhysicsInterpolation), "An isolated project defaults interpolation to false.");
                saved.Set(ProjectSettings.PhysicsInterpolation, true);
                saved.Save();
            }
            using var loaded = new ProjectSettingsRegistry(project, user);
            loaded.Load();
            Check(loaded.Get(ProjectSettings.PhysicsInterpolation),
                "The typed interpolation setting survives a project-file round trip.");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void CameraCanvas()
    {
        using var viewport = new TestViewport();
        var camera = new Camera
        {
            Name = "Camera",
            AnchorMode = AnchorMode.FixedTopLeft,
            LimitEnabled = false
        };
        viewport.AddChild(camera);
        using var tree = new SceneTree(viewport);
        tree.PhysicsFrameStarted += _ => camera.Position += new Vector2(10, 0);
        tree.PhysicsFrame(1d / 60);
        tree.PhysicsFrame(1d / 60);
        Check(viewport.CanvasTransform.Origin.X == -20 &&
              viewport.GetInterpolatedCanvasTransform(.5f).Origin.X == -15,
            "Physics-driven camera presentation interpolates the viewport canvas without changing logical input coordinates.");
        camera.ResetPhysicsInterpolation();
        Check(viewport.GetInterpolatedCanvasTransform(.5f).Origin.X == -20,
            "Camera reset also resets its viewport presentation history.");
        Check(camera.ProcessCallback == ProcessPhase.Idle,
            "An inherited interpolated camera keeps its configured idle mode while physics drives presentation snapshots.");
        camera.PhysicsInterpolationMode = PhysicsInterpolationMode.Off;
        Check(viewport.GetInterpolatedCanvasTransform(.5f) == viewport.CanvasTransform,
            "Disabling camera interpolation returns to immediate idle camera presentation.");
    }

    private static void ControlDefaultAndOptIn()
    {
        using var viewport = new TestViewport();
        var control = new MovingControl { Name = "MovingControl", Size = new(10, 10), PhysicsProcessEnabled = true };
        viewport.AddChild(control);
        using var tree = new SceneTree(viewport);
        tree.PhysicsFrame(1d / 60);
        tree.PhysicsFrame(1d / 60);
        Check(control.Position.X == 20 && control.GetInterpolatedVisualTransform(.5f).Origin.X == 20,
            "A Control defaults to current-pose presentation while its policy is Off.");
        control.PhysicsInterpolationMode = PhysicsInterpolationMode.On;
        tree.PhysicsFrame(1d / 60);
        Check(control.Position.X == 30 && control.GetInterpolatedVisualTransform(.5f).Origin.X == 25,
            "An opt-in Control interpolates its visual pose without changing logical layout.");
    }

    private static void FailureAndPacking()
    {
        using var source = new Entity { Name = "Source", PhysicsInterpolationMode = PhysicsInterpolationMode.Off };
        using var packed = new PackedScene();
        packed.Pack(source);
        using var copy = packed.Instantiate();
        Check(copy.PhysicsInterpolationMode == PhysicsInterpolationMode.Off,
            "Packed scenes retain the typed node interpolation policy.");
        using var sourceControl = new Control { Name = "Control" };
        using var packedControl = new PackedScene();
        packedControl.Pack(sourceControl);
        using var controlCopy = packedControl.Instantiate();
        Check(controlCopy.PhysicsInterpolationMode == PhysicsInterpolationMode.Off,
            "A packed Control reconstructs its Off default without an explicit override.");
        using var root = new Node { Name = "Root" };
        var failing = new FailingResetNode { Name = "Failing" };
        var later = new FailingResetNode { Name = "Later" };
        root.AddChild(failing); root.AddChild(later);
        using var tree = new SceneTree(root);
        failing.ThrowOnReset = true;
        Reject<AggregateException>(root.ResetPhysicsInterpolation);
        Check(failing.ResetCount == 1 && later.ResetCount == 1,
            "A failing reset notification still reaches later descendants.");
    }

    private sealed class MovingEntity : Entity
    {
        internal int ResetCount;
        protected override void OnPhysicsProcess(double delta) => Position += new Vector2(10, 0);
        protected override void OnNotification(int what)
        {
            if (what == NotificationResetPhysicsInterpolation) ResetCount++;
            base.OnNotification(what);
        }
    }

    private sealed class FailingResetNode : Node
    {
        internal int ResetCount;
        internal bool ThrowOnReset;
        protected override void OnNotification(int what)
        {
            if (what == NotificationResetPhysicsInterpolation)
            {
                ResetCount++;
                if (ThrowOnReset) throw new ApplicationException("reset failed");
            }
            base.OnNotification(what);
        }
    }

    private sealed class MovingControl : Control
    {
        protected override void OnPhysicsProcess(double delta) => Position += new Vector2(10, 0);
    }

    private sealed class TestViewport : Viewport
    {
        public override Rect2 GetVisibleRect() => new(0, 0, 100, 80);
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
