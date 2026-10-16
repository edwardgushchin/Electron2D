namespace Electron2D;

public partial class ProjectSettingsRegistry
{
    internal Action<ShaderGlobalParameters>[] CaptureShaderGlobals()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return _settings.Values.Where(entry => entry.Name.StartsWith("shader_globals/", StringComparison.Ordinal))
                .OrderBy(entry => entry.Order).ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .Select(entry => entry switch
                {
                    SettingEntry<GlobalShaderParameterSettings<bool>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<int>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<uint>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<float>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<Vector2i>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<Vector3i>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<Vector4i>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<Rect2i>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<Vector2>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<Vector3>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<Vector4>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<Color>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<Rect2>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<Transform>> typed => CaptureShaderNumber(typed),
                    SettingEntry<GlobalShaderParameterSettings<float[]>> typed => CaptureShaderMatrix(typed),
                    SettingEntry<GlobalShaderParameterSettings<string>> typed => CaptureShaderImage(typed),
                    _ => throw new InvalidDataException($"Project shader global '{entry.Name}' requires an exact supported GlobalShaderParameterSettings carrier.")
                }).ToArray();
        }
    }
    private Action<ShaderGlobalParameters> CaptureShaderNumber<T>(SettingEntry<GlobalShaderParameterSettings<T>> entry) where T : unmanaged
    {
        var data = GetWithOverrideCore((ProjectSetting<GlobalShaderParameterSettings<T>>)entry.Definition); ValidateShaderVersion(data.Version);
        var name = entry.Name["shader_globals/".Length..];
        return target => target.Add(name, data.Type, data.Value);
    }
    private Action<ShaderGlobalParameters> CaptureShaderMatrix(SettingEntry<GlobalShaderParameterSettings<float[]>> entry)
    {
        var data = GetWithOverrideCore((ProjectSetting<GlobalShaderParameterSettings<float[]>>)entry.Definition); ValidateShaderVersion(data.Version);
        var name = entry.Name["shader_globals/".Length..];
        return target => target.AddMatrix(name, data.Type, data.Value);
    }
    private Action<ShaderGlobalParameters> CaptureShaderImage(SettingEntry<GlobalShaderParameterSettings<string>> entry)
    {
        var data = GetWithOverrideCore((ProjectSetting<GlobalShaderParameterSettings<string>>)entry.Definition); ValidateShaderVersion(data.Version);
        var name = entry.Name["shader_globals/".Length..];
        var path = data.Value is null ? throw new InvalidDataException("A project shader image path cannot be null.") : data.Value.Length == 0 ? "" : GlobalizePathCore(data.Value);
        return target => target.AddProjectImage(name, data.Type, path);
    }
    private static void ValidateShaderVersion(int version)
    { if (version != 1) throw new InvalidDataException($"Unsupported project shader parameter schema version {version}."); }
}
