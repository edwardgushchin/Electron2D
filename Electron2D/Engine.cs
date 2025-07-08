using Electron2D.Graphics;
using Electron2D.Platform;
using Electron2D.Resources;
using Electron2D.Systems;
using SDL3;

namespace Electron2D;

internal sealed class Engine : IDisposable
{
    public EventSystem EventSystem { get; }
    
    public Window Window { get; }
    
    public Render Render { get; }
    
    public RenderSystem RenderSystem { get; }
    
    public ResourceManager ResourceManager { get; }
    
    public Settings Settings { get; }
    
    
    public Engine(string windowTitle, Settings? settings = null)
    {
        if (!SDL.Init(SDL.InitFlags.Video))
            throw new Exception($"SDL Init failed: {SDL.GetError()}");
        
        Settings = settings ?? new Settings
        {
            Fullscreen = FullscreenMode.Enabled,
            Resizable = false,
            VSync = VSyncMode.Adaptive
        };
        
        EventSystem = new EventSystem();
        Window = new Window(windowTitle, Settings);
        Render = new Render(Window);
        RenderSystem = new RenderSystem(Render);
        ResourceManager = new ResourceManager(Render);
    }

    public void Shutdown()
    {
        SDL.Quit();
    }

    public void Dispose()
    {
        Window.Dispose();
        Render.Dispose();
    }
}