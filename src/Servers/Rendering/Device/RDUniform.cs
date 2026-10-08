namespace Electron2D;

/// <summary>Describes one shader binding and its borrowed resource identities.</summary>
public sealed class RDUniform : ElectronObject
{
    private readonly List<RID> _ids = [];
    private int _binding;
    private RenderingDevice.UniformType _type = RenderingDevice.UniformType.Image;
    /// <summary>Creates an empty image binding.</summary>
    public RDUniform() { }
    /// <summary>Gets or sets the descriptor binding number.</summary>
    /// <value>A nonnegative shader binding, initially zero.</value>
    public int Binding { get { ThrowIfDisposed(); return _binding; } set { ThrowIfDisposed(); ArgumentOutOfRangeException.ThrowIfNegative(value); _binding = value; } }
    /// <summary>Gets or sets the resource category.</summary>
    /// <value>The image category initially; set a supported buffer category before creating a compute uniform set.</value>
    public RenderingDevice.UniformType UniformType { get { ThrowIfDisposed(); return _type; } set { ThrowIfDisposed(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _type = value; } }
    /// <summary>Appends a borrowed resource identity.</summary>
    /// <param name="id">The resource, validated against the owning device when the set is created.</param>
    public void AddID(RID id) { ThrowIfDisposed(); _ids.Add(id); }
    /// <summary>Removes all borrowed identities.</summary>
    public void ClearIDs() { ThrowIfDisposed(); _ids.Clear(); }
    /// <summary>Copies the binding's resource identities.</summary>
    /// <returns>An independent array in insertion order.</returns>
    public RID[] GetIDs() { ThrowIfDisposed(); return _ids.ToArray(); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<RDUniform, int>(nameof(Binding), p => p.Binding, (p, v) => p.Binding = v, _ => 0),
         new PropertyDescriptor<RDUniform, RenderingDevice.UniformType>(nameof(UniformType), p => p.UniformType, (p, v) => p.UniformType = v, _ => RenderingDevice.UniformType.Image)]);
}
