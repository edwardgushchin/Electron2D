namespace Electron2D;

public class SceneTree : Object
{
    private readonly List<SceneTreeDiagnostic> _diagnostics = new();

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
    }

    internal void PhysicsFrame(double delta)
    {
        Root.PhysicsProcessRecursive(delta);
    }

    internal void DispatchInput(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        Root.InputRecursive(inputEvent);
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
}

internal sealed record SceneTreeDiagnostic(Node Node, string Callback, Exception Exception);
