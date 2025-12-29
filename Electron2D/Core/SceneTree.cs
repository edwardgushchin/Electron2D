namespace Electron2D;

public class SceneTree
{
    public SceneTree(Node root)
    {
        Root = root;
        Root.InternalEnterTree(this);
        Root.InternalReadyIfNeeded();
    }

    public Node Root { get; }
}