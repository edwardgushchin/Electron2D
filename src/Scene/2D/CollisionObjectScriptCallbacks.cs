namespace Electron2D;

/// <summary>Adds typed physical picking callbacks to a live node Script callback set.</summary>
/// <typeparam name="TObject">Compatible collision-object receiver.</typeparam>
public class CollisionObjectScriptCallbacks<TObject> : CanvasItemScriptCallbacks<TObject> where TObject : CollisionObject
{
    /// <summary>Creates an unused node/canvas/picking callback set.</summary>
    public CollisionObjectScriptCallbacks() { }
    /// <summary>Receives an unhandled picked event with its viewport and global logical shape index.</summary>
    public Action<TObject, Viewport, InputEvent, int>? InputEvent { get; init; }
    /// <summary>Runs at the native mouse-entry hook before its typed signal.</summary>
    public Action<TObject>? MouseEnter { get; init; }
    /// <summary>Runs at the native mouse-exit hook before its typed signal.</summary>
    public Action<TObject>? MouseExit { get; init; }
    /// <summary>Receives the newly entered global logical shape index.</summary>
    public Action<TObject, int>? MouseShapeEnter { get; init; }
    /// <summary>Receives the departed global logical shape index.</summary>
    public Action<TObject, int>? MouseShapeExit { get; init; }
    internal override void Populate(NodeScriptBinding target, TObject owner)
    {
        base.Populate(target, owner);
        target.InputEvent = InputEvent is { } input ? (viewport, value, shape) => input(owner, viewport, value, shape) : null;
        target.MouseEnter = MouseEnter is { } enter ? () => enter(owner) : null;
        target.MouseExit = MouseExit is { } exit ? () => exit(owner) : null;
        target.MouseShapeEnter = MouseShapeEnter is { } shapeEnter ? shape => shapeEnter(owner, shape) : null;
        target.MouseShapeExit = MouseShapeExit is { } shapeExit ? shape => shapeExit(owner, shape) : null;
    }
}
