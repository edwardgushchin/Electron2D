namespace Electron2D;

/// <summary>Controls editor metadata applied while a packed scene is instantiated.</summary>
/// <remarks>
/// Gameplay uses <see cref="Disabled"/>. The other modes require <see cref="Engine.IsEditorHint"/>.
/// </remarks>
public enum PackedSceneEditState
{
    /// <summary>Creates a runtime scene instance without editable-scene metadata.</summary>
    Disabled = 0,

    /// <summary>Creates an editable instance with independent scene-local resources and source property revert values.</summary>
    Instance = 1,

    /// <summary>Opens the main authored scene with saved pins and its original scene-local resources.</summary>
    Main = 2,

    /// <summary>Creates a detached editable root that packs as a new scene derived from the source template.</summary>
    MainInherited = 3
}
