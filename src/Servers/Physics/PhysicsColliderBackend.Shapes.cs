namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    private PhysicsColliderShape[] _shapeInputs = [];
    private OneWayContactData?[] _oneWayInputs = [];
    private bool _rebuildingShapes;
    private void BeginShapeInputs(int count)
    {
        if (_rebuildingShapes) throw new InvalidOperationException("Collider geometry cannot rebuild recursively.");
        if (_shapeInputs.Length < count) Array.Resize(ref _shapeInputs, Math.Max(8, count));
        if (_oneWayInputs.Length < count) Array.Resize(ref _oneWayInputs, Math.Max(8, count));
        _rebuildingShapes = true;
    }
    private void EndShapeInputs(int count)
    {
        Array.Clear(_shapeInputs, 0, count);
        Array.Clear(_oneWayInputs, count, _oneWayInputs.Length - count);
        _rebuildingShapes = false;
    }
    internal void RebuildShapes(IReadOnlyList<CollisionObject.ShapeSlot> slots, uint layer, uint mask,
        bool sensor, float density, float friction = 1, float bounce = 0)
    {
        var count = slots.Count; BeginShapeInputs(count);
        try
        {
            for (var index = 0; index < count; index++)
            {
                var slot = slots[index];
                if (!slot.Active) continue;
                _ = slot.Shape.GetGeometryData();
                if (!slot.Transform.IsFinite() || !slot.Transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(slot.Transform.Skew))
                    throw new InvalidOperationException("Physics shapes require unit scale and zero skew.");
            }
            for (var index = 0; index < count; index++)
            {
                var slot = slots[index]; var enabled = slot.Active;
                _shapeInputs[index] = new(slot.Shape, slot.Transform, index, enabled, sensor || !enabled ? null : slot.OneWay);
            }
            Attached.RebuildShapes(_shapeInputs.AsSpan(0, count), layer, mask, sensor, density, friction, bounce);
            _filterDirty = false;
        }
        finally { EndShapeInputs(count); }
    }
    internal void RebuildShapes(IReadOnlyList<PhysicsServerCollider.ShapeSlot> slots, uint layer, uint mask,
        bool sensor, float density, float friction = 1, float bounce = 0)
    {
        var count = slots.Count; BeginShapeInputs(count);
        try
        {
            for (var index = 0; index < count; index++)
            {
                var slot = slots[index];
                if (!slot.Disabled)
                {
                    PhysicsServerCollider.ValidateTransform(slot.LocalTransform);
                    if (!slot.Shape.Geometry.IsDisposed) _ = slot.Shape.Geometry.GetGeometryData();
                }
            }
            for (var index = 0; index < count; index++)
            {
                var slot = slots[index]; var shape = slot.Shape.Geometry;
                var oneWay = !sensor && !slot.Disabled && !shape.IsDisposed && slot.OneWay
                    ? OneWayContactData.Capture(ref _oneWayInputs[index], slot.Direction.Rotated(slot.LocalTransform.Rotation), slot.Margin) : null;
                _shapeInputs[index] = new(shape, slot.LocalTransform, index, !slot.Disabled, oneWay);
            }
            Attached.RebuildShapes(_shapeInputs.AsSpan(0, count), layer, mask, sensor, density, friction, bounce);
            _filterDirty = false;
        }
        finally { EndShapeInputs(count); }
    }
}
