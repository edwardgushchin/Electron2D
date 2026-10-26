using System.Diagnostics;
using System.Text.Json;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class AuthorizationChecks
{
    internal static void Run(SessionProject project, SessionClient human, SessionOperationsHost host)
    {
        var checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
        void Rejected(string actor, string token)
        {
            try { SessionOperationsClient.ConnectAsync(project.Root, actor, token).GetAwaiter().GetResult().DisposeAsync().GetAwaiter().GetResult(); throw new InvalidOperationException("Invalid capability authenticated."); }
            catch (IOException) { checks++; }
        }
        JsonElement Invoke(SessionOperationsClient client, string method, object args)
        {
            var pending = client.InvokeAsync(method, JsonSerializer.SerializeToElement(args)); var elapsed = Stopwatch.StartNew();
            while (!pending.IsCompleted) { if (elapsed.ElapsedMilliseconds > 10000) throw new TimeoutException("Owner did not dispatch."); host.Pump(); Thread.Sleep(2); }
            return pending.GetAwaiter().GetResult();
        }
        Rejected("forged", new string('0', 64));
        var capability = host.GrantClient("scoped actor"); Rejected("wrong actor", capability);
        var client = SessionOperationsClient.ConnectAsync(project.Root, "scoped actor", capability).GetAwaiter().GetResult();
        try
        {
            var code = human.InspectCode(); var original = code.Text;
            var denied = Invoke(client, "save", new { kind = "Code", expectedRevision = code.Revision });
            Check(denied.GetProperty("error").GetProperty("code").GetString() == "PERMISSION_DENIED", "Default capability published files.");
            denied = Invoke(client, "refresh", new { kind = "Code", expectedRevision = code.Revision, resolution = "UseDisk" });
            Check(denied.GetProperty("error").GetProperty("code").GetString() == "PERMISSION_DENIED", "Default capability discarded unsaved code.");
            var pending = client.InvokeAsync("code_edit", JsonSerializer.SerializeToElement(new { documentID = code.ID, expectedRevision = code.Revision, start = 0, length = 0, text = "// stopped\n", label = "must not dispatch" }));
            Thread.Sleep(100); host.CancelClientWork(capability); host.Pump();
            var stopped = pending.GetAwaiter().GetResult(); Check(stopped.GetProperty("error").GetProperty("code").GetString() == "CANCELLED" && human.InspectCode().Text == original, "Stop committed an undispatched edit.");
            Check(Invoke(client, "context", new { }).GetProperty("error").ValueKind == JsonValueKind.Null, "Stop unnecessarily disconnected the conversation.");
            var late = Invoke(client, "code_edit", new { documentID = code.ID, expectedRevision = code.Revision, start = 0, length = 0, text = "// late after stop\n", label = "must stay stopped" });
            Check(late.GetProperty("error").GetProperty("code").GetString() == "CANCELLED" && human.InspectCode().Text == original, "A late request bypassed Stop before turn acknowledgement.");
            host.ResumeClientWork(capability);
            Check(Invoke(client, "code_edit", new { documentID = code.ID, expectedRevision = code.Revision, start = 0, length = 0, text = "", label = "resumed no-op" }).GetProperty("error").ValueKind == JsonValueKind.Null, "Explicit new-task admission did not resume safely.");
            pending = client.InvokeAsync("code_edit", JsonSerializer.SerializeToElement(new { documentID = code.ID, expectedRevision = code.Revision, start = 0, length = 0, text = "// revoked\n", label = "revoked before dispatch" }));
            Thread.Sleep(100); host.RevokeClient(capability); Thread.Sleep(20); host.Pump();
            try { pending.GetAwaiter().GetResult(); } catch (IOException) { } catch (ObjectDisposedException) { }
            Check(human.InspectCode().Text == original, "Expired capability committed a queued edit.");
            Rejected("scoped actor", capability);
        }
        finally { client.DisposeAsync().GetAwaiter().GetResult(); host.RevokeClient(capability); }
        var writerToken = host.GrantClient("publisher", allowFileWrites: true);
        var writer = SessionOperationsClient.ConnectAsync(project.Root, "publisher", writerToken).GetAwaiter().GetResult();
        try
        {
            var code = human.InspectCode();
            var saved = Invoke(writer, "save", new { kind = "Code", expectedRevision = code.Revision });
            Check(saved.GetProperty("error").ValueKind == JsonValueKind.Null && File.ReadAllText(IOPath.Combine(project.Root, project.CodeFile)) == code.Text, "Explicit publication capability did not use the shared save boundary.");
        }
        finally { writer.DisposeAsync().GetAwaiter().GetResult(); host.RevokeClient(writerToken); }
        Console.WriteLine($"PASS {checks} authentication, named capability, publication/discard and stop/revocation contracts.");
    }
}
