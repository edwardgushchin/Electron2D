namespace Electron2D;

public partial class GraphEdit
{
    private readonly record struct MoveState(GraphElement Element, Vector2 From);
    private readonly List<MoveState> _dragged = [];
    private string[] _requestNames = [];
    private GraphElement?[] _requestElements = [];
    private void PrepareRequests(int count) { if (_requestNames.Length >= count) return; var capacity = Math.Max(count, Math.Max(8, _requestNames.Length * 2)); Array.Resize(ref _requestNames, capacity); Array.Resize(ref _requestElements, capacity); }
    private void DeleteSelected()
    {
        var count = 0; foreach (var e in _elements) if (e.Selected) { _requestNames[count] = e.Name; _requestElements[count++] = e; }
        try { for (var i = 0; i < count; i++) if (_requestElements[i] is { IsDisposed: false } e && e.Parent == this) e.RequestGraphDelete(); if (!IsDisposed) DeleteNodesRequest?.Invoke(_requestNames.AsMemory(0, count)); } finally { Array.Clear(_requestElements, 0, count); Array.Clear(_requestNames, 0, count); }
    }
    private readonly HashSet<GraphElement> _previousSelection = [];
    private Vector2 _pressPosition, _pointer, _panScroll;
    private bool _elementPressed, _moving, _boxSelecting, _boxAdd, _panning, _minimapDragging, _panKey;
    private GraphNode? _connectionNode;
    private int _connectionPort;
    private bool _connectionOutput, _keyboardConnecting;
    internal bool KeyboardConnecting => _keyboardConnecting;
    internal void FollowGraphConnection(GraphNode node, int port, bool output)
    { foreach (var c in _connections) if (output ? c.Value.FromNode == node.Name && c.Value.FromPort == port : c.Value.ToNode == node.Name && c.Value.ToPort == port) { if (Element(output ? c.Value.ToNode : c.Value.FromNode) is GraphNode target && target.FocusMode == FocusMode.All) target.GrabFocus(); return; } }
    internal void KeyboardPort(GraphNode node, int port, bool output)
    {
        var point = node.Position + (output ? node.GetOutputPortPosition(port) : node.GetInputPortPosition(port)) * _zoom;
        if (_connectionNode == null) { _pointer = point; PressPort(node, port, output); _keyboardConnecting = true; return; }
        try { if (output != _connectionOutput) { var from = output ? node : _connectionNode; var to = output ? _connectionNode : node; var fp = output ? port : _connectionPort; var tp = output ? _connectionPort : port; var a = from.GetOutputPortType(fp); var b = to.GetInputPortType(tp); if ((a == b || _validTypes.Contains((a, b)) || node.IgnoreInvalidConnectionType) && OnIsNodeHoverValid(from.Name, fp, to.Name, tp)) ConnectionRequest?.Invoke(from.Name, fp, to.Name, tp); } } finally { ForceConnectionDragEnd(); }
    }
    /// <summary>Tests a graph-local pointer against an input port's theme-sized hit region.</summary><param name="node">Direct graph node.</param><param name="port">Compressed input port.</param><param name="mousePosition">Graph-local pixels.</param><returns>Whether it hits.</returns>
    protected virtual bool OnIsInInputHotzone(GraphNode node, int port, Vector2 mousePosition) => PortHotzone(node, port, mousePosition, true);
    /// <summary>Tests a graph-local pointer against an output port's theme-sized hit region.</summary><param name="node">Direct graph node.</param><param name="port">Compressed output port.</param><param name="mousePosition">Graph-local pixels.</param><returns>Whether it hits.</returns>
    protected virtual bool OnIsInOutputHotzone(GraphNode node, int port, Vector2 mousePosition) => PortHotzone(node, port, mousePosition, false);
    /// <summary>Applies additional connection validation after the port type policy.</summary><param name="fromNode">Output name.</param><param name="fromPort">Output index.</param><param name="toNode">Input name.</param><param name="toPort">Input index.</param><returns>True by default.</returns>
    protected virtual bool OnIsNodeHoverValid(string fromNode, int fromPort, string toNode, int toPort) => true;
    private bool PortHotzone(GraphNode node, int port, Vector2 point, bool input)
    {
        var center = node.Position + (input ? node.GetInputPortPosition(port) : node.GetOutputPortPosition(port)) * _zoom;
        var inner = GetThemeConstant("port_hotzone_inner_extent") * _zoom; var outer = GetThemeConstant("port_hotzone_outer_extent") * _zoom;
        var icon = GetThemeIcon("port")?.GetSize() ?? new Vector2(12, 12); var height = Math.Max(Math.Max(12, icon.Y), node.GraphPortHeight(port, input)) * _zoom;
        return new Rect2(center - new Vector2(input ? outer : inner, height / 2), new(inner + outer, height)).HasPoint(point);
    }
    private bool HitPort(Vector2 point, out GraphNode node, out int port, out bool output, bool? outputs = null)
    {
        for (var i = GetChildCount() - 1; i >= 0; i--) if (GetChild(i) is GraphNode n && n.Visible && !ClickableChild(n, GetGlobalTransformWithCanvas() * point))
            {
                if (outputs != false) for (var p = 0; p < n.GetOutputPortCount(); p++) if (OnIsInOutputHotzone(n, p, point)) { node = n; port = p; output = true; return true; }
                if (outputs != true) for (var p = 0; p < n.GetInputPortCount(); p++) if (OnIsInInputHotzone(n, p, point)) { node = n; port = p; output = false; return true; }
            }
        node = null!; port = -1; output = false; return false;
    }
    private static bool ClickableChild(Node node, Vector2 viewportPoint)
    {
        for (var i = node.GetChildCount(includeInternal: true) - 1; i >= 0; i--) if (node.GetChild(i, includeInternal: true) is Control { Visible: true } child)
            { if (ClickableChild(child, viewportPoint) || child.MouseFilter == MouseFilter.Stop && child.HitTest(viewportPoint)) return true; }
        return false;
    }
    private bool ConnectionTarget(Vector2 point, out GraphNode node, out int port)
    {
        node = null!; port = -1; if (_connectionNode is null || _connectionNode.IsDisposed || !HitPort(point, out var target, out var index, out _, !_connectionOutput)) return false;
        var from = _connectionOutput ? _connectionNode : target; var to = _connectionOutput ? target : _connectionNode; var fromPort = _connectionOutput ? _connectionPort : index; var toPort = _connectionOutput ? index : _connectionPort;
        if (fromPort >= from.GetOutputPortCount() || toPort >= to.GetInputPortCount()) return false;
        var a = from.GetOutputPortType(fromPort); var b = to.GetInputPortType(toPort);
        if (a != b && !_validTypes.Contains((a, b)) && !target.IgnoreInvalidConnectionType || !OnIsNodeHoverValid(from.Name, fromPort, to.Name, toPort)) return false;
        node = target; port = index; return true;
    }
    private bool OverlayHit(Vector2 point) => _connectionNode != null || _moving || _boxSelecting || _minimapDragging || _minimap && MinimapRect.HasPoint(point) || HitPort(point, out _, out _, out _);
    private void BeginConnection(GraphNode node, int port, bool output)
    {
        _connectionNode = node; _connectionPort = port; _connectionOutput = output; ConnectionDragStarted?.Invoke(node.Name, port, output); _overlay.QueueRedraw();
    }
    /// <summary>Cancels a current connection drag and emits its end event once.</summary>
    public void ForceConnectionDragEnd() { MutableEdit(); if (_connectionNode is null) return; _connectionNode = null; _keyboardConnecting = false; _overlay.QueueRedraw(); ConnectionDragEnded?.Invoke(); }
    private void ReleaseConnection(Vector2 position)
    {
        var node = _connectionNode; if (node is null) return;
        try
        {
            if (!node.IsDisposed && node.Parent == this)
            {
                if (ConnectionTarget(position, out var target, out var port)) { if (_connectionOutput) ConnectionRequest?.Invoke(node.Name, _connectionPort, target.Name, port); else ConnectionRequest?.Invoke(target.Name, port, node.Name, _connectionPort); }
                else if (_connectionOutput) ConnectionToEmpty?.Invoke(node.Name, _connectionPort, position); else ConnectionFromEmpty?.Invoke(node.Name, _connectionPort, position);
            }
        }
        finally { ForceConnectionDragEnd(); }
    }
    private void PressPort(GraphNode node, int port, bool output)
    {
        var type = output ? node.GetOutputPortType(port) : node.GetInputPortType(port);
        if (type < 0) return;
        if (output ? _rightDisconnects || _rightDisconnect.Contains(type) : _leftDisconnect.Contains(type))
        {
            foreach (var c in _connections)
            {
                var v = c.Value; if (output ? v.FromNode != node.Name || v.FromPort != port : v.ToNode != node.Name || v.ToPort != port) continue;
                var other = Element(output ? v.ToNode : v.FromNode) as GraphNode; var otherPort = output ? v.ToPort : v.FromPort;
                DisconnectionRequest?.Invoke(v.FromNode, v.FromPort, v.ToNode, v.ToPort);
                if (other is { IsDisposed: false } && otherPort < (output ? other.GetInputPortCount() : other.GetOutputPortCount())) BeginConnection(other, otherPort, !output);
                return;
            }
        }
        BeginConnection(node, port, output);
    }
    internal void ElementInput(GraphElement element, InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey) { OnGUIInput(inputEvent); return; }
        if (inputEvent is InputEventMouseButton button)
        {
            var position = element.Position + button.Position * _zoom;
            if (button.Pressed && button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown or MouseButton.WheelLeft or MouseButton.WheelRight) { WheelAt(button, position); AcceptEvent(); return; }
            if (button.Pressed && (button.ButtonIndex == MouseButton.Middle || button.ButtonIndex == MouseButton.Left && _panKey)) { _pressPosition = position; _panning = true; _panScroll = _scroll; AcceptEvent(); return; }
            if (!button.Pressed && button.ButtonIndex == MouseButton.Middle) { FinishPointer(); AcceptEvent(); return; }
            if (button.ButtonIndex == MouseButton.Left && button.Pressed)
            {
                if (HitPort(position, out var node, out var port, out var output)) { _pointer = position; PressPort(node, port, output); AcceptEvent(); return; }
                if (!element.Selectable) return;
                if (button.ControlPressed) element.Selected = !element.Selected; else if (!element.Selected) SetSelected(element);
                if (FocusMode == FocusMode.All) GrabFocus();
                _pressPosition = _pointer = position; _elementPressed = element.Selected && element.Draggable;
                _dragged.Clear();
                foreach (var e in _elements) if (e.Draggable && e.Selected || SelectedFrameAncestor(e)) { _dragged.Add(new(e, e.PositionOffset)); e.StartGraphDrag(); }
                AcceptEvent();
            }
            else if (!button.Pressed && button.ButtonIndex == MouseButton.Left) { _pointer = position; FinishPointer(); AcceptEvent(); }
            else if (button.Pressed && button.ButtonIndex == MouseButton.Right) { PopupRequest?.Invoke(position); AcceptEvent(); }
        }
        else if (inputEvent is InputEventMouseMotion motion) { PointerMotion(element.Position + motion.Position * _zoom); if (_elementPressed || _connectionNode != null) AcceptEvent(); }
    }
    private bool SelectedFrameAncestor(GraphElement element) { foreach (var e in _elements) if (e is GraphFrame { Selected: true, Draggable: true } && IsAttachedTo(element, e)) return true; return false; }
    private void PointerMotion(Vector2 position)
    {
        _pointer = position;
        if (_panning) { UserScroll(_panScroll - (position - _pressPosition)); return; }
        if (_minimapDragging) { PanMinimap(position); return; }
        if (_elementPressed)
        {
            var delta = (position - _pressPosition) / _zoom;
            if (!_moving && delta.LengthSquared() < 4) return;
            if (!_moving) { _moving = true; BeginNodeMove?.Invoke(); }
            _updatingFrames = true;
            try { foreach (var move in _dragged) if (!move.Element.IsDisposed && move.Element.Parent == this) { var target = move.From + delta; if (_snapping) target = target.Snapped((float)_snapDistance); move.Element.PositionOffset = target; } }
            finally { _updatingFrames = false; }
            UpdateFrames(); _lines.QueueRedraw(); _overlay.QueueRedraw();
        }
        else if (_boxSelecting)
        {
            var rect = new Rect2(_pressPosition, position - _pressPosition).Abs();
            for (var i = 0; i < _elements.Count; i++) { var e = _elements[i]; e.Selected = rect.Intersects(e.GetRect()) || _boxAdd && _previousSelection.Contains(e); }
            _overlay.QueueRedraw();
        }
        else if (_connectionNode != null) _overlay.QueueRedraw();
        else { var c = Closest(position, 4); if (!ReferenceEquals(c, _hoverConnection)) { _hoverConnection = c; _lines.QueueRedraw(); } }
    }
    private void FinishPointer()
    {
        if (_connectionNode != null) { ReleaseConnection(_pointer); return; }
        var moved = _moving; _moving = _elementPressed = _panning = _minimapDragging = _boxSelecting = false;
        if (moved)
        {
            try { foreach (var move in _dragged) if (!move.Element.IsDisposed && move.Element.Parent == this) move.Element.EndGraphDrag(); }
            finally { EndNodeMove?.Invoke(); }
            if (GraphElementsLinkedToFrameRequest != null)
                for (var i = GetChildCount() - 1; i >= 0; i--) if (GetChild(i) is GraphFrame frame && !frame.Selected && frame.GetRect().HasPoint(_pointer))
                    { var count = 0; foreach (var move in _dragged) if (!move.Element.IsDisposed && move.Element is not GraphFrame && !_parents.ContainsKey(move.Element.Name)) _requestNames[count++] = move.Element.Name; try { if (count > 0) GraphElementsLinkedToFrameRequest.Invoke(_requestNames.AsMemory(0, count), frame.Name); } finally { Array.Clear(_requestNames, 0, count); } break; }
        }
        _dragged.Clear(); _previousSelection.Clear(); _overlay.QueueRedraw();
    }
    private void CancelInteraction() { ForceConnectionDragEnd(); FinishPointer(); _panKey = false; }
    private void PanMinimap(Vector2 position)
    {
        var bounds = GraphBounds(); var map = MinimapRect; var scale = Math.Min(map.Size.X / bounds.Size.X, map.Size.Y / bounds.Size.Y); var origin = map.Position + (map.Size - bounds.Size * scale) / 2;
        UserScroll((bounds.Position + (position - origin) / scale) * _zoom - Size / 2);
    }
    private void WheelAt(InputEventMouseButton button, Vector2 position)
    {
        var up = button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelLeft; var vertical = button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown;
        var zoom = (_panningScheme == PanningSchemeMode.ScrollZooms) != (button.ControlPressed || button.MetaPressed);
        if (zoom && vertical) ZoomAt(_zoom * (up ? _zoomStep : 1 / _zoomStep), position); else UserScroll(_scroll + (vertical ? new Vector2(0, up ? -64 : 64) : new Vector2(up ? -64 : 64, 0)));
    }
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        base.OnGUIInput(inputEvent);
        if (inputEvent is InputEventKey key)
        {
            if (key.Keycode == Key.Space) { _panKey = key.Pressed; AcceptEvent(); return; }
            if (!key.Pressed || key.Echo) return;
            if (key.IsActionPressed("ui_cancel", true, true)) { CancelInteraction(); AcceptEvent(); return; }
            if (key.IsActionPressed("ui_graph_delete", true, true)) { DeleteSelected(); AcceptEvent(); return; }
            if (key.IsActionPressed("ui_copy", true, true)) { CopyNodesRequest?.Invoke(); AcceptEvent(); return; }
            if (key.IsActionPressed("ui_cut", true, true)) { CutNodesRequest?.Invoke(); AcceptEvent(); return; }
            if (key.IsActionPressed("ui_paste", true, true)) { PasteNodesRequest?.Invoke(); AcceptEvent(); return; }
            if (key.IsActionPressed("ui_graph_duplicate", true, true)) { DuplicateNodesRequest?.Invoke(); AcceptEvent(); return; }
            if (key.IsActionPressed("ui_text_select_all", true, true)) { for (var i = 0; i < _elements.Count; i++) _elements[i].Selected = true; AcceptEvent(); return; }
            if (key.Keycode is Key.Left or Key.Right or Key.Up or Key.Down)
            {
                var delta = key.Keycode switch { Key.Left => new Vector2(-1, 0), Key.Right => new Vector2(1, 0), Key.Up => new Vector2(0, -1), _ => new Vector2(0, 1) }; delta *= _snapping ? _snapDistance : key.ShiftPressed ? 10 : 1;
                _dragged.Clear(); foreach (var e in _elements) if (e.Selected && e.Draggable || SelectedFrameAncestor(e)) { _dragged.Add(new(e, e.PositionOffset)); e.StartGraphDrag(); }
                if (_dragged.Count > 0) { BeginNodeMove?.Invoke(); _updatingFrames = true; try { foreach (var move in _dragged) if (!move.Element.IsDisposed && move.Element.Parent == this) move.Element.PositionOffset = move.From + delta; } finally { _updatingFrames = false; UpdateFrames(); } try { foreach (var move in _dragged) if (!move.Element.IsDisposed) move.Element.EndGraphDrag(); } finally { _dragged.Clear(); EndNodeMove?.Invoke(); } }
                AcceptEvent();
            }
            return;
        }
        if (inputEvent is InputEventMouseMotion motion) { PointerMotion(motion.Position); if (_panning || _boxSelecting || _moving || _connectionNode != null || _minimapDragging) AcceptEvent(); return; }
        if (inputEvent is not InputEventMouseButton button) return;
        _pointer = button.Position;
        if (!button.Pressed) { if (button.ButtonIndex is MouseButton.Left or MouseButton.Middle) { FinishPointer(); AcceptEvent(); } return; }
        if (button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown or MouseButton.WheelLeft or MouseButton.WheelRight)
        {
            WheelAt(button, button.Position);
            AcceptEvent(); return;
        }
        if (button.ButtonIndex == MouseButton.Right) { PopupRequest?.Invoke(button.Position); AcceptEvent(); return; }
        if (FocusMode == FocusMode.All) GrabFocus();
        _pressPosition = button.Position;
        if (button.ButtonIndex == MouseButton.Middle || button.ButtonIndex == MouseButton.Left && _panKey) { _panning = true; _panScroll = _scroll; AcceptEvent(); return; }
        if (button.ButtonIndex != MouseButton.Left) return;
        if (_minimap && MinimapRect.HasPoint(button.Position)) { _minimapDragging = true; PanMinimap(button.Position); AcceptEvent(); return; }
        if (HitPort(button.Position, out var node, out var port, out var output)) { PressPort(node, port, output); AcceptEvent(); return; }
        _boxSelecting = true; _boxAdd = button.ControlPressed || button.ShiftPressed; _previousSelection.Clear(); foreach (var e in _elements) if (e.Selected) _previousSelection.Add(e); if (!_boxAdd) SetSelected(null); AcceptEvent(); _overlay.QueueRedraw();
    }
}
