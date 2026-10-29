namespace Electron2D;

/// <summary>Disables font resources, shaping and text output while keeping the engine's other services usable.</summary>
/// <remarks>Factories return an empty identity. Resource operations ignore their identity and setters have
/// no effect; queries return empty results and documented missing-position sentinels. General Unicode
/// identifier, tag, normalization and structured-parser utilities remain available.</remarks>
public sealed partial class TextServerDummy : TextServerExtension
{
    /// <summary>Creates an unregistered disabled text implementation.</summary>
    public TextServerDummy() { }
    /// <inheritdoc />
    protected override string OnGetName() => "Dummy";
    /// <inheritdoc />
    protected override Feature OnGetFeatures() => 0;
    /// <inheritdoc />
    protected override bool OnHas(RID rid) => false;
    /// <inheritdoc />
    protected override void OnReferenceOversamplingLevel(float oversampling) { }
    /// <inheritdoc />
    protected override void OnUnreferenceOversamplingLevel(float oversampling) { }
    /// <inheritdoc />
    protected override bool OnTryGetSpanMetadata<T>(RID shaped, int index, out T metadata) { metadata = default!; return false; }
    /// <inheritdoc />
    protected override bool OnIsLocaleRightToLeft(string locale) { NativeTextBreak.ValidateString(locale); return false; }
    /// <inheritdoc />
    protected override bool OnIsLocaleUsingSupportData(string locale) { NativeTextBreak.ValidateString(locale); return false; }
    /// <inheritdoc />
    protected override byte[] OnGetSupportData() => [];
    /// <inheritdoc />
    protected override string OnGetSupportDataFilename() => string.Empty;
    /// <inheritdoc />
    protected override string OnGetSupportDataInfo() => string.Empty;
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
    protected override int[] OnStringGetWordBreaks(string text, string language = "", int charsPerLine = 0)
    { NativeTextBreak.ValidateString(text); NativeTextBreak.ValidateString(language); return []; }
    /// <inheritdoc />
    protected override string OnStringToUpper(string text, string language = "") => Unchanged(text, language);
    /// <inheritdoc />
    protected override string OnStringToLower(string text, string language = "") => Unchanged(text, language);
    /// <inheritdoc />
    protected override string OnStringToTitle(string text, string language = "") => Unchanged(text, language);
    private static string Unchanged(string text, string language)
    { NativeTextBreak.ValidateString(text); NativeTextBreak.ValidateString(language); return text; }
}
