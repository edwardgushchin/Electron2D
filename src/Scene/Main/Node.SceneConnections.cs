namespace Electron2D;

public partial class Node
{
    private static readonly object SceneConnectionGate = new();
    private List<SceneConnectionRuntime>? _sceneConnections;
    internal void EnsureConnectionMutation() => EnsureMutable();
    internal void RetainSceneConnection(SceneConnectionRuntime connection)
    {
        lock (SceneConnectionGate)
        {
            (_sceneConnections ??= []).Add(connection);
            if (!ReferenceEquals(connection.Subscriber, this))
                (connection.Subscriber._sceneConnections ??= []).Add(connection);
        }
        connection.Token?.OnFinished(() => RemoveSceneConnection(connection));
        try { if (connection.Token?.IsConnected != false) EmitEditorStateChanged(); }
        catch { RemoveSceneConnection(connection, notify: false); throw; }
    }
    internal SceneConnectionRuntime RetainRestoredSceneConnection(SceneConnectionSchema schema, Node subscriber, StoredPropertyValue bindings)
    {
        EnsureConnectionMutation(); subscriber.EnsureConnectionMutation();
        var connection = new SceneConnectionRuntime(schema, this, subscriber, bindings, null);
        RetainSceneConnection(connection); return connection;
    }
    internal IDisposable RetainRecordedSceneConnection(SceneConnectionSchema schema, Node subscriber, StoredPropertyValue bindings) =>
        new RecordedConnectionLease(RetainRestoredSceneConnection(schema, subscriber, bindings));
    private sealed class RecordedConnectionLease(SceneConnectionRuntime connection) : IDisposable
    {
        private WeakReference<SceneConnectionRuntime>? _connection = new(connection);
        public void Dispose()
        {
            if (_connection is null || !_connection.TryGetTarget(out var current)) { _connection = null; return; }
            if (current.Publisher.IsDisposed || current.Subscriber.IsDisposed) { _connection = null; return; }
            current.Publisher.EnsureConnectionMutation(); current.Subscriber.EnsureConnectionMutation();
            _connection = null; RemoveSceneConnection(current);
        }
    }
    internal SceneConnectionRuntime[] CaptureSceneConnections()
    {
        SceneConnectionRuntime[] snapshot;
        lock (SceneConnectionGate) snapshot = _sceneConnections?.ToArray() ?? [];
        return snapshot.Where(c => ReferenceEquals(c.Publisher, this) && c.Token?.IsConnected != false).ToArray();
    }
    private static void RemoveSceneConnection(SceneConnectionRuntime connection, bool notify = true)
    {
        bool removed;
        lock (SceneConnectionGate)
        {
            removed = connection.Publisher._sceneConnections?.Remove(connection) == true;
            connection.Subscriber._sceneConnections?.Remove(connection);
        }
        if (notify && removed && connection.Token?.WasConnectedAtDisposal != false && !connection.Subscriber.IsDisposed)
            connection.Publisher.EmitEditorStateChanged();
    }
    private void DisposeSceneConnections()
    {
        SceneConnectionRuntime[] connections;
        lock (SceneConnectionGate)
        {
            connections = _sceneConnections?.ToArray() ?? [];
            foreach (var connection in connections) RemoveSceneConnection(connection);
        }
        List<Exception>? errors = null;
        foreach (var connection in connections)
        {
            try { connection.Token?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors is not null) throw new AggregateException(errors);
    }
}
