namespace Electron2D;

/// <summary>Authored correction fraction, penetration slack and geometric contact-history limits.</summary>
internal readonly record struct PhysicsContactSettings(float Bias, float AllowedPenetration, float RecycleRadius = 1, float MaxSeparation = 1.5f)
{
    internal static bool ValidBias(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
    internal static bool ValidPenetration(float value) => float.IsFinite(value) && value >= 0 && (value == 0 || value * PhysicsSpace.MetersPerUnit > 0) &&
        float.IsFinite(value * PhysicsSpace.MetersPerUnit * (value * PhysicsSpace.MetersPerUnit));
    internal static bool ValidPersistenceDistance(float value) => ValidPenetration(value) && float.IsFinite(value * value) &&
        (value == 0 || value * PhysicsSpace.MetersPerUnit * (value * PhysicsSpace.MetersPerUnit) > 0);
    internal void Validate()
    {
        if (!ValidBias(Bias) || !ValidPenetration(AllowedPenetration) || !ValidPersistenceDistance(RecycleRadius) || !ValidPersistenceDistance(MaxSeparation)) throw new ArgumentOutOfRangeException(nameof(PhysicsContactSettings));
    }
    internal static PhysicsContactSettings FromProject() => new(
        ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultContactBias),
        ProjectSettings.GetWithOverride(ProjectSettings.Physics2DContactMaxAllowedPenetration),
        ProjectSettings.GetWithOverride(ProjectSettings.Physics2DContactRecycleRadius),
        ProjectSettings.GetWithOverride(ProjectSettings.Physics2DContactMaxSeparation));
}
