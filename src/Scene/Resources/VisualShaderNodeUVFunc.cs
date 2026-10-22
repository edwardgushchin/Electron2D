namespace Electron2D;

/// <summary>Pans or scales numeric UV coordinates in an imported canvas graph.</summary>
/// <remarks>An unconnected UV input uses the current stage UV, including when a numeric default is stored.
/// Inputs one and two are scale and offset/pivot. Edits require reimport before drawing changes.</remarks>
public sealed class VisualShaderNodeUVFunc : VisualShaderNode
{
    /// <summary>The coordinate operation.</summary>
    public enum FunctionEnum
    {
        /// <summary>Adds offset multiplied by scale to UV.</summary>
        Panning = 0,
        /// <summary>Scales UV around a pivot.</summary>
        Scaling = 1,
        /// <summary>The nonselectable function count.</summary>
        Max = 2,
    }
    private FunctionEnum _function;
    /// <summary>Creates a panning operation with unit scale, zero offset and implicit UV.</summary>
    public VisualShaderNodeUVFunc() { InitializeDefault(1, Vector2.One); InitializeDefault(2, Vector2.Zero); }
    /// <summary>Gets or selects panning or scaling.</summary>
    /// <value>Panning initially. Existing offset/pivot values survive a change; an absent value becomes zero for Panning or (0.5,0.5) for Scaling.</value>
    /// <remarks>An unchanged selection is quiet. A changed selection commits its defaults and function before Changed is emitted.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined or Max.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public FunctionEnum Function
    {
        get { ThrowIfDisposed(); return _function; }
        set
        {
            ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == FunctionEnum.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_function == value) return;
            if (!RestoringDefaultTable) ChangeDefaultTypes([value == FunctionEnum.Scaling ? new Vector2(.5f, .5f) : Vector2.Zero], 2);
            _function = value; EmitChanged();
        }
    }
    internal override PortType[] Inputs => [PortType.Vector2, PortType.Vector2, PortType.Vector2];
    internal override PortType[] Outputs => [PortType.Vector2];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => _function == FunctionEnum.Panning
        ? "(" + inputs[2] + " * " + inputs[1] + " + " + inputs[0] + ")"
        : "((" + inputs[0] + " - " + inputs[2] + ") * " + inputs[1] + " + " + inputs[2] + ")";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeUVFunc)target)._function = _function;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeUVFunc();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeUVFunc, FunctionEnum>(nameof(Function), n => n.Function, (n, v) => n.Function = v, stored: true)]);
}
