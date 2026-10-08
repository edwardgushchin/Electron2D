namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    internal readonly record struct OneWaySettings(bool Enabled, Vector2 Direction, float Margin);
    private int _oneWayShapeCount, _oneWayHistoryCount;
    private RenderHandle? _oneWayHistoryGPU, _oneWayNextGPU, _oneWayHistoryTableGPU, _oneWayNextTableGPU, _oneWaySummary;
    private int _oneWayHistoryCapacity, _oneWayNextCapacity, _oneWayHistoryTableCapacity, _oneWayNextTableCapacity;
    internal int OneWayPairCount => _oneWayHistoryCount;

    internal OneWaySettings GetShapeOneWay(ShapeHandle shape) { Validate(shape); return _shapeSlots[shape.Index].OneWay; }
    internal void SetShapeOneWay(ShapeHandle shape, in OneWaySettings settings)
    {
        Validate(shape);
        if (!float.IsFinite(settings.Margin) || settings.Margin < 0) throw new ArgumentOutOfRangeException(nameof(settings));
        var normalized = settings with { Direction = CollisionShape.NormalizeOneWayDirection(settings.Direction) };
        ref var slot = ref _shapeSlots[shape.Index];
        if (slot.OneWay == normalized) return;
        _oneWayShapeCount += (normalized.Enabled ? 1 : 0) - (slot.OneWay.Enabled ? 1 : 0);
        slot.OneWay = normalized; MarkShape(shape.Index, false);
    }

    private void EnsureOneWay()
    {
        Grow(ref _oneWayHistoryGPU, ref _oneWayHistoryCapacity, 1, 48, false);
        Grow(ref _oneWayNextGPU, ref _oneWayNextCapacity, 1, 48, false);
        Grow(ref _oneWayHistoryTableGPU, ref _oneWayHistoryTableCapacity, checked(2 * _oneWayHistoryCapacity), 4, false);
        Grow(ref _oneWayNextTableGPU, ref _oneWayNextTableCapacity, checked(2 * _oneWayNextCapacity), 4, false);
        _oneWaySummary ??= Buffer(8);
    }
    private void PublishOneWay(int count)
    {
        (_oneWayHistoryGPU, _oneWayNextGPU) = (_oneWayNextGPU, _oneWayHistoryGPU);
        (_oneWayHistoryCapacity, _oneWayNextCapacity) = (_oneWayNextCapacity, _oneWayHistoryCapacity);
        (_oneWayHistoryTableGPU, _oneWayNextTableGPU) = (_oneWayNextTableGPU, _oneWayHistoryTableGPU);
        (_oneWayHistoryTableCapacity, _oneWayNextTableCapacity) = (_oneWayNextTableCapacity, _oneWayHistoryTableCapacity);
        _oneWayHistoryCount = count;
    }
    private void DisposeOneWay()
    {
        _oneWayHistoryGPU?.Dispose(); _oneWayNextGPU?.Dispose(); _oneWayHistoryTableGPU?.Dispose(); _oneWayNextTableGPU?.Dispose(); _oneWaySummary?.Dispose();
    }
}
