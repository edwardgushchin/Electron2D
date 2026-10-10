namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal int SolverIterations { get; private set; }
    internal void SetSolverIterations(int value)
    {
        EnsureQueryAccess();
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (SolverIterations == value) return;
        SolverIterations = value;
        _backend.SetSolverIterations(value);
    }

    internal PhysicsContactSettings ContactSettings { get; private set; }
    internal void SetContactSettings(in PhysicsContactSettings settings)
    {
        EnsureQueryAccess(); settings.Validate(); if (ContactSettings == settings) return;
        ContactSettings = settings;
        _backend.SetContactSettings(settings);
    }
}
