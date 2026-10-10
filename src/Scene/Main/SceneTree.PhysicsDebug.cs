namespace Electron2D;

public sealed partial class SceneTree
{
    private bool _debugCollisionsHint;
    internal readonly Color DebugContactColor = ProjectSettings.GetWithOverride(ProjectSettings.DebugCollisionContactColor);
    internal readonly int DebugContactLimit = ProjectSettings.GetWithOverride(ProjectSettings.DebugCollisionMaxContacts);
    internal ReadOnlySpan<WorldRuntime> DebugPhysicsWorlds => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_physicsWorlds);
    internal readonly Color DebugCollisionsColor = ProjectSettings.GetWithOverride(ProjectSettings.DebugCollisionShapeColor);

    /// <summary>Gets or sets whether scene collision geometry, casts, joint markers and contact points are drawn.</summary>
    /// <value>False initially; diagnostics work in every build configuration.</value>
    /// <remarks>Changes invalidate attached diagnostic nodes, including hidden ones, for their next canvas recording.
    /// Ordinary visibility, modulation, clipping, transforms and canvas routing still apply. Drawing consumes
    /// authored geometry, cached cast results and bounded contact snapshots from the last completed physics step.
    /// GPU worlds read only selected point coordinates when contact diagnostics are enabled; no renderer is started on a server.
    /// The tree samples the project shape color at construction; CollisionShape uses its own DebugColor.
    /// Contact color and point limit are sampled from project settings at tree construction. No game controls are added.</remarks>
    /// <exception cref="ObjectDisposedException">The tree is finalized or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the scene owner thread, a world is stepping or failed, or diagnostic GPU preparation fails.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The sampled contact limit exceeds addressable diagnostic storage.</exception>
    public bool DebugCollisionsHint
    {
        get { EnsureOwnerThread(); EnsureAcceptingWork(); return _debugCollisionsHint; }
        set
        {
            EnsureOwnerThread(); EnsureAcceptingWork();
            if (_debugCollisionsHint == value) return;
            EnsurePhysicsParticipationChange(releasing: !value);
            try { foreach (var runtime in _physicsWorlds) runtime.ExistingSpace?.SetDebugContacts(value ? DebugContactLimit : 0); }
            catch
            {
                if (value) foreach (var runtime in _physicsWorlds) runtime.ExistingSpace?.SetDebugContacts(0);
                throw;
            }
            _debugCollisionsHint = value;
            foreach (var node in Root.EnumerateDepthFirst())
                if (node is CollisionShape or CollisionPolygon or RayCast or ShapeCast or Joint or TileMapLayer) ((CanvasItem)node).InvalidateCanvas();
        }
    }
}
