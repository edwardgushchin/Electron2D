namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal PhysicsSleepSettings SleepSettings { get; private set; }
    private bool _sleepSettingsDirty;
    internal void SetSleepSettings(in PhysicsSleepSettings settings)
    {
        EnsureQueryAccess(); EnsureImplementationWriteAccess(); settings.Validate();
        if (SleepSettings == settings && !_sleepSettingsDirty) return;
        SleepSettings = settings;
        _sleepSettingsDirty = true;
        PrepareSleepSettings();
    }
    private void PrepareSleepSettings()
    {
        if (!_sleepSettingsDirty) return;
        EnsureImplementationWriteAccess();
        _backend.SetSleepSettings(SleepSettings);
        _sleepSettingsDirty = false;
    }
}
