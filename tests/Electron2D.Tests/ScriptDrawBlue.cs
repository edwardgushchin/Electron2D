using Electron2D;

internal sealed class ScriptDrawBlue
{
    internal static readonly string SourcePath = Source();
    internal static void Bind(RigidBody owner) => ScriptDrawTests.Install(owner, Colors.Blue);
    internal static void Clear(RigidBody owner) { }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
