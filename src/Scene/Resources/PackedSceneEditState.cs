namespace Electron2D;

/// <summary>Controls editor metadata applied while a packed scene is instantiated.</summary>
/// <remarks>
/// Runtime builds support only <see cref="Disabled"/>. The remaining values retain stable serialized identities for a
/// future editor and are rejected explicitly until that domain exists.
/// </remarks>
public enum PackedSceneEditState
{
    /// <summary>Creates a runtime scene instance without editable-scene metadata.</summary>
    Disabled = 0,

    /// <summary>Requests local editable-instance metadata from an editor build.</summary>
    Instance = 1,

    /// <summary>Requests main-scene editing metadata from an editor build.</summary>
    Main = 2,

    /// <summary>Requests inherited-main-scene editing metadata from an editor build.</summary>
    MainInherited = 3
}
