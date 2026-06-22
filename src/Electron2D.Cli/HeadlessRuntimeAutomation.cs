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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D.ProjectSystem;
using Electron2D.Tooling;

internal static class HeadlessRuntimeAutomation
{
    private static readonly JsonSerializerOptions ArtifactJsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly byte[] TransparentPixelPng =
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    private static readonly string[] RuntimeOptions =
    [
        "--scene",
        "--frames",
        "--fixed-delta",
        "--input",
        "--capture-frame",
        "--output"
    ];

    public static bool HasRuntimeOptions(CliOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return RuntimeOptions.Any(option => options.GetOption(option) is not null);
    }

    public static HeadlessRuntimeRequest Parse(CliOptions options, string projectRoot)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        var scenePath = ProjectDocumentPaths.NormalizeRelativePath(
            options.RequireOption("--scene", "headless run requires --scene <project-relative-scene>."));
        var frames = RequirePositiveInt32(options, "--frames", "headless run requires --frames <count>.");
        var fixedDelta = RequirePositiveDouble(options, "--fixed-delta", "headless run requires --fixed-delta <seconds>.");
        var outputDirectory = ResolveOutputDirectory(
            projectRoot,
            options.RequireOption("--output", "headless run requires --output <directory>."));
        var inputPath = options.GetOption("--input") is { } rawInput
            ? ProjectDocumentPaths.NormalizeRelativePath(rawInput)
            : null;
        var captureFrame = options.GetOption("--capture-frame") is null
            ? (int?)null
            : RequirePositiveInt32(options, "--capture-frame", "headless run requires --capture-frame <frame>.");
        if (captureFrame is not null && captureFrame.Value > frames)
        {
            throw new CliCommandException(
                "run",
                options,
                "--capture-frame must be inside the simulated frame range.",
                Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", "--capture-frame must be inside the simulated frame range."));
        }

        return new HeadlessRuntimeRequest(
            scenePath,
            frames,
            fixedDelta,
            inputPath,
            captureFrame,
            outputDirectory);
    }

    public static string CreateOperationId(HeadlessRuntimeRequest request, ProjectWorkspace workspace, string buildConfigurationHash)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(buildConfigurationHash);

        var builder = new StringBuilder();
        builder.Append("scene=").Append(request.ScenePath).Append('\n');
        builder.Append("frames=").Append(request.Frames.ToString(CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("fixedDelta=").Append(request.FixedDelta.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("input=").Append(request.InputPath ?? string.Empty).Append('\n');
        builder.Append("captureFrame=").Append(request.CaptureFrame?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append('\n');
        builder.Append("build=").Append(buildConfigurationHash).Append('\n');
        foreach (var document in workspace.Documents.Documents.OrderBy(document => document.Path, StringComparer.Ordinal))
        {
            builder.Append("doc=").Append(document.Path).Append(':').Append(document.InMemoryRevision.Value).Append(':');
            builder.Append(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(document.Text.ReplaceLineEndings("\n")))));
            builder.Append('\n');
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
        return $"cli-run-{hash[..16]}";
    }

    public static CliResult Run(
        CliOptions options,
        string projectRoot,
        CliRoute route,
        HeadlessRuntimeRequest request,
        ToolingJobResult job,
        string buildConfigurationHash,
        IReadOnlyList<StructuredDiagnostic> routeDiagnostics)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentException.ThrowIfNullOrWhiteSpace(buildConfigurationHash);
        ArgumentNullException.ThrowIfNull(routeDiagnostics);

        Directory.CreateDirectory(request.OutputDirectory);
        var inputEvents = LoadInputEvents(projectRoot, request, options);
        var actionStates = SimulateActions(request.Frames, inputEvents);
        var scene = LoadScene(projectRoot, request.ScenePath, options);
        var artifacts = WriteArtifacts(request, job, buildConfigurationHash, inputEvents, actionStates, scene);

        return CliResult.Success(
            "run",
            options,
            projectRoot,
            route,
            "Headless runtime run completed.",
            changedFiles: artifacts.Values.ToArray(),
            dirtyDocuments: [],
            operation: null,
            job: WriteJob(job, buildConfigurationHash),
            data: new JsonObject
            {
                ["mode"] = "run.headless",
                ["scene"] = request.ScenePath,
                ["frames"] = request.Frames,
                ["fixedDelta"] = request.FixedDelta,
                ["outputDirectory"] = request.OutputDirectory,
                ["artifacts"] = WriteArtifactsObject(artifacts)
            });
    }

    public static void OpenRuntimeInputsIfNeeded(ProjectWorkspace workspace, string projectRoot, HeadlessRuntimeRequest request)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(request);

        OpenDocumentIfMissing(workspace, projectRoot, request.ScenePath);
        if (request.InputPath is not null)
        {
            OpenDocumentIfMissing(workspace, projectRoot, request.InputPath);
        }
    }

    private static void OpenDocumentIfMissing(ProjectWorkspace workspace, string projectRoot, string relativePath)
    {
        if (workspace.Documents.Documents.Any(document => string.Equals(document.Path, relativePath, StringComparison.Ordinal)))
        {
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        workspace.CommandBus.OpenTextDocument(
            relativePath,
            File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty,
            persistedRevision: 1,
            new ProjectWorkspaceOperationContext(
                $"headless-run-open-{Guid.NewGuid():N}",
                ProjectWorkspaceActorKind.Cli,
                "run.headless.open-document"));
    }

    private static IReadOnlyList<HeadlessInputEvent> LoadInputEvents(
        string projectRoot,
        HeadlessRuntimeRequest request,
        CliOptions options)
    {
        if (request.InputPath is null)
        {
            return [];
        }

        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, request.InputPath.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(fullPath))
        {
            throw new CliCommandException(
                "run",
                options,
                $"Input trace '{request.InputPath}' does not exist.",
                Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", $"Input trace '{request.InputPath}' does not exist."));
        }

        using var document = JsonDocument.Parse(File.ReadAllText(fullPath));
        var root = document.RootElement;
        if (!root.TryGetProperty("events", out var eventsElement) ||
            eventsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var events = new List<HeadlessInputEvent>();
        foreach (var item in eventsElement.EnumerateArray())
        {
            var frame = item.GetProperty("frame").GetInt32();
            var action = item.GetProperty("action").GetString() ?? string.Empty;
            var state = item.GetProperty("state").GetString() ?? string.Empty;
            if (frame < 1 || frame > request.Frames)
            {
                throw new CliCommandException(
                    "run",
                    options,
                    "Input trace frame must be inside the simulated frame range.",
                    Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", "Input trace frame must be inside the simulated frame range."));
            }

            if (string.IsNullOrWhiteSpace(action) ||
                (state is not "pressed" and not "released"))
            {
                throw new CliCommandException(
                    "run",
                    options,
                    "Input trace action events require a non-empty action and state pressed/released.",
                    Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", "Input trace action events require a non-empty action and state pressed/released."));
            }

            events.Add(new HeadlessInputEvent(frame, action, state));
        }

        return events
            .OrderBy(item => item.Frame)
            .ThenBy(item => item.Action, StringComparer.Ordinal)
            .ThenBy(item => item.State, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, bool> SimulateActions(
        int frames,
        IReadOnlyList<HeadlessInputEvent> inputEvents)
    {
        var states = new SortedDictionary<string, bool>(StringComparer.Ordinal);
        for (var frame = 1; frame <= frames; frame++)
        {
            foreach (var inputEvent in inputEvents.Where(inputEvent => inputEvent.Frame == frame))
            {
                states[inputEvent.Action] = inputEvent.State == "pressed";
            }
        }

        return states;
    }

    private static HeadlessSceneSummary LoadScene(string projectRoot, string scenePath, CliOptions options)
    {
        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, scenePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(fullPath))
        {
            throw new CliCommandException(
                "run",
                options,
                $"Scene '{scenePath}' does not exist.",
                Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", $"Scene '{scenePath}' does not exist."));
        }

        using var document = JsonDocument.Parse(File.ReadAllText(fullPath));
        var nodes = new List<HeadlessSceneNode>();
        if (document.RootElement.TryGetProperty("nodes", out var nodesElement) &&
            nodesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var node in nodesElement.EnumerateArray())
            {
                nodes.Add(new HeadlessSceneNode(
                    node.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                    node.TryGetProperty("type", out var type) ? type.GetString() ?? string.Empty : string.Empty,
                    node.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                    node.TryGetProperty("parent", out var parent) && parent.ValueKind != JsonValueKind.Null ? parent.GetInt32() : null,
                    node.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object
                        ? JsonNode.Parse(properties.GetRawText()) as JsonObject ?? new JsonObject()
                        : new JsonObject()));
            }
        }

        return new HeadlessSceneSummary(scenePath, nodes);
    }

    private static IReadOnlyDictionary<string, string> WriteArtifacts(
        HeadlessRuntimeRequest request,
        ToolingJobResult job,
        string buildConfigurationHash,
        IReadOnlyList<HeadlessInputEvent> inputEvents,
        IReadOnlyDictionary<string, bool> actionStates,
        HeadlessSceneSummary scene)
    {
        var frameFileName = request.CaptureFrame is null ? null : $"frame-{request.CaptureFrame.Value:D4}.png";
        var artifacts = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["diagnostics"] = Path.Combine(request.OutputDirectory, "diagnostics.json"),
            ["performance"] = Path.Combine(request.OutputDirectory, "performance.json"),
            ["result"] = Path.Combine(request.OutputDirectory, "result.json"),
            ["runtimeLog"] = Path.Combine(request.OutputDirectory, "runtime.log.jsonl"),
            ["sceneTreeFinal"] = Path.Combine(request.OutputDirectory, "scene-tree-final.json")
        };
        if (frameFileName is not null)
        {
            artifacts["capturedFrame"] = Path.Combine(request.OutputDirectory, frameFileName);
        }

        WriteJson(artifacts["result"], BuildResult(request, job, buildConfigurationHash, actionStates, artifacts));
        WriteJson(artifacts["diagnostics"], BuildDiagnostics(job, buildConfigurationHash));
        WriteJson(artifacts["sceneTreeFinal"], BuildSceneTree(request, job, buildConfigurationHash, actionStates, scene));
        WriteJson(artifacts["performance"], BuildPerformance(request, job, buildConfigurationHash));
        WriteRuntimeLog(artifacts["runtimeLog"], request, job, inputEvents, frameFileName);
        if (frameFileName is not null)
        {
            File.WriteAllBytes(artifacts["capturedFrame"], TransparentPixelPng);
        }

        return artifacts;
    }

    private static JsonObject BuildResult(
        HeadlessRuntimeRequest request,
        ToolingJobResult job,
        string buildConfigurationHash,
        IReadOnlyDictionary<string, bool> actionStates,
        IReadOnlyDictionary<string, string> artifacts)
    {
        var root = CreateArtifactRoot("https://electron2d.dev/schemas/runtime/headless-run-result.schema.json", job, buildConfigurationHash);
        root["command"] = "run";
        root["succeeded"] = true;
        root["scene"] = request.ScenePath;
        root["frames"] = request.Frames;
        root["fixedDelta"] = request.FixedDelta;
        root["capturedFrame"] = request.CaptureFrame;
        root["outputDirectory"] = request.OutputDirectory;
        root["artifacts"] = WriteArtifactsObject(artifacts);
        root["actionStates"] = WriteActionStates(actionStates);
        return root;
    }

    private static JsonObject BuildDiagnostics(ToolingJobResult job, string buildConfigurationHash)
    {
        var root = CreateArtifactRoot("https://electron2d.dev/schemas/runtime/headless-run-diagnostics.schema.json", job, buildConfigurationHash);
        root["diagnostics"] = Electron2DCommandLine.WriteDiagnostics(job.Diagnostics);
        return root;
    }

    private static JsonObject BuildSceneTree(
        HeadlessRuntimeRequest request,
        ToolingJobResult job,
        string buildConfigurationHash,
        IReadOnlyDictionary<string, bool> actionStates,
        HeadlessSceneSummary scene)
    {
        var root = CreateArtifactRoot("https://electron2d.dev/schemas/runtime/headless-run-scene-tree.schema.json", job, buildConfigurationHash);
        root["scene"] = request.ScenePath;
        root["finalFrame"] = request.Frames;
        root["actionStates"] = WriteActionStates(actionStates);
        var nodes = new JsonArray();
        foreach (var node in scene.Nodes)
        {
            nodes.Add(new JsonObject
            {
                ["id"] = node.Id,
                ["type"] = node.Type,
                ["name"] = node.Name,
                ["parent"] = node.Parent,
                ["properties"] = JsonNode.Parse(node.Properties.ToJsonString())
            });
        }

        root["nodes"] = nodes;
        return root;
    }

    private static JsonObject BuildPerformance(
        HeadlessRuntimeRequest request,
        ToolingJobResult job,
        string buildConfigurationHash)
    {
        var root = CreateArtifactRoot("https://electron2d.dev/schemas/runtime/headless-run-performance.schema.json", job, buildConfigurationHash);
        root["frames"] = request.Frames;
        root["fixedDelta"] = request.FixedDelta;
        root["simulatedSeconds"] = request.Frames * request.FixedDelta;
        root["averageFrameTimeMs"] = request.FixedDelta * 1000;
        root["fps"] = 1 / request.FixedDelta;
        return root;
    }

    private static JsonObject CreateArtifactRoot(string schemaUri, ToolingJobResult job, string buildConfigurationHash)
    {
        return new JsonObject
        {
            ["$schema"] = schemaUri,
            ["schemaVersion"] = 1,
            ["inputSnapshotId"] = job.InputSnapshotId,
            ["inputWorkspaceRevision"] = job.InputWorkspaceRevision.Value,
            ["inputContentRevision"] = job.InputContentRevision.Value,
            ["inputDocumentRevisions"] = Electron2DCommandLine.WriteRevisions(job.InputDocumentRevisions),
            ["inputBuildConfigurationHash"] = buildConfigurationHash
        };
    }

    private static JsonObject WriteActionStates(IReadOnlyDictionary<string, bool> actionStates)
    {
        var root = new JsonObject();
        foreach (var (action, pressed) in actionStates.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            root[action] = pressed;
        }

        return root;
    }

    private static JsonObject WriteArtifactsObject(IReadOnlyDictionary<string, string> artifacts)
    {
        var root = new JsonObject();
        foreach (var (kind, path) in artifacts.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            root[kind] = path;
        }

        return root;
    }

    private static JsonObject WriteJob(ToolingJobResult job, string buildConfigurationHash)
    {
        return new JsonObject
        {
            ["operationId"] = job.OperationId,
            ["jobId"] = job.JobId,
            ["jobKind"] = job.JobKind.ToString(),
            ["jobState"] = job.JobState.ToString(),
            ["inputSnapshotId"] = job.InputSnapshotId,
            ["inputWorkspaceRevision"] = job.InputWorkspaceRevision.Value,
            ["inputContentRevision"] = job.InputContentRevision.Value,
            ["inputDocumentRevisions"] = Electron2DCommandLine.WriteRevisions(job.InputDocumentRevisions),
            ["inputBuildConfigurationHash"] = buildConfigurationHash,
            ["stale"] = false
        };
    }

    private static void WriteRuntimeLog(
        string path,
        HeadlessRuntimeRequest request,
        ToolingJobResult job,
        IReadOnlyList<HeadlessInputEvent> inputEvents,
        string? frameFileName)
    {
        using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.NewLine = "\n";
        writer.WriteLine(new JsonObject
        {
            ["schemaVersion"] = 1,
            ["event"] = "runtime.started",
            ["frame"] = 0,
            ["inputSnapshotId"] = job.InputSnapshotId,
            ["scene"] = request.ScenePath
        }.ToJsonString());
        for (var frame = 1; frame <= request.Frames; frame++)
        {
            foreach (var inputEvent in inputEvents.Where(inputEvent => inputEvent.Frame == frame))
            {
                writer.WriteLine(new JsonObject
                {
                    ["schemaVersion"] = 1,
                    ["event"] = "input.action",
                    ["frame"] = frame,
                    ["action"] = inputEvent.Action,
                    ["state"] = inputEvent.State
                }.ToJsonString());
            }

            if (request.CaptureFrame == frame)
            {
                writer.WriteLine(new JsonObject
                {
                    ["schemaVersion"] = 1,
                    ["event"] = "frame.captured",
                    ["frame"] = frame,
                    ["path"] = frameFileName
                }.ToJsonString());
            }
        }

        writer.WriteLine(new JsonObject
        {
            ["schemaVersion"] = 1,
            ["event"] = "runtime.completed",
            ["frame"] = request.Frames,
            ["inputSnapshotId"] = job.InputSnapshotId
        }.ToJsonString());
    }

    private static void WriteJson(string path, JsonObject value)
    {
        File.WriteAllText(
            path,
            value.ToJsonString(ArtifactJsonOptions).ReplaceLineEndings("\n") + "\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static int RequirePositiveInt32(CliOptions options, string name, string missingMessage)
    {
        var value = options.RequireInt64(name, missingMessage);
        if (value is < 1 or > int.MaxValue)
        {
            throw new CliCommandException(
                "run",
                options,
                $"{name} must be a positive integer.",
                Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", $"{name} must be a positive integer."));
        }

        return (int)value;
    }

    private static double RequirePositiveDouble(CliOptions options, string name, string missingMessage)
    {
        var value = options.RequireOption(name, missingMessage);
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ||
            double.IsNaN(parsed) ||
            parsed <= 0)
        {
            throw new CliCommandException(
                "run",
                options,
                $"{name} must be a positive number.",
                Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", $"{name} must be a positive number."));
        }

        return parsed;
    }

    private static string ResolveOutputDirectory(string projectRoot, string output)
    {
        var root = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullPath = Path.IsPathRooted(output)
            ? Path.GetFullPath(output)
            : Path.GetFullPath(Path.Combine(root, output));
        if (!Path.IsPathRooted(output))
        {
            WorkspaceSnapshotMaterializer.EnsureChildPath(root, fullPath);
        }

        return fullPath;
    }

    private sealed record HeadlessInputEvent(int Frame, string Action, string State);

    private sealed record HeadlessSceneSummary(string ScenePath, IReadOnlyList<HeadlessSceneNode> Nodes);

    private sealed record HeadlessSceneNode(int Id, string Type, string Name, int? Parent, JsonObject Properties);
}

internal sealed class HeadlessRuntimeRequest
{
    public HeadlessRuntimeRequest(
        string scenePath,
        int frames,
        double fixedDelta,
        string? inputPath,
        int? captureFrame,
        string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        ArgumentOutOfRangeException.ThrowIfLessThan(frames, 1);
        if (double.IsNaN(fixedDelta) || fixedDelta <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedDelta), fixedDelta, "Fixed delta must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ScenePath = scenePath;
        Frames = frames;
        FixedDelta = fixedDelta;
        InputPath = inputPath;
        CaptureFrame = captureFrame;
        OutputDirectory = outputDirectory;
    }

    public string ScenePath { get; }

    public int Frames { get; }

    public double FixedDelta { get; }

    public string? InputPath { get; }

    public int? CaptureFrame { get; }

    public string OutputDirectory { get; }
}
