using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Electron2D;

public sealed partial class JSONRPC
{
    /// <summary>Creates a request by copying parameter and ID documents.</summary>
    /// <param name="method">The case-sensitive wire method.</param>
    /// <param name="parameters">An object or array; null omits parameters.</param>
    /// <param name="id">A string, number or JSON null; null emits an explicit null ID.</param>
    /// <returns>A new caller-owned request document.</returns>
    /// <exception cref="ArgumentException">The parameters or ID have an invalid JSON kind, or the method is reserved.</exception>
    public JSON MakeRequest(string method, JSON? parameters, JSON? id)
    {
        ThrowIfDisposed();
        return Document(Request(method, parameters?.Data?.DeepClone(), true, id?.Data?.DeepClone()));
    }

    /// <summary>Creates a request using concrete native parameter and ID schemas.</summary>
    /// <typeparam name="TParameters">The structured parameter model.</typeparam>
    /// <typeparam name="TID">A concrete string, numeric or nullable ID type.</typeparam>
    /// <param name="method">The wire method.</param>
    /// <param name="parameters">The complete parameter object or positional array.</param>
    /// <param name="id">The correlation ID.</param>
    /// <param name="parametersTypeInfo">Optional compiled parameter metadata.</param>
    /// <param name="idTypeInfo">Optional compiled ID metadata.</param>
    /// <returns>A new caller-owned document snapshot.</returns>
    public JSON MakeRequest<TParameters, TID>(string method, TParameters parameters, TID id,
        JsonTypeInfo<TParameters>? parametersTypeInfo = null, JsonTypeInfo<TID>? idTypeInfo = null)
    {
        ThrowIfDisposed();
        return Document(Request(method, Encode(parameters, parametersTypeInfo), true, Encode(id, idTypeInfo)));
    }

    /// <summary>Creates a notification by copying a structured parameter document.</summary>
    /// <param name="method">The wire method.</param>
    /// <param name="parameters">An object or array; null omits parameters.</param>
    /// <returns>A new caller-owned document without an ID.</returns>
    public JSON MakeNotification(string method, JSON? parameters)
    {
        ThrowIfDisposed();
        return Document(Request(method, parameters?.Data?.DeepClone(), false, null));
    }

    /// <summary>Creates a notification from a concrete native parameter schema.</summary>
    /// <typeparam name="TParameters">The structured parameter model.</typeparam>
    /// <param name="method">The wire method.</param>
    /// <param name="parameters">The whole positional array or named object.</param>
    /// <param name="parametersTypeInfo">Optional compiled parameter metadata.</param>
    /// <returns>A new caller-owned document without an ID.</returns>
    public JSON MakeNotification<TParameters>(string method, TParameters parameters, JsonTypeInfo<TParameters>? parametersTypeInfo = null)
    {
        ThrowIfDisposed();
        return Document(Request(method, Encode(parameters, parametersTypeInfo), false, null));
    }

    /// <summary>Creates a success response by copying result and ID documents.</summary>
    /// <param name="result">Any JSON document value, including null.</param>
    /// <param name="id">A string, number or JSON null.</param>
    /// <returns>A new caller-owned response document.</returns>
    public JSON MakeResponse(JSON? result, JSON? id)
    {
        ThrowIfDisposed();
        var identity = id?.Data?.DeepClone();
        CheckID(identity);
        return Document(Response(result?.Data?.DeepClone(), identity));
    }

    /// <summary>Creates a success response from concrete result and ID schemas.</summary>
    /// <typeparam name="TResult">The concrete result model.</typeparam>
    /// <typeparam name="TID">The string, numeric or nullable ID type.</typeparam>
    /// <param name="result">The result value.</param>
    /// <param name="id">The correlation ID.</param>
    /// <param name="resultTypeInfo">Optional compiled result metadata.</param>
    /// <param name="idTypeInfo">Optional compiled ID metadata.</param>
    /// <returns>A new caller-owned response snapshot.</returns>
    public JSON MakeResponse<TResult, TID>(TResult result, TID id,
        JsonTypeInfo<TResult>? resultTypeInfo = null, JsonTypeInfo<TID>? idTypeInfo = null)
    {
        ThrowIfDisposed();
        var identity = Encode(id, idTypeInfo);
        CheckID(identity);
        return Document(Response(Encode(result, resultTypeInfo), identity));
    }

    /// <summary>Creates an error response with a standard or application-defined integer code.</summary>
    /// <param name="code">The protocol or application error code.</param>
    /// <param name="message">The public error description.</param>
    /// <param name="id">The copied correlation ID document, or null for an unknown ID.</param>
    /// <returns>A new caller-owned error document.</returns>
    public JSON MakeResponseError(int code, string message, JSON? id = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(message);
        var identity = id?.Data?.DeepClone();
        CheckID(identity);
        return Document(Error(code, message, identity));
    }

    /// <summary>Creates an error response using a standard protocol error identity.</summary>
    /// <param name="code">One of the five defined protocol errors.</param>
    /// <param name="message">The public error description.</param>
    /// <param name="id">The copied correlation ID document, or null for an unknown ID.</param>
    /// <returns>A new caller-owned error document.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="code"/> is undefined.</exception>
    public JSON MakeResponseError(JSONRPCErrorCode code, string message, JSON? id = null)
    {
        ThrowIfDisposed();
        if (!Enum.IsDefined(code)) throw new ArgumentOutOfRangeException(nameof(code));
        return MakeResponseError((int)code, message, id);
    }

    /// <summary>Creates an error response from a concrete ID schema.</summary>
    /// <typeparam name="TID">The string, numeric or nullable ID type.</typeparam>
    /// <param name="code">The standard or application error code.</param>
    /// <param name="message">The public description.</param>
    /// <param name="id">The correlation ID.</param>
    /// <param name="idTypeInfo">Optional compiled ID metadata.</param>
    /// <returns>A new caller-owned error snapshot.</returns>
    public JSON MakeResponseError<TID>(int code, string message, TID id, JsonTypeInfo<TID>? idTypeInfo = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(message);
        var identity = Encode(id, idTypeInfo);
        CheckID(identity);
        return Document(Error(code, message, identity));
    }

    private static JsonNode? Encode<T>(T value, JsonTypeInfo<T>? typeInfo) => JsonSerializer.SerializeToNode(value, Prepare(typeInfo));

    private static JsonObject Request(string method, JsonNode? parameters, bool hasID, JsonNode? id)
    {
        CheckMethodName(method);
        CheckID(id);
        if (parameters is not null and not JsonArray and not JsonObject) throw new ArgumentException("Parameters must be an array or object.", nameof(parameters));
        var request = new JsonObject { ["jsonrpc"] = "2.0", ["method"] = method };
        if (parameters is not null) request["params"] = parameters;
        if (hasID) request["id"] = id;
        return request;
    }

    private static void CheckID(JsonNode? id)
    {
        if (!IsID(id)) throw new ArgumentException("An ID must be a string, number or JSON null.", nameof(id));
    }
}
