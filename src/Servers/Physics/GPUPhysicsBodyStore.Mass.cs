namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    internal readonly record struct MassProfile(float Mass, float Inertia = 0, Vector2? Center = null);
    private readonly PhysicsMass.Geometry _massGeometry = new();
    private readonly List<int> _dirtyMasses = [];
    private long _massEpoch = -1;

    internal MassProfile GetMassProfile(BodyHandle body) { Validate(body); return _slots[body.Index].MassProfile; }
    internal PhysicsMass.Properties GetMassProperties(BodyHandle body)
    {
        Validate(body); PrepareMasses(); return _slots[body.Index].MassProperties;
    }
    internal void SetMassProfile(BodyHandle body, in MassProfile profile)
    {
        Validate(body); PhysicsMass.Validate(profile.Mass, profile.Inertia, profile.Center);
        ref var slot = ref _slots[body.Index];
        if (slot.MassProfile == profile) return;
        var resolved = CalculateMass(body.Index, profile);
        slot.MassProfile = profile; ApplyMass(body.Index, resolved);
    }
    private PhysicsMass.Properties CalculateMass(int body, in MassProfile profile)
    {
        _massGeometry.Clear();
        for (var index = _slots[body].FirstShape; index >= 0; index = _shapeSlots[index].NextOnBody)
        {
            ref readonly var shape = ref _shapeSlots[index];
            if (shape.Geometry?.Source is { } source) _massGeometry.Append(source, shape.Pose, shape.Sensor);
        }
        return _massGeometry.Calculate(profile.Mass, profile.Inertia, profile.Center);
    }
    private void MarkMass(int index)
    {
        if (!_slots[index].MassDirty) { _slots[index].MassDirty = true; _dirtyMasses.Add(index); }
    }
    private void PrepareMasses()
    {
        if (_massEpoch != Shape.GeometryEpoch)
        {
            for (var i = 0; i < _shapeHighWater; i++)
            {
                ref readonly var shape = ref _shapeSlots[i];
                if (shape.Alive && shape.Geometry is { Source: { } source } geometry &&
                    (source.GeometryRevision != geometry.MassRevision || source.IsDisposed != geometry.MassDisposed)) MarkMass(shape.Body.Index);
            }
            foreach (var geometry in _geometryEntries)
                if (geometry.Source is { } source) { geometry.MassRevision = source.GeometryRevision; geometry.MassDisposed = source.IsDisposed; }
            _massEpoch = Shape.GeometryEpoch;
        }
        // Validate all dirty geometry before a device submission. Failed authoring can be corrected and retried.
        foreach (var index in _dirtyMasses)
            if (_slots[index].Alive) ApplyMass(index, CalculateMass(index, _slots[index].MassProfile));
        foreach (var index in _dirtyMasses) _slots[index].MassDirty = false;
        _dirtyMasses.Clear();
    }
    private void ApplyMass(int index, in PhysicsMass.Properties resolved)
    {
        ref var slot = ref _slots[index];
        if (slot.MassProperties == resolved) return;
        slot.MassProperties = resolved;
        ref var command = ref Edit(index); command.Mask |= Mass;
        command.Body.Properties.X = 1 / resolved.Mass;
        command.Body.Properties.Y = resolved.Inertia > 0 ? 1 / resolved.Inertia : 0;
        command.Center = new(resolved.Center.X, resolved.Center.Y, 0, 0);
    }
}
