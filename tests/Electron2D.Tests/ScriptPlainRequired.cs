using Electron2D;

internal sealed class ScriptPlainRequired : ElectronObject
{
    internal static readonly string SourcePath = Source();
    internal static int Initializations;
    internal readonly int Value;
    internal readonly Script? ConstructorSource;
    internal ScriptPlainRequired(int value) { Value = value; ConstructorSource = GetScript(); Initializations++; }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
