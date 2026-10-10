namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal PhysicsSleepSettings SleepSettings { get; private set; }
    internal void SetSleepSettings(in PhysicsSleepSettings settings)
    {
        EnsureQueryAccess(); settings.Validate();
        if (SleepSettings == settings) return;
        SleepSettings = settings;
        _backend.SetSleepSettings(settings);
    }
}
