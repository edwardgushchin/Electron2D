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
    /// <summary>Defines the accessibility policy sampled when a native display opens.</summary>
    /// <value>Auto by default. Changes apply to the next display lifetime.</value>
    public static ProjectSetting<AccessibilitySupport> AccessibilitySupport { get; } =
        new("accessibility/general/accessibility_support", global::Electron2D.AccessibilitySupport.Auto, value => value is >= global::Electron2D.AccessibilitySupport.Auto and <= global::Electron2D.AccessibilitySupport.Disabled);
}
