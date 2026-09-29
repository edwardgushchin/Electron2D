namespace Electron2D;

/// <summary>A typed value carried during one GUI drag operation.</summary>
/// <remarks>The caller owns the value; the scene tree borrows the payload until the drag ends. This is a
/// drag-specific typed C# contract, not a general property or signal value container.</remarks>
public abstract class DragPayload
{
    /// <summary>Creates a payload for a concrete drag-value type.</summary>
    protected DragPayload() { }
}

/// <summary>Carries one nonnull value of a known compile-time type during a GUI drag.</summary>
/// <typeparam name="T">The dragged value's type.</typeparam>
public sealed class DragPayload<T> : DragPayload
{
    /// <summary>Creates a payload that borrows the supplied value.</summary>
    /// <param name="value">The nonnull value to carry.</param>
    /// <exception cref="ArgumentNullException">The value is null.</exception>
    public DragPayload(T value) { ArgumentNullException.ThrowIfNull(value); Value = value; }

    /// <summary>Gets the original value supplied by the caller.</summary>
    /// <value>The borrowed value; it is not copied or disposed with the drag.</value>
    public T Value { get; }
}
