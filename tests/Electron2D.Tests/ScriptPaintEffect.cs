using Electron2D;
internal sealed class ScriptPaintEffect : RichTextEffect
{
    internal int Calls;
    internal Script? ConstructorScript;
    internal int SourceProperties, SourceChanges;
    internal static bool RetireOnSourceProperties;
    public ScriptPaintEffect()
    {
        ConstructorScript = GetScript(); BBCode = "compiled_paint";
        PropertyListChanged += _ => { SourceProperties++; if (RetireOnSourceProperties) Dispose(); };
        ScriptChanged += _ => SourceChanges++;
    }
    protected override bool OnProcessCustomFX(CharFXTransform state) { Calls++; state.Color = Colors.Cyan; return true; }
    protected override Resource CreateDuplicateInstance() => new ScriptPaintEffect();
    internal static readonly string SourcePath = Source();
    internal static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
