using System.Threading;

namespace Electron2D;

public class Object
{
    private static long s_nextInstanceId;

    private readonly Dictionary<string, List<SignalConnection>> _signalConnections = new(StringComparer.Ordinal);
    private readonly HashSet<string> _signals = new(StringComparer.Ordinal);
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

    public void AddUserSignal(string signal)
    {
        ThrowIfFreed();
        _signals.Add(ValidateSignalName(signal));
    }

    public bool HasSignal(string signal)
    {
        ThrowIfFreed();
        return _signals.Contains(ValidateSignalName(signal));
    }

    public Error Connect(string signal, Callable callable, ConnectFlags flags = ConnectFlags.None)
    {
        ThrowIfFreed();
        var signalName = ValidateSignalName(signal);
        if (!_signals.Contains(signalName))
        {
            return Error.Unavailable;
        }

        if (callable.IsNull())
        {
            return Error.InvalidParameter;
        }

        if (!_signalConnections.TryGetValue(signalName, out var connections))
        {
            connections = new List<SignalConnection>();
            _signalConnections.Add(signalName, connections);
        }

        var existing = connections.FirstOrDefault(connection => connection.Callable == callable);
        if (existing is not null)
        {
            if (flags.HasFlag(ConnectFlags.ReferenceCounted))
            {
                existing.ReferenceCount++;
                return Error.Ok;
            }

            return Error.AlreadyExists;
        }

        connections.Add(new SignalConnection(callable, flags));
        return Error.Ok;
    }

    public void Disconnect(string signal, Callable callable)
    {
        ThrowIfFreed();
        var signalName = ValidateSignalName(signal);
        if (!_signalConnections.TryGetValue(signalName, out var connections))
        {
            return;
        }

        var connection = connections.FirstOrDefault(item => item.Callable == callable);
        if (connection is null)
        {
            return;
        }

        if (connection.ReferenceCount > 1)
        {
            connection.ReferenceCount--;
            return;
        }

        connections.Remove(connection);
        if (connections.Count == 0)
        {
            _signalConnections.Remove(signalName);
        }
    }

    public bool IsConnected(string signal, Callable callable)
    {
        ThrowIfFreed();
        var signalName = ValidateSignalName(signal);
        return _signalConnections.TryGetValue(signalName, out var connections) &&
            connections.Any(connection => connection.Callable == callable);
    }

    public Error EmitSignal(string signal, params object?[] args)
    {
        ThrowIfFreed();
        var signalName = ValidateSignalName(signal);
        if (!_signals.Contains(signalName))
        {
            return Error.Unavailable;
        }

        if (!_signalConnections.TryGetValue(signalName, out var connections))
        {
            return Error.Ok;
        }

        var result = Error.Ok;
        var signalArguments = args ?? Array.Empty<object?>();
        foreach (var connection in connections.ToArray())
        {
            if (connection.Callable.TryCall(signalArguments, out _) != Error.Ok)
            {
                result = Error.Failed;
            }
        }

        return result;
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
        _signals.Clear();
        _signalConnections.Clear();
    }

    private static string ValidateSignalName(string signal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signal);
        return signal;
    }

    private sealed class SignalConnection
    {
        public SignalConnection(Callable callable, ConnectFlags flags)
        {
            Callable = callable;
            Flags = flags;
        }

        public Callable Callable { get; }

        public ConnectFlags Flags { get; }

        public int ReferenceCount { get; set; } = 1;
    }
}
