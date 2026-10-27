namespace Electron2D;

/// <summary>Loads supported resource files through the engine's typed resource path cache.</summary>
/// <remarks>Supported resources include registered typed archives/format extensions, image textures, dynamic fonts,
/// WAV/MP3/Ogg audio, compiled C# source assets and certificate/private-key files. Static operations use a permanent retained service.
/// The returned resource belongs to the caller and is cached weakly while it remains live. Synchronous load
/// operations serialize cache decisions. Explicit threaded requests prepare independent graphs on workers and
/// publish cache identities/callbacks on their scene owner or consuming standalone caller. File roots retain newly
/// decoded dependency graph leases; the weak cache owns no resources. LocaleResourceRemaps choose backing files
/// while preserving logical cache paths. Culture changes and pseudolocalization refreshes reload associated live
/// resources before scene translation notifications; those configured reloads perform cold file I/O.</remarks>
public sealed partial class ResourceLoader : ElectronObject
{
    /// <summary>Controls how a load uses or refreshes the path cache.</summary>
    /// <remarks>Deep modes equal their ordinary counterparts for dependency-free image and font files.</remarks>
    public enum CacheMode
    {
        /// <summary>Load an independent resource without registering it.</summary>
        Ignore,
        /// <summary>Reuse a live cached resource, or load and register a new one.</summary>
        Reuse,
        /// <summary>Reload into a live cached resource, or load and register a new one.</summary>
        Replace,
        /// <summary>Ignore recursively; equivalent to Ignore for dependency-free images and fonts.</summary>
        IgnoreDeep,
        /// <summary>Refreshes root and external dependencies recursively, preserving compatible cached identities.</summary>
        ReplaceDeep
    }

    internal static readonly ResourceLoader Runtime = new();
    private bool _abortOnMissing = true;
    [ThreadStatic] private static bool? _archiveMissingPolicy;
    internal static bool AbortOnMissingResources => ResourceLoadGraph.Current?.AbortOnMissingResources ?? _archiveMissingPolicy ?? Volatile.Read(ref Runtime._abortOnMissing);
    /// <summary>Selects whether newly loaded archive graphs reject absent resource factories/dependencies.</summary>
    /// <param name="abort">True for strict failure, the default; false enables bounded generic, mesh, ordinary texture and material placeholders.</param>
    /// <remarks>Captured once for each load graph/request. Version 4 preserves missing-payload provenance across saves;
    /// version 8 adds typed material recovery. An available factory cannot replace absent data with an empty concrete resource. Corruption, incompatible
    /// typed references and factory/codec failures always reject.</remarks>
    public static void SetAbortOnMissingResources(bool abort) => Volatile.Write(ref Runtime._abortOnMissing, abort);
    internal readonly ResourceFileRegistry FileTypes = new();
    private readonly object _loadGate = new();
    private readonly List<ResourceFormatLoader> _fileLoaders = [new ResourceArchiveLoader(), new ScriptSourceLoader(), new ShaderIncludeSourceLoader(), new CompressedTextureSourceLoader(), new CompressedArraySourceLoader(), new TranslationCatalogLoader()];
    private ResourceLoader() { }
    private static object LoadGate => Runtime._loadGate;
    private static List<ResourceFormatLoader> FileLoaders => Runtime._fileLoaders;
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The resource-loading service is permanent.");
    private static bool IsBitmapFont(string path) => System.IO.Path.GetExtension(path).Equals(".fnt", StringComparison.OrdinalIgnoreCase) || System.IO.Path.GetExtension(path).Equals(".font", StringComparison.OrdinalIgnoreCase);
    private static readonly string[] FontExtensions = ["ttf", "otf", "woff", "woff2", "ttc", "otc", "fnt", "font"];

    /// <summary>Registers a borrowed typed file loader without duplicating identity.</summary><param name="formatLoader">Live format extension.</param><param name="atFront">Whether it precedes existing extensions.</param>
    public static void AddResourceFormatLoader(ResourceFormatLoader formatLoader, bool atFront = false) { ArgumentNullException.ThrowIfNull(formatLoader); ObjectDisposedException.ThrowIf(formatLoader.IsDisposed, formatLoader); lock (LoadGate) { if (FileLoaders.Any(item => ReferenceEquals(item, formatLoader))) return; if (FileLoaders.Count >= 64) throw new InvalidOperationException("At most 64 format loaders can be registered."); if (atFront) FileLoaders.Insert(0, formatLoader); else FileLoaders.Add(formatLoader); } }
    /// <summary>Removes loader registration without disposing its object.</summary><param name="formatLoader">Extension identity.</param>
    public static void RemoveResourceFormatLoader(ResourceFormatLoader formatLoader) { ArgumentNullException.ThrowIfNull(formatLoader); lock (LoadGate) FileLoaders.RemoveAll(item => ReferenceEquals(item, formatLoader)); }
    private static ResourceFormatLoader[] LoaderSnapshot() { if (ResourceLoadGraph.Current is { } stage) return stage.Formats; lock (LoadGate) return FileLoaders.Where(l => !l.IsDisposed).ToArray(); }
    /// <summary>Returns external dependency tokens from the currently selected resource file.</summary><param name="path">Logical source file, including a configured locale source.</param><param name="addTypes">Whether to append stable type IDs.</param><returns>Copied ordered dependencies; archive relative fallback paths resolve beside the selected physical file.</returns>
    public static string[] GetDependencies(string path, bool addTypes = false) { path = ResourceUID.EnsurePath(path); path = LocaleMap().Resolve(path, TranslationServer.Culture.Name); foreach (var loader in LoaderSnapshot()) if (loader.RecognizePath(path)) return loader.GetDependencies(path, addTypes); return IsBitmapFont(path) ? FontFile.BitmapDependencies(path, addTypes) : []; }
    /// <summary>Reports the UID stored by a recognized file format.</summary><param name="path">Source file.</param><returns>UID or InvalidID.</returns>
    public static long GetResourceUID(string path) { path = ResourceUID.EnsurePath(path); foreach (var loader in LoaderSnapshot()) if (loader.RecognizePath(path)) return loader.GetResourceUID(path); return ResourceUID.InvalidID; }

    /// <summary>Rewrites recognized external dependency paths without instantiating resource objects.</summary><param name="path">Logical archive or plugin file; locale selection chooses the physical file to edit.</param><param name="renames">Old-to-new file paths. Archives accept resolved paths and stored relative tokens, resolve replacements beside the physical archive and retain relative storage.</param>
    public static void RenameDependencies(string path, IReadOnlyDictionary<string, string> renames) { path = ResourceUID.EnsurePath(path); path = LocaleMap().Resolve(path, TranslationServer.Culture.Name); foreach (var loader in LoaderSnapshot()) if (loader.RecognizePath(path)) { loader.RenameDependencies(path, renames); return; } throw new NotSupportedException("No loader recognizes dependency rewrite."); }
    /// <summary>Returns compiled types used by a recognized resource file.</summary><param name="path">Source file.</param><returns>Copied type census; empty when unrecognized.</returns>
    public static Type[] GetClassesUsed(string path) { path = ResourceUID.EnsurePath(path); foreach (var loader in LoaderSnapshot()) if (loader.RecognizePath(path)) return loader.GetClassesUsed(path); return []; }
    /// <summary>Loads a supported typed resource from an operating-system, res:// or user:// path.</summary>
    /// <typeparam name="TResource">Registered archive/plugin resource, ImageTexture, FontFile, implemented AudioStream, VideoStreamTheora, ShaderInclude, X509Certificate, CryptoKey, or an assignable resource base type.</typeparam>
    /// <param name="path">Logical file path; after UID resolution its exact string remains the cache key even when locale selection chooses another file.</param>
    /// <param name="cacheMode">Whether to reuse, ignore or refresh an existing live instance.</param>
    /// <returns>A caller-owned live resource. Reuse and Replace can return the same cached instance.</returns>
    /// <exception cref="ArgumentException">The path or cache mode is invalid.</exception>
    /// <exception cref="NotSupportedException">The resource type or file extension is unsupported.</exception>
    /// <exception cref="InvalidOperationException">Reuse finds a different resource type, or locale/remap configuration changes during mapped graph preparation.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="FormatException">Encoded audio is malformed or outside its verified channel profile.</exception>
    /// <exception cref="InvalidDataException">The encoded resource is malformed or exceeds supported limits.</exception>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Certificate/key input is malformed or does not match the requested key role.</exception>
    public static TResource Load<TResource>(string path, CacheMode cacheMode = CacheMode.Reuse) where TResource : Resource => (TResource)LoadCore(path, cacheMode, typeof(TResource));

    internal static Resource LoadCore(string path, CacheMode cacheMode, Type requestedType)
    {
        if (ResourceLoadGraph.Current is { } stage) return stage.Load(path, cacheMode, requestedType);
        return LoadImmediate(path, cacheMode, requestedType);
    }

    internal static Resource LoadImmediate(string path, CacheMode cacheMode, Type requestedType, bool isolated = false, CacheMode? hookMode = null, string? originalPath = null)
        => LoadImmediateTracked(path, cacheMode, requestedType, out _, isolated, hookMode, originalPath);

    internal static PackedScene LoadSceneTemplate(string path, out IDisposable? retention, CacheMode cacheMode = CacheMode.Reuse)
    {
        if (ResourceLoadGraph.Current is not null) throw new InvalidOperationException("Scene activation cannot be nested in a resource staging graph.");
        lock (LoadGate)
        {
            var scene = (PackedScene)LoadImmediateTracked(path, cacheMode, typeof(PackedScene), out var owned);
            if (Runtime._sceneTemplateOwners.TryGetValue(scene, out var owner)) retention = owner.Retain();
            else if (owned)
            {
                owner = new ResourceFileOwnership([scene]); Runtime._sceneTemplateOwners.Add(scene, owner);
                retention = owner.TransferToLease();
            }
            else retention = null;
            return scene;
        }
    }

    private static Resource LoadImmediateTracked(string path, CacheMode cacheMode, Type requestedType, out bool owned, bool isolated = false, CacheMode? hookMode = null, string? originalPath = null)
    {
        owned = false;
        CheckFilePath(path);
        if ((uint)cacheMode > 4) throw new ArgumentOutOfRangeException(nameof(cacheMode));
        path = ResourceUID.EnsurePath(path);
        if (!isolated && LocaleMap().Contains(path)) return LoadLocalized(path, cacheMode, requestedType, out owned);
        foreach (var loader in LoaderSnapshot()) if (loader.RecognizePath(path, requestedType))
            {
                if (loader is ResourceArchiveLoader) return LoadFileResourceTracked(path, cacheMode, out owned, requestedType);
                lock (isolated ? ResourceLoadGraph.Current!.EntryGate : LoadGate)
                {
                    var cached = isolated ? null : Resource.GetRegisteredPath(path);
                    if (cacheMode == CacheMode.Reuse && cached is not null) return requestedType.IsInstanceOfType(cached) ? cached : throw new InvalidOperationException("Cached resource does not match requested type.");
                    _fileLoads ??= new(StringComparer.Ordinal);
                    var fileKey = ResourceArchive.Absolute(path);
                    if (_fileLoads.Count >= 64 || !_fileLoads.Add(fileKey)) throw new InvalidDataException("File format dependency cycle or depth budget exceeded.");
                    try
                    {
                        var loaded = loader.Load(path, originalPath ?? path, isolated && ResourceLoadGraph.Current!.UseSubThreads, hookMode ?? cacheMode) ?? throw new InvalidDataException("Format loader returned null.");
                        if (isolated && loaded.FilePathRegistered) throw new InvalidDataException("A staged format hook must return independent resource state.");
                        if (ReferenceEquals(loaded, cached)) { if (requestedType.IsInstanceOfType(loaded) && !loaded.IsDisposed) return loaded; throw new InvalidDataException("Format loader returned an invalid cached resource."); }
                        try
                        {
                            if (!requestedType.IsInstanceOfType(loaded) || loaded.IsDisposed) throw new InvalidDataException("Format loader returned an incompatible resource.");
                            if (cacheMode is CacheMode.Replace or CacheMode.ReplaceDeep && cached?.GetType() == loaded.GetType()) { cached.CopyFileState(loaded); loaded.Dispose(); return cached; }
                            if (cacheMode is CacheMode.Ignore or CacheMode.IgnoreDeep) loaded.SetPathCache(path); else loaded.TakeOverPath(path); owned = true; return loaded;
                        }
                        catch { loaded.Dispose(); throw; }
                    }
                    finally { _fileLoads.Remove(fileKey); }
                }
            }
        CheckType(requestedType);
        CheckFilePath(path);
        if (cacheMode is < CacheMode.Ignore or > CacheMode.ReplaceDeep)
            throw new ArgumentOutOfRangeException(nameof(cacheMode));

        lock (isolated ? ResourceLoadGraph.Current!.EntryGate : LoadGate)
        {
            var cached = isolated ? null : Resource.GetRegisteredPath(path);
            if (cacheMode == CacheMode.Reuse && cached is ImageTexture or FontFile or AudioStream or VideoStream or X509Certificate or CryptoKey && requestedType.IsInstanceOfType(cached)) return cached;
            if (cacheMode == CacheMode.Reuse && cached is not null)
                throw new InvalidOperationException("The cached resource has a different type.");

            var fontFile = requestedType.IsAssignableFrom(typeof(FontFile)) &&
                (!requestedType.IsAssignableFrom(typeof(ImageTexture)) || IsExtension(path, FontExtensions));
            Resource loaded;
            var audioType = AudioType(path, requestedType);
            var cryptoType = CryptoType(path, requestedType);
            if (requestedType.IsAssignableFrom(typeof(VideoStreamTheora)) && System.IO.Path.GetExtension(path).Equals(".ogv", StringComparison.OrdinalIgnoreCase))
            {
                var video = new VideoStreamTheora { File = path };
                try { using var validation = new NativeVideoDecoder(video.ReadFile(), 0); }
                catch { video.Dispose(); throw; }
                if (cacheMode is CacheMode.Replace or CacheMode.ReplaceDeep && cached is VideoStreamTheora existing)
                { using (video) existing.ReloadFrom(video); return existing; }
                loaded = video;
            }
            else if (cryptoType is not null)
            {
                Resource created = cryptoType == typeof(CryptoKey) ? new CryptoKey() : new X509Certificate();
                try { if (created is CryptoKey key) key.Load(path); else ((X509Certificate)created).Load(path); } catch { created.Dispose(); throw; }
                if (cacheMode is CacheMode.Replace or CacheMode.ReplaceDeep && cached?.GetType() == cryptoType) { using (created) { if (cached is CryptoKey key) key.ReloadFrom((CryptoKey)created); else ((X509Certificate)cached!).ReloadFrom((X509Certificate)created); } return cached!; }
                loaded = created;
            }
            else if (audioType is not null)
            {
                AudioStream audio = audioType == typeof(AudioStreamWAV) ? AudioStreamWAV.LoadFromFile(path) : audioType == typeof(AudioStreamMP3) ? AudioStreamMP3.LoadFromFile(path) : AudioStreamOggVorbis.LoadFromFile(path);
                if (cacheMode is CacheMode.Replace or CacheMode.ReplaceDeep && cached is AudioStream existing && existing.GetType() == audioType)
                { using (audio) existing.ReloadFrom(audio); return existing; }
                loaded = audio;
            }
            else if (fontFile)
            {
                if (cacheMode is CacheMode.Replace or CacheMode.ReplaceDeep && cached is FontFile font)
                {
                    if (IsBitmapFont(path)) font.LoadBitmapFont(path); else font.LoadDynamicFont(path); return font;
                }
                var createdFont = new FontFile();
                try { if (IsBitmapFont(path)) createdFont.LoadBitmapFont(path); else createdFont.LoadDynamicFont(path); loaded = createdFont; }
                catch { createdFont.Dispose(); throw; }
            }
            else
            {
                if (typeof(CryptoKey).IsAssignableFrom(requestedType) || typeof(X509Certificate).IsAssignableFrom(requestedType)) throw new NotSupportedException("The file extension does not match the requested security resource type.");
                if (typeof(AudioStream).IsAssignableFrom(requestedType)) throw new NotSupportedException("The file extension does not match the requested audio resource type.");
                using var image = Image.LoadFromFile(path);
                if (cacheMode is CacheMode.Replace or CacheMode.ReplaceDeep && cached is ImageTexture texture)
                {
                    texture.SetImage(image); return texture;
                }
                loaded = ImageTexture.CreateFromImage(image);
            }
            try
            {
                if (cacheMode is CacheMode.Ignore or CacheMode.IgnoreDeep)
                    loaded.SetPathCache(path);
                else if (cached is not null)
                    loaded.TakeOverPath(path);
                else
                    loaded.ResourcePath = path;
                owned = true;
                return loaded;
            }
            catch
            {
                loaded.Dispose();
                throw;
            }
        }
    }

    [ThreadStatic] private static HashSet<string>? _fileLoads;
    internal static Resource LoadFileResource(string path, CacheMode cacheMode, Type? expectedType = null)
        => LoadFileResourceTracked(path, cacheMode, out _, expectedType);

    private static Resource LoadFileResourceTracked(string path, CacheMode cacheMode, out bool owned, Type? expectedType = null)
    {
        owned = false;
        ArgumentException.ThrowIfNullOrEmpty(path); if ((uint)cacheMode > 4) throw new ArgumentOutOfRangeException(nameof(cacheMode)); path = ResourceUID.EnsurePath(path);
        if (ResourceLoadGraph.Current is { } stage) return stage.Load(path, cacheMode, expectedType ?? typeof(Resource));
        if (!ResourceArchive.IsPath(path)) return LoadCore(path, cacheMode, expectedType ?? typeof(Resource));
        lock (LoadGate)
        {
            var cached = Resource.GetRegisteredPath(path); if (cacheMode == CacheMode.Reuse && cached is not null) { if (expectedType is not null && !expectedType.IsInstanceOfType(cached)) throw new InvalidOperationException("Cached resource does not match requested type."); return cached; }
            _fileLoads ??= new(StringComparer.Ordinal); var fileKey = ResourceArchive.Absolute(path); if (_fileLoads.Count >= 64 || !_fileLoads.Add(fileKey)) throw new InvalidDataException("External file dependency cycle requires bundled resources.");
            var previousPolicy = _archiveMissingPolicy; _archiveMissingPolicy ??= Volatile.Read(ref Runtime._abortOnMissing);
            try
            {
                using var context = ResourceArchive.Decode(path, cacheMode); var parsed = context.Root; if (expectedType is not null && !expectedType.IsInstanceOfType(parsed)) throw new InvalidDataException("File resource does not match requested type.");
                if (cacheMode is CacheMode.Replace or CacheMode.ReplaceDeep && cached?.GetType() == parsed.GetType())
                {
                    var changedSources = context.RedirectRoot(cached); using var source = context.ReleaseRoot();
                    foreach (var changed in changedSources)
                    {
                        changed.NotifyConstructionScriptChanged();
                        ObjectDisposedException.ThrowIf(changed.IsDisposed, changed);
                    }
                    cached.CopyFileState(source);
                    ResourceUID.SetID(ResourceArchive.ReadUID(path), path); return cached;
                }
                var loaded = context.ReleaseRoot(); try { if (cacheMode is CacheMode.Ignore or CacheMode.IgnoreDeep) loaded.SetPathCache(path); else if (cached is not null) loaded.TakeOverPath(path); else loaded.ResourcePath = path; ResourceUID.SetID(ResourceArchive.ReadUID(path), path); owned = true; return loaded; } catch { loaded.Dispose(); throw; }
            }
            finally { _archiveMissingPolicy = previousPolicy; _fileLoads.Remove(fileKey); }
        }
    }

    /// <summary>Reports whether a supported resource file exists or is already cached.</summary>
    /// <typeparam name="TResource">Requested resource type or compatible base type.</typeparam>
    /// <param name="path">Exact cache path or file path.</param>
    /// <returns>True when a live cached instance has this type, or a recognized resource file exists for it.</returns>
    /// <remarks>The cache is checked first, so a cached resource may outlive removal of its source file.</remarks>
    /// <exception cref="ArgumentException">The path is null or empty.</exception>
    public static bool Exists<TResource>(string path) where TResource : Resource
    {
        CheckFilePath(path);
        path = ResourceUID.EnsurePath(path);
        if (Resource.GetRegisteredPath(path) is TResource) return true;
        path = LocaleMap().Resolve(path, TranslationServer.Culture.Name);
        foreach (var loader in LoaderSnapshot()) if (loader.RecognizePath(path, typeof(TResource)) && loader.Exists(path)) { var type = loader.GetResourceType(path); return type is not null && typeof(TResource).IsAssignableFrom(type); }
        return Resource.GetRegisteredPath(path) is TResource ||

            (typeof(TResource).IsAssignableFrom(typeof(ImageTexture)) && Image.RecognizesFile(path) ||
             typeof(TResource).IsAssignableFrom(typeof(FontFile)) && IsExtension(path, FontExtensions) || AudioType<TResource>(path) is not null || CryptoType<TResource>(path) is not null ||
             typeof(TResource).IsAssignableFrom(typeof(VideoStreamTheora)) && System.IO.Path.GetExtension(path).Equals(".ogv", StringComparison.OrdinalIgnoreCase)) && FileAccess.FileExists(path);
    }

    /// <summary>Reports whether any live registered resource occupies an exact cache path.</summary>
    /// <param name="path">Ordinal, case-sensitive cache path.</param>
    /// <returns>True for a live registered resource, including a resource registered outside this loader.</returns>
    /// <exception cref="ArgumentException">The path is null or empty.</exception>
    public static bool HasCached(string path)
    {
        CheckPath(path);
        path = ResourceUID.EnsurePath(path);
        if (ResourceLoadGraph.Current?.IsCreatingPath(path) == true) return false;
        return Resource.GetRegisteredPath(path) is not null;
    }

    /// <summary>Gets a live cached resource of the requested type, or null when absent.</summary>
    /// <typeparam name="TResource">Resource or one of its concrete derived types.</typeparam>
    /// <param name="path">Ordinal, case-sensitive cache path.</param>
    /// <returns>A borrowed live instance, or null when the path is absent or holds another type.</returns>
    /// <remarks>Retrieval does not extend ownership; the resource's owner controls disposal.</remarks>
    /// <exception cref="ArgumentException">The path is null or empty.</exception>
    public static TResource? GetCachedRef<TResource>(string path) where TResource : Resource
    {
        CheckPath(path);
        path = ResourceUID.EnsurePath(path);
        if (ResourceLoadGraph.Current?.IsCreatingPath(path) == true) return null;
        return Resource.GetRegisteredPath(path) as TResource;
    }

    /// <summary>Gets caller-owned lowercase filename extensions supported for a resource type.</summary>
    /// <typeparam name="TResource">Requested resource type or compatible base type.</typeparam>
    /// <returns>The supported extensions without dots, or an empty array for unsupported types.</returns>
    public static string[] GetRecognizedExtensionsForType<TResource>() where TResource : Resource
    {
        var archive = new List<string>(ResourceFileTypes.Extensions(typeof(TResource)));
        foreach (var loader in LoaderSnapshot()) if (loader is not ResourceArchiveLoader && loader.HandlesType(typeof(TResource))) archive.AddRange(loader.GetRecognizedExtensions());
        var images = typeof(TResource).IsAssignableFrom(typeof(ImageTexture));
        var fonts = typeof(TResource).IsAssignableFrom(typeof(FontFile));
        var audio = new List<string>();
        if (typeof(TResource).IsAssignableFrom(typeof(AudioStreamWAV))) audio.Add("wav");
        if (typeof(TResource).IsAssignableFrom(typeof(AudioStreamMP3))) audio.Add("mp3");
        if (typeof(TResource).IsAssignableFrom(typeof(AudioStreamOggVorbis))) audio.Add("ogg");
        if (typeof(TResource).IsAssignableFrom(typeof(VideoStreamTheora))) audio.Add("ogv");
        if (typeof(TResource).IsAssignableFrom(typeof(CryptoKey))) audio.Add("key");
        if (typeof(TResource).IsAssignableFrom(typeof(X509Certificate))) audio.Add("crt");
        if (images || fonts) return [.. images ? Image.RecognizedFileExtensions() : [], .. fonts ? FontExtensions : [], .. audio, .. archive];
        return audio.Concat(archive).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    }

    private static Type? CryptoType<TResource>(string path) where TResource : Resource => CryptoType(path, typeof(TResource));
    private static Type? CryptoType(string path, Type requestedType)
    {
        var type = System.IO.Path.GetExtension(path).ToLowerInvariant() switch { ".key" => typeof(CryptoKey), ".crt" => typeof(X509Certificate), _ => null };
        return type is not null && requestedType.IsAssignableFrom(type) ? type : null;
    }
    private static Type? AudioType<TResource>(string path) where TResource : Resource => AudioType(path, typeof(TResource));
    private static Type? AudioType(string path, Type requested)
    {
        var extension = System.IO.Path.GetExtension(path);
        var type = extension.ToLowerInvariant() switch { ".wav" => typeof(AudioStreamWAV), ".mp3" => typeof(AudioStreamMP3), ".ogg" => typeof(AudioStreamOggVorbis), _ => null };
        return type is not null && requested.IsAssignableFrom(type) ? type : null;
    }
    private static void CheckType(Type requestedType)
    {
        if (!requestedType.IsAssignableFrom(typeof(ImageTexture)) && !requestedType.IsAssignableFrom(typeof(FontFile)) && !requestedType.IsAssignableFrom(typeof(AudioStreamWAV)) && !requestedType.IsAssignableFrom(typeof(AudioStreamMP3)) && !requestedType.IsAssignableFrom(typeof(AudioStreamOggVorbis)) && !requestedType.IsAssignableFrom(typeof(VideoStreamTheora)) && !requestedType.IsAssignableFrom(typeof(CryptoKey)) && !requestedType.IsAssignableFrom(typeof(X509Certificate)))
            throw new NotSupportedException($"Resource files for {requestedType.Name} are not integrated.");
    }

    private static void CheckPath(string path) => ArgumentException.ThrowIfNullOrEmpty(path);

    private static void CheckFilePath(string path)
    {
        CheckPath(path);
        ProjectSettings.GlobalizePath(path);
    }

    private static bool IsExtension(string path, string[] extensions)
    {
        var extension = System.IO.Path.GetExtension(path).TrimStart('.');
        return extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
