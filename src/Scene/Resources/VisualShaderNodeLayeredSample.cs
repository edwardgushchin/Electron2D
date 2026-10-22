namespace Electron2D;

/// <summary>Base for independent image-layer sampling in an imported visual graph.</summary>
/// <remarks>Unconnected coordinates use the current UV and layer zero. Unconnected LOD uses implicit sampling;
/// authored numeric defaults remain retained source data. Port without a connected sampler produces zero.</remarks>
public abstract class VisualShaderNodeLayeredSample : VisualShaderNode
{
    /// <summary>The independent image-layer producer.</summary>
    public enum SourceEnum
    {
        /// <summary>The borrowed image array.</summary>
        Texture = 0,
        /// <summary>The sampler connected to input two.</summary>
        Port = 1,
        /// <summary>The nonselectable source count.</summary>
        Max = 2,
    }
    private SourceEnum _source;
    /// <summary>Initializes a property-backed layered sampler.</summary>
    protected VisualShaderNodeLayeredSample() { }
    /// <summary>Gets or selects the independent image source.</summary>
    /// <value>Texture initially. UV and layer default to the current UV and layer zero; unconnected Port produces zero.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined or a nonselectable sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public SourceEnum Source
    {
        get { ThrowIfDisposed(); return _source; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == SourceEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_source == value) return; _source = value; EmitChanged(); }
    }
    internal override PortType[] Inputs => [PortType.Vector3, PortType.Scalar, PortType.Sampler];
    internal override PortType[] Outputs => [PortType.Vector4];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("Layer sampling requires the containing stage's sampler plan.");
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeLayeredSample)target)._source = _source;
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeLayeredSample, SourceEnum>(nameof(Source), n => n.Source, (n, v) => n.Source = v, stored: true)]);
}
