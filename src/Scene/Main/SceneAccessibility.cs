using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using Flag = Electron2D.AccessibilityServer.AccessibilityFlags;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
namespace Electron2D;

internal sealed class SceneAccessibility(SceneTree tree)
{
    private readonly List<Node> _nodes = [];
    private ulong _revision = ulong.MaxValue;
    private readonly Dictionary<int, RID> _focus = [], _previousFocus = [];
    private readonly Dictionary<RID, int> _childOrder = [];
    internal static SceneAccessibility? Create(SceneTree tree) =>
        tree.Root is Window window && window.GetWindowID() >= 0 && AccessibilityServer.AutomaticOutput ? new(tree) : null;
    private static Window? NativeWindow(Node node)
    {
        var window = node.GetWindow();
        while (window is not null && window.GetWindowID() < 0) window = window.Embedder?.GetWindow();
        return window;
    }
    internal RID GetElement(Node node)
    {
        if (node.IsDisposed || node.IsDepartingTree || !ReferenceEquals(node.Tree, tree)) return default;
        var window = NativeWindow(node); if (window is null) return default;
        if (AccessibilityServer.SourceElement(node) is { } existing)
        {
            if (existing.WindowID == window.GetWindowID()) return existing.ID;
            node.DetachAccessibility(); return GetElement(node);
        }
        var root = AccessibilityServer.GetWindowRoot(window.GetWindowID());
        var role = RoleFor(node);
        var id = node == window ? root : AccessibilityServer.CreateElement(window.GetWindowID(), role);
        var e = AccessibilityServer.BindSource(id, node); node.SetAccessibilityElement(id);
        if (!e.IsWindow)
        {
            var parent = node.Parent is { } p && NativeWindow(p) == window ? GetElement(p) : root;
            AccessibilityServer.UpdateAddChild(parent.IsValid() ? parent : root, id);
        }
        if (node is Control control) AccessibilityServer.UpdateSourceActions(e, control);
        node.AccessibilityQueued = true;
        return id;
    }
    internal void Sync()
    {
        if (_revision != tree.PathRevision)
        {
            _nodes.Clear(); tree.Root.AppendAccessibilityNodes(_nodes); _revision = tree.PathRevision;
            _childOrder.Clear();
            foreach (var node in _nodes)
            {
                var id = GetElement(node); if (!id.IsValid()) continue;
                var e = AccessibilityServer.SourceElement(node)!; if (e.IsWindow) continue;
                var window = NativeWindow(node)!;
                var parent = node.Parent is { } p && NativeWindow(p) == window ? GetElement(p) : AccessibilityServer.GetWindowRoot(e.WindowID);
                var ordinal = _childOrder.GetValueOrDefault(parent); _childOrder[parent] = ordinal + 1;
                AccessibilityServer.ReconcileSourceParent(e, parent, ordinal);
            }
        }
        _focus.Clear();
        foreach (var node in _nodes)
        {
            if (node.IsDisposed || node.IsDepartingTree || !ReferenceEquals(node.Tree, tree)) continue;
            var id = GetElement(node); if (!id.IsValid()) continue;
            var e = AccessibilityServer.SourceElement(node)!;
            var name = node switch { Control c => c.AccessibleCaption, Window w => w.AccessibilityTitle, _ => "" };
            var description = node is Control c2 ? c2.AccessibleDescription : "";
            var role = RoleFor(node); var flags = 0UL;
            var visible = node is not CanvasItem canvas || canvas.IsVisibleInTree;
            if (node is Window win) visible = win.Visible;
            if (!visible) flags |= 1UL << (int)Flag.Hidden;
            if (node is BaseButton { Disabled: true }) flags |= 1UL << (int)Flag.Disabled;
            if (node is Window or Control { ClipContents: true }) flags |= 1UL << (int)Flag.ClipsChildren;
            var isChecked = node is BaseButton { ToggleMode: true, ButtonPressed: true };
            var bounds = node switch
            {
                Control c => (c.GetViewport()!.GetScreenTransform() * c.GetGlobalTransformWithCanvas()) * new Rect2(Vector2.Zero, c.Size),
                Window w when w.GetWindowID() >= 0 => new Rect2(Vector2.Zero, DisplayServer.WindowGetSize(w.GetWindowID())),
                Window w => w.GetScreenTransform() * new Rect2(Vector2.Zero, w.Size),
                _ => default,
            };
            var changed = !e.SourcePrepared || name != e.SourceName || description != e.SourceDescription || role != e.SourceRole || flags != e.SourceFlags || isChecked != e.SourceChecked || bounds != e.SourceBounds;
            if (!e.SourcePrepared || name != e.SourceName) AccessibilityServer.UpdateSetName(id, name);
            if (!e.SourcePrepared || description != e.SourceDescription) AccessibilityServer.UpdateSetDescription(id, description);
            if (!e.SourcePrepared || role != e.SourceRole) AccessibilityServer.UpdateSetRole(id, role);
            if (!e.SourcePrepared || bounds != e.SourceBounds) AccessibilityServer.UpdateSetBounds(id, bounds);
            if (!e.SourcePrepared || isChecked != e.SourceChecked) AccessibilityServer.UpdateSetChecked(id, isChecked);
            var alteredFlags = e.SourcePrepared ? e.SourceFlags ^ flags : (1UL << 10) - 1;
            for (var index = 0; index < 10; index++) if ((alteredFlags & 1UL << index) != 0) AccessibilityServer.UpdateSetFlag(id, (Flag)index, (flags & 1UL << index) != 0);
            if (node is Control actionControl) AccessibilityServer.UpdateSourceActions(e, actionControl);
            if (e.IsWindow)
            {
                AccessibilityServer.SetWindowFocused(e.WindowID, DisplayServer.WindowIsFocused(e.WindowID));
                DisplayServer.Service!.SynchronizeAccessibilityRect(e.WindowID);
            }
            e.SourceName = name; e.SourceDescription = description; e.SourceRole = role; e.SourceFlags = flags; e.SourceChecked = isChecked; e.SourceBounds = bounds; e.SourcePrepared = true;
            if (changed || node.AccessibilityQueued)
            {
                node.AccessibilityQueued = false; node.DispatchAccessibilityUpdate();
            }
            if (!node.IsDisposed && node is Control focused && HasActiveFocus(focused) && visible && (flags & 1UL << (int)Flag.Disabled) == 0)
            {
                var selected = node.FocusedAccessibilityElement();
                if (!AccessibilityServer.HasElement(selected)) throw new InvalidOperationException("The focused accessibility element is not live.");
                AccessibilityServer.ValidateSourceFocus(e.WindowID, selected);
                _focus[e.WindowID] = selected;
            }
        }
        foreach (var (window, id) in _previousFocus) if (!_focus.ContainsKey(window)) AccessibilityServer.ClearFocusIf(id);
        _previousFocus.Clear();
        foreach (var (window, id) in _focus) { AccessibilityServer.UpdateSetFocus(id); _previousFocus[window] = id; }
    }
    private static bool HasActiveFocus(Control control)
    {
        if (!control.HasFocus()) return false;
        var viewport = control.GetViewport();
        while (viewport is SubViewport { Parent: SubViewportContainer container })
        {
            if (!container.HasFocus()) return false;
            viewport = container.GetViewport();
        }
        return viewport is not Window { Embedder: not null } embedded || embedded.HasFocus();
    }
    private static Role RoleFor(Node node) => node switch
    {
        Window => Role.Window,
        CheckBox => Role.CheckBox,
        CheckButton => Role.CheckButton,
        BaseButton { ButtonGroup: not null, ToggleMode: true } => Role.RadioButton,
        LinkButton => Role.Link,
        BaseButton => Role.Button,
        Label => Role.StaticText,
        Container { AccessibilityRegion: true } => Role.Region,
        Panel or PanelContainer => Role.Panel,
        _ => Role.Container,
    };
}
