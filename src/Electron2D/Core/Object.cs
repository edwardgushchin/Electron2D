using System.Threading;

namespace Electron2D;

public class Object
{
    private static long s_nextInstanceId;

    private readonly ulong _instanceId = (ulong)Interlocked.Increment(ref s_nextInstanceId);
    private bool _freed;

    public ulong GetInstanceId()
    {
        return _instanceId;
    }

    public void Free()
    {
        if (_freed)
        {
            return;
        }

        _freed = true;
        OnFree();
    }

    public static bool IsInstanceValid(Object? instance)
    {
        return instance is not null && !instance._freed;
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

    protected virtual void OnFree()
    {
    }
}
