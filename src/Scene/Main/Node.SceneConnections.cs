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
        connection.Token.OnFinished(() => RemoveSceneConnection(connection));
    }
    internal SceneConnectionRuntime[] CaptureSceneConnections()
    {
        SceneConnectionRuntime[] snapshot;
        lock (SceneConnectionGate) snapshot = _sceneConnections?.ToArray() ?? [];
        return snapshot.Where(c => ReferenceEquals(c.Publisher, this) && c.Token.IsConnected).ToArray();
    }
    private static void RemoveSceneConnection(SceneConnectionRuntime connection)
    {
        lock (SceneConnectionGate)
        {
            connection.Publisher._sceneConnections?.Remove(connection);
            connection.Subscriber._sceneConnections?.Remove(connection);
        }
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
            try { connection.Token.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors is not null) throw new AggregateException(errors);
    }
}
