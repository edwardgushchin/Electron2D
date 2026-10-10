namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    private PhysicsSpace? _viewSpace;
    private long _viewAttachment;
    private PhysicsColliderBackend? _viewBackend;

    internal PhysicsDirectBodyState GetView(PhysicsSpace space)
    {
        var backend = Backend;
        if (backend.Space != space) throw new InvalidOperationException("The body is not attached to this physics world.");
        if (View is null || View.IsDisposed || _viewSpace != space || _viewAttachment != backend.AttachmentVersion)
        {
            View = new PhysicsDirectBodyState(this, space);
            _viewSpace = space; _viewBackend = backend; _viewAttachment = backend.AttachmentVersion;
        }
        return View;
    }
    internal void InvalidateView() { View = null; _viewSpace = null; _viewBackend = null; _viewAttachment = 0; }
    internal void ValidateView(PhysicsDirectBodyState view, PhysicsSpace space)
    {
        try
        {
            if (!ReferenceEquals(View, view) || Space != space || _viewAttachment != _viewBackend!.AttachmentVersion)
                throw new ObjectDisposedException(nameof(PhysicsDirectBodyState), "The backend attachment ended.");
        }
        catch (ArgumentException) { throw new ObjectDisposedException(nameof(PhysicsDirectBodyState), "The body was released."); }
    }
    internal float ViewAngularVelocity => _viewBackend!.AngularVelocity;
    internal Vector2 ViewLinearVelocity => _viewBackend!.LinearVelocity;
    internal Vector2 ViewCenterOfMass => _viewBackend!.CenterOfMass;
    internal Vector2 ViewCenterOfMassLocal => _viewBackend!.CenterOfMassLocal;
    internal float ViewInverseMass => _viewBackend!.InverseMass;
    internal float ViewInverseInertia => _viewBackend!.InverseInertia;
    internal bool ViewSleeping => !_viewBackend!.IsAwake;
    internal Transform ViewTransform => _viewBackend!.GetTransform();
    internal Vector2 GetViewPointVelocity(Vector2 offset) { Finite(offset); return _viewBackend!.GetPointVelocity(offset); }
    internal void SetViewConstantForce(Vector2 force)
    {
        Finite(force);
        if (Owners.Scene is RigidBody rigid) rigid.ConstantForce = force; else ConstantForce = force;
        _viewBackend!.SetAwake(true);
    }
    internal void SetViewConstantTorque(float torque)
    {
        Finite(torque);
        if (Owners.Scene is RigidBody rigid) rigid.ConstantTorque = torque; else ConstantTorque = torque;
        _viewBackend!.SetAwake(true);
    }
    internal void CaptureViewContacts(PhysicsDirectBodyState view) => _viewBackend!.CaptureViewContacts(view, ContactLimit);
}
