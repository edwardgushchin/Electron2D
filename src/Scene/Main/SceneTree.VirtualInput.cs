namespace Electron2D;

public sealed partial class SceneTree
{
    private readonly Queue<(ObjectIdentity Owner, ObjectIdentity Viewport, string Action, bool Pressed)> _virtualInput = [];
    private bool _drainingVirtualInput;
    internal void QueueVirtualAction(Node owner, string action, bool pressed)
    {
        EnsureOwnerThread();
        if (owner.GetViewport() is not { } viewport || action.Length == 0) return;
        if (_virtualInput.Count >= 65536) throw new InvalidOperationException("Too many generated input transitions.");
        _virtualInput.Enqueue((owner.BorrowIdentity(), viewport.BorrowIdentity(), action, pressed));
        if (!_isDispatchingInput && !_drainingVirtualInput)
        { List<Exception>? errors = null; DrainVirtualInput(ref errors); ThrowCollected("Generated input callbacks failed.", errors); }
    }
    private void DrainVirtualInput(ref List<Exception>? errors)
    {
        if (_drainingVirtualInput || _isDispatchingInput) return;
        _drainingVirtualInput = true;
        try
        {
            var delivered = 0;
            while (_virtualInput.TryDequeue(out var pending))
            {
                if (++delivered > 65536) { _virtualInput.Clear(); CollectException(ref errors, new InvalidOperationException("Generated input callbacks did not converge.")); break; }
                if (pending.Owner.Target is not Node { IsInsideTree: true } owner || !ReferenceEquals(owner.Tree, this) || pending.Viewport.Target is not Viewport viewport || !ReferenceEquals(viewport.Tree, this)) continue;
                try
                {
                    using var input = new InputEventAction { Action = pending.Action, Pressed = pending.Pressed, Strength = pending.Pressed ? 1 : 0 };
                    DispatchViewportInput(viewport, input, inLocalCoordinates: true, executionOwned: _activeExecution != 0);
                }
                catch (Exception error) { CollectException(ref errors, error); }
            }
        }
        finally { _drainingVirtualInput = false; }
    }
}
