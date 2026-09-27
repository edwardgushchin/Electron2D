namespace Electron2D;

/// <summary>A left-aligned toggle button with a switch indicator after its content.</summary>
/// <remarks>Checked/unchecked, disabled and mirrored theme icons determine the indicator size and appearance.
/// The indicator follows RTL layout and preserves fractional vertical centering.</remarks>
public class CheckButton : Button
{
    private static readonly string[] IconNames = ["checked", "unchecked", "checked_disabled", "unchecked_disabled",
        "checked_mirrored", "unchecked_mirrored", "checked_disabled_mirrored", "unchecked_disabled_mirrored"];
    private static readonly PropertyDescriptor[] CheckProperties =
    [
        new PropertyDescriptor<CheckButton, HorizontalAlignment>(nameof(Alignment), b => b.Alignment, (b,v) => b.Alignment = v, _ => HorizontalAlignment.Left, stored: true),
        new PropertyDescriptor<CheckButton, bool>(nameof(ToggleMode), b => b.ToggleMode, (b,v) => b.ToggleMode = v, _ => true, stored: true)
    ];
    /// <summary>Creates an empty check button with toggle mode enabled.</summary>
    public CheckButton() : this(string.Empty) { }
    /// <summary>Creates a check button with untranslated text.</summary>
    /// <param name="text">Initial text; may be empty.</param>
    /// <exception cref="ArgumentNullException">The text is null.</exception>
    public CheckButton(string text) : base(text) { ToggleMode = true; Alignment = HorizontalAlignment.Left; }
    private string IndicatorName(bool pressed) => IsLayoutRTL()
        ? Disabled ? pressed ? "checked_disabled_mirrored" : "unchecked_disabled_mirrored" : pressed ? "checked_mirrored" : "unchecked_mirrored"
        : Disabled ? pressed ? "checked_disabled" : "unchecked_disabled" : pressed ? "checked" : "unchecked";
    private Vector2 IndicatorSize()
    {
        var on = GetThemeIcon(IndicatorName(true)); var off = GetThemeIcon(IndicatorName(false));
        return FitButtonIcon((on?.GetSize() ?? Vector2.Zero).Max(off?.GetSize() ?? Vector2.Zero));
    }
    internal override void RefreshButtonIndicator()
    {
        var width = IndicatorSize().X; SetButtonInternalMargins(IsLayoutRTL() ? width : 0, IsLayoutRTL() ? 0 : width);
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
        var texture = GetThemeIcon(IndicatorName(ButtonPressed)) ?? throw new InvalidOperationException("A check button requires its indicator icon.");
        var size = IndicatorSize(); var style = GetThemeStyleBox("normal") ?? throw new InvalidOperationException("A check button requires its normal style.");
        var x = IsLayoutRTL() ? style.GetMargin(Side.Left) : Size.X - size.X - style.GetMargin(Side.Right);
        var y = (Size.Y - size.Y) / 2 + GetThemeConstant("check_v_offset");
        DrawTextureRect(texture, new(new(x, y), FitButtonIcon(texture.GetSize())), false,
            GetThemeColor(ButtonPressed ? "button_checked_color" : "button_unchecked_color"));
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(CheckButton) ? CreateCheckButton : base.CreateSceneInstanceFactory();
    private static Node CreateCheckButton() => new CheckButton();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) if (property.Name != nameof(Alignment) && property.Name != nameof(ToggleMode)) yield return property;
        foreach (var property in CheckProperties) yield return property;
    }
}
