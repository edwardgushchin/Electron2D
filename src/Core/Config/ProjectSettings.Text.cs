namespace Electron2D;

public sealed partial class ProjectSettings
{
    /// <summary>Selects a registered text implementation by its exact name when the engine starts.</summary>
    /// <value>Empty by default, preserving the current selection; the initial selection is Advanced (ICU/HarfBuzz).</value>
    /// <remarks>Sampled before project themes and the main loop start. Built-in names are Advanced (ICU/HarfBuzz),
    /// Fallback (Built-in) and Dummy. A missing name rejects startup. Feature overrides apply normally;
    /// changing the value during a run takes effect on the next engine lifecycle.</remarks>
    public static ProjectSetting<string> TextDriver { get; } =
        new("internationalization/rendering/text_driver", string.Empty, value => value.Length <= 65536 && !value.Contains('\0'));
}
