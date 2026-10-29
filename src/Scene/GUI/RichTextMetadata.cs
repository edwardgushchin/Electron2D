namespace Electron2D;

/// <summary>Provides the exact typed borrowed payload of a rich-text metadata event.</summary>
public sealed class RichTextMetadata
{
    private interface IValue
    { bool AddText(TextServer server, RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? features, string language); }
    private sealed record Value<T>(T Item) : IValue
    {
        public bool AddText(TextServer server, RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? features, string language) =>
            server.ShapedTextAddString(shaped, text, fonts, size, features, language, Item);
    }
    private readonly IValue _value;
    private RichTextMetadata(IValue value) => _value = value;
    internal static RichTextMetadata Create<T>(T value) => new(new Value<T>(value));
    internal bool AddText(TextServer server, RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? features, string language) =>
        _value.AddText(server, shaped, text, fonts, size, features, language);
    /// <summary>Reads the payload under its original exact type.</summary><typeparam name="T">Stored type.</typeparam><param name="value">Borrowed value or default.</param><returns>Whether T matches.</returns>
    public bool TryGet<T>(out T value) { if (_value is Value<T> stored) { value = stored.Item; return true; } value = default!; return false; }
}
