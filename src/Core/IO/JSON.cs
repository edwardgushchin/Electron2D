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
    private static readonly JsonDocumentOptions ParseOptions = new() { AllowTrailingCommas = true };
    private static readonly JsonSerializerOptions NativeOptions = CreateNativeOptions();
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

    /// <summary>Gets the zero-based line of the most recent parse error, or zero after success.</summary>
    public int GetErrorLine() { lock (_gate) { ThrowIfDisposed(); return _errorLine; } }

    /// <summary>Gets the most recent parse error, or an empty string after success.</summary>
    public string GetErrorMessage() { lock (_gate) { ThrowIfDisposed(); return _errorMessage; } }

    /// <summary>Gets the last source supplied with text retention enabled.</summary>
    /// <remarks>The text is empty after a parse without retention or an explicit <see cref="Data"/> assignment.</remarks>
    public string GetParsedText() { lock (_gate) { ThrowIfDisposed(); return _parsedText; } }

    /// <summary>Parses JSON text and updates the resource's root and diagnostics.</summary>
    /// <param name="jsonText">The JSON source; objects, arrays, and scalar roots are accepted.</param>
    /// <param name="keepText">Whether to retain the original source verbatim.</param>
    /// <returns>True on success; false when the text is malformed or exceeds the parser's limits.</returns>
    /// <remarks>Trailing commas are accepted. Other malformed syntax follows the managed JSON parser and is reported through the diagnostic methods.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="jsonText"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public bool Parse(string jsonText, bool keepText = false)
    {
        ArgumentNullException.ThrowIfNull(jsonText);
        JsonNode? parsed = null;
        JsonException? failure = null;
        try { parsed = JsonNode.Parse(jsonText, documentOptions: ParseOptions); }
        catch (JsonException error) { failure = error; }

        lock (_gate)
        {
            ThrowIfDisposed();
            _data = parsed;
            _parsedText = keepText ? jsonText : string.Empty;
            _errorLine = failure is null ? 0 : checked((int)(failure.LineNumber ?? 0));
            _errorMessage = failure?.Message ?? string.Empty;
        }
        return failure is null;
    }

    /// <summary>Parses one JSON value, returning null for malformed text and for a valid JSON null.</summary>
    /// <param name="jsonText">The JSON source.</param>
    /// <returns>A mutable JSON tree, or null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="jsonText"/> is null.</exception>
    public static JsonNode? ParseString(string jsonText)
    {
        ArgumentNullException.ThrowIfNull(jsonText);
        try { return JsonNode.Parse(jsonText, documentOptions: ParseOptions); }
        catch (JsonException) { return null; }
    }

    /// <summary>Formats a JSON tree, optionally sorting keys and inserting an arbitrary indent string.</summary>
    /// <param name="data">The JSON root; null emits the JSON literal null.</param>
    /// <param name="indent">Text repeated at each nesting level; empty emits compact JSON.</param>
    /// <param name="sortKeys">Whether object keys are sorted ordinally.</param>
    /// <param name="fullPrecision">Whether floating values created from native types retain round-trip precision.</param>
    /// <returns>A JSON document containing the supplied value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="indent"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The JSON tree changes during traversal or is too deep.</exception>
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
    /// <returns>A decoded native value, or default for JSON null.</returns>
    /// <remarks>The generic destination type is required; arbitrary engine objects and runtime type names are not constructed from JSON.</remarks>
    /// <exception cref="NotSupportedException">The destination is an engine object or an untyped object container.</exception>
    /// <exception cref="JsonException">The JSON value does not match <typeparamref name="T"/>.</exception>
    public static T? ToNative<T>(JsonNode? json)
    {
        CheckNativeType<T>();
        return JsonSerializer.Deserialize<T>(json?.ToJsonString() ?? "null", NativeOptions);
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
        if (depth > 128) throw new InvalidOperationException("JSON nesting exceeds 128 levels.");
        if (node is null) { text.Append("null"); return; }
        if (node is JsonObject objectNode)
        {
            text.Append('{');
            IEnumerable<KeyValuePair<string, JsonNode?>> entries = sortKeys
                ? objectNode.OrderBy(entry => entry.Key, StringComparer.Ordinal) : objectNode;
            var first = true;
            foreach (var entry in entries)
            {
                if (!first) text.Append(',');
                if (indent.Length != 0) { text.Append('\n'); AppendIndent(text, indent, depth + 1); }
                text.Append(JsonSerializer.Serialize(entry.Key));
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
            text.Append(double.IsNaN(number) ? "null" : double.IsPositiveInfinity(number) ? "1e99999" :
                double.IsNegativeInfinity(number) ? "-1e99999" :
                number.ToString(fullPrecision ? "R" : "G14", CultureInfo.InvariantCulture));
        }
        else if (value.TryGetValue<float>(out var single))
        {
            text.Append(float.IsNaN(single) ? "null" : float.IsPositiveInfinity(single) ? "1e99999" :
                float.IsNegativeInfinity(single) ? "-1e99999" :
                single.ToString(fullPrecision ? "R" : "G7", CultureInfo.InvariantCulture));
        }
        else text.Append(value.ToJsonString());
    }

    private static void AppendIndent(StringBuilder text, string indent, int depth)
    {
        for (var index = 0; index < depth; index++) text.Append(indent);
    }
}
