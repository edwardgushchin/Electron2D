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
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D.ProjectSystem;

namespace Electron2D.Tooling;

internal enum EditorCapabilityKind
{
    Unknown,
    ProjectMutation,
    EditorSessionAction,
    RuntimeAction,
    BackgroundJob,
    ReadOnlyQuery
}

internal enum EditorCapabilitySupportStatus
{
    Unknown,
    Supported,
    Partial,
    Experimental,
    NotApplicable
}

internal enum EditorCapabilityCliBindingKind
{
    Unknown,
    DedicatedCommand,
    GenericTransaction,
    NotApplicable
}

internal sealed record EditorCapabilityManifest(
    int SchemaVersion,
    string ManifestVersion,
    EditorCapabilityApiManifestReference ApiManifest,
    IReadOnlyList<EditorCapability> Capabilities);

internal sealed record EditorCapabilityApiManifestReference(string Path, IReadOnlyList<string> References);

internal sealed record EditorCapability(
    string Id,
    string Title,
    IReadOnlyList<string> Categories,
    EditorCapabilityKind Kind,
    bool ReleaseRequired,
    EditorCapabilityEndpoint Editor,
    EditorCapabilityEndpoint Tooling,
    EditorCapabilityEndpoint Mcp,
    EditorCapabilityCliBinding Cli);

internal sealed record EditorCapabilityEndpoint(
    string Command,
    EditorCapabilitySupportStatus Status,
    string Explanation);

internal sealed record EditorCapabilityCliBinding(
    EditorCapabilityCliBindingKind Kind,
    string? Command,
    string Explanation);

internal sealed record EditorCapabilityManifestVerificationInput(
    IReadOnlyList<string> McpToolOrResourceNames,
    IReadOnlyList<string> ToolingCommandNames,
    string RepositoryRoot);

internal sealed class EditorCapabilityManifestVerificationResult
{
    public EditorCapabilityManifestVerificationResult(IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded => Diagnostics.Count == 0;

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }
}

internal static class EditorCapabilityManifestFactory
{
    private const string GenericTextMutationExplanation =
        "Current Preview route uses the shared workspace transaction path for stable text project documents.";

    public static EditorCapabilityManifest CreateDefault()
    {
        return new EditorCapabilityManifest(
            1,
            "0.1.0-preview",
            new EditorCapabilityApiManifestReference(
                "data/api/electron2d-api-manifest.json",
                [
                    "electron2d://api/type/Electron2D.Node",
                    "electron2d://api/type/Electron2D.Node2D",
                    "electron2d://api/type/Electron2D.Resource",
                    "electron2d://api/type/Electron2D.Control",
                    "electron2d://api/type/Electron2D.AnimationPlayer",
                    "electron2d://api/type/Electron2D.TileMapLayer",
                    "electron2d://api/type/Electron2D.InputMap",
                    "electron2d://api/type/Electron2D.SceneTree"
                ]),
            [
                SupportedMutation(
                    "scene.node.set_property",
                    "Set saved scene node property",
                    ["scene", "node", "inspector"],
                    "SceneSetProperty"),
                SupportedMutation(
                    "scene.node.add",
                    "Add node to scene source",
                    ["scene", "node"],
                    "SceneAddNode"),
                SupportedMutation(
                    "scene.node.remove",
                    "Remove node from scene source",
                    ["scene", "node"],
                    "SceneRemoveNode"),
                SupportedMutation(
                    "scene.node.move",
                    "Move or reparent node in scene source",
                    ["scene", "node"],
                    "SceneMoveNode"),
                SupportedMutation(
                    "scene.signal.connect",
                    "Connect scene signal",
                    ["scene", "signals"],
                    "SceneConnectSignal"),
                SupportedMutation(
                    "scene.group.update",
                    "Update node groups",
                    ["scene", "groups"],
                    "SceneUpdateGroups"),
                SupportedMutation(
                    "project.input_map.update",
                    "Update Input Map source",
                    ["input-map", "project-settings"],
                    "ProjectInputMapUpdate"),
                SupportedMutation(
                    "project.settings.update",
                    "Update Project Settings source",
                    ["project-settings"],
                    "ProjectSettingsUpdate"),
                SupportedMutation(
                    "project.main_scene.set",
                    "Set main scene",
                    ["main-scene", "project-settings"],
                    "ProjectMainSceneSet"),
                SupportedMutation(
                    "export.presets.update",
                    "Update export presets source",
                    ["export-presets", "project-settings"],
                    "ExportPresetsUpdate"),
                SupportedMutation(
                    "resource.import_settings.update",
                    "Update resource import settings source",
                    ["resources", "import-settings"],
                    "ResourceImportSettingsUpdate"),
                SupportedJob(
                    "resource.import",
                    "Import resources",
                    ["resources", "import-settings"],
                    "ResourceImport",
                    "resource.import",
                    "resource_import",
                    "import"),
                SupportedJob(
                    "tests.run",
                    "Run project tests",
                    ["tests", "diagnostics"],
                    "ProjectTestRun",
                    "project.test",
                    "project_test",
                    "test"),
                SupportedJob(
                    "project.export",
                    "Run project export job",
                    ["export-presets"],
                    "ProjectExportRun",
                    "project.export",
                    "project_export",
                    "export"),
                SupportedQuery(
                    "diagnostics.read",
                    "Read workspace diagnostics",
                    ["diagnostics"],
                    "DiagnosticsRead",
                    "diagnostics.read",
                    "workspace_get_state"),
                SupportedRuntime(
                    "runtime.control.start",
                    "Start runtime session",
                    ["runtime-control"],
                    "RuntimeStart",
                    "project.run",
                    "project_run",
                    "run"),
                PartialMutation(
                    "spriteframes.edit",
                    "Edit SpriteFrames resource",
                    ["spriteframes", "resources"],
                    "SpriteFramesEdit"),
                PartialMutation(
                    "animationplayer.edit",
                    "Edit AnimationPlayer tracks",
                    ["animationplayer", "scene"],
                    "AnimationPlayerEdit"),
                PartialMutation(
                    "tilemap.edit",
                    "Edit TileMap cells and palette",
                    ["tilemap", "scene", "resources"],
                    "TileMapEdit"),
                PartialMutation(
                    "ui.theme.edit",
                    "Edit UI theme resources",
                    ["ui-themes", "resources"],
                    "UiThemeEdit"),
                SupportedFineGrainedRuntime(
                    "runtime.control.pause_step_input",
                    "Pause, step and inject input into visible runtime",
                    ["runtime-control"],
                    "RuntimePauseStepInput")
            ]);
    }

    private static EditorCapability SupportedMutation(
        string id,
        string title,
        IReadOnlyList<string> categories,
        string editorCommand)
    {
        return new EditorCapability(
            id,
            title,
            categories,
            EditorCapabilityKind.ProjectMutation,
            ReleaseRequired: true,
            new EditorCapabilityEndpoint(editorCommand, EditorCapabilitySupportStatus.Supported, "Editor operation maps to the shared project document mutation route."),
            new EditorCapabilityEndpoint("workspace.apply-transaction", EditorCapabilitySupportStatus.Supported, GenericTextMutationExplanation),
            new EditorCapabilityEndpoint("workspace_apply_transaction", EditorCapabilitySupportStatus.Supported, "MCP exposes the same project mutation through a stable workspace tool."),
            new EditorCapabilityCliBinding(EditorCapabilityCliBindingKind.GenericTransaction, Command: null, "CLI uses `e2d workspace transaction` instead of a dedicated command for this mutation."));
    }

    private static EditorCapability PartialMutation(
        string id,
        string title,
        IReadOnlyList<string> categories,
        string editorCommand)
    {
        return new EditorCapability(
            id,
            title,
            categories,
            EditorCapabilityKind.ProjectMutation,
            ReleaseRequired: false,
            new EditorCapabilityEndpoint(editorCommand, EditorCapabilitySupportStatus.Partial, "Editor workflow is represented in the manifest but still needs its specialized production UI task."),
            new EditorCapabilityEndpoint("workspace.apply-transaction", EditorCapabilitySupportStatus.Partial, "Generic text mutation exists; specialized semantic command is not yet complete."),
            new EditorCapabilityEndpoint("workspace_apply_transaction", EditorCapabilitySupportStatus.Partial, "Generic MCP transaction exists; specialized tool semantics are not yet complete."),
            new EditorCapabilityCliBinding(EditorCapabilityCliBindingKind.GenericTransaction, Command: null, "CLI can use a generic transaction while the specialized workflow is completed."));
    }

    private static EditorCapability SupportedJob(
        string id,
        string title,
        IReadOnlyList<string> categories,
        string editorCommand,
        string toolingCommand,
        string mcpTool,
        string cliCommand)
    {
        return new EditorCapability(
            id,
            title,
            categories,
            EditorCapabilityKind.BackgroundJob,
            ReleaseRequired: true,
            new EditorCapabilityEndpoint(editorCommand, EditorCapabilitySupportStatus.Supported, "Editor queues the operation through the shared job contract."),
            new EditorCapabilityEndpoint(toolingCommand, EditorCapabilitySupportStatus.Supported, "Tooling queues the operation with snapshot identity."),
            new EditorCapabilityEndpoint(mcpTool, EditorCapabilitySupportStatus.Supported, "MCP exposes the job tool with queued job events."),
            new EditorCapabilityCliBinding(EditorCapabilityCliBindingKind.DedicatedCommand, cliCommand, $"CLI has a dedicated `e2d {cliCommand}` route for this job."));
    }

    private static EditorCapability SupportedRuntime(
        string id,
        string title,
        IReadOnlyList<string> categories,
        string editorCommand,
        string toolingCommand,
        string mcpTool,
        string cliCommand)
    {
        return new EditorCapability(
            id,
            title,
            categories,
            EditorCapabilityKind.RuntimeAction,
            ReleaseRequired: true,
            new EditorCapabilityEndpoint(editorCommand, EditorCapabilitySupportStatus.Supported, "Editor run workflow is routed through the shared runtime/job contract."),
            new EditorCapabilityEndpoint(toolingCommand, EditorCapabilitySupportStatus.Supported, "Tooling exposes runtime job queueing with snapshot identity."),
            new EditorCapabilityEndpoint(mcpTool, EditorCapabilitySupportStatus.Supported, "MCP exposes runtime start through the project run tool."),
            new EditorCapabilityCliBinding(EditorCapabilityCliBindingKind.DedicatedCommand, cliCommand, $"CLI has a dedicated `e2d {cliCommand}` route."));
    }

    private static EditorCapability SupportedFineGrainedRuntime(
        string id,
        string title,
        IReadOnlyList<string> categories,
        string editorCommand)
    {
        return new EditorCapability(
            id,
            title,
            categories,
            EditorCapabilityKind.RuntimeAction,
            ReleaseRequired: true,
            new EditorCapabilityEndpoint(editorCommand, EditorCapabilitySupportStatus.Supported, "Editor-attached runtime controls use the shared workspace runtime session."),
            new EditorCapabilityEndpoint("runtime.pause-step-input", EditorCapabilitySupportStatus.Supported, "Tooling controls the active editor-attached runtime session through the runtime service."),
            new EditorCapabilityEndpoint("runtime_pause", EditorCapabilitySupportStatus.Supported, "MCP exposes fine-grained runtime controls through the active editor runtime session."),
            new EditorCapabilityCliBinding(EditorCapabilityCliBindingKind.NotApplicable, Command: null, "Fine-grained visible Editor controls are not a dedicated headless CLI command."));
    }

    private static EditorCapability SupportedQuery(
        string id,
        string title,
        IReadOnlyList<string> categories,
        string editorCommand,
        string toolingCommand,
        string mcpTool)
    {
        return new EditorCapability(
            id,
            title,
            categories,
            EditorCapabilityKind.ReadOnlyQuery,
            ReleaseRequired: true,
            new EditorCapabilityEndpoint(editorCommand, EditorCapabilitySupportStatus.Supported, "Editor diagnostics are readable from the shared workspace state."),
            new EditorCapabilityEndpoint(toolingCommand, EditorCapabilitySupportStatus.Supported, "Tooling exposes diagnostics through the workspace state query contract."),
            new EditorCapabilityEndpoint(mcpTool, EditorCapabilitySupportStatus.Supported, "MCP exposes diagnostics through the workspace state tool."),
            new EditorCapabilityCliBinding(EditorCapabilityCliBindingKind.NotApplicable, Command: null, "Read-only diagnostics are available through MCP/resources and do not require a dedicated CLI command."));
    }
}

internal static class EditorCapabilityManifestSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public static string Serialize(EditorCapabilityManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return ToJson(manifest).ToJsonString(IndentedOptions).ReplaceLineEndings("\n") + "\n";
    }

    public static JsonObject ToJson(EditorCapabilityManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return new JsonObject
        {
            ["schemaVersion"] = manifest.SchemaVersion,
            ["manifestVersion"] = manifest.ManifestVersion,
            ["apiManifest"] = new JsonObject
            {
                ["path"] = manifest.ApiManifest.Path,
                ["references"] = WriteStringArray(manifest.ApiManifest.References)
            },
            ["capabilities"] = WriteCapabilities(manifest.Capabilities)
        };
    }

    private static JsonArray WriteCapabilities(IEnumerable<EditorCapability> capabilities)
    {
        var array = new JsonArray();
        foreach (var capability in capabilities.OrderBy(capability => capability.Id, StringComparer.Ordinal))
        {
            array.Add(new JsonObject
            {
                ["capability"] = capability.Id,
                ["title"] = capability.Title,
                ["categories"] = WriteStringArray(capability.Categories),
                ["kind"] = KindName(capability.Kind),
                ["releaseRequired"] = capability.ReleaseRequired,
                ["editor"] = WriteEndpoint(capability.Editor, "command"),
                ["tooling"] = WriteEndpoint(capability.Tooling, "command"),
                ["mcp"] = WriteEndpoint(capability.Mcp, "toolOrResource"),
                ["cli"] = new JsonObject
                {
                    ["kind"] = CliKindName(capability.Cli.Kind),
                    ["command"] = capability.Cli.Command,
                    ["explanation"] = capability.Cli.Explanation
                }
            });
        }

        return array;
    }

    private static JsonObject WriteEndpoint(EditorCapabilityEndpoint endpoint, string commandProperty)
    {
        return new JsonObject
        {
            [commandProperty] = endpoint.Command,
            ["status"] = StatusName(endpoint.Status),
            ["explanation"] = endpoint.Explanation
        };
    }

    private static JsonArray WriteStringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values.OrderBy(value => value, StringComparer.Ordinal))
        {
            array.Add(value);
        }

        return array;
    }

    private static string KindName(EditorCapabilityKind kind)
    {
        return kind switch
        {
            EditorCapabilityKind.ProjectMutation => "projectMutation",
            EditorCapabilityKind.EditorSessionAction => "editorSessionAction",
            EditorCapabilityKind.RuntimeAction => "runtimeAction",
            EditorCapabilityKind.BackgroundJob => "backgroundJob",
            EditorCapabilityKind.ReadOnlyQuery => "readOnlyQuery",
            _ => "unknown"
        };
    }

    private static string StatusName(EditorCapabilitySupportStatus status)
    {
        return status switch
        {
            EditorCapabilitySupportStatus.Supported => "supported",
            EditorCapabilitySupportStatus.Partial => "partial",
            EditorCapabilitySupportStatus.Experimental => "experimental",
            EditorCapabilitySupportStatus.NotApplicable => "not_applicable",
            _ => "unknown"
        };
    }

    private static string CliKindName(EditorCapabilityCliBindingKind kind)
    {
        return kind switch
        {
            EditorCapabilityCliBindingKind.DedicatedCommand => "dedicatedCommand",
            EditorCapabilityCliBindingKind.GenericTransaction => "genericTransaction",
            EditorCapabilityCliBindingKind.NotApplicable => "notApplicable",
            _ => "unknown"
        };
    }
}

internal static class EditorCapabilityManifestVerifier
{
    private static readonly string[] RequiredCategories =
    [
        "scene",
        "node",
        "inspector",
        "resources",
        "signals",
        "groups",
        "input-map",
        "project-settings",
        "spriteframes",
        "animationplayer",
        "tilemap",
        "ui-themes",
        "import-settings",
        "main-scene",
        "export-presets",
        "tests",
        "diagnostics",
        "runtime-control"
    ];

    public static EditorCapabilityManifestVerificationResult Verify(
        EditorCapabilityManifest manifest,
        EditorCapabilityManifestVerificationInput input)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StructuredDiagnostic>();
        ValidateShape(manifest, input, diagnostics);
        ValidateCapabilities(manifest, input, diagnostics);
        return new EditorCapabilityManifestVerificationResult(diagnostics);
    }

    private static void ValidateShape(
        EditorCapabilityManifest manifest,
        EditorCapabilityManifestVerificationInput input,
        List<StructuredDiagnostic> diagnostics)
    {
        if (manifest.SchemaVersion != 1)
        {
            diagnostics.Add(Diagnostic("E2D-CAPABILITY-0003", $"Editor capability manifest schemaVersion '{manifest.SchemaVersion}' is not supported."));
        }

        if (!string.Equals(manifest.ManifestVersion, "0.1.0-preview", StringComparison.Ordinal))
        {
            diagnostics.Add(Diagnostic("E2D-CAPABILITY-0003", $"Editor capability manifestVersion '{manifest.ManifestVersion}' is not supported."));
        }

        var apiManifestPath = Path.Combine(input.RepositoryRoot, manifest.ApiManifest.Path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(apiManifestPath))
        {
            diagnostics.Add(Diagnostic("E2D-CAPABILITY-0003", $"API manifest reference was not found: {manifest.ApiManifest.Path}"));
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var capability in manifest.Capabilities)
        {
            if (!ids.Add(capability.Id))
            {
                diagnostics.Add(Diagnostic("E2D-CAPABILITY-0003", $"Duplicate editor capability id: {capability.Id}"));
            }
        }

        var categories = manifest.Capabilities
            .SelectMany(capability => capability.Categories)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var category in RequiredCategories)
        {
            if (!categories.Contains(category))
            {
                diagnostics.Add(Diagnostic("E2D-CAPABILITY-0003", $"Editor capability manifest is missing required category: {category}"));
            }
        }
    }

    private static void ValidateCapabilities(
        EditorCapabilityManifest manifest,
        EditorCapabilityManifestVerificationInput input,
        List<StructuredDiagnostic> diagnostics)
    {
        var toolingCommands = input.ToolingCommandNames.ToHashSet(StringComparer.Ordinal);
        var mcpNames = input.McpToolOrResourceNames.ToHashSet(StringComparer.Ordinal);

        foreach (var capability in manifest.Capabilities)
        {
            ValidateMandatoryFields(capability, diagnostics);
            ValidateParity(capability, diagnostics);
            ValidateCli(capability, diagnostics);
            if (!toolingCommands.Contains(capability.Tooling.Command))
            {
                diagnostics.Add(Diagnostic("E2D-CAPABILITY-0004", $"Capability '{capability.Id}' references unknown Tooling command '{capability.Tooling.Command}'."));
            }

            if (!mcpNames.Contains(capability.Mcp.Command))
            {
                diagnostics.Add(Diagnostic("E2D-CAPABILITY-0004", $"Capability '{capability.Id}' references unknown MCP tool/resource '{capability.Mcp.Command}'."));
            }
        }
    }

    private static void ValidateMandatoryFields(EditorCapability capability, List<StructuredDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(capability.Id) ||
            string.IsNullOrWhiteSpace(capability.Title) ||
            capability.Categories.Count == 0 ||
            capability.Kind == EditorCapabilityKind.Unknown ||
            capability.Editor.Status == EditorCapabilitySupportStatus.Unknown ||
            capability.Tooling.Status == EditorCapabilitySupportStatus.Unknown ||
            capability.Mcp.Status == EditorCapabilitySupportStatus.Unknown ||
            capability.Cli.Kind == EditorCapabilityCliBindingKind.Unknown ||
            string.IsNullOrWhiteSpace(capability.Editor.Command) ||
            string.IsNullOrWhiteSpace(capability.Tooling.Command) ||
            string.IsNullOrWhiteSpace(capability.Mcp.Command) ||
            string.IsNullOrWhiteSpace(capability.Editor.Explanation) ||
            string.IsNullOrWhiteSpace(capability.Tooling.Explanation) ||
            string.IsNullOrWhiteSpace(capability.Mcp.Explanation) ||
            string.IsNullOrWhiteSpace(capability.Cli.Explanation))
        {
            diagnostics.Add(Diagnostic("E2D-CAPABILITY-0003", $"Capability '{capability.Id}' has incomplete required fields."));
        }
    }

    private static void ValidateParity(EditorCapability capability, List<StructuredDiagnostic> diagnostics)
    {
        if (capability.Editor.Status == EditorCapabilitySupportStatus.Supported &&
            (capability.Tooling.Status != EditorCapabilitySupportStatus.Supported ||
                capability.Mcp.Status != EditorCapabilitySupportStatus.Supported))
        {
            diagnostics.Add(Diagnostic("E2D-CAPABILITY-0001", $"Capability '{capability.Id}' is supported in Editor but not fully supported by Tooling and MCP."));
        }

        if (capability.ReleaseRequired &&
            (capability.Tooling.Status is EditorCapabilitySupportStatus.Partial or EditorCapabilitySupportStatus.Experimental ||
                capability.Mcp.Status is EditorCapabilitySupportStatus.Partial or EditorCapabilitySupportStatus.Experimental))
        {
            diagnostics.Add(Diagnostic("E2D-CAPABILITY-0001", $"Release-required capability '{capability.Id}' cannot use partial or experimental Tooling/MCP support."));
        }
    }

    private static void ValidateCli(EditorCapability capability, List<StructuredDiagnostic> diagnostics)
    {
        if (capability.Cli.Kind == EditorCapabilityCliBindingKind.DedicatedCommand &&
            string.IsNullOrWhiteSpace(capability.Cli.Command))
        {
            diagnostics.Add(Diagnostic("E2D-CAPABILITY-0002", $"Capability '{capability.Id}' has dedicated CLI binding without command."));
        }

        if (capability.ReleaseRequired &&
            capability.Kind is EditorCapabilityKind.ProjectMutation or EditorCapabilityKind.BackgroundJob &&
            capability.Cli.Kind == EditorCapabilityCliBindingKind.NotApplicable)
        {
            diagnostics.Add(Diagnostic("E2D-CAPABILITY-0002", $"Release-required capability '{capability.Id}' requires a dedicated CLI command or generic transaction binding."));
        }
    }

    private static StructuredDiagnostic Diagnostic(string code, string message)
    {
        var definition = DiagnosticCodeRegistry.Get(code);
        return StructuredDiagnostic.Create(
            definition.Code,
            definition.Severity,
            definition.Category,
            message,
            location: null,
            relatedLocations: [],
            suggestedFixes: []);
    }
}

internal static class ProjectToolingCommandCatalog
{
    private static readonly string[] Names =
    [
        "workspace.apply-transaction",
        "workspace.save-affected-documents",
        "workspace.resolve-conflict",
        "workspace.undo-transaction",
        "project.validate",
        "project.build",
        "project.run",
        "project.test",
        "project.export",
        "resource.import",
        "resource.inspect",
        "resource.find-references",
        "diagnostics.read",
        "runtime.start",
        "runtime.stop",
        "runtime.pause-step-input",
        "runtime.resume",
        "runtime.capture-frame",
        "runtime.get-scene-tree",
        "runtime.get-diagnostics",
        "runtime.highlight-node",
        "task.list",
        "task.get",
        "task.create",
        "task.update",
        "task.claim",
        "task.set-status",
        "task.append-activity",
        "task.submit-for-acceptance"
    ];

    public static IReadOnlyList<string> SupportedCommandNames { get; } =
        new ReadOnlyCollection<string>(Names);
}
