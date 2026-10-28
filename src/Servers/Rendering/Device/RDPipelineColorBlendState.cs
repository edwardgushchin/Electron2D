namespace Electron2D;

/// <summary>Describes copied logical color operations, per-attachment blending and the initial blend constant.</summary>
public sealed class RDPipelineColorBlendState : ElectronObject
{
    private RDPipelineColorBlendStateAttachment[] _attachments = [];
    private Color _constant = new(0, 0, 0, 1);
    private bool _enableLogicOp;
    private RenderingDevice.LogicOperation _logicOp;
    /// <summary>Creates an empty attachment configuration.</summary>
    public RDPipelineColorBlendState() { }
    /// <summary>Gets or sets whether framebuffer components use the selected bitwise logical operation.</summary>
    /// <value>False initially. When enabled, arithmetic blending is disabled for every attachment; floating-point and sRGB attachments receive the source value without a logical operation. Pipeline creation requires an enabled native logic-operation feature.</value>
    public bool EnableLogicOp { get { ThrowIfDisposed(); return _enableLogicOp; } set { ThrowIfDisposed(); _enableLogicOp = value; } }
    /// <summary>Gets or sets the copied source/destination bitwise operation.</summary>
    /// <value>Clear initially. Effective only when EnableLogicOp is true. Component write masks still apply.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is unknown or the exclusive Max sentinel.</exception>
    public RenderingDevice.LogicOperation LogicOp
    {
        get { ThrowIfDisposed(); return _logicOp; }
        set { ThrowIfDisposed(); if ((uint)value >= (uint)RenderingDevice.LogicOperation.Max) throw new ArgumentOutOfRangeException(nameof(value)); _logicOp = value; }
    }
    /// <summary>Gets or replaces the attachment list, copying its array and borrowing its elements.</summary>
    /// <value>At least one entry per logical framebuffer color slot. Unused slots and extra trailing entries are ignored.</value>
    public RDPipelineColorBlendStateAttachment[] Attachments
    {
        get { ThrowIfDisposed(); return (RDPipelineColorBlendStateAttachment[])_attachments.Clone(); }
        set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); if (value.Any(a => a is null)) throw new ArgumentException("Attachments cannot contain null.", nameof(value)); _attachments = (RDPipelineColorBlendStateAttachment[])value.Clone(); }
    }
    /// <summary>Gets or sets the finite initial blend constant.</summary>
    public Color BlendConstant
    {
        get { ThrowIfDisposed(); return _constant; }
        set { ThrowIfDisposed(); if (!float.IsFinite(value.R) || !float.IsFinite(value.G) || !float.IsFinite(value.B) || !float.IsFinite(value.A)) throw new ArgumentOutOfRangeException(nameof(value)); _constant = value; }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDPipelineColorBlendState, bool>(nameof(EnableLogicOp), p => p.EnableLogicOp, (p, v) => p.EnableLogicOp = v, _ => false),
        new PropertyDescriptor<RDPipelineColorBlendState, RenderingDevice.LogicOperation>(nameof(LogicOp), p => p.LogicOp, (p, v) => p.LogicOp = v, _ => RenderingDevice.LogicOperation.Clear),
        new PropertyDescriptor<RDPipelineColorBlendState, RDPipelineColorBlendStateAttachment[]>(nameof(Attachments), p => p.Attachments, (p, v) => p.Attachments = v, _ => []),
        new PropertyDescriptor<RDPipelineColorBlendState, Color>(nameof(BlendConstant), p => p.BlendConstant, (p, v) => p.BlendConstant = v, _ => new(0, 0, 0, 1))
    ]);
}
