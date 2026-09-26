namespace Electron2D;

internal sealed class PhysicsAreaFields(float gravity, Vector2 gravityVector)
{
    internal Area.SpaceOverride GravitySpaceOverride;
    internal Area.SpaceOverride LinearDampSpaceOverride;
    internal Area.SpaceOverride AngularDampSpaceOverride;
    internal float Gravity = gravity;
    internal Vector2 GravityVector = gravityVector;
    internal bool GravityPoint;
    internal float GravityPointUnitDistance;
    internal float LinearDamp = 0.1f;
    internal float AngularDamp = 1f;
    internal int Priority;

    internal bool HasOverrides => GravitySpaceOverride != Area.SpaceOverride.Disabled ||
        LinearDampSpaceOverride != Area.SpaceOverride.Disabled || AngularDampSpaceOverride != Area.SpaceOverride.Disabled;

    internal Vector2 ComputeGravity(Transform transform, Vector2 position)
    {
        if (!GravityPoint) return GravityVector * Gravity;
        var toward = transform * GravityVector - position;
        var lengthSquared = toward.LengthSquared();
        if (lengthSquared == 0) return Vector2.Zero;
        var strength = GravityPointUnitDistance > 0
            ? Gravity * GravityPointUnitDistance * GravityPointUnitDistance / lengthSquared
            : Gravity;
        return toward.Normalized() * strength;
    }

    internal static void ValidateFinite(float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
    }

    internal static void ValidateFinite(Vector2 value)
    {
        if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value));
    }

    internal static void ValidateMode(Area.SpaceOverride value)
    {
        if (value is < Area.SpaceOverride.Disabled or > Area.SpaceOverride.ReplaceCombine)
            throw new ArgumentOutOfRangeException(nameof(value));
    }
}
