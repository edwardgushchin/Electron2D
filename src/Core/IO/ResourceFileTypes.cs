namespace Electron2D;

/// <summary>Registers stable compiled factories and typed portable value codecs for resource/scene files.</summary>
/// <remarks>Registration is allocating setup. IDs are ordinal and immutable; file data never loads assemblies,
/// invokes reflected members or constructs arbitrary CLR types. Register the same schemas before saving and in
/// every loading process. Factories are direct static delegates and must return fresh exact-type instances.</remarks>
public static partial class ResourceFileTypes
{
    private static object Gate => ResourceLoader.Runtime.FileTypes.Gate;
    private static Dictionary<string, ResourceFileType> ByID => ResourceLoader.Runtime.FileTypes.ByID;
    private static Dictionary<Type, ResourceFileType> ByType => ResourceLoader.Runtime.FileTypes.ByType;
    private static Dictionary<Type, ResourceValueCodec> Codecs => ResourceLoader.Runtime.FileTypes.Codecs;
    private static Dictionary<Type, ResourceArrayCodec> ResourceArrays => ResourceLoader.Runtime.FileTypes.ResourceArrays;
    static ResourceFileTypes()
    {
        RegisterNode("Node", CreateNode);
        RegisterNode("Entity", CreateEntity);
        RegisterResource("Resource", CreateResource);
        RegisterResource("PackedScene", CreatePackedScene);
        RegisterResource("PhysicsMaterial", CreatePhysicsMaterial);
        RegisterResource("ColorPalette", CreateColorPalette);
        RegisterNode("Sprite", CreateSprite);
        RegisterResource("Image", CreateImage);
        RegisterResource("ImageTexture", CreateImageTexture);
        RegisterResource("AtlasTexture", CreateAtlasTexture);

        RegisterBuiltInNodes();
        RegisterAudioFileResources();
        RegisterResource("StyleBoxFlat", CreateFlatFileResource);
        RegisterResource("StyleBoxLine", CreateLineFileResource);
        RegisterResource("StyleBoxTexture", CreateTextureStyleFileResource);
        RegisterResource("StyleBoxEmpty", CreateEmptyFileResource);
        RegisterResource("FontFile", CreateFontFileResource);
        RegisterResource("CircleShape", CreateCircleFileResource);
        RegisterResource("CapsuleShape", CreateCapsuleFileResource);
        RegisterResource("SegmentShape", CreateSegmentFileResource);
        RegisterResource("RectangleShape", CreateRectangleFileResource);
        ResourceFileCodecs.Prepare();
        RegisterResourceArray<Font>();
    }
    private static Node CreateNode() => new();
    private static Entity CreateEntity() => new();
    private static Resource CreateResource() => new();
    private static PackedScene CreatePackedScene() => new();
    private static PhysicsMaterial CreatePhysicsMaterial() => new();
    private static ColorPalette CreateColorPalette() => new();
    private static Sprite CreateSprite() => new();
    private static Image CreateImage() => new();
    private static ImageTexture CreateImageTexture() => new();
    private static AtlasTexture CreateAtlasTexture() => new();
    private static CircleShape CreateCircleFileResource() => new();
    private static CapsuleShape CreateCapsuleFileResource() => new();
    private static SegmentShape CreateSegmentFileResource() => new();
    private static RectangleShape CreateRectangleFileResource() => new();
    private static StyleBoxFlat CreateFlatFileResource() => new();
    private static StyleBoxLine CreateLineFileResource() => new();
    private static StyleBoxTexture CreateTextureStyleFileResource() => new();
    private static StyleBoxEmpty CreateEmptyFileResource() => new();
    private static FontFile CreateFontFileResource() => new();
    /// <summary>Registers a compiled node factory used by file-backed PackedScene.</summary><typeparam name="TNode">Exact concrete node type.</typeparam><param name="id">Stable portable schema ID.</param><param name="factory">Static exact-type constructor.</param>
    public static void RegisterNode<TNode>(string id, Func<TNode> factory) where TNode : Node => Register(id, factory);
    /// <summary>Registers a compiled resource factory with storage-enabled typed properties.</summary><typeparam name="TResource">Exact concrete resource type.</typeparam><param name="id">Stable portable schema ID.</param><param name="factory">Static exact-type constructor.</param>
    public static void RegisterResource<TResource>(string id, Func<TResource> factory) where TResource : Resource
    {
        Register(id, factory);
        RegisterResourceArray<TResource>();
    }
    private static void Register<T>(string id, Func<T> factory) where T : ElectronObject
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(factory);
        if (id.Length > 256 || id.Any(c => char.IsControl(c))) throw new ArgumentException("Type ID is not a portable token.", nameof(id));
        if (factory.Target is not null) throw new ArgumentException("File factories must be static.", nameof(factory));
        lock (Gate)
        {
            if (ByID.ContainsKey(id) || ByType.ContainsKey(typeof(T))) throw new InvalidOperationException("Type or ID is already registered.");
            var type = new ResourceFileType(id, typeof(T), () => factory());
            ByID.Add(id, type);
            ByType.Add(typeof(T), type);
        }
    }
    /// <summary>Registers explicit portable encoding for a concrete stored value type.</summary><typeparam name="TValue">Concrete scalar/math/array value type; no delegate, native pointer, node or resource.</typeparam><param name="write">Direct value writer; observes the stream's endian setting.</param><param name="read">Direct value reader with validation.</param><remarks>Built-in codecs cannot be replaced. Application codecs are part of the agreed file schema.</remarks>
    public static void RegisterValueCodec<TValue>(Action<StreamPeerBuffer, TValue> write, Func<StreamPeerBuffer, TValue> read)
    {
        ArgumentNullException.ThrowIfNull(write);
        ArgumentNullException.ThrowIfNull(read);
        var type = typeof(TValue);
        if (type == typeof(nint) || type == typeof(nuint) || type == typeof(RID) || typeof(ElectronObject).IsAssignableFrom(type) || typeof(Delegate).IsAssignableFrom(type)) throw new ArgumentException("Transient identities are not portable values.");
        lock (Gate)
        {
            if (!Codecs.TryAdd(type, new ResourceValueCodec<TValue>(write, read))) throw new InvalidOperationException("Value codec already registered.");
        }
    }
    private static void RegisterResourceArray<T>() where T : Resource
    {
        lock (Gate) ResourceArrays.TryAdd(typeof(T[]), new ResourceArrayCodec<T>());
    }
    internal static ResourceArrayCodec ResourceArray(Type type)
    {
        lock (Gate) return ResourceArrays.GetValueOrDefault(type) ?? throw new NotSupportedException("Resource array requires a registered element schema.");
    }
    internal static bool SupportsResource(Type type)
    {
        lock (Gate) return ByType.Keys.Any(t => typeof(Resource).IsAssignableFrom(t) && type.IsAssignableFrom(t));
    }
    internal static string[] Extensions(Type type)
    {
        lock (Gate)
        {
            var extensions = new List<string>();
            if (ByType.Keys.Any(t => typeof(Resource).IsAssignableFrom(t) && t != typeof(PackedScene) && type.IsAssignableFrom(t))) extensions.Add("e2dres");
            if (type.IsAssignableFrom(typeof(PackedScene))) extensions.Add("e2dscene");
            return extensions.ToArray();
        }
    }
    internal static ResourceFileType Get(Type type)
    {
        lock (Gate) return ByType.GetValueOrDefault(type) ?? throw new NotSupportedException($"File type {type.Name} has no registered factory.");
    }
    internal static ResourceFileType Get(string id)
    {
        lock (Gate) return ByID.GetValueOrDefault(id) ?? throw new InvalidDataException($"Unknown file type ID '{id}'.");
    }
    internal static string ValueID(Type type) => type.IsArray ? ValueID(type.GetElementType()!) + "[]" : type.IsConstructedGenericType ? type.GetGenericTypeDefinition().FullName!.Split('`')[0] + "<" + string.Join(",", type.GetGenericArguments().Select(ValueID)) + ">" : type.FullName!;
    internal static Type? FindValueType(string id)
    {
        lock (Gate) return Codecs.Keys.Concat(ByType.Keys.SelectMany(Ancestors)).FirstOrDefault(t => ValueID(t) == id);
    }
    private static IEnumerable<Type> Ancestors(Type type)
    {
        for (Type? current = type; current is not null; current = current.BaseType) yield return current;
    }
    internal static bool HasCodec(Type type)
    {
        lock (Gate) return Codecs.ContainsKey(type);
    }
    internal static ResourceValueCodec<T> Codec<T>()
    {
        lock (Gate)
        {
            if (Codecs.TryGetValue(typeof(T), out var codec)) return (ResourceValueCodec<T>)codec;
            if (typeof(T).IsEnum)
            {
                var unsigned = Enum.GetUnderlyingType(typeof(T)) == typeof(ulong);
                var created = unsigned ? new ResourceValueCodec<T>((b, v) => b.PutU64(Convert.ToUInt64(v)), b => (T)Enum.ToObject(typeof(T), b.GetU64())) : new ResourceValueCodec<T>((b, v) => b.Put64(Convert.ToInt64(v)), b => (T)Enum.ToObject(typeof(T), b.Get64()));
                Codecs.Add(typeof(T), created);
                return created;
            }
        }
        throw new NotSupportedException($"Stored value type {typeof(T).Name} has no portable codec.");
    }
}
internal sealed class ResourceFileType(string id, Type type, Func<ElectronObject> create)
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ElectronObject, object> _issued = new();
    internal string ID => id;
    internal Type Type => type;
    internal ElectronObject Factory()
    {
        var value = create() ?? throw new InvalidOperationException("File factory returned null.");
        if (!_issued.TryAdd(value, new object())) throw new InvalidOperationException("File factory reused an issued object.");
        if (value.IsDisposed) throw new InvalidOperationException("File factory returned a disposed identity.");
        if (value is Node attached && (attached.Parent is not null || attached.Tree is not null || attached.Owner is not null)) throw new InvalidOperationException("File node factory returned a borrowed attached node.");
        if (value.GetType() != type || value is Node node && node.Children.Count != 0)
        {
            var error = new InvalidOperationException("File factory must return a fresh exact default type.");
            try { value.Dispose(); } catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
            throw error;
        }
        return value;
    }
}
internal abstract class ResourceValueCodec;
internal sealed class ResourceValueCodec<T>(Action<StreamPeerBuffer, T> write, Func<StreamPeerBuffer, T> read) : ResourceValueCodec
{
    internal void Write(StreamPeerBuffer stream, T value) => write(stream, value);
    internal T Read(StreamPeerBuffer stream) => read(stream);
}

internal abstract class ResourceArrayCodec
{
    internal abstract Resource?[] Read(StreamPeerBuffer stream, ResourceArchiveRead context);
}
internal sealed class ResourceArrayCodec<T> : ResourceArrayCodec where T : Resource
{
    internal override Resource?[] Read(StreamPeerBuffer stream, ResourceArchiveRead context)
    {
        var count = stream.Get32();
        if (count == -1) return null!;
        if (count is < 0 or > 65536 || count > stream.GetAvailableBytes() / 4) throw new InvalidDataException("Invalid resource array count.");
        var values = new T[count];
        for (var i = 0; i < count; i++)
        {
            var resource = context.Reference(stream.Get32());
            if (resource is not null && resource is not T) throw new InvalidDataException("Resource array element type mismatch.");
            values[i] = (T)resource!;
        }
        return values;
    }
}

internal sealed class ResourceFileRegistry
{
    internal readonly object Gate = new();
    internal readonly Dictionary<string, ResourceFileType> ByID = new(StringComparer.Ordinal);
    internal readonly Dictionary<Type, ResourceFileType> ByType = new();
    internal readonly Dictionary<Type, ResourceValueCodec> Codecs = new();
    internal readonly Dictionary<Type, ResourceArrayCodec> ResourceArrays = new();
}
