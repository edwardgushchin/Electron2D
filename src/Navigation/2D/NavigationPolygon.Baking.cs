namespace Electron2D;

public sealed partial class NavigationPolygon
{
    /// <summary>Selects scene geometry producers.</summary>
    public enum ParsedGeometryType
    {
        /// <summary>Mesh and polygon geometry.</summary>
        MeshInstances = 0,
        /// <summary>Static collision geometry.</summary>
        StaticColliders = 1,
        /// <summary>Both supported source families.</summary>
        Both = 2,
        /// <summary>Invalid boundary sentinel.</summary>
        Max = 3
    }
    /// <summary>Selects output polygon partitioning.</summary>
    public enum SamplePartitionType
    {
        /// <summary>Convex polygons formed by merging adjacent triangles.</summary>
        ConvexPartition = 0,
        /// <summary>Triangle polygons.</summary>
        Triangulate = 1,
        /// <summary>Invalid boundary sentinel.</summary>
        Max = 2
    }
    /// <summary>Selects which scene nodes are parsed.</summary>
    public enum SourceGeometryMode
    {
        /// <summary>The root and its descendants.</summary>
        RootNodeChildren = 0,
        /// <summary>Grouped nodes and their descendants.</summary>
        GroupsWithChildren = 1,
        /// <summary>Only grouped nodes.</summary>
        GroupsExplicit = 2,
        /// <summary>Invalid boundary sentinel.</summary>
        Max = 3
    }
    private Vector2[][] _outlines = [];
    private NavigationBakeSettings _settings = new();
    private static bool FloatNonnegative(float value) => float.IsFinite(value) && value >= 0;
    private static bool FloatPositive(float value) => float.IsFinite(value) && value > 0;
    /// <summary>Gets or changes the clearance erosion distance around traversable boundaries and ordinary obstructions.</summary>
    /// <value>Initially 10f.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its finite supported domain.</exception>
    public float AgentRadius
    {
        get { lock (_gate) { ThrowIfDisposed(); return _settings.AgentRadius; } }
        set { if (!(FloatNonnegative(value))) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { AgentRadius = value }; } EmitChanged(); }
    }
    /// <summary>Gets or changes the positive cell size required by maps consuming generated geometry.</summary>
    /// <value>Initially 1f.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its finite supported domain.</exception>
    public float CellSize
    {
        get { lock (_gate) { ThrowIfDisposed(); return _settings.CellSize; } }
        set { if (!(FloatPositive(value))) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { CellSize = value }; if (_data.CellSize is not null) _data = _data with { CellSize = value }; } EmitChanged(); }
    }
    /// <summary>Gets or changes the nonwalkable border inside an enabled baking rectangle.</summary>
    /// <value>Initially 0f.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its finite supported domain.</exception>
    public float BorderSize
    {
        get { lock (_gate) { ThrowIfDisposed(); return _settings.BorderSize; } }
        set { if (!(FloatNonnegative(value))) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { BorderSize = value }; } EmitChanged(); }
    }
    /// <summary>Gets or changes the optional positive-area rectangle restricting the bake.</summary>
    /// <value>Initially default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its finite supported domain.</exception>
    public Rect2 BakingRect
    {
        get { lock (_gate) { ThrowIfDisposed(); return _settings.BakingRect; } }
        set { if (!(value.IsFinite())) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { BakingRect = value }; } EmitChanged(); }
    }
    /// <summary>Gets or changes the offset applied to the baking rectangle.</summary>
    /// <value>Initially default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its finite supported domain.</exception>
    public Vector2 BakingRectOffset
    {
        get { lock (_gate) { ThrowIfDisposed(); return _settings.BakingRectOffset; } }
        set { if (!(value.IsFinite())) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { BakingRectOffset = value }; } EmitChanged(); }
    }
    /// <summary>Gets or changes the 32-bit static collision-layer selection mask.</summary>
    /// <value>Initially uint.MaxValue.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its finite supported domain.</exception>
    public uint ParsedCollisionMask
    {
        get { lock (_gate) { ThrowIfDisposed(); return _settings.ParsedCollisionMask; } }
        set { if (!(true)) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { ParsedCollisionMask = value }; } EmitChanged(); }
    }
    /// <summary>Gets or changes the scene geometry family to collect.</summary>
    /// <value>Initially ParsedGeometryType.Both.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its finite supported domain.</exception>
    public ParsedGeometryType ParsedGeometry
    {
        get { lock (_gate) { ThrowIfDisposed(); return _settings.ParsedGeometry; } }
        set { if (!((uint)value < 3)) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { ParsedGeometry = value }; } EmitChanged(); }
    }
    /// <summary>Gets or changes the convex polygon or triangle partitioning of the walkable surface.</summary>
    /// <value>Initially SamplePartitionType.ConvexPartition.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its finite supported domain.</exception>
    public SamplePartitionType SamplePartition
    {
        get { lock (_gate) { ThrowIfDisposed(); return _settings.SamplePartition; } }
        set { if (!((uint)value < 2)) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { SamplePartition = value }; } EmitChanged(); }
    }
    /// <summary>Gets or changes the root or group-based scene traversal mode.</summary>
    /// <value>Initially SourceGeometryMode.RootNodeChildren.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its finite supported domain.</exception>
    public SourceGeometryMode SourceGeometry
    {
        get { lock (_gate) { ThrowIfDisposed(); return _settings.SourceGeometry; } }
        set { if (!((uint)value < 3)) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { SourceGeometry = value }; } EmitChanged(); }
    }
    /// <summary>Gets or changes the group name used by group traversal modes.</summary>
    /// <value>Initially navigation_polygon_source_geometry_group.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its finite supported domain.</exception>
    public string SourceGeometryGroupName
    {
        get { lock (_gate) { ThrowIfDisposed(); return _settings.SourceGeometryGroupName; } }
        set { if (!(value is not null)) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); _settings = _settings with { SourceGeometryGroupName = value }; } EmitChanged(); }
    }
    /// <summary>Reads one of the 32 parsed collision-layer bits.</summary><param name="layerNumber">One-based layer from 1 to 32.</param><returns>The bit state.</returns>
    public bool GetParsedCollisionMaskValue(int layerNumber) { ArgumentOutOfRangeException.ThrowIfLessThan(layerNumber, 1); ArgumentOutOfRangeException.ThrowIfGreaterThan(layerNumber, 32); return (ParsedCollisionMask & (1u << (layerNumber - 1))) != 0; }
    /// <summary>Changes one parsed collision-layer bit.</summary><param name="layerNumber">One-based layer from 1 to 32.</param><param name="value">Enabled state.</param>
    public void SetParsedCollisionMaskValue(int layerNumber, bool value)
    { ArgumentOutOfRangeException.ThrowIfLessThan(layerNumber, 1); ArgumentOutOfRangeException.ThrowIfGreaterThan(layerNumber, 32); lock (_gate) { ThrowIfDisposed(); var bit = 1u << (layerNumber - 1); _settings = _settings with { ParsedCollisionMask = value ? _settings.ParsedCollisionMask | bit : _settings.ParsedCollisionMask & ~bit }; } EmitChanged(); }
    /// <summary>Appends a copied finite outline.</summary><param name="outline">Local-space contour, including empty authoring state.</param>
    public void AddOutline(ReadOnlySpan<Vector2> outline) { var copy = NavigationGeometryStorage.CopyOutline(outline); lock (_gate) { ThrowIfDisposed(); SetOutlinesCore([.. _outlines, copy]); } EmitChanged(); }
    /// <summary>Inserts a copied outline.</summary><param name="outline">Finite local-space contour.</param><param name="index">Insertion index from zero through count.</param>
    public void AddOutlineAtIndex(ReadOnlySpan<Vector2> outline, int index)
    { var copy = NavigationGeometryStorage.CopyOutline(outline); lock (_gate) { ThrowIfDisposed(); if ((uint)index > (uint)_outlines.Length) throw new ArgumentOutOfRangeException(nameof(index)); var all = new Vector2[_outlines.Length + 1][]; _outlines.AsSpan(0, index).CopyTo(all); all[index] = copy; _outlines.AsSpan(index).CopyTo(all.AsSpan(index + 1)); SetOutlinesCore(all); } EmitChanged(); }
    /// <summary>Replaces one copied outline.</summary><param name="index">Existing outline index.</param><param name="outline">Finite local-space contour.</param>
    public void SetOutline(int index, ReadOnlySpan<Vector2> outline)
    { var copy = NavigationGeometryStorage.CopyOutline(outline); lock (_gate) { ThrowIfDisposed(); if ((uint)index >= (uint)_outlines.Length) throw new ArgumentOutOfRangeException(nameof(index)); var all = (Vector2[][])_outlines.Clone(); all[index] = copy; SetOutlinesCore(all); } EmitChanged(); }
    /// <summary>Returns an independent outline.</summary><param name="index">Existing outline index.</param><returns>Copied finite points.</returns>
    public Vector2[] GetOutline(int index) { lock (_gate) { ThrowIfDisposed(); if ((uint)index >= (uint)_outlines.Length) throw new ArgumentOutOfRangeException(nameof(index)); return (Vector2[])_outlines[index].Clone(); } }
    /// <summary>Returns the authored outline count.</summary><returns>Number of outlines.</returns>
    public int GetOutlineCount() { lock (_gate) { ThrowIfDisposed(); return _outlines.Length; } }
    /// <summary>Removes an existing outline.</summary><param name="index">Existing outline index.</param>
    public void RemoveOutline(int index) { lock (_gate) { ThrowIfDisposed(); if ((uint)index >= (uint)_outlines.Length) throw new ArgumentOutOfRangeException(nameof(index)); var all = new Vector2[_outlines.Length - 1][]; _outlines.AsSpan(0, index).CopyTo(all); _outlines.AsSpan(index + 1).CopyTo(all.AsSpan(index)); _outlines = all; } EmitChanged(); }
    /// <summary>Clears outlines without altering the current authored or baked geometry.</summary>
    public void ClearOutlines() { lock (_gate) { ThrowIfDisposed(); _outlines = []; } EmitChanged(); }
    private void SetOutlinesCore(Vector2[][] outlines) { NavigationGeometryStorage.ValidateBudget(new(outlines, [], [])); _outlines = outlines; }
    internal Vector2[][] GetOutlinesSnapshot() { lock (_gate) { ThrowIfDisposed(); return _outlines; } }
    internal NavigationBakeSettings SettingsSnapshot() { lock (_gate) { ThrowIfDisposed(); return _settings; } }
    internal (NavigationBakeSettings Settings, Vector2[][] Outlines) CaptureBake() { lock (_gate) { ThrowIfDisposed(); return (_settings, _outlines); } }
    internal void PublishBake(NavigationPolygonData data) { lock (_gate) { ThrowIfDisposed(); _data = data; } EmitChanged(); }
    private byte[] SaveOutlines() { using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); writer.Write(1); NavigationGeometryStorage.WriteOutlines(writer, GetOutlinesSnapshot()); return stream.ToArray(); }
    private void LoadOutlines(byte[] bytes)
    { ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length == 0) { ClearOutlines(); return; } using var stream = new MemoryStream(bytes, false); using var reader = new BinaryReader(stream); try { if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported navigation outlines version."); var data = NavigationGeometryStorage.ReadOutlines(reader); if (stream.Position != stream.Length) throw new InvalidDataException("Trailing navigation outlines data."); lock (_gate) { ThrowIfDisposed(); _outlines = data; } EmitChanged(); } catch (EndOfStreamException error) { throw new InvalidDataException("Truncated navigation outlines.", error); } }
    private static readonly PropertyDescriptor[] BakeProperties =
    [
        new PropertyDescriptor<NavigationPolygon, float>(nameof(AgentRadius), n => n.AgentRadius, (n, v) => n.AgentRadius = v, _ => 10f, stored: true),
        new PropertyDescriptor<NavigationPolygon, float>(nameof(CellSize), n => n.CellSize, (n, v) => n.CellSize = v, _ => 1f, stored: true),
        new PropertyDescriptor<NavigationPolygon, float>(nameof(BorderSize), n => n.BorderSize, (n, v) => n.BorderSize = v, _ => 0f, stored: true),
        new PropertyDescriptor<NavigationPolygon, Rect2>(nameof(BakingRect), n => n.BakingRect, (n, v) => n.BakingRect = v, _ => default, stored: true),
        new PropertyDescriptor<NavigationPolygon, Vector2>(nameof(BakingRectOffset), n => n.BakingRectOffset, (n, v) => n.BakingRectOffset = v, _ => default, stored: true),
        new PropertyDescriptor<NavigationPolygon, uint>(nameof(ParsedCollisionMask), n => n.ParsedCollisionMask, (n, v) => n.ParsedCollisionMask = v, _ => uint.MaxValue, stored: true),
        new PropertyDescriptor<NavigationPolygon, ParsedGeometryType>(nameof(ParsedGeometry), n => n.ParsedGeometry, (n, v) => n.ParsedGeometry = v, _ => ParsedGeometryType.Both, stored: true),
        new PropertyDescriptor<NavigationPolygon, SamplePartitionType>(nameof(SamplePartition), n => n.SamplePartition, (n, v) => n.SamplePartition = v, _ => SamplePartitionType.ConvexPartition, stored: true),
        new PropertyDescriptor<NavigationPolygon, SourceGeometryMode>(nameof(SourceGeometry), n => n.SourceGeometry, (n, v) => n.SourceGeometry = v, _ => SourceGeometryMode.RootNodeChildren, stored: true),
        new PropertyDescriptor<NavigationPolygon, string>(nameof(SourceGeometryGroupName), n => n.SourceGeometryGroupName, (n, v) => n.SourceGeometryGroupName = v, _ => "navigation_polygon_source_geometry_group", stored: true),
        new PropertyDescriptor<NavigationPolygon, byte[]>("_navigation_outlines", n => n.SaveOutlines(), (n, v) => n.LoadOutlines(v), _ => [], stored: true)
    ];
}
internal sealed record NavigationBakeSettings
{
    internal float AgentRadius { get; init; } = 10f;
    internal float CellSize { get; init; } = 1f;
    internal float BorderSize { get; init; } = 0f;
    internal Rect2 BakingRect { get; init; } = default;
    internal Vector2 BakingRectOffset { get; init; } = default;
    internal uint ParsedCollisionMask { get; init; } = uint.MaxValue;
    internal NavigationPolygon.ParsedGeometryType ParsedGeometry { get; init; } = NavigationPolygon.ParsedGeometryType.Both;
    internal NavigationPolygon.SamplePartitionType SamplePartition { get; init; } = NavigationPolygon.SamplePartitionType.ConvexPartition;
    internal NavigationPolygon.SourceGeometryMode SourceGeometry { get; init; } = NavigationPolygon.SourceGeometryMode.RootNodeChildren;
    internal string SourceGeometryGroupName { get; init; } = "navigation_polygon_source_geometry_group";
}
