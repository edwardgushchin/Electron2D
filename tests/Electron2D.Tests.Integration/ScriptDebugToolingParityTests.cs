/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using Electron2D.Mcp;
using Electron2D.ProjectSystem;
using Electron2D.Tooling;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ScriptDebugToolingParityTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 23, 8, 30, 0, TimeSpan.Zero);

    private static readonly string[] ScriptCommandNames =
    [
        "script_create",
        "script_open",
        "script_read",
        "script_rename",
        "script_delete",
        "script_search_text",
        "script_apply_text_edits",
        "script_save",
        "script_format",
        "script_get_diagnostics",
        "script_get_completions",
        "script_get_signature_help",
        "script_get_hover",
        "script_get_definition",
        "script_get_document_symbols",
        "script_find_references",
        "script_rename_symbol",
        "script_get_code_actions",
        "script_apply_code_action"
    ];

    private static readonly string[] DebugCommandNames =
    [
        "debug_set_breakpoint",
        "debug_update_breakpoint",
        "debug_remove_breakpoint",
        "debug_start",
        "debug_attach",
        "debug_restart",
        "debug_pause",
        "debug_continue",
        "debug_step_into",
        "debug_step_over",
        "debug_step_out",
        "debug_get_threads",
        "debug_get_stack",
        "debug_get_locals",
        "debug_get_arguments",
        "debug_get_watches",
        "debug_evaluate_watches",
        "debug_add_watch",
        "debug_update_watch",
        "debug_remove_watch",
        "debug_stop"
    ];

    [Fact]
    public void ToolingAndMcpPublishScriptDebugParityCommandsAndMcpRoutesDoNotReturnUnsupported()
    {
        var allCommands = ScriptCommandNames.Concat(DebugCommandNames).ToArray();
        var toolingCommands = ProjectToolingHost.SupportedCommandNames;
        var projectRoot = CreateProjectRoot("mcp-parity", ScriptText(out _));
        using var session = McpServerSession.Open(projectRoot, registry: null, FixedInstant);
        var mcpTools = session.ListTools().Select(tool => tool.Name).ToArray();

        foreach (var command in allCommands)
        {
            Assert.Contains(command, toolingCommands);
            Assert.Contains(command, mcpTools);
        }

        var read = session.CallTool(new McpToolRequest("script_read", new Dictionary<string, string>
        {
            ["path"] = "Scripts/HeroController.cs"
        }));
        Assert.True(read.Succeeded, FormatDiagnostics(read.Diagnostics));
        Assert.Equal("Scripts/HeroController.cs", read.Content["path"]!.GetValue<string>());
        Assert.Contains("HeroController", read.Content["text"]!.GetValue<string>(), StringComparison.Ordinal);

        var debugStart = session.CallTool(new McpToolRequest("debug_start", new Dictionary<string, string>
        {
            ["inputBuildConfigurationHash"] = "sha256:mcp-debug"
        }));
        var stack = session.CallTool(new McpToolRequest("debug_get_stack", new Dictionary<string, string>()));

        Assert.True(debugStart.Succeeded, FormatDiagnostics(debugStart.Diagnostics));
        Assert.True(stack.Succeeded, FormatDiagnostics(stack.Diagnostics));
        Assert.NotNull(debugStart.Content["inputSnapshotId"]);
        Assert.True(stack.Content["threads"]!.AsArray().Count >= 2);
    }

    [Fact]
    public void ScriptServiceUsesWorkspaceTransactionsLiveRoslynAndSaveConflictGuard()
    {
        var source = ScriptText(out var positions);
        using var workspace = CreateWorkspace("script-service", source);
        var host = new ProjectToolingHost(workspace);
        var request = new ToolingScriptIdeRequest(
            "Scripts/HeroController.cs",
            positions["completion"],
            positions["signature"],
            positions["hover"],
            positions["definition"],
            "MoveHero",
            responseDocumentRevision: 0);

        var completions = host.Script.GetCompletions(request);

        Assert.True(completions.Succeeded, FormatDiagnostics(completions.Diagnostics));
        Assert.True(completions.RoslynSemanticModel);
        Assert.False(completions.WorkspaceSnapshotUsedForIde);
        Assert.Equal(new ProjectDocumentRevision(1), completions.DocumentRevision);
        Assert.Equal(1, completions.SemanticVersion);
        Assert.Contains(completions.CompletionItems, item => item.DisplayText == "Sprite2D" && item.IsSelected);
        Assert.Equal("CS0103", host.Script.GetDiagnostics(request).Diagnostic!.Code);
        Assert.Contains("DocumentedMove", host.Script.GetHover(request).Hover!.SymbolDisplay, StringComparison.Ordinal);
        Assert.Contains(host.Script.GetDocumentSymbols(request).Symbols, symbol => symbol.Name == "HeroController");
        Assert.NotEmpty(host.Script.GetCodeActions(request).CodeActions);

        var changed = source.Replace("var speed = 240;", "var speed = 280;", StringComparison.Ordinal);
        var applied = host.Script.ApplyTextEdits(
            new ToolingScriptApplyTextEditsRequest(
                "op-script-agent-edit",
                "Scripts/HeroController.cs",
                new ProjectDocumentRevision(1),
                [ToolingScriptTextEdit.ReplaceAll(changed)],
                "undo-script-agent-edit"),
            AgentContext(OperationCapability.TaskWrite));

        Assert.True(applied.Succeeded, FormatDiagnostics(applied.Operation.Diagnostics));
        Assert.Equal("script_apply_text_edits", applied.Operation.OperationKind);
        Assert.Contains("Scripts/HeroController.cs", applied.Operation.DirtyDocuments);
        Assert.Contains("undo-script-agent-edit", workspace.UndoRedo.UndoGroups);
        Assert.Contains("var speed = 280;", workspace.Documents.GetByPath("Scripts/HeroController.cs").Text, StringComparison.Ordinal);

        var human = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-human-script-dirty",
            ProjectWorkspaceActorKind.Human,
            "script.manual-edit",
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            undoGroupId: "undo-human-script-dirty",
            [WorkspaceTransactionDocumentEdit.ReplaceText(
                "Scripts/HeroController.cs",
                new ProjectDocumentRevision(2),
                changed.Replace("var speed = 280;", "var speed = 320;", StringComparison.Ordinal))]));
        Assert.True(human.Succeeded, FormatDiagnostics(human.Diagnostics));

        var conflict = host.Script.Save(
            new ToolingScriptSaveRequest(
                "op-script-save-conflict",
                "Scripts/HeroController.cs",
                new ProjectDocumentRevision(2),
                dryRun: false),
            AgentContext(OperationCapability.TaskWrite));

        Assert.False(conflict.Succeeded);
        Assert.Contains(conflict.Operation.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0002");
        Assert.Contains("var speed = 240;", File.ReadAllText(Path.Combine(workspace.ProjectRoot, "Scripts", "HeroController.cs")), StringComparison.Ordinal);

        var saved = host.Script.Save(
            new ToolingScriptSaveRequest(
                "op-script-save-current",
                "Scripts/HeroController.cs",
                new ProjectDocumentRevision(3),
                dryRun: false),
            AgentContext(OperationCapability.TaskWrite));

        Assert.True(saved.Succeeded, FormatDiagnostics(saved.Operation.Diagnostics));
        Assert.Empty(saved.Operation.DirtyDocuments);
        Assert.Contains("var speed = 320;", File.ReadAllText(Path.Combine(workspace.ProjectRoot, "Scripts", "HeroController.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void DebugServiceUsesSnapshotStartExplicitFramesWatchDefinitionsAndAttachGuard()
    {
        using var workspace = CreateWorkspace("debug-service", ScriptText(out _));
        var host = new ProjectToolingHost(workspace);

        var breakpoint = host.Debug.SetBreakpoint(
            new ToolingDebugSetBreakpointRequest("op-debug-breakpoint", "doc-hero", "Scripts/HeroController.cs", line: 10, column: 17),
            AgentContext(OperationCapability.TaskWrite));
        var updatedBreakpoint = host.Debug.UpdateBreakpoint(
            new ToolingDebugUpdateBreakpointRequest(
                "op-debug-breakpoint-update",
                breakpoint.Breakpoint!.BreakpointId,
                enabled: false,
                line: 11,
                column: 9),
            AgentContext(OperationCapability.TaskWrite));
        var rejectedAttach = host.Debug.Attach(
            new ToolingDebugAttachRequest(
                "op-debug-attach-rejected",
                processId: 123456,
                activeEditorGameProcessId: Environment.ProcessId,
                interactiveApproved: false,
                "sha256:debug-attach"),
            AgentContext(OperationCapability.TaskWrite));
        var started = host.Debug.Start(
            new ToolingDebugStartRequest("op-debug-start", "sha256:debug", activeEditorGameProcessId: Environment.ProcessId),
            AgentContext(OperationCapability.TaskWrite));

        Assert.True(breakpoint.Succeeded, FormatDiagnostics(breakpoint.Diagnostics));
        Assert.True(updatedBreakpoint.Succeeded, FormatDiagnostics(updatedBreakpoint.Diagnostics));
        Assert.False(updatedBreakpoint.Breakpoint!.Enabled);
        Assert.Equal(11, updatedBreakpoint.Breakpoint.SourceAnchor.Line);
        Assert.False(rejectedAttach.Succeeded);
        Assert.Contains(rejectedAttach.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0002");
        Assert.True(started.Succeeded, FormatDiagnostics(started.Diagnostics));
        Assert.Equal("sha256:debug", started.InputBuildConfigurationHash);
        Assert.False(string.IsNullOrWhiteSpace(started.InputSnapshotId));
        Assert.Contains("Scripts/HeroController.cs", started.InputDocumentRevisions.Keys);

        var stack = host.Debug.GetStack();
        Assert.True(stack.Succeeded, FormatDiagnostics(stack.Diagnostics));
        Assert.Equal(started.Threads.Select(thread => thread.ThreadId).Order(), stack.Threads.Select(thread => thread.ThreadId).Order());
        Assert.All(stack.Threads, thread => Assert.Contains(stack.StacksByThread, pair => pair.Key == thread.ThreadId));

        var frameId = started.StackFrames[0].FrameId;
        var locals = host.Debug.GetLocals(new ToolingDebugFrameRequest(frameId));
        var arguments = host.Debug.GetArguments(new ToolingDebugFrameRequest(frameId));
        var watches = host.Debug.GetWatches();
        var evaluated = host.Debug.EvaluateWatches(new ToolingDebugFrameRequest(frameId));
        var addedWatch = host.Debug.AddWatch(new ToolingDebugWatchRequest("op-debug-watch-add", "hero.Health + 1"));
        var updatedWatch = host.Debug.UpdateWatch(new ToolingDebugWatchUpdateRequest("op-debug-watch-update", addedWatch.Watch!.WatchId, "hero.Health + 2"));
        var removedWatch = host.Debug.RemoveWatch(new ToolingDebugRemoveWatchRequest("op-debug-watch-remove", updatedWatch.Watch!.WatchId));

        Assert.All(locals.Variables, variable => Assert.Equal(frameId, variable.FrameId));
        Assert.All(arguments.Variables, variable => Assert.Equal(frameId, variable.FrameId));
        Assert.Contains(locals.Variables, variable => variable.Name == "speed" && variable.Value == "240");
        Assert.Contains(arguments.Variables, variable => variable.Name == "delta" && variable.Value == "0.016");
        Assert.Contains(watches.Watches, watch => watch.Expression == "hero.Health" && watch.Value is null);
        Assert.Contains(evaluated.Watches, watch => watch.Expression == "hero.Health" && watch.Value == "100");
        Assert.Equal("hero.Health + 1", addedWatch.Watch.Expression);
        Assert.Equal("hero.Health + 2", updatedWatch.Watch.Expression);
        Assert.True(removedWatch.Succeeded);
    }

    [Fact]
    public async Task EditorScriptDebugToolingSmokeWritesVisibleScriptDebugAndAgentWorkspaceArtifacts()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-script-debug-tooling-");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("--script-debug-tooling-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Editor script/debug tooling smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor script/debug tooling smoke passed", output);
            Assert.Equal("Script", lines["SelectedWorkspace"]);
            Assert.Equal("script_apply_text_edits", lines["ScriptOperation"]);
            Assert.Equal("CS0103", lines["DiagnosticCode"]);
            Assert.Equal("Sprite2D", lines["CompletionSelected"]);
            Assert.Equal("breakpoint-hero-update", lines["BreakpointId"]);
            Assert.Equal("2", lines["ThreadCount"]);
            Assert.Equal("2", lines["StackThreadCount"]);
            Assert.Equal("speed=240", lines["LocalValue"]);
            Assert.Equal("delta=0.016", lines["ArgumentValue"]);
            Assert.Equal("hero.Health", lines["WatchDefinition"]);
            Assert.Equal("hero.Health=100", lines["WatchEvaluation"]);
            Assert.Equal("T-0161", lines["CurrentTask"]);
            Assert.Equal("transaction://op-script-agent-edit", lines["LinkedTransactions"]);
            Assert.Equal("job://op-debug-start", lines["LinkedJobs"]);
            Assert.Equal("artifact://script-debug-tooling/screenshot.png", lines["LinkedArtifacts"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);

            var statePath = lines["StatePath"];
            var screenshotPath = lines["ScreenshotPath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(statePath), $"Missing script/debug tooling state artifact: {statePath}");
            Assert.True(File.Exists(screenshotPath), $"Missing script/debug tooling screenshot artifact: {screenshotPath}");
            Assert.True(File.Exists(analysisPath), $"Missing script/debug tooling analysis artifact: {analysisPath}");

            var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
            Assert.Equal(1280, width);
            Assert.Equal(720, height);

            using var analysis = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var data = analysis.RootElement;

            Assert.Equal("Electron2D.ScriptDebugToolingVisualAnalysis", data.GetProperty("format").GetString());
            Assert.Equal("automated-script-debug-tooling-harness", data.GetProperty("harness").GetString());
            Assert.Equal("Script", data.GetProperty("selectedWorkspace").GetString());
            Assert.Equal(0, data.GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, data.GetProperty("forbiddenUiMatches").GetArrayLength());
            Assert.True(data.GetProperty("clickableControlCount").GetInt32() >= 24);
            Assert.True(data.GetProperty("script").GetProperty("agentEditVisible").GetBoolean());
            Assert.True(data.GetProperty("script").GetProperty("diagnosticsVisible").GetBoolean());
            Assert.True(data.GetProperty("debug").GetProperty("breakpointVisible").GetBoolean());
            Assert.True(data.GetProperty("debug").GetProperty("stackVisible").GetBoolean());
            Assert.True(data.GetProperty("debug").GetProperty("watchEvaluationVisible").GetBoolean());
            Assert.True(data.GetProperty("agentWorkspace").GetProperty("taskVisible").GetBoolean());
            Assert.True(data.GetProperty("agentWorkspace").GetProperty("linksVisible").GetBoolean());
            Assert.True(data.GetProperty("screenshotReviewed").GetBoolean());
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
        }
    }

    private static ProjectWorkspace CreateWorkspace(string name, string scriptText)
    {
        var root = CreateProjectRoot(name, scriptText);
        var workspace = ProjectWorkspace.CreateHeadless(root, $"owner-{name}");
        workspace.CommandBus.OpenTextDocument(
            "Scripts/HeroController.cs",
            scriptText,
            1,
            ProjectWorkspaceOperationContext.ForTest($"open-script-{name}"));
        return workspace;
    }

    private static string CreateProjectRoot(string name, string scriptText)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-ScriptDebugToolingParityTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Scripts"));
        File.WriteAllText(Path.Combine(root, "Scripts", "HeroController.cs"), scriptText);
        return root;
    }

    private static string ScriptText(out IReadOnlyDictionary<string, int> positions)
    {
        const string text = """
using Electron2D;

namespace Smoke.Scripts;

public sealed class HeroController : Node
{
    /// <summary>
    /// Moves hero with delta.
    /// </summary>
    public void [[hover]]DocumentedMove(float delta)
    {
        var speed = 240;
        var velocity = new Vector2(12, 24[[signature]]);
        var sprite = new Sprite2D();
        [[definition]]DocumentedMove(delta);
        MissingSymbol();
        var completionProbe = [[completion]]delta;
    }
}
""";

        var markerPositions = new Dictionary<string, int>(StringComparer.Ordinal);
        var current = text;
        while (true)
        {
            var start = current.IndexOf("[[", StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = current.IndexOf("]]", start, StringComparison.Ordinal);
            if (end < 0)
            {
                throw new FormatException("Script tooling marker is missing its closing token.");
            }

            var name = current[(start + 2)..end];
            markerPositions.Add(name, start);
            current = current.Remove(start, end - start + 2);
        }

        positions = markerPositions;
        return current.ReplaceLineEndings("\n");
    }

    private static OperationContext AgentContext(params OperationCapability[] capabilities)
    {
        return new OperationContext(
            "agent-t0161",
            PrincipalKind.Agent,
            "agent-session-t0161",
            capabilities,
            "script-debug-tooling-test");
    }

    private static string FormatDiagnostics(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] bytes)
    {
        Assert.True(bytes.Length >= 24, "PNG must contain a signature and IHDR chunk.");
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47 }, bytes.Take(4).ToArray());
        Assert.Equal("IHDR", System.Text.Encoding.ASCII.GetString(bytes, 12, 4));

        return (
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)),
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
    }

    private static Dictionary<string, string> ParseMachineReadableOutput(string output)
    {
        return output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
