namespace Electron2D;

/// <summary>Shapes contextual, bidirectional and vertical text with the packaged native text backend.</summary>
/// <remarks>Each instance owns its service-created fonts and buffers. Register and select it through
/// TextServerManager; release its owned identities and remove it from the registry before disposal.</remarks>
public sealed class TextServerAdvanced : TextServerExtension
{
    /// <summary>Creates an unregistered advanced text implementation.</summary>
    public TextServerAdvanced() { }
    /// <inheritdoc />
    protected override string OnGetName() => "Advanced (ICU/HarfBuzz)";
}
