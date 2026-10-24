using SDL3;

namespace Electron2D;

public sealed partial class RenderingServer
{
    internal (Vector2i Size, RenderingDevice.DataFormat Format) DeviceScreen(int screen)
    {
        EnsureDeviceAccess();
        if (_backend is not GPUCanvasBackend gpu || NativeWindow(screen) is null)
            throw new ArgumentOutOfRangeException(nameof(screen), "The native GPU window is absent or retired.");
        var child = screen == 0 ? null : _nativeWindows[screen].Presentation;
        return gpu.ScreenInfo(child);
    }
}

internal sealed unsafe partial class GPUCanvasBackend
{
    private RenderingDevice.DataFormat? _screenFormat;
    internal (Vector2i Size, RenderingDevice.DataFormat Format) ScreenInfo(ChildPresentation? child)
    {
        var window = child?.Window ?? _window;
        Check(SDL.GetWindowSizeInPixels(window, out var width, out var height), "query graphics screen size");
        if (child is null && !_windowClaimed && _screenFormat is { } retained) return (new(width, height), retained);
        var format = SDL.GetGPUSwapchainTextureFormat(Device, window) switch
        {
            SDL.GPUTextureFormat.R8G8B8A8Unorm => RenderingDevice.DataFormat.R8G8B8A8UNORM,
            SDL.GPUTextureFormat.B8G8R8A8Unorm => RenderingDevice.DataFormat.B8G8R8A8UNORM,
            SDL.GPUTextureFormat.R8G8B8A8UnormSRGB => RenderingDevice.DataFormat.R8G8B8A8SRGB,
            SDL.GPUTextureFormat.B8G8R8A8UnormSRGB => RenderingDevice.DataFormat.B8G8R8A8SRGB,
            _ => throw new NotSupportedException("The native screen format is unavailable in this SDR presentation profile."),
        };
        if (child is null) _screenFormat = format;
        return (new(width, height), format);
    }
}
