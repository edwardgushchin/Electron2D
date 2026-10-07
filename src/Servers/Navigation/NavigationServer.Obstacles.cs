namespace Electron2D;

public sealed partial class NavigationServer
{
    private NavigationObstacleState Obstacle(RID rid) => _obstacles.TryGetValue(rid, out var obstacle) && (obstacle.Scene is null || obstacle.Scene.TryGetTarget(out var node) && !node.IsDisposed) ? obstacle : throw new ArgumentException("The RID does not identify a live navigation obstacle.", nameof(rid));
    internal RID CreateObstacle(NavigationObstacle? node = null) { lock (_gate) { var rid = RID.Allocate(); _obstacles.Add(rid, new(rid, node)); return rid; } }
    internal void ReleaseSceneObstacle(RID rid) { lock (_gate) _obstacles.Remove(rid); }
    internal void StageSceneObstacle(RID rid, Vector2 position, float radius, Vector2[] vertices)
    { Finite(position); Nonnegative(radius); var copy = ValidateContour(vertices); lock (_gate) { var state = Obstacle(rid); state.Position = position; state.Radius = radius; state.Vertices = copy; } }
    /// <summary>Creates a caller-owned enabled obstacle with zero radius and empty contour.</summary>
    /// <returns>Live obstacle RID requiring FreeRID.</returns>
    public static RID ObstacleCreate() => Shared.CreateObstacle();
    /// <summary>Assigns a live map; empty detaches the obstacle.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><param name="map">Live map RID or empty.</param>
    /// <exception cref="ArgumentException">A required RID is invalid.</exception>
    /// <exception cref="InvalidOperationException">A scene map belongs to another tree.</exception>
    public static void ObstacleSetMap(RID obstacle, RID map) { lock (Shared._gate) { var state = Shared.Obstacle(obstacle); if (map.IsValid()) { Shared.Map(map); Shared.ValidateSceneMap(map, state.Scene is not null && state.Scene.TryGetTarget(out var node) ? node.Tree : null); } state.Map = map; } }
    /// <summary>Returns current obstacle map membership.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><returns>Map or empty.</returns>
    /// <exception cref="ArgumentException">The RID is invalid.</exception>
    public static RID ObstacleGetMap(RID obstacle) { lock (Shared._gate) return Shared.Obstacle(obstacle).Map; }
    /// <summary>Sets a copied simple oriented local contour; empty or one point creates no static segments.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><param name="vertices">Finite offsets from obstacle Position. Two points create a two-sided wall; winding controls larger contours.</param>
    /// <exception cref="ArgumentException">The RID or contour is invalid, duplicated or self-intersecting.</exception>
    public static void ObstacleSetVertices(RID obstacle, ReadOnlySpan<Vector2> vertices) { var copy = ValidateContour(vertices); lock (Shared._gate) Shared.Obstacle(obstacle).Vertices = copy; }
    /// <summary>Returns a copied oriented local obstacle contour.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><returns>Independent offset array.</returns>
    /// <exception cref="ArgumentException">The RID is invalid.</exception>
    public static Vector2[] ObstacleGetVertices(RID obstacle) { lock (Shared._gate) return (Vector2[])Shared.Obstacle(obstacle).Vertices.Clone(); }
    /// <summary>Returns participation in avoidance.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><returns>Current setting, initially true.</returns>
    /// <exception cref="ArgumentException">The RID is invalid.</exception>
    public static bool ObstacleGetAvoidanceEnabled(RID obstacle) { lock (Shared._gate) return Shared.Obstacle(obstacle).AvoidanceEnabled; }
    /// <summary>Sets participation in avoidance.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><param name="value">New setting.</param>
    /// <exception cref="ArgumentException">The RID or vector is invalid.</exception>
    public static void ObstacleSetAvoidanceEnabled(RID obstacle, bool value) { lock (Shared._gate) Shared.Obstacle(obstacle).AvoidanceEnabled = value; }
    /// <summary>Returns whether participation is paused.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><returns>Current setting, initially false.</returns>
    /// <exception cref="ArgumentException">The RID is invalid.</exception>
    public static bool ObstacleGetPaused(RID obstacle) { lock (Shared._gate) return Shared.Obstacle(obstacle).Paused; }
    /// <summary>Sets whether participation is paused.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><param name="value">New setting.</param>
    /// <exception cref="ArgumentException">The RID or vector is invalid.</exception>
    public static void ObstacleSetPaused(RID obstacle, bool value) { lock (Shared._gate) Shared.Obstacle(obstacle).Paused = value; }
    /// <summary>Returns 32-bit layers visible to agent masks.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><returns>Current setting, initially 1u.</returns>
    /// <exception cref="ArgumentException">The RID is invalid.</exception>
    public static uint ObstacleGetAvoidanceLayers(RID obstacle) { lock (Shared._gate) return Shared.Obstacle(obstacle).AvoidanceLayers; }
    /// <summary>Sets 32-bit layers visible to agent masks.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><param name="value">New setting.</param>
    /// <exception cref="ArgumentException">The RID or vector is invalid.</exception>
    public static void ObstacleSetAvoidanceLayers(RID obstacle, uint value) { lock (Shared._gate) Shared.Obstacle(obstacle).AvoidanceLayers = value; }
    /// <summary>Returns finite nonnegative moving-disc radius.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><returns>Current setting, initially 0f.</returns>
    /// <exception cref="ArgumentException">The RID is invalid.</exception>
    public static float ObstacleGetRadius(RID obstacle) { lock (Shared._gate) return Shared.Obstacle(obstacle).Radius; }
    /// <summary>Sets finite nonnegative moving-disc radius.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><param name="value">New setting.</param>
    /// <exception cref="ArgumentException">The RID or vector is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is negative or nonfinite.</exception>
    public static void ObstacleSetRadius(RID obstacle, float value) { Nonnegative(value); lock (Shared._gate) Shared.Obstacle(obstacle).Radius = value; }
    /// <summary>Returns finite world-space translation.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><returns>Current setting, initially Vector2.Zero.</returns>
    /// <exception cref="ArgumentException">The RID is invalid.</exception>
    public static Vector2 ObstacleGetPosition(RID obstacle) { lock (Shared._gate) return Shared.Obstacle(obstacle).Position; }
    /// <summary>Sets finite world-space translation.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><param name="value">New setting.</param>
    /// <exception cref="ArgumentException">The RID or vector is invalid.</exception>
    public static void ObstacleSetPosition(RID obstacle, Vector2 value) { Finite(value); lock (Shared._gate) Shared.Obstacle(obstacle).Position = value; }
    /// <summary>Returns finite moving-disc velocity; static contours remain stationary predictions.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><returns>Current setting, initially Vector2.Zero.</returns>
    /// <exception cref="ArgumentException">The RID is invalid.</exception>
    public static Vector2 ObstacleGetVelocity(RID obstacle) { lock (Shared._gate) return Shared.Obstacle(obstacle).Velocity; }
    /// <summary>Sets finite moving-disc velocity; static contours remain stationary predictions.</summary>
    /// <param name="obstacle">Live obstacle RID.</param><param name="value">New setting.</param>
    /// <exception cref="ArgumentException">The RID or vector is invalid.</exception>
    public static void ObstacleSetVelocity(RID obstacle, Vector2 value) { Finite(value); lock (Shared._gate) Shared.Obstacle(obstacle).Velocity = value; }
    internal static Vector2[] ValidateContour(ReadOnlySpan<Vector2> vertices)
    {
        var copy = vertices.ToArray(); foreach (var point in copy) Finite(point);
        for (var i = 0; i < copy.Length; i++) for (var j = i + 1; j < copy.Length; j++) if (copy[i] == copy[j]) throw new ArgumentException("Obstacle contour points must differ.", nameof(vertices));
        if (copy.Length > 2)
        {
            if (Geometry.TriangulatePolygon(copy).Length == 0) throw new ArgumentException("Obstacle contours must be simple with nonzero area.", nameof(vertices));
            for (var i = 0; i < copy.Length; i++) for (var j = i + 2; j < copy.Length; j++) if ((j + 1) % copy.Length != i && Geometry.SegmentIntersectsSegment(copy[i], copy[(i + 1) % copy.Length], copy[j], copy[(j + 1) % copy.Length]) is not null) throw new ArgumentException("Obstacle contours cannot intersect.", nameof(vertices));
        }
        return copy;
    }
}
internal sealed class NavigationObstacleState(RID rid, NavigationObstacle? scene)
{
    internal readonly RID RID = rid;
    internal readonly WeakReference<NavigationObstacle>? Scene = scene is null ? null : new(scene);
    internal RID Map;
    internal bool AvoidanceEnabled = true, Paused;
    internal uint AvoidanceLayers = 1;
    internal float Radius;
    internal Vector2 Position, Velocity;
    internal Vector2[] Vertices = [];
}
