namespace Electron2D;

/// <summary>A metadata-only placeholder for an independent image array.</summary>
public sealed class PlaceholderTextureArray : PlaceholderTextureLayered
{
    /// <summary>Creates one metadata layer of size one by one.</summary>
    public PlaceholderTextureArray() { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new PlaceholderTextureArray();
}
