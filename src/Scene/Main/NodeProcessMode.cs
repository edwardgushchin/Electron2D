namespace Electron2D;

/// <summary>Controls when a node receives process and physics-process callbacks.</summary>
/// <remarks>
/// The values affect Electron2D's explicitly enabled host-driven process, physics-process, and scene-input callback
/// lanes. They do not control rendering, audio, or a physics server.
/// </remarks>
public enum NodeProcessMode
{
    /// <summary>Uses the nearest ancestor's resolved mode; hierarchy roots resolve to <see cref="Pausable"/>.</summary>
    Inherit = 0,

    /// <summary>Runs only while the scene tree is not paused.</summary>
    Pausable = 1,

    /// <summary>Runs only while the scene tree is paused.</summary>
    WhenPaused = 2,

    /// <summary>Runs regardless of the scene tree pause state.</summary>
    Always = 3,

    /// <summary>Never runs and disables descendants that inherit this mode.</summary>
    Disabled = 4
}
