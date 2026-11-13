namespace Electron2D;

internal sealed partial class NavigationMapIteration
{
    private readonly record struct CorridorArrival(int Cell, Vector2 Entry, Portal Portal, int Parent, double Cost,
        (int Source, int PortalIndex) Key);

    internal Vector2[] Path(Vector2 origin, Vector2 destination, bool optimize, uint layers) => Query(NavigationPathQuerySettings.Default with { StartPosition = origin, TargetPosition = destination, NavigationLayers = layers, MetadataFlags = NavigationPathQueryParameters.PathMetadataFlags.IncludeNone, PostProcessing = optimize ? NavigationPathQueryParameters.PathPostProcessing.CorridorFunnel : NavigationPathQueryParameters.PathPostProcessing.EdgeCentered }).Path;
    internal NavigationPathQueryData Query(NavigationPathQuerySettings settings)
    {
        var usable = new bool[Cells.Length];
        var minimumTravelCost = float.PositiveInfinity;
        for (var i = 0; i < Cells.Length; i++)
        {
            var cell = Cells[i];
            usable[i] = cell.Enabled && (cell.Layers & settings.NavigationLayers) != 0 &&
                (cell.IsLink ? Allowed(cell.LinkStartRegion) && Allowed(cell.LinkEndRegion) : Allowed(cell.Region));
            if (usable[i])
                minimumTravelCost = Math.Min(minimumTravelCost, cell.TravelCost);
        }
        var start = Project(settings.StartPosition, out var from); var finish = Project(settings.TargetPosition, out var to);
        if (start < 0 || finish < 0) return NavigationPathQueryData.Empty;
        var points = new List<(Vector2 Point, int Cell)>();
        var samePolygon = start == finish;
        if (samePolygon && (from == to || Cells[start].TravelCost == minimumTravelCost))
        {
            Add(points, from, start);
            Add(points, to, finish);
            return Complete(points);
        }
        var bestArrivals = new Dictionary<(int Source, int PortalIndex), int>();
        var processedPolygons = new bool[Cells.Length];
        var arrivals = new List<CorridorArrival>();
        var queue = new PriorityQueue<int, (double Total, double NegativeCost, int Polygon, int Sequence)>();
        var target = settings.TargetPosition;
        var nearestReached = -1;
        var nearestEntryDistance = double.PositiveInfinity;
        var processed = 0;
        var limitReached = false;
        var limitDistanceSquared = (double)settings.PathSearchMaxDistance * settings.PathSearchMaxDistance;
        var reached = FindCorridor(finish, to, trackFrontier: !samePolygon);
        if (reached < 0)
        {
            to = Closest(Cells[start], target);
            if (nearestReached >= 0)
            {
                var reachablePoint = Closest(Cells[nearestReached], target);
                if (DistanceSquared(reachablePoint, target) <= DistanceSquared(to, target))
                {
                    reached = FindCorridor(nearestReached, reachablePoint, trackFrontier: false);
                    if (reached >= 0)
                        to = reachablePoint;
                }
            }
        }
        if (reached < 0) { Add(points, from, start); Add(points, to, start); return Complete(points); }
        var best = arrivals[reached].Cell;
        var route = new List<int>();
        for (var at = reached; at != 0; at = arrivals[at].Parent)
            route.Add(at);
        route.Reverse();
        if (settings.PostProcessing == NavigationPathQueryParameters.PathPostProcessing.CorridorFunnel)
        {
            var corridor = new List<(Vector2 Left, Vector2 Right, int Cell)>(route.Count);
            foreach (var arrivalIndex in route)
            {
                var arrival = arrivals[arrivalIndex];
                var edge = arrival.Portal;
                var mid = edge.A * .5f + edge.B * .5f;
                var a = Cells[arrivals[arrival.Parent].Cell].Center;
                var b = Cells[arrival.Cell].Center;
                var left = ((double)b.X - a.X) * ((double)edge.A.Y - mid.Y) - ((double)b.Y - a.Y) * ((double)edge.A.X - mid.X) >= 0;
                corridor.Add(left ? (edge.A, edge.B, arrival.Cell) : (edge.B, edge.A, arrival.Cell));
            }
            points = FunnelWithProvenance(from, to, start, best, corridor);
        }
        else
        {
            Add(points, from, start);
            var centered = settings.PostProcessing == NavigationPathQueryParameters.PathPostProcessing.EdgeCentered;
            foreach (var arrivalIndex in route)
            {
                var arrival = arrivals[arrivalIndex];
                var portal = arrival.Portal;
                Add(points, centered ? portal.Centered ?? portal.A * .5f + portal.B * .5f : arrival.Entry, arrival.Cell);
            }
            Add(points, to, best);
        }
        return Complete(points);

        int FindCorridor(int goal, Vector2 destination, bool trackFrontier)
        {
            bestArrivals.Clear();
            var startKey = (start, -1);
            bestArrivals.Add(startKey, 0);
            arrivals.Clear();
            arrivals.Add(new(start, from, default, -1, 0, startKey));
            queue.Clear();
            queue.Enqueue(0, (Math.Sqrt(DistanceSquared(from, destination)) * minimumTravelCost, 0, start, 0));
            var startsInGoal = start == goal;
            var completed = startsInGoal ? 0 : -1;
            var completedCost = startsInGoal
                ? Math.Sqrt(DistanceSquared(from, destination)) * Cells[start].TravelCost
                : double.PositiveInfinity;
            while (queue.TryDequeue(out var arrivalIndex, out var priority))
            {
                var current = arrivals[arrivalIndex];
                var index = current.Cell;
                if (bestArrivals[current.Key] != arrivalIndex)
                    continue;
                if (priority.Total >= completedCost)
                    break;

                var cell = Cells[index];
                if (index == goal)
                {
                    if (!startsInGoal)
                        return arrivalIndex;

                    // A return to the start polygon can finish here or continue via another shortcut.
                    var cost = current.Cost + Math.Sqrt(DistanceSquared(current.Entry, destination)) * cell.TravelCost;
                    if (cost < completedCost)
                    {
                        completed = arrivalIndex;
                        completedCost = cost;
                    }
                }
                if (trackFrontier && index != start && !cell.IsLink)
                {
                    var remaining = DistanceSquared(current.Entry, target);
                    if (remaining < nearestEntryDistance)
                    {
                        nearestEntryDistance = remaining;
                        nearestReached = index;
                    }
                }

                // Once a limit ends exploration, the second pass can finish the corridor to the reached polygon.
                if (!processedPolygons[index])
                {
                    processedPolygons[index] = true;
                    processed++;
                }
                if (!limitReached && (settings.PathSearchMaxPolygons > 0 && processed >= settings.PathSearchMaxPolygons ||
                    limitDistanceSquared > 0 && DistanceSquared(from, current.Entry) > limitDistanceSquared))
                {
                    limitReached = true;
                    break;
                }

                var portals = Edges[index];
                for (var portalIndex = 0; portalIndex < portals.Length; portalIndex++)
                {
                    var portal = portals[portalIndex];
                    var next = portal.Target;
                    if (!usable[next])
                        continue;
                    var point = ClosestSegment(current.Entry, portal.A, portal.B);
                    var travel = Math.Sqrt(DistanceSquared(current.Entry, point)) * cell.TravelCost;
                    var enter = cell.Region != Cells[next].Region ? Cells[next].EnterCost : 0;
                    var cost = current.Cost + travel + enter;
                    var remaining = Math.Sqrt(DistanceSquared(point, destination));
                    var final = next == goal && !startsInGoal;
                    if (final)
                        cost += remaining * Cells[goal].TravelCost;
                    // Different incoming portals can have different costs to reach the next exit.
                    var key = final ? (goal, -1) : (index, portalIndex);
                    if (bestArrivals.TryGetValue(key, out var previous) && cost >= arrivals[previous].Cost)
                        continue;
                    var sequence = arrivals.Count;
                    bestArrivals[key] = sequence;
                    var estimate = final ? 0 : remaining * minimumTravelCost;
                    queue.Enqueue(sequence, (cost + estimate, -cost, next, sequence));
                    arrivals.Add(new(next, point, portal, arrivalIndex, cost, key));
                }
            }
            return completed;
        }

        bool Allowed(RID region) => !settings.ExcludedRegions.AsSpan().Contains(region) && (settings.IncludedRegions.Length == 0 || settings.IncludedRegions.AsSpan().Contains(region));
        int Project(Vector2 point, out Vector2 projected)
        { var selected = -1; projected = Vector2.Zero; var bestDistance = double.PositiveInfinity; for (var i = 0; i < Cells.Length; i++) { if (!usable[i] || Cells[i].IsLink) continue; var p = Closest(Cells[i], point); var d = DistanceSquared(point, p); if (d < bestDistance) { selected = i; projected = p; bestDistance = d; } } return selected; }
        NavigationPathQueryData Complete(List<(Vector2 Point, int Cell)> path)
        {
            if (settings.SimplifyPath && path.Count > 2) { var positions = path.Select(p => p.Point).ToArray(); var indices = SimplifyIndices(positions, settings.SimplifyEpsilon); path = indices.Select(i => path[i]).ToList(); }
            var length = Clip(path, settings.PathReturnMaxLength, settings.PathReturnMaxRadius);
            var flags = settings.MetadataFlags; var output = new Vector2[path.Count]; var types = (flags & NavigationPathQueryParameters.PathMetadataFlags.IncludeTypes) != 0 ? new NavigationPathQueryResult.PathSegmentType[path.Count] : []; var rids = (flags & NavigationPathQueryParameters.PathMetadataFlags.IncludeRIDs) != 0 ? new RID[path.Count] : []; var owners = (flags & NavigationPathQueryParameters.PathMetadataFlags.IncludeOwners) != 0 ? new ulong[path.Count] : [];
            for (var i = 0; i < path.Count; i++) { var point = path[i]; var cell = Cells[point.Cell]; output[i] = point.Point; if (types.Length > 0) types[i] = cell.IsLink ? NavigationPathQueryResult.PathSegmentType.Link : NavigationPathQueryResult.PathSegmentType.Region; if (rids.Length > 0) rids[i] = cell.Region; if (owners.Length > 0) owners[i] = cell.OwnerID; }
            return new(output, types, rids, owners, (float)length);
        }
    }
    private void Add(List<(Vector2 Point, int Cell)> points, Vector2 point, int cell)
    { if (points.Count == 0 || points[^1].Point != point || Cells[points[^1].Cell].Region != Cells[cell].Region && (Cells[points[^1].Cell].IsLink || Cells[cell].IsLink)) points.Add((point, cell)); }
    private List<(Vector2 Point, int Cell)> FunnelWithProvenance(Vector2 from, Vector2 to, int start, int finish, List<(Vector2 Left, Vector2 Right, int Cell)> corridor)
    {
        var portals = new List<(Vector2 Left, Vector2 Right, int Cell)>(corridor) { (to, to, finish) }; var corners = new List<(Vector2 Point, int Cell)> { (from, start) }; var apex = from; var left = from; var right = from; var leftIndex = 0; var rightIndex = 0; var leftCell = start; var rightCell = start;
        for (var i = 0; i < portals.Count; i++)
        {
            var p = portals[i];
            if (-Area(apex, right, p.Right) <= 0) { if (apex == right || -Area(apex, left, p.Right) > 0) { right = p.Right; rightIndex = i; rightCell = p.Cell; } else { Add(corners, left, leftCell); apex = left; left = right = apex; rightIndex = leftIndex; rightCell = leftCell; i = leftIndex; continue; } }
            if (-Area(apex, left, p.Left) >= 0) { if (apex == left || -Area(apex, right, p.Left) < 0) { left = p.Left; leftIndex = i; leftCell = p.Cell; } else { Add(corners, right, rightCell); apex = right; left = right = apex; leftIndex = rightIndex; leftCell = rightCell; i = rightIndex; } }
        }
        Add(corners, to, finish);
        var result = new List<(Vector2 Point, int Cell)> { corners[0] }; var segment = 0;
        foreach (var p in corridor)
        {
            while (segment + 1 < corners.Count)
            {
                if (Intersect(corners[segment].Point, corners[segment + 1].Point, p.Left, p.Right, out var hit)) { Add(result, hit, p.Cell); break; }
                Add(result, corners[++segment].Point, corners[segment].Cell);
            }
        }
        while (segment + 1 < corners.Count) { segment++; Add(result, corners[segment].Point, corners[segment].Cell); }
        return result;
    }
    private static bool Intersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 point)
    {
        point = default; if (c == d) { point = c; return ClosestSegment(c, a, b).IsEqualApprox(c); }
        var rx = (double)b.X - a.X; var ry = (double)b.Y - a.Y; var sx = (double)d.X - c.X; var sy = (double)d.Y - c.Y; var denominator = rx * sy - ry * sx; if (denominator == 0) return false;
        var dx = (double)c.X - a.X; var dy = (double)c.Y - a.Y; var t = (dx * sy - dy * sx) / denominator; var u = (dx * ry - dy * rx) / denominator; if (t < 0 || t > 1 || u < 0 || u > 1) return false; point = Interpolate(a, b, t); return true;
    }
    private static double Clip(List<(Vector2 Point, int Cell)> path, float maxLength, float maxRadius)
    {
        double length = 0; if (path.Count < 2) return 0; var origin = path[0].Point;
        for (var i = 1; i < path.Count; i++)
        {
            var a = path[i - 1].Point; var b = path[i].Point; var segment = Math.Sqrt(DistanceSquared(a, b)); var t = 1d;
            if (maxRadius > 0 && DistanceSquared(origin, b) > (double)maxRadius * maxRadius && segment > 0) { var dx = (double)b.X - a.X; var dy = (double)b.Y - a.Y; var ox = (double)a.X - origin.X; var oy = (double)a.Y - origin.Y; var aa = dx * dx + dy * dy; var bb = 2 * (ox * dx + oy * dy); var cc = ox * ox + oy * oy - (double)maxRadius * maxRadius; t = Math.Clamp((-bb + Math.Sqrt(Math.Max(0, bb * bb - 4 * aa * cc))) / (2 * aa), 0, 1); }
            if (maxLength > 0 && length + segment * t > maxLength && segment > 0) t = Math.Clamp((maxLength - length) / segment, 0, 1);
            length += segment * t; if (t < 1) { path[i] = (Interpolate(a, b, t), path[i].Cell); path.RemoveRange(i + 1, path.Count - i - 1); break; }
        }
        return length;
    }
    internal static int[] SimplifyIndices(ReadOnlySpan<Vector2> path, float epsilon)
    {
        if (path.Length < 3) return Enumerable.Range(0, path.Length).ToArray(); var keep = new bool[path.Length]; keep[0] = keep[^1] = true; var stack = new Stack<(int Start, int End)>(); stack.Push((0, path.Length - 1)); var threshold = (double)epsilon * epsilon;
        // ponytail: exact RDP has quadratic worst-case distance scans; accelerate only after measured long-path need.
        while (stack.TryPop(out var range)) { double farthest = 0; var index = -1; for (var i = range.Start + 1; i < range.End; i++) { var closest = ClosestSegment(path[i], path[range.Start], path[range.End]); var d = DistanceSquared(closest, path[i]); if (d > farthest) { farthest = d; index = i; } } if (index >= 0 && farthest > threshold) { keep[index] = true; stack.Push((index, range.End)); stack.Push((range.Start, index)); } }
        var indices = new List<int>(); for (var i = 0; i < keep.Length; i++) if (keep[i]) indices.Add(i); return indices.ToArray();
    }
}
