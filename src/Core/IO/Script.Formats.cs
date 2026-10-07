namespace Electron2D;

internal sealed class ScriptSourceLoader : ResourceFormatLoader
{
    public override string[] GetRecognizedExtensions() => ["cs"];
    public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(Script));
    public override Type? GetResourceType(string path) => Script.FindPath(path) == null ? null : typeof(Script);
    public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode) => Script.LoadSource(path);
    public override string GetResourceScriptClass(string path) => Script.FindPath(path)?.ID ?? "";
    public override Type[] GetClassesUsed(string path) => Script.FindPath(path) is { } entry ? [typeof(Script), entry.Type] : [];
    public override string[] GetDependencies(string path, bool addTypes = false) => Script.FindPath(path)?.Documents.Where(d => d.Path != ResourceArchive.Absolute(path)).Select(d => addTypes ? d.Path + "::CSharpSource" : d.Path).ToArray() ?? [];
}
internal sealed class ScriptSourceSaver : ResourceFormatSaver
{
    public override string[] GetRecognizedExtensions(Resource resource) => resource is Script ? ["cs"] : [];
    public override bool Recognize(Resource resource) => resource is Script;
    public override void Save(Resource resource, string path, SaverFlags flags) => ResourceArchive.AtomicWrite(path, new System.Text.UTF8Encoding(false, true).GetBytes(((Script)resource).SourceCode));
}
