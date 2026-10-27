using Electron2D;

internal sealed class RequiredScriptRPCActor : Node
{
    internal static readonly string SourcePath = ScriptActor.Source();
    internal readonly Script? ConstructorSource;
    internal readonly int Value;
    public RequiredScriptRPCActor(int value) { Value = value; ConstructorSource = GetScript(); }
}
