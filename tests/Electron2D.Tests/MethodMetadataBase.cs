using Electron2D;

internal abstract class MethodMetadataBase : ElectronObject
{
    internal static readonly string SourcePath = Source();
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
    public virtual int Optional() => 2;
    public abstract int Required();
}

