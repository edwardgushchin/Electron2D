namespace Electron2D;

internal sealed class CompressedTextureSourceLoader : ResourceFormatLoader
{
    public override string[] GetRecognizedExtensions() => ["ctex"];
    public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(CompressedTexture));
    public override Type? GetResourceType(string path) => typeof(CompressedTexture);
    public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode)
    {
        var texture = new CompressedTexture();
        try { texture.Load(path); return texture; }
        catch { texture.Dispose(); throw; }
    }
}
