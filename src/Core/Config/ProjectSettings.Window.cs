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

    /// <summary>Allows user resizing of the configured project window.</summary>
    /// <value>True by default. An explicitly assigned window flag takes precedence.</value>
    public static ProjectSetting<bool> WindowResizable { get; } = new("display/window/size/resizable", true);

    /// <summary>Requests a borderless configured project window.</summary>
    /// <value>False by default. An explicitly assigned window flag takes precedence.</value>
    public static ProjectSetting<bool> WindowBorderless { get; } = new("display/window/size/borderless", false);

    /// <summary>Requests that the configured project window stay above ordinary windows.</summary>
    /// <value>False by default. Wayland leaves stacking to the compositor. An explicitly assigned window flag takes precedence.</value>
    public static ProjectSetting<bool> WindowAlwaysOnTop { get; } = new("display/window/size/always_on_top", false);

    /// <summary>Requests that the configured project window accept no keyboard focus.</summary>
    /// <value>False by default. Wayland retains its ordinary top-level focus policy. An explicitly assigned window flag takes precedence.</value>
    public static ProjectSetting<bool> WindowNoFocus { get; } = new("display/window/size/no_focus", false);

    /// <summary>Requests sharp native corners for the configured project window on Windows 11.</summary>
    /// <value>False by default, using the system corner policy. An explicitly assigned window flag takes precedence.</value>
    public static ProjectSetting<bool> WindowSharpCorners { get; } = new("display/window/size/sharp_corners", false);

    /// <summary>Disables the minimize button on the configured native top-level window.</summary>
    /// <value>False by default. On Windows an explicitly assigned window flag takes precedence.</value>
    public static ProjectSetting<bool> WindowMinimizeDisabled { get; } = new("display/window/size/minimize_disabled", false);

    /// <summary>Disables the maximize button on the configured native top-level window.</summary>
    /// <value>False by default. On Windows an explicitly assigned window flag takes precedence.</value>
    public static ProjectSetting<bool> WindowMaximizeDisabled { get; } = new("display/window/size/maximize_disabled", false);

    /// <summary>Allows the configured project's native window transparency request.</summary>
    /// <value>False by default. Read before opening the root surface.</value>
    /// <remarks>Applies to the project flag. Explicit <see cref="Window.Transparent"/> assignments retain their own native contract.</remarks>
    public static ProjectSetting<bool> AllowPerPixelTransparency { get; } =
        new("display/window/per_pixel_transparency/allowed", false);

    /// <summary>Requests a native window capable of presenting per-pixel alpha.</summary>
    /// <value>False by default. Requires <see cref="AllowPerPixelTransparency"/> and a compatible native renderer.</value>
    public static ProjectSetting<bool> WindowTransparent { get; } = new("display/window/size/transparent", false);

    /// <summary>Clears the configured root viewport to transparent black.</summary>
    /// <value>False by default. An explicit <see cref="Viewport.TransparentBG"/> assignment takes precedence.</value>
    /// <remarks>Applies independently of the native window transparency request.</remarks>
    public static ProjectSetting<bool> ViewportTransparentBackground { get; } =
        new("rendering/viewport/transparent_background", false);

    /// <summary>Requests the configured project's main-window presentation synchronization mode.</summary>
    /// <value>Enabled by default. Undefined modes are invalid. The effective feature value is read at startup.</value>
    /// <remarks>The --disable-vsync launch option and <see cref="MovieWriterDisableVSync"/> take precedence.
    /// Unsupported modes fall back to Enabled. Use <see cref="DisplayServer.WindowSetVSyncMode"/> to change a live window.</remarks>
    public static ProjectSetting<DisplayServer.VSyncMode> WindowVSyncMode { get; } =
        new("display/window/vsync/vsync_mode", DisplayServer.VSyncMode.Enabled, Enum.IsDefined);

    /// <summary>Controls whether the configured project requests that the system keep the screen awake.</summary>
    /// <value>True by default. The effective feature value applies at startup.</value>
    /// <remarks>Requires a native screen-inhibition provider. Use <see cref="DisplayServer.ScreenSetKeepOn"/> to change a live request.</remarks>
    public static ProjectSetting<bool> KeepScreenOn { get; } =
        new("display/window/energy_saving/keep_screen_on", true);
}
