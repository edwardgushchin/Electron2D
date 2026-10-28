using Electron2D;

internal sealed class ButtonScriptSecond
{
    internal static readonly string SourcePath = Source();
    internal static void Bind(BaseButton owner) => ButtonScriptTests.Install(owner, true);
    internal static void Clear(BaseButton owner) { }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
