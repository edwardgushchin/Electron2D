namespace Electron2D;

public class Node : Object
{
    private readonly List<Node> _children = new();
    private string _name = string.Empty;
    private SceneTree? _tree;
    private bool _readyCalled;

    public string Name
    {
        get
        {
            ThrowIfFreed();
            return _name;
        }
        set
        {
            ThrowIfFreed();
            _name = value ?? string.Empty;
        }
    }

    public Node? Parent { get; private set; }

    public void AddChild(Node child)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(child);
        child.ThrowIfFreed();

        if (child.Parent is not null || child._tree is not null)
        {
            throw new InvalidOperationException("Node already has a parent or is inside a SceneTree.");
        }

        _children.Add(child);
        child.Parent = this;

        if (_tree is not null)
        {
            _tree.AttachSubtree(child);
        }
    }

    public void RemoveChild(Node child)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(child);

        if (!_children.Remove(child))
        {
            throw new InvalidOperationException("Node is not a child of this node.");
        }

        if (child._tree is not null)
        {
            child.ExitTreeRecursive();
        }

        child.Parent = null;
    }

    public int GetChildCount()
    {
        ThrowIfFreed();
        return _children.Count;
    }

    public bool IsInsideTree()
    {
        ThrowIfFreed();
        return _tree is not null;
    }

    public SceneTree? GetTree()
    {
        ThrowIfFreed();
        return _tree;
    }

    public virtual void _EnterTree()
    {
    }

    public virtual void _Ready()
    {
    }

    public virtual void _Process(double delta)
    {
    }

    public virtual void _PhysicsProcess(double delta)
    {
    }

    public virtual void _Input(InputEvent inputEvent)
    {
    }

    public virtual void _ExitTree()
    {
    }

    internal void EnterTreeRecursive(SceneTree tree)
    {
        _tree = tree;
        tree.InvokeUserCallback(this, nameof(_EnterTree), _EnterTree);

        foreach (var child in _children)
        {
            child.EnterTreeRecursive(tree);
        }
    }

    internal void ReadyRecursive()
    {
        foreach (var child in _children)
        {
            child.ReadyRecursive();
        }

        if (_readyCalled)
        {
            return;
        }

        _readyCalled = true;
        _tree?.InvokeUserCallback(this, nameof(_Ready), _Ready);
    }

    internal void ProcessRecursive(double delta)
    {
        _tree?.InvokeUserCallback(this, nameof(_Process), () => _Process(delta));

        foreach (var child in _children)
        {
            child.ProcessRecursive(delta);
        }
    }

    internal void PhysicsProcessRecursive(double delta)
    {
        _tree?.InvokeUserCallback(this, nameof(_PhysicsProcess), () => _PhysicsProcess(delta));

        foreach (var child in _children)
        {
            child.PhysicsProcessRecursive(delta);
        }
    }

    internal void InputRecursive(InputEvent inputEvent)
    {
        _tree?.InvokeUserCallback(this, nameof(_Input), () => _Input(inputEvent));

        foreach (var child in _children)
        {
            child.InputRecursive(inputEvent);
        }
    }

    internal void ExitTreeRecursive()
    {
        var tree = _tree;

        foreach (var child in _children)
        {
            child.ExitTreeRecursive();
        }

        tree?.InvokeUserCallback(this, nameof(_ExitTree), _ExitTree);
        _tree = null;
    }
}
