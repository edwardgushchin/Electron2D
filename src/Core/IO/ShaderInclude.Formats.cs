namespace Electron2D;

internal sealed class ShaderIncludeSourceLoader : ResourceFormatLoader
{
    public override string[] GetRecognizedExtensions() => ["hlslinc", "glslinc"];
    public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(ShaderInclude));
    public override Type? GetResourceType(string path) => typeof(ShaderInclude);
    public override Type[] GetClassesUsed(string path) => [typeof(ShaderInclude)];
    public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode)
    {
        using var file = FileAccess.Open(path, FileAccessModeFlags.Read);
        var length = file.Length;
        if (length > ShaderInclude.MaximumSourceBytes) throw new InvalidDataException("Shader include source exceeds its byte budget.");
        var bytes = file.ReadBytes(checked((int)length));
        if (bytes.Length != length || file.Length != length) throw new InvalidDataException("Shader include source changed during its read.");
        var code = ShaderInclude.SourceEncoding.GetString(bytes);
        return new ShaderInclude { Code = code };
    }
}
internal sealed class ShaderIncludeSourceSaver : ResourceFormatSaver
{
    public override string[] GetRecognizedExtensions(Resource resource) => resource is ShaderInclude ? ["hlslinc", "glslinc"] : [];
    public override bool Recognize(Resource resource) => resource is ShaderInclude;
    public override void Save(Resource resource, string path, SaverFlags flags) => ResourceArchive.AtomicWrite(path, ShaderInclude.SourceEncoding.GetBytes(((ShaderInclude)resource).Code));
}
