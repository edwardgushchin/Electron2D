namespace Electron2D;

/// <summary>Describes device-owned image dimensions, encoding, mip levels, layers and usage roles.</summary>
public sealed class RDTextureFormat : ElectronObject
{
    /// <summary>Creates a single-layer, one-texel R8 normalized image description.</summary>
    public RDTextureFormat() { }
    private uint _arrayLayers = 1;
    /// <summary>Gets or sets the number of independent image layers.</summary>
    /// <value>Initially 1; copied when the device creates its resource.</value>
    public uint ArrayLayers { get { ThrowIfDisposed(); return _arrayLayers; } set { ThrowIfDisposed(); _arrayLayers = RDTextureConfiguration.Positive(value); } }
    private uint _width = 1;
    /// <summary>Gets or sets the base mip width in texels.</summary>
    /// <value>Initially 1; copied when the device creates its resource.</value>
    public uint Width { get { ThrowIfDisposed(); return _width; } set { ThrowIfDisposed(); _width = RDTextureConfiguration.Positive(value); } }
    private uint _height = 1;
    /// <summary>Gets or sets the base mip height in texels.</summary>
    /// <value>Initially 1; copied when the device creates its resource.</value>
    public uint Height { get { ThrowIfDisposed(); return _height; } set { ThrowIfDisposed(); _height = RDTextureConfiguration.Positive(value); } }
    private uint _mipmaps = 1;
    /// <summary>Gets or sets the mip count, including the base level.</summary>
    /// <value>Initially 1; copied when the device creates its resource.</value>
    public uint Mipmaps { get { ThrowIfDisposed(); return _mipmaps; } set { ThrowIfDisposed(); _mipmaps = RDTextureConfiguration.Positive(value); } }
    private RenderingDevice.DataFormat _format = RenderingDevice.DataFormat.R8UNORM;
    /// <summary>Gets or sets the encoded pixel or compressed-block format.</summary>
    /// <value>Initially RenderingDevice.DataFormat.R8UNORM; copied when the device creates its resource.</value>
    public RenderingDevice.DataFormat Format { get { ThrowIfDisposed(); return _format; } set { ThrowIfDisposed(); _format = RDTextureConfiguration.FormatValue(value); } }
    private RenderingDevice.TextureType _textureType = RenderingDevice.TextureType.Texture;
    /// <summary>Gets or sets ordinary-image or independently sampled array storage.</summary>
    /// <value>Initially RenderingDevice.TextureType.Texture; copied when the device creates its resource.</value>
    public RenderingDevice.TextureType TextureType { get { ThrowIfDisposed(); return _textureType; } set { ThrowIfDisposed(); _textureType = RDTextureConfiguration.TextureKind(value); } }
    private RenderingDevice.TextureUsageBits _usageBits = 0;
    /// <summary>Gets or sets required allocation, sampling and transfer roles.</summary>
    /// <value>Initially 0; copied when the device creates its resource.</value>
    public RenderingDevice.TextureUsageBits UsageBits { get { ThrowIfDisposed(); return _usageBits; } set { ThrowIfDisposed(); _usageBits = RDTextureConfiguration.Usage(value); } }
    private readonly HashSet<RenderingDevice.DataFormat> _shareable = [];
    /// <summary>Adds an explicitly allowed alternate view format.</summary>
    /// <param name="format">A concrete format identifier.</param>
    public void AddShareableFormat(RenderingDevice.DataFormat format) { ThrowIfDisposed(); _shareable.Add(RDTextureConfiguration.FormatValue(format)); }
    /// <summary>Removes an allowed alternate view format.</summary>
    /// <param name="format">The concrete format to remove.</param>
    public void RemoveShareableFormat(RenderingDevice.DataFormat format) { ThrowIfDisposed(); _shareable.Remove(RDTextureConfiguration.FormatValue(format)); }
    internal RenderingDevice.DataFormat[] ShareableFormats => _shareable.ToArray();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [
            new PropertyDescriptor<RDTextureFormat, uint>(nameof(ArrayLayers), p => p.ArrayLayers, (p, v) => p.ArrayLayers = v, _ => 1),
            new PropertyDescriptor<RDTextureFormat, uint>(nameof(Width), p => p.Width, (p, v) => p.Width = v, _ => 1),
            new PropertyDescriptor<RDTextureFormat, uint>(nameof(Height), p => p.Height, (p, v) => p.Height = v, _ => 1),
            new PropertyDescriptor<RDTextureFormat, uint>(nameof(Mipmaps), p => p.Mipmaps, (p, v) => p.Mipmaps = v, _ => 1),
            new PropertyDescriptor<RDTextureFormat, RenderingDevice.DataFormat>(nameof(Format), p => p.Format, (p, v) => p.Format = v, _ => RenderingDevice.DataFormat.R8UNORM),
            new PropertyDescriptor<RDTextureFormat, RenderingDevice.TextureType>(nameof(TextureType), p => p.TextureType, (p, v) => p.TextureType = v, _ => RenderingDevice.TextureType.Texture),
            new PropertyDescriptor<RDTextureFormat, RenderingDevice.TextureUsageBits>(nameof(UsageBits), p => p.UsageBits, (p, v) => p.UsageBits = v, _ => 0)
        ]);
}
