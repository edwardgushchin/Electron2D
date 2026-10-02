namespace Electron2D;

public sealed partial class Input
{
    private int _defaultCursorShape;

    internal CursorShape DefaultCursorShape => (CursorShape)Volatile.Read(ref _defaultCursorShape);

    /// <summary>Gets or sets the active native pointer mode.</summary>
    /// <remarks>The setter applies immediately to the active display and retains the previous mode if SDL rejects it.</remarks>
    /// <exception cref="InvalidOperationException">There is no active display or the native change fails.</exception>
    public MouseMode MouseMode
    {
        get => RequireDisplay().MouseGetMode();
        set => RequireDisplay().MouseSetMode(value);
    }

    /// <summary>Gets the shape most recently installed on the active display.</summary>
    /// <returns>The currently selected shape, including one selected directly through DisplayServer.</returns>
    /// <exception cref="InvalidOperationException">There is no active display.</exception>
    public CursorShape GetCurrentCursorShape() => RequireDisplay().CursorGetShape();

    /// <summary>Sets the viewport's default native cursor shape and refreshes its current selection.</summary>
    /// <param name="shape">A standard shape; Arrow by default.</param>
    /// <remarks>A hovered Control can override this default. The active scene's cursor is refreshed without synthesizing a mouse-motion input event.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The shape is invalid.</exception>
    /// <exception cref="InvalidOperationException">There is no active display or the native change fails.</exception>
    public void SetDefaultCursorShape(CursorShape shape = CursorShape.Arrow)
    {
        RequireDisplay().CursorSetShape(shape);
        Volatile.Write(ref _defaultCursorShape, (int)shape);
        if (Engine.Instance.MainLoop is SceneTree tree)
            tree.RefreshGUICursor();
    }

    /// <summary>Installs or clears a copied image for one native cursor shape.</summary>
    /// <param name="image">A caller-owned Image or readable Texture, or null to restore the system shape.</param>
    /// <param name="shape">The shape slot to customize.</param>
    /// <param name="hotspot">The active pixel position in the source image.</param>
    /// <remarks>The display copies the pixels before return. A later edit of the source requires another call.</remarks>
    public void SetCustomMouseCursor(Resource? image, CursorShape shape = CursorShape.Arrow, Vector2 hotspot = default) =>
        RequireDisplay().CursorSetCustomImage(image, shape, hotspot);

    /// <summary>Requests pointer movement to a client-area position on backends that support warping.</summary>
    /// <param name="position">Finite client coordinates; fractional values are truncated to native integer units.</param>
    /// <exception cref="ArgumentException">The position is nonfinite or outside native integer coordinates.</exception>
    /// <exception cref="InvalidOperationException">There is no active display.</exception>
    /// <exception cref="NotSupportedException">The display backend does not support pointer warping.</exception>
    public void WarpMouse(Vector2 position)
    {
        if (!position.IsFinite() || (double)position.X < int.MinValue || (double)position.X > int.MaxValue ||
            (double)position.Y < int.MinValue || (double)position.Y > int.MaxValue)
            throw new ArgumentException("Pointer coordinates must be finite and within native integer limits.", nameof(position));
        RequireDisplay().WarpMouse(new Vector2i((int)position.X, (int)position.Y));
    }

    private static DisplayServer RequireDisplay() => DisplayServer.Instance ??
        throw new InvalidOperationException("Pointer control requires an active display server.");
}
