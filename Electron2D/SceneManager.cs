namespace Electron2D;

/// <summary>
/// Менеджер сцен: отвечает за загрузку, переключение и выгрузку сцен.
/// </summary>
internal class SceneManager
{
    public Scene? ActiveScene { get; private set; }
    
    private readonly Dictionary<string, Scene>? _scenes = new();

    public void Initialize()
    {
        Logger.Info("Initializing scene manager...");
        
        Logger.Info("Scene Manager initialized successfully.");
    }
    
    public void AddScene(Scene scene, string sceneName)
    {
        _scenes!.Add(sceneName, scene);
        
        Logger.Info($"Scene '{sceneName}' has been successfully added.");
    }

    public void AddSceneFromFile(string scenePath, string sceneName)
    {
        
    }
    
    public void LoadScene(string sceneName, Renderer renderer)
    {
        ActiveScene?.Shutdown();
        ActiveScene = _scenes![sceneName];
        ActiveScene.RenderContext = renderer;
        ActiveScene.RenderContext.SetClearColor(ActiveScene.ClearColor);

        Logger.Info($"Scene '{sceneName}' has been successfully loaded and set as active.");
    }

    public void Shutdown()
    {
        ActiveScene?.Shutdown();
        _scenes!.Clear();
        
        Logger.Info("The scene manager has been successfully shutdown.");
    }
}