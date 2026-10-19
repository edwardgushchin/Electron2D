namespace Electron2D;

public partial class Window
{
    /// <summary>Selects placement when a window first opens or an embedded window becomes visible.</summary>
    public enum WindowInitialPosition
    {
        /// <summary>Retain the authored position, or let the native system place an unconfigured root.</summary>
        Absolute = 0,
        /// <summary>Center a native root on the primary screen.</summary>
        CenterPrimaryScreen = 1,
        /// <summary>Center a native root on its main-window screen.</summary>
        CenterMainWindowScreen = 2,
        /// <summary>Center a native root on CurrentScreen.</summary>
        CenterOtherScreen = 3,
        /// <summary>Center a native root on the screen selected by pointer focus.</summary>
        CenterScreenWithMouseFocus = 4,
        /// <summary>Center a native root on the screen selected by keyboard focus.</summary>
        CenterScreenWithKeyboardFocus = 5
    }
    private WindowInitialPosition _initialPosition;

    /// <summary>Gets or sets the stored initial window-placement policy.</summary>
    /// <value>Absolute initially. Every centered mode uses the containing viewport for embedded windows.</value>
    /// <remarks>Changing this value does not move an already visible window. Native root placement runs before
    /// scene entry and centers in full screen bounds. Wayland rejects explicit native positioning; embedded
    /// centering is available there. Native children apply the same policies when their host is created.
    /// Every valid assignment notifies PropertyListChanged after commitment, including an equal value.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The policy is undefined.</exception>
    /// <exception cref="Exception">A property-list observer fails after policy commitment.</exception>
    /// <exception cref="InvalidOperationException">Mutation violates scene ownership or capture guards.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public WindowInitialPosition InitialPosition
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _initialPosition; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _initialPosition = value; NotifyPropertyListChanged(); }
    }

    /// <summary>Requests the smallest client size permitted by the current window and content constraints.</summary>
    /// <remarks>Combines MinSize with scaled content minimum when WrapControls is enabled and the title minimum
    /// when required, then applies nonzero MaxSize axes. The portable viewport has a one-pixel minimum per axis.
    /// Native size changes follow the normal asynchronous SizeChanged path and may be further constrained.</remarks>
    /// <exception cref="OverflowException">Scaled content dimensions are not representable.</exception>
    /// <exception cref="InvalidOperationException">The owner, capture, content hook or native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void ResetSize() => Size = Vector2i.One;

    private void RequestMeasuredWindowSize(Vector2i size)
    {
        EnsureMutable(); RenderingOwner?.EnsureViewportMutation();
        if (_display is not null) _display.WindowSetSizeCore(size);
        else CommitSize(size);
    }
    private bool _measuringWindowMinimum;
    private Vector2i GetWindowMinimumSize()
    {
        if (_measuringWindowMinimum) throw new InvalidOperationException("Window minimum measurement cannot be reentered.");
        _measuringWindowMinimum = true;
        try
        {
            var minimum = _minSize.Max(TitleMinimum());
            if (_wrapControls)
            {
                var content = (GetContentsMinimumSize() * _contentScaleFactor).Ceil();
                minimum = minimum.Max(new Vector2i(checked((int)content.X), checked((int)content.Y)));
            }
            return minimum;
        }
        finally { _measuringWindowMinimum = false; }
    }
    private void ApplyInitialNativePosition()
    {
        if (_initialPosition == WindowInitialPosition.Absolute)
        {
            if (_screenPosition is { } position) _display!.WindowSetPositionCore(position);
            return;
        }
        var screen = _initialPosition switch
        {
            WindowInitialPosition.CenterPrimaryScreen => DisplayServer.ScreenPrimary,
            WindowInitialPosition.CenterMainWindowScreen => DisplayServer.ScreenOfMainWindow,
            WindowInitialPosition.CenterOtherScreen => _currentScreen ?? 0,
            WindowInitialPosition.CenterScreenWithMouseFocus => DisplayServer.ScreenWithMouseFocus,
            WindowInitialPosition.CenterScreenWithKeyboardFocus => DisplayServer.ScreenWithKeyboardFocus,
            _ => throw new InvalidOperationException("The stored placement policy is invalid.")
        };
        var positionOnScreen = _display!.ScreenGetPositionCore(screen);
        var screenSize = _display.ScreenGetSizeCore(screen);
        var size = _display.WindowGetSizeCore();
        _display.WindowSetPositionCore(new(checked((int)((long)positionOnScreen.X + ((long)screenSize.X - size.X) / 2)),
            checked((int)((long)positionOnScreen.Y + ((long)screenSize.Y - size.Y) / 2))));
    }
    private void ApplyInitialEmbeddedPosition()
    {
        if (_initialPosition != WindowInitialPosition.Absolute && Embedder is { } host)
        {
            var area = host.GetVisibleRect();
            Position = (Vector2i)((area.Size - (Vector2)Size) / 2);
        }
    }
}
