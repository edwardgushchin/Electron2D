using Electron2D;

internal abstract class ScriptRPCBaseActor : Node
{
    internal static readonly string SourcePath = Source();
    internal static readonly RPCMethod<ScriptRPCBaseActor, int> BaseScore = ScriptRPCTests.Method<ScriptRPCBaseActor>(9101, static (n, value) => n.Score += value * 100);
    internal static readonly RPCMethod<ScriptRPCBaseActor, int> Power = ScriptRPCTests.Method<ScriptRPCBaseActor>(9102, static (n, value) => n.PowerValue += value);
    internal int Score, PowerValue;
    internal static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
