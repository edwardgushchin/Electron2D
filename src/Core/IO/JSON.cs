using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Electron2D;

/// <summary>Parses and formats JSON data without introducing an engine-wide value container.</summary>
/// <remarks>
/// <see cref="JsonNode"/> values belong to this JSON boundary. They are mutable and must not be edited concurrently.
/// Configuration and project settings retain their separate typed value contracts.
/// </remarks>
public sealed class JSON : Resource
{
    private static readonly JsonSerializerOptions NativeOptions = CreateNativeOptions();
    private static readonly IComparer<string> KeyOrder = Comparer<string>.Create(CompareKeys);
    private readonly object _gate = new();
    private JsonNode? _data;
    private string _parsedText = string.Empty;
    private string _errorMessage = string.Empty;
    private int _errorLine;

    /// <summary>Creates an empty JSON resource.</summary>
    public JSON() { }

    /// <summary>Gets or replaces the live parsed JSON tree.</summary>
    /// <value>The parsed root, or null for JSON null, a failed parse, or an empty resource.</value>
    /// <remarks>
    /// The getter returns the owned mutable tree. Setting the property takes a deep copy and clears retained source text
    /// and diagnostics. Mutation through the returned tree is caller-owned and is not synchronized by this resource.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public JsonNode? Data
    {
        get { lock (_gate) { ThrowIfDisposed(); return _data; } }
        set
        {
            var copy = value?.DeepClone();
            lock (_gate)
            {
                ThrowIfDisposed();
                _data = copy;
                _parsedText = string.Empty;
                _errorMessage = string.Empty;
                _errorLine = 0;
            }
        }
    }

    /// <summary>Gets the zero-based source line of the most recent parse error, or zero after success.</summary>
    public int GetErrorLine() { lock (_gate) { ThrowIfDisposed(); return _errorLine; } }

    /// <summary>Gets the document parser's most recent error, or an empty string after success.</summary>
    public string GetErrorMessage() { lock (_gate) { ThrowIfDisposed(); return _errorMessage; } }

    /// <summary>Gets the last source supplied with text retention enabled.</summary>
    /// <remarks>Success and failure retain source when requested. A parse without retention or an explicit <see cref="Data"/> assignment clears it.</remarks>
    public string GetParsedText() { lock (_gate) { ThrowIfDisposed(); return _parsedText; } }

    /// <summary>Parses JSON text and updates the resource's root and diagnostics.</summary>
    /// <param name="jsonText">The JSON source; objects, arrays, and scalar roots are accepted.</param>
    /// <param name="keepText">Whether to retain the original source verbatim.</param>
    /// <returns>True on success; false when the text is malformed or exceeds the parser's limits.</returns>
    /// <remarks>Trailing commas, raw line breaks in strings and permissive number text are accepted. Number conversion uses at most 18 mantissa digits. Nesting is limited to 1024 levels. Diagnostics count literal line feeds.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="jsonText"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public bool Parse(string jsonText, bool keepText = false)
    {
        ArgumentNullException.ThrowIfNull(jsonText);
        var parser = new JSONDocumentParser(jsonText);
        var success = parser.TryParse(out var parsed);

        lock (_gate)
        {
            ThrowIfDisposed();
            _data = parsed;
            _parsedText = keepText ? jsonText : string.Empty;
            _errorLine = success ? 0 : parser.ErrorLine;
            _errorMessage = success ? string.Empty : parser.ErrorMessage;
        }
        return success;
    }

    /// <summary>Parses one document value with the same syntax as <see cref="Parse"/>, returning null for malformed text and for a valid JSON null.</summary>
    /// <param name="jsonText">The JSON source.</param>
    /// <returns>A mutable JSON tree, or null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="jsonText"/> is null.</exception>
    public static JsonNode? ParseString(string jsonText)
    {
        ArgumentNullException.ThrowIfNull(jsonText);
        return new JSONDocumentParser(jsonText).TryParse(out var parsed) ? parsed : null;
    }

    /// <summary>Formats a JSON tree, optionally sorting keys and inserting an arbitrary indent string.</summary>
    /// <param name="data">The JSON root; null emits the JSON literal null.</param>
    /// <param name="indent">Text repeated at each nesting level; empty emits compact JSON.</param>
    /// <param name="sortKeys">Whether object keys are sorted by Unicode scalar value.</param>
    /// <param name="fullPrecision">Whether floating values retain all round-trip digits.</param>
    /// <returns>Formatted document text containing the supplied value.</returns>
    /// <remarks>Parsed numbers use floating formatting; typed integer nodes keep integer text. Strings use document-specific escaping, including a vertical-tab escape.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="indent"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The JSON tree changes during traversal or exceeds 1024 nested levels.</exception>
    public static string Stringify(JsonNode? data, string indent = "", bool sortKeys = true, bool fullPrecision = false)
    {
        ArgumentNullException.ThrowIfNull(indent);
        var text = new StringBuilder();
        AppendValue(text, data, indent, sortKeys, fullPrecision, 0);
        return text.ToString();
    }

    /// <summary>Converts an explicitly typed native value into JSON-only data.</summary>
    /// <typeparam name="T">The concrete scalar, collection, or serializable model type.</typeparam>
    /// <param name="value">The native value.</param>
    /// <returns>An independent mutable JSON tree, or null for a null value.</returns>
    /// <remarks>Engine object identity is deliberately not serialized. The caller chooses the model type at compile time.</remarks>
    /// <exception cref="NotSupportedException">The type is an engine object or an untyped object container.</exception>
    /// <exception cref="JsonException">The value cannot be serialized.</exception>
    public static JsonNode? FromNative<T>(T value)
    {
        CheckNativeType<T>();
        return JsonSerializer.SerializeToNode(value, NativeOptions);
    }

    /// <summary>Converts JSON-only data into a caller-selected native type.</summary>
    /// <typeparam name="T">The concrete destination type.</typeparam>
    /// <param name="json">The JSON value.</param>
    /// <returns>A decoded native value, or the selected type's default for JSON null, including value types.</returns>
    /// <remarks>The generic destination type is required; arbitrary engine objects and runtime type names are not constructed from JSON.</remarks>
    /// <exception cref="NotSupportedException">The destination is an engine object or an untyped object container.</exception>
    /// <exception cref="JsonException">The JSON value does not match <typeparamref name="T"/>.</exception>
    public static T? ToNative<T>(JsonNode? json)
    {
        CheckNativeType<T>();
        return json is null ? default : JsonSerializer.Deserialize<T>(json.ToJsonString(), NativeOptions);
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new JSON();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            var copy = (JSON)target;
            lock (copy._gate)
            {
                copy._data = _data?.DeepClone();
                copy._parsedText = _parsedText;
                copy._errorLine = _errorLine;
                copy._errorMessage = _errorMessage;
            }
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) _data = null;
        base.Dispose(disposing);
    }

    private static void CheckNativeType<T>()
    {
        var type = typeof(T);
        if (type == typeof(object) || typeof(ElectronObject).IsAssignableFrom(type))
            throw new NotSupportedException("JSON native conversion requires a concrete non-engine type.");
    }

    private static JsonSerializerOptions CreateNativeOptions()
    {
        var options = new JsonSerializerOptions(ConfigFile.ValueJsonOptions);
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            if (info.Type == typeof(object) || typeof(ElectronObject).IsAssignableFrom(info.Type))
                throw new NotSupportedException("JSON native conversion requires concrete non-engine member types.");
        });
        options.TypeInfoResolver = resolver;
        return options;
    }

    private static void AppendValue(StringBuilder text, JsonNode? node, string indent, bool sortKeys, bool fullPrecision, int depth)
    {
        if (depth > 1024) throw new InvalidOperationException("JSON nesting exceeds 1024 levels.");
        if (node is null) { text.Append("null"); return; }
        if (node is JsonObject objectNode)
        {
            text.Append('{');
            IEnumerable<KeyValuePair<string, JsonNode?>> entries = sortKeys
                ? objectNode.OrderBy(entry => entry.Key, KeyOrder) : objectNode;
            var first = true;
            foreach (var entry in entries)
            {
                if (!first) text.Append(',');
                if (indent.Length != 0) { text.Append('\n'); AppendIndent(text, indent, depth + 1); }
                AppendQuotedString(text, entry.Key);
                text.Append(indent.Length == 0 ? ":" : ": ");
                AppendValue(text, entry.Value, indent, sortKeys, fullPrecision, depth + 1);
                first = false;
            }
            if (!first && indent.Length != 0) { text.Append('\n'); AppendIndent(text, indent, depth); }
            text.Append('}');
            return;
        }
        if (node is JsonArray array)
        {
            text.Append('[');
            for (var index = 0; index < array.Count; index++)
            {
                if (index != 0) text.Append(',');
                if (indent.Length != 0) { text.Append('\n'); AppendIndent(text, indent, depth + 1); }
                AppendValue(text, array[index], indent, sortKeys, fullPrecision, depth + 1);
            }
            if (array.Count != 0 && indent.Length != 0) { text.Append('\n'); AppendIndent(text, indent, depth); }
            text.Append(']');
            return;
        }

        var value = (JsonValue)node;
        if (value.TryGetValue<double>(out var number))
        {
            AppendNumber(text, number, fullPrecision);
        }
        else if (value.TryGetValue<float>(out var single))
        {
            AppendNumber(text, single, fullPrecision);
        }
        else if (value.TryGetValue<string>(out var stringValue)) AppendQuotedString(text, stringValue);
        else text.Append(value.ToJsonString());
    }

    private static void AppendNumber(StringBuilder text, double value, bool fullPrecision)
    {
        if (double.IsNaN(value)) { text.Append("null"); return; }
        if (double.IsPositiveInfinity(value)) { text.Append("1e99999"); return; }
        if (double.IsNegativeInfinity(value)) { text.Append("-1e99999"); return; }
        if (value == 0) { text.Append("0.0"); return; }

        if (fullPrecision)
        {
            AppendRoundTripNumber(text, value);
            return;
        }

        var precision = Math.Clamp(14 - (int)Math.Floor(Math.Log10(Math.Abs(value))), 1, 32);
        var formatted = value.ToString($"F{precision}", CultureInfo.InvariantCulture);
        var end = formatted.Length - 1;
        while (formatted[end] == '0' && formatted[end - 1] != '.') end--;
        text.Append(formatted.AsSpan(0, end + 1));
    }

    private static void AppendRoundTripNumber(StringBuilder text, double value)
    {
        var raw = value.ToString("R", CultureInfo.InvariantCulture);
        var negative = raw[0] == '-';
        var mantissaStart = negative ? 1 : 0;
        var exponentAt = raw.IndexOf('E');
        var mantissaEnd = exponentAt < 0 ? raw.Length : exponentAt;
        var exponent = exponentAt < 0 ? 0 : int.Parse(raw.AsSpan(exponentAt + 1), CultureInfo.InvariantCulture);
        var pointAt = raw.IndexOf('.', mantissaStart, mantissaEnd - mantissaStart);
        var integerDigits = (pointAt < 0 ? mantissaEnd : pointAt) - mantissaStart;
        var digits = raw.AsSpan(mantissaStart, mantissaEnd - mantissaStart).ToString().Replace(".", "", StringComparison.Ordinal);
        var leading = 0;
        while (digits[leading] == '0') leading++;
        var scientificExponent = integerDigits - leading - 1 + exponent;
        digits = digits[leading..].TrimEnd('0');
        if (negative) text.Append('-');

        var decimalPosition = scientificExponent + 1;
        if (decimalPosition > 15 || decimalPosition <= -4)
        {
            text.Append(digits[0]);
            if (digits.Length > 1) { text.Append('.'); text.Append(digits.AsSpan(1)); }
            text.Append('e');
            text.Append(scientificExponent < 0 ? '-' : '+');
            text.Append(Math.Abs(scientificExponent).ToString("D2", CultureInfo.InvariantCulture));
        }
        else if (decimalPosition <= 0)
        {
            text.Append("0.");
            text.Append('0', -decimalPosition);
            text.Append(digits);
        }
        else if (decimalPosition >= digits.Length)
        {
            text.Append(digits);
            text.Append('0', decimalPosition - digits.Length);
            text.Append(".0");
        }
        else
        {
            text.Append(digits.AsSpan(0, decimalPosition));
            text.Append('.');
            text.Append(digits.AsSpan(decimalPosition));
        }
    }

    private static void AppendQuotedString(StringBuilder text, string value)
    {
        text.Append('"');
        foreach (var character in value)
        {
            switch (character)
            {
                case '\\': text.Append("\\\\"); break;
                case '\b': text.Append("\\b"); break;
                case '\f': text.Append("\\f"); break;
                case '\n': text.Append("\\n"); break;
                case '\r': text.Append("\\r"); break;
                case '\t': text.Append("\\t"); break;
                case '\v': text.Append("\\v"); break;
                case '"': text.Append("\\\""); break;
                default: text.Append(character); break;
            }
        }
        text.Append('"');
    }

    private static int CompareKeys(string left, string right)
    {
        var leftRunes = left.EnumerateRunes();
        var rightRunes = right.EnumerateRunes();
        while (true)
        {
            var hasLeft = leftRunes.MoveNext();
            var hasRight = rightRunes.MoveNext();
            if (!hasLeft || !hasRight) return hasLeft.CompareTo(hasRight);
            var comparison = leftRunes.Current.Value.CompareTo(rightRunes.Current.Value);
            if (comparison != 0) return comparison;
        }
    }

    private static void AppendIndent(StringBuilder text, string indent, int depth)
    {
        for (var index = 0; index < depth; index++) text.Append(indent);
    }
}
