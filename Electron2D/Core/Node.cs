namespace Electron2D;

public class Node
{
    #region Varibles

    private string _name;
    private Node? _parent;
    private readonly List<Node> _children = [];
    private readonly List<GroupEntry> _groups = [];
    private SceneTree? _tree;
    private bool _readyCalled;

    #endregion
    
    public Node(string name)
    {
        _name = name;
        Transform = new Transform(this);
        ProcessMode = ProcessMode.Inherit;
    }

    #region Properties
    
    public Transform Transform { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            if (IsInsideTree) OnRenamed.Emit();
        }
    }
    
    public Node? Parent => _parent;
    
    public bool IsInsideTree => _tree is not null;
    
    public SceneTree? Tree => _tree;
    
    public ProcessMode ProcessMode { get; set; }
    
    #endregion

    #region Signals
    
    /// <summary>
    /// Этот сигнал генерируется, когда дочерний узел входит в дерево сцены, обычно потому, что этот узел вошел в дерево
    /// </summary>
    public readonly Signal<Node> OnChildEnteredTree = new();
    
    /// <summary>
    /// тот сигнал генерируется, когда дочерний узел собирается покинуть дерево сцен, обычно потому, что этот узел выходит из дерева, или потому, что дочерний узел удаляется или освобождается.
    /// </summary>
    public readonly Signal<Node> OnChildExitingTree = new();
    
    /// <summary>
    /// Сообщения генерируются при изменении списка дочерних узлов. Это происходит при добавлении, перемещении или удалении дочерних узлов.
    /// </summary>
    public readonly Signal OnChildOrderChanged = new();
    
    /// <summary>
    /// 
    /// </summary>
    public readonly Signal OnEntered = new();
    
    /// <summary>
    /// 
    /// </summary>
    public readonly Signal OnExiting = new();
    
    /// <summary>
    /// 
    /// </summary>
    public readonly Signal OnExited = new();
    
    /// <summary>
    /// Генерируется, когда узел считается готовым, после вызова функции <see cref="Ready"/>.
    /// </summary>
    public readonly Signal OnReady = new();
    
    /// <summary>
    /// Сообщение генерируется при изменении имени узла, если узел находится внутри дерева.
    /// </summary>
    public readonly Signal OnRenamed = new();
    
    #endregion

    #region EnterTree
    
    protected virtual void EnterTree() {}

    internal void InternalEnterTree(SceneTree tree)
    {
        _tree = tree;

        // enter_tree: top-to-bottom :contentReference[oaicite:18]{index=18}
        EnterTree();
        OnEntered.Emit(); // после EnterTree :contentReference[oaicite:19]{index=19}

        // У родителя child_entered_tree должен быть после enter_tree+tree_entered ребёнка :contentReference[oaicite:20]{index=20}
        _parent?.OnChildEnteredTree.Emit(this);

        // Рекурсивно в детей
        for (var i = 0; i < _children.Count; i++)
            _children[i].InternalEnterTree(tree);
    }
    
    #endregion

    #region Ready
    
    protected virtual void Ready() {}

    internal void InternalReady()
    {
        OnReady.Emit();
        Ready();
    }
    
    #endregion

    #region Process
    
    protected virtual void Process(float delta) {}

    internal void InternalProcess(float delta) => Process(delta);
    
    #endregion

    #region PhysicsProcess
    
    protected virtual void PhysicsProcess(float fixedDelta)
    {
        throw new NotImplementedException();
    }

    internal void InternalPhysicsProcess(float fixedDelta) => PhysicsProcess(fixedDelta);
    
    #endregion

    #region ExitTree

    protected virtual void ExitTree() {}

    internal void InternalExitTree()
    {
        // exit_tree: bottom-to-top :contentReference[oaicite:23]{index=23}
        for (var i = _children.Count - 1; i >= 0; i--)
            _children[i].InternalExitTree();

        ExitTree();

        // tree_exiting: после _exit_tree :contentReference[oaicite:24]{index=24}
        OnExiting.Emit();

        // У родителя child_exiting_tree после tree_exiting ребёнка :contentReference[oaicite:25]{index=25}
        _parent?.OnChildExitingTree.Emit(this);
    }

    #endregion

    #region Input

    protected virtual void Input(InputEvent inputEvent)
    {
        throw new NotImplementedException();
    }
    
    internal void InternalInput(InputEvent inputEvent) => Input(inputEvent);

    #endregion

    #region ShortcutInput

    protected virtual void ShortcutInput(InputEvent inputEvent)
    {
        throw new NotImplementedException();
    }
    
    internal void InternalShortcutInput(InputEvent inputEvent) => ShortcutInput(inputEvent);

    #endregion

    #region UnhandledInput

    protected virtual void UnhandledInput(InputEvent inputEvent)
    {
        throw new NotImplementedException();
    }
    
    internal void InternalUnhandledInput(InputEvent inputEvent) => UnhandledInput(inputEvent);

    #endregion

    #region UnhandledKeyInput

    protected virtual void UnhandledKeyInput(InputEvent inputEvent)
    {
        throw new NotImplementedException();
    }
    
    internal  void InternalUnhandledKeyInput(InputEvent inputEvent) => UnhandledKeyInput(inputEvent);

    #endregion
    
    public void AddChild(Node child)
    {
        if (child is null) throw new ArgumentNullException(nameof(child));
        if (child == this) throw new InvalidOperationException("Cannot add node to itself.");
        if (IsDescendantOf(child)) throw new InvalidOperationException("Cannot create cycles.");

        // Отцепить от старого родителя (как в большинстве движков)
        child._parent?.RemoveChild(child);

        _children.Add(child);
        child._parent = this;
        child.Transform.SetParent(Transform);

        OnChildOrderChanged.Emit(); // список детей изменился :contentReference[oaicite:15]{index=15}

        // Если родитель уже в дереве — ребёнок входит в дерево и получает enter/ready
        if (_tree is null) return;
        child.InternalEnterTree(_tree);
        child.InternalReadyIfNeeded();
    }

    public void RemoveChild(Node child)
    {
        if (child is null) throw new ArgumentNullException(nameof(child));
        var idx = _children.IndexOf(child);
        if (idx < 0) return;

        // Если ребёнок в дереве — сначала корректный выход поддерева
        if (child._tree is not null)
        {
            child.InternalExitTree();        // _exit_tree bottom-up :contentReference[oaicite:16]{index=16}
            child.InternalFinalizeExit();    // “уже вышел” + очистка
        }

        _children.RemoveAt(idx);
        child._parent = null;
        child.Transform.SetParent(null);

        OnChildOrderChanged.Emit(); // список детей изменился :contentReference[oaicite:17]{index=17}
    }
    
    public Node GetChild(int index) => _children[index];

    public int GetChildCount() => _children.Count;

    public Node GetParent() => _parent ?? throw new InvalidOperationException("Node has no parent.");

    public int GetIntex()
    {
        if (_parent is null) return -1;
        return _parent._children.IndexOf(this);
    }

    public Node GetNode(string path)
    {
        return GetNodeOrNull(path) ?? throw new InvalidOperationException($"Node path not found: {path}");
    }

    public Node? GetNodeOrNull(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return this;
        return TryGetNodeByPath(path.AsSpan(), out var node) ? node : null;
    }

    public bool HasNode(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        return TryGetNodeByPath(path.AsSpan(), out _);
    }

    public void AddToGroup(string group, bool persistent = false)
    {
        if (string.IsNullOrWhiteSpace(group)) throw new ArgumentException("Group name cannot be empty.", nameof(group));
        for (var i = 0; i < _groups.Count; i++)
        {
            if (_groups[i].Name != group) continue;
            if (_groups[i].Persistent == persistent) return;
            _groups[i] = new GroupEntry(group, persistent);
            return;
        }

        _groups.Add(new GroupEntry(group, persistent));
    }

    public string[] GetGroups()
    {
        if (_groups.Count == 0) return [];
        var result = new string[_groups.Count];
        for (var i = 0; i < _groups.Count; i++)
            result[i] = _groups[i].Name;
        return result;
    }

    public bool IsInGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group)) return false;
        for (var i = 0; i < _groups.Count; i++)
            if (_groups[i].Name == group) return true;
        return false;
    }
    
    internal void InternalReadyIfNeeded()
    {
        if (_readyCalled) return;

        // ready: post-order (дети, потом родитель) :contentReference[oaicite:21]{index=21}
        for (var i = 0; i < _children.Count; i++)
            _children[i].InternalReadyIfNeeded();

        Ready();
        _readyCalled = true;
        OnReady.Emit(); // после Ready :contentReference[oaicite:22]{index=22}
    }

    
    internal void InternalFinalizeExit()
    {
        // Очистить tree у поддерева
        var tree = _tree;
        _tree = null;

        for (var i = 0; i < _children.Count; i++)
            _children[i].InternalFinalizeExit();

        // tree_exited: “уже вышел и не активен” :contentReference[oaicite:26]{index=26}
        OnExited.Emit();
    }
    
    private bool IsDescendantOf(Node possibleAncestor)
    {
        for (var p = _parent; p is not null; p = p._parent)
            if (p == possibleAncestor) return true;
        return false;
    }
    
    internal void MarkTransformDirtyFromSelf()
    {
        for (var i = 0; i < _children.Count; i++)
            _children[i].MarkTransformDirtyRecursive();
    }

    internal void MarkTransformDirtyFromParent()
    {
        for (var i = 0; i < _children.Count; i++)
            _children[i].MarkTransformDirtyRecursive();
    }

    private void MarkTransformDirtyRecursive()
    {
        Transform.MarkWorldDirtyFromParent();
        for (var i = 0; i < _children.Count; i++)
            _children[i].MarkTransformDirtyRecursive();
    }

    private bool TryGetNodeByPath(ReadOnlySpan<char> path, out Node node)
    {
        node = this;
        if (path.IsEmpty || path.SequenceEqual(".")) return true;

        var index = 0;
        if (path[0] == '/')
        {
            if (_tree is null) return false;
            node = _tree.Root;
            index = 1;
        }

        while (index < path.Length)
        {
            var next = path.Slice(index).IndexOf('/');
            ReadOnlySpan<char> segment;
            if (next < 0)
            {
                segment = path.Slice(index);
                index = path.Length;
            }
            else
            {
                segment = path.Slice(index, next);
                index += next + 1;
            }

            if (segment.IsEmpty || segment is ".")
                continue;

            if (segment is "..")
            {
                if (node._parent is null) return false;
                node = node._parent;
                continue;
            }

            var child = node.FindChildByName(segment);
            if (child is null) return false;
            node = child;
        }

        return true;
    }

    private Node? FindChildByName(ReadOnlySpan<char> name)
    {
        for (var i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (name.SequenceEqual(child._name))
                return child;
        }
        return null;
    }

    private readonly record struct GroupEntry(string Name, bool Persistent);
}