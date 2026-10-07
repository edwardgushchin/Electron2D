namespace Electron2D;

/// <summary>Represents one typed CodeEdit completion candidate.</summary>
/// <remarks>Text/configuration are immutable snapshots; the icon and an explicitly assigned generic value
/// remain borrowed. Use WithDefaultValue to attach a value without an untyped dynamic payload.</remarks>
public sealed class CodeCompletionOption
{
    private sealed record Value<T>(T Item);
    private readonly object? _value;
    /// <summary>Creates a candidate with no default payload.</summary><param name="kind">Defined candidate kind.</param><param name="displayText">Nonnull display text.</param><param name="insertText">Nonnull inserted text.</param><param name="fontColor">Finite display color or white.</param><param name="icon">Live borrowed texture or null.</param><param name="location">Local zero, ancestor distance 1 through 256, or a location sentinel.</param>
    public CodeCompletionOption(CodeEdit.CodeCompletionKind kind, string displayText, string insertText, Color? fontColor = null, Texture? icon = null, int location = (int)CodeEdit.CodeCompletionLocation.Other)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind)); ArgumentNullException.ThrowIfNull(displayText); ArgumentNullException.ThrowIfNull(insertText); if (fontColor is { } color && !color.IsFinite()) throw new ArgumentException("Color must be finite.", nameof(fontColor)); if (icon?.IsDisposed == true) throw new ObjectDisposedException(nameof(icon)); if (location < 0) throw new ArgumentOutOfRangeException(nameof(location));
        Kind = kind; DisplayText = displayText; InsertText = insertText; FontColor = fontColor ?? Colors.White; Icon = icon; Location = location;
    }
    private CodeCompletionOption(CodeCompletionOption option, object? value, string? display = null, string? insert = null) { Kind = option.Kind; DisplayText = display ?? option.DisplayText; InsertText = insert ?? option.InsertText; FontColor = option.FontColor; Icon = option.Icon; Location = option.Location; _value = value; }
    internal CodeCompletionOption WithText(string display, string insert) => new(this, _value, display, insert);
    /// <summary>Gets the candidate kind.</summary><value>Constructor kind.</value>
    public CodeEdit.CodeCompletionKind Kind { get; }
    /// <summary>Gets menu text.</summary><value>Immutable source text.</value>
    public string DisplayText { get; }
    /// <summary>Gets insertion text.</summary><value>Immutable source text.</value>
    public string InsertText { get; }
    /// <summary>Gets menu font color.</summary><value>White by default.</value>
    public Color FontColor { get; }
    /// <summary>Gets the borrowed icon.</summary><value>Texture or null.</value>
    public Texture? Icon { get; }
    /// <summary>Gets relative source location.</summary><value>Other by default; ancestor distance remains an integer.</value>
    public int Location { get; }
    /// <summary>Creates an independent candidate sharing configuration and a borrowed typed payload.</summary><typeparam name="T">Exact payload type.</typeparam><param name="value">Borrowed value.</param><returns>New candidate.</returns>
    public CodeCompletionOption WithDefaultValue<T>(T value) => new(this, new Value<T>(value));
    /// <summary>Tries to read the default value under its exact generic type.</summary><typeparam name="T">Stored type.</typeparam><param name="value">Stored value, or default on mismatch/absence.</param><returns>Whether the exact type is present.</returns>
    public bool TryGetDefaultValue<T>(out T value) { if (_value is Value<T> stored) { value = stored.Item; return true; } value = default!; return false; }
}
