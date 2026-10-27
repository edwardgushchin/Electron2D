using Electron2D;

internal sealed class LiveScriptReverse : IDisposable
{
    internal static readonly string SourcePath = Source();
    internal static int Released;
    internal static void Bind(LiveScriptBody owner)
    {
        owner.State = new LiveScriptReverse(); owner.Step = static (b, _) => { b.Counter += 2; b.LinearVelocity = new(-3, 0); };
        owner.Counter = 20; owner.Binds++; owner.PhysicsProcessEnabled = true;
    }
    internal static void Clear(LiveScriptBody owner)
    { owner.Step = null; owner.State?.Dispose(); owner.State = null; owner.Clears++; if (owner.FailClear) throw owner.ClearError; }
    public void Dispose() => Released++;
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
