using System.Runtime.InteropServices;

namespace Electron2D;

public sealed class SceneTree
{
    private Node[] _freeQueue;
    private int _freeCount;
    private bool _inputHandled;
    private readonly GroupIndex _groups = new();
    private bool _cameraDirty;
    private bool _quitRequested;
    public bool QuitRequested => _quitRequested;
    
    public readonly Signal OnQuitRequested = new();
    public readonly Signal<uint> OnWindowCloseRequested = new();


    public SceneTree(Node root, int deferredFreeQueueCapacity  = 1024)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        if (deferredFreeQueueCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(deferredFreeQueueCapacity));

        _freeQueue = new Node[deferredFreeQueueCapacity];

        Root.InternalEnterTree(this);
        Root.InternalReady();
    }

    public Node Root { get; }

    public bool Paused { get; set; }
    
    public Camera? CurrentCamera { get; private set; }

    public ReadOnlySpan<Node> GetNodesInGroup(string group) => _groups.GetNodes(group);

    internal int RegisterInGroup(string group, Node node) => _groups.Add(group, node);

    internal void UnregisterFromGroup(string group, int index, Node removedNode)
        => _groups.Remove(group, index, removedNode);

    internal void QueueFree(Node node)
    {
        if ((uint)_freeCount >= (uint)_freeQueue.Length)
            throw new InvalidOperationException(
                "SceneTree deferred free queue overflow. Increase DeferredFreeQueueCapacity in EngineConfig.");


        _freeQueue[_freeCount++] = node;
    }

    public void FlushFreeQueue()
    {
        for (var i = 0; i < _freeCount; i++)
        {
            var n = _freeQueue[i];
            _freeQueue[i] = null!;
            n.InternalFreeImmediate();
        }
        _freeCount = 0;
    }
    
    public void DispatchInputEvents(ReadOnlySpan<InputEvent> events)
    {
        for (var i = 0; i < events.Length; i++)
        {
            _inputHandled = false;
            DispatchInput(Root, events[i]);
            if (_inputHandled) continue;

            DispatchShortcutInput(Root, events[i]);
            if (_inputHandled) continue;

            DispatchUnhandledInput(Root, events[i]);
            if (_inputHandled) continue;

            if (events[i].Type is InputEventType.KeyDown or InputEventType.KeyUp)
                DispatchUnhandledKeyInput(Root, events[i]);
        }
    }

    private bool DispatchInput(Node node, InputEvent ev)
    {
        node.InternalInput(ev);
        if (_inputHandled) return true;

        var count = node.GetChildCount();
        for (var i = 0; i < count; i++)
            if (DispatchInput(node.GetChild(i), ev)) return true;

        return false;
    }

    private bool DispatchShortcutInput(Node node, InputEvent ev)
    {
        node.InternalShortcutInput(ev);
        if (_inputHandled) return true;

        var count = node.GetChildCount();
        for (var i = 0; i < count; i++)
            if (DispatchShortcutInput(node.GetChild(i), ev)) return true;

        return false;
    }

    private bool DispatchUnhandledInput(Node node, InputEvent ev)
    {
        node.InternalUnhandledInput(ev);
        if (_inputHandled) return true;

        var count = node.GetChildCount();
        for (var i = 0; i < count; i++)
            if (DispatchUnhandledInput(node.GetChild(i), ev)) return true;

        return false;
    }

    private bool DispatchUnhandledKeyInput(Node node, InputEvent ev)
    {
        node.InternalUnhandledKeyInput(ev);
        if (_inputHandled) return true;

        var count = node.GetChildCount();
        for (var i = 0; i < count; i++)
            if (DispatchUnhandledKeyInput(node.GetChild(i), ev)) return true;

        return false;
    }
    
    public void Process(float delta)
        => ProcessNode(Root, parentMode: ProcessMode.Always, delta);

    public void PhysicsProcess(float fixedDelta)
        => PhysicsProcessNode(Root, parentMode: ProcessMode.Always, fixedDelta);

    private void ProcessNode(Node node, ProcessMode parentMode, float delta)
    {
        var mode = node.ProcessMode == ProcessMode.Inherit ? parentMode : node.ProcessMode;
        if (ShouldRun(mode))
            node.InternalProcess(delta);

        var count = node.GetChildCount();
        for (var i = 0; i < count; i++)
            ProcessNode(node.GetChild(i), mode, delta);
    }

    private void PhysicsProcessNode(Node node, ProcessMode parentMode, float fixedDelta)
    {
        var mode = node.ProcessMode == ProcessMode.Inherit ? parentMode : node.ProcessMode;
        if (ShouldRun(mode))
            node.InternalPhysicsProcess(fixedDelta);

        var count = node.GetChildCount();
        for (var i = 0; i < count; i++)
            PhysicsProcessNode(node.GetChild(i), mode, fixedDelta);
    }

    private bool ShouldRun(ProcessMode mode)
    {
        if (mode == ProcessMode.Disable) return false;
        if (mode == ProcessMode.Always) return true;
        if (mode == ProcessMode.Pausable) return !Paused;
        if (mode == ProcessMode.WhenPaused) return Paused;
        return true;
    }
    
    internal void RegisterCamera(Camera cam)
    {
        // Ничего тяжёлого. Если камер нет — ставим первую.
        if (CurrentCamera is null)
        {
            CurrentCamera = cam;
            _cameraDirty = false;
        }
    }

    internal void UnregisterCamera(Camera cam)
    {
        if (CurrentCamera == cam)
        {
            CurrentCamera = null;
            _cameraDirty = true; // выберем другую камеру позже (когда узел реально уйдёт из дерева)
        }
    }

    internal void SetCurrentCamera(Camera cam)
    {
        // Защита от “чужой” камеры
        if (!ReferenceEquals(cam.Tree, this)) return;

        CurrentCamera = cam;
        _cameraDirty = false;
    }

    internal Camera? EnsureCurrentCamera()
    {
        if (CurrentCamera is not null) return CurrentCamera;
        if (!_cameraDirty) return null;

        // Редкий случай: текущая камера удалена. Ищем первую доступную.
        CurrentCamera = FindFirstCamera(Root);
        _cameraDirty = false;
        return CurrentCamera;
    }

    private static Camera? FindFirstCamera(Node node)
    {
        if (node is Camera c) return c;

        var count = node.GetChildCount();
        for (var i = 0; i < count; i++)
        {
            var found = FindFirstCamera(node.GetChild(i));
            if (found is not null) return found;
        }

        return null;
    }

    internal void MarkInputHandled() => _inputHandled = true;
    
    public void Quit() => _quitRequested = true;
}
