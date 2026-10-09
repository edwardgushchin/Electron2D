using System.Numerics;
using System.Runtime.InteropServices;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    internal readonly record struct ShapeHandle(int Index, uint Generation, long Owner);
    internal readonly record struct ShapePair(ShapeHandle A, ShapeHandle B);

    private sealed class GeometryEntry(int index)
    {
        internal readonly int Index = index;
        internal Shape? Source;
        internal int References, Start, Capacity;
        internal uint Generation;
        internal ulong Revision, MassRevision;
        internal bool Disposed, Dirty, MassDisposed;
    }
    private struct ShapeSlot
    {
        internal BodyHandle Body;
        internal GeometryEntry? Geometry;
        internal Transform Pose;
        internal uint Generation, Layer, Mask, Revision;
        internal int NextFree, PreviousOnBody, NextOnBody;
        internal bool Alive, Sensor, Dirty;
        internal float Friction, Bounce;
        internal OneWaySettings OneWay;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct GeometryData
    {
        internal uint Start, Count, Kind, Generation;
        internal Vector3 Parameters;
        internal uint Revision;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct GeometryEdit
    {
        internal int Index;
        internal uint Padding1, Padding2, Padding3;
        internal GeometryData Data;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ShapeData
    {
        internal Float4 Pose;
        internal uint Body, BodyGeneration, Geometry, GeometryGeneration;
        internal uint Generation, Layer, Mask, Flags;
        internal Vector2 Material;
        internal uint Revision, Padding;
        internal Float4 OneWay;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ShapeEdit
    {
        internal int Index;
        internal uint Padding1, Padding2, Padding3;
        internal ShapeData Data;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct VertexEdit
    {
        internal int Index;
        internal uint Padding;
        internal Vector2 Position;
    }

    private readonly Dictionary<Shape, GeometryEntry> _geometryByResource = new(ReferenceEqualityComparer.Instance);
    private readonly List<GeometryEntry> _geometryEntries = [];
    private readonly Stack<int> _freeGeometry = [];
    private readonly List<(int Start, int Count)> _freeVertices = [];
    private readonly List<int> _dirtyGeometry = [], _dirtyShapes = [];
    private ShapeSlot[] _shapeSlots = [];
    private ShapeEdit[] _shapeEdits = [];
    private GeometryEdit[] _geometryEdits = [];
    private VertexEdit[] _vertexEdits = [];
    private int _shapeHighWater, _shapeFree = -1, _vertexHighWater, _vertexEditCount;
    private long _shapeVersion, _pairBodyVersion = -1, _pairShapeVersion = -1, _spatialEpoch = -1;
    private bool _treeTopologyDirty;
    internal int ShapeCount { get; private set; }
    internal int PairCount { get; private set; }
    internal long GeometryUploadBytes { get; private set; }
    internal long ShapeUploadBytes { get; private set; }
    internal long BroadPhaseSubmissionCount { get; private set; }
    internal long PairCapacityRetries { get; private set; }

    internal ShapeHandle AddShape(BodyHandle body, Shape geometry, Transform? localPose = null,
        uint layer = 1, uint mask = uint.MaxValue, bool sensor = false, float friction = 1, float bounce = 0)
    {
        Validate(body); ValidateFieldShape(body.Index, sensor); ArgumentNullException.ThrowIfNull(geometry);
        ObjectDisposedException.ThrowIf(geometry.IsDisposed, geometry);
        ValidateMaterial(friction, bounce);
        var pose = localPose ?? Transform.Identity; ValidateShapePose(pose);
        var index = _shapeFree;
        if (index < 0)
        {
            if (_shapeHighWater == _shapeSlots.Length) Array.Resize(ref _shapeSlots, Math.Max(64, checked(_shapeSlots.Length * 2)));
            index = _shapeHighWater++;
        }
        else _shapeFree = _shapeSlots[index].NextFree;
        ref var slot = ref _shapeSlots[index];
        slot.Generation = checked(slot.Generation + 1); slot.Alive = true;
        slot.Friction = friction; slot.Bounce = bounce;
        slot.OneWay = new(false, Vector2.Down, 1);
        slot.Body = body; slot.Pose = pose; slot.Layer = layer; slot.Mask = mask; slot.Sensor = sensor;
        slot.Geometry = RetainGeometry(geometry);
        slot.PreviousOnBody = -1; slot.NextOnBody = _slots[body.Index].FirstShape;
        if (slot.NextOnBody >= 0) _shapeSlots[slot.NextOnBody].PreviousOnBody = index;
        _slots[body.Index].FirstShape = index;
        ShapeCount++; MarkShape(index); _treeTopologyDirty = true;
        return new(index, slot.Generation, _identity);
    }

    internal void RemoveShape(ShapeHandle shape)
    {
        Validate(shape);
        ref var slot = ref _shapeSlots[shape.Index];
        if (slot.OneWay.Enabled) _oneWayShapeCount--;
        if (slot.PreviousOnBody >= 0) _shapeSlots[slot.PreviousOnBody].NextOnBody = slot.NextOnBody;
        else _slots[slot.Body.Index].FirstShape = slot.NextOnBody;
        if (slot.NextOnBody >= 0) _shapeSlots[slot.NextOnBody].PreviousOnBody = slot.PreviousOnBody;
        ReleaseGeometry(slot.Geometry!); slot.Geometry = null; slot.Alive = false;
        slot.NextFree = _shapeFree; _shapeFree = shape.Index; ShapeCount--;
        MarkShape(shape.Index); _treeTopologyDirty = true;
    }

    internal void SetShapePose(ShapeHandle shape, Transform pose)
    {
        Validate(shape); ValidateShapePose(pose);
        ref var slot = ref _shapeSlots[shape.Index];
        if (slot.Pose == pose) return;
        slot.Pose = pose; MarkShape(shape.Index);
    }

    internal void SetShapeFilter(ShapeHandle shape, uint layer, uint mask, bool sensor)
    {
        Validate(shape);
        ref var slot = ref _shapeSlots[shape.Index];
        ValidateFieldShape(slot.Body.Index, sensor);
        if (slot.Layer == layer && slot.Mask == mask && slot.Sensor == sensor) return;
        var massChanged = slot.Sensor != sensor;
        slot.Layer = layer; slot.Mask = mask; slot.Sensor = sensor; MarkShape(shape.Index, massChanged);
    }

    internal void SetShapeMaterial(ShapeHandle shape, float friction, float bounce)
    {
        Validate(shape); ValidateMaterial(friction, bounce);
        ref var slot = ref _shapeSlots[shape.Index];
        if (slot.Friction == friction && slot.Bounce == bounce) return;
        slot.Friction = friction; slot.Bounce = bounce; MarkShape(shape.Index, false);
    }
    private static void ValidateMaterial(float friction, float bounce)
    {
        if (!float.IsFinite(friction) || !float.IsFinite(bounce)) throw new ArgumentOutOfRangeException(nameof(friction));
    }

    private void Validate(ShapeHandle shape)
    {
        EnsureAccess();
        if (shape.Owner != _identity || (uint)shape.Index >= (uint)_shapeHighWater ||
            !_shapeSlots[shape.Index].Alive || _shapeSlots[shape.Index].Generation != shape.Generation)
            throw new ArgumentException("The GPU shape handle is stale or foreign.", nameof(shape));
    }
    private static void ValidateShapePose(Transform pose)
    {
        if (!pose.IsFinite() || !pose.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(pose.Skew))
            throw new ArgumentException("GPU shape placement requires a finite unit-scale pose.", nameof(pose));
    }
    private void MarkShape(int index, bool massChanged = true)
    {
        Wake(_shapeSlots[index].Body.Index, true);
        if (massChanged) MarkMass(_shapeSlots[index].Body.Index);
        _shapeSlots[index].Revision++;
        if (!_shapeSlots[index].Dirty) { _dirtyShapes.Add(index); _shapeSlots[index].Dirty = true; }
        _shapeVersion++;
    }
    private void MarkGeometry(GeometryEntry entry)
    {
        if (!entry.Dirty) { _dirtyGeometry.Add(entry.Index); entry.Dirty = true; }
        _shapeVersion++;
    }
    private GeometryEntry RetainGeometry(Shape source)
    {
        if (_geometryByResource.TryGetValue(source, out var entry)) { entry.References++; return entry; }
        if (_freeGeometry.TryPop(out var index)) entry = _geometryEntries[index];
        else { entry = new(_geometryEntries.Count); _geometryEntries.Add(entry); }
        entry.Generation = checked(entry.Generation + 1); entry.Source = source; entry.References = 1;
        _geometryByResource.Add(source, entry); MarkGeometry(entry);
        return entry;
    }
    private void ReleaseGeometry(GeometryEntry entry)
    {
        if (--entry.References != 0) return;
        _geometryByResource.Remove(entry.Source!); entry.Source = null;
        FreeVertices(entry.Start, entry.Capacity); entry.Start = entry.Capacity = 0;
        _freeGeometry.Push(entry.Index); MarkGeometry(entry);
    }
    private void RemoveBodyShapes(BodyHandle body)
    {
        while (_slots[body.Index].FirstShape is var index && index >= 0)
            RemoveShape(new(index, _shapeSlots[index].Generation, _identity));
    }
    private int AllocateVertices(int capacity)
    {
        // ponytail: first-fit runs on cold geometry edits; use size bins if measured authoring churn dominates.
        for (var i = 0; i < _freeVertices.Count; i++)
        {
            var range = _freeVertices[i];
            if (range.Count < capacity) continue;
            if (range.Count == capacity) _freeVertices.RemoveAt(i);
            else _freeVertices[i] = (range.Start + capacity, range.Count - capacity);
            return range.Start;
        }
        var start = _vertexHighWater; _vertexHighWater = checked(start + capacity); return start;
    }
    private void FreeVertices(int start, int count)
    {
        if (count == 0) return;
        for (var i = 0; i < _freeVertices.Count;)
        {
            var range = _freeVertices[i];
            if (range.Start + range.Count == start) { start = range.Start; count += range.Count; }
            else if (start + count == range.Start) count += range.Count;
            else { i++; continue; }
            _freeVertices.RemoveAt(i); i = 0;
        }
        _freeVertices.Add((start, count));
    }

    private void PrepareGeometry()
    {
        if (_spatialEpoch != Shape.GeometryEpoch)
            foreach (var entry in _geometryEntries)
                if (entry.Source is { } source && (entry.Revision != source.GeometryRevision || entry.Disposed != source.IsDisposed)) MarkGeometry(entry);
        if (_geometryEdits.Length < _dirtyGeometry.Count) Array.Resize(ref _geometryEdits, Capacity(_dirtyGeometry.Count));
        _vertexEditCount = 0;
        for (var i = 0; i < _dirtyGeometry.Count; i++)
        {
            var entry = _geometryEntries[_dirtyGeometry[i]];
            var edit = new GeometryEdit { Index = entry.Index, Data = new() { Generation = entry.Generation } };
            if (entry.Source is { IsDisposed: false } source)
            {
                var geometry = source.GetGeometry();
                var count = geometry.Kind switch
                {
                    PhysicsShapeGeometry.ShapeKind.Circle => 1,
                    PhysicsShapeGeometry.ShapeKind.Rectangle => 4,
                    PhysicsShapeGeometry.ShapeKind.ConvexPolygon or PhysicsShapeGeometry.ShapeKind.ConcavePolygon => geometry.Points.Length,
                    _ => 2
                };
                if (geometry.Kind == PhysicsShapeGeometry.ShapeKind.ConvexPolygon && count > 1 && geometry.Points[0] == geometry.Points[^1]) count--;
                if (count > entry.Capacity)
                {
                    var capacity = checked((int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(4, count)));
                    var start = AllocateVertices(capacity); FreeVertices(entry.Start, entry.Capacity);
                    entry.Start = start; entry.Capacity = capacity;
                }
                if (_vertexEdits.Length < _vertexEditCount + count) Array.Resize(ref _vertexEdits, Capacity(checked(_vertexEditCount + count)));
                for (var v = 0; v < count; v++)
                {
                    var point = geometry.Kind switch
                    {
                        PhysicsShapeGeometry.ShapeKind.Circle => Vector2.Zero,
                        PhysicsShapeGeometry.ShapeKind.Rectangle => v switch { 0 => geometry.A, 1 => new(geometry.B.X, geometry.A.Y), 2 => geometry.B, _ => new(geometry.A.X, geometry.B.Y) },
                        PhysicsShapeGeometry.ShapeKind.ConvexPolygon or PhysicsShapeGeometry.ShapeKind.ConcavePolygon => geometry.Points[v],
                        _ => v == 0 ? geometry.A : geometry.B
                    };
                    _vertexEdits[_vertexEditCount++] = new() { Index = entry.Start + v, Position = point };
                }
                edit.Data.Start = (uint)entry.Start; edit.Data.Count = (uint)count; edit.Data.Kind = (uint)geometry.Kind;
                var winding = 1f;
                if (geometry.Kind == PhysicsShapeGeometry.ShapeKind.ConvexPolygon)
                {
                    double area = 0;
                    for (var v = 0; v < count; v++)
                    {
                        var a = geometry.Points[v]; var b = geometry.Points[(v + 1) % count];
                        area += (double)a.X * b.Y - (double)a.Y * b.X;
                    }
                    winding = area < 0 ? -1 : 1;
                }
                edit.Data.Parameters = new(geometry.Radius, geometry.SlideOnSlope ? 1 : 0, winding);
                edit.Data.Revision = unchecked((uint)source.GeometryRevision);
            }
            if (entry.Source is { } current) { entry.Revision = current.GeometryRevision; entry.Disposed = current.IsDisposed; }
            _geometryEdits[i] = edit;
        }
        if (_shapeEdits.Length < _dirtyShapes.Count) Array.Resize(ref _shapeEdits, Capacity(_dirtyShapes.Count));
        for (var i = 0; i < _dirtyShapes.Count; i++)
        {
            var index = _dirtyShapes[i]; ref readonly var slot = ref _shapeSlots[index];
            var edit = new ShapeEdit { Index = index, Data = new() { Generation = slot.Generation } };
            if (slot.Alive)
                edit.Data = new()
                {
                    Pose = new(slot.Pose.Origin.X, slot.Pose.Origin.Y, MathF.Cos(slot.Pose.Rotation), MathF.Sin(slot.Pose.Rotation)),
                    Body = (uint)slot.Body.Index,
                    BodyGeneration = slot.Body.Generation,
                    Geometry = (uint)slot.Geometry!.Index,
                    GeometryGeneration = slot.Geometry.Generation,
                    Generation = slot.Generation,
                    Layer = slot.Layer,
                    Mask = slot.Mask,
                    Flags = 1u | (slot.Sensor ? 2u : 0u) | (slot.OneWay.Enabled ? 4u : 0u),
                    OneWay = new(slot.OneWay.Direction.X, slot.OneWay.Direction.Y, slot.OneWay.Margin, 0),
                    Material = new(slot.Friction, slot.Bounce),
                    Revision = slot.Revision
                };
            _shapeEdits[i] = edit;
        }
    }

    private void CommitGeometry()
    {
        foreach (var index in _dirtyShapes) _shapeSlots[index].Dirty = false;
        foreach (var index in _dirtyGeometry) _geometryEntries[index].Dirty = false;
        Array.Clear(_shapeEdits, 0, _dirtyShapes.Count); Array.Clear(_geometryEdits, 0, _dirtyGeometry.Count);
        Array.Clear(_vertexEdits, 0, _vertexEditCount);
        _dirtyShapes.Clear(); _dirtyGeometry.Clear(); _vertexEditCount = 0;
        _spatialEpoch = Shape.GeometryEpoch;
    }
    private static int Capacity(int count) => checked((int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(64, count)));
}
