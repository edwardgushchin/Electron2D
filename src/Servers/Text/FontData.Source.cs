namespace Electron2D;

internal sealed partial class FontData
{
    private static byte[] ReadDynamicSource(string path)
    {
        using var file = FileAccess.Open(path, FileAccessModeFlags.Read); var length = file.Length;
        if (length is <= 0 or > 64 * 1024 * 1024) throw new InvalidDataException("Font files must contain between one byte and sixty-four MiB.");
        var bytes = file.ReadBytes((int)length);
        if (bytes.Length != length || file.Length != length) throw new IOException("The font file changed while being read.");
        return bytes;
    }
    internal static (byte[] Bytes, byte[] Metrics) ReadSource(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var extension = System.IO.Path.GetExtension(path);
        if (extension.Equals(".pfm", StringComparison.OrdinalIgnoreCase) || extension.Equals(".afm", StringComparison.OrdinalIgnoreCase))
        {
            var stem = path[..^extension.Length];
            foreach (var suffix in new[] { ".pfb", ".PFB", ".pfa", ".PFA" })
                if (FileAccess.FileExists(stem + suffix)) return ReadSourcePair(stem + suffix, path);
            throw new InvalidDataException("Type 1 metrics require a matching PFB or PFA outline source.");
        }
        var data = ReadDynamicSource(path);
        if (extension.Equals(".pfb", StringComparison.OrdinalIgnoreCase) || extension.Equals(".pfa", StringComparison.OrdinalIgnoreCase))
        {
            var stem = path[..^extension.Length];
            foreach (var suffix in new[] { ".pfm", ".PFM", ".afm", ".AFM" })
                if (FileAccess.FileExists(stem + suffix)) return ReadSourcePair(path, stem + suffix, data);
        }
        return (data, []);
    }
    private static (byte[] Bytes, byte[] Metrics) ReadSourcePair(string path, string metricsPath, byte[]? bytes = null)
    {
        bytes ??= ReadDynamicSource(path); var metrics = ReadDynamicSource(metricsPath);
        if ((long)bytes.Length + metrics.Length > 64 * 1024 * 1024) throw new InvalidDataException("Outline and auxiliary font data exceed the source byte budget.");
        return (bytes, metrics);
    }
}
