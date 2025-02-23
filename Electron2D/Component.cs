namespace Electron2D;

public abstract class Component
{
    protected internal GameObject? Owner { get; set; }

    protected internal abstract void Awake();
    
    protected internal abstract void Start();
    
    protected internal abstract void Update(float deltaTime);
    
    protected internal abstract void OnDestroy();
}