namespace Electron2D;

public partial class Label
{
    private static readonly PropertyDescriptor[] LabelProperties =
    [
        new PropertyDescriptor<Label, MouseFilter>(nameof(MouseFilter), label => label.MouseFilter, (label, value) => label.MouseFilter = value, _ => MouseFilter.Ignore, stored: true),
        new PropertyDescriptor<Label, SizeFlags>(nameof(SizeFlagsVertical), label => label.SizeFlagsVertical, (label, value) => label.SizeFlagsVertical = value, _ => SizeFlags.ShrinkCenter, stored: true),
        new PropertyDescriptor<Label, string>(nameof(Text), label => label.Text, (label, value) => label.Text = value, _ => string.Empty, stored: true),
        new PropertyDescriptor<Label, HorizontalAlignment>(nameof(HorizontalAlignment), label => label.HorizontalAlignment, (label, value) => label.HorizontalAlignment = value, _ => HorizontalAlignment.Left, stored: true),
        new PropertyDescriptor<Label, VerticalAlignment>(nameof(VerticalAlignment), label => label.VerticalAlignment, (label, value) => label.VerticalAlignment = value, _ => VerticalAlignment.Top, stored: true),
        new PropertyDescriptor<Label, TextAutowrapMode>(nameof(AutowrapMode), label => label.AutowrapMode, (label, value) => label.AutowrapMode = value, _ => TextAutowrapMode.Off, stored: true),
        new PropertyDescriptor<Label, TextJustificationFlags>(nameof(JustificationFlags), label => label.JustificationFlags, (label, value) => label.JustificationFlags = value, _ => (TextJustificationFlags)163, stored: true),
        new PropertyDescriptor<Label, TextOverrunBehavior>(nameof(TextOverrunBehavior), label => label.TextOverrunBehavior, (label, value) => label.TextOverrunBehavior = value, _ => global::Electron2D.TextOverrunBehavior.NoTrimming, stored: true),
        new PropertyDescriptor<Label, bool>(nameof(ClipText), label => label.ClipText, (label, value) => label.ClipText = value, _ => false, stored: true),
        new PropertyDescriptor<Label, bool>(nameof(Uppercase), label => label.Uppercase, (label, value) => label.Uppercase = value, _ => false, stored: true),
        new PropertyDescriptor<Label, global::Electron2D.TextDirection>(nameof(TextDirection), label => label.TextDirection, (label, value) => label.TextDirection = value, _ => global::Electron2D.TextDirection.Auto, stored: true),
        new PropertyDescriptor<Label, StructuredTextParser>(nameof(StructuredTextBIDIOverride), label => label.StructuredTextBIDIOverride, (label, value) => label.StructuredTextBIDIOverride = value, _ => StructuredTextParser.Default, stored: true),
        new PropertyDescriptor<Label, int>(nameof(LinesSkipped), label => label.LinesSkipped, (label, value) => label.LinesSkipped = value, _ => 0, stored: true),
        new PropertyDescriptor<Label, int>(nameof(MaxLinesVisible), label => label.MaxLinesVisible, (label, value) => label.MaxLinesVisible = value, _ => -1, stored: true),
        new PropertyDescriptor<Label, TextVisibleCharactersBehavior>(nameof(VisibleCharactersBehavior), label => label.VisibleCharactersBehavior, (label, value) => label.VisibleCharactersBehavior = value, _ => TextVisibleCharactersBehavior.CharsBeforeShaping, stored: true),
        new PropertyDescriptor<Label, TextLineBreakFlags>(nameof(AutowrapTrimFlags), label => label.AutowrapTrimFlags, (label, value) => label.AutowrapTrimFlags = value, _ => TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces, stored: true),
        new PropertyDescriptor<Label, LabelSettings?>(nameof(LabelSettings), label => label.LabelSettings, (label, value) => label.LabelSettings = value, _ => null, stored: true),
        new PropertyDescriptor<Label, string>(nameof(Language), label => label.Language, (label, value) => label.Language = value, _ => string.Empty, stored: true),
        new PropertyDescriptor<Label, string>(nameof(ParagraphSeparator), label => label.ParagraphSeparator, (label, value) => label.ParagraphSeparator = value, _ => @"\n", stored: true),
        new PropertyDescriptor<Label, string>(nameof(EllipsisChar), label => label.EllipsisChar, (label, value) => label.EllipsisChar = value, _ => "…", stored: true),
        new PropertyDescriptor<Label, string[]>(nameof(StructuredTextBIDIOverrideOptions), label => label.StructuredTextBIDIOverrideOptions, (label, value) => label.StructuredTextBIDIOverrideOptions = value, _ => [], stored: true),
        new PropertyDescriptor<Label, float[]>(nameof(TabStops), label => label.TabStops, (label, value) => label.TabStops = value, _ => [], stored: true),
        new PropertyDescriptor<Label, float>(nameof(VisibleRatio), label => label.VisibleRatio, (label, value) => label.VisibleRatio = value, _ => 1, stored: true),
        new PropertyDescriptor<Label, int>(nameof(VisibleCharacters), label => label.VisibleCharacters, (label, value) => label.VisibleCharacters = value, _ => -1, stored: true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
            if (property.Name != nameof(MouseFilter) && property.Name != nameof(SizeFlagsVertical)) yield return property;
        foreach (var property in LabelProperties) yield return property;
    }
}
