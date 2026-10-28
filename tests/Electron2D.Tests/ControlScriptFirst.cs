using Electron2D;

internal sealed class ControlScriptFirst
{
    internal static readonly string SourcePath = Source();
    internal static bool FailBind, FailClear;
    internal static readonly IOException Error = new("control script transition");
    internal static readonly PropertyDescriptor<Control, int> Value = new("ScriptValue", owner => ControlScriptTests.Get(owner).Value,
        (owner, value) => ControlScriptTests.Get(owner).Value = value, _ => 0, stored: true);
    internal static void Bind(Control owner) { ControlScriptTests.Install(owner, false); if (FailBind) throw Error; }
    internal static void Clear(Control owner) { if (FailClear) throw Error; }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
