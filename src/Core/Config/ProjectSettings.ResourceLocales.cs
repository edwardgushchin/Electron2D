namespace Electron2D;

public sealed partial class ProjectSettings
{
    /// <summary>Defines ordered locale-specific resource file candidates.</summary>
    /// <value>internationalization/locale/translation_remaps; an empty copied array by default, with at most 4096 records.</value>
    /// <remarks>Effective feature overrides apply to subsequent loads and translation refreshes. Highest positive locale
    /// score wins; the first exact match or last tied nonexact match is selected. Missing selected files fall back to
    /// the original. Configuration edits alone do not reload live resources; change Culture or reload pseudolocalization
    /// on the scene owner to refresh them. File reloads allocate and may block.</remarks>
    public static ProjectSetting<ResourceLocaleRemap[]> LocaleResourceRemaps { get; } =
        new("internationalization/locale/translation_remaps", [], value => value.Length <= 4096 && value.All(item => item is not null));
}
