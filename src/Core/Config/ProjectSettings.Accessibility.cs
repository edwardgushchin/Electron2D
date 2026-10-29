namespace Electron2D;

/// <summary>Selects automatic native accessibility output for a display lifetime.</summary>
public enum AccessibilitySupport
{
    /// <summary>Follows the host accessibility-enabled state.</summary>
    Auto = 0,
    /// <summary>Publishes this application's semantic scene without changing desktop preferences.</summary>
    Always = 1,
    /// <summary>Disables native accessibility output for this display lifetime.</summary>
    Disabled = 2,
}

public sealed partial class ProjectSettings
{
    /// <summary>Selects the native accessibility driver when a display opens.</summary>
    /// <value>"atspi" by default; "dummy" disables the bridge, including explicit element operations.</value>
    /// <remarks>Driver names are case-sensitive. AT-SPI requires the host GIO libraries and accessibility bus.
    /// Unsupported hosts report unavailable. Changes apply to the next display lifetime.</remarks>
    public static ProjectSetting<string> AccessibilityDriver { get; } =
        new("accessibility/general/accessibility_driver", "atspi", value => value is "atspi" or "dummy");

    /// <summary>Limits automatic scene semantic publication to this many passes per wall-clock second.</summary>
    /// <value>60 by default; any positive integer is accepted.</value>
    /// <remarks>Sampled when a SceneTree is created. The first pass runs immediately; later passes coalesce
    /// changes without catch-up bursts. Native requests, retirement and explicit service writes are not throttled.</remarks>
    public static ProjectSetting<int> AccessibilityUpdatesPerSecond { get; } =
        new("accessibility/general/updates_per_second", 60, value => value > 0);

    /// <summary>Defines the accessibility policy sampled when a native display opens.</summary>
    /// <value>Auto by default. Changes apply to the next display lifetime.</value>
    public static ProjectSetting<AccessibilitySupport> AccessibilitySupport { get; } =
        new("accessibility/general/accessibility_support", global::Electron2D.AccessibilitySupport.Auto, value => value is >= global::Electron2D.AccessibilitySupport.Auto and <= global::Electron2D.AccessibilitySupport.Disabled);
}
