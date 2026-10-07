namespace Electron2D;

public partial class RichTextLabel
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var descriptor in base.GetPropertyDescriptors()) { if (descriptor.Name == nameof(FocusMode)) { yield return new PropertyDescriptor<RichTextLabel, FocusMode>(nameof(FocusMode), c => c.FocusMode, (c, v) => c.FocusMode = v, _ => FocusMode.Accessibility, stored: true); continue; } if (descriptor.Name == nameof(ClipContents)) { yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(ClipContents), c => c.ClipContents, (c, v) => c.ClipContents = v, _ => true, stored: true); continue; } yield return descriptor; }
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(BBCodeEnabled), c => c.BBCodeEnabled, (c, v) => c.BBCodeEnabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(SelectionEnabled), c => c.SelectionEnabled, (c, v) => c.SelectionEnabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(ContextMenuEnabled), c => c.ContextMenuEnabled, (c, v) => c.ContextMenuEnabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(DeselectOnFocusLossEnabled), c => c.DeselectOnFocusLossEnabled, (c, v) => c.DeselectOnFocusLossEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(DragAndDropSelectionEnabled), c => c.DragAndDropSelectionEnabled, (c, v) => c.DragAndDropSelectionEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(FitContent), c => c.FitContent, (c, v) => c.FitContent = v, _ => false, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(HintUnderlined), c => c.HintUnderlined, (c, v) => c.HintUnderlined = v, _ => true, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(MetaUnderlined), c => c.MetaUnderlined, (c, v) => c.MetaUnderlined = v, _ => true, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(ScrollActive), c => c.ScrollActive, (c, v) => c.ScrollActive = v, _ => true, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(ScrollFollowing), c => c.ScrollFollowing, (c, v) => c.ScrollFollowing = v, _ => false, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(ScrollFollowingVisibleCharacters), c => c.ScrollFollowingVisibleCharacters, (c, v) => c.ScrollFollowingVisibleCharacters = v, _ => false, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(ShortcutKeysEnabled), c => c.ShortcutKeysEnabled, (c, v) => c.ShortcutKeysEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, bool>(nameof(Threaded), c => c.Threaded, (c, v) => c.Threaded = v, _ => false, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, TextAutowrapMode>(nameof(AutowrapMode), c => c.AutowrapMode, (c, v) => c.AutowrapMode = v, _ => TextAutowrapMode.WordSmart, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, TextLineBreakFlags>(nameof(AutowrapTrimFlags), c => c.AutowrapTrimFlags, (c, v) => c.AutowrapTrimFlags = v, _ => TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, TextJustificationFlags>(nameof(JustificationFlags), c => c.JustificationFlags, (c, v) => c.JustificationFlags = v, _ => (TextJustificationFlags)163, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, HorizontalAlignment>(nameof(HorizontalAlignment), c => c.HorizontalAlignment, (c, v) => c.HorizontalAlignment = v, _ => HorizontalAlignment.Left, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, VerticalAlignment>(nameof(VerticalAlignment), c => c.VerticalAlignment, (c, v) => c.VerticalAlignment = v, _ => VerticalAlignment.Top, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, TextDirection>(nameof(TextDirection), c => c.TextDirection, (c, v) => c.TextDirection = v, _ => TextDirection.Auto, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, TextVisibleCharactersBehavior>(nameof(VisibleCharactersBehavior), c => c.VisibleCharactersBehavior, (c, v) => c.VisibleCharactersBehavior = v, _ => TextVisibleCharactersBehavior.CharsBeforeShaping, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, StructuredTextParser>(nameof(StructuredTextBIDIOverride), c => c.StructuredTextBIDIOverride, (c, v) => c.StructuredTextBIDIOverride = v, _ => StructuredTextParser.Default, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, string>(nameof(Text), c => c.Text, (c, v) => c.Text = v, _ => "", stored: true);
        yield return new PropertyDescriptor<RichTextLabel, string>(nameof(Language), c => c.Language, (c, v) => c.Language = v, _ => "", stored: true);
        yield return new PropertyDescriptor<RichTextLabel, int>(nameof(TabSize), c => c.TabSize, (c, v) => c.TabSize = v, _ => 4, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, int>(nameof(ProgressBarDelay), c => c.ProgressBarDelay, (c, v) => c.ProgressBarDelay = v, _ => 1000, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, int>(nameof(VisibleCharacters), c => c.VisibleCharacters, (c, v) => c.VisibleCharacters = v, _ => -1, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, float>(nameof(VisibleRatio), c => c.VisibleRatio, (c, v) => c.VisibleRatio = v, _ => 1, stored: true);
        yield return new PropertyDescriptor<RichTextLabel, float[]>(nameof(TabStops), c => c.TabStops, (c, v) => c.TabStops = v, _ => [], stored: true);
        yield return new PropertyDescriptor<RichTextLabel, string[]>(nameof(StructuredTextBIDIOverrideOptions), c => c.StructuredTextBIDIOverrideOptions, (c, v) => c.StructuredTextBIDIOverrideOptions = v, _ => [], stored: true);
        yield return new PropertyDescriptor<RichTextLabel, RichTextEffect[]>(nameof(CustomEffects), c => c.CustomEffects, (c, v) => c.CustomEffects = v, _ => [], stored: true);
    }
}
