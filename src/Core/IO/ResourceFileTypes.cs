namespace Electron2D;

/// <summary>Registers stable compiled factories and typed portable value codecs for resource/scene files.</summary>
/// <remarks>Registration is allocating setup. IDs are ordinal and immutable; file data never loads assemblies,
/// invokes reflected members or constructs arbitrary CLR types. Register the same schemas before saving and in
/// every loading process. Factories are direct static delegates and must return fresh exact-type instances.
/// Threaded loading may invoke factories/codecs concurrently; their preparation must be thread-safe.</remarks>
public static partial class ResourceFileTypes
{
    private static object Gate => ResourceLoader.Runtime.FileTypes.Gate;
    private static Dictionary<string, ResourceFileType> ByID => ResourceLoader.Runtime.FileTypes.ByID;
    private static Dictionary<Type, ResourceFileType> ByType => ResourceLoader.Runtime.FileTypes.ByType;
    private static Dictionary<Type, ResourceValueCodec> Codecs => ResourceLoader.Runtime.FileTypes.Codecs;
    private static Dictionary<Type, ResourceArrayCodec> ResourceArrays => ResourceLoader.Runtime.FileTypes.ResourceArrays;
    private static InstancePlaceholder CreateInstancePlaceholder() => new();
    static ResourceFileTypes()
    {
        RegisterNode("Node", CreateNode);
        RegisterNode("InstancePlaceholder", CreateInstancePlaceholder);
        RegisterNode("Entity", CreateEntity);
        RegisterNode("CanvasModulate", CreateCanvasModulate);
        RegisterNode("ShaderGlobalsOverride", CreateShaderGlobalsOverride);
        RegisterNode("FoldableContainer", CreateFoldableContainer);
        RegisterNode("TouchScreenButton", CreateTouchScreenButton);
        RegisterNode("VideoStreamPlayer", CreateVideoStreamPlayer);
        RegisterResource("VideoStreamTheora", CreateVideoStreamTheora);
        RegisterNode("VirtualJoystick", CreateVirtualJoystick);
        RegisterResource("FoldableGroup", CreateFoldableGroup);
        RegisterNode("PointLight", CreatePointLight);
        RegisterNode("DirectionalLight", CreateDirectionalLight);
        RegisterNode("LightOccluder", CreateLightOccluder);
        RegisterResource("CanvasTexture", CreateCanvasTexture);
        RegisterResource("OccluderPolygon", CreateOccluderPolygon);
        RegisterNode("CPUParticles", CreateCPUParticles);
        RegisterNode("GPUParticles", CreateGPUParticles);
        RegisterNode("Skeleton", CreateSkeleton);
        RegisterNode("Bone", CreateBone);
        RegisterNode("PhysicalBone", CreatePhysicalBone);
        RegisterResource("SkeletonModificationStack", CreateSkeletonStack);
        RegisterResource("SkeletonModificationLookAt", CreateSkeletonLookAt);
        RegisterResource("SkeletonModificationTwoBoneIK", CreateSkeletonTwoBoneIK);
        RegisterResource("SkeletonModificationCCDIK", CreateSkeletonCCDIK);
        RegisterResource("SkeletonModificationFABRIK", CreateSkeletonFABRIK);
        RegisterResource("SkeletonModificationJiggle", CreateSkeletonJiggle);
        RegisterResource("SkeletonModificationStackHolder", CreateSkeletonStackHolder);
        RegisterResource("SkeletonModificationPhysicalBones", CreateSkeletonPhysicalBones);
        RegisterResourceArray<SkeletonModification>();
        RegisterResource("MissingResource", CreateMissingResource);
        RegisterResource("PlaceholderMesh", CreatePlaceholderMesh);
        RegisterResourceArray<Resource>();
        RegisterResource("ArrayMesh", CreateArrayMesh);
        RegisterResource("ImporterMesh", CreateImporterMesh);
        RegisterResource("ImmediateMesh", CreateImmediateMesh);
        RegisterResource("NavigationPolygon", CreateNavigationPolygon);
        RegisterResource("NavigationMeshSourceGeometryData", CreateNavigationSourceGeometry);
        RegisterResource("PolygonPathFinder", CreatePolygonPathFinder);
        RegisterNode("NavigationRegion", CreateNavigationRegion);
        RegisterNode("NavigationLink", CreateNavigationLink);
        RegisterNode("NavigationAgent", CreateNavigationAgent);
        RegisterNode("NavigationObstacle", CreateNavigationObstacle);
        RegisterResourceArray<Material>();
        RegisterResource("Resource", CreateResource);
        RegisterResource("PackedScene", CreatePackedScene);
        RegisterResource("FontVariation", CreateFontVariation);
        RegisterResource("DPITexture", CreateDPITexture);
        RegisterResource("Script", CreateScript);
        RegisterResource("PhysicsMaterial", CreatePhysicsMaterial);
        RegisterResource("TileSet", CreateTileSet);
        RegisterResource("TileMapPattern", CreateTileMapPattern);
        RegisterResource("TileSetAtlasSource", CreateTileSetAtlasSource);
        RegisterResource("TileSetScenesCollectionSource", CreateTileSetScenesCollectionSource);
        RegisterResourceArray<TileSetSource>();
        RegisterResource("RDShaderSPIRV", CreateRDShaderSPIRV);
        RegisterResource("RDShaderFile", CreateRDShaderFile);
        RegisterResource("ColorPalette", CreateColorPalette);
        RegisterResource("SyntaxHighlighter", CreateSyntaxHighlighter);
        RegisterResource("CodeHighlighter", CreateCodeHighlighter);
        RegisterResource("RichTextEffect", CreateRichTextEffect);
        RegisterNode("Sprite", CreateSprite);
        RegisterNode("TileMapLayer", CreateTileMapLayer);
        RegisterResource("BitMap", CreateBitMap);
        RegisterResource("Image", CreateImage);
        RegisterResource("ImageTexture", CreateImageTexture);
        RegisterResource("PlaceholderTexture", CreatePlaceholderTexture);
        RegisterResource("CompressedTexture", CreateCompressedTexture);
        RegisterResourceArray<Texture>();
        RegisterResourceArray<TextureLayered>();
        RegisterResource("TextureArray", CreateTextureArray);
        RegisterResource("CompressedTextureArray", CreateCompressedTextureArray);
        RegisterResource("TextureRD", CreateTextureRD);
        RegisterResource("DrawableTexture", CreateDrawableTexture);
        RegisterResource("BlitMaterial", CreateBlitMaterial);
        RegisterResource("TextureArrayRD", CreateTextureArrayRD);
        RegisterResource("PlaceholderTextureArray", CreatePlaceholderTextureArray);
        RegisterResource("AtlasTexture", CreateAtlasTexture);
        RegisterResource("MeshTexture", CreateMeshTexture);
        RegisterResource("PortableCompressedTexture", CreatePortableCompressedTexture);

        RegisterAnimationGraphFiles();
        RegisterBuiltInNodes();
        RegisterAudioFileResources();
        RegisterResource("Theme", CreateThemeFileResource);
        RegisterResource("StyleBoxFlat", CreateFlatFileResource);
        RegisterResource("StyleBoxLine", CreateLineFileResource);
        RegisterResource("StyleBoxTexture", CreateTextureStyleFileResource);
        RegisterResource("StyleBoxEmpty", CreateEmptyFileResource);
        RegisterResource("FontFile", CreateFontFileResource);
        RegisterResource("SystemFont", CreateSystemFontResource);
        RegisterResource("Curve", CreateCurveResource);
        RegisterResource("CurveTexture", CreateCurveTextureResource);
        RegisterResource("CurveXYZTexture", CreateCurveXYZTextureResource);
        RegisterResource("AudioStreamWAV", CreateAudioStreamWAVResource);
        RegisterResource("Animation", CreateAnimationResource);
        RegisterResource("AnimationLibrary", CreateAnimationLibraryResource);
        RegisterResource("Gradient", CreateGradientResource);
        RegisterResource("GradientTexture", CreateGradientTextureResource);
        RegisterResource("GradientRampTexture", CreateGradientRampTextureResource);
        RegisterResource("FastNoiseLite", CreateFastNoiseResource);
        RegisterResource("NoiseTexture", CreateNoiseTextureResource);
        RegisterResource("CanvasItemMaterial", CreateCanvasItemMaterialResource);
        RegisterResource("ParticleProcessMaterial", CreateParticleProcessMaterialResource);
        RegisterResource("Shader", CreateShaderFileResource);
        RegisterResource("ShaderInclude", CreateShaderIncludeFileResource);
        RegisterResource("Translation", CreateTranslationFileResource);
        RegisterResource("ShaderMaterial", CreateShaderMaterialFileResource);
        RegisterResource("CircleShape", CreateCircleFileResource);
        RegisterResource("CapsuleShape", CreateCapsuleFileResource);
        RegisterResource("SegmentShape", CreateSegmentFileResource);
        RegisterResource("RectangleShape", CreateRectangleFileResource);
        RegisterResource("ConvexPolygonShape", CreateConvexPolygonFileResource);
        RegisterResource("ConcavePolygonShape", CreateConcavePolygonFileResource);
        RegisterResource("SeparationRayShape", CreateSeparationRayFileResource);
        RegisterResource("WorldBoundaryShape", CreateWorldBoundaryFileResource);
        ResourceFileCodecs.Prepare();
        RegisterResourceArray<Font>();
    }
    private static Theme CreateThemeFileResource() => new();
    private static AudioStreamWAV CreateAudioStreamWAVResource() => new();
    private static VideoStreamPlayer CreateVideoStreamPlayer() => new();
    private static VideoStreamTheora CreateVideoStreamTheora() => new();
    private static Animation CreateAnimationResource() => new();
    private static AnimationLibrary CreateAnimationLibraryResource() => new();
    private static Shader CreateShaderFileResource() => new();
    private static ShaderInclude CreateShaderIncludeFileResource() => new();
    private static Translation CreateTranslationFileResource() => new();
    private static ShaderMaterial CreateShaderMaterialFileResource() => new();
    private static Node CreateNode() => new();
    private static Entity CreateEntity() => new();
    private static PointLight CreatePointLight() => new();
    private static DirectionalLight CreateDirectionalLight() => new();
    private static LightOccluder CreateLightOccluder() => new();
    private static ShaderGlobalsOverride CreateShaderGlobalsOverride() => new();
    private static CanvasModulate CreateCanvasModulate() => new();
    private static FoldableContainer CreateFoldableContainer() => new();
    private static TouchScreenButton CreateTouchScreenButton() => new();
    private static VirtualJoystick CreateVirtualJoystick() => new();
    private static FoldableGroup CreateFoldableGroup() => new();
    private static CanvasTexture CreateCanvasTexture() => new();
    private static OccluderPolygon CreateOccluderPolygon() => new();
    private static CPUParticles CreateCPUParticles() => new();
    private static GPUParticles CreateGPUParticles() => new();
    private static Skeleton CreateSkeleton() => new();
    private static Bone CreateBone() => new();
    private static PhysicalBone CreatePhysicalBone() => new();
    private static SkeletonModificationStack CreateSkeletonStack() => new();
    private static SkeletonModificationLookAt CreateSkeletonLookAt() => new();
    private static SkeletonModificationTwoBoneIK CreateSkeletonTwoBoneIK() => new();
    private static SkeletonModificationCCDIK CreateSkeletonCCDIK() => new();
    private static SkeletonModificationFABRIK CreateSkeletonFABRIK() => new();
    private static SkeletonModificationJiggle CreateSkeletonJiggle() => new();
    private static SkeletonModificationStackHolder CreateSkeletonStackHolder() => new();
    private static SkeletonModificationPhysicalBones CreateSkeletonPhysicalBones() => new();
    private static SyntaxHighlighter CreateSyntaxHighlighter() => new();
    private static CodeHighlighter CreateCodeHighlighter() => new();
    private static RichTextEffect CreateRichTextEffect() => new();
    private static NavigationPolygon CreateNavigationPolygon() => new();
    private static NavigationMeshSourceGeometryData CreateNavigationSourceGeometry() => new();
    private static PolygonPathFinder CreatePolygonPathFinder() => new();
    private static NavigationRegion CreateNavigationRegion() => new();
    private static NavigationLink CreateNavigationLink() => new();
    private static NavigationAgent CreateNavigationAgent() => new();
    private static NavigationObstacle CreateNavigationObstacle() => new();
    private static MissingResource CreateMissingResource() => new();
    private static PlaceholderMesh CreatePlaceholderMesh() => new();
    private static ArrayMesh CreateArrayMesh() => new();
    private static ImporterMesh CreateImporterMesh() => new();
    private static ImmediateMesh CreateImmediateMesh() => new();
    private static Resource CreateResource() => new();
    private static Curve CreateCurveResource() => new();
    private static CurveTexture CreateCurveTextureResource() => new();
    private static CurveXYZTexture CreateCurveXYZTextureResource() => new();
    private static Gradient CreateGradientResource() => new();
    private static GradientTexture CreateGradientTextureResource() => new();
    private static GradientRampTexture CreateGradientRampTextureResource() => new();
    private static FastNoiseLite CreateFastNoiseResource() => new();
    private static NoiseTexture CreateNoiseTextureResource() => new();
    private static CanvasItemMaterial CreateCanvasItemMaterialResource() => new();
    private static ParticleProcessMaterial CreateParticleProcessMaterialResource() => new();
    private static PackedScene CreatePackedScene() => new();
    private static FontVariation CreateFontVariation() => new();
    private static DPITexture CreateDPITexture() => new();
    private static Script CreateScript() => new();
    private static RDShaderSPIRV CreateRDShaderSPIRV() => new();
    private static RDShaderFile CreateRDShaderFile() => new();
    private static TileSet CreateTileSet() => new();
    private static TileMapPattern CreateTileMapPattern() => new();
    private static TileSetAtlasSource CreateTileSetAtlasSource() => new();
    private static TileSetScenesCollectionSource CreateTileSetScenesCollectionSource() => new();
    private static PhysicsMaterial CreatePhysicsMaterial() => new();
    private static ColorPalette CreateColorPalette() => new();
    private static TileMapLayer CreateTileMapLayer() => new();
    private static Sprite CreateSprite() => new();
    private static Image CreateImage() => new();
    private static BitMap CreateBitMap() => new();
    private static ImageTexture CreateImageTexture() => new();
    private static PlaceholderTexture CreatePlaceholderTexture() => new();
    private static CompressedTexture CreateCompressedTexture() => new();
    private static TextureArray CreateTextureArray() => new();
    private static CompressedTextureArray CreateCompressedTextureArray() => new();
    private static DrawableTexture CreateDrawableTexture() => new();
    private static BlitMaterial CreateBlitMaterial() => new();
    private static TextureRD CreateTextureRD() => new();
    private static TextureArrayRD CreateTextureArrayRD() => new();
    private static PlaceholderTextureArray CreatePlaceholderTextureArray() => new();
    private static AtlasTexture CreateAtlasTexture() => new();
    private static PortableCompressedTexture CreatePortableCompressedTexture() => new();
    private static MeshTexture CreateMeshTexture() => new();
    private static CircleShape CreateCircleFileResource() => new();
    private static ConvexPolygonShape CreateConvexPolygonFileResource() => new();
    private static ConcavePolygonShape CreateConcavePolygonFileResource() => new();
    private static SeparationRayShape CreateSeparationRayFileResource() => new();
    private static WorldBoundaryShape CreateWorldBoundaryFileResource() => new();
    private static CapsuleShape CreateCapsuleFileResource() => new();
    private static SegmentShape CreateSegmentFileResource() => new();
    private static RectangleShape CreateRectangleFileResource() => new();
    private static StyleBoxFlat CreateFlatFileResource() => new();
    private static StyleBoxLine CreateLineFileResource() => new();
    private static StyleBoxTexture CreateTextureStyleFileResource() => new();
    private static StyleBoxEmpty CreateEmptyFileResource() => new();
    private static FontFile CreateFontFileResource() => new();
    private static SystemFont CreateSystemFontResource() => new();
    /// <summary>Registers a compiled node factory used by file-backed PackedScene.</summary><typeparam name="TNode">Exact concrete node type.</typeparam><param name="id">Stable portable schema ID.</param><param name="factory">Static exact-type constructor.</param>
    /// <remarks>Each factory invocation must construct its returned object. Pre-existing results reject before acquisition,
    /// default-state checks or decoding; rejected borrowed objects are never disposed by the loader.</remarks>
    public static void RegisterNode<TNode>(string id, Func<TNode> factory) where TNode : Node => Register(id, factory);
    /// <summary>Registers a compiled resource factory with storage-enabled typed properties.</summary><typeparam name="TResource">Exact concrete resource type.</typeparam><param name="id">Stable portable schema ID.</param><param name="factory">Static exact-type constructor.</param>
    /// <remarks>Each factory invocation must construct its returned object. Pre-existing results reject before acquisition,
    /// default-state checks or decoding; rejected borrowed objects are never disposed by the loader.</remarks>
    public static void RegisterResource<TResource>(string id, Func<TResource> factory) where TResource : Resource
    {
        Register(id, factory);
        RegisterResourceArray<TResource>();
        TileCustomDataType.Register<TResource>();
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
            TileCustomDataType.Register<TValue>();
        }
    }
    private static void RegisterResourceArray<T>() where T : Resource
    {
        lock (Gate) ResourceArrays.TryAdd(typeof(T[]), new ResourceArrayCodec<T>());
        TileCustomDataType.Register<T[]>();
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
    internal static ResourceFileType? Find(string id) { lock (Gate) return ByID.GetValueOrDefault(id); }
    internal static ResourceFileType Get(string id)
    {
        lock (Gate) return ByID.GetValueOrDefault(id) ?? throw new InvalidDataException($"Unknown file type ID '{id}'.");
    }
    internal static string ValueID(Type type) => type.IsArray ? ValueID(type.GetElementType()!) + "[]" : type.IsConstructedGenericType ? type.GetGenericTypeDefinition().FullName!.Split('`')[0] + "<" + string.Join(",", type.GetGenericArguments().Select(ValueID)) + ">" : type.FullName!;
    internal static Type? FindValueType(string id)
    {
        lock (Gate) return Codecs.Keys.Concat(ResourceArrays.Keys).Concat(ByType.Keys.SelectMany(Ancestors)).FirstOrDefault(t => ValueID(t) == id);
    }
    private static IEnumerable<Type> Ancestors(Type type)
    {
        for (Type? current = type; current is not null; current = current.BaseType) yield return current;
    }
    internal static ResourceValueCodec? FindCodec(Type type) { lock (Gate) return Codecs.GetValueOrDefault(type); }
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
        var value = ElectronObject.InvokeConstructionFactory(create, out var created) ?? throw new InvalidOperationException("File factory returned null.");
        if (!created) throw new InvalidOperationException("File factory returned a pre-existing instance.");
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
internal abstract class ResourceValueCodec
{
    internal abstract void ValidateEncoded(StreamPeerBuffer stream);
    internal abstract StoredPropertyValue ReadStoredValue(StreamPeerBuffer stream);
    internal abstract PropertyDescriptor CreateDeferredDescriptor(string name);
}
internal sealed class ResourceValueCodec<T>(Action<StreamPeerBuffer, T> write, Func<StreamPeerBuffer, T> read) : ResourceValueCodec
{
    internal void Write(StreamPeerBuffer stream, T value) => write(stream, value);
    internal T Read(StreamPeerBuffer stream) => read(stream);
    internal override StoredPropertyValue ReadStoredValue(StreamPeerBuffer stream) => new StoredPropertyValue<T>(read(stream));
    internal override PropertyDescriptor CreateDeferredDescriptor(string name) => new PropertyDescriptor<InstancePlaceholder, T>(name,
        node => node.GetStoredValue<T>(name), (node, value) => node.SetPendingField(name, MissingResourceData.Capture(name, value)), stored: true);
    internal override void ValidateEncoded(StreamPeerBuffer stream) { _ = read(stream); if (stream.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing scalar data."); }
}

internal abstract class ResourceArrayCodec
{
    internal abstract Resource?[] Read(StreamPeerBuffer stream, ResourceArchiveRead context);
    internal abstract Resource?[] ReadReferences(StreamPeerBuffer stream, Resource?[] references);
}
internal sealed class ResourceArrayCodec<T> : ResourceArrayCodec where T : Resource
{
    internal override Resource?[] Read(StreamPeerBuffer stream, ResourceArchiveRead context) => ReadCore(stream, context.Reference);
    internal override Resource?[] ReadReferences(StreamPeerBuffer stream, Resource?[] references) => ReadCore(stream, id => id >= 0 && id < references.Length ? references[id] : throw new InvalidDataException("Invalid recorded reference."));
    private static Resource?[] ReadCore(StreamPeerBuffer stream, Func<int, Resource?> reference)
    {
        var count = stream.Get32();
        if (count == -1) return null!;
        if (count is < 0 or > 65536 || count > stream.GetAvailableBytes() / 4) throw new InvalidDataException("Invalid resource array count.");
        var values = new T[count];
        for (var i = 0; i < count; i++)
        {
            var resource = reference(stream.Get32());
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
    internal readonly Dictionary<string, AnimationFileParameterBinding> AnimationParameters = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, Func<AnimationTree, bool>> AnimationPredicates = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, SceneConnectionSchema> SceneConnections = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, AnimationFileMethodBinding> AnimationMethods = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, AnimationFileBinding> AnimationBindings = new(StringComparer.Ordinal);
    internal readonly Dictionary<Type, TileCustomDataType> TileDataTypes = new();
}
