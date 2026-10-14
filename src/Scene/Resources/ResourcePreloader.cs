using System.Globalization;
using System.Text;

namespace Electron2D;

/// <summary>Retains named resources for direct access by compiled scene code.</summary>
/// <remarks>Resources remain borrowed. Packed scenes preserve aliases, scene-local duplication and loaded graph ownership.
/// Duplicate additions and rename collisions choose the first free numbered suffix from 2; names are case-sensitive.</remarks>
public class ResourcePreloader : Node
{
    private const string StoredPrefix = "PreloadedResource/";
    private static readonly UTF8Encoding NameEncoding = new(false, true);
    private static readonly IComparer<string> NameOrder = Comparer<string>.Create(JSON.CompareKeys);
    private readonly OrderedDictionary<string, Resource> _resources = new(StringComparer.Ordinal);

    /// <summary>Creates an empty resource holder.</summary>
    public ResourcePreloader() { }

    /// <summary>Adds a borrowed resource, choosing a numbered suffix when the name is occupied.</summary>
    /// <param name="name">A valid Unicode name; empty names and NUL are permitted.</param>
    /// <param name="resource">The live resource to retain.</param>
    /// <exception cref="ArgumentNullException">Name or resource is null.</exception>
    /// <exception cref="ArgumentException">Name contains an unpaired UTF16 surrogate.</exception>
    /// <exception cref="ObjectDisposedException">This node or resource is disposed.</exception>
    public void AddResource(string name, Resource resource)
    {
        ValidateMutation(); ValidateName(name); ArgumentNullException.ThrowIfNull(resource);
        ObjectDisposedException.ThrowIf(resource.IsDisposed, resource);
        _resources.Add(FreeName(name), resource);
    }

    /// <summary>Gets a retained borrowed resource by its exact case-sensitive name.</summary>
    /// <param name="name">The registered name.</param>
    /// <returns>The resource identity; its logical lifetime remains controlled by its owner.</returns>
    /// <exception cref="KeyNotFoundException">Name is absent.</exception>
    /// <exception cref="ArgumentNullException">Name is null.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public Resource GetResource(string name)
    {
        CheckRead(); ArgumentNullException.ThrowIfNull(name);
        return _resources.TryGetValue(name, out var resource) ? resource : throw new KeyNotFoundException("The resource name is absent: " + name);
    }

    /// <summary>Copies registered names in insertion order.</summary>
    /// <returns>A caller-owned array; reconstruction from canonical storage may sort that order.</returns>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public string[] GetResourceList() { CheckRead(); return _resources.Keys.ToArray(); }

    /// <summary>Checks name membership without testing the borrowed resource's logical lifetime.</summary>
    /// <param name="name">The exact case-sensitive name.</param>
    /// <returns>True when the name is registered.</returns>
    /// <exception cref="ArgumentNullException">Name is null.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public bool HasResource(string name) { CheckRead(); ArgumentNullException.ThrowIfNull(name); return _resources.ContainsKey(name); }

    /// <summary>Removes a name without disposing its resource.</summary>
    /// <param name="name">The registered name.</param>
    /// <exception cref="KeyNotFoundException">Name is absent.</exception>
    /// <exception cref="ArgumentNullException">Name is null.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public void RemoveResource(string name)
    {
        ValidateMutation(); ArgumentNullException.ThrowIfNull(name);
        if (!_resources.Remove(name)) throw new KeyNotFoundException("The resource name is absent: " + name);
    }

    /// <summary>Moves a resource to a name, with numbered collision handling and insertion order.</summary>
    /// <param name="name">The existing name.</param>
    /// <param name="newname">The requested destination.</param>
    /// <remarks>The source name is considered free; renaming to itself moves it to the end.</remarks>
    /// <exception cref="KeyNotFoundException">Source name is absent.</exception>
    /// <exception cref="ArgumentNullException">A name is null.</exception>
    /// <exception cref="ArgumentException">Destination contains an unpaired UTF16 surrogate.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public void RenameResource(string name, string newname)
    {
        ValidateMutation(); ArgumentNullException.ThrowIfNull(name); ValidateName(newname);
        if (!_resources.TryGetValue(name, out var resource)) throw new KeyNotFoundException("The resource name is absent: " + name);
        var destination = FreeName(newname, name);
        _resources.Remove(name); _resources.Add(destination, resource);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        foreach (var name in _resources.Keys.Order(NameOrder)) yield return Descriptor(name);
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(ResourcePreloader) ? CreateInstance : base.CreateSceneInstanceFactory();
    private static Node CreateInstance() => new ResourcePreloader();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally { if (disposing) _resources.Clear(); }
    }

    internal static PropertyDescriptor? StoredResource(string property, Type valueType)
    {
        if (valueType != typeof(Resource) || !property.StartsWith(StoredPrefix, StringComparison.Ordinal)) return null;
        var encoded = property[StoredPrefix.Length..];
        try
        {
            var bytes = Convert.FromHexString(encoded);
            if (Convert.ToHexString(bytes) != encoded) return null;
            return Descriptor(NameEncoding.GetString(bytes));
        }
        catch (FormatException) { return null; }
        catch (DecoderFallbackException) { return null; }
    }

    private static PropertyDescriptor<ResourcePreloader, Resource?> Descriptor(string name) => new(
        StoredPrefix + Convert.ToHexString(NameEncoding.GetBytes(name)),
        n => { n.CheckRead(); return n._resources.GetValueOrDefault(name); },
        (n, value) => { n.ValidateMutation(); if (value is null) n._resources.Remove(name); else { ObjectDisposedException.ThrowIf(value.IsDisposed, value); n._resources[name] = value; } },
        _ => null, stored: true);
    private void CheckRead() { ThrowIfDisposed(); EnsurePhysicsObjectAccess(); }
    private string FreeName(string name, string? excluded = null)
    {
        if (name == excluded || !_resources.ContainsKey(name)) return name;
        for (var index = 2; ; index = checked(index + 1))
        {
            var candidate = name + " " + index.ToString(CultureInfo.InvariantCulture);
            if (candidate == excluded || !_resources.ContainsKey(candidate)) return candidate;
        }
    }
    private static void ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        try { _ = NameEncoding.GetByteCount(name); }
        catch (EncoderFallbackException error) { throw new ArgumentException("Resource names require valid Unicode.", nameof(name), error); }
    }
}
