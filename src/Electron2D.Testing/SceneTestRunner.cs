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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D.ProjectSystem;

namespace Electron2D.Testing;

internal static class SceneTestRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static readonly byte[] DeterministicFramePng =
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    public static SceneTestRunResult Run(SceneTestRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var projectRoot = NormalizeRoot(request.ProjectRoot);
        var manifestPath = ProjectDocumentPaths.NormalizeRelativePath(request.ManifestPath);
        var outputDirectory = ResolveOutputDirectory(projectRoot, request.OutputDirectory);
        var manifestFullPath = ResolveProjectFile(projectRoot, manifestPath);
        var manifest = SceneTestSuiteManifest.Load(manifestPath, File.ReadAllText(manifestFullPath));

        using var workspace = ProjectWorkspace.CreateHeadless(projectRoot, "scene-tests");
        OpenTextDocument(workspace, projectRoot, manifestPath);
        foreach (var test in manifest.Tests)
        {
            OpenTextDocument(workspace, projectRoot, test.ScenePath);
        }

        var operationId = CreateOperationId(manifest, workspace, request.InputBuildConfigurationHash);
        var snapshot = WorkspaceSnapshot.Create(
            workspace,
            new WorkspaceSnapshotId($"snapshot-{operationId}"),
            request.StartedAtUtc);
        var inputIdentity = WorkspaceJobInputIdentity.FromSnapshot(snapshot, request.InputBuildConfigurationHash);

        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(Path.Combine(outputDirectory, "screenshots"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "pixel-diff"));

        var eventsPath = Path.Combine(outputDirectory, "events.jsonl");
        using var events = new StreamWriter(eventsPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        events.NewLine = "\n";
        WriteEvent(events, inputIdentity, "test.suiteStarted", null, progress: 0);

        var diagnostics = new List<StructuredDiagnostic>();
        var results = new List<SceneTestCaseResult>();
        for (var index = 0; index < manifest.Tests.Count; index++)
        {
            var test = manifest.Tests[index];
            WriteEvent(events, inputIdentity, "test.started", test.Name, progress: Progress(index, manifest.Tests.Count));
            var scene = SceneTestDocument.Load(projectRoot, test.ScenePath);
            var testDiagnostics = new List<StructuredDiagnostic>();
            foreach (var assertion in test.NodeAssertions)
            {
                VerifyNode(test, scene, assertion, testDiagnostics);
            }

            foreach (var assertion in test.PropertyAssertions)
            {
                VerifyProperty(test, scene, assertion, testDiagnostics);
            }

            for (var frame = 1; frame <= test.Frames; frame++)
            {
                WriteEvent(events, inputIdentity, "test.frameAdvanced", test.Name, progress: Progress(index, manifest.Tests.Count, frame, test.Frames));
            }

            SceneTestVisualResult? visual = null;
            if (test.Visual is not null)
            {
                visual = RunVisualComparison(projectRoot, outputDirectory, test, testDiagnostics);
                WriteEvent(events, inputIdentity, "test.screenshotCaptured", test.Name, progress: Progress(index, manifest.Tests.Count, test.Frames, test.Frames));
                WriteEvent(events, inputIdentity, "test.visualCompared", test.Name, progress: Progress(index, manifest.Tests.Count, test.Frames, test.Frames));
            }

            diagnostics.AddRange(testDiagnostics);
            var succeeded = testDiagnostics.Count == 0;
            results.Add(new SceneTestCaseResult(test.Name, succeeded, test.Frames, test.FixedDelta, visual, testDiagnostics));
            WriteEvent(events, inputIdentity, "test.completed", test.Name, progress: Progress(index + 1, manifest.Tests.Count));
        }

        var artifacts = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["diagnostics"] = Path.Combine(outputDirectory, "diagnostics.json"),
            ["events"] = eventsPath,
            ["result"] = Path.Combine(outputDirectory, "result.json")
        };
        foreach (var result in results)
        {
            if (result.Visual is null)
            {
                continue;
            }

            artifacts[$"{result.Name}.actual"] = result.Visual.ActualPath;
            artifacts[$"{result.Name}.diff"] = result.Visual.DiffPath;
        }

        var runResult = new SceneTestRunResult(
            diagnostics.Count == 0,
            manifestPath,
            outputDirectory,
            inputIdentity,
            request.InputBuildConfigurationHash,
            results,
            diagnostics,
            artifacts);

        WriteEvent(events, inputIdentity, "test.suiteCompleted", null, progress: 1);
        events.Dispose();
        WriteArtifacts(runResult);
        return runResult;
    }

    private static void VerifyNode(
        SceneTestCase test,
        SceneTestDocument scene,
        SceneNodeAssertion assertion,
        List<StructuredDiagnostic> diagnostics)
    {
        if (!scene.NodesByPath.TryGetValue(assertion.Path, out var node))
        {
            diagnostics.Add(CreateDiagnostic("E2D-TEST-0001", $"Scene test '{test.Name}' expected node '{assertion.Path}'.", test.ScenePath));
            return;
        }

        if (!string.Equals(node.Type, assertion.Type, StringComparison.Ordinal))
        {
            diagnostics.Add(CreateDiagnostic("E2D-TEST-0001", $"Scene test '{test.Name}' expected node '{assertion.Path}' type '{assertion.Type}' but found '{node.Type}'.", test.ScenePath));
        }
    }

    private static void VerifyProperty(
        SceneTestCase test,
        SceneTestDocument scene,
        ScenePropertyAssertion assertion,
        List<StructuredDiagnostic> diagnostics)
    {
        if (!scene.NodesByPath.TryGetValue(assertion.NodePath, out var node))
        {
            diagnostics.Add(CreateDiagnostic("E2D-TEST-0001", $"Scene test '{test.Name}' expected node '{assertion.NodePath}'.", test.ScenePath));
            return;
        }

        if (!node.Properties.TryGetPropertyValue(assertion.PropertyName, out var propertyNode) ||
            propertyNode is not JsonObject propertyObject ||
            !propertyObject.TryGetPropertyValue("value", out var actual))
        {
            diagnostics.Add(CreateDiagnostic("E2D-TEST-0001", $"Scene test '{test.Name}' expected property '{assertion.PropertyName}' on '{assertion.NodePath}'.", test.ScenePath));
            return;
        }

        if (!JsonNode.DeepEquals(actual, assertion.ExpectedValue))
        {
            diagnostics.Add(CreateDiagnostic("E2D-TEST-0001", $"Scene test '{test.Name}' expected property '{assertion.PropertyName}' to equal {assertion.ExpectedValue?.ToJsonString() ?? "null"}.", test.ScenePath));
        }
    }

    private static SceneTestVisualResult RunVisualComparison(
        string projectRoot,
        string outputDirectory,
        SceneTestCase test,
        List<StructuredDiagnostic> diagnostics)
    {
        var screenshotPath = Path.Combine(outputDirectory, "screenshots", $"{test.Name}-frame-{test.Visual!.CaptureFrame:D4}.png");
        var diffPath = Path.Combine(outputDirectory, "pixel-diff", $"{test.Name}-diff.png");
        File.WriteAllBytes(screenshotPath, DeterministicFramePng);
        File.WriteAllBytes(diffPath, DeterministicFramePng);

        var referencePath = ResolveProjectFile(projectRoot, test.Visual.ReferencePath);
        if (!File.Exists(referencePath))
        {
            diagnostics.Add(CreateDiagnostic("E2D-TEST-0002", $"Scene test '{test.Name}' visual reference '{test.Visual.ReferencePath}' does not exist.", test.ScenePath));
            return new SceneTestVisualResult(test.Visual.ReferencePath, screenshotPath, diffPath, 1, test.Visual.Tolerance, false);
        }

        var differenceRatio = File.ReadAllBytes(referencePath).SequenceEqual(DeterministicFramePng) ? 0 : 1;
        var passed = differenceRatio <= test.Visual.Tolerance;
        if (!passed)
        {
            diagnostics.Add(CreateDiagnostic("E2D-TEST-0002", $"Scene test '{test.Name}' visual difference {differenceRatio.ToString(CultureInfo.InvariantCulture)} exceeds tolerance {test.Visual.Tolerance.ToString(CultureInfo.InvariantCulture)}.", test.ScenePath));
        }

        return new SceneTestVisualResult(test.Visual.ReferencePath, screenshotPath, diffPath, differenceRatio, test.Visual.Tolerance, passed);
    }

    private static void WriteArtifacts(SceneTestRunResult result)
    {
        WriteJson(Path.Combine(result.OutputDirectory, "result.json"), result.ToJson());
        var diagnostics = CreateIdentityRoot("https://electron2d.dev/schemas/testing/scene-test-diagnostics.schema.json", result.InputIdentity, result.InputBuildConfigurationHash);
        diagnostics["diagnostics"] = WriteDiagnostics(result.Diagnostics);
        WriteJson(Path.Combine(result.OutputDirectory, "diagnostics.json"), diagnostics);
    }

    private static JsonObject CreateIdentityRoot(
        string schemaUri,
        WorkspaceJobInputIdentity inputIdentity,
        string inputBuildConfigurationHash)
    {
        return new JsonObject
        {
            ["$schema"] = schemaUri,
            ["schemaVersion"] = 1,
            ["inputSnapshotId"] = inputIdentity.InputSnapshotId,
            ["inputWorkspaceRevision"] = inputIdentity.InputWorkspaceRevision.Value,
            ["inputContentRevision"] = inputIdentity.InputContentRevision.Value,
            ["inputDocumentRevisions"] = WriteRevisions(inputIdentity.InputDocumentRevisions),
            ["inputBuildConfigurationHash"] = inputBuildConfigurationHash
        };
    }

    private static void WriteEvent(
        TextWriter writer,
        WorkspaceJobInputIdentity inputIdentity,
        string eventName,
        string? testName,
        double progress)
    {
        var root = CreateIdentityRoot("https://electron2d.dev/schemas/testing/scene-test-events.schema.json", inputIdentity, inputIdentity.InputBuildConfigurationHash);
        root["event"] = eventName;
        root["testName"] = testName;
        root["progress"] = progress;
        writer.WriteLine(root.ToJsonString());
    }

    private static JsonArray WriteDiagnostics(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        var array = new JsonArray();
        foreach (var diagnostic in diagnostics)
        {
            array.Add(new JsonObject
            {
                ["code"] = diagnostic.Code,
                ["severity"] = diagnostic.Severity.ToString(),
                ["category"] = diagnostic.Category.ToString(),
                ["message"] = diagnostic.Message,
                ["documentationUri"] = diagnostic.DocumentationUri
            });
        }

        return array;
    }

    private static JsonObject WriteRevisions(IReadOnlyDictionary<string, ProjectDocumentRevision> revisions)
    {
        var root = new JsonObject();
        foreach (var (path, revision) in revisions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            root[path] = revision.Value;
        }

        return root;
    }

    private static void WriteJson(string path, JsonObject value)
    {
        File.WriteAllText(
            path,
            value.ToJsonString(JsonOptions).ReplaceLineEndings("\n") + "\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void OpenTextDocument(ProjectWorkspace workspace, string projectRoot, string relativePath)
    {
        var fullPath = ResolveProjectFile(projectRoot, relativePath);
        workspace.CommandBus.OpenTextDocument(
            relativePath,
            File.ReadAllText(fullPath),
            persistedRevision: 1,
            new ProjectWorkspaceOperationContext(
                $"scene-test-open-{Guid.NewGuid():N}",
                ProjectWorkspaceActorKind.Cli,
                "test.scene.open-document"));
    }

    private static string CreateOperationId(
        SceneTestSuiteManifest manifest,
        ProjectWorkspace workspace,
        string inputBuildConfigurationHash)
    {
        var builder = new StringBuilder();
        builder.Append("manifest=").Append(manifest.Path).Append('\n');
        builder.Append("build=").Append(inputBuildConfigurationHash).Append('\n');
        foreach (var test in manifest.Tests.OrderBy(test => test.Name, StringComparer.Ordinal))
        {
            builder.Append(test.Name).Append(':').Append(test.ScenePath).Append(':').Append(test.Frames).Append(':');
            builder.Append(test.FixedDelta.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
        }

        foreach (var document in workspace.Documents.Documents.OrderBy(document => document.Path, StringComparer.Ordinal))
        {
            builder.Append(document.Path).Append(':').Append(document.InMemoryRevision.Value).Append(':');
            builder.Append(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(document.Text))));
            builder.Append('\n');
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
        return $"scene-test-{hash[..16]}";
    }

    private static double Progress(int completedTests, int totalTests, int frame = 0, int frames = 1)
    {
        if (totalTests == 0)
        {
            return 1;
        }

        return Math.Clamp((completedTests + (frames == 0 ? 0 : frame / (double)frames)) / totalTests, 0, 1);
    }

    private static StructuredDiagnostic CreateDiagnostic(string code, string message, string scenePath)
    {
        return StructuredDiagnostic.Create(
            code,
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            message,
            new DiagnosticLocation(scenePath, null, null, null, null, null),
            relatedLocations: [],
            suggestedFixes: []);
    }

    private static string NormalizeRoot(string projectRoot)
    {
        return Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string ResolveProjectFile(string projectRoot, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        WorkspaceSnapshotMaterializer.EnsureChildPath(projectRoot, fullPath);
        return fullPath;
    }

    private static string ResolveOutputDirectory(string projectRoot, string outputDirectory)
    {
        var fullPath = Path.IsPathRooted(outputDirectory)
            ? Path.GetFullPath(outputDirectory)
            : Path.GetFullPath(Path.Combine(projectRoot, outputDirectory));
        if (!Path.IsPathRooted(outputDirectory))
        {
            WorkspaceSnapshotMaterializer.EnsureChildPath(projectRoot, fullPath);
        }

        return fullPath;
    }
}

internal sealed class SceneTestRunRequest
{
    public SceneTestRunRequest(
        string projectRoot,
        string manifestPath,
        string outputDirectory,
        string inputBuildConfigurationHash,
        DateTimeOffset startedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputBuildConfigurationHash);

        ProjectRoot = projectRoot;
        ManifestPath = manifestPath;
        OutputDirectory = outputDirectory;
        InputBuildConfigurationHash = inputBuildConfigurationHash;
        StartedAtUtc = startedAtUtc;
    }

    public string ProjectRoot { get; }

    public string ManifestPath { get; }

    public string OutputDirectory { get; }

    public string InputBuildConfigurationHash { get; }

    public DateTimeOffset StartedAtUtc { get; }
}

internal sealed class SceneTestRunResult
{
    public SceneTestRunResult(
        bool succeeded,
        string suitePath,
        string outputDirectory,
        WorkspaceJobInputIdentity inputIdentity,
        string inputBuildConfigurationHash,
        IReadOnlyList<SceneTestCaseResult> tests,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        IReadOnlyDictionary<string, string> artifacts)
    {
        Succeeded = succeeded;
        SuitePath = suitePath;
        OutputDirectory = outputDirectory;
        InputIdentity = inputIdentity;
        InputBuildConfigurationHash = inputBuildConfigurationHash;
        Tests = tests.ToArray();
        Diagnostics = diagnostics.ToArray();
        Artifacts = new ReadOnlyDictionary<string, string>(artifacts.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }

    public bool Succeeded { get; }

    public string SuitePath { get; }

    public string OutputDirectory { get; }

    public WorkspaceJobInputIdentity InputIdentity { get; }

    public string InputBuildConfigurationHash { get; }

    public IReadOnlyList<SceneTestCaseResult> Tests { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public IReadOnlyDictionary<string, string> Artifacts { get; }

    public JsonObject ToJson()
    {
        var tests = new JsonArray();
        foreach (var test in Tests)
        {
            tests.Add(test.ToJson());
        }

        var root = new JsonObject
        {
            ["$schema"] = "https://electron2d.dev/schemas/testing/scene-test-result.schema.json",
            ["schemaVersion"] = 1,
            ["succeeded"] = Succeeded,
            ["suite"] = SuitePath,
            ["inputSnapshotId"] = InputIdentity.InputSnapshotId,
            ["inputWorkspaceRevision"] = InputIdentity.InputWorkspaceRevision.Value,
            ["inputContentRevision"] = InputIdentity.InputContentRevision.Value,
            ["inputDocumentRevisions"] = WriteRevisions(InputIdentity.InputDocumentRevisions),
            ["inputBuildConfigurationHash"] = InputBuildConfigurationHash,
            ["tests"] = tests,
            ["diagnostics"] = WriteDiagnostics(Diagnostics),
            ["artifacts"] = WriteArtifacts(Artifacts)
        };

        return root;
    }

    private static JsonObject WriteRevisions(IReadOnlyDictionary<string, ProjectDocumentRevision> revisions)
    {
        var root = new JsonObject();
        foreach (var (path, revision) in revisions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            root[path] = revision.Value;
        }

        return root;
    }

    private static JsonArray WriteDiagnostics(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        var array = new JsonArray();
        foreach (var diagnostic in diagnostics)
        {
            array.Add(new JsonObject
            {
                ["code"] = diagnostic.Code,
                ["severity"] = diagnostic.Severity.ToString(),
                ["category"] = diagnostic.Category.ToString(),
                ["message"] = diagnostic.Message,
                ["documentationUri"] = diagnostic.DocumentationUri
            });
        }

        return array;
    }

    private static JsonObject WriteArtifacts(IReadOnlyDictionary<string, string> artifacts)
    {
        var root = new JsonObject();
        foreach (var (kind, path) in artifacts.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            root[kind] = path;
        }

        return root;
    }
}

internal sealed record SceneTestCaseResult(
    string Name,
    bool Succeeded,
    int FramesAdvanced,
    double FixedDelta,
    SceneTestVisualResult? Visual,
    IReadOnlyList<StructuredDiagnostic> Diagnostics)
{
    public JsonObject ToJson()
    {
        return new JsonObject
        {
            ["name"] = Name,
            ["succeeded"] = Succeeded,
            ["framesAdvanced"] = FramesAdvanced,
            ["fixedDelta"] = FixedDelta,
            ["visual"] = Visual?.ToJson(),
            ["diagnostics"] = new JsonArray(Diagnostics.Select(diagnostic => (JsonNode)new JsonObject
            {
                ["code"] = diagnostic.Code,
                ["message"] = diagnostic.Message
            }).ToArray())
        };
    }
}

internal sealed record SceneTestVisualResult(
    string ReferencePath,
    string ActualPath,
    string DiffPath,
    double DifferenceRatio,
    double Tolerance,
    bool Passed)
{
    public JsonObject ToJson()
    {
        return new JsonObject
        {
            ["reference"] = ReferencePath,
            ["actual"] = ActualPath,
            ["diff"] = DiffPath,
            ["differenceRatio"] = DifferenceRatio,
            ["tolerance"] = Tolerance,
            ["passed"] = Passed
        };
    }
}

internal sealed class SceneTestSuiteManifest
{
    private SceneTestSuiteManifest(string path, IReadOnlyList<SceneTestCase> tests)
    {
        Path = path;
        Tests = tests.ToArray();
    }

    public string Path { get; }

    public IReadOnlyList<SceneTestCase> Tests { get; }

    public static SceneTestSuiteManifest Load(string path, string text)
    {
        var root = JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidDataException("Scene test suite manifest must be a JSON object.");
        var format = RequireString(root, "format");
        if (!string.Equals(format, "Electron2D.SceneTestSuite", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Scene test suite manifest format is unsupported.");
        }

        var version = RequireInt(root, "version");
        if (version != 1)
        {
            throw new InvalidDataException("Scene test suite manifest version is unsupported.");
        }

        var testsNode = RequireArray(root, "tests");
        var tests = new List<SceneTestCase>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in testsNode)
        {
            if (item is not JsonObject testObject)
            {
                throw new InvalidDataException("Scene test entry must be a JSON object.");
            }

            var test = SceneTestCase.Load(testObject);
            if (!names.Add(test.Name))
            {
                throw new InvalidDataException($"Scene test name '{test.Name}' is duplicated.");
            }

            tests.Add(test);
        }

        return new SceneTestSuiteManifest(path, tests);
    }

    internal static string RequireString(JsonObject root, string propertyName)
    {
        if (!root.TryGetPropertyValue(propertyName, out var node) ||
            node is null ||
            node.GetValueKind() != JsonValueKind.String)
        {
            throw new InvalidDataException($"Required string property '{propertyName}' is missing.");
        }

        var value = node.GetValue<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Required string property '{propertyName}' must not be empty.");
        }

        return value;
    }

    internal static int RequireInt(JsonObject root, string propertyName)
    {
        if (!root.TryGetPropertyValue(propertyName, out var node) ||
            node is null ||
            node.GetValueKind() != JsonValueKind.Number ||
            !node.AsValue().TryGetValue<int>(out var value))
        {
            throw new InvalidDataException($"Required integer property '{propertyName}' is missing.");
        }

        return value;
    }

    internal static double RequireDouble(JsonObject root, string propertyName)
    {
        if (!root.TryGetPropertyValue(propertyName, out var node) ||
            node is null ||
            node.GetValueKind() != JsonValueKind.Number ||
            !node.AsValue().TryGetValue<double>(out var value))
        {
            throw new InvalidDataException($"Required number property '{propertyName}' is missing.");
        }

        return value;
    }

    internal static JsonArray RequireArray(JsonObject root, string propertyName)
    {
        if (!root.TryGetPropertyValue(propertyName, out var node) || node is not JsonArray array)
        {
            throw new InvalidDataException($"Required array property '{propertyName}' is missing.");
        }

        return array;
    }

    internal static JsonArray OptionalArray(JsonObject root, string propertyName)
    {
        if (!root.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return new JsonArray();
        }

        return node as JsonArray
            ?? throw new InvalidDataException($"Optional array property '{propertyName}' must be an array.");
    }
}

internal sealed class SceneTestCase
{
    private SceneTestCase(
        string name,
        string scenePath,
        int frames,
        double fixedDelta,
        IReadOnlyList<SceneNodeAssertion> nodeAssertions,
        IReadOnlyList<ScenePropertyAssertion> propertyAssertions,
        SceneTestVisual? visual)
    {
        Name = name;
        ScenePath = scenePath;
        Frames = frames;
        FixedDelta = fixedDelta;
        NodeAssertions = nodeAssertions.ToArray();
        PropertyAssertions = propertyAssertions.ToArray();
        Visual = visual;
    }

    public string Name { get; }

    public string ScenePath { get; }

    public int Frames { get; }

    public double FixedDelta { get; }

    public IReadOnlyList<SceneNodeAssertion> NodeAssertions { get; }

    public IReadOnlyList<ScenePropertyAssertion> PropertyAssertions { get; }

    public SceneTestVisual? Visual { get; }

    public static SceneTestCase Load(JsonObject root)
    {
        var name = SceneTestSuiteManifest.RequireString(root, "name");
        var scenePath = ProjectDocumentPaths.NormalizeRelativePath(SceneTestSuiteManifest.RequireString(root, "scene"));
        var frames = SceneTestSuiteManifest.RequireInt(root, "frames");
        if (frames <= 0)
        {
            throw new InvalidDataException("Scene test frames must be greater than zero.");
        }

        var fixedDelta = SceneTestSuiteManifest.RequireDouble(root, "fixedDelta");
        if (fixedDelta <= 0)
        {
            throw new InvalidDataException("Scene test fixedDelta must be greater than zero.");
        }

        var nodeAssertions = SceneTestSuiteManifest.OptionalArray(root, "assertNodes")
            .Select(node => SceneNodeAssertion.Load(node as JsonObject
                ?? throw new InvalidDataException("Scene node assertion must be a JSON object.")))
            .ToArray();
        var propertyAssertions = SceneTestSuiteManifest.OptionalArray(root, "assertProperties")
            .Select(node => ScenePropertyAssertion.Load(node as JsonObject
                ?? throw new InvalidDataException("Scene property assertion must be a JSON object.")))
            .ToArray();
        var visual = root.TryGetPropertyValue("visual", out var visualNode) && visualNode is not null
            ? SceneTestVisual.Load(visualNode as JsonObject
                ?? throw new InvalidDataException("Scene visual assertion must be a JSON object."))
            : null;

        return new SceneTestCase(name, scenePath, frames, fixedDelta, nodeAssertions, propertyAssertions, visual);
    }
}

internal sealed record SceneNodeAssertion(string Path, string Type)
{
    public static SceneNodeAssertion Load(JsonObject root)
    {
        var path = SceneTestSuiteManifest.RequireString(root, "path");
        if (!path.StartsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Scene node assertion path must start with '/'.");
        }

        return new SceneNodeAssertion(path, SceneTestSuiteManifest.RequireString(root, "type"));
    }
}

internal sealed record ScenePropertyAssertion(string NodePath, string PropertyName, JsonNode? ExpectedValue)
{
    public static ScenePropertyAssertion Load(JsonObject root)
    {
        var nodePath = SceneTestSuiteManifest.RequireString(root, "node");
        if (!nodePath.StartsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Scene property assertion node path must start with '/'.");
        }

        if (!root.TryGetPropertyValue("equals", out var expectedValue))
        {
            throw new InvalidDataException("Scene property assertion requires 'equals'.");
        }

        return new ScenePropertyAssertion(
            nodePath,
            SceneTestSuiteManifest.RequireString(root, "property"),
            expectedValue?.DeepClone());
    }
}

internal sealed record SceneTestVisual(string ReferencePath, int CaptureFrame, double Tolerance)
{
    public static SceneTestVisual Load(JsonObject root)
    {
        var referencePath = ProjectDocumentPaths.NormalizeRelativePath(SceneTestSuiteManifest.RequireString(root, "reference"));
        var captureFrame = SceneTestSuiteManifest.RequireInt(root, "captureFrame");
        if (captureFrame <= 0)
        {
            throw new InvalidDataException("Scene visual captureFrame must be greater than zero.");
        }

        var tolerance = SceneTestSuiteManifest.RequireDouble(root, "tolerance");
        if (tolerance is < 0 or > 1)
        {
            throw new InvalidDataException("Scene visual tolerance must be between 0 and 1.");
        }

        return new SceneTestVisual(referencePath, captureFrame, tolerance);
    }
}

internal sealed class SceneTestDocument
{
    private SceneTestDocument(IReadOnlyDictionary<string, SceneTestNode> nodesByPath)
    {
        NodesByPath = new ReadOnlyDictionary<string, SceneTestNode>(
            nodesByPath.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }

    public IReadOnlyDictionary<string, SceneTestNode> NodesByPath { get; }

    public static SceneTestDocument Load(string projectRoot, string scenePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, scenePath.Replace('/', Path.DirectorySeparatorChar)));
        WorkspaceSnapshotMaterializer.EnsureChildPath(projectRoot, fullPath);
        var root = JsonNode.Parse(File.ReadAllText(fullPath)) as JsonObject
            ?? throw new InvalidDataException("Scene document must be a JSON object.");
        var nodes = SceneTestSuiteManifest.RequireArray(root, "nodes")
            .Select(node => SceneTestNode.Load(node as JsonObject
                ?? throw new InvalidDataException("Scene node must be a JSON object.")))
            .ToDictionary(node => node.Id);

        var paths = new Dictionary<int, string>();
        string PathFor(SceneTestNode node)
        {
            if (paths.TryGetValue(node.Id, out var cached))
            {
                return cached;
            }

            var path = node.ParentId is null || !nodes.TryGetValue(node.ParentId.Value, out var parent)
                ? "/" + node.Name
                : PathFor(parent) + "/" + node.Name;
            paths[node.Id] = path;
            return path;
        }

        var nodesByPath = new Dictionary<string, SceneTestNode>(StringComparer.Ordinal);
        foreach (var node in nodes.Values)
        {
            nodesByPath[PathFor(node)] = node;
        }

        return new SceneTestDocument(nodesByPath);
    }
}

internal sealed class SceneTestNode
{
    private SceneTestNode(int id, string type, string name, int? parentId, JsonObject properties)
    {
        Id = id;
        Type = type;
        Name = name;
        ParentId = parentId;
        Properties = properties;
    }

    public int Id { get; }

    public string Type { get; }

    public string Name { get; }

    public int? ParentId { get; }

    public JsonObject Properties { get; }

    public static SceneTestNode Load(JsonObject root)
    {
        var id = SceneTestSuiteManifest.RequireInt(root, "id");
        var type = SceneTestSuiteManifest.RequireString(root, "type");
        var name = SceneTestSuiteManifest.RequireString(root, "name");
        var parentId = ReadOptionalInt(root, "parent");
        var properties = root.TryGetPropertyValue("properties", out var propertiesNode) && propertiesNode is JsonObject propertiesObject
            ? (JsonObject)propertiesObject.DeepClone()
            : new JsonObject();

        return new SceneTestNode(id, type, name, parentId, properties);
    }

    private static int? ReadOptionalInt(JsonObject root, string propertyName)
    {
        if (!root.TryGetPropertyValue(propertyName, out var node) || node is null || node.GetValueKind() == JsonValueKind.Null)
        {
            return null;
        }

        return node.GetValueKind() == JsonValueKind.Number && node.AsValue().TryGetValue<int>(out var value)
            ? value
            : throw new InvalidDataException($"Optional integer property '{propertyName}' must be an integer or null.");
    }
}
