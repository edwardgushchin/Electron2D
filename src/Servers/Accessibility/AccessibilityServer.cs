namespace Electron2D;

/// <summary>Publishes typed semantic elements and actions to the native assistive-technology service.</summary>
/// <remarks>Static operations use a permanent retained object. Element operations require the active display owner.
/// Native availability is distinct from application output and from human screen-reader acceptance.</remarks>
public sealed partial class AccessibilityServer : ElectronObject
{
    internal static readonly AccessibilityServer Runtime = new();
    internal sealed class Element
    {
        internal readonly RID ID = RID.Allocate();
        internal string? TextValue;
        internal int TextScalarCount, SourceTextVersion;
        internal Rect2[] TextRectangles = [];
        internal readonly List<TextStyle> TextStyles = [];
        internal Control? TextSource;
        internal int TextLine;
        internal bool TextEnabled, EditableTextRegistered, TextProtected, IsTextLine, SourceTextPrepared, SourceTextEditable, SourceTextSelectable, TextLineHasNewline;
        internal string? SourceLineValue;
        internal string TextLanguage = "", TextPlaceholder = "";
        internal Action<RID, int, RID, int>? SourceTextSelectionAction;
        internal Action<string>? SourceTextReplaceAction;
        internal readonly List<Element> SourceTextLines = [];
        internal Element? SelectionAnchor, SelectionFocus;
        internal int SelectionAnchorOffset, SelectionFocusOffset;
        internal Action<RID, int, RID, int>? SetTextSelectionAction;
        internal Action<string>? ReplaceTextAction;
        internal int TextFormatMask;
        internal HorizontalAlignment TextAlignment;
        internal bool TextVertical, TextUnderline, TextStrikethrough, TextOverline;
        internal Color? TextForeground, TextBackground;
        internal Color TextDecorationColor = Colors.Black;
        internal readonly int WindowID;
        internal readonly string Path;
        internal readonly List<Element> Children = [];
        internal readonly Dictionary<AccessibilityAction, Action> Actions = [];
        internal readonly List<Relation> Relations = [], ExpectedRelations = [];
        internal readonly HashSet<Element> RelationOwners = [];
        internal ulong SourceRelationRevision;
        internal ButtonGroup? SourceRelationGroup;
        internal bool SourceRelationToggle, HyperlinkRegistered;
        internal Element? Parent;
        internal Node? Source;
        internal string? SourceName, SourceDescription;
        internal Rect2 SourceBounds;
        internal AccessibilityRole SourceRole;
        internal ulong SourceFlags;
        internal bool SourceChecked, SourcePrepared, SourceCanFocus;
        internal Action? SourceFocusAction, SourceBlurAction, SourceClickAction, SourceIncrementAction, SourceDecrementAction;
        internal Action<double>? SetValueAction, SourceSetValueAction;
        internal bool HasNumericValue, SourceNumericPrepared, SourceCanEdit;
        internal double NumericValue, NumericMinimum, NumericMaximum, NumericStep, NumericJump;
        internal (double Value, double Minimum, double Maximum, double Step, double Jump) SourceNumeric;
        internal byte ScrollMask, SourceScrollAxes;
        internal Vector2 ScrollOffset, ScrollMinimum, ScrollMaximum, SourceScrollOffset, SourceScrollMinimum, SourceScrollMaximum;
        internal bool SourceScrollPrepared;
        internal Dictionary<AccessibilityAction, Action<AccessibilityScrollUnit>>? ScrollUnitActions;
        internal Action<AccessibilityScrollHint>? ScrollIntoViewAction;
        internal Action<Vector2>? ScrollToPointAction, SetScrollOffsetAction, SourceSetScrollOffsetAction;
        internal Action<AccessibilityScrollUnit>? SourceScrollUpAction, SourceScrollDownAction, SourceScrollLeftAction, SourceScrollRightAction;
        internal Action? SourceScrollBackwardAction, SourceScrollForwardAction;

        internal CollectionState? Collection;
        internal bool SelectionRegistered, TableRegistered, TableCellRegistered, CollectionRetirementQueued;
        internal Control? CollectionSource;
        internal Element? CollectionOwner;
        internal object? CollectionItem;
        internal TreeItem? CollectionTreeItem;
        internal int CollectionSourceIndex, CollectionSourceColumn;
        internal bool CollectionSelectable = true;
        internal Element? CollectionParent;
        internal Dictionary<object, Element>? SourceCollectionElements;
        internal HashSet<object>? SourceCollectionExpected;
        internal List<object>? SourceCollectionRemoved;
        internal List<Element>? SourceCollectionOrder;
        internal CollectionState? SourceCollectionSnapshot;
        internal long SourceCollectionVersion = long.MinValue, SourceCollectionStateVersion = long.MinValue;
        internal Element? SourceCollectionActive;
        internal bool OwnedSubelement, IsApplication, IsWindow, Focused, Checked, Retired, ScreenOriginKnown, WindowFocused;
        internal Rect2 OuterRect, InnerRect;
        internal AccessibilityRole Role;
        internal ulong Flags;
        internal Rect2 Bounds;
        internal Transform Transform = Transform.Identity;
        internal AccessibilityLiveMode? LiveMode, SourceLiveMode;
        internal AccessibilityPopupType? PopupType, SourcePopupType;
        internal string Tooltip = "", SourceTooltip = "";
        internal bool AnnouncementPending;
        internal string? AnnouncementText, SourceLiveContent;
        internal Action? SourceShowTooltipAction, SourceHideTooltipAction, SourceContextMenuAction;
        internal bool SourceContextMenuEnabled;
        internal string Name = "", Description = "";
        internal Element(int windowID, AccessibilityRole role)
        {
            WindowID = windowID; Role = role; Path = "/org/electron2d/accessibility/e" + ID.GetID().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        internal bool Flag(AccessibilityFlags flag) => (Flags & (1UL << (int)flag)) != 0;
    }
    private readonly Dictionary<RID, Element> _elements = [];
    private readonly Dictionary<int, Element> _windows = [];
    internal readonly Element Application = new(-1, AccessibilityRole.Container) { IsApplication = true };
    private ATSPIServer? _native;
    private bool _attempted;
    private readonly Dictionary<int, Element> _focused = [];
    private AccessibilityServer() { }
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The accessibility service is permanent.");

    /// <summary>Reports whether the active display can connect to a real native accessibility service.</summary>
    /// <returns>True for an established native bridge; false without a display or on an unavailable backend.</returns>
    /// <remarks>The first query during a display lifetime performs cold native initialization on its owner.</remarks>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    public static bool IsSupported()
    {
        if (!DisplayServer.IsAvailable || Runtime._support == AccessibilitySupport.Disabled) return false;
        _ = DisplayServer.WindowGetSize();
        if (!Runtime._attempted)
        {
            Runtime._attempted = true;
            Runtime._native = ATSPIServer.TryCreate(Runtime);
        }
        return Runtime._native?.Available == true;
    }
    private static AccessibilityServer Require()
    {
        if (!IsSupported()) throw new NotSupportedException("The active display has no native accessibility bridge.");
        return Runtime;
    }
    private Element Get(RID id) => _elements.TryGetValue(id, out var e) && !e.Retired ? e : throw new KeyNotFoundException("The accessibility element is absent, retired or owned by another server.");
    internal IEnumerable<Element> Elements => _elements.Values;
    private Element Add(int windowID, AccessibilityRole role)
    {
        ValidateRole(role); _elementRevision++; var e = new Element(windowID, role); _elements.Add(e.ID, e);
        try { _native!.Register(e); }
        catch { _elements.Remove(e.ID); throw; }
        return e;
    }
    private Element Window(int windowID)
    {
        _ = DisplayServer.WindowGetSize(windowID);
        if (_windows.TryGetValue(windowID, out var root)) return root;
        root = Add(windowID, AccessibilityRole.Window); root.IsWindow = true;
        root.WindowFocused = DisplayServer.WindowIsFocused(windowID);
        root.Name = DisplayServer.Service!.WindowGetTitle(windowID); root.Bounds = new(Vector2.Zero, DisplayServer.WindowGetSize(windowID));
        _windows.Add(windowID, root); Attach(Application, root, -1); return root;
    }
    private static void ValidateRole(AccessibilityRole role) { if (role is < AccessibilityRole.Unknown or > AccessibilityRole.TextRun) throw new ArgumentOutOfRangeException(nameof(role)); }
    private static void ValidateFlag(AccessibilityFlags flag) { if (flag is < AccessibilityFlags.Hidden or > AccessibilityFlags.ClipsChildren) throw new ArgumentOutOfRangeException(nameof(flag)); }
    private void Attach(Element parent, Element child, int index)
    {
        if (parent.WindowID != child.WindowID && !parent.IsApplication) throw new ArgumentException("Elements belong to different native windows.");
        for (var p = parent; p is not null; p = p.Parent) if (p == child) throw new ArgumentException("An accessibility relationship cannot create a cycle.");
        var count = parent.Children.Count - (child.Parent == parent ? 1 : 0);
        if (index < -1 || index > count) throw new ArgumentOutOfRangeException(nameof(index));
        var previous = child.Parent; var oldIndex = previous?.Children.IndexOf(child) ?? -1;
        var newIndex = index < 0 ? count : index;
        if (previous == parent && oldIndex == newIndex) return;
        previous?.Children.Remove(child); child.Parent = parent; parent.Children.Insert(newIndex, child);
        List<Exception>? errors = null;
        if (previous is not null)
            try { _native!.ChildrenChanged(previous, child, false, oldIndex); } catch (Exception error) { (errors ??= []).Add(error); }
        try { _native!.ChildrenChanged(parent, child, true); } catch (Exception error) { (errors ??= []).Add(error); }
        try { ValidateActiveRelations(); } catch (Exception error) { (errors ??= []).Add(error); }
        try { ValidateTextSelections(); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Accessibility hierarchy notifications failed.", errors);
    }
    /// <summary>Creates a producer-owned element associated with a native window.</summary>
    /// <param name="windowID">A live native window identity.</param><param name="role">The semantic role.</param>
    /// <returns>A new element RID, initially unattached to the semantic tree.</returns>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A typed selector, insertion position or finite geometry contract is invalid.</exception>
    public static RID CreateElement(int windowID, AccessibilityRole role)
    {
        ValidateRole(role); var r = Require(); _ = r.Window(windowID); return r.Add(windowID, role).ID;
    }
    /// <summary>Creates an attached sub-element owned by its semantic parent.</summary>
    /// <param name="parentRID">The live parent.</param><param name="role">The semantic role.</param>
    /// <param name="insertPosition">Insertion index, or -1 to append.</param><returns>A new sub-element identity.</returns>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A typed selector, insertion position or finite geometry contract is invalid.</exception>
    public static RID CreateSubElement(RID parentRID, AccessibilityRole role, int insertPosition = -1)
    {
        var r = Require(); var parent = r.Get(parentRID); ValidateRole(role);
        if (insertPosition < -1 || insertPosition > parent.Children.Count) throw new ArgumentOutOfRangeException(nameof(insertPosition));
        var e = r.Add(parent.WindowID, role); e.OwnedSubelement = true;
        try { r.Attach(parent, e, insertPosition); return e.ID; } catch { r.Free(e); throw; }
    }
    /// <summary>Gets the service-owned root element for a live native window.</summary>
    /// <param name="windowID">A native window identity.</param><returns>The borrowed root RID.</returns>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    public static RID GetWindowRoot(int windowID) => Require().Window(windowID).ID;
    /// <summary>Reports membership in the current accessibility element owner.</summary>
    /// <param name="id">An identity to inspect.</param><returns>True only for a live element in this service.</returns>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    public static bool HasElement(RID id)
    {
        if (!DisplayServer.IsAvailable) return false;
        _ = DisplayServer.WindowGetSize(); return Runtime._elements.ContainsKey(id);
    }
    /// <summary>Frees a producer-owned element and its owned sub-elements, detaching ordinary child elements.</summary>
    /// <param name="id">A live element; borrowed native-window roots cannot be freed by callers.</param>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    /// <exception cref="AggregateException">Native retirement failed after all owned elements were attempted.</exception>
    /// <exception cref="AggregateException">Native publication fails after required graph and ownership work is attempted.</exception>
    public static void FreeElement(RID id)
    {
        var r = Require(); var e = r.Get(id); if (e.IsWindow || e.Source is not null) throw new InvalidOperationException("The native window or scene node owns this accessibility element."); r.Free(e);
    }
    private void Free(Element e)
    {
        var textOwner = e.IsTextLine && e.TextSource is null ? ManualTextOwner(e.Parent) : null;
        var oldText = textOwner is null ? null : CaptureText(textOwner);
        List<Exception>? errors = null;
        if (_focusOwners.GetValueOrDefault(e.WindowID) == e)
        {
            _focusOwners.Remove(e.WindowID);
            if (_focused.Remove(e.WindowID, out var selected))
            {
                selected.Focused = false;
                try { _native!.StateChanged(selected, "focused", false); } catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        else if (_focused.GetValueOrDefault(e.WindowID) == e) { _focused.Remove(e.WindowID); e.Focused = false; }
        while (e.Children.Count != 0)
        {
            var child = e.Children[^1];
            if (child.OwnedSubelement)
            {
                try { Free(child); } catch (Exception error) { (errors ??= []).Add(error); }
            }
            else
            {
                var index = e.Children.Count - 1; e.Children.RemoveAt(index); child.Parent = null;
                try { _native!.ChildrenChanged(e, child, false, index); } catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        if (e.Parent is { } parent)
        {
            var index = parent.Children.IndexOf(e); parent.Children.RemoveAt(index); e.Parent = null;
            try { _native!.ChildrenChanged(parent, e, false, index); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (e.SourceCollectionElements is { } collection)
            foreach (var item in collection.Values.ToArray())
                if (!item.Retired && item.CollectionOwner == e)
                    try { Free(item); } catch (Exception error) { (errors ??= []).Add(error); }
        e.Parent = null; e.Source = null; e.SourceFocusAction = e.SourceBlurAction = e.SourceClickAction = e.SourceIncrementAction = e.SourceDecrementAction = null; e.SetValueAction = e.SourceSetValueAction = null;
        e.SelectionAnchor = e.SelectionFocus = null; e.SetTextSelectionAction = e.SourceTextSelectionAction = null; e.ReplaceTextAction = e.SourceTextReplaceAction = null; e.TextSource = null; e.SourceTextLines.Clear();
        e.Retired = true; ClearCollectionSource(e); ClearScroll(e); e.SourceShowTooltipAction = e.SourceHideTooltipAction = e.SourceContextMenuAction = null; e.AnnouncementText = null; e.Actions.Clear(); _elements.Remove(e.ID); _elementRevision++;
        RetireRelations(e, ref errors); e.ExpectedRelations.Clear(); e.SourceRelationGroup = null;
        try { ValidateActiveRelations(); } catch (Exception error) { (errors ??= []).Add(error); }
        try { ValidateTextSelections(); } catch (Exception error) { (errors ??= []).Add(error); }
        if (textOwner is not null && !textOwner.Retired)
            try { _native!.TextChanged(textOwner, oldText!, CaptureText(textOwner)); } catch (Exception error) { (errors ??= []).Add(error); }
        try { _native!.Unregister(e); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Accessibility element retirement failed.", errors);
    }
    /// <summary>Attaches or reparents an existing element at the end of the parent's child list.</summary>
    /// <param name="id">The parent.</param><param name="childID">A live child in the same native window.</param>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    /// <exception cref="ArgumentException">The relationship crosses native windows or creates a cycle.</exception>
    /// <exception cref="AggregateException">Native publication fails after required graph and ownership work is attempted.</exception>
    public static void UpdateAddChild(RID id, RID childID) { var r = Require(); var child = r.Get(childID); if (child.IsWindow) throw new InvalidOperationException("Native window roots are owned by the service."); r.Attach(r.Get(id), child, -1); }
    /// <summary>Sets the human-readable accessible name.</summary><param name="id">Element.</param><param name="name">Name, including an empty value.</param>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    /// <exception cref="ArgumentNullException">A required text or callback argument is null.</exception>
    public static void UpdateSetName(RID id, string name) { ArgumentNullException.ThrowIfNull(name); var r = Require(); var e = r.Get(id); if (e.Name == name) return; e.Name = name; r._native!.PropertyChanged(e, "accessible-name", name); }
    /// <summary>Sets the human-readable accessible description.</summary><param name="id">Element.</param><param name="description">Description.</param>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    /// <exception cref="ArgumentNullException">A required text or callback argument is null.</exception>
    public static void UpdateSetDescription(RID id, string description) { ArgumentNullException.ThrowIfNull(description); var r = Require(); var e = r.Get(id); if (e.Description == description) return; e.Description = description; r._native!.PropertyChanged(e, "accessible-description", description); }
    /// <summary>Sets the role used by native assistive clients.</summary><param name="id">Element.</param><param name="role">Semantic role.</param>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A typed selector, insertion position or finite geometry contract is invalid.</exception>
    public static void UpdateSetRole(RID id, AccessibilityRole role) { ValidateRole(role); var r = Require(); var e = r.Get(id); if (e.Role == role) return; e.Role = role; r._native!.RoleChanged(e); }
    /// <summary>Updates an independent semantic state.</summary><param name="id">Element.</param><param name="flag">State selector.</param><param name="value">New state.</param>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A typed selector, insertion position or finite geometry contract is invalid.</exception>
    public static void UpdateSetFlag(RID id, AccessibilityFlags flag, bool value)
    {
        ValidateFlag(flag); var r = Require(); var e = r.Get(id); if (e.Flag(flag) == value) return;
        if (value) e.Flags |= 1UL << (int)flag; else e.Flags &= ~(1UL << (int)flag);
        r._native!.StateChanged(e, flag, value);
        if (flag == AccessibilityFlags.ReadOnly && e.HasNumericValue) r._native.ActionsChanged(e);
    }
    /// <summary>Updates a checkable element's current checked state.</summary><param name="id">Element.</param><param name="isChecked">New checked state.</param>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    public static void UpdateSetChecked(RID id, bool isChecked) { var r = Require(); var e = r.Get(id); if (e.Checked == isChecked) return; e.Checked = isChecked; r._native!.StateChanged(e, "checked", isChecked); }
    internal static Rect2 TransformBounds(Rect2 bounds, Transform transform)
    {
        var x = transform.X * bounds.Size.X; var y = transform.Y * bounds.Size.Y;
        var minimum = new Vector2(MathF.Min(0, x.X) + MathF.Min(0, y.X), MathF.Min(0, x.Y) + MathF.Min(0, y.Y));
        return new Rect2(transform * bounds.Position + minimum, x.Abs() + y.Abs());
    }
    /// <summary>Sets finite window-local bounds before the element transform.</summary><param name="id">Element.</param><param name="rect">A finite rectangle with nonnegative dimensions.</param>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A typed selector, insertion position or finite geometry contract is invalid.</exception>
    public static void UpdateSetBounds(RID id, Rect2 rect)
    {
        if (!rect.IsFinite() || !rect.End.IsFinite() || rect.Size.X < 0 || rect.Size.Y < 0) throw new ArgumentOutOfRangeException(nameof(rect));
        var r = Require(); var e = r.Get(id); if (!TransformBounds(rect, e.Transform).IsFinite() || !TransformBounds(rect, e.Transform).End.IsFinite()) throw new ArgumentOutOfRangeException(nameof(rect)); if (e.Bounds == rect) return; e.Bounds = rect; r._native!.BoundsChanged(e);
    }
    /// <summary>Sets a finite transform from element bounds to native-window coordinates.</summary><param name="id">Element.</param><param name="transform">Affine transform.</param>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A typed selector, insertion position or finite geometry contract is invalid.</exception>
    public static void UpdateSetTransform(RID id, Transform transform)
    {
        if (!transform.IsFinite()) throw new ArgumentOutOfRangeException(nameof(transform));
        var r = Require(); var e = r.Get(id); if (!TransformBounds(e.Bounds, transform).IsFinite() || !TransformBounds(e.Bounds, transform).End.IsFinite()) throw new ArgumentOutOfRangeException(nameof(transform)); if (e.Transform == transform) return; e.Transform = transform; r._native!.BoundsChanged(e);
    }
    /// <summary>Registers a compiled no-argument activation, focus, branch, numeric step or sequential scroll callback.</summary><param name="id">Element.</param><param name="action">Action kind.</param><param name="callback">Borrowed callback run on the display owner.</param>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A typed selector, insertion position or finite geometry contract is invalid.</exception>
    /// <exception cref="ArgumentNullException">A required text or callback argument is null.</exception>
    public static void UpdateAddAction(RID id, AccessibilityAction action, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback); if (action is not (AccessibilityAction.Click or AccessibilityAction.Focus or AccessibilityAction.Blur or AccessibilityAction.Decrement or AccessibilityAction.Increment or AccessibilityAction.Expand or AccessibilityAction.Collapse or AccessibilityAction.ScrollBackward or AccessibilityAction.ScrollForward or AccessibilityAction.ShowTooltip or AccessibilityAction.HideTooltip or AccessibilityAction.ShowContextMenu)) throw new ArgumentOutOfRangeException(nameof(action));
        var r = Require(); var e = r.Get(id); e.Actions[action] = callback; r._native!.ActionsChanged(e);
    }
    /// <summary>Publishes the currently focused semantic element.</summary><param name="id">A live element.</param>
    /// <remarks>Active-descendant chains select semantic focus without moving the ordinary GUI focus owner.</remarks>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">An element identity is absent, retired or belongs to another owner.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    public static void UpdateSetFocus(RID id)
    {
        var r = Require(); r.SetFocusOwner(r.Get(id));
    }
    /// <summary>Publishes the native window's active focus state.</summary><param name="windowID">Native window.</param><param name="focused">Whether the window currently has native focus.</param>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    public static void SetWindowFocused(int windowID, bool focused)
    {
        var r = Require(); var root = r.Window(windowID); if (root.WindowFocused == focused) return;
        root.WindowFocused = focused;
        if (r._focused.TryGetValue(windowID, out var e)) r._native!.StateChanged(e, "focused", focused);
        r._native!.StateChanged(root, "active", focused);
    }
    /// <summary>Publishes native screen bounds for a window and its client area.</summary>
    /// <param name="windowID">Live native window identity.</param>
    /// <param name="rectOut">Finite outer screen bounds, including decorations.</param>
    /// <param name="rectIn">Finite client screen bounds, excluding decorations.</param>
    /// <remarks>Screen coordinates remain unknown until a producer supplies their true origin. Window-relative queries always work.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Either rectangle is nonfinite or has negative dimensions.</exception>
    /// <exception cref="NotSupportedException">No supported native display bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Native display access is off its owner, or an ownership rule is violated.</exception>
    public static void SetWindowRect(int windowID, Rect2 rectOut, Rect2 rectIn)
    {
        ValidateWindowRect(rectOut, nameof(rectOut)); ValidateWindowRect(rectIn, nameof(rectIn));
        var r = Require(); var root = r.Window(windowID);
        if (root.ScreenOriginKnown && root.OuterRect == rectOut && root.InnerRect == rectIn) return;
        root.OuterRect = rectOut; root.InnerRect = rectIn; root.ScreenOriginKnown = true;
        r._native!.BoundsChanged(root);
    }
    private static void ValidateWindowRect(Rect2 rect, string parameterName)
    {
        if (!rect.IsFinite() || !rect.End.IsFinite() || rect.Size.X < 0 || rect.Size.Y < 0) throw new ArgumentOutOfRangeException(parameterName);
    }
    internal static void ClearFocusIf(RID id)
    {
        var r = Runtime;
        if (!r._elements.TryGetValue(id, out var owner) || r._focusOwners.GetValueOrDefault(owner.WindowID) != owner && r._focused.GetValueOrDefault(owner.WindowID) != owner) return;
        r._focusOwners.Remove(owner.WindowID);
        if (r._focused.Remove(owner.WindowID, out var selected)) { selected.Focused = false; r._native?.StateChanged(selected, "focused", false); }
    }
    internal static void PumpNative() { Runtime._native?.Pump(); }
    internal static void RetireDisplay()
    {
        var r = Runtime;
        try { r._native?.Dispose(); }
        finally
        {
            r._native = null; r._attempted = false; r._automatic = false; r._focused.Clear(); r._focusOwners.Clear(); r._activeOwners.Clear();
            foreach (var e in r._elements.Values)
            {
                r.ClearCollectionSource(e); ClearScroll(e); e.SourceShowTooltipAction = e.SourceHideTooltipAction = e.SourceContextMenuAction = null; e.AnnouncementText = null;
                e.SelectionAnchor = e.SelectionFocus = null; e.SetTextSelectionAction = e.SourceTextSelectionAction = null; e.ReplaceTextAction = e.SourceTextReplaceAction = null; e.TextSource = null; e.SourceTextLines.Clear();
                e.Retired = true; e.Actions.Clear(); e.Children.Clear(); e.Parent = null; e.Source = null;
                e.Relations.Clear(); e.RelationOwners.Clear(); e.ExpectedRelations.Clear(); e.SourceRelationGroup = null;
                e.SourceFocusAction = e.SourceBlurAction = e.SourceClickAction = e.SourceIncrementAction = e.SourceDecrementAction = null; e.SetValueAction = e.SourceSetValueAction = null;
            }
            r._elements.Clear(); r._windows.Clear(); r._collectionSources.Clear(); r._collectionRetirements.Clear(); r.Application.Children.Clear();
        }
    }
    internal static void RetireWindow(int windowID)
    {
        var r = Runtime; if (r._native is null || !r._windows.Remove(windowID)) return;
        List<Exception>? errors = null;
        foreach (var e in r._elements.Values.Where(e => e.WindowID == windowID).ToArray())
            if (!e.Retired) try { r.Free(e); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Accessibility window retirement failed.", errors);
    }
}
