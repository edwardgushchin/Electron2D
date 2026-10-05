namespace Electron2D;

/// <summary>Exposes the physics space used by a scene tree's two-dimensional world.</summary>
/// <remarks>Canvas and navigation-map identities are implemented in their respective server slices.</remarks>
public sealed class World : Resource
{
    private RID _space;

    internal World(RID space) => _space = space;

    /// <summary>Gets the server identity of the live physics space.</summary>
    /// <value>A stable RID while the owning SceneTree is alive.</value>
    /// <exception cref="ArgumentException">The owning physics space was freed.</exception>
    /// <exception cref="ObjectDisposedException">This world wrapper was disposed.</exception>
    public RID Space
    {
        get { ThrowIfDisposed(); PhysicsServer.Service.GetSceneSpace(_space); return _space; }
    }

    /// <summary>Gets the live direct-query view of this physics space.</summary>
    /// <value>The view shares the scene's solver state and obeys its owner thread.</value>
    /// <exception cref="ArgumentException">The owning physics space was freed.</exception>
    /// <exception cref="ObjectDisposedException">This world wrapper was disposed.</exception>
    public PhysicsDirectSpaceState DirectSpaceState
    {
        get { ThrowIfDisposed(); return PhysicsServer.SpaceGetDirectState(_space); }
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new World(_space);

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) =>
        ((World)target)._space = _space;
}
