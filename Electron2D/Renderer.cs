using SDL3;

namespace Electron2D;

internal class Renderer(WindowManager windowManager, ref Settings settings)
{
    private readonly Settings _settings = settings;
    private IntPtr _rendererHandle;

    public void Initialize()
    {
        Logger.Info("Initializing render...");
        
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
        
        Logger.Info("The render has been successfully initialized.");
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
        Logger.Info("The renderer has been successfully shutdown.");
    }
    
    internal IntPtr GetRendererHandle() => _rendererHandle;
}