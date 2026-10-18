namespace Electron2D;

public sealed partial class ProjectSettings
{
    /// <summary>Enables explicitly requested navigation-agent path drawing.</summary>
    /// <value>True by default. Sampled with feature overrides on first navigation diagnostics use; restart to resample.</value>
    public static ProjectSetting<bool> NavigationDebugAgentPaths { get; } = new("debug/shapes/navigation/2d/enable_agent_paths", true);
    /// <summary>Provides the path and waypoint color for agents without a custom color.</summary>
    /// <value>Opaque red by default. Sampled with feature overrides on first navigation diagnostics use.</value>
    public static ProjectSetting<Color> NavigationDebugAgentPathColor { get; } = new("debug/shapes/navigation/2d/agent_path_color", Colors.Red, value => value.IsFinite());
    /// <summary>Provides the square waypoint size for agents using default debug settings.</summary>
    /// <value>Four canvas units by default. Must be finite and nonnegative; sampled with feature overrides on first navigation diagnostics use.</value>
    public static ProjectSetting<float> NavigationDebugAgentPathPointSize { get; } = new("debug/shapes/navigation/2d/agent_path_point_size", 4f, value => float.IsFinite(value) && value >= 0);
}
