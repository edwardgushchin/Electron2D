using SDL3;

namespace Electron2D;

internal class Renderer(WindowManager windowManager, ref Settings settings) : IRenderContext
{
    private readonly Settings _settings = settings;
    private IntPtr _rendererHandle;
    
    private bool _isInitialized = false;
    private Color _pendingClearColor;

    public void Initialize()
    {
        Logger.Info("Initializing render...");
        
        _rendererHandle = SDL.CreateRenderer(windowManager.GetWindowHandle(), null);
        
        _isInitialized = true;
        
        SetClearColor(_pendingClearColor);

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
    
    public void SetClearColor(Color color)
    {
        _pendingClearColor = color;

        if (_isInitialized)
        {
            SDL.SetRenderDrawColor(_rendererHandle, color.R, color.G, color.B, color.A);
        }
    }

    public Color GetClearColor()
    {
        SDL.GetRenderDrawColor(_rendererHandle, out var r, out var g, out var b, out var a);
        return new Color(r, g, b, a);
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