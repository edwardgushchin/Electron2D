using SDL3;
using Electron2D.Graphics;

namespace Electron2D.Resources;

public class ResourceManager
{
    private readonly Dictionary<string, IResource> _resources = new();
    
    private readonly Render _render;

    internal ResourceManager(Render render)
    {
        _render = render;
    }

    public Texture LoadTexture(string name, string path)
    {
        if (_resources.TryGetValue(name, out var value))
            return (Texture)value;
        
        var handle = Image.LoadTexture(_render.Handle, path);
        if (handle == IntPtr.Zero)
            throw new Exception($"Failed to load image: {path}, SDL Error: {SDL.GetError()}");
        
        var resource = new Texture(handle, OnNoReferences);
        
        _resources[name] = resource;
        return resource;
    }
    
    /// <summary>
    /// Получает ранее загруженный ресурс по имени.
    /// </summary>
    public T Get<T>(string name) where T : class, IResource
    {
        if (_resources.TryGetValue(name, out var resource))
        {
            if (resource is T typed)
                return typed;
            throw new InvalidCastException($"Resource '{name}' is not of type {typeof(T).Name}.");
        }

        throw new KeyNotFoundException($"Resource '{name}' not found.");
    }

    /// <summary>
    /// Удаляет ресурс.
    /// </summary>
    public bool Unload(string name)
    {
        return _resources.Remove(name);
    }

    public void UnloadAll()
    {
        foreach (var res in _resources.Values)
        {
            if (res is IDisposable d)
                d.Dispose();
        }
        _resources.Clear();
    }
    
    private void OnNoReferences(Texture tex)
    {
        var key = _resources.FirstOrDefault(kv => kv.Value == tex).Key;
        if (key != null)
            _resources.Remove(key);

        tex.Dispose();
    }
}