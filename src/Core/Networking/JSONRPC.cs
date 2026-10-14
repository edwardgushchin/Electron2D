using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Electron2D;

/// <summary>Builds JSON-RPC 2.0 messages and synchronously dispatches explicitly registered typed methods.</summary>
/// <remarks>
/// Documents and text are caller-owned. This object opens no transport and performs no reflected CLR method lookup.
/// Handlers run on the calling thread and retain their scene and native ownership requirements.
/// Parsing, conversion and document snapshots allocate; use them outside real-time callbacks.
/// </remarks>
public sealed partial class JSONRPC : ElectronObject
{
    private static readonly JsonSerializerOptions WireOptions = new() { MaxDepth = 1024 };
    private readonly object _gate = new();
    private readonly Dictionary<string, Func<JsonNode?, JsonNode?>> _methods = new(StringComparer.Ordinal);

    /// <summary>Creates an empty caller-owned method dispatcher.</summary>
    public JSONRPC() { }

    /// <summary>Dispatches an independent snapshot of a JSON request or optionally a root batch.</summary>
    /// <param name="action">The caller-owned input document, whose tree is not modified.</param>
    /// <param name="recurse">Whether the root may be a batch. Nested arrays remain invalid requests.</param>
    /// <returns>A new caller-owned response document, or null when all actions are notifications.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The dispatcher or input document is disposed.</exception>
    public JSON? ProcessAction(JSON action, bool recurse = false)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(action);
        var result = Dispatch(action.Data?.DeepClone(), recurse);
        return result is null ? null : Document(result);
    }

    /// <summary>Parses strict wire JSON, dispatches a request or root batch and formats its response.</summary>
    /// <param name="action">The input JSON text.</param>
    /// <returns>Compact JSON, or empty text for empty input or notifications only.</returns>
    /// <remarks>Numeric ID tokens retain their original precision. Invalid JSON produces a ParseError response.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The dispatcher is disposed.</exception>
    public string ProcessString(string action)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(action);
        if (action.Length == 0) return string.Empty;
        JsonNode? input;
        try { input = JsonNode.Parse(action, documentOptions: new JsonDocumentOptions { MaxDepth = 1024 }); }
        catch (JsonException) { return Error(JSONRPCErrorCode.ParseError, "Parse error").ToJsonString(WireOptions); }
        return Dispatch(input, true)?.ToJsonString(WireOptions) ?? string.Empty;
    }

    /// <summary>Registers or replaces a method using a concrete positional-array or named-object parameter schema.</summary>
    /// <typeparam name="TParameters">The complete structured parameter model.</typeparam>
    /// <typeparam name="TResult">The concrete serializable result type.</typeparam>
    /// <param name="name">The case-sensitive wire name; rpc. names are reserved.</param>
    /// <param name="callback">The synchronous typed handler.</param>
    /// <param name="parametersTypeInfo">Complete compiled metadata when reflection is unavailable.</param>
    /// <param name="resultTypeInfo">Complete compiled result metadata when reflection is unavailable.</param>
    /// <remarks>The handler receives the whole parameter array or object. It validates positional arity itself.</remarks>
    /// <exception cref="NotSupportedException">A schema is untyped or contains engine objects.</exception>
    public void SetMethod<TParameters, TResult>(string name, Func<TParameters, TResult> callback,
        JsonTypeInfo<TParameters>? parametersTypeInfo = null, JsonTypeInfo<TResult>? resultTypeInfo = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(callback);
        var parameters = Prepare(parametersTypeInfo);
        var result = Prepare(resultTypeInfo);
        Register(name, node => JsonSerializer.SerializeToNode(callback(Decode(node, parameters)), result));
    }

    /// <summary>Registers or replaces a method without parameters.</summary>
    /// <typeparam name="TResult">The concrete serializable result type.</typeparam>
    /// <param name="name">The case-sensitive wire name.</param>
    /// <param name="callback">The synchronous result handler.</param>
    /// <param name="resultTypeInfo">Complete compiled result metadata when reflection is unavailable.</param>
    /// <remarks>Parameters must be absent or an empty object or array.</remarks>
    public void SetMethod<TResult>(string name, Func<TResult> callback, JsonTypeInfo<TResult>? resultTypeInfo = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(callback);
        var result = Prepare(resultTypeInfo);
        Register(name, node => { RequireEmpty(node); return JsonSerializer.SerializeToNode(callback(), result); });
    }

    /// <summary>Registers a typed action whose successful request result is JSON null.</summary>
    /// <typeparam name="TParameters">The complete structured parameter model.</typeparam>
    /// <param name="name">The case-sensitive wire name.</param>
    /// <param name="callback">The synchronous action.</param>
    /// <param name="parametersTypeInfo">Complete compiled parameter metadata when reflection is unavailable.</param>
    public void SetMethod<TParameters>(string name, Action<TParameters> callback, JsonTypeInfo<TParameters>? parametersTypeInfo = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(callback);
        var parameters = Prepare(parametersTypeInfo);
        Register(name, node => { callback(Decode(node, parameters)); return null; });
    }

    /// <summary>Registers a parameterless action whose successful request result is JSON null.</summary>
    /// <param name="name">The case-sensitive wire name.</param>
    /// <param name="callback">The synchronous action.</param>
    public void SetMethod(string name, Action callback)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(callback);
        Register(name, node => { RequireEmpty(node); callback(); return null; });
    }

    /// <summary>Registers an explicit JSON document handler for document-specific protocols.</summary>
    /// <param name="name">The case-sensitive wire name.</param>
    /// <param name="callback">Receives a temporary independent parameter document and returns a borrowed result document.</param>
    /// <remarks>The parameter document is disposed after invocation. The result is copied first; caller-owned results are not disposed.</remarks>
    public void SetMethod(string name, Func<JSON, JSON?> callback)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(callback);
        Register(name, node => { using var parameters = Document(node); return callback(parameters)?.Data?.DeepClone(); });
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) _methods.Clear();
        base.Dispose(disposing);
    }

    private void Register(string name, Func<JsonNode?, JsonNode?> callback)
    {
        CheckMethodName(name);
        lock (_gate) { ThrowIfDisposed(); _methods[name] = callback; }
    }

    private JsonNode? Dispatch(JsonNode? action, bool recurse)
    {
        ThrowIfDisposed();
        if (action is JsonArray batch && recurse)
        {
            if (batch.Count == 0) return Error(JSONRPCErrorCode.InvalidRequest, "Invalid Request");
            var results = new JsonArray();
            foreach (var item in batch)
            {
                var response = Dispatch(item, false);
                if (response is not null) results.Add(response);
            }
            return results.Count == 0 ? null : results;
        }
        if (action is not JsonObject request) return Error(JSONRPCErrorCode.InvalidRequest, "Invalid Request");
        JsonNode? id, parameters;
        string method;
        bool hasID;
        try
        {
            hasID = request.TryGetPropertyValue("id", out id);
            if (!IsID(id) || request["jsonrpc"] is not JsonValue version || !version.TryGetValue<string>(out var text) || text != "2.0"
                || request["method"] is not JsonValue methodValue || !methodValue.TryGetValue<string>(out method!))
                return Error(JSONRPCErrorCode.InvalidRequest, "Invalid Request");
            if (request.TryGetPropertyValue("params", out parameters) && parameters is not JsonArray and not JsonObject)
                return hasID ? Error(JSONRPCErrorCode.InvalidParams, "Invalid params", id) : null;
        }
        catch (ArgumentException) { return Error(JSONRPCErrorCode.InvalidRequest, "Invalid Request"); }
        Func<JsonNode?, JsonNode?>? callback;
        lock (_gate) { ThrowIfDisposed(); _methods.TryGetValue(method, out callback); }
        if (callback is null) return hasID ? Error(JSONRPCErrorCode.MethodNotFound, "Method not found: " + method, id) : null;
        JsonNode? result;
        try { result = callback(parameters); }
        catch (InvalidParametersException) { return hasID ? Error(JSONRPCErrorCode.InvalidParams, "Invalid params", id) : null; }
        catch (ArgumentException) { return hasID ? Error(JSONRPCErrorCode.InvalidParams, "Invalid params", id) : null; }
        catch (Exception) { return hasID ? Error(JSONRPCErrorCode.InternalError, "Internal error", id) : null; }
        try { _ = result?.ToJsonString(WireOptions); }
        catch (Exception) { return hasID ? Error(JSONRPCErrorCode.InternalError, "Internal error", id) : null; }
        return hasID ? Response(result, id) : null;
    }

    private static T Decode<T>(JsonNode? node, JsonTypeInfo<T> typeInfo)
    {
        if (node is null) throw new InvalidParametersException();
        try { return JsonSerializer.Deserialize(node.ToJsonString(WireOptions), typeInfo)!; }
        catch (JsonException) { throw new InvalidParametersException(); }
        catch (ArgumentException) { throw new InvalidParametersException(); }
    }

    private static void RequireEmpty(JsonNode? node)
    {
        if (node is null || node is JsonArray { Count: 0 } || node is JsonObject { Count: 0 }) return;
        throw new InvalidParametersException();
    }

    private static JsonTypeInfo<T> Prepare<T>(JsonTypeInfo<T>? typeInfo)
    {
        JSON.CheckNativeType<T>();
        return ConfigFile.CheckJSONTypeInfo(typeInfo ?? ConfigFile.GetJSONTypeInfo<T>());
    }

    private static void CheckMethodName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.StartsWith("rpc.", StringComparison.Ordinal)) throw new ArgumentException("The rpc. prefix is reserved.", nameof(name));
    }

    private static bool IsID(JsonNode? id)
    {
        if (id is null) return true;
        if (id.GetValueKind() == JsonValueKind.String) return true;
        if (id.GetValueKind() != JsonValueKind.Number) return false;
        try { _ = id.ToJsonString(WireOptions); return true; }
        catch (ArgumentException) { return false; }
        catch (JsonException) { return false; }
    }
    private static JSON Document(JsonNode? node) => new() { Data = node };
    private static JsonObject Response(JsonNode? result, JsonNode? id) => new() { ["jsonrpc"] = "2.0", ["id"] = id?.DeepClone(), ["result"] = result };
    private static JsonObject Error(JSONRPCErrorCode code, string message, JsonNode? id = null) => Error((int)code, message, id);
    private static JsonObject Error(int code, string message, JsonNode? id = null) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["error"] = new JsonObject { ["code"] = code, ["message"] = message },
    };

    private sealed class InvalidParametersException : Exception;
}
