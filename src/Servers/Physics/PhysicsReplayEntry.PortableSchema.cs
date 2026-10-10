namespace Electron2D;

internal sealed partial class PhysicsReplayEntry
{
    private ulong[] _portableExceptions = [];
    internal void AddPortableSchema(ref PhysicsSnapshotSchema schema, PhysicsSnapshotMap map)
    {
        var config = _configuration;
        schema.Add(PortableKind); schema.Add(config.Layer); schema.Add(config.Mask); schema.Add(config.Priority); schema.Add((int)config.Mode);
        schema.Add(config.MadeStatic); schema.Add(config.TopLevel); schema.Add(config.Monitorable); schema.Add(Scene?.IsInsideTree ?? false);
        schema.Add(config.BonePath); schema.Add(config.BoneSimulate); schema.Add(config.BoneActive); schema.Add(config.BoneFollow); schema.Add(config.BoneAuto);
        AddFields(ref schema, config.Fields);
        var material = config.Material;
        schema.Add(material is not null);
        if (material is not null) { schema.Add(material.Friction); schema.Add(material.Bounce); schema.Add(material.Rough); schema.Add(material.Absorbent); }
        schema.Add(_shapeCount);
        for (var i = 0; i < _shapeCount; i++)
        {
            var slot = _shapes[i]; schema.Add(slot.Pose); schema.Add(slot.Active); schema.Add(slot.Index); schema.Add(slot.OneWay);
            schema.Add(slot.Margin); schema.Add(slot.Direction); schema.Add(slot.Bias); schema.Add(slot.Shape.IsDisposed);
            if (slot.Shape.IsDisposed) continue;
            var geometry = slot.Shape.GetGeometry(); schema.Add((int)geometry.Kind); schema.Add(geometry.A); schema.Add(geometry.B);
            schema.Add(geometry.Radius); schema.Add(geometry.SlideOnSlope); schema.Add(geometry.Points.Length);
            foreach (var point in geometry.Points) schema.Add(point);
        }
        if (_rigid is { } rigidState)
        {
            schema.Add(rigidState.Settings._mass);
            schema.Add(rigidState.Settings._gravityScale);
            schema.Add(rigidState.Settings._linearDamp);
            schema.Add(rigidState.Settings._angularDamp);
            schema.Add((int)rigidState.Settings._linearDampMode);
            schema.Add((int)rigidState.Settings._angularDampMode);
            schema.Add(rigidState.Settings._freeze);
            schema.Add(rigidState.Settings._lockRotation);
            schema.Add((int)rigidState.Settings._freezeMode);
            schema.Add(rigidState.Settings._inertia);
            schema.Add((int)rigidState.Settings._centerOfMassMode);
            schema.Add(rigidState.Settings._centerOfMass);
            schema.Add(rigidState.Settings._contactMonitor);
            schema.Add(rigidState.Settings._maxContactsReported);
            schema.Add(rigidState.Settings._customIntegrator);
        }
        if (_animatable is { } animatableState)
        {
            schema.Add(animatableState.Settings._syncToPhysics);
        }
        if (_character is { } characterState)
        {
            schema.Add((int)characterState.Settings._motionMode);
            schema.Add((int)characterState.Settings._platformOnLeave);
            schema.Add(characterState.Settings._safeMargin);
            schema.Add(characterState.Settings._floorStopOnSlope);
            schema.Add(characterState.Settings._floorConstantSpeed);
            schema.Add(characterState.Settings._floorBlockOnWall);
            schema.Add(characterState.Settings._slideOnCeiling);
            schema.Add(characterState.Settings._maxSlides);
            schema.Add(characterState.Settings._floorMaxAngle);
            schema.Add(characterState.Settings._floorSnapLength);
            schema.Add(characterState.Settings._wallMinSlideAngle);
            schema.Add(characterState.Settings._upDirection);
            schema.Add(characterState.Settings._platformFloorLayers);
            schema.Add(characterState.Settings._platformWallLayers);
        }
        if (_area is { } areaState)
        {
            schema.Add(areaState.Settings._monitoring);
            schema.Add(areaState.Settings._monitorable);
        }
        if (_bodyRuntime is { } runtime)
        {
            var c = runtime.Configuration;
            schema.Add(c.Mass); schema.Add(c.Inertia); schema.Add(c.Center.HasValue); schema.Add(c.Center.GetValueOrDefault());
            schema.Add(_runtime!.ContactLimit); schema.Add((int)c.CCD); schema.Add(c.Omit);
            schema.Add(c.Friction.HasValue); schema.Add(c.Friction.GetValueOrDefault()); schema.Add(c.Bounce.HasValue); schema.Add(c.Bounce.GetValueOrDefault());
            schema.Add(c.GravityScale); schema.Add(c.Linear); schema.Add(c.Angular); schema.Add((int)c.LinearMode); schema.Add((int)c.AngularMode);
        }
        if (_portableExceptions.Length < _exceptions.Count) Array.Resize(ref _portableExceptions, _exceptions.Count);
        for (var i = 0; i < _exceptions.Count; i++) _portableExceptions[i] = map.NetworkID(_exceptions[i]);
        Array.Sort(_portableExceptions, 0, _exceptions.Count); schema.Add(_exceptions.Count);
        for (var i = 0; i < _exceptions.Count; i++) schema.Add(_portableExceptions[i]);
    }
    internal static void AddFields(ref PhysicsSnapshotSchema schema, GPUPhysicsBodyStore.FieldParameters fields)
    {
        schema.Add(fields.GravityVector); schema.Add(fields.Gravity); schema.Add(fields.GravityPoint); schema.Add(fields.PointUnitDistance);
        schema.Add(fields.LinearDamp); schema.Add(fields.AngularDamp); schema.Add((int)fields.GravityMode);
        schema.Add((int)fields.LinearMode); schema.Add((int)fields.AngularMode); schema.Add(fields.Priority);
    }
}
