namespace Electron2D;

public sealed partial class Image
{
    private static readonly object FormatLoaderGate = new();
    private static readonly List<ImageFormatLoaderExtension> FormatLoaders = [];
    private static readonly string[] BuiltInExtensions = ["png", "jpg", "jpeg", "webp", "bmp", "tga", "svg", "dds"];

    internal static void AddFormatLoader(ImageFormatLoaderExtension loader)
    {
        lock (FormatLoaderGate)
        {
            ObjectDisposedException.ThrowIf(loader.IsDisposed, loader);
            if (FormatLoaders.Any(item => ReferenceEquals(item, loader))) return;
            if (FormatLoaders.Count >= 64) throw new InvalidOperationException("At most 64 image decoders can be registered.");
            FormatLoaders.Add(loader);
        }
    }
    internal static void RemoveFormatLoader(ImageFormatLoaderExtension loader)
    {
        lock (FormatLoaderGate)
        {
            var index = FormatLoaders.FindIndex(item => ReferenceEquals(item, loader));
            if (index >= 0) FormatLoaders.RemoveAt(index);
        }
    }
    private static ImageFormatLoaderExtension[] FormatLoaderSnapshot()
    {
        lock (FormatLoaderGate) return [.. FormatLoaders];
    }
    internal static string[] RecognizedFileExtensions()
    {
        var extensions = new List<string>(BuiltInExtensions);
        foreach (var loader in FormatLoaderSnapshot()) extensions.AddRange(loader.Extensions());
        return extensions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
    internal static bool RecognizesFile(string path)
    {
        var extension = System.IO.Path.GetExtension(path).TrimStart('.');
        if (BuiltInExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) return true;
        foreach (var loader in FormatLoaderSnapshot())
            if (loader.Extensions().Contains(extension, StringComparer.OrdinalIgnoreCase)) return true;
        return false;
    }

    private void LoadFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        path = ResourceUID.EnsurePath(path);
        var extension = System.IO.Path.GetExtension(path).TrimStart('.');
        var codec = CodecFromPath(path);
        var loaders = FormatLoaderSnapshot();
        FileAccess? file = null;
        try
        {
            if (codec is not null) file = FileAccess.Open(path, FileAccessModeFlags.Read);
            foreach (var loader in codec is null ? loaders : [])
            {
                if (!loader.Extensions().Contains(extension, StringComparer.OrdinalIgnoreCase)) continue;
                file ??= FileAccess.Open(path, FileAccessModeFlags.Read);
                var originalLength = file.Length;
                CheckEncodedLength(originalLength);
                file.Seek(0);
                using var decoded = new Image();
                if (!loader.Decode(decoded, file, ImageFormatLoader.LoaderFlags.None, 1f)) continue;
                var state = decoded.CopyPixels();
                if (state.Width == 0 || state.Height == 0) throw new InvalidDataException("The decoder returned an empty image.");
                if (file.Length != originalLength) throw new IOException("The encoded image changed while it was decoded.");
                Commit(state);
                return;
            }
            if (file is null) throw new NotSupportedException("No image decoder recognizes the filename extension.");
            if (codec is null) throw new NotSupportedException("No image decoder recognized the file contents.");
            var length = file.Length;
            CheckEncodedLength(length);
            var bytes = file.ReadBytes((int)length);
            if (bytes.Length != length || file.Length != length) throw new IOException("The encoded image changed while it was read.");
            if (codec == "DDS") LoadDDSFromBuffer(bytes); else DecodeAndCommit(bytes, codec);
        }
        finally { file?.Dispose(); }
    }
    private static void CheckEncodedLength(long length)
    {
        if (length is <= 0 or > MaximumEncodedBytes) throw new InvalidDataException("Encoded images must contain between 1 byte and 64 MiB.");
    }
}
