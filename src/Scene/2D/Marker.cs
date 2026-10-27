namespace Electron2D;

/// <summary>Marks a spatial location with an editor-only cross.</summary>
/// <remarks>The cross follows the inherited canvas transform, visibility and modulation. Its positive X/Y
/// axes are red/green and its negative axes use half brightness. It draws only inside a scene tree while
/// <see cref="Engine.IsEditorHint"/> is true; gameplay retains an ordinary spatial anchor.</remarks>
public class Marker : Entity
{
    private float _gizmoExtents = 10;

    /// <summary>Creates a marker with ten-unit editor half-axes.</summary>
    public Marker() { }

    /// <summary>Gets or sets the local half-axis length of the editor cross.</summary>
    /// <value>Ten initially. Zero hides the cross; a finite negative value reverses its axis colors.</value>
    /// <exception cref="ArgumentException">The value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The marker is disposed.</exception>
    public float GizmoExtents
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _gizmoExtents; }
        set { EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentException("Gizmo extents must be finite.", nameof(value)); _gizmoExtents = value; QueueRedraw(); }
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        var reversed = IsNotificationReversed;
        if (!reversed) base.OnNotification(what);
        if (what == NotificationDraw && IsInsideTree && Engine.IsEditorHint)
        {
            DrawLine(new(_gizmoExtents, 0), Vector2.Zero, new(.96f, .20f, .32f));
            DrawLine(Vector2.Zero, new(-_gizmoExtents, 0), new(.48f, .10f, .16f));
            DrawLine(new(0, _gizmoExtents), Vector2.Zero, new(.53f, .84f, .01f));
            DrawLine(Vector2.Zero, new(0, -_gizmoExtents), new(.265f, .42f, .005f));
        }
        if (reversed) base.OnNotification(what);
    }

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Marker) ? CreateMarker : base.CreateSceneInstanceFactory();
    private static Node CreateMarker() => new Marker();

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return ExtentsProperty;
    }
    private static readonly PropertyDescriptor ExtentsProperty = new PropertyDescriptor<Marker, float>(nameof(GizmoExtents), n => n.GizmoExtents, (n, v) => n.GizmoExtents = v, _ => 10, stored: true);
}
