using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D.Editor;

internal static class MCPChecks
{
    internal static void Run(SessionProject project, SessionOperationsHost host)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var arg in new[] { typeof(ProjectSession).Assembly.Location, "--session-mcp", project.Root, "independent MCP client" }) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!; var checks = 0;
        try
        {
            var initialized = Send(new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new { protocolVersion = "2025-06-18", capabilities = new { }, clientInfo = new { name = "electron2d-tests", version = "1" } } });
            Check(initialized.GetProperty("result").GetProperty("protocolVersion").GetString() == "2025-06-18", "MCP version was not negotiated.");
            process.StandardInput.WriteLine("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}"); process.StandardInput.Flush();
            var catalog = Send(new { jsonrpc = "2.0", id = 2, method = "tools/list", @params = new { } });
            var tools = catalog.GetProperty("result").GetProperty("tools");
            Check(tools.GetArrayLength() >= 16 && tools.EnumerateArray().Any(t => t.GetProperty("name").GetString() == "code_edit" && t.GetProperty("inputSchema").GetProperty("type").GetString() == "object"), "Executable MCP tools/schema are missing.");
            var context = Send(new { jsonrpc = "2.0", id = 3, method = "tools/call", @params = new { name = "context", arguments = new { } } });
            var result = context.GetProperty("result"); Check(!result.GetProperty("isError").GetBoolean() && result.GetProperty("structuredContent").GetProperty("value").GetProperty("projectName").GetString() == project.Name, "MCP did not reach the current unsaved owner.");
            var invalid = Send(new { jsonrpc = "2.0", id = 4, method = "tools/call", @params = new { name = "code_read", arguments = new { start = -1 } } });
            Check(invalid.GetProperty("result").GetProperty("isError").GetBoolean(), "Invalid operation was reported as successful MCP output.");
            process.StandardInput.Close();
            if (!process.WaitForExit(10000)) throw new TimeoutException("MCP bridge did not retire after input closed.");
            Check(process.ExitCode == 0, process.StandardError.ReadToEnd());
            Console.WriteLine($"PASS {checks} independent MCP process/version/tools/schema/live-owner/error/retirement contracts.");
        }
        finally { if (!process.HasExited) { process.Kill(true); process.WaitForExit(); } }
        JsonElement Send(object message)
        {
            process.StandardInput.WriteLine(JsonSerializer.Serialize(message)); process.StandardInput.Flush();
            var pending = process.StandardOutput.ReadLineAsync(); var elapsed = Stopwatch.StartNew();
            while (!pending.IsCompleted) { if (elapsed.ElapsedMilliseconds > 15000) throw new TimeoutException("MCP response timed out."); host.Pump(); Thread.Sleep(2); }
            var line = pending.GetAwaiter().GetResult() ?? throw new InvalidOperationException("MCP exited: " + process.StandardError.ReadToEnd());
            using var document = JsonDocument.Parse(line); return document.RootElement.Clone();
        }
        void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
    }
}
