namespace Electron2D;

/// <summary>Controls physics participation while a collision object's effective process mode is disabled.</summary>
public enum CollisionDisableMode
{
    /// <summary>Remove the object from its physics world until processing is enabled again.</summary>
    Remove = 0,
    /// <summary>Temporarily make bodies static; areas retain normal sensor participation.</summary>
    MakeStatic = 1,
    /// <summary>Keep normal physics participation while process callbacks remain disabled.</summary>
    KeepActive = 2
}
