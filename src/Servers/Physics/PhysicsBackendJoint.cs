namespace Electron2D;

/// <summary>Defines one selected constraint using logical body identities and immutable solver policy.</summary>
/// <remarks>Common typed getters retain authoring; configuration supplies the complete parameter and flag family.
/// Numerical hooks cannot clear, replace or change scene/server joint paths, geometry or parameters, including unchanged assignments.
/// Ordinary game callbacks retain their configuration writes. The engine prepares automatic scene spring rest length
/// before numerical solving.</remarks>
public abstract class PhysicsBackendJoint
{
    private int _claimed;
    internal void Claim() { if (Interlocked.CompareExchange(ref _claimed, 1, 0) != 0) throw new InvalidOperationException("A joint implementation can belong to only one attachment."); }
    /// <summary>Supplies the selected implementation operation: is attached.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract bool IsAttached { get; }
    /// <summary>Supplies the selected implementation operation: attach.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void Attach(RID first, RID second, in PhysicsJointConfiguration settings);
    /// <summary>Supplies the selected implementation operation: detach.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void Detach();
    /// <summary>Supplies the selected implementation operation: apply solver policy.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ApplySolverPolicy(in PhysicsJointConfiguration settings);
    /// <summary>Supplies the selected implementation operation: apply portable frames.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ApplyPortableFrames(Transform first, Transform second);
    /// <summary>Supplies the selected implementation operation: set collide connected.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetCollideConnected(bool value);
    /// <summary>Supplies the selected implementation operation: set pin limits.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetPinLimits(bool enabled, float lower, float upper);
    /// <summary>Supplies the selected implementation operation: set pin motor enabled.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetPinMotorEnabled(bool value);
    /// <summary>Supplies the selected implementation operation: set pin motor velocity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetPinMotorVelocity(float value);
    /// <summary>Supplies the selected implementation operation: set pin motor max torque.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetPinMotorMaxTorque(float value);
    /// <summary>Supplies the selected implementation operation: set groove limits.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetGrooveLimits(float lower, float upper);
}
