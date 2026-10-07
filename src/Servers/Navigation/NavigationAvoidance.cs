// Adapted from RVO2 ORCA: Copyright 2008 University of North Carolina at Chapel Hill.
// Licensed under Apache-2.0; see licence/RVO2-LICENSE.txt.
// Modified for managed typed state, simultaneous publication, reusable workspaces and finite double intermediates.
namespace Electron2D;

internal static class NavigationAvoidance
{
    internal readonly record struct V(double X, double Y)
    {
        internal V(Vector2 value) : this(value.X, value.Y) { }
        internal double LengthSquared => X * X + Y * Y;
        internal V Unit(V fallback) => LengthSquared > 1e-24 ? this / Math.Sqrt(LengthSquared) : fallback;
        internal Vector2 Vector => new((float)X, (float)Y);
        public static V operator +(V a, V b) => new(a.X + b.X, a.Y + b.Y);
        public static V operator -(V a, V b) => new(a.X - b.X, a.Y - b.Y);
        public static V operator -(V a) => new(-a.X, -a.Y);
        public static V operator *(V a, double b) => new(a.X * b, a.Y * b);
        public static V operator /(V a, double b) => new(a.X / b, a.Y / b);
    }
    internal readonly record struct Line(V Point, V Direction);
    internal readonly record struct Neighbor(double Distance, V Position, V Velocity, double Radius, long ID);
    internal readonly record struct Edge(V Point, V Next, V PreviousDirection, V Direction, V NextDirection, bool Convex, bool NextConvex);
    private static double Dot(V a, V b) => a.X * b.X + a.Y * b.Y;
    private static double Det(V a, V b) => a.X * b.Y - a.Y * b.X;
    internal static void AddNeighbor(NavigationAgentState agent, Neighbor other)
    {
        var neighbors = agent.Neighbors; var count = Math.Max(0, agent.MaxNeighbors);
        if (count == 0 || other.Distance >= (double)agent.NeighborDistance * agent.NeighborDistance) return;
        var index = neighbors.Count;
        while (index > 0 && (neighbors[index - 1].Distance > other.Distance || neighbors[index - 1].Distance == other.Distance && neighbors[index - 1].ID > other.ID)) index--;
        if (index >= count) return; neighbors.Insert(index, other); if (neighbors.Count > count) neighbors.RemoveAt(count);
    }
    internal static Vector2 Solve(NavigationAgentState agent, double delta)
    {
        var position = agent.SimulationPosition; var velocity = agent.CurrentVelocity; var lines = agent.Lines; lines.Clear();
        foreach (var obstacle in agent.Edges) AddEdge(agent, obstacle.Edge, delta);
        var obstacleLines = lines.Count;
        foreach (var neighbor in agent.Neighbors)
        {
            var relativePosition = neighbor.Position - position; var relativeVelocity = velocity - neighbor.Velocity;
            var distance = relativePosition.LengthSquared; var radius = agent.Radius + neighbor.Radius; var radiusSquared = radius * radius;
            V direction, correction;
            if (distance > radiusSquared)
            {
                var inverse = 1 / Math.Max(agent.TimeHorizonAgents, delta); var w = relativeVelocity - relativePosition * inverse; var length = w.LengthSquared; var dot = Dot(w, relativePosition);
                if (dot < 0 && dot * dot > radiusSquared * length)
                { var unit = w.Unit(new(1, 0)); direction = new(unit.Y, -unit.X); correction = unit * (radius * inverse - Math.Sqrt(length)); }
                else
                {
                    var leg = Math.Sqrt(Math.Max(0, distance - radiusSquared));
                    direction = Det(relativePosition, w) > 0 ? new V(relativePosition.X * leg - relativePosition.Y * radius, relativePosition.X * radius + relativePosition.Y * leg) / distance : -new V(relativePosition.X * leg + relativePosition.Y * radius, -relativePosition.X * radius + relativePosition.Y * leg) / distance;
                    correction = direction * Dot(relativeVelocity, direction) - relativeVelocity;
                }
            }
            else
            {
                var w = relativeVelocity - relativePosition / delta;
                // Coincident stationary discs need opposite deterministic normals rather than zero/zero normalization.
                var unit = w.Unit(new(agent.RID.GetID() < neighbor.ID ? -1 : 1, 0)); direction = new(unit.Y, -unit.X); correction = unit * (radius / delta - Math.Sqrt(w.LengthSquared));
            }
            lines.Add(new(velocity + correction * .5, direction));
        }
        var result = new V(agent.PreferredVelocity); var fail = Program2(lines, agent.MaxSpeed, result, false, ref result);
        if (fail < lines.Count) Program3(agent, obstacleLines, fail, ref result);
        if (!double.IsFinite(result.X) || !double.IsFinite(result.Y)) result = default;
        if (result.LengthSquared > (double)agent.MaxSpeed * agent.MaxSpeed) result = result.Unit(new(1, 0)) * agent.MaxSpeed;
        return result.Vector;
    }
    private static void AddEdge(NavigationAgentState agent, Edge edge, double delta)
    {
        var lines = agent.Lines; var inverse = 1 / Math.Max(agent.TimeHorizonObstacles, delta); var radius = (double)agent.Radius; var radiusSquared = radius * radius;
        var a = edge.Point - agent.SimulationPosition; var b = edge.Next - agent.SimulationPosition; var velocity = agent.CurrentVelocity;
        foreach (var line in lines) if (Det(a * inverse - line.Point, line.Direction) - inverse * radius >= -1e-5 && Det(b * inverse - line.Point, line.Direction) - inverse * radius >= -1e-5) return;
        var d1 = a.LengthSquared; var d2 = b.LengthSquared; var segment = b - a; var s = -Dot(a, segment) / segment.LengthSquared; var lineDistance = (-a - segment * s).LengthSquared;
        if (s < 0 && d1 <= radiusSquared) { if (edge.Convex) lines.Add(new(default, new V(-a.Y, a.X).Unit(edge.Direction))); return; }
        if (s > 1 && d2 <= radiusSquared) { if (edge.NextConvex && Det(b, edge.NextDirection) >= 0) lines.Add(new(default, new V(-b.Y, b.X).Unit(edge.Direction))); return; }
        if (s >= 0 && s < 1 && lineDistance <= radiusSquared) { lines.Add(new(default, -edge.Direction)); return; }
        V left, right; var same = false; var convexA = edge.Convex; var convexB = edge.NextConvex; var previous = edge.PreviousDirection; var next = edge.NextDirection; var directionA = edge.Direction;
        if (s < 0 && lineDistance <= radiusSquared)
        {
            if (!convexA) return; b = a; same = true; convexB = convexA; next = directionA;
            var leg = Math.Sqrt(Math.Max(0, d1 - radiusSquared)); left = new V(a.X * leg - a.Y * radius, a.X * radius + a.Y * leg) / d1; right = new V(a.X * leg + a.Y * radius, -a.X * radius + a.Y * leg) / d1;
        }
        else if (s > 1 && lineDistance <= radiusSquared)
        {
            if (!convexB) return; a = b; same = true; convexA = convexB; previous = directionA; directionA = next;
            var leg = Math.Sqrt(Math.Max(0, d2 - radiusSquared)); left = new V(b.X * leg - b.Y * radius, b.X * radius + b.Y * leg) / d2; right = new V(b.X * leg + b.Y * radius, -b.X * radius + b.Y * leg) / d2;
        }
        else
        {
            var leg1 = Math.Sqrt(Math.Max(0, d1 - radiusSquared)); var leg2 = Math.Sqrt(Math.Max(0, d2 - radiusSquared));
            left = convexA ? new V(a.X * leg1 - a.Y * radius, a.X * radius + a.Y * leg1) / d1 : -directionA;
            right = convexB ? new V(b.X * leg2 + b.Y * radius, -b.X * radius + b.Y * leg2) / d2 : directionA;
        }
        var foreignLeft = convexA && Det(left, -previous) >= 0; if (foreignLeft) left = -previous;
        var foreignRight = convexB && Det(right, next) <= 0; if (foreignRight) right = next;
        var cutoffLeft = a * inverse; var cutoffRight = b * inverse; var cutoff = cutoffRight - cutoffLeft;
        var t = same ? .5 : Dot(velocity - cutoffLeft, cutoff) / cutoff.LengthSquared; var tl = Dot(velocity - cutoffLeft, left); var tr = Dot(velocity - cutoffRight, right);
        if (t < 0 && tl < 0 || same && tl < 0 && tr < 0) { var unit = (velocity - cutoffLeft).Unit(-a.Unit(new(1, 0))); lines.Add(new(cutoffLeft + unit * (radius * inverse), new(unit.Y, -unit.X))); return; }
        if (t > 1 && tr < 0) { var unit = (velocity - cutoffRight).Unit(-b.Unit(new(1, 0))); lines.Add(new(cutoffRight + unit * (radius * inverse), new(unit.Y, -unit.X))); return; }
        var dc = t < 0 || t > 1 || same ? double.PositiveInfinity : (velocity - (cutoffLeft + cutoff * t)).LengthSquared;
        var dl = tl < 0 ? double.PositiveInfinity : (velocity - (cutoffLeft + left * tl)).LengthSquared; var dr = tr < 0 ? double.PositiveInfinity : (velocity - (cutoffRight + right * tr)).LengthSquared;
        V direction, point;
        if (dc <= dl && dc <= dr) { direction = -directionA; point = cutoffLeft; }
        else if (dl <= dr) { if (foreignLeft) return; direction = left; point = cutoffLeft; }
        else { if (foreignRight) return; direction = -right; point = cutoffRight; }
        lines.Add(new(point + new V(-direction.Y, direction.X) * (radius * inverse), direction));
    }
    internal static void AddEdges(NavigationAgentState agent, NavigationObstacleState obstacle)
    {
        var vertices = obstacle.Vertices; if (vertices.Length < 2) return; var origin = new V(obstacle.Position); var point = agent.SimulationPosition;
        var range = (double)agent.TimeHorizonObstacles * agent.MaxSpeed + agent.Radius; range *= range;
        for (var i = 0; i < vertices.Length; i++)
        {
            var previous = new V(vertices[(i + vertices.Length - 1) % vertices.Length]) + origin; var a = new V(vertices[i]) + origin; var b = new V(vertices[(i + 1) % vertices.Length]) + origin; var next = new V(vertices[(i + 2) % vertices.Length]) + origin; var segment = b - a;
            if (Det(segment, point - a) >= 0) continue; var t = Math.Clamp(Dot(point - a, segment) / segment.LengthSquared, 0, 1); var distance = (point - (a + segment * t)).LengthSquared; if (distance >= range) continue;
            var edge = new Edge(a, b, (a - previous).Unit(new(1, 0)), segment.Unit(new(1, 0)), (next - b).Unit(new(1, 0)), vertices.Length == 2 || Det(a - previous, b - a) >= 0, vertices.Length == 2 || Det(b - a, next - b) >= 0);
            var index = agent.Edges.Count; while (index > 0 && agent.Edges[index - 1].Distance > distance) index--; agent.Edges.Insert(index, (distance, edge));
        }
    }
    private static bool Program1(List<Line> lines, int index, double radius, V desired, bool direction, ref V result)
    {
        var line = lines[index]; var dot = Dot(line.Point, line.Direction); var discriminant = dot * dot + radius * radius - line.Point.LengthSquared; if (discriminant < 0) return false;
        var root = Math.Sqrt(discriminant); var left = -dot - root; var right = -dot + root;
        for (var i = 0; i < index; i++) { var denominator = Det(line.Direction, lines[i].Direction); var numerator = Det(lines[i].Direction, line.Point - lines[i].Point); if (Math.Abs(denominator) <= 1e-5) { if (numerator < 0) return false; continue; } var t = numerator / denominator; if (denominator >= 0) right = Math.Min(right, t); else left = Math.Max(left, t); if (left > right) return false; }
        var selected = direction ? Dot(desired, line.Direction) > 0 ? right : left : Math.Clamp(Dot(line.Direction, desired - line.Point), left, right); result = line.Point + line.Direction * selected; return true;
    }
    private static int Program2(List<Line> lines, double radius, V desired, bool direction, ref V result)
    {
        result = direction ? desired * radius : desired.LengthSquared > radius * radius ? desired.Unit(new(1, 0)) * radius : desired;
        for (var i = 0; i < lines.Count; i++) if (Det(lines[i].Direction, lines[i].Point - result) > 0) { var saved = result; if (!Program1(lines, i, radius, desired, direction, ref result)) { result = saved; return i; } }
        return lines.Count;
    }
    private static void Program3(NavigationAgentState agent, int obstacles, int begin, ref V result)
    {
        var lines = agent.Lines; var projected = agent.ProjectedLines; var distance = 0d;
        for (var i = begin; i < lines.Count; i++)
            if (Det(lines[i].Direction, lines[i].Point - result) > distance)
            {
                projected.Clear(); for (var j = 0; j < obstacles; j++) projected.Add(lines[j]);
                for (var j = obstacles; j < i; j++)
                { var determinant = Det(lines[i].Direction, lines[j].Direction); V point; if (Math.Abs(determinant) <= 1e-5) { if (Dot(lines[i].Direction, lines[j].Direction) > 0) continue; point = (lines[i].Point + lines[j].Point) * .5; } else point = lines[i].Point + lines[i].Direction * (Det(lines[j].Direction, lines[i].Point - lines[j].Point) / determinant); projected.Add(new(point, (lines[j].Direction - lines[i].Direction).Unit(new(1, 0)))); }
                var saved = result; if (Program2(projected, agent.MaxSpeed, new(-lines[i].Direction.Y, lines[i].Direction.X), true, ref result) < projected.Count) result = saved;
                distance = Det(lines[i].Direction, lines[i].Point - result);
            }
    }
}
