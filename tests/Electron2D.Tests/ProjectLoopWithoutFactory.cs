using System.Runtime.CompilerServices;
using Electron2D;

[GlobalClass]
internal sealed class ProjectLoopWithoutFactory : MainLoop
{
    internal static readonly string SourcePath = Source();
    private static string Source([CallerFilePath] string path = "") => path;
}
