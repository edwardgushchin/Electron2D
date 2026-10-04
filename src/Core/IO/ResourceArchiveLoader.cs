namespace Electron2D;

internal sealed class ResourceArchiveLoader : ResourceFormatLoader
{
    public override string[] GetRecognizedExtensions() => ["e2dres", "e2dscene"];
    public override bool HandlesType(Type type) => ResourceFileTypes.SupportsResource(type);
    public override Type? GetResourceType(string path) => ResourceArchive.Inspect(path, out _)[0].Type.Type;
    public override bool RecognizePath(string path, Type? type = null)
    {
        if (!ResourceArchive.IsPath(path) || type is not null && !HandlesType(type)) return false;
        return true;
    }
    public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode) => ResourceLoader.LoadFileResource(originalPath.Length == 0 ? path : originalPath, cacheMode);
    public override long GetResourceUID(string path) => ResourceArchive.ReadUID(path);
    public override string[] GetDependencies(string path, bool addTypes = false) => ResourceArchive.Dependencies(path, addTypes);
    public override void RenameDependencies(string path, IReadOnlyDictionary<string, string> renames) => ResourceArchive.RewriteDependencies(path, renames);
    public override Type[] GetClassesUsed(string path)
    {
        var definitions = ResourceArchive.Inspect(path, out var bigEndian);
        var types = new HashSet<Type>();
        foreach (var d in definitions)
        {
            types.Add(d.Type.Type);
            if (d.Type.Type != typeof(PackedScene) || d.Path.Length != 0) continue;
            using var stream = new StreamPeerBuffer
            {
                DataArray = d.Payload,
                BigEndian = bigEndian
            };
            var count = stream.Get32();
            if (count is < 0 or > 65536) throw new InvalidDataException("Scene count exceeds budget.");
            for (var i = 0; i < count; i++)
            {
                types.Add(ResourceFileTypes.Get(ResourceArchiveStrings.Read(stream)).Type);
                _ = ResourceArchiveStrings.Read(stream);
                _ = stream.Get32();
                _ = stream.Get32();
                _ = stream.Get32();
                var groups = stream.Get32();
                if (groups is < 0 or > 65536) throw new InvalidDataException();
                for (var g = 0; g < groups; g++) _ = ResourceArchiveStrings.Read(stream);
                var fields = stream.Get32();
                if (fields is < 0 or > 65536) throw new InvalidDataException();
                for (var f = 0; f < fields; f++)
                {
                    _ = ResourceArchiveStrings.Read(stream);
                    _ = ResourceArchiveStrings.Read(stream);
                    var size = stream.Get32();
                    if (size < 0 || size > stream.GetAvailableBytes()) throw new InvalidDataException();
                    stream.Seek(stream.GetPosition() + size);
                }
            }
        }
        return types.ToArray();
    }
    public override string GetResourceScriptClass(string path)
    {
        var root = ResourceArchive.Inspect(path, out _)[0];
        return root.Type.Type.Assembly == typeof(Resource).Assembly ? "" : root.Type.ID;
    }
}
