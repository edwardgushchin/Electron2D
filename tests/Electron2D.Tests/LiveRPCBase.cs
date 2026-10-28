using Electron2D;

internal class LiveRPCBase
{
    internal static readonly string SourcePath = Source();
    internal static readonly RPCMethod<RigidBody, int> Shadow = LiveRPCTests.Method<RigidBody>(9911, static (n, v) => LiveRPCTests.StateFor(n).Value += 100 * v);
    internal static readonly RPCMethod<RigidBody, int> Extra = LiveRPCTests.Method<RigidBody>(9913, static (n, v) => LiveRPCTests.StateFor(n).Extra = v);
    internal static void Bind(RigidBody owner) => LiveRPCTests.Install(owner, 10);
    internal static void Clear(RigidBody owner) { }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
