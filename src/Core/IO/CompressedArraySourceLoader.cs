namespace Electron2D;

internal sealed class CompressedArraySourceLoader : ResourceFormatLoader
{
    public override string[] GetRecognizedExtensions() => ["ctexarray"];
    public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(CompressedTextureArray));
    public override Type? GetResourceType(string path) => typeof(CompressedTextureArray);
    public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode)
    {
        var texture = new CompressedTextureArray();
        try { texture.Load(path); return texture; }
        catch { texture.Dispose(); throw; }
    }
}
