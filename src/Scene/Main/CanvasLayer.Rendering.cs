namespace Electron2D;

public partial class CanvasLayer
{
    private RenderingCanvasRuntime? _canvasRuntime;
    internal RenderingCanvasRuntime CanvasRuntime { get { CheckQuery(); return _canvasRuntime ??= RenderingCanvasRegistry.Register(layer: this); } }
    /// <summary>Returns this layer's stable borrowed logical canvas identity.</summary>
    /// <returns>A scene-owned RID independent of renderer startup; FreeRID cannot release it.</returns>
    public RID GetCanvas() => CanvasRuntime.RID;
}

public abstract partial class CanvasItem
{
    /// <summary>Returns the borrowed canvas identity selected by this item's scene membership.</summary>
    /// <returns>The layer or viewport default canvas; an empty RID while detached.</returns>
    public RID GetCanvas() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return IsInsideTree ? _canvasLayer is { } layer ? layer.GetCanvas() : CanvasViewport!.DefaultCanvasRuntime.RID : default; }
}
