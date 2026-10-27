namespace Electron2D;

/// <summary>Draws a reference border around this control's local rectangle.</summary>
/// <remarks>Editor-only by default. The border follows inherited layout, transform, visibility and modulation;
/// it does not change minimum size or input policy. Attached access requires the scene owner thread.</remarks>
public class ReferenceRect : Control
{
    private Color _borderColor = Colors.Red;
    private float _borderWidth = 1;
    private bool _editorOnly = true;

    /// <summary>Creates a control with a one-unit red editor border.</summary>
    public ReferenceRect() { }

    /// <summary>Gets or sets the border color.</summary>
    /// <value>Opaque red initially; finite HDR and signed channels are retained.</value>
    /// <exception cref="ArgumentException">A channel is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public Color BorderColor
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _borderColor; }
        set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Border color must be finite.", nameof(value)); if (_borderColor == value) return; _borderColor = value; QueueRedraw(); }
    }

    /// <summary>Gets or sets the nonnegative border width in local units.</summary>
    /// <value>One initially. Negative finite values clamp to zero; zero draws no border.</value>
    /// <exception cref="ArgumentException">The width is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public float BorderWidth
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _borderWidth; }
        set { EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentException("Border width must be finite.", nameof(value)); value = MathF.Max(0, value); if (_borderWidth == value) return; _borderWidth = value; QueueRedraw(); }
    }

    /// <summary>Gets or sets whether the border requires the editor role.</summary>
    /// <value>True initially. False also draws the border during gameplay.</value>
    /// <remarks>Changes and <see cref="Engine.IsEditorHint"/> transitions affect the next canvas submission.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool EditorOnly
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _editorOnly; }
        set { EnsureMutable(); if (_editorOnly == value) return; _editorOnly = value; QueueRedraw(); }
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        var reversed = IsNotificationReversed;
        if (!reversed) base.OnNotification(what);
        if (what == NotificationDraw && IsInsideTree && (Engine.IsEditorHint || !_editorOnly))
            DrawRect(new(Vector2.Zero, Size), _borderColor, false, _borderWidth);
        if (reversed) base.OnNotification(what);
    }

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(ReferenceRect) ? CreateReferenceRect : base.CreateSceneInstanceFactory();
    private static Node CreateReferenceRect() => new ReferenceRect();

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        foreach (var property in BorderProperties) yield return property;
    }
    private static readonly PropertyDescriptor[] BorderProperties =
    [
        new PropertyDescriptor<ReferenceRect, Color>(nameof(BorderColor), n => n.BorderColor, (n, v) => n.BorderColor = v, _ => Colors.Red, stored: true),
        new PropertyDescriptor<ReferenceRect, float>(nameof(BorderWidth), n => n.BorderWidth, (n, v) => n.BorderWidth = v, _ => 1, stored: true),
        new PropertyDescriptor<ReferenceRect, bool>(nameof(EditorOnly), n => n.EditorOnly, (n, v) => n.EditorOnly = v, _ => true, stored: true)
    ];
}
