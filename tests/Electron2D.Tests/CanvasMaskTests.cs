using Electron2D;

internal static class CanvasMaskTests
{
    internal static void Run()
    {
        using var window = new Window(); using var item = new Entity { Name = "Item" };
        Check(item.VisibilityLayer == 1 && window.CanvasCullMask == uint.MaxValue, "Default visibility masks.");
        for (var bit = 0; bit < 32; bit++)
        {
            item.VisibilityLayer = 0; window.CanvasCullMask = uint.MaxValue;
            item.SetVisibilityLayerBit(bit, true); window.SetCanvasCullMaskBit(bit, false);
            Check(item.GetVisibilityLayerBit(bit) && !window.GetCanvasCullMaskBit(bit) && item.VisibilityLayer == (1u << bit) && window.CanvasCullMask == ~(1u << bit), "Unsigned bit edits preserve other bits.");
            item.SetVisibilityLayerBit(bit, false); window.SetCanvasCullMaskBit(bit, true);
            Check(item.VisibilityLayer == 0 && window.CanvasCullMask == uint.MaxValue, "Bit edits restore the full masks.");
        }
        foreach (var bit in new[] { -1, 32, int.MaxValue })
        {
            Reject<ArgumentOutOfRangeException>(() => item.GetVisibilityLayerBit(bit)); Reject<ArgumentOutOfRangeException>(() => item.SetVisibilityLayerBit(bit, true));
            Reject<ArgumentOutOfRangeException>(() => window.GetCanvasCullMaskBit(bit)); Reject<ArgumentOutOfRangeException>(() => window.SetCanvasCullMaskBit(bit, false));
        }
        Check(item.VisibilityLayer == 0 && window.CanvasCullMask == uint.MaxValue, "Invalid edits are atomic.");
        window.CanvasCullMask = 0x80000003; item.VisibilityLayer = 0x80000002; window.AddChild(item); item.Owner = window;
        using var packed = new PackedScene(); packed.Pack(window); using var copy = (Window)packed.Instantiate();
        Check(copy.CanvasCullMask == window.CanvasCullMask && ((Entity)copy.GetChild(0)).VisibilityLayer == item.VisibilityLayer, "Packed masks retain all 32 bits.");
        Check(window.GetPropertyList().Single(p => p.Name == nameof(Viewport.CanvasCullMask)).IsStored && item.GetPropertyList().Single(p => p.Name == nameof(CanvasItem.VisibilityLayer)).IsStored, "Both mask descriptors are stored.");

        var root = new TestViewport(); var active = new Entity(); root.AddChild(active); using var tree = new SceneTree(root);
        var notifications = 0; active.VisibilityChanged += _ => notifications++; active.Hidden += _ => notifications++;
        active.VisibilityLayer = 0; root.CanvasCullMask = 0;
        Check(active.Visible && active.IsVisibleInTree && notifications == 0, "Mask culling does not change logical visibility or emit events.");
        var capture = root.BeginSceneCapture();
        try
        {
            Reject<InvalidOperationException>(() => active.VisibilityLayer = 1); Reject<InvalidOperationException>(() => active.SetVisibilityLayerBit(0, true));
            Reject<InvalidOperationException>(() => root.CanvasCullMask = 1); Reject<InvalidOperationException>(() => root.SetCanvasCullMaskBit(0, true));
        }
        finally { Node.EndSceneCapture(capture); }
        Task.Run(() =>
        {
            Reject<InvalidOperationException>(() => _ = active.VisibilityLayer); Reject<InvalidOperationException>(() => active.GetVisibilityLayerBit(0));
            Reject<InvalidOperationException>(() => active.VisibilityLayer = 1); Reject<InvalidOperationException>(() => active.SetVisibilityLayerBit(0, true));
            Reject<InvalidOperationException>(() => _ = root.CanvasCullMask); Reject<InvalidOperationException>(() => root.GetCanvasCullMaskBit(0));
            Reject<InvalidOperationException>(() => root.CanvasCullMask = 1); Reject<InvalidOperationException>(() => root.SetCanvasCullMaskBit(0, true));
        }).GetAwaiter().GetResult();
        tree.Dispose();
        Reject<ObjectDisposedException>(() => _ = active.VisibilityLayer); Reject<ObjectDisposedException>(() => active.GetVisibilityLayerBit(0));
        Reject<ObjectDisposedException>(() => active.VisibilityLayer = 1); Reject<ObjectDisposedException>(() => active.SetVisibilityLayerBit(0, true));
        Reject<ObjectDisposedException>(() => _ = root.CanvasCullMask); Reject<ObjectDisposedException>(() => root.GetCanvasCullMaskBit(0));
        Reject<ObjectDisposedException>(() => root.CanvasCullMask = 1); Reject<ObjectDisposedException>(() => root.SetCanvasCullMaskBit(0, true));
        Console.WriteLine("Canvas masks, storage, ownership and logical visibility passed.");
    }

    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 100, 80); }
    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
