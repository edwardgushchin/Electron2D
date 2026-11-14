using System.Runtime.CompilerServices;
using Electron2D;

[GlobalClass]
internal sealed class ProjectLoopWrongType : ElectronObject
{
    internal static readonly string SourcePath = Source();
    private static string Source([CallerFilePath] string path = "") => path;
}
