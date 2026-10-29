namespace Electron2D;

public sealed partial class TextServerFallback
{
    /// <inheritdoc />
    protected override bool OnIsLocaleRightToLeft(string locale) { NativeTextBreak.ValidateString(locale); return false; }
    /// <inheritdoc />
    protected override bool OnIsLocaleUsingSupportData(string locale) { NativeTextBreak.ValidateString(locale); return false; }
    /// <inheritdoc />
    protected override byte[] OnGetSupportData() => [];
    /// <inheritdoc />
    protected override string OnGetSupportDataFilename() => string.Empty;
    /// <inheritdoc />
    protected override string OnGetSupportDataInfo() => "Not supported";
    /// <inheritdoc />
    protected override bool OnLoadSupportData(string filename) { NativeTextBreak.ValidateString(filename); return false; }
    /// <inheritdoc />
    protected override bool OnSaveSupportData(string filename) { NativeTextBreak.ValidateString(filename); return false; }
    /// <inheritdoc />
    protected override int OnIsConfusable(string text, ReadOnlySpan<string> dictionary)
    { NativeTextBreak.ValidateString(text); foreach (var entry in dictionary) NativeTextBreak.ValidateString(entry); return -1; }
    /// <inheritdoc />
    protected override bool OnSpoofCheck(string text) { NativeTextBreak.ValidateString(text); return false; }
    /// <inheritdoc />
    protected override int[] OnStringGetCharacterBreaks(string text, string language = "")
    { NativeTextBreak.ValidateString(language); return TextCase.ScalarBreaks(text); }
    /// <inheritdoc />
    protected override int[] OnStringGetWordBreaks(string text, string language = "", int charsPerLine = 0) => NativeTextBreak.StringWordBreaks(text, language, charsPerLine, simple: true);
    /// <inheritdoc />
    protected override string OnStringToUpper(string text, string language = "")
    { NativeTextBreak.ValidateString(text); NativeTextBreak.ValidateString(language); return TextCase.SimpleCase(text, true); }
    /// <inheritdoc />
    protected override string OnStringToLower(string text, string language = "")
    { NativeTextBreak.ValidateString(text); NativeTextBreak.ValidateString(language); return TextCase.SimpleCase(text, false); }
    /// <inheritdoc />
    protected override string OnStringToTitle(string text, string language = "")
    { NativeTextBreak.ValidateString(text); NativeTextBreak.ValidateString(language); var result = TextCase.SimpleTitle(text); NativeTextBreak.ValidateString(result); return result; }
    /// <inheritdoc />
    protected override bool OnFontIsScriptSupported(RID fontRID, string script) => SupportsSimpleScript(fontRID, script);
    /// <inheritdoc />
    protected override Dictionary<uint, FontFeatureInfo> OnFontSupportedFeatureList(RID fontRID) => [];
}
