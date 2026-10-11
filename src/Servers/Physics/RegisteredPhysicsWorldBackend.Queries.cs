namespace Electron2D;

internal sealed partial class RegisteredPhysicsWorldBackend
{
    private sealed class QueryFrame
    {
        internal readonly List<PhysicsPointResult> Points = [];
        internal readonly List<PhysicsShapeResult> Shapes = [];
        internal readonly List<ShapeContactPair> Contacts = [];
    }
    private readonly List<QueryFrame> _queryFrames = [];
    private int _queryDepth;
    private QueryFrame BeginQuery()
    {
        Enter();
        try { if (_queryFrames.Count == _queryDepth) _queryFrames.Add(new()); return _queryFrames[_queryDepth++]; }
        catch { Exit(); throw; }
    }
    private void EndQuery() { _queryDepth--; Exit(); }
    private static int Compare(RID first, int firstSlot, RID second, int secondSlot)
    { var order = first.GetID().CompareTo(second.GetID()); return order == 0 ? firstSlot.CompareTo(secondSlot) : order; }
    private void ValidateHit(RID rid, int slot, uint mask, bool bodies, bool areas, ulong? canvas, RID[] excluded) =>
        PhysicsServer.Service.ValidateQueryResult(rid, slot, Space, mask, bodies, areas, canvas, excluded);
    internal override PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, RID[] excluded,
        bool collideWithAreas, bool collideWithBodies, bool hitFromInside)
    {
        Enter();
        try
        {
            var result = _implementation.IntersectRay(from, to, mask, excluded, collideWithAreas, collideWithBodies, hitFromInside);
            if (result is not { } hit) return null;
            ValidateHit(hit.ColliderRID, hit.ShapeIndex, mask, collideWithBodies, collideWithAreas, null, excluded);
            return new(hit.ColliderRID, hit.ShapeIndex, hit.Position, hit.Normal);
        }
        finally { Exit(); }
    }
    internal override List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters)
    {
        var mask = parameters.CollisionMask; var bodies = parameters.CollideWithBodies; var areas = parameters.CollideWithAreas;
        var canvas = parameters.CanvasInstanceID; var excluded = parameters.ExclusionsArray; var frame = BeginQuery();
        try
        {
            var values = _implementation.CollectPointHits(parameters, parameters.ExclusionsArray) ?? throw new InvalidOperationException("A point query returned no collection.");
            frame.Points.Clear(); frame.Points.EnsureCapacity(values.Count);
            foreach (var hit in values)
            {
                ValidateHit(hit.ColliderRID, hit.ShapeIndex, mask, bodies, areas, canvas, excluded);
                frame.Points.Add(new(hit.ColliderRID, hit.ShapeIndex));
            }
            frame.Points.Sort(static (a, b) => Compare(a.ColliderRID, a.ShapeIndex, b.ColliderRID, b.ShapeIndex));
            for (var i = 1; i < frame.Points.Count; i++)
                if (Compare(frame.Points[i - 1].ColliderRID, frame.Points[i - 1].ShapeIndex, frame.Points[i].ColliderRID, frame.Points[i].ShapeIndex) == 0)
                    throw new InvalidOperationException("A point query returned a duplicate logical shape.");
            return frame.Points;
        }
        finally { EndQuery(); }
    }
    internal override List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters)
    {
        var mask = parameters.CollisionMask; var bodies = parameters.CollideWithBodies; var areas = parameters.CollideWithAreas;
        var excluded = parameters.ExclusionsArray; var frame = BeginQuery();
        try
        {
            var values = _implementation.CollectShapeHits(parameters, parameters.ExclusionsArray) ?? throw new InvalidOperationException("A shape query returned no collection.");
            frame.Shapes.Clear(); frame.Shapes.EnsureCapacity(values.Count);
            foreach (var hit in values)
            {
                ValidateHit(hit.ColliderRID, hit.ShapeIndex, mask, bodies, areas, null, excluded);
                frame.Shapes.Add(new(hit.ColliderRID, hit.ShapeIndex));
            }
            frame.Shapes.Sort(static (a, b) => Compare(a.ColliderRID, a.ShapeIndex, b.ColliderRID, b.ShapeIndex));
            for (var i = 1; i < frame.Shapes.Count; i++)
                if (Compare(frame.Shapes[i - 1].ColliderRID, frame.Shapes[i - 1].ShapeIndex, frame.Shapes[i].ColliderRID, frame.Shapes[i].ShapeIndex) == 0)
                    throw new InvalidOperationException("A shape query returned a duplicate logical shape.");
            return frame.Shapes;
        }
        finally { EndQuery(); }
    }
    internal override (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters)
    {
        Enter();
        try
        {
            var result = _implementation.CastMotion(parameters, parameters.ExclusionsArray);
            if (!float.IsFinite(result.SafeFraction) || !float.IsFinite(result.UnsafeFraction) || result.SafeFraction < 0 ||
                result.SafeFraction > result.UnsafeFraction || result.UnsafeFraction > 1)
                throw new InvalidOperationException("A motion cast returned invalid fractions.");
            return result;
        }
        finally { Exit(); }
    }
    internal override List<ShapeContactPair> CollectShapeContacts(PhysicsShapeQueryParameters parameters, int limit)
    {
        var mask = parameters.CollisionMask; var bodies = parameters.CollideWithBodies; var areas = parameters.CollideWithAreas;
        var excluded = parameters.ExclusionsArray; var frame = BeginQuery();
        try
        {
            var values = _implementation.CollectShapeContacts(parameters, parameters.ExclusionsArray, limit) ?? throw new InvalidOperationException("A contact query returned no collection.");
            if (values.Count > limit) throw new InvalidOperationException("A contact query returned too many pairs.");
            frame.Contacts.Clear(); frame.Contacts.EnsureCapacity(values.Count);
            foreach (var hit in values)
            {
                if (hit.Piece < 0) throw new InvalidOperationException("A contact query returned an invalid piece index.");
                ValidateHit(hit.RID, hit.ShapeIndex, mask, bodies, areas, null, excluded); VectorResult(hit.QueryPoint); VectorResult(hit.ColliderPoint);
                frame.Contacts.Add(new(hit.RID, hit.ShapeIndex, hit.Piece, hit.QueryPoint, hit.ColliderPoint));
            }
            return frame.Contacts;
        }
        finally { EndQuery(); }
    }
    internal override PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters)
    {
        var mask = parameters.CollisionMask; var bodies = parameters.CollideWithBodies; var areas = parameters.CollideWithAreas;
        var excluded = parameters.ExclusionsArray; Enter();
        try
        {
            var result = _implementation.GetRestInfo(parameters, parameters.ExclusionsArray);
            if (result is not { } hit) return null;
            ValidateHit(hit.ColliderRID, hit.ShapeIndex, mask, bodies, areas, null, excluded);
            return new(hit.ColliderRID, hit.ShapeIndex, hit.Point, hit.Normal, hit.LinearVelocity);
        }
        finally { Exit(); }
    }
}
