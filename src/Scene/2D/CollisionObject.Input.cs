namespace Electron2D;

public abstract partial class CollisionObject
{
    private EventHandlers<Action<Node, InputEvent, int>>? _eventInputEvent;
    private EventHandlers<Action>? _eventMouseEntered;
    private EventHandlers<Action>? _eventMouseExited;
    private EventHandlers<Action<int>>? _eventMouseShapeEntered;
    private EventHandlers<Action<int>>? _eventMouseShapeExited;

    private bool _inputPickable = true;
    /// <summary>Gets or sets whether unhandled viewport pointer events can pick this object's shapes.</summary>
    /// <value>True for collision objects and areas; physics bodies default to false.</value>
    /// <remarks>Picking also requires a nonzero collision layer, visible active scene membership and processing eligibility.
    /// Changes affect the next physics picking pass, without changing simulation or ordinary queries.</remarks>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner, capture-owned, or the selected numerical implementation is borrowed.</exception>
    /// <exception cref="ObjectDisposedException">The object is disposed.</exception>
    public bool InputPickable { get { ThrowIfDisposed(); return _inputPickable; } set { EnsureMutable(); _inputPickable = value; } }

    /// <summary>Occurs after the virtual input callback for a picked logical shape.</summary>
    /// <remarks>Arguments are the receiving viewport node, its borrowed local event and the sampled global shape index.
    /// The event is valid only during delivery; duplicate it to retain it. Mark the viewport input handled to stop later hits. Handler order and duplicate entries are preserved in an emission snapshot. Subscription changes affect later emissions; prepared one-shot removal reuses handler capacity. Disposal clears publisher-owned handlers.</remarks>
    public event Action<Node, InputEvent, int>? InputEvent
    {
        add => EventHandlers<Action<Node, InputEvent, int>>.Add(ref _eventInputEvent, value);
        remove => _eventInputEvent?.Remove(value);
    }
    /// <summary>Occurs when the pointer first enters this object's shapes.</summary>
    /// <remarks>Handler order and duplicate entries are preserved in an emission snapshot. Subscription changes affect later emissions; prepared one-shot removal reuses handler capacity. Disposal clears publisher-owned handlers.</remarks>
    public event Action? MouseEntered
    {
        add => EventHandlers<Action>.Add(ref _eventMouseEntered, value);
        remove => _eventMouseEntered?.Remove(value);
    }
    /// <summary>Occurs when the pointer leaves all of this object's shapes.</summary>
    /// <remarks>Handler order and duplicate entries are preserved in an emission snapshot. Subscription changes affect later emissions; prepared one-shot removal reuses handler capacity. Disposal clears publisher-owned handlers.</remarks>
    public event Action? MouseExited
    {
        add => EventHandlers<Action>.Add(ref _eventMouseExited, value);
        remove => _eventMouseExited?.Remove(value);
    }
    /// <summary>Occurs when the pointer enters a logical shape; the argument is its global shape index.</summary>
    /// <remarks>Handler order and duplicate entries are preserved in an emission snapshot. Subscription changes affect later emissions; prepared one-shot removal reuses handler capacity. Disposal clears publisher-owned handlers.</remarks>
    public event Action<int>? MouseShapeEntered
    {
        add => EventHandlers<Action<int>>.Add(ref _eventMouseShapeEntered, value);
        remove => _eventMouseShapeEntered?.Remove(value);
    }
    /// <summary>Occurs when the pointer leaves a logical shape; the argument is its previously sampled global index.</summary>
    /// <remarks>Handler order and duplicate entries are preserved in an emission snapshot. Subscription changes affect later emissions; prepared one-shot removal reuses handler capacity. Disposal clears publisher-owned handlers.</remarks>
    public event Action<int>? MouseShapeExited
    {
        add => EventHandlers<Action<int>>.Add(ref _eventMouseShapeExited, value);
        remove => _eventMouseShapeExited?.Remove(value);
    }

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
        if (kind == 0 && input is { IsDisposed: false })
        {
            using var inputs = BeginBufferedEvent(_eventInputEvent);
            for (var i = 0; i < inputs.Count; i++)
            {
                if (IsDisposed || !IsInsideTree || input.IsDisposed) break;
                try { inputs[i](viewport, input, shape); } catch (Exception error) { CollectException(ref errors, error); }
            }
        }
        using var simple = BeginBufferedEvent((kind == 1 ? _eventMouseEntered : kind == 2 ? _eventMouseExited : null));
        for (var i = 0; i < simple.Count; i++)
        { if (IsDisposed || !IsInsideTree) break; try { simple[i](); } catch (Exception error) { CollectException(ref errors, error); } }
        using var shapes = BeginBufferedEvent((kind == 3 ? _eventMouseShapeEntered : kind == 4 ? _eventMouseShapeExited : null));
        for (var i = 0; i < shapes.Count; i++)
        { if (IsDisposed || !IsInsideTree) break; try { shapes[i](shape); } catch (Exception error) { CollectException(ref errors, error); } }
    }
    internal virtual void ClearPhysicsEventHandlers()
    {
        _eventInputEvent?.Clear(); _eventMouseEntered?.Clear(); _eventMouseExited?.Clear();
        _eventMouseShapeEntered?.Clear(); _eventMouseShapeExited?.Clear();
    }
}
