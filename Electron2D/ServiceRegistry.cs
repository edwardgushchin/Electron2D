namespace Electron2D;

/// <summary>
/// Very small DI container / Service‑Locator for runtime resolved dependencies.
/// </summary>
internal sealed class ServiceRegistry
{
    private readonly Dictionary<Type, object> _map = new();

    public void Add<T>(T instance) where T : notnull
        => _map[typeof(T)] = instance;

    public T Get<T>() where T : notnull
        => _map.TryGetValue(typeof(T), out var obj)
            ? (T)obj
            : throw new InvalidOperationException($"Service {typeof(T).Name} not found.");
}
