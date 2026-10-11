namespace Electron2D;

/// <summary>Shared authored world thresholds in scene units/s, radians/s and seconds.</summary>
/// <param name="LinearThreshold">Linear Threshold in the owning operation's engine-unit contract.</param>
/// <param name="AngularThreshold">Angular Threshold in the owning operation's engine-unit contract.</param>
/// <param name="TimeToSleep">Time To Sleep in the owning operation's engine-unit contract.</param>
public readonly record struct PhysicsSleepSettings(float LinearThreshold, float AngularThreshold, float TimeToSleep)
{
    internal static PhysicsSleepSettings FromProject()
    {
        var settings = ProjectSettings.Service;
        return new(settings.GetWithOverrideCore(ProjectSettings.Physics2DSleepThresholdLinear),
            settings.GetWithOverrideCore(ProjectSettings.Physics2DSleepThresholdAngular),
            settings.GetWithOverrideCore(ProjectSettings.Physics2DTimeBeforeSleep));
    }

    internal static bool ValidLinear(float value) => float.IsFinite(value) && value >= 0 &&
        (value == 0 || value * PhysicsSpace.MetersPerUnit > 0);
    internal static bool ValidNonnegative(float value) => float.IsFinite(value) && value >= 0;
    internal void Validate()
    {
        if (!ValidLinear(LinearThreshold) || !ValidNonnegative(AngularThreshold) || !ValidNonnegative(TimeToSleep))
            throw new ArgumentOutOfRangeException(nameof(PhysicsSleepSettings), "Sleep thresholds must be finite, nonnegative and representable.");
    }
}
