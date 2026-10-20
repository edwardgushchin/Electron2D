using Electron2D;

public static partial class BackendChecks
{
    public static void RegisterScriptFactoryBackend(string name, Action step) =>
        PhysicsServerManager.RegisterServer(name, () => new Solver { StepHook = step });
}
