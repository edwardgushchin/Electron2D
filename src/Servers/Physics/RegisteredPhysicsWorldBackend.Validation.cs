namespace Electron2D;

internal sealed partial class RegisteredPhysicsWorldBackend
{
    private static Vector2 VectorResult(Vector2 value)
    { if (!value.IsFinite()) throw new InvalidOperationException("A selected implementation returned a nonfinite vector."); return value; }
    private static float ScalarResult(float value)
    { if (!float.IsFinite(value)) throw new InvalidOperationException("A selected implementation returned a nonfinite scalar."); return value; }
    private static float NonnegativeResult(float value)
    { ScalarResult(value); if (value < 0) throw new InvalidOperationException("A selected implementation returned a negative physical property."); return value; }
    private static int CountResult(int value)
    { if (value < 0) throw new InvalidOperationException("A selected implementation returned a negative count."); return value; }
    private static Transform TransformResult(Transform value)
    {
        if (!value.IsFinite() || !value.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(value.Skew))
            throw new InvalidOperationException("A selected implementation returned an invalid rigid pose.");
        return value;
    }
    private static (Vector2 Position, float Rotation) PoseResult((Vector2 Position, float Rotation) value)
    { VectorResult(value.Position); ScalarResult(value.Rotation); return value; }
    private static (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) MotionResult((Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) value)
    { VectorResult(value.LinearVelocity); ScalarResult(value.AngularVelocity); return value; }
    private static PhysicsMass.Properties MassResult(PhysicsMassProperties value)
    {
        ScalarResult(value.Mass); NonnegativeResult(value.Inertia); VectorResult(value.Center);
        if (value.Mass <= 0) throw new InvalidOperationException("A selected implementation returned nonpositive mass."); return new(value.Mass, value.Inertia, value.Center);
    }
    private static PhysicsBodyFields FieldResult(PhysicsBodyFields value)
    { VectorResult(value.Gravity); ScalarResult(value.LinearDamp); ScalarResult(value.AngularDamp); return value; }
}
