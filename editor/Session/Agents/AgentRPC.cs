using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.Editor;

internal sealed class AgentRPC(Stream input, Stream output, Action<JsonElement> notification, Action<string> lost, Action<string> warning) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly SemaphoreSlim _writer = new(1);
    private readonly CancellationTokenSource _stop = new();
    private long _sequence;
    internal Task Completion { get; private set; } = Task.CompletedTask;
    internal void Start() => Completion = Read();
    internal async Task<JsonElement> Request(string method, JsonObject values, CancellationToken cancellation)
    {
        var id = Interlocked.Increment(ref _sequence);
        var source = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (_pending.Count >= 32 || !_pending.TryAdd(id, source)) throw new InvalidOperationException("Executor request budget exceeded.");
        try
        {
            await Send(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = method, ["params"] = values }, cancellation).ConfigureAwait(false);
            return await source.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellation).ConfigureAwait(false);
        }
        finally { _pending.TryRemove(id, out _); }
    }
    internal Task Notify(string method, JsonObject? values, CancellationToken cancellation) =>
        Send(new JsonObject { ["jsonrpc"] = "2.0", ["method"] = method, ["params"] = values }, cancellation);
    private async Task Send(JsonObject message, CancellationToken cancellation)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _stop.Token);
        await _writer.WaitAsync(linked.Token).ConfigureAwait(false);
        try { await JSONLines.Write(output, message, linked.Token).ConfigureAwait(false); }
        finally { _writer.Release(); }
    }
    private async Task Read()
    {
        try
        {
            var reader = new JSONLines(input);
            while (await reader.Read(_stop.Token).ConfigureAwait(false) is { } message)
            {
                if (message.TryGetProperty("method", out var method))
                {
                    if (message.TryGetProperty("id", out var id))
                    {
                        var name = method.GetString(); JsonObject response;
                        if (name is "item/commandExecution/requestApproval" or "item/fileChange/requestApproval")
                            response = new JsonObject { ["result"] = new JsonObject { ["decision"] = "decline" } };
                        else if (name == "tool/requestUserInput")
                            response = new JsonObject { ["result"] = new JsonObject { ["answers"] = new JsonObject() } };
                        else response = new JsonObject { ["error"] = new JsonObject { ["code"] = -32601, ["message"] = "This executor profile does not provide external approval or interactive form capabilities. Use the editor conversation and scoped project operations." } };
                        response["jsonrpc"] = "2.0"; response["id"] = JsonNode.Parse(id.GetRawText());
                        await Send(response, _stop.Token).ConfigureAwait(false);
                        warning("Executor requested an unsupported interaction: " + name + ". Supply a complete task in the editor; no external effects were approved.");
                    }
                    else notification(message);
                }
                else if (message.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.TryGetInt64(out var number) && _pending.TryRemove(number, out var source))
                {
                    if (message.TryGetProperty("error", out var error)) source.TrySetException(new IOException(error.TryGetProperty("message", out var detail) ? detail.GetString() : "Executor request failed."));
                    else if (message.TryGetProperty("result", out var result)) source.TrySetResult(result.Clone());
                    else source.TrySetException(new InvalidDataException("Executor response has no result or error."));
                }
            }
            if (!_stop.IsCancellationRequested) lost("Executor disconnected before its conversation was retired.");
        }
        catch (Exception error) when (error is IOException or JsonException or OperationCanceledException or ObjectDisposedException or InvalidDataException or InvalidOperationException)
        { if (!_stop.IsCancellationRequested) lost("Executor protocol failed: " + error.Message); }
        finally { foreach (var source in _pending.Values) source.TrySetException(new IOException("Executor connection closed; inspect committed operations before retrying.")); }
    }
    public async ValueTask DisposeAsync()
    {
        _stop.Cancel(); input.Dispose(); output.Dispose(); await Completion.ConfigureAwait(false);
        await _writer.WaitAsync().ConfigureAwait(false); _writer.Release(); _writer.Dispose(); _stop.Dispose();
    }
}
