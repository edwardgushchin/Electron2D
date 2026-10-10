namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    internal readonly record struct ReplayConfiguration(float Mass, float Inertia, Vector2? Center, PhysicsMass.Properties MassProperties,
        int Contacts, CCDMode CCD, bool Omit, float? Friction, float? Bounce, float GravityScale, float Linear, float Angular,
        RigidBody.DampMode LinearMode, RigidBody.DampMode AngularMode, Action<PhysicsDirectBodyState>? Force, Action<PhysicsDirectBodyState>? Sync);
    internal sealed class ReplayState
    {
        internal ReplayConfiguration Configuration;
        internal Vector2 ConstantForce, PendingForce, Gravity, Surface;
        internal float ConstantTorque, PendingTorque, LinearDamp, AngularDamp, SurfaceAngular;
        internal bool CanSleep, FieldsInitialized, Active;
        internal PhysicsDirectBodyState.Contact[] Contacts = [];
        internal int ContactCount;
    }
    private ReplayConfiguration CurrentReplayConfiguration() => new(Mass, Inertia, CustomCenter, MassProperties, MaxContacts, ContinuousMode, OmitForces,
        FrictionOverride, BounceOverride, BodyGravityScale, BodyLinearDamp, BodyAngularDamp, BodyLinearDampMode, BodyAngularDampMode, ForceCallback, SyncCallback);
    internal void CaptureReplay(ReplayState state)
    {
        state.Configuration = CurrentReplayConfiguration();
        state.ConstantForce = ConstantForce; state.ConstantTorque = ConstantTorque; state.PendingForce = PendingForce; state.PendingTorque = PendingTorque;
        state.Gravity = Gravity; state.LinearDamp = LinearDamp; state.AngularDamp = AngularDamp; state.FieldsInitialized = FieldsInitialized; state.Active = ActiveBeforeStep;
        state.Surface = _surfaceLinear; state.SurfaceAngular = _surfaceAngular; state.CanSleep = _canSleep;
        var contacts = View is { IsDisposed: false } view ? view.CapturedContacts : default;
        state.ContactCount = contacts.Length; PhysicsReplayCopy.Buffer(contacts, ref state.Contacts);
    }
    internal void ValidateReplay(ReplayState state) => PhysicsReplayCopy.Require(!Released && CurrentReplayConfiguration() == state.Configuration && View?.CallbackActive != true);
    internal void RestoreReplay(ReplayState state)
    {
        Backend.SetContactReporting(ContactLimit > 0);
        ConstantForce = state.ConstantForce; ConstantTorque = state.ConstantTorque; PendingForce = state.PendingForce; PendingTorque = state.PendingTorque;
        Gravity = state.Gravity; LinearDamp = state.LinearDamp; AngularDamp = state.AngularDamp; FieldsInitialized = state.FieldsInitialized; ActiveBeforeStep = state.Active;
        _surfaceLinear = state.Surface; _surfaceAngular = state.SurfaceAngular; _canSleep = state.CanSleep;
        if (View is { IsDisposed: false } view) view.RestoreContacts(state.Contacts.AsSpan(0, state.ContactCount));
    }
}
