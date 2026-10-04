namespace Electron2D;

/// <summary>Selects whether an item's drawn alpha masks its same-Z canvas descendants.</summary>
public enum ClipChildrenMode
{
    /// <summary>Draws the item and descendants normally.</summary>
    Disabled = 0,
    /// <summary>Uses the item's alpha as a mask without drawing its own color.</summary>
    Only = 1,
    /// <summary>Draws the item before its descendants, then masks their combined color.</summary>
    AndDraw = 2,
    /// <summary>Defines the exclusive upper bound; it is not an assignable mode.</summary>
    Max = 3
}
