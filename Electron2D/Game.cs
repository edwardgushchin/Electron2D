using SDL3;
using Electron2D.Inputs;
using Electron2D.Platform;
using Electron2D.Graphics;
using Electron2D.Components;
using Electron2D.Resources;

namespace Electron2D;

public abstract class Game : IDisposable
{
    private readonly Engine _engine;
    
    private bool _isRunning;
    
    private float _lastTime = SDL.GetTicks() / 1000f;
    

    protected Game(string windowTitle, Settings? settings = null)
    {
        _engine = new Engine(windowTitle, settings);
        ResourceManager = _engine.ResourceManager;
        
        SubscribeEvents();
    }
    
    private void InternalInitialize()
    {
        if (Camera.ActiveCamera == null)
        {
            var defaultCamera = new Camera("DefaultCamera");
            RootNode.AddChild(defaultCamera);
            Camera.ActiveCamera = defaultCamera;
            var size = _engine.Render.GetWidthAndHeight();
            defaultCamera.UpdateScreenSize(size.width, size.height);
        }
        
        Initialize();
    }
    
    private void InternalWindowResized(int width, int height)
    {
        foreach (var camera in RootNode.FindNodesOfType<Camera>())
        {
            camera.UpdateScreenSize(width, height);
        }
        
        WindowResized(width, height);
    }
    
    private void SubscribeEvents()
    {
        _engine.EventSystem.Quit += () => _isRunning = false;
        
        _engine.EventSystem.WindowResized += (e) =>
        {
            _engine.Render.UpdateRenderSize();
            InternalWindowResized(e.Data1, e.Data2);
        };
    }

    private float GetDeltaTime()
    {
        var currentTime = SDL.GetTicks() / 1000f;
        var delta = currentTime - _lastTime;
        _lastTime = currentTime;
        return delta;
    }

    private void Render()
    {
        _engine.RenderSystem.CollectSprites(RootNode);
        _engine.RenderSystem.RenderAll();
    }
    
    public void Run()
    {
        InternalInitialize();

        _isRunning = true;
        while (_isRunning)
        {
            _engine.EventSystem.PollEvent();
            
            Input.UpdateState(); // обновляем состояние клавиш (вместе с событиями)
        
            RootNode.InternalUpdate(GetDeltaTime()); // здесь внутри можно вызывать Input.GetKeyDown и т.п.
        
            Render();
        }

        Shutdown();
    }
    
    private void Shutdown()
    {
        RootNode.InternalDestroy();
        _engine.Shutdown();
    }

    protected void Exit()
    {
        _isRunning = false;
    }
    
    protected Node RootNode { get; } = new("root");
    
    protected ResourceManager ResourceManager { get; }
    
    protected virtual void Initialize() { }
    
    protected virtual void WindowResized(int width, int height) {}

    public void Dispose()
    {
        _engine.Dispose();
    }
}