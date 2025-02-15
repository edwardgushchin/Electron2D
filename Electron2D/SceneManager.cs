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

    }
    
    public void AddScene(Scene scene, string sceneName)
    {
        _scenes!.Add(sceneName, scene);
    }

    public void AddSceneFromFile(string scenePath, string sceneName)
    {
        
    }
    
    public void LoadScene(string sceneName)
    {
        ActiveScene?.Shutdown();
        ActiveScene = _scenes![sceneName];
    }

    public void Shutdown()
    {
        ActiveScene?.Shutdown();
        _scenes!.Clear();
    }
}