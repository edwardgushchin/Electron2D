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
    /// <summary>Configures edge connection color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(1, 0, 1, 1) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> NavigationDebugEdgeConnectionColor { get; } = new("debug/shapes/navigation/2d/edge_connection_color", new(1, 0, 1, 1), value => value.IsFinite());
    /// <summary>Configures geometry edge color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(.5f, 1, 1, 1) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> NavigationDebugGeometryEdgeColor { get; } = new("debug/shapes/navigation/2d/geometry_edge_color", new(.5f, 1, 1, 1), value => value.IsFinite());
    /// <summary>Configures geometry face color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(.5f, 1, 1, .4f) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> NavigationDebugGeometryFaceColor { get; } = new("debug/shapes/navigation/2d/geometry_face_color", new(.5f, 1, 1, .4f), value => value.IsFinite());
    /// <summary>Configures geometry edge disabled color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(.5f, .5f, .5f, 1) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> NavigationDebugGeometryEdgeDisabledColor { get; } = new("debug/shapes/navigation/2d/geometry_edge_disabled_color", new(.5f, .5f, .5f, 1), value => value.IsFinite());
    /// <summary>Configures geometry face disabled color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(.5f, .5f, .5f, .4f) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> NavigationDebugGeometryFaceDisabledColor { get; } = new("debug/shapes/navigation/2d/geometry_face_disabled_color", new(.5f, .5f, .5f, .4f), value => value.IsFinite());
    /// <summary>Configures link connection color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(1, .5f, 1, 1) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> NavigationDebugLinkConnectionColor { get; } = new("debug/shapes/navigation/2d/link_connection_color", new(1, .5f, 1, 1), value => value.IsFinite());
    /// <summary>Configures link connection disabled color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(.5f, .5f, .5f, 1) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> NavigationDebugLinkConnectionDisabledColor { get; } = new("debug/shapes/navigation/2d/link_connection_disabled_color", new(.5f, .5f, .5f, 1), value => value.IsFinite());
    /// <summary>Configures enable edge connections for retained navigation diagnostics.</summary>
    /// <value>True by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<bool> NavigationDebugEdgeConnections { get; } = new("debug/shapes/navigation/2d/enable_edge_connections", true);
    /// <summary>Configures enable edge lines for retained navigation diagnostics.</summary>
    /// <value>True by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<bool> NavigationDebugEdgeLines { get; } = new("debug/shapes/navigation/2d/enable_edge_lines", true);
    /// <summary>Configures enable geometry face random color for retained navigation diagnostics.</summary>
    /// <value>True by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<bool> NavigationDebugGeometryFaceRandomColor { get; } = new("debug/shapes/navigation/2d/enable_geometry_face_random_color", true);
    /// <summary>Configures enable link connections for retained navigation diagnostics.</summary>
    /// <value>True by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<bool> NavigationDebugLinkConnections { get; } = new("debug/shapes/navigation/2d/enable_link_connections", true);
    /// <summary>Configures agents radius color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(1, 1, 0, .25f) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> AvoidanceDebugAgentsRadiusColor { get; } = new("debug/shapes/avoidance/2d/agents_radius_color", new(1, 1, 0, .25f), value => value.IsFinite());
    /// <summary>Configures obstacles radius color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(1, .5f, 0, .25f) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> AvoidanceDebugObstaclesRadiusColor { get; } = new("debug/shapes/avoidance/2d/obstacles_radius_color", new(1, .5f, 0, .25f), value => value.IsFinite());
    /// <summary>Configures obstacles static face pushin color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(1, 0, 0, 0) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> AvoidanceDebugStaticFacePushInColor { get; } = new("debug/shapes/avoidance/2d/obstacles_static_face_pushin_color", new(1, 0, 0, 0), value => value.IsFinite());
    /// <summary>Configures obstacles static edge pushin color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(1, 0, 0, 1) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> AvoidanceDebugStaticEdgePushInColor { get; } = new("debug/shapes/avoidance/2d/obstacles_static_edge_pushin_color", new(1, 0, 0, 1), value => value.IsFinite());
    /// <summary>Configures obstacles static face pushout color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(1, 1, 0, .5f) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> AvoidanceDebugStaticFacePushOutColor { get; } = new("debug/shapes/avoidance/2d/obstacles_static_face_pushout_color", new(1, 1, 0, .5f), value => value.IsFinite());
    /// <summary>Configures obstacles static edge pushout color for retained navigation diagnostics.</summary>
    /// <value>RGBA new(1, 1, 0, 1) by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<Color> AvoidanceDebugStaticEdgePushOutColor { get; } = new("debug/shapes/avoidance/2d/obstacles_static_edge_pushout_color", new(1, 1, 0, 1), value => value.IsFinite());
    /// <summary>Configures enable agents radius for retained navigation diagnostics.</summary>
    /// <value>True by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<bool> AvoidanceDebugAgentsRadius { get; } = new("debug/shapes/avoidance/2d/enable_agents_radius", true);
    /// <summary>Configures enable obstacles radius for retained navigation diagnostics.</summary>
    /// <value>True by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<bool> AvoidanceDebugObstaclesRadius { get; } = new("debug/shapes/avoidance/2d/enable_obstacles_radius", true);
    /// <summary>Configures enable obstacles static for retained navigation diagnostics.</summary>
    /// <value>True by default; sampled with feature overrides on first global geometry diagnostics use. Restart to resample.</value>
    public static ProjectSetting<bool> AvoidanceDebugObstaclesStatic { get; } = new("debug/shapes/avoidance/2d/enable_obstacles_static", true);
}
