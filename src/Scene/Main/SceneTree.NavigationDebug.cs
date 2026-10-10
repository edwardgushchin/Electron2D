namespace Electron2D;

public sealed partial class SceneTree
{
    private bool _debugNavigationHint;
    /// <summary>Gets or sets whether tile navigation polygons are drawn when their visibility mode is Default.</summary>
    /// <remarks>Changes invalidate attached tile canvases. Ordinary visibility, transforms and clipping still apply.</remarks>
    public bool DebugNavigationHint
    {
        get { EnsureOwnerThread(); EnsureAcceptingWork(); return _debugNavigationHint; }
        set { EnsureOwnerThread(); EnsureAcceptingWork(); if (_debugNavigationHint == value) return; _debugNavigationHint = value; foreach (var node in Root.EnumerateDepthFirst()) if (node is TileMapLayer tiles) tiles.InvalidateCanvas(); }
    }
}
