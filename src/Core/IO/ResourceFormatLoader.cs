namespace Electron2D;
/// <summary>Defines a caller-owned typed resource-file loading extension.</summary>
/// <remarks>ResourceLoader borrows ordered registration. Hooks execute on the loading thread and return actual
/// caller-owned resources; dependency and cache contracts remain explicit. Threaded requests may invoke hooks
/// concurrently. Retain the borrowed loader until pending requests are collected, prepare independent payloads,
/// and avoid mutating registered resources or scene/native state during preparation.</remarks>
public abstract class ResourceFormatLoader : ElectronObject
{
    /// <summary>Constructs a managed format extension.</summary>
    protected ResourceFormatLoader()
    { }
    /// <summary>Returns copied supported extensions without dots.</summary><returns>Format extensions.</returns>
    public abstract string[] GetRecognizedExtensions();
    /// <summary>Reports support for a requested concrete/base resource type.</summary><param name="type">CLR resource type.</param><returns>Whether the type is supported.</returns>
    public abstract bool HandlesType(Type type);
    /// <summary>Determines the file's exact resource type without constructing resources.</summary><param name="path">Source file.</param><returns>Exact resource type, or null when unrecognized.</returns>
    public abstract Type? GetResourceType(string path);
    /// <summary>Loads a recognized resource file or throws its concrete failure.</summary><param name="path">Resolved source path.</param><param name="originalPath">Caller path identity.</param><param name="useSubThreads">Whether dependency preparation may use subthreads; implementations may stay synchronous.</param><param name="cacheMode">Root/dependency cache policy.</param><returns>A caller-owned resource.</returns>
    public abstract Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode);
    /// <summary>Reports recognized existing-file availability.</summary><param name="path">Source path.</param><returns>Native file existence by default.</returns>
    public virtual bool Exists(string path)
    {
        ThrowIfDisposed();
        return FileAccess.FileExists(path);
    }
    /// <summary>Reports path/type support.</summary><param name="path">Source path.</param><param name="type">Requested resource type; null means no hint.</param><returns>Extension/type recognition by default.</returns>
    public virtual bool RecognizePath(string path, Type? type = null)
    {
        ThrowIfDisposed();
        return (type is null || HandlesType(type)) && GetRecognizedExtensions().Contains(System.IO.Path.GetExtension(path).TrimStart('.'), StringComparer.OrdinalIgnoreCase);
    }
    /// <summary>Reports the stored resource UID.</summary><param name="path">Source path.</param><returns>InvalidID when no UID is retained.</returns>
    public virtual long GetResourceUID(string path)
    {
        ThrowIfDisposed();
        return ResourceUID.InvalidID;
    }
    /// <summary>Returns portable external resource dependencies.</summary><param name="path">Source path.</param><param name="addTypes">Whether to append stable type identity to dependency tokens.</param><returns>Copied dependency tokens.</returns>
    public virtual string[] GetDependencies(string path, bool addTypes = false)
    {
        ThrowIfDisposed();
        return [];
    }
    /// <summary>Rewrites external dependency paths atomically.</summary><param name="path">Source file.</param><param name="renames">Old-to-new path mapping.</param>
    public virtual void RenameDependencies(string path, IReadOnlyDictionary<string, string> renames)
    {
        ThrowIfDisposed();
        throw new NotSupportedException("This format has no dependency rewrite operation.");
    }
    /// <summary>Returns compiled types used by this file.</summary><param name="path">Source path.</param><returns>Copied exact type census.</returns>
    public virtual Type[] GetClassesUsed(string path)
    {
        ThrowIfDisposed();
        var type = GetResourceType(path);
        return type is null ? [] : [type];
    }
    /// <summary>Returns the compiled script-class identity stored by the format.</summary><param name="path">Source path.</param><returns>Empty when no script identity exists.</returns>
    public virtual string GetResourceScriptClass(string path)
    {
        ThrowIfDisposed();
        return "";
    }
}
