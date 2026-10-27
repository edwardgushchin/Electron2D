using Electron2D;

internal sealed class ResourceBindingReverse
{
    internal static readonly string SourcePath = Source();
    internal static void Bind(LiveScriptResource owner)
    {
        owner.Binds++; owner.State = new ResourceBindingReverse(); owner.Gain = 20;
        owner.Evaluate = static r => -r.Gain;
        if (owner.FailBind) throw owner.BindError;
    }
    internal static void Clear(LiveScriptResource owner) => ResourceBindingForward.Clear(owner);
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
