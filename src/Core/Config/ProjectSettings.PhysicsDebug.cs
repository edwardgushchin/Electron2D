namespace Electron2D;

public sealed partial class ProjectSettings
{
    /// <summary>Defines the default collision diagnostic color, sampled by new shape nodes and scene trees.</summary>
    /// <value>Finite RGBA (0, 0.6, 0.7, 0.42); active feature overrides apply at construction.</value>
    public static ProjectSetting<Color> DebugCollisionShapeColor { get; } =
        new("debug/shapes/collision/shape_color", new Color(0, .6f, .7f, .42f), value => value.IsFinite());

    /// <summary>Defines whether filled collision diagnostics also record opaque one-pixel outlines.</summary>
    /// <value>True by default. Changes apply when shape geometry is next recorded; existing commands require redraw.</value>
    public static ProjectSetting<bool> DebugCollisionDrawOutlines { get; } =
        new("debug/shapes/collision/draw_2d_outlines", true);
}
