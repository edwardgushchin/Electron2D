namespace Electron2D.Systems;

/// <summary>
/// Convenience base class with sensible defaults.
/// </summary>
internal abstract class SystemBase : ISystem
{
    protected Engine Engine { get; private set; } = null!;

    public virtual int Order => 0;

    public virtual void Initialize(Engine engine)
    {
        Engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public abstract void Update(float dt);

    public virtual void Shutdown() { }
}