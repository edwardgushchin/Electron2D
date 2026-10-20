using Electron2D;

internal sealed class ScriptRPCActor : ScriptRPCBaseActor
{
    internal static new readonly string SourcePath = Source();
    internal static readonly RPCMethod<ScriptRPCActor, int> Add = ScriptRPCTests.Method<ScriptRPCActor>(9101, static (n, value) => { if (n.Fail) throw new ApplicationException("RPC callback"); n.Score += value; n.Sender = n.Multiplayer.GetRemoteSenderID(); });
    internal static readonly RPCMethod<ScriptRPCActor, int> Disabled = ScriptRPCTests.Method<ScriptRPCActor>(9103, static (n, value) => n.Score += value);
    internal static int Created;
    internal bool Fail;
    internal int Sender;
    public ScriptRPCActor() { Created++; }
    private static Node CreateActor() => new ScriptRPCActor();
    protected override Func<Node> CreateSceneInstanceFactory() => CreateActor;
    internal static new string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
