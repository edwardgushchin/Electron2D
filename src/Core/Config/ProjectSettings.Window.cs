namespace Electron2D;

public sealed partial class ProjectSettings
{
    /// <summary>Sets the base width used for configured project windows and stretch layout.</summary>
    /// <value>1152 by default. Must be positive. Active feature overrides are read at startup.</value>
    public static ProjectSetting<int> ViewportWidth { get; } =
        new("display/window/size/viewport_width", 1152, value => value > 0);

    /// <summary>Sets the base height used for configured project windows and stretch layout.</summary>
    /// <value>648 by default. Must be positive. Active feature overrides are read at startup.</value>
    public static ProjectSetting<int> ViewportHeight { get; } =
        new("display/window/size/viewport_height", 648, value => value > 0);

    /// <summary>Sets the initial client width of an unassigned configured project window on desktop.</summary>
    /// <value>Zero uses the project viewport width. Negative values are invalid. Active feature overrides are read at startup.</value>
    public static ProjectSetting<int> WindowWidthOverride { get; } =
        new("display/window/size/window_width_override", 0, value => value >= 0);

    /// <summary>Sets the initial client height of an unassigned configured project window on desktop.</summary>
    /// <value>Zero uses the project viewport height. Negative values are invalid. Active feature overrides are read at startup.</value>
    public static ProjectSetting<int> WindowHeightOverride { get; } =
        new("display/window/size/window_height_override", 0, value => value >= 0);

    /// <summary>Sets the default rasterization mode of the configured project's root window.</summary>
    /// <value>Disabled by default. Active feature overrides are read at startup.</value>
    public static ProjectSetting<ContentScaleMode> WindowStretchMode { get; } =
        new("display/window/stretch/mode", ContentScaleMode.Disabled, Enum.IsDefined);

    /// <summary>Sets the default aspect fitting policy of the configured project's root window.</summary>
    /// <value>Keep by default. Active feature overrides are read at startup.</value>
    public static ProjectSetting<ContentScaleAspect> WindowStretchAspect { get; } =
        new("display/window/stretch/aspect", ContentScaleAspect.Keep, Enum.IsDefined);

    /// <summary>Sets the default content scale factor of the configured project's root window.</summary>
    /// <value>One by default. Must be finite and positive. Active feature overrides are read at startup.</value>
    public static ProjectSetting<float> WindowStretchScale { get; } =
        new("display/window/stretch/scale", 1f, value => float.IsFinite(value) && value > 0);

    /// <summary>Sets the default fractional or whole-number scaling policy of the configured project's root window.</summary>
    /// <value>Fractional by default. Active feature overrides are read at startup.</value>
    public static ProjectSetting<ContentScaleStretch> WindowStretchScaleMode { get; } =
        new("display/window/stretch/scale_mode", ContentScaleStretch.Fractional, Enum.IsDefined);

    /// <summary>Sets the initial presentation mode of the configured project's root window.</summary>
    /// <value>Windowed by default. The active feature override applies when the window mode is unassigned.</value>
    public static ProjectSetting<WindowMode> WindowInitialMode { get; } =
        new("display/window/size/mode", WindowMode.Windowed, Enum.IsDefined);

    /// <summary>Sets the initial desktop placement policy of the configured project's root window.</summary>
    /// <value>Primary-screen centering by default. The active feature override is read at startup.</value>
    public static ProjectSetting<Window.WindowInitialPosition> WindowInitialPositionType { get; } =
        new("display/window/size/initial_position_type", Window.WindowInitialPosition.CenterPrimaryScreen, Enum.IsDefined);

    /// <summary>Sets the desktop coordinates used by the project's absolute placement policy.</summary>
    /// <value>Zero by default. Signed coordinates support monitors on either side of the desktop origin.</value>
    public static ProjectSetting<Vector2i> WindowInitialPosition { get; } =
        new("display/window/size/initial_position", Vector2i.Zero);

    /// <summary>Sets the desktop screen index used by the project's other-screen centering policy.</summary>
    /// <value>Zero by default. Must be nonnegative. Display availability is checked when the window opens.</value>
    public static ProjectSetting<int> WindowInitialScreen { get; } =
        new("display/window/size/initial_screen", 0, value => value >= 0);
}
