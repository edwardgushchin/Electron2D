using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver
    {
        private readonly List<Body> _fieldAreas = [];
        private void PrepareFieldAreas()
        {
            _fieldAreas.Clear();
            foreach (var body in Bodies.Values) if (body.IsArea && body.Shapes != 0) _fieldAreas.Add(body);
            _fieldAreas.Sort(static (a, b) => a.AreaFields.Priority == b.AreaFields.Priority
                ? a.RID.GetID().CompareTo(b.RID.GetID()) : b.AreaFields.Priority.CompareTo(a.AreaFields.Priority));
        }
        internal PhysicsBodyFields ResolveFields(Body body)
        {
            var gravity = Vector2.Zero; var linear = 0f; var angular = 0f;
            var stopGravity = false; var stopLinear = false; var stopAngular = false;
            foreach (var area in _fieldAreas)
            {
                if ((area.Mask & body.Layer) == 0 || !Overlap(area, body)) continue;
                var f = area.AreaFields;
                var g = f.GravityVector * f.Gravity;
                if (f.GravityPoint)
                {
                    var offset = area.Position + f.GravityVector.Rotated(area.Rotation) - body.Position;
                    var distance = offset.Length();
                    g = distance == 0 ? default : offset / distance * f.Gravity *
                        (f.PointUnitDistance > 0 ? f.PointUnitDistance * f.PointUnitDistance / (distance * distance) : 1);
                }
                Apply(ref gravity, ref stopGravity, g, f.GravityMode);
                Apply(ref linear, ref stopLinear, f.LinearDamp, f.LinearMode);
                Apply(ref angular, ref stopAngular, f.AngularDamp, f.AngularMode);
            }
            if (!stopGravity) gravity += Fields.GravityVector * Fields.Gravity;
            if (!stopLinear) linear += Fields.LinearDamp;
            if (!stopAngular) angular += Fields.AngularDamp;
            return new(gravity, linear, angular, true);
        }
        private static void Apply(ref Vector2 total, ref bool stopped, Vector2 value, Area.SpaceOverride mode)
        {
            if (stopped || mode == Area.SpaceOverride.Disabled) return;
            total = mode is Area.SpaceOverride.Replace or Area.SpaceOverride.ReplaceCombine ? value : total + value;
            stopped = mode is Area.SpaceOverride.Replace or Area.SpaceOverride.CombineReplace;
        }
        private static void Apply(ref float total, ref bool stopped, float value, Area.SpaceOverride mode)
        {
            if (stopped || mode == Area.SpaceOverride.Disabled) return;
            total = mode is Area.SpaceOverride.Replace or Area.SpaceOverride.ReplaceCombine ? value : total + value;
            stopped = mode is Area.SpaceOverride.Replace or Area.SpaceOverride.CombineReplace;
        }
    }
}
