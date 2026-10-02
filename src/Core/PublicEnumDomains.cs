namespace Electron2D;

/// <summary>Identifies standard pointer shapes for controls, input defaults and the native display.</summary>
public enum CursorShape
{
    /// <summary>The default pointer arrow.</summary>
    Arrow = 0,
    /// <summary>The text-selection I-beam.</summary>
    IBeam = 1,
    /// <summary>The pointing hand for links.</summary>
    PointingHand = 2,
    /// <summary>The crosshair for precise positioning.</summary>
    Cross = 3,
    /// <summary>The nonblocking wait indicator, usually paired with an arrow.</summary>
    Wait = 4,
    /// <summary>The blocking wait indicator, usually replacing the arrow.</summary>
    Busy = 5,
    /// <summary>The dragging hand pointer.</summary>
    Drag = 6,
    /// <summary>The pointer indicating that a dragged item can be dropped.</summary>
    CanDrop = 7,
    /// <summary>The pointer indicating that a dragged item cannot be dropped.</summary>
    Forbidden = 8,
    /// <summary>The vertical-resize pointer.</summary>
    VSize = 9,
    /// <summary>The horizontal-resize pointer.</summary>
    HSize = 10,
    /// <summary>The northeast-southwest diagonal-resize pointer.</summary>
    BDiagSize = 11,
    /// <summary>The northwest-southeast diagonal-resize pointer.</summary>
    FDiagSize = 12,
    /// <summary>The four-direction move pointer.</summary>
    Move = 13,
    /// <summary>The vertical split-resize pointer.</summary>
    VSplit = 14,
    /// <summary>The horizontal split-resize pointer.</summary>
    HSplit = 15,
    /// <summary>The help pointer.</summary>
    Help = 16,
    /// <summary>The number of pointer shapes; not a selectable shape.</summary>
    Max = 17,
}

/// <summary>Defines pointer visibility, capture and window confinement.</summary>
public enum MouseMode
{
    /// <summary>The pointer is visible and free to leave the window.</summary>
    Visible = 0,
    /// <summary>The pointer is hidden and free to leave the window.</summary>
    Hidden = 1,
    /// <summary>The pointer is hidden, captured, and reports relative motion.</summary>
    Captured = 2,
    /// <summary>The visible pointer is confined to the main window.</summary>
    Confined = 3,
    /// <summary>The hidden pointer is confined to the main window.</summary>
    ConfinedHidden = 4,
    /// <summary>The number of pointer modes; not a selectable mode.</summary>
    Max = 5,
}

/// <summary>Identifies a native window's presentation mode.</summary>
public enum WindowMode
{
    /// <summary>A floating window with its configured decorations.</summary>
    Windowed = 0,
    /// <summary>A window minimized by the window manager.</summary>
    Minimized = 1,
    /// <summary>A window expanded to its screen's work area.</summary>
    Maximized = 2,
    /// <summary>A borderless window covering its screen.</summary>
    Fullscreen = 3,
    /// <summary>A fullscreen window requesting a dedicated video mode where supported.</summary>
    /// <remarks>Wayland uses ordinary fullscreen and reports <see cref="Fullscreen"/>.</remarks>
    ExclusiveFullscreen = 4,
}

/// <summary>Identifies a native window policy; values are indices, not a bit mask.</summary>
public enum WindowFlag
{
    /// <summary>Whether dragging the window border is prevented from resizing it.</summary>
    ResizeDisabled = 0,
    /// <summary>Whether the window has no native border and title bar.</summary>
    Borderless = 1,
    /// <summary>Whether the window stays above ordinary windows.</summary>
    /// <remarks>The ordinary Wayland top-level window cannot apply this policy.</remarks>
    AlwaysOnTop = 2,
    /// <summary>Whether the window background can be transparent; requires transparent window creation and a renderer.</summary>
    Transparent = 3,
    /// <summary>Whether the window is prevented from receiving keyboard focus.</summary>
    /// <remarks>SDL supports changing this policy only for Wayland popup-menu windows, which the main window is not.</remarks>
    NoFocus = 4,
    /// <summary>Whether the window is a transient menu popup; requires multiple-window ownership.</summary>
    Popup = 5,
    /// <summary>Whether content extends under the native title bar; requires platform title-bar integration.</summary>
    ExtendToTitle = 6,
    /// <summary>Whether mouse input passes to an underlying application window; requires native hit-test integration.</summary>
    MousePassthrough = 7,
    /// <summary>Whether native rounded window corners are suppressed; requires platform window-style integration.</summary>
    SharpCorners = 8,
    /// <summary>Whether ordinary screen capture excludes the window; requires platform capture-policy integration.</summary>
    ExcludeFromCapture = 9,
    /// <summary>Whether the window manager treats the window as a popup; requires multiple-window ownership.</summary>
    PopupWmHint = 10,
    /// <summary>Whether native minimization controls are disabled; requires platform window-style integration.</summary>
    MinimizeDisabled = 11,
    /// <summary>Whether native maximization controls are disabled; requires platform window-style integration.</summary>
    MaximizeDisabled = 12,
    /// <summary>The number of defined policy identifiers; not a window policy.</summary>
    Max = 13,
}

/// <summary>Aligns content at the leading edge, center, or trailing edge.</summary>
public enum AlignmentMode
{
    /// <summary>Begins at the leading edge.</summary>
    Begin = 0,
    /// <summary>Centers the child group.</summary>
    Center = 1,
    /// <summary>Ends at the trailing edge.</summary>
    End = 2
}

/// <summary>Controls how a nine-patch center region fills one axis in controls and styles.</summary>
public enum AxisStretchMode
{
    /// <summary>Stretches one source center across the destination center.</summary>
    Stretch = 0,
    /// <summary>Repeats at natural pixel size, clipping the last partial tile.</summary>
    Tile = 1,
    /// <summary>Rounds the repeat count and scales complete tiles to fit.</summary>
    TileFit = 2
}

/// <summary>Controls how a texture is placed inside a rectangular control.</summary>
public enum TextureStretchMode
{
    /// <summary>Stretches to the full control rectangle.</summary>
    Scale = 0,
    /// <summary>Repeats at natural logical pixel size.</summary>
    Tile = 1,
    /// <summary>Keeps natural size at the leading top-left position.</summary>
    Keep = 2,
    /// <summary>Centers natural size.</summary>
    KeepCentered = 3,
    /// <summary>Fits aspect with integer-truncated dimensions.</summary>
    KeepAspect = 4,
    /// <summary>Centers an aspect-preserving integer fit.</summary>
    KeepAspectCentered = 5,
    /// <summary>Covers the rectangle using a centered source crop.</summary>
    KeepAspectCovered = 6
}

/// <summary>Selects the physics or idle scene-tree update phase.</summary>
public enum ProcessPhase
{
    /// <summary>Advances during fixed-step physics-process frames.</summary>
    Physics = 0,

    /// <summary>Advances during variable-step process frames.</summary>
    Idle = 1
}

/// <summary>Inherits, disables, or restores a capability within a Control subtree.</summary>
public enum RecursiveBehavior
{
    /// <summary>Follow the direct parent control, or allow the capability when there is none.</summary>
    Inherited = 0,
    /// <summary>Disable the capability unless a descendant explicitly enables it.</summary>
    Disabled = 1,
    /// <summary>Allow the capability regardless of the parent control's policy.</summary>
    Enabled = 2
}

/// <summary>Identifies one vector component.</summary>
public enum Vector2Axis
{
    /// <summary>Identifies the horizontal X component.</summary>
    X = 0,

    /// <summary>Identifies the vertical Y component.</summary>
    Y = 1,
}

/// <summary>Identifies one vector component.</summary>
public enum Vector3Axis
{
    /// <summary>Identifies the X component.</summary>
    X = 0,

    /// <summary>Identifies the Y component.</summary>
    Y = 1,

    /// <summary>Identifies the Z component.</summary>
    Z = 2,

}

/// <summary>Identifies one vector component.</summary>
public enum Vector4Axis
{
    /// <summary>Identifies the X component.</summary>
    X = 0,

    /// <summary>Identifies the Y component.</summary>
    Y = 1,

    /// <summary>Identifies the Z component.</summary>
    Z = 2,

    /// <summary>Identifies the W component.</summary>
    W = 3,
}
