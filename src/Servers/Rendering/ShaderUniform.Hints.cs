namespace Electron2D;

internal sealed partial record ShaderUniform
{
    internal (PropertyHint Hint, string Text, bool Color)? HintMetadata { get; init; }
    internal PropertyHint Hint => HintMetadata?.Hint ?? PropertyHint.None;
    internal string HintString => HintMetadata?.Text ?? "";
    internal bool SourceColor => HintMetadata?.Color ?? false;
}
