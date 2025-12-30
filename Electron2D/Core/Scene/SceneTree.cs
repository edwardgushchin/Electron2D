using System.Runtime.InteropServices;

namespace Electron2D;

public sealed class SceneTree
{
    private readonly GroupIndex _groups = new();
    private readonly List<SpriteRenderer> _spriteRenderers = new(capacity: 256);

    private Node[] _freeQueue;
    private int _freeCount;
    private bool _inputHandled;
    private bool _cameraDirty;

    internal ReadOnlySpan<SpriteRenderer> SpriteRenderers => CollectionsMarshal.AsSpan(_spriteRenderers);

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
    
    public Control? FocusedControl { get; private set; }

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
            var ev = events[i];
            _inputHandled = false;

            // 1) Node._input()
            DispatchInputRecursive(Root, ev, InputDispatchPhase.Input, parentMode: ProcessMode.Always);
            if (_inputHandled) continue;

            // 2) GUI phase (пока: всё в FocusedControl)
            DispatchGUI(ev);
            if (_inputHandled) continue;

            // 3) Node._shortcut_input() (только “shortcut eligible”)
            if (IsShortcutEligible(ev.Type))
            {
                DispatchInputRecursive(Root, ev, InputDispatchPhase.ShortcutInput, parentMode: ProcessMode.Always);
                if (_inputHandled) continue;
            }

            // 4) Node._unhandled_key_input() (только key)
            if (IsKeyEvent(ev.Type))
            {
                DispatchInputRecursive(Root, ev, InputDispatchPhase.UnhandledKeyInput, parentMode: ProcessMode.Always);
                if (_inputHandled) continue;
            }

            // 5) Node._unhandled_input()
            DispatchInputRecursive(Root, ev, InputDispatchPhase.UnhandledInput, parentMode: ProcessMode.Always);
        }
    }

    private static bool IsKeyEvent(InputEventType t)
        => t is InputEventType.KeyDown or InputEventType.KeyUp;

    private static bool IsShortcutEligible(InputEventType t)
        => t is InputEventType.KeyDown or InputEventType.KeyUp
            or InputEventType.GamepadButtonDown or InputEventType.GamepadButtonUp;
    
    public void Process(float delta)
        => ProcessNode(Root, parentMode: ProcessMode.Always, delta);

    public void PhysicsProcess(float fixedDelta)
        => PhysicsProcessNode(Root, parentMode: ProcessMode.Always, fixedDelta);

    public void Quit() => QuitRequested = true;

    internal int RegisterInGroup(string group, Node node) => _groups.Add(group, node);

    internal void UnregisterFromGroup(string group, int index, Node removedNode)
        => _groups.Remove(group, index, removedNode);

    internal void RegisterSpriteRenderer(SpriteRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        if (renderer.SceneIndex >= 0)
            return;

        renderer.SceneIndex = _spriteRenderers.Count;
        _spriteRenderers.Add(renderer);
    }

    internal void UnregisterSpriteRenderer(SpriteRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        var idx = renderer.SceneIndex;
        if (idx < 0)
            return;

        // Защита от рассинхронизации индекса (не hot-path)
        if ((uint)idx >= (uint)_spriteRenderers.Count || !ReferenceEquals(_spriteRenderers[idx], renderer))
        {
            idx = _spriteRenderers.IndexOf(renderer);
            if (idx < 0)
            {
                renderer.SceneIndex = -1;
                return;
            }
        }

        var lastIndex = _spriteRenderers.Count - 1;

        if (idx != lastIndex)
        {
            var moved = _spriteRenderers[lastIndex];
            _spriteRenderers[idx] = moved;
            moved.SceneIndex = idx;
        }

        _spriteRenderers.RemoveAt(lastIndex);
        renderer.SceneIndex = -1;
    }

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
        if (!ReferenceEquals(cam.SceneTree, this)) return;

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
    
    internal void SetFocusedControl(Control? control)
    {
        if (control is null)
        {
            FocusedControl = null;
            return;
        }

        // Фокусить можно только контрол, который реально в этом дереве
        if (!ReferenceEquals(control.SceneTree, this)) return;

        FocusedControl = control;
    }

    internal void UnfocusIf(Control control)
    {
        if (ReferenceEquals(FocusedControl, control))
            FocusedControl = null;
    }

    private bool DispatchInputRecursive(Node node, InputEvent ev, InputDispatchPhase phase, ProcessMode parentMode)
    {
        // effective mode
        var mode = node.ProcessMode == ProcessMode.Inherit ? parentMode : node.ProcessMode;

        // reverse depth-first: сначала дети (с конца), потом узел
        var count = node.ChildCount;
        for (var i = count - 1; i >= 0; i--)
        {
            if (DispatchInputRecursive(node.GetChild(i), ev, phase, mode))
                return true;
        }

        if (_inputHandled) return true;

        if (!ShouldRun(mode))
            return false;

        switch (phase)
        {
            case InputDispatchPhase.Input:
                node.InternalInput(ev);
                break;
            case InputDispatchPhase.ShortcutInput:
                node.InternalShortcutInput(ev);
                break;
            case InputDispatchPhase.UnhandledKeyInput:
                node.InternalUnhandledKeyInput(ev);
                break;
            case InputDispatchPhase.UnhandledInput:
                node.InternalUnhandledInput(ev);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown input dispatch phase.");
        }

        return _inputHandled;
    }

    private void ProcessNode(Node node, ProcessMode parentMode, float delta)
    {
        var mode = node.ProcessMode == ProcessMode.Inherit ? parentMode : node.ProcessMode;
        if (ShouldRun(mode))
            node.InternalProcess(delta);

        var count = node.ChildCount;
        for (var i = 0; i < count; i++)
            ProcessNode(node.GetChild(i), mode, delta);
    }

    private void PhysicsProcessNode(Node node, ProcessMode parentMode, float fixedDelta)
    {
        var mode = node.ProcessMode == ProcessMode.Inherit ? parentMode : node.ProcessMode;
        if (ShouldRun(mode))
            node.InternalPhysicsProcess(fixedDelta);

        var count = node.ChildCount;
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

        var count = node.ChildCount;
        for (var i = 0; i < count; i++)
        {
            var found = FindFirstCamera(node.GetChild(i));
            if (found is not null) return found;
        }

        return null;
    }
    
    private void DispatchGUI(InputEvent ev)
    {
        var fc = FocusedControl;
        if (fc is null) return;

        // Если фокус “протух” (контрол ушёл из дерева) — сбрасываем
        if (!ReferenceEquals(fc.SceneTree, this))
        {
            FocusedControl = null;
            return;
        }

        // Уважаем паузу/ProcessMode так же, как и для обычных узлов
        var mode = GetEffectiveProcessMode(fc);
        if (!ShouldRun(mode)) return;

        fc.InternalGUIInput(ev);
    }

    private static ProcessMode GetEffectiveProcessMode(Node node)
    {
        var mode = node.ProcessMode;
        while (mode == ProcessMode.Inherit)
        {
            var p = node.Parent;
            if (p is null) return ProcessMode.Always;
            node = p;
            mode = node.ProcessMode;
        }
        return mode;
    }
}
