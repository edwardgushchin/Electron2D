namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private PhysicsContactSettings _contactSettings = PhysicsContactSettings.FromProject();
    private float _contactTickDuration, _jointTickBias;
    private readonly float _constraintDefaultBias = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultConstraintBias);
    private int _solverIterations = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DSolverIterations);
    internal int GetSolverIterations() { EnsureAccess(); return _solverIterations; }
    internal void SetSolverIterations(int value)
    {
        EnsureAccess(); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (_solverIterations == value) return;
        _solverIterations = value; WakeForSolverPolicy();
    }
    internal PhysicsContactSettings GetContactSettings() { EnsureAccess(); return _contactSettings; }
    internal void SetContactSettings(in PhysicsContactSettings settings)
    {
        EnsureAccess(); settings.Validate(); if (_contactSettings == settings) return;
        _contactSettings = settings; WakeForSolverPolicy();
    }
    private void WakeForSolverPolicy()
    {
        _sleepSolverPolicy = null;
        for (var i = 0; i < _highWater; i++)
            if (_slots[i].Alive && _slots[i].Mode >= PhysicsServer.BodyMode.Rigid) Wake(i);
    }
}
