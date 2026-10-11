namespace Electron2D;

/// <summary>Places a borrowed shadow and distance-field contour in a canvas.</summary>
/// <remarks>Visibility and transforms follow Entity. A missing or empty contour has no effect.</remarks>
public class LightOccluder : Entity
{
    private OccluderPolygon? _occluder;
    private uint _occluderLightMask = 1;
    private bool _sdfCollision = true;

    /// <summary>Creates an occluder with no contour, membership bit one and distance-field participation enabled.</summary>
    public LightOccluder() { }

    /// <summary>Gets or sets the borrowed contour; disposal of this node never disposes it.</summary>
    /// <value>Null initially.</value>
    public OccluderPolygon? Occluder { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _occluder; } set { EnsureMutable(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); _occluder = value; } }

    /// <summary>Gets or sets shadow membership matched against each light's ShadowItemCullMask.</summary>
    /// <value>One initially; zero excludes the contour from shadows.</value>
    public uint OccluderLightMask { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _occluderLightMask; } set { EnsureMutable(); _occluderLightMask = value; } }

    /// <summary>Gets or sets whether the contour participates in the canvas signed distance field.</summary>
    /// <value>True initially; shadow participation is independent.</value>
    public bool SDFCollision { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _sdfCollision; } set { EnsureMutable(); _sdfCollision = value; } }

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(LightOccluder) ? CreateOccluder : base.CreateSceneInstanceFactory();
    private static Node CreateOccluder() => new LightOccluder();

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<LightOccluder, OccluderPolygon?>(nameof(Occluder), n => n.Occluder, (n, v) => n.Occluder = v, _ => null, stored: true),
        new PropertyDescriptor<LightOccluder, uint>(nameof(OccluderLightMask), n => n.OccluderLightMask, (n, v) => n.OccluderLightMask = v, _ => 1u, stored: true),
        new PropertyDescriptor<LightOccluder, bool>(nameof(SDFCollision), n => n.SDFCollision, (n, v) => n.SDFCollision = v, _ => true, stored: true),
    ]);
}
