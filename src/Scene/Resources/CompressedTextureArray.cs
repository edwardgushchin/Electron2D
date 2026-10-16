namespace Electron2D;

/// <summary>Provides file-backed independent image layers for array shader sampling.</summary>
/// <remarks>Load owns copied pixels. Sampler bindings borrow the resource; native shader use requires a backend
/// with image-array sampling. This resource does not add ordinary canvas texture drawing.</remarks>
public sealed class CompressedTextureArray : CompressedTextureLayered
{
    /// <summary>Creates an empty array with no loaded path or image layers.</summary>
    public CompressedTextureArray() { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CompressedTextureArray();
}
