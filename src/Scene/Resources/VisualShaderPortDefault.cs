namespace Electron2D;

/// <summary>An immutable typed input-default entry in a visual shader graph.</summary>
/// <remarks>This collection element describes one numeric shader port. It has no untyped public value accessor.</remarks>
public abstract class VisualShaderPortDefault
{
    internal VisualShaderPortDefault(int port, System.Type valueType)
    { ArgumentOutOfRangeException.ThrowIfNegative(port); Port = port; ValueType = valueType; }
    /// <summary>Gets the zero-based input port index.</summary>
    /// <value>The nonnegative index copied at construction.</value>
    public int Port { get; }
    /// <summary>Gets the exact CLR type of this entry's value.</summary>
    /// <value>The concrete numeric value type.</value>
    public System.Type ValueType { get; }
    internal abstract object BoxedValue { get; }
}

/// <summary>An immutable numeric input-default entry with a concrete value type.</summary>
/// <typeparam name="T">Single, Int32, UInt32, Boolean, Vector2, Vector3, Vector4, Color, or System.Numerics.Matrix4x4.</typeparam>
public sealed class VisualShaderPortDefault<T> : VisualShaderPortDefault where T : unmanaged
{
    /// <summary>Copies and validates an input-default value.</summary>
    /// <param name="port">The nonnegative input index.</param>
    /// <param name="value">The finite numeric value.</param>
    /// <exception cref="ArgumentException">The type or value is unsupported.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The port is negative.</exception>
    public VisualShaderPortDefault(int port, T value) : base(port, typeof(T))
    { VisualShaderValues.Validate(value); Value = value; }
    /// <summary>Gets the copied input value.</summary>
    /// <value>The immutable numeric value validated at construction.</value>
    public T Value { get; }
    internal override object BoxedValue => Value;
}
