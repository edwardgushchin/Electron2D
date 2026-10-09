namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly Dictionary<WorldRuntime, ContactCanvas> _contactCanvases = [];
    private sealed class ContactCanvas(RID item)
    {
        internal readonly RID Item = item;
        internal ulong Revision = ulong.MaxValue;
    }
    private void UpdatePhysicsContacts(SceneTree tree)
    {
        foreach (var pair in _contactCanvases)
            if (!tree.DebugCollisionsHint || !pair.Key.Alive || !ReferenceEquals(pair.Key.SceneOwner, tree))
            { ReleaseCanvasRID(pair.Value.Item); _contactCanvases.Remove(pair.Key); }
        if (!tree.DebugCollisionsHint) return;
        foreach (var runtime in tree.DebugPhysicsWorlds)
        {
            if (runtime.ExistingSpace is not { } space) continue;
            var points = space.DebugContacts;
            if (!_contactCanvases.TryGetValue(runtime, out var canvas))
            {
                if (points.IsEmpty) continue;
                var item = CanvasItemCreateCore();
                try { CanvasItemSetParentCore(item, runtime.Canvas.RID); canvas = new(item); _contactCanvases.Add(runtime, canvas); }
                catch { ReleaseCanvasRID(item); throw; }
            }
            if (canvas.Revision == space.DebugContactRevision) continue;
            CanvasItemClearCore(canvas.Item);
            var target = CommandTarget(canvas.Item);
            using (target.StartServerDrawing())
                foreach (var point in points) target.DrawRect(new(point - new Vector2(2, 2), new Vector2(5, 5)), tree.DebugContactColor);
            canvas.Revision = space.DebugContactRevision;
        }
    }
}
