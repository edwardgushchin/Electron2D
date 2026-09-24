using System.Globalization;
using System.Text;

namespace Electron2D;

/// <summary>Reads XML tokens from an owned UTF-8 byte buffer.</summary>
/// <remarks>This is a forward-only, permissive token reader. It does not validate document structure or expand external entities.
/// Byte offsets refer to the input buffer; instance operations are serialized and may allocate.</remarks>
public sealed class XMLParser : ElectronObject
{
    /// <summary>Identifies the current XML token.</summary>
    public enum NodeType
    {
        /// <summary>No token has been read.</summary>
        None = 0,
        /// <summary>An opening element.</summary>
        Element = 1,
        /// <summary>A closing element.</summary>
        ElementEnd = 2,
        /// <summary>Text between elements.</summary>
        Text = 3,
        /// <summary>A comment or declaration beginning with an exclamation mark.</summary>
        Comment = 4,
        /// <summary>A CDATA section.</summary>
        CDATA = 5,
        /// <summary>A processing instruction or other question-mark declaration.</summary>
        Unknown = 6,
    }

    private readonly object _gate = new();
    private readonly List<(string Name, string Value)> _attributes = [];
    private byte[]? _data;
    private int _position;
    private int _currentLine;
    private long _nodeOffset;
    private NodeType _nodeType;
    private string _nodeText = string.Empty;
    private bool _empty;

    /// <summary>Creates an unopened parser.</summary>
    public XMLParser() { }

    /// <summary>Copies a nonempty buffer and resets its byte cursor and line counter.</summary>
    /// <param name="buffer">UTF-8 XML bytes; the caller retains ownership.</param>
    /// <remarks>The last token and attributes remain observable immediately after opening, before the next <see cref="Read"/>. Invalid input leaves the previous source intact.</remarks>
    /// <exception cref="ArgumentException">The buffer is empty.</exception>
    /// <exception cref="ArgumentNullException">The buffer is null.</exception>
    /// <exception cref="ObjectDisposedException">The parser is disposed.</exception>
    public void OpenBuffer(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (buffer.Length == 0) throw new ArgumentException("The XML buffer cannot be empty.", nameof(buffer));
        lock (_gate)
        {
            ThrowIfDisposed();
            _data = (byte[])buffer.Clone();
            _position = 0;
            _currentLine = 0;
        }
    }

    /// <summary>Reads an ordinary or directory-backed virtual file and resets its byte cursor and line counter.</summary>
    /// <param name="path">An operating-system, res://, or user:// path.</param>
    /// <remarks>A failed open preserves the previous source and token. A successful open retains the prior token immediately after opening, before the next <see cref="Read"/>.</remarks>
    /// <exception cref="IOException">The file cannot be read or is empty.</exception>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="ObjectDisposedException">The parser is disposed.</exception>
    public void Open(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        ThrowIfDisposed();
        var buffer = FileAccess.GetFileAsBytes(path);
        if (buffer.Length == 0) throw new IOException("The XML file is empty.");
        OpenBuffer(buffer);
    }

    /// <summary>Reads the next token.</summary>
    /// <returns>True when a token was read; false at end of input.</returns>
    /// <exception cref="InvalidOperationException">No buffer is open.</exception>
    /// <exception cref="ObjectDisposedException">The parser is disposed.</exception>
    public bool Read()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            var data = _data ?? throw new InvalidOperationException("No XML buffer is open.");
            if (AtEnd(data)) return false;

            _nodeOffset = _position;
            _nodeType = NodeType.None;
            _nodeText = string.Empty;
            _empty = false;

            var textStart = _position;
            while (!AtEnd(data) && data[_position] != (byte)'<') Next(data);
            if (_position != textStart)
            {
                var count = _position - textStart;
                var hasContent = false;
                for (var i = textStart; i < _position; i++)
                    if (!IsWhitespace(data[i])) { hasContent = true; break; }
                if (count >= 3 || hasContent)
                {
                    _nodeType = NodeType.Text;
                    _nodeText = Decode(data, textStart, count, unescape: true);
                    return true;
                }
            }

            if (AtEnd(data)) return false;
            Next(data); // '<'
            if (AtEnd(data)) return false;
            switch (data[_position])
            {
                case (byte)'/': ReadEnd(data); break;
                case (byte)'?': ReadUnknown(data); break;
                case (byte)'!': ReadCommentOrCDATA(data); break;
                default: ReadElement(data); break;
            }
            return true;
        }
    }

    /// <summary>Reads the token beginning at a byte offset.</summary>
    /// <param name="position">A zero-based offset inside the current buffer.</param>
    /// <returns>True when a token was read.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The offset is outside the buffer.</exception>
    /// <exception cref="InvalidOperationException">No buffer is open.</exception>
    /// <exception cref="ObjectDisposedException">The parser is disposed.</exception>
    public bool Seek(long position)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            var data = _data ?? throw new InvalidOperationException("No XML buffer is open.");
            if (position < 0 || position >= data.Length) throw new ArgumentOutOfRangeException(nameof(position));
            _position = (int)position;
            return Read();
        }
    }

    /// <summary>Advances past the closing token of the current nonempty element.</summary>
    /// <remarks>Leaves the closing token current; stops at EOF if none exists.</remarks>
    public void SkipSection()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_nodeType != NodeType.Element || _empty) return;
            var depth = 1;
            while (depth > 0 && Read())
            {
                if (_nodeType == NodeType.Element && !_empty) depth++;
                else if (_nodeType == NodeType.ElementEnd) depth--;
            }
        }
    }

    /// <summary>Gets the current token type.</summary>
    /// <returns>The current token identity, or None on a new parser before its first read.</returns>
    public NodeType GetNodeType() { lock (_gate) { ThrowIfDisposed(); return _nodeType; } }

    /// <summary>Gets the current name or markup content; reports an error and returns empty for text tokens.</summary>
    /// <returns>The node name or content, or empty text for a text token.</returns>
    public string GetNodeName()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_nodeType != NodeType.Text) return _nodeText;
        }
        System.Diagnostics.Trace.TraceError("A text XML node has no node name.");
        return string.Empty;
    }

    /// <summary>Gets the current text content; reports an error and returns empty for other token types.</summary>
    /// <returns>Decoded text, or an empty string for another token type.</returns>
    public string GetNodeData()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_nodeType == NodeType.Text) return _nodeText;
        }
        System.Diagnostics.Trace.TraceError("The current XML node is not text.");
        return string.Empty;
    }

    /// <summary>Gets the byte offset at which the current read started.</summary>
    /// <returns>A zero-based byte offset.</returns>
    public long GetNodeOffset() { lock (_gate) { ThrowIfDisposed(); return _nodeOffset; } }

    /// <summary>Gets the zero-based count of consumed newline bytes.</summary>
    /// <returns>The number of newline bytes consumed since opening.</returns>
    public int GetCurrentLine() { lock (_gate) { ThrowIfDisposed(); return _currentLine; } }

    /// <summary>Gets whether the current element has a self-closing slash.</summary>
    /// <returns>True for a self-closing opening element.</returns>
    public bool IsEmpty() { lock (_gate) { ThrowIfDisposed(); return _empty; } }

    /// <summary>Gets the number of attributes of the current or last element.</summary>
    /// <returns>The attribute count.</returns>
    /// <remarks>Text, comment, CDATA and unknown tokens retain the last element's attribute list until another element is read.</remarks>
    public int GetAttributeCount() { lock (_gate) { ThrowIfDisposed(); return _attributes.Count; } }

    /// <summary>Gets an attribute name by its order in the opening element.</summary>
    /// <param name="index">Zero-based attribute index.</param>
    /// <returns>The attribute name, or empty text with a trace diagnostic for an invalid index.</returns>
    public string GetAttributeName(int index)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if ((uint)index < (uint)_attributes.Count) return _attributes[index].Name;
        }
        System.Diagnostics.Trace.TraceError("XML attribute index is out of range.");
        return string.Empty;
    }

    /// <summary>Gets an attribute value by its order in the opening element.</summary>
    /// <param name="index">Zero-based attribute index.</param>
    /// <returns>The decoded attribute value, or empty text with a trace diagnostic for an invalid index.</returns>
    public string GetAttributeValue(int index)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if ((uint)index < (uint)_attributes.Count) return _attributes[index].Value;
        }
        System.Diagnostics.Trace.TraceError("XML attribute index is out of range.");
        return string.Empty;
    }

    /// <summary>Reports whether the current opening element contains an exact attribute name.</summary>
    /// <param name="name">Case-sensitive attribute name.</param>
    /// <returns>True if an attribute with this exact name exists.</returns>
    public bool HasAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (_gate) { ThrowIfDisposed(); return _attributes.Any(attribute => attribute.Name == name); }
    }

    /// <summary>Gets a named attribute, reporting an error and returning empty when it is absent.</summary>
    /// <param name="name">Case-sensitive attribute name.</param>
    /// <returns>The first value with the supplied name, or empty text with a trace diagnostic when absent.</returns>
    public string GetNamedAttributeValue(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (_gate)
        {
            ThrowIfDisposed();
            foreach (var attribute in _attributes)
                if (attribute.Name == name) return attribute.Value;
        }
        System.Diagnostics.Trace.TraceError($"XML attribute '{name}' was not found.");
        return string.Empty;
    }

    /// <summary>Gets a named attribute, or empty text when it is absent.</summary>
    /// <param name="name">Case-sensitive attribute name.</param>
    /// <returns>The first value with the supplied name, or empty text.</returns>
    public string GetNamedAttributeValueSafe(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (_gate)
        {
            ThrowIfDisposed();
            foreach (var attribute in _attributes)
                if (attribute.Name == name) return attribute.Value;
            return string.Empty;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_gate)
            {
                _data = null;
                _attributes.Clear();
                ResetCursor();
            }
        }
        base.Dispose(disposing);
    }

    private bool AtEnd(byte[] data) => _position >= data.Length || data[_position] == 0;

    private void Next(byte[] data)
    {
        if (_position < data.Length && data[_position] == (byte)'\n') _currentLine++;
        _position++;
    }

    private void ResetCursor()
    {
        _position = 0;
        _currentLine = 0;
        _nodeOffset = 0;
        _nodeType = NodeType.None;
        _nodeText = string.Empty;
        _empty = false;
        _attributes.Clear();
    }

    private void ReadEnd(byte[] data)
    {
        _nodeType = NodeType.ElementEnd;
        _attributes.Clear();
        Next(data);
        var start = _position;
        while (!AtEnd(data) && data[_position] != (byte)'>') Next(data);
        _nodeText = Decode(data, start, _position - start);
        if (!AtEnd(data)) Next(data);
    }

    private void ReadUnknown(byte[] data)
    {
        _nodeType = NodeType.Unknown;
        var start = _position;
        while (!AtEnd(data) && data[_position] != (byte)'>') Next(data);
        _nodeText = Decode(data, start, _position - start);
        if (!AtEnd(data)) Next(data);
    }

    private void ReadCommentOrCDATA(byte[] data)
    {
        _nodeType = NodeType.Comment;
        Next(data); // '!'
        if (_position + 6 < data.Length && data.AsSpan(_position).StartsWith("[CDATA["u8))
        {
            _nodeType = NodeType.CDATA;
            _position += 7;
            var start = _position;
            while (!AtEnd(data) && !data.AsSpan(_position).StartsWith("]]>"u8)) Next(data);
            _nodeText = Decode(data, start, _position - start);
            if (!AtEnd(data)) { Next(data); Next(data); Next(data); }
            return;
        }

        var comment = data.AsSpan(_position).StartsWith("--"u8);
        if (comment) { Next(data); Next(data); }
        var begin = _position;
        if (comment)
        {
            while (!AtEnd(data) && !data.AsSpan(_position).StartsWith("-->"u8)) Next(data);
            _nodeText = Decode(data, begin, _position - begin);
            if (!AtEnd(data)) { Next(data); Next(data); Next(data); }
        }
        else
        {
            var depth = 1;
            while (!AtEnd(data) && depth > 0)
            {
                if (data[_position] == (byte)'<') depth++;
                else if (data[_position] == (byte)'>') depth--;
                Next(data);
            }
            _nodeText = Decode(data, begin, _position - begin - (depth == 0 ? 1 : 0));
        }
    }

    private void ReadElement(byte[] data)
    {
        _nodeType = NodeType.Element;
        _attributes.Clear();
        var nameStart = _position;
        while (!AtEnd(data) && data[_position] != (byte)'>' && !IsWhitespace(data[_position])) Next(data);
        var nameEnd = _position;
        if (nameEnd > nameStart && data[nameEnd - 1] == (byte)'/') { _empty = true; nameEnd--; }
        _nodeText = Decode(data, nameStart, nameEnd - nameStart);

        while (!AtEnd(data) && data[_position] != (byte)'>')
        {
            if (IsWhitespace(data[_position])) { Next(data); continue; }
            if (data[_position] == (byte)'/') { _empty = true; Next(data); continue; }
            var attributeStart = _position;
            while (!AtEnd(data) && data[_position] != (byte)'>' && data[_position] != (byte)'=' && !IsWhitespace(data[_position])) Next(data);
            var attributeEnd = _position;
            while (!AtEnd(data) && data[_position] != (byte)'>' && data[_position] != (byte)'\'' && data[_position] != (byte)'"') Next(data);
            if (AtEnd(data) || data[_position] == (byte)'>') break;
            var quote = data[_position];
            Next(data);
            var valueStart = _position;
            while (!AtEnd(data) && data[_position] != quote) Next(data);
            var value = Decode(data, valueStart, _position - valueStart, unescape: true);
            if (!AtEnd(data)) Next(data);
            _attributes.Add((Decode(data, attributeStart, attributeEnd - attributeStart), value));
        }
        if (!AtEnd(data)) Next(data);
    }

    private static bool IsWhitespace(byte value) => value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n';

    private static string Decode(byte[] data, int start, int count, bool unescape = false)
    {
        var value = Encoding.UTF8.GetString(data, start, count);
        return unescape ? Unescape(value) : value;
    }

    private static string Unescape(string value)
    {
        if (!value.Contains('&')) return value;
        var result = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '&') { result.Append(value[i]); continue; }
            var end = value.IndexOf(';', i + 1);
            if (end < 0) { result.Append('&'); continue; }
            var entity = value.AsSpan(i + 1, end - i - 1);
            string? replacement = entity switch
            {
                "amp" => "&",
                "lt" => "<",
                "gt" => ">",
                "quot" => "\"",
                "apos" => "'",
                _ => null,
            };
            if (replacement is null && entity.Length > 1 && entity[0] == '#')
            {
                var hex = entity.Length > 2 && (entity[1] is 'x' or 'X');
                var digits = entity[(hex ? 2 : 1)..];
                if (int.TryParse(digits, hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None,
                        CultureInfo.InvariantCulture, out var scalar) && Rune.IsValid(scalar))
                    replacement = char.ConvertFromUtf32(scalar);
            }
            if (replacement is null) { result.Append('&'); continue; }
            result.Append(replacement);
            i = end;
        }
        return result.ToString();
    }
}
