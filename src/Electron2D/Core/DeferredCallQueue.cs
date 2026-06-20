namespace Electron2D;

internal static class DeferredCallQueue
{
    private static readonly Queue<DeferredCall> s_globalCalls = new();

    [ThreadStatic]
    private static SceneTree? t_currentTree;

    public static bool HasGlobalPendingCalls
    {
        get
        {
            lock (s_globalCalls)
            {
                return s_globalCalls.Count > 0;
            }
        }
    }

    public static void Enqueue(Callable callable, object?[] args)
    {
        var tree = (callable.GetObject() as Node)?.GetTree() ?? t_currentTree;
        if (tree is not null)
        {
            tree.QueueDeferredCall(callable, args);
            return;
        }

        lock (s_globalCalls)
        {
            s_globalCalls.Enqueue(new DeferredCall(callable, args));
        }
    }

    public static IDisposable EnterTree(SceneTree tree)
    {
        var previousTree = t_currentTree;
        t_currentTree = tree;
        return new TreeScope(previousTree);
    }

    public static void DrainGlobal(SceneTree tree)
    {
        while (true)
        {
            DeferredCall call;
            lock (s_globalCalls)
            {
                if (s_globalCalls.Count == 0)
                {
                    return;
                }

                call = s_globalCalls.Dequeue();
            }

            Execute(tree, call);
        }
    }

    public static void Execute(SceneTree tree, DeferredCall call)
    {
        var result = call.Callable.TryCall(call.Arguments, out _, out var exception);
        if (result != Error.Ok && exception is not null)
        {
            tree.ReportUserCodeException(
                call.Callable.GetObject() as Node,
                call.Callable.GetMethod(),
                exception,
                RuntimeUserCodeFailureKind.DeferredCall);
        }
    }

    internal readonly record struct DeferredCall(Callable Callable, object?[] Arguments);

    private sealed class TreeScope : IDisposable
    {
        private readonly SceneTree? _previousTree;
        private bool _disposed;

        public TreeScope(SceneTree? previousTree)
        {
            _previousTree = previousTree;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            t_currentTree = _previousTree;
            _disposed = true;
        }
    }
}
