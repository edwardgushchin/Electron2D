namespace Electron2D;

/// <summary>Samples an ordinary image, canvas channel, screen copy or connected sampler in an imported graph.</summary>
/// <remarks>Unconnected UV uses the current UV (screen coordinates for Screen); unconnected LOD uses implicit sampling.
/// Authored numeric defaults on those ports are retained source data and do not replace these implicit inputs.
/// Port without a connected sampler produces zero. Sources and image interpretation change only the next import.</remarks>
public sealed class VisualShaderNodeTexture : VisualShaderNode
{
    /// <summary>The sampled image producer.</summary>
    public enum SourceEnum
    {
        /// <summary>The borrowed Texture property.</summary>
        Texture = 0,
        /// <summary>The fragment backbuffer copy.</summary>
        Screen = 1,
        /// <summary>The current canvas diffuse image.</summary>
        Canvas = 2,
        /// <summary>The current canvas normal image.</summary>
        CanvasNormal = 3,
        /// <summary>The sampler connected to input two.</summary>
        Port = 5,
        /// <summary>The nonselectable source-table bound.</summary>
        Max = 8,
    }
    /// <summary>The interpretation of a property-backed image.</summary>
    public enum TextureTypeEnum
    {
        /// <summary>Raw sampled components.</summary>
        Data = 0,
        /// <summary>Encoded color, converted to linear RGB in HDR canvas output.</summary>
        Color = 1,
        /// <summary>Raw normal components and a flat-normal fallback.</summary>
        NormalMap = 2,
        /// <summary>The nonselectable type count.</summary>
        Max = 3,
    }
    private SourceEnum _source;
    private TextureTypeEnum _textureType;
    private Texture? _texture;
    /// <summary>Creates a property-backed data-image sampler.</summary>
    public VisualShaderNodeTexture() { }
    /// <summary>Gets or selects the image producer.</summary>
    /// <value>Texture initially. Screen and canvas channels produce zero outside the fragment stage.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined or a nonselectable sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public SourceEnum Source
    {
        get { ThrowIfDisposed(); return _source; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == SourceEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_source == value) return; _source = value; EmitChanged(); }
    }
    /// <summary>Gets or selects the property image interpretation.</summary>
    /// <value>Data initially; dormant for nonproperty sources.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined or a nonselectable sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public TextureTypeEnum TextureType
    {
        get { ThrowIfDisposed(); return _textureType; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == TextureTypeEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_textureType == value) return; _textureType = value; EmitChanged(); }
    }
    /// <summary>Gets or assigns the borrowed ordinary source image.</summary>
    /// <value>Null initially; import uses the image fallback when absent. Changes require reimport.</value>
    /// <exception cref="ObjectDisposedException">The node or supplied resource is disposed.</exception>
    public Texture? Texture
    { get { ThrowIfDisposed(); return _texture; } set { ThrowIfDisposed(); if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value); _texture = value; EmitChanged(); } }
    internal override PortType[] Inputs => [PortType.Vector2, PortType.Scalar, PortType.Sampler];
    internal override PortType[] Outputs => [PortType.Vector4];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("Image sampling requires the containing stage's sampler plan.");
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { var copy = (VisualShaderNodeTexture)target; copy._source = _source; copy._textureType = _textureType; copy._texture = _texture; }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource); ((VisualShaderNodeTexture)target)._texture = (Texture?)duplicateSubresource(_texture); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeTexture();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeTexture, SourceEnum>(nameof(Source), n => n.Source, (n,v) => n.Source = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeTexture, TextureTypeEnum>(nameof(TextureType), n => n.TextureType, (n,v) => n.TextureType = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeTexture, Texture?>(nameof(Texture), n => n.Texture, (n,v) => n.Texture = v, stored:true)]);
}
