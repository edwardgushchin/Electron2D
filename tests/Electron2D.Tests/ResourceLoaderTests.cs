using Electron2D;
using IOPath = System.IO.Path;

internal static class ResourceLoaderTests
{
    internal static void Run()
    {
        var root = IOPath.Combine(IOPath.GetTempPath(), "Electron2D-loader-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { VerifyTextureLoading(root); }
        finally { Directory.Delete(root, recursive: true); }
        Console.WriteLine("ResourceLoader image-texture file, cache and failure checks passed.");
    }

    private static void VerifyTextureLoading(string root)
    {
        var path = IOPath.Combine(root, "texture.PNG");
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        image.Fill(Colors.Red);
        image.SavePNG(path);

        Check((int)ResourceLoader.CacheMode.Ignore == 0 &&
              (int)ResourceLoader.CacheMode.Reuse == 1 &&
              (int)ResourceLoader.CacheMode.Replace == 2 &&
              (int)ResourceLoader.CacheMode.IgnoreDeep == 3 &&
              (int)ResourceLoader.CacheMode.ReplaceDeep == 4,
            "All cache-mode identities retain pinned numeric values.");
        var extensions = ResourceLoader.GetRecognizedExtensionsForType<Texture>();
        Check(extensions.SequenceEqual(["png", "jpg", "jpeg", "webp", "bmp", "tga", "svg"]),
            "Extension discovery reports every integrated image decoder.");
        extensions[0] = "changed";
        Check(ResourceLoader.GetRecognizedExtensionsForType<ImageTexture>()[0] == "png",
            "Extension results are caller-owned.");
        Check(ResourceLoader.Exists<ImageTexture>(path) && !ResourceLoader.HasCached(path) &&
              ResourceLoader.GetCachedRef<ImageTexture>(path) is null,
            "A supported file exists before any resource is cached.");

        using var texture = ResourceLoader.Load<ImageTexture>(path);
        Check(texture.ResourcePath == path && ResourceLoader.HasCached(path) &&
              ReferenceEquals(ResourceLoader.GetCachedRef<ImageTexture>(path), texture) &&
              ReferenceEquals(ResourceLoader.GetCachedRef<Resource>(path), texture) &&
              ReferenceEquals(ResourceLoader.Load<Texture>(path), texture) &&
              Pixel(texture) == Colors.Red,
            "Initial load produces a registered, sampled texture and returns it through typed base views.");
        var copies = new ImageTexture[8];
        Parallel.For(0, copies.Length, index => copies[index] = ResourceLoader.Load<ImageTexture>(path));
        Check(copies.All(copy => ReferenceEquals(copy, texture)),
            "Concurrent reuse requests converge on one live cached wrapper.");

        var settings = ProjectSettings.Service;
        var priorRoots = (ProjectSettings.ProjectRoot, ProjectSettings.UserDataRoot);
        var userRoot = IOPath.Combine(root, "user");
        Directory.CreateDirectory(userRoot);
        try
        {
            ProjectSettings.ConfigurePaths(root, userRoot);
            using var projectAlias = ResourceLoader.Load<ImageTexture>("res://texture.PNG", ResourceLoader.CacheMode.Ignore);
            image.SavePNG("user://texture.png");
            using var userAlias = ResourceLoader.Load<ImageTexture>("user://texture.png", ResourceLoader.CacheMode.Ignore);
            Check(Pixel(projectAlias) == Colors.Red && Pixel(userAlias) == Colors.Red &&
                  projectAlias.ResourcePath == "res://texture.PNG" && userAlias.ResourcePath == "user://texture.png",
                "Project and user path aliases decode through FileAccess without changing their visible keys.");
        }
        finally { ProjectSettings.ConfigurePaths(priorRoots.ProjectRoot, priorRoots.UserDataRoot); }

        image.Fill(Colors.Green);
        image.SavePNG(path);
        Check(ReferenceEquals(ResourceLoader.Load<ImageTexture>(path), texture) && Pixel(texture) == Colors.Red,
            "Reuse returns cached pixels even after the file changes.");
        using (var independent = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore))
            Check(!ReferenceEquals(independent, texture) && independent.ResourcePath == path &&
                  Pixel(independent) == Colors.Green && ReferenceEquals(ResourceLoader.GetCachedRef<ImageTexture>(path), texture),
                "Ignore returns an unregistered independent texture without displacing the cached one.");
        using (var independent = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.IgnoreDeep))
            Check(!ReferenceEquals(independent, texture) && Pixel(independent) == Colors.Green,
                "Deep ignore has the same behavior for dependency-free images.");

        var changes = 0;
        texture.Changed += _ => changes++;
        Check(ReferenceEquals(ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Replace), texture) &&
              Pixel(texture) == Colors.Green && changes == 1,
            "Replace refreshes pixels in the existing registered texture and notifies its consumers.");
        image.Fill(Colors.Blue);
        image.SavePNG(path);
        Check(ReferenceEquals(ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.ReplaceDeep), texture) &&
              Pixel(texture) == Colors.Blue && changes == 2,
            "Deep replace refreshes the same leaf texture instance.");
        File.WriteAllBytes(path, [0, 1, 2, 3]);
        Reject<InvalidDataException>(() => ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Replace));
        Check(Pixel(texture) == Colors.Blue && changes == 2 && ResourceLoader.HasCached(path),
            "A malformed replacement preserves cached pixels and identity.");
        Reject<ArgumentOutOfRangeException>(() => ResourceLoader.Load<ImageTexture>(path, (ResourceLoader.CacheMode)99));
        Reject<NotSupportedException>(() => ResourceLoader.Load<Curve>(path));
        Check(!ResourceLoader.Exists<Curve>(path) &&
              ResourceLoader.GetRecognizedExtensionsForType<Curve>().Length == 0 &&
              ResourceLoader.GetCachedRef<Curve>(path) is null,
            "An unsupported file type has no false loader capability or typed cached reference.");
        Reject<NotSupportedException>(() => ResourceLoader.Load<ImageTexture>(IOPath.Combine(root, "unsupported.bin")));
        Reject<ArgumentException>(() => ResourceLoader.HasCached(string.Empty));
        using (var invalidCache = new ImageTexture { ResourcePath = "res://../escape.png" })
        {
            Reject<UnauthorizedAccessException>(() => ResourceLoader.Load<ImageTexture>(invalidCache.ResourcePath));
            Reject<UnauthorizedAccessException>(() => ResourceLoader.Exists<ImageTexture>(invalidCache.ResourcePath));
        }

        var takeoverPath = IOPath.Combine(root, "takeover.png");
        image.SavePNG(takeoverPath);
        using (var oldOwner = new Curve { ResourcePath = takeoverPath })
        {
            Reject<InvalidOperationException>(() => ResourceLoader.Load<Resource>(takeoverPath));
            using var newOwner = ResourceLoader.Load<ImageTexture>(takeoverPath, ResourceLoader.CacheMode.Replace);
            Check(oldOwner.ResourcePath.Length == 0 && newOwner.ResourcePath == takeoverPath &&
                  ReferenceEquals(ResourceLoader.GetCachedRef<ImageTexture>(takeoverPath), newOwner),
                "Replace recreates a texture and takes over a path cached under a different resource type.");
        }

        File.Delete(path);
        Check(ResourceLoader.Exists<ImageTexture>(path), "A cached resource remains discoverable when its file is removed.");
        texture.Dispose();
        Check(!ResourceLoader.HasCached(path) && !ResourceLoader.Exists<ImageTexture>(path) &&
              ResourceLoader.GetCachedRef<ImageTexture>(path) is null,
            "Disposal removes the weak cache entry; a missing file then reports absent.");
    }

    private static Color Pixel(ImageTexture texture)
    {
        using var snapshot = texture.GetImage();
        return snapshot!.GetPixel(0, 0);
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
