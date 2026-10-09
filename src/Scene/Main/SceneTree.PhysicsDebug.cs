namespace Electron2D;

public sealed partial class SceneTree
{
    private bool _debugCollisionsHint;
    internal readonly Color DebugCollisionsColor = ProjectSettings.GetWithOverride(ProjectSettings.DebugCollisionShapeColor);

    /// <summary>Gets or sets whether scene collision geometry, casts and joint markers are drawn.</summary>
    /// <value>False initially; diagnostics work in every build configuration.</value>
    /// <remarks>Changes invalidate attached diagnostic nodes, including hidden ones, for their next canvas recording.
    /// Ordinary visibility, modulation, clipping, transforms and canvas routing still apply. Drawing consumes
    /// authored geometry and cached cast results, without reading physics state or enabling a renderer on a server.
    /// The tree samples the project shape color at construction; CollisionShape uses its own DebugColor.
    /// This flag does not enable contact-point diagnostics or add controls to game interfaces.</remarks>
    /// <exception cref="ObjectDisposedException">The tree is finalized or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the scene owner thread.</exception>
    public bool DebugCollisionsHint
    {
        get { EnsureOwnerThread(); EnsureAcceptingWork(); return _debugCollisionsHint; }
        set
        {
            EnsureOwnerThread(); EnsureAcceptingWork();
            if (_debugCollisionsHint == value) return;
            _debugCollisionsHint = value;
            foreach (var node in Root.EnumerateDepthFirst())
                if (node is CollisionShape or CollisionPolygon or RayCast or ShapeCast or Joint) ((CanvasItem)node).InvalidateCanvas();
        }
    }
}
