namespace Electron2D;

public abstract partial class Shape
{
    /// <summary>Tests whether two resources intersect at their supplied global poses.</summary>
    /// <param name="localTransform">This shape's finite translated/rotated unit-scale pose.</param>
    /// <param name="withShape">The other live resource; it may be this instance.</param>
    /// <param name="shapeTransform">The other shape's finite translated/rotated unit-scale pose.</param>
    /// <returns>True when an applicable shape pair intersects, including exact touching.</returns>
    /// <exception cref="ArgumentNullException">The other resource is null.</exception>
    /// <exception cref="ObjectDisposedException">Either resource is disposed.</exception>
    /// <exception cref="ArgumentException">A pose is nonfinite, scaled or skewed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Transformed geometry exceeds the finite calculation range.</exception>
    public bool Collide(Transform localTransform, Shape withShape, Transform shapeTransform) =>
        CollideWithMotion(localTransform, Vector2.Zero, withShape, shapeTransform, Vector2.Zero);

    /// <summary>Returns at most sixteen global boundary-contact pairs for two posed resources.</summary>
    /// <param name="localTransform">This shape's finite translated/rotated unit-scale pose.</param>
    /// <param name="withShape">The other live resource; it may be this instance.</param>
    /// <param name="shapeTransform">The other shape's finite translated/rotated unit-scale pose.</param>
    /// <returns>A caller-owned array alternating this shape's point and the other shape's point; empty if no contacts exist.</returns>
    /// <remarks>Deepest pairs are retained at capacity. Exact edge touching can collide without a separating contact.</remarks>
    /// <exception cref="ArgumentNullException">The other resource is null.</exception>
    /// <exception cref="ObjectDisposedException">Either resource is disposed.</exception>
    /// <exception cref="ArgumentException">A pose is nonfinite, scaled or skewed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Transformed geometry exceeds the finite calculation range.</exception>
    public Vector2[] CollideAndGetContacts(Transform localTransform, Shape withShape, Transform shapeTransform) =>
        CollideWithMotionAndGetContacts(localTransform, Vector2.Zero, withShape, shapeTransform, Vector2.Zero);

    /// <summary>Tests the independently swept regions of two collision resources.</summary>
    /// <param name="localTransform">This shape's finite translated/rotated unit-scale global pose.</param>
    /// <param name="localMotion">This shape's finite displacement along global axes.</param>
    /// <param name="withShape">The other live resource; it may be this instance.</param>
    /// <param name="shapeTransform">The other shape's finite translated/rotated unit-scale global pose.</param>
    /// <param name="shapeMotion">The other shape's finite displacement along global axes.</param>
    /// <returns>True when the applicable swept regions intersect.</returns>
    /// <remarks>These are independent swept volumes, without synchronized time fractions. Concave terrain motion
    /// is ignored. Separation rays extend only by their positive axial motion and ignore the counterpart's motion.
    /// World boundaries take precedence: their motion is ignored and the other shape is tested at its final pose.
    /// Two concave resources, two separation rays or two world boundaries do not collide. No scene, space or RID is required.
    /// Resources must not be mutated or disposed concurrently with a query.</remarks>
    /// <exception cref="ArgumentNullException">The other resource is null.</exception>
    /// <exception cref="ObjectDisposedException">Either resource is disposed.</exception>
    /// <exception cref="ArgumentException">A pose is nonfinite, scaled or skewed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Motion or transformed geometry exceeds the finite calculation range.</exception>
    public bool CollideWithMotion(Transform localTransform, Vector2 localMotion, Shape withShape,
        Transform shapeTransform, Vector2 shapeMotion) =>
        PhysicsShapeCollision.Evaluate(this, localTransform, localMotion, withShape, shapeTransform, shapeMotion, [], out _);

    /// <summary>Returns at most sixteen boundary-contact pairs for independently swept resource regions.</summary>
    /// <param name="localTransform">This shape's finite translated/rotated unit-scale global pose.</param>
    /// <param name="localMotion">This shape's finite displacement along global axes.</param>
    /// <param name="withShape">The other live resource; it may be this instance.</param>
    /// <param name="shapeTransform">The other shape's finite translated/rotated unit-scale global pose.</param>
    /// <param name="shapeMotion">The other shape's finite displacement along global axes.</param>
    /// <returns>A caller-owned even array, this region's point followed by the other region's point; empty if no contacts exist.</returns>
    /// <remarks>The difference from the first point to the second gives the separating normal and depth.
    /// Contacts lie on swept-region boundaries. Pair rules are those of <see cref="CollideWithMotion"/>.</remarks>
    /// <exception cref="ArgumentNullException">The other resource is null.</exception>
    /// <exception cref="ObjectDisposedException">Either resource is disposed.</exception>
    /// <exception cref="ArgumentException">A pose is nonfinite, scaled or skewed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Motion or transformed geometry exceeds the finite calculation range.</exception>
    public Vector2[] CollideWithMotionAndGetContacts(Transform localTransform, Vector2 localMotion, Shape withShape,
        Transform shapeTransform, Vector2 shapeMotion)
    {
        Span<Vector2> contacts = stackalloc Vector2[32];
        PhysicsShapeCollision.Evaluate(this, localTransform, localMotion, withShape, shapeTransform, shapeMotion,
            contacts, out var count);
        return count == 0 ? [] : contacts[..(count * 2)].ToArray();
    }
}
