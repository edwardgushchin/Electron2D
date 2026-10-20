namespace Electron2D;

public sealed partial class RenderingServer
{
    private List<Action> _queuedFrameCallbacks = [];
    private List<Action> _completedFrameCallbacks = [];

    /// <summary>Draws the active native scene immediately, including eligible offscreen viewports.</summary>
    /// <param name="swapBuffers">Presents the root window when true; false still renders its texture.</param>
    /// <param name="frameStep">Finite nonnegative increment of the rendering clock, independent of scene processing.</param>
    /// <remarks>Runs on the native owner thread and bypasses RenderLoopEnabled. It does not advance process or physics callbacks.
    /// Uses the same retained canvas, materials, compute/readback and visibility pipeline as an automatic frame.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The rendering step is negative, nonfinite or overflows the clock.</exception>
    /// <exception cref="InvalidOperationException">No active native scene exists, the caller is off-owner or a frame is already executing.</exception>
    public static void ForceDraw(bool swapBuffers = true, double frameStep = 0d)
    {
        var server = RequireService();
        server.EnsureOwner();
        if (!double.IsFinite(frameStep) || frameStep < 0 || !double.IsFinite(server.CanvasTime + frameStep)) throw new ArgumentOutOfRangeException(nameof(frameStep));
        var tree = server._window.Tree ?? throw new InvalidOperationException("The native window has no active scene tree.");
        server.Render(tree, frameStep, force: true, swapBuffers);
    }

    /// <summary>Submits pending renderer compute work and waits for prior GPU operations to complete.</summary>
    /// <remarks>First drains queued render-thread actions. The synchronization itself does not draw, present or consume
    /// completed-frame requests; user actions retain their ordinary operations. This cold blocking operation has a
    /// synchronization cost. Use the native owner outside capture/submission, or after a frame has completed.
    /// Selected action errors propagate after native synchronization/readback completion; several errors aggregate.</remarks>
    /// <exception cref="InvalidOperationException">No renderer exists, the caller is off-owner or a frame is still being captured/submitted.</exception>
    public static void ForceSync()
    {
        var server = RequireService(); server.EnsureOwner();
        if (server._rendering && !server._frameSubmitted) throw new InvalidOperationException("A frame cannot be synchronized before its submission completes.");
        var error = server.DrainRenderCallbacks();
        try
        {
            server.EnsureOwner();
            server._renderingDevice?.FlushRendererCommands();
            server._backend.Sync(server._backend.FindTarget(server._window));
            server._renderingDevice?.CompleteRendererReadbacks();
        }
        catch (Exception next) { AddCanvasVisibilityError(ref error, next); }
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }

    /// <summary>Schedules an owner-thread callback after the next completed canvas frame.</summary>
    /// <param name="callback">An application-owned action; captured objects must remain valid until invocation.</param>
    /// <remarks>Each request executes once, in request order, after readbacks and FramePostDraw. Requests made during capture
    /// join that frame; requests made after submission wait for a later frame. All selected actions execute even if another
    /// observer/action fails; errors propagate after native commitment and cleanup. Reentrant drawing is rejected.
    /// Session teardown releases pending captures without invoking them.</remarks>
    /// <exception cref="ArgumentNullException">The callback is null.</exception>
    /// <exception cref="InvalidOperationException">No renderer exists or the caller is off-owner.</exception>
    public static void RequestFrameDrawnCallback(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var server = RequireService(); server.EnsureOwner();
        server._queuedFrameCallbacks.Add(callback);
        server.MarkChanged();
    }

    private void CaptureFrameCallbacks()
    {
        (_queuedFrameCallbacks, _completedFrameCallbacks) = (_completedFrameCallbacks, _queuedFrameCallbacks);
    }

    private void DeliverFrameCallbacks(ref Exception? error)
    {
        for (var i = 0; i < _completedFrameCallbacks.Count; i++)
        {
            var callback = _completedFrameCallbacks[i];
            _completedFrameCallbacks[i] = null!;
            try { callback(); }
            catch (Exception callbackError) { error = error is null ? callbackError : new AggregateException(error, callbackError); }
        }
        _completedFrameCallbacks.Clear();
    }
}
