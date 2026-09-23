namespace Electron2D;

/// <summary>Controls whether a node automatically translates messages.</summary>
public enum NodeAutoTranslateMode
{
    /// <summary>Uses the nearest ancestor's mode; a parentless node enables translation.</summary>
    Inherit = 0,

    /// <summary>Enables automatic translation for this node and inheriting descendants.</summary>
    Always = 1,

    /// <summary>Disables automatic translation for this node and inheriting descendants.</summary>
    Disabled = 2
}
