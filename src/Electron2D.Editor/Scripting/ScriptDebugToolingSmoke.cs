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
using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D.ProjectSystem;
using Electron2D.Tooling;

namespace Electron2D.Editor.Scripting;

internal static class ScriptDebugToolingSmoke
{
    public static ScriptDebugToolingSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var fullWorkRoot = Path.GetFullPath(workRoot);
        Directory.CreateDirectory(fullWorkRoot);
        var projectRoot = Path.Combine(fullWorkRoot, "SmokeProject");
        var scriptsRoot = Path.Combine(projectRoot, "Scripts");
        Directory.CreateDirectory(scriptsRoot);

        var marked = CreateMarkedText();
        var scriptPath = Path.Combine(scriptsRoot, "HeroController.cs");
        File.WriteAllText(scriptPath, marked.Text);

        using var workspace = ProjectWorkspace.CreateHeadless(projectRoot, "editor-script-debug-tooling-smoke");
        workspace.CommandBus.OpenTextDocument(
            "Scripts/HeroController.cs",
            marked.Text,
            persistedRevision: 1,
            ProjectWorkspaceOperationContext.ForTest("open-script-debug-tooling-smoke"));

        var host = new ProjectToolingHost(workspace);
        var request = new ToolingScriptIdeRequest(
            "Scripts/HeroController.cs",
            marked.Positions["completion"],
            marked.Positions["signature"],
            marked.Positions["hover"],
            marked.Positions["definition"],
            "MoveHero",
            responseDocumentRevision: 0);

        var diagnostics = host.Script.GetDiagnostics(request);
        var completions = host.Script.GetCompletions(request);
        var changed = marked.Text.Replace("var speed = 240;", "var speed = 280;", StringComparison.Ordinal);
        var scriptMutation = host.Script.ApplyTextEdits(
            new ToolingScriptApplyTextEditsRequest(
                "op-script-agent-edit",
                "Scripts/HeroController.cs",
                new ProjectDocumentRevision(1),
                [ToolingScriptTextEdit.ReplaceAll(changed)],
                "undo-script-agent-edit"),
            AgentContext(OperationCapability.TaskWrite));
        var breakpoint = host.Debug.SetBreakpoint(
            new ToolingDebugSetBreakpointRequest(
                "op-debug-breakpoint",
                "doc-script-debug-hero",
                "Scripts/HeroController.cs",
                line: 10,
                column: 17),
            AgentContext(OperationCapability.TaskWrite));
        var debugSession = host.Debug.Start(
            new ToolingDebugStartRequest("op-debug-start", "sha256:script-debug-tooling", Environment.ProcessId),
            AgentContext(OperationCapability.TaskWrite));
        var stack = host.Debug.GetStack();
        var frameId = debugSession.StackFrames[0].FrameId;
        var locals = host.Debug.GetLocals(new ToolingDebugFrameRequest(frameId));
        var arguments = host.Debug.GetArguments(new ToolingDebugFrameRequest(frameId));
        var watchDefinitions = host.Debug.GetWatches();
        var watchEvaluations = host.Debug.EvaluateWatches(new ToolingDebugFrameRequest(frameId));
        var linkedTransactions = new[] { "transaction://op-script-agent-edit" };
        var linkedJobs = new[] { "job://op-debug-start" };
        var linkedArtifacts = new[] { "artifact://script-debug-tooling/screenshot.png" };

        var statePath = Path.Combine(fullWorkRoot, "script-debug-tooling.state.json");
        File.WriteAllText(
            statePath,
            WriteState(
                workspace,
                scriptMutation,
                diagnostics,
                completions,
                breakpoint,
                debugSession,
                stack,
                locals,
                arguments,
                watchDefinitions,
                watchEvaluations,
                linkedTransactions,
                linkedJobs,
                linkedArtifacts).ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");

        var visual = ScriptDebugToolingVisualHarness.WriteArtifacts(
            scriptMutation,
            diagnostics,
            completions,
            breakpoint,
            debugSession,
            stack,
            locals,
            arguments,
            watchDefinitions,
            watchEvaluations,
            currentTask: "T-0161",
            linkedTransactions,
            linkedJobs,
            linkedArtifacts,
            Path.Combine(fullWorkRoot, "visual"));

        return new ScriptDebugToolingSmokeResult(
            "Script",
            scriptMutation,
            diagnostics,
            completions,
            breakpoint,
            debugSession,
            stack,
            locals,
            arguments,
            watchDefinitions,
            watchEvaluations,
            "T-0161",
            linkedTransactions,
            linkedJobs,
            linkedArtifacts,
            statePath,
            visual.ScreenshotPath,
            visual.AnalysisPath,
            visual.TextOverflowCount,
            visual.ClickableControlCount,
            visual.ForbiddenUiMatchCount,
            visual.ScreenshotReviewed);
    }

    private static JsonObject WriteState(
        ProjectWorkspace workspace,
        ToolingScriptMutationResult scriptMutation,
        ToolingScriptIdeResult diagnostics,
        ToolingScriptIdeResult completions,
        ToolingDebugCommandResult breakpoint,
        ToolingDebugSessionResult debugSession,
        ToolingDebugStackResult stack,
        ToolingDebugVariablesResult locals,
        ToolingDebugVariablesResult arguments,
        ToolingDebugWatchesResult watchDefinitions,
        ToolingDebugWatchesResult watchEvaluations,
        IReadOnlyList<string> linkedTransactions,
        IReadOnlyList<string> linkedJobs,
        IReadOnlyList<string> linkedArtifacts)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.ScriptDebugToolingState",
            ["selectedWorkspace"] = "Script",
            ["workspaceRevision"] = workspace.Revisions.WorkspaceRevision.Value,
            ["contentRevision"] = workspace.Revisions.ContentRevision.Value,
            ["scriptOperation"] = scriptMutation.Operation.OperationKind,
            ["scriptDocumentRevision"] = completions.DocumentRevision.Value,
            ["scriptSemanticVersion"] = completions.SemanticVersion,
            ["roslynSemanticModel"] = completions.RoslynSemanticModel,
            ["workspaceSnapshotUsedForIde"] = completions.WorkspaceSnapshotUsedForIde,
            ["diagnosticCode"] = diagnostics.Diagnostic?.Code,
            ["completionSelected"] = completions.CompletionItems.FirstOrDefault(item => item.IsSelected)?.DisplayText,
            ["breakpointId"] = breakpoint.Breakpoint?.BreakpointId,
            ["threadCount"] = debugSession.Threads.Count,
            ["stackThreadCount"] = stack.StacksByThread.Count,
            ["localValue"] = FormatVariable(locals.Variables.Single(variable => variable.Name == "speed")),
            ["argumentValue"] = FormatVariable(arguments.Variables.Single(variable => variable.Name == "delta")),
            ["watchDefinition"] = watchDefinitions.Watches.Single(watch => watch.Expression == "hero.Health").Expression,
            ["watchEvaluation"] = FormatWatch(watchEvaluations.Watches.Single(watch => watch.Expression == "hero.Health")),
            ["currentTask"] = "T-0161",
            ["linkedTransactions"] = ToJsonArray(linkedTransactions),
            ["linkedJobs"] = ToJsonArray(linkedJobs),
            ["linkedArtifacts"] = ToJsonArray(linkedArtifacts)
        };
    }

    private static ScriptDebugToolingMarkedText CreateMarkedText()
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

        var positions = new Dictionary<string, int>(StringComparer.Ordinal);
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
                throw new FormatException("Script/debug tooling marker is missing its closing token.");
            }

            var name = current[(start + 2)..end];
            positions.Add(name, start);
            current = current.Remove(start, end - start + 2);
        }

        return new ScriptDebugToolingMarkedText(current.ReplaceLineEndings("\n"), positions);
    }

    private static OperationContext AgentContext(params OperationCapability[] capabilities)
    {
        return new OperationContext(
            "agent-t0161",
            PrincipalKind.Agent,
            "agent-session-t0161",
            capabilities,
            "script-debug-tooling-smoke");
    }

    private static string FormatVariable(ToolingDebugVariable variable)
    {
        return $"{variable.Name}={variable.Value}";
    }

    private static string FormatWatch(ToolingDebugWatch watch)
    {
        return $"{watch.Expression}={watch.Value}";
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }
}

internal sealed record ScriptDebugToolingMarkedText(
    string Text,
    IReadOnlyDictionary<string, int> Positions);
