namespace Electron2D;

/// <summary>A numeric vector graph operation with a selectable component count.</summary>
/// <remarks>These are numeric shader components, independent of the engine's planar scene domain.</remarks>
public abstract class VisualShaderNodeVectorBase : VisualShaderNode
{
    /// <summary>The number of numeric components processed by a vector operation.</summary>
    public enum OpTypeEnum
    {
        /// <summary>Two numeric components.</summary>
        Vector2 = 0,
        /// <summary>Three numeric components.</summary>
        Vector3 = 1,
        /// <summary>Four numeric components.</summary>
        Vector4 = 2,
        /// <summary>The nonselectable component-count sentinel.</summary>
        Max = 3,
    }
    private OpTypeEnum _operationType = OpTypeEnum.Vector3;
    /// <summary>Initializes a three-component numeric operation.</summary>
    protected VisualShaderNodeVectorBase() { }
    /// <summary>Gets or selects the component count, converting existing input defaults.</summary>
    /// <value>Vector3 initially; expanding a default repeats its last previous component.</value>
    /// <remarks>An unchanged selection emits no event. Changing width recreates removed defaults with zero
    /// vectors, or (0,0,0,1) for four components. Cross product requires three components.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The component-count selection is undefined.</exception>
    /// <exception cref="NotSupportedException">The selected cross-product width is not three.</exception>
    /// <exception cref="ObjectDisposedException">The operation is disposed.</exception>
    public OpTypeEnum OpType
    {
        get { ThrowIfDisposed(); return _operationType; }
        set
        {
            ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == OpTypeEnum.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (this is VisualShaderNodeVectorOp { Operator: VisualShaderNodeVectorOp.OperatorEnum.Cross } && value != OpTypeEnum.Vector3)
                throw new NotSupportedException("A vector cross product requires three numeric components.");
            if (_operationType == value) return;
            var type = value switch { OpTypeEnum.Vector2 => PortType.Vector2, OpTypeEnum.Vector3 => PortType.Vector3, _ => PortType.Vector4 };
            ChangeDefaultWidth(type); _operationType = value; EmitChanged();
        }
    }
    internal PortType VectorKind => _operationType switch
    { OpTypeEnum.Vector2 => PortType.Vector2, OpTypeEnum.Vector3 => PortType.Vector3, _ => PortType.Vector4 };
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeVectorBase)target)._operationType = _operationType;
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] { new PropertyDescriptor<VisualShaderNodeVectorBase, OpTypeEnum>(nameof(OpType), n => n.OpType,
            (n, v) => n.OpType = v, stored: true) }.Concat(base.GetPropertyDescriptors());
}
