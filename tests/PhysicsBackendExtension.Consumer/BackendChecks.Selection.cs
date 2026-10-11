using Electron2D;

public static partial class BackendChecks
{
    private static void DebugFailureChecks()
    {
        foreach (var point in new[] { false, true })
        {
            var name = point ? "BadDebugPoint" : "BadDebugCount";
            PhysicsServerManager.RegisterServer(name, () => new Solver { BadDebugPoint = point, BadDebugCount = !point });
            using var world = new World(name);
            _ = world.Space;
            using var root = new SubViewport { World = world };
            root.AddChild(new StaticBody());
            using var tree = new SceneTree(root) { DebugCollisionsHint = true };
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetTick(world.Space));
        }
    }
    private static void SelectionChecks()
    {
        Reject<ArgumentException>(() => PhysicsServerManager.RegisterServer("CPU", () => new Solver()));
        Reject<ArgumentException>(() => PhysicsServerManager.RegisterServer("GPU", () => new Solver()));
        Reject<ArgumentException>(() => PhysicsServerManager.RegisterServer("DEFAULT", () => new Solver()));
        Reject<ArgumentException>(() => PhysicsServerManager.RegisterServer(" ", () => new Solver()));
        Reject<ArgumentNullException>(() => PhysicsServerManager.RegisterServer("NullFactory", null!));
        Reject<ArgumentException>(() => PhysicsServerManager.SetDefaultServer("Unknown", 100));
        var firstCalls = 0; var secondCalls = 0;
        PhysicsServerManager.RegisterServer("First", () => { firstCalls++; return new Solver(); });
        PhysicsServerManager.RegisterServer("Second", () => { secondCalls++; return new Solver(); });
        Reject<ArgumentException>(() => PhysicsServerManager.RegisterServer("First", () => new Solver()));
        PhysicsServerManager.SetDefaultServer("First", 10);
        using var captured = new World("DEFAULT");
        PhysicsServerManager.SetDefaultServer("Second", 10);
        using var tied = new World("DEFAULT");
        PhysicsServerManager.SetDefaultServer("Second", 11);
        using var next = new World("DEFAULT");
        Check(firstCalls == 0 && secondCalls == 0, "Default selections capture factories without executing them");
        Check(captured.RequestedPhysicsBackendName == "DEFAULT" && captured.PhysicsBackendName == "First" && tied.PhysicsBackendName == "First" && next.PhysicsBackendName == "Second" && firstCalls == 2 && secondCalls == 1, "Strict priority and immutable lazy selections hold");
        using var explicitCPU = new World();
        Check(explicitCPU.PhysicsBackend == PhysicsServer.Backend.CPU, "Parameterless creation retains CPU");
        using var enumCPU = new World(PhysicsServer.Backend.CPU);
        Check(enumCPU.PhysicsBackendName == "CPU", "Enum selection stays independent of the default");
        PhysicsServerManager.RegisterServer("NullResult", () => null!);
        Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreate("NullResult"));
        var disposed = new Solver(); disposed.Dispose();
        PhysicsServerManager.RegisterServer("DisposedResult", () => disposed);
        Reject<ObjectDisposedException>(() => PhysicsServer.SpaceCreate("DisposedResult"));
        PhysicsServerManager.RegisterServer("Recursive", () => { PhysicsServer.SpaceCreate("Recursive"); return new Solver(); });
        Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreate("Recursive"));
        PhysicsServerManager.RegisterServer("Throwing", () => throw new InvalidOperationException("Factory failure"));
        Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreate("Throwing"));
        PhysicsServerManager.RegisterServer("Unlocked", () =>
        {
            Task.Run(() => PhysicsServerManager.RegisterServer("FromWorker", () => new Solver())).GetAwaiter().GetResult();
            return new Solver();
        });
        Solver? initialFailure = null;
        PhysicsServerManager.RegisterServer("InitFailure", () => initialFailure = new Solver { FailInit = true, FailFinish = true, FailDisposal = true });
        try { PhysicsServer.SpaceCreate("InitFailure"); throw new InvalidOperationException("Expected initialization failure"); }
        catch (AggregateException error)
        {
            Check(error.Flatten().InnerExceptions.Count == 3 && initialFailure!.Finished && initialFailure.IsDisposed,
                "Initialization, Finish and disposal errors are all retained after implementation retirement");
        }
        var unlocked = PhysicsServer.SpaceCreate("Unlocked"); PhysicsServer.FreeRID(unlocked);
        Check(captured.PhysicsBackendName == "First" && !captured.IsDisposed, "Factory failures preserve existing worlds");
    }
}
