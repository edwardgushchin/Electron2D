using Electron2D;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

internal static class JSONRPCTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static JsonNode Parse(string text) => JsonNode.Parse(text) ?? throw new InvalidOperationException("Missing response.");
    private static void Code(string response, JSONRPCErrorCode code) => Check(Parse(response)["error"]!["code"]!.GetValue<int>() == (int)code, "Protocol error " + code);
    private static string Request(string method, string parameters = "[]", string id = "1") => "{\"jsonrpc\":\"2.0\",\"method\":\"" + method + "\",\"params\":" + parameters + ",\"id\":" + id + "}";

    internal static void Run()
    {
        using var rpc = new JSONRPC();
        rpc.SetMethod<int[], int>("subtract", values => { if (values.Length != 2) throw new ArgumentException("Arity."); return values[0] - values[1]; });
        rpc.SetMethod<JSONRPCNamedParameters, int>("named", p => p.Minuend - p.Subtrahend, JSONRPCModels.Default.JSONRPCNamedParameters, JSONRPCModels.Default.Int32);
        rpc.SetMethod<int[], int>("sum", values => values.Sum());
        rpc.SetMethod<string[]>("data", () => ["hello", "world"]);
        var calls = 0;
        rpc.SetMethod<int[]>("update", values => calls += values.Length);
        rpc.SetMethod("tick", () => { calls++; });
        Check(Parse(rpc.ProcessString(Request("subtract", "[42,23]")))["result"]!.GetValue<int>() == 19, "Positional call.");
        Check(Parse(rpc.ProcessString(Request("named", "{\"subtrahend\":23,\"minuend\":42}")))["result"]!.GetValue<int>() == 19, "Compiled named schema.");
        Check(Parse(rpc.ProcessString(Request("data")))["result"]!.AsArray().Count == 2, "Parameterless result.");
        Check(rpc.ProcessString("{\"jsonrpc\":\"2.0\",\"method\":\"update\",\"params\":[1,2,3]}") == "" && calls == 3, "Notifications execute without response.");
        Check(rpc.ProcessString("{\"jsonrpc\":\"2.0\",\"method\":\"tick\"}") == "" && calls == 4, "Absent parameters.");
        Check(rpc.ProcessString("{\"jsonrpc\":\"2.0\",\"method\":\"missing\"}") == "", "Unknown notification suppressed.");
        var nullID = Parse(rpc.ProcessString(Request("tick", "[]", "null"))).AsObject();
        Check(nullID.ContainsKey("id") && nullID["id"] is null && nullID.ContainsKey("result") && nullID["result"] is null, "Explicit null ID receives null result.");
        foreach (var id in new[] { "9007199254740993", "1.2345678901234567890123456789", "1e999", "\"café🌏\"" })
            Check(Parse(rpc.ProcessString(Request("subtract", "[5,2]", id)))["id"]!.ToJsonString() == id.Replace("café🌏", "caf\\u00E9\\uD83C\\uDF0F"), "Exact ID token/Unicode correlation: " + id);
        Code(rpc.ProcessString("{"), JSONRPCErrorCode.ParseError);
        Code(rpc.ProcessString(" "), JSONRPCErrorCode.ParseError);
        Check(rpc.ProcessString("") == "", "Empty transport text.");
        foreach (var text in new[] { "null", "3", "[]", "{}", "{\"method\":\"tick\"}", "{\"jsonrpc\":1,\"method\":\"tick\"}", "{\"jsonrpc\":\"1.0\",\"method\":\"tick\"}", "{\"jsonrpc\":\"2.0\",\"method\":false}", Request("tick", "[]", "true"), Request("tick", "[]", "{}"), "{\"jsonrpc\":\"2.0\",\"jsonrpc\":\"2.0\",\"method\":\"tick\"}" })
            Code(rpc.ProcessString(text), JSONRPCErrorCode.InvalidRequest);
        Code(rpc.ProcessString(Request("missing")), JSONRPCErrorCode.MethodNotFound);
        Code(rpc.ProcessString(Request("Subtract", "[5,2]")), JSONRPCErrorCode.MethodNotFound);
        foreach (var text in new[] { Request("subtract", "[1]"), Request("subtract", "[1,\"bad\"]"), Request("subtract", "{}"), Request("named", "{}"), Request("tick", "[1]"), Request("tick", "null"), Request("sum", "true") })
            Code(rpc.ProcessString(text), JSONRPCErrorCode.InvalidParams);
        Check(rpc.ProcessString("{\"jsonrpc\":\"2.0\",\"method\":\"subtract\",\"params\":false}") == "", "Invalid notification parameters suppressed.");
        rpc.SetMethod("fail", () => { throw new InvalidOperationException("private exception text"); });
        var error = rpc.ProcessString(Request("fail"));
        Code(error, JSONRPCErrorCode.InternalError); Check(!error.Contains("private", StringComparison.Ordinal), "Exception text is not a wire result.");
        Check(rpc.ProcessString("{\"jsonrpc\":\"2.0\",\"method\":\"fail\"}") == "", "Failed notification suppressed.");
        var batch = "[" + Request("sum", "[1,2,4]", "\"1\"") + ",{\"jsonrpc\":\"2.0\",\"method\":\"tick\"}," + Request("subtract", "[42,23]", "\"2\"") + ",1,[]," + Request("missing", "[]", "\"5\"") + "]";
        var responses = Parse(rpc.ProcessString(batch)).AsArray();
        Check(responses.Count == 5 && responses[0]!["result"]!.GetValue<int>() == 7 && responses[1]!["result"]!.GetValue<int>() == 19 && responses[2]!["error"]!["code"]!.GetValue<int>() == -32600, "Ordered mixed root batch; nested batch invalid.");
        Check(rpc.ProcessString("[{\"jsonrpc\":\"2.0\",\"method\":\"tick\"},{\"jsonrpc\":\"2.0\",\"method\":\"fail\"}]") == "", "Notification-only batch has no empty response array.");
        using var input = new JSON { Data = JsonNode.Parse(batch) };
        var before = input.Data!.ToJsonString();
        using var single = rpc.ProcessAction(input);
        Check(single!.Data!["error"]!["code"]!.GetValue<int>() == -32600, "Batch switch defaults false.");
        using var multi = rpc.ProcessAction(input, true);
        Check(multi!.Data!.AsArray().Count == 5 && input.Data!.ToJsonString() == before, "Document dispatch preserves input ownership.");
        using var parameters = new JSON { Data = JSON.FromNative(new[] { 9, 4 }) };
        using var identity = new JSON { Data = JSON.FromNative("local") };
        using var made = rpc.MakeRequest("subtract", parameters, identity);
        parameters.Data!.AsArray()[0] = 999;
        identity.Data = JSON.FromNative("changed");
        using var madeResult = rpc.ProcessAction(made);
        Check(madeResult!.Data!["result"]!.GetValue<int>() == 5 && madeResult.Data["id"]!.GetValue<string>() == "local", "Makers snapshot parameter and ID documents.");
        using var typed = rpc.MakeRequest("named", new JSONRPCNamedParameters { Minuend = 8, Subtrahend = 3 }, 71L, JSONRPCModels.Default.JSONRPCNamedParameters);
        using var typedResult = rpc.ProcessAction(typed);
        Check(typedResult!.Data!["result"]!.GetValue<int>() == 5, "Typed maker and compiled model metadata.");
        using var notification = rpc.MakeNotification("sum", new[] { 1, 2 });
        Check(rpc.ProcessAction(notification) is null, "Typed notification maker.");
        using var rawNotification = rpc.MakeNotification("tick", (JSON?)null);
        Check(rpc.ProcessAction(rawNotification) is null, "Raw notification maker.");
        using var success = rpc.MakeResponse(new[] { 1, 2 }, "result");
        using var rawSuccess = rpc.MakeResponse(parameters, null);
        using var custom = rpc.MakeResponseError(123, "application", "id");
        using var unknown = rpc.MakeResponseError(-32001, "unknown");
        Check(success.Data!["result"]!.AsArray().Count == 2 && rawSuccess.Data!["id"] is null && custom.Data!["error"]!["code"]!.GetValue<int>() == 123 && unknown.Data!.AsObject().ContainsKey("id"), "All response makers and application errors.");
        foreach (var code in Enum.GetValues<JSONRPCErrorCode>()) { using var standard = rpc.MakeResponseError(code, "standard"); Check(standard.Data!["error"]!["code"]!.GetValue<int>() == (int)code, "Typed standard error maker."); }
        Reject<ArgumentOutOfRangeException>(() => rpc.MakeResponseError((JSONRPCErrorCode)1, "invalid"));
        Reject<ArgumentException>(() => rpc.MakeRequest("sum", 1, 3));
        Reject<ArgumentException>(() => rpc.MakeRequest("sum", new[] { 1 }, true));
        Reject<ArgumentException>(() => rpc.SetMethod("rpc.internal", () => { }));
        Reject<NotSupportedException>(() => rpc.SetMethod<object, int>("untyped", _ => 1));
        Reject<NotSupportedException>(() => rpc.SetMethod<int[], Node>("engine", _ => new Node()));
        Reject<ArgumentNullException>(() => rpc.ProcessString(null!));
        Reject<ArgumentNullException>(() => rpc.ProcessAction(null!));
        using var nonFinite = new JSON { Data = JsonValue.Create(double.NaN) };
        Reject<ArgumentException>(() => rpc.MakeResponse(null, nonFinite));
        JSON? borrowedParameters = null;
        rpc.SetMethod("echo", (JSON document) => { borrowedParameters = document; return document; });
        var echo = Parse(rpc.ProcessString(Request("echo", "{\"x\":5}")));
        Check(echo["result"]!["x"]!.GetValue<int>() == 5 && borrowedParameters!.IsDisposed, "Temporary document result copied before retirement.");
        using var badResult = new JSON { Data = JsonValue.Create(double.NaN) };
        rpc.SetMethod("badResult", (JSON _) => badResult);
        Code(rpc.ProcessString(Request("badResult")), JSONRPCErrorCode.InternalError);
        Check(!badResult.IsDisposed, "Borrowed callback results remain owned by caller.");
        rpc.SetMethod<int>("replacement", () => { rpc.SetMethod<int>("replacement", () => 2); return 1; });
        var replaced = Parse(rpc.ProcessString("[" + Request("replacement") + "," + Request("replacement") + "]")).AsArray();
        Check(replaced[0]!["result"]!.GetValue<int>() == 1 && replaced[1]!["result"]!.GetValue<int>() == 2, "Callback reentry replaces later registration.");
        Parallel.For(0, 32, i => { var response = Parse(rpc.ProcessString(Request("sum", "[1,2]", i.ToString(System.Globalization.CultureInfo.InvariantCulture)))); Check(response["result"]!.GetValue<int>() == 3, "Concurrent pure dispatch."); });
        using (var retired = new JSONRPC())
        {
            retired.Dispose(); Reject<ObjectDisposedException>(() => retired.ProcessString("")); Reject<ObjectDisposedException>(() => retired.MakeNotification("tick", null)); Reject<ObjectDisposedException>(() => retired.SetMethod("tick", () => { }));
        }
        using (var retiring = new JSONRPC())
        {
            retiring.SetMethod<int>("close", () => { retiring.Dispose(); return 7; });
            Check(Parse(retiring.ProcessString(Request("close")))["result"]!.GetValue<int>() == 7, "An already captured callback may retire its dispatcher.");
            Reject<ObjectDisposedException>(() => retiring.ProcessString(Request("close")));
        }
        RunSceneTCP();
        Console.WriteLine("JSON-RPC: typed/compiled/document schemas, all makers, exact IDs, strict wire/shape/error/notification/batch boundaries, reentry/concurrency/lifetime and public scene TCP passed.");
    }

    private static void RunSceneTCP()
    {
        using var node = new SceneRPC();
        using var tree = new SceneTree(node);
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!node.Complete && elapsed.Elapsed < TimeSpan.FromSeconds(10)) { tree.ProcessFrame(0.001); Thread.Sleep(1); }
        Check(node.Complete, "Public scene TCP request dispatch, response and notification.");
    }

    private sealed class SceneRPC : Node
    {
        private readonly JSONRPC _rpc = new();
        private readonly TCPServer _server = new();
        private readonly StreamPeerTCP _client = new();
        private readonly PacketPeerStream _clientPackets = new();
        private readonly PacketPeerStream _serverPackets = new();
        private StreamPeerTCP? _accepted;
        private bool _sent;
        private int _notifications;
        internal bool Complete;
        internal SceneRPC() { ProcessEnabled = true; }
        protected override void OnReady()
        {
            _rpc.SetMethod<int[], int>("sum", values => values.Sum());
            _rpc.SetMethod("notify", () => { _notifications++; });
            _server.Listen(0); _client.ConnectToHost("localhost", _server.GetLocalPort());
            _clientPackets.StreamPeer = _client;
        }
        protected override void OnProcess(double delta)
        {
            if (Complete) return;
            _client.Poll();
            if (_accepted is null && _server.IsConnectionAvailable()) { _accepted = _server.TakeConnection(); _serverPackets.StreamPeer = _accepted; }
            _accepted?.Poll();
            if (!_sent && _client.GetStatus() == StreamSocketStatus.Connected)
            {
                _clientPackets.PutPacket(Encoding.UTF8.GetBytes("[" + Request("sum", "[20,22]", "\"scene\"") + ",{\"jsonrpc\":\"2.0\",\"method\":\"notify\"}]")); _sent = true;
            }
            if (_serverPackets.GetAvailablePacketCount() != 0)
            {
                var response = _rpc.ProcessString(Encoding.UTF8.GetString(_serverPackets.GetPacket()));
                if (response.Length != 0) _serverPackets.PutPacket(Encoding.UTF8.GetBytes(response));
            }
            if (_clientPackets.GetAvailablePacketCount() != 0)
            {
                var response = Parse(Encoding.UTF8.GetString(_clientPackets.GetPacket())).AsArray();
                Check(response.Count == 1 && response[0]!["result"]!.GetValue<int>() == 42 && response[0]!["id"]!.GetValue<string>() == "scene" && _notifications == 1, "Actual native framed TCP RPC."); Complete = true;
            }
        }
        protected override void Dispose(bool disposing)
        {
            try { _clientPackets.Dispose(); _serverPackets.Dispose(); _accepted?.Dispose(); _client.Dispose(); _server.Dispose(); _rpc.Dispose(); }
            finally { base.Dispose(disposing); }
        }
    }
}

internal sealed record JSONRPCNamedParameters
{
    [JsonRequired, JsonPropertyName("minuend")] public int Minuend { get; init; }
    [JsonRequired, JsonPropertyName("subtrahend")] public int Subtrahend { get; init; }
}

[JsonSerializable(typeof(JSONRPCNamedParameters))]
[JsonSerializable(typeof(int))]
internal partial class JSONRPCModels : JsonSerializerContext;
