namespace Electron2D;

internal static class DeferredCallQueue
{
    private static readonly Queue<DeferredCall> s_calls = new();

    public static bool HasPendingCalls => s_calls.Count > 0;

    public static void Enqueue(Callable callable, object?[] args)
    {
        s_calls.Enqueue(new DeferredCall(callable, args));
    }

    public static void Drain()
    {
        while (s_calls.Count > 0)
        {
            var call = s_calls.Dequeue();
            call.Callable.TryCall(call.Arguments, out _);
        }
    }

    private readonly record struct DeferredCall(Callable Callable, object?[] Arguments);
}
