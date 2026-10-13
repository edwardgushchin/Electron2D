using System.Runtime.ExceptionServices;

namespace Electron2D;

/// <summary>Runs one world's numerical interval on a retained thread between owner preparation and publication barriers.</summary>
internal sealed class PhysicsSolverThread : IDisposable
{
    private readonly PhysicsSpace _space;
    private readonly AutoResetEvent _request = new(false), _completed = new(false);
    private readonly Thread _thread;
    private PhysicsWorldBackend? _backend;
    private ExceptionDispatchInfo? _failure;
    private double _delta;
    private bool _stopping, _disposed;

    internal PhysicsSolverThread(PhysicsSpace space)
    {
        _space = space;
        _thread = new(Run) { IsBackground = true, Name = "Electron2D physics solver" };
        try { _thread.Start(); }
        catch { _request.Dispose(); _completed.Dispose(); throw; }
    }

    internal void Solve(PhysicsWorldBackend backend, double delta)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _backend = backend; _delta = delta; _failure = null;
        _space.BeginSolverBorrow(_thread.ManagedThreadId);
        try
        {
            _request.Set(); _completed.WaitOne();
            _failure?.Throw();
        }
        finally { _space.EndSolverBorrow(); _backend = null; _failure = null; }
    }

    private void Run()
    {
        while (true)
        {
            _request.WaitOne();
            if (_stopping) return;
            try { _backend!.Solve(_delta); }
            catch (Exception error) { _failure = ExceptionDispatchInfo.Capture(error); }
            finally { _completed.Set(); }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _stopping = true; _request.Set(); _thread.Join();
        _request.Dispose(); _completed.Dispose();
    }
}
