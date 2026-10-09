namespace Electron2D;

/// <summary>Authored outer-tick correction fraction and scene-unit penetration slack.</summary>
internal readonly record struct PhysicsContactSettings(float Bias, float AllowedPenetration)
{
    internal static bool ValidBias(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
    internal static bool ValidPenetration(float value) => float.IsFinite(value) && value >= 0 && (value == 0 || value * PhysicsSpace.MetersPerUnit > 0) &&
        float.IsFinite(value * PhysicsSpace.MetersPerUnit * (value * PhysicsSpace.MetersPerUnit));
    internal void Validate()
    {
        if (!ValidBias(Bias) || !ValidPenetration(AllowedPenetration)) throw new ArgumentOutOfRangeException(nameof(PhysicsContactSettings));
    }
    internal static PhysicsContactSettings FromProject() => new(
        ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultContactBias),
        ProjectSettings.GetWithOverride(ProjectSettings.Physics2DContactMaxAllowedPenetration));
}
