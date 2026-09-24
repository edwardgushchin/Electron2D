using Electron2D;

internal static class AStarTests
{
    internal static void Run()
    {
        PointsAndCapacity();
        DirectedConnectionsAndSegments();
        WeightedAndPartialPaths();
        CustomCallbacksAndFailure();
        LongPathAndLifetime();
        Console.WriteLine("AStar graph, directed path, weight, callback and lifetime checks passed.");
    }

    private static void PointsAndCapacity()
    {
        using var graph = new AStar();
        Check(graph.GetPointCount() == 0 && graph.GetPointCapacity() == 16 &&
              graph.GetAvailablePointID() == 0 && !graph.NeighborFilterEnabled &&
              graph.GetClosestPoint(Vector2.Zero) == -1 &&
              graph.GetClosestPositionInSegment(Vector2.Zero) == Vector2.Zero,
            "The empty graph retains its exact defaults.");
        Reject<ArgumentOutOfRangeException>(() => graph.AddPoint(-1, Vector2.Zero));
        Reject<ArgumentException>(() => graph.AddPoint(1, new(float.NaN, 0)));
        Reject<ArgumentOutOfRangeException>(() => graph.AddPoint(1, Vector2.Zero, -1));
        Reject<ArgumentOutOfRangeException>(() => graph.AddPoint(1, Vector2.Zero, float.PositiveInfinity));
        Reject<ArgumentOutOfRangeException>(() => graph.ReserveSpace(0));
        Check(graph.GetPointCount() == 0 && graph.GetPointCapacity() == 16,
            "Invalid point and capacity requests do not mutate the graph.");

        graph.ReserveSpace(5);
        Check(graph.GetPointCapacity() == 8, "A pre-allocation reservation rounds to the pinned power-of-two capacity.");
        graph.AddPoint(1, new(1, 2), 2);
        graph.AddPoint(2, new(2, 3));
        graph.AddPoint(3, new(3, 4));
        graph.AddPoint(1, new(10, 20), 4);
        Check(graph.GetPointCount() == 3 && graph.GetPointCapacity() == 8 &&
              graph.GetPointPosition(1) == new Vector2(10, 20) &&
              graph.GetPointWeightScale(1) == 4 && graph.GetPointIDs().SequenceEqual([1, 2, 3]),
            "Updating a point retains count and storage order while replacing coordinates and weight.");
        graph.SetPointPosition(2, new(5, 6));
        graph.SetPointWeightScale(2, 0);
        Check(graph.GetPointPosition(2) == new Vector2(5, 6) && graph.GetPointWeightScale(2) == 0,
            "Point setters commit finite values, including zero weight.");
        Reject<KeyNotFoundException>(() => graph.SetPointPosition(9, Vector2.Zero));
        Reject<KeyNotFoundException>(() => graph.GetPointPosition(9));
        graph.RemovePoint(2);
        Check(graph.GetPointIDs().SequenceEqual([1, 3]) && graph.GetAvailablePointID() == 2,
            "Removal swaps the final storage entry and makes the removed ID available.");
        graph.AddPoint(4, Vector2.Zero);
        graph.RemovePoint(1);
        Check(graph.GetPointIDs().SequenceEqual([4, 3]) && graph.GetAvailablePointID() == 1,
            "The reported ID order follows swap-last removal.");
        graph.ReserveSpace(20);
        Check(graph.GetPointCapacity() == 32, "Reservation grows to the next power of two.");
        graph.Clear();
        Check(graph.GetPointCount() == 0 && graph.GetPointCapacity() == 32 && graph.GetAvailablePointID() == 0,
            "Clear keeps capacity but resets IDs and all points.");
        using (var growth = new AStar())
        {
            for (var id = 0; id < 12; id++) growth.AddPoint(id, new(id, 0));
            Check(growth.GetPointCapacity() == 16 && growth.GetAvailablePointID() == 12,
                "The first twelve additions retain initial capacity and advance the available ID.");
            growth.AddPoint(12, new(12, 0));
            Check(growth.GetPointCapacity() == 32, "The thirteenth addition crosses the pinned load threshold.");
        }
        graph.AddPoint(long.MaxValue, Vector2.Zero);
        graph.RemovePoint(long.MaxValue);
        graph.AddPoint(long.MaxValue, Vector2.Zero);
        Reject<InvalidOperationException>(() => graph.GetAvailablePointID());
        using var distant = new AStar();
        distant.AddPoint(7, new(1e11f, 0));
        Check(distant.GetClosestPoint(Vector2.Zero) == -1,
            "The pinned nearest-point scan retains its finite squared-distance bound.");
    }

    private static void DirectedConnectionsAndSegments()
    {
        using var graph = new AStar();
        graph.AddPoint(1, new(0, 0));
        graph.AddPoint(2, new(0, 5));
        graph.AddPoint(3, new(10, 0));
        graph.ConnectPoints(1, 2, bidirectional: false);
        graph.AddPoint(1, new(0, 0), 2);
        Check(graph.ArePointsConnected(1, 2) && graph.ArePointsConnected(2, 1) &&
              graph.ArePointsConnected(1, 2, bidirectional: false) &&
              !graph.ArePointsConnected(2, 1, bidirectional: false) &&
              graph.GetPointConnections(1).SequenceEqual([2]) && graph.GetPointConnections(2).Length == 0 &&
              graph.GetClosestPositionInSegment(new(3, 3)) == new Vector2(0, 3),
            "A one-way edge is still one physical segment with directed travel.");
        graph.ConnectPoints(2, 1, bidirectional: false);
        Check(graph.ArePointsConnected(2, 1, bidirectional: false), "A reverse edge upgrades one-way travel.");
        graph.DisconnectPoints(1, 2, bidirectional: false);
        Check(!graph.ArePointsConnected(1, 2, bidirectional: false) &&
              graph.ArePointsConnected(2, 1, bidirectional: false) &&
              graph.ArePointsConnected(1, 2), "Removing one direction preserves the reverse segment.");
        graph.SetPointDisabled(2);
        Check(graph.IsPointDisabled(2) && graph.GetClosestPositionInSegment(new(3, 3)) == Vector2.Zero &&
              graph.GetClosestPoint(new(0, 4)) == 1 && graph.GetClosestPoint(new(0, 4), includeDisabled: true) == 2,
            "Disabled endpoints are excluded from segment and nearest-point queries by default.");
        graph.SetPointDisabled(2, false);
        graph.ConnectPoints(2, 3);
        graph.RemovePoint(2);
        Check(!graph.ArePointsConnected(1, 2) && !graph.ArePointsConnected(3, 2) &&
              graph.GetPointConnections(1).Length == 0 && graph.GetPointConnections(3).Length == 0,
            "Removing a point clears incoming and outgoing edges.");
        Reject<ArgumentException>(() => graph.ConnectPoints(1, 1));
        Reject<KeyNotFoundException>(() => graph.ConnectPoints(1, 9));
        Reject<KeyNotFoundException>(() => graph.DisconnectPoints(1, 9));
        Check(!graph.ArePointsConnected(1, 9), "Missing connection queries return false.");
        using var ties = new AStar();
        ties.AddPoint(9, new(0, 0)); ties.AddPoint(3, new(2, 0));
        Check(ties.GetClosestPoint(new(1, 0)) == 3,
            "Equal nearest distances select the lowest ID regardless of storage order.");
    }

    private static void WeightedAndPartialPaths()
    {
        using var graph = new AStar();
        graph.AddPoint(1, new(0, 0)); graph.AddPoint(2, new(0, 1));
        graph.AddPoint(3, new(1, 1)); graph.AddPoint(4, new(2, 0));
        graph.ConnectPoints(1, 2, false); graph.ConnectPoints(2, 3, false);
        graph.ConnectPoints(4, 3, false); graph.ConnectPoints(1, 4, false);
        Check(graph.GetIDPath(1, 3).SequenceEqual([1, 2, 3]) &&
              graph.GetPointPath(1, 3).SequenceEqual([new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1)]),
            "The pinned four-point directed route chooses the shorter path.");
        graph.SetPointWeightScale(2, 3);
        var weighted = graph.GetIDPath(1, 3);
        Check(weighted.SequenceEqual([1, 4, 3]), "A destination weight redirects the pinned path through point four.");
        weighted[0] = 99;
        Check(graph.GetIDPath(1, 3).SequenceEqual([1, 4, 3]), "Returned path arrays are caller-owned snapshots.");
        graph.SetPointDisabled(1);
        Check(graph.GetIDPath(1, 1).Length == 0 && graph.GetIDPath(1, 3, true).Length == 0,
            "A disabled source yields no path even to itself or with partial routing.");
        graph.SetPointDisabled(1, false);
        Check(graph.GetIDPath(1, 1).SequenceEqual([1]), "A live point routes to itself.");
        graph.SetPointDisabled(3);
        Check(graph.GetIDPath(1, 3).Length == 0 && graph.GetIDPath(1, 3, true).SequenceEqual([1, 2]),
            "A disabled target has no full route, while partial routing ends at the nearest reachable point.");
        graph.SetPointDisabled(3, false);
        graph.DisconnectPoints(1, 2, false); graph.DisconnectPoints(1, 4, false);
        Check(graph.GetIDPath(1, 3).Length == 0 && graph.GetIDPath(1, 3, true).SequenceEqual([1]),
            "An isolated source is the only partial endpoint.");
        Reject<KeyNotFoundException>(() => graph.GetIDPath(1, 9));
    }

    private static void CustomCallbacksAndFailure()
    {
        using var graph = new CustomAStar();
        graph.AddPoint(1, new(0, 0)); graph.AddPoint(2, new(10, 0));
        graph.AddPoint(3, new(1, 0)); graph.AddPoint(4, new(11, 0));
        graph.ConnectPoints(1, 2, false); graph.ConnectPoints(1, 3, false);
        graph.ConnectPoints(3, 2, false); graph.ConnectPoints(2, 4, false);
        graph.DirectCost = 10;
        Check(graph.GetIDPath(1, 4).SequenceEqual([1, 3, 2, 4]) &&
              graph.ComputeCalls > 0 && graph.EstimateCalls > 0,
            "Typed cost hooks and decrease-key search select the lower-cost route.");
        graph.NeighborFilterEnabled = true;
        graph.FilterThree = true;
        Check(graph.GetIDPath(1, 4).SequenceEqual([1, 2, 4]) && graph.FilterCalls > 0,
            "The filter callback runs only when enabled and skips a selected neighbor.");
        graph.NeighborFilterEnabled = false;
        graph.FilterCalls = 0;
        graph.GetIDPath(1, 4);
        Check(graph.FilterCalls == 0, "A disabled filter is not called.");
        graph.MutateInCompute = true;
        Reject<InvalidOperationException>(() => graph.GetIDPath(1, 4));
        Check(graph.GetPointCount() == 4, "A callback cannot mutate graph topology during search.");
        graph.MutateInCompute = false;
        graph.ReenterInCompute = true;
        Reject<InvalidOperationException>(() => graph.GetIDPath(1, 4));
        graph.ReenterInCompute = false;
        graph.ReturnInvalidCost = true;
        Reject<InvalidOperationException>(() => graph.GetIDPath(1, 4));
        graph.ReturnInvalidCost = false;
        graph.ReturnInvalidEstimate = true;
        Reject<InvalidOperationException>(() => graph.GetIDPath(1, 4));
        graph.ReturnInvalidEstimate = false;
        graph.DisposeInCompute = true;
        Reject<InvalidOperationException>(() => graph.GetIDPath(1, 4));
        Check(!graph.IsDisposed, "Disposal from a search callback rejects before changing graph lifetime.");
        graph.DisposeInCompute = false;
        Check(graph.GetIDPath(1, 4).SequenceEqual([1, 3, 2, 4]),
            "Failure cleanup permits a later search over the unchanged graph.");
    }

    private static void LongPathAndLifetime()
    {
        var graph = new AStar();
        const int count = 1024;
        graph.ReserveSpace(count);
        for (var id = 0; id < count; id++)
        {
            graph.AddPoint(id, new(id, 0));
            if (id > 0) graph.ConnectPoints(id - 1, id, false);
        }
        var path = graph.GetIDPath(0, count - 1);
        Check(path.Length == count && path[0] == 0 && path[^1] == count - 1 &&
              graph.GetPointCapacity() >= count, "A long directed path stays iterative and respects reservation.");
        graph.Dispose();
        Reject<ObjectDisposedException>(() => graph.GetPointCount());
        Reject<ObjectDisposedException>(() => graph.AddPoint(0, Vector2.Zero));
    }

    private sealed class CustomAStar : AStar
    {
        internal int ComputeCalls;
        internal int EstimateCalls;
        internal int FilterCalls;
        internal float DirectCost;
        internal bool FilterThree;
        internal bool MutateInCompute;
        internal bool ReenterInCompute;
        internal bool ReturnInvalidCost;
        internal bool ReturnInvalidEstimate;
        internal bool DisposeInCompute;

        protected override float OnComputeCost(long fromID, long toID)
        {
            ComputeCalls++;
            if (MutateInCompute) AddPoint(9, Vector2.Zero);
            if (ReenterInCompute) GetIDPath(fromID, toID);
            if (ReturnInvalidCost) return float.NaN;
            if (DisposeInCompute) Dispose();
            return fromID == 1 && toID == 2 ? DirectCost : 1;
        }

        protected override float OnEstimateCost(long fromID, long endID)
        { EstimateCalls++; return ReturnInvalidEstimate ? float.NaN : 0; }

        protected override bool OnFilterNeighbor(long fromID, long neighborID)
        { FilterCalls++; return FilterThree && neighborID == 3; }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
