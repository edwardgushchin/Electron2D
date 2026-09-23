using System.Globalization;
using System.Text;
using System.Xml;

namespace Electron2D;

public sealed partial class Image
{
    /// <summary>Rasterizes an SVG string and replaces the image atomically.</summary>
    /// <param name="svg">An uncompressed SVG document.</param>
    /// <param name="scale">Finite positive multiplier for the SVG's intrinsic dimensions.</param>
    /// <remarks>Uses the same size, format and failure contract as <see cref="LoadSVGFromBuffer"/>.</remarks>
    /// <exception cref="ArgumentNullException">The document is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Scale is not finite and positive.</exception>
    /// <exception cref="InvalidDataException">The document is malformed or exceeds image limits.</exception>
    /// <exception cref="ObjectDisposedException">The image is disposed.</exception>
    public void LoadSVGFromString(string svg, float scale = 1f)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(svg);
        if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        if (svg.Length > MaximumEncodedBytes) throw new InvalidDataException("SVG input exceeds the 64 MiB image limit.");
        byte[] bytes;
        try
        {
            var encoding = new UTF8Encoding(false, true);
            if (encoding.GetByteCount(svg) > MaximumEncodedBytes) throw new InvalidDataException("SVG input exceeds the 64 MiB image limit.");
            bytes = encoding.GetBytes(svg);
        }
        catch (EncoderFallbackException error) { throw new InvalidDataException("SVG text must be valid UTF-8 content.", error); }
        LoadSVGFromBuffer(bytes, scale);
    }

    private static Vector2I SVGSize(byte[] data, float scale)
    {
        if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        try
        {
            using var stream = new MemoryStream(data, writable: false);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaximumEncodedBytes,
            });
            if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "svg")
                throw new InvalidDataException("The document root must be an SVG element.");

            var viewBox = reader.GetAttribute("viewBox");
            var viewSize = ParseViewBox(viewBox);
            var width = ParseLength(reader.GetAttribute("width"), viewSize?.Width ?? 300);
            var height = ParseLength(reader.GetAttribute("height"), viewSize?.Height ?? 150);
            if (width is null && height is null)
            {
                width = viewSize?.Width ?? 300;
                height = viewSize?.Height ?? 150;
            }
            else if (width is null) width = viewSize is { } view ? height * view.Width / view.Height : 300;
            else if (height is null) height = viewSize is { } view ? width * view.Height / view.Width : 150;

            while (reader.Read()) { }
            return new(RasterDimension(width ?? throw new InvalidDataException("SVG width is missing."), scale),
                RasterDimension(height ?? throw new InvalidDataException("SVG height is missing."), scale));
        }
        catch (XmlException error) { throw new InvalidDataException("Invalid SVG XML.", error); }
    }

    private static (double Width, double Height)? ParseViewBox(string? value)
    {
        if (value is null) return null;
        var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4)
            parts = value.Split([',', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
            !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var height) ||
            !double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(width) || !double.IsFinite(height) ||
            Math.Abs(x) > float.MaxValue || Math.Abs(y) > float.MaxValue ||
            width < float.Epsilon || height < float.Epsilon || width > float.MaxValue || height > float.MaxValue)
            throw new InvalidDataException("SVG viewBox must contain four finite numbers with positive dimensions.");
        return (width, height);
    }

    private static double? ParseLength(string? value, double percentBase)
    {
        if (value is null) return null;
        value = value.Trim();
        double factor = 1;
        if (value.EndsWith('%')) { factor = percentBase / 100; value = value[..^1]; }
        else
        {
            foreach (var (unit, multiplier) in new (string, double)[]
            {
                ("px", 1), ("pt", 96d / 72), ("pc", 16), ("mm", 96d / 25.4), ("cm", 96d / 2.54), ("in", 96)
            })
                if (value.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
                {
                    factor = multiplier; value = value[..^unit.Length]; break;
                }
        }
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
            !double.IsFinite(number) || number <= 0 || !double.IsFinite(number * factor))
            throw new InvalidDataException("SVG dimensions must be positive finite pixel or physical lengths.");
        return number * factor;
    }

    private static int RasterDimension(double length, float scale)
    {
        var pixels = Math.Max(1, Math.Round(length * scale, MidpointRounding.AwayFromZero));
        if (!double.IsFinite(pixels) || pixels > 16384)
            throw new InvalidDataException("SVG raster dimensions exceed 16,384 pixels per axis.");
        return (int)pixels;
    }
}
