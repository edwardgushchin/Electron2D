using System.Numerics;
using System.Runtime.InteropServices;

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
    private bool _destroyed;

    private bool _readyCalled;
    private bool _queuedForFree;
    private int _componentCount;

    private ProcessMode _processMode;
    private ProcessMode _effectiveProcessMode;

    private bool _processEnabled;
    private bool _physicsProcessEnabled;

    
    private Signal? _onReady;
    private Signal? _onExited;
    private Signal? _onExiting;
    private Signal? _onEntered;
    private Signal? _onChildOrderChanged;
    private Signal<Node>? _onChildExitingTree;
    private Signal<Node>? _onChildEnteredTree;
    private Signal? _onRenamed;

    #region Constructors
    public Node(string name)
    {
        _name = name;
        Transform = new Transform(this);

        _processMode = ProcessMode.Inherit;
        _effectiveProcessMode = ProcessMode.Always;

        // Godot-like: если метод переопределён — включаем по умолчанию.
        var flags = ProcessingOverrideCache.Get(GetType());
        _processEnabled = flags.ProcessOverridden;
        _physicsProcessEnabled = flags.PhysicsOverridden;
    }
    #endregion

    #region Signals
    /// <summary>
    /// Генерируется, когда дочерний узел вошёл в дерево сцены (обычно потому, что этот узел вошёл в дерево).
    /// </summary>
    public Signal<Node> OnChildEnteredTree => _onChildEnteredTree ??= new Signal<Node>();

    /// <summary>
    /// Генерируется, когда дочерний узел собирается покинуть дерево сцены
    /// (обычно потому, что этот узел выходит из дерева, или потому, что дочерний узел удаляется/освобождается).
    /// </summary>
    public Signal<Node> OnChildExitingTree => _onChildExitingTree ??= new Signal<Node>();

    /// <summary>
    /// Генерируется при изменении списка дочерних узлов (добавление/удаление/перемещение).
    /// </summary>
    public Signal OnChildOrderChanged => _onChildOrderChanged ??= new Signal();

    /// <summary>
    /// Генерируется после вызова <see cref="EnterTree"/> (узел вошёл в дерево).
    /// </summary>
    public Signal OnEntered => _onEntered ??= new Signal();

    /// <summary>
    /// Генерируется после вызова <see cref="ExitTree"/> (узел начал выход из дерева).
    /// </summary>
    public Signal OnExiting => _onExiting ??= new Signal();

    /// <summary>
    /// Генерируется после завершения выхода узла из дерева (после финализации выхода у поддерева).
    /// </summary>
    public Signal OnExited => _onExited ??= new Signal();

    /// <summary>
    /// Генерируется, когда узел считается готовым (после вызова <see cref="Ready"/>).
    /// </summary>
    public Signal OnReady => _onReady ??= new Signal();

    /// <summary>
    /// Генерируется при изменении имени узла, если узел находится внутри дерева.
    /// </summary>
    public Signal OnRenamed => _onRenamed ??= new Signal();
    #endregion

    #region Properties
    public Transform Transform { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name)
                return;

            _name = value;

            if (IsInsideTree)
                _onRenamed?.Emit();
        }
    }

    public Node? Parent => _parent;

    public bool IsInsideTree => _sceneTree is not null;

    public SceneTree? SceneTree => _sceneTree;

    public ProcessMode ProcessMode
    {
        get => _processMode;
        set
        {
            if (_processMode == value) return;
            _processMode = value;
            _sceneTree?.MarkProcessingListsDirty();
        }
    }

    public int ChildCount => _children.Count;

    public int GroupCount => _groups.Count;

    /// <summary>
    /// Компоненты узла без аллокаций. Возвращаемый span валиден до изменения списка компонентов.
    /// </summary>
    public ReadOnlySpan<IComponent> Components => _components.AsSpan(0, _componentCount);

    /// <summary>
    /// Дети узла без аллокаций. Возвращаемый span валиден до изменения списка детей.
    /// </summary>
    public ReadOnlySpan<Node> Children => CollectionsMarshal.AsSpan(_children);
    
    public bool IsProcessEnabled => _processEnabled;
    public bool IsPhysicsProcessEnabled => _physicsProcessEnabled;

    public void SetProcess(bool enabled)
    {
        if (_processEnabled == enabled) return;
        _processEnabled = enabled;
        _sceneTree?.MarkProcessingListsDirty();
    }

    public void SetPhysicsProcess(bool enabled)
    {
        if (_physicsProcessEnabled == enabled) return;
        _physicsProcessEnabled = enabled;
        _sceneTree?.MarkProcessingListsDirty();
    }
    
    internal bool ProcessEnabledInternal => _processEnabled;
    internal bool PhysicsProcessEnabledInternal => _physicsProcessEnabled;
    internal bool IsQueuedForFreeInternal => _queuedForFree;
    internal ProcessMode EffectiveProcessModeInternal => _effectiveProcessMode;
    #endregion

    #region Public API: hierarchy
    public void AddChild(Node child) => AddChild(child, keepWorldTransform: false);

    public void AddChild(Node child, bool keepWorldTransform)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child == this)
            throw new InvalidOperationException("Cannot add node to itself.");

        if (IsDescendantOf(child))
            throw new InvalidOperationException("Cannot create cycles.");

        // no-op: уже наш ребёнок
        if (child._parent == this)
            return;

        // Кэшируем world TRS если нужно сохранить мировое положение при репаренте.
        Vector2 worldPos = default;
        float worldRot = 0f;
        Vector2 worldScale = default;

        if (keepWorldTransform)
        {
            worldPos = child.Transform.WorldPosition;
            worldRot = child.Transform.WorldRotation;
            worldScale = child.Transform.WorldScale;
        }

        // Репарент внутри одного и того же SceneTree: НЕ вызываем Exit/Enter.
        if (_sceneTree is not null && ReferenceEquals(child._sceneTree, _sceneTree))
        {
            var oldParent = child._parent;
            if (oldParent is not null)
            {
                // удалить из oldParent._children без выхода из дерева
                var idx = oldParent._children.IndexOf(child);
                if (idx >= 0)
                    oldParent._children.RemoveAt(idx);

                oldParent._onChildOrderChanged?.Emit();
            }

            _children.Add(child);

            child._parent = this;
            child.Transform.SetParent(Transform);

            _onChildOrderChanged?.Emit();
            
            _sceneTree.MarkProcessingListsDirty();

            if (!keepWorldTransform)
                return;

            child.Transform.WorldPosition = worldPos;
            child.Transform.WorldRotation = worldRot;
            child.Transform.WorldScale = worldScale;

            return;
        }

        // Иначе — узел мигрирует между деревьями / из orphan → дерево:
        child._parent?.RemoveChild(child);

        _children.Add(child);

        child._parent = this;
        child.Transform.SetParent(Transform);

        if (keepWorldTransform)
        {
            child.Transform.WorldPosition = worldPos;
            child.Transform.WorldRotation = worldRot;
            child.Transform.WorldScale = worldScale;
        }

        _onChildOrderChanged?.Emit();

        if (_sceneTree is null)
            return;

        child.InternalEnterTree(_sceneTree);
        _onChildEnteredTree?.Emit(child);
        child.InternalReady();
    }

    public void RemoveChild(Node child)
    {
        ArgumentNullException.ThrowIfNull(child);

        var idx = _children.IndexOf(child);
        if (idx < 0)
            return;

        if (child._sceneTree is not null)
        {
            _onChildExitingTree?.Emit(child);
            child.InternalExitTree();
            child.InternalFinalizeExit();
        }

        _children.RemoveAt(idx);

        child._parent = null;
        child.Transform.SetParent(null);

        _onChildOrderChanged?.Emit();
    }

    public Node GetChild(int index) => _children[index];

    public int GetIndex()
    {
        if (_parent is null)
            return -1;

        return _parent._children.IndexOf(this);
    }
    #endregion

    #region Public API: components
    public T AddComponent<T>() where T : class, IComponent, new()
    {
        var component = new T();
        AddComponentInstance(component);
        return component;
    }
    #endregion

    #region Public API: node paths
    public Node GetNode(string path)
        => GetNodeOrNull(path) ?? throw new InvalidOperationException($"Node path not found: {path}");

    public Node? GetNodeOrNull(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return this;

        return TryGetNodeByPath(path.AsSpan(), out var node) ? node : null;
    }

    public bool HasNode(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return true;

        return TryGetNodeByPath(path.AsSpan(), out _);
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
            ReadOnlySpan<char> segment;

            if (slash >= 0)
            {
                segment = path[..slash];
                path = path[(slash + 1)..];

                if (segment.IsEmpty)
                    continue; // '//' => пропускаем
            }
            else
            {
                segment = path;
                path = ReadOnlySpan<char>.Empty;
            }

            if (IsDot(segment))
                continue;

            if (IsDotDot(segment))
            {
                if (node._parent is null)
                    return false;

                node = node._parent;
                continue;
            }

            var child = node.FindChildByName(segment);
            if (child is null)
                return false;

            node = child;
        }

        result = node;
        return true;

        static bool IsDotDot(ReadOnlySpan<char> s) => s.Length == 2 && s[0] == '.' && s[1] == '.';
        static bool IsDot(ReadOnlySpan<char> s) => s.Length == 1 && s[0] == '.';
    }
    #endregion

    #region Public API: groups
    public void AddToGroup(string group, bool persistent = false)
    {
        if (string.IsNullOrWhiteSpace(group))
            throw new ArgumentException("Group name cannot be empty.", nameof(group));

        for (var i = 0; i < _groups.Count; i++)
        {
            if (_groups[i].Name != group)
                continue;

            var entry = _groups[i];
            if (entry.Persistent == persistent)
                return;

            entry.Persistent = persistent;
            _groups[i] = entry;
            return;
        }

        var newEntry = new GroupEntry(group, persistent);

        if (_sceneTree is not null)
            newEntry.TreeIndex = _sceneTree.RegisterInGroup(group, this);

        _groups.Add(newEntry);
    }

    public string GetGroupName(int index) => _groups[index].Name;

    public bool IsGroupPersistent(int index) => _groups[index].Persistent;

    public void RemoveFromGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
            return;

        for (var i = 0; i < _groups.Count; i++)
        {
            if (_groups[i].Name != group)
                continue;

            var entry = _groups[i];

            if (_sceneTree is not null && entry.TreeIndex >= 0)
                _sceneTree.UnregisterFromGroup(group, entry.TreeIndex, this);

            _groups.RemoveAt(i);
            return;
        }
    }

    public bool IsInGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
            return false;

        for (var i = 0; i < _groups.Count; i++)
        {
            if (_groups[i].Name == group)
                return true;
        }

        return false;
    }
    #endregion

    #region Public API: lifetime
    public void QueueFree()
    {
        if (_sceneTree is not null && _parent is null)
            throw new InvalidOperationException("Cannot QueueFree the SceneTree root.");

        if (_queuedForFree)
            return;

        _queuedForFree = true;

        if (_sceneTree is not null)
            _sceneTree.QueueFree(this);
        else
            InternalFreeImmediate();
    }
    #endregion

    #region Protected API: Node lifecycle (override points)
    protected virtual void Destroy() { }

    protected virtual void EnterTree() { }

    protected virtual void Ready() { }

    protected virtual void Process(float delta) { }

    protected virtual void PhysicsProcess(float fixedDelta) { }

    protected virtual void ExitTree() { }
    #endregion

    #region Protected API: input (override points)
    /// <summary>
    /// Вызывается при возникновении события ввода.
    /// Событие распространяется вверх по дереву, пока не будет помечено как обработанное.
    /// </summary>
    protected virtual void HandleInput(InputEvent inputEvent) { }

    /// <summary>
    /// Вызывается для shortcut-ввода (до unhandled), если поддерживается пайплайном ввода.
    /// </summary>
    protected virtual void HandleShortcutInput(InputEvent inputEvent) { }

    /// <summary>
    /// Вызывается для необработанного ввода (после <see cref="HandleInput"/> и после UI).
    /// </summary>
    protected virtual void HandleUnhandledInput(InputEvent inputEvent) { }

    /// <summary>
    /// Вызывается для необработанных событий клавиатуры (опциональный быстрый путь).
    /// </summary>
    protected virtual void HandleUnhandledKeyInput(InputEvent inputEvent) { }

    protected void SetInputHandled() => _sceneTree?.MarkInputHandled();
    #endregion

    #region Internal API: SceneTree integration
    internal void InternalEnterTree(SceneTree tree)
    {
        _sceneTree = tree;
        tree.MarkProcessingListsDirty();

        // Регистрируем группы этого узла в SceneTree-индексе.
        for (var i = 0; i < _groups.Count; i++)
        {
            var entry = _groups[i];
            if (entry.TreeIndex >= 0)
                continue;

            entry.TreeIndex = tree.RegisterInGroup(entry.Name, this);
            _groups[i] = entry;
        }

        // Регистрируем render-компоненты, которые были добавлены ДО входа узла в дерево.
        for (var i = 0; i < _componentCount; i++)
        {
            if (_components[i] is SpriteRenderer sr)
                tree.RegisterSpriteRenderer(sr);
        }

        EnterTree();
        _onEntered?.Emit();

        // enter_tree: top-to-bottom
        for (var i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            child.InternalEnterTree(tree);

            // parent signal after child реально вошёл
            _onChildEnteredTree?.Emit(child);
        }
    }

    internal void InternalReady()
    {
        if (_readyCalled) return;

        for (var i = 0; i < _children.Count; i++)
            _children[i].InternalReady();

        Ready();

        _readyCalled = true;
        // emit only if signal created (см. Patch Set 5)
        _onReady?.Emit();

        // ВАЖНО: дети, добавленные во время Ready(), тоже должны получить Ready().
        for (var i = 0; i < _children.Count; i++)
            _children[i].InternalReady();
    }

    internal void InternalProcess(float delta) => Process(delta);

    internal void InternalPhysicsProcess(float fixedDelta) => PhysicsProcess(fixedDelta);

    internal void InternalExitTree()
    {
        // exit_tree: bottom-to-top
        for (var i = _children.Count - 1; i >= 0; i--)
        {
            var child = _children[i];

            _onChildExitingTree?.Emit(child);
            child.InternalExitTree();
        }

        ExitTree();
        _onExiting?.Emit();
    }

    internal void InternalInput(InputEvent inputEvent) => HandleInput(inputEvent);

    internal void InternalShortcutInput(InputEvent inputEvent) => HandleShortcutInput(inputEvent);

    internal void InternalUnhandledInput(InputEvent inputEvent) => HandleUnhandledInput(inputEvent);

    internal void InternalUnhandledKeyInput(InputEvent inputEvent) => HandleUnhandledKeyInput(inputEvent);

    internal void InternalFreeImmediate()
    {
        if (_destroyed) return;
        
        // Root нельзя освобождать напрямую.
        if (_parent is null && _sceneTree is not null)
            throw new InvalidOperationException("Cannot free the SceneTree root.");

        _parent?.RemoveChild(this);

        // После RemoveChild поддерево уже вне дерева: можно уничтожать ресурсы.
        DestroySubtreeBottomUp(this);
    }

    internal void InternalFinalizeExit()
    {
        var tree = _sceneTree;

        // Если UI-контрол уходил из дерева — сбросить фокус, чтобы не осталась “висячая” ссылка.
        if (tree is not null && this is Control c)
            tree.UnfocusIf(c);

        if (tree is not null)
        {
            // Снять render-компоненты из индекса, пока tree ещё доступен.
            for (var i = 0; i < _componentCount; i++)
            {
                if (_components[i] is SpriteRenderer sr)
                    tree.UnregisterSpriteRenderer(sr);
            }

            // Снять группы из индекса, пока tree ещё доступен.
            for (var i = 0; i < _groups.Count; i++)
            {
                var entry = _groups[i];
                if (entry.TreeIndex < 0)
                    continue;

                tree.UnregisterFromGroup(entry.Name, entry.TreeIndex, this);
                // TreeIndex будет сброшен через callback InternalUpdateGroupIndex(...).
            }
        }
        
        tree?.MarkProcessingListsDirty();

        _sceneTree = null;
        // _queuedForFree НЕ трогаем: если узел стоял в очереди на free — он должен остаться queued,
        // иначе возможен повторный QueueFree и double free.
        _readyCalled = false;

        for (var i = 0; i < _children.Count; i++)
            _children[i].InternalFinalizeExit();

        _onExited?.Emit();
    }

    internal void InternalUpdateGroupIndex(string group, int newIndex)
    {
        for (var i = 0; i < _groups.Count; i++)
        {
            if (_groups[i].Name != group)
                continue;

            var entry = _groups[i];
            entry.TreeIndex = newIndex;
            _groups[i] = entry;
            return;
        }
    }
    
    internal void SetEffectiveProcessModeInternal(ProcessMode mode) => _effectiveProcessMode = mode;
    #endregion

    #region Private helpers
    private void AddComponentInstance(IComponent component)
    {
        if ((uint)_componentCount >= (uint)_components.Length)
            Array.Resize(ref _components, _components.Length * 2); // подготовьте capacity при загрузке сцены

        _components[_componentCount++] = component;
        component.OnAttach(this);

        // Если узел уже в дереве — регистрируем renderable сразу.
        if (_sceneTree is not null && component is SpriteRenderer sr)
            _sceneTree.RegisterSpriteRenderer(sr);
    }

    private static void DestroySubtreeBottomUp(Node node)
    {
        if (node._destroyed) return;
        node._destroyed = true;

        var list = node._children;

        // Разрываем иерархию у детей, чтобы внешние ссылки на ноды не видели “мертвого родителя”.
        for (var i = 0; i < list.Count; i++)
        {
            var child = list[i];
            child._parent = null;
            child.Transform.SetParent(null);
            DestroySubtreeBottomUp(child);
        }

        node.DetachAllComponents();
        node.Destroy();

        node._queuedForFree = false;
        list.Clear();
        node._groups.Clear();
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
        {
            if (p == possibleAncestor)
                return true;
        }

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
    #endregion
}
