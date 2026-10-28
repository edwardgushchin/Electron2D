using Electron2D;

internal sealed class LiveRPCReverse : LiveRPCBase
{
    internal static new readonly string SourcePath = Source();
    internal static readonly RPCMethod<RigidBody, int> Drive = LiveRPCTests.Method<RigidBody>(9911, static (n, v) => LiveRPCTests.Drive(n, v));
    internal static readonly RPCMethod<RigidBody, int> Ack = LiveRPCTests.Method<RigidBody>(9912, static (n, v) => LiveRPCTests.StateFor(n).Ack = v);
    internal static new void Bind(RigidBody owner) => LiveRPCTests.Install(owner, -1);
    internal static new void Clear(RigidBody owner) { }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
