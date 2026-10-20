using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly object _renderCallbacksGate = new();
    private List<Action> _queuedRenderCallbacks = [], _activeRenderCallbacks = [];
    private bool _acceptRenderCallbacks = true, _drainingRenderCallbacks;

    /// <summary>Reports whether this caller is the active renderer's native owner thread.</summary>
    /// <returns>True on the scene/render owner thread; false on another thread.</returns>
    /// <remarks>The current canvas backends render on the scene owner thread. This observation is safe from any
    /// thread and does not reserve the session or relax resource access rules.</remarks>
    /// <exception cref="InvalidOperationException">The rendering session is absent or retiring.</exception>
    public static bool IsOnRenderThread()
    {
        var server = RequireService();
        lock (server._renderCallbacksGate) { server.CheckRenderCallbacks(); return server._ownerThread == Environment.CurrentManagedThreadId; }
    }

    /// <summary>Runs an action on the active renderer's native owner thread.</summary>
    /// <param name="callback">A borrowed action whose captured objects must remain valid until invocation.</param>
    /// <remarks>Other threads enqueue and return. The owner drains a FIFO snapshot before the next automatic or
    /// forced render attempt, even with drawing disabled, or during ForceSync. Owner calls drain prior requests and
    /// then invoke inline; nested owner calls invoke inline without recursively draining. Actions accepted during
    /// a drain wait for a later drain. All selected actions are attempted; errors propagate on the owner after the
    /// selected actions and any frame/synchronization work. Teardown cancels pending actions and releases captures;
    /// requests never transfer to a later session. Existing list, rendering and resource guards still apply.</remarks>
    /// <exception cref="ArgumentNullException">The action is null.</exception>
    /// <exception cref="InvalidOperationException">The rendering session is absent or retiring.</exception>
    /// <exception cref="AggregateException">Several selected actions or subsequent native operations failed.</exception>
    public static void CallOnRenderThread(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback); var server = RequireService();
        lock (server._renderCallbacksGate)
        {
            server.CheckRenderCallbacks();
            if (server._ownerThread != Environment.CurrentManagedThreadId) { server._queuedRenderCallbacks.Add(callback); server.MarkChanged(); return; }
        }
        var error = server.DrainRenderCallbacks();
        try { lock (server._renderCallbacksGate) server.CheckRenderCallbacks(); callback(); }
        catch (Exception next) { AddCanvasVisibilityError(ref error, next); }
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }

    private void CheckRenderCallbacks()
    {
        if (!_acceptRenderCallbacks || IsDisposed || !ReferenceEquals(Service, this)) throw new InvalidOperationException("The rendering session is retiring.");
    }

    private Exception? DrainRenderCallbacks()
    {
        if (_drainingRenderCallbacks) return null;
        lock (_renderCallbacksGate)
        {
            if (_queuedRenderCallbacks.Count == 0) return null;
            (_queuedRenderCallbacks, _activeRenderCallbacks) = (_activeRenderCallbacks, _queuedRenderCallbacks);
        }
        Exception? error = null; _drainingRenderCallbacks = true;
        try
        {
            for (var i = 0; i < _activeRenderCallbacks.Count; i++)
            {
                var callback = _activeRenderCallbacks[i]; _activeRenderCallbacks[i] = null!;
                try { callback(); } catch (Exception next) { AddCanvasVisibilityError(ref error, next); }
            }
        }
        finally { _activeRenderCallbacks.Clear(); _drainingRenderCallbacks = false; }
        return error;
    }

    private void RetireRenderCallbacks()
    {
        lock (_renderCallbacksGate) { _acceptRenderCallbacks = false; _queuedRenderCallbacks.Clear(); _activeRenderCallbacks.Clear(); }
    }
}
