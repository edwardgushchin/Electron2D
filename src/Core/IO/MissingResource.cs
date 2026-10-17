namespace Electron2D;

/// <summary>Preserves bounded typed file fields when an original compiled resource factory is unavailable.</summary>
/// <remarks>Fields are asset data, never CLR member dispatch. Scalar descriptors expose copied encoded bytes;
/// typed access validates the original codec/schema. Materials and other nested resources remain borrowed.</remarks>
public sealed class MissingResource : Resource
{
    internal MissingResourceData Data = new();
    /// <summary>Creates an empty placeholder with recording disabled.</summary>
    public MissingResource() { }
    /// <summary>Gets or sets the stable factory identity written when this resource is saved.</summary>
    /// <value>An ordinal ID; empty uses the ordinary MissingResource registration.</value>
    /// <remarks>Explicit assignment authors a recorded-state binding and clears unavailable-payload provenance.
    /// Automatic copying and file persistence preserve that provenance.</remarks>
    public string OriginalClass { get { ThrowIfDisposed(); return Data.OriginalClass; } set { ArgumentNullException.ThrowIfNull(value); ThrowIfDisposed(); if (value.Length > 65536) throw new ArgumentException("Factory ID exceeds file budget.", nameof(value)); Data.OriginalClass = value; Data.MissingPayload = false; EmitChanged(); } }
    /// <summary>Gets or selects whether previously absent recorded fields may be added.</summary>
    /// <value>False initially. Existing fields retain writable exact-schema storage in either mode.</value>
    public bool RecordingProperties { get { ThrowIfDisposed(); return Data.Recording; } set { ThrowIfDisposed(); Data.Recording = value; } }
    /// <summary>Copies a typed value into an original asset field.</summary>
    /// <typeparam name="TValue">A registered scalar codec, Resource or registered typed resource array.</typeparam>
    /// <param name="name">Nonblank original file field name; no CLR property is invoked.</param>
    /// <param name="value">Value to copy; nested resource identities are borrowed.</param>
    /// <remarks>Adding requires RecordingProperties; replacement requires the same schema. Failure leaves prior fields intact.</remarks>
    public void SetRecordedProperty<TValue>(string name, TValue value) { ThrowIfDisposed(); Data.Set(name, value); EmitChanged(); }
    /// <summary>Reads a copied typed scalar/array or borrowed resource from one original field.</summary>
    /// <typeparam name="TValue">Exact recorded schema with an available codec.</typeparam>
    /// <param name="name">Existing original field name.</param>
    /// <returns>The decoded copied value or borrowed resource identity.</returns>
    public TValue GetRecordedProperty<TValue>(string name) { ThrowIfDisposed(); return Data.Get<TValue>(name); }
    private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<MissingResource, string>(nameof(OriginalClass), r => r.OriginalClass, (r, v) => r.OriginalClass = v, _ => ""), new PropertyDescriptor<MissingResource, bool>(nameof(RecordingProperties), r => r.RecordingProperties, (r, v) => r.RecordingProperties = v, _ => false)];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties).Concat(Data.Descriptors(this));
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new MissingResource();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) { ThrowIfDisposed(); ((MissingResource)target).Data = Data.Copy(duplicateSubresource); }
    /// <inheritdoc />
    protected override void RemapResourceReferences(Func<Resource, Resource> remap) { base.RemapResourceReferences(remap); Data.Remap(remap); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { Data.Fields.Clear(); base.Dispose(disposing); }
}
