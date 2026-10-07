using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace Electron2D;

internal sealed record ScriptDocument(string Path, Guid Algorithm, byte[] Hash);
internal sealed record ScriptRegistration(string ID, Type Type, string Path, Guid Module, ScriptDocument[] Documents,
    Func<ElectronObject>? Factory, PropertyDescriptor[] Properties);

public sealed partial class Script
{
    private static readonly object CatalogGate = new();
    private static readonly Dictionary<string, ScriptRegistration> ByID = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ScriptRegistration> ByPath = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private static readonly Dictionary<Type, ScriptRegistration> ByType = [];
    private static readonly Guid TypeDocuments = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
    private static readonly Guid SHA256ID = new("8829D00F-11B8-4213-878B-770E8597AC16");
    private static readonly Guid SHA1ID = new("FF1816EC-AA5E-4D10-87F7-6F4963833460");
    /// <summary>Registers one compiled C# node/source association and its exact scene factory and user property schema.</summary>
    /// <typeparam name="TNode">Concrete project Node type.</typeparam><param name="id">Stable portable file type identity.</param>
    /// <param name="sourcePath">Source path proven by the compiled portable PDB.</param><param name="factory">Static exact-type constructor.</param>
    /// <param name="properties">User-defined typed properties; native inherited properties are discovered separately.</param>
    /// <exception cref="ArgumentException">Factory or schema is invalid.</exception><exception cref="InvalidOperationException">Identity is already registered.</exception>
    /// <exception cref="NotSupportedException">Matching compiled assembly and portable source symbols are unavailable.</exception>
    public static void RegisterNode<TNode>(string id, string sourcePath, Func<TNode> factory, params PropertyDescriptor[] properties) where TNode : Node
    {
        ArgumentNullException.ThrowIfNull(factory);
        Register(typeof(TNode), id, sourcePath, () => factory(), properties, () => ResourceFileTypes.RegisterNode(id, factory), factory.Target);
    }
    /// <summary>Registers one compiled C# resource/source association and its exact archive factory and user schema.</summary>
    /// <typeparam name="TResource">Concrete project Resource type.</typeparam><param name="id">Stable portable file type identity.</param>
    /// <param name="sourcePath">Source path proven by portable PDB.</param><param name="factory">Static exact-type constructor.</param>
    /// <param name="properties">User-defined typed property schema.</param>
    /// <exception cref="ArgumentException">Factory or schema is invalid.</exception><exception cref="InvalidOperationException">Identity is already registered.</exception>
    /// <exception cref="NotSupportedException">Matching assembly/source symbols are unavailable.</exception>
    public static void RegisterResource<TResource>(string id, string sourcePath, Func<TResource> factory, params PropertyDescriptor[] properties) where TResource : Resource
    {
        ArgumentNullException.ThrowIfNull(factory);
        Register(typeof(TResource), id, sourcePath, () => factory(), properties, () => ResourceFileTypes.RegisterResource(id, factory), factory.Target);
    }
    /// <summary>Registers an abstract project type for inherited source/metadata discovery without a construction factory.</summary>
    /// <typeparam name="T">Abstract project Node or Resource type.</typeparam><param name="id">Stable script identity.</param>
    /// <param name="sourcePath">Portable-PDB source path.</param><param name="properties">User-defined typed schema.</param>
    /// <exception cref="ArgumentException">The type is concrete or does not derive from Node/Resource.</exception>
    public static void RegisterAbstract<T>(string id, string sourcePath, params PropertyDescriptor[] properties) where T : ElectronObject
    {
        if (!typeof(T).IsAbstract) throw new ArgumentException("The script type must be abstract.");
        Register(typeof(T), id, sourcePath, null, properties, null, null);
    }
    private static void Register(Type type, string id, string path, Func<ElectronObject>? factory, PropertyDescriptor[] properties, Action? registerFileType, object? factoryTarget)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentNullException.ThrowIfNull(properties);
        if (type.Assembly == typeof(Script).Assembly || type.ContainsGenericParameters || !typeof(Node).IsAssignableFrom(type) && !typeof(Resource).IsAssignableFrom(type)) throw new ArgumentException("Scripts require a closed project Node or Resource type.");
        if (id.Length > 256 || id.Any(char.IsControl) || factoryTarget != null) throw new ArgumentException("Script identities must be portable and file factories static.");
        path = System.IO.Path.GetFullPath(ResourceArchive.Absolute(path));
        if (!System.IO.Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase) || type.IsAbstract && factory != null) throw new ArgumentException("Registration requires C# source and an appropriate concrete/abstract factory.");
        var copy = (PropertyDescriptor[])properties.Clone(); var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in copy) if (property == null || property.OwnerType.Assembly == typeof(Script).Assembly || !property.OwnerType.IsAssignableFrom(type) || !names.Add(property.Name)) throw new ArgumentException("Invalid script property schema.", nameof(properties));
        var documents = ReadDocuments(type);
        if (!documents.Any(d => ByPath.Comparer.Equals(d.Path, path))) throw new ArgumentException("The compiled type does not belong to this source document.", nameof(path));
        var record = new ScriptRegistration(id, type, path, type.Module.ModuleVersionId, documents, factory, copy);
        lock (CatalogGate)
        {
            if (ByID.ContainsKey(id) || ByType.ContainsKey(type) || ByPath.ContainsKey(path)) throw new InvalidOperationException("Script ID, type or primary source is already registered.");
            registerFileType?.Invoke(); ByID.Add(id, record); ByType.Add(type, record); ByPath.Add(path, record);
        }
    }
    private static ScriptDocument[] ReadDocuments(Type type)
    {
        var location = type.Assembly.Location;
        if (location.Length == 0) throw new NotSupportedException("Source registration requires a file-backed compiled assembly with portable symbols.");
        using var stream = File.OpenRead(location); using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        if (metadata.GetGuid(metadata.GetModuleDefinition().Mvid) != type.Module.ModuleVersionId) throw new NotSupportedException("The compiled assembly changed; rebuild and restart the project.");
        if (!pe.TryOpenAssociatedPortablePdb(location, File.OpenRead, out var provider, out _)) throw new NotSupportedException("Matching portable source symbols are required.");
        using (provider)
        {
            var pdb = provider!.GetMetadataReader(); var handle = MetadataTokens.TypeDefinitionHandle(type.MetadataToken & 0x00ffffff); var documents = new HashSet<DocumentHandle>();
            foreach (var infoHandle in pdb.GetCustomDebugInformation(handle))
            {
                var info = pdb.GetCustomDebugInformation(infoHandle); if (pdb.GetGuid(info.Kind) != TypeDocuments) continue;
                var blob = pdb.GetBlobReader(info.Value); while (blob.RemainingBytes > 0) documents.Add(MetadataTokens.DocumentHandle(blob.ReadCompressedInteger()));
            }
            foreach (var method in metadata.GetTypeDefinition(handle).GetMethods())
            {
                var debug = pdb.GetMethodDebugInformation(method); if (!debug.Document.IsNil) documents.Add(debug.Document);
                foreach (var point in debug.GetSequencePoints()) if (!point.Document.IsNil) documents.Add(point.Document);
            }
            return documents.Where(d => !d.IsNil).Select(d => { var doc = pdb.GetDocument(d); if (pdb.GetGuid(doc.Language) != new Guid("3F5162F8-07C6-11D3-9053-00C04FA302A1")) throw new NotSupportedException("The script document must be C# source."); var algorithm = pdb.GetGuid(doc.HashAlgorithm); if (algorithm != SHA256ID && algorithm != SHA1ID) throw new NotSupportedException("Unsupported compiled source checksum."); return new ScriptDocument(System.IO.Path.GetFullPath(pdb.GetString(doc.Name)), algorithm, pdb.GetBlobBytes(doc.Hash)); }).OrderBy(d => d.Path, StringComparer.Ordinal).ToArray();
        }
    }
    internal static ScriptRegistration? FindPath(string path) { lock (CatalogGate) return ByPath.GetValueOrDefault(System.IO.Path.GetFullPath(ResourceArchive.Absolute(path))); }
    private static ScriptRegistration? FindType(Type type) { lock (CatalogGate) return ByType.GetValueOrDefault(type); }
    private static ScriptRegistration FindID(string id) { lock (CatalogGate) return ByID.GetValueOrDefault(id) ?? throw new InvalidDataException("Unknown compiled script identity: " + id); }
    internal static byte[] ReadSource(ScriptDocument document)
    {
        using var stream = File.OpenRead(document.Path); if (stream.Length > MaximumSourceBytes) throw new InvalidDataException("Script source exceeds its byte budget.");
        var bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes);
        var hash = document.Algorithm == SHA256ID ? SHA256.HashData(bytes) : SHA1.HashData(bytes);
        if (!CryptographicOperations.FixedTimeEquals(hash, document.Hash)) throw new InvalidDataException("Source differs from the compiled class; rebuild and restart the project: " + document.Path);
        return bytes;
    }
    internal static Script LoadSource(string path)
    {
        var record = FindPath(path) ?? throw new InvalidDataException("No compiled C# type is registered for this source.");
        byte[]? primary = null; foreach (var doc in record.Documents) { var bytes = ReadSource(doc); if (ByPath.Comparer.Equals(doc.Path, record.Path)) primary = bytes; }
        var result = new Script(); result._registration = record; result._sourceCode = DecodeSource(primary!); return result;
    }
}
