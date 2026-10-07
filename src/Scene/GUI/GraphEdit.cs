namespace Electron2D;

/// <summary>Describes a directed graph connection independently of scene or resource ownership.</summary>
/// <param name="FromNode">Output node name.</param><param name="FromPort">Compressed output port.</param><param name="ToNode">Input node name.</param><param name="ToPort">Compressed input port.</param><param name="KeepAlive">Retain the record while an endpoint is missing.</param>
public readonly record struct GraphConnection(string FromNode, int FromPort, string ToNode, int ToPort, bool KeepAlive = false);

/// <summary>Edits positioned graph elements, directed port connections and logical grouping frames.</summary>
/// <remarks>Interactive connection and deletion gestures issue requests. Applications explicitly mutate
/// Connections or scene children. Snapshot getters allocate outside the repeated pointer/render path.</remarks>
public partial class GraphEdit : Control
{
    /// <summary>Selects the displayed graph grid.</summary>
    public enum GridPatternMode
    {
        /// <summary>Draws grid lines.</summary>
        Lines = 0,
        /// <summary>Draws grid dots.</summary>
        Dots = 1
    }
    /// <summary>Selects the wheel's unmodified navigation operation.</summary>
    public enum PanningSchemeMode
    {
        /// <summary>Wheel zooms; modified wheel pans.</summary>
        ScrollZooms = 0,
        /// <summary>Wheel pans; modified wheel zooms.</summary>
        ScrollPans = 1
    }
    private sealed class Connection(GraphConnection value)
    {
        internal GraphConnection Value = value;
        internal float Activity;
        internal readonly Vector2[] Points = new Vector2[65];
        internal Vector2 From, To;
        internal Vector2[]? CustomPoints;
        internal bool Valid, Dirty = true;
        internal Color FromColor, ToColor;
    }
    private sealed class Binding
    {
        internal GraphElement Element = null!;
        internal string Name = "";
        internal int Z;
        internal Vector2 OffsetValue;
        internal Action Offset = null!, Resize = null!, Select = null!, Deselect = null!, Raise = null!, Shrink = null!;
        internal Action<Node> Rename = null!;
    }
    private readonly List<GraphElement> _elements = [];
    private readonly Dictionary<GraphElement, Binding> _bindings = [];
    private readonly List<Connection> _connections = [];
    private readonly Dictionary<string, string> _parents = new(StringComparer.Ordinal);
    private readonly HashSet<(int From, int To)> _validTypes = [];
    private readonly HashSet<int> _leftDisconnect = [], _rightDisconnect = [];
    private Dictionary<int, string> _typeNames = [];
    private readonly OwnedLayer _lines, _overlay;
    private readonly OwnedMenu _menu;
    private readonly Label _zoomLabel;
    private readonly Button _zoomOut, _zoomIn, _zoomReset, _snapButton, _gridButton, _minimapButton, _arrangeButton;
    private bool _releasing, _updatingFrames, _customLine, _drawingEdit;
    private void MutableEdit() { EnsureMutable(); if (_drawingEdit) throw new InvalidOperationException("Connection geometry/drawing cannot mutate its graph."); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (_drawingEdit) throw new InvalidOperationException("Connection geometry/drawing cannot dispose its graph."); base.ValidateDisposal(); }
    private Vector2 _scroll;
    private float _zoom = 1, _zoomMin = .23256795f, _zoomMax = 2.0736003f, _zoomStep = 1.2f, _curvature = .5f, _thickness = 4, _minimapOpacity = .65f;
    private Vector2 _minimapSize = new(240, 160);
    private int _snapDistance = 20;
    private bool _antialiased = true, _rightDisconnects, _snapping = true, _showGrid = true, _minimap = true;
    private bool _showMenu = true, _showZoomButtons = true, _showZoomLabel, _showGridButtons = true, _showMinimapButton = true, _showArrangeButton = true;
    private GridPatternMode _gridPattern;
    private PanningSchemeMode _panningScheme;
    private void CheckEdit() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); for (var i = 0; i < _elements.Count; i++) { var e = _elements[i]; if (_bindings.TryGetValue(e, out var b) && b.Name != e.Name) RenameElement(b, false); } }
    private void ChangedGraph() { ProjectElements(); _lines.QueueRedraw(); _overlay.QueueRedraw(); QueueRedraw(); }
    /// <summary>Creates a clipped graph editor with owned navigation controls and drawing layers.</summary>
    public GraphEdit()
    {
        FocusMode = FocusMode.All; ClipContents = true; MouseFilter = MouseFilter.Stop;
        _customLine = GetType().GetMethod(nameof(OnGetConnectionLine), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.DeclaringType != typeof(GraphEdit);
        _lines = new(this, false) { Name = "_graph_connections", ZIndex = 1, MouseFilter = MouseFilter.Ignore };
        _overlay = new(this, true) { Name = "_graph_overlay", ZIndex = 3, MouseFilter = MouseFilter.Pass };
        _menu = new(this) { Name = "_graph_menu", ZIndex = 4 }; _zoomLabel = new Label { Text = "100%", CustomMinimumSize = new(48, 0), Visible = false };
        _menu.AddChild(_zoomLabel);
        Button Add(string name, string tooltip, Action action, bool toggle = false) { var button = new Button { Name = name, TooltipText = tooltip, ToggleMode = toggle, FocusMode = FocusMode.All }; button.Pressed += action; _menu.AddChild(button); return button; }
        _zoomOut = Add("ZoomOut", "Zoom out", () => Zoom = Math.Max(ZoomMin, Zoom / ZoomStep));
        _zoomReset = Add("ZoomReset", "Reset zoom", () => Zoom = 1); _zoomIn = Add("ZoomIn", "Zoom in", () => Zoom = Math.Min(ZoomMax, Zoom * ZoomStep));
        _snapButton = Add("Snapping", "Toggle snapping", () => SnappingEnabled = !SnappingEnabled, true);
        _gridButton = Add("Grid", "Toggle grid", () => ShowGrid = !ShowGrid, true);
        _minimapButton = Add("Minimap", "Toggle minimap", () => MinimapEnabled = !MinimapEnabled, true);
        _arrangeButton = Add("Arrange", "Arrange nodes", ArrangeNodes);
        AddChild(_lines, InternalMode.Front); AddChild(_overlay, InternalMode.Back); AddChild(_menu, InternalMode.Back);
        ChildAdded += ElementAdded; ChildRemoved += ElementRemoved; RefreshMenu();
    }
    private sealed class OwnedLayer(GraphEdit owner, bool overlay) : Control
    {
        protected override bool HasPoint(Vector2 point) => overlay && owner.OverlayHit(point);
        protected override void OnGUIInput(InputEvent inputEvent) { owner.OnGUIInput(inputEvent); }
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationDraw) { owner._drawingEdit = true; try { if (overlay) owner.DrawOverlay(this); else owner.DrawConnections(this); } finally { owner._drawingEdit = false; } } }
        protected override void ValidateDisposal() { if (!owner.IsDisposed && !owner._releasing) throw new InvalidOperationException("GraphEdit owns its drawing layers."); base.ValidateDisposal(); }
    }
    private sealed class OwnedMenu(GraphEdit owner) : HBoxContainer
    {
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationDraw && owner.GetThemeStyleBox("menu_panel") is { } style) DrawStyleBox(style, new(-style.GetOffset(), Size + style.GetMinimumSize())); }
        protected override void ValidateDisposal() { if (!owner.IsDisposed && !owner._releasing) throw new InvalidOperationException("GraphEdit owns its menu."); base.ValidateDisposal(); }
    }
    /// <summary>Returns the borrowed menu container for application controls.</summary><returns>The owned horizontal menu.</returns>
    public HBoxContainer GetMenuHBox() { CheckEdit(); return _menu; }
    /// <summary>Gets or replaces the directed connection snapshot.</summary>
    public GraphConnection[] Connections
    {
        get { CheckEdit(); var result = new GraphConnection[_connections.Count]; for (var i = 0; i < result.Length; i++) result[i] = _connections[i].Value; return result; }
        set { MutableEdit(); ArgumentNullException.ThrowIfNull(value); var prepared = new List<Connection>(value.Length); var unique = new HashSet<(string, int, string, int)>(); foreach (var connection in value) { ValidateConnection(connection); if (!unique.Add((connection.FromNode, connection.FromPort, connection.ToNode, connection.ToPort))) throw new ArgumentException("Duplicate graph connection.", nameof(value)); prepared.Add(new(connection)); } _connections.Clear(); _connections.AddRange(prepared); ChangedGraph(); }
    }
    private static void ValidateConnection(GraphConnection value)
    { if (string.IsNullOrEmpty(value.FromNode) || string.IsNullOrEmpty(value.ToNode) || value.FromPort < 0 || value.ToPort < 0) throw new ArgumentException("A graph connection requires names and nonnegative port indices."); }
    private Connection? FindConnection(string from, int fromPort, string to, int toPort)
    { foreach (var c in _connections) if (c.Value.FromNode == from && c.Value.FromPort == fromPort && c.Value.ToNode == to && c.Value.ToPort == toPort) return c; return null; }
    /// <summary>Adds a directed record without applying interactive type policy.</summary><param name="fromNode">Output node name.</param><param name="fromPort">Compressed output index.</param><param name="toNode">Input node name.</param><param name="toPort">Compressed input index.</param><param name="keepAlive">Retain missing endpoints.</param><remarks>Already present records are unchanged; invalid arguments throw typed exceptions.</remarks>
    public void ConnectNode(string fromNode, int fromPort, string toNode, int toPort, bool keepAlive = false)
    { MutableEdit(); var value = new GraphConnection(fromNode, fromPort, toNode, toPort, keepAlive); ValidateConnection(value); if (FindConnection(fromNode, fromPort, toNode, toPort) != null) return; _connections.Add(new(value)); ChangedGraph(); return; }
    /// <summary>Removes the named directed record if present.</summary><param name="fromNode">Output name.</param><param name="fromPort">Output index.</param><param name="toNode">Input name.</param><param name="toPort">Input index.</param>
    public void DisconnectNode(string fromNode, int fromPort, string toNode, int toPort) { MutableEdit(); var c = FindConnection(fromNode, fromPort, toNode, toPort); if (c != null) { _connections.Remove(c); ChangedGraph(); } }
    /// <summary>Clears connection records without removing graph elements.</summary>
    public void ClearConnections() { MutableEdit(); _connections.Clear(); ChangedGraph(); }
    /// <summary>Tests whether a directed record exists.</summary><param name="fromNode">Output name.</param><param name="fromPort">Output index.</param><param name="toNode">Input name.</param><param name="toPort">Input index.</param><returns>Whether it exists.</returns>
    public bool IsNodeConnected(string fromNode, int fromPort, string toNode, int toPort) { CheckEdit(); return FindConnection(fromNode, fromPort, toNode, toPort) != null; }
    /// <summary>Counts records incident on the named node and port.</summary><param name="fromNode">Endpoint name.</param><param name="fromPort">Endpoint index.</param><returns>Incident record count.</returns>
    public int GetConnectionCount(string fromNode, int fromPort) { CheckEdit(); var count = 0; foreach (var c in _connections) if (c.Value.FromNode == fromNode && c.Value.FromPort == fromPort || c.Value.ToNode == fromNode && c.Value.ToPort == fromPort) count++; return count; }
    /// <summary>Copies connection records incident on a node.</summary><param name="node">Node name.</param><returns>Caller-owned snapshot.</returns>
    public GraphConnection[] GetConnectionListFromNode(string node) { CheckEdit(); return _connections.Where(c => c.Value.FromNode == node || c.Value.ToNode == node).Select(c => c.Value).ToArray(); }
    /// <summary>Assigns a connection's finite activity blend toward the activity theme color.</summary><param name="fromNode">Output name.</param><param name="fromPort">Output index.</param><param name="toNode">Input name.</param><param name="toPort">Input index.</param><param name="amount">Finite blend clamped to zero through one.</param>
    public void SetConnectionActivity(string fromNode, int fromPort, string toNode, int toPort, float amount) { MutableEdit(); if (!float.IsFinite(amount)) throw new ArgumentOutOfRangeException(nameof(amount)); var c = FindConnection(fromNode, fromPort, toNode, toPort); if (c is null) return; c.Activity = Math.Clamp(amount, 0, 1); _lines.QueueRedraw(); }
    /// <summary>Adds an interactive left disconnect type.</summary><param name="type">Port type identifier.</param>
    public void AddValidLeftDisconnectType(int type) { MutableEdit(); _leftDisconnect.Add(type); }
    /// <summary>Adds an interactive right disconnect type.</summary><param name="type">Port type identifier.</param>
    public void AddValidRightDisconnectType(int type) { MutableEdit(); _rightDisconnect.Add(type); }
    /// <summary>Adds an allowed directed pair of port types.</summary><param name="fromType">Output type.</param><param name="toType">Input type.</param>
    public void AddValidConnectionType(int fromType, int toType) { MutableEdit(); _validTypes.Add((fromType, toType)); }
    /// <summary>Removes an interactive left disconnect type.</summary><param name="type">Port type identifier.</param>
    public void RemoveValidLeftDisconnectType(int type) { MutableEdit(); _leftDisconnect.Remove(type); }
    /// <summary>Removes an interactive right disconnect type.</summary><param name="type">Port type identifier.</param>
    public void RemoveValidRightDisconnectType(int type) { MutableEdit(); _rightDisconnect.Remove(type); }
    /// <summary>Removes an allowed directed pair of port types.</summary><param name="fromType">Output type.</param><param name="toType">Input type.</param>
    public void RemoveValidConnectionType(int fromType, int toType) { MutableEdit(); _validTypes.Remove((fromType, toType)); }
    /// <summary>Tests an explicitly registered directed pair; equal types are accepted automatically by interactive wiring.</summary><param name="fromType">Output type.</param><param name="toType">Input type.</param><returns>Whether explicitly registered.</returns>
    public bool IsValidConnectionType(int fromType, int toType) { CheckEdit(); return _validTypes.Contains((fromType, toType)); }
    /// <summary>Reports the BeginNodeMove graph interaction.</summary>
    public event Action? BeginNodeMove;
    /// <summary>Reports the EndNodeMove graph interaction.</summary>
    public event Action? EndNodeMove;
    /// <summary>Reports the ConnectionDragStarted graph interaction.</summary>
    public event Action<string, int, bool>? ConnectionDragStarted;
    /// <summary>Reports the ConnectionDragEnded graph interaction.</summary>
    public event Action? ConnectionDragEnded;
    /// <summary>Reports the ConnectionFromEmpty graph interaction.</summary>
    public event Action<string, int, Vector2>? ConnectionFromEmpty;
    /// <summary>Reports the ConnectionToEmpty graph interaction.</summary>
    public event Action<string, int, Vector2>? ConnectionToEmpty;
    /// <summary>Reports the ConnectionRequest graph interaction.</summary>
    public event Action<string, int, string, int>? ConnectionRequest;
    /// <summary>Reports the DisconnectionRequest graph interaction.</summary>
    public event Action<string, int, string, int>? DisconnectionRequest;
    /// <summary>Reports the CopyNodesRequest graph interaction.</summary>
    public event Action? CopyNodesRequest;
    /// <summary>Reports the CutNodesRequest graph interaction.</summary>
    public event Action? CutNodesRequest;
    /// <summary>Reports the PasteNodesRequest graph interaction.</summary>
    public event Action? PasteNodesRequest;
    /// <summary>Reports the DuplicateNodesRequest graph interaction.</summary>
    public event Action? DuplicateNodesRequest;
    /// <summary>Reports the DeleteNodesRequest graph interaction.</summary>
    public event Action<ReadOnlyMemory<string>>? DeleteNodesRequest;
    /// <summary>Reports the FrameRectChanged graph interaction.</summary>
    public event Action<GraphFrame, Rect2>? FrameRectChanged;
    /// <summary>Reports the GraphElementsLinkedToFrameRequest graph interaction.</summary>
    public event Action<ReadOnlyMemory<string>, string>? GraphElementsLinkedToFrameRequest;
    /// <summary>Reports the NodeSelected graph interaction.</summary>
    public event Action<Node>? NodeSelected;
    /// <summary>Reports the NodeDeselected graph interaction.</summary>
    public event Action<Node>? NodeDeselected;
    /// <summary>Reports the PopupRequest graph interaction.</summary>
    public event Action<Vector2>? PopupRequest;
    /// <summary>Reports the ScrollOffsetChanged graph interaction.</summary>
    public event Action<Vector2>? ScrollOffsetChanged;
}
