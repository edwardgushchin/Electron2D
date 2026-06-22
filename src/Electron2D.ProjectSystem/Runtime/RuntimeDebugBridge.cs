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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal enum RuntimeDebugSessionKind
{
    HeadlessPreview,
    EditorAttachedPreview
}

internal enum RuntimeDebugSessionState
{
    Running,
    Paused,
    Stopped,
    Crashed
}

internal sealed class RuntimeDebugStartRequest
{
    public RuntimeDebugStartRequest(
        string projectRoot,
        string scenePath,
        RuntimeDebugSessionKind sessionKind,
        bool developmentMode,
        string buildConfigurationHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(buildConfigurationHash);

        ProjectRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        ScenePath = ProjectDocumentPaths.NormalizeRelativePath(scenePath);
        SessionKind = sessionKind;
        DevelopmentMode = developmentMode;
        BuildConfigurationHash = buildConfigurationHash;
    }

    public string ProjectRoot { get; }

    public string ScenePath { get; }

    public RuntimeDebugSessionKind SessionKind { get; }

    public bool DevelopmentMode { get; }

    public string BuildConfigurationHash { get; }
}

internal sealed class RuntimeDebugStartResult
{
    private RuntimeDebugStartResult(
        RuntimeDebugSession? session,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        Session = session;
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded => Session is not null && Diagnostics.Count == 0;

    public RuntimeDebugSession? Session { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public static RuntimeDebugStartResult Success(RuntimeDebugSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new RuntimeDebugStartResult(session, []);
    }

    public static RuntimeDebugStartResult Failed(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return new RuntimeDebugStartResult(null, diagnostics.ToArray());
    }
}

internal sealed class RuntimeDebugCommandResult
{
    private RuntimeDebugCommandResult(
        bool succeeded,
        RuntimeDebugNodeSnapshot? node,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        Succeeded = succeeded;
        Node = node;
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public RuntimeDebugNodeSnapshot? Node { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public static RuntimeDebugCommandResult Success(RuntimeDebugNodeSnapshot? node = null)
    {
        return new RuntimeDebugCommandResult(true, node, []);
    }

    public static RuntimeDebugCommandResult Failed(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return new RuntimeDebugCommandResult(false, null, diagnostics.ToArray());
    }
}

internal sealed class RuntimeDebugSession
{
    private readonly SortedDictionary<string, bool> inputActions = new(StringComparer.Ordinal);
    private readonly IReadOnlyList<RuntimeDebugNodeSnapshot> nodes;

    internal RuntimeDebugSession(
        string sessionId,
        string projectRoot,
        string scene,
        RuntimeDebugSessionKind sessionKind,
        string buildConfigurationHash,
        IReadOnlyList<RuntimeDebugNodeSnapshot> nodes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(scene);
        ArgumentException.ThrowIfNullOrWhiteSpace(buildConfigurationHash);
        ArgumentNullException.ThrowIfNull(nodes);

        SessionId = sessionId;
        ProjectRoot = projectRoot;
        Scene = scene;
        SessionKind = sessionKind;
        BuildConfigurationHash = buildConfigurationHash;
        this.nodes = nodes.ToArray();
        State = RuntimeDebugSessionState.Running;
    }

    public string SessionId { get; }

    public string ProjectRoot { get; }

    public string Scene { get; }

    public RuntimeDebugSessionKind SessionKind { get; }

    public RuntimeDebugSessionState State { get; private set; }

    public int CurrentFrame { get; private set; }

    public int CurrentPhysicsFrame { get; private set; }

    public double SimulatedSeconds { get; private set; }

    public double LastFrameDelta { get; private set; }

    public double LastPhysicsDelta { get; private set; }

    public string BuildConfigurationHash { get; }

    public IReadOnlyDictionary<string, bool> InputActions =>
        new ReadOnlyDictionary<string, bool>(inputActions);

    public IReadOnlyList<StructuredDiagnostic> Diagnostics => [];

    public void Pause()
    {
        EnsureNotStopped();
        State = RuntimeDebugSessionState.Paused;
    }

    public void Resume()
    {
        EnsureNotStopped();
        State = RuntimeDebugSessionState.Running;
    }

    public void Stop()
    {
        State = RuntimeDebugSessionState.Stopped;
    }

    public void MarkCrashed()
    {
        State = RuntimeDebugSessionState.Crashed;
    }

    public void StepFrame(int count, double fixedDelta)
    {
        EnsureNotStopped();
        ValidateStep(count, fixedDelta, nameof(fixedDelta));

        CurrentFrame += count;
        LastFrameDelta = fixedDelta;
        SimulatedSeconds += count * fixedDelta;
    }

    public void StepPhysics(int count, double fixedDelta)
    {
        EnsureNotStopped();
        ValidateStep(count, fixedDelta, nameof(fixedDelta));

        CurrentPhysicsFrame += count;
        LastPhysicsDelta = fixedDelta;
    }

    public void InjectInput(string action, bool pressed)
    {
        EnsureNotStopped();
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        inputActions[action] = pressed;
    }

    public RuntimeDebugSceneTreeSnapshot GetSceneTree()
    {
        return new RuntimeDebugSceneTreeSnapshot(
            Scene,
            CurrentFrame,
            CurrentPhysicsFrame,
            nodes);
    }

    public RuntimeDebugCommandResult InspectNode(string nodePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodePath);

        var normalized = NormalizeNodePath(nodePath);
        var node = nodes.FirstOrDefault(node => string.Equals(node.Path, normalized, StringComparison.Ordinal));
        return node is null
            ? RuntimeDebugCommandResult.Failed([RuntimeDebugBridge.CreateDiagnostic($"Runtime node '{normalized}' was not found.", nodePath: normalized)])
            : RuntimeDebugCommandResult.Success(node);
    }

    public RuntimeDebugCommandResult TrySetNodeProperty(string nodePath, string propertyName, JsonNode value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(value);

        return RuntimeDebugCommandResult.Failed(
        [
            RuntimeDebugBridge.CreateDiagnostic(
                "Runtime debug bridge does not mutate arbitrary node properties in the current Preview scope.",
                nodePath: NormalizeNodePath(nodePath))
        ]);
    }

    public RuntimeDebugMetricsSnapshot GetMetrics()
    {
        return new RuntimeDebugMetricsSnapshot(
            State,
            CurrentFrame,
            CurrentPhysicsFrame,
            SimulatedSeconds,
            LastFrameDelta,
            LastPhysicsDelta,
            LastFrameDelta > 0 ? 1 / LastFrameDelta : 0);
    }

    public RuntimeDebugScreenshot CaptureScreenshot()
    {
        EnsureNotStopped();
        return RuntimeDebugScreenshot.Create(CurrentFrame, CurrentPhysicsFrame);
    }

    private void EnsureNotStopped()
    {
        if (State is RuntimeDebugSessionState.Stopped or RuntimeDebugSessionState.Crashed)
        {
            throw new InvalidOperationException("Runtime debug session is not running.");
        }
    }

    private static void ValidateStep(int count, double fixedDelta, string parameterName)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        if (double.IsNaN(fixedDelta) || fixedDelta <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, fixedDelta, "Runtime debug step delta must be positive.");
        }
    }

    private static string NormalizeNodePath(string nodePath)
    {
        var trimmed = nodePath.Trim();
        return trimmed.StartsWith("/", StringComparison.Ordinal) ? trimmed : "/" + trimmed;
    }
}

internal sealed class RuntimeDebugSceneTreeSnapshot
{
    public RuntimeDebugSceneTreeSnapshot(
        string scene,
        int currentFrame,
        int currentPhysicsFrame,
        IEnumerable<RuntimeDebugNodeSnapshot> nodes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scene);
        ArgumentNullException.ThrowIfNull(nodes);

        Scene = scene;
        CurrentFrame = currentFrame;
        CurrentPhysicsFrame = currentPhysicsFrame;
        Nodes = nodes.ToArray();
    }

    public string Scene { get; }

    public int CurrentFrame { get; }

    public int CurrentPhysicsFrame { get; }

    public IReadOnlyList<RuntimeDebugNodeSnapshot> Nodes { get; }
}

internal sealed class RuntimeDebugNodeSnapshot
{
    public RuntimeDebugNodeSnapshot(
        int id,
        string type,
        string name,
        string path,
        string? parentPath,
        JsonObject properties,
        bool? visible)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(properties);

        Id = id;
        Type = type;
        Name = name;
        Path = path;
        ParentPath = parentPath;
        Properties = properties;
        Visible = visible;
    }

    public int Id { get; }

    public string Type { get; }

    public string Name { get; }

    public string Path { get; }

    public string? ParentPath { get; }

    public JsonObject Properties { get; }

    public bool? Visible { get; }
}

internal sealed record RuntimeDebugMetricsSnapshot(
    RuntimeDebugSessionState State,
    int CurrentFrame,
    int CurrentPhysicsFrame,
    double SimulatedSeconds,
    double LastFrameDelta,
    double LastPhysicsDelta,
    double Fps);

internal sealed class RuntimeDebugScreenshot
{
    private static readonly byte[] TransparentPixelPng =
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    private RuntimeDebugScreenshot(
        int frame,
        int physicsFrame,
        byte[] bytes,
        string sha256)
    {
        Frame = frame;
        PhysicsFrame = physicsFrame;
        Bytes = bytes;
        Sha256 = sha256;
    }

    public int Frame { get; }

    public int PhysicsFrame { get; }

    public string ContentType => "image/png";

    public byte[] Bytes { get; }

    public string Sha256 { get; }

    public static RuntimeDebugScreenshot Create(int frame, int physicsFrame)
    {
        var bytes = TransparentPixelPng.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return new RuntimeDebugScreenshot(frame, physicsFrame, bytes, hash);
    }
}

internal static class RuntimeDebugBridge
{
    public static RuntimeDebugStartResult Start(RuntimeDebugStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.DevelopmentMode)
        {
            return RuntimeDebugStartResult.Failed(
            [
                CreateDiagnostic("Runtime debug bridge is available only in development/debug mode.", file: request.ScenePath)
            ]);
        }

        var fullScenePath = Path.GetFullPath(Path.Combine(request.ProjectRoot, request.ScenePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(fullScenePath))
        {
            return RuntimeDebugStartResult.Failed(
            [
                CreateDiagnostic($"Runtime scene '{request.ScenePath}' does not exist.", file: request.ScenePath)
            ]);
        }

        try
        {
            var nodes = RuntimeDebugSceneParser.Parse(request.ScenePath, File.ReadAllText(fullScenePath));
            var sessionId = CreateSessionId(request, nodes);
            return RuntimeDebugStartResult.Success(new RuntimeDebugSession(
                sessionId,
                request.ProjectRoot,
                request.ScenePath,
                request.SessionKind,
                request.BuildConfigurationHash,
                nodes));
        }
        catch (JsonException exception)
        {
            return RuntimeDebugStartResult.Failed(
            [
                CreateDiagnostic($"Runtime scene '{request.ScenePath}' could not be parsed: {exception.Message}", file: request.ScenePath)
            ]);
        }
    }

    internal static StructuredDiagnostic CreateDiagnostic(
        string message,
        string? file = null,
        string? nodePath = null)
    {
        return StructuredDiagnostic.Create(
            "E2D-RUNTIME-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Runtime,
            message,
            location: file is null && nodePath is null ? null : new DiagnosticLocation(file: file, nodePath: nodePath),
            relatedLocations: [],
            suggestedFixes: []);
    }

    private static string CreateSessionId(RuntimeDebugStartRequest request, IReadOnlyList<RuntimeDebugNodeSnapshot> nodes)
    {
        var fingerprint = string.Join(
            "\n",
            nodes.Select(node => string.Create(
                CultureInfo.InvariantCulture,
                $"{node.Id}:{node.Path}:{node.Type}:{node.Properties.ToJsonString()}")));
        var material = $"{request.ProjectRoot}\n{request.ScenePath}\n{request.SessionKind}\n{request.BuildConfigurationHash}\n{fingerprint}";
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
        return $"runtime-debug-{hash[..16]}";
    }
}

internal static class RuntimeDebugJsonSerializer
{
    public static JsonObject ToJson(RuntimeDebugSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new JsonObject
        {
            ["sessionId"] = session.SessionId,
            ["sessionKind"] = session.SessionKind.ToString(),
            ["scene"] = session.Scene,
            ["state"] = session.State.ToString(),
            ["currentFrame"] = session.CurrentFrame,
            ["currentPhysicsFrame"] = session.CurrentPhysicsFrame,
            ["inputActions"] = WriteInputActions(session.InputActions),
            ["buildConfigurationHash"] = session.BuildConfigurationHash
        };
    }

    public static JsonObject ToJson(RuntimeDebugSceneTreeSnapshot sceneTree)
    {
        ArgumentNullException.ThrowIfNull(sceneTree);

        var nodes = new JsonArray();
        foreach (var node in sceneTree.Nodes)
        {
            nodes.Add(ToJson(node));
        }

        return new JsonObject
        {
            ["scene"] = sceneTree.Scene,
            ["currentFrame"] = sceneTree.CurrentFrame,
            ["currentPhysicsFrame"] = sceneTree.CurrentPhysicsFrame,
            ["nodes"] = nodes
        };
    }

    public static JsonObject ToJson(RuntimeDebugNodeSnapshot node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return new JsonObject
        {
            ["id"] = node.Id,
            ["type"] = node.Type,
            ["name"] = node.Name,
            ["path"] = node.Path,
            ["parentPath"] = node.ParentPath,
            ["visible"] = node.Visible,
            ["properties"] = Clone(node.Properties)
        };
    }

    public static JsonObject ToJson(RuntimeDebugMetricsSnapshot metrics)
    {
        return new JsonObject
        {
            ["state"] = metrics.State.ToString(),
            ["currentFrame"] = metrics.CurrentFrame,
            ["currentPhysicsFrame"] = metrics.CurrentPhysicsFrame,
            ["simulatedSeconds"] = metrics.SimulatedSeconds,
            ["lastFrameDelta"] = metrics.LastFrameDelta,
            ["lastPhysicsDelta"] = metrics.LastPhysicsDelta,
            ["fps"] = metrics.Fps
        };
    }

    public static JsonObject ToJson(RuntimeDebugScreenshot screenshot, string? path)
    {
        ArgumentNullException.ThrowIfNull(screenshot);

        return new JsonObject
        {
            ["contentType"] = screenshot.ContentType,
            ["frame"] = screenshot.Frame,
            ["physicsFrame"] = screenshot.PhysicsFrame,
            ["byteLength"] = screenshot.Bytes.Length,
            ["sha256"] = screenshot.Sha256,
            ["path"] = path
        };
    }

    private static JsonObject WriteInputActions(IReadOnlyDictionary<string, bool> actions)
    {
        var root = new JsonObject();
        foreach (var (action, pressed) in actions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            root[action] = pressed;
        }

        return root;
    }

    private static JsonNode Clone(JsonNode node)
    {
        return JsonNode.Parse(node.ToJsonString()) ?? throw new InvalidOperationException("Runtime debug JSON node could not be cloned.");
    }
}

internal static class RuntimeDebugSceneParser
{
    public static IReadOnlyList<RuntimeDebugNodeSnapshot> Parse(string scenePath, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        ArgumentNullException.ThrowIfNull(text);

        using var document = JsonDocument.Parse(text);
        if (!document.RootElement.TryGetProperty("nodes", out var nodesElement) ||
            nodesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var rawNodes = nodesElement
            .EnumerateArray()
            .Select(ReadRawNode)
            .OrderBy(node => node.Id)
            .ToArray();
        var byId = rawNodes.ToDictionary(node => node.Id);
        var paths = new Dictionary<int, string>();

        string PathFor(RuntimeDebugRawNode node)
        {
            if (paths.TryGetValue(node.Id, out var existing))
            {
                return existing;
            }

            var path = node.ParentId is { } parentId && byId.TryGetValue(parentId, out var parent)
                ? PathFor(parent).TrimEnd('/') + "/" + node.Name
                : "/" + node.Name;
            paths[node.Id] = path;
            return path;
        }

        var snapshots = new List<RuntimeDebugNodeSnapshot>();
        foreach (var node in rawNodes)
        {
            var parentPath = node.ParentId is { } parentId && byId.TryGetValue(parentId, out var parent)
                ? PathFor(parent)
                : null;
            var path = PathFor(node);
            snapshots.Add(new RuntimeDebugNodeSnapshot(
                node.Id,
                node.Type,
                node.Name,
                path,
                parentPath,
                CloneObject(node.Properties),
                ReadVisible(node.Properties)));
        }

        return snapshots;
    }

    private static RuntimeDebugRawNode ReadRawNode(JsonElement node)
    {
        var id = node.TryGetProperty("id", out var idElement) ? idElement.GetInt32() : 0;
        var type = node.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? "Electron2D.Node" : "Electron2D.Node";
        var name = node.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? $"Node{id}" : $"Node{id}";
        var parent = node.TryGetProperty("parent", out var parentElement) && parentElement.ValueKind != JsonValueKind.Null
            ? parentElement.GetInt32()
            : (int?)null;
        var properties = node.TryGetProperty("properties", out var propertiesElement) && propertiesElement.ValueKind == JsonValueKind.Object
            ? JsonNode.Parse(propertiesElement.GetRawText()) as JsonObject ?? new JsonObject()
            : new JsonObject();

        return new RuntimeDebugRawNode(id, type, name, parent, properties);
    }

    private static bool? ReadVisible(JsonObject properties)
    {
        return properties.TryGetPropertyValue("visible", out var visibleNode) &&
            visibleNode is JsonObject visibleObject &&
            visibleObject.TryGetPropertyValue("value", out var valueNode) &&
            valueNode is JsonValue value &&
            value.TryGetValue<bool>(out var visible)
                ? visible
                : null;
    }

    private static JsonObject CloneObject(JsonObject node)
    {
        return JsonNode.Parse(node.ToJsonString()) as JsonObject ?? new JsonObject();
    }

    private sealed record RuntimeDebugRawNode(
        int Id,
        string Type,
        string Name,
        int? ParentId,
        JsonObject Properties);
}
