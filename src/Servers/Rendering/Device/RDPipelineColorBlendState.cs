namespace Electron2D;

/// <summary>Describes copied per-attachment blending and the initial blend constant.</summary>
public sealed class RDPipelineColorBlendState : ElectronObject
{
    private RDPipelineColorBlendStateAttachment[] _attachments = [];
    private Color _constant = new(0, 0, 0, 1);
    /// <summary>Creates an empty attachment configuration.</summary>
    public RDPipelineColorBlendState() { }
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
        new PropertyDescriptor<RDPipelineColorBlendState, RDPipelineColorBlendStateAttachment[]>(nameof(Attachments), p => p.Attachments, (p, v) => p.Attachments = v, _ => []),
        new PropertyDescriptor<RDPipelineColorBlendState, Color>(nameof(BlendConstant), p => p.BlendConstant, (p, v) => p.BlendConstant = v, _ => new(0, 0, 0, 1))
    ]);
}
