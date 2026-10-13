namespace Electron2D;

public sealed partial class SceneTree
{
    private readonly HashSet<(Window Window, bool GlobalInput)> _cursorRefreshWindows = [];
    private bool _refreshingMouseCursor;
    internal void RequestMouseCursorRefresh(Window window, bool globalInput = false)
    {
        ThrowIfDisposed(); EnsureOwnerThread(); EnsureAcceptingWork();
        if (!ReferenceEquals(window.Tree, this)) throw new InvalidOperationException("Cursor refresh window belongs to a different tree.");
        _ = window.GetClientMousePosition();
        if (_refreshingMouseCursor) return;
        if (_activeExecution != 0 || _lifecycleExecutionDepth != 0 || _isChangingPause)
        {
            var key = (window, globalInput);
            if (!_cursorRefreshWindows.Add(key)) return;
            Defer(() =>
            {
                if (!_cursorRefreshWindows.Remove(key) || window.IsDisposed || !ReferenceEquals(window.Tree, this)) return;
                RefreshMouseCursor(window, globalInput, executionOwned: true);
            });
            return;
        }
        RefreshMouseCursor(window, globalInput, executionOwned: false);
    }
    private void RefreshMouseCursor(Window window, bool globalInput, bool executionOwned)
    {
        _refreshingMouseCursor = true;
        try
        {
            using var motion = globalInput ? Input.Service.CreateCursorRefreshEvent() : new InputEventMouseMotion
            {
                Device = InputEvent.DeviceIdInternal,
                Position = window.GetMousePosition(),
            };
            if (!globalInput) motion.GlobalPosition = window.GlobalCanvasTransform.AffineInverse() * motion.Position;
            DispatchViewportInput(window, motion, inLocalCoordinates: !globalInput, executionOwned: executionOwned);
        }
        finally { _refreshingMouseCursor = false; }
    }
}
