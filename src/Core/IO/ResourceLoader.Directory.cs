namespace Electron2D;

public sealed partial class ResourceLoader
{
    /// <summary>Lists recognized resource filenames and immediate subdirectories.</summary>
    /// <param name="directoryPath">A native, res:// or user:// directory path.</param>
    /// <returns>A caller-owned ordinally sorted snapshot; subdirectory names end in a slash.</returns>
    /// <remarks>Uses current live format loaders and built-in resource extensions without decoding files or
    /// consulting the weak cache. Hidden and navigational entries are omitted. Listing allocates and may block.
    /// Recognition describes format support, not file validity. Imported/exported sidecar remapping requires
    /// the resource import resolver; sidecars alone do not invent a loadable resource.</remarks>
    /// <exception cref="ArgumentNullException">The directory path is null.</exception>
    /// <exception cref="ArgumentException">The directory path is invalid.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    /// <exception cref="IOException">Enumeration fails.</exception>
    /// <exception cref="UnauthorizedAccessException">Directory access is denied or the path escapes its virtual root.</exception>
    /// <exception cref="NotSupportedException">The path scheme is unsupported.</exception>
    public static string[] ListDirectory(string directoryPath)
    {
        using var directory = DirAccess.Open(directoryPath);
        directoryPath = ResourceUID.EnsurePath(directoryPath);
        var loaders = LoaderSnapshot();
        var names = new SortedSet<string>(StringComparer.Ordinal);
        directory.ListDirBegin();
        for (var name = directory.GetNext(); name.Length != 0; name = directory.GetNext())
        {
            if (directory.CurrentIsDir()) { names.Add(name + "/"); continue; }
            var path = System.IO.Path.Combine(directoryPath, name);
            var recognized = Image.RecognizesFile(path) || IsExtension(path, FontExtensions) ||
                AudioType(path, typeof(Resource)) is not null || CryptoType(path, typeof(Resource)) is not null;
            if (!recognized) foreach (var loader in loaders)
                    if (loader.RecognizePath(path)) { recognized = true; break; }
            if (recognized) names.Add(name);
        }
        return [.. names];
    }
}
