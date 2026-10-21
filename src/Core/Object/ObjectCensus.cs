namespace Electron2D;

internal static class ObjectCensus
{
    private static readonly object Gate = new();
    private static readonly List<WeakReference<ElectronObject>> Objects = [];
    private static int _untilCompaction;

    internal static void Register(WeakReference<ElectronObject> reference)
    {
        lock (Gate)
        {
            if (_untilCompaction-- <= 0)
            {
                CountUnderLock(null, null);
                _untilCompaction = Math.Max(64, Objects.Count / 2);
            }
            Objects.Add(reference);
        }
    }

    internal static int Count(Performance.Monitor monitor)
    {
        var tree = monitor is Performance.Monitor.ObjectNodeCount or Performance.Monitor.ObjectOrphanNodeCount
            ? Engine.MainLoop as SceneTree : null;
        lock (Gate) return CountUnderLock(monitor, tree);
    }

    private static int CountUnderLock(Performance.Monitor? monitor, SceneTree? tree)
    {
        var count = 0;
        var retained = 0;
        for (var i = 0; i < Objects.Count; i++)
        {
            var reference = Objects[i];
            if (!reference.TryGetTarget(out var value) || value.IsDisposalCommitted) continue;
            Objects[retained++] = reference;
            if (monitor == Performance.Monitor.ObjectCount || value is Node node &&
                (monitor == Performance.Monitor.ObjectNodeCount && tree is not null && ReferenceEquals(node.Tree, tree) ||
                 monitor == Performance.Monitor.ObjectOrphanNodeCount && (tree is null || !ReferenceEquals(node.Tree, tree))))
                count++;
        }
        Objects.RemoveRange(retained, Objects.Count - retained);
        return count;
    }
}
