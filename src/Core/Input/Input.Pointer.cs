namespace Electron2D;

public sealed partial class Input
{
    private int _defaultCursorShape;

    internal CursorShape DefaultCursorShape => (CursorShape)Volatile.Read(ref _defaultCursorShape);

    /// <summary>Controls visibility and confinement of the main window's mouse pointer.</summary>
    /// <remarks>The Enum suffix avoids a C# name collision with the MouseMode property.</remarks>
    public enum MouseModeEnum
    {
        /// <summary>Visible, unrestricted pointer.</summary>
        Visible = 0,
        /// <summary>Hidden, unrestricted pointer.</summary>
        Hidden = 1,
        /// <summary>Hidden pointer with relative motion.</summary>
        Captured = 2,
        /// <summary>Visible pointer confined to the window.</summary>
        Confined = 3,
        /// <summary>Hidden pointer confined to the window.</summary>
        ConfinedHidden = 4,
        /// <summary>Number of modes; not selectable.</summary>
        Max = 5,
    }

    /// <summary>Identifies the theme cursor to display in the main window.</summary>
    public enum CursorShape
    {
        /// <summary>Ordinary arrow.</summary>
        Arrow = 0,
        /// <summary>Text selection.</summary>
        IBeam = 1,
        /// <summary>Clickable link.</summary>
        PointingHand = 2,
        /// <summary>Crosshair.</summary>
        Cross = 3,
        /// <summary>Nonblocking wait.</summary>
        Wait = 4,
        /// <summary>Blocking wait.</summary>
        Busy = 5,
        /// <summary>Drag.</summary>
        Drag = 6,
        /// <summary>Drop allowed.</summary>
        CanDrop = 7,
        /// <summary>Drop forbidden.</summary>
        Forbidden = 8,
        /// <summary>Vertical resize.</summary>
        VSize = 9,
        /// <summary>Horizontal resize.</summary>
        HSize = 10,
        /// <summary>Northeast-southwest diagonal resize.</summary>
        BDiagSize = 11,
        /// <summary>Northwest-southeast diagonal resize.</summary>
        FDiagSize = 12,
        /// <summary>Move in any direction.</summary>
        Move = 13,
        /// <summary>Vertical split resize.</summary>
        VSplit = 14,
        /// <summary>Horizontal split resize.</summary>
        HSplit = 15,
        /// <summary>Help.</summary>
        Help = 16,
    }

    /// <summary>Gets or sets the active native pointer mode.</summary>
    /// <remarks>The setter applies immediately to the active display and retains the previous mode if SDL rejects it.</remarks>
    /// <exception cref="InvalidOperationException">There is no active display or the native change fails.</exception>
    public MouseModeEnum MouseMode
    {
        get => (MouseModeEnum)RequireDisplay().MouseGetMode();
        set => RequireDisplay().MouseSetMode((DisplayServer.MouseMode)value);
    }

    /// <summary>Gets the shape most recently installed on the active display.</summary>
    /// <returns>The currently selected shape, including one selected directly through DisplayServer.</returns>
    /// <exception cref="InvalidOperationException">There is no active display.</exception>
    public CursorShape GetCurrentCursorShape() => (CursorShape)RequireDisplay().CursorGetShape();

    /// <summary>Sets the viewport's default native cursor shape and refreshes its current selection.</summary>
    /// <param name="shape">A standard shape; Arrow by default.</param>
    /// <remarks>A hovered Control can override this default. The active scene's cursor is refreshed without synthesizing a mouse-motion input event.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The shape is invalid.</exception>
    /// <exception cref="InvalidOperationException">There is no active display or the native change fails.</exception>
    public void SetDefaultCursorShape(CursorShape shape = CursorShape.Arrow)
    {
        RequireDisplay().CursorSetShape((DisplayServer.CursorShape)shape);
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
        RequireDisplay().CursorSetCustomImage(image, (DisplayServer.CursorShape)shape, hotspot);

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
        RequireDisplay().WarpMouse(new Vector2I((int)position.X, (int)position.Y));
    }

    private static DisplayServer RequireDisplay() => DisplayServer.Instance ??
        throw new InvalidOperationException("Pointer control requires an active display server.");
}
