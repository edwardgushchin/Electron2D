namespace Electron2D;

public class Resource : RefCounted
{
    private string _resourceName = string.Empty;
    private string _resourcePath = string.Empty;
    private string _resourceSceneUniqueId = string.Empty;
    private bool _resourceLocalToScene;

    public string ResourceName
    {
        get
        {
            ThrowIfFreed();
            return _resourceName;
        }
        set
        {
            ThrowIfFreed();
            _resourceName = value ?? string.Empty;
        }
    }

    public string ResourcePath
    {
        get
        {
            ThrowIfFreed();
            return _resourcePath;
        }
        protected set
        {
            ThrowIfFreed();
            _resourcePath = value ?? string.Empty;
        }
    }

    public bool ResourceLocalToScene
    {
        get
        {
            ThrowIfFreed();
            return _resourceLocalToScene;
        }
        set
        {
            ThrowIfFreed();
            _resourceLocalToScene = value;
        }
    }

    public string ResourceSceneUniqueId
    {
        get
        {
            ThrowIfFreed();
            return _resourceSceneUniqueId;
        }
        set
        {
            ThrowIfFreed();
            _resourceSceneUniqueId = value ?? string.Empty;
        }
    }

    public void TakeOverPath(string path)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ResourcePath = path;
    }
}
