namespace Electron2D;

/// <summary>Adds retained canvas recording to a live node Script callback set.</summary>
/// <typeparam name="TItem">Compatible canvas-item receiver.</typeparam>
public class CanvasItemScriptCallbacks<TItem> : NodeScriptCallbacks<TItem> where TItem : CanvasItem
{
    /// <summary>Creates an unused node/canvas callback set.</summary>
    public CanvasItemScriptCallbacks() { }
    /// <summary>Records commands after the native drawing hook in this item's ordinary recording scope.</summary>
    /// <remarks>Runs after the draw notification and synchronous Draw signal. Signal blocking does not suppress it.
    /// Retained replay does not invoke it. Failure discards partial recording and leaves a pending retry.
    /// Installing or retiring this callback requests a fresh recording.</remarks>
    public Action<TItem>? Draw { get; init; }
    internal override void Populate(ObjectScriptBinding binding, TItem owner)
    {
        base.Populate(binding, owner);
        var target = (NodeScriptBinding)binding;
        target.Draw = Draw is { } draw ? () => draw(owner) : null;
    }
}
