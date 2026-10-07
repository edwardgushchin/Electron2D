namespace Electron2D;

/// <summary>Describes an OpenType variation axis's design-coordinate bounds and default.</summary>
/// <param name="Minimum">Minimum design coordinate.</param><param name="Maximum">Maximum design coordinate.</param><param name="Default">Default design coordinate.</param>
public readonly record struct FontVariationAxis(float Minimum, float Maximum, float Default);

internal sealed record FontInstance(int FaceIndex, Dictionary<uint, float> Coordinates, float Embolden, Transform Transform,
    float BaselineOffset, int PaletteIndex, Color[] CustomColors);
