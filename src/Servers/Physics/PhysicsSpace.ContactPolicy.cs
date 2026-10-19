namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal int SolverIterations { get; private set; }
    private bool _solverIterationsDirty, _contactSettingsDirty;
    internal void SetSolverIterations(int value)
    {
        EnsureQueryAccess();
        EnsureImplementationWriteAccess();
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (SolverIterations == value && !_solverIterationsDirty) return;
        SolverIterations = value;
        _solverIterationsDirty = true;
        PrepareSolverIterations();
    }

    internal PhysicsContactSettings ContactSettings { get; private set; }
    internal void SetContactSettings(in PhysicsContactSettings settings)
    {
        EnsureQueryAccess(); EnsureImplementationWriteAccess(); settings.Validate();
        if (ContactSettings == settings && !_contactSettingsDirty) return;
        ContactSettings = settings;
        _contactSettingsDirty = true;
        PrepareContactSettings();
    }
    private void PrepareSolverIterations()
    {
        if (!_solverIterationsDirty) return;
        EnsureImplementationWriteAccess();
        _backend.SetSolverIterations(SolverIterations);
        _solverIterationsDirty = false;
    }
    private void PrepareContactSettings()
    {
        if (!_contactSettingsDirty) return;
        EnsureImplementationWriteAccess();
        _backend.SetContactSettings(ContactSettings);
        _contactSettingsDirty = false;
    }
    private int _pendingJointCollisionPolicies;
    internal void ChangePendingJointCollisionPolicies(int delta) => _pendingJointCollisionPolicies += delta;

    private void PrepareWorldPolicies()
    {
        if (_pendingJointCollisionPolicies != 0)
            foreach (var joint in _jointRuntimes) joint.PrepareCollisionPolicy();
        _backend.PrepareActive();
        PrepareSleepSettings();
        PrepareContactSettings();
        PrepareSolverIterations();
        PrepareConstraintDefaultBias();
    }
}
