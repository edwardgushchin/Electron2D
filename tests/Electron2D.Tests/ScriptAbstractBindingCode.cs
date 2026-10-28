using Electron2D;

internal sealed class ScriptAbstractBindingCode
{
    internal static readonly string SourcePath = Source();
    internal static int Binds, Clears;
    internal static void Bind(BaseButton owner) => Binds++;
    internal static void Clear(BaseButton owner) => Clears++;
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
