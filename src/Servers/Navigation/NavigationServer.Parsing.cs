namespace Electron2D;

public sealed partial class NavigationServer
{
    private readonly Dictionary<RID, Action<NavigationPolygon, NavigationMeshSourceGeometryData, Node>?> _parsers = [];
    private readonly HashSet<NavigationMeshSourceGeometryData> _parsing = new(ReferenceEqualityComparer.Instance);
    /// <summary>Creates a caller-owned source parser identity.</summary><returns>A RID requiring FreeRID.</returns>
    public static RID SourceGeometryParserCreate()
    { lock (Shared._gate) { if (Shared._parsers.Count >= 1024) throw new InvalidOperationException("Navigation parser capacity is exhausted."); var rid = RID.Allocate(); Shared._parsers.Add(rid, null); return rid; } }
    /// <summary>Assigns a typed parser callback, or clears it with null.</summary>
    /// <param name="parser">Live parser RID.</param><param name="callback">Invoked for each selected node after built-in geometry collection. Points added must use the supplied root's local space.</param>
    public static void SourceGeometryParserSetCallback(RID parser, Action<NavigationPolygon, NavigationMeshSourceGeometryData, Node>? callback)
    { lock (Shared._gate) { if (!Shared._parsers.ContainsKey(parser)) throw new ArgumentException("The RID does not identify a source parser.", nameof(parser)); Shared._parsers[parser] = callback; } }
    /// <summary>Clears and collects source geometry in root-local coordinates on the scene owner.</summary>
    /// <param name="navigationPolygon">Live source selection configuration.</param><param name="sourceGeometryData">Live destination for collected contours.</param><param name="rootNode">Root coordinate frame and scene/group owner; detached root traversal is also supported.</param><param name="callback">Optional callback after successful parsing.</param>
    /// <remarks>Registration snapshots allow callbacks to update/free parsers. Same-destination recursive parsing rejects. A parser failure can leave partial source data; no target navigation mesh is changed.</remarks>
    public static void ParseSourceGeometryData(NavigationPolygon navigationPolygon, NavigationMeshSourceGeometryData sourceGeometryData, Node rootNode, Action? callback = null)
    {
        ArgumentNullException.ThrowIfNull(navigationPolygon); ArgumentNullException.ThrowIfNull(sourceGeometryData); ArgumentNullException.ThrowIfNull(rootNode);
        ObjectDisposedException.ThrowIf(rootNode.IsDisposed, rootNode); rootNode.Tree?.EnsureOwnerThread(); var settings = navigationPolygon.SettingsSnapshot();
        var roots = settings.SourceGeometry == NavigationPolygon.SourceGeometryMode.RootNodeChildren ? new[] { rootNode } : rootNode.Tree?.GetNodesInGroup(settings.SourceGeometryGroupName).ToArray() ?? throw new InvalidOperationException("Group parsing requires an attached scene tree.");
        var transform = rootNode is Entity entity ? entity.GlobalTransform.AffineInverse() : Transform.Identity;
        Action<NavigationPolygon, NavigationMeshSourceGeometryData, Node>[] parsers;
        lock (Shared._gate) { if (!Shared._parsing.Add(sourceGeometryData)) throw new InvalidOperationException("Source parsing cannot re-enter the same destination."); parsers = Shared._parsers.Values.Where(p => p is not null).Select(p => p!).ToArray(); }
        try
        {
            sourceGeometryData.Clear(); var nodes = new Stack<Node>(); for (var i = roots.Length - 1; i >= 0; i--) nodes.Push(roots[i]); var visited = 0;
            while (nodes.TryPop(out var node))
            {
                if (node.IsDisposed) continue; if (++visited > NavigationGeometryStorage.Limit) throw new InvalidOperationException("Navigation scene parsing exceeds its node budget.");
                ParseBuiltIn(settings, sourceGeometryData, node, transform);
                foreach (var parser in parsers) parser(navigationPolygon, sourceGeometryData, node);
                if (node.IsDisposed || settings.SourceGeometry == NavigationPolygon.SourceGeometryMode.GroupsExplicit) continue;
                var children = node.GetChildren(); for (var i = children.Length - 1; i >= 0; i--) nodes.Push(children[i]);
            }
            callback?.Invoke();
        }
        finally { lock (Shared._gate) Shared._parsing.Remove(sourceGeometryData); }
    }
    private static void AddSourceContour(NavigationMeshSourceGeometryData data, ReadOnlySpan<Vector2> points, Transform transform, Vector2 offset = default)
    {
        var copy = new Vector2[points.Length]; double area = 0;
        for (var i = 0; i < copy.Length; i++) { copy[i] = transform * (points[i] + offset); if (!copy[i].IsFinite()) throw new ArgumentException("Source transformation produces nonfinite geometry."); }
        for (var i = 0; i < copy.Length; i++) area += (double)copy[i].X * copy[(i + 1) % copy.Length].Y - (double)copy[i].Y * copy[(i + 1) % copy.Length].X;
        if (area < 0) Array.Reverse(copy); data.AddObstructionOutline(copy);
    }
    private static void AddMesh(NavigationMeshSourceGeometryData data, Mesh mesh, Transform transform)
    { var faces = mesh.GetFaces(); for (var i = 0; i < faces.Length; i += 3) AddSourceContour(data, faces.AsSpan(i, 3), transform); }
    private static Vector2[] Circle(float radius, Vector2 center = default)
    { var points = new Vector2[12]; for (var i = 0; i < points.Length; i++) { var angle = i * Mathf.Tau / points.Length; points[i] = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius; } return points; }
    private static Vector2[] ShapeContour(Shape shape) => shape switch
    {
        RectangleShape rectangle => [new(-rectangle.Size.X / 2, -rectangle.Size.Y / 2), new(rectangle.Size.X / 2, -rectangle.Size.Y / 2), rectangle.Size / 2, new(-rectangle.Size.X / 2, rectangle.Size.Y / 2)],
        CircleShape circle => Circle(circle.Radius),
        CapsuleShape capsule => Capsule(capsule),
        ConvexPolygonShape convex => convex.Points,
        ConcavePolygonShape concave => concave.Segments,
        _ => []
    };
    private static Vector2[] Capsule(CapsuleShape capsule)
    {
        var points = new Vector2[14]; var half = capsule.Height / 2 - capsule.Radius;
        for (var i = 0; i <= 6; i++) { var angle = MathF.PI + i * MathF.PI / 6; points[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * capsule.Radius + new Vector2(0, -half); }
        for (var i = 0; i <= 6; i++) { var angle = i * MathF.PI / 6; points[7 + i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * capsule.Radius + new Vector2(0, half); }
        return points;
    }
    private static void ParseBuiltIn(NavigationBakeSettings settings, NavigationMeshSourceGeometryData data, Node node, Transform root)
    {
        if (node is NavigationObstacle { AffectNavigationMesh: true } obstacle)
        {
            var transform = root * obstacle.GlobalTransform; var contour = obstacle.Vertices;
            if (contour.Length > 1) { for (var i = 0; i < contour.Length; i++) contour[i] = transform * contour[i]; data.AddProjectedObstruction(contour, obstacle.CarveNavigationMesh); }
            if (obstacle.Radius > 0) { contour = Circle(obstacle.Radius); for (var i = 0; i < contour.Length; i++) contour[i] = transform * contour[i]; data.AddProjectedObstruction(contour, obstacle.CarveNavigationMesh); }
        }
        if (settings.ParsedGeometry != NavigationPolygon.ParsedGeometryType.StaticColliders)
        {
            if (node is Polygon polygon) AddSourceContour(data, polygon.Vertices, root * polygon.GlobalTransform, polygon.Offset);
            else if (node is MeshInstance { Mesh: { IsDisposed: false } mesh } instance) AddMesh(data, mesh, root * instance.GlobalTransform);
            else if (node is MultiMeshInstance { MultiMesh: { IsDisposed: false, Mesh: { IsDisposed: false } multiMesh } multi } many)
            {
                var count = multi.VisibleInstanceCount < 0 ? multi.InstanceCount : multi.VisibleInstanceCount;
                for (var i = 0; i < count; i++) AddMesh(data, multiMesh, root * many.GlobalTransform * multi.GetInstanceTransform2D(i));
            }
        }
        if (node is TileMapLayer navigationTiles) foreach (var points in navigationTiles.CaptureNavigationTraversable())
            { for (var i = 0; i < points.Length; i++) points[i] = root * navigationTiles.GlobalTransform * points[i]; data.AddTraversableOutline(points); }
        if (settings.ParsedGeometry == NavigationPolygon.ParsedGeometryType.MeshInstances) return;
        if (node is StaticBody body && (body.CollisionLayer & settings.ParsedCollisionMask) != 0)
        {
            foreach (var owner in body.GetShapeOwners()) if (!body.IsShapeOwnerDisabled(owner))
                    for (var i = 0; i < body.ShapeOwnerGetShapeCount(owner); i++) AddSourceContour(data, ShapeContour(body.ShapeOwnerGetShape(owner, i)), root * body.GlobalTransform * body.ShapeOwnerGetTransform(owner));
        }
        else if (node is TileMapLayer tiles)
            foreach (var points in tiles.CaptureNavigationContours(settings.ParsedCollisionMask)) AddSourceContour(data, points, root * tiles.GlobalTransform);
    }
}
