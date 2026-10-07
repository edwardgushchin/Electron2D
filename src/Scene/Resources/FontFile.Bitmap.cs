using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Electron2D;

/// <summary>Controls bitmap-font scaling from its fixed source size.</summary>
public enum FixedSizeScaleMode
{
    /// <summary>Uses the source size regardless of requested size.</summary>
    Disable = 0,
    /// <summary>Scales by the exact requested-to-source ratio.</summary>
    Enabled = 2,
    /// <summary>Rounds the requested-to-source ratio to an integer.</summary>
    IntegerOnly = 1
}

public partial class FontFile
{
    private int _fixedSize;
    private FixedSizeScaleMode _fixedScaleMode;
    /// <summary>Gets or sets the bitmap source size.</summary>
    /// <value>Zero disables fixed-size selection; valid values are zero through 16384. Scalable faces retain their ordinary size behavior.</value>
    /// <exception cref="ArgumentOutOfRangeException">The size is outside its supported range.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int FixedSize
    {
        get { lock (FontGate) { ThrowIfDisposed(); return _fixedSize; } }
        set { if ((uint)value > 16384) throw new ArgumentOutOfRangeException(nameof(value)); EditFixedSettings(value, null); }
    }
    /// <summary>Gets or sets fixed bitmap-size scaling.</summary>
    /// <value>Disable initially. IntegerOnly rounds positive ratios away from zero at the half boundary, including zero below one half.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is unknown.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public FixedSizeScaleMode FixedSizeScaleMode
    {
        get { lock (FontGate) { ThrowIfDisposed(); return _fixedScaleMode; } }
        set { if (value < FixedSizeScaleMode.Disable || value > FixedSizeScaleMode.Enabled) throw new ArgumentOutOfRangeException(nameof(value)); EditFixedSettings(null, value); }
    }
    private void EditFixedSettings(int? size, FixedSizeScaleMode? mode)
    {
        List<FontData>? retired = null;
        lock (FontGate)
        {
            ThrowIfDisposed(); var nextSize = size ?? _fixedSize; var nextMode = mode ?? _fixedScaleMode;
            if (nextSize == _fixedSize && nextMode == _fixedScaleMode) return;
            var copies = new List<FontCache>();
            for (var i = 0; i < _caches.Count; i++) { var data = DataLocked(i); lock (data.Gate) { var copy = _caches[i].Clone(); copy.FixedSize = nextSize; copy.ScaleMode = nextMode; copies.Add(copy); } }
            if (copies.Count > 0) retired = PublishCachesLocked(copies);
            _fixedSize = nextSize; _fixedScaleMode = nextMode; InvalidateFontStateLocked();
        }
        if (retired != null) NotifyRetired(retired); else EmitChanged();
    }
    /// <summary>Atomically imports a text or binary version-three AngelCode BMFont and its image pages.</summary>
    /// <param name="path">Operating-system, res:// or user:// font descriptor path.</param>
    /// <remarks>Loads copied pixels, metrics, scalar glyph indices, kerning, packed channels and supplied outlines.
    /// Missing/malformed pages and invalid records preserve the previous font. Bitmap loading selects its fixed size,
    /// disables hinting/subpixel positioning and clears scalable bytes; fallbacks and feature defaults remain borrowed/preserved.</remarks>
    /// <exception cref="InvalidDataException">The descriptor, image or record is malformed or exceeds its budget.</exception>
    /// <exception cref="IOException">A descriptor or page cannot be read.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void LoadBitmapFont(string path)
    {
        ThrowIfDisposed(); var imported = ReadBitmap(path); FontData replacement; List<FontData> retired = [];
        lock (FontGate)
        {
            ThrowIfDisposed(); replacement = new FontData([], authored: imported.Cache);
            replacement.AllowSystemFallback = true; replacement.Hinting = FontHinting.None; replacement.SubpixelPositioning = FontSubpixelPositioning.Disabled;
            replacement.OpenTypeFeatures = _fontData!.OpenTypeFeatures; replacement.FamilyName = imported.Name; replacement.FontStyle = imported.Style;
            replacement.FontWeight = (imported.Style & FontStyle.Bold) != 0 ? 700 : 400;
            retired.Add(_fontData); retired.AddRange(_cacheData.Values); _cacheData.Clear(); _caches.Clear(); _caches.Add(imported.Cache);
            _fixedSize = imported.Cache.FixedSize; _fixedScaleMode = imported.Cache.ScaleMode; _fontData = replacement; _bytes = []; _deferred = false; InvalidateFontStateLocked();
        }
        try { EmitChanged(); } finally { foreach (var data in retired) data.Dispose(); }
    }
    private sealed class BitmapInput
    {
        internal string Name = string.Empty, Charset = "ANSI";
        internal bool Unicode = true, Packed;
        internal int Size = 16, Height, Ascent, Outline, Pages;
        internal FontStyle Style;
        internal int[] Channels = [4, 4, 4, 0];
        internal readonly Dictionary<int, string> Files = [];
        internal readonly List<(uint ID, int X, int Y, int W, int H, int DX, int DY, int Advance, int Page, int Channel)> Glyphs = [];
        internal readonly List<(uint A, uint B, int Amount)> Kernings = [];
    }
    private static (FontCache Cache, string Name, FontStyle Style) ReadBitmap(string path)
    {
        var input = ReadBitmapInput(path);
        try
        {
            if (input.Size is <= 0 or > 16384 || input.Height < 0 || input.Ascent < 0 || input.Pages is <= 0 or > 16384 || input.Files.Count != input.Pages || input.Glyphs.Count > 65536 || input.Kernings.Count > 65536 || input.Channels.Any(c => c < 0 || c > 4)) throw new InvalidDataException("Invalid bitmap font dimensions/counts.");
            var cache = new FontCache { FixedSize = input.Size }; var baseSize = cache.Size(new(input.Size, 0));
            baseSize.Metrics = [input.Ascent, input.Height - input.Ascent, 0, 0]; long totalPixels = 0;
            for (var pageIndex = 0; pageIndex < input.Pages; pageIndex++)
            {
                if (!input.Files.TryGetValue(pageIndex, out var file)) throw new InvalidDataException("Bitmap font pages must have contiguous indices.");
                var absolute = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ProjectSettings.GlobalizePath(path))!, file.Replace('\\', System.IO.Path.DirectorySeparatorChar));
                using var image = Image.LoadFromFile(absolute); image.Convert(Image.Format.Rgba8); var pixels = image.GetData();
                if (image.Width <= 0 || image.Width > 16384 || image.Height <= 0 || image.Height > 16384) throw new InvalidDataException("Bitmap page exceeds canvas limits.");
                foreach (var converted in ConvertBitmapPage(input, image.Width, image.Height, pixels))
                {
                    totalPixels += converted.Page.Pixels.Length; if (totalPixels > MaximumFontBytes) throw new InvalidDataException("Bitmap pages exceed sixty-four MiB.");
                    PutPage(cache.Size(new(input.Size, converted.Outline)), pageIndex * (input.Packed ? 4 : 1) + converted.Channel, converted.Page);
                }
            }
            foreach (var g in input.Glyphs)
            {
                var scalar = BitmapScalar(input, g.ID); if (scalar == 0) continue;
                if (g.X < 0 || g.Y < 0 || g.W < 0 || g.H < 0 || g.Page < 0 || g.Page >= input.Pages) throw new InvalidDataException("Invalid bitmap glyph rectangle or page.");
                var channel = input.Packed ? g.Channel switch { 1 => 2, 2 => 1, 4 => 0, 8 => 3, _ => throw new InvalidDataException("Packed glyph requires one channel.") } : 0;
                var page = g.Page * (input.Packed ? 4 : 1) + channel; var bitmap = baseSize.Pages[page]!;
                if ((long)g.X + g.W > bitmap.Width || (long)g.Y + g.H > bitmap.Height) throw new InvalidDataException("Glyph lies outside its page.");
                var colored = !input.Packed && (input.Channels.All(c => c == 0) || input.Channels.AsSpan().SequenceEqual([4, 4, 4, 0]));
                var glyph = new FontCacheGlyph(new(g.DX, g.DY - input.Ascent), new(g.W, g.H), new(g.X, g.Y, g.W, g.H), page, colored);
                if (!baseSize.Glyphs.TryAdd(scalar, glyph)) throw new InvalidDataException("Duplicate bitmap character.");
                baseSize.Advances.Add(scalar, new(g.Advance < 0 ? g.W + 1 : g.Advance, 0));
                if (cache.Sizes.TryGetValue(new(input.Size, 1), out var outlineSize)) outlineSize.Glyphs.Add(scalar, glyph);
            }
            foreach (var k in input.Kernings) baseSize.Kerning.Add(new((int)BitmapScalar(input, k.A), (int)BitmapScalar(input, k.B)), new(k.Amount, 0));
            return (cache, input.Name, input.Style);
        }
        catch (Exception error) when (error is ArgumentException or DecoderFallbackException or OverflowException or EndOfStreamException or IndexOutOfRangeException or FormatException)
        { throw new InvalidDataException("Malformed bitmap font.", error); }
    }
    private static BitmapInput ReadBitmapInput(string path)
    {
        using var source = FileAccess.Open(path, FileAccessModeFlags.Read); var length = source.Length;
        if (length is <= 0 or > MaximumFontBytes) throw new InvalidDataException("Bitmap font descriptor must contain at most sixty-four MiB.");
        var bytes = source.ReadBytes((int)length); if (bytes.Length != length || source.Length != length) throw new IOException("Bitmap descriptor changed while being read.");
        var input = new BitmapInput();
        try
        {
            if (bytes.Length >= 3 && bytes[0] == 'B' && bytes[1] == 'M' && bytes[2] == 'F') ReadBinaryBitmap(bytes, input);
            else ReadTextBitmap(new UTF8Encoding(false, true).GetString(bytes), input);
            return input;
        }
        catch (Exception error) when (error is ArgumentException or OverflowException or EndOfStreamException or IndexOutOfRangeException or FormatException)
        { throw new InvalidDataException("Malformed bitmap font.", error); }
    }
    internal static string[] BitmapDependencies(string path, bool addTypes)
    {
        var input = ReadBitmapInput(path);
        if (input.Pages is <= 0 or > 16384 || input.Files.Count != input.Pages) throw new InvalidDataException("Invalid bitmap page count.");
        var directory = System.IO.Path.GetDirectoryName(ProjectSettings.GlobalizePath(path))!; var result = new string[input.Pages];
        for (var i = 0; i < result.Length; i++)
        {
            if (!input.Files.TryGetValue(i, out var file)) throw new InvalidDataException("Bitmap page indices must be contiguous.");
            result[i] = System.IO.Path.GetFullPath(System.IO.Path.Combine(directory, file.Replace('\\', System.IO.Path.DirectorySeparatorChar))) + (addTypes ? "::ImageTexture" : "");
        }
        return result;
    }
    private static uint BitmapScalar(BitmapInput input, uint scalar)
    {
        if (!input.Unicode && scalar >= 128)
        {
            if (scalar > 255) throw new InvalidDataException("OEM bitmap character exceeds one byte.");
            var codepage = input.Charset.ToUpperInvariant() switch { "0" or "ANSI" => 1252, "238" or "EASTEUROPE" => 1250, "204" or "RUSSIAN" => 1251, "161" or "GREEK" => 1253, "162" or "TURKISH" => 1254, "177" or "HEBREW" => 1255, "178" or "ARABIC" => 1256, "186" or "BALTIC" => 1257, "163" or "VIETNAMESE" => 1258, _ => throw new InvalidDataException("Unsupported bitmap OEM character set.") };
            var encoding = CodePagesEncodingProvider.Instance.GetEncoding(codepage) ?? throw new NotSupportedException("OEM encoding is unavailable.");
            Span<byte> value = stackalloc byte[1] { (byte)scalar }; Span<char> character = stackalloc char[2]; var count = encoding.GetChars(value, character); if (count != 1) throw new InvalidDataException("Invalid OEM scalar."); scalar = character[0];
        }
        if (!Rune.IsValid(scalar)) throw new InvalidDataException("Invalid bitmap Unicode scalar."); return scalar;
    }
    private static IEnumerable<(int Outline, int Channel, FontCachePage Page)> ConvertBitmapPage(BitmapInput input, int width, int height, byte[] pixels)
    {
        var color = !input.Packed && (input.Channels.All(c => c == 0) || input.Channels.AsSpan().SequenceEqual([4, 4, 4, 0]));
        var combinedColor = !input.Packed && input.Outline > 0 && input.Channels.All(c => c == 2);
        if (color) { yield return (0, 0, new(width, height, pixels, [])); yield break; }
        var glyphChannel = Array.IndexOf(input.Channels, 0); var outlineChannel = Array.IndexOf(input.Channels, 1); var combinedChannel = Array.IndexOf(input.Channels, 2);
        if (!input.Packed && glyphChannel < 0 && combinedChannel < 0) throw new InvalidDataException("Bitmap page has no glyph channel.");
        for (var channel = 0; channel < (input.Packed ? 4 : 1); channel++)
        {
            var isCombined = combinedColor || input.Packed && input.Channels[3] == 2 || !input.Packed && glyphChannel < 0;
            for (var outline = 0; outline <= (input.Outline > 0 && (isCombined || outlineChannel >= 0) ? 1 : 0); outline++)
            {
                var result = new byte[pixels.Length];
                for (var i = 0; i < pixels.Length; i += 4)
                {
                    if (combinedColor)
                    {
                        for (var c = 0; c < 4; c++) result[i + c] = pixels[i + c] > 127 ? outline == 0 ? pixels[i + c] : (byte)0 : outline == 1 ? (byte)(pixels[i + c] * 2) : (byte)0;
                    }
                    else
                    {
                        var c = input.Packed ? channel : isCombined ? combinedChannel : outline == 0 ? glyphChannel : outlineChannel;
                        var value = pixels[i + c]; var alpha = isCombined ? value > 15 ? outline == 0 ? (byte)((value - 15) * 2) : (byte)0 : outline == 1 ? (byte)(value * 2) : (byte)0 : value;
                        result[i] = result[i + 1] = result[i + 2] = 255; result[i + 3] = alpha;
                    }
                }
                yield return (outline, channel, new(width, height, result, []));
            }
        }
    }
    private static void ReadTextBitmap(string text, BitmapInput input)
    {
        var info = false; var common = false;
        foreach (var line in text.TrimStart('\uFEFF').Split('\n'))
        {
            var record = line.Trim(); if (record.Length == 0) continue;
            var stop = record.IndexOfAny([' ', '\t']); var tag = stop < 0 ? record : record[..stop]; var values = BitmapFields(stop < 0 ? string.Empty : record[(stop + 1)..]);
            int N(string name, int fallback = 0) => values.TryGetValue(name, out var value) ? int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture) : fallback;
            switch (tag)
            {
                case "info": if (info) throw new InvalidDataException("Duplicate info record."); info = true; input.Name = values.GetValueOrDefault("face", ""); input.Size = Math.Abs(N("size", 16)); input.Unicode = N("unicode", 1) != 0; input.Charset = values.GetValueOrDefault("charset", "ANSI"); input.Outline = N("outline"); input.Style = (N("bold") != 0 ? FontStyle.Bold : 0) | (N("italic") != 0 ? FontStyle.Italic : 0); break;
                case "common": if (common) throw new InvalidDataException("Duplicate common record."); common = true; input.Height = N("lineHeight"); input.Ascent = N("base"); input.Pages = N("pages"); input.Packed = N("packed") != 0; input.Channels = [N("redChnl", 4), N("greenChnl", 4), N("blueChnl", 4), N("alphaChnl")]; break;
                case "page": input.Files.Add(N("id"), values.GetValueOrDefault("file") ?? throw new InvalidDataException("Page requires a filename.")); break;
                case "char": input.Glyphs.Add((checked((uint)N("id")), N("x"), N("y"), N("width"), N("height"), N("xoffset"), N("yoffset"), N("xadvance"), N("page"), N("chnl", 15))); break;
                case "kerning": input.Kernings.Add((checked((uint)N("first")), checked((uint)N("second")), N("amount"))); break;
                case "chars": case "kernings": break;
                default: throw new InvalidDataException("Unknown bitmap font record.");
            }
        }
        if (!info || !common) throw new InvalidDataException("Bitmap font requires info/common records.");
    }
    private static Dictionary<string, string> BitmapFields(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal); var position = 0;
        while (position < text.Length)
        {
            while (position < text.Length && char.IsWhiteSpace(text[position])) position++; if (position == text.Length) break;
            var begin = position; while (position < text.Length && text[position] != '=' && !char.IsWhiteSpace(text[position])) position++;
            if (position == text.Length || text[position] != '=') throw new InvalidDataException("Malformed bitmap field."); var name = text[begin..position++];
            string value;
            if (position < text.Length && text[position] == '"') { begin = ++position; while (position < text.Length && text[position] != '"') position++; if (position == text.Length) throw new InvalidDataException("Unterminated bitmap string."); value = text[begin..position++]; }
            else { begin = position; while (position < text.Length && !char.IsWhiteSpace(text[position])) position++; value = text[begin..position]; }
            if (!result.TryAdd(name, value)) throw new InvalidDataException("Duplicate bitmap field.");
        }
        return result;
    }
    private static void ReadBinaryBitmap(byte[] bytes, BitmapInput input)
    {
        if (bytes.Length < 4 || bytes[3] != 3) throw new InvalidDataException("Only binary BMFont version three is supported.");
        var position = 4; var seen = new HashSet<byte>();
        while (position < bytes.Length)
        {
            if (bytes.Length - position < 5) throw new InvalidDataException("Truncated bitmap block."); var type = bytes[position++]; var length = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(position)); position += 4;
            if (length > bytes.Length - position || !seen.Add(type)) throw new InvalidDataException("Invalid/duplicate bitmap block."); var b = bytes.AsSpan(position, (int)length); position += (int)length;
            switch (type)
            {
                case 1: if (b.Length < 15 || b[^1] != 0) throw new InvalidDataException("Invalid info block."); input.Size = Math.Abs((int)BinaryPrimitives.ReadInt16LittleEndian(b)); input.Unicode = (b[2] & 2) != 0; input.Charset = b[3].ToString(CultureInfo.InvariantCulture); input.Style = ((b[2] & 8) != 0 ? FontStyle.Bold : 0) | ((b[2] & 4) != 0 ? FontStyle.Italic : 0); input.Outline = b[13]; input.Name = new UTF8Encoding(false, true).GetString(b[14..^1]); break;
                case 2: if (b.Length != 15) throw new InvalidDataException("Invalid common block."); input.Height = BinaryPrimitives.ReadUInt16LittleEndian(b); input.Ascent = BinaryPrimitives.ReadUInt16LittleEndian(b[2..]); input.Pages = BinaryPrimitives.ReadUInt16LittleEndian(b[8..]); input.Packed = (b[10] & 128) != 0; input.Channels = [b[12], b[13], b[14], b[11]]; break;
                case 3: var names = new UTF8Encoding(false, true).GetString(b).Split('\0'); if (names[^1].Length != 0) throw new InvalidDataException("Unterminated page name."); for (var i = 0; i < names.Length - 1; i++) input.Files.Add(i, names[i]); break;
                case 4: if (b.Length % 20 != 0) throw new InvalidDataException("Invalid chars block."); for (var i = 0; i < b.Length; i += 20) { var g = b[i..]; input.Glyphs.Add((BinaryPrimitives.ReadUInt32LittleEndian(g), BinaryPrimitives.ReadUInt16LittleEndian(g[4..]), BinaryPrimitives.ReadUInt16LittleEndian(g[6..]), BinaryPrimitives.ReadUInt16LittleEndian(g[8..]), BinaryPrimitives.ReadUInt16LittleEndian(g[10..]), BinaryPrimitives.ReadInt16LittleEndian(g[12..]), BinaryPrimitives.ReadInt16LittleEndian(g[14..]), BinaryPrimitives.ReadInt16LittleEndian(g[16..]), g[18], g[19])); } break;
                case 5: if (b.Length % 10 != 0) throw new InvalidDataException("Invalid kernings block."); for (var i = 0; i < b.Length; i += 10) { var k = b[i..]; input.Kernings.Add((BinaryPrimitives.ReadUInt32LittleEndian(k), BinaryPrimitives.ReadUInt32LittleEndian(k[4..]), BinaryPrimitives.ReadInt16LittleEndian(k[8..]))); } break;
                default: throw new InvalidDataException("Unknown bitmap block.");
            }
        }
        if (!seen.Contains(1) || !seen.Contains(2) || !seen.Contains(3) || !seen.Contains(4)) throw new InvalidDataException("Bitmap font lacks required blocks.");
    }
}
