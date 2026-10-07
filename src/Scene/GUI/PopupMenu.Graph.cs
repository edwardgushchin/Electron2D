namespace Electron2D;

public partial class PopupMenu
{
    private float _graphScale = 1;
    private float GraphScaleFromOwner()
    {
        for (var ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor is PopupMenu menu) return menu._graphScale;
            if (ancestor is GraphElement element) return element.ScalingMenus && element.Parent is GraphEdit graph ? graph.Zoom : 1;
        }
        return 1;
    }
    internal void ApplyGraphScale(float scale)
    {
        if (_graphScale == scale) return;
        var ratio = scale / _graphScale; _graphScale = scale;
        CanvasTransform = new Transform(0, new Vector2(scale, scale), 0, Vector2.Zero);
        if (!Visible) return;
        _arranging = true;
        try
        {
            if (_contentRect is { } rect) _contentRect = new(rect.Position, (Vector2i)(((Vector2)rect.Size * ratio).Ceil()));
            _expandedSize = (Vector2i)(((Vector2)Size * ratio).Ceil()); Size = _expandedSize;
        }
        finally { _arranging = false; }
        Arrange();
    }
}
