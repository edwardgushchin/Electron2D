namespace Electron2D;

public partial class Window
{
    /// <summary>Starts moving the window from a left-button press in its client area.</summary>
    /// <remarks>Embedded windows move within their viewport. Native windows use the active
    /// window manager through <see cref="DisplayServer.WindowStartDrag"/>. An inactive window or
    /// a window without an initiating press is unchanged.</remarks>
    /// <exception cref="InvalidOperationException">Mutation violates the scene owner or capture policy.</exception>
    /// <exception cref="NotSupportedException">The native display backend or window manager lacks this operation.</exception>
    /// <exception cref="ObjectDisposedException">This window is disposed.</exception>
    public void StartDrag()
    {
        EnsureMutable();
        if (!Visible)
            return;
        if (Embedder is not null)
            Tree!.StartEmbeddedWindowGesture(this, 0);
        else
            _display?.WindowStartGestureCore(null);
    }

    /// <summary>Starts resizing the window from the selected edge or corner during a left-button press.</summary>
    /// <param name="edge">The edge or corner to move.</param>
    /// <remarks>Embedded windows apply their client size limits and preserve the opposite edge.
    /// Native windows follow <see cref="DisplayServer.WindowStartResize"/>. A window with resizing
    /// disabled ignores the request.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The edge is invalid.</exception>
    /// <exception cref="InvalidOperationException">Mutation violates the scene owner or capture policy.</exception>
    /// <exception cref="NotSupportedException">The native display backend or window manager lacks this operation.</exception>
    /// <exception cref="ObjectDisposedException">This window is disposed.</exception>
    public void StartResize(DisplayServer.WindowResizeEdge edge)
    {
        EnsureMutable();
        DisplayServer.ValidateWindowResizeEdge(edge);
        if (!Visible || Unresizable)
            return;
        if (Embedder is not null)
        {
            var edges = edge switch
            {
                DisplayServer.WindowResizeEdge.TopLeft => 5,
                DisplayServer.WindowResizeEdge.Top => 4,
                DisplayServer.WindowResizeEdge.TopRight => 6,
                DisplayServer.WindowResizeEdge.Left => 1,
                DisplayServer.WindowResizeEdge.Right => 2,
                DisplayServer.WindowResizeEdge.BottomLeft => 9,
                DisplayServer.WindowResizeEdge.Bottom => 8,
                _ => 10,
            };
            Tree!.StartEmbeddedWindowGesture(this, edges);
        }
        else
            _display?.WindowStartGestureCore(edge);
    }
}
