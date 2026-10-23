using System.Text;

namespace Electron2D;

internal sealed class FontSupportPolicy
{
    internal static readonly FontSupportPolicy Empty = new([], []);
    private static readonly UTF8Encoding UTF8 = new(false, true);
    private readonly Dictionary<string, bool> _languages, _scripts;
    private readonly Dictionary<uint, bool> _tags = [];
    internal FontSupportPolicy(Dictionary<string, bool> languages, Dictionary<string, bool> scripts)
    {
        _languages = languages; _scripts = scripts;
        foreach (var pair in scripts) if (pair.Key.Length == 4 && pair.Key.All(static value => value <= 127)) _tags[Tag(pair.Key)] = pair.Value;
    }
    internal static void Validate(string key)
    {
        ArgumentNullException.ThrowIfNull(key); if (key.Length > 65536 || key.Contains('\0')) throw new ArgumentException("Font support keys must be NUL-free and bounded to 65536 UTF16 units.", nameof(key)); UTF8.GetByteCount(key);
    }
    private static uint Tag(string key) => (uint)key[0] << 24 | (uint)key[1] << 16 | (uint)key[2] << 8 | key[3];
    internal bool Get(bool script, string key) => (script ? _scripts : _languages).GetValueOrDefault(key);
    internal string[] Keys(bool script) => (script ? _scripts : _languages).Keys.ToArray();
    internal FontSupportPolicy With(bool script, string key, bool? supported)
    {
        var selected = script ? _scripts : _languages;
        if (supported is { } value ? selected.TryGetValue(key, out var prior) && prior == value : !selected.ContainsKey(key)) return this;
        var copy = new Dictionary<string, bool>(selected, StringComparer.Ordinal); if (supported is { } next) copy[key] = next; else copy.Remove(key);
        return script ? new(_languages, copy) : new(copy, _scripts);
    }
    internal bool SupportsLanguage(ReadOnlySpan<char> language) => _languages.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(language, out var value) ? value : _languages.GetValueOrDefault("*", true);
    internal bool SupportsScript(string script, HashSet<uint> automaticScripts)
    {
        if (_scripts.TryGetValue(script, out var value)) return value;
        if (_scripts.TryGetValue("*", out value)) return value;
        uint tag = 0; for (var i = 0; i < 4; i++) tag = tag << 8 | (uint)(i < script.Length && script[i] <= 127 ? script[i] : ' ');
        return automaticScripts.Contains(tag);
    }
    internal int Priority(ReadOnlySpan<char> language, uint script, HashSet<uint> automaticScripts)
    {
        var scriptSupported = _tags.TryGetValue(script, out var explicitScript) ? explicitScript : _scripts.TryGetValue("*", out var wildcardScript) ? wildcardScript : automaticScripts.Contains(script);
        if (!scriptSupported) return 2;
        var languageSupported = SupportsLanguage(language);
        return languageSupported ? 0 : 1;
    }
    /// <summary>Preserves the first three UTF16 units without underscores, borrowing input or caller scratch.</summary>
    internal static ReadOnlySpan<char> LanguageKey(string language, Span<char> scratch)
    {
        var prefix = language.AsSpan(0, Math.Min(3, language.Length));
        if (!prefix.Contains('_')) return prefix;
        var count = 0;
        foreach (var value in prefix) if (value != '_') scratch[count++] = value;
        return scratch[..count];
    }
    internal byte[] Save()
    {
        if (_languages.Count == 0 && _scripts.Count == 0) return [];
        using var output = new MemoryStream(); using var writer = new BinaryWriter(output, UTF8, true); writer.Write(1); Write(_languages); Write(_scripts); return output.ToArray();
        void Write(Dictionary<string, bool> values) { writer.Write(values.Count); foreach (var pair in values) { writer.Write(pair.Key); writer.Write(pair.Value); if (output.Length > 64 * 1024 * 1024) throw new InvalidDataException("Font support overrides exceed sixty-four MiB."); } }
    }
    internal static FontSupportPolicy Load(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        try { return Parse(bytes); }
        catch (Exception error) when (error is EndOfStreamException or FormatException or ArgumentException)
        { throw new InvalidDataException("Malformed font support policy.", error); }
    }
    private static FontSupportPolicy Parse(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length == 0) return Empty; if (bytes.Length > 64 * 1024 * 1024) throw new InvalidDataException("Font support overrides exceed sixty-four MiB.");
        using var input = new MemoryStream(bytes, false); using var reader = new BinaryReader(input, UTF8); if (reader.ReadInt32() != 1) throw new InvalidDataException("Unknown font support policy version."); var languages = Read(); var scripts = Read(); if (input.Position != input.Length) throw new InvalidDataException("Trailing font support policy data."); return new(languages, scripts);
        string ReadKey()
        {
            var length = reader.Read7BitEncodedInt();
            if (length < 0 || length > 4 * 65536 || length > input.Length - input.Position) throw new InvalidDataException("Invalid font support key length.");
            return UTF8.GetString(reader.ReadBytes(length));
        }
        Dictionary<string, bool> Read()
        {
            var count = reader.ReadInt32(); if (count < 0 || count > bytes.Length / 2) throw new InvalidDataException("Invalid font support entry count."); var result = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (var i = 0; i < count; i++) { var key = ReadKey(); Validate(key); var value = reader.ReadByte(); if (value > 1 || !result.TryAdd(key, value != 0)) throw new InvalidDataException("Invalid or duplicate font support entry."); }
            return result;
        }
    }
}
