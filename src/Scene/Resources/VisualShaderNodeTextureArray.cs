namespace Electron2D;

/// <summary>Samples independently stored image layers using numeric UV and layer coordinates.</summary>
public sealed class VisualShaderNodeTextureArray : VisualShaderNodeLayeredSample
{
    private TextureLayered? _textureArray;
    /// <summary>Creates a property-backed array-image sampler.</summary>
    public VisualShaderNodeTextureArray() { }
    /// <summary>Gets or assigns the borrowed independent image array.</summary>
    /// <value>Null initially; import uses the image fallback when absent. Changes require reimport.</value>
    /// <exception cref="ObjectDisposedException">The node or supplied resource is disposed.</exception>
    public TextureLayered? TextureArray
    { get { ThrowIfDisposed(); return _textureArray; } set { ThrowIfDisposed(); if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value); _textureArray = value; EmitChanged(); } }
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { base.CopyDefinitionTo(target); ((VisualShaderNodeTextureArray)target)._textureArray = _textureArray; }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource); ((VisualShaderNodeTextureArray)target)._textureArray = (TextureLayered?)duplicateSubresource(_textureArray); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeTextureArray();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeTextureArray, TextureLayered?>(nameof(TextureArray), n => n.TextureArray, (n, v) => n.TextureArray = v, stored: true)]);
}
