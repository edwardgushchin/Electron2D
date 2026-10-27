namespace Electron2D;

public sealed partial class SceneTree
{
    private bool _debugNavigationHint;
    /// <summary>Gets or sets whether navigation regions, links, avoidance radii/contours and default-mode tile polygons are drawn.</summary>
    /// <remarks>Changes invalidate attached tile canvases; retained navigation diagnostics observe this tree-local request at recording.
    /// Does not change NavigationServer.GetDebugEnabled or explicit agent path requests. Canvas routing and visibility still apply.</remarks>
    public bool DebugNavigationHint
    {
        get { EnsureOwnerThread(); EnsureAcceptingWork(); return _debugNavigationHint; }
        set { EnsureOwnerThread(); EnsureAcceptingWork(); if (_debugNavigationHint == value) return; _debugNavigationHint = value; foreach (var node in Root.EnumerateDepthFirst()) if (node is TileMapLayer tiles) tiles.InvalidateCanvas(); }
    }
}
