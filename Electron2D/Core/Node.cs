namespace Electron2D;

public class Node
{
    private string _name;
    private Node? _parent;
    private readonly List<Node> _children = new();
    private SceneTree? _tree;
    private bool _readyCalled;
    
    
    public Node(string name)
    {
        _name = name;
        Transform = new Transform();
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
    
    public bool IsInsideTree => _tree is not null;
    
    public SceneTree? Tree => _tree;
    
    public ProcessMode ProcessMode { get; }
    
    /// <summary>
    /// Этот сигнал генерируется, когда дочерний узел входит в дерево сцены, обычно потому, что этот узел вошел в дерево
    /// </summary>
    public readonly  Signal<Node> OnChildEnteredTree = new();
    
    /// <summary>
    /// тот сигнал генерируется, когда дочерний узел собирается покинуть дерево сцен, обычно потому, что этот узел выходит из дерева, или потому, что дочерний узел удаляется или освобождается.
    /// </summary>
    public readonly  Signal<Node> OnChildExitingTree = new();
    
    /// <summary>
    /// Сообщения генерируются при изменении списка дочерних узлов. Это происходит при добавлении, перемещении или удалении дочерних узлов.
    /// </summary>
    public readonly  Signal OnChildOrderChanged = new();
    
    /// <summary>
    /// 
    /// </summary>
    public readonly  Signal OnEntered = new();
    
    /// <summary>
    /// 
    /// </summary>
    public readonly  Signal OnExiting = new();
    
    /// <summary>
    /// 
    /// </summary>
    public readonly  Signal OnExited = new();
    
    /// <summary>
    /// Генерируется, когда узел считается готовым, после вызова функции <see cref="Ready"/>.
    /// </summary>
    public readonly  Signal OnReady = new();
    
    /// <summary>
    /// Сообщение генерируется при изменении имени узла, если узел находится внутри дерева.
    /// </summary>
    public readonly  Signal OnRenamed = new();
    
    
    protected virtual void EnterTree()
    {
        throw new NotImplementedException();
    }
    
    protected virtual void Ready()
    {
        throw new NotImplementedException();
    }

    protected virtual void Process(float delta)
    {
        throw new NotImplementedException();
    }
    
    protected virtual void PhysicsProcess(float fixedDelta)
    {
        throw new NotImplementedException();
    }
    
    protected virtual void ExitTree()
    {
        throw new NotImplementedException();
    }
    
    protected virtual void Input(InputEvent inputEvent)
    {
        throw new NotImplementedException();
    }

    protected virtual void ShortcutInput(InputEvent inputEvent)
    {
        throw new NotImplementedException();
    }

    protected virtual void UnhandledInput(InputEvent inputEvent)
    {
        throw new NotImplementedException();
    }

    protected virtual void UnhandledKeyInput(InputEvent inputEvent)
    {
        throw new NotImplementedException();
    }
    
    public void AddChild(Node child)
    {
        if (child is null) throw new ArgumentNullException(nameof(child));
        if (child == this) throw new InvalidOperationException("Cannot add node to itself.");
        if (IsDescendantOf(child)) throw new InvalidOperationException("Cannot create cycles.");

        // Отцепить от старого родителя (как в большинстве движков)
        child._parent?.RemoveChild(child);

        _children.Add(child);
        child._parent = this;

        OnChildOrderChanged.Emit(); // список детей изменился :contentReference[oaicite:15]{index=15}

        // Если родитель уже в дереве — ребёнок входит в дерево и получает enter/ready
        if (_tree is not null)
        {
            child.InternalEnterTree(_tree);
            child.InternalReadyIfNeeded();
        }
    }

    public void RemoveChild(Node child)
    {
        if (child is null) throw new ArgumentNullException(nameof(child));
        int idx = _children.IndexOf(child);
        if (idx < 0) return;

        // Если ребёнок в дереве — сначала корректный выход поддерева
        if (child._tree is not null)
        {
            child.InternalExitTree();        // _exit_tree bottom-up :contentReference[oaicite:16]{index=16}
            child.InternalFinalizeExit();    // “уже вышел” + очистка
        }

        _children.RemoveAt(idx);
        child._parent = null;

        OnChildOrderChanged.Emit(); // список детей изменился :contentReference[oaicite:17]{index=17}
    }
    
    public Node GetChild(int index) => _children[index];

    public int GetChildCount() => _children.Count;

    public Node GetParent()
    {
        throw new NotImplementedException();
    }

    public int GetIntex()
    {
        throw new NotImplementedException();
    }

    public Node GetNode(string path)
    {
        throw new NotImplementedException();
    }

    public Node? GetNodeOrNull(string path)
    {
        throw new NotImplementedException();
    }

    public bool HasNode(string path)
    {
        throw new NotImplementedException();
    }

    public void AddToGroup(string group, bool persistent = false)
    {
        throw new NotImplementedException();
    }

    public string[] GetGroups()
    {
        throw new NotImplementedException();
    }

    public bool IsInGroup(string group)
    {
        throw new NotImplementedException();
    }
    
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
}