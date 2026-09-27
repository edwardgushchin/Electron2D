using Electron2D;
using Path = System.IO.Path;

internal static class FontResourceLoaderTests
{
    internal static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-font-loader-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "font.WOFF2"); File.WriteAllBytes(path, FontTestFixtures.OpenSans);
            Check(ResourceLoader.Exists<Font>(path) && !ResourceLoader.Exists<Texture>(path), "Font files use typed extension discovery.");
            var extensions = ResourceLoader.GetRecognizedExtensionsForType<Font>();
            Check(extensions.Contains("woff2") && ResourceLoader.GetRecognizedExtensionsForType<Resource>().Contains("png"), "Base resource discovery combines the actual format families.");
            extensions[0] = "modified";
            Check(ResourceLoader.GetRecognizedExtensionsForType<FontFile>()[0] == "ttf", "Extension snapshots cannot mutate loader state.");
            using var font = ResourceLoader.Load<FontFile>(path);
            Check(font.FontName == "Open Sans" && font.ResourcePath == path && font.HasChar('A') && ReferenceEquals(ResourceLoader.Load<Font>(path), font), "Load creates one cached, usable font identity.");
            var copies = new Font[8]; Parallel.For(0, copies.Length, i => copies[i] = ResourceLoader.Load<Font>(path));
            Check(copies.All(copy => ReferenceEquals(copy, font)), "Concurrent font reuse returns one identity.");
            using (var separate = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.IgnoreDeep))
                Check(!ReferenceEquals(separate, font) && separate.ResourcePath == path && ReferenceEquals(ResourceLoader.GetCachedRef<Font>(path), font), "Ignore modes retain the visible path without displacing cached ownership.");
            var changes = 0; font.Changed += _ => changes++;
            File.WriteAllBytes(path, FontTestFixtures.Arabic);
            Check(ReferenceEquals(ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.ReplaceDeep), font) && font.HasChar('ب') && changes == 1, "Replace updates the same font and its real character map.");
            var committedName = font.FontName; var committedSize = font.GetStringSize("ب");
            File.WriteAllBytes(path, [1, 2, 3]);
            Reject<InvalidDataException>(() => ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Replace));
            Check(font.FontName == committedName && font.GetStringSize("ب") == committedSize && changes == 1, "Malformed replacement leaves the live font and layout intact.");
            File.Delete(path);
            Check(ResourceLoader.Exists<Font>(path) && ReferenceEquals(ResourceLoader.Load<Resource>(path), font), "A live cached font remains usable when its source disappears.");
            Reject<InvalidOperationException>(() => ResourceLoader.Load<Texture>(path));
            font.Dispose(); Check(!ResourceLoader.HasCached(path), "Disposed fonts leave the weak resource path cache.");
        }
        finally { Directory.Delete(root, true); }
        Console.WriteLine("Font resource loading verifies type discovery, concurrent reuse, independent loads, atomic replacement and weak-cache lifetime.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
