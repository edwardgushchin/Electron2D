namespace Electron2D;

/// <summary>Describes one supported OpenType feature without an untyped metadata container.</summary>
/// <param name="Label">The font-defined localized label, or null when the feature supplies no label.</param>
/// <param name="IsBoolean">True for a registered on/off feature; false for an integer selector, including unknown tags.</param>
/// <param name="Hidden">Whether ordinary feature-selection UI should hide this registered shaping feature.</param>
public readonly record struct FontFeatureInfo(string? Label, bool IsBoolean, bool Hidden);
