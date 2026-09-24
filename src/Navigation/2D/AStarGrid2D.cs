namespace Electron2D;

/// <summary>Finds paths through a rectangular two-dimensional grid.</summary>
/// <remarks>Call <see cref="Update"/> after changing grid geometry. Calls are not synchronized;
/// search callbacks may inspect the grid but cannot mutate, dispose or re-enter it.</remarks>
public class AStarGrid2D : ElectronObject
{
    /// <summary>Controls whether and when diagonal neighbors are traversable.</summary>
    public enum DiagonalMode
    {
        /// <summary>Allow every unblocked diagonal destination.</summary>
        Always,
        /// <summary>Traverse only cardinal neighbors.</summary>
        Never,
        /// <summary>Require either neighboring cardinal cell to be walkable.</summary>
        AtLeastOneWalkable,
        /// <summary>Require both neighboring cardinal cells to be walkable.</summary>
        OnlyIfNoObstacles,
        /// <summary>Exclusive upper bound for valid modes.</summary>
        Max
    }

    /// <summary>Selects the default distance function for path costs or estimates.</summary>
    public enum Heuristic
    {
        /// <summary>Straight-line distance.</summary>
        Euclidean,
        /// <summary>Sum of horizontal and vertical distance.</summary>
        Manhattan,
        /// <summary>Eight-neighbor distance with diagonal cost.</summary>
        Octile,
        /// <summary>Maximum of horizontal and vertical distance.</summary>
        Chebyshev,
        /// <summary>Exclusive upper bound for valid heuristics.</summary>
        Max
    }

    /// <summary>Controls the world-space arrangement of grid cells.</summary>
    public enum CellShape
    {
        /// <summary>Axis-aligned square grid positions.</summary>
        Square,
        /// <summary>Right-facing isometric grid positions.</summary>
        IsometricRight,
        /// <summary>Down-facing isometric grid positions.</summary>
        IsometricDown,
        /// <summary>Exclusive upper bound for valid shapes.</summary>
        Max
    }

    private static readonly (int X, int Y)[] NeighborOffsets =
        [(0, -1), (1, 0), (0, 1), (-1, 0), (-1, -1), (1, -1), (1, 1), (-1, 1)];

    private Rect2i _region;
    private Vector2 _offset;
    private Vector2 _cellSize = Vector2.One;
    private CellShape _cellShape;
    private DiagonalMode _diagonalMode;
    private Heuristic _defaultComputeHeuristic;
    private Heuristic _defaultEstimateHeuristic;
    private bool _jumpingEnabled;
    private bool _dirty;
    private bool _searching;
    private int _pass;
    private Point[] _points = [];
    private Point? _end;

    /// <summary>Creates an empty grid with unit square cells.</summary>
    public AStarGrid2D() { }

    /// <summary>Gets or sets the integer grid bounds. Geometry changes need <see cref="Update"/>.</summary>
    public Rect2i Region
    {
        get { ThrowIfDisposed(); return _region; }
        set
        {
            EnsureMutable(); ValidateRegion(value);
            if (_region == value) return;
            _region = value; _dirty = true;
        }
    }

    /// <summary>Gets or sets the size while retaining <see cref="Region"/>'s origin.</summary>
    /// <remarks>Use Region for new code. A changed size needs <see cref="Update"/>.</remarks>
    public Vector2i Size
    {
        get { ThrowIfDisposed(); return _region.Size; }
        set { Region = new Rect2i(_region.Position, value); }
    }

    /// <summary>Gets or sets the finite world-space offset. A change needs <see cref="Update"/>.</summary>
    public Vector2 Offset
    {
        get { ThrowIfDisposed(); return _offset; }
        set
        {
            EnsureMutable(); ValidateFinite(value);
            if (_offset.IsEqualApprox(value)) return;
            _offset = value; _dirty = true;
        }
    }

    /// <summary>Gets or sets finite cell dimensions. A change needs <see cref="Update"/>.</summary>
    public Vector2 CellSize
    {
        get { ThrowIfDisposed(); return _cellSize; }
        set
        {
            EnsureMutable(); ValidateFinite(value);
            if (_cellSize.IsEqualApprox(value)) return;
            _cellSize = value; _dirty = true;
        }
    }

    /// <summary>Gets or sets the layout used to map cell IDs into world positions.</summary>
    public CellShape Shape
    {
        get { ThrowIfDisposed(); return _cellShape; }
        set
        {
            EnsureMutable(); ValidateEnum(value);
            if (_cellShape == value) return;
            _cellShape = value; _dirty = true;
        }
    }

    /// <summary>Gets or sets whether jump-point search skips intermediate cells.</summary>
    /// <remarks>Jumping ignores per-cell weights, matching its source search contract.</remarks>
    public bool JumpingEnabled
    {
        get { ThrowIfDisposed(); return _jumpingEnabled; }
        set { EnsureMutable(); _jumpingEnabled = value; }
    }

    /// <summary>Gets or sets diagonal-neighbor eligibility.</summary>
    public DiagonalMode Diagonals
    {
        get { ThrowIfDisposed(); return _diagonalMode; }
        set { EnsureMutable(); ValidateEnum(value); _diagonalMode = value; }
    }

    /// <summary>Gets or sets the default edge-cost heuristic.</summary>
    public Heuristic DefaultComputeHeuristic
    {
        get { ThrowIfDisposed(); return _defaultComputeHeuristic; }
        set { EnsureMutable(); ValidateEnum(value); _defaultComputeHeuristic = value; }
    }

    /// <summary>Gets or sets the default remaining-cost heuristic.</summary>
    public Heuristic DefaultEstimateHeuristic
    {
        get { ThrowIfDisposed(); return _defaultEstimateHeuristic; }
        set { EnsureMutable(); ValidateEnum(value); _defaultEstimateHeuristic = value; }
    }

    /// <summary>Reports whether changed geometry requires <see cref="Update"/>.</summary>
    public bool IsDirty() { ThrowIfDisposed(); return _dirty; }

    /// <summary>Tests whether integer coordinates belong to <see cref="Region"/>.</summary>
    public bool IsInBounds(int x, int y) { ThrowIfDisposed(); return InBounds(x, y); }

    /// <summary>Tests whether a point ID belongs to <see cref="Region"/>.</summary>
    public bool IsInBoundsV(Vector2i id) => IsInBounds(id.X, id.Y);

    /// <summary>Rebuilds the grid from its current geometry and resets all point flags and weights.</summary>
    public void Update()
    {
        EnsureMutable();
        if (!_dirty) return;
        var width = _region.Size.X;
        var height = _region.Size.Y;
        var points = new Point[checked(width * height)];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var id = new Vector2i(_region.Position.X + x, _region.Position.Y + y);
                var position = Position(id);
                if (!position.IsFinite()) throw new ArgumentException("Grid positions must remain finite.");
                points[y * width + x] = new Point(id, position);
            }
        }
        _points = points;
        _dirty = false;
    }

    /// <summary>Clears grid points and resets the region to zero; the current dirty flag remains.</summary>
    public void Clear() { EnsureMutable(); _points = []; _region = default; }

    /// <summary>Disables or enables an initialized cell for pathfinding.</summary>
    public void SetPointSolid(Vector2i id, bool solid = true) { EnsureReadyForMutation(); GetPoint(id).Solid = solid; }

    /// <summary>Reports whether an initialized cell is disabled for pathfinding.</summary>
    public bool IsPointSolid(Vector2i id) => GetPoint(id).Solid;

    /// <summary>Sets a finite nonnegative cost multiplier paid when entering a cell.</summary>
    public void SetPointWeightScale(Vector2i id, float weightScale)
    {
        EnsureReadyForMutation(); ValidateWeight(weightScale);
        GetPoint(id).WeightScale = weightScale;
    }

    /// <summary>Gets a cell's entry-cost multiplier.</summary>
    public float GetPointWeightScale(Vector2i id) => GetPoint(id).WeightScale;

    /// <summary>Fills the intersection of an integer region and the grid with one solid flag.</summary>
    public void FillSolidRegion(Rect2i region, bool solid = true)
    {
        EnsureReadyForMutation();
        foreach (var point in PointsInRegion(region)) point.Solid = solid;
    }

    /// <summary>Fills the intersection of an integer region and the grid with one entry weight.</summary>
    public void FillWeightScaleRegion(Rect2i region, float weightScale)
    {
        EnsureReadyForMutation(); ValidateWeight(weightScale);
        foreach (var point in PointsInRegion(region)) point.WeightScale = weightScale;
    }

    /// <summary>Gets the world position of an initialized cell.</summary>
    public Vector2 GetPointPosition(Vector2i id) => GetPoint(id).Position;

    /// <summary>Gets caller-owned typed snapshots for cells in an intersecting region, in row-major order.</summary>
    public (Vector2i ID, Vector2 Position, bool Solid, float WeightScale)[] GetPointDataInRegion(Rect2i region)
    {
        EnsureReady();
        var cells = PointsInRegion(region);
        var result = new (Vector2i ID, Vector2 Position, bool Solid, float WeightScale)[cells.Count];
        for (var i = 0; i < cells.Count; i++)
        {
            var p = cells[i];
            result[i] = (p.ID, p.Position, p.Solid, p.WeightScale);
        }
        return result;
    }

    /// <summary>Gets the full or closest-reached partial path of cell IDs.</summary>
    public Vector2i[] GetIDPath(Vector2i fromID, Vector2i toID, bool allowPartialPath = false)
    {
        var (start, end) = Search(fromID, toID, allowPartialPath);
        if (end is null) return [];
        var result = new Vector2i[PathLength(start, end)];
        var i = result.Length;
        for (var p = end; p is not null; p = p.Previous)
        {
            result[--i] = p.ID;
            if (ReferenceEquals(p, start)) break;
        }
        return result;
    }

    /// <summary>Gets the full or closest-reached partial path of current cell positions.</summary>
    public Vector2[] GetPointPath(Vector2i fromID, Vector2i toID, bool allowPartialPath = false)
    {
        var (start, end) = Search(fromID, toID, allowPartialPath);
        if (end is null) return [];
        var result = new Vector2[PathLength(start, end)];
        var i = result.Length;
        for (var p = end; p is not null; p = p.Previous)
        {
            result[--i] = p.Position;
            if (ReferenceEquals(p, start)) break;
        }
        return result;
    }

    /// <summary>Computes the unweighted cost between a current point and a neighbor or jump point.</summary>
    protected virtual float OnComputeCost(Vector2i fromID, Vector2i toID) => HeuristicCost(fromID, toID, _defaultComputeHeuristic);

    /// <summary>Estimates the remaining cost between a current point and the requested target.</summary>
    protected virtual float OnEstimateCost(Vector2i fromID, Vector2i endID) => HeuristicCost(fromID, endID, _defaultEstimateHeuristic);

    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        base.ValidateDisposal();
        if (_searching) throw new InvalidOperationException("A grid cannot be disposed during path search.");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _points = [];
        base.Dispose(disposing);
    }

    private (Point Start, Point? End) Search(Vector2i fromID, Vector2i toID, bool allowPartialPath)
    {
        EnsureReady();
        if (_searching) throw new InvalidOperationException("Path search cannot be re-entered.");
        var start = GetPoint(fromID);
        var target = GetPoint(toID);
        if (start.Solid || target.Solid && !allowPartialPath) return (start, null);
        if (ReferenceEquals(start, target)) return (start, start);

        _searching = true;
        _end = target;
        try
        {
            if (_pass == int.MaxValue)
            {
                foreach (var point in _points) point.OpenPass = point.ClosedPass = 0;
                _pass = 0;
            }
            var pass = ++_pass;
            var queue = new PriorityQueue<(Point Point, int Version), (float F, float NegativeG, long Order)>();
            var neighbors = new List<Point>(8);
            long order = 0;
            start.G = 0;
            start.F = Estimate(start.ID, target.ID);
            start.AbsG = 0;
            start.AbsF = Estimate(start.ID, target.ID);
            start.OpenPass = pass;
            start.Version++;
            queue.Enqueue((start, start.Version), (start.F, 0, order++));
            Point? closest = null;
            while (queue.TryDequeue(out var entry, out _))
            {
                var point = entry.Point;
                if (point.ClosedPass == pass || point.Version != entry.Version) continue;
                if (closest is null || closest.AbsF > point.AbsF ||
                    closest.AbsF >= point.AbsF && closest.AbsG > point.AbsG)
                    closest = point;
                if (ReferenceEquals(point, target)) return (start, target);
                point.ClosedPass = pass;
                GetNeighbors(point, neighbors);
                foreach (var candidate in neighbors)
                {
                    var neighbor = _jumpingEnabled ? Jump(point, candidate) : candidate;
                    if (neighbor is null || neighbor.ClosedPass == pass) continue;
                    var weight = _jumpingEnabled ? 1f : neighbor.WeightScale;
                    var cost = point.G + Compute(point.ID, neighbor.ID) * weight;
                    if (!float.IsFinite(cost) || cost < 0)
                        throw new InvalidOperationException("A path cost overflowed finite nonnegative values.");
                    if (neighbor.OpenPass == pass && cost >= neighbor.G) continue;
                    neighbor.OpenPass = pass;
                    neighbor.Previous = point;
                    neighbor.G = cost;
                    neighbor.F = cost + Estimate(neighbor.ID, target.ID);
                    if (!float.IsFinite(neighbor.F))
                        throw new InvalidOperationException("A path estimate overflowed finite values.");
                    neighbor.AbsG = cost;
                    neighbor.AbsF = neighbor.F - cost;
                    neighbor.Version++;
                    queue.Enqueue((neighbor, neighbor.Version), (neighbor.F, -neighbor.G, order++));
                }
            }
            return (start, allowPartialPath ? closest : null);
        }
        finally { _searching = false; _end = null; }
    }

    private void GetNeighbors(Point point, List<Point> result)
    {
        result.Clear();
        var x = (long)point.ID.X; var y = (long)point.ID.Y;
        var top = Walkable(x, y - 1);
        var right = Walkable(x + 1, y);
        var bottom = Walkable(x, y + 1);
        var left = Walkable(x - 1, y);
        if (top) result.Add(GetPointUnchecked((int)x, (int)(y - 1)));
        if (right) result.Add(GetPointUnchecked((int)(x + 1), (int)y));
        if (bottom) result.Add(GetPointUnchecked((int)x, (int)(y + 1)));
        if (left) result.Add(GetPointUnchecked((int)(x - 1), (int)y));
        for (var i = 4; i < NeighborOffsets.Length; i++)
        {
            var adjacent = i switch
            {
                4 => (left, top),
                5 => (top, right),
                6 => (right, bottom),
                _ => (bottom, left)
            };
            var allowed = _diagonalMode switch
            {
                DiagonalMode.Always => true,
                DiagonalMode.AtLeastOneWalkable => adjacent.Item1 || adjacent.Item2,
                DiagonalMode.OnlyIfNoObstacles => adjacent.Item1 && adjacent.Item2,
                _ => false
            };
            var (dx, dy) = NeighborOffsets[i];
            if (allowed && Walkable(x + dx, y + dy))
                result.Add(GetPointUnchecked((int)(x + dx), (int)(y + dy)));
        }
    }

    private Point? Jump(Point from, Point to)
    {
        long fromX = from.ID.X, fromY = from.ID.Y;
        long x = to.ID.X, y = to.ID.Y;
        var dx = x - fromX;
        var dy = y - fromY;
        if (_diagonalMode is DiagonalMode.Always or DiagonalMode.AtLeastOneWalkable)
        {
            if (dx == 0 || dy == 0) return ForcedSuccessor(x, y, dx, dy, false);
            while (Walkable(x, y) && (_diagonalMode == DiagonalMode.Always ||
                   Walkable(x, y - dy) || Walkable(x - dx, y)))
            {
                if (AtEnd(x, y)) return _end;
                if (Walkable(x - dx, y + dy) && !Walkable(x - dx, y) ||
                    Walkable(x + dx, y - dy) && !Walkable(x, y - dy))
                    return GetPointUnchecked((int)x, (int)y);
                if (ForcedSuccessor(x + dx, y, dx, 0, false) is not null ||
                    ForcedSuccessor(x, y + dy, 0, dy, false) is not null)
                    return GetPointUnchecked((int)x, (int)y);
                x += dx; y += dy;
            }
        }
        else if (_diagonalMode == DiagonalMode.OnlyIfNoObstacles)
        {
            if (dx == 0 || dy == 0) return ForcedSuccessor(fromX, fromY, dx, dy, true);
            while (Walkable(x, y) && Walkable(x, y - dy) && Walkable(x - dx, y))
            {
                if (AtEnd(x, y)) return _end;
                if (Walkable(x + dx, y + dy) && !Walkable(x, y + dy) || !Walkable(x + dx, y))
                    return GetPointUnchecked((int)x, (int)y);
                if (ForcedSuccessor(x, y, dx, 0, false) is not null ||
                    ForcedSuccessor(x, y, 0, dy, false) is not null)
                    return GetPointUnchecked((int)x, (int)y);
                x += dx; y += dy;
            }
        }
        else
        {
            if (dy == 0) return ForcedSuccessor(fromX, fromY, dx, 0, true);
            while (Walkable(x, y))
            {
                if (AtEnd(x, y)) return _end;
                if (Walkable(x - 1, y) && !Walkable(x - 1, y - dy) ||
                    Walkable(x + 1, y) && !Walkable(x + 1, y - dy))
                    return GetPointUnchecked((int)x, (int)y);
                if (ForcedSuccessor(x, y, 1, 0, true) is not null ||
                    ForcedSuccessor(x, y, -1, 0, true) is not null)
                    return GetPointUnchecked((int)x, (int)y);
                y += dy;
            }
        }
        return null;
    }

    private Point? ForcedSuccessor(long x, long y, long dx, long dy, bool inclusive)
    {
        var previousLeft = false; var previousRight = false;
        var left = false; var right = false;
        var nextX = inclusive ? x + dx : x;
        var nextY = inclusive ? y + dy : y;
        var leftX = x - dy; var leftY = y - dx;
        var rightX = x + dy; var rightY = y + dx;
        while (Walkable(nextX, nextY))
        {
            if (AtEnd(nextX, nextY)) return _end;
            previousLeft = left || Walkable(leftX, leftY);
            previousRight = right || Walkable(rightX, rightY);
            leftX += dx; leftY += dy;
            rightX += dx; rightY += dy;
            left = Walkable(leftX, leftY);
            right = Walkable(rightX, rightY);
            if (left && !previousLeft || right && !previousRight)
                return GetPointUnchecked((int)nextX, (int)nextY);
            nextX += dx; nextY += dy;
        }
        return null;
    }

    private bool AtEnd(long x, long y) => _end is not null && _end.ID.X == x && _end.ID.Y == y;

    private Vector2 Position(Vector2i id)
    {
        var x = (float)id.X; var y = (float)id.Y;
        var half = _cellSize / 2f;
        return _cellShape switch
        {
            CellShape.IsometricRight => _offset + half + new Vector2(x + y, y - x) * half,
            CellShape.IsometricDown => _offset + half + new Vector2(x - y, x + y) * half,
            _ => _offset + new Vector2(x, y) * _cellSize
        };
    }

    private List<Point> PointsInRegion(Rect2i region)
    {
        if (region.Size.X < 0 || region.Size.Y < 0) throw new ArgumentOutOfRangeException(nameof(region));
        var left = Math.Max((long)region.Position.X, _region.Position.X);
        var top = Math.Max((long)region.Position.Y, _region.Position.Y);
        var right = Math.Min((long)region.Position.X + region.Size.X, (long)_region.Position.X + _region.Size.X);
        var bottom = Math.Min((long)region.Position.Y + region.Size.Y, (long)_region.Position.Y + _region.Size.Y);
        var result = new List<Point>();
        for (var y = top; y < bottom; y++)
            for (var x = left; x < right; x++)
                result.Add(GetPointUnchecked((int)x, (int)y));
        return result;
    }

    private bool InBounds(long x, long y) => x >= _region.Position.X && y >= _region.Position.Y &&
        x < (long)_region.Position.X + _region.Size.X && y < (long)_region.Position.Y + _region.Size.Y;

    private bool Walkable(long x, long y) => InBounds(x, y) && !GetPointUnchecked((int)x, (int)y).Solid;

    private Point GetPoint(Vector2i id)
    {
        EnsureReady();
        return InBounds(id.X, id.Y) ? GetPointUnchecked(id.X, id.Y) :
            throw new KeyNotFoundException($"Grid point {id} is out of bounds.");
    }

    private Point GetPointUnchecked(int x, int y) => _points[(y - _region.Position.Y) * _region.Size.X + x - _region.Position.X];

    private void EnsureMutable()
    {
        ThrowIfDisposed();
        if (_searching) throw new InvalidOperationException("A grid cannot be changed during path search.");
    }

    private void EnsureReady()
    {
        ThrowIfDisposed();
        if (_dirty) throw new InvalidOperationException("Call Update after changing grid geometry.");
    }

    private void EnsureReadyForMutation() { EnsureMutable(); EnsureReady(); }

    private static void ValidateRegion(Rect2i region)
    {
        if (region.Size.X < 0 || region.Size.Y < 0 ||
            (long)region.Position.X + region.Size.X > int.MaxValue ||
            (long)region.Position.Y + region.Size.Y > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(region));
    }

    private static void ValidateFinite(Vector2 value)
    {
        if (!value.IsFinite()) throw new ArgumentException("Grid geometry must be finite.");
    }

    private static void ValidateWeight(float value)
    {
        if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void ValidateEnum<T>(T value) where T : struct, Enum
    {
        if (Convert.ToInt32(value) < 0 || Convert.ToInt32(value) >= Convert.ToInt32(Enum.GetValues<T>()[^1]))
            throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static float HeuristicCost(Vector2i from, Vector2i to, Heuristic heuristic)
    {
        var dx = Math.Abs((double)to.X - from.X);
        var dy = Math.Abs((double)to.Y - from.Y);
        return (float)(heuristic switch
        {
            Heuristic.Manhattan => dx + dy,
            Heuristic.Octile => (Math.Sqrt(2) - 1) * Math.Min(dx, dy) + Math.Max(dx, dy),
            Heuristic.Chebyshev => Math.Max(dx, dy),
            _ => Math.Sqrt(dx * dx + dy * dy)
        });
    }

    private float Estimate(Vector2i from, Vector2i to)
    {
        var result = OnEstimateCost(from, to);
        if (!float.IsFinite(result) || result < 0) throw new InvalidOperationException("An estimated cost must be finite and nonnegative.");
        return result;
    }

    private float Compute(Vector2i from, Vector2i to)
    {
        var result = OnComputeCost(from, to);
        if (!float.IsFinite(result) || result < 0) throw new InvalidOperationException("A computed cost must be finite and nonnegative.");
        return result;
    }

    private static int PathLength(Point start, Point end)
    {
        var count = 1;
        for (var p = end; !ReferenceEquals(p, start); p = p.Previous ?? throw new InvalidOperationException("The path chain is incomplete.")) count++;
        return count;
    }

    private sealed class Point(Vector2i id, Vector2 position)
    {
        internal readonly Vector2i ID = id;
        internal readonly Vector2 Position = position;
        internal bool Solid;
        internal float WeightScale = 1f;
        internal Point? Previous;
        internal float G, F, AbsG, AbsF;
        internal int OpenPass, ClosedPass, Version;
    }
}
