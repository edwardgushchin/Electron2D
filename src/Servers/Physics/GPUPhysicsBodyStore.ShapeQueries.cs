using System.Runtime.InteropServices;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    /// <summary>Retains query geometry without registering a body or a world collider. Dispose on the store owner thread.</summary>
    internal sealed class QueryGeometry : IDisposable
    {
        private readonly GPUPhysicsBodyStore _store;
        private GeometryEntry? _entry;
        internal QueryGeometry(GPUPhysicsBodyStore store, Shape source)
        {
            store.EnsureAccess(); ArgumentNullException.ThrowIfNull(source); ObjectDisposedException.ThrowIf(source.IsDisposed, source);
            _store = store; _entry = store.RetainGeometry(source);
        }
        internal (uint Index, uint Generation) Validate(GPUPhysicsBodyStore store)
        {
            if (!ReferenceEquals(_store, store)) throw new ArgumentException("The query geometry belongs to another GPU world.");
            ObjectDisposedException.ThrowIf(_entry is null || _entry.Source is not { IsDisposed: false }, this);
            return ((uint)_entry.Index, _entry.Generation);
        }
        public void Dispose()
        {
            if (_entry is null) return;
            if (_store._owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Query geometry requires its owner thread.");
            if (!_store._disposed) _store.ReleaseGeometry(_entry);
            _entry = null;
        }
    }
    internal enum ShapeQueryMode { Intersect, Contacts, Rest, Cast }
    internal readonly record struct ShapeQuery(QueryGeometry Geometry, Transform Pose, Vector2 Motion = default, float Margin = 0,
        ShapeQueryMode Mode = ShapeQueryMode.Intersect, uint Mask = uint.MaxValue, bool Bodies = true, bool Areas = false,
        int Limit = 32, int ExclusionStart = 0, int ExclusionCount = 0);
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShapeQueryHit
    {
        internal ulong Collider;
        internal int LogicalShape;
        internal uint Shape, ShapeGeneration, Body, BodyGeneration, Piece;
        internal Float4 Points, Contact, Motion;
        internal readonly Vector2 QueryPoint => new(Points.X, Points.Y);
        internal readonly Vector2 ColliderPoint => new(Points.Z, Points.W);
        internal readonly Vector2 Normal => new(Contact.X, Contact.Y);
        internal readonly float Depth => Contact.Z;
        internal readonly Vector2 Velocity => new(Motion.X, Motion.Y);
        internal readonly float SafeFraction => Motion.Z;
        internal readonly float UnsafeFraction => Motion.W;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ShapeQueryInput
    {
        internal QueryInput Query;
        internal Float4 RotationMargin;
        internal uint Geometry, Generation, Mode, Padding;
    }
    private ShapeQueryInput[] _shapeQueryInputs = [];
    private RenderHandle? _shapeQueryPipeline;
    internal QueryGeometry RetainQueryGeometry(Shape shape) => new(this, shape);
    private static int ShapeQueryLimit(in ShapeQuery query) => query.Mode is ShapeQueryMode.Cast or ShapeQueryMode.Rest ? Math.Min(1, query.Limit) : query.Limit;

    /// <summary>Queries current resident geometry, returning capped logical intersections, contact pairs, deepest rest information or motion brackets.</summary>
    internal void QueryShapes(ReadOnlySpan<ShapeQuery> queries, ReadOnlySpan<ulong> exclusions, Span<int> counts, Span<ShapeQueryHit> hits)
    {
        EnsureAccess();
        if (counts.Length < queries.Length) throw new ArgumentException("The query count destination is too small.", nameof(counts));
        var total = 0;
        foreach (ref readonly var q in queries)
        {
            ArgumentNullException.ThrowIfNull(q.Geometry); q.Geometry.Validate(this); ValidateShapePose(q.Pose);
            if (!q.Motion.IsFinite() || !(q.Pose.Origin + q.Motion).IsFinite() || !float.IsFinite(q.Margin) || q.Margin < 0 || (uint)q.Mode > 3 || q.Limit < 0 ||
                q.ExclusionStart < 0 || q.ExclusionStart > exclusions.Length || q.ExclusionCount < 0 || q.ExclusionCount > exclusions.Length - q.ExclusionStart)
                throw new ArgumentOutOfRangeException(nameof(queries));
            total = checked(total + ShapeQueryLimit(q));
        }
        if (hits.Length < total) throw new ArgumentException("The query hit destination is too small.", nameof(hits));
        if (queries.IsEmpty) return;
        PrepareQueryRequests(queries.Length);
        if (_shapeQueryInputs.Length < queries.Length) Array.Resize(ref _shapeQueryInputs, Capacity(queries.Length));
        uint offset = 0;
        for (var i = 0; i < queries.Length; i++)
        {
            ref readonly var q = ref queries[i]; var geometry = q.Geometry.Validate(this); var limit = ShapeQueryLimit(q); _queryLimits[i] = limit;
            _shapeQueryInputs[i] = new()
            {
                Query = new()
                {
                    Ray = new(q.Pose.Origin.X, q.Pose.Origin.Y, q.Motion.X, q.Motion.Y),
                    Mask = q.Mask,
                    Flags = (q.Bodies ? 1u : 0) | (q.Areas ? 2u : 0),
                    Limit = (uint)limit,
                    Offset = offset,
                    ExclusionStart = (uint)q.ExclusionStart,
                    ExclusionCount = (uint)q.ExclusionCount
                },
                RotationMargin = new(MathF.Cos(q.Pose.Rotation), MathF.Sin(q.Pose.Rotation), q.Margin, 0),
                Geometry = geometry.Index,
                Generation = geometry.Generation,
                Mode = (uint)q.Mode
            };
            offset += (uint)limit;
        }
        ExecuteQueries<ShapeQueryInput, ShapeQueryHit>(_shapeQueryInputs.AsSpan(0, queries.Length), exclusions, counts, hits, total,
            ref _shapeQueryPipeline, "PhysicsResidentShapeQueries.comp.spv", centers: true);
    }
}
