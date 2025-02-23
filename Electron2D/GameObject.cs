namespace Electron2D;

public class GameObject(string name)
{
    public string Name { get; private set; } = name;

    public Transform Transform { get; } = new();
    
    private readonly List<Component> _components = [];
    

    public void AddComponent(Component component)
    {
        component.Owner = this;
        _components.Add(component);
    }
    
    public T? GetComponent<T>() where T : Component
    {
        return _components.OfType<T>().FirstOrDefault();
    }

    public void RemoveComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component == null) return;
        _components.Remove(component);
        component.OnDestroy();
    }
    
    protected virtual void Awake() {}

    internal void InternalAwake()
    {
        Awake();
        
        foreach (var component in _components)
        {
            component.Awake();
        }
    }
    
    protected virtual void Start() {}
    
    internal void InternalStart()
    {
        Start();
        
        foreach (var component in _components)
        {
            component.Start();
        }
    }
    
    protected virtual void Update(float deltaTime) {}
    
    internal void InternalUpdate(float deltaTime)
    {
        Update(deltaTime);
        
        foreach (var component in _components)
        {
            component.Update(deltaTime);
        }
    }
    
    protected virtual void OnDestroy() {}

    internal void InternalOnDestroy()
    {
        OnDestroy();
        
        foreach (var component in _components)
        {
            component.OnDestroy();
        }
    }
}