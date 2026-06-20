namespace Electron2D;

public class Node : Object
{
    private readonly List<Node> _children = new();
    private readonly Dictionary<string, bool> _groups = new(StringComparer.Ordinal);
    private string _name = string.Empty;
    private Node? _parent;
    private Node? _owner;
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
            _parent?.MakeChildNameUnique(this);
        }
    }

    public Node? Owner
    {
        get
        {
            ThrowIfFreed();
            return _owner;
        }
        set
        {
            ThrowIfFreed();
            SetOwner(value);
        }
    }

    public void AddChild(Node child)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(child);
        child.ThrowIfFreed();

        if (ReferenceEquals(child, this) || child.IsAncestorOf(this))
        {
            throw new InvalidOperationException("A node cannot become a child of itself or its descendant.");
        }

        if (child._parent is not null || child._tree is not null)
        {
            throw new InvalidOperationException("Node already has a parent or is inside a SceneTree.");
        }

        _children.Add(child);
        child._parent = this;
        MakeChildNameUnique(child);

        if (_tree is not null)
        {
            _tree.AttachSubtree(child);
        }
    }

    public void RemoveChild(Node child)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(child);
        child.ThrowIfFreed();

        DetachChild(child, clearInvalidOwners: true);
    }

    public Node? GetParent()
    {
        ThrowIfFreed();
        return _parent;
    }

    public Node? GetChild(int index)
    {
        ThrowIfFreed();
        var childIndex = NormalizeChildIndex(index);
        if (childIndex < 0 || childIndex >= _children.Count)
        {
            return null;
        }

        return _children[childIndex];
    }

    public int GetChildCount()
    {
        ThrowIfFreed();
        return _children.Count;
    }

    public int GetIndex()
    {
        ThrowIfFreed();
        return _parent is null ? -1 : _parent._children.IndexOf(this);
    }

    public bool IsAncestorOf(Node node)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(node);
        node.ThrowIfFreed();

        var current = node._parent;
        while (current is not null)
        {
            if (ReferenceEquals(current, this))
            {
                return true;
            }

            current = current._parent;
        }

        return false;
    }

    public void MoveChild(Node childNode, int toIndex)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(childNode);
        childNode.ThrowIfFreed();

        var currentIndex = _children.IndexOf(childNode);
        if (currentIndex < 0)
        {
            throw new InvalidOperationException("Node is not a child of this node.");
        }

        var childIndex = NormalizeChildIndex(toIndex);
        if (childIndex < 0 || childIndex >= _children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(toIndex));
        }

        if (currentIndex == childIndex)
        {
            return;
        }

        _children.RemoveAt(currentIndex);
        _children.Insert(childIndex, childNode);
    }

    public void Reparent(Node newParent, bool keepGlobalTransform = true)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(newParent);
        newParent.ThrowIfFreed();
        _ = keepGlobalTransform;

        if (_parent is null)
        {
            throw new InvalidOperationException("Node must already have a parent before Reparent can be used.");
        }

        if (ReferenceEquals(newParent, this) || IsAncestorOf(newParent))
        {
            throw new InvalidOperationException("A node cannot be reparented to itself or its descendant.");
        }

        var oldParent = _parent;
        oldParent.DetachChild(this, clearInvalidOwners: false);
        newParent.AddChild(this);
        ClearInvalidOwnersRecursive();
    }

    public void QueueFree()
    {
        ThrowIfFreed();

        if (_parent is null && _tree is not null)
        {
            throw new InvalidOperationException("The SceneTree root node cannot be queued for deletion.");
        }

        if (!MarkQueuedForDeletion())
        {
            return;
        }

        if (_tree is not null)
        {
            _tree.QueueDelete(this);
            return;
        }

        Free();
    }

    public void AddToGroup(string group, bool persistent = false)
    {
        ThrowIfFreed();
        var groupName = ValidateGroupName(group);

        if (_groups.TryGetValue(groupName, out var existingPersistent))
        {
            _groups[groupName] = existingPersistent || persistent;
            return;
        }

        _groups.Add(groupName, persistent);
    }

    public void RemoveFromGroup(string group)
    {
        ThrowIfFreed();
        var groupName = ValidateGroupName(group);
        _groups.Remove(groupName);
    }

    public bool IsInGroup(string group)
    {
        ThrowIfFreed();
        var groupName = ValidateGroupName(group);
        return _groups.ContainsKey(groupName);
    }

    public string[] GetGroups()
    {
        ThrowIfFreed();
        return _groups.Keys.OrderBy(group => group, StringComparer.Ordinal).ToArray();
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

    public Node GetNode(NodePath path)
    {
        var node = GetNodeOrNull(path);
        if (node is null)
        {
            throw new InvalidOperationException($"Node path '{path}' was not found.");
        }

        return node;
    }

    public Node? GetNodeOrNull(NodePath path)
    {
        ThrowIfFreed();
        if (path.IsEmpty())
        {
            return null;
        }

        var current = path.IsAbsolute() ? _tree?.Root : this;
        if (current is null)
        {
            return null;
        }

        var names = path.GetNodeNames();
        for (var index = 0; index < names.Length; index++)
        {
            var name = names[index];
            if (path.IsAbsolute() && index == 0 && string.Equals(name, current._name, StringComparison.Ordinal))
            {
                continue;
            }

            if (name == ".")
            {
                continue;
            }

            if (name == "..")
            {
                current = current._parent;
                if (current is null)
                {
                    return null;
                }

                continue;
            }

            current = current.GetDirectChildByName(name);
            if (current is null)
            {
                return null;
            }
        }

        return current;
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

        foreach (var child in _children.ToArray())
        {
            child.EnterTreeRecursive(tree);
        }
    }

    internal void ReadyRecursive()
    {
        if (!IsInstanceValid(this) || _tree is null)
        {
            return;
        }

        foreach (var child in _children.ToArray())
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
        if (!IsInstanceValid(this) || _tree is null)
        {
            return;
        }

        _tree?.InvokeUserCallback(this, nameof(_Process), () => _Process(delta));

        foreach (var child in _children.ToArray())
        {
            child.ProcessRecursive(delta);
        }
    }

    internal void PhysicsProcessRecursive(double delta)
    {
        if (!IsInstanceValid(this) || _tree is null)
        {
            return;
        }

        _tree?.InvokeUserCallback(this, nameof(_PhysicsProcess), () => _PhysicsProcess(delta));

        foreach (var child in _children.ToArray())
        {
            child.PhysicsProcessRecursive(delta);
        }
    }

    internal void InputRecursive(InputEvent inputEvent)
    {
        if (!IsInstanceValid(this) || _tree is null)
        {
            return;
        }

        _tree?.InvokeUserCallback(this, nameof(_Input), () => _Input(inputEvent));

        foreach (var child in _children.ToArray())
        {
            child.InputRecursive(inputEvent);
        }
    }

    internal void ExitTreeRecursive()
    {
        var tree = _tree;
        if (tree is null)
        {
            return;
        }

        foreach (var child in _children.ToArray())
        {
            child.ExitTreeRecursive();
        }

        tree?.InvokeUserCallback(this, nameof(_ExitTree), _ExitTree);
        _tree = null;
    }

    protected override void OnFree()
    {
        ExitTreeRecursive();

        if (_parent is not null)
        {
            _parent._children.Remove(this);
            _parent = null;
        }

        foreach (var child in _children.ToArray())
        {
            child.Free();
        }

        _children.Clear();
        _groups.Clear();
        _owner = null;
        ClearQueuedForDeletion();
        base.OnFree();
    }

    private void DetachChild(Node child, bool clearInvalidOwners)
    {
        if (!_children.Remove(child))
        {
            throw new InvalidOperationException("Node is not a child of this node.");
        }

        if (child._tree is not null)
        {
            child.ExitTreeRecursive();
        }

        child._parent = null;

        if (clearInvalidOwners)
        {
            child.ClearInvalidOwnersRecursive();
        }
    }

    private void SetOwner(Node? owner)
    {
        if (owner is not null)
        {
            owner.ThrowIfFreed();
            if (ReferenceEquals(owner, this) || !owner.IsAncestorOf(this))
            {
                throw new InvalidOperationException("Owner must be an ancestor of this node.");
            }
        }

        _owner = owner;
    }

    private void ClearInvalidOwnersRecursive()
    {
        if (_owner is not null && !_owner.IsAncestorOf(this))
        {
            _owner = null;
        }

        foreach (var child in _children)
        {
            child.ClearInvalidOwnersRecursive();
        }
    }

    private int NormalizeChildIndex(int index)
    {
        return index < 0 ? _children.Count + index : index;
    }

    private void MakeChildNameUnique(Node child)
    {
        var baseName = string.IsNullOrWhiteSpace(child._name) ? child.GetType().Name : child._name;
        var candidate = baseName;
        var suffix = 2;

        while (_children.Any(node => !ReferenceEquals(node, child) && node._name == candidate))
        {
            candidate = $"{baseName}{suffix}";
            suffix++;
        }

        child._name = candidate;
    }

    internal static string ValidateGroupName(string group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        return group;
    }

    internal bool IsGroupPersistent(string group)
    {
        ThrowIfFreed();
        var groupName = ValidateGroupName(group);
        return _groups.TryGetValue(groupName, out var persistent) && persistent;
    }

    internal void CollectNodesInGroup(string group, List<Node> nodes)
    {
        if (_groups.ContainsKey(group))
        {
            nodes.Add(this);
        }

        foreach (var child in _children)
        {
            child.CollectNodesInGroup(group, nodes);
        }
    }

    private Node? GetDirectChildByName(string name)
    {
        foreach (var child in _children)
        {
            if (string.Equals(child._name, name, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }
}
