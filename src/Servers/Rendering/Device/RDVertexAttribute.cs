namespace Electron2D;

/// <summary>Describes copied VertexAttribute configuration for graphics resources.</summary>
public sealed class RDVertexAttribute : ElectronObject
{
    /// <summary>Creates the default configuration.</summary>
    public RDVertexAttribute() { }
    private uint _binding = uint.MaxValue;
    /// <summary>Gets or sets the zero-based source buffer binding.</summary>
    /// <value>Initially uint.MaxValue; validated and copied by its device consumer.</value>
    public uint Binding { get { ThrowIfDisposed(); return _binding; } set { ThrowIfDisposed(); _binding = value; } }
    private RenderingDevice.DataFormat _format = RenderingDevice.DataFormat.Max;
    /// <summary>Gets or sets the encoded vertex field format.</summary>
    /// <value>Initially RenderingDevice.DataFormat.Max; validated and copied by its device consumer.</value>
    public RenderingDevice.DataFormat Format { get { ThrowIfDisposed(); return _format; } set { ThrowIfDisposed(); _format = value; } }
    private RenderingDevice.VertexFrequency _frequency = RenderingDevice.VertexFrequency.Vertex;
    /// <summary>Gets or sets per-vertex or per-instance record advancement.</summary>
    /// <value>Initially RenderingDevice.VertexFrequency.Vertex; validated and copied by its device consumer.</value>
    public RenderingDevice.VertexFrequency Frequency { get { ThrowIfDisposed(); return _frequency; } set { ThrowIfDisposed(); _frequency = value; } }
    private uint _location = 0;
    /// <summary>Gets or sets the shader input location.</summary>
    /// <value>Initially 0; validated and copied by its device consumer.</value>
    public uint Location { get { ThrowIfDisposed(); return _location; } set { ThrowIfDisposed(); _location = value; } }
    private uint _offset = 0;
    /// <summary>Gets or sets the field byte offset within a record.</summary>
    /// <value>Initially 0; validated and copied by its device consumer.</value>
    public uint Offset { get { ThrowIfDisposed(); return _offset; } set { ThrowIfDisposed(); _offset = value; } }
    private uint _stride = 0;
    /// <summary>Gets or sets the byte distance between successive records.</summary>
    /// <value>Initially 0; validated and copied by its device consumer.</value>
    public uint Stride { get { ThrowIfDisposed(); return _stride; } set { ThrowIfDisposed(); _stride = value; } }
    /// <summary>Projects the caller-owned configuration through inherited typed property metadata.</summary>
    /// <returns>Inherited descriptors followed by this description's properties; configuration is copied when consumed.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDVertexAttribute, uint>(nameof(Binding), p => p.Binding, (p, v) => p.Binding = v, _ => uint.MaxValue),
        new PropertyDescriptor<RDVertexAttribute, RenderingDevice.DataFormat>(nameof(Format), p => p.Format, (p, v) => p.Format = v, _ => RenderingDevice.DataFormat.Max),
        new PropertyDescriptor<RDVertexAttribute, RenderingDevice.VertexFrequency>(nameof(Frequency), p => p.Frequency, (p, v) => p.Frequency = v, _ => RenderingDevice.VertexFrequency.Vertex),
        new PropertyDescriptor<RDVertexAttribute, uint>(nameof(Location), p => p.Location, (p, v) => p.Location = v, _ => 0),
        new PropertyDescriptor<RDVertexAttribute, uint>(nameof(Offset), p => p.Offset, (p, v) => p.Offset = v, _ => 0),
        new PropertyDescriptor<RDVertexAttribute, uint>(nameof(Stride), p => p.Stride, (p, v) => p.Stride = v, _ => 0),
    ]);
}
