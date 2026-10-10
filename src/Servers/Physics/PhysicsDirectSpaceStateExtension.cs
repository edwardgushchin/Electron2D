namespace Electron2D;

/// <summary>Implements all direct-space query algorithms in managed consumer code for one borrowed live space.</summary>
/// <remarks>Inherited public queries validate the bound space, prepare authored geometry and invoke the typed hooks.
/// Hooks receive immutable input values and borrowed output spans. Results are validated and object associations are sampled
/// before caller output changes. Nested calls have independent scratch and exclusions, restored in finally.
/// This view owns no world and does not replace the server's cached built-in view. It cannot be disposed, step its world,
/// release its world, or capture/restore its checkpoints during a query hook. Configuring a larger destination or deeper nesting may allocate;
/// unchanged warmed calls reuse storage. Implementations must obey query geometry, filtering and ordering contracts.</remarks>
public abstract partial class PhysicsDirectSpaceStateExtension : PhysicsDirectSpaceState
{
    private readonly PhysicsSpace _space;
    private readonly List<QueryFrame> _frames = [];
    private int _depth;

    /// <summary>Binds a query implementation to one current engine-assigned physics space.</summary>
    /// <param name="space">The borrowed live space RID. This binding never retargets.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live physics space.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner, the solver is executing or the world has failed.</exception>
    /// <exception cref="ObjectDisposedException">The physics service has been disposed.</exception>
    protected PhysicsDirectSpaceStateExtension(RID space) : base(space)
    {
        _space = PhysicsServer.Service.GetSceneSpace(space); _space.EnsureQueryAccess();
    }

    /// <summary>Tests whether an identity is in the current synchronous query's exclusions.</summary>
    /// <param name="body">The physical body or Area identity to test.</param>
    /// <returns>True when excluded by the innermost active query; false outside a query.</returns>
    /// <remarks>Nested queries temporarily replace exclusions. No RID allocation or ownership transfer occurs.</remarks>
    /// <exception cref="InvalidOperationException">The bound space cannot currently be queried.</exception>
    /// <exception cref="ObjectDisposedException">The view or bound space has been disposed.</exception>
    public bool IsBodyExcludedFromQuery(RID body)
    {
        ThrowIfDisposed(); _space.EnsureQueryAccess();
        return _depth != 0 && _frames[_depth - 1].Excluded.AsSpan().Contains(body);
    }

    /// <inheritdoc />
    protected sealed override void ValidateDisposal()
    {
        if (_depth != 0) throw new InvalidOperationException("A borrowed query view cannot be disposed inside its hook.");
        base.ValidateDisposal();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _frames.Clear();
        base.Dispose(disposing);
    }

    private sealed class QueryFrame
    {
        internal RID[] Excluded = [];
        internal uint Mask;
        internal bool Bodies, Areas;
        internal ulong? Canvas;
        internal PhysicsPointResult[] Points = [];
        internal PhysicsShapeResult[] Shapes = [];
        internal Vector2[] Contacts = [];
    }
    private QueryFrame Begin(RID[] excluded, uint mask, bool bodies, bool areas, ulong? canvas = null)
    {
        ThrowIfDisposed(); _space.EnsureQueryAccess();
        if (_frames.Count == _depth) _frames.Add(new());
        var frame = _frames[_depth];
        frame.Excluded = excluded; frame.Mask = mask; frame.Bodies = bodies; frame.Areas = areas; frame.Canvas = canvas;
        _space.BeginExtensionCallback(); _depth++; return frame;
    }
    private void End(QueryFrame frame)
    {
        frame.Excluded = []; Array.Clear(frame.Points); Array.Clear(frame.Shapes);
        _depth--; _space.EndExtensionCallback();
    }
    private static void Grow<T>(ref T[] storage, int count)
    {
        if (storage.Length < count) Array.Resize(ref storage, count);
    }
    private static void Count(int count, int capacity)
    {
        if ((uint)count > (uint)capacity) throw new InvalidOperationException("A query hook returned an invalid result count.");
    }
    private static void Order(RID previous, int previousSlot, RID current, int currentSlot)
    {
        var comparison = previous.GetID().CompareTo(current.GetID());
        if (comparison > 0 || comparison == 0 && previousSlot >= currentSlot)
            throw new InvalidOperationException("A query hook returned duplicate or unordered logical shapes.");
    }
    private static RID QueryShapeRID(PhysicsShapeQueryParameters parameters)
    {
        var shape = parameters.Shape ?? PhysicsServer.Service.GetShapeGeometry(parameters.ShapeRID);
        if (shape.IsDisposed) throw new ObjectDisposedException(nameof(Shape));
        return parameters.ShapeRID;
    }
    private void ValidateCollider(QueryFrame frame, RID rid, int slot)
    {
        PhysicsServer.Service.ValidateQueryResult(rid, slot, _space, frame.Mask, frame.Bodies, frame.Areas, frame.Canvas, frame.Excluded);
    }
    internal PhysicsRayResult? QueryRay(Vector2 from, Vector2 to, uint mask, RID[] excluded, bool areas, bool bodies, bool inside)
    {
        var frame = Begin(excluded, mask, bodies, areas);
        try
        {
            var result = IntersectRayCore(from, to, mask, bodies, areas, inside);
            if (result is not { } hit) return null;
            ValidateCollider(frame, hit.ColliderRID, hit.ShapeIndex);
            return new PhysicsRayResult(hit.ColliderRID, hit.ShapeIndex, hit.Position, hit.Normal);
        }
        finally { End(frame); }
    }
    internal int QueryPoint(PhysicsPointQueryParameters parameters, Span<PhysicsPointResult> output)
    {
        var frame = Begin(parameters.ExclusionsArray, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, parameters.CanvasInstanceID);
        try
        {
            Grow(ref frame.Points, output.Length);
            var storage = frame.Points.AsSpan(0, output.Length);
            var count = IntersectPointCore(parameters.Position, frame.Canvas!.Value, frame.Mask, frame.Bodies, frame.Areas, storage);
            Count(count, storage.Length);
            for (var i = 0; i < count; i++)
            {
                var hit = storage[i]; ValidateCollider(frame, hit.ColliderRID, hit.ShapeIndex);
                if (i != 0) Order(storage[i - 1].ColliderRID, storage[i - 1].ShapeIndex, hit.ColliderRID, hit.ShapeIndex);
                storage[i] = new PhysicsPointResult(hit.ColliderRID, hit.ShapeIndex);
            }
            storage[..count].CopyTo(output); return count;
        }
        finally { End(frame); }
    }
    internal int QueryShape(PhysicsShapeQueryParameters parameters, Span<PhysicsShapeResult> output)
    {
        var shape = QueryShapeRID(parameters);
        var frame = Begin(parameters.ExclusionsArray, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas);
        try
        {
            Grow(ref frame.Shapes, output.Length);
            var storage = frame.Shapes.AsSpan(0, output.Length);
            var count = IntersectShapeCore(shape, parameters.Transform, parameters.Motion, parameters.Margin, frame.Mask, frame.Bodies, frame.Areas, storage);
            Count(count, storage.Length);
            for (var i = 0; i < count; i++)
            {
                var hit = storage[i]; ValidateCollider(frame, hit.ColliderRID, hit.ShapeIndex);
                if (i != 0) Order(storage[i - 1].ColliderRID, storage[i - 1].ShapeIndex, hit.ColliderRID, hit.ShapeIndex);
                storage[i] = new PhysicsShapeResult(hit.ColliderRID, hit.ShapeIndex);
            }
            storage[..count].CopyTo(output); return count;
        }
        finally { End(frame); }
    }
    internal (float SafeFraction, float UnsafeFraction) QueryMotion(PhysicsShapeQueryParameters parameters)
    {
        var shape = QueryShapeRID(parameters);
        var frame = Begin(parameters.ExclusionsArray, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas);
        try
        {
            var result = CastMotionCore(shape, parameters.Transform, parameters.Motion, parameters.Margin, frame.Mask, frame.Bodies, frame.Areas);
            if (!float.IsFinite(result.SafeFraction) || !float.IsFinite(result.UnsafeFraction) || result.SafeFraction < 0 ||
                result.UnsafeFraction > 1 || result.SafeFraction > result.UnsafeFraction)
                throw new InvalidOperationException("A motion hook returned invalid safe/unsafe fractions.");
            return result;
        }
        finally { End(frame); }
    }
    internal int QueryContacts(PhysicsShapeQueryParameters parameters, Span<Vector2> output)
    {
        var shape = QueryShapeRID(parameters);
        var frame = Begin(parameters.ExclusionsArray, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas);
        try
        {
            var length = output.Length / 2 * 2; Grow(ref frame.Contacts, length);
            var storage = frame.Contacts.AsSpan(0, length); storage.Fill(new(float.NaN, float.NaN));
            var count = CollideShapeCore(shape, parameters.Transform, parameters.Motion, parameters.Margin, frame.Mask, frame.Bodies, frame.Areas, storage);
            Count(count, length / 2);
            for (var i = 0; i < count * 2; i++)
                if (!storage[i].IsFinite()) throw new InvalidOperationException("A contact hook returned a nonfinite point.");
            storage[..(count * 2)].CopyTo(output); return count;
        }
        finally { End(frame); }
    }
    internal PhysicsRestInfo? QueryRest(PhysicsShapeQueryParameters parameters)
    {
        var shape = QueryShapeRID(parameters);
        var frame = Begin(parameters.ExclusionsArray, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas);
        try
        {
            var result = GetRestInfoCore(shape, parameters.Transform, parameters.Motion, parameters.Margin, frame.Mask, frame.Bodies, frame.Areas);
            if (result is not { } hit) return null;
            ValidateCollider(frame, hit.ColliderRID, hit.ShapeIndex);
            return new PhysicsRestInfo(hit.ColliderRID, hit.ShapeIndex, hit.Point, hit.Normal, hit.LinearVelocity);
        }
        finally { End(frame); }
    }
}
