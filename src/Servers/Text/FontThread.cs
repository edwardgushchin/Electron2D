using System.Runtime.ExceptionServices;

namespace Electron2D;

// All mutable native font/raster state lives on this thread. A serialized reusable call slot keeps
// synchronous resource queries thread-safe without allocating jobs in prepared shaping/drawing paths.
// ponytail: native cache misses serialize here; shard font owners only if measured contention requires it.
internal static class FontThread
{
    private static readonly object InvocationGate = new(), WorkGate = new();
    private static readonly Thread Worker = Start();
    private static Action? _operation;
    private static ExceptionDispatchInfo? _error;
    private static bool _complete;
    private static int _owner;

    private static Thread Start()
    {
        var thread = new Thread(Loop) { IsBackground = true, Name = "Electron2D text" };
        thread.Start(); return thread;
    }
    internal static void Invoke(Action operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _ = Worker;
        if (Environment.CurrentManagedThreadId == Volatile.Read(ref _owner)) { operation(); return; }
        lock (InvocationGate)
        {
            ExceptionDispatchInfo? error;
            lock (WorkGate)
            {
                _operation = operation; _complete = false; Monitor.Pulse(WorkGate);
                while (!_complete) Monitor.Wait(WorkGate);
                error = _error; _error = null;
            }
            error?.Throw();
        }
    }
    private static void Loop()
    {
        Volatile.Write(ref _owner, Environment.CurrentManagedThreadId);
        while (true)
        {
            Action? operation;
            lock (WorkGate)
            {
                while (_operation is null) Monitor.Wait(WorkGate);
                operation = _operation;
            }
            ExceptionDispatchInfo? error = null;
            try { operation(); } catch (Exception failure) { error = ExceptionDispatchInfo.Capture(failure); }
            operation = null;
            lock (WorkGate)
            {
                _operation = null; _error = error; _complete = true; Monitor.Pulse(WorkGate);
            }
            error = null;
        }
    }
}
