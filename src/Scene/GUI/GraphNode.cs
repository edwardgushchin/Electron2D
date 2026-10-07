namespace Electron2D;

/// <summary>Retains an exact typed, runtime-only borrowed graph slot payload.</summary>
public sealed class GraphSlotMetadata
{
    private sealed record Value<T>(T Item);
    private readonly object _value;
    private GraphSlotMetadata(object value) => _value = value;
    internal static GraphSlotMetadata Create<T>(T value) => new(new Value<T>(value));
    /// <summary>Reads a payload only under its originally assigned type.</summary><typeparam name="T">Exact stored type.</typeparam><param name="value">Borrowed payload on success.</param><returns>Whether the type matches, including stored nulls.</returns>
    public bool TryGet<T>(out T value) { if (_value is Value<T> stored) { value = stored.Item; return true; } value = default!; return false; }
}

/// <summary>Arranges titled graph rows and exposes independently typed input and output ports.</summary>
/// <remarks>Slot indices identify visible direct rows; port indices compress only enabled ports.
/// Textures and metadata remain borrowed. Configuration is stored; metadata is runtime-only.</remarks>
public partial class GraphNode : GraphElement
{
    private sealed class Slot
    {
        internal bool Left, Right, Style = true;
        internal int LeftType, RightType;
        internal Color LeftColor = Colors.White, RightColor = Colors.White;
        internal Texture? LeftIcon, RightIcon;
        internal GraphSlotMetadata? LeftMeta, RightMeta;
    }
    private readonly Dictionary<int, Slot> _slots = [];
    private readonly List<Row> _rows = [];
    private readonly record struct Row(Control Child, int Slot, float Minimum, float Maximum, float Ratio, float Height, bool Expand);
    private readonly GraphTitlebar _titlebar;
    private bool _ignoreInvalid, _arranging;
    private int _selectedSlot = -1, _storedSlots;
    private FocusMode _slotsFocus = FocusMode.Accessibility;
    /// <summary>Creates an empty graph node with an owned titlebar and accessibility focus policy.</summary>
    public GraphNode() { MouseFilter = MouseFilter.Stop; FocusMode = FocusMode.Accessibility; _titlebar = new(this, false); }
    /// <summary>Gets or sets the title displayed by the owned label.</summary>
    public string Title { get { CheckGraph(); return _titlebar.Label.Text; } set { MutableGraph(); ArgumentNullException.ThrowIfNull(value); _titlebar.Label.Text = value; UpdateMinimumSize(); QueueSort(); QueueRedraw(); } }
    /// <summary>Gets or sets whether this node accepts differently typed connection targets.</summary>
    public bool IgnoreInvalidConnectionType { get { CheckGraph(); return _ignoreInvalid; } set { MutableGraph(); _ignoreInvalid = value; } }
    /// <summary>Gets or sets Click, All or Accessibility focus for graph rows.</summary>
    public FocusMode SlotsFocusMode { get { CheckGraph(); return _slotsFocus; } set { MutableGraph(); if (value is < FocusMode.Click or > FocusMode.Accessibility) throw new ArgumentOutOfRangeException(nameof(value)); _slotsFocus = value; if (value == FocusMode.Click) _selectedSlot = -1; QueueRedraw(); } }
    /// <summary>Reports changed row geometry after layout.</summary>
    public event Action? SlotSizesChanged;
    /// <summary>Reports a changed slot configuration.</summary>
    public event Action<int>? SlotUpdated;
    /// <summary>Returns the borrowed titlebar; additional buttons may be added as normal children.</summary><returns>The owned horizontal container.</returns>
    public HBoxContainer GetTitlebarHBox() { CheckGraph(); return _titlebar; }
    private Slot? ReadSlot(int index) { CheckGraph(); if (index < 0 || index > 65535) throw new ArgumentOutOfRangeException(nameof(index)); return _slots.GetValueOrDefault(index); }
    private Slot WriteSlot(int index) { MutableGraph(); var slot = ReadSlot(index); if (slot != null) return slot; slot = new(); _slots.Add(index, slot); _storedSlots = Math.Max(_storedSlots, index + 1); NotifyPropertyListChanged(); return slot; }
    private void ChangedSlot(int index) { PruneIcons(); UpdateMinimumSize(); QueueSort(); QueueRedraw(); SlotUpdated?.Invoke(index); }
    /// <summary>Replaces port configuration for one slot, retaining its metadata.</summary><param name="slotIndex">Zero through 65535.</param><param name="enableLeftPort">Input enabled.</param><param name="typeLeft">Input type identifier.</param><param name="colorLeft">Input tint.</param><param name="enableRightPort">Output enabled.</param><param name="typeRight">Output type identifier.</param><param name="colorRight">Output tint.</param><param name="customIconLeft">Borrowed input icon or theme fallback.</param><param name="customIconRight">Borrowed output icon or theme fallback.</param><param name="drawStylebox">Whether row decoration contributes margins.</param>
    public void SetSlot(int slotIndex, bool enableLeftPort, int typeLeft, Color colorLeft, bool enableRightPort, int typeRight, Color colorRight, Texture? customIconLeft = null, Texture? customIconRight = null, bool drawStylebox = true)
    {
        CheckColor(colorLeft); CheckColor(colorRight); ValidateIcon(customIconLeft); ValidateIcon(customIconRight); var slot = WriteSlot(slotIndex); slot.Left = enableLeftPort; slot.LeftType = typeLeft; slot.LeftColor = colorLeft; slot.Right = enableRightPort; slot.RightType = typeRight; slot.RightColor = colorRight; slot.LeftIcon = customIconLeft; slot.RightIcon = customIconRight; WatchIcon(customIconLeft); WatchIcon(customIconRight); slot.Style = drawStylebox; ChangedSlot(slotIndex);
    }
    private static void CheckColor(Color value) { if (!float.IsFinite(value.R) || !float.IsFinite(value.G) || !float.IsFinite(value.B) || !float.IsFinite(value.A)) throw new ArgumentOutOfRangeException(nameof(value)); }
    /// <summary>Removes the slot configuration and borrowed metadata.</summary><param name="slotIndex">Nonnegative slot.</param>
    public void ClearSlot(int slotIndex) { MutableGraph(); ReadSlot(slotIndex); if (_slots.Remove(slotIndex)) ChangedSlot(slotIndex); }
    /// <summary>Removes all slot configuration and metadata.</summary>
    public void ClearAllSlots() { MutableGraph(); UnwatchIcons(); _slots.Clear(); _storedSlots = 0; _selectedSlot = -1; NotifyPropertyListChanged(); UpdateMinimumSize(); QueueSort(); QueueRedraw(); }
    /// <summary>Reads the left port enabled for a slot.</summary><param name="slotIndex">Nonnegative slot.</param><returns>Configured value or its default.</returns>
    public bool IsSlotEnabledLeft(int slotIndex) => ReadSlot(slotIndex)?.Left ?? false;
    /// <summary>Assigns the left port enabled.</summary><param name="slotIndex">Nonnegative slot.</param><param name="value">New configuration.</param>
    public void SetSlotEnabledLeft(int slotIndex, bool value) { WriteSlot(slotIndex).Left = value; ChangedSlot(slotIndex); }
    /// <summary>Reads the left port type for a slot.</summary><param name="slotIndex">Nonnegative slot.</param><returns>Configured value or its default.</returns>
    public int GetSlotTypeLeft(int slotIndex) => ReadSlot(slotIndex)?.LeftType ?? 0;
    /// <summary>Assigns the left port type.</summary><param name="slotIndex">Nonnegative slot.</param><param name="value">New configuration.</param>
    public void SetSlotTypeLeft(int slotIndex, int value) { WriteSlot(slotIndex).LeftType = value; ChangedSlot(slotIndex); }
    /// <summary>Reads the left port color for a slot.</summary><param name="slotIndex">Nonnegative slot.</param><returns>Configured value or its default.</returns>
    public Color GetSlotColorLeft(int slotIndex) => ReadSlot(slotIndex)?.LeftColor ?? Colors.White;
    /// <summary>Assigns the left port color.</summary><param name="slotIndex">Nonnegative slot.</param><param name="value">New configuration.</param>
    public void SetSlotColorLeft(int slotIndex, Color value) { CheckColor(value); WriteSlot(slotIndex).LeftColor = value; ChangedSlot(slotIndex); }
    /// <summary>Reads the left port customicon for a slot.</summary><param name="slotIndex">Nonnegative slot.</param><returns>Configured value or its default.</returns>
    public Texture? GetSlotCustomIconLeft(int slotIndex) => ReadSlot(slotIndex)?.LeftIcon ?? null;
    /// <summary>Assigns the left port customicon.</summary><param name="slotIndex">Nonnegative slot.</param><param name="value">New configuration.</param>
    public void SetSlotCustomIconLeft(int slotIndex, Texture? value) { ValidateIcon(value); WriteSlot(slotIndex).LeftIcon = value; WatchIcon(value); ChangedSlot(slotIndex); }
    /// <summary>Reads the left port metadata for a slot.</summary><param name="slotIndex">Nonnegative slot.</param><returns>Configured value or its default.</returns>
    public GraphSlotMetadata? GetSlotMetadataLeft(int slotIndex) => ReadSlot(slotIndex)?.LeftMeta ?? null;
    /// <summary>Assigns an exact typed borrowed runtime payload.</summary><typeparam name="T">Stored type.</typeparam><param name="slotIndex">Nonnegative slot.</param><param name="value">Borrowed payload.</param>
    public void SetSlotMetadataLeft<T>(int slotIndex, T value) { WriteSlot(slotIndex).LeftMeta = GraphSlotMetadata.Create(value); ChangedSlot(slotIndex); }
    /// <summary>Reads the right port enabled for a slot.</summary><param name="slotIndex">Nonnegative slot.</param><returns>Configured value or its default.</returns>
    public bool IsSlotEnabledRight(int slotIndex) => ReadSlot(slotIndex)?.Right ?? false;
    /// <summary>Assigns the right port enabled.</summary><param name="slotIndex">Nonnegative slot.</param><param name="value">New configuration.</param>
    public void SetSlotEnabledRight(int slotIndex, bool value) { WriteSlot(slotIndex).Right = value; ChangedSlot(slotIndex); }
    /// <summary>Reads the right port type for a slot.</summary><param name="slotIndex">Nonnegative slot.</param><returns>Configured value or its default.</returns>
    public int GetSlotTypeRight(int slotIndex) => ReadSlot(slotIndex)?.RightType ?? 0;
    /// <summary>Assigns the right port type.</summary><param name="slotIndex">Nonnegative slot.</param><param name="value">New configuration.</param>
    public void SetSlotTypeRight(int slotIndex, int value) { WriteSlot(slotIndex).RightType = value; ChangedSlot(slotIndex); }
    /// <summary>Reads the right port color for a slot.</summary><param name="slotIndex">Nonnegative slot.</param><returns>Configured value or its default.</returns>
    public Color GetSlotColorRight(int slotIndex) => ReadSlot(slotIndex)?.RightColor ?? Colors.White;
    /// <summary>Assigns the right port color.</summary><param name="slotIndex">Nonnegative slot.</param><param name="value">New configuration.</param>
    public void SetSlotColorRight(int slotIndex, Color value) { CheckColor(value); WriteSlot(slotIndex).RightColor = value; ChangedSlot(slotIndex); }
    /// <summary>Reads the right port customicon for a slot.</summary><param name="slotIndex">Nonnegative slot.</param><returns>Configured value or its default.</returns>
    public Texture? GetSlotCustomIconRight(int slotIndex) => ReadSlot(slotIndex)?.RightIcon ?? null;
    /// <summary>Assigns the right port customicon.</summary><param name="slotIndex">Nonnegative slot.</param><param name="value">New configuration.</param>
    public void SetSlotCustomIconRight(int slotIndex, Texture? value) { ValidateIcon(value); WriteSlot(slotIndex).RightIcon = value; WatchIcon(value); ChangedSlot(slotIndex); }
    /// <summary>Reads the right port metadata for a slot.</summary><param name="slotIndex">Nonnegative slot.</param><returns>Configured value or its default.</returns>
    public GraphSlotMetadata? GetSlotMetadataRight(int slotIndex) => ReadSlot(slotIndex)?.RightMeta ?? null;
    /// <summary>Assigns an exact typed borrowed runtime payload.</summary><typeparam name="T">Stored type.</typeparam><param name="slotIndex">Nonnegative slot.</param><param name="value">Borrowed payload.</param>
    public void SetSlotMetadataRight<T>(int slotIndex, T value) { WriteSlot(slotIndex).RightMeta = GraphSlotMetadata.Create(value); ChangedSlot(slotIndex); }
    /// <summary>Reports whether a slot draws its themed row decoration.</summary><param name="slotIndex">Nonnegative slot.</param><returns>True by default.</returns>
    public bool IsSlotDrawStylebox(int slotIndex) => ReadSlot(slotIndex)?.Style ?? true;
    /// <summary>Configures row decoration and its content margins.</summary><param name="slotIndex">Nonnegative slot.</param><param name="enable">Whether to decorate.</param>
    public void SetSlotDrawStylebox(int slotIndex, bool enable) { WriteSlot(slotIndex).Style = enable; ChangedSlot(slotIndex); }
    internal float GraphPortHeight(int index, bool input) => Port(index, input).Child.Size.Y;
    private int PortCount(bool input)
    {
        CheckGraph(); var count = 0; var row = 0;
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child, true)) { if (_slots.TryGetValue(row, out var slot) && (input ? slot.Left : slot.Right)) count++; row++; }
        return count;
    }
    private (Slot Slot, int Index, Control Child) Port(int index, bool input)
    {
        CheckGraph(); if (index < 0) throw new ArgumentOutOfRangeException(nameof(index)); var port = 0; var row = 0;
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child, true)) { if (_slots.TryGetValue(row, out var slot) && (input ? slot.Left : slot.Right) && port++ == index) return (slot, row, child); row++; }
        throw new ArgumentOutOfRangeException(nameof(index));
    }
    /// <summary>Counts enabled input ports.</summary><returns>Compressed port count.</returns>
    public int GetInputPortCount() => PortCount(true);
    /// <summary>Reads input port position under its compressed index.</summary><param name="portIndex">Valid enabled-port index.</param><returns>Current port position.</returns>
    public Vector2 GetInputPortPosition(int portIndex) { var port = Port(portIndex, true); return new(GetThemeConstant("port_h_offset"), port.Child.Position.Y + port.Child.Size.Y / 2); }
    /// <summary>Reads input port type under its compressed index.</summary><param name="portIndex">Valid enabled-port index.</param><returns>Current port type.</returns>
    public int GetInputPortType(int portIndex) { var port = Port(portIndex, true); return port.Slot.LeftType; }
    /// <summary>Reads input port color under its compressed index.</summary><param name="portIndex">Valid enabled-port index.</param><returns>Current port color.</returns>
    public Color GetInputPortColor(int portIndex) { var port = Port(portIndex, true); return port.Slot.LeftColor; }
    /// <summary>Reads input port slot under its compressed index.</summary><param name="portIndex">Valid enabled-port index.</param><returns>Current port slot.</returns>
    public int GetInputPortSlot(int portIndex) { var port = Port(portIndex, true); return port.Index; }
    /// <summary>Counts enabled output ports.</summary><returns>Compressed port count.</returns>
    public int GetOutputPortCount() => PortCount(false);
    /// <summary>Reads output port position under its compressed index.</summary><param name="portIndex">Valid enabled-port index.</param><returns>Current port position.</returns>
    public Vector2 GetOutputPortPosition(int portIndex) { var port = Port(portIndex, false); return new(Size.X - GetThemeConstant("port_h_offset"), port.Child.Position.Y + port.Child.Size.Y / 2); }
    /// <summary>Reads output port type under its compressed index.</summary><param name="portIndex">Valid enabled-port index.</param><returns>Current port type.</returns>
    public int GetOutputPortType(int portIndex) { var port = Port(portIndex, false); return port.Slot.RightType; }
    /// <summary>Reads output port color under its compressed index.</summary><param name="portIndex">Valid enabled-port index.</param><returns>Current port color.</returns>
    public Color GetOutputPortColor(int portIndex) { var port = Port(portIndex, false); return port.Slot.RightColor; }
    /// <summary>Reads output port slot under its compressed index.</summary><param name="portIndex">Valid enabled-port index.</param><returns>Current port slot.</returns>
    public int GetOutputPortSlot(int portIndex) { var port = Port(portIndex, false); return port.Index; }
    private float TitleHeight => _titlebar is null ? 0 : _titlebar.GetCombinedMinimumSize().Y + (GetThemeStyleBox("titlebar")?.GetMinimumSize().Y ?? 0);
    private Vector2 Measure(bool desired)
    {
        var panel = GetThemeStyleBox("panel")?.GetMinimumSize() ?? Vector2.Zero;
        var title = GetThemeStyleBox("titlebar")?.GetMinimumSize() ?? Vector2.Zero;
        var result = (_titlebar is null ? Vector2.Zero : desired ? _titlebar.GetBoundDesiredSize() : _titlebar.GetBoundMinimumSize()) + title;
        var rows = 0;
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child, true))
            {
                var size = desired ? child.GetBoundDesiredSize() : child.GetBoundMinimumSize();
                if (IsSlotDrawStylebox(rows)) size += GetThemeStyleBox("slot")?.GetMinimumSize() ?? Vector2.Zero;
                result.X = Math.Max(result.X, size.X + panel.X); result.Y += size.Y + (rows++ > 0 ? GetThemeConstant("separation") : 0);
            }
        return result + new Vector2(0, panel.Y);
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() => Measure(false);
    internal override Vector2 GetDesiredSize() => Measure(true);
    internal override void ArrangeGraphChildren()
    {
        if (_arranging || _titlebar is null) return;
        _arranging = true;
        try
        {
            var panel = GetThemeStyleBox("panel"); var margins = panel?.GetMinimumSize() ?? Vector2.Zero; var offset = panel?.GetOffset() ?? Vector2.Zero;
            var title = GetThemeStyleBox("titlebar"); var titleMargins = title?.GetMinimumSize() ?? Vector2.Zero;
            FitChildInRect(_titlebar, new(title?.GetOffset() ?? Vector2.Zero, new(Math.Max(0, Size.X - titleMargins.X), Math.Max(0, TitleHeight - titleMargins.Y))));
            _rows.Clear(); var minimum = 0f; var available = 0f; var ratios = 0f; var slotIndex = 0;
            var rowStyle = GetThemeStyleBox("slot");
            for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child))
                {
                    var decoration = IsSlotDrawStylebox(slotIndex) ? rowStyle?.GetMinimumSize() ?? Vector2.Zero : Vector2.Zero;
                    var min = child.GetBoundMinimumSize().Y + decoration.Y; var max = child.GetCombinedMaximumSize().Y; if (max >= 0) max += decoration.Y;
                    var expand = (child.SizeFlagsVertical & SizeFlags.Expand) != 0; var ratio = child.SizeFlagsStretchRatio;
                    _rows.Add(new(child, slotIndex++, min, max, ratio, min, expand)); minimum += min; if (expand) { available += min; ratios += ratio; }
                }
            available += Math.Max(0, Size.Y - TitleHeight - margins.Y - Math.Max(0, _rows.Count - 1) * GetThemeConstant("separation") - minimum);
            // ponytail: Weighted redistribution scans rows quadratically; reuse row storage until large-node profiling justifies an indexed solver.
            while (ratios > 0)
            {
                var retry = false;
                for (var i = 0; i < _rows.Count; i++) { var row = _rows[i]; if (!row.Expand) continue; var height = available * row.Ratio / ratios; if (height < row.Minimum || row.Maximum >= 0 && height > row.Maximum) { height = Math.Max(row.Minimum, row.Maximum >= 0 ? Math.Min(height, row.Maximum) : height); _rows[i] = row with { Height = height, Expand = false }; available -= height; ratios -= row.Ratio; retry = true; break; } _rows[i] = row with { Height = height }; }
                if (!retry) break;
            }
            var y = TitleHeight + offset.Y;
            foreach (var row in _rows)
            {
                if (row.Child.IsDisposed || row.Child.Parent != this) continue;
                var styled = IsSlotDrawStylebox(row.Slot); var rowMargins = styled ? rowStyle?.GetMinimumSize() ?? Vector2.Zero : Vector2.Zero; var rowOffset = styled ? rowStyle?.GetOffset() ?? Vector2.Zero : Vector2.Zero;
                FitChildInRect(row.Child, new(new(offset.X + rowOffset.X, y + rowOffset.Y), new(Math.Max(0, Size.X - margins.X - rowMargins.X), Math.Max(0, row.Height - rowMargins.Y))));
                y += row.Height + GetThemeConstant("separation");
            }
            SlotSizesChanged?.Invoke(); QueueRedraw();
        }
        finally { _rows.Clear(); _arranging = false; }
    }
    /// <summary>Draws a themed port; overrides may submit custom canvas commands.</summary><param name="slotIndex">Visible row index.</param><param name="position">Integer local port center.</param><param name="left">Whether this is an input port.</param><param name="color">Configured tint.</param>
    protected virtual void OnDrawPort(int slotIndex, Vector2i position, bool left, Color color)
    {
        var slot = ReadSlot(slotIndex); var icon = (left ? slot?.LeftIcon : slot?.RightIcon) ?? GetThemeIcon("port");
        if (icon != null) DrawTexture(icon, (Vector2)position - icon.GetSize() / 2, color);
    }
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        if (inputEvent.IsPressed() && Parent is GraphEdit graph)
        {
            if (inputEvent.IsActionPressed("ui_cancel", true, true)) { graph.ForceConnectionDragEnd(); AcceptEvent(); return; }
            if (inputEvent.IsActionPressed("ui_graph_delete", true, true) && graph.KeyboardConnecting) { graph.ForceConnectionDragEnd(); AcceptEvent(); return; }
            if (SlotsFocusMode == FocusMode.All)
            {
                var count = 0; for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child, true)) count++;
                if (inputEvent.IsActionPressed("ui_up", true, true) || inputEvent.IsActionPressed("ui_down", true, true))
                { _selectedSlot += inputEvent.IsActionPressed("ui_down", true, true) ? 1 : -1; if (_selectedSlot < 0 || _selectedSlot >= count) _selectedSlot = -1; QueueRedraw(); if (_selectedSlot >= 0) { AcceptEvent(); return; } }
            }
            var left = inputEvent.IsActionPressed("ui_left", true, true); var right = inputEvent.IsActionPressed("ui_right", true, true);
            var followLeft = inputEvent.IsActionPressed("ui_graph_follow_left", true, true);
            var followRight = inputEvent.IsActionPressed("ui_graph_follow_right", true, true);
            if (_selectedSlot >= 0 && (left || right || followLeft || followRight))
            {
                var output = right || followRight; var ports = output ? GetOutputPortCount() : GetInputPortCount();
                for (var p = 0; p < ports; p++) if ((output ? GetOutputPortSlot(p) : GetInputPortSlot(p)) == _selectedSlot)
                    { if (followLeft || followRight) graph.FollowGraphConnection(this, p, output); else graph.KeyboardPort(this, p, output); AcceptEvent(); return; }
            }
            if (_selectedSlot >= 0 && inputEvent.IsActionPressed("ui_accept", true, true))
            { var row = 0; for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child, true) && row++ == _selectedSlot) { _selectedSlot = -1; child.GrabFocus(); AcceptEvent(); return; } }
        }
        base.OnGUIInput(inputEvent);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what != NotificationDraw || _titlebar is null) return;
        GraphDrawing = true; try
        {
            var titleHeight = TitleHeight;
            if (GetThemeStyleBox(Selected ? "panel_selected" : "panel") is { } panel) DrawStyleBox(panel, new(new(0, titleHeight), new(Size.X, Math.Max(0, Size.Y - titleHeight))));
            if (GetThemeStyleBox(Selected ? "titlebar_selected" : "titlebar") is { } title) DrawStyleBox(title, new(Vector2.Zero, new(Size.X, titleHeight)));
            if (HasFocus() && GetThemeStyleBox("panel_focus") is { } focus) DrawStyleBox(focus, new(Vector2.Zero, Size));
            var index = 0;
            for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child))
                {
                    if (IsSlotDrawStylebox(index) && GetThemeStyleBox(index == _selectedSlot ? "slot_selected" : "slot") is { } style) DrawStyleBox(style, child.GetRect().GrowIndividual(style.GetMargin(Side.Left), style.GetMargin(Side.Top), style.GetMargin(Side.Right), style.GetMargin(Side.Bottom)));
                    if (_slots.TryGetValue(index, out var slot)) { var y = (int)(child.Position.Y + child.Size.Y / 2); if (slot.Left) OnDrawPort(index, new(GetThemeConstant("port_h_offset"), y), true, slot.LeftColor); if (slot.Right) OnDrawPort(index, new((int)Size.X - GetThemeConstant("port_h_offset"), y), false, slot.RightColor); }
                    index++;
                }
            if (CanResize && GetThemeIcon("resizer") is { } icon) DrawTexture(icon, Size - icon.GetSize(), GetThemeColor("resizer_color"));
        }
        finally { GraphDrawing = false; }
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(GraphNode) ? CreateGraphNode : base.CreateSceneInstanceFactory();
    private static Node CreateGraphNode() => new GraphNode();
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { SlotUpdated = null; SlotSizesChanged = null; UnwatchIcons(); _slots.Clear(); _rows.Clear(); } base.Dispose(disposing); }
}
