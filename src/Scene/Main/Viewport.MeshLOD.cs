namespace Electron2D;

public abstract partial class Viewport
{
    internal const float DefaultMeshLODThreshold = 1f;
    private float _meshLODThreshold = DefaultMeshLODThreshold;
    private bool _meshLODThresholdAssigned;

    internal static bool IsMeshLODThreshold(float value) => float.IsFinite(value) && value >= 0;

    internal void ApplyProjectMeshLODThreshold()
    {
        if (!_meshLODThresholdAssigned) _meshLODThreshold = ProjectSettings.GetWithOverride(ProjectSettings.MeshLODThreshold);
    }

    /// <summary>Gets or sets the maximum projected mesh LOD error in framebuffer pixels.</summary>
    /// <value>One pixel by default. Zero selects base geometry. An unassigned root window uses the project setting at startup.</value>
    /// <remarks>Positive values select the coarsest authored level within this error bound. Changes apply to the next draw of this viewport.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the viewport is being captured or submitted.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public float MeshLODThreshold
    {
        get { CheckTransformQuery(); return _meshLODThreshold; }
        set
        {
            EnsureMutable();
            if (!IsMeshLODThreshold(value)) throw new ArgumentOutOfRangeException(nameof(value));
            RenderingOwner?.EnsureServerViewportChange();
            _meshLODThresholdAssigned = true;
            if (_meshLODThreshold == value) return;
            _meshLODThreshold = value;
            RenderingOwner?.MarkChanged();
        }
    }

    private static readonly PropertyDescriptor[] MeshLODProperties =
    [
        new PropertyDescriptor<Viewport, float>(nameof(MeshLODThreshold), n => n.MeshLODThreshold, (n, v) => n.MeshLODThreshold = v,
            _ => DefaultMeshLODThreshold, (_, v) => IsMeshLODThreshold(v), stored: true)
        { StoreCondition = n => n._meshLODThresholdAssigned }
    ];
}
