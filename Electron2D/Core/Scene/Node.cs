namespace Electron2D;

public class Node
{
    private const int InitialComponentCapacity = 4;

    private readonly List<Node> _children = [];
    private readonly List<GroupEntry> _groups = [];
    private IComponent[] _components = new IComponent[InitialComponentCapacity];

    private string _name;
    private Node? _parent;
    private SceneTree? _sceneTree;
    private bool _readyCalled;
    private bool _queuedForFree;
    private int _componentCount;

    public Node(string name)
    {
        _name = name;
        Transform = new Transform(this);
        ProcessMode = ProcessMode.Inherit;
    }

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

    public bool IsInsideTree => _sceneTree is not null;

    public SceneTree? SceneTree => _sceneTree;

    public ProcessMode ProcessMode { get; set; }
    
    public int ChildCount => _children.Count;

    public int GroupCount => _groups.Count;

    public ReadOnlySpan<IComponent> Components => _components.AsSpan(0, _componentCount);
    
    public IReadOnlyList<Node> Children => _children;

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
        if (_sceneTree is not null && ReferenceEquals(child._sceneTree, _sceneTree))
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

        if (_sceneTree is null)
            return;

        child.InternalEnterTree(_sceneTree);
        OnChildEnteredTree.Emit(child);
        child.InternalReady();
    }

    public void RemoveChild(Node child)
    {
        ArgumentNullException.ThrowIfNull(child);

        var idx = _children.IndexOf(child);
        if (idx < 0) return;

        if (child._sceneTree is not null)
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
        var component = new T();
        AddComponentInstance(component);
        return component;
    }

    public Node GetChildAt(int index) => _children[index];

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

        if (_sceneTree is not null)
            entry.TreeIndex = _sceneTree.RegisterInGroup(group, this);

        _groups.Add(entry);
    }

    public string GetGroupName(int index) => _groups[index].Name;

    public bool IsGroupPersistent(int index) => _groups[index].Persistent;

    public void RemoveFromGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group)) return;

        for (var i = 0; i < _groups.Count; i++)
        {
            if (_groups[i].Name != group) continue;

            var e = _groups[i];

            if (_sceneTree is not null && e.TreeIndex >= 0)
                _sceneTree.UnregisterFromGroup(group, e.TreeIndex, this);

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

        if (_sceneTree is null)
        {
            // вне дерева — освобождаем сразу
            InternalFreeImmediate();
            return;
        }

        _sceneTree.QueueFree(this);
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
            node = _sceneTree?.Root ?? this;
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

    public virtual void Destroy() { }

    protected virtual void EnterTree() { }

    protected virtual void Ready() { }

    protected virtual void Process(float delta) { }

    protected virtual void PhysicsProcess(float fixedDelta) { }

    protected virtual void ExitTree() { }

    /// <summary>
    /// Вызывается при возникновении события ввода. Событие ввода распространяется вверх по дереву узлов, пока какой-либо узел его не обработает.
    /// Вызывается только в том случае, если обработка ввода включена, что происходит автоматически, если этот метод переопределен, и может быть переключено с помощью set_process_input().
    /// Чтобы обработать событие ввода и предотвратить его дальнейшее распространение на другие узлы, можно вызвать Viewport.set_input_as_handled().
    /// Для игрового ввода обычно лучше подходят <see cref="HandleUnhandledInput"/> и <see cref="HandleUnhandledKeyInput"/>, поскольку они позволяют графическому интерфейсу пользователя перехватывать события первым.
    /// Примечание: Этот метод вызывается только в том случае, если узел присутствует в дереве сцены (т.е. если он не является «сиротским»).
    /// </summary>
    /// <param name="inputEvent"></param>
    protected virtual void HandleInput(InputEvent inputEvent) { }

    /// <summary>
    /// Вызывается, когда событие ввода не было обработано методом <see cref="HandleInput"/> или каким-либо элементом управления графического интерфейса. Вызывается после _shortcut_input() и после _unhandled_key_input(). Событие ввода распространяется вверх по дереву узлов, пока какой-либо узел его не обработает.
    /// Вызывается только в том случае, если включена обработка необработанного ввода, что происходит автоматически, если этот метод переопределен, и может быть переключено с помощью set_process_unhandled_input().
    /// Чтобы обработать событие ввода и остановить его дальнейшее распространение на другие узлы, можно вызвать Viewport.set_input_as_handled().
    /// Для игрового ввода этот метод обычно лучше подходит, чем <see cref="HandleInput"/>, поскольку события графического интерфейса требуют более высокого приоритета. Для сочетаний клавиш рекомендуется использовать _shortcut_input(), поскольку он вызывается перед этим методом. Наконец, для обработки событий клавиатуры рекомендуется использовать _unhandled_key_input() по соображениям производительности.
    /// Примечание: Этот метод вызывается только в том случае, если узел присутствует в дереве сцены (т.е. если он не является "сиротским" узлом).
    /// </summary>
    /// <param name="inputEvent"></param>
    protected virtual void HandleShortcutInput(InputEvent inputEvent) { }

    protected virtual void HandleUnhandledInput(InputEvent inputEvent) { }

    protected virtual void HandleUnhandledKeyInput(InputEvent inputEvent) { }

    protected void SetInputHandled()
    {
        _sceneTree?.MarkInputHandled();
    }

    internal void InternalEnterTree(SceneTree tree)
    {
        _sceneTree = tree;

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

    internal void InternalProcess(float delta) => Process(delta);

    internal void InternalPhysicsProcess(float fixedDelta) => PhysicsProcess(fixedDelta);

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

    internal void InternalInput(InputEvent inputEvent) => HandleInput(inputEvent);

    internal void InternalShortcutInput(InputEvent inputEvent) => HandleShortcutInput(inputEvent);

    internal void InternalUnhandledInput(InputEvent inputEvent) => HandleUnhandledInput(inputEvent);

    internal void InternalUnhandledKeyInput(InputEvent inputEvent) => HandleUnhandledKeyInput(inputEvent);

    internal void InternalFreeImmediate()
    {
        // root нельзя фришить так
        if (_parent is null && _sceneTree is not null)
            throw new InvalidOperationException("Cannot free the SceneTree root.");

        _parent?.RemoveChild(this);

        // после RemoveChild поддерево уже вне дерева: можно уничтожать ресурсы
        DestroySubtreeBottomUp(this);
    }

    internal void InternalFinalizeExit()
    {
        // Если UI-контрол уходил из дерева — сбросить фокус, чтобы не осталась “висячая” ссылка.
        if (_sceneTree is not null && this is Control c)
            _sceneTree.UnfocusIf(c);
        
        // Снять группы из индекса пока _tree ещё доступен
        if (_sceneTree is not null)
        {
            for (var i = 0; i < _groups.Count; i++)
            {
                var e = _groups[i];
                if (e.TreeIndex < 0) continue;
                _sceneTree.UnregisterFromGroup(e.Name, e.TreeIndex, this);
                // TreeIndex будет сброшен через callback InternalUpdateGroupIndex(...)
            }
        }

        _sceneTree = null;
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

    private void AddComponentInstance(IComponent component)
    {
        if ((uint)_componentCount >= (uint)_components.Length)
            Array.Resize(ref _components, _components.Length * 2); // подготовьте capacity при загрузке сцены
        _components[_componentCount++] = component;
        component.OnAttach(this);
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
        for (var i = 0; i < _componentCount; i++)
        {
            _components[i].OnDetach();
            _components[i] = null!;
        }
        _componentCount = 0;
    }

    private bool IsDescendantOf(Node possibleAncestor)
    {
        for (var p = _parent; p is not null; p = p._parent)
            if (p == possibleAncestor) return true;
        return false;
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
}
