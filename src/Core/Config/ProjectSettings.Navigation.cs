namespace Electron2D;

public sealed partial class ProjectSettings
{
    /// <summary>Sets the initial topology cell size of each world's navigation map.</summary>
    /// <value>One world unit by default. Finite values are clamped to at least 0.0001 when applied.</value>
    public static ProjectSetting<float> NavigationDefaultCellSize { get; } =
        new("navigation/2d/default_cell_size", 1f, float.IsFinite);

    /// <summary>Sets the initial maximum distance for matching free region edges on world navigation maps.</summary>
    /// <value>One world unit by default. Must be finite and nonnegative.</value>
    public static ProjectSetting<float> NavigationDefaultEdgeConnectionMargin { get; } =
        new("navigation/2d/default_edge_connection_margin", 1f, value => float.IsFinite(value) && value >= 0);

    /// <summary>Sets the initial attachment radius for link endpoints on world navigation maps.</summary>
    /// <value>Four world units by default. Must be finite and nonnegative.</value>
    public static ProjectSetting<float> NavigationDefaultLinkConnectionRadius { get; } =
        new("navigation/2d/default_link_connection_radius", 4f, value => float.IsFinite(value) && value >= 0);

    /// <summary>Sets the initial multiplier applied to the cell size for edge matching on world navigation maps.</summary>
    /// <value>One by default. Finite values are clamped to the range 0.0001 to 0.1 when applied.</value>
    public static ProjectSetting<float> NavigationMergeRasterizerCellScale { get; } =
        new("navigation/2d/merge_rasterizer_cell_scale", 1f, float.IsFinite);

    /// <summary>Enables optional margin connections by default on world navigation maps.</summary>
    /// <value>True by default. Shared raster edges remain connected when false.</value>
    public static ProjectSetting<bool> NavigationUseEdgeConnections { get; } =
        new("navigation/2d/use_edge_connections", true);
}
