namespace Electron2D;

public class Node
{
    private readonly List<Node> _children = [];

    public event Action<Node>? ChildAdded;
    public event Action<Node>? ChildRemoved;
    
    public Node(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Node name cannot be null or whitespace.", nameof(name));
        Name = name;
        
        Transform = new Transform(this);
    }

    private void InternalAwake()
    {
        Awake();
    }
    
    internal void InternalUpdate(float deltaTime)
    {
        foreach (var child in _children)
        {
            if (child.IsEnabled)
                child.InternalUpdate(deltaTime);
        }
        
        Update(deltaTime);
    }

    internal void InternalDestroy()
    {
        foreach (var child in _children)
        {
            child.InternalDestroy();
        }

        Destroy();
    }

    public void AddChild(Node child)
    {
        if (child.Parent != null)
            throw new InvalidOperationException("Node already has a parent.");
        if (_children.Any(c => c.Name == child.Name))
            throw new InvalidOperationException($"Child with name '{child.Name}' already exists.");

        _children.Add(child);
        child.Parent = this;
        child.Transform.MarkTransformDirty();
        child.InternalAwake();

        ChildAdded?.Invoke(child);
    }

    public bool RemoveChild(Node child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
            child.Transform.MarkTransformDirty();
            ChildRemoved?.Invoke(child);
            return true;
        }
        return false;
    }
    
    public bool RemoveChildByName(string name)
    {
        Node? child = null;
        foreach (var c in _children)
        {
            if (c.Name == name)
            {
                child = c;
                break;
            }
        }

        if (child != null)
            return RemoveChild(child);

        foreach (var c in _children)
        {
            if (c.RemoveChildByName(name))
                return true;
        }

        return false;
    }

    public List<Node> GetChildren() => _children;

    public Node? GetChildByName(string name)
    {
        foreach (var c in _children)
        {
            if (c.Name == name) return c;
        }

        return null;
    }
    
    public Node? FindNodeByName(string name)
    {
        if (Name == name)
            return this;

        foreach (var child in _children)
        {
            var found = child.FindNodeByName(name);
            if (found != null)
                return found;
        }
        return null;
    }
    
    public IEnumerable<Node> FindNodesByName(string name)
    {
        if (Name == name)
            yield return this;

        foreach (var child in _children)
        {
            foreach (var found in child.FindNodesByName(name))
                yield return found;
        }
    }
    
    public T? FindNodeOfType<T>() where T : Node
    {
        if (this is T typed)
            return typed;

        foreach (var child in _children)
        {
            var found = child.FindNodeOfType<T>();
            if (found != null)
                return found;
        }
        return null;
    }
    
    public IEnumerable<T> FindNodesOfType<T>() where T : Node
    {
        if (this is T typed)
            yield return typed;

        foreach (var child in _children)
        {
            foreach (var found in child.FindNodesOfType<T>())
                yield return found;
        }
    }

    protected virtual void Awake() { }

    protected virtual void Update(float deltaTime) { }
    
    public virtual void Destroy() { }
    
    public Transform Transform { get; }
    
    public Node? Parent { get; private set; }

    public bool IsEnabled { get; set; } = true;

    public string Name { get; }
    
    public bool IsStatic { get; set; } = false;
}