namespace Electron2D;

/// <summary>Controls editor metadata applied while a packed scene is instantiated.</summary>
/// <remarks>
/// Gameplay uses <see cref="Disabled"/>. <see cref="MainInherited"/> is available in editor context.
/// <see cref="Instance"/> and <see cref="Main"/> are reserved and rejected explicitly.
/// </remarks>
public enum PackedSceneEditState
{
    /// <summary>Creates a runtime scene instance without editable-scene metadata.</summary>
    Disabled = 0,

    /// <summary>Requests local editable-instance metadata from an editor build.</summary>
    Instance = 1,

    /// <summary>Requests main-scene editing metadata from an editor build.</summary>
    Main = 2,

    /// <summary>Creates a detached editable root that packs as a new scene derived from the source template.</summary>
    MainInherited = 3
}
