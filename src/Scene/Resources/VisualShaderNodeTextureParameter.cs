namespace Electron2D;

/// <summary>Base for named sampled-image graph parameters and their import-time image policies.</summary>
public abstract class VisualShaderNodeTextureParameter : VisualShaderNodeParameter
{
    /// <summary>The sampled image interpretation.</summary>
    public enum TextureTypeEnum
    {
        /// <summary>Raw components.</summary>
        Data = 0,
        /// <summary>Encoded RGB, converted to linear in HDR canvas output.</summary>
        Color = 1,
        /// <summary>Raw normal components.</summary>
        NormalMap = 2,
        /// <summary>Raw flow-map components.</summary>
        Anisotropy = 3,
        /// <summary>The nonselectable type count.</summary>
        Max = 4,
    }
    /// <summary>The render-only fallback for missing data/color images.</summary>
    public enum ColorDefaultEnum
    {
        /// <summary>Opaque white.</summary>
        White = 0,
        /// <summary>Opaque black.</summary>
        Black = 1,
        /// <summary>Transparent black.</summary>
        Transparent = 2,
        /// <summary>The nonselectable fallback count.</summary>
        Max = 3,
    }
    /// <summary>The shader sampler filtering policy.</summary>
    public enum TextureFilterEnum
    {
        /// <summary>The canvas shader default: linear base-level sampling.</summary>
        Default = 0,
        /// <summary>Nearest base-level sampling.</summary>
        Nearest = 1,
        /// <summary>Linear base-level sampling.</summary>
        Linear = 2,
        /// <summary>Nearest texels with mip levels under the project mip-selection policy.</summary>
        NearestMipmap = 3,
        /// <summary>Linear texels with mip levels under the project mip-selection policy.</summary>
        LinearMipmap = 4,
        /// <summary>Nearest mipmapped sampling with anisotropy.</summary>
        NearestMipmapAnisotropic = 5,
        /// <summary>Linear mipmapped sampling with anisotropy.</summary>
        LinearMipmapAnisotropic = 6,
        /// <summary>The nonselectable filter count.</summary>
        Max = 7,
    }
    /// <summary>The shader image wrapping policy.</summary>
    public enum TextureRepeatEnum
    {
        /// <summary>The canvas shader default: clamp to edge.</summary>
        Default = 0,
        /// <summary>Repeat UV coordinates.</summary>
        Enabled = 1,
        /// <summary>Clamp UV coordinates to the image edge.</summary>
        Disabled = 2,
        /// <summary>The nonselectable repeat count.</summary>
        Max = 3,
    }
    /// <summary>The image storage owner.</summary>
    public enum TextureSourceEnum
    {
        /// <summary>The material or registered global image.</summary>
        None = 0,
        /// <summary>The renderer-owned fragment backbuffer.</summary>
        Screen = 1,
        /// <summary>The nonselectable source-table bound.</summary>
        Max = 4,
    }
    private TextureTypeEnum _textureType;
    private ColorDefaultEnum _colorDefault;
    private TextureFilterEnum _textureFilter;
    private TextureRepeatEnum _textureRepeat;
    private TextureSourceEnum _textureSource;
    /// <summary>Initializes a data-image parameter with white fallback and shader-default sampling.</summary>
    protected VisualShaderNodeTextureParameter() { }
    /// <summary>Gets or selects the sampled image interpretation.</summary>
    /// <value>Data initially. Normal and flow defaults apply only to ordinary images; array images fall back to white.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined or a nonselectable sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public TextureTypeEnum TextureType
    {
        get { ThrowIfDisposed(); return _textureType; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == TextureTypeEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_textureType == value) return; _textureType = value; EmitChanged(); }
    }
    /// <summary>Gets or selects the render-only fallback color.</summary>
    /// <value>White initially; retained but ignored for normal/flow interpretations. Screen sources require White at import.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined or a nonselectable sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public ColorDefaultEnum ColorDefault
    {
        get { ThrowIfDisposed(); return _colorDefault; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == ColorDefaultEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_colorDefault == value) return; _colorDefault = value; EmitChanged(); }
    }
    /// <summary>Gets or selects the shader sampling filter.</summary>
    /// <value>Default initially; independent of CanvasItem parent sampling. An explicit Shader sampler state takes precedence.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined or a nonselectable sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public TextureFilterEnum TextureFilter
    {
        get { ThrowIfDisposed(); return _textureFilter; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == TextureFilterEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_textureFilter == value) return; _textureFilter = value; EmitChanged(); }
    }
    /// <summary>Gets or selects shader image wrapping.</summary>
    /// <value>Default initially; independent of CanvasItem parent sampling. An explicit Shader sampler state takes precedence.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined or a nonselectable sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public TextureRepeatEnum TextureRepeat
    {
        get { ThrowIfDisposed(); return _textureRepeat; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == TextureRepeatEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_textureRepeat == value) return; _textureRepeat = value; EmitChanged(); }
    }
    /// <summary>Gets or selects parameter storage or the fragment screen producer.</summary>
    /// <value>None initially. Incompatible screen shape, stage or image hints reject at import.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined or a nonselectable sentinel.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public TextureSourceEnum TextureSource
    {
        get { ThrowIfDisposed(); return _textureSource; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == TextureSourceEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_textureSource == value) return; _textureSource = value; EmitChanged(); }
    }
    internal override PortType[] Outputs => [PortType.Sampler];
    internal override object? ParameterDefault => null;
    internal override bool HasParameterDefault => false;
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { base.CopyDefinitionTo(target); var copy = (VisualShaderNodeTextureParameter)target; copy._textureType = _textureType; copy._colorDefault = _colorDefault; copy._textureFilter = _textureFilter; copy._textureRepeat = _textureRepeat; copy._textureSource = _textureSource; }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeTextureParameter, TextureTypeEnum>(nameof(TextureType), n => n.TextureType, (n,v) => n.TextureType = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeTextureParameter, ColorDefaultEnum>(nameof(ColorDefault), n => n.ColorDefault, (n,v) => n.ColorDefault = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeTextureParameter, TextureFilterEnum>(nameof(TextureFilter), n => n.TextureFilter, (n,v) => n.TextureFilter = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeTextureParameter, TextureRepeatEnum>(nameof(TextureRepeat), n => n.TextureRepeat, (n,v) => n.TextureRepeat = v, stored:true),
         new PropertyDescriptor<VisualShaderNodeTextureParameter, TextureSourceEnum>(nameof(TextureSource), n => n.TextureSource, (n,v) => n.TextureSource = v, stored:true)]);
}
