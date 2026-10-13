namespace Electron2D;

/// <summary>Retains named physics implementation factories and the priority-selected default.</summary>
/// <remarks>Static operations use one retained service object. Selection changes affect later worlds only;
/// Implicit scene worlds capture this priority choice when the project physics engine is DEFAULT.
/// Caller parameterless and enum creation retain their explicit built-in choices. Factories execute outside the registry lock.</remarks>
public sealed class PhysicsServerManager : ElectronObject
{
    private static readonly PhysicsServerManager SharedInstance = new();
    private PhysicsServerManager() { }
    /// <summary>Registers a named factory producing one fresh independent world implementation per creation.</summary>
    /// <param name="name">Nonempty unique name; CPU, GPU and DEFAULT are reserved.</param>
    /// <param name="factory">Factory returning an unbound, undisposed implementation.</param>
    /// <exception cref="ArgumentException">The name is empty, reserved or already registered.</exception>
    /// <exception cref="ArgumentNullException">The name or factory is null.</exception>
    /// <remarks>Null, disposed, reused and recursive factory results reject during creation; existing worlds remain intact.</remarks>
    public static void RegisterServer(string name, Func<PhysicsServerBackend> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        SharedInstance.ThrowIfDisposed();
        PhysicsServer.Service.BackendRegistry.RegisterServer(name, space => new RegisteredPhysicsWorldBackend(space,
            factory() ?? throw new InvalidOperationException("A physics factory returned no implementation.")));
    }
    /// <summary>Selects a registered default only when its priority strictly exceeds the current priority.</summary>
    /// <param name="name">Existing registration name.</param>
    /// <param name="priority">Priority compared with the initial CPU priority of zero; equal priorities keep the earlier choice.</param>
    /// <exception cref="ArgumentException">The name is empty or unregistered.</exception>
    public static void SetDefaultServer(string name, int priority)
    {
        SharedInstance.ThrowIfDisposed();
        PhysicsServer.Service.BackendRegistry.SetDefaultServer(name, priority);
    }
}
