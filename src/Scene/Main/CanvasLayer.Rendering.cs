namespace Electron2D;

public partial class CanvasLayer
{
    private volatile RenderingCanvasRuntime? _canvasRuntime;
    private readonly object _canvasGate = new();
    internal RenderingCanvasRuntime CanvasRuntime
    {
        get
        {
            ThrowIfDisposed(); Tree?.EnsureProcessingReadAccess();
            if (_canvasRuntime is { } current) return current;
            lock (_canvasGate) return _canvasRuntime ??= RenderingCanvasRegistry.Register(layer: this);
        }
    }
    /// <summary>Returns this layer's stable borrowed logical canvas identity.</summary>
    /// <returns>A scene-owned RID independent of renderer startup; FreeRID cannot release it.</returns>
    /// <remarks>The owner or any active processing group may read this logical identity with coordinated lifetime changes.</remarks>
    public RID GetCanvas() => CanvasRuntime.RID;
}

public abstract partial class CanvasItem
{
    /// <summary>Returns the borrowed canvas identity selected by this item's scene membership.</summary>
    /// <returns>The layer or viewport default canvas; an empty RID while detached.</returns>
    /// <remarks>The owner or any active processing group may read this identity without native startup.
    /// Viewport-free scenes use their fallback world. Callers coordinate concurrent lifetime changes.</remarks>
    public RID GetCanvas() { ThrowIfDisposed(); Tree?.EnsureProcessingReadAccess(); return IsInsideTree ? _canvasLayer is { } layer ? layer.GetCanvas() : (CanvasViewport?.FindWorld() ?? Tree!.GetPhysicsWorld())!.Canvas : default; }
}
