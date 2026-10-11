namespace Electron2D;

/// <summary>An independently sampled image-array resource borrowing rendering-device storage.</summary>
public sealed class TextureArrayRD : TextureLayeredRD
{
    /// <summary>Creates an unbound device image array.</summary>
    public TextureArrayRD() { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new TextureArrayRD();
}
