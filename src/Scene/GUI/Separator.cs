namespace Electron2D;

/// <summary>Provides the common theme and drawing contract for horizontal and vertical visual separators.</summary>
/// <remarks>The borrowed separator style draws across the long axis, centered by its truncated intrinsic
/// minimum on the short axis. Theme separation sets the intrinsic short-axis minimum; the long-axis minimum
/// is three pixels. Theme edits reuse inherited owner invalidation. Separators retain the ordinary stopping
/// pointer policy; decorative separators can select MouseFilter.Ignore.</remarks>
public abstract class Separator : Control
{
    private readonly bool _vertical;
    /// <summary>Constructs a separator with a fixed axis.</summary>
    /// <param name="vertical">True for a vertical line, false for a horizontal line.</param>
    protected Separator(bool vertical) => _vertical = vertical;
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        var separation = GetThemeConstant("separation");
        return _vertical ? new(separation, 3) : new(3, separation);
    }
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Theme lookup supplies no separator style.</exception>
    /// <exception cref="OverflowException">Control or style dimensions exceed pixel-coordinate integer bounds.</exception>
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what != NotificationDraw) return;
        var style = GetThemeStyleBox("separator") ?? throw new InvalidOperationException("Separator drawing requires its theme style.");
        var minimum = style.GetMinimumSize();
        var width = checked((int)Size.X); var height = checked((int)Size.Y);
        var styleWidth = checked((int)minimum.X); var styleHeight = checked((int)minimum.Y);
        var rect = _vertical ? new Rect2((width - (long)styleWidth) / 2, 0, styleWidth, height)
            : new Rect2(0, (height - (long)styleHeight) / 2, width, styleHeight);
        DrawStyleBox(style, rect);
    }
}

/// <summary>Draws a horizontal separator using the inherited separator style and spacing theme.</summary>
public class HSeparator : Separator
{
    /// <summary>Creates a horizontal separator with a three-pixel long-axis minimum.</summary>
    public HSeparator() : base(false) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(HSeparator) ? CreateHorizontal : base.CreateSceneInstanceFactory();
    private static Node CreateHorizontal() => new HSeparator();
}

/// <summary>Draws a vertical separator using the inherited separator style and spacing theme.</summary>
public class VSeparator : Separator
{
    /// <summary>Creates a vertical separator with a three-pixel long-axis minimum.</summary>
    public VSeparator() : base(true) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(VSeparator) ? CreateVertical : base.CreateSceneInstanceFactory();
    private static Node CreateVertical() => new VSeparator();
}
