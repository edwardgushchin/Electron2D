namespace Electron2D;

/// <summary>Represents an absent or stripped material without applying its shading program.</summary>
/// <remarks>Canvas consumers use ordinary color drawing. Tolerant typed archives retain unavailable material
/// fields and resource references for later restoration; this does not recreate the absent shading or a particle
/// compute program. Explicitly created placeholders have no source payload.</remarks>
public sealed class PlaceholderMaterial : Material
{
    internal MissingResourceData Data = new();
    /// <summary>Creates a program-free canvas material with default resource metadata.</summary>
    public PlaceholderMaterial() { }
    internal override MaterialState? GetCanvasState() { ThrowIfDisposed(); return null; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new PlaceholderMaterial();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ThrowIfDisposed(); ((PlaceholderMaterial)target).Data = Data.Copy(duplicateSubresource);
    }
    /// <inheritdoc />
    protected override void RemapResourceReferences(Func<Resource, Resource> remap) { base.RemapResourceReferences(remap); Data.Remap(remap); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { Data.Fields.Clear(); base.Dispose(disposing); }
}
