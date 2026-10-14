namespace Electron2D;

/// <summary>Draws a solid color across this control's local rectangle.</summary>
/// <remarks>The color is independent of the inherited canvas modulation. Intrinsic minimum size is zero.
/// Pointer input stops by default; decorative backgrounds can select MouseFilter.Ignore.
/// Attached access requires the scene owner thread and authored mutation is forbidden during capture.</remarks>
public class ColorRect : Control
{
    private Color _color = Colors.White;
    /// <summary>Creates an opaque white rectangle with the inherited control defaults.</summary>
    public ColorRect() { }
    /// <summary>Gets or sets the fill color.</summary>
    /// <value>Opaque white initially. Finite HDR and signed channels are retained; equal writes are silent.</value>
    /// <exception cref="ArgumentException">A channel is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The rectangle is disposed.</exception>
    public Color Color
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _color; }
        set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Color channels must be finite.", nameof(value)); if (_color == value) return; _color = value; QueueRedraw(); }
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationDraw) DrawRect(new(Vector2.Zero, Size), _color);
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(ColorRect) ? CreateRectangle : base.CreateSceneInstanceFactory();
    private static Node CreateRectangle() => new ColorRect();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return FillProperty;
    }
    private static readonly PropertyDescriptor FillProperty = new PropertyDescriptor<ColorRect, Color>(nameof(Color), c => c.Color, (c, v) => c.Color = v, _ => Colors.White, stored: true);
}
