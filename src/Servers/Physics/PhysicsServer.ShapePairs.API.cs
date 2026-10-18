namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Tests two independently swept collision resources through a selected physics space.</summary>
    /// <param name="space">The live space whose implementation evaluates the pair.</param>
    /// <param name="first">The first live shape RID.</param>
    /// <param name="firstPose">The first shape's finite unit-scale global pose.</param>
    /// <param name="firstMotion">The first shape's finite global displacement.</param>
    /// <param name="second">The second live shape RID.</param>
    /// <param name="secondPose">The second shape's finite unit-scale global pose.</param>
    /// <param name="secondMotion">The second shape's finite global displacement.</param>
    /// <param name="contacts">Caller storage for alternating first/second global contact points; its length must be even.</param>
    /// <param name="count">The number of complete pairs written on success.</param>
    /// <returns>Whether the applicable swept regions intersect, including when no output pairs fit.</returns>
    /// <remarks>Uses the same independent swept-region rules as Shape.CollideWithMotion. Shape resources need
    /// not be attached to the space. Output changes only after complete result validation; unused tails remain
    /// unchanged. Empty output still executes the query. Space owner, lifetime, solver and failure guards apply.
    /// Both shape resources are borrowed through result validation; geometry/policy edits, reset/copy, RID replacement and retirement reject.
    /// Warmed engine staging is reused; implementation callbacks control their own allocations.</remarks>
    /// <exception cref="ArgumentException">A RID is invalid, a pose is unsupported, or output length is odd.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Motion or transformed geometry is nonfinite or exceeds calculation limits.</exception>
    /// <exception cref="InvalidOperationException">Space access is forbidden or the implementation returns invalid output.</exception>
    /// <exception cref="ObjectDisposedException">The service, space or a shape is disposed.</exception>
    public static bool ShapeCollide(RID space, RID first, Transform firstPose, Vector2 firstMotion,
        RID second, Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count) =>
        Service.GetSceneSpace(space).CollideShapes(first, firstPose, firstMotion, second, secondPose, secondMotion, contacts, out count);

    /// <summary>Tests one logical body shape slot against an independently swept shape resource.</summary>
    /// <param name="body">A live scene or server body, attached or detached.</param>
    /// <param name="index">The logical body shape-slot index, including disabled slots.</param>
    /// <param name="shape">The other live shape RID.</param>
    /// <param name="pose">The other shape's finite unit-scale global pose.</param>
    /// <param name="motion">The other shape's finite global displacement.</param>
    /// <param name="contacts">Caller storage for alternating body/other global points; its length must be even.</param>
    /// <param name="count">The number of complete pairs written on success.</param>
    /// <returns>Whether the applicable regions intersect, including when no output pairs fit.</returns>
    /// <remarks>Composes the current body and authored slot poses and requires their result to be finite,
    /// unit-scale and zero-skew before invoking the selected implementation. Filters, slot enablement and other world
    /// objects do not affect this explicit pair. Attached bodies use their actual selected implementation;
    /// detached bodies use resource geometry. Pending motion, mode and pose publication on this body retries before
    /// the selected query; retry failure preserves caller contacts and count without advancing the physics tick.
    /// Output is validated atomically and unused tails remain unchanged.</remarks>
    /// <exception cref="ArgumentException">A RID, pose or output capacity is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The slot, motion or transformed geometry is invalid.</exception>
    /// <exception cref="InvalidOperationException">Owner/solver/failure access is forbidden or output is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The service, body, space or shape is disposed.</exception>
    public static bool BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion,
        Span<Vector2> contacts, out int count) => Service.BodyCollideShapeCore(body, index, shape, pose, motion, contacts, out count);
}
