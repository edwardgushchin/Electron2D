using Electron2D;
internal sealed class ScriptPaintEffect : RichTextEffect
{
    internal int Calls;
    public ScriptPaintEffect() { BBCode = "compiled_paint"; }
    protected override bool OnProcessCustomFX(CharFXTransform state) { Calls++; state.Color = Colors.Cyan; return true; }
    protected override Resource CreateDuplicateInstance() => new ScriptPaintEffect();
    internal static readonly string SourcePath = Source();
    internal static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
