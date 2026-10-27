using Electron2D;

internal sealed class RequiredScriptResource : Resource
{
    internal static readonly string SourcePath = ScriptActor.Source();
    internal readonly Script? ConstructorSource;
    internal readonly string Value;
    internal int Initializations;
    public RequiredScriptResource(string value)
    {
        Value = value; ConstructorSource = GetScript(); Initializations++;
    }
}
