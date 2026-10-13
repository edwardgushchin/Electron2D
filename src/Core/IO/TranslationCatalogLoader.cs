using System.Buffers.Binary;
using System.Text;
using System.Globalization;

namespace Electron2D;

internal sealed class TranslationCatalogLoader : ResourceFormatLoader
{
    private const int MaximumBytes = 16 * 1024 * 1024;
    private const int MaximumEntries = 100_000;
    private static readonly UTF8Encoding Encoding = new(false, true);
    private sealed record Entry(string Context, string Source, bool Plural, string[] Forms);
    public override string[] GetRecognizedExtensions() => ["po", "mo"];
    public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(Translation));
    public override Type? GetResourceType(string path) => typeof(Translation);
    public override Type[] GetClassesUsed(string path) => [typeof(Translation)];
    public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode)
    {
        using var file = FileAccess.Open(path, FileAccessModeFlags.Read);
        var length = file.Length;
        if (length > MaximumBytes) throw new InvalidDataException("Translation catalog exceeds its byte budget.");
        var bytes = file.ReadBytes(checked((int)length));
        if (bytes.Length != length || file.Length != length) throw new InvalidDataException("Translation catalog changed during reading.");
        var entries = System.IO.Path.GetExtension(path).Equals(".mo", StringComparison.OrdinalIgnoreCase) ? ReadMO(bytes) : ReadPO(Encoding.GetString(bytes).TrimStart('\uFEFF'));
        var headers = entries.Where(entry => entry.Source.Length == 0 && entry.Context.Length == 0).ToArray();
        if (headers.Length != 1 || headers[0].Plural || headers[0].Forms.Length != 1 || headers[0].Forms[0].Length == 0) throw new InvalidDataException("Catalog must contain one nonempty header.");
        var locale = "en"; var definition = "";
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in headers[0].Forms[0].Split('\n'))
        {
            var colon = line.IndexOf(':'); if (colon < 0) continue;
            var key = line[..colon].Trim(); var value = line[(colon + 1)..].Trim();
            if (!fields.Add(key)) throw new InvalidDataException("Duplicate catalog header field.");
            if (key is "Language" or "X-Language") locale = value;
            if (key == "Plural-Forms") definition = value;
            if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                foreach (var part in value.Split(';').Skip(1))
                    if (part.Trim().StartsWith("charset=", StringComparison.OrdinalIgnoreCase) && !part.Trim()[8..].Trim(' ', '"').Equals("UTF-8", StringComparison.OrdinalIgnoreCase))
                        throw new NotSupportedException("Translation catalogs require UTF-8 encoding.");
        }
        CatalogPluralRule? rule = definition.Length == 0 ? null : CatalogPluralRule.Parse(definition);
        if (entries.Any(entry => entry.Plural) && rule is null) throw new InvalidDataException("Plural catalog entries require a valid Plural-Forms header.");
        var identities = new HashSet<(string, string)>();
        foreach (var entry in entries)
        {
            if (!identities.Add((entry.Context, entry.Source))) throw new InvalidDataException("Duplicate catalog message identity.");
            if (entry.Plural && entry.Forms.Length != rule!.Forms) throw new InvalidDataException("Catalog entry does not match its declared plural count.");
        }
        var result = new Translation();
        try
        {
            result.Locale = locale; result.PluralRulesOverride = definition;
            foreach (var entry in entries)
            {
                if (entry.Source.Length == 0 && entry.Context.Length == 0) continue;
                if (entry.Plural) result.AddPluralMessage(entry.Source, entry.Forms, entry.Context);
                else result.AddMessage(entry.Source, entry.Forms[0], entry.Context);
            }
            return result;
        }
        catch { result.Dispose(); throw; }
    }
    private static List<Entry> ReadMO(byte[] bytes)
    {
        if (bytes.Length < 28) throw new InvalidDataException("Truncated MO header.");
        var magic = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        if (magic is not (0x950412de or 0xde120495)) throw new InvalidDataException("Invalid MO signature.");
        uint Word(int offset) => magic == 0x950412de ? BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4)) : BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
        var revision = Word(4);
        if ((revision >> 16) > 1) throw new NotSupportedException("Unsupported MO major revision.");
        if ((revision & 0xffff) != 0)
        {
            if (bytes.Length < 48) throw new InvalidDataException("Truncated MO extension header.");
            if (Word(28) != 0 || Word(36) != 0) throw new NotSupportedException("MO system-dependent segments require a native ABI substitution contract.");
        }
        var count = Word(8); var originals = Word(12); var translations = Word(16);
        void Range(uint offset, ulong size) { if (offset > bytes.Length || size > (ulong)bytes.Length - offset) throw new InvalidDataException("MO table or string exceeds file bounds."); }
        if (count > MaximumEntries) throw new InvalidDataException("MO message budget exceeded.");
        Range(originals, count * 8UL); Range(translations, count * 8UL);
        if (Word(20) != 0) Range(Word(24), Word(20) * 4UL);
        // Bound decoded storage as well as the file: hostile descriptors can repeatedly alias a large string.
        long total = 0;
        string Text(uint table, uint index)
        {
            var descriptor = checked((int)(table + index * 8)); var size = Word(descriptor); var offset = Word(descriptor + 4);
            Range(offset, size + 1UL); total += size;
            if (total > MaximumBytes || bytes[(int)(offset + size)] != 0) throw new InvalidDataException("Invalid MO string terminator or decoded size budget.");
            return Encoding.GetString(bytes, (int)offset, (int)size);
        }
        var entries = new List<Entry>((int)count);
        for (uint index = 0; index < count; index++)
        {
            var id = Text(originals, index); var translated = Text(translations, index);
            var sources = id.Split('\0');
            if (sources.Length > 2 || (sources.Length == 2 && sources[1].Length == 0)) throw new InvalidDataException("Invalid MO plural source.");
            var contextEnd = sources[0].IndexOf('\u0004');
            var context = contextEnd < 0 ? "" : sources[0][..contextEnd]; var source = contextEnd < 0 ? sources[0] : sources[0][(contextEnd + 1)..];
            if (source.Contains('\u0004')) throw new InvalidDataException("Invalid MO context separator.");
            var forms = translated.Split('\0');
            if (forms.Length > 64 || (sources.Length == 1 && forms.Length != 1)) throw new InvalidDataException("Invalid MO translated forms.");
            entries.Add(new Entry(context, source, sources.Length == 2, forms));
        }
        return entries;
    }
    private static List<Entry> ReadPO(string text)
    {
        var entries = new List<Entry>();
        StringBuilder? context = null, source = null, plural = null;
        var processed = 0;
        var forms = new List<StringBuilder>(); var field = ""; var form = -1; var fuzzy = false;
        void Finish()
        {
            if (source is null) { if (context is not null) throw new InvalidDataException("PO context has no message."); return; }
            if (forms.Count == 0 || (plural is null && forms.Count != 1) || plural is { Length: 0 }) throw new InvalidDataException("Incomplete PO message.");
            if (++processed > MaximumEntries) throw new InvalidDataException("PO message budget exceeded.");
            if (!fuzzy)
            {
                if (entries.Count >= MaximumEntries) throw new InvalidDataException("PO message budget exceeded.");
                entries.Add(new Entry(context?.ToString() ?? "", source.ToString(), plural is not null, forms.Select(value => value.ToString()).ToArray()));
            }
            context = source = plural = null; forms.Clear(); field = ""; form = -1; fuzzy = false;
        }
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } raw)
        {
            var line = raw.Trim();
            if (line.Length == 0) { Finish(); fuzzy = false; continue; }
            if (line[0] == '#')
            {
                if (forms.Count > 0) Finish();
                if (line.StartsWith("#,", StringComparison.Ordinal) && line[2..].Split(',').Any(flag => flag.Trim() == "fuzzy")) fuzzy = true;
                continue;
            }
            string value;
            if (line[0] == '"') value = Unquote(line);
            else
            {
                var space = line.IndexOfAny([' ', '\t']); if (space < 0) throw new InvalidDataException("Invalid PO directive.");
                var directive = line[..space]; value = Unquote(line[(space + 1)..].Trim());
                if (directive == "msgctxt") { if (source is not null) Finish(); if (context is not null) throw new InvalidDataException("Duplicate PO context."); field = "context"; context = new(); }
                else if (directive == "msgid") { if (source is not null) Finish(); field = "source"; source = new(); }
                else if (directive == "msgid_plural") { if (source is null || plural is not null || forms.Count != 0) throw new InvalidDataException("Unexpected PO plural source."); field = "plural"; plural = new(); }
                else if (directive == "msgstr") { if (source is null || plural is not null || forms.Count != 0) throw new InvalidDataException("Unexpected PO translation."); field = "form"; form = 0; forms.Add(new()); }
                else if (directive.StartsWith("msgstr[", StringComparison.Ordinal) && directive.EndsWith(']'))
                {
                    if (plural is null || !int.TryParse(directive.AsSpan(7, directive.Length - 8), NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index != forms.Count || index >= 64) throw new InvalidDataException("PO plural indexes must be contiguous and bounded.");
                    field = "form"; form = index; forms.Add(new());
                }
                else throw new InvalidDataException("Unknown PO directive.");
            }
            switch (field)
            {
                case "context": context!.Append(value); break;
                case "source": source!.Append(value); break;
                case "plural": plural!.Append(value); break;
                case "form": forms[form].Append(value); break;
                default: throw new InvalidDataException("PO continuation has no directive.");
            }
        }
        Finish(); return entries;
    }
    private static string Unquote(string text)
    {
        if (text.Length < 2 || text[0] != '"' || text[^1] != '"') throw new InvalidDataException("Invalid PO quoted string.");
        var result = new StringBuilder();
        for (var i = 1; i < text.Length - 1; i++)
        {
            var value = text[i];
            if (value == '"' || value == '\0') throw new InvalidDataException("Invalid PO string character.");
            if (value == '\\')
            {
                if (++i >= text.Length - 1) throw new InvalidDataException("Incomplete PO escape.");
                value = text[i] switch { 'a' => '\a', 'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t', 'v' => '\v', '\\' => '\\', '"' => '"', _ => throw new InvalidDataException("Unsupported PO escape.") };
            }
            result.Append(value);
        }
        return result.ToString();
    }
}
