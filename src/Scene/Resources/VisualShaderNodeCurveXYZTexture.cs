namespace Electron2D;

/// <summary>Samples the RGB components of a borrowed curve image at a scalar coordinate.</summary>
/// <remarks>The scalar input initially stores zero. Sampling uses both UV components equal to that input;
/// removing its default without connecting a source produces zero. Null images use white. Values retain
/// their numeric range without color conversion. Graph edits require reimport; changes to an already bound
/// curve image refresh its pixels through the ordinary texture path. Assigned uninitialized images follow
/// the source texture readiness contract. The node never disposes its image.</remarks>
public sealed class VisualShaderNodeCurveXYZTexture : VisualShaderNodeResizableBase
{
    private CurveXYZTexture? _texture;
    /// <summary>Creates a curve sampler with scalar input zero and no image.</summary>
    public VisualShaderNodeCurveXYZTexture() { InitializeDefault(0, 0f); }
    /// <summary>Gets or assigns the borrowed curve image used by the next import.</summary>
    /// <value>Null initially; the imported sampler uses the white image fallback.</value>
    /// <exception cref="ObjectDisposedException">This node or the supplied image is disposed.</exception>
    public CurveXYZTexture? Texture
    { get { ThrowIfDisposed(); return _texture; } set { ThrowIfDisposed(); if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value); _texture = value; EmitChanged(); } }
    internal override PortType[] Inputs => [PortType.Scalar];
    internal override PortType[] Outputs => [PortType.Vector3];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("Curve sampling requires the containing stage's sampler plan.");
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { base.CopyDefinitionTo(target); ((VisualShaderNodeCurveXYZTexture)target)._texture = _texture; }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource); ((VisualShaderNodeCurveXYZTexture)target)._texture = (CurveXYZTexture?)duplicateSubresource(_texture); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeCurveXYZTexture();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeCurveXYZTexture, CurveXYZTexture?>(nameof(Texture), n => n.Texture, (n, v) => n.Texture = v, stored: true)]);
}
