namespace Electron2D;

/// <summary>Chooses the horizontal direction used to resolve a control's layout rectangle.</summary>
public enum ControlLayoutDirection
{
    /// <summary>Inherit from the nearest control in the same translation domain.</summary>
    Inherited = 0,
    /// <summary>Use the active application translation locale.</summary>
    ApplicationLocale = 1,
    /// <summary>Deprecated alias for <see cref="ApplicationLocale"/>.</summary>
    [Obsolete("Use ApplicationLocale instead.")]
    Locale = ApplicationLocale,
    /// <summary>Resolve from left to right.</summary>
    LTR = 2,
    /// <summary>Resolve from right to left.</summary>
    RTL = 3,
    /// <summary>Use the current system UI culture.</summary>
    SystemLocale = 4,
    /// <summary>Sentinel above the valid selectable directions.</summary>
    Max = 5
}
