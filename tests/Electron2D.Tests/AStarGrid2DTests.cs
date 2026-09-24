using Electron2D;

internal static class AStarGrid2DTests
{
    internal static void Run()
    {
        GeometryAndState();
        PathsAndDiagonals();
        JumpingAndCallbacks();
        Console.WriteLine("AStarGrid2D geometry, paths, jumping and lifetime checks passed.");
    }

    private static void GeometryAndState()
    {
        using var grid = new AStarGrid2D();
        Check(grid.Region == default && grid.Size == Vector2i.Zero &&
              grid.Offset == Vector2.Zero && grid.CellSize == Vector2.One &&
              grid.Shape == AStarGrid2D.CellShape.Square &&
              grid.Diagonals == AStarGrid2D.DiagonalMode.Always &&
              grid.DefaultComputeHeuristic == AStarGrid2D.Heuristic.Euclidean &&
              grid.DefaultEstimateHeuristic == AStarGrid2D.Heuristic.Euclidean &&
              !grid.JumpingEnabled && !grid.IsDirty(), "The empty grid retains its pinned defaults.");
        Check((int)AStarGrid2D.CellShape.Square == 0 &&
              (int)AStarGrid2D.CellShape.IsometricRight == 1 &&
              (int)AStarGrid2D.CellShape.IsometricDown == 2 &&
              (int)AStarGrid2D.CellShape.Max == 3 &&
              (int)AStarGrid2D.DiagonalMode.Always == 0 &&
              (int)AStarGrid2D.DiagonalMode.Never == 1 &&
              (int)AStarGrid2D.DiagonalMode.AtLeastOneWalkable == 2 &&
              (int)AStarGrid2D.DiagonalMode.OnlyIfNoObstacles == 3 &&
              (int)AStarGrid2D.DiagonalMode.Max == 4 &&
              (int)AStarGrid2D.Heuristic.Euclidean == 0 &&
              (int)AStarGrid2D.Heuristic.Manhattan == 1 &&
              (int)AStarGrid2D.Heuristic.Octile == 2 &&
              (int)AStarGrid2D.Heuristic.Chebyshev == 3 &&
              (int)AStarGrid2D.Heuristic.Max == 4,
            "All three enum families retain their pinned numeric identities.");
        Reject<ArgumentOutOfRangeException>(() => grid.Region = new Rect2i(0, 0, -1, 1));
        Reject<ArgumentOutOfRangeException>(() => grid.Region = new Rect2i(int.MaxValue, 0, 2, 1));
        Reject<ArgumentException>(() => grid.Offset = new(float.NaN, 0));
        Reject<ArgumentException>(() => grid.CellSize = new(float.PositiveInfinity, 1));
        Reject<ArgumentOutOfRangeException>(() => grid.Shape = AStarGrid2D.CellShape.Max);
        Check(!grid.IsDirty(), "Invalid geometry rejects before dirty-state mutation.");

        grid.Region = new Rect2i(-2, 3, 3, 2);
        Check(grid.IsDirty() && grid.IsInBounds(-2, 3) && grid.IsInBoundsV(new(0, 4)) &&
              !grid.IsInBounds(1, 4), "Region bounds update immediately, before cell reconstruction.");
        Reject<InvalidOperationException>(() => grid.GetPointPosition(new(-2, 3)));
        grid.CellSize = new(2, 4); grid.Offset = new(1, -3);
        grid.Update();
        Check(!grid.IsDirty() && grid.GetPointPosition(new(-2, 3)) == new Vector2(-3, 9) &&
              grid.GetPointWeightScale(new(-2, 3)) == 1 && !grid.IsPointSolid(new(-2, 3)),
            "Update builds current positions, default weight and walkability.");
        grid.Size = new Vector2i(2, 2);
        Check(grid.Region.Position == new Vector2i(-2, 3) && grid.IsDirty(),
            "The legacy size projection retains the region origin and invalidates geometry.");
        grid.Update();
        grid.SetPointSolid(new(-2, 3));
        grid.SetPointWeightScale(new(-1, 4), 2);
        Check(!grid.IsDirty() && grid.IsPointSolid(new(-2, 3)) && grid.GetPointWeightScale(new(-1, 4)) == 2,
            "Cell flags and weights act immediately without Update.");
        grid.FillSolidRegion(new Rect2i(-3, 3, 2, 1), false);
        grid.FillWeightScaleRegion(new Rect2i(-2, 4, 20, 2), 3);
        var data = grid.GetPointDataInRegion(new Rect2i(-4, 3, 8, 2));
        Check(data.Length == 4 && data[0].ID == new Vector2i(-2, 3) && !data[0].Solid &&
              data[2].ID == new Vector2i(-2, 4) && data[2].WeightScale == 3 &&
              data[3].ID == new Vector2i(-1, 4) && data[3].WeightScale == 3,
            "Clipped region data preserves row-major IDs, positions, flags and weights.");
        Check(grid.GetPointDataInRegion(new Rect2i(9, 9, 2, 2)).Length == 0,
            "Nonintersecting region data is empty.");
        Reject<ArgumentOutOfRangeException>(() => grid.SetPointWeightScale(new(-2, 3), -1));
        Reject<ArgumentOutOfRangeException>(() => grid.FillWeightScaleRegion(grid.Region, float.NaN));
        Reject<KeyNotFoundException>(() => grid.SetPointSolid(new(2, 2)));
        Check(grid.GetPointWeightScale(new(-2, 3)) == 1,
            "Invalid writes leave existing cell weights intact.");
        data[0] = default;
        Check(grid.GetPointDataInRegion(grid.Region)[0].ID == new Vector2i(-2, 3),
            "Region data arrays are caller-owned snapshots.");
        grid.Offset = new(2, -3); grid.Update();
        Check(!grid.IsPointSolid(new(-2, 3)) && grid.GetPointWeightScale(new(-1, 4)) == 1,
            "Geometry reconstruction resets obstacle and weight data.");

        grid.Region = new Rect2i(1, 2, 1, 1);
        grid.Offset = Vector2.One; grid.CellSize = new(4, 6);
        grid.Shape = AStarGrid2D.CellShape.IsometricRight; grid.Update();
        Check(grid.GetPointPosition(new(1, 2)) == new Vector2(9, 7),
            "Right isometric geometry uses the pinned half-cell transform.");
        grid.Shape = AStarGrid2D.CellShape.IsometricDown; grid.Update();
        Check(grid.GetPointPosition(new(1, 2)) == new Vector2(1, 13),
            "Down isometric geometry uses the pinned half-cell transform.");
        grid.Region = new Rect2i(0, 0, 1, 1);
        grid.Clear();
        Check(grid.Region == default && grid.IsDirty(), "Clear resets region and preserves the current dirty flag.");
        grid.Update();
        Check(!grid.IsDirty() && !grid.IsInBounds(0, 0), "Updating a cleared grid yields no cells.");
    }

    private static void PathsAndDiagonals()
    {
        using var grid = new AStarGrid2D { Region = new Rect2i(0, 0, 4, 5) };
        grid.Update();
        var start = Vector2i.Zero;
        var end = new Vector2i(3, 4);
        Check(grid.GetIDPath(start, end).SequenceEqual([
            new(0, 0), new(1, 1), new(2, 2), new(3, 3), new(3, 4)]) &&
              grid.GetPointPath(start, end).SequenceEqual([
                  new(0, 0), new(1, 1), new(2, 2), new(3, 3), new(3, 4)]),
            "The pinned class fixture follows four diagonal steps and one cardinal step.");
        grid.SetPointSolid(start);
        Check(grid.GetIDPath(start, start).Length == 0 && grid.GetIDPath(start, end, true).Length == 0,
            "A solid source never yields a path.");
        grid.SetPointSolid(start, false);
        Check(grid.GetIDPath(start, start).SequenceEqual([start]), "A walkable point routes to itself.");
        grid.SetPointSolid(end);
        Check(grid.GetIDPath(start, end).Length == 0 &&
              grid.GetIDPath(start, end, true).SequenceEqual([
                  new(0, 0), new(1, 1), new(2, 2), new(3, 3)]),
            "A solid target has no full route but allows a closest-reached partial route.");
        grid.SetPointSolid(end, false);
        Reject<KeyNotFoundException>(() => grid.GetIDPath(start, new(9, 9)));

        using var corner = new AStarGrid2D { Region = new Rect2i(0, 0, 2, 2) };
        corner.Update();
        corner.SetPointSolid(new(1, 0)); corner.SetPointSolid(new(0, 1));
        var diagonal = new Vector2i(1, 1);
        Check(corner.GetIDPath(start, diagonal).SequenceEqual([start, diagonal]),
            "Always permits a diagonal through two blocked cardinal neighbors.");
        corner.Diagonals = AStarGrid2D.DiagonalMode.AtLeastOneWalkable;
        Check(corner.GetIDPath(start, diagonal).Length == 0,
            "AtLeastOneWalkable rejects a fully blocked corner.");
        corner.SetPointSolid(new(0, 1), false);
        Check(corner.GetIDPath(start, diagonal).SequenceEqual([start, diagonal]),
            "AtLeastOneWalkable accepts one open side of a corner.");
        corner.Diagonals = AStarGrid2D.DiagonalMode.OnlyIfNoObstacles;
        Check(corner.GetIDPath(start, diagonal).SequenceEqual([start, new Vector2i(0, 1), diagonal]),
            "OnlyIfNoObstacles takes the remaining cardinal detour.");
        corner.Diagonals = AStarGrid2D.DiagonalMode.Never;
        Check(corner.GetIDPath(start, diagonal).SequenceEqual([start, new Vector2i(0, 1), diagonal]),
            "Never uses cardinal neighbors only.");
        Reject<ArgumentOutOfRangeException>(() => corner.Diagonals = AStarGrid2D.DiagonalMode.Max);
        Reject<ArgumentOutOfRangeException>(() => corner.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Max);

        using var weights = new AStarGrid2D { Region = new Rect2i(0, 0, 3, 2), Diagonals = AStarGrid2D.DiagonalMode.Never };
        weights.Update(); weights.SetPointWeightScale(new(1, 0), 10);
        Check(weights.GetIDPath(start, new(2, 0)).SequenceEqual([
            start, new(0, 1), new(1, 1), new(2, 1), new(2, 0)]),
            "Entry weights redirect the path through a longer cheap route.");
        foreach (var heuristic in new[] { AStarGrid2D.Heuristic.Manhattan, AStarGrid2D.Heuristic.Octile,
                     AStarGrid2D.Heuristic.Chebyshev })
        {
            weights.DefaultComputeHeuristic = heuristic;
            weights.DefaultEstimateHeuristic = heuristic;
            Check(weights.GetIDPath(start, new(2, 0)).Length > 0,
                "Every built-in heuristic participates in a full route.");
        }
        Check(!weights.IsDirty(), "Path policy changes do not invalidate geometry.");
        using var probe = new HeuristicProbe();
        var target = new Vector2i(3, 4);
        probe.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Euclidean;
        Check(probe.Cost(target) == 5, "Euclidean cost uses straight-line ID distance.");
        probe.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan;
        Check(probe.Cost(target) == 7, "Manhattan cost sums ID axis distances.");
        probe.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Octile;
        Check(MathF.Abs(probe.Cost(target) - (float)(3 * Math.Sqrt(2) + 1)) < 1e-5f,
            "Octile cost uses three diagonals and one cardinal step.");
        probe.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Chebyshev;
        Check(probe.Cost(target) == 4, "Chebyshev cost uses the larger ID axis distance.");
    }

    private static void JumpingAndCallbacks()
    {
        using var grid = new AStarGrid2D { Region = new Rect2i(0, 0, 6, 6), JumpingEnabled = true };
        grid.Update();
        var start = Vector2i.Zero;
        var end = new Vector2i(5, 5);
        grid.SetPointWeightScale(new(1, 1), 50);
        Check(grid.GetIDPath(start, end).SequenceEqual([start, end]) &&
              grid.GetPointPath(start, end).SequenceEqual([Vector2.Zero, new Vector2(5, 5)]),
            "Jumping emits sparse jump points and ignores intermediate cell weights.");
        grid.Diagonals = AStarGrid2D.DiagonalMode.AtLeastOneWalkable;
        Check(grid.GetIDPath(start, end).SequenceEqual([start, end]),
            "Jumping follows the at-least-one-walkable diagonal scan.");
        grid.Diagonals = AStarGrid2D.DiagonalMode.OnlyIfNoObstacles;
        Check(grid.GetIDPath(start, end).SequenceEqual([start, end]),
            "Jumping follows the unobstructed diagonal scan.");
        grid.Diagonals = AStarGrid2D.DiagonalMode.Never;
        Check(grid.GetIDPath(start, new(0, 5)).SequenceEqual([start, new Vector2i(0, 5)]),
            "Jumping follows the cardinal forced-successor scan.");
        grid.Diagonals = AStarGrid2D.DiagonalMode.Always;
        grid.SetPointSolid(new(1, 1));
        var obstaclePath = grid.GetIDPath(start, end);
        Check(obstaclePath.Length > 0 && obstaclePath[0] == start && obstaclePath[^1] == end &&
              !obstaclePath.Contains(new Vector2i(1, 1)),
            "Jumping finds forced successors around an obstacle without entering it.");
        grid.SetPointSolid(new(1, 1), false);
        grid.JumpingEnabled = false;
        Check(grid.GetIDPath(start, end).Length > 2,
            "Ordinary weighted search returns intermediate cells.");

        using var custom = new CustomGrid { Region = new Rect2i(0, 0, 3, 1) };
        custom.Update();
        Check(custom.GetIDPath(start, new(2, 0)).Length == 3 &&
              custom.ComputeCalls > 0 && custom.EstimateCalls > 0,
            "Protected typed cost and estimate hooks run during search.");
        custom.Mutate = true;
        Reject<InvalidOperationException>(() => custom.GetIDPath(start, new(2, 0)));
        custom.Mutate = false;
        custom.Reenter = true;
        Reject<InvalidOperationException>(() => custom.GetIDPath(start, new(2, 0)));
        custom.Reenter = false;
        custom.InvalidCost = true;
        Reject<InvalidOperationException>(() => custom.GetIDPath(start, new(2, 0)));
        custom.InvalidCost = false;
        custom.InvalidEstimate = true;
        Reject<InvalidOperationException>(() => custom.GetIDPath(start, new(2, 0)));
        custom.InvalidEstimate = false;
        custom.DisposeInside = true;
        Reject<InvalidOperationException>(() => custom.GetIDPath(start, new(2, 0)));
        Check(!custom.IsDisposed, "Disposal from a callback rejects without invalidating the grid.");
        custom.DisposeInside = false;
        Check(custom.GetIDPath(start, new(2, 0)).Length == 3,
            "A failed callback does not prevent a later search.");
        custom.Dispose();
        Reject<ObjectDisposedException>(() => custom.IsDirty());
    }

    private sealed class CustomGrid : AStarGrid2D
    {
        internal int ComputeCalls, EstimateCalls;
        internal bool Mutate, Reenter, InvalidCost, InvalidEstimate, DisposeInside;

        protected override float OnComputeCost(Vector2i fromID, Vector2i toID)
        {
            ComputeCalls++;
            if (Mutate) SetPointSolid(toID);
            if (Reenter) GetIDPath(fromID, toID);
            if (DisposeInside) Dispose();
            return InvalidCost ? float.NaN : 1;
        }

        protected override float OnEstimateCost(Vector2i fromID, Vector2i endID)
        { EstimateCalls++; return InvalidEstimate ? float.NaN : 0; }
    }

    private sealed class HeuristicProbe : AStarGrid2D
    {
        internal float Cost(Vector2i toID) => base.OnComputeCost(Vector2i.Zero, toID);
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
