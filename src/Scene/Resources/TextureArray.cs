namespace Electron2D;

/// <summary>A resource containing an independently sampled array of image layers.</summary>
/// <remarks>Assign it to a reflected array sampler through ShaderMaterial.SetShaderLayeredParameter.
/// GPU sampling uses the layer coordinate; ordinary canvas rectangle drawing uses Texture instead.</remarks>
public sealed class TextureArray : ImageTextureLayered
{
    /// <summary>Creates an uninitialized array with zero dimensions and layers.</summary>
    public TextureArray() { }
    /// <summary>Creates a caller-owned metadata placeholder with this array's dimensions and layer count.</summary>
    /// <returns>A placeholder containing no copied image data.</returns>
    /// <exception cref="ObjectDisposedException">This array is disposed.</exception>
    public PlaceholderTextureArray CreatePlaceholder() { var pixels = CaptureLayers(); return new() { Size = pixels is null ? default : new(pixels.First.Source.Width, pixels.First.Source.Height), Layers = pixels?.Layers.Length ?? 0 }; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new TextureArray();
}
