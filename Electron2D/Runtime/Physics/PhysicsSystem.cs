namespace Electron2D;

internal sealed class PhysicsSystem
{
    public void Initialize(PhysicsConfig cfg) { }
    public void Shutdown() { }

    public void Step(float fixedDt, SceneTree scene)
    {
        // TODO: Box2D интеграция.
        // Сейчас оставлено как заглушка: компонент Rigidbody уже готов к versioned-sync.
    }
}