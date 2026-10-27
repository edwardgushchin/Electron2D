using Electron2D;

internal sealed class ResourceBindingForward
{
    internal static readonly string SourcePath = Source();
    internal static int Released;
    internal static void Bind(LiveScriptResource owner)
    {
        owner.Binds++; owner.State = new ResourceBindingForward(); owner.Gain = 10;
        owner.Evaluate = static r => r.Gain;
        if (owner.FailBind) throw owner.BindError;
    }
    internal static void Clear(LiveScriptResource owner)
    {
        owner.Clears++; owner.Evaluate = null; if (owner.State is not null) Released++; owner.State = null;
        if (owner.FailClear) throw owner.ClearError;
    }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
