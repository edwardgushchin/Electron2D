namespace Electron2D;

public sealed partial class OS
{
    /// <summary>Gets the active graphics driver's reported name and version on Windows and desktop Linux.</summary>
    /// <returns>A caller-owned name and version pair, with empty strings for unavailable fields, or an empty array when neither field is available.</returns>
    /// <remarks>The values belong to the current renderer and querying them does not initialize graphics. Direct3D reads the selected adapter through DXCore when available.</remarks>
    public static string[] GetVideoAdapterDriverInfo()
    {
        if (!(OperatingSystem.IsWindows() || OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid()))
            return [];
        var adapter = RenderingServer.ActiveAdapter;
        return adapter.DriverName.Length == 0 && adapter.DriverVersion.Length == 0 ? [] : [adapter.DriverName, adapter.DriverVersion];
    }
}
