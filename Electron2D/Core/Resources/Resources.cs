namespace Electron2D;

public static class Resources
{
    private static ResourceSystem? _sys;

    internal static void Bind(ResourceSystem sys) => _sys = sys;
    internal static void Unbind() => _sys = null;

    private static ResourceSystem Sys =>
        _sys ?? throw new InvalidOperationException("Resources are not available (Engine is not running or already disposed).");

    public static Texture GetTexture(string id) => Sys.GetTexture(id);

    public static bool TryGetTexture(string id, out Texture texture) => Sys.TryGetTexture(id, out texture);

    public static void UnloadTexture(string id) => Sys.UnloadTexture(id);
}