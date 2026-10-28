using Electron2D;

internal sealed class ButtonScriptFirst
{
    internal static readonly string SourcePath = Source();
    internal static bool FailBind, FailClear;
    internal static readonly IOException Error = new("button script transition");
    internal static readonly PropertyDescriptor<BaseButton, int> Value = new("ButtonScriptValue", owner => ButtonScriptTests.Get(owner).Value,
        (owner, value) => ButtonScriptTests.Get(owner).Value = value, _ => 0, stored: true);
    internal static void Bind(BaseButton owner) { ButtonScriptTests.Install(owner, false); if (FailBind) throw Error; }
    internal static void Clear(BaseButton owner) { if (FailClear) throw Error; }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
