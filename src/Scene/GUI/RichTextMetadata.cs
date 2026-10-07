namespace Electron2D;

/// <summary>Provides the exact typed borrowed payload of a rich-text metadata event.</summary>
public sealed class RichTextMetadata
{
    private sealed record Value<T>(T Item);
    private readonly object _value;
    private RichTextMetadata(object value) => _value = value;
    internal static RichTextMetadata Create<T>(T value) => new(new Value<T>(value));
    /// <summary>Reads the payload under its original exact type.</summary><typeparam name="T">Stored type.</typeparam><param name="value">Borrowed value or default.</param><returns>Whether T matches.</returns>
    public bool TryGet<T>(out T value) { if (_value is Value<T> stored) { value = stored.Item; return true; } value = default!; return false; }
}
