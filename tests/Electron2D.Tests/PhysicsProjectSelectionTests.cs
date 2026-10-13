using Electron2D;
using IOPath = System.IO.Path;
using static PhysicsDebugTests;

internal static partial class PhysicsProjectSelectionTests
{
    internal static void Run(bool gpuAvailable)
    {
        var engine = ProjectSettings.Get(ProjectSettings.Physics2DEngine);
        var fallback = ProjectSettings.Get(ProjectSettings.Physics2DAllowCPUFallback);
        try
        {
            Check(ProjectSettings.HasSetting(ProjectSettings.Physics2DEngine) && ProjectSettings.Physics2DEngine.DefaultValue == "DEFAULT" &&
                ProjectSettings.HasSetting(ProjectSettings.Physics2DAllowCPUFallback) && !ProjectSettings.Physics2DAllowCPUFallback.DefaultValue,
                "Project physics selection and fallback are registered typed settings with their intended defaults");
            RoundTrip();
            FeatureSelection();
            Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(ProjectSettings.Physics2DEngine, " "));
            ProjectSettings.Set(ProjectSettings.Physics2DAllowCPUFallback, false);
            VerifyImplicit("CPU", PhysicsServer.Backend.CPU);
            if (gpuAvailable) VerifyImplicit("GPU", PhysicsServer.Backend.GPU);
            CaptureAndReset();
            if (!gpuAvailable) UnavailableGPU();
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DEngine, engine); ProjectSettings.Set(ProjectSettings.Physics2DAllowCPUFallback, fallback); }
        Console.WriteLine($"Project physics: settings, implicit viewport/scene, immutable capture/reset, explicit choices and {(gpuAvailable ? "CPU/GPU" : "denied GPU/fallback")} passed.");
    }

    private static void VerifyImplicit(string name, PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DEngine, name);
        foreach (var viewportRoot in new[] { false, true })
        {
            using var root = viewportRoot ? (Node)new SubViewport() : new Node();
            using var shape = new CircleShape { Radius = 1 };
            var body = new RigidBody
            {
                GravityScale = 0,
                CanSleep = false,
                LinearVelocity = new(120, 0),
                LinearDamp = 0,
                LinearDampMode = RigidBody.DampMode.Replace
            };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
            using var tree = new SceneTree(root); var world = body.GetWorld()!;
            Check(world.RequestedPhysicsBackend == backend && world.RequestedPhysicsBackendName == name && world.PhysicsBackend == backend &&
                world.PhysicsBackendName == name && world.PhysicsFallbackReason is null, "Implicit worlds execute the project implementation without fallback");
            tree.PhysicsFrame(1d / 60); Check(MathF.Abs(body.Position.X - 2) < .001f, "Project-selected numerical owner produces real motion");
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false); GC.WaitForPendingFinalizers();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Project-selected {name} viewport={viewportRoot} steps allocated {own}/{all} B");
            Console.WriteLine($"Project-selected {name} viewport={viewportRoot}: 128 full frames {own}/{all} B.");
        }
    }

    private static void CaptureAndReset()
    {
        ProjectSettings.Set(ProjectSettings.Physics2DEngine, "CPU");
        using var view = new SubViewport(); var captured = view.World!; using var alias = (World)captured.Duplicate();
        ProjectSettings.Set(ProjectSettings.Physics2DEngine, "GPU");
        Check(captured.RequestedPhysicsBackendName == "CPU" && alias.Space == captured.Space && captured.PhysicsBackendName == "CPU",
            "Changing project settings before lazy startup preserves the captured runtime and aliases");
        using var next = new SubViewport(); Check(next.World!.RequestedPhysicsBackendName == "GPU", "A later implicit world captures the changed selection lazily");
        using var caller = new World(); using var explicitCPU = new World(PhysicsServer.Backend.CPU);
        var raw = PhysicsServer.SpaceCreate();
        try
        {
            Check(caller.PhysicsBackend == PhysicsServer.Backend.CPU && explicitCPU.PhysicsBackend == PhysicsServer.Backend.CPU &&
            PhysicsServer.SpaceGetBackend(raw) == PhysicsServer.Backend.CPU, "Caller parameterless and explicit CPU creation ignore the project GPU default");
        }
        finally { PhysicsServer.FreeRID(raw); }
        var canvas = captured.Canvas;
        ProjectSettings.Set(ProjectSettings.Physics2DEngine, "UnknownProjectProvider");
        Reject<ArgumentException>(() => view.World = null);
        Check(view.World!.Canvas == canvas, "Unknown project selection rejects reset before changing an existing world");
        using var invalid = new SubViewport(); Reject<ArgumentException>(() => _ = invalid.World);
        ProjectSettings.Set(ProjectSettings.Physics2DEngine, "CPU"); view.World = null;
        Check(view.World!.Canvas != canvas && view.World.RequestedPhysicsBackendName == "CPU", "Null reset creates an independent newly selected default");
        using var reentry = new SubViewport();
        using (var tree = new SceneTree(new Node()))
        {
            tree.Root.AddChild(reentry); _ = reentry.FindWorld()!.Canvas; tree.Root.RemoveChild(reentry);
        }
        ProjectSettings.Set(ProjectSettings.Physics2DEngine, "GPU");
        Check(reentry.FindWorld()!.RequestedPhysicsBackendName == "GPU", "Expired scene-owned defaults recapture project selection on reentry");
    }

    private static void UnavailableGPU()
    {
        ProjectSettings.Set(ProjectSettings.Physics2DEngine, "GPU"); ProjectSettings.Set(ProjectSettings.Physics2DAllowCPUFallback, false);
        using var strict = new SubViewport(); var world = strict.World!; var rejected = false;
        try { _ = world.Space; } catch (Exception) { rejected = true; }
        Check(rejected && world.RequestedPhysicsBackend == PhysicsServer.Backend.GPU, "Denied actual GPU startup rejects strict implicit creation");
        ProjectSettings.Set(ProjectSettings.Physics2DAllowCPUFallback, true);
        rejected = false; try { _ = world.Space; } catch (Exception) { rejected = true; }
        Check(rejected, "Changing project fallback never changes an already captured strict runtime");
        using var relaxed = new SubViewport(); var selected = relaxed.World!;
        Check(selected.RequestedPhysicsBackendName == "GPU" && selected.RequestedPhysicsBackend == PhysicsServer.Backend.GPU &&
            selected.PhysicsBackend == PhysicsServer.Backend.CPU && selected.PhysicsBackendName == "CPU" && selected.PhysicsFallbackReason is not null,
            "A later allowed fallback exposes requested GPU, actual CPU and startup cause");
        var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(120, 0), LinearDamp = 0, LinearDampMode = RigidBody.DampMode.Replace };
        relaxed.AddChild(body); using var tree = new SceneTree(relaxed); tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.Position.X - 2) < .001f, "Fallback-selected implicit world remains an executable CPU simulation");
    }

    private static void FeatureSelection()
    {
        const string feature = "physics-project-test";
        Check(!ProjectSettings.HasFeature(feature), "The project selection probe owns its isolated feature tag");
        ProjectSettings.Set(ProjectSettings.Physics2DEngine, "UnknownWithoutFeature");
        ProjectSettings.Set(ProjectSettings.Physics2DAllowCPUFallback, true);
        ProjectSettings.SetFeatureOverride(ProjectSettings.Physics2DEngine, feature, "CPU");
        ProjectSettings.SetFeatureOverride(ProjectSettings.Physics2DAllowCPUFallback, feature, false);
        try
        {
            ProjectSettings.AddCustomFeature(feature);
            using var view = new SubViewport(); var captured = view.World!;
            ProjectSettings.RemoveCustomFeature(feature);
            Check(captured.RequestedPhysicsBackendName == "CPU" && captured.PhysicsBackendName == "CPU",
                "Implicit creation captures the active feature selection before lazy startup");
            using var later = new SubViewport(); Reject<ArgumentException>(() => _ = later.World);
        }
        finally
        {
            ProjectSettings.RemoveCustomFeature(feature);
            ProjectSettings.ClearFeatureOverride(ProjectSettings.Physics2DEngine, feature);
            ProjectSettings.ClearFeatureOverride(ProjectSettings.Physics2DAllowCPUFallback, feature);
        }
    }

    private static void RoundTrip()
    {
        var path = IOPath.Combine(IOPath.GetTempPath(), "electron2d-project-physics-" + Guid.NewGuid().ToString("N"));
        var project = IOPath.Combine(path, "project"); var user = IOPath.Combine(path, "user");
        Directory.CreateDirectory(project); Directory.CreateDirectory(user);
        try
        {
            using (var saved = new ProjectSettingsRegistry(project, user))
            {
                saved.Set(ProjectSettings.Physics2DEngine, "RegisteredLater"); saved.Set(ProjectSettings.Physics2DAllowCPUFallback, true);
                saved.SetFeatureOverride(ProjectSettings.Physics2DEngine, "physics-test", "GPU");
                saved.SetFeatureOverride(ProjectSettings.Physics2DAllowCPUFallback, "physics-test", false); saved.Save();
            }
            using var loaded = new ProjectSettingsRegistry(project, user); loaded.Load();
            Check(loaded.Get(ProjectSettings.Physics2DEngine) == "RegisteredLater" && loaded.Get(ProjectSettings.Physics2DAllowCPUFallback),
                "Selection and fallback persist before provider registration without untyped configuration");
            Check(loaded.GetWithOverride(ProjectSettings.Physics2DEngine, ["physics-test"]) == "GPU" &&
                !loaded.GetWithOverride(ProjectSettings.Physics2DAllowCPUFallback, ["physics-test"]), "Feature-specific selection and fallback survive persistence");
        }
        finally { Directory.Delete(path, recursive: true); }
    }
}
