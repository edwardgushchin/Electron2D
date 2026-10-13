using Electron2D;

public static partial class BackendChecks
{
    public static void RunProjectSelection()
    {
        var engine = ProjectSettings.Get(ProjectSettings.Physics2DEngine); var fallback = ProjectSettings.Get(ProjectSettings.Physics2DAllowCPUFallback);
        try
        {
            var calls = 0;
            PhysicsServerManager.RegisterServer("ProjectProvider", () => { calls++; return new Solver(); });
            ProjectSettings.Set(ProjectSettings.Physics2DEngine, "ProjectProvider");
            ProjectSettings.Set(ProjectSettings.Physics2DAllowCPUFallback, false);
            using var view = new SubViewport(); var captured = view.World!; using var alias = (World)captured.Duplicate();
            Check(calls == 0 && captured.RequestedPhysicsBackend == PhysicsServer.Backend.Extension && captured.RequestedPhysicsBackendName == "ProjectProvider",
                "Implicit project selection captures a registration without calling its factory");
            ProjectSettings.Set(ProjectSettings.Physics2DEngine, "CPU");
            using var circle = new CircleShape { Radius = 1 };
            var body = new RigidBody
            {
                GravityScale = 0,
                CanSleep = false,
                LinearVelocity = new(120, 0),
                LinearDamp = 0,
                LinearDampMode = RigidBody.DampMode.Replace
            };
            body.AddChild(new CollisionShape { Shape = circle }); view.AddChild(body); using var tree = new SceneTree(view);
            Check(calls == 1 && alias.Space == captured.Space && captured.PhysicsBackendName == "ProjectProvider" && captured.PhysicsFallbackReason is null,
                "Project edits before startup preserve the captured independent provider and alias identity");
            tree.PhysicsFrame(1d / 60); Check(MathF.Abs(body.Position.X - 2) < .001f, "Project-selected provider executes actual numerical scene motion");
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Project-selected provider full frames allocated {own}/{all} B");
            PhysicsServerManager.SetDefaultServer("ProjectProvider", 10); ProjectSettings.Set(ProjectSettings.Physics2DEngine, "DEFAULT");
            using var first = new SubViewport(); var selectedDefault = first.World!;
            PhysicsServerManager.SetDefaultServer("CPU", 11); using var later = new SubViewport();
            Check(selectedDefault.RequestedPhysicsBackendName == "DEFAULT" && selectedDefault.PhysicsBackendName == "ProjectProvider" &&
                selectedDefault.RequestedPhysicsBackend == PhysicsServer.Backend.Extension && later.World!.PhysicsBackendName == "CPU",
                "DEFAULT captures manager priority selection while later implicit worlds see a later default");
            PhysicsServerManager.RegisterServer("ProjectBroken", () => throw new InvalidOperationException("Project provider initialization failure"));
            ProjectSettings.Set(ProjectSettings.Physics2DEngine, "ProjectBroken"); ProjectSettings.Set(ProjectSettings.Physics2DAllowCPUFallback, true);
            using var failed = new SubViewport(); var rejected = failed.World!;
            Reject<InvalidOperationException>(() => _ = rejected.Space);
            Check(rejected.RequestedPhysicsBackend == PhysicsServer.Backend.Extension && captured.PhysicsBackendName == "ProjectProvider",
                "Project fallback never hides a provider failure or changes an existing world");
            Console.WriteLine($"Project provider: immutable lazy capture, DEFAULT priority, aliases, numerical response, failure and 128 frames {own}/{all} B passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DEngine, engine); ProjectSettings.Set(ProjectSettings.Physics2DAllowCPUFallback, fallback); }
    }
}
