namespace Electron2D;

/// <summary>Implements the complete live body-state operation family for one borrowed current body attachment.</summary>
/// <remarks>Inherited public operations validate inputs and dispatch typed hooks on the bound world owner outside solving.
/// Binding is qualified by attachment generation and never revives after detach/reentry. Contact hooks return immutable
/// engine-validated snapshots; caller-created views do not replace the server's cached built-in view. Hook nesting is allowed.
/// View/body/world release, attachment transfer, active recursive stepping and checkpoints reject while a hook is borrowed.
/// Library dispatch allocates no warmed managed memory; user code retains responsibility for physical algorithms and its allocations.</remarks>
public abstract partial class PhysicsDirectBodyStateExtension : PhysicsDirectBodyState
{
    /// <summary>Binds the view to a live scene/server body's current solver attachment.</summary>
    /// <param name="body">The live engine-assigned body RID; an Area or detached body is rejected.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">The body is detached, access is off-owner or the world is solving/failed.</exception>
    /// <exception cref="ObjectDisposedException">A required resource is disposed.</exception>
    protected PhysicsDirectBodyStateExtension(RID body) : base(body) { }

    /// <inheritdoc />
    protected sealed override void ValidateDisposal()
    {
        if (CallbackActive) throw new InvalidOperationException("A borrowed body extension cannot be disposed inside its hook.");
        base.ValidateDisposal();
    }
    private void Begin()
    {
        Access(); Space.BeginExtensionCallback(); Runtime.BeginExtension(); BeginCallback();
    }
    private void End() { EndCallback(); Runtime.EndExtension(); Space.EndExtensionCallback(); }
    private T Read<T>(Func<PhysicsDirectBodyStateExtension, T> operation)
    {
        Begin();
        try { var result = operation(this); Access(); return result; }
        finally { End(); }
    }
    private T Read<A, T>(A argument, Func<PhysicsDirectBodyStateExtension, A, T> operation)
    {
        Begin();
        try { var result = operation(this, argument); Access(); return result; }
        finally { End(); }
    }
    private void Write<A>(A argument, Action<PhysicsDirectBodyStateExtension, A> operation)
    {
        Begin();
        try { operation(this, argument); Access(); }
        finally { End(); }
    }
    private static float Result(float value, bool nonnegative = false)
    {
        if (!float.IsFinite(value) || nonnegative && value < 0) throw new InvalidOperationException("A body-state hook returned an invalid scalar.");
        return value;
    }
    private static Vector2 Result(Vector2 value)
    {
        if (!value.IsFinite()) throw new InvalidOperationException("A body-state hook returned a nonfinite vector.");
        return value;
    }
    private static Transform Result(Transform value)
    {
        if (!value.IsFinite() || !value.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(value.Skew))
            throw new InvalidOperationException("A body-state hook returned an invalid rigid transform.");
        return value;
    }
    private int ResultContactCount(int count)
    {
        if ((uint)count > (uint)Runtime.ContactLimit) throw new InvalidOperationException("A body-state hook exceeded the authored contact limit.");
        return count;
    }
    private PhysicsDirectSpaceState ResultSpace(PhysicsDirectSpaceState value)
    {
        if (value is null) throw new InvalidOperationException("A body-state hook returned no space view.");
        value.EnsureContext(Space); return value;
    }
    internal PhysicsBodyContact ReadContact(int index) => Read(index, static (owner, index) =>
    {
        var count = owner.ResultContactCount(owner.GetContactCountCore());
        if ((uint)index >= (uint)count) throw new ArgumentOutOfRangeException(nameof(index));
        var contact = owner.GetContactCore(index);
        if (contact.BodyRID != owner.Runtime.RID || contact.BodyAttachment != owner.Runtime.Backend.AttachmentVersion || !contact.ColliderRID.IsValid())
            throw new InvalidOperationException("A body-state hook returned an invalid observed-body contact identity.");
        return contact;
    });
}
