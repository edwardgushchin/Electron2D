namespace Electron2D;

/// <summary>Holds a typed, non-owning reference to an engine object.</summary>
/// <typeparam name="T">The engine-object type returned by <see cref="GetRef"/>.</typeparam>
/// <remarks>The wrapper does not keep its target alive or own its disposal. A disposed target is unavailable even while a strong managed reference still exists.</remarks>
public sealed class WeakRef<T> : ElectronObject where T : ElectronObject
{
    private readonly WeakReference<T>? _target;

    /// <summary>Creates a weak reference to a live object, or an empty reference for null or a disposed object.</summary>
    /// <param name="target">The object to observe without extending its lifetime.</param>
    public WeakRef(T? target)
    {
        if (target is not null && !target.IsDisposed) _target = new WeakReference<T>(target);
    }

    /// <summary>Gets the live target when it still exists and has not begun disposal.</summary>
    /// <returns>The target, or null after disposal, collection, or empty construction.</returns>
    /// <exception cref="ObjectDisposedException">This weak-reference wrapper is disposed.</exception>
    public T? GetRef()
    {
        ThrowIfDisposed();
        return _target is not null && _target.TryGetTarget(out var target) && !target.IsDisposed ? target : null;
    }
}
