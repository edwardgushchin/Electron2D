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
    private bool _isEnabled = true;
    private bool _isPaused = false;
    private bool _queuedForFree;
    private IComponent[] _components = new IComponent[4];
    private int _compCount;

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

    public int ChildCount => _children.Count;

    public int GroupCount => _groups.Count;
    
    internal ReadOnlySpan<IComponent> InternalComponents => _components.AsSpan(0, _compCount);
    
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
    
    #region Lifecycle methods
    
    #region EnterTree
    
    protected virtual void EnterTree() {}

    internal void InternalEnterTree(SceneTree tree)
    {
        _tree = tree;

        // Регистрируем группы этого узла в SceneTree-индексе
        for (var i = 0; i < _groups.Count; i++)
        {
            var e = _groups[i];
            if (e.TreeIndex >= 0) continue;
            e.TreeIndex = tree.RegisterInGroup(e.Name, this);
            _groups[i] = e;
        }

        EnterTree();
        OnEntered.Emit();

        // enter_tree: top-to-bottom
        for (var i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            child.InternalEnterTree(tree);

            // parent signal after child реально вошёл
            OnChildEnteredTree.Emit(child);
        }
    }

    
    #endregion

    #region Ready
    
    protected virtual void Ready() {}

    internal void InternalReady()
    {
        if (_readyCalled) return;

        // ready: post-order (children, then parent)
        for (var i = 0; i < _children.Count; i++)
            _children[i].InternalReady();

        Ready();
        _readyCalled = true;
        OnReady.Emit();
    }
    
    #endregion

    #region Process
    
    protected virtual void Process(float delta) {}

    internal void InternalProcess(float delta) => Process(delta);
    
    #endregion

    #region PhysicsProcess
    
    protected virtual void PhysicsProcess(float fixedDelta) {}

    internal void InternalPhysicsProcess(float fixedDelta) => PhysicsProcess(fixedDelta);
    
    #endregion

    #region ExitTree

    protected virtual void ExitTree() {}

    internal void InternalExitTree()
    {
        // exit_tree: bottom-to-top
        for (var i = _children.Count - 1; i >= 0; i--)
        {
            var child = _children[i];

            OnChildExitingTree.Emit(child);
            child.InternalExitTree();
        }

        ExitTree();
        OnExiting.Emit();
    }


    #endregion

    #region Input

    protected virtual void HandleInput(InputEvent inputEvent) {}
    
    internal void InternalInput(InputEvent inputEvent) => HandleInput(inputEvent);

    #endregion

    #region ShortcutInput

    protected virtual void HandleShortcutInput(InputEvent inputEvent) {}
    
    internal void InternalShortcutInput(InputEvent inputEvent) => HandleShortcutInput(inputEvent);

    #endregion

    #region UnhandledInput

    protected virtual void HandleUnhandledInput(InputEvent inputEvent) { }
    
    internal void InternalUnhandledInput(InputEvent inputEvent) => HandleUnhandledInput(inputEvent);

    #endregion

    #region UnhandledKeyInput

    protected virtual void HandleUnhandledKeyInput(InputEvent inputEvent) {}
    
    internal void InternalUnhandledKeyInput(InputEvent inputEvent) => HandleUnhandledKeyInput(inputEvent);

    #endregion
    
    protected void SetInputHandled()
    {
        _tree?.MarkInputHandled();
    }

    
    #endregion
    
    public void AddChild(Node child) => AddChild(child, keepWorldTransform: false);

    public void AddChild(Node child, bool keepWorldTransform)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (child == this) throw new InvalidOperationException("Cannot add node to itself.");
        if (IsDescendantOf(child)) throw new InvalidOperationException("Cannot create cycles.");
        
        // no-op: уже наш ребёнок
        if (child._parent == this) return;

        // кеш world если нужно
        var wp = default(System.Numerics.Vector2);
        var wr = 0f;
        var ws = default(System.Numerics.Vector2);
        if (keepWorldTransform)
        {
            wp = child.Transform.WorldPosition;
            wr = child.Transform.WorldRotation;
            ws = child.Transform.WorldScale;
        }

        // Репарент внутри одного и того же SceneTree: НЕ вызываем Exit/Enter
        if (_tree is not null && ReferenceEquals(child._tree, _tree))
        {
            var oldParent = child._parent;
            if (oldParent is not null)
            {
                // удалить из oldParent._children без выхода из дерева
                var idx = oldParent._children.IndexOf(child);
                if (idx >= 0)
                    oldParent._children.RemoveAt(idx);
                oldParent.OnChildOrderChanged.Emit();
            }

            _children.Add(child);
            child._parent = this;
            child.Transform.SetParent(Transform);
            OnChildOrderChanged.Emit();

            if (!keepWorldTransform) return;
            child.Transform.WorldPosition = wp;
            child.Transform.WorldRotation = wr;
            child.Transform.WorldScale = ws;

            return;
        }

        // Иначе — узел мигрирует между деревьями / из orphan → дерево:
        child._parent?.RemoveChild(child);

        _children.Add(child);

        child._parent = this;
        child.Transform.SetParent(Transform);

        if (keepWorldTransform)
        {
            child.Transform.WorldPosition = wp;
            child.Transform.WorldRotation = wr;
            child.Transform.WorldScale = ws;
        }

        OnChildOrderChanged.Emit();

        if (_tree is null)
            return;

        child.InternalEnterTree(_tree);
        OnChildEnteredTree.Emit(child);
        child.InternalReady();
    }

    public void RemoveChild(Node child)
    {
        ArgumentNullException.ThrowIfNull(child);

        var idx = _children.IndexOf(child);
        if (idx < 0) return;

        if (child._tree is not null)
        {
            OnChildExitingTree.Emit(child);
            child.InternalExitTree();
            child.InternalFinalizeExit();
        }

        _children.RemoveAt(idx);
        child._parent = null;
        child.Transform.SetParent(null);

        OnChildOrderChanged.Emit();
    }
    
    public T AddComponent<T>() where T : class, IComponent, new()
    {
        var c = new T();
        AddComponentInstance(c);
        return c;
    }

    private void AddComponentInstance(IComponent c)
    {
        if ((uint)_compCount >= (uint)_components.Length)
            Array.Resize(ref _components, _components.Length * 2); // подготовьте capacity при загрузке сцены
        _components[_compCount++] = c;
        c.OnAttach(this);
    }
    
    public Node GetChildAt(int index) => _children[index];

    [Obsolete("Use ChildCount property.")]
    public int GetChildCount() => ChildCount;

    [Obsolete("Use Parent property.")]
    public Node GetParent() => _parent ?? throw new InvalidOperationException("Node has no parent.");

    [Obsolete("Use GetChildAt(int) instead.")]
    public Node GetChild(int index) => GetChildAt(index);

    public int GetIndex()
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
        if (string.IsNullOrWhiteSpace(group))
            throw new ArgumentException("Group name cannot be empty.", nameof(group));

        for (var i = 0; i < _groups.Count; i++)
        {
            if (_groups[i].Name != group) continue;

            var e = _groups[i];
            if (e.Persistent == persistent) return;

            e.Persistent = persistent;
            _groups[i] = e;
            return;
        }

        var entry = new GroupEntry(group, persistent);

        if (_tree is not null)
            entry.TreeIndex = _tree.RegisterInGroup(group, this);

        _groups.Add(entry);
    }

    [Obsolete("Use GroupCount property.")]
    public int GetGroupCount() => GroupCount;

    public string GetGroupName(int index) => _groups[index].Name;

    public bool IsGroupPersistent(int index) => _groups[index].Persistent;
    
    public void RemoveFromGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group)) return;

        for (var i = 0; i < _groups.Count; i++)
        {
            if (_groups[i].Name != group) continue;

            var e = _groups[i];

            if (_tree is not null && e.TreeIndex >= 0)
                _tree.UnregisterFromGroup(group, e.TreeIndex, this);

            _groups.RemoveAt(i);
            return;
        }
    }

    public bool IsInGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group)) return false;
        for (var i = 0; i < _groups.Count; i++)
            if (_groups[i].Name == group) return true;
        return false;
    }
    
    public void QueueFree()
    {
        if (_queuedForFree) return;
        _queuedForFree = true;

        if (_tree is null)
        {
            // вне дерева — освобождаем сразу
            InternalFreeImmediate();
            return;
        }

        _tree.QueueFree(this);
    }
    
    internal void InternalFreeImmediate()
    {
        // root нельзя фришить так
        if (_parent is null && _tree is not null)
            throw new InvalidOperationException("Cannot free the SceneTree root.");

        _parent?.RemoveChild(this);

        // после RemoveChild поддерево уже вне дерева: можно уничтожать ресурсы
        DestroySubtreeBottomUp(this);
    }

    private static void DestroySubtreeBottomUp(Node node)
    {
        var list = node._children;
        for (var i = 0; i < list.Count; i++)
            DestroySubtreeBottomUp(list[i]);

        node.DetachAllComponents();
        node.Destroy();
    }

    private void DetachAllComponents()
    {
        for (var i = 0; i < _compCount; i++)
        {
            _components[i].OnDetach();
            _components[i] = null!;
        }
        _compCount = 0;
    }

    // Пользовательский “деструктор” узла под свои ресурсы (текстуры, аудио, хэндлы).
    // Вызывается ТОЛЬКО при QueueFree/Free, НЕ при простом RemoveChild.
    
    internal void InternalFinalizeExit()
    {
        // Снять группы из индекса пока _tree ещё доступен
        if (_tree is not null)
        {
            for (var i = 0; i < _groups.Count; i++)
            {
                var e = _groups[i];
                if (e.TreeIndex < 0) continue;
                _tree.UnregisterFromGroup(e.Name, e.TreeIndex, this);
                // TreeIndex будет сброшен через callback InternalUpdateGroupIndex(...)
            }
        }

        _tree = null;
        _queuedForFree = false;
        
        _readyCalled = false;

        for (var i = 0; i < _children.Count; i++)
            _children[i].InternalFinalizeExit();

        OnExited.Emit();
    }
    
    internal void InternalUpdateGroupIndex(string group, int newIndex)
    {
        for (var i = 0; i < _groups.Count; i++)
        {
            if (_groups[i].Name != group) continue;
            var e = _groups[i];
            e.TreeIndex = newIndex;
            _groups[i] = e;
            return;
        }
    }

    
    private bool IsDescendantOf(Node possibleAncestor)
    {
        for (var p = _parent; p is not null; p = p._parent)
            if (p == possibleAncestor) return true;
        return false;
    }

    public bool TryGetNodeByPath(ReadOnlySpan<char> path, out Node? result)
    {
        result = null;

        if (path.IsEmpty || IsDot(path))
        {
            result = this;
            return true;
        }

        var node = this;

        if (path[0] == '/')
        {
            node = _tree?.Root ?? this;
            path = path[1..];
        }

        while (!path.IsEmpty)
        {
            var slash = path.IndexOf('/');
            ReadOnlySpan<char> seg;

            if (slash >= 0)
            {
                seg = path[..slash];
                path = path[(slash + 1)..];
                if (seg.IsEmpty) continue; // '//' => пропускаем
            }
            else
            {
                seg = path;
                path = ReadOnlySpan<char>.Empty;
            }

            if (IsDot(seg)) continue;

            if (IsDotDot(seg))
            {
                if (node._parent is null) return false;
                node = node._parent;
                continue;
            }

            var child = node.FindChildByName(seg);
            if (child is null) return false;
            node = child;
        }

        result = node;
        return true;

        static bool IsDotDot(ReadOnlySpan<char> s) => s.Length == 2 && s[0] == '.' && s[1] == '.';
        static bool IsDot(ReadOnlySpan<char> s) => s.Length == 1 && s[0] == '.';
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
    
    public virtual void Destroy() { }
}
