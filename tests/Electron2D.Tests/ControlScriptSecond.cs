using Electron2D;

internal sealed class ControlScriptSecond
{
    internal static readonly string SourcePath = Source();
    internal static void Bind(Control owner) => ControlScriptTests.Install(owner, true);
    internal static void Clear(Control owner) { }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
