namespace Electron2D;

public abstract partial class CanvasItem
{
    /// <summary>Notifies an attached canvas item after its viewport selects a different world.</summary>
    /// <remarks>Value 36. Delivery follows committed canvas/physics association, in parent-first order; observer failures do not prevent later deliveries.</remarks>
    public const int NotificationWorldChanged = 36;

    /// <summary>Gets the canvas and physics world selected by this item's viewport.</summary>
    /// <returns>The selected <see cref="World"/>, or null while this item is detached.</returns>
    /// <exception cref="InvalidOperationException">An attached item is queried off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public World? GetWorld()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return Tree is null ? null : GetViewport()?.FindWorld() ?? Tree.GetPhysicsWorld();
    }
}
