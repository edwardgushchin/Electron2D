using SDL3;

namespace Electron2D;

internal class Renderer(WindowManager windowManager, ref Settings settings)
{
    private readonly Settings _settings = settings;
    private IntPtr _rendererHandle;

    public void Initialize()
    {
        _rendererHandle = SDL.CreateRenderer(windowManager.GetWindowHandle(), null);

        switch (_settings.VSync)
        {
            case VSyncMode.Disabled:
                SDL.SetRenderVSync(_rendererHandle, SDL.RendererVSyncDisabled);
                break;
            case VSyncMode.EveryFrame:
                SDL.SetRenderVSync(_rendererHandle, 1);
                break;
            case VSyncMode.EverySecondFrame:
                SDL.SetRenderVSync(_rendererHandle, 2);
                break;
            case VSyncMode.Adaptive:
                SDL.SetRenderVSync(_rendererHandle, SDL.RendererVSyncAdaptive);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public void Clear()
    {
        SDL.RenderClear(_rendererHandle);
    }

    public void Present()
    {
        SDL.RenderPresent(_rendererHandle);
    }
    
    public void Shutdown()
    {
        SDL.DestroyRenderer(windowManager.GetWindowHandle());
    }
    
    internal IntPtr GetRendererHandle() => _rendererHandle;
}