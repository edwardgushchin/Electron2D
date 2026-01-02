using System.Runtime.InteropServices;

namespace Electron2D;

public sealed class SceneTree
{
    #region Constants
    private const int DefaultDeferredFreeQueueCapacity = 1024;
    #endregion

    #region Instance fields
    private readonly GroupIndex _groupIndex = new();
    private readonly List<SpriteRenderer> _spriteRenderers = new(capacity: 256);
    private readonly List<Node> _processNodes = new(capacity: 256);
    private readonly List<Node> _physicsNodes = new(capacity: 256);
    private readonly Node[] _freeQueue;
    
    private int _freeCount;
    private bool _inputHandled;
    private bool _cameraDirty;
    private bool _processingListsDirty = true;
    #endregion

    #region Constructors
    public SceneTree(Node root, int deferredFreeQueueCapacity = DefaultDeferredFreeQueueCapacity)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));

        if (deferredFreeQueueCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(deferredFreeQueueCapacity));

        _freeQueue = new Node[deferredFreeQueueCapacity];

        Root.InternalEnterTree(this);
        Root.InternalReady();
    }
    #endregion

    #region Events
    public readonly Signal OnQuitRequested = new();
    public readonly Signal<uint> OnWindowCloseRequested = new();
    #endregion

    #region Properties
    public Node Root { get; }

    public bool Paused { get; set; }

    public Camera? CurrentCamera { get; private set; }

    public bool QuitRequested { get; private set; }

    public Control? FocusedControl { get; private set; }

    public Color ClearColor { get; set; } = new(0x000000FF);

    internal ReadOnlySpan<SpriteRenderer> SpriteRenderers => CollectionsMarshal.AsSpan(_spriteRenderers);
    #endregion

    #region Public API
    public ReadOnlySpan<Node> GetNodesInGroup(string group) => _groupIndex.GetNodes(group);

    internal void FlushFreeQueue()
    {
        for (var i = 0; i < _freeCount; i++)
        {
            var node = _freeQueue[i];
            _freeQueue[i] = null!;
            node.InternalFreeImmediate();
        }

        _freeCount = 0;
    }

    internal void DispatchInputEvents(ReadOnlySpan<InputEvent> events)
    {
        for (var i = 0; i < events.Length; i++)
        {
            var ev = events[i];
            _inputHandled = false;

            // 1) Node._input()
            DispatchInputRecursive(Root, ev, InputDispatchPhase.Input, parentMode: ProcessMode.Always);
            if (_inputHandled)
                continue;

            // 2) GUI phase (пока: всё в FocusedControl)
            DispatchGuiInput(ev);
            if (_inputHandled)
                continue;

            // 3) Node._shortcut_input() (только “shortcut eligible”)
            if (IsShortcutEligible(ev.Type))
            {
                DispatchInputRecursive(Root, ev, InputDispatchPhase.ShortcutInput, parentMode: ProcessMode.Always);
                if (_inputHandled)
                    continue;
            }

            // 4) Node._unhandled_key_input() (только key)
            if (IsKeyEvent(ev.Type))
            {
                DispatchInputRecursive(Root, ev, InputDispatchPhase.UnhandledKeyInput, parentMode: ProcessMode.Always);
                if (_inputHandled)
                    continue;
            }

            // 5) Node._unhandled_input()
            DispatchInputRecursive(Root, ev, InputDispatchPhase.UnhandledInput, parentMode: ProcessMode.Always);
        }
    }

    internal void Process(float delta)
    {
        EnsureProcessingLists();

        for (var i = 0; i < _processNodes.Count; i++)
        {
            var node = _processNodes[i];

            // Нода могла выйти из дерева/очередь на free/disable после rebuild.
            if (!ReferenceEquals(node.SceneTree, this)) continue;
            if (node.IsQueuedForFreeInternal) continue;
            if (!node.ProcessEnabledInternal) continue;

            if (ShouldRun(node.EffectiveProcessModeInternal))
                node.InternalProcess(delta);
        }
    }

    internal void PhysicsProcess(float fixedDelta)
    {
        EnsureProcessingLists();

        for (var i = 0; i < _physicsNodes.Count; i++)
        {
            var node = _physicsNodes[i];

            if (!ReferenceEquals(node.SceneTree, this)) continue;
            if (node.IsQueuedForFreeInternal) continue;
            if (!node.PhysicsProcessEnabledInternal) continue;

            if (ShouldRun(node.EffectiveProcessModeInternal))
                node.InternalPhysicsProcess(fixedDelta);
        }
    }

    public void Quit() => QuitRequested = true;
    #endregion

    #region Internal helpers
    internal int RegisterInGroup(string group, Node node) => _groupIndex.Add(group, node);

    internal void UnregisterFromGroup(string group, int index, Node removedNode)
        => _groupIndex.Remove(group, index, removedNode);

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

        // Защита от рассинхронизации индекса (не hot-path).
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
        // Если камер нет — ставим первую текущей (и синхронизируем флаги).
        if (CurrentCamera is null)
            SetCurrentCamera(cam);
    }

    internal void UnregisterCamera(Camera cam)
    {
        // Камера уходит из дерева — не может оставаться current.
        cam.SetCurrentFromTree(false);

        if (!ReferenceEquals(CurrentCamera, cam))
            return;

        CurrentCamera = null;
        _cameraDirty = true; // выберем другую камеру позже
    }

    internal void SetCurrentCamera(Camera cam)
    {
        // Защита от “чужой” камеры.
        if (!ReferenceEquals(cam.SceneTree, this))
            return;

        if (ReferenceEquals(CurrentCamera, cam))
        {
            cam.SetCurrentFromTree(true);
            _cameraDirty = false;
            return;
        }

        var prev = CurrentCamera;
        prev?.SetCurrentFromTree(false);

        CurrentCamera = cam;
        cam.SetCurrentFromTree(true);
        _cameraDirty = false;
    }

    internal Camera? EnsureCurrentCamera()
    {
        if (CurrentCamera is not null)
            return CurrentCamera;

        if (!_cameraDirty)
            return null;

        // Редкий случай: текущая камера удалена. Ищем первую доступную.
        var found = FindFirstCamera(Root);
        _cameraDirty = false;

        if (found is not null)
            SetCurrentCamera(found);
        else
            CurrentCamera = null;

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

        // Фокусить можно только контрол, который реально в этом дереве.
        if (!ReferenceEquals(control.SceneTree, this))
            return;

        FocusedControl = control;
    }

    internal void UnfocusIf(Control control)
    {
        if (ReferenceEquals(FocusedControl, control))
            FocusedControl = null;
    }
    
    internal void MarkProcessingListsDirty() => _processingListsDirty = true;
    #endregion

    #region Private helpers
    private bool DispatchInputRecursive(Node node, InputEvent ev, InputDispatchPhase phase, ProcessMode parentMode)
    {
        // Effective mode.
        var mode = node.ProcessMode == ProcessMode.Inherit ? parentMode : node.ProcessMode;

        // Reverse depth-first: сначала дети (с конца), потом узел.
        var count = node.ChildCount;
        for (var i = count - 1; i >= 0; i--)
        {
            if (DispatchInputRecursive(node.GetChild(i), ev, phase, mode))
                return true;
        }

        if (_inputHandled)
            return true;

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
    
    private void EnsureProcessingLists()
    {
        if (!_processingListsDirty)
            return;

        _processingListsDirty = false;

        _processNodes.Clear();
        _physicsNodes.Clear();

        BuildProcessingLists(Root, ProcessMode.Always);
    }

    private void BuildProcessingLists(Node node, ProcessMode parentMode)
    {
        var mode = node.ProcessMode == ProcessMode.Inherit ? parentMode : node.ProcessMode;
        node.SetEffectiveProcessModeInternal(mode);

        if (mode != ProcessMode.Disabled)
        {
            if (node.ProcessEnabledInternal)
                _processNodes.Add(node);

            if (node.PhysicsProcessEnabledInternal)
                _physicsNodes.Add(node);
        }

        var count = node.ChildCount;
        for (var i = 0; i < count; i++)
            BuildProcessingLists(node.GetChild(i), mode);
    }

    private bool ShouldRun(ProcessMode mode)
    {
        return mode switch
        {
            ProcessMode.Disabled => false,
            ProcessMode.Always => true,
            ProcessMode.Pausable => !Paused,
            ProcessMode.WhenPaused => Paused,
            _ => true
        };
    }

    private static Camera? FindFirstCamera(Node node)
    {
        if (node is Camera c)
            return c;

        var count = node.ChildCount;
        for (var i = 0; i < count; i++)
        {
            var found = FindFirstCamera(node.GetChild(i));
            if (found is not null)
                return found;
        }

        return null;
    }

    private void DispatchGuiInput(InputEvent ev)
    {
        var focused = FocusedControl;
        if (focused is null)
            return;

        // Если фокус “протух” (контрол ушёл из дерева) — сбрасываем.
        if (!ReferenceEquals(focused.SceneTree, this))
        {
            FocusedControl = null;
            return;
        }

        // Уважаем паузу/ProcessMode так же, как и для обычных узлов.
        var mode = GetEffectiveProcessMode(focused);
        if (!ShouldRun(mode))
            return;

        focused.InternalGUIInput(ev);
    }

    private static ProcessMode GetEffectiveProcessMode(Node node)
    {
        var mode = node.ProcessMode;
        while (mode == ProcessMode.Inherit)
        {
            var parent = node.Parent;
            if (parent is null)
                return ProcessMode.Always;

            node = parent;
            mode = node.ProcessMode;
        }

        return mode;
    }

    private static bool IsKeyEvent(InputEventType type)
        => type is InputEventType.KeyDown or InputEventType.KeyUp;

    private static bool IsShortcutEligible(InputEventType type)
        => type is InputEventType.KeyDown or InputEventType.KeyUp
            or InputEventType.GamepadButtonDown or InputEventType.GamepadButtonUp;
    #endregion
}
