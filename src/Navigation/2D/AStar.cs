using System.Numerics;

namespace Electron2D;

/// <summary>Finds weighted paths through a caller-owned directed graph of two-dimensional points.</summary>
/// <remarks>The graph is independent of SceneTree and rendering. Calls are not synchronized; coordinate access
/// from multiple threads externally. Search callbacks may inspect the graph but cannot mutate or re-enter it.</remarks>
public class AStar : ElectronObject
{
    private readonly Dictionary<long, Point> _points = [];
    private readonly List<long> _pointOrder = [];
    private long _lastFreeID;
    private int _capacity = 16;
    private int _pass;
    private bool _allocated;
    private bool _searching;
    private bool _neighborFilterEnabled;

    /// <summary>Creates an empty graph with the reference's initial sixteen-slot reported capacity.</summary>
    public AStar() { }

    /// <summary>Gets or sets whether <see cref="OnFilterNeighbor"/> can reject outgoing edges during a search.</summary>
    /// <value>False by default.</value>
    /// <exception cref="InvalidOperationException">The setting is changed by a search callback.</exception>
    /// <exception cref="ObjectDisposedException">The graph is disposed.</exception>
    public bool NeighborFilterEnabled
    {
        get { ThrowIfDisposed(); return _neighborFilterEnabled; }
        set { EnsureMutableGraph(); _neighborFilterEnabled = value; }
    }

    /// <summary>Gets the next unoccupied nonnegative point ID, searching upward from the last freed ID.</summary>
    /// <returns>The available ID.</returns>
    /// <exception cref="InvalidOperationException">No larger ID is representable.</exception>
    /// <exception cref="ObjectDisposedException">The graph is disposed.</exception>
    public long GetAvailablePointID()
    {
        ThrowIfDisposed();
        if (!_points.ContainsKey(_lastFreeID)) return _lastFreeID;
        var id = _lastFreeID;
        do
        {
            if (id == long.MaxValue) throw new InvalidOperationException("No larger point ID is representable.");
            id++;
        } while (_points.ContainsKey(id));
        return _lastFreeID = id;
    }

    /// <summary>Adds a point or replaces an existing point's position and weight without changing its edges or enabled state.</summary>
    /// <param name="id">Nonnegative point ID.</param>
    /// <param name="position">Finite two-dimensional position.</param>
    /// <param name="weightScale">Finite nonnegative multiplier paid when a path enters this point.</param>
    /// <exception cref="ArgumentOutOfRangeException">The ID or weight is invalid.</exception>
    /// <exception cref="ArgumentException">The position is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">A search callback attempts mutation.</exception>
    public void AddPoint(long id, Vector2 position, float weightScale = 1f)
    {
        EnsureMutableGraph();
        ValidateID(id); ValidatePosition(position); ValidateWeight(weightScale);
        if (_points.TryGetValue(id, out var existing))
        {
            existing.Position = position;
            existing.WeightScale = weightScale;
            return;
        }
        var capacity = _points.Count > (_capacity - _capacity / 4) - 1
            ? checked(_capacity * 2) : _capacity;
        _points.EnsureCapacity(_points.Count + 1);
        _pointOrder.EnsureCapacity(_pointOrder.Count + 1);
        _points.Add(id, new Point(id, position, weightScale));
        _pointOrder.Add(id);
        _capacity = capacity;
        _allocated = true;
    }

    /// <summary>Returns whether a point with this ID exists.</summary>
    /// <param name="id">Point ID to inspect.</param>
    /// <returns>True for a registered point.</returns>
    public bool HasPoint(long id) { ThrowIfDisposed(); return _points.ContainsKey(id); }

    /// <summary>Gets a registered point's position.</summary>
    /// <param name="id">Point ID.</param>
    /// <returns>The stored position.</returns>
    /// <exception cref="KeyNotFoundException">The point is absent.</exception>
    public Vector2 GetPointPosition(long id) => GetPoint(id).Position;

    /// <summary>Changes a registered point's position.</summary>
    /// <param name="id">Point ID.</param>
    /// <param name="position">Finite replacement position.</param>
    /// <exception cref="KeyNotFoundException">The point is absent.</exception>
    /// <exception cref="ArgumentException">The position is nonfinite.</exception>
    public void SetPointPosition(long id, Vector2 position)
    {
        EnsureMutableGraph(); ValidatePosition(position);
        GetPoint(id).Position = position;
    }

    /// <summary>Gets the cost multiplier paid when entering a registered point.</summary>
    /// <param name="id">Point ID.</param>
    /// <returns>The stored weight.</returns>
    /// <exception cref="KeyNotFoundException">The point is absent.</exception>
    public float GetPointWeightScale(long id) => GetPoint(id).WeightScale;

    /// <summary>Changes the cost multiplier paid when entering a registered point.</summary>
    /// <param name="id">Point ID.</param>
    /// <param name="weightScale">Finite nonnegative multiplier.</param>
    /// <exception cref="KeyNotFoundException">The point is absent.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The weight is invalid.</exception>
    public void SetPointWeightScale(long id, float weightScale)
    {
        EnsureMutableGraph(); ValidateWeight(weightScale);
        GetPoint(id).WeightScale = weightScale;
    }

    /// <summary>Removes a point and every incoming or outgoing connection to it.</summary>
    /// <param name="id">Point ID.</param>
    /// <exception cref="KeyNotFoundException">The point is absent.</exception>
    public void RemovePoint(long id)
    {
        EnsureMutableGraph(); GetPoint(id);
        foreach (var point in _points.Values) RemoveOutgoing(point, id);
        _points.Remove(id);
        RemoveID(_pointOrder, id);
        _lastFreeID = id;
    }

    /// <summary>Gets caller-owned point IDs in graph storage order.</summary>
    /// <returns>A fresh array; removing a point may swap the final ID into its position.</returns>
    public long[] GetPointIDs() { ThrowIfDisposed(); return _pointOrder.ToArray(); }

    /// <summary>Gets caller-owned outgoing connection IDs from a registered point.</summary>
    /// <param name="id">Point ID.</param>
    /// <returns>A fresh array in graph storage order.</returns>
    /// <exception cref="KeyNotFoundException">The point is absent.</exception>
    public long[] GetPointConnections(long id) => GetPoint(id).Outgoing.ToArray();

    /// <summary>Gets the number of registered points.</summary>
    /// <returns>Point count.</returns>
    public long GetPointCount() { ThrowIfDisposed(); return _points.Count; }

    /// <summary>Gets the reported backing point-map capacity.</summary>
    /// <returns>A power of two, initially sixteen; clear and removal preserve it.</returns>
    public long GetPointCapacity() { ThrowIfDisposed(); return _capacity; }

    /// <summary>Reserves backing storage for a positive number of points.</summary>
    /// <param name="numNodes">Requested capacity, bounded by managed collection limits.</param>
    /// <exception cref="ArgumentOutOfRangeException">The request is nonpositive or exceeds supported capacity.</exception>
    public void ReserveSpace(long numNodes)
    {
        EnsureMutableGraph();
        if (numNodes <= 0 || numNodes > 1_073_741_824)
            throw new ArgumentOutOfRangeException(nameof(numNodes));
        var rounded = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(4L, numNodes));
        _points.EnsureCapacity((int)numNodes);
        if (!_allocated || numNodes > _capacity) _capacity = rounded;
    }

    /// <summary>Removes every point and connection, preserving allocated capacity.</summary>
    public void Clear()
    {
        EnsureMutableGraph();
        _points.Clear(); _pointOrder.Clear(); _lastFreeID = 0;
    }

    /// <summary>Enables or disables a registered point for pathfinding.</summary>
    /// <param name="id">Point ID.</param>
    /// <param name="disabled">Whether pathfinding should skip the point.</param>
    /// <exception cref="KeyNotFoundException">The point is absent.</exception>
    public void SetPointDisabled(long id, bool disabled = true)
    {
        EnsureMutableGraph(); GetPoint(id).Enabled = !disabled;
    }

    /// <summary>Reports whether a registered point is disabled for pathfinding.</summary>
    /// <param name="id">Point ID.</param>
    /// <returns>True when paths skip this point.</returns>
    /// <exception cref="KeyNotFoundException">The point is absent.</exception>
    public bool IsPointDisabled(long id) => !GetPoint(id).Enabled;

    /// <summary>Adds an outgoing edge and optionally its reverse edge.</summary>
    /// <param name="id">Source point ID.</param>
    /// <param name="toID">Destination point ID, different from the source.</param>
    /// <param name="bidirectional">Whether travel should work in both directions; true by default.</param>
    /// <exception cref="ArgumentException">A point is connected to itself.</exception>
    /// <exception cref="KeyNotFoundException">Either point is absent.</exception>
    public void ConnectPoints(long id, long toID, bool bidirectional = true)
    {
        EnsureMutableGraph();
        if (id == toID) throw new ArgumentException("A point cannot connect to itself.", nameof(toID));
        var from = GetPoint(id); var to = GetPoint(toID);
        if (!from.Outgoing.Contains(toID)) from.Outgoing.EnsureCapacity(from.Outgoing.Count + 1);
        if (bidirectional && !to.Outgoing.Contains(id)) to.Outgoing.EnsureCapacity(to.Outgoing.Count + 1);
        AddOutgoing(from, toID);
        if (bidirectional) AddOutgoing(to, id);
    }

    /// <summary>Removes an outgoing edge and optionally its reverse edge.</summary>
    /// <param name="id">Source point ID.</param>
    /// <param name="toID">Destination point ID.</param>
    /// <param name="bidirectional">Whether to remove travel in both directions; true by default.</param>
    /// <exception cref="KeyNotFoundException">Either point is absent.</exception>
    public void DisconnectPoints(long id, long toID, bool bidirectional = true)
    {
        EnsureMutableGraph();
        var from = GetPoint(id); var to = GetPoint(toID);
        RemoveOutgoing(from, toID);
        if (bidirectional) RemoveOutgoing(to, id);
    }

    /// <summary>Reports whether a physical segment exists, or a directed edge when requested.</summary>
    /// <param name="id">Source point ID.</param>
    /// <param name="toID">Destination point ID.</param>
    /// <param name="bidirectional">True checks either direction; false checks travel from source to destination.</param>
    /// <returns>True when the requested connection exists.</returns>
    public bool ArePointsConnected(long id, long toID, bool bidirectional = true)
    {
        ThrowIfDisposed();
        return _points.TryGetValue(id, out var from) && from.Outgoing.Contains(toID) ||
               bidirectional && _points.TryGetValue(toID, out var to) && to.Outgoing.Contains(id);
    }

    /// <summary>Gets the closest enabled point to a finite position.</summary>
    /// <param name="toPosition">Query position.</param>
    /// <param name="includeDisabled">Whether disabled points are candidates.</param>
    /// <returns>The lowest ID among points below the pinned 1e20 squared-distance bound, or minus one when none qualify.</returns>
    /// <exception cref="ArgumentException">The query position is nonfinite.</exception>
    public long GetClosestPoint(Vector2 toPosition, bool includeDisabled = false)
    {
        ThrowIfDisposed(); ValidatePosition(toPosition);
        var closest = -1L; var squared = 1e20f;
        foreach (var id in _pointOrder)
        {
            var point = _points[id];
            if (!includeDisabled && !point.Enabled) continue;
            var distance = toPosition.DistanceSquaredTo(point.Position);
            if (distance < squared || distance == squared && id < closest)
            { squared = distance; closest = id; }
        }
        return closest;
    }

    /// <summary>Gets the nearest position on any segment whose two endpoints are enabled.</summary>
    /// <param name="toPosition">Finite query position.</param>
    /// <returns>The closest segment position, or zero when no segment qualifies.</returns>
    /// <exception cref="ArgumentException">The query position is nonfinite.</exception>
    public Vector2 GetClosestPositionInSegment(Vector2 toPosition)
    {
        ThrowIfDisposed(); ValidatePosition(toPosition);
        var closest = Vector2.Zero; var squared = 1e20f;
        foreach (var id in _pointOrder)
        {
            var from = _points[id];
            if (!from.Enabled) continue;
            foreach (var toID in from.Outgoing)
            {
                var to = _points[toID];
                if (!to.Enabled) continue;
                var segment = to.Position - from.Position;
                var lengthSquared = segment.LengthSquared();
                var fraction = lengthSquared == 0 ? 0 : Math.Clamp((toPosition - from.Position).Dot(segment) / lengthSquared, 0, 1);
                var position = from.Position + segment * fraction;
                var distance = toPosition.DistanceSquaredTo(position);
                if (distance < squared) { squared = distance; closest = position; }
            }
        }
        return closest;
    }

    /// <summary>Gets a caller-owned path of point IDs from source to target.</summary>
    /// <param name="fromID">Registered source point ID.</param>
    /// <param name="toID">Registered target point ID.</param>
    /// <param name="allowPartialPath">Whether an unreachable target yields the closest reached point instead.</param>
    /// <returns>An ordered path including endpoints, or an empty array when no route qualifies.</returns>
    /// <exception cref="KeyNotFoundException">Either point is absent.</exception>
    /// <exception cref="InvalidOperationException">A search is re-entered or a cost callback returns an invalid value.</exception>
    public long[] GetIDPath(long fromID, long toID, bool allowPartialPath = false)
    {
        var (start, end) = Search(fromID, toID, allowPartialPath);
        if (end is null) return [];
        var path = new long[PathLength(start, end)];
        var index = path.Length;
        for (var point = end; point is not null; point = point.Previous)
        {
            path[--index] = point.ID;
            if (ReferenceEquals(point, start)) break;
        }
        return path;
    }

    /// <summary>Gets a caller-owned path of point positions from source to target.</summary>
    /// <param name="fromID">Registered source point ID.</param>
    /// <param name="toID">Registered target point ID.</param>
    /// <param name="allowPartialPath">Whether an unreachable target yields the closest reached point instead.</param>
    /// <returns>Current positions along the ordered path, or an empty array when no route qualifies.</returns>
    /// <exception cref="KeyNotFoundException">Either point is absent.</exception>
    /// <exception cref="InvalidOperationException">A search is re-entered or a cost callback returns an invalid value.</exception>
    public Vector2[] GetPointPath(long fromID, long toID, bool allowPartialPath = false)
    {
        var (start, end) = Search(fromID, toID, allowPartialPath);
        if (end is null) return [];
        var path = new Vector2[PathLength(start, end)];
        var index = path.Length;
        for (var point = end; point is not null; point = point.Previous)
        {
            path[--index] = point.Position;
            if (ReferenceEquals(point, start)) break;
        }
        return path;
    }

    /// <summary>Computes the unweighted travel cost between two connected points.</summary>
    /// <param name="fromID">Source point ID.</param>
    /// <param name="toID">Destination point ID.</param>
    /// <returns>The Euclidean distance by default. Overrides must return a finite nonnegative value.</returns>
    protected virtual float OnComputeCost(long fromID, long toID) => GetPoint(fromID).Position.DistanceTo(GetPoint(toID).Position);

    /// <summary>Estimates remaining travel cost to the requested target.</summary>
    /// <param name="fromID">Candidate point ID.</param>
    /// <param name="endID">Target point ID.</param>
    /// <returns>The Euclidean distance by default. Overrides must return a finite nonnegative value.</returns>
    protected virtual float OnEstimateCost(long fromID, long endID) => GetPoint(fromID).Position.DistanceTo(GetPoint(endID).Position);

    /// <summary>Decides whether an outgoing neighbor should be skipped during a search.</summary>
    /// <param name="fromID">Currently expanded point ID.</param>
    /// <param name="neighborID">Neighbor point ID.</param>
    /// <returns>False by default; called only while NeighborFilterEnabled is true.</returns>
    protected virtual bool OnFilterNeighbor(long fromID, long neighborID) => false;

    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        base.ValidateDisposal();
        if (_searching) throw new InvalidOperationException("A graph cannot be disposed during path search.");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _points.Clear(); _pointOrder.Clear(); }
        base.Dispose(disposing);
    }

    private (Point Start, Point? End) Search(long fromID, long toID, bool allowPartialPath)
    {
        ThrowIfDisposed();
        if (_searching) throw new InvalidOperationException("Path search cannot be re-entered.");
        var start = GetPoint(fromID); var target = GetPoint(toID);
        if (!start.Enabled || !target.Enabled && !allowPartialPath) return (start, null);
        if (ReferenceEquals(start, target)) return (start, start);

        _searching = true;
        try
        {
            if (_pass == int.MaxValue)
            {
                foreach (var point in _points.Values) point.OpenPass = point.ClosedPass = 0;
                _pass = 0;
            }
            var pass = ++_pass;
            var queue = new PriorityQueue<(Point Point, int Version), (float F, float NegativeG, long Order)>();
            long order = 0;
            start.G = 0; start.F = Estimate(start.ID, target.ID);
            start.AbsG = 0; start.AbsF = start.F;
            start.OpenPass = pass; start.Version++;
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
                foreach (var neighborID in point.Outgoing)
                {
                    var neighbor = _points[neighborID];
                    if (!neighbor.Enabled || neighbor.ClosedPass == pass ||
                        _neighborFilterEnabled && OnFilterNeighbor(point.ID, neighbor.ID)) continue;
                    var cost = point.G + Compute(point.ID, neighbor.ID) * neighbor.WeightScale;
                    if (!float.IsFinite(cost) || cost < 0)
                        throw new InvalidOperationException("A path cost overflowed finite nonnegative values.");
                    if (neighbor.OpenPass == pass && cost >= neighbor.G) continue;
                    neighbor.OpenPass = pass;
                    neighbor.Previous = point;
                    neighbor.G = cost;
                    neighbor.F = cost + Estimate(neighbor.ID, target.ID);
                    if (!float.IsFinite(neighbor.F))
                        throw new InvalidOperationException("A path estimate overflowed finite values.");
                    neighbor.AbsG = cost; neighbor.AbsF = neighbor.F - cost;
                    neighbor.Version++;
                    queue.Enqueue((neighbor, neighbor.Version), (neighbor.F, -neighbor.G, order++));
                }
            }
            return (start, allowPartialPath ? closest : null);
        }
        finally { _searching = false; }
    }

    private float Estimate(long fromID, long toID)
    {
        var result = OnEstimateCost(fromID, toID);
        if (!float.IsFinite(result) || result < 0)
            throw new InvalidOperationException("An estimated cost must be finite and nonnegative.");
        return result;
    }

    private float Compute(long fromID, long toID)
    {
        var result = OnComputeCost(fromID, toID);
        if (!float.IsFinite(result) || result < 0)
            throw new InvalidOperationException("A computed cost must be finite and nonnegative.");
        return result;
    }

    private static int PathLength(Point start, Point end)
    {
        var count = 1;
        for (var point = end; !ReferenceEquals(point, start); point = point.Previous ?? throw new InvalidOperationException("The path chain is incomplete."))
            count++;
        return count;
    }

    private Point GetPoint(long id)
    {
        ThrowIfDisposed();
        return _points.TryGetValue(id, out var point) ? point : throw new KeyNotFoundException($"Point {id} does not exist.");
    }

    private void EnsureMutableGraph()
    {
        ThrowIfDisposed();
        if (_searching) throw new InvalidOperationException("A graph cannot be changed during path search.");
    }

    private static void ValidateID(long id)
    {
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
    }

    private static void ValidatePosition(Vector2 position)
    {
        if (!position.IsFinite()) throw new ArgumentException("A point position must be finite.", nameof(position));
    }

    private static void ValidateWeight(float weight)
    {
        if (!float.IsFinite(weight) || weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
    }

    private static void AddOutgoing(Point point, long id)
    {
        if (!point.Outgoing.Contains(id)) point.Outgoing.Add(id);
    }

    private static void RemoveOutgoing(Point point, long id) => RemoveID(point.Outgoing, id);

    private static void RemoveID(List<long> ids, long id)
    {
        var index = ids.IndexOf(id);
        if (index < 0) return;
        ids[index] = ids[^1];
        ids.RemoveAt(ids.Count - 1);
    }

    private sealed class Point(long id, Vector2 position, float weightScale)
    {
        internal readonly long ID = id;
        internal Vector2 Position = position;
        internal float WeightScale = weightScale;
        internal bool Enabled = true;
        internal readonly List<long> Outgoing = [];
        internal Point? Previous;
        internal float G, F, AbsG, AbsF;
        internal int OpenPass, ClosedPass, Version;
    }
}
