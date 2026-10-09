namespace Electron2D;

public abstract partial class Viewport
{
    private bool _physicsObjectPicking, _physicsObjectPickingSort, _physicsObjectPickingFirstOnly;
    private bool _physicsPickingAuthored;
    internal PhysicsPickingState? Picking;
    internal void InitializePhysicsPicking() { if (!_physicsPickingAuthored) _physicsObjectPicking = ProjectSettings.GetWithOverride(ProjectSettings.PhysicsObjectPicking); }
    /// <summary>Gets or sets whether unhandled pointer events are queued for physics object picking.</summary>
    /// <value>False initially. Scene activation samples the project default for an unauthored root viewport.</value>
    /// <remarks>Delivery runs at the beginning of a physics frame, after its start notification and before node callbacks.
    /// Disabling clears pending events and hover. At most 64 eligible logical shape hits per canvas are selected before optional sorting.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="AggregateException">Hover-exit callbacks fail after disabling commits.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public bool PhysicsObjectPicking
    {
        get { ThrowIfDisposed(); return _physicsObjectPicking; }
        set { EnsureMutable(); _physicsPickingAuthored = true; if (_physicsObjectPicking == value) return; _physicsObjectPicking = value; if (!value) Tree?.ClearPhysicsPicking(this); }
    }
    /// <summary>Gets or sets whether selected hits are ordered by descending effective Z and reverse scene order.</summary>
    /// <value>False initially; unsorted delivery order is unspecified.</value>
    /// <remarks>Sorting occurs after the 64-hit cap and cannot promote an omitted object.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public bool PhysicsObjectPickingSort { get { ThrowIfDisposed(); return _physicsObjectPickingSort; } set { EnsureMutable(); _physicsObjectPickingSort = value; } }
    /// <summary>Gets or sets whether only the first eligible hit in each canvas receives a picking event.</summary>
    /// <value>False initially. Combine with PhysicsObjectPickingSort for Z/tree priority.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public bool PhysicsObjectPickingFirstOnly { get { ThrowIfDisposed(); return _physicsObjectPickingFirstOnly; } set { EnsureMutable(); _physicsObjectPickingFirstOnly = value; } }
    private static readonly PropertyDescriptor[] PickingProperties =
    [
        new PropertyDescriptor<Viewport, bool>(nameof(PhysicsObjectPicking), n => n.PhysicsObjectPicking, (n,v) => n.PhysicsObjectPicking=v, _ => false, stored:true),
        new PropertyDescriptor<Viewport, bool>(nameof(PhysicsObjectPickingSort), n => n.PhysicsObjectPickingSort, (n,v) => n.PhysicsObjectPickingSort=v, _ => false, stored:true),
        new PropertyDescriptor<Viewport, bool>(nameof(PhysicsObjectPickingFirstOnly), n => n.PhysicsObjectPickingFirstOnly, (n,v) => n.PhysicsObjectPickingFirstOnly=v, _ => false, stored:true)
    ];
}
