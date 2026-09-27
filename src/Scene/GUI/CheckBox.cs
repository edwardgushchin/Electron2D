namespace Electron2D;

/// <summary>A left-aligned toggle button with a checkbox or grouped radio indicator.</summary>
/// <remarks>The indicator reserves the maximum dimensions of all checked, unchecked, radio and disabled theme icons.
/// ButtonGroup selects radio imagery. The indicator precedes content and follows RTL layout.</remarks>
public class CheckBox : Button
{
    private static readonly string[] IconNames = ["checked", "unchecked", "radio_checked", "radio_unchecked",
        "checked_disabled", "unchecked_disabled", "radio_checked_disabled", "radio_unchecked_disabled"];
    private static readonly PropertyDescriptor[] CheckProperties =
    [
        new PropertyDescriptor<CheckBox, HorizontalAlignment>(nameof(Alignment), b => b.Alignment, (b,v) => b.Alignment = v, _ => HorizontalAlignment.Left, stored: true),
        new PropertyDescriptor<CheckBox, bool>(nameof(ToggleMode), b => b.ToggleMode, (b,v) => b.ToggleMode = v, _ => true, stored: true)
    ];
    /// <summary>Creates an empty checkbox with toggle mode enabled.</summary>
    public CheckBox() : this(string.Empty) { }
    /// <summary>Creates a checkbox with untranslated text.</summary>
    /// <param name="text">Initial text; may be empty.</param>
    /// <exception cref="ArgumentNullException">The text is null.</exception>
    public CheckBox(string text) : base(text) { ToggleMode = true; Alignment = HorizontalAlignment.Left; }
    private Vector2 IndicatorSize()
    {
        var result = Vector2.Zero;
        foreach (var name in IconNames) if (GetThemeIcon(name) is { } icon) result = result.Max(icon.GetSize());
        return FitButtonIcon(result);
    }
    internal override void RefreshButtonIndicator()
    {
        var width = IndicatorSize().X; SetButtonInternalMargins(IsLayoutRTL() ? 0 : width, IsLayoutRTL() ? width : 0);
    }
    internal override void PollButtonTextures(ref int count, ref bool changed)
    {
        base.PollButtonTextures(ref count, ref changed);
        foreach (var name in IconNames) PollButtonTexture(GetThemeIcon(name), ref count, ref changed);
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        var size = base.OnGetMinimumSize(); var icon = IndicatorSize();
        if (icon.X <= 0 && icon.Y <= 0) return size;
        var padding = LargestButtonStyleSize; var content = size - padding;
        if (content.X > 0 && icon.X > 0) content.X += ButtonSeparation;
        content.X += icon.X; content.Y = Math.Max(content.Y, icon.Y);
        return content + padding;
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what != NotificationDraw || IsDisposed) return;
        var radio = ButtonGroup is not null;
        var name = radio
            ? Disabled ? ButtonPressed ? "radio_checked_disabled" : "radio_unchecked_disabled" : ButtonPressed ? "radio_checked" : "radio_unchecked"
            : Disabled ? ButtonPressed ? "checked_disabled" : "unchecked_disabled" : ButtonPressed ? "checked" : "unchecked";
        var texture = GetThemeIcon(name) ?? throw new InvalidOperationException("A checkbox requires its indicator icon.");
        var size = IndicatorSize(); var style = GetThemeStyleBox("normal") ?? throw new InvalidOperationException("A checkbox requires its normal style.");
        var x = IsLayoutRTL() ? Size.X - style.GetMargin(Side.Right) - size.X : style.GetMargin(Side.Left);
        var y = checked((int)((Size.Y - size.Y) / 2)) + GetThemeConstant("check_v_offset");
        DrawTextureRect(texture, new(new(x, y), FitButtonIcon(texture.GetSize())), false,
            GetThemeColor(ButtonPressed ? "checkbox_checked_color" : "checkbox_unchecked_color"));
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(CheckBox) ? CreateCheckBox : base.CreateSceneInstanceFactory();
    private static Node CreateCheckBox() => new CheckBox();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) if (property.Name != nameof(Alignment) && property.Name != nameof(ToggleMode)) yield return property;
        foreach (var property in CheckProperties) yield return property;
    }
}
