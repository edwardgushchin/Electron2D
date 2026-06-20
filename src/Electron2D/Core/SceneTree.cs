namespace Electron2D;

public class SceneTree : Object
{
    private readonly List<SceneTreeDiagnostic> _diagnostics = new();
    private readonly List<Node> _deleteQueue = new();

    public SceneTree()
    {
        Root = new Node { Name = "Root" };
        AttachSubtree(Root);
    }

    public Node Root { get; }

    internal IReadOnlyList<SceneTreeDiagnostic> Diagnostics => _diagnostics;

    internal void AttachSubtree(Node node)
    {
        node.EnterTreeRecursive(this);
        node.ReadyRecursive();
    }

    internal void ProcessFrame(double delta)
    {
        Root.ProcessRecursive(delta);
        FlushDeleteQueue();
    }

    internal void PhysicsFrame(double delta)
    {
        Root.PhysicsProcessRecursive(delta);
        FlushDeleteQueue();
    }

    internal void DispatchInput(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        Root.InputRecursive(inputEvent);
        FlushDeleteQueue();
    }

    internal void QueueDelete(Node node)
    {
        if (!_deleteQueue.Contains(node))
        {
            _deleteQueue.Add(node);
        }
    }

    internal void InvokeUserCallback(Node node, string callback, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            _diagnostics.Add(new SceneTreeDiagnostic(node, callback, exception));
        }
    }

    private void FlushDeleteQueue()
    {
        while (_deleteQueue.Count > 0)
        {
            var queuedNodes = _deleteQueue.ToArray();
            _deleteQueue.Clear();

            foreach (var node in queuedNodes)
            {
                if (Object.IsInstanceValid(node))
                {
                    node.Free();
                }
            }
        }
    }
}

internal sealed record SceneTreeDiagnostic(Node Node, string Callback, Exception Exception);
