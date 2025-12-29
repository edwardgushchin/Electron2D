using System.Runtime.InteropServices;

namespace Electron2D;

public sealed class SceneTree
{
    private readonly GroupIndex _groups = new();
    private Node[] _freeQueue;
    private int _freeCount;
    private bool _inputHandled;
    private bool _cameraDirty;

    public SceneTree(Node root, int deferredFreeQueueCapacity = 1024)
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

    public bool QuitRequested { get; private set; }

    public readonly Signal OnQuitRequested = new();

    public readonly Signal<uint> OnWindowCloseRequested = new();

    public ReadOnlySpan<Node> GetNodesInGroup(string group) => _groups.GetNodes(group);

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
            DispatchInputRecursive(Root, events[i], InputDispatchPhase.Input);
            if (_inputHandled) continue;

            DispatchInputRecursive(Root, events[i], InputDispatchPhase.ShortcutInput);
            if (_inputHandled) continue;

            DispatchInputRecursive(Root, events[i], InputDispatchPhase.UnhandledInput);
            if (_inputHandled) continue;

            if (events[i].Type is InputEventType.KeyDown or InputEventType.KeyUp)
                DispatchInputRecursive(Root, events[i], InputDispatchPhase.UnhandledKeyInput);
        }
    }

    public void Process(float delta)
        => ProcessNode(Root, parentMode: ProcessMode.Always, delta);

    public void PhysicsProcess(float fixedDelta)
        => PhysicsProcessNode(Root, parentMode: ProcessMode.Always, fixedDelta);

    public void Quit() => QuitRequested = true;

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

    internal void MarkInputHandled() => _inputHandled = true;

    private bool DispatchInputRecursive(Node node, InputEvent ev, InputDispatchPhase phase)
    {
        switch (phase)
        {
            case InputDispatchPhase.Input:
                node.InternalInput(ev);
                break;
            case InputDispatchPhase.ShortcutInput:
                node.InternalShortcutInput(ev);
                break;
            case InputDispatchPhase.UnhandledInput:
                node.InternalUnhandledInput(ev);
                break;
            case InputDispatchPhase.UnhandledKeyInput:
                node.InternalUnhandledKeyInput(ev);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown input dispatch phase.");
        }

        if (_inputHandled) return true;

        var count = node.GetChildCount();
        for (var i = 0; i < count; i++)
            if (DispatchInputRecursive(node.GetChild(i), ev, phase)) return true;

        return false;
    }

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

    private enum InputDispatchPhase
    {
        Input,
        ShortcutInput,
        UnhandledInput,
        UnhandledKeyInput
    }
}
