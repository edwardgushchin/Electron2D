namespace Electron2D;

public sealed partial class ProjectSettings
{
    /// <summary>Defines the default collision diagnostic color, sampled by new shape nodes and scene trees.</summary>
    /// <value>Finite RGBA (0, 0.6, 0.7, 0.42); active feature overrides apply at construction.</value>
    public static ProjectSetting<Color> DebugCollisionShapeColor { get; } =
        new("debug/shapes/collision/shape_color", new Color(0, .6f, .7f, .42f), value => value.IsFinite());

    /// <summary>Defines the finite color of contact-point markers in scene collision diagnostics.</summary>
    /// <value>RGBA (1, 0.2, 0.1, 0.8); sampled by new scene trees with feature overrides.</value>
    public static ProjectSetting<Color> DebugCollisionContactColor { get; } =
        new("debug/shapes/collision/contact_color", new Color(1, .2f, .1f, .8f), value => value.IsFinite());

    /// <summary>Defines the maximum number of boundary-point markers captured per diagnostic physics space.</summary>
    /// <value>10,000 initially; nonnegative. New scene trees sample this setting. Zero disables contact capture.</value>
    /// <remarks>Each manifold point contributes up to two boundary points. Enabling diagnostics prepares retained
    /// storage for the limit; limits beyond addressable buffer sizes or available memory fail during preparation.</remarks>
    public static ProjectSetting<int> DebugCollisionMaxContacts { get; } =
        new("debug/shapes/collision/max_contacts_displayed", 10000, value => value >= 0);

    /// <summary>Defines whether filled collision diagnostics also record opaque one-pixel outlines.</summary>
    /// <value>True by default. Changes apply when shape geometry is next recorded; existing commands require redraw.</value>
    public static ProjectSetting<bool> DebugCollisionDrawOutlines { get; } =
        new("debug/shapes/collision/draw_2d_outlines", true);
}
