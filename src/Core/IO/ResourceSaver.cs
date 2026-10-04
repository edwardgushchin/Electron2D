namespace Electron2D;
/// <summary>Saves typed resources with ordered registered formats and portable archive defaults.</summary>
/// <remarks>Static operations use permanent retained state. Format registration borrows caller-owned savers.
/// Encoding, file I/O, UID discovery and snapshots allocate; no save operation is a frame-loop primitive.</remarks>
public sealed class ResourceSaver : ElectronObject
{
    internal static readonly ResourceSaver Runtime = new();
    private readonly object _gate = new();
    private readonly List<ResourceFormatSaver> _savers = [new ResourceArchiveSaver()];
    private readonly HashSet<Resource> _saving = new(ReferenceEqualityComparer.Instance);
    private ResourceSaver()
    { }
    /// <summary>Registers a borrowed saver without duplicating its identity.</summary><param name="formatSaver">Live extension.</param><param name="atFront">Whether it precedes existing formats.</param>
    public static void AddResourceFormatSaver(ResourceFormatSaver formatSaver, bool atFront = false)
    {
        ArgumentNullException.ThrowIfNull(formatSaver);
        ObjectDisposedException.ThrowIf(formatSaver.IsDisposed, formatSaver);
        lock (Runtime._gate)
        {
            if (Runtime._savers.Any(item => ReferenceEquals(item, formatSaver))) return;
            if (Runtime._savers.Count >= 64) throw new InvalidOperationException("At most 64 format savers can be registered.");
            if (atFront) Runtime._savers.Insert(0, formatSaver);
            else Runtime._savers.Add(formatSaver);
        }
    }
    /// <summary>Removes borrowed registration without disposing the saver.</summary><param name="formatSaver">Extension identity.</param>
    public static void RemoveResourceFormatSaver(ResourceFormatSaver formatSaver)
    {
        ArgumentNullException.ThrowIfNull(formatSaver);
        lock (Runtime._gate) Runtime._savers.RemoveAll(item => ReferenceEquals(item, formatSaver));
    }
    private static ResourceFormatSaver[] Snapshot()
    {
        lock (Runtime._gate) return Runtime._savers.Where(s => !s.IsDisposed).ToArray();
    }
    /// <summary>Returns recognized save extensions in format priority order.</summary><param name="resource">Source resource.</param><returns>A copied ordered extension census.</returns>
    public static string[] GetRecognizedExtensions(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ObjectDisposedException.ThrowIf(resource.IsDisposed, resource);
        return Snapshot().Where(s => s.Recognize(resource)).SelectMany(s => s.GetRecognizedExtensions(resource)).ToArray();
    }
    /// <summary>Saves a resource using the first successful recognized saver.</summary><param name="resource">Caller-owned live resource.</param><param name="path">Explicit destination, or empty selects ResourcePath.</param><param name="flags">Known persistence flags.</param><remarks>ChangePath is temporary and restored even after failure. A built-in archive encode replaces a destination only after complete validation/encoding.</remarks>
    public static void Save(Resource resource, string path = "", SaverFlags flags = SaverFlags.None)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ObjectDisposedException.ThrowIf(resource.IsDisposed, resource);
        ArgumentNullException.ThrowIfNull(path);
        if ((flags & ~(SaverFlags)127) != 0) throw new ArgumentOutOfRangeException(nameof(flags));
        if (path.Length == 0) path = resource.ResourcePath;
        ArgumentException.ThrowIfNullOrEmpty(path);
        path = ResourceUID.EnsurePath(path);
        lock (Runtime._gate) if (!Runtime._saving.Add(resource)) throw new InvalidOperationException("Resource saving cannot reenter itself.");
        var oldPath = resource.ResourcePath;
        var oldRegistered = resource.FilePathRegistered;
        List<Exception>? errors = null;
        try
        {
            foreach (var saver in Snapshot()) if (saver.Recognize(resource) && saver.RecognizePath(resource, path))
                {
                    try
                    {
                        if ((flags & SaverFlags.ChangePath) != 0) resource.SetPathCache(ProjectSettings.LocalizePath(path));
                        saver.Save(resource, path, flags);
                        return;
                    }
                    catch (Exception error)
                    {
                        (errors ??= []).Add(error);
                    }
                    finally
                    {
                        if ((flags & SaverFlags.ChangePath) != 0)
                        {
                            if (oldRegistered) resource.ResourcePath = oldPath;
                            else resource.SetPathCache(oldPath);
                        }
                    }
                }
            if (errors is not null) throw new AggregateException(errors);
            throw new NotSupportedException("No registered saver recognizes this resource/path.");
        }
        finally
        {
            lock (Runtime._gate) Runtime._saving.Remove(resource);
        }
    }
    /// <summary>Reads a persisted UID or prepares a path UID when requested.</summary><param name="path">Resource path.</param><param name="generate">Whether an absent UID is generated and registered.</param><returns>UID or ResourceUID.InvalidID.</returns>
    public static long GetResourceIDForPath(string path, bool generate = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        path = ResourceUID.EnsurePath(path);
        var id = ResourceArchive.ReadUID(path);
        if (id != ResourceUID.InvalidID)
        {
            ResourceUID.SetID(id, path);
            return id;
        }
        id = ResourceUID.FindIDForPath(path);
        if (id != ResourceUID.InvalidID || !generate) return id;
        id = ResourceUID.CreateID();
        ResourceUID.AddID(id, path);
        return id;
    }
    /// <summary>Changes a recognized persisted file UID.</summary><param name="resource">Existing resource path.</param><param name="uid">Nonnegative identity.</param>
    public static void SetUID(string resource, long uid)
    {
        ArgumentException.ThrowIfNullOrEmpty(resource);
        ArgumentOutOfRangeException.ThrowIfNegative(uid);
        resource = ResourceUID.EnsurePath(resource);
        foreach (var saver in Snapshot())
        {
            try
            {
                saver.SetUID(resource, uid);
                ResourceUID.SetID(uid, resource);
                return;
            }
            catch (NotSupportedException)
            { }
        }
        throw new NotSupportedException("No registered saver can replace this UID.");
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The resource-saving service is permanent.");
}
internal sealed class ResourceArchiveSaver : ResourceFormatSaver
{
    public override bool Recognize(Resource resource)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        try
        {
            ResourceFileTypes.Get(resource.GetType());
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
    public override string[] GetRecognizedExtensions(Resource resource) => Recognize(resource) ? resource is PackedScene ? ["e2dscene"] : ["e2dres"] : [];
    public override void Save(Resource resource, string path, SaverFlags flags) => ResourceArchive.Save(resource, path, flags);
    public override void SetUID(string path, long uid) => ResourceArchive.SetUID(path, uid);
}
