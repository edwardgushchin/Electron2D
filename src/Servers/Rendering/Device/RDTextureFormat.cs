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
    public uint MIPMaps { get { ThrowIfDisposed(); return _mipmaps; } set { ThrowIfDisposed(); _mipmaps = RDTextureConfiguration.Positive(value); } }
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
    private RenderingDevice.TextureSamples _samples = RenderingDevice.TextureSamples.Samples1;
    /// <summary>Selects the exact allocation sample count.</summary>
    /// <remarks>Multisample color/depth targets may include Sampling for explicit per-sample shader reads. Color Storage images also support explicit sample loads, stores, queries and R32 atomics with the enabled native feature. Empty data, one mip and the exact native format/usage/count tuple are validated at allocation.</remarks>
    /// <value>Initially RenderingDevice.TextureSamples.Samples1; copied and validated at allocation.</value>
    public RenderingDevice.TextureSamples Samples { get { ThrowIfDisposed(); return _samples; } set { ThrowIfDisposed(); _samples = value; } }
    private bool _isResolveBuffer = false;
    /// <summary>Marks a single-sample color image or array as a resolve destination.</summary>
    /// <value>Initially false; copied and validated at allocation.</value>
    public bool IsResolveBuffer { get { ThrowIfDisposed(); return _isResolveBuffer; } set { ThrowIfDisposed(); _isResolveBuffer = value; } }
    private readonly HashSet<RenderingDevice.DataFormat> _shareable = [];
    /// <summary>Adds a permitted native view format; a nonempty list must include the allocation format.</summary>
    /// <param name="format">A concrete format identifier.</param>
    public void AddShareableFormat(RenderingDevice.DataFormat format) { ThrowIfDisposed(); _shareable.Add(RDTextureConfiguration.FormatValue(format)); }
    /// <summary>Removes a permitted native view format before allocation; existing textures retain their copied list.</summary>
    /// <param name="format">The concrete format to remove.</param>
    public void RemoveShareableFormat(RenderingDevice.DataFormat format) { ThrowIfDisposed(); _shareable.Remove(RDTextureConfiguration.FormatValue(format)); }
    internal RenderingDevice.DataFormat[] ShareableFormats => _shareable.ToArray();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [
            new PropertyDescriptor<RDTextureFormat, RenderingDevice.TextureSamples>(nameof(Samples), p => p.Samples, (p, v) => p.Samples = v, _ => RenderingDevice.TextureSamples.Samples1),
            new PropertyDescriptor<RDTextureFormat, bool>(nameof(IsResolveBuffer), p => p.IsResolveBuffer, (p, v) => p.IsResolveBuffer = v, _ => false),

            new PropertyDescriptor<RDTextureFormat, uint>(nameof(ArrayLayers), p => p.ArrayLayers, (p, v) => p.ArrayLayers = v, _ => 1),
            new PropertyDescriptor<RDTextureFormat, uint>(nameof(Width), p => p.Width, (p, v) => p.Width = v, _ => 1),
            new PropertyDescriptor<RDTextureFormat, uint>(nameof(Height), p => p.Height, (p, v) => p.Height = v, _ => 1),
            new PropertyDescriptor<RDTextureFormat, uint>(nameof(MIPMaps), p => p.MIPMaps, (p, v) => p.MIPMaps = v, _ => 1),
            new PropertyDescriptor<RDTextureFormat, RenderingDevice.DataFormat>(nameof(Format), p => p.Format, (p, v) => p.Format = v, _ => RenderingDevice.DataFormat.R8UNORM),
            new PropertyDescriptor<RDTextureFormat, RenderingDevice.TextureType>(nameof(TextureType), p => p.TextureType, (p, v) => p.TextureType = v, _ => RenderingDevice.TextureType.Texture),
            new PropertyDescriptor<RDTextureFormat, RenderingDevice.TextureUsageBits>(nameof(UsageBits), p => p.UsageBits, (p, v) => p.UsageBits = v, _ => 0)
        ]);
}
