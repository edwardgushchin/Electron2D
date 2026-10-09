namespace Electron2D;

public partial class RigidBody
{
    internal sealed class RigidReplay
    {
        internal readonly record struct Configuration(float _mass, float _gravityScale, float _linearDamp, float _angularDamp, DampMode _linearDampMode, DampMode _angularDampMode, bool _freeze, bool _lockRotation, RigidFreezeMode _freezeMode, float _inertia, RigidCenterOfMassMode _centerOfMassMode, Vector2 _centerOfMass, bool _contactMonitor, int _maxContactsReported, bool _customIntegrator);
        internal Configuration Settings;
        internal Vector2 _linearVelocity;
        internal float _angularVelocity;
        internal bool _canSleep;
        internal bool _sleeping;
        internal Vector2 _constantForce;
        internal float _constantTorque;
        internal Transform _frozenSolverPose;
        internal bool _frozenQueryPoseApplied;
        internal int _contactCount;
        internal bool _sleepChangePending;
        internal readonly PhysicsShapePairTracker Pairs = new();
    }
    private RigidReplay.Configuration ReplayConfiguration() => new(_mass, _gravityScale, _linearDamp, _angularDamp, _linearDampMode, _angularDampMode, _freeze, _lockRotation, _freezeMode, _inertia, _centerOfMassMode, _centerOfMass, _contactMonitor, _maxContactsReported, _customIntegrator);
    internal void CaptureReplay(RigidReplay state)
    {
        PhysicsReplayCopy.Require(!_frozenQueryPoseApplied);
        state.Settings = ReplayConfiguration();
        state._linearVelocity = _linearVelocity;
        state._angularVelocity = _angularVelocity;
        state._canSleep = _canSleep;
        state._sleeping = _sleeping;
        state._constantForce = _constantForce;
        state._constantTorque = _constantTorque;
        state._frozenSolverPose = _frozenSolverPose;
        state._frozenQueryPoseApplied = _frozenQueryPoseApplied;
        state._contactCount = _contactCount;
        state._sleepChangePending = _sleepChangePending;
        _shapePairs.CopyTo(state.Pairs);
    }
    internal void ValidateReplay(RigidReplay state)
    {
        PhysicsReplayCopy.Require(state.Settings == ReplayConfiguration());
        PhysicsReplayCopy.Require(!_frozenQueryPoseApplied);
    }
    internal void RestoreReplay(RigidReplay state)
    {
        _linearVelocity = state._linearVelocity;
        _angularVelocity = state._angularVelocity;
        _canSleep = state._canSleep;
        _sleeping = state._sleeping;
        _constantForce = state._constantForce;
        _constantTorque = state._constantTorque;
        _frozenSolverPose = state._frozenSolverPose;
        _frozenQueryPoseApplied = state._frozenQueryPoseApplied;
        _contactCount = state._contactCount;
        _sleepChangePending = state._sleepChangePending;
        state.Pairs.CopyTo(_shapePairs); _pairChanges.Clear();
    }
}
