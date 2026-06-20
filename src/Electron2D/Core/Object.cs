using System.Threading;

namespace Electron2D;

public class Object
{
    private static long s_nextInstanceId;

    private readonly ulong _instanceId = (ulong)Interlocked.Increment(ref s_nextInstanceId);
    private bool _freed;
    private bool _freeing;
    private bool _queuedForDeletion;

    public ulong GetInstanceId()
    {
        return _instanceId;
    }

    public void Free()
    {
        if (_freed || _freeing)
        {
            return;
        }

        _freeing = true;
        try
        {
            OnFree();
        }
        finally
        {
            _freed = true;
            _freeing = false;
            _queuedForDeletion = false;
        }
    }

    public static bool IsInstanceValid(Object? instance)
    {
        return instance is not null && !instance._freed;
    }

    public bool IsQueuedForDeletion()
    {
        return _queuedForDeletion;
    }

    public override string ToString()
    {
        return $"{GetType().Name}:{_instanceId}";
    }

    protected void ThrowIfFreed()
    {
        if (_freed)
        {
            throw new InvalidOperationException($"{GetType().Name} instance was freed.");
        }
    }

    protected bool MarkQueuedForDeletion()
    {
        if (_queuedForDeletion)
        {
            return false;
        }

        _queuedForDeletion = true;
        return true;
    }

    protected void ClearQueuedForDeletion()
    {
        _queuedForDeletion = false;
    }

    protected virtual void OnFree()
    {
    }
}
