namespace Electron2D.Systems;

/// <summary>
/// Central registry & runner for all ISystem instances.
/// Call Register() before Engine.Initialize().
/// </summary>
internal sealed class SystemManager
{
    private readonly List<ISystem> _systems = new();

    public void Register(ISystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        _systems.Add(system);
        _systems.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    public void InitializeAll(Engine engine)
    {
        foreach (var s in _systems)
            s.Initialize(engine);
    }

    public void UpdateAll(float dt)
    {
        foreach (var s in _systems)
            s.Update(dt);
    }

    public void ShutdownAll()
    {
        for (var i = _systems.Count - 1; i >= 0; i--)
            _systems[i].Shutdown();
    }
}