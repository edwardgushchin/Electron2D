namespace Electron2D;

/// <summary>Arranges visible direct controls in a pixel-aligned grid with a fixed column count.</summary>
/// <remarks>Rows and columns expand uniformly when any member requests expansion. Child ratios are ignored.
/// Layout uses inherited deferred sorting and fill/shrink fitting. Horizontal order follows layout direction.</remarks>
public class GridContainer : Container
{
    private int _columns = 1, _hSeparation = 4, _vSeparation = 4;
    private bool _arranging;
    private readonly List<Control> _children = [];
    private Track[] _columnTracks = [], _rowTracks = [];
    private int[] _minimumColumns = [], _minimumRows = [];
    private struct Track { internal int Minimum, Maximum, Final, Offset; internal bool Expand; }
    private static readonly PropertyDescriptor[] GridProperties =
    [
        new PropertyDescriptor<GridContainer, int>(nameof(Columns), node => node.Columns, (node, value) => node.Columns = value, _ => 1, stored: true),
        new PropertyDescriptor<GridContainer, int>(nameof(HSeparation), node => node.HSeparation, (node, value) => node.HSeparation = value, _ => 4, stored: true),
        new PropertyDescriptor<GridContainer, int>(nameof(VSeparation), node => node.VSeparation, (node, value) => node.VSeparation = value, _ => 4, stored: true)
    ];
    /// <summary>Creates a one-column grid with four-pixel horizontal and vertical separation.</summary>
    public GridContainer() { }
    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    /// <summary>Gets or sets the number of columns before wrapping to the next row.</summary>
    /// <value>One initially. Empty trailing columns participate in expansion without increasing the minimum size.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is less than one.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The grid is disposed.</exception>
    public int Columns
    {
        get { Check(); return _columns; }
        set { EnsureMutable(); if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); if (_columns == value) return; _columns = value; QueueSort(); UpdateMinimumSize(); }
    }
    /// <summary>Gets or sets the signed horizontal pixel gap between occupied columns.</summary>
    /// <value>Four initially; a local typed projection of the horizontal theme constant.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The grid is disposed.</exception>
    public int HSeparation
    {
        get { Check(); return _hSeparation; }
        set { EnsureMutable(); if (_hSeparation == value) return; _hSeparation = value; QueueSort(); UpdateMinimumSize(); }
    }
    /// <summary>Gets or sets the signed vertical pixel gap between rows.</summary>
    /// <value>Four initially; a local typed projection of the vertical theme constant.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The grid is disposed.</exception>
    public int VSeparation
    {
        get { Check(); return _vSeparation; }
        set { EnsureMutable(); if (_vSeparation == value) return; _vSeparation = value; QueueSort(); UpdateMinimumSize(); }
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        var configuredColumns = _columns;
        Prepare(ref _minimumColumns, Math.Min(ChildCount, configuredColumns));
        Prepare(ref _minimumRows, ChildCount == 0 ? 0 : (ChildCount - 1) / configuredColumns + 1);
        var count = 0;
        for (var index = 0; index < ChildCount; index++)
            if (Sortable(GetChild(index), true) && GetChild(index) is Control child)
            {
                var minimum = child.GetBoundMinimumSize(); var column = count % configuredColumns; var row = count / configuredColumns;
                _minimumColumns[column] = Math.Max(_minimumColumns[column], Pixel(minimum.X));
                _minimumRows[row] = Math.Max(_minimumRows[row], Pixel(minimum.Y)); count++;
            }
        if (count == 0) return Vector2.Zero;
        var columns = Math.Min(count, configuredColumns); var rows = (count - 1) / configuredColumns + 1;
        var width = checked((columns - 1) * _hSeparation); var height = checked((rows - 1) * _vSeparation);
        for (var index = 0; index < columns; index++) width = checked(width + _minimumColumns[index]);
        for (var index = 0; index < rows; index++) height = checked(height + _minimumRows[index]);
        return new(width, height);
    }
    private void Arrange()
    {
        if (!IsInsideTree || !IsVisibleInTree) return;
        if (_arranging) { QueueSort(); return; }
        _arranging = true; var expectedTree = Tree;
        _children.Clear();
        try
        {
            for (var index = 0; index < ChildCount; index++) if (Sortable(GetChild(index)) && GetChild(index) is Control child) _children.Add(child);
            if (_children.Count == 0) return;
            var columns = _columns; var horizontalGap = _hSeparation; var verticalGap = _vSeparation;
            var columnCount = Math.Min(_children.Count, columns); var rowCount = (_children.Count - 1) / columns + 1;
            Prepare(ref _columnTracks, columnCount); Prepare(ref _rowTracks, rowCount);
            var size = Size; var maximum = GetCombinedMaximumSize(); var propagate = PropagateMaximumSize;
            var maximumPixels = new Vector2(Pixel(maximum.X), Pixel(maximum.Y));
            for (var index = 0; index < _children.Count; index++)
            {
                var child = _children[index]; child.ContainerMaximum = propagate ? maximumPixels : null;
                var minimum = child.GetBoundMinimumSize(); var childMaximum = child.GetCombinedMaximumSize();
                Add(ref _columnTracks[index % columns], minimum.X, childMaximum.X, maximumPixels.X, size.X, child.SizeFlagsHorizontal);
                Add(ref _rowTracks[index / columns], minimum.Y, childMaximum.Y, maximumPixels.Y, size.Y, child.SizeFlagsVertical);
            }
            Allocate(_columnTracks.AsSpan(0, columnCount), columns - columnCount, size.X, horizontalGap);
            Allocate(_rowTracks.AsSpan(0, rowCount), 0, size.Y, verticalGap);
            var rtl = IsLayoutRTL(); var width = Pixel(size.X); List<Exception>? errors = null;
            for (var index = 0; index < _children.Count; index++)
            {
                if (IsDisposed || !IsInsideTree || !ReferenceEquals(Tree, expectedTree)) break;
                var child = _children[index]; if (child.IsDisposed || !ReferenceEquals(child.Parent, this) || !Sortable(child)) continue;
                var column = _columnTracks[index % columns]; var row = _rowTracks[index / columns];
                if (propagate)
                    child.ContainerMaximum = new(maximumPixels.X < 0 ? maximumPixels.X : MathF.Max(0, maximumPixels.X - column.Offset), maximumPixels.Y < 0 ? maximumPixels.Y : MathF.Max(0, maximumPixels.Y - row.Offset));
                try { FitChildInRect(child, new(rtl ? checked(width - column.Offset - column.Final) : column.Offset, row.Offset, column.Final, row.Final)); }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            ThrowCollected("Grid child layout callbacks failed.", errors);
        }
        finally { _children.Clear(); _arranging = false; }
    }
    private static void Add(ref Track track, float minimum, float maximum, float parentMaximum, float available, SizeFlags flags)
    {
        track.Minimum = Math.Max(track.Minimum, Pixel(minimum));
        var bound = Pixel(maximum); if (bound < 0) bound = Pixel(parentMaximum >= 0 ? parentMaximum : available);
        track.Maximum = Math.Max(track.Maximum, bound); track.Expand |= (flags & SizeFlags.Expand) != 0;
    }
    private static void Allocate(Span<Track> tracks, int empty, float available, int separation)
    {
        var remaining = available - checked((tracks.Length - 1) * separation); var expanded = empty;
        foreach (ref var track in tracks)
        {
            track.Maximum = Math.Max(track.Maximum, track.Minimum); track.Final = track.Minimum;
            if (track.Expand) expanded++; else remaining -= track.Minimum;
        }
        // ponytail: Minimum/maximum refit is quadratic in occupied tracks; use indexed allocation if large-grid profiling requires it.
        while (expanded > 0)
        {
            var largest = -1;
            for (var index = 0; index < tracks.Length; index++)
                if (tracks[index].Expand && (largest < 0 || tracks[index].Minimum > tracks[largest].Minimum)) largest = index;
            if (largest < 0) { if (remaining < 0) expanded = 0; break; }
            if (remaining / expanded >= tracks[largest].Minimum) break;
            tracks[largest].Expand = false; remaining -= tracks[largest].Minimum; expanded--;
        }
        while (expanded > 0)
        {
            var capped = -1;
            for (var index = 0; index < tracks.Length; index++)
                if (tracks[index].Expand && remaining / expanded > tracks[index].Maximum) { capped = index; break; }
            if (capped < 0) break;
            tracks[capped].Expand = false; tracks[capped].Final = tracks[capped].Maximum; remaining -= tracks[capped].Final; expanded--;
        }
        var share = expanded == 0 ? 0 : Pixel(remaining / expanded);
        var extra = expanded == 0 ? 0 : Pixel(remaining - (float)expanded * share); var offset = 0;
        for (var index = 0; index < tracks.Length; index++)
        {
            ref var track = ref tracks[index];
            if (track.Expand) { track.Final = share; if (extra > 0) { track.Final = checked(track.Final + 1); extra--; } }
            track.Offset = offset;
            if (index + 1 < tracks.Length) offset = checked(offset + track.Final + separation);
        }
    }
    private static void Prepare<T>(ref T[] values, int count)
    {
        if (values.Length < count) Array.Resize(ref values, Math.Max(count, values.Length == 0 ? 4 : checked(values.Length * 2)));
        Array.Clear(values, 0, count);
    }
    private static int Pixel(float value)
    {
        if (!float.IsFinite(value) || (double)value > int.MaxValue || (double)value < int.MinValue) throw new InvalidOperationException("Grid layout exceeds integer pixel range.");
        return (int)value;
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationSortChildren)
        {
            try { Arrange(); }
            finally { if (!IsDisposed && IsInsideTree) UpdateMinimumSize(); }
        }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(GridProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(GridContainer) ? CreateGrid : base.CreateSceneInstanceFactory();
    private static Node CreateGrid() => new GridContainer();
}
