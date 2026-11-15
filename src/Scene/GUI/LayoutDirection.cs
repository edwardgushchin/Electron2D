namespace Electron2D;

/// <summary>Chooses the horizontal direction used to resolve a control or window's layout rectangle.</summary>
public enum LayoutDirection
{
    /// <summary>Inherit from the nearest control or window in the same translation domain, otherwise use the project root policy.</summary>
    Inherited = 0,
    /// <summary>Use the domain's active translation culture, with a matching catalog or fallback language for RTL.</summary>
    ApplicationLocale = 1,
    /// <summary>Resolve from left to right.</summary>
    LTR = 2,
    /// <summary>Resolve from right to left.</summary>
    RTL = 3,
    /// <summary>Use the current managed UI culture, with a matching catalog or fallback language for RTL.</summary>
    SystemLocale = 4,
    /// <summary>Sentinel above the valid selectable directions.</summary>
    Max = 5
}
