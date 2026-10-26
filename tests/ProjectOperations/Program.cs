using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

var output = IOPath.GetFullPath(args[0]); Directory.CreateDirectory(output);
var project = SessionProject.Create(IOPath.Combine(output, "Game"), AppContext.BaseDirectory);
var owner = project.OpenSession(); using var human = owner.Connect("human");
var host = owner.StartOperations(project, AppContext.BaseDirectory); var checks = 0;
var original = human.InspectCode().Text;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
void PumpUntil(Task task)
{
    var timer = Stopwatch.StartNew();
    while (!task.IsCompleted) { if (timer.ElapsedMilliseconds > 15000) throw new TimeoutException("Operation owner did not complete the request."); host.Pump(); Thread.Sleep(2); }
}
JsonElement Invoke(SessionOperationsClient client, string method, object values)
{
    var pending = client.InvokeAsync(method, JsonSerializer.SerializeToElement(values)); PumpUntil(pending); return pending.GetAwaiter().GetResult();
}
JsonElement Value(JsonElement reply) { Check(reply.GetProperty("error").ValueKind == JsonValueKind.Null, reply.GetRawText()); return reply.GetProperty("value"); }
var remote = SessionOperationsClient.ConnectAsync(project.Root, "test agent").GetAwaiter().GetResult();
try
{
    MCPChecks.Run(project, host);
    AuthorizationChecks.Run(project, human, host);
    ExecutorFailureChecks.Run(project, host);
    var context = Value(Invoke(remote, "context", new { })); Check(context.GetProperty("projectName").GetString() == "Game", "Wrong project context.");
    var scene = human.InspectScene(); var square = scene.Nodes.Single(n => n.Name == "Square");
    var page = Value(Invoke(remote, "scene_read", new { offset = 0, limit = 128 }));
    Check(page.GetProperty("nodes").GetArrayLength() == scene.Nodes.Count && page.GetProperty("nodes")[1].GetProperty("transform").GetProperty("origin").GetProperty("x").GetSingle() == 40, "Scene transport lost typed pose data.");
    SessionCodeEdit? pendingSource = null;
    using (human.BindCodeBuffer(() => { var edit = pendingSource; pendingSource = null; return edit; }))
    {
        var code = human.InspectCode(); pendingSource = new(code.ID, code.Revision, "// pending manual source\n" + code.Text);
        var changed = Value(Invoke(remote, "scene_apply", new { documentID = scene.ID, label = "agent move", edits = new[] { new { kind = "position", nodeID = square.ID, expectedRevision = square.Revision, x = 80f, y = 40f } } }));
        Check(human.InspectCode().Text.StartsWith("// pending manual source", StringComparison.Ordinal), "Transport bypassed pending manual input.");
        Check(human.InspectScene().Nodes.Single(n => n.ID == square.ID).Transform!.Value.Origin.X == 80, "Shared scene was not changed.");
        Check(changed.GetProperty("origin").GetString()!.StartsWith("external:test agent:", StringComparison.Ordinal), "Origin could impersonate manual work.");
        var conflict = Invoke(remote, "code_edit", new { documentID = code.ID, expectedRevision = code.Revision, start = 0, length = 0, text = "stale", label = "stale replace" });
        Check(conflict.GetProperty("error").GetProperty("code").GetString() == "CONFLICT", "Stale code did not produce a useful conflict.");
        var operationID = changed.GetProperty("operationID").GetGuid(); Value(Invoke(remote, "undo", new { operationID }));
        Check(human.InspectCode().Text.StartsWith("// pending manual source", StringComparison.Ordinal), "Undo lost unrelated manual source.");
        Value(Invoke(remote, "redo", new { operationID }));
    }
    var selection = human.InspectSelection(); scene = human.InspectScene();
    Value(Invoke(remote, "selection", new { nodeID = square.ID, expectedSceneRevision = scene.Revision, expectedSelectionRevision = selection.Revision }));
    Check(human.InspectSelection().NodeID == square.ID && human.InspectScene().Revision == scene.Revision, "Selection mutated content or stayed private to a client.");
    var source = human.InspectCode(); human.Apply("unicode source fixture", new SessionCodeEdit(source.ID, source.Revision, "A😀B\r\n"));
    var unicode = Value(Invoke(remote, "code_read", new { start = 1, length = 1 }));
    Check(unicode.GetProperty("text").GetString() == "😀" && unicode.GetProperty("next").GetInt32() == 3, "Source pagination split a surrogate pair.");
    var invalid = Invoke(remote, "code_read", new { start = 2, length = 1 }); Check(invalid.GetProperty("error").GetProperty("code").GetString() == "INVALID_ARGUMENT", "Invalid UTF-16 offset accepted.");
    source = human.InspectCode(); human.Apply("restore source", new SessionCodeEdit(source.ID, source.Revision, original));
    source = human.InspectCode(); var before = source.Text;
    var queued = remote.InvokeAsync("code_edit", JsonSerializer.SerializeToElement(new { documentID = source.ID, expectedRevision = source.Revision, start = 0, length = 0, text = "// cancelled\n", label = "cancelled before owner dispatch" }));
    Thread.Sleep(100); remote.DisposeAsync().GetAwaiter().GetResult();
    try { queued.GetAwaiter().GetResult(); } catch (IOException) { } catch (ObjectDisposedException) { }
    Thread.Sleep(100); host.Pump(); Check(human.InspectCode().Text == before, "A disconnected queued mutation committed.");
    var reconnected = SessionOperationsClient.ConnectAsync(project.Root, "reconnected agent").GetAwaiter().GetResult();
    try { Value(Invoke(reconnected, "context", new { })); } finally { reconnected.DisposeAsync().GetAwaiter().GetResult(); }
    if (args.Length > 1 && args[1] == "--jobs")
    {
        var jobsClient = SessionOperationsClient.ConnectAsync(project.Root, "no-UI job client").GetAwaiter().GetResult();
        try { JobChecks.Run(project, human, host, jobsClient, output); before = human.InspectCode().Text; }
        finally { jobsClient.DisposeAsync().GetAwaiter().GetResult(); }
    }
    // Discovery corruption cannot prevent lease release even when endpoint retirement reports the failure.
    File.WriteAllText(IOPath.Combine(project.Root, ".electron2d/session/operations.json"), "invalid discovery");
    try { owner.Dispose(); } catch (AggregateException) { }
    host.Completion.GetAwaiter().GetResult();
    using var recovered = project.OpenSession(); using var after = recovered.Connect("recovered");
    Check(after.InspectCode().Text == before && after.InspectScene().IsDirty, "Failed endpoint cleanup retained the owner lease or lost unsaved scene recovery.");
    Console.WriteLine($"PASS {checks} shared operations, real Unix transport, pending-input/conflict/history/selection/unicode/disconnect/recovery contracts.");
}
finally { remote.DisposeAsync().GetAwaiter().GetResult(); host.Dispose(); owner.Dispose(); }
return 0;
