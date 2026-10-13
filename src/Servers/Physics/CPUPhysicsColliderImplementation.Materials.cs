using Box2D.NET;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class CPUPhysicsColliderImplementation
{
    private readonly record struct ShapeInput(PhysicsColliderShape Slot, ShapeGeometry? Geometry, float Bias);
    private ShapeInput[] _shapeInputs = [];
    private int _shapeInputCount = -1;
    private (uint Layer, uint Mask, bool Sensor, float Density) _shapeSettings;

    private bool UpdateMaterials(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask, bool sensor,
        float density, float friction, float bounce)
    {
        if (_shapeInputCount != slots.Length || _shapeSettings != (layer, mask, sensor, density)) return false;
        for (var i = 0; i < slots.Length; i++)
        {
            ref readonly var slot = ref slots[i]; ref readonly var saved = ref _shapeInputs[i];
            if (saved.Slot != slot || saved.Bias != (slot.Shape.IsDisposed ? 0 : slot.Shape.CustomSolverBias) ||
                !ReferenceEquals(saved.Geometry, slot.Enabled && !slot.Shape.IsDisposed ? slot.Shape.GetGeometryData() : null)) return false;
        }
        foreach (var id in _shapes) if (!b2Shape_IsValid(id)) return false;
        var definition = b2DefaultShapeDef();
        if (!sensor) CPUPhysicsWorldBackend.SetMaterial(ref definition, friction, bounce);
        foreach (var id in _shapes)
        {
            var material = b2Shape_GetSurfaceMaterial(id);
            material.friction = definition.material.friction; material.restitution = definition.material.restitution;
            material.userMaterialId = definition.material.userMaterialId;
            b2Shape_SetSurfaceMaterial(id, material);
        }
        WakeTouching(); SetAwake(true);
        return true;
    }

    private void CaptureShapeInputs(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask, bool sensor, float density)
    {
        if (_shapeInputs.Length < slots.Length) Array.Resize(ref _shapeInputs, Math.Max(8, slots.Length));
        for (var i = 0; i < slots.Length; i++)
        {
            ref readonly var slot = ref slots[i];
            _shapeInputs[i] = new(slot, slot.Enabled && !slot.Shape.IsDisposed ? slot.Shape.GetGeometryData() : null,
                slot.Shape.IsDisposed ? 0 : slot.Shape.CustomSolverBias);
        }
        Array.Clear(_shapeInputs, slots.Length, _shapeInputs.Length - slots.Length);
        _shapeInputCount = slots.Length; _shapeSettings = (layer, mask, sensor, density);
    }
}
