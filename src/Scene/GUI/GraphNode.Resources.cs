namespace Electron2D;

public partial class GraphNode
{
    private readonly HashSet<Texture> _icons = [];
    private static void ValidateIcon(Texture? icon) { if (icon?.IsDisposed == true) throw new ObjectDisposedException(nameof(icon)); }
    private void WatchIcon(Texture? icon) { if (icon is null || !_icons.Add(icon)) return; icon.Changed += IconChanged; icon.Disposed += IconDisposed; }
    private void IconChanged(Resource resource)
    {
        if (IsDisposed) return;
        if (Tree is { IsOwnerThread: false } tree) { if (!tree.IsClosing) tree.Defer(() => IconChanged(resource)); return; }
        QueueRedraw();
    }
    private void IconDisposed(ElectronObject resource)
    {
        if (IsDisposed) return;
        if (Tree is { IsOwnerThread: false } tree) { if (!tree.IsClosing) tree.Defer(() => IconDisposed(resource)); return; }
        var texture = (Texture)resource; _icons.Remove(texture); texture.Changed -= IconChanged; texture.Disposed -= IconDisposed;
        foreach (var slot in _slots.Values) { if (slot.LeftIcon == texture) slot.LeftIcon = null; if (slot.RightIcon == texture) slot.RightIcon = null; }
        QueueRedraw();
    }
    private void PruneIcons() { _icons.RemoveWhere(icon => { foreach (var slot in _slots.Values) if (slot.LeftIcon == icon || slot.RightIcon == icon) return false; icon.Changed -= IconChanged; icon.Disposed -= IconDisposed; return true; }); }
    private void UnwatchIcons() { foreach (var icon in _icons) { icon.Changed -= IconChanged; icon.Disposed -= IconDisposed; } _icons.Clear(); }
}
