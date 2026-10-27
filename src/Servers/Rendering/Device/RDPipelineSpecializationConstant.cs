using System.Runtime.CompilerServices;

namespace Electron2D;

/// <summary>Identifies a caller-owned typed pipeline specialization override.</summary>
/// <remarks>Use the generic description to select Boolean, signed/unsigned 16/32/64-bit integer, Half, Single or Double data. Pipelines copy descriptions and retain their shader identity.</remarks>
public abstract class RDPipelineSpecializationConstant : ElectronObject
{
    /// <summary>Initializes a typed specialization description.</summary>
    protected RDPipelineSpecializationConstant() { }
    private int _constantID;
    /// <summary>Gets or sets the scalar specialization identifier declared by the module.</summary>
    /// <value>Initially zero; nonnegative identities are validated when a pipeline consumes the description.</value>
    public int ConstantID { get { ThrowIfDisposed(); return _constantID; } set { ThrowIfDisposed(); _constantID = value; } }
    internal abstract (int Kind, ulong Bits) CaptureValue();
    /// <summary>Projects the specialization identifier through typed object metadata.</summary>
    /// <returns>Inherited descriptors followed by the copied identifier field.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [new PropertyDescriptor<RDPipelineSpecializationConstant, int>(nameof(ConstantID), p => p.ConstantID, (p, v) => p.ConstantID = v, _ => 0)]);
}

/// <summary>Supplies a strongly typed scalar value for a pipeline specialization identifier.</summary>
/// <typeparam name="T">Boolean, Int16, UInt16, Int32, UInt32, Int64, UInt64, Half, Single or Double; other value types require their native shader capability adapter.</typeparam>
/// <remarks>Value initially has no override, preserving an unassigned description. Assign a value before passing it to a pipeline; omitted descriptions leave the module's defaults.</remarks>
public sealed class RDPipelineSpecializationConstant<T> : RDPipelineSpecializationConstant where T : unmanaged
{
    /// <summary>Creates an unassigned typed specialization description.</summary>
    /// <exception cref="NotSupportedException">T has no executing scalar specialization profile.</exception>
    public RDPipelineSpecializationConstant() { ScalarKind(); }
    private T? _value;
    /// <summary>Gets or sets the copied scalar override, or null when no value has been assigned.</summary>
    /// <value>Initially null. Scalar type and module type must agree when consumed.</value>
    public T? Value { get { ThrowIfDisposed(); return _value; } set { ThrowIfDisposed(); _value = value; } }
    private static int ScalarKind() => typeof(T) == typeof(bool) ? 0 : typeof(T) == typeof(int) ? 1 : typeof(T) == typeof(uint) ? 2 : typeof(T) == typeof(float) ? 3 : typeof(T) == typeof(double) ? 4 : typeof(T) == typeof(long) ? 5 : typeof(T) == typeof(ulong) ? 6 : typeof(T) == typeof(Half) ? 7 : typeof(T) == typeof(short) ? 8 : typeof(T) == typeof(ushort) ? 9 : throw new NotSupportedException("Specialization requires Boolean, Int16, UInt16, Int32, UInt32, Int64, UInt64, Half, Single or Double data.");
    internal override (int Kind, ulong Bits) CaptureValue()
    {
        ThrowIfDisposed(); if (_value is not { } value) throw new ArgumentException("Assign a specialization value before creating a pipeline.");
        var kind = ScalarKind(); return (kind, kind == 0 ? Unsafe.As<T, bool>(ref value) ? 1ul : 0ul : kind >= 7 ? Unsafe.As<T, ushort>(ref value) : kind >= 4 ? Unsafe.As<T, ulong>(ref value) : Unsafe.As<T, uint>(ref value));
    }
    /// <summary>Projects the nullable typed scalar value alongside inherited identifier metadata.</summary>
    /// <returns>Inherited descriptors followed by the value and its unassigned default.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [new PropertyDescriptor<RDPipelineSpecializationConstant<T>, T?>(nameof(Value), p => p.Value, (p, v) => p.Value = v, _ => null)]);
}
