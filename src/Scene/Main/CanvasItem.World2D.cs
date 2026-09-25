namespace Electron2D;

public abstract partial class CanvasItem
{
    /// <summary>Gets the two-dimensional physics world of this item's scene tree.</summary>
    /// <returns>The tree's shared world, or null while this item is detached.</returns>
    /// <exception cref="InvalidOperationException">An attached item is queried off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public World2D? GetWorld2D()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return Tree?.GetPhysicsWorld2D();
    }
}
