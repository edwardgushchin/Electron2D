namespace Electron2D;

public abstract partial class CollisionObject
{
    private bool _inputPickable = true;
    /// <summary>Gets or sets whether unhandled viewport pointer events can pick this object's shapes.</summary>
    /// <value>True for collision objects and areas; physics bodies default to false.</value>
    /// <remarks>Picking also requires a nonzero collision layer, visible active scene membership and processing eligibility.
    /// Changes affect the next physics picking pass, without changing simulation or ordinary queries.</remarks>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The object is disposed.</exception>
    public bool InputPickable { get { ThrowIfDisposed(); return _inputPickable; } set { EnsureMutable(); _inputPickable = value; } }

    /// <summary>Occurs after the virtual input callback for a picked logical shape.</summary>
    /// <remarks>Arguments are the receiving viewport node, its borrowed local event and the sampled global shape index.
    /// The event is valid only during delivery; duplicate it to retain it. Mark the viewport input handled to stop later hits.</remarks>
    public event Action<Node, InputEvent, int>? InputEvent;
    /// <summary>Occurs when the pointer first enters this object's shapes.</summary>
    public event Action? MouseEntered;
    /// <summary>Occurs when the pointer leaves all of this object's shapes.</summary>
    public event Action? MouseExited;
    /// <summary>Occurs when the pointer enters a logical shape; the argument is its global shape index.</summary>
    public event Action<int>? MouseShapeEntered;
    /// <summary>Occurs when the pointer leaves a logical shape; the argument is its previously sampled global index.</summary>
    public event Action<int>? MouseShapeExited;

    /// <summary>Receives an unhandled pointer event during the viewport's physics picking pass.</summary>
    /// <param name="viewport">The viewport delivering the event.</param>
    /// <param name="inputEvent">A borrowed event in viewport coordinates.</param>
    /// <param name="shapeIndex">The global logical shape index.</param>
    protected virtual void OnInputEvent(Viewport viewport, InputEvent inputEvent, int shapeIndex) { }
    /// <summary>Runs before MouseEntered when the pointer first enters the object.</summary>
    protected virtual void OnMouseEnter() { }
    /// <summary>Runs before MouseExited when the pointer leaves the object.</summary>
    protected virtual void OnMouseExit() { }
    /// <summary>Runs before MouseShapeEntered for a newly entered shape.</summary>
    /// <param name="shapeIndex">The global logical shape index.</param>
    protected virtual void OnMouseShapeEnter(int shapeIndex) { }
    /// <summary>Runs before MouseShapeExited for a departed shape.</summary>
    /// <param name="shapeIndex">The previously sampled global shape index.</param>
    protected virtual void OnMouseShapeExit(int shapeIndex) { }

    internal void DispatchPhysicsPointer(int kind, Viewport viewport, InputEvent? input, int shape, ref List<Exception>? errors)
    {
        if (IsDisposed || !IsInsideTree) return;
        try
        {
            switch (kind)
            {
                case 0: OnInputEvent(viewport, input!, shape); break;
                case 1: OnMouseEnter(); break;
                case 2: OnMouseExit(); break;
                case 3: OnMouseShapeEnter(shape); break;
                case 4: OnMouseShapeExit(shape); break;
            }
        }
        catch (Exception error) { CollectException(ref errors, error); }
        if (IsDisposed || !IsInsideTree) return;
        if (kind == 0 && input is { IsDisposed: false } && InputEvent is { } inputs)
            foreach (var callback in Delegate.EnumerateInvocationList(inputs))
            {
                if (IsDisposed || !IsInsideTree || input.IsDisposed) break;
                try { callback(viewport, input, shape); } catch (Exception error) { CollectException(ref errors, error); }
            }
        var simple = kind == 1 ? MouseEntered : kind == 2 ? MouseExited : null;
        if (simple is not null) foreach (var callback in Delegate.EnumerateInvocationList(simple))
            { if (IsDisposed || !IsInsideTree) break; try { callback(); } catch (Exception error) { CollectException(ref errors, error); } }
        var shapes = kind == 3 ? MouseShapeEntered : kind == 4 ? MouseShapeExited : null;
        if (shapes is not null) foreach (var callback in Delegate.EnumerateInvocationList(shapes))
            { if (IsDisposed || !IsInsideTree) break; try { callback(shape); } catch (Exception error) { CollectException(ref errors, error); } }
    }
    private void ClearPhysicsInputHandlers() { InputEvent = null; MouseEntered = null; MouseExited = null; MouseShapeEntered = null; MouseShapeExited = null; }
}
