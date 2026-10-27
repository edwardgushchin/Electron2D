using Electron2D;

internal sealed class ScriptDrawRed
{
    internal static readonly string SourcePath = Source();
    internal static void Bind(RigidBody owner) => ScriptDrawTests.Install(owner, Colors.Red);
    internal static void Clear(RigidBody owner) { if (ScriptDrawTests.FailClear) throw ScriptDrawTests.ClearError; }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
