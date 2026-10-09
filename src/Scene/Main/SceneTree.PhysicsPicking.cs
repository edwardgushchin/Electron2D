namespace Electron2D;

public sealed partial class SceneTree
{
    private readonly List<Viewport> _pickingViewports = [];
    private readonly List<CanvasLayer> _pickingLayers = [];
    private static bool PointerCaptured => DisplayServer.IsAvailable && Input.MouseMode == MouseMode.Captured;

    private void QueuePhysicsPicking(Viewport viewport, InputEvent input)
    {
        if (!viewport.PhysicsObjectPicking || input is not (InputEventMouse or InputEventScreenTouch or InputEventScreenDrag)) return;
        if (PointerCaptured) { ClearPhysicsPicking(viewport); return; }
        var state = viewport.Picking ??= new();
        if (input is InputEventMouse mouse)
        {
            state.MouseKnown = true;
            var passive = state.PassiveMouse;
            passive.Position = mouse.Position; passive.GlobalPosition = mouse.Position; passive.ButtonMask = mouse.ButtonMask;
            passive.AltPressed = mouse.AltPressed; passive.ShiftPressed = mouse.ShiftPressed;
            passive.ControlPressed = mouse.ControlPressed; passive.MetaPressed = mouse.MetaPressed;
            if (_inputHandled)
            {
                if (!state.TailBlocked)
                { state.Events.Enqueue(null); state.TailBlocked = true; }
                return;
            }
        }
        if (_inputHandled) return;
        var copy = input.XformedBy(Transform.Identity);
        if (ReferenceEquals(copy, input)) copy = (InputEvent)input.Duplicate();
        state.Events.Enqueue(copy); state.TailBlocked = false;
        _inputHandled = true;
    }

    private void ProcessPhysicsPicking(ref List<Exception>? errors)
    {
        // Capture membership before callbacks; additions participate on the next tick.
        foreach (var pair in _guiStates)
        {
            var viewport = pair.Key;
            if (viewport.IsDisposed || !viewport.PhysicsObjectPicking) continue;
            if (pair.Value.GuiHoverKnown && viewport.Picking?.MouseKnown != true)
            {
                var state = viewport.Picking ??= new(); state.MouseKnown = true;
                state.PassiveMouse.Position = pair.Value.GuiHoverPosition;
                state.PassiveMouse.GlobalPosition = pair.Value.GuiHoverPosition;
            }
            if (viewport.Picking is not null) _pickingViewports.Add(viewport);
        }
        try
        {
            foreach (var viewport in _pickingViewports)
            {
                if (viewport.IsDisposed || !ReferenceEquals(viewport.Tree, this)) continue;
                using var scope = SelectGUI(viewport);
                var state = viewport.Picking!;
                if (!viewport.PhysicsObjectPicking || viewport.GUIDisableInput || PointerCaptured)
                { try { ClearPhysicsPicking(viewport); } catch (Exception error) { CollectException(ref errors, error); } continue; }
                var oldViewport = _inputViewport; var oldDispatch = _isDispatchingInput;
                _inputViewport = viewport; _isDispatchingInput = true; state.Processing = true;
                try
                {
                    CleanupPhysicsHover(viewport, state, false, ref errors, targetsOnly: true);
                    var mouseEvent = false;
                    while (state.Events.TryDequeue(out var input))
                    {
                        if (input is null) { CleanupPhysicsHover(viewport, state, true, ref errors); mouseEvent = true; continue; }
                        try
                        {
                            mouseEvent |= input is InputEventMouse;
                            if (!CanPick(viewport, state)) continue;
                            _inputHandled = false;
                            try { PickPhysicsEvent(viewport, state, input, false, ref errors); }
                            catch (Exception error) { CollectException(ref errors, error); }
                        }
                        finally { try { input.Dispose(); } catch (Exception error) { CollectException(ref errors, error); } }
                    }
                    state.TailBlocked = false;
                    if (!mouseEvent && state.MouseKnown && CanPick(viewport, state) && _gui.GuiHoverTarget is null &&
                        viewport.Parent is not SubViewportContainer { MouseFilter: MouseFilter.Ignore })
                    {
                        _inputHandled = false;
                        try
                        {
                            var mouse = state.PassiveMouse;
                            var window = viewport.GetWindow();
                            while (window?.Embedder is { } embedder) window = embedder.GetWindow();
                            if (window is not null && window.GetWindowID() != DisplayServer.InvalidWindowId) mouse.Position = viewport.GetMousePosition();
                            mouse.Device = InputEvent.DeviceIdInternal; mouse.GlobalPosition = mouse.Position;
                            mouse.Relative = mouse.Velocity = mouse.ScreenRelative = mouse.ScreenVelocity = Vector2.Zero;
                            mouse.ButtonMask = Input.MouseButtonMask;
                            mouse.AltPressed = Input.IsKeyPressed(Key.Alt); mouse.ShiftPressed = Input.IsKeyPressed(Key.Shift);
                            mouse.ControlPressed = Input.IsKeyPressed(Key.Control); mouse.MetaPressed = Input.IsKeyPressed(Key.Meta);
                            PickPhysicsEvent(viewport, state, mouse, true, ref errors);
                        }
                        catch (Exception error) { CollectException(ref errors, error); }
                    }
                }
                finally
                {
                    state.Processing = false;
                    if (state.ClearRequested || viewport.IsDisposed || !ReferenceEquals(viewport.Tree, this))
                        try { ClearPhysicsPicking(viewport); } catch (Exception error) { CollectException(ref errors, error); }
                    state.Hits.Clear(); _pickingLayers.Clear(); _gui.InputTraversal.Clear();
                    _inputHandled = false; _isDispatchingInput = oldDispatch; _inputViewport = oldViewport;
                }
            }
        }
        finally { _pickingViewports.Clear(); }
    }

    private bool CanPick(Viewport viewport, PhysicsPickingState state) => !viewport.IsDisposed && ReferenceEquals(viewport.Tree, this) &&
        viewport.PhysicsObjectPicking && !viewport.GUIDisableInput && !state.ClearRequested;

    private bool EligiblePickObject(CollisionObject body) => !body.IsDisposed && ReferenceEquals(body.Tree, this) &&
        body.InputPickable && body.IsVisibleInTree && body.CollisionLayer != 0 && body.Backend.Space is not null;

    private bool EligiblePickTarget(CollisionObject target) => !target.IsDisposed && ReferenceEquals(target.Tree, this) && target.CanProcess();

    private bool ValidPick(PhysicsPickingState.Hit hit) => EligiblePickObject(hit.Body) && EligiblePickTarget(hit.Target) && hit.Body.Backend.Space!.RID == hit.Space &&
        hit.Body.Backend.CanvasInstanceID == hit.Canvas && (uint)hit.Index < (uint)hit.Body.ShapeSlots.Count &&
        ReferenceEquals(hit.Body.ShapeSlots[hit.Index], hit.Slot) && hit.Slot.Active;

    private void PickPhysicsEvent(Viewport viewport, PhysicsPickingState state, InputEvent input, bool passive, ref List<Exception>? errors)
    {
        var point = input switch
        {
            InputEventMouse mouse => mouse.Position,
            InputEventScreenTouch touch => touch.Position,
            InputEventScreenDrag drag => drag.Position,
            _ => throw new InvalidOperationException("A picking event must have a position.")
        };
        var mouseEvent = input is InputEventMouse;
        if (mouseEvent) state.Sequence++;
        var world = viewport.FindWorld()!;
        try
        {
            if (!viewport.GetVisibleRect().HasPoint(point)) return;
            CaptureInputNodes(); _pickingLayers.Clear();
            foreach (var node in _gui.InputTraversal)
                if (node is CanvasLayer layer && !layer.IsDisposed && ReferenceEquals(layer.CanvasViewport, viewport)) _pickingLayers.Add(layer);
            PickPhysicsCanvas(viewport, state, world, point, viewport.CanvasTransform, 0, input, passive, ref errors);
            foreach (var layer in _pickingLayers)
            {
                if (!CanPick(viewport, state) || _inputHandled || input.IsDisposed || !ReferenceEquals(viewport.FindWorld()!.Runtime, world.Runtime)) break;
                if (layer.IsDisposed || !ReferenceEquals(layer.Tree, this) || !ReferenceEquals(layer.CanvasViewport, viewport)) continue;
                try { PickPhysicsCanvas(viewport, state, world, point, layer.GetFinalTransform(), layer.InstanceID, input, passive, ref errors); }
                catch (Exception error) { CollectException(ref errors, error); }
            }
        }
        finally { if (mouseEvent) CleanupPhysicsHover(viewport, state, false, ref errors); }
    }

    private void PickPhysicsCanvas(Viewport viewport, PhysicsPickingState state, World world, Vector2 point, Transform canvas, ulong canvasID,
        InputEvent input, bool passive, ref List<Exception>? errors)
    {
        state.Hits.Clear(); CollectPhysicsPicks(state, world, point, canvas, canvasID);
        if (viewport.PhysicsObjectPickingSort) state.Hits.Sort(static (a, b) =>
        {
            var order = b.Target.EffectiveZIndex.CompareTo(a.Target.EffectiveZIndex);
            if (order != 0) return order;
            if (ReferenceEquals(a.Target, b.Target)) return a.Index.CompareTo(b.Index);
            return a.Target.IsGreaterThan(b.Target) ? -1 : 1;
        });
        foreach (var hit in state.Hits)
        {
            if (!CanPick(viewport, state) || _inputHandled || input.IsDisposed) break;
            if (!ValidPick(hit) || !ReferenceEquals(viewport.FindWorld()!.Runtime, world.Runtime)) continue;
            var entered = !state.Objects.ContainsKey(hit.Target);
            if (input is InputEventMouse)
            {
                state.Objects[hit.Target] = state.Sequence;
                if (entered) hit.Target.DispatchPhysicsPointer(1, viewport, null, hit.Index, ref errors);
                if (!CanPick(viewport, state) || !ValidPick(hit)) continue;
                var shapeKey = (hit.Target, hit.Index);
                var shapeEntered = !state.Shapes.ContainsKey(shapeKey);
                state.Shapes[shapeKey] = (hit, state.Sequence);
                if (shapeEntered) hit.Target.DispatchPhysicsPointer(3, viewport, null, hit.Index, ref errors);
            }
            if (CanPick(viewport, state) && ValidPick(hit) && !input.IsDisposed && (!passive || entered))
                hit.Target.DispatchPhysicsPointer(0, viewport, input, hit.Index, ref errors);
            if (viewport.IsDisposed || viewport.PhysicsObjectPickingFirstOnly) break;
        }
    }

    private void CollectPhysicsPicks(PhysicsPickingState state, World world, Vector2 point, Transform canvas, ulong canvasID)
    {
        state.Parameters.Position = canvas.AffineInverse() * point; state.Parameters.CanvasInstanceID = canvasID;
        foreach (var result in world.DirectSpaceState!.CollectPointHits(state.Parameters))
        {
            if (state.Hits.Count == 64) break;
            if (result.Collider is not { } body || !EligiblePickObject(body) || result.ColliderObject is not CollisionObject target || !EligiblePickTarget(target) || (uint)result.ShapeIndex >= (uint)body.ShapeSlots.Count) continue;
            var slot = body.ShapeSlots[result.ShapeIndex];
            if (slot.Active) state.Hits.Add(new(body, slot, result.ShapeIndex, world.Space, canvasID, target));
        }
    }

    private void CleanupPhysicsHover(Viewport viewport, PhysicsPickingState state, bool all, ref List<Exception>? errors, bool targetsOnly = false)
    {
        state.ExitObjects.Clear(); state.ExitShapes.Clear();
        foreach (var pair in state.Objects)
            if (all || !EligiblePickTarget(pair.Key) || !targetsOnly && pair.Value != state.Sequence) state.ExitObjects.Add(pair.Key);
        foreach (var pair in state.Shapes)
            if (all || !EligiblePickTarget(pair.Key.Target) || !targetsOnly && (pair.Value.Sequence != state.Sequence || !ValidPick(pair.Value.Hit))) state.ExitShapes.Add(pair.Key);
        foreach (var target in state.ExitObjects) state.Objects.Remove(target);
        foreach (var key in state.ExitShapes) state.Shapes.Remove(key);
        foreach (var target in state.ExitObjects) target.DispatchPhysicsPointer(2, viewport, null, -1, ref errors);
        foreach (var key in state.ExitShapes) key.Target.DispatchPhysicsPointer(4, viewport, null, key.Index, ref errors);
        state.ExitObjects.Clear(); state.ExitShapes.Clear();
    }

    internal void ClearPhysicsPicking(Viewport viewport)
    {
        if (viewport.Picking is not { } state) return;
        state.MouseKnown = false; state.TailBlocked = false; state.ClearRequested = true;
        if (state.Processing) return;
        state.Processing = true; List<Exception>? errors = null;
        try { state.DisposeEvents(ref errors); CleanupPhysicsHover(viewport, state, true, ref errors); }
        finally { state.Processing = false; state.ClearRequested = false; }
        ThrowCollected("Physics pointer-exit callbacks failed.", errors);
    }
}

internal sealed class PhysicsPickingState : IDisposable
{
    internal readonly record struct Hit(CollisionObject Body, CollisionObject.ShapeSlot Slot, int Index, RID Space, ulong Canvas, CollisionObject Target);
    internal readonly Queue<InputEvent?> Events = [];
    internal readonly List<Hit> Hits = [];
    internal readonly Dictionary<CollisionObject, ulong> Objects = [];
    internal readonly Dictionary<(CollisionObject Target, int Index), (Hit Hit, ulong Sequence)> Shapes = [];
    internal readonly List<CollisionObject> ExitObjects = [];
    internal readonly List<(CollisionObject Target, int Index)> ExitShapes = [];
    internal readonly PhysicsPointQueryParameters Parameters = new() { CollideWithAreas = true };
    private InputEventMouseMotion? _passive;
    internal InputEventMouseMotion PassiveMouse => _passive is { IsDisposed: false } ? _passive : _passive = new() { Device = InputEvent.DeviceIdInternal };
    internal bool MouseKnown, TailBlocked, Processing, ClearRequested;
    internal ulong Sequence;
    internal void DisposeEvents(ref List<Exception>? errors)
    {
        while (Events.TryDequeue(out var input))
            try { input?.Dispose(); } catch (Exception error) { AnimationNode.CollectException(ref errors, error); }
    }
    public void Dispose()
    {
        List<Exception>? errors = null; DisposeEvents(ref errors);
        try { _passive?.Dispose(); } catch (Exception error) { AnimationNode.CollectException(ref errors, error); }
        Objects.Clear(); Shapes.Clear(); Hits.Clear(); ExitObjects.Clear(); ExitShapes.Clear();
        AnimationNode.ThrowCollected("Physics picking cleanup callbacks failed.", errors);
    }
}
