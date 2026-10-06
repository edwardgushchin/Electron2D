namespace Electron2D;

public partial class TextEdit
{
    private void RestoreHighlighter(SyntaxHighlighter? resource)
    {
        var copy = resource is null ? null : (SyntaxHighlighter)resource.Duplicate();
        try { SyntaxHighlighter = copy; _storedHighlighter = copy; } catch { copy?.Dispose(); throw; }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
        {
            if (property.Name == nameof(FocusMode)) yield return new PropertyDescriptor<TextEdit, FocusMode>(nameof(FocusMode), t => t.FocusMode, (t, v) => t.FocusMode = v, _ => FocusMode.All, stored: true);
            else if (property.Name == nameof(MouseDefaultCursorShape)) yield return new PropertyDescriptor<TextEdit, CursorShape>(nameof(MouseDefaultCursorShape), t => t.MouseDefaultCursorShape, (t, v) => t.MouseDefaultCursorShape = v, _ => CursorShape.IBeam, stored: true);
            else if (property.Name == nameof(ClipContents)) yield return new PropertyDescriptor<TextEdit, bool>(nameof(ClipContents), t => t.ClipContents, (t, v) => t.ClipContents = v, _ => true, stored: true);
            else yield return property;
        }
        yield return new PropertyDescriptor<TextEdit, string>(nameof(Text), t => t.Text, (t, v) => t.Text = v, _ => "", stored: true);
        yield return new PropertyDescriptor<TextEdit, SyntaxHighlighter?>(nameof(SyntaxHighlighter), t => t.SyntaxHighlighter, (t, v) => t.RestoreHighlighter(v), _ => null, stored: true);
        yield return new PropertyDescriptor<TextEdit, TextAutowrapMode>(nameof(AutowrapMode), t => t.AutowrapMode, (t, v) => t.AutowrapMode = v, _ => TextAutowrapMode.WordSmart, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(BackspaceDeletesCompositeCharacterEnabled), t => t.BackspaceDeletesCompositeCharacterEnabled, (t, v) => t.BackspaceDeletesCompositeCharacterEnabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(CaretBlink), t => t.CaretBlink, (t, v) => t.CaretBlink = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, double>(nameof(CaretBlinkInterval), t => t.CaretBlinkInterval, (t, v) => t.CaretBlinkInterval = v, _ => 0.65, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(CaretDrawWhenEditableDisabled), t => t.CaretDrawWhenEditableDisabled, (t, v) => t.CaretDrawWhenEditableDisabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(CaretMidGrapheme), t => t.CaretMidGrapheme, (t, v) => t.CaretMidGrapheme = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(CaretMoveOnRightClick), t => t.CaretMoveOnRightClick, (t, v) => t.CaretMoveOnRightClick = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(CaretMultiple), t => t.CaretMultiple, (t, v) => t.CaretMultiple = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, TextEditCaretType>(nameof(CaretType), t => t.CaretType, (t, v) => t.CaretType = v, _ => default, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(ContextMenuEnabled), t => t.ContextMenuEnabled, (t, v) => t.ContextMenuEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, string>(nameof(CustomWordSeparators), t => t.CustomWordSeparators, (t, v) => t.CustomWordSeparators = v, _ => "", stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(DeselectOnFocusLossEnabled), t => t.DeselectOnFocusLossEnabled, (t, v) => t.DeselectOnFocusLossEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(DragAndDropSelectionEnabled), t => t.DragAndDropSelectionEnabled, (t, v) => t.DragAndDropSelectionEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(DrawControlChars), t => t.DrawControlChars, (t, v) => t.DrawControlChars = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(DrawSpaces), t => t.DrawSpaces, (t, v) => t.DrawSpaces = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(DrawTabs), t => t.DrawTabs, (t, v) => t.DrawTabs = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(Editable), t => t.Editable, (t, v) => t.Editable = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(EmptySelectionClipboardEnabled), t => t.EmptySelectionClipboardEnabled, (t, v) => t.EmptySelectionClipboardEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(HighlightAllOccurrences), t => t.HighlightAllOccurrences, (t, v) => t.HighlightAllOccurrences = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(HighlightCurrentLine), t => t.HighlightCurrentLine, (t, v) => t.HighlightCurrentLine = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(IndentWrappedLines), t => t.IndentWrappedLines, (t, v) => t.IndentWrappedLines = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, string>(nameof(Language), t => t.Language, (t, v) => t.Language = v, _ => "", stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(MiddleMousePasteEnabled), t => t.MiddleMousePasteEnabled, (t, v) => t.MiddleMousePasteEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(MinimapDraw), t => t.MinimapDraw, (t, v) => t.MinimapDraw = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, int>(nameof(MinimapWidth), t => t.MinimapWidth, (t, v) => t.MinimapWidth = v, _ => 80, stored: true);
        yield return new PropertyDescriptor<TextEdit, string>(nameof(PlaceholderText), t => t.PlaceholderText, (t, v) => t.PlaceholderText = v, _ => "", stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(ScrollFitContentHeight), t => t.ScrollFitContentHeight, (t, v) => t.ScrollFitContentHeight = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(ScrollFitContentWidth), t => t.ScrollFitContentWidth, (t, v) => t.ScrollFitContentWidth = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, int>(nameof(ScrollHorizontal), t => t.ScrollHorizontal, (t, v) => t.ScrollHorizontal = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(ScrollPastEndOfFile), t => t.ScrollPastEndOfFile, (t, v) => t.ScrollPastEndOfFile = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(ScrollSmooth), t => t.ScrollSmooth, (t, v) => t.ScrollSmooth = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, double>(nameof(ScrollVScrollSpeed), t => t.ScrollVScrollSpeed, (t, v) => t.ScrollVScrollSpeed = v, _ => 80.0, stored: true);
        yield return new PropertyDescriptor<TextEdit, double>(nameof(ScrollVertical), t => t.ScrollVertical, (t, v) => t.ScrollVertical = v, _ => 0.0, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(SelectingEnabled), t => t.SelectingEnabled, (t, v) => t.SelectingEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(ShortcutKeysEnabled), t => t.ShortcutKeysEnabled, (t, v) => t.ShortcutKeysEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, StructuredTextParser>(nameof(StructuredTextBIDIOverride), t => t.StructuredTextBIDIOverride, (t, v) => t.StructuredTextBIDIOverride = v, _ => StructuredTextParser.Default, stored: true);
        yield return new PropertyDescriptor<TextEdit, string[]>(nameof(StructuredTextBIDIOverrideOptions), t => t.StructuredTextBIDIOverrideOptions, (t, v) => t.StructuredTextBIDIOverrideOptions = v, _ => [], stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(TabInputMode), t => t.TabInputMode, (t, v) => t.TabInputMode = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, TextDirection>(nameof(TextDirection), t => t.TextDirection, (t, v) => t.TextDirection = v, _ => global::Electron2D.TextDirection.Auto, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(UseCustomWordSeparators), t => t.UseCustomWordSeparators, (t, v) => t.UseCustomWordSeparators = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TextEdit, bool>(nameof(UseDefaultWordSeparators), t => t.UseDefaultWordSeparators, (t, v) => t.UseDefaultWordSeparators = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TextEdit, LineWrappingMode>(nameof(WrapMode), t => t.WrapMode, (t, v) => t.WrapMode = v, _ => default, stored: true);
    }
}
