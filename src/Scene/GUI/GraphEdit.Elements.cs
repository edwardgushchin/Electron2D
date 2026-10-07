namespace Electron2D;

public partial class GraphEdit
{
    private GraphElement? Element(string name) { foreach (var e in _elements) if (!e.IsDisposed && e.Parent == this && e.Name == name) return e; return null; }
    private void ElementAdded(Node _, Node child)
    {
        if (child is not GraphElement e) return;
        var binding = new Binding { Element = e, Name = e.Name, Z = e.ZIndex, OffsetValue = e.PositionOffset };
        binding.Offset = () => { if (!IsDisposed && e.Parent == this) { var delta = e.PositionOffset - binding.OffsetValue; binding.OffsetValue = e.PositionOffset; if (e is GraphFrame && !_updatingFrames) { _updatingFrames = true; try { for (var i = 0; i < _elements.Count; i++) if (IsAttachedTo(_elements[i], e)) _elements[i].PositionOffset += delta; } finally { _updatingFrames = false; } } ProjectElement(e); UpdateFrames(); _lines.QueueRedraw(); _overlay.QueueRedraw(); } };
        binding.Resize = () => { if (!IsDisposed && e.Parent == this) { UpdateFrames(); _lines.QueueRedraw(); _overlay.QueueRedraw(); } };
        binding.Select = () => NodeSelected?.Invoke(e); binding.Deselect = () => NodeDeselected?.Invoke(e);
        binding.Raise = () => { if (!IsDisposed && e.Parent == this) MoveChild(e, GetChildCount() - 1); };
        binding.Shrink = UpdateFrames;
        binding.Rename = _ => RenameElement(binding);
        _elements.Add(e); _bindings.Add(e, binding); _dragged.EnsureCapacity(_elements.Count); _previousSelection.EnsureCapacity(_elements.Count); PrepareRequests(_elements.Count);
        e.ZIndex = e is GraphFrame ? 0 : 2;
        e.PositionOffsetChanged += binding.Offset; e.Resized += binding.Resize; e.NodeSelected += binding.Select; e.NodeDeselected += binding.Deselect; e.RaiseRequest += binding.Raise; e.Renamed += binding.Rename;
        if (e is GraphNode node) node.SlotSizesChanged += binding.Resize;
        if (e is GraphFrame frame) frame.AutoshrinkChanged += binding.Shrink;
        ProjectElement(e); UpdateFrames(); ChangedGraph();
    }
    private void ElementRemoved(Node _, Node child)
    {
        if (child is not GraphElement e || !_bindings.Remove(e, out var b)) return;
        e.PositionOffsetChanged -= b.Offset; e.Resized -= b.Resize; e.NodeSelected -= b.Select; e.NodeDeselected -= b.Deselect; e.RaiseRequest -= b.Raise; e.Renamed -= b.Rename;
        if (e is GraphNode node) node.SlotSizesChanged -= b.Resize; if (e is GraphFrame frame) frame.AutoshrinkChanged -= b.Shrink;
        _elements.Remove(e); _parents.Remove(b.Name);
        foreach (var member in _elements) if (_parents.GetValueOrDefault(member.Name) == b.Name) _parents.Remove(member.Name);
        if (!e.IsDisposed) e.ZIndex = b.Z;
        if (!_releasing) { UpdateFrames(); ChangedGraph(); }
    }
    private void RenameElement(Binding b, bool refresh = true)
    {
        var old = b.Name; var name = b.Element.Name; b.Name = name;
        if (_parents.Remove(old, out var parent)) _parents[name] = parent;
        foreach (var e in _elements) if (_parents.GetValueOrDefault(e.Name) == old) _parents[e.Name] = name;
        foreach (var c in _connections) { if (c.Value.FromNode == old) c.Value = c.Value with { FromNode = name }; if (c.Value.ToNode == old) c.Value = c.Value with { ToNode = name }; c.Dirty = true; }
        if (refresh) ChangedGraph();
    }
    private void ProjectElement(GraphElement e)
    {
        if (e.IsDisposed || e.Parent != this) return;
        var position = e.PositionOffset * _zoom - _scroll;
        if (!position.IsFinite()) throw new InvalidOperationException("Graph projection exceeded finite geometry.");
        e.Position = position; e.Scale = new(_zoom, _zoom); e.RefreshGraphMenus();
    }
    private void ProjectElements() { foreach (var e in _elements) ProjectElement(e); }
    /// <summary>Selects one direct graph element, or clears selection with null.</summary><param name="node">A direct graph element or null.</param>
    public void SetSelected(Node? node)
    {
        MutableEdit(); if (node != null && (node is not GraphElement || node.Parent != this)) throw new ArgumentException("Selection requires a direct graph element.", nameof(node));
        for (var i = 0; i < _elements.Count; i++) _elements[i].Selected = ReferenceEquals(_elements[i], node);
    }
    /// <summary>Attaches an element logically without reparenting its scene node.</summary><param name="element">Direct element name.</param><param name="frame">Direct frame name.</param>
    public void AttachGraphElementToFrame(string element, string frame)
    {
        MutableEdit(); if (Element(element) is null || Element(frame) is not GraphFrame) throw new ArgumentException("Both graph elements must exist and the parent must be a frame.");
        var ancestor = frame;
        for (var i = 0; i <= _elements.Count; i++) { if (ancestor == element) throw new ArgumentException("Frame attachments cannot form cycles."); if (!_parents.TryGetValue(ancestor, out ancestor!)) break; }
        _parents[element] = frame; UpdateFrames(); ChangedGraph();
    }
    /// <summary>Removes an element's logical attachment.</summary><param name="element">Element name.</param>
    public void DetachGraphElementFromFrame(string element) { MutableEdit(); if (_parents.Remove(element)) { UpdateFrames(); ChangedGraph(); } }
    /// <summary>Finds the borrowed logical parent frame.</summary><param name="element">Element name.</param><returns>Frame or null.</returns>
    public GraphFrame? GetElementFrame(string element) { CheckEdit(); return _parents.TryGetValue(element, out var frame) ? Element(frame) as GraphFrame : null; }
    /// <summary>Copies the names directly attached to a frame.</summary><param name="frame">Frame name.</param><returns>Caller-owned name snapshot.</returns>
    public string[] GetAttachedNodesOfFrame(string frame) { CheckEdit(); return _elements.Where(e => _parents.GetValueOrDefault(e.Name) == frame).Select(e => e.Name).ToArray(); }
    private void UpdateFrames()
    {
        if (_updatingFrames || _releasing) return; _updatingFrames = true;
        try
        {
            for (var depth = _elements.Count; depth >= 0; depth--)
                for (var i = 0; i < _elements.Count; i++) if (_elements[i] is GraphFrame frame && AttachmentDepth(frame.Name) == depth)
                    {
                        var has = false; var bounds = new Rect2();
                        foreach (var e in _elements) if (_parents.GetValueOrDefault(e.Name) == frame.Name) { var rect = new Rect2(e.PositionOffset, e.Size); bounds = has ? bounds.Merge(rect) : rect; has = true; }
                        if (!has) continue;
                        bounds = bounds.Grow(frame.AutoshrinkMargin); bounds.Position -= new Vector2(0, frame.GraphTitleHeight); bounds.Size += new Vector2(0, frame.GraphTitleHeight);
                        var old = new Rect2(frame.PositionOffset, frame.Size);
                        if (!frame.AutoshrinkEnabled) bounds = bounds.Merge(old);
                        if (bounds == old) continue;
                        frame.PositionOffset = bounds.Position; frame.Size = bounds.Size; ProjectElement(frame); FrameRectChanged?.Invoke(frame, frame.GetRect());
                    }
        }
        finally { _updatingFrames = false; }
    }
    private int AttachmentDepth(string name) { var depth = 0; while (_parents.TryGetValue(name, out name!)) { if (++depth > _elements.Count) throw new InvalidOperationException("Cyclic graph frame attachments."); } return depth; }
    private bool IsAttachedTo(GraphElement e, GraphElement ancestor) { var name = e.Name; for (var i = 0; i < _elements.Count && _parents.TryGetValue(name, out name!); i++) if (name == ancestor.Name) return true; return false; }
    /// <summary>Places the selected nodes, or all nodes when none are selected, in stable directed layers.</summary>
    /// <remarks>Cycles share a bounded final layer; graph-name order provides deterministic placement.</remarks>
    public void ArrangeNodes()
    {
        MutableEdit(); var selected = _elements.Any(e => e.Selected); var nodes = _elements.OfType<GraphNode>().Where(e => !selected || e.Selected).OrderBy(e => e.Name, StringComparer.Ordinal).ToArray(); if (nodes.Length == 0) return;
        var levels = new int[nodes.Length];
        for (var pass = 0; pass < nodes.Length - 1; pass++) foreach (var c in _connections) { var from = Array.FindIndex(nodes, n => n.Name == c.Value.FromNode); var to = Array.FindIndex(nodes, n => n.Name == c.Value.ToNode); if (from >= 0 && to >= 0 && from != to) levels[to] = Math.Min(nodes.Length - 1, Math.Max(levels[to], levels[from] + 1)); }
        var widths = new float[nodes.Length]; var heights = new float[nodes.Length]; var x = new float[nodes.Length]; var origin = nodes[0].PositionOffset;
        for (var i = 0; i < nodes.Length; i++) { widths[levels[i]] = Math.Max(widths[levels[i]], nodes[i].Size.X); origin = origin.Min(nodes[i].PositionOffset); }
        for (var i = 1; i < x.Length; i++) x[i] = x[i - 1] + widths[i - 1] + 80;
        BeginNodeMove?.Invoke();
        try { for (var i = 0; i < nodes.Length; i++) { var n = nodes[i]; n.StartGraphDrag(); n.PositionOffset = origin + new Vector2(x[levels[i]], heights[levels[i]]); heights[levels[i]] += n.Size.Y + 40; n.EndGraphDrag(); } UpdateFrames(); }
        finally { EndNodeMove?.Invoke(); }
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(GraphEdit) ? CreateGraphEdit : base.CreateSceneInstanceFactory();
    private static Node CreateGraphEdit() => new GraphEdit();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _releasing = true; ChildAdded -= ElementAdded;
            while (_elements.Count > 0) ElementRemoved(this, _elements[^1]);
            ChildRemoved -= ElementRemoved; _connections.Clear(); _parents.Clear();
            BeginNodeMove = null; EndNodeMove = null; ConnectionDragStarted = null; ConnectionDragEnded = null; ConnectionFromEmpty = null; ConnectionToEmpty = null; ConnectionRequest = null; DisconnectionRequest = null; CopyNodesRequest = null; CutNodesRequest = null; PasteNodesRequest = null; DuplicateNodesRequest = null; DeleteNodesRequest = null; FrameRectChanged = null; GraphElementsLinkedToFrameRequest = null; NodeSelected = null; NodeDeselected = null; PopupRequest = null; ScrollOffsetChanged = null;
        }
        base.Dispose(disposing);
    }
}
