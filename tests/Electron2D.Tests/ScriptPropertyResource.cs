using Electron2D;

internal sealed class ScriptPropertyResource : ScriptPropertyBase
{
    internal new static readonly string SourcePath = Source();
    internal static readonly PropertyDescriptor<Resource, int> Amount = new("ScriptValue", b => Get(b).Value, (b, v) => SetValue(b, v), _ => 13, stored: true);
    internal static void InstallResource(Resource owner) => Install(owner, 2);
    internal static void ClearResource(Resource owner) => Clear(owner);
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
