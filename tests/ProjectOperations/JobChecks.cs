using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class JobChecks
{
    internal static void Run(SessionProject project, SessionClient human, SessionOperationsHost host, SessionOperationsClient remote, string output)
    {
        var checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
        JsonElement Invoke(string method, object args)
        {
            var task = remote.InvokeAsync(method, JsonSerializer.SerializeToElement(args)); var timer = Stopwatch.StartNew();
            while (!task.IsCompleted) { if (timer.ElapsedMilliseconds > 10000) throw new TimeoutException("Owner dispatch timed out."); host.Pump(); Thread.Sleep(2); }
            var reply = task.GetAwaiter().GetResult(); if (reply.GetProperty("error").ValueKind != JsonValueKind.Null) throw new InvalidOperationException(reply.GetRawText());
            return reply.GetProperty("value");
        }
        JsonElement Wait(Guid id)
        {
            var timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < 90000)
            {
                var status = Invoke("job_status", new { jobID = id });
                if (status.GetProperty("status").GetString() is "Completed" or "Failed" or "Cancelled") return status;
                Thread.Sleep(100);
            }
            throw new TimeoutException("No-UI snapshot did not retire.");
        }
        var source = human.InspectCode();
        human.Apply("no-UI live color", new SessionCodeEdit(source.ID, source.Revision, source.Text.Replace("Colors.CornflowerBlue", "Colors.LimeGreen", StringComparison.Ordinal)));
        source = human.InspectCode(); var scene = human.InspectScene();
        var queued = Invoke("job_start", new { expectedSceneRevision = scene.Revision, expectedCodeRevision = source.Revision, kind = "Run", mode = "Rendered", backend = "gpu", steps = 2, captureSteps = new[] { 2 } });
        var id = queued.GetProperty("id").GetGuid();
        human.Apply("manual after no-UI snapshot", new SessionCodeEdit(source.ID, source.Revision, "// later no-UI human\n" + source.Text));
        var status = Wait(id); Check(status.GetProperty("status").GetString() == "Completed" && status.GetProperty("isStale").GetBoolean(), "No-UI run/staleness did not reflect its immutable unsaved inputs.");
        Check(status.GetProperty("assertionCount").GetInt32() > 0 && status.GetProperty("assertions").EnumerateArray().All(a => a.GetProperty("passed").GetBoolean()), "No-UI assertions were not real successful results.");
        var index = status.GetProperty("artifacts").EnumerateArray().Single(a => a.GetProperty("kind").GetString() == "image").GetProperty("index").GetInt32();
        var artifact = Invoke("job_artifact", new { jobID = id, artifact = index }); var bytes = Convert.FromBase64String(artifact.GetProperty("base64").GetString()!);
        Check(artifact.GetProperty("snapshotID").GetGuid() == queued.GetProperty("snapshotID").GetGuid() && Convert.ToHexString(SHA256.HashData(bytes)).Equals(artifact.GetProperty("sha256").GetString(), StringComparison.OrdinalIgnoreCase), "Transport image lost digest or snapshot provenance.");
        var path = IOPath.Combine(output, "no-ui-game.png"); File.WriteAllBytes(path, bytes); using (var image = Image.LoadFromFile(path)) Check(image.GetPixel(80, 40).G > .7f && image.GetPixel(40, 40).G < .7f, "No-UI capture did not render the shared pose.");
        Check(human.InspectCode().Text.StartsWith("// later no-UI human", StringComparison.Ordinal) && human.InspectJobs().Count == 1, "No-UI job bypassed the shared registry or lost later authoring.");
        source = human.InspectCode(); var preserved = source.Text;
        human.Apply("invalid source fixture", new SessionCodeEdit(source.ID, source.Revision, "this is invalid C#")); source = human.InspectCode();
        queued = Invoke("job_start", new { expectedSceneRevision = scene.Revision, expectedCodeRevision = source.Revision, kind = "Build" });
        status = Wait(queued.GetProperty("id").GetGuid());
        Check(status.GetProperty("status").GetString() == "Failed" && status.GetProperty("diagnostics").EnumerateArray().Any(d => d.GetProperty("code").GetString() == "BUILD_FAILED"), "Transport concealed a real compiler failure.");
        source = human.InspectCode(); human.Apply("restore shared code", new SessionCodeEdit(source.ID, source.Revision, preserved));
        foreach (var job in human.InspectJobs()) { job.DisposeAsync().AsTask().GetAwaiter().GetResult(); job.RemoveResult(); }
        Check(human.InspectJobs().Count == 0, "Shared job registry retained removed results.");
        Console.WriteLine($"PASS {checks} no-UI transport/snapshot/rendered PNG/digest/stale/compiler-failure/shared-registry contracts.");
    }
}
