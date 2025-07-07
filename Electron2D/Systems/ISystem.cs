namespace Electron2D.Systems;

/// <summary>
/// A minimal contract for an Engine-level subsystem.
/// Systems should be lightweight: hold references to services/components
/// and coordinate work each frame. They must be deterministic and free of
/// heavy ctor-logic; all resource allocation should happen in <see cref="Initialize"/>.
/// </summary>
internal interface ISystem
{
    /// <summary>Execution order (lower runs first).</summary>
    int Order { get; }

    /// <summary>Called once after registration, when Engine is ready.</summary>
    void Initialize(Engine engine);

    /// <summary>Per‑frame update in Engine.Update().</summary>
    void Update(float dt);

    /// <summary>Called during Engine.Shutdown() for cleanup.</summary>
    void Shutdown();
}