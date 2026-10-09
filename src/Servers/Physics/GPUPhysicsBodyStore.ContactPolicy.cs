namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private PhysicsContactSettings _contactSettings = PhysicsContactSettings.FromProject();
    private float _contactTickDuration, _jointTickBias;
    private readonly float _constraintDefaultBias = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultConstraintBias);
    internal PhysicsContactSettings GetContactSettings() { EnsureAccess(); return _contactSettings; }
    internal void SetContactSettings(in PhysicsContactSettings settings)
    {
        EnsureAccess(); settings.Validate(); if (_contactSettings == settings) return;
        _contactSettings = settings; _sleepSolverPolicy = null;
        for (var i = 0; i < _highWater; i++)
            if (_slots[i].Alive && _slots[i].Mode >= PhysicsServer.BodyMode.Rigid) Wake(i);
    }
}
