namespace Electron2D;

public partial class GraphEdit
{
    private static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    /// <summary>Gets or sets the graph ConnectionLinesAntialiased policy.</summary><value>Initially true.</value>
    public bool ConnectionLinesAntialiased { get { CheckEdit(); return _antialiased; } set { MutableEdit(); if (_antialiased == value) return; _antialiased = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph ConnectionLinesCurvature policy.</summary><value>Initially .5f.</value>
    public float ConnectionLinesCurvature { get { CheckEdit(); return _curvature; } set { MutableEdit(); Finite(value); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_curvature == value) return; _curvature = value; foreach (var c in _connections) c.Dirty = true; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph ConnectionLinesThickness policy.</summary><value>Initially 4.</value>
    public float ConnectionLinesThickness { get { CheckEdit(); return _thickness; } set { MutableEdit(); Finite(value); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_thickness == value) return; _thickness = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph GridPattern policy.</summary><value>Initially GridPatternMode.Lines.</value>
    public GridPatternMode GridPattern { get { CheckEdit(); return _gridPattern; } set { MutableEdit(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_gridPattern == value) return; _gridPattern = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph PanningScheme policy.</summary><value>Initially PanningSchemeMode.ScrollZooms.</value>
    public PanningSchemeMode PanningScheme { get { CheckEdit(); return _panningScheme; } set { MutableEdit(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_panningScheme == value) return; _panningScheme = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph MinimapEnabled policy.</summary><value>Initially true.</value>
    public bool MinimapEnabled { get { CheckEdit(); return _minimap; } set { MutableEdit(); if (_minimap == value) return; _minimap = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph MinimapOpacity policy.</summary><value>Initially .65f.</value>
    public float MinimapOpacity { get { CheckEdit(); return _minimapOpacity; } set { MutableEdit(); Finite(value); if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); if (_minimapOpacity == value) return; _minimapOpacity = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph MinimapSize policy.</summary><value>Initially new(240,160).</value>
    public Vector2 MinimapSize { get { CheckEdit(); return _minimapSize; } set { MutableEdit(); if (!value.IsFinite() || value.X <= 0 || value.Y <= 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_minimapSize == value) return; _minimapSize = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph RightDisconnects policy.</summary><value>Initially false.</value>
    public bool RightDisconnects { get { CheckEdit(); return _rightDisconnects; } set { MutableEdit(); if (_rightDisconnects == value) return; _rightDisconnects = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph SnappingDistance policy.</summary><value>Initially 20.</value>
    public int SnappingDistance { get { CheckEdit(); return _snapDistance; } set { MutableEdit(); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_snapDistance == value) return; _snapDistance = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph SnappingEnabled policy.</summary><value>Initially true.</value>
    public bool SnappingEnabled { get { CheckEdit(); return _snapping; } set { MutableEdit(); if (_snapping == value) return; _snapping = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph ShowArrangeButton policy.</summary><value>Initially true.</value>
    public bool ShowArrangeButton { get { CheckEdit(); return _showArrangeButton; } set { MutableEdit(); if (_showArrangeButton == value) return; _showArrangeButton = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph ShowGrid policy.</summary><value>Initially true.</value>
    public bool ShowGrid { get { CheckEdit(); return _showGrid; } set { MutableEdit(); if (_showGrid == value) return; _showGrid = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph ShowGridButtons policy.</summary><value>Initially true.</value>
    public bool ShowGridButtons { get { CheckEdit(); return _showGridButtons; } set { MutableEdit(); if (_showGridButtons == value) return; _showGridButtons = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph ShowMenu policy.</summary><value>Initially true.</value>
    public bool ShowMenu { get { CheckEdit(); return _showMenu; } set { MutableEdit(); if (_showMenu == value) return; _showMenu = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph ShowMinimapButton policy.</summary><value>Initially true.</value>
    public bool ShowMinimapButton { get { CheckEdit(); return _showMinimapButton; } set { MutableEdit(); if (_showMinimapButton == value) return; _showMinimapButton = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph ShowZoomButtons policy.</summary><value>Initially true.</value>
    public bool ShowZoomButtons { get { CheckEdit(); return _showZoomButtons; } set { MutableEdit(); if (_showZoomButtons == value) return; _showZoomButtons = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph ShowZoomLabel policy.</summary><value>Initially false.</value>
    public bool ShowZoomLabel { get { CheckEdit(); return _showZoomLabel; } set { MutableEdit(); if (_showZoomLabel == value) return; _showZoomLabel = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets the graph ZoomStep policy.</summary><value>Initially 1.2f.</value>
    public float ZoomStep { get { CheckEdit(); return _zoomStep; } set { MutableEdit(); Finite(value); if (value <= 1) throw new ArgumentOutOfRangeException(nameof(value)); if (_zoomStep == value) return; _zoomStep = value; RefreshMenu(); ChangedGraph(); } }
    /// <summary>Gets or sets finite viewport scroll in scaled graph pixels, without emitting the user-scroll event.</summary>
    public Vector2 ScrollOffset { get { CheckEdit(); return _scroll; } set { MutableEdit(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); if (_scroll == value) return; _scroll = value; ChangedGraph(); } }
    /// <summary>Gets or sets zoom around the viewport center, clamped to ZoomMin and ZoomMax.</summary>
    public float Zoom { get { CheckEdit(); return _zoom; } set { MutableEdit(); Finite(value); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); ZoomAt(value, Size / 2); } }
    /// <summary>Gets or sets the positive minimum zoom, no greater than ZoomMax.</summary>
    public float ZoomMin { get { CheckEdit(); return _zoomMin; } set { MutableEdit(); Finite(value); if (value <= 0 || value > _zoomMax) throw new ArgumentOutOfRangeException(nameof(value)); _zoomMin = value; Zoom = _zoom; } }
    /// <summary>Gets or sets the positive maximum zoom, no smaller than ZoomMin.</summary>
    public float ZoomMax { get { CheckEdit(); return _zoomMax; } set { MutableEdit(); Finite(value); if (value < _zoomMin) throw new ArgumentOutOfRangeException(nameof(value)); _zoomMax = value; Zoom = _zoom; } }
    /// <summary>Gets or replaces a copied map of integer port type names.</summary>
    public Dictionary<int, string> TypeNames { get { CheckEdit(); return new(_typeNames); } set { MutableEdit(); ArgumentNullException.ThrowIfNull(value); if (value.Values.Any(v => v is null)) throw new ArgumentException("Type names cannot be null.", nameof(value)); _typeNames = new(value); } }
    private void ZoomAt(float value, Vector2 center)
    { value = Math.Clamp(value, _zoomMin, _zoomMax); if (value == _zoom) return; var scroll = (_scroll + center) * (value / _zoom) - center; if (!scroll.IsFinite()) throw new InvalidOperationException("Graph zoom exceeded finite geometry."); _scroll = scroll; _zoom = value; RefreshMenu(); ChangedGraph(); }
    private void UserScroll(Vector2 value) { ScrollOffset = value; ScrollOffsetChanged?.Invoke(_scroll); }
    private void RefreshMenu()
    {
        if (_menu is null || _arrangeButton is null) return;
        _menu.Visible = _showMenu; _zoomLabel.Visible = _showZoomLabel;
        if (_showZoomLabel) _zoomLabel.Text = MathF.Round(_zoom * 100).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
        _zoomOut.Visible = _zoomIn.Visible = _zoomReset.Visible = _showZoomButtons;
        _gridButton.Visible = _snapButton.Visible = _showGridButtons; _minimapButton.Visible = _showMinimapButton; _arrangeButton.Visible = _showArrangeButton;
        _snapButton.SetPressedNoSignal(_snapping); _gridButton.SetPressedNoSignal(_showGrid); _minimapButton.SetPressedNoSignal(_minimap);
        _zoomOut.Icon = GetThemeIcon("zoom_out"); _zoomIn.Icon = GetThemeIcon("zoom_in"); _zoomReset.Icon = GetThemeIcon("zoom_reset"); _snapButton.Icon = GetThemeIcon("snapping_toggle"); _gridButton.Icon = GetThemeIcon("grid_toggle"); _minimapButton.Icon = GetThemeIcon("minimap_toggle"); _arrangeButton.Icon = GetThemeIcon("layout");
        _menu.Position = new(10, 10); _menu.Size = _menu.GetCombinedMinimumSize();
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var p in base.GetPropertyDescriptors())
        { if (p.Name == nameof(FocusMode)) yield return new PropertyDescriptor<GraphEdit, FocusMode>(p.Name, n => n.FocusMode, (n, v) => n.FocusMode = v, _ => FocusMode.All, stored: true); else if (p.Name == nameof(ClipContents)) yield return new PropertyDescriptor<GraphEdit, bool>(p.Name, n => n.ClipContents, (n, v) => n.ClipContents = v, _ => true, stored: true); else yield return p; }
        yield return new PropertyDescriptor<GraphEdit, bool>(nameof(ConnectionLinesAntialiased), n => n.ConnectionLinesAntialiased, (n, v) => n.ConnectionLinesAntialiased = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphEdit, float>(nameof(ConnectionLinesCurvature), n => n.ConnectionLinesCurvature, (n, v) => n.ConnectionLinesCurvature = v, _ => .5f, stored: true);
        yield return new PropertyDescriptor<GraphEdit, float>(nameof(ConnectionLinesThickness), n => n.ConnectionLinesThickness, (n, v) => n.ConnectionLinesThickness = v, _ => 4, stored: true);
        yield return new PropertyDescriptor<GraphEdit, GridPatternMode>(nameof(GridPattern), n => n.GridPattern, (n, v) => n.GridPattern = v, _ => GridPatternMode.Lines, stored: true);
        yield return new PropertyDescriptor<GraphEdit, PanningSchemeMode>(nameof(PanningScheme), n => n.PanningScheme, (n, v) => n.PanningScheme = v, _ => PanningSchemeMode.ScrollZooms, stored: true);
        yield return new PropertyDescriptor<GraphEdit, bool>(nameof(MinimapEnabled), n => n.MinimapEnabled, (n, v) => n.MinimapEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphEdit, float>(nameof(MinimapOpacity), n => n.MinimapOpacity, (n, v) => n.MinimapOpacity = v, _ => .65f, stored: true);
        yield return new PropertyDescriptor<GraphEdit, Vector2>(nameof(MinimapSize), n => n.MinimapSize, (n, v) => n.MinimapSize = v, _ => new(240, 160), stored: true);
        yield return new PropertyDescriptor<GraphEdit, bool>(nameof(RightDisconnects), n => n.RightDisconnects, (n, v) => n.RightDisconnects = v, _ => false, stored: true);
        yield return new PropertyDescriptor<GraphEdit, int>(nameof(SnappingDistance), n => n.SnappingDistance, (n, v) => n.SnappingDistance = v, _ => 20, stored: true);
        yield return new PropertyDescriptor<GraphEdit, bool>(nameof(SnappingEnabled), n => n.SnappingEnabled, (n, v) => n.SnappingEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphEdit, bool>(nameof(ShowArrangeButton), n => n.ShowArrangeButton, (n, v) => n.ShowArrangeButton = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphEdit, bool>(nameof(ShowGrid), n => n.ShowGrid, (n, v) => n.ShowGrid = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphEdit, bool>(nameof(ShowGridButtons), n => n.ShowGridButtons, (n, v) => n.ShowGridButtons = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphEdit, bool>(nameof(ShowMenu), n => n.ShowMenu, (n, v) => n.ShowMenu = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphEdit, bool>(nameof(ShowMinimapButton), n => n.ShowMinimapButton, (n, v) => n.ShowMinimapButton = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphEdit, bool>(nameof(ShowZoomButtons), n => n.ShowZoomButtons, (n, v) => n.ShowZoomButtons = v, _ => true, stored: true);
        yield return new PropertyDescriptor<GraphEdit, bool>(nameof(ShowZoomLabel), n => n.ShowZoomLabel, (n, v) => n.ShowZoomLabel = v, _ => false, stored: true);
        yield return new PropertyDescriptor<GraphEdit, float>(nameof(ZoomStep), n => n.ZoomStep, (n, v) => n.ZoomStep = v, _ => 1.2f, stored: true);
        yield return new PropertyDescriptor<GraphEdit, Vector2>(nameof(ScrollOffset), n => n.ScrollOffset, (n, v) => n.ScrollOffset = v, _ => Vector2.Zero, stored: true);
        yield return new PropertyDescriptor<GraphEdit, float>(nameof(Zoom), n => n.Zoom, (n, v) => n.Zoom = v, _ => 1, stored: true);
        yield return new PropertyDescriptor<GraphEdit, float>(nameof(ZoomMin), n => n.ZoomMin, (n, v) => n.ZoomMin = v, _ => .23256795f, stored: true);
        yield return new PropertyDescriptor<GraphEdit, float>(nameof(ZoomMax), n => n.ZoomMax, (n, v) => n.ZoomMax = v, _ => 2.0736003f, stored: true);
        yield return new PropertyDescriptor<GraphEdit, GraphConnection[]>(nameof(Connections), n => n.Connections, (n, v) => n.Connections = v, _ => [], stored: true);
        yield return new PropertyDescriptor<GraphEdit, Dictionary<int, string>>(nameof(TypeNames), n => n.TypeNames, (n, v) => n.TypeNames = v, _ => new(), stored: true);
    }
}
