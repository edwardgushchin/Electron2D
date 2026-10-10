using Clipper2Lib;

namespace Electron2D;

public partial class TileMapLayer
{
    private readonly record struct BodyKey(int Layer, Vector2 Linear, float Angular, bool OneWay, float Margin, float Row);
    private sealed class GeneratedBody(RID rid, Vector2i quadrant, Shape[] shapes, Transform drawTransform)
    {
        internal readonly RID RID = rid;
        internal readonly Vector2i Quadrant = quadrant;
        internal readonly Shape[] Shapes = shapes;
        internal readonly Transform DrawTransform = drawTransform;
        internal void Release() { try { PhysicsServer.FreeRID(RID); } finally { foreach (var shape in Shapes) shape.Dispose(); } }
    }
    private readonly Dictionary<Vector2i, List<GeneratedBody>> _quadrants = [];
    private readonly Dictionary<RID, Vector2i> _bodyCoords = [];
    private Transform _shapeBasis = Transform.Identity;

    /// <summary>Publishes pending tile edits now; cannot run during a solver or overlap callback.</summary>
    /// <remarks>Geometry compilation is allocating authoring work. Unchanged layers do no work.
    /// Updates outside the tree retain authored cells but do not create physics bodies.</remarks>
    public void UpdateInternals()
    {
        EnsureMutable(); if (_updating) throw new InvalidOperationException("Tile updates cannot be reentered.");
        if (!_allDirty && _dirtyCells.Count == 0) return;
        Tree?.EnsurePhysicsParticipationChange();
        var all = _allDirty; var changed = all ? _cells.Keys.Union(_dirtyCells).ToArray() : _dirtyCells.ToArray();
        _allDirty = false; _dirtyCells.Clear(); _updating = true;
        try
        {
            foreach (var coords in changed)
            {
                if (_runtimeData.Remove(coords, out var old)) old.Release();
                if (!IsInsideTree || !_enabled || !IsVisibleInTree || !_cells.TryGetValue(coords, out var cell) || Resolve(cell) is not { } source || !UseTileDataRuntimeUpdate(coords)) continue;
                var copy = source.GetTileData(cell.Atlas, cell.Alternative).Copy();
                try
                {
                    TileDataRuntimeUpdate(coords, copy);
                    if (IsDisposed || !IsInsideTree || !_enabled) { copy.Release(); if (IsDisposed) return; continue; }
                    _runtimeData.Add(coords, copy);
                }
                catch { copy.Release(); throw; }
            }
            UpdateCells(changed, !IsInsideTree || !_enabled || !IsVisibleInTree || _tileSet is null);
            if (IsDisposed) return;
            if (!IsInsideTree || !_enabled || !_collisionEnabled || _tileSet is null) ReleasePhysics();
            else
            {
                var dirtyQuadrants = all ? new HashSet<Vector2i>(_quadrants.Keys) : [];
                foreach (var coords in changed) dirtyQuadrants.Add(QuadrantFor(coords));
                foreach (var quadrant in dirtyQuadrants) RebuildQuadrant(quadrant);
                _shapeBasis = ShapeBasis();
            }
            QueueRedraw();
        }
        catch { _allDirty = true; throw; }
        finally { _updating = false; }
    }
    private Vector2i QuadrantFor(Vector2i coords) => new(FloorDivide(coords.X, _quadrantSize), FloorDivide(coords.Y, _quadrantSize));
    private static int FloorDivide(int value, int size) { var q = value / size; return value % size < 0 ? q - 1 : q; }
    private Transform ShapeBasis()
    {
        var global = GlobalTransform;
        var rigid = new Transform(global.Rotation, Vector2.Zero);
        var basis = rigid.AffineInverse() * global; basis.Origin = Vector2.Zero;
        return basis;
    }
    private Transform BodyTransform(Vector2i quadrant) => new(GlobalTransform.Rotation, GlobalTransform * MapToLocal(quadrant));
    private void RebuildQuadrant(Vector2i quadrant)
    {
        var set = RequireSet(); var basis = ShapeBasis(); var origin = MapToLocal(quadrant);
        var groups = new Dictionary<BodyKey, PathsD>();
        foreach (var pair in _cells)
        {
            if (QuadrantFor(pair.Key) != quadrant || EffectiveData(pair.Key, pair.Value) is not { IsDisposed: false } data) continue;
            var center = MapToLocal(pair.Key);
            var count = Math.Min(set.PhysicsLayers.Count, data.Layers.Count);
            for (var layer = 0; layer < count; layer++)
            {
                var policy = data.Layers[layer];
                foreach (var polygon in policy.Polygons)
                {
                    if (polygon.Points.Length < 3) continue;
                    // Validate topology before union so malformed contours cannot fill unrelated cells.
                    if (Geometry.DecomposePolygonInConvex(polygon.Points).Length == 0) continue;
                    var key = new BodyKey(layer, policy.LinearVelocity, policy.AngularVelocity, polygon.OneWay, polygon.Margin, polygon.OneWay ? center.Y : 0);
                    if (!groups.TryGetValue(key, out var paths)) groups.Add(key, paths = []);
                    var path = new PathD(polygon.Points.Length);
                    foreach (var point in polygon.Points)
                    {
                        var p = basis * (CellPoint(point, pair.Value.Alternative) + center - origin);
                        if (!p.IsFinite() || Math.Abs((double)p.X) > InternalClipper.MaxCoord / 100000d || Math.Abs((double)p.Y) > InternalClipper.MaxCoord / 100000d)
                            throw new InvalidOperationException("Tile geometry exceeds the finite clipping coordinate range.");
                        path.Add(new PointD(p.X, p.Y));
                    }
                    if (!Clipper.IsPositive(path)) path.Reverse(); paths.Add(path);
                }
            }
        }
        var prepared = new List<GeneratedBody>();
        try
        {
            foreach (var group in groups)
            {
                var shapes = Compile(group.Value); if (shapes.Length == 0) continue;
                RID body = default;
                try
                {
                    body = PhysicsServer.BodyCreate(); var key = group.Key; var layer = set.PhysicsLayers[key.Layer];
                    PhysicsServer.BodySetMode(body, _kinematic ? PhysicsServer.BodyMode.Kinematic : PhysicsServer.BodyMode.Static);
                    PhysicsServer.BodySetTransform(body, BodyTransform(quadrant));
                    PhysicsServer.BodySetCollisionLayer(body, layer.Layer); PhysicsServer.BodySetCollisionMask(body, layer.Mask);
                    PhysicsServer.BodySetCollisionPriority(body, layer.Priority);
                    PhysicsServer.BodySetFriction(body, layer.Material?.ComputedFriction ?? 1); PhysicsServer.BodySetBounce(body, layer.Material?.ComputedBounce ?? 0);
                    PhysicsServer.BodySetLinearVelocity(body, key.Linear); PhysicsServer.BodySetAngularVelocity(body, key.Angular);
                    PhysicsServer.BodyAttachObject(body, this); PhysicsServer.BodyAttachCanvasInstanceID(body, GetCanvasLayerNode()?.InstanceID ?? 0);
                    var direction = basis.BasisXform(Vector2.Down).Normalized();
                    for (var i = 0; i < shapes.Length; i++)
                    {
                        PhysicsServer.BodyAddShape(body, shapes[i].GetRID());
                        PhysicsServer.BodySetShapeAsOneWayCollision(body, i, key.OneWay, key.Margin, direction);
                    }
                    prepared.Add(new(body, quadrant, shapes, new Transform(0, origin) * basis.AffineInverse()));
                }
                catch { if (body.IsValid()) PhysicsServer.FreeRID(body); foreach (var shape in shapes) shape.Dispose(); throw; }
            }
            var space = GetWorld()!.Space;
            foreach (var body in prepared) PhysicsServer.BodySetSpace(body.RID, space);
        }
        catch { foreach (var body in prepared) body.Release(); throw; }
        ReleaseQuadrant(quadrant);
        if (prepared.Count == 0) return;
        _quadrants.Add(quadrant, prepared);
        foreach (var body in prepared) _bodyCoords.Add(body.RID, quadrant);
    }
    private static Shape[] Compile(PathsD paths)
    {
        var clipper = new ClipperD(5) { PreserveCollinear = false }; clipper.AddSubject(paths);
        var union = new PathsD(); if (!clipper.Execute(ClipType.Union, FillRule.NonZero, union)) throw new InvalidOperationException("Tile polygon union failed.");
        var shapes = new List<Shape>();
        try
        {
            if (union.Any(path => !Clipper.IsPositive(path)))
            {
                if (Clipper.Triangulate(union, 5, out var triangles) != TriangulateResult.success) throw new InvalidOperationException("Tile polygon triangulation failed.");
                foreach (var triangle in triangles) AddConvex(triangle.Select(p => new Vector2((float)p.x, (float)p.y)).ToArray(), shapes);
            }
            else foreach (var path in union)
                    foreach (var part in Geometry.DecomposePolygonInConvex(path.Select(p => new Vector2((float)p.x, (float)p.y)).ToArray())) AddConvex(part, shapes);
            return shapes.ToArray();
        }
        catch { foreach (var shape in shapes) shape.Dispose(); throw; }
    }
    private static void AddConvex(Vector2[] points, List<Shape> shapes)
    {
        var shape = new ConvexPolygonShape(); try { shape.Points = points; shapes.Add(shape); } catch { shape.Dispose(); throw; }
    }
    private static Vector2 CellPoint(Vector2 point, int alternative)
    {
        if ((alternative & TileSetAtlasSource.TransformTranspose) != 0) point = new(point.Y, point.X);
        if ((alternative & TileSetAtlasSource.TransformFlipH) != 0) point.X = -point.X;
        if ((alternative & TileSetAtlasSource.TransformFlipV) != 0) point.Y = -point.Y;
        return point;
    }
    private void ReleaseQuadrant(Vector2i quadrant)
    {
        if (!_quadrants.Remove(quadrant, out var bodies)) return;
        foreach (var body in bodies) { _bodyCoords.Remove(body.RID); body.Release(); }
    }
    private void ReleasePhysics()
    {
        foreach (var bodies in _quadrants.Values) foreach (var body in bodies) body.Release();
        _quadrants.Clear(); _bodyCoords.Clear();
    }
    private void MoveWorld()
    {
        var space = GetWorld()!.Space;
        foreach (var bodies in _quadrants.Values) foreach (var body in bodies) PhysicsServer.BodySetSpace(body.RID, space);
    }
    private void UpdateBodyTransforms()
    {
        if (_tileSet is null) return;
        if (!_shapeBasis.IsEqualApprox(ShapeBasis())) { MarkAll(); return; }
        foreach (var bodies in _quadrants.Values) foreach (var body in bodies) PhysicsServer.BodySetTransform(body.RID, BodyTransform(body.Quadrant));
    }
    private void UpdateCanvas(ulong id)
    { foreach (var bodies in _quadrants.Values) foreach (var body in bodies) PhysicsServer.BodyAttachCanvasInstanceID(body.RID, id); }
}
