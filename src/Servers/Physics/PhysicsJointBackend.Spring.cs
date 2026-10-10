namespace Electron2D;

internal sealed partial class PhysicsJointBackend
{
    internal void PrepareSolverStep(float delta, PhysicsJointRuntime settings)
    {
        if (_implementation is not null) CPU.PrepareSolverStep(delta, settings);
    }
    internal void ValidateSolverStep(PhysicsSpace space)
    {
        if (_implementation is not null) CPU.ValidateSolverStep(space);
    }
    internal void ApplySolverStep()
    {
        if (_implementation is not null) CPU.ApplySolverStep();
    }
}
