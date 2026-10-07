namespace Electron2D;

public partial class CodeEdit
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var descriptor in base.GetPropertyDescriptors()) { if (descriptor.Name == nameof(LayoutDirection)) { yield return new PropertyDescriptor<CodeEdit, LayoutDirection>(nameof(LayoutDirection), c => c.LayoutDirection, (c, v) => c.LayoutDirection = v, _ => LayoutDirection.LTR, stored: true); continue; } if (descriptor.Name == nameof(TextDirection)) { yield return new PropertyDescriptor<CodeEdit, TextDirection>(nameof(TextDirection), c => c.TextDirection, (c, v) => c.TextDirection = v, _ => TextDirection.LTR, stored: true); continue; } yield return descriptor; }
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(AutoBraceCompletionEnabled), c => c.AutoBraceCompletionEnabled, (c, v) => c.AutoBraceCompletionEnabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(AutoBraceCompletionHighlightMatching), c => c.AutoBraceCompletionHighlightMatching, (c, v) => c.AutoBraceCompletionHighlightMatching = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, Dictionary<string, string>>(nameof(AutoBraceCompletionPairs), c => new Dictionary<string, string>(c.AutoBraceCompletionPairs, StringComparer.Ordinal), (c, v) => c.AutoBraceCompletionPairs = v, _ => new(StringComparer.Ordinal) { ["("] = ")", ["["] = "]", ["{"] = "}", ["\""] = "\"", ["'"] = "'" }, stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(CodeCompletionEnabled), c => c.CodeCompletionEnabled, (c, v) => c.CodeCompletionEnabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, string[]>(nameof(CodeCompletionPrefixes), c => c.CodeCompletionPrefixes, (c, v) => c.CodeCompletionPrefixes = v, _ => [], stored: true);
        yield return new PropertyDescriptor<CodeEdit, string[]>(nameof(DelimiterComments), c => c.DelimiterComments, (c, v) => c.DelimiterComments = v, _ => [], stored: true);
        yield return new PropertyDescriptor<CodeEdit, string[]>(nameof(DelimiterStrings), c => c.DelimiterStrings, (c, v) => c.DelimiterStrings = v, _ => ["' '", "\" \""], stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(GuttersDrawBookmarks), c => c.GuttersDrawBookmarks, (c, v) => c.GuttersDrawBookmarks = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(GuttersDrawBreakpointsGutter), c => c.GuttersDrawBreakpointsGutter, (c, v) => c.GuttersDrawBreakpointsGutter = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(GuttersDrawExecutingLines), c => c.GuttersDrawExecutingLines, (c, v) => c.GuttersDrawExecutingLines = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(GuttersDrawFoldGutter), c => c.GuttersDrawFoldGutter, (c, v) => c.GuttersDrawFoldGutter = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(GuttersDrawLineNumbers), c => c.GuttersDrawLineNumbers, (c, v) => c.GuttersDrawLineNumbers = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, int>(nameof(GuttersLineNumbersMinDigits), c => c.GuttersLineNumbersMinDigits, (c, v) => c.GuttersLineNumbersMinDigits = v, _ => 3, stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(GuttersZeroPadLineNumbers), c => c.GuttersZeroPadLineNumbers, (c, v) => c.GuttersZeroPadLineNumbers = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(IndentAutomatic), c => c.IndentAutomatic, (c, v) => c.IndentAutomatic = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, string[]>(nameof(IndentAutomaticPrefixes), c => c.IndentAutomaticPrefixes, (c, v) => c.IndentAutomaticPrefixes = v, _ => [":", "{", "[", "("], stored: true);
        yield return new PropertyDescriptor<CodeEdit, int>(nameof(IndentSize), c => c.IndentSize, (c, v) => c.IndentSize = v, _ => 4, stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(IndentUseSpaces), c => c.IndentUseSpaces, (c, v) => c.IndentUseSpaces = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(LineFolding), c => c.LineFolding, (c, v) => c.LineFolding = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, int[]>(nameof(LineLengthGuidelines), c => c.LineLengthGuidelines, (c, v) => c.LineLengthGuidelines = v, _ => [], stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(SymbolLookupOnClick), c => c.SymbolLookupOnClick, (c, v) => c.SymbolLookupOnClick = v, _ => false, stored: true);
        yield return new PropertyDescriptor<CodeEdit, bool>(nameof(SymbolTooltipOnHover), c => c.SymbolTooltipOnHover, (c, v) => c.SymbolTooltipOnHover = v, _ => false, stored: true);
    }
}
