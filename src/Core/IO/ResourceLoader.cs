namespace Electron2D;

/// <summary>Loads supported resource files through the engine's typed resource path cache.</summary>
/// <remarks>Supported resources are image textures, dynamic font files, WAV/MP3/Ogg audio and certificate/private-key files.
/// The returned resource belongs to the caller and is cached weakly while it remains live. Synchronous load
/// operations serialize cache decisions; this service does not own caller resources or their renderer payloads.</remarks>
public static class ResourceLoader
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
        /// <summary>Replace recursively; equivalent to Replace for dependency-free images and fonts.</summary>
        ReplaceDeep
    }

    private static readonly object LoadGate = new();
    private static readonly string[] FontExtensions = ["ttf", "otf", "woff", "woff2", "ttc", "otc"];
    private static readonly string[] ImageExtensions = ["png", "jpg", "jpeg", "webp", "bmp", "tga", "svg"];

    /// <summary>Loads a supported typed resource from an operating-system, res:// or user:// path.</summary>
    /// <typeparam name="TResource"><see cref="ImageTexture"/>, <see cref="FontFile"/>, an implemented AudioStream type, X509Certificate, CryptoKey, or an assignable resource base type.</typeparam>
    /// <param name="path">File path; the exact path string is the cache key.</param>
    /// <param name="cacheMode">Whether to reuse, ignore or refresh an existing live instance.</param>
    /// <returns>A caller-owned live resource. Reuse and Replace can return the same cached instance.</returns>
    /// <exception cref="ArgumentException">The path or cache mode is invalid.</exception>
    /// <exception cref="NotSupportedException">The resource type or file extension is unsupported.</exception>
    /// <exception cref="InvalidOperationException">Reuse finds a different resource type at the path.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="FormatException">Encoded audio is malformed or outside its verified channel profile.</exception>
    /// <exception cref="InvalidDataException">The encoded resource is malformed or exceeds supported limits.</exception>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Certificate/key input is malformed or does not match the requested key role.</exception>
    public static TResource Load<TResource>(string path, CacheMode cacheMode = CacheMode.Reuse)
        where TResource : Resource
    {
        CheckType<TResource>();
        CheckFilePath(path);
        if (cacheMode is < CacheMode.Ignore or > CacheMode.ReplaceDeep)
            throw new ArgumentOutOfRangeException(nameof(cacheMode));

        lock (LoadGate)
        {
            var cached = Resource.GetRegisteredPath(path);
            if (cacheMode == CacheMode.Reuse && cached is ImageTexture or FontFile or AudioStream or X509Certificate or CryptoKey && cached is TResource reused) return reused;
            if (cacheMode == CacheMode.Reuse && cached is not null)
                throw new InvalidOperationException("The cached resource has a different type.");

            var fontFile = typeof(TResource).IsAssignableFrom(typeof(FontFile)) &&
                (!typeof(TResource).IsAssignableFrom(typeof(ImageTexture)) || IsExtension(path, FontExtensions));
            Resource loaded;
            var audioType = AudioType<TResource>(path);
            var cryptoType = CryptoType<TResource>(path);
            if (cryptoType is not null)
            {
                Resource created = cryptoType == typeof(CryptoKey) ? new CryptoKey() : new X509Certificate();
                try { if (created is CryptoKey key) key.Load(path); else ((X509Certificate)created).Load(path); } catch { created.Dispose(); throw; }
                if (cacheMode is CacheMode.Replace or CacheMode.ReplaceDeep && cached?.GetType() == cryptoType) { using (created) { if (cached is CryptoKey key) key.ReloadFrom((CryptoKey)created); else ((X509Certificate)cached!).ReloadFrom((X509Certificate)created); } return (TResource)cached!; }
                loaded = created;
            }
            else if (audioType is not null)
            {
                AudioStream audio = audioType == typeof(AudioStreamWAV) ? AudioStreamWAV.LoadFromFile(path) : audioType == typeof(AudioStreamMP3) ? AudioStreamMP3.LoadFromFile(path) : AudioStreamOggVorbis.LoadFromFile(path);
                if (cacheMode is CacheMode.Replace or CacheMode.ReplaceDeep && cached is AudioStream existing && existing.GetType() == audioType)
                { using (audio) existing.ReloadFrom(audio); return (TResource)(Resource)existing; }
                loaded = audio;
            }
            else if (fontFile)
            {
                if (cacheMode is CacheMode.Replace or CacheMode.ReplaceDeep && cached is FontFile font)
                {
                    font.LoadDynamicFont(path); return (TResource)(Resource)font;
                }
                var createdFont = new FontFile();
                try { createdFont.LoadDynamicFont(path); loaded = createdFont; }
                catch { createdFont.Dispose(); throw; }
            }
            else
            {
                if (typeof(CryptoKey).IsAssignableFrom(typeof(TResource)) || typeof(X509Certificate).IsAssignableFrom(typeof(TResource))) throw new NotSupportedException("The file extension does not match the requested security resource type.");
                if (typeof(AudioStream).IsAssignableFrom(typeof(TResource))) throw new NotSupportedException("The file extension does not match the requested audio resource type.");
                using var image = Image.LoadFromFile(path);
                if (cacheMode is CacheMode.Replace or CacheMode.ReplaceDeep && cached is ImageTexture texture)
                {
                    texture.SetImage(image); return (TResource)(Resource)texture;
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
                return (TResource)(Resource)loaded;
            }
            catch
            {
                loaded.Dispose();
                throw;
            }
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
        return Resource.GetRegisteredPath(path) is TResource ||
            (typeof(TResource).IsAssignableFrom(typeof(ImageTexture)) && IsExtension(path, ImageExtensions) ||
             typeof(TResource).IsAssignableFrom(typeof(FontFile)) && IsExtension(path, FontExtensions) || AudioType<TResource>(path) is not null || CryptoType<TResource>(path) is not null) && FileAccess.FileExists(path);
    }

    /// <summary>Reports whether any live registered resource occupies an exact cache path.</summary>
    /// <param name="path">Ordinal, case-sensitive cache path.</param>
    /// <returns>True for a live registered resource, including a resource registered outside this loader.</returns>
    /// <exception cref="ArgumentException">The path is null or empty.</exception>
    public static bool HasCached(string path)
    {
        CheckPath(path);
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
        return Resource.GetRegisteredPath(path) as TResource;
    }

    /// <summary>Gets caller-owned lowercase filename extensions supported for a resource type.</summary>
    /// <typeparam name="TResource">Requested resource type or compatible base type.</typeparam>
    /// <returns>The supported extensions without dots, or an empty array for unsupported types.</returns>
    public static string[] GetRecognizedExtensionsForType<TResource>() where TResource : Resource
    {
        var images = typeof(TResource).IsAssignableFrom(typeof(ImageTexture));
        var fonts = typeof(TResource).IsAssignableFrom(typeof(FontFile));
        var audio = new List<string>();
        if (typeof(TResource).IsAssignableFrom(typeof(AudioStreamWAV))) audio.Add("wav");
        if (typeof(TResource).IsAssignableFrom(typeof(AudioStreamMP3))) audio.Add("mp3");
        if (typeof(TResource).IsAssignableFrom(typeof(AudioStreamOggVorbis))) audio.Add("ogg");
        if (typeof(TResource).IsAssignableFrom(typeof(CryptoKey))) audio.Add("key");
        if (typeof(TResource).IsAssignableFrom(typeof(X509Certificate))) audio.Add("crt");
        if (images || fonts) return [.. images ? ImageExtensions : [], .. fonts ? FontExtensions : [], .. audio];
        return audio.ToArray();

    }

    private static Type? CryptoType<TResource>(string path) where TResource : Resource
    {
        var type = System.IO.Path.GetExtension(path).ToLowerInvariant() switch { ".key" => typeof(CryptoKey), ".crt" => typeof(X509Certificate), _ => null };
        return type is not null && typeof(TResource).IsAssignableFrom(type) ? type : null;
    }
    private static Type? AudioType<TResource>(string path) where TResource : Resource
    {
        var requested = typeof(TResource);
        var extension = System.IO.Path.GetExtension(path);
        var type = extension.ToLowerInvariant() switch { ".wav" => typeof(AudioStreamWAV), ".mp3" => typeof(AudioStreamMP3), ".ogg" => typeof(AudioStreamOggVorbis), _ => null };
        return type is not null && requested.IsAssignableFrom(type) ? type : null;
    }
    private static void CheckType<TResource>() where TResource : Resource
    {
        if (!typeof(TResource).IsAssignableFrom(typeof(ImageTexture)) && !typeof(TResource).IsAssignableFrom(typeof(FontFile)) && !typeof(TResource).IsAssignableFrom(typeof(AudioStreamWAV)) && !typeof(TResource).IsAssignableFrom(typeof(AudioStreamMP3)) && !typeof(TResource).IsAssignableFrom(typeof(AudioStreamOggVorbis)) && !typeof(TResource).IsAssignableFrom(typeof(CryptoKey)) && !typeof(TResource).IsAssignableFrom(typeof(X509Certificate)))
            throw new NotSupportedException($"Resource files for {typeof(TResource).Name} are not integrated.");
    }

    private static void CheckPath(string path) => ArgumentException.ThrowIfNullOrEmpty(path);

    private static void CheckFilePath(string path)
    {
        CheckPath(path);
        ProjectSettings.Instance.GlobalizePath(path);
    }

    private static bool IsExtension(string path, string[] extensions)
    {
        var extension = System.IO.Path.GetExtension(path).TrimStart('.');
        return extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
