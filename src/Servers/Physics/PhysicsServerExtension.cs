namespace Electron2D;

/// <summary>Base for caller-defined world physics implementations using the complete typed backend hook family.</summary>
/// <remarks>Register a factory with PhysicsServerManager. Each returned object belongs to exactly one world;
/// hooks run under its guarded world context. Solving may borrow the captured numerical thread.
/// The engine owns logical identities and observers, while this implementation owns numerical bodies,
/// constraints, geometry and private history.</remarks>
public abstract class PhysicsServerExtension : PhysicsServerBackend
{
    /// <summary>Creates an unbound implementation for a registered factory.</summary>
    protected PhysicsServerExtension() { }
}
