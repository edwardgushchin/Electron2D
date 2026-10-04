namespace Electron2D;
/// <summary>Defines a caller-owned typed resource-file saving extension.</summary>
/// <remarks>ResourceSaver borrows registered instances. Hooks execute synchronously on the saving thread;
/// implementations own encoding/atomic replacement and typed failure semantics.</remarks>
public abstract class ResourceFormatSaver : ElectronObject
{
    /// <summary>Constructs a typed saver extension.</summary>
    protected ResourceFormatSaver()
    { }
    /// <summary>Returns copied format extensions without dots.</summary><param name="resource">Typed resource.</param><returns>Recognized extensions.</returns>
    public abstract string[] GetRecognizedExtensions(Resource resource);
    /// <summary>Reports whether this saver handles the typed resource.</summary><param name="resource">Typed resource.</param><returns>Whether it is recognized.</returns>
    public abstract bool Recognize(Resource resource);
    /// <summary>Reports whether this saver handles a resource/destination pair.</summary><param name="resource">Typed resource.</param><param name="path">Destination path.</param><returns>Extension recognition by default.</returns>
    public virtual bool RecognizePath(Resource resource, string path)
    {
        ThrowIfDisposed();
        return GetRecognizedExtensions(resource).Contains(System.IO.Path.GetExtension(path).TrimStart('.'), StringComparer.OrdinalIgnoreCase);
    }
    /// <summary>Saves one typed resource or throws its concrete failure.</summary><param name="resource">Source resource.</param><param name="path">Destination path.</param><param name="flags">Persistence policies.</param>
    public abstract void Save(Resource resource, string path, SaverFlags flags);
    /// <summary>Replaces the persisted UID without changing resource content.</summary><param name="path">Existing destination.</param><param name="uid">Nonnegative resource UID.</param>
    public virtual void SetUID(string path, long uid)
    {
        ThrowIfDisposed();
        throw new NotSupportedException("This format does not retain UIDs.");
    }
}
