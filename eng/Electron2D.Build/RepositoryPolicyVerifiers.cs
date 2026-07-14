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

using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Electron2D.Build;

internal sealed class LicensePolicyVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    private const string ExpectedLicense = """
    MIT License

    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>

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
    """;

    private const string ExpectedCSharpHeader = """
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

    """;

    public async Task<int> VerifyAsync(CancellationToken cancellationToken)
    {
        var errors = new List<BuildDiagnostic>();
        var licensePath = Path.Combine(repositoryRoot, "LICENSE");
        if (!File.Exists(licensePath))
        {
            errors.Add(Error("E2D-BUILD-LICENSES-LICENSE-MISSING", "LICENSE file was not found.", "LICENSE"));
        }
        else if (!string.Equals(Normalize(File.ReadAllText(licensePath, Encoding.UTF8)).Trim(), Normalize(ExpectedLicense).Trim(), StringComparison.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-LICENSES-LICENSE-TEXT", "LICENSE does not match the required MIT License text.", "LICENSE"));
        }

        var files = await GetSourceFilesAsync(cancellationToken).ConfigureAwait(false);
        var checkedCount = 0;
        foreach (var relativePath in files)
        {
            if (IsIgnoredSourcePath(relativePath))
            {
                continue;
            }

            var fullPath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                continue;
            }

            if (!relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            checkedCount++;
            var content = Normalize(File.ReadAllText(fullPath, Encoding.UTF8));
            if (!content.StartsWith(Normalize(ExpectedCSharpHeader), StringComparison.Ordinal))
            {
                errors.Add(Error("E2D-BUILD-LICENSES-SOURCE-HEADER", $"Source file is missing the required MIT license header: {relativePath}.", relativePath));
            }
        }

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            "verify licenses",
            "info",
            "E2D-BUILD-LICENSES-VERIFY-PASSED",
            $"Source license header verification passed for {checkedCount} source files."));
        return RepositoryBuildExitCodes.Success;
    }

    private async Task<IReadOnlyList<string>> GetSourceFilesAsync(CancellationToken cancellationToken)
    {
        if (Directory.Exists(Path.Combine(repositoryRoot, ".git")))
        {
            var result = await processRunner.RunAsync(
                new ProcessRunRequest(
                    "verify licenses git ls-files",
                    "git",
                    ["ls-files", "*.cs"],
                    repositoryRoot,
                    TimeSpan.FromSeconds(30)),
                cancellationToken).ConfigureAwait(false);

            if (result.ExitCode == 0)
            {
                return result.StandardOutput
                    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(path => path.Replace('\\', '/'))
                    .ToArray();
            }
        }

        return Directory.EnumerateFiles(repositoryRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();
    }

    private static bool IsIgnoredSourcePath(string relativePath)
    {
        if (!relativePath.StartsWith("src/", StringComparison.Ordinal) &&
            !relativePath.StartsWith("eng/", StringComparison.Ordinal) &&
            !relativePath.StartsWith("tests/", StringComparison.Ordinal) &&
            !relativePath.StartsWith("data/templates/", StringComparison.Ordinal))
        {
            return true;
        }

        return relativePath.Split('/').Any(part => part is "bin" or "obj" or "artifacts" or "publish" or "packages" or "TestResults" or "coverage");
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private static BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("verify", "verify licenses", "error", code, message, Path: path);
    }
}

internal sealed class ReleaseMetadataVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    private static readonly Dictionary<string, string> ExpectedProperties = new(StringComparer.Ordinal)
    {
        ["Version"] = "0.1-preview",
        ["PackageVersion"] = "0.1-preview",
        ["AssemblyVersion"] = "0.1.0.0",
        ["FileVersion"] = "0.1.0.0",
        ["InformationalVersion"] = "0.1-preview",
        ["PackageId"] = "Electron2D",
        ["Authors"] = "Electron2D Team",
        ["PackageLicenseExpression"] = "MIT",
        ["PackageReadmeFile"] = "README.md",
        ["PackageIcon"] = "electron2d_windows_icon_128.png",
        ["RepositoryType"] = "git"
    };

    public async Task<int> VerifyAsync(CancellationToken cancellationToken)
    {
        var errors = new List<BuildDiagnostic>();
        var projectPath = Path.Combine(repositoryRoot, "src", "Electron2D", "Electron2D.csproj");
        var editorProjectPath = Path.Combine(repositoryRoot, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var readmePath = Path.Combine(repositoryRoot, "README.md");
        var packageIconPath = Path.Combine(repositoryRoot, "data", "assets", "branding", "icon", "electron2d_windows_icon_128.png");
        var editorIconPath = Path.Combine(repositoryRoot, "data", "assets", "branding", "icon", "electron2d.ico");

        var project = LoadXml(projectPath, "E2D-BUILD-RELEASE-METADATA-RUNTIME-PROJECT", errors);
        if (project is not null)
        {
            foreach (var pair in ExpectedProperties)
            {
                var actual = ProjectProperty(project, pair.Key);
                if (!string.Equals(actual, pair.Value, StringComparison.Ordinal))
                {
                    errors.Add(Error("E2D-BUILD-RELEASE-METADATA-PROPERTY", $"Project metadata mismatch for {pair.Key}: expected '{pair.Value}', got '{actual}'.", "src/Electron2D/Electron2D.csproj"));
                }
            }

            var iconItem = project.Descendants("None")
                .FirstOrDefault(item => string.Equals((string?)item.Attribute("Include"), @"..\..\data\assets\branding\icon\electron2d_windows_icon_128.png", StringComparison.Ordinal));
            if (iconItem is null ||
                !string.Equals((string?)iconItem.Attribute("Pack"), "true", StringComparison.Ordinal) ||
                !string.Equals((string?)iconItem.Attribute("PackagePath"), "\\", StringComparison.Ordinal))
            {
                errors.Add(Error("E2D-BUILD-RELEASE-METADATA-PACKAGE-ICON", "Runtime package icon must be packed at the package root.", "src/Electron2D/Electron2D.csproj"));
            }
        }

        var editorProject = LoadXml(editorProjectPath, "E2D-BUILD-RELEASE-METADATA-EDITOR-PROJECT", errors);
        if (editorProject is not null &&
            !string.Equals(ProjectProperty(editorProject, "ApplicationIcon"), @"..\..\data\assets\branding\icon\electron2d.ico", StringComparison.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-RELEASE-METADATA-EDITOR-ICON", "Editor application icon metadata is invalid.", "src/Electron2D.Editor/Electron2D.Editor.csproj"));
        }

        foreach (var requiredPath in new[] { readmePath, packageIconPath, editorIconPath })
        {
            if (!File.Exists(requiredPath))
            {
                errors.Add(Error("E2D-BUILD-RELEASE-METADATA-FILE-MISSING", $"Required release metadata file was not found: {Path.GetRelativePath(repositoryRoot, requiredPath)}.", Path.GetRelativePath(repositoryRoot, requiredPath).Replace('\\', '/')));
            }
        }

        if (File.Exists(readmePath))
        {
            var readme = File.ReadAllText(readmePath, Encoding.UTF8);
            if (!readme.Contains("0.1-preview", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(Error("E2D-BUILD-RELEASE-METADATA-README-VERSION", "README.md does not mention 0.1-preview.", "README.md"));
            }

            foreach (var asset in new[] { "data/assets/branding/readme/electron2d_readme_dark.svg", "data/assets/branding/readme/electron2d_readme_light.svg" })
            {
                if (!readme.Contains(asset, StringComparison.Ordinal))
                {
                    errors.Add(Error("E2D-BUILD-RELEASE-METADATA-README-ASSET", $"README.md does not reference brand asset: {asset}.", "README.md"));
                }
            }
        }

        if (Directory.Exists(Path.Combine(repositoryRoot, ".git")))
        {
            var trackedDrafts = await processRunner.RunAsync(
                new ProcessRunRequest(
                    "verify release-metadata git ls-files",
                    "git",
                    ["ls-files", "CHANGELOG*", "RELEASE-NOTES*"],
                    repositoryRoot,
                    TimeSpan.FromSeconds(30)),
                cancellationToken).ConfigureAwait(false);
            if (trackedDrafts.ExitCode == 0 && !string.IsNullOrWhiteSpace(trackedDrafts.StandardOutput))
            {
                var draftPath = trackedDrafts.StandardOutput
                    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault() ?? "CHANGELOG*";
                errors.Add(Error("E2D-BUILD-RELEASE-METADATA-LOCAL-DRAFT-TRACKED", "Local-only release draft files are tracked by Git.", draftPath.Replace('\\', '/')));
            }
        }

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            "verify release-metadata",
            "info",
            "E2D-BUILD-RELEASE-METADATA-PASSED",
            "Release metadata verification passed."));
        return RepositoryBuildExitCodes.Success;
    }

    private static XDocument? LoadXml(string path, string code, List<BuildDiagnostic> errors)
    {
        if (!File.Exists(path))
        {
            errors.Add(Error(code, $"Project file was not found: {path}.", path.Replace('\\', '/')));
            return null;
        }

        try
        {
            return XDocument.Load(path);
        }
        catch (Exception ex) when (ex is IOException or System.Xml.XmlException)
        {
            errors.Add(Error(code, $"Project file could not be read: {ex.Message}.", path.Replace('\\', '/')));
            return null;
        }
    }

    private static string? ProjectProperty(XDocument project, string name)
    {
        return project.Descendants(name).Select(element => element.Value.Trim()).FirstOrDefault(value => value.Length > 0);
    }

    private static BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("verify", "verify release-metadata", "error", code, message, Path: path);
    }
}

internal sealed class ProjectTemplateVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics)
{
    private const string TemplateRelativeRoot = "data/templates/electron2d-empty";

    private static readonly Regex StaleAgentParityCounterPattern = new(
        @"\b(?:missing\s+types|missing\s+members|signature\s+mismatches|inheritance\s+mismatches|default\s+mismatches|unexpected\s+changes)\s*(?::|=)\s*0\b",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly string[] RequiredFiles =
    [
        ".template.config/template.json",
        "Electron2D.Empty.csproj",
        "global.json",
        "electron2d.lock.json",
        "Program.cs",
        "Scripts/MainScene.cs",
        "project.e2d.json",
        "scenes/main.scene.json",
        "README.md",
        ".gitignore",
        "AGENTS.md",
        ".codex/skills/electron2d-scene/SKILL.md",
        ".codex/skills/electron2d-gameplay-code/SKILL.md",
        ".codex/skills/electron2d-resource-import/SKILL.md",
        ".codex/skills/electron2d-run-test/SKILL.md",
        ".codex/skills/electron2d-export/SKILL.md",
        ".taskboard/.gitignore",
        ".taskboard/board.e2tasks",
        ".taskboard/tasks/welcome.e2task"
    ];

    public int Verify()
    {
        var errors = new List<BuildDiagnostic>();
        var templateRoot = Path.Combine(repositoryRoot, TemplateRelativeRoot.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(templateRoot))
        {
            errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-MISSING", "Template directory data/templates/electron2d-empty was not found.", TemplateRelativeRoot));
        }

        foreach (var requiredFile in RequiredFiles)
        {
            var fullPath = Path.Combine(templateRoot, requiredFile.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-FILE-MISSING", $"Template file was not found: {requiredFile}.", $"{TemplateRelativeRoot}/{requiredFile}"));
            }
        }

        VerifyProjectManifest(templateRoot, errors);
        VerifyTaskFiles(templateRoot, errors);
        VerifyAgentInstructions(templateRoot, errors);
        VerifyGitIgnore(templateRoot, errors);

        var skillRoot = Path.Combine(templateRoot, ".codex", "skills");
        var skillCount = Directory.Exists(skillRoot)
            ? Directory.EnumerateFiles(skillRoot, "SKILL.md", SearchOption.AllDirectories).Count()
            : 0;
        if (skillCount != 5)
        {
            errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-SKILLS", $"Expected 5 starter skills, found {skillCount}.", $"{TemplateRelativeRoot}/.codex/skills"));
        }

        foreach (var forbidden in new[] { "TASKS.md", "completed-tasks", "dev-diary" })
        {
            if (File.Exists(Path.Combine(templateRoot, forbidden)) || Directory.Exists(Path.Combine(templateRoot, forbidden)))
            {
                errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-WORKFLOW-FILE", $"Template must not include repository workflow file or directory: {forbidden}.", $"{TemplateRelativeRoot}/{forbidden}"));
            }
        }

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            "verify project-template",
            "info",
            "E2D-BUILD-PROJECT-TEMPLATE-PASSED",
            "Project template manifest and required file verification passed.",
            Path: TemplateRelativeRoot));
        return RepositoryBuildExitCodes.Success;
    }

    private static void VerifyProjectManifest(string templateRoot, List<BuildDiagnostic> errors)
    {
        var path = Path.Combine(templateRoot, "project.e2d.json");
        var root = LoadJsonObject(path, $"{TemplateRelativeRoot}/project.e2d.json", "E2D-BUILD-PROJECT-TEMPLATE-PROJECT-MANIFEST", errors);
        if (root is null)
        {
            return;
        }

        if (!TryGetString(root.Value, "format", out var format) ||
            !string.Equals(format, "Electron2D.ProjectSettings", StringComparison.Ordinal) ||
            !TryGetInt32(root.Value, "formatVersion", out var formatVersion) ||
            formatVersion != 1 ||
            !TryGetString(root.Value, "mainScene", out _) ||
            !TryGetString(root.Value, "rendererProfile", out _))
        {
            errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-PROJECT-MANIFEST", "project.e2d.json does not match the required project settings manifest shape.", $"{TemplateRelativeRoot}/project.e2d.json"));
        }
    }

    private static void VerifyTaskFiles(string templateRoot, List<BuildDiagnostic> errors)
    {
        var board = LoadJsonObject(
            Path.Combine(templateRoot, ".taskboard", "board.e2tasks"),
            $"{TemplateRelativeRoot}/.taskboard/board.e2tasks",
            "E2D-BUILD-PROJECT-TEMPLATE-TASK-BOARD",
            errors);
        if (board is not null &&
            (!TryGetString(board.Value, "format", out var boardFormat) ||
             !string.Equals(boardFormat, "Electron2D.TaskBoard", StringComparison.Ordinal) ||
             !TryGetInt32(board.Value, "version", out var boardVersion) ||
             boardVersion != 3 ||
             !board.Value.TryGetProperty("placements", out var placements) ||
             placements.ValueKind != JsonValueKind.Array))
        {
            errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-TASK-BOARD", "Task board format is invalid.", $"{TemplateRelativeRoot}/.taskboard/board.e2tasks"));
        }

        var welcome = LoadJsonObject(
            Path.Combine(templateRoot, ".taskboard", "tasks", "welcome.e2task"),
            $"{TemplateRelativeRoot}/.taskboard/tasks/welcome.e2task",
            "E2D-BUILD-PROJECT-TEMPLATE-WELCOME-TASK",
            errors);
        if (welcome is not null &&
            (!TryGetString(welcome.Value, "format", out var taskFormat) ||
             !string.Equals(taskFormat, "Electron2D.TaskFile", StringComparison.Ordinal) ||
             !TryGetInt32(welcome.Value, "version", out var taskVersion) ||
             taskVersion != 3 ||
             !TryGetString(welcome.Value, "taskUid", out _) ||
             !TryGetString(welcome.Value, "status", out var status) ||
             !string.Equals(status, "Ready", StringComparison.Ordinal)))
        {
            errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-WELCOME-TASK", "Starter task document is invalid.", $"{TemplateRelativeRoot}/.taskboard/tasks/welcome.e2task"));
        }
    }

    private static void VerifyAgentInstructions(string templateRoot, List<BuildDiagnostic> errors)
    {
        var path = Path.Combine(templateRoot, "AGENTS.md");
        if (!File.Exists(path))
        {
            return;
        }

        var text = File.ReadAllText(path, Encoding.UTF8);
        foreach (var requiredText in new[]
        {
            "Electron2D 0.1-preview",
            ".NET 10.0.101",
            "e2d validate",
            "e2d api compare-godot <type>",
            "does not prove full Godot 4.7 strict parity",
            "separate parity evidence",
            "ProjectTaskManager",
            "e2d tasks submit"
        })
        {
            if (!text.Contains(requiredText, StringComparison.Ordinal))
            {
                errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-AGENTS", $"AGENTS.md does not contain required text: {requiredText}.", $"{TemplateRelativeRoot}/AGENTS.md"));
            }
        }

        foreach (var forbidden in new[] { "TASKS.md", "completed-tasks", "dev-diary", "strict verifier", "parity verified" })
        {
            if (text.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-AGENTS", $"AGENTS.md contains forbidden or obsolete guidance: {forbidden}.", $"{TemplateRelativeRoot}/AGENTS.md"));
            }
        }

        if (StaleAgentParityCounterPattern.IsMatch(text))
        {
            errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-AGENTS", "AGENTS.md must not present zero strict-parity counters as evidence from e2d api compare-godot.", $"{TemplateRelativeRoot}/AGENTS.md"));
        }
    }

    private static void VerifyGitIgnore(string templateRoot, List<BuildDiagnostic> errors)
    {
        var path = Path.Combine(templateRoot, ".gitignore");
        if (!File.Exists(path))
        {
            return;
        }

        var lines = File.ReadAllLines(path, Encoding.UTF8).Select(line => line.Trim()).ToHashSet(StringComparer.Ordinal);
        foreach (var requiredLine in new[] { ".electron2d/import-cache/", ".electron2d/workspaces/", ".electron2d/context/", ".electron2d/session/", ".electron2d/user/" })
        {
            if (!lines.Contains(requiredLine))
            {
                errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-GITIGNORE", $".gitignore is missing required line: {requiredLine}.", $"{TemplateRelativeRoot}/.gitignore"));
            }
        }

        if (lines.Contains(".electron2d/") || lines.Contains(".taskboard/") || lines.Contains(".taskboard"))
        {
            errors.Add(Error("E2D-BUILD-PROJECT-TEMPLATE-GITIGNORE", ".gitignore must not hide tracked .taskboard documents or attachments.", $"{TemplateRelativeRoot}/.gitignore"));
        }
    }

    private static JsonElement? LoadJsonObject(string path, string relativePath, string code, List<BuildDiagnostic> errors)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add(Error(code, $"JSON root must be a JSON object, got {document.RootElement.ValueKind}.", relativePath));
                return null;
            }

            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            errors.Add(Error(code, $"JSON file is invalid: {ex.Message}.", relativePath));
            return null;
        }
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
        {
            value = property.GetString()!;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetInt32(JsonElement element, string propertyName, out int value)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("verify", "verify project-template", "error", code, message, Path: path);
    }
}

internal sealed record ManualApiProfileType(
    string FullName,
    string GodotReference,
    string Decision,
    string Rationale,
    bool EditorOnly,
    string? GodotApiScope,
    bool HasGodotApiContract,
    ManualElectronApiContract? ElectronApiContract);

internal sealed record ManualElectronApiContract(
    string DefaultMemberDecision,
    string Rationale,
    IReadOnlyDictionary<string, ManualElectronApiMemberDecision> MemberDecisions);

internal sealed record ManualElectronApiMemberDecision(
    string Kind,
    string Name,
    string Decision,
    string Compatibility,
    string? GodotName,
    string Rationale);

internal sealed record ManualApiProfileDocument(IReadOnlyDictionary<string, ManualApiProfileType> Types);

internal static class ManualApiProfileReader
{
    private static readonly HashSet<string> AllowedDecisions = new(StringComparer.Ordinal)
    {
        "approved",
        "deferred",
        "unsupported"
    };
    private static readonly HashSet<string> AllowedGodotApiScopes = new(StringComparer.Ordinal)
    {
        "full",
        "subset"
    };

    public static ManualApiProfileDocument Read(
        string repositoryRoot,
        string profilePath,
        string command,
        string step,
        List<BuildDiagnostic> errors)
    {
        if (!File.Exists(profilePath))
        {
            errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-MISSING", "Manual public API profile was not found.", ToRepositoryPath(repositoryRoot, profilePath)));
            return new ManualApiProfileDocument(new Dictionary<string, ManualApiProfileType>(StringComparer.Ordinal));
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(profilePath, Encoding.UTF8));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SCHEMA", "Manual public API profile root must be a JSON object.", ToRepositoryPath(repositoryRoot, profilePath)));
                return new ManualApiProfileDocument(new Dictionary<string, ManualApiProfileType>(StringComparer.Ordinal));
            }

            if (!ApiManifestCommand.TryGetArray(root, "types", out var types))
            {
                errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SCHEMA", "Manual public API profile must contain a types array.", ToRepositoryPath(repositoryRoot, profilePath)));
                return new ManualApiProfileDocument(new Dictionary<string, ManualApiProfileType>(StringComparer.Ordinal));
            }

            var entries = new Dictionary<string, ManualApiProfileType>(StringComparer.Ordinal);
            foreach (var type in types.EnumerateArray())
            {
                if (type.ValueKind != JsonValueKind.Object)
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SCHEMA", "Manual public API profile type entry must be a JSON object.", ToRepositoryPath(repositoryRoot, profilePath)));
                    continue;
                }

                var hasFullName = ApiManifestCommand.TryGetString(type, "fullName", out var fullName);
                var hasGodotReference = ApiManifestCommand.TryGetString(type, "godotReference", out var godotReference);
                var hasDecision = ApiManifestCommand.TryGetString(type, "decision", out var decision);
                var hasRationale = TryGetStringAllowEmpty(type, "rationale", out var rationale);
                var hasValidEditorOnly = TryGetOptionalBool(type, "editorOnly", out var editorOnly);
                var hasValidGodotApiScope = TryGetOptionalString(type, "godotApiScope", out var godotApiScope);
                if (!hasFullName || !hasGodotReference || !hasDecision || !hasRationale)
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SCHEMA", "Manual public API profile type entry must contain fullName, godotReference, decision and rationale.", ToRepositoryPath(repositoryRoot, profilePath)));
                    continue;
                }

                if (!hasValidEditorOnly)
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SCHEMA", $"Manual public API profile type entry has non-boolean editorOnly: {fullName}.", ToRepositoryPath(repositoryRoot, profilePath)));
                    continue;
                }

                if (!hasValidGodotApiScope)
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SCHEMA", $"Manual public API profile type entry has non-string godotApiScope: {fullName}.", ToRepositoryPath(repositoryRoot, profilePath)));
                    continue;
                }

                if (!AllowedDecisions.Contains(decision))
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-INVALID-DECISION", $"Manual public API profile contains invalid decision for {fullName}: {decision}.", ToRepositoryPath(repositoryRoot, profilePath)));
                }

                if (string.IsNullOrWhiteSpace(rationale))
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-RATIONALE", $"Manual public API profile type entry must include non-empty rationale: {fullName}.", ToRepositoryPath(repositoryRoot, profilePath)));
                }

                if (string.Equals(decision, "approved", StringComparison.Ordinal))
                {
                    if (godotApiScope is null || !AllowedGodotApiScopes.Contains(godotApiScope))
                    {
                        errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-GODOT-API-SCOPE", $"Approved manual public API profile type must declare godotApiScope=full|subset: {fullName}.", ToRepositoryPath(repositoryRoot, profilePath)));
                    }
                }
                else if (godotApiScope is not null)
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-GODOT-API-SCOPE", $"Only approved manual public API profile types may declare godotApiScope: {fullName}.", ToRepositoryPath(repositoryRoot, profilePath)));
                }

                if (!GodotClassPacketExists(repositoryRoot, godotReference))
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-GODOT-REFERENCE", $"Manual public API profile type references missing Godot class packet: {godotReference}.", ToRepositoryPath(repositoryRoot, profilePath)));
                }

                if (string.Equals(decision, "approved", StringComparison.Ordinal) &&
                    rationale.Contains("editor/tools", StringComparison.OrdinalIgnoreCase) &&
                    !editorOnly)
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-EDITOR-ONLY-RATIONALE", $"Manual public API profile rationale limits an approved type to editor/tools, but editorOnly is not true: {fullName}.", ToRepositoryPath(repositoryRoot, profilePath)));
                }

                var hasGodotApiContract = TryValidateGodotApiContract(
                    repositoryRoot,
                    type,
                    fullName,
                    godotReference,
                    command,
                    step,
                    profilePath,
                    errors);
                if (string.Equals(godotApiScope, "subset", StringComparison.Ordinal) && !hasGodotApiContract)
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SUBSET-CONTRACT", $"Approved godotApiScope=subset type is missing godotApiContract: {fullName}.", ToRepositoryPath(repositoryRoot, profilePath)));
                }

                if (!string.Equals(godotApiScope, "subset", StringComparison.Ordinal) && hasGodotApiContract)
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SUBSET-CONTRACT", $"godotApiContract is allowed only for an approved godotApiScope=subset type: {fullName}.", ToRepositoryPath(repositoryRoot, profilePath)));
                }

                var electronApiContract = TryValidateElectronApiContract(
                    repositoryRoot,
                    type,
                    fullName,
                    godotReference,
                    command,
                    step,
                    profilePath,
                    errors);
                if (!string.Equals(godotApiScope, "subset", StringComparison.Ordinal) && electronApiContract is not null)
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SUBSET-CONTRACT", $"electronApiContract is allowed only for an approved godotApiScope=subset type: {fullName}.", ToRepositoryPath(repositoryRoot, profilePath)));
                }

                var normalizedFullName = NormalizeApiName(fullName);
                if (!entries.TryAdd(normalizedFullName, new ManualApiProfileType(fullName, godotReference, decision, rationale, editorOnly, godotApiScope, hasGodotApiContract, electronApiContract)))
                {
                    errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-DUPLICATE-TYPE", $"Manual public API profile contains duplicate type: {fullName}.", ToRepositoryPath(repositoryRoot, profilePath)));
                }
            }

            return new ManualApiProfileDocument(entries);
        }
        catch (JsonException ex)
        {
            errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-JSON", $"Manual public API profile is not valid JSON: {ex.Message}.", ToRepositoryPath(repositoryRoot, profilePath)));
            return new ManualApiProfileDocument(new Dictionary<string, ManualApiProfileType>(StringComparer.Ordinal));
        }
    }

    public static string NormalizeApiName(string value) => value.Replace('+', '.');

    private static bool GodotClassPacketExists(string repositoryRoot, string godotReference)
    {
        if (string.IsNullOrWhiteSpace(godotReference) ||
            godotReference.Contains('/') ||
            godotReference.Contains('\\') ||
            godotReference.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        var path = Path.Combine(repositoryRoot, "data", "api", "godot-4.7", "classes", godotReference + ".api.json");
        return File.Exists(path);
    }

    private static bool TryValidateGodotApiContract(
        string repositoryRoot,
        JsonElement type,
        string fullName,
        string godotReference,
        string command,
        string step,
        string profilePath,
        List<BuildDiagnostic> errors)
    {
        if (!type.TryGetProperty("godotApiContract", out var contract))
        {
            return false;
        }

        var repositoryPath = ToRepositoryPath(repositoryRoot, profilePath);
        if (contract.ValueKind != JsonValueKind.Object ||
            !ApiManifestCommand.TryGetString(contract, "scope", out var scope) ||
            !ApiManifestCommand.TryGetString(contract, "defaultMemberDecision", out var defaultDecision) ||
            !TryGetStringAllowEmpty(contract, "rationale", out var rationale) ||
            !ApiManifestCommand.TryGetArray(contract, "memberDecisions", out var memberDecisions) ||
            !ApiManifestCommand.TryGetArray(contract, "enumValueDecisions", out var enumValueDecisions))
        {
            errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SUBSET-CONTRACT", $"godotApiContract must contain scope, defaultMemberDecision, rationale, memberDecisions and enumValueDecisions: {fullName}.", repositoryPath));
            return true;
        }

        if (!string.Equals(scope, "subset", StringComparison.Ordinal) ||
            defaultDecision is not ("deferred" or "unsupported") ||
            string.IsNullOrWhiteSpace(rationale))
        {
            errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SUBSET-CONTRACT", $"godotApiContract must use scope=subset, a deferred/unsupported default and a non-empty rationale: {fullName}.", repositoryPath));
        }

        var packetPath = Path.Combine(repositoryRoot, "data", "api", "godot-4.7", "classes", godotReference + ".api.json");
        using var packet = File.Exists(packetPath)
            ? JsonDocument.Parse(File.ReadAllText(packetPath, Encoding.UTF8))
            : null;
        var memberNames = packet is null ||
            !packet.RootElement.TryGetProperty("members", out var membersElement) ||
            membersElement.ValueKind != JsonValueKind.Array
            ? []
            : membersElement.EnumerateArray()
                .Select(member => ApiManifestCommand.TryGetString(member, "godotName", out var name) ? name : string.Empty)
                .Where(name => name.Length > 0)
                .ToArray();
        var selectors = new HashSet<string>(StringComparer.Ordinal);
        foreach (var memberDecision in memberDecisions.EnumerateArray())
        {
            if (memberDecision.ValueKind != JsonValueKind.Object ||
                !ApiManifestCommand.TryGetString(memberDecision, "selector", out var selector) ||
                !ApiManifestCommand.TryGetString(memberDecision, "decision", out var memberDecisionValue) ||
                !TryGetStringAllowEmpty(memberDecision, "rationale", out var memberRationale) ||
                !AllowedDecisions.Contains(memberDecisionValue) ||
                string.IsNullOrWhiteSpace(memberRationale) ||
                !selectors.Add(selector) ||
                (memberNames.Length > 0 && !memberNames.Any(name => MatchesGodotMemberSelector(name, selector))))
            {
                errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SUBSET-CONTRACT", $"godotApiContract contains an invalid or unmatched member decision: {fullName}.", repositoryPath));
            }
        }

        var packetEnums = packet is null || !packet.RootElement.TryGetProperty("enums", out var enumsElement) || enumsElement.ValueKind != JsonValueKind.Array
            ? []
            : enumsElement.EnumerateArray().ToArray();
        var enumDecisionKeys = new HashSet<string>(StringComparer.Ordinal);
        var coveredEnumNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var enumDecision in enumValueDecisions.EnumerateArray())
        {
            var enumName = string.Empty;
            var valueName = string.Empty;
            var numericValue = string.Empty;
            var enumDecisionValue = string.Empty;
            var enumRationale = string.Empty;
            var valid = enumDecision.ValueKind == JsonValueKind.Object &&
                ApiManifestCommand.TryGetString(enumDecision, "enum", out enumName) &&
                ApiManifestCommand.TryGetString(enumDecision, "name", out valueName) &&
                ApiManifestCommand.TryGetString(enumDecision, "value", out numericValue) &&
                ApiManifestCommand.TryGetString(enumDecision, "decision", out enumDecisionValue) &&
                TryGetStringAllowEmpty(enumDecision, "rationale", out enumRationale) &&
                AllowedDecisions.Contains(enumDecisionValue) &&
                !string.IsNullOrWhiteSpace(enumRationale) &&
                enumDecisionKeys.Add(enumName + "\n" + valueName) &&
                GodotEnumValueMatches(packetEnums, enumName, valueName, numericValue);
            if (!valid)
            {
                errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SUBSET-CONTRACT", $"godotApiContract contains an invalid or unmatched enum value decision: {fullName}.", repositoryPath));
                continue;
            }

            coveredEnumNames.Add(enumName);
        }

        foreach (var enumName in coveredEnumNames)
        {
            var packetEnum = packetEnums.Single(item => ApiManifestCommand.TryGetString(item, "name", out var name) && string.Equals(name, enumName, StringComparison.Ordinal));
            var missing = packetEnum.GetProperty("values").EnumerateArray()
                .Select(value => ApiManifestCommand.TryGetString(value, "name", out var name) ? name : string.Empty)
                .Where(name => name.Length > 0 && !enumDecisionKeys.Contains(enumName + "\n" + name))
                .ToArray();
            if (missing.Length > 0)
            {
                errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SUBSET-CONTRACT", $"godotApiContract does not classify every value of {godotReference}.{enumName}: {string.Join(", ", missing)}.", repositoryPath));
            }
        }

        return true;
    }

    private static ManualElectronApiContract? TryValidateElectronApiContract(
        string repositoryRoot,
        JsonElement type,
        string fullName,
        string godotReference,
        string command,
        string step,
        string profilePath,
        List<BuildDiagnostic> errors)
    {
        if (!type.TryGetProperty("electronApiContract", out var contract))
        {
            return null;
        }

        var repositoryPath = ToRepositoryPath(repositoryRoot, profilePath);
        if (contract.ValueKind != JsonValueKind.Object ||
            !ApiManifestCommand.TryGetString(contract, "scope", out var scope) ||
            !ApiManifestCommand.TryGetString(contract, "defaultMemberDecision", out var defaultDecision) ||
            !TryGetStringAllowEmpty(contract, "rationale", out var rationale) ||
            !ApiManifestCommand.TryGetArray(contract, "memberDecisions", out var memberDecisions))
        {
            errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SUBSET-CONTRACT", $"electronApiContract must contain scope, defaultMemberDecision, rationale and memberDecisions: {fullName}.", repositoryPath));
            return new ManualElectronApiContract("unsupported", string.Empty, new Dictionary<string, ManualElectronApiMemberDecision>(StringComparer.Ordinal));
        }

        if (!string.Equals(scope, "exportedMembers", StringComparison.Ordinal) ||
            defaultDecision is not ("deferred" or "unsupported") ||
            string.IsNullOrWhiteSpace(rationale))
        {
            errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SUBSET-CONTRACT", $"electronApiContract must use scope=exportedMembers, a deferred/unsupported default and a non-empty rationale: {fullName}.", repositoryPath));
        }

        var packetPath = Path.Combine(repositoryRoot, "data", "api", "godot-4.7", "classes", godotReference + ".api.json");
        using var packet = File.Exists(packetPath)
            ? JsonDocument.Parse(File.ReadAllText(packetPath, Encoding.UTF8))
            : null;
        var decisions = new Dictionary<string, ManualElectronApiMemberDecision>(StringComparer.Ordinal);
        foreach (var memberDecision in memberDecisions.EnumerateArray())
        {
            var kind = string.Empty;
            var name = string.Empty;
            var decision = string.Empty;
            var compatibility = string.Empty;
            var memberRationale = string.Empty;
            string? godotName = null;
            var hasGodotName = memberDecision.ValueKind == JsonValueKind.Object &&
                TryGetOptionalString(memberDecision, "godotName", out godotName);
            var valid = memberDecision.ValueKind == JsonValueKind.Object &&
                ApiManifestCommand.TryGetString(memberDecision, "kind", out kind) &&
                ApiManifestCommand.TryGetString(memberDecision, "name", out name) &&
                ApiManifestCommand.TryGetString(memberDecision, "decision", out decision) &&
                ApiManifestCommand.TryGetString(memberDecision, "compatibility", out compatibility) &&
                TryGetStringAllowEmpty(memberDecision, "rationale", out memberRationale) &&
                hasGodotName &&
                AllowedDecisions.Contains(decision) &&
                compatibility is "godotMember" or "godotEnumValue" or "electronExtension" &&
                !string.IsNullOrWhiteSpace(memberRationale) &&
                (string.Equals(compatibility, "electronExtension", StringComparison.Ordinal) == (godotName is null)) &&
                IsApprovedGodotMapping(type, packet?.RootElement, compatibility, godotName);
            var key = kind + "\n" + name;
            if (!valid || !decisions.TryAdd(key, new ManualElectronApiMemberDecision(kind, name, decision, compatibility, godotName, memberRationale)))
            {
                errors.Add(Error(command, step, "E2D-BUILD-API-PROFILE-SUBSET-CONTRACT", $"electronApiContract contains an invalid, unmapped or duplicate member decision: {fullName}.{name}.", repositoryPath));
            }
        }

        return new ManualElectronApiContract(defaultDecision, rationale, decisions);
    }

    private static bool IsApprovedGodotMapping(
        JsonElement type,
        JsonElement? packet,
        string compatibility,
        string? godotName)
    {
        if (string.Equals(compatibility, "electronExtension", StringComparison.Ordinal))
        {
            return godotName is null;
        }

        if (packet is null || godotName is null ||
            !type.TryGetProperty("godotApiContract", out var godotContract))
        {
            return false;
        }

        if (string.Equals(compatibility, "godotMember", StringComparison.Ordinal))
        {
            var exists = packet.Value.TryGetProperty("members", out var members) &&
                members.ValueKind == JsonValueKind.Array &&
                members.EnumerateArray().Any(member =>
                    ApiManifestCommand.TryGetString(member, "godotName", out var name) &&
                    string.Equals(name, godotName, StringComparison.Ordinal));
            var approved = godotContract.TryGetProperty("memberDecisions", out var sourceDecisions) &&
                sourceDecisions.ValueKind == JsonValueKind.Array &&
                sourceDecisions.EnumerateArray().Any(sourceDecision =>
                    ApiManifestCommand.TryGetString(sourceDecision, "selector", out var selector) &&
                    ApiManifestCommand.TryGetString(sourceDecision, "decision", out var decision) &&
                    string.Equals(decision, "approved", StringComparison.Ordinal) &&
                    MatchesGodotMemberSelector(godotName, selector));
            return exists && approved;
        }

        var separator = godotName.IndexOf('.', StringComparison.Ordinal);
        if (separator <= 0 || separator == godotName.Length - 1)
        {
            return false;
        }

        var enumName = godotName[..separator];
        var valueName = godotName[(separator + 1)..];
        var existsEnumValue = packet.Value.TryGetProperty("enums", out var enums) &&
            enums.ValueKind == JsonValueKind.Array &&
            enums.EnumerateArray().Any(item =>
                ApiManifestCommand.TryGetString(item, "name", out var currentEnum) &&
                string.Equals(currentEnum, enumName, StringComparison.Ordinal) &&
                item.TryGetProperty("values", out var values) &&
                values.ValueKind == JsonValueKind.Array &&
                values.EnumerateArray().Any(value =>
                    ApiManifestCommand.TryGetString(value, "name", out var currentValue) &&
                    string.Equals(currentValue, valueName, StringComparison.Ordinal)));
        var approvedEnumValue = godotContract.TryGetProperty("enumValueDecisions", out var enumDecisions) &&
            enumDecisions.ValueKind == JsonValueKind.Array &&
            enumDecisions.EnumerateArray().Any(sourceDecision =>
                ApiManifestCommand.TryGetString(sourceDecision, "enum", out var currentEnum) &&
                ApiManifestCommand.TryGetString(sourceDecision, "name", out var currentValue) &&
                ApiManifestCommand.TryGetString(sourceDecision, "decision", out var decision) &&
                string.Equals(currentEnum, enumName, StringComparison.Ordinal) &&
                string.Equals(currentValue, valueName, StringComparison.Ordinal) &&
                string.Equals(decision, "approved", StringComparison.Ordinal));
        return existsEnumValue && approvedEnumValue;
    }

    private static bool MatchesGodotMemberSelector(string name, string selector)
    {
        var pattern = "^" + Regex.Escape(selector).Replace("\\*", ".*", StringComparison.Ordinal) + "$";
        return Regex.IsMatch(name, pattern, RegexOptions.CultureInvariant);
    }

    private static bool GodotEnumValueMatches(JsonElement[] packetEnums, string enumName, string valueName, string numericValue)
    {
        return packetEnums.Any(item =>
            ApiManifestCommand.TryGetString(item, "name", out var name) &&
            string.Equals(name, enumName, StringComparison.Ordinal) &&
            item.TryGetProperty("values", out var values) &&
            values.ValueKind == JsonValueKind.Array &&
            values.EnumerateArray().Any(value =>
                ApiManifestCommand.TryGetString(value, "name", out var currentValue) &&
                string.Equals(currentValue, valueName, StringComparison.Ordinal) &&
                ApiManifestCommand.TryGetString(value, "value", out var currentNumericValue) &&
                string.Equals(currentNumericValue, numericValue, StringComparison.Ordinal)));
    }

    private static bool TryGetStringAllowEmpty(JsonElement element, string propertyName, out string value)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetOptionalBool(JsonElement element, string propertyName, out bool value)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            value = false;
            return true;
        }

        if (property.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (property.ValueKind == JsonValueKind.False)
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }

    private static bool TryGetOptionalString(JsonElement element, string propertyName, out string? value)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            value = null;
            return true;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = null;
        return false;
    }

    private static string ToRepositoryPath(string repositoryRoot, string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static BuildDiagnostic Error(string command, string step, string code, string message, string path)
    {
        return new BuildDiagnostic(command, step, "error", code, message, Path: path);
    }
}

internal sealed class ApiCompatibilityVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, ApiManifestCommand apiManifest)
{
    private const string ManifestRelativePath = "data/api/electron2d-api-manifest.json";
    private const string ProfileRelativePath = "data/api/electron2d-public-api-profile.json";

    private static readonly string[] RequiredStatuses =
    [
        "Supported",
        "Deferred",
        "Unsupported",
        "Unapproved"
    ];

    private static readonly string[] ForbiddenTypes =
    [
        "Electron2D.IComponent",
        "Electron2D.SpriteRenderer",
        "Electron2D.SpriteAnimator",
        "Electron2D.AudioSource",
        "Electron2D.Rigidbody",
        "Electron2D.Collider",
        "Electron2D.BoxCollider",
        "Electron2D.CircleCollider",
        "Electron2D.PolygonCollider",
        "Electron2D.PhysicsBodyType"
    ];

    private static readonly string[] RootContractPaths =
    [
        "docs/release-management/api-compatibility.md",
        "docs/documentation/api-manifest.md",
        "docs/documentation/github-wiki-api-reference.md",
        "docs/releases/0.1-preview.md"
    ];

    private static readonly string[] ApiCompareGodotContractPaths =
    [
        "docs/architecture/agent-native-workflow.md",
        "docs/documentation/api-manifest.md",
        "docs/documentation/github-wiki-api-reference.md",
        "docs/cli/e2d-cli.md",
        "docs/releases/0.1-preview.md"
    ];

    private static readonly (string Path, string[] Required, string[] Forbidden)[] CurrentRootObjectDocumentationContracts =
    [
        (
            "docs/core-types/variant.md",
            ["Electron2D.ElectronObject", "`ElectronObject?`"],
            ["Electron2D.Object", "`Object?`"]),
        (
            "docs/object-model/base-object-lifetime.md",
            ["Electron2D.ElectronObject", "`ElectronObject`"],
            ["Electron2D.Object"]),
        (
            "docs/documentation/github-wiki-api-reference.md",
            ["`ElectronObject.md`"],
            ["`Object.md`"])
    ];

    private static readonly string[] StaleApiCompareGodotStrictParityFragments =
    [
        "Status: Supported / Parity verified",
        "является строгим verifier-ом",
        "strict verifier",
        "missing types: 0",
        "missing members: 0",
        "signature mismatches: 0",
        "inheritance mismatches: 0",
        "default mismatches: 0",
        "unexpected changes: 0"
    ];

    private static readonly Regex[] StaleApiCompareGodotStrictParityPatterns =
    [
        new(@"\bmissing\s+types\s*(?::|=)\s*0\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
        new(@"\bmissing\s+members\s*(?::|=)\s*0\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
        new(@"\bsignature\s+mismatches\s*(?::|=)\s*0\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
        new(@"\binheritance\s+mismatches\s*(?::|=)\s*0\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
        new(@"\bdefault\s+mismatches\s*(?::|=)\s*0\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
        new(@"\bunexpected\s+changes\s*(?::|=)\s*0\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
    ];

    private static readonly string[] StaleApiCompareGodotLookupContractFragments =
    [
        "не перечитывает manual profile напрямую",
        "не перечитывает его напрямую",
        "полный отчёт `api compare-godot`",
        "полном CLI compatibility report"
    ];

    private static readonly string[] FilledProfileForbiddenEmptyProfileFragments =
    [
        "Начальный профиль пустой",
        "Для пустого профиля",
        "до утверждения первых типов",
        "могут намеренно падать на текущей сборке",
        "ожидаемый profile-gate failure"
    ];

    private static readonly string[] ManualPublicApiListPaths =
    [
        "docs/release-management/api-compatibility.md",
        "docs/documentation/api-manifest.md",
        "docs/documentation/github-wiki-api-reference.md",
        "docs/releases/0.1-preview.md",
        "docs/core-types/variant.md",
        "docs/rendering/texture-resource-baseline.md",
        "docs/scripting/editor-script-workflow.md"
    ];

    private static readonly HashSet<string> ManualPublicApiAdvancedListPaths = new(StringComparer.Ordinal)
    {
        "docs/release-management/api-compatibility.md",
        "docs/documentation/api-manifest.md",
        "docs/documentation/github-wiki-api-reference.md",
        "docs/releases/0.1-preview.md",
        "docs/core-types/variant.md",
        "docs/rendering/texture-resource-baseline.md",
        "docs/scripting/editor-script-workflow.md"
    };

    private static readonly string[] RootContractWaiverStatusPaths =
    [
        "docs/release-management/api-compatibility.md",
        "docs/documentation/api-manifest.md",
        "docs/documentation/github-wiki-api-reference.md",
        "docs/releases/0.1-preview.md"
    ];

    private static readonly string[] ElectronExtensionContractPaths =
    [
        "docs/release-management/AUDIT-REQUEST.md",
        "docs/release-management/api-compatibility.md",
        "docs/architecture/engine-platform-stack.md",
        "docs/documentation/api-manifest.md",
        "docs/releases/0.1-preview.md"
    ];

    private const string ElectronExtensionContractFragment =
        "Намеренно отсутствующий или несовместимый Godot member обязан иметь явное `Deferred`/`Unsupported` решение. " +
        "Отдельный Electron2D-specific public extension допустим только при точном owner-approved member/value решении `electronExtension`, рабочем поведении и `parity = not_applicable`; " +
        "такой extension нельзя выдавать за совпадающий Godot member или использовать для молчаливого замещения отсутствующего Godot API.";

    private static readonly string[] ManualPublicApiListContextFragments =
    [
        "Public API",
        "C# API",
        "public surface",
        "public runtime surface",
        "release public model",
        "prerequisite manifest",
        "public UI types",
        "baseline",
        "exported runtime",
        "runtime экспорт",
        "экспортирует",
        "публичная поверхность",
        "публичной поверхности",
        "публичный API",
        "публичного API",
        "минимальный публичный API",
        "целевой публичный API",
        "полностью рабочими",
        "проверяемый минимум",
        "минимальный набор типов",
        "минимальные узлы",
        "минимальный публичный набор",
        "минимальные типы",
        "обязательный минимум",
        "обязательные ресурсы",
        "обязательны аналоги",
        "минимальный lifecycle",
        "нужен законченный 2D-набор",
        "формы:",
        "события:"
    ];

    private static readonly HashSet<string> ManualPublicApiFenceNames = new(StringComparer.Ordinal)
    {
        "Object",
        "RefCounted",
        "Resource",
        "Node",
        "SceneTree",
        "PackedScene",
        "Node2D",
        "CanvasItem",
        "CanvasLayer",
        "Viewport",
        "Timer",
        "_EnterTree",
        "_Ready",
        "_Process",
        "_PhysicsProcess",
        "_Input",
        "_ExitTree",
        "AddChild",
        "RemoveChild",
        "GetNode",
        "GetNodeOrNull",
        "QueueFree",
        "CallDeferred",
        "EmitSignal",
        "Connect",
        "Disconnect",
        "Vector2",
        "Vector2I",
        "Rect2",
        "Rect2I",
        "Transform2D",
        "Color",
        "Mathf",
        "RandomNumberGenerator",
        "NodePath",
        "StringName",
        "Callable",
        "Signal",
        "Rid",
        "Variant",
        "Sprite2D",
        "AnimatedSprite2D",
        "Camera2D",
        "TileMapLayer",
        "Polygon2D",
        "Line2D",
        "TextureRect",
        "ColorRect",
        "NinePatchRect",
        "Label",
        "DrawLine",
        "DrawPolyline",
        "DrawRect",
        "DrawCircle",
        "DrawPolygon",
        "DrawTexture",
        "DrawTextureRect",
        "DrawString",
        "QueueRedraw",
        "Texture2D",
        "AtlasTexture",
        "ImageTexture",
        "PlaceholderTexture2D",
        "Image",
        "SpriteFrames",
        "Font",
        "AudioStream",
        "Shader",
        "ShaderMaterial",
        "PhysicsMaterial",
        "Theme",
        "Translation",
        "InputMap",
        "RenderingServer",
        "RenderingDeviceFeatures",
        "Input.IsActionPressed",
        "Input.IsActionJustPressed",
        "Input.GetActionStrength",
        "Input.GetVector",
        "InputEventKey",
        "InputEventMouseButton",
        "InputEventMouseMotion",
        "InputEventJoypadButton",
        "InputEventJoypadMotion",
        "InputEventScreenTouch",
        "InputEventScreenDrag",
        "InputEventAction",
        "StaticBody2D",
        "RigidBody2D",
        "CharacterBody2D",
        "Area2D",
        "CollisionShape2D",
        "RayCast2D",
        "RectangleShape2D",
        "CircleShape2D",
        "CapsuleShape2D",
        "SegmentShape2D",
        "ConvexPolygonShape2D",
        "ConcavePolygonShape2D",
        "MoveAndCollide",
        "MoveAndSlide",
        "IsOnFloor",
        "IsOnWall",
        "IsOnCeiling",
        "FloorNormal",
        "FloorSnapLength",
        "UpDirection",
        "Animation",
        "AnimationLibrary",
        "AnimationPlayer",
        "Tween",
        "Control",
        "Container",
        "Panel",
        "Button",
        "TextureButton",
        "LineEdit",
        "TextEdit",
        "CodeEdit",
        "SyntaxHighlighter",
        "CodeHighlighter",
        "PopupMenu",
        "TabContainer",
        "Tree",
        "ItemList",
        "SplitContainer",
        "ScrollBar",
        "Label",
        "CheckBox",
        "Slider",
        "ProgressBar",
        "ScrollContainer",
        "HBoxContainer",
        "VBoxContainer",
        "GridContainer",
        "MarginContainer",
        "CenterContainer",
        "Export",
        "Tool",
        "AudioStreamPlayer",
        "AudioStreamPlayer2D",
        "AudioServer"
    };

    private static readonly string[] RootContractRequiredFragments =
    [
        "Корневой контракт совместимости Godot 4.7",
        "`4.7-stable`",
        "`T-0242`",
        "`T-0243`",
        "`T-0244`",
        "`T-0245`",
        "manual API profile",
        "C#",
        "HLSL",
        "Windows/Linux/macOS",
        "Android/iOS/WebAssembly browser",
        "`EditorOnly`"
    ];

    private static readonly string[] RootContractDomainRequiredFragments =
    [
        "## Карта потребителей контракта",
        "Потребитель / задача",
        "Первое evidence",
        "verify ui-public-api-gate --wiki-path .github/wiki",
        "`API-UI-and-Text.md`",
        "`API-Compatibility.md`",
        "`Type | Status | Decision | Rationale`",
        "колонки `Status`"
    ];

    private static readonly string[] RootContractForbiddenFragments =
    [
        "docs/runtime/godot-47-public-api-public-api-parity-contract.md",
        "Electron2D 0.1-preview 2D",
        "утверждённый 2D-профиль",
        "утверждённого 2D-профиля",
        "утверждённом 2D-профиле",
        "обязательного 2D-профиля",
        "согласованного Electron2D 2D-поднабора",
        "согласованном 2D-поднаборе",
        "API вне 2D-профиля",
        "Не 100% всего Godot API.",
        "100% Godot C# API внутри утверждённого 2D-профиля.",
        "расширение этой проверки отдельными UI/Text-правилами остаётся C#-миграционным долгом",
        "Отдельная командная проверка этого правила должна быть перенесена в C#-инструмент"
    ];

    private static readonly Regex CompatibilityRowPattern = new(
        @"^\|\s*`(?<type>Electron2D\.[^`|]+)`\s*\|(?:[^|]*\|)*\s*(?<status>Supported|Deferred|Unsupported|Unapproved|Partial|Experimental|Planned)\s*\|",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ManualPublicApiListPattern = new(
        @"^\s*-\s*`Electron2D\.[^`]+`\s*[.;:,]?\s*$",
        RegexOptions.CultureInvariant);

    private static readonly Regex ManualPublicApiFenceLinePattern = new(
        @"^\s*(?<name>_?[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)?)(?:\([^)]*\))?\s*[.;:,]?\s*$",
        RegexOptions.CultureInvariant);

    private static readonly Regex ManualPublicApiCSharpDeclarationPattern = new(
        @"^\s*(?:public|protected|internal|private)\s+(?:abstract\s+|sealed\s+|readonly\s+|static\s+|partial\s+)*(?:(?:class|struct|record|interface|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)|[A-Za-z_][A-Za-z0-9_<>?.]*(?:\[\])?\s+(?<name>[A-Z][A-Za-z0-9_]*)\s*(?:\(|\{|;|=>|=))",
        RegexOptions.CultureInvariant);

    private static readonly Regex ManualPublicApiAttributeLinePattern = new(
        @"^\s*\[(?<name>[A-Za-z][A-Za-z0-9_]*)\]\s*[.;:,]?\s*$",
        RegexOptions.CultureInvariant);

    private static readonly Regex ManualPublicApiInlineTokenPattern = new(
        @"`(?<name>_?[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)?(?:\(\))?)`|(?<![A-Za-z0-9_])(?<name>_?[A-Z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)?(?:\(\))?)(?![A-Za-z0-9_])",
        RegexOptions.CultureInvariant);

    private static readonly Regex UnapprovedWaiverStatusPattern = new(
        @"(?im)^\s*\|\s*`?Not\s+planned`?\s*\|",
        RegexOptions.CultureInvariant);

    public int Verify(string[] args)
    {
        var parse = Parse(args);
        if (!parse.Succeeded)
        {
            diagnostics.Write(new BuildDiagnostic("verify", "verify api-compatibility", "error", "E2D-BUILD-CLI-INVALID-ARGUMENTS", parse.ErrorMessage));
            return RepositoryBuildExitCodes.Failed;
        }

        var manifestPath = Path.Combine(repositoryRoot, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var profilePath = Path.Combine(repositoryRoot, ProfileRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var manifestShape = apiManifest.VerifyShape(manifestPath, "verify", "verify api-compatibility");
        if (manifestShape != RepositoryBuildExitCodes.Success)
        {
            return manifestShape;
        }

        var compatibilityPath = ResolveCompatibilityPath(parse.WikiPath);
        var errors = new List<BuildDiagnostic>();
        int failWithErrors()
        {
            foreach (var error in errors)
            {
                diagnostics.Write(error);
            }

            return RepositoryBuildExitCodes.Failed;
        }

        var profile = ManualApiProfileReader.Read(repositoryRoot, profilePath, "verify", "verify api-compatibility", errors);
        if (errors.Count > 0)
        {
            return failWithErrors();
        }

        var manifestTypes = ReadManifestTypes(manifestPath, errors);
        if (errors.Count > 0)
        {
            return failWithErrors();
        }

        VerifyForbiddenManifestTypes(manifestTypes, errors);
        if (errors.Count > 0)
        {
            return failWithErrors();
        }

        VerifyManualProfileGate(profile, manifestTypes, errors, "verify", "verify api-compatibility");
        VerifySubsetExportedMemberGate(profile, errors, "verify", "verify api-compatibility");
        if (errors.Count > 0)
        {
            return failWithErrors();
        }

        if (!File.Exists(compatibilityPath))
        {
            errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-WIKI-MISSING", "GitHub Wiki compatibility page was not found.", ToRepositoryPath(compatibilityPath)));
        }

        if (File.Exists(compatibilityPath))
        {
            VerifyCompatibilityPage(compatibilityPath, manifestTypes, errors);
        }

        if (File.Exists(Path.Combine(repositoryRoot, "mkdocs.yml")))
        {
            errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-LOCAL-SITE", "Local documentation site configuration mkdocs.yml is not allowed for the GitHub Wiki table.", "mkdocs.yml"));
        }

        if (Directory.Exists(Path.Combine(repositoryRoot, "site")))
        {
            errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-LOCAL-SITE", "Local generated site directory is not allowed for the GitHub Wiki table.", "site"));
        }

        VerifyRootPublicApiContract(profile, manifestTypes, errors);

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            "verify api-compatibility",
            "info",
            "E2D-BUILD-API-COMPATIBILITY-PASSED",
            $"API compatibility verification passed. Public types: {manifestTypes.Count}.",
            Path: ToRepositoryPath(compatibilityPath)));
        return RepositoryBuildExitCodes.Success;
    }

    private static IReadOnlyList<string> ReadManifestTypes(string manifestPath, List<BuildDiagnostic> errors)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
            if (!ApiManifestCommand.TryGetArray(document.RootElement, "types", out var types))
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            foreach (var type in types.EnumerateArray())
            {
                if (ApiManifestCommand.TryGetString(type, "fullName", out var fullName))
                {
                    result.Add(fullName);
                }
            }

            return result;
        }
        catch (JsonException ex)
        {
            errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-MANIFEST-JSON", $"API manifest could not be read after shape verification: {ex.Message}.", ManifestRelativePath));
            return Array.Empty<string>();
        }
    }

    private static void VerifyForbiddenManifestTypes(IReadOnlyList<string> manifestTypes, List<BuildDiagnostic> errors)
    {
        var forbidden = new HashSet<string>(ForbiddenTypes, StringComparer.Ordinal);
        foreach (var typeName in manifestTypes.Order(StringComparer.Ordinal))
        {
            if (forbidden.Contains(typeName))
            {
                errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-FORBIDDEN-TYPE", $"Forbidden legacy type is exported in API manifest: {typeName}.", ManifestRelativePath));
            }
        }
    }

    private void VerifyCompatibilityPage(string compatibilityPath, IReadOnlyList<string> manifestTypes, List<BuildDiagnostic> errors)
    {
        var text = Normalize(File.ReadAllText(compatibilityPath, Encoding.UTF8));
        if (!text.Contains("Generated by eng/Electron2D.Build update wiki", StringComparison.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-GENERATED-MARKER", "Generated API compatibility page is missing the generated marker.", ToRepositoryPath(compatibilityPath)));
        }

        if (!text.Contains("Generated from `data/api/electron2d-public-api-profile.json`.", StringComparison.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-SOURCE", "Generated API compatibility page must identify the manual public API profile as its source.", ToRepositoryPath(compatibilityPath)));
        }

        foreach (var required in new[] { "## Status Legend", "## Current Public Runtime Surface" })
        {
            if (!text.Contains(required, StringComparison.Ordinal))
            {
                errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-SHAPE", $"Compatibility page is missing required structure: {required}.", ToRepositoryPath(compatibilityPath)));
            }
        }

        foreach (var status in RequiredStatuses)
        {
            if (!text.Contains($"| {status} |", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-STATUS", $"Compatibility table does not contain status: {status}.", ToRepositoryPath(compatibilityPath)));
            }
        }

        var rows = ExtractCompatibilityRows(text, errors, compatibilityPath);
        var normalizedRows = rows.Keys
            .Select(typeName => typeName.Replace('+', '.'))
            .ToHashSet(StringComparer.Ordinal);
        var forbidden = new HashSet<string>(ForbiddenTypes, StringComparer.Ordinal);
        foreach (var typeName in manifestTypes.Order(StringComparer.Ordinal))
        {
            if (forbidden.Contains(typeName))
            {
                errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-FORBIDDEN-TYPE", $"Forbidden legacy type is exported in API manifest: {typeName}.", ManifestRelativePath));
            }

            if (!rows.ContainsKey(typeName) && !normalizedRows.Contains(typeName))
            {
                errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-TYPE-MISSING", $"Generated compatibility page is missing public type row: {typeName}.", ToRepositoryPath(compatibilityPath)));
            }
        }

        foreach (var typeName in rows.Keys.Order(StringComparer.Ordinal))
        {
            if (forbidden.Contains(typeName))
            {
                errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-FORBIDDEN-TYPE", $"Forbidden legacy type is published in GitHub Wiki compatibility table: {typeName}.", ToRepositoryPath(compatibilityPath)));
            }
        }
    }

    private void VerifyManualProfileGate(
        ManualApiProfileDocument profile,
        IReadOnlyList<string> manifestTypes,
        List<BuildDiagnostic> errors,
        string command,
        string step)
    {
        foreach (var typeName in manifestTypes.Order(StringComparer.Ordinal))
        {
            var normalized = ManualApiProfileReader.NormalizeApiName(typeName);
            if (!profile.Types.TryGetValue(normalized, out var entry))
            {
                errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT", $"Exported runtime public type is missing approved manual profile decision: {typeName}.", Path: ProfileRelativePath));
                return;
            }

            if (!string.Equals(entry.Decision, "approved", StringComparison.Ordinal))
            {
                errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT", $"Exported runtime public type is excluded by manual profile decision '{entry.Decision}': {typeName}.", Path: ProfileRelativePath));
                return;
            }

            if (entry.EditorOnly)
            {
                errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-PROFILE-EDITOR-ONLY-EXPORT", $"Exported runtime public type is marked editorOnly in manual profile and must not be part of exported game runtime API: {typeName}.", Path: ProfileRelativePath));
                return;
            }
        }
    }

    private void VerifySubsetExportedMemberGate(
        ManualApiProfileDocument profile,
        List<BuildDiagnostic> errors,
        string command,
        string step)
    {
        var manifestPath = Path.Combine(repositoryRoot, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
            if (!ApiManifestCommand.TryGetArray(document.RootElement, "types", out var types))
            {
                return;
            }

            foreach (var type in types.EnumerateArray())
            {
                if (!ApiManifestCommand.TryGetString(type, "fullName", out var fullName) ||
                    !profile.Types.TryGetValue(ManualApiProfileReader.NormalizeApiName(fullName), out var profileType) ||
                    !string.Equals(profileType.GodotApiScope, "subset", StringComparison.Ordinal) ||
                    !ApiManifestCommand.TryGetArray(type, "members", out var members))
                {
                    continue;
                }

                foreach (var member in members.EnumerateArray())
                {
                    if (!ApiManifestCommand.TryGetString(member, "kind", out var kind) ||
                        !ApiManifestCommand.TryGetString(member, "name", out var name))
                    {
                        continue;
                    }

                    var key = kind + "\n" + name;
                    ManualElectronApiMemberDecision? decision = null;
                    var hasApprovedDecision = profileType.ElectronApiContract is not null &&
                        profileType.ElectronApiContract.MemberDecisions.TryGetValue(key, out decision) &&
                        string.Equals(decision.Decision, "approved", StringComparison.Ordinal);
                    if (!hasApprovedDecision)
                    {
                        errors.Add(new BuildDiagnostic(
                            command,
                            step,
                            "error",
                            "E2D-BUILD-API-PROFILE-SUBSET-EXPORTED-MEMBER",
                            $"Exported subset member is not explicitly approved by electronApiContract: {fullName}.{name} ({kind}).",
                            Path: ProfileRelativePath));
                        continue;
                    }

                    if (!ManifestMemberDecisionMatches(member, decision!))
                    {
                        errors.Add(new BuildDiagnostic(
                            command,
                            step,
                            "error",
                            "E2D-BUILD-API-PROFILE-SUBSET-EXPORTED-MEMBER",
                            $"Generated manifest member decision does not match electronApiContract: {fullName}.{name} ({kind}).",
                            Path: ManifestRelativePath));
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-COMPATIBILITY-MANIFEST-JSON", $"API manifest subset member decisions could not be read: {ex.Message}", Path: ManifestRelativePath));
        }
    }

    private static bool ManifestMemberDecisionMatches(JsonElement member, ManualElectronApiMemberDecision expected)
    {
        if (!member.TryGetProperty("electronApiDecision", out var actual) ||
            actual.ValueKind != JsonValueKind.Object ||
            !ApiManifestCommand.TryGetString(actual, "kind", out var kind) ||
            !ApiManifestCommand.TryGetString(actual, "name", out var name) ||
            !ApiManifestCommand.TryGetString(actual, "decision", out var decision) ||
            !ApiManifestCommand.TryGetString(actual, "compatibility", out var compatibility) ||
            !TryGetOptionalStringValue(actual, "godotName", out var godotName) ||
            !member.TryGetProperty("profile", out var memberProfile) ||
            !ApiManifestCommand.TryGetString(memberProfile, "status", out var status) ||
            !ApiManifestCommand.TryGetString(memberProfile, "parity", out var parity))
        {
            return false;
        }

        var expectedParity = string.Equals(expected.Compatibility, "electronExtension", StringComparison.Ordinal)
            ? "not_applicable"
            : "profile_approved";
        return string.Equals(kind, expected.Kind, StringComparison.Ordinal) &&
            string.Equals(name, expected.Name, StringComparison.Ordinal) &&
            string.Equals(decision, expected.Decision, StringComparison.Ordinal) &&
            string.Equals(compatibility, expected.Compatibility, StringComparison.Ordinal) &&
            string.Equals(godotName, expected.GodotName, StringComparison.Ordinal) &&
            string.Equals(status, "supported", StringComparison.Ordinal) &&
            string.Equals(parity, expectedParity, StringComparison.Ordinal);
    }

    private static bool TryGetOptionalStringValue(JsonElement element, string propertyName, out string? value)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            value = null;
            return true;
        }

        if (property.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.GetString()))
        {
            value = property.GetString();
            return true;
        }

        value = null;
        return false;
    }

    private void VerifyRootPublicApiContract(ManualApiProfileDocument profile, IReadOnlyList<string> manifestTypes, List<BuildDiagnostic> errors)
    {
        var combined = new StringBuilder();
        foreach (var relativePath in RootContractPaths)
        {
            var path = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-CONTRACT-DOC-MISSING", $"Root public API contract document was not found: {relativePath}.", relativePath));
                continue;
            }

            combined.AppendLine(Normalize(File.ReadAllText(path, Encoding.UTF8)));
        }

        var text = combined.ToString();
        foreach (var required in RootContractRequiredFragments)
        {
            if (!text.Contains(required, StringComparison.Ordinal))
            {
                errors.Add(Error(
                    "E2D-BUILD-API-COMPATIBILITY-CONTRACT-FRAGMENT",
                    $"Root public API contract is missing required fragment: {required}.",
                    "docs/release-management/api-compatibility.md"));
            }
        }

        foreach (var forbidden in RootContractForbiddenFragments)
        {
            if (text.Contains(forbidden, StringComparison.Ordinal))
            {
                errors.Add(Error(
                    "E2D-BUILD-API-COMPATIBILITY-CONTRACT-OBSOLETE",
                    $"Root public API contract contains obsolete fragment: {forbidden}.",
                    "docs/release-management/api-compatibility.md"));
            }
        }

        VerifyNoManualPublicApiLists(manifestTypes, errors);
        VerifyNoUnapprovedWaiverStatuses(errors);
        VerifyElectronExtensionContract(errors);
        VerifyApiCompareGodotProfileApprovalContract(errors);
        VerifyCurrentRootObjectDocumentationContract(errors);
        VerifyFilledManualProfileDocumentation(profile, manifestTypes, errors);
        VerifyDomainDocumentConsumerMap(errors);
    }

    private void VerifyCurrentRootObjectDocumentationContract(List<BuildDiagnostic> errors)
    {
        foreach (var contract in CurrentRootObjectDocumentationContracts)
        {
            var relativePath = contract.Path;
            var path = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                continue;
            }

            var text = Normalize(File.ReadAllText(path, Encoding.UTF8));
            foreach (var required in contract.Required)
            {
                if (text.Contains(required, StringComparison.Ordinal))
                {
                    continue;
                }

                errors.Add(Error(
                    "E2D-BUILD-API-COMPATIBILITY-STALE-ROOT-OBJECT-NAME",
                    $"Current Electron2D domain documentation is missing the required ElectronObject fragment: {required}.",
                    relativePath));
            }

            foreach (var forbidden in contract.Forbidden)
            {
                if (text.Contains(forbidden, StringComparison.Ordinal))
                {
                    errors.Add(Error(
                        "E2D-BUILD-API-COMPATIBILITY-STALE-ROOT-OBJECT-NAME",
                        $"Current Electron2D domain documentation contains an obsolete engine-root fragment: {forbidden}.",
                        relativePath));
                }
            }
        }
    }

    private void VerifyApiCompareGodotProfileApprovalContract(List<BuildDiagnostic> errors)
    {
        foreach (var relativePath in ApiCompareGodotContractPaths)
        {
            var path = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                continue;
            }

            var text = Normalize(File.ReadAllText(path, Encoding.UTF8));
            if (!text.Contains("api compare-godot", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var forbidden in StaleApiCompareGodotStrictParityFragments)
            {
                if (text.Contains(forbidden, StringComparison.Ordinal))
                {
                    errors.Add(Error(
                        "E2D-BUILD-API-COMPATIBILITY-STALE-PARITY-CLAIM",
                        $"Current api compare-godot documentation must describe profile approval with not_verified parity evidence, not a strict parity proof: {forbidden}.",
                        relativePath));
                    break;
                }
            }

            foreach (var forbidden in StaleApiCompareGodotStrictParityPatterns)
            {
                if (forbidden.IsMatch(text))
                {
                    errors.Add(Error(
                        "E2D-BUILD-API-COMPATIBILITY-STALE-PARITY-CLAIM",
                        $"Current api compare-godot documentation must describe profile approval with not_verified parity evidence, not strict parity counters matched by pattern: {forbidden}.",
                        relativePath));
                    break;
                }
            }

            foreach (var forbidden in StaleApiCompareGodotLookupContractFragments)
            {
                if (!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                errors.Add(Error(
                    "E2D-BUILD-API-COMPATIBILITY-STALE-LOOKUP-CONTRACT",
                    $"Current api compare-godot documentation must describe a single-type lookup that reads the manual profile directly and uses the manifest only for runtime availability and parity evidence: {forbidden}.",
                    relativePath));
                break;
            }
        }
    }

    private void VerifyFilledManualProfileDocumentation(ManualApiProfileDocument profile, IReadOnlyList<string> manifestTypes, List<BuildDiagnostic> errors)
    {
        if (profile.Types.Count == 0 && manifestTypes.Count == 0)
        {
            return;
        }

        const string RelativePath = "docs/documentation/api-manifest.md";
        var path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            return;
        }

        var text = Normalize(File.ReadAllText(path, Encoding.UTF8));
        foreach (var forbidden in FilledProfileForbiddenEmptyProfileFragments)
        {
            if (text.Contains(forbidden, StringComparison.Ordinal))
            {
                errors.Add(Error(
                    "E2D-BUILD-API-COMPATIBILITY-STALE-EMPTY-PROFILE",
                    $"API manifest documentation still describes an empty current profile even though the profile or manifest is populated: {forbidden}.",
                    RelativePath));
                return;
            }
        }
    }

    private void VerifyNoUnapprovedWaiverStatuses(List<BuildDiagnostic> errors)
    {
        foreach (var relativePath in RootContractWaiverStatusPaths)
        {
            var path = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                continue;
            }

            var text = Normalize(File.ReadAllText(path, Encoding.UTF8));
            if (UnapprovedWaiverStatusPattern.IsMatch(text))
            {
                errors.Add(Error(
                    "E2D-BUILD-API-COMPATIBILITY-CONTRACT-WAIVER-STATUS",
                    $"Root public API contract must use explicit Deferred/Unsupported manual profile decisions instead of an unapproved waiver status: {relativePath}.",
                    relativePath));
            }
        }
    }

    private void VerifyElectronExtensionContract(List<BuildDiagnostic> errors)
    {
        foreach (var relativePath in ElectronExtensionContractPaths)
        {
            var path = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                continue;
            }

            var text = Normalize(File.ReadAllText(path, Encoding.UTF8));
            if (text.Contains(ElectronExtensionContractFragment, StringComparison.Ordinal))
            {
                continue;
            }

            errors.Add(Error(
                "E2D-BUILD-API-COMPATIBILITY-EXTENSION-CONTRACT",
                $"Root public API document must preserve the owner-approved Electron2D extension contract: {relativePath}.",
                relativePath));
        }
    }

    private void VerifyDomainDocumentConsumerMap(List<BuildDiagnostic> errors)
    {
        const string relativePath = "docs/release-management/api-compatibility.md";
        var path = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            return;
        }

        var text = Normalize(File.ReadAllText(path, Encoding.UTF8));
        foreach (var required in RootContractDomainRequiredFragments)
        {
            if (!text.Contains(required, StringComparison.Ordinal))
            {
                errors.Add(Error(
                    "E2D-BUILD-API-COMPATIBILITY-CONTRACT-FRAGMENT",
                    $"Root public API domain contract is missing required fragment: {required}.",
                    relativePath));
            }
        }
    }

    private void VerifyNoManualPublicApiLists(IReadOnlyList<string> manifestTypes, List<BuildDiagnostic> errors)
    {
        var publicApiNames = BuildManualPublicApiNameSet(manifestTypes);
        foreach (var relativePath in ManualPublicApiListPaths)
        {
            var path = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                continue;
            }

            var text = Normalize(File.ReadAllText(path, Encoding.UTF8));
            var enableAdvancedDetection = ManualPublicApiAdvancedListPaths.Contains(relativePath);
            if (ContainsManualPublicApiList(text, publicApiNames, enableAdvancedDetection))
            {
                errors.Add(Error(
                    "E2D-BUILD-API-COMPATIBILITY-CONTRACT-MANUAL-LIST",
                    $"Root public API contract and scoped domain documents must not contain a manual Markdown or fenced-code list of public API elements: {relativePath}.",
                    relativePath));
            }
        }
    }

    private static HashSet<string> BuildManualPublicApiNameSet(IReadOnlyList<string> manifestTypes)
    {
        var names = new HashSet<string>(ManualPublicApiFenceNames, StringComparer.Ordinal);
        foreach (var fullName in manifestTypes)
        {
            names.Add(fullName);
            const string prefix = "Electron2D.";
            if (fullName.StartsWith(prefix, StringComparison.Ordinal))
            {
                var shortName = fullName[prefix.Length..].Replace('+', '.');
                names.Add(shortName);
                var nestedNameSeparator = shortName.LastIndexOf('.');
                if (nestedNameSeparator >= 0)
                {
                    names.Add(shortName[(nestedNameSeparator + 1)..]);
                }

                const string attributeSuffix = "Attribute";
                if (shortName.EndsWith(attributeSuffix, StringComparison.Ordinal) && shortName.Length > attributeSuffix.Length)
                {
                    names.Add(shortName[..^attributeSuffix.Length]);
                }
            }
        }

        return names;
    }

    private static bool ContainsManualPublicApiList(string text, ISet<string> publicApiNames, bool enableAdvancedDetection)
    {
        var inManualSurfaceContext = false;
        var inCodeFence = false;
        var codeFenceStartedInManualSurfaceContext = false;
        var codeFencePublicApiMatches = 0;
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                if (inCodeFence)
                {
                    if (codeFenceStartedInManualSurfaceContext && codeFencePublicApiMatches >= 2)
                    {
                        return true;
                    }

                    inCodeFence = false;
                    codeFenceStartedInManualSurfaceContext = false;
                    codeFencePublicApiMatches = 0;
                }
                else
                {
                    inCodeFence = true;
                    codeFenceStartedInManualSurfaceContext = inManualSurfaceContext;
                    codeFencePublicApiMatches = 0;
                }

                continue;
            }

            if (inCodeFence)
            {
                if (enableAdvancedDetection && codeFenceStartedInManualSurfaceContext && IsManualPublicApiFenceLine(line, publicApiNames))
                {
                    codeFencePublicApiMatches++;
                }

                continue;
            }

            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                inManualSurfaceContext = ContainsManualPublicApiListContext(line);
            }
            else if (line.Length > 0 && ContainsManualPublicApiListContext(line))
            {
                inManualSurfaceContext = true;
            }

            if (enableAdvancedDetection && line.Length > 0 && inManualSurfaceContext && CountManualPublicApiInlineNames(line, publicApiNames) >= 3)
            {
                return true;
            }

            if (inManualSurfaceContext && ManualPublicApiListPattern.IsMatch(line))
            {
                return true;
            }
        }

        return codeFenceStartedInManualSurfaceContext && codeFencePublicApiMatches >= 2;
    }

    private static bool ContainsManualPublicApiListContext(string line)
    {
        return ManualPublicApiListContextFragments.Any(fragment => line.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsManualPublicApiFenceLine(string line, ISet<string> publicApiNames)
    {
        var csharpDeclarationMatch = ManualPublicApiCSharpDeclarationPattern.Match(line);
        if (csharpDeclarationMatch.Success && publicApiNames.Contains(csharpDeclarationMatch.Groups["name"].Value))
        {
            return true;
        }

        var attributeMatch = ManualPublicApiAttributeLinePattern.Match(line);
        if (attributeMatch.Success && publicApiNames.Contains(attributeMatch.Groups["name"].Value))
        {
            return true;
        }

        var match = ManualPublicApiFenceLinePattern.Match(line);
        return match.Success && IsManualPublicApiName(match.Groups["name"].Value, publicApiNames);
    }

    private static int CountManualPublicApiInlineNames(string line, ISet<string> publicApiNames)
    {
        var matches = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in ManualPublicApiInlineTokenPattern.Matches(line))
        {
            var value = match.Groups["name"].Value;
            if (IsManualPublicApiName(value, publicApiNames))
            {
                matches.Add(NormalizeManualPublicApiToken(value));
            }
        }

        return matches.Count;
    }

    private static bool IsManualPublicApiName(string value, ISet<string> publicApiNames)
    {
        var normalized = NormalizeManualPublicApiToken(value);
        if (publicApiNames.Contains(normalized))
        {
            return true;
        }

        var firstSegment = normalized.Split('.')[0];
        return publicApiNames.Contains(firstSegment);
    }

    private static string NormalizeManualPublicApiToken(string value)
    {
        var normalized = value.Trim();
        if (normalized.EndsWith("()", StringComparison.Ordinal))
        {
            normalized = normalized[..^2];
        }

        return normalized;
    }

    private Dictionary<string, string> ExtractCompatibilityRows(string text, List<BuildDiagnostic> errors, string compatibilityPath)
    {
        var rows = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            var match = CompatibilityRowPattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var typeName = match.Groups["type"].Value;
            var status = match.Groups["status"].Value;
            if (!rows.TryAdd(typeName, status))
            {
                errors.Add(Error("E2D-BUILD-API-COMPATIBILITY-DUPLICATE-TYPE", $"Compatibility table contains duplicate row for public type: {typeName}.", ToRepositoryPath(compatibilityPath)));
            }
        }

        return rows;
    }

    private string ResolveCompatibilityPath(string wikiPath)
    {
        var resolved = ResolveRepositoryOrAbsolutePath(wikiPath);
        if (File.Exists(resolved) && resolved.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return resolved;
        }

        return Path.Combine(resolved, "API-Compatibility.md");
    }

    private string ResolveRepositoryOrAbsolutePath(string path)
    {
        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar)));
    }

    private string ToRepositoryPath(string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private static BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("verify", "verify api-compatibility", "error", code, message, Path: path);
    }

    private static ApiCompatibilityArguments Parse(string[] args)
    {
        if (args.Length == 4 &&
            string.Equals(args[2], "--wiki-path", StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(args[3]))
        {
            return new ApiCompatibilityArguments(true, args[3], string.Empty);
        }

        return new ApiCompatibilityArguments(false, string.Empty, "Expected: verify api-compatibility --wiki-path <path>.");
    }

    private sealed record ApiCompatibilityArguments(bool Succeeded, string WikiPath, string ErrorMessage);
}

internal sealed class ApiManifestCommand(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    private const string DefaultManifestRelativePath = "data/api/electron2d-api-manifest.json";
    private const string DefaultProfileRelativePath = "data/api/electron2d-public-api-profile.json";

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var parse = Parse(args);
        if (!parse.Succeeded)
        {
            diagnostics.Write(new BuildDiagnostic("update", "update api-manifest", "error", "E2D-BUILD-CLI-INVALID-ARGUMENTS", parse.ErrorMessage));
            return RepositoryBuildExitCodes.Failed;
        }

        var outputPath = ResolveRepositoryOrAbsolutePath(parse.OutputPath ?? DefaultManifestRelativePath);
        var targetPath = outputPath;
        var expectedPath = parse.Check
            ? Path.Combine(repositoryRoot, ".temp", "api-manifest", "expected", "electron2d-api-manifest.json")
            : outputPath;
        Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);

        var generated = await GenerateManifestAsync(expectedPath, parse.WikiPath ?? ".github/wiki", cancellationToken).ConfigureAwait(false);
        if (generated != RepositoryBuildExitCodes.Success)
        {
            return generated;
        }

        var profileGate = VerifyGeneratedManifestProfileGate(expectedPath, parse.Check ? "update api-manifest --check" : "update api-manifest");
        if (profileGate != RepositoryBuildExitCodes.Success)
        {
            return profileGate;
        }

        if (parse.Check)
        {
            if (!File.Exists(targetPath))
            {
                diagnostics.Write(new BuildDiagnostic("update", "update api-manifest --check", "error", "E2D-BUILD-API-MANIFEST-MISSING", "API manifest was not found.", Path: ToRepositoryPath(targetPath)));
                return RepositoryBuildExitCodes.Failed;
            }

            if (!string.Equals(Normalize(File.ReadAllText(expectedPath, Encoding.UTF8)), Normalize(File.ReadAllText(targetPath, Encoding.UTF8)), StringComparison.Ordinal))
            {
                diagnostics.Write(new BuildDiagnostic("update", "update api-manifest --check", "error", "E2D-BUILD-API-MANIFEST-STALE", "API manifest is out of date.", Path: ToRepositoryPath(targetPath)));
                return RepositoryBuildExitCodes.Failed;
            }

            var shape = VerifyShape(targetPath, "update", "update api-manifest --check");
            if (shape != RepositoryBuildExitCodes.Success)
            {
                return shape;
            }

            diagnostics.Write(new BuildDiagnostic("update", "update api-manifest --check", "info", "E2D-BUILD-API-MANIFEST-CHECK-PASSED", "API manifest is synchronized.", Path: ToRepositoryPath(targetPath)));
            return RepositoryBuildExitCodes.Success;
        }

        var updatedShape = VerifyShape(targetPath, "update", "update api-manifest");
        if (updatedShape != RepositoryBuildExitCodes.Success)
        {
            return updatedShape;
        }

        diagnostics.Write(new BuildDiagnostic("update", "update api-manifest", "info", "E2D-BUILD-API-MANIFEST-UPDATED", "API manifest was updated.", Path: ToRepositoryPath(targetPath)));
        return RepositoryBuildExitCodes.Success;
    }

    public int VerifyShape(string manifestPath, string command = "update", string step = "update wiki --check")
    {
        var errors = new List<BuildDiagnostic>();
        if (!File.Exists(manifestPath))
        {
            errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-MANIFEST-MISSING", "API manifest was not found.", Path: ToRepositoryPath(manifestPath)));
        }
        else
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
                VerifyManifestShape(document.RootElement, errors, command, step, manifestPath);
            }
            catch (JsonException ex)
            {
                errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-MANIFEST-INVALID-JSON", $"API manifest is not valid JSON: {ex.Message}.", Path: ToRepositoryPath(manifestPath)));
            }
        }

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(command, step, "info", "E2D-BUILD-API-MANIFEST-SHAPE-PASSED", "API manifest shape verification passed.", Path: ToRepositoryPath(manifestPath)));
        return RepositoryBuildExitCodes.Success;
    }

    private async Task<int> GenerateManifestAsync(string outputPath, string wikiPath, CancellationToken cancellationToken)
    {
        var projectPath = Path.Combine(repositoryRoot, "src", "Electron2D", "Electron2D.csproj");
        var generatorProject = Path.Combine(repositoryRoot, "eng", "Electron2D.ApiManifestGenerator", "Electron2D.ApiManifestGenerator.csproj");
        var xmlPath = Path.Combine(repositoryRoot, ".temp", "api-manifest", "Electron2D.xml");
        var assemblyPath = Path.Combine(repositoryRoot, "src", "Electron2D", "bin", "Debug", "net10.0", "Electron2D.dll");
        var profilePath = ResolveRepositoryOrAbsolutePath(DefaultProfileRelativePath);

        foreach (var required in new[] { projectPath, generatorProject, profilePath })
        {
            if (!File.Exists(required))
            {
                diagnostics.Write(new BuildDiagnostic("update", "update api-manifest", "error", "E2D-BUILD-API-MANIFEST-SOURCE-MISSING", $"Required API manifest source file was not found: {ToRepositoryPath(required)}.", Path: ToRepositoryPath(required)));
                return RepositoryBuildExitCodes.Failed;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(xmlPath)!);
        var build = await processRunner.RunAsync(
            new ProcessRunRequest(
                "update api-manifest build",
                "dotnet",
                ["build", projectPath, "--no-restore", "-p:GenerateDocumentationFile=true", $"-p:DocumentationFile={xmlPath}"],
                repositoryRoot,
                TimeSpan.FromMinutes(5)),
            cancellationToken).ConfigureAwait(false);
        if (build.ExitCode != 0)
        {
            WriteProcessFailure("E2D-BUILD-API-MANIFEST-BUILD-FAILED", "Runtime project build failed while generating API manifest.", build);
            return RepositoryBuildExitCodes.Failed;
        }

        var generator = await processRunner.RunAsync(
            new ProcessRunRequest(
                "update api-manifest generate",
                "dotnet",
                ["run", "--project", generatorProject, "--", "--repo-root", repositoryRoot, "--assembly", assemblyPath, "--xml", xmlPath, "--profile", profilePath, "--output", outputPath],
                repositoryRoot,
                TimeSpan.FromMinutes(5)),
            cancellationToken).ConfigureAwait(false);
        if (generator.ExitCode != 0)
        {
            WriteProcessFailure("E2D-BUILD-API-MANIFEST-GENERATE-FAILED", "API manifest generator failed.", generator);
            return RepositoryBuildExitCodes.Failed;
        }

        return RepositoryBuildExitCodes.Success;
    }

    private int VerifyGeneratedManifestProfileGate(string manifestPath, string step)
    {
        var errors = new List<BuildDiagnostic>();
        var profilePath = ResolveRepositoryOrAbsolutePath(DefaultProfileRelativePath);
        var profile = ManualApiProfileReader.Read(repositoryRoot, profilePath, "update", step, errors);
        var manifestTypes = new List<string>();
        if (File.Exists(manifestPath))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
                if (TryGetArray(document.RootElement, "types", out var types))
                {
                    foreach (var type in types.EnumerateArray())
                    {
                        if (TryGetString(type, "fullName", out var fullName))
                        {
                            manifestTypes.Add(fullName);
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                errors.Add(new BuildDiagnostic("update", step, "error", "E2D-BUILD-API-MANIFEST-INVALID-JSON", $"API manifest is not valid JSON after generation: {ex.Message}.", Path: ToRepositoryPath(manifestPath)));
            }
        }

        foreach (var fullName in manifestTypes.Order(StringComparer.Ordinal))
        {
            if (!profile.Types.TryGetValue(ManualApiProfileReader.NormalizeApiName(fullName), out var entry) ||
                !string.Equals(entry.Decision, "approved", StringComparison.Ordinal))
            {
                diagnostics.Write(new BuildDiagnostic("update", step, "error", "E2D-BUILD-API-PROFILE-UNAPPROVED-EXPORT", $"Exported runtime public type is not approved by manual public API profile: {fullName}.", Path: DefaultProfileRelativePath));
                return RepositoryBuildExitCodes.Failed;
            }

            if (entry.EditorOnly)
            {
                diagnostics.Write(new BuildDiagnostic("update", step, "error", "E2D-BUILD-API-PROFILE-EDITOR-ONLY-EXPORT", $"Exported runtime public type is marked editorOnly by manual public API profile: {fullName}.", Path: DefaultProfileRelativePath));
                return RepositoryBuildExitCodes.Failed;
            }
        }

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        return errors.Count == 0 ? RepositoryBuildExitCodes.Success : RepositoryBuildExitCodes.Failed;
    }

    private void WriteProcessFailure(string code, string message, ProcessRunResult process)
    {
        foreach (var diagnostic in process.Diagnostics)
        {
            diagnostics.Write(diagnostic);
        }

        diagnostics.Write(new BuildDiagnostic("update", "update api-manifest", "error", code, message, ProcessExitCode: process.ExitCode, TimedOut: process.TimedOut));
    }

    private string ResolveCompatibilityPath(string wikiPath)
    {
        var resolved = ResolveRepositoryOrAbsolutePath(wikiPath);
        if (File.Exists(resolved) && resolved.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return resolved;
        }

        return Path.Combine(resolved, "API-Compatibility.md");
    }

    private string ResolveRepositoryOrAbsolutePath(string path)
    {
        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar)));
    }

    private string ToRepositoryPath(string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static void VerifyManifestShape(JsonElement root, List<BuildDiagnostic> errors, string command, string step, string manifestPath)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-MANIFEST-SCHEMA", "API manifest root must be a JSON object.", Path: manifestPath));
            return;
        }

        if (!TryGetArray(root, "types", out var types))
        {
            errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-MANIFEST-TYPES", "API manifest is missing types array.", Path: manifestPath));
            return;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in types.EnumerateArray())
        {
            if (!TryGetString(type, "id", out var id) ||
                !TryGetString(type, "fullName", out var fullName) ||
                !TryGetString(type, "name", out _) ||
                !TryGetString(type, "kind", out _))
            {
                errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-MANIFEST-TYPE-SCHEMA", "API manifest type entry must contain id, fullName, name and kind.", Path: manifestPath));
                continue;
            }

            if (!ids.Add(id))
            {
                errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-MANIFEST-DUPLICATE-ID", $"API manifest contains duplicate id: {id}.", Path: manifestPath));
            }

            if (!id.Contains(fullName, StringComparison.Ordinal))
            {
                errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-MANIFEST-TYPE-ID", $"API manifest type id does not include fullName: {fullName}.", Path: manifestPath));
            }

            if (type.TryGetProperty("members", out var members))
            {
                if (members.ValueKind != JsonValueKind.Array)
                {
                    errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-MANIFEST-MEMBER-SCHEMA", $"API manifest members must be an array for {fullName}.", Path: manifestPath));
                    continue;
                }

                foreach (var member in members.EnumerateArray())
                {
                    if (!TryGetString(member, "id", out var memberId) ||
                        !TryGetString(member, "name", out _) ||
                        !TryGetString(member, "kind", out _))
                    {
                        errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-MANIFEST-MEMBER-SCHEMA", $"API manifest member entry is incomplete for {fullName}.", Path: manifestPath));
                        continue;
                    }

                    if (!ids.Add(memberId))
                    {
                        errors.Add(new BuildDiagnostic(command, step, "error", "E2D-BUILD-API-MANIFEST-DUPLICATE-ID", $"API manifest contains duplicate id: {memberId}.", Path: manifestPath));
                    }
                }
            }
        }
    }

    private static ApiManifestArguments Parse(string[] args)
    {
        var check = false;
        string? output = null;
        string? wikiPath = null;
        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--check":
                    check = true;
                    break;
                case "--output" when i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]):
                    output = args[++i];
                    break;
                case "--wiki-path" when i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]):
                    wikiPath = args[++i];
                    break;
                default:
                    return new ApiManifestArguments(false, check, output, wikiPath, "Expected: update api-manifest [--check] [--output <path>].");
            }
        }

        return new ApiManifestArguments(true, check, output, wikiPath, string.Empty);
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    internal static bool TryGetArray(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        value = default;
        return false;
    }

    internal static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
        {
            value = property.GetString()!;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private sealed record ApiManifestArguments(bool Succeeded, bool Check, string? OutputPath, string? WikiPath, string ErrorMessage);
}

internal sealed class ApiWikiCommand(string repositoryRoot, JsonDiagnosticSink diagnostics, ApiManifestCommand apiManifest, ProcessRunner processRunner)
{
    private const string ManifestRelativePath = "data/api/electron2d-api-manifest.json";
    private const string DefaultWikiRelativePath = ".github/wiki";
    private const string GeneratedMarker = "<!-- Generated by eng/Electron2D.Build update wiki. Do not edit by hand. -->";

    private static readonly ApiWikiCategory[] Categories =
    [
        new(
            "API-Core.md",
            "Core",
            "ElectronObject lifetime, identity, names, callable values and low-level result types.",
            type => Named(type, "ElectronObject", "Object", "RefCounted", "Callable", "Error", "ConnectFlags", "Rid", "StringName")),
        new(
            "API-Scene-Tree.md",
            "Scene Tree",
            "Nodes, node paths, scene packing and scene traversal.",
            type => Named(type, "Node", "Node2D", "NodePath", "PackedScene", "ProcessMode", "SceneTree")),
        new(
            "API-Resources.md",
            "Resources",
            "Resource base types and stable resource identifiers.",
            type => Named(type, "Resource", "ResourceUid")),
        new(
            "API-Math-and-Data.md",
            "Math and Data",
            "2D math value types, color, random number generation, variants and collection values.",
            type => Named(type, "Mathf", "Vector2", "Vector2I", "Rect2", "Rect2I", "Transform2D", "Color", "RandomNumberGenerator", "Variant") ||
                ShortDisplayName(type).StartsWith("Collections.", StringComparison.Ordinal)),
        new(
            "API-Input.md",
            "Input",
            "Input state, input maps, keyboard, mouse, gamepad and touch event types.",
            type => Named(type, "Input", "InputMap", "InputEvent", "Key", "KeyLocation", "MouseButton", "MouseButtonMask", "JoyAxis", "JoyButton") ||
                ShortDisplayName(type).StartsWith("InputEvent", StringComparison.Ordinal)),
        new(
            "API-Display-and-Localization.md",
            "Display and Localization",
            "Display state, orientation requests, virtual keyboard state and translations.",
            type => Named(type, "DisplayServer", "Translation", "TranslationServer")),
        new(
            "API-Rendering.md",
            "Rendering",
            "2D drawing nodes, textures, tile maps, viewports, cameras, materials, shaders and rendering server state.",
            type => Named(type, "AtlasTexture", "ImageTexture", "Texture2D", "TileData", "TileMapLayer", "TileSet", "TileSetAtlasSource", "TileSetSource", "CanvasItem", "CanvasLayer", "Camera2D", "Viewport", "ViewportTexture", "Sprite2D", "RenderingServer", "Material", "Shader", "ShaderMaterial")),
        new(
            "API-Animation-and-Tweening.md",
            "Animation and Tweening",
            "Frame animation, animation resources, playback nodes and tween sequences.",
            type => Named(type, "AnimatedSprite2D", "Animation", "AnimationLibrary", "AnimationPlayer", "SpriteFrames", "Tween", "Tweener", "CallbackTweener", "IntervalTweener", "PropertyTweener")),
        new(
            "API-Audio.md",
            "Audio",
            "Audio resources, playback nodes and audio server state.",
            type => Named(type, "AudioServer", "AudioStream", "AudioStreamPlayer", "AudioStreamPlayer2D")),
        new(
            "API-Physics.md",
            "Physics",
            "2D physics bodies, areas, shapes, query parameters, collisions and physics server boundaries.",
            type => Named(type, "World2D", "Area2D", "CollisionObject2D", "CollisionShape2D", "Shape2D", "CapsuleShape2D", "CircleShape2D", "ConcavePolygonShape2D", "ConvexPolygonShape2D", "RectangleShape2D", "SegmentShape2D", "PhysicsBody2D", "PhysicsDirectSpaceState2D", "PhysicsMaterial", "PhysicsPointQueryParameters2D", "PhysicsRayQueryParameters2D", "PhysicsServer2D", "PhysicsShapeQueryParameters2D", "RayCast2D", "StaticBody2D", "RigidBody2D", "CharacterBody2D", "KinematicCollision2D")),
        new(
            "API-UI-and-Text.md",
            "UI and Text",
            "UI controls, labels, fonts and text alignment values.",
            type => Named(type, "BaseButton", "BoxContainer", "BoxContainerAlignmentMode", "Button", "CenterContainer", "CheckBox", "Container", "Control", "FocusMode", "GridContainer", "GrowDirection", "HBoxContainer", "ItemList", "Label", "LineEdit", "MarginContainer", "NinePatchRect", "Panel", "PopupMenu", "ProgressBar", "Range", "ScrollContainer", "ScrollHintMode", "ScrollMode", "SizeFlags", "Slider", "StyleBox", "StyleBoxFlat", "TabContainer", "TextureButton", "TextureRect", "Theme", "Tree", "TreeItem", "VBoxContainer", "Font", "HorizontalAlignment", "MouseFilter", "VerticalAlignment")),
        new(
            "API-Scripting-Metadata.md",
            "Scripting Metadata",
            "Attributes used by scripts, serialization metadata and editor-facing script annotations.",
            type => Named(type, "ExportAttribute", "SignalAttribute", "ToolAttribute"))
    ];

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var parse = Parse(args);
        if (!parse.Succeeded)
        {
            diagnostics.Write(new BuildDiagnostic("update", "update wiki", "error", "E2D-BUILD-CLI-INVALID-ARGUMENTS", parse.ErrorMessage));
            return RepositoryBuildExitCodes.Failed;
        }

        var manifestPath = Path.Combine(repositoryRoot, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var manifestShape = apiManifest.VerifyShape(manifestPath, "update", parse.Check ? "update wiki --check" : "update wiki");
        if (manifestShape != RepositoryBuildExitCodes.Success)
        {
            return manifestShape;
        }

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
        var render = await RenderExpectedPagesAsync(manifest.RootElement, cancellationToken).ConfigureAwait(false);
        if (render is null)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var wikiRoot = parse.OutputPath is null
            ? Path.Combine(repositoryRoot, ".temp", "api-wiki", parse.Check ? "check-wiki" : "generated-wiki")
            : ResolveRepositoryOrAbsolutePath(parse.OutputPath);

        if (parse.Check)
        {
            if (parse.OutputPath is null)
            {
                RecreateDirectory(wikiRoot);
                WriteWikiFiles(wikiRoot, render.Pages);
            }

            return VerifyWikiOutput(wikiRoot, render.Pages);
        }

        return WriteWikiOutput(wikiRoot, render.Pages);
    }

    private int VerifyWikiOutput(string wikiRoot, IReadOnlyDictionary<string, string> expectedPages)
    {
        var errors = new List<BuildDiagnostic>();
        if (!Directory.Exists(wikiRoot))
        {
            errors.Add(Error("E2D-BUILD-WIKI-ROOT-MISSING", "GitHub Wiki clone was not found.", wikiRoot));
        }

        var compatibilityPath = Path.Combine(wikiRoot, "API-Compatibility.md");
        if (!File.Exists(compatibilityPath))
        {
            errors.Add(Error("E2D-BUILD-WIKI-COMPATIBILITY-MISSING", "GitHub Wiki compatibility page is missing: API-Compatibility.md.", "API-Compatibility.md"));
        }
        else
        {
            VerifyCompatibilityPage(compatibilityPath, errors);
        }

        if (Directory.Exists(wikiRoot))
        {
            foreach (var pair in expectedPages.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                var page = pair.Key;
                var path = Path.Combine(wikiRoot, page.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path))
                {
                    errors.Add(Error("E2D-BUILD-WIKI-PAGE-MISSING", $"Missing generated Wiki page: {page}.", page));
                    continue;
                }

                var text = Normalize(File.ReadAllText(path, Encoding.UTF8));
                var expected = Normalize(pair.Value);
                if (!text.Contains(GeneratedMarker, StringComparison.Ordinal))
                {
                    errors.Add(Error("E2D-BUILD-WIKI-GENERATED-MARKER", $"Generated Wiki page is missing marker: {page}.", page));
                }

                if (!string.Equals(text, expected, StringComparison.Ordinal))
                {
                    errors.Add(Error("E2D-BUILD-WIKI-PAGE-STALE", $"Generated Wiki page is out of date: {page}.", page));
                }

                if (text.Contains(".md)", StringComparison.Ordinal))
                {
                    errors.Add(Error("E2D-BUILD-WIKI-LINK-SHAPE", $"Generated Wiki page contains .md links: {page}.", page));
                }
            }

            var expectedRelativePaths = new HashSet<string>(expectedPages.Keys, StringComparer.Ordinal);
            foreach (var file in Directory.EnumerateFiles(wikiRoot, "*.md", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file, Encoding.UTF8);
                if (!text.Contains(GeneratedMarker, StringComparison.Ordinal))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(wikiRoot, file).Replace('\\', '/');
                if (!expectedRelativePaths.Contains(relative))
                {
                    errors.Add(Error("E2D-BUILD-WIKI-STALE-PAGE", $"Stale generated Wiki page: {relative}.", relative));
                }
            }
        }

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic("update", "update wiki --check", "info", "E2D-BUILD-WIKI-CHECK-PASSED", $"GitHub Wiki API reference verification passed. Generated pages: {expectedPages.Count}.", Path: ToRepositoryPath(wikiRoot)));
        return RepositoryBuildExitCodes.Success;
    }

    private int WriteWikiOutput(string wikiRoot, IReadOnlyDictionary<string, string> expectedPages)
    {
        Directory.CreateDirectory(wikiRoot);
        WriteWikiFiles(wikiRoot, expectedPages);
        RemoveStaleGeneratedPages(wikiRoot, expectedPages.Keys);

        diagnostics.Write(new BuildDiagnostic("update", "update wiki", "info", "E2D-BUILD-WIKI-UPDATED", $"GitHub Wiki API reference was updated. Generated pages: {expectedPages.Count}.", Path: ToRepositoryPath(wikiRoot)));
        return RepositoryBuildExitCodes.Success;
    }

    private async Task<WikiRenderResult?> RenderExpectedPagesAsync(JsonElement manifest, CancellationToken cancellationToken)
    {
        var projectPath = Path.Combine(repositoryRoot, "src", "Electron2D", "Electron2D.csproj");
        if (!File.Exists(projectPath))
        {
            return new WikiRenderResult(RenderManifestPages(manifest));
        }

        var xmlPath = Path.Combine(repositoryRoot, ".temp", "api-wiki", "Electron2D.xml");
        var assemblyPath = Path.Combine(repositoryRoot, "src", "Electron2D", "bin", "Debug", "net10.0", "Electron2D.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(xmlPath)!);

        var build = await processRunner.RunAsync(
            new ProcessRunRequest(
                "update wiki build",
                "dotnet",
                ["build", projectPath, "--no-restore", "-p:GenerateDocumentationFile=true", $"-p:DocumentationFile={xmlPath}"],
                repositoryRoot,
                TimeSpan.FromMinutes(5)),
            cancellationToken).ConfigureAwait(false);
        if (build.ExitCode != 0)
        {
            foreach (var diagnostic in build.Diagnostics)
            {
                diagnostics.Write(diagnostic);
            }

            diagnostics.Write(new BuildDiagnostic("update", "update wiki", "error", "E2D-BUILD-WIKI-BUILD-FAILED", "Runtime project build failed while generating GitHub Wiki API reference.", ProcessExitCode: build.ExitCode, TimedOut: build.TimedOut));
            return null;
        }

        foreach (var requiredPath in new[] { assemblyPath, xmlPath })
        {
            if (!File.Exists(requiredPath))
            {
                diagnostics.Write(new BuildDiagnostic("update", "update wiki", "error", "E2D-BUILD-WIKI-SOURCE-MISSING", $"Required Wiki source file was not found: {ToRepositoryPath(requiredPath)}.", Path: ToRepositoryPath(requiredPath)));
                return null;
            }
        }

        try
        {
            return new WikiRenderResult(RenderReflectionPages(manifest, assemblyPath, xmlPath));
        }
        catch (Exception ex) when (ex is IOException or BadImageFormatException or FileLoadException or FileNotFoundException or InvalidOperationException or JsonException or System.Xml.XmlException)
        {
            diagnostics.Write(new BuildDiagnostic("update", "update wiki", "error", "E2D-BUILD-WIKI-RENDER-FAILED", $"GitHub Wiki API reference render failed: {ex.Message}."));
            return null;
        }
    }

    private static Dictionary<string, string> RenderManifestPages(JsonElement manifest)
    {
        var strictParityEvidenceStatus = ReadStrictParityEvidenceStatus(manifest);
        var types = manifest.GetProperty("types")
            .EnumerateArray()
            .Select(ApiWikiType.FromManifest)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        var pages = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Home.md"] = RenderManifestHome(types),
            ["_Sidebar.md"] = RenderSidebar(),
            ["_Footer.md"] = RenderFooter(types.Length),
            ["API-by-Category.md"] = RenderManifestApiByCategory(types),
            ["API-Reference.md"] = RenderManifestApiReference(types),
            ["API-Compatibility.md"] = RenderManifestCompatibility(types)
        };

        foreach (var category in Categories)
        {
            pages[category.PageFileName] = RenderManifestCategoryPage(category, types);
        }

        foreach (var type in types)
        {
            pages[TypePageName(type.Name)] = RenderManifestTypePage(type, strictParityEvidenceStatus);
        }

        return NormalizePages(pages);
    }

    private static Dictionary<string, string> RenderReflectionPages(JsonElement manifest, string assemblyPath, string xmlPath)
    {
        var strictParityEvidenceStatus = ReadStrictParityEvidenceStatus(manifest);
        var profiles = LoadProfiles(manifest);
        var assembly = Assembly.LoadFrom(assemblyPath);
        var xml = XDocument.Load(xmlPath);
        var docs = xml.Root?.Element("members")?.Elements("member")
            .Where(item => item.Attribute("name") is not null)
            .ToDictionary(item => item.Attribute("name")!.Value, item => item, StringComparer.Ordinal)
            ?? new Dictionary<string, XElement>(StringComparer.Ordinal);
        var publicTypes = assembly.GetExportedTypes()
            .Where(type => type.Assembly == assembly)
            .OrderBy(DisplayName, StringComparer.Ordinal)
            .ToArray();

        var pageFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in publicTypes)
        {
            if (!pageFileNames.Add(TypePageName(ShortDisplayName(type))))
            {
                throw new InvalidOperationException("Duplicate generated Wiki page name: " + TypePageName(ShortDisplayName(type)));
            }
        }

        var uncategorizedTypes = publicTypes
            .Where(type => !Categories.Any(category => category.Matches(type)))
            .Select(ShortDisplayName)
            .ToArray();
        if (uncategorizedTypes.Length > 0)
        {
            throw new InvalidOperationException("Public types are missing API categories: " + string.Join(", ", uncategorizedTypes));
        }

        var context = new WikiReflectionContext(publicTypes, docs, profiles, strictParityEvidenceStatus);
        var pages = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Home.md"] = RenderReflectionHome(context),
            ["_Sidebar.md"] = RenderSidebar(),
            ["_Footer.md"] = RenderFooter(publicTypes.Length),
            ["API-by-Category.md"] = RenderReflectionApiByCategory(context),
            ["API-Reference.md"] = RenderReflectionApiReference(context),
            ["API-Compatibility.md"] = RenderReflectionCompatibility(context)
        };

        foreach (var category in Categories)
        {
            pages[category.PageFileName] = RenderReflectionCategoryPage(category, context);
        }

        foreach (var type in publicTypes)
        {
            pages[TypePageName(ShortDisplayName(type))] = RenderReflectionTypePage(type, context);
        }

        return NormalizePages(pages);
    }

    private static string RenderManifestHome(IReadOnlyList<ApiWikiType> types)
    {
        var builder = NewGeneratedPage();
        builder.AppendLine("Electron2D is an Agent-native cross-platform 2D game engine for C# and .NET. The `0.1-preview` line focuses on a clean runtime API, deterministic project tooling and documentation that can be read by both developers and coding agents.");
        builder.AppendLine();
        builder.AppendLine("This Wiki is the public documentation hub for the preview API surface. It is generated from the API manifest, so the reference follows the code that ships in the engine package.");
        builder.AppendLine();
        builder.AppendLine("## Start here");
        builder.AppendLine();
        builder.AppendLine("- [API by Category](API-by-Category) - browse the runtime API by domain.");
        builder.AppendLine("- [Complete API Index](API-Reference) - alphabetical index of every public type.");
        builder.AppendLine("- [API Compatibility](API-Compatibility) - manual profile decisions and generated compatibility status.");
        builder.AppendLine();
        AppendCategoryTable(builder, types);
        return builder.ToString();
    }

    private static string RenderReflectionHome(WikiReflectionContext context)
    {
        var builder = NewGeneratedPage();
        builder.AppendLine("Electron2D is an Agent-native cross-platform 2D game engine for C# and .NET. The `0.1-preview` line focuses on a clean runtime API, deterministic project tooling and documentation that can be read by both developers and coding agents.");
        builder.AppendLine();
        builder.AppendLine("This Wiki is the public documentation hub for the preview API surface. It is generated from the compiled runtime assembly and XML documentation comments, so the reference follows the code that ships in the engine package.");
        builder.AppendLine();
        builder.AppendLine("## What is Electron2D?");
        builder.AppendLine();
        builder.AppendLine("Electron2D provides a compact 2D runtime model: objects, nodes, resources, scenes, 2D math, input events, rendering-facing nodes, animation, UI/text primitives and an initial 2D physics surface. The project is being built for desktop-first development with a release path toward packaged tools, editor workflows and agent-assisted game creation.");
        builder.AppendLine();
        builder.AppendLine("## Start here");
        builder.AppendLine();
        builder.AppendLine("- [API by Category](API-by-Category) - browse the runtime API by domain.");
        builder.AppendLine("- [Complete API Index](API-Reference) - alphabetical index of every public type.");
        builder.AppendLine("- [API Compatibility](API-Compatibility) - manual profile decisions and generated compatibility status.");
        builder.AppendLine();
        builder.AppendLine("## API documentation");
        builder.AppendLine();
        builder.AppendLine("| Area | Description | Types |");
        builder.AppendLine("| --- | --- | ---: |");
        foreach (var category in Categories)
        {
            var count = context.Types.Count(category.Matches);
            builder.AppendLine("| [" + EscapeTable(category.Title) + "](" + PageStem(category.PageFileName) + ") | " + EscapeTable(category.Description) + " | `" + count.ToString(System.Globalization.CultureInfo.InvariantCulture) + "` |");
        }

        builder.AppendLine();
        builder.AppendLine("## Preview status");
        builder.AppendLine();
        builder.AppendLine("| Item | Status |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Runtime API | `" + context.Types.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + "` public types generated from the current assembly |");
        builder.AppendLine("| Documentation source | XML comments and compiled public surface |");
        builder.AppendLine("| Navigation | Category pages, complete index and focused common API sidebar |");
        builder.AppendLine("| Release line | `0.1-preview` |");
        builder.AppendLine();
        builder.AppendLine("## Agent-native cross-platform 2D game engine workflow");
        builder.AppendLine();
        builder.AppendLine("The documentation is generated in a stable structure so humans and coding agents can link to the same pages, compare public types, and avoid guessing which preview APIs exist.");
        builder.AppendLine();
        builder.AppendLine("The compatibility page marks which APIs are supported, deferred, unsupported or still unapproved by the manual public API profile.");
        return builder.ToString();
    }

    private static string RenderSidebar()
    {
        var builder = NewGeneratedPage();
        builder.AppendLine();
        builder.AppendLine("# Electron2D");
        builder.AppendLine();
        builder.AppendLine("- [Home](Home)");
        builder.AppendLine("- [API by Category](API-by-Category)");
        builder.AppendLine("- [Complete API Index](API-Reference)");
        builder.AppendLine("- [API Compatibility](API-Compatibility)");
        builder.AppendLine();
        builder.AppendLine("## API Areas");
        builder.AppendLine();
        foreach (var category in Categories)
        {
            builder.AppendLine("- [" + category.Title + "](" + PageStem(category.PageFileName) + ")");
        }

        builder.AppendLine();
        builder.AppendLine("## Common API");
        builder.AppendLine();
        builder.AppendLine("- [ElectronObject](ElectronObject)");
        builder.AppendLine("- [Node](Node)");
        builder.AppendLine("- [Node2D](Node2D)");
        builder.AppendLine("- [SceneTree](SceneTree)");
        builder.AppendLine("- [Resource](Resource)");
        builder.AppendLine("- [Vector2](Vector2)");
        builder.AppendLine("- [Input](Input)");
        builder.AppendLine("- [Sprite2D](Sprite2D)");
        builder.AppendLine("- [Area2D](Area2D)");
        builder.AppendLine("- [RigidBody2D](RigidBody2D)");
        return builder.ToString();
    }

    private static string RenderFooter(int typeCount)
    {
        var builder = NewGeneratedPage();
        builder.AppendLine();
        builder.AppendLine("Electron2D `0.1-preview` API reference. Generated from `" + typeCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + "` public runtime types.");
        return builder.ToString();
    }

    private static string RenderManifestApiByCategory(IReadOnlyList<ApiWikiType> types)
    {
        var builder = NewPage("API by Category");
        builder.AppendLine();
        builder.AppendLine("Browse the generated runtime API by domain. Use the complete index when you already know a type name.");
        builder.AppendLine();
        AppendCategoryTable(builder, types);
        return builder.ToString();
    }

    private static string RenderReflectionApiByCategory(WikiReflectionContext context)
    {
        var builder = NewPage("API by Category");
        builder.AppendLine();
        builder.AppendLine("Browse the generated runtime API by domain. Use the complete index when you already know a type name.");
        builder.AppendLine();
        builder.AppendLine("| Category | Description | Types |");
        builder.AppendLine("| --- | --- | ---: |");
        foreach (var category in Categories)
        {
            builder.AppendLine("| [" + EscapeTable(category.Title) + "](" + PageStem(category.PageFileName) + ") | " +
                EscapeTable(category.Description) + " | `" +
                context.Types.Count(category.Matches).ToString(System.Globalization.CultureInfo.InvariantCulture) + "` |");
        }

        return builder.ToString();
    }

    private static string RenderManifestApiReference(IReadOnlyList<ApiWikiType> types)
    {
        var builder = NewPage("API Reference");
        builder.AppendLine();
        builder.AppendLine("Complete generated public type index for the Electron2D runtime assembly.");
        builder.AppendLine();
        builder.AppendLine("- [API by Category](API-by-Category)");
        builder.AppendLine("- [API Compatibility](API-Compatibility)");
        builder.AppendLine();
        builder.AppendLine("## Type Index");
        builder.AppendLine();
        foreach (var group in types.GroupBy(type => type.Namespace).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            builder.AppendLine("### `" + (group.Key.Length == 0 ? "<global>" : group.Key) + "`");
            builder.AppendLine();
            builder.AppendLine("| Type | Kind | Summary |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var type in group)
            {
                builder.AppendLine("| [" + EscapeTable(type.Name) + "](" + PageStem(TypePageName(type.Name)) + ") | " +
                    EscapeTable(type.Kind) + " | " + EscapeTable(type.Summary) + " |");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string RenderManifestCompatibility(IReadOnlyList<ApiWikiType> types)
    {
        var builder = NewPage("API Compatibility");
        AppendCompatibilityPageBody(builder, types.Select(type => new ApiWikiCompatibilityRow(
            type.FullName,
            StatusLabel(type.Profile.Status),
            ProfileDecision(type.Profile.Status),
            type.Profile.Notes)));
        return builder.ToString();
    }

    private static string RenderReflectionApiReference(WikiReflectionContext context)
    {
        var builder = NewPage("API Reference");
        builder.AppendLine();
        builder.AppendLine("Complete generated public type index for the Electron2D runtime assembly.");
        builder.AppendLine();
        builder.AppendLine("- [API by Category](API-by-Category)");
        builder.AppendLine("- [API Compatibility](API-Compatibility)");
        builder.AppendLine();
        builder.AppendLine("## Type Index");
        builder.AppendLine();
        foreach (var group in context.Types.GroupBy(type => type.Namespace ?? string.Empty).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            builder.AppendLine("### `" + (group.Key.Length == 0 ? "<global>" : group.Key) + "`");
            builder.AppendLine();
            builder.AppendLine("| Type | Kind | Summary |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var type in group)
            {
                builder.AppendLine("| [" + EscapeTable(ShortDisplayName(type)) + "](" + PageStem(TypePageName(ShortDisplayName(type))) + ") | " +
                    EscapeTable(TypeKind(type)) + " | " + EscapeTable(PlainSummary(context, TypeId(type))) + " |");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string RenderReflectionCompatibility(WikiReflectionContext context)
    {
        var builder = NewPage("API Compatibility");
        AppendCompatibilityPageBody(builder, context.Types.Select(type =>
        {
            var profile = context.Profiles[DisplayName(type)];
            return new ApiWikiCompatibilityRow(
                DisplayName(type),
                StatusLabel(profile.Status),
                ProfileDecision(profile.Status),
                profile.Notes);
        }));
        return builder.ToString();
    }

    private static void AppendCompatibilityPageBody(StringBuilder builder, IEnumerable<ApiWikiCompatibilityRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("Generated from `data/api/electron2d-public-api-profile.json`.");
        builder.AppendLine();
        builder.AppendLine("## Status Legend");
        builder.AppendLine();
        builder.AppendLine("| Status | Meaning |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Supported | Approved by the manual profile for the current runtime surface; full parity evidence is tracked by the owning class tasks. |");
        builder.AppendLine("| Deferred | Explicitly excluded from the current release and reserved for later decision. |");
        builder.AppendLine("| Unsupported | Explicitly excluded from the current release. |");
        builder.AppendLine("| Unapproved | Exported by the runtime assembly without a manual profile decision; this is a failing gate state. |");
        builder.AppendLine();
        builder.AppendLine("## Current Public Runtime Surface");
        builder.AppendLine();
        builder.AppendLine("| Type | Status | Decision | Rationale |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var row in rows.OrderBy(row => row.FullName, StringComparer.Ordinal))
        {
            builder.AppendLine("| `" + EscapeTable(row.FullName) + "` | " + EscapeTable(row.Status) + " | `" + EscapeTable(row.Decision) + "` | " + EscapeTable(row.Rationale) + " |");
        }
    }

    private static string RenderManifestCategoryPage(ApiWikiCategory category, IReadOnlyList<ApiWikiType> types)
    {
        var categoryTypes = types
            .Where(type => string.Equals(type.Category, category.Title, StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
        var builder = NewPage(category.Title);
        builder.AppendLine();
        builder.AppendLine(category.Description);
        builder.AppendLine();
        builder.AppendLine("This category currently contains `" + categoryTypes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "` public types.");
        builder.AppendLine();
        builder.AppendLine("| Type | Kind | Summary |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var type in categoryTypes)
        {
            builder.AppendLine("| [" + EscapeTable(type.Name) + "](" + PageStem(TypePageName(type.Name)) + ") | " +
                EscapeTable(type.Kind) + " | " + EscapeTable(type.Summary) + " |");
        }

        return builder.ToString();
    }

    private static string RenderReflectionCategoryPage(ApiWikiCategory category, WikiReflectionContext context)
    {
        var categoryTypes = context.Types
            .Where(category.Matches)
            .OrderBy(ShortDisplayName, StringComparer.Ordinal)
            .ToArray();
        var builder = NewPage(category.Title);
        builder.AppendLine();
        builder.AppendLine(category.Description);
        builder.AppendLine();
        builder.AppendLine("This category currently contains `" + categoryTypes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "` public types.");
        builder.AppendLine();
        builder.AppendLine("| Type | Kind | Summary |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var type in categoryTypes)
        {
            builder.AppendLine("| [" + EscapeTable(ShortDisplayName(type)) + "](" + PageStem(TypePageName(ShortDisplayName(type))) + ") | " +
                EscapeTable(TypeKind(type)) + " | " + EscapeTable(PlainSummary(context, TypeId(type))) + " |");
        }

        return builder.ToString();
    }

    private static string RenderManifestTypePage(ApiWikiType type, string strictParityEvidenceStatus)
    {
        var category = Categories.First(category => string.Equals(category.Title, type.Category, StringComparison.Ordinal));
        var members = OrderedMembers(type.Members).ToArray();
        var builder = NewPage(type.Name);
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Full name | `" + type.FullName + "` |");
        builder.AppendLine("| Namespace | `" + type.Namespace + "` |");
        builder.AppendLine("| Kind | `" + type.Kind + "` |");
        builder.AppendLine("| Category | [" + EscapeTable(category.Title) + "](" + PageStem(category.PageFileName) + ") |");
        builder.AppendLine();
        builder.AppendLine("## Overview");
        builder.AppendLine();
        builder.AppendLine(string.IsNullOrWhiteSpace(type.Summary) ? "No XML documentation text was provided." : type.Summary);
        builder.AppendLine();
        builder.AppendLine("## Syntax");
        builder.AppendLine();
        builder.AppendLine("```csharp");
        builder.AppendLine(TypeDeclaration(type));
        builder.AppendLine("```");
        AppendCompatibilityBlock(builder, type.Profile, strictParityEvidenceStatus);

        if (members.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Members");
            builder.AppendLine();
            builder.AppendLine("| Member | Kind | Summary |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var member in members)
            {
                builder.AppendLine("| [`" + EscapeTable(MemberDisplayName(type, member)) + "`](#" + Anchor(MemberDisplayName(type, member)) + ") | " +
                    EscapeTable(MemberKind(member.Kind)) + " | " + EscapeTable(member.Summary) + " |");
            }

            builder.AppendLine();
            builder.AppendLine("## Member Details");
            foreach (var member in members)
            {
                builder.AppendLine();
                builder.AppendLine("### " + MemberDisplayName(type, member));
                builder.AppendLine();
                builder.AppendLine("Kind: `" + MemberKind(member.Kind) + "`");
                builder.AppendLine();
                builder.AppendLine("```csharp");
                builder.AppendLine(member.Signature);
                builder.AppendLine("```");
                if (!string.IsNullOrWhiteSpace(member.Summary))
                {
                    builder.AppendLine();
                    builder.AppendLine("#### Summary");
                    builder.AppendLine();
                    builder.AppendLine(member.Summary);
                }
            }
        }

        return builder.ToString();
    }

    private static string RenderReflectionTypePage(Type type, WikiReflectionContext context)
    {
        var builder = NewPage(ShortDisplayName(type));
        var typeDoc = FindDoc(context, TypeId(type));
        var category = Categories.First(category => category.Matches(type));
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Full name | `" + DisplayName(type) + "` |");
        builder.AppendLine("| Namespace | `" + (type.Namespace ?? string.Empty) + "` |");
        builder.AppendLine("| Kind | `" + TypeKind(type) + "` |");
        builder.AppendLine("| Category | [" + EscapeTable(category.Title) + "](" + PageStem(category.PageFileName) + ") |");
        builder.AppendLine();
        builder.AppendLine("## Overview");
        builder.AppendLine();
        AppendDocSection(builder, context, typeDoc?.Element("summary"));
        builder.AppendLine();
        builder.AppendLine("## Syntax");
        builder.AppendLine();
        builder.AppendLine("```csharp");
        builder.AppendLine(TypeDeclaration(type));
        builder.AppendLine("```");
        AppendCompatibilityBlock(builder, context.Profiles[DisplayName(type)], context.StrictParityEvidenceStatus);
        AppendOptionalDocBlock(builder, context, "Remarks", typeDoc?.Element("remarks"));
        AppendOptionalDocBlock(builder, context, "Thread Safety", typeDoc?.Element("threadsafety"));
        AppendOptionalDocBlock(builder, context, "Since", typeDoc?.Element("since"));
        AppendSeeAlso(builder, typeDoc);

        var members = GetDocumentedMembers(type);
        if (members.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Members");
            builder.AppendLine();
            builder.AppendLine("| Member | Kind | Summary |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var member in members)
            {
                builder.AppendLine("| [`" + EscapeTable(member.DisplayName) + "`](#" + Anchor(member.DisplayName) + ") | " +
                    EscapeTable(member.Kind) + " | " + EscapeTable(PlainSummary(context, member.XmlId)) + " |");
            }

            builder.AppendLine();
            builder.AppendLine("## Member Details");
            foreach (var member in members)
            {
                AppendMemberDetails(builder, context, member);
            }
        }

        return builder.ToString();
    }

    private static void VerifyCompatibilityPage(string compatibilityPath, List<BuildDiagnostic> errors)
    {
        var text = Normalize(File.ReadAllText(compatibilityPath, Encoding.UTF8));
        var firstVisibleLine = text.Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0 && !(line.StartsWith("<!--", StringComparison.Ordinal) && line.EndsWith("-->", StringComparison.Ordinal)));
        if (string.Equals(firstVisibleLine, "# API Compatibility", StringComparison.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-WIKI-COMPATIBILITY-TITLE", "GitHub Wiki compatibility page must not repeat the GitHub page title as a top-level heading.", "API-Compatibility.md"));
        }

        foreach (var required in new[] { GeneratedMarker, "Generated from `data/api/electron2d-public-api-profile.json`.", "## Status Legend", "## Current Public Runtime Surface", "| Supported |", "| Deferred |", "| Unsupported |", "| Unapproved |" })
        {
            if (!text.Contains(required, StringComparison.Ordinal))
            {
                errors.Add(Error("E2D-BUILD-WIKI-COMPATIBILITY-SHAPE", $"GitHub Wiki compatibility page is missing required structure: {required}.", "API-Compatibility.md"));
            }
        }
    }

    private static void WriteWikiFiles(string wikiRoot, IReadOnlyDictionary<string, string> pages)
    {
        foreach (var pair in pages.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var path = Path.Combine(wikiRoot, pair.Key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, NormalizeNewlines(pair.Value), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    private static void RemoveStaleGeneratedPages(string wikiRoot, IEnumerable<string> expectedPages)
    {
        var expected = new HashSet<string>(expectedPages, StringComparer.Ordinal);
        foreach (var actualFile in Directory.EnumerateFiles(wikiRoot, "*.md", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(actualFile, Encoding.UTF8);
            if (!text.Contains(GeneratedMarker, StringComparison.Ordinal))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(wikiRoot, actualFile).Replace('\\', '/');
            if (!expected.Contains(relativePath))
            {
                File.Delete(actualFile);
            }
        }
    }

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
    }

    private static Dictionary<string, ApiWikiProfile> LoadProfiles(JsonElement manifest)
    {
        var profiles = new Dictionary<string, ApiWikiProfile>(StringComparer.Ordinal);
        foreach (var type in manifest.GetProperty("types").EnumerateArray())
        {
            var fullName = RequiredString(type, "fullName");
            var profile = type.GetProperty("profile");
            profiles[fullName] = ReadProfile(profile);
        }

        return profiles;
    }

    private static void AppendCategoryTable(StringBuilder builder, IReadOnlyList<ApiWikiType> types)
    {
        builder.AppendLine("## API documentation");
        builder.AppendLine();
        builder.AppendLine("| Area | Description | Types |");
        builder.AppendLine("| --- | --- | ---: |");
        foreach (var category in Categories)
        {
            var count = types.Count(type => string.Equals(type.Category, category.Title, StringComparison.Ordinal));
            builder.AppendLine("| [" + EscapeTable(category.Title) + "](" + PageStem(category.PageFileName) + ") | " +
                EscapeTable(category.Description) + " | `" + count.ToString(System.Globalization.CultureInfo.InvariantCulture) + "` |");
        }
    }

    private static void AppendCompatibilityBlock(StringBuilder builder, ApiWikiProfile profile, string strictParityEvidenceStatus)
    {
        builder.AppendLine();
        builder.AppendLine("## Godot 4.7 C# profile compatibility");
        builder.AppendLine();
        builder.AppendLine("```text");
        builder.AppendLine("Profile: " + profile.Name);
        builder.AppendLine("Status: " + StatusLabel(profile.Status) + " / " + ParityLabel(profile.Parity));
        builder.AppendLine("Parity evidence: " + strictParityEvidenceStatus);
        builder.AppendLine("Out of profile: " + (profile.OutOfProfile ? "yes" : "no"));
        builder.AppendLine("Godot reference: " + profile.GodotReference);
        builder.AppendLine("Editor only: " + (profile.EditorOnly ? "yes" : "no"));
        builder.AppendLine("```");
        if (!string.IsNullOrWhiteSpace(profile.Notes))
        {
            builder.AppendLine();
            builder.AppendLine(profile.Notes);
        }
    }

    private static IEnumerable<ApiWikiMember> OrderedMembers(IEnumerable<ApiWikiMember> members)
    {
        return members
            .OrderBy(member => MemberOrder(member.Kind))
            .ThenBy(member => member.Name, StringComparer.Ordinal)
            .ThenBy(member => member.Signature, StringComparer.Ordinal);
    }

    private static int MemberOrder(string kind)
    {
        return kind switch
        {
            "EnumValue" or "Constant" or "Field" => 0,
            "Property" => 1,
            "Event" => 2,
            "Constructor" => 3,
            "Method" or "Operator" => 4,
            _ => 5
        };
    }

    private static string MemberKind(string kind)
    {
        return string.Equals(kind, "EnumValue", StringComparison.Ordinal) ? "Enum value" : kind;
    }

    private static string MemberDisplayName(ApiWikiType type, ApiWikiMember member)
    {
        var parameterTypes = string.Join(", ", member.Parameters.Select(parameter => parameter.Type));
        return member.Kind switch
        {
            "Constructor" => type.Name + "(" + parameterTypes + ")",
            "Method" or "Operator" => member.Name + "(" + parameterTypes + ")",
            _ => member.Name
        };
    }

    private static string TypeDeclaration(ApiWikiType type)
    {
        var modifiers = string.Equals(type.FullName, "Electron2D.Mathf", StringComparison.Ordinal)
            ? "public static"
            : "public";
        var baseTypes = new List<string>();
        if (!string.IsNullOrWhiteSpace(type.BaseType) && !string.Equals(type.Kind, "enum", StringComparison.Ordinal) && !string.Equals(type.Kind, "struct", StringComparison.Ordinal))
        {
            baseTypes.Add(type.BaseType);
        }

        baseTypes.AddRange(type.Interfaces.OrderBy(name => name, StringComparer.Ordinal));
        return modifiers + " " + type.Kind + " " + type.FullName +
            (baseTypes.Count == 0 ? string.Empty : " : " + string.Join(", ", baseTypes));
    }

    private static void AppendMemberDetails(StringBuilder builder, WikiReflectionContext context, ApiWikiReflectionMember member)
    {
        var doc = FindDoc(context, member.XmlId);
        builder.AppendLine();
        builder.AppendLine("### " + member.DisplayName);
        builder.AppendLine();
        builder.AppendLine("Kind: `" + member.Kind + "`");
        builder.AppendLine();
        builder.AppendLine("```csharp");
        builder.AppendLine(member.Signature);
        builder.AppendLine("```");
        AppendOptionalDocBlock(builder, context, "Summary", doc?.Element("summary"));
        AppendOptionalDocBlock(builder, context, "Remarks", doc?.Element("remarks"));
        AppendParameters(builder, context, doc, "Parameters", "param", "name");
        AppendParameters(builder, context, doc, "Type Parameters", "typeparam", "name");
        AppendOptionalDocBlock(builder, context, "Returns", doc?.Element("returns"));
        AppendOptionalDocBlock(builder, context, "Value", doc?.Element("value"));
        AppendExceptions(builder, context, doc);
        AppendOptionalDocBlock(builder, context, "Thread Safety", doc?.Element("threadsafety"));
        AppendOptionalDocBlock(builder, context, "Since", doc?.Element("since"));
        AppendSeeAlso(builder, doc);
    }

    private static IReadOnlyList<ApiWikiReflectionMember> GetDocumentedMembers(Type type)
    {
        var members = new List<ApiWikiReflectionMember>();
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => !field.IsSpecialName && (!type.IsEnum || field.Name != "value__"))
            .OrderBy(field => field.Name, StringComparer.Ordinal))
        {
            var id = "F:" + XmlTypeName(type) + "." + field.Name;
            var kind = field.IsLiteral
                ? type.IsEnum ? "Enum value" : "Constant"
                : "Field";
            members.Add(new ApiWikiReflectionMember(kind, ProjectMemberName(field.Name), FieldSignature(field), id));
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            var id = "P:" + XmlTypeName(type) + "." + property.Name;
            members.Add(new ApiWikiReflectionMember("Property", ProjectMemberName(property.Name), PropertySignature(property), id));
        }

        foreach (var @event in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(@event => @event.Name, StringComparer.Ordinal))
        {
            var id = "E:" + XmlTypeName(type) + "." + @event.Name;
            members.Add(new ApiWikiReflectionMember("Event", ProjectMemberName(@event.Name), EventSignature(@event), id));
        }

        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .OrderBy(constructor => constructor.ToString(), StringComparer.Ordinal))
        {
            var id = MethodId(type, constructor, "#ctor");
            members.Add(new ApiWikiReflectionMember("Constructor", ConstructorDisplayName(type, constructor), ConstructorSignature(type, constructor), id));
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal))
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ThenBy(method => method.ToString(), StringComparer.Ordinal))
        {
            var id = MethodId(type, method, method.Name);
            var kind = method.Name.StartsWith("op_", StringComparison.Ordinal) ? "Operator" : "Method";
            members.Add(new ApiWikiReflectionMember(kind, MethodDisplayName(method), MethodSignature(method), id));
        }

        return members;
    }

    private static void AppendOptionalDocBlock(StringBuilder builder, WikiReflectionContext context, string heading, XElement? element)
    {
        if (element is null || string.IsNullOrWhiteSpace(element.Value))
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("#### " + heading);
        builder.AppendLine();
        AppendDocSection(builder, context, element);
    }

    private static void AppendDocSection(StringBuilder builder, WikiReflectionContext context, XElement? element)
    {
        if (element is null || string.IsNullOrWhiteSpace(element.Value))
        {
            builder.AppendLine("No XML documentation text was provided.");
            return;
        }

        var text = Markdown(context, element).Trim();
        builder.AppendLine(string.IsNullOrWhiteSpace(text) ? "No XML documentation text was provided." : text);
    }

    private static void AppendParameters(StringBuilder builder, WikiReflectionContext context, XElement? doc, string heading, string elementName, string attributeName)
    {
        var parameters = doc?.Elements(elementName)
            .Where(item => !string.IsNullOrWhiteSpace(item.Attribute(attributeName)?.Value) && !string.IsNullOrWhiteSpace(item.Value))
            .ToArray() ?? [];
        if (parameters.Length == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("#### " + heading);
        builder.AppendLine();
        foreach (var parameter in parameters)
        {
            builder.AppendLine("- `" + parameter.Attribute(attributeName)!.Value + "`: " + Markdown(context, parameter).Trim());
        }
    }

    private static void AppendExceptions(StringBuilder builder, WikiReflectionContext context, XElement? doc)
    {
        var exceptions = doc?.Elements("exception")
            .Where(item => !string.IsNullOrWhiteSpace(item.Attribute("cref")?.Value) && !string.IsNullOrWhiteSpace(item.Value))
            .ToArray() ?? [];
        if (exceptions.Length == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("#### Exceptions");
        builder.AppendLine();
        foreach (var exception in exceptions)
        {
            builder.AppendLine("- `" + CrefDisplay(exception.Attribute("cref")!.Value) + "`: " + Markdown(context, exception).Trim());
        }
    }

    private static void AppendSeeAlso(StringBuilder builder, XElement? doc)
    {
        var seeAlso = doc?.Elements("seealso")
            .Select(item => item.Attribute("cref")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => CrefDisplay(value!))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray() ?? [];
        if (seeAlso.Length == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("#### See Also");
        builder.AppendLine();
        foreach (var item in seeAlso)
        {
            builder.AppendLine("- `" + item + "`");
        }
    }

    private static StringBuilder NewPage(string title)
    {
        var builder = NewGeneratedPage();
        builder.AppendLine();
        builder.AppendLine("[Home](Home) | [API by Category](API-by-Category) | [Complete API Index](API-Reference) | [API Compatibility](API-Compatibility)");
        builder.AppendLine();
        builder.AppendLine("<!-- Page title: " + title + " -->");
        return builder;
    }

    private static StringBuilder NewGeneratedPage()
    {
        var builder = new StringBuilder();
        builder.AppendLine(GeneratedMarker);
        return builder;
    }

    private static XElement? FindDoc(WikiReflectionContext context, string id)
    {
        if (context.Docs.TryGetValue(id, out var exact))
        {
            return exact;
        }

        var parameterIndex = id.IndexOf('(', StringComparison.Ordinal);
        if (parameterIndex > 0)
        {
            var prefix = id[..parameterIndex];
            return context.Docs.FirstOrDefault(item => item.Key.StartsWith(prefix + "(", StringComparison.Ordinal)).Value;
        }

        return context.Docs.FirstOrDefault(item => item.Key.StartsWith(id + "(", StringComparison.Ordinal)).Value;
    }

    private static string PlainSummary(WikiReflectionContext context, string id)
    {
        var summary = FindDoc(context, id)?.Element("summary");
        if (summary is null || string.IsNullOrWhiteSpace(summary.Value))
        {
            return string.Empty;
        }

        return NormalizeWhitespace(Markdown(context, summary));
    }

    private static string Markdown(WikiReflectionContext context, XElement element)
    {
        var builder = new StringBuilder();
        foreach (var node in element.Nodes())
        {
            AppendMarkdownNode(context, builder, node);
        }

        return NormalizeParagraphSpacing(builder.ToString());
    }

    private static void AppendMarkdownNode(WikiReflectionContext context, StringBuilder builder, XNode node)
    {
        if (node is XText text)
        {
            builder.Append(text.Value);
            return;
        }

        if (node is not XElement element)
        {
            return;
        }

        switch (element.Name.LocalName)
        {
            case "para":
                builder.AppendLine();
                builder.AppendLine();
                foreach (var child in element.Nodes())
                {
                    AppendMarkdownNode(context, builder, child);
                }

                builder.AppendLine();
                break;
            case "see":
            case "seealso":
                var cref = element.Attribute("cref")?.Value;
                builder.Append(cref is null ? element.Value : "`" + CrefDisplay(cref) + "`");
                break;
            case "paramref":
            case "typeparamref":
                var name = element.Attribute("name")?.Value;
                builder.Append(string.IsNullOrWhiteSpace(name) ? element.Value : "`" + name + "`");
                break;
            case "c":
                builder.Append("`" + element.Value.Trim() + "`");
                break;
            default:
                foreach (var child in element.Nodes())
                {
                    AppendMarkdownNode(context, builder, child);
                }

                break;
        }
    }

    private static string NormalizeParagraphSpacing(string value)
    {
        var lines = Normalize(value).Split('\n');
        var result = new List<string>();
        var previousBlank = false;
        foreach (var rawLine in lines)
        {
            var line = NormalizeWhitespace(rawLine);
            var blank = string.IsNullOrWhiteSpace(line);
            if (blank && previousBlank)
            {
                continue;
            }

            result.Add(line);
            previousBlank = blank;
        }

        return string.Join(Environment.NewLine, result).Trim();
    }

    private static string NormalizeWhitespace(string value)
    {
        var builder = new StringBuilder();
        var inWhitespace = false;
        foreach (var c in value.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                if (!inWhitespace)
                {
                    builder.Append(' ');
                }

                inWhitespace = true;
            }
            else
            {
                builder.Append(c);
                inWhitespace = false;
            }
        }

        return builder.ToString();
    }

    private static string TypeDeclaration(Type type)
    {
        if (typeof(MulticastDelegate).IsAssignableFrom(type.BaseType))
        {
            var invoke = type.GetMethod("Invoke")!;
            return "public delegate " + TypeDisplayName(invoke.ReturnType) + " " + type.Name + "(" + Parameters(invoke.GetParameters()) + ");";
        }

        var modifiers = new List<string> { "public" };
        if (type.IsAbstract && type.IsSealed)
        {
            modifiers.Add("static");
        }
        else
        {
            if (type.IsAbstract && !type.IsInterface)
            {
                modifiers.Add("abstract");
            }

            if (type.IsSealed && !type.IsValueType && !type.IsEnum)
            {
                modifiers.Add("sealed");
            }
        }

        var kind = type.IsInterface ? "interface" : type.IsEnum ? "enum" : type.IsValueType ? "struct" : "class";
        var baseTypes = new List<string>();
        if (type.BaseType is not null && type.BaseType != typeof(object) && !type.IsEnum && !type.IsValueType)
        {
            baseTypes.Add(TypeDisplayName(type.BaseType));
        }

        baseTypes.AddRange(type.GetInterfaces().Where(iface => iface.IsPublic).Select(TypeDisplayName).OrderBy(name => name, StringComparer.Ordinal));
        return string.Join(" ", modifiers) + " " + kind + " " + TypeDisplayName(type) +
            (baseTypes.Count == 0 ? string.Empty : " : " + string.Join(", ", baseTypes));
    }

    private static string FieldSignature(FieldInfo field)
    {
        var modifiers = field.IsLiteral ? "public const " : field.IsStatic ? "public static " : "public ";
        return modifiers + TypeDisplayName(field.FieldType) + " " + ProjectMemberName(field.Name);
    }

    private static string PropertySignature(PropertyInfo property)
    {
        var accessors = new List<string>();
        if (property.GetMethod is not null && property.GetMethod.IsPublic)
        {
            accessors.Add("get;");
        }

        if (property.SetMethod is not null && property.SetMethod.IsPublic)
        {
            accessors.Add("set;");
        }

        var indexParameters = property.GetIndexParameters();
        var name = indexParameters.Length == 0
            ? ProjectMemberName(property.Name)
            : "this[" + Parameters(indexParameters) + "]";
        var modifiers = property.GetMethod?.IsStatic == true || property.SetMethod?.IsStatic == true ? "public static " : "public ";
        return modifiers + TypeDisplayName(property.PropertyType) + " " + name + " { " + string.Join(" ", accessors) + " }";
    }

    private static string EventSignature(EventInfo @event)
    {
        return "public event " + TypeDisplayName(@event.EventHandlerType!) + " " + ProjectMemberName(@event.Name);
    }

    private static string ConstructorSignature(Type type, ConstructorInfo constructor)
    {
        return "public " + TypeDisplayName(type) + "(" + Parameters(constructor.GetParameters()) + ")";
    }

    private static string MethodSignature(MethodInfo method)
    {
        var modifiers = method.IsStatic ? "public static " : "public ";
        return modifiers + TypeDisplayName(method.ReturnType) + " " + ProjectMemberName(method.Name) + "(" + Parameters(method.GetParameters()) + ")";
    }

    private static string ConstructorDisplayName(Type type, ConstructorInfo constructor)
    {
        return TypeDisplayName(type) + "(" + Parameters(constructor.GetParameters(), includeTypesOnly: true) + ")";
    }

    private static string MethodDisplayName(MethodInfo method)
    {
        return ProjectMemberName(method.Name) + "(" + Parameters(method.GetParameters(), includeTypesOnly: true) + ")";
    }

    private static string Parameters(ParameterInfo[] parameters, bool includeTypesOnly = false)
    {
        return string.Join(", ", parameters.Select(parameter =>
            includeTypesOnly
                ? TypeDisplayName(parameter.ParameterType)
                : TypeDisplayName(parameter.ParameterType) + " " + EscapeCSharpIdentifier(parameter.Name ?? "arg")));
    }

    private static string EscapeCSharpIdentifier(string name)
    {
        return IsCSharpReservedKeyword(name) ? "@" + name : name;
    }

    private static bool IsCSharpReservedKeyword(string name)
    {
        return name is
            "abstract" or
            "as" or
            "base" or
            "bool" or
            "break" or
            "byte" or
            "case" or
            "catch" or
            "char" or
            "checked" or
            "class" or
            "const" or
            "continue" or
            "decimal" or
            "default" or
            "delegate" or
            "do" or
            "double" or
            "else" or
            "enum" or
            "event" or
            "explicit" or
            "extern" or
            "false" or
            "finally" or
            "fixed" or
            "float" or
            "for" or
            "foreach" or
            "goto" or
            "if" or
            "implicit" or
            "in" or
            "int" or
            "interface" or
            "internal" or
            "is" or
            "lock" or
            "long" or
            "namespace" or
            "new" or
            "null" or
            "object" or
            "operator" or
            "out" or
            "override" or
            "params" or
            "private" or
            "protected" or
            "public" or
            "readonly" or
            "ref" or
            "return" or
            "sbyte" or
            "sealed" or
            "short" or
            "sizeof" or
            "stackalloc" or
            "static" or
            "string" or
            "struct" or
            "switch" or
            "this" or
            "throw" or
            "true" or
            "try" or
            "typeof" or
            "uint" or
            "ulong" or
            "unchecked" or
            "unsafe" or
            "ushort" or
            "using" or
            "virtual" or
            "void" or
            "volatile" or
            "while";
    }

    private static string TypeDisplayName(Type type)
    {
        if (type.IsByRef)
        {
            return TypeDisplayName(type.GetElementType()!) + "&";
        }

        if (type.IsArray)
        {
            return TypeDisplayName(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType is not null)
        {
            return TypeDisplayName(nullableType) + "?";
        }

        if (!type.IsGenericType)
        {
            return DisplayName(type);
        }

        var definitionName = DisplayName(type.GetGenericTypeDefinition());
        var tickIndex = definitionName.IndexOf('`', StringComparison.Ordinal);
        if (tickIndex >= 0)
        {
            definitionName = definitionName[..tickIndex];
        }

        return definitionName + "<" + string.Join(", ", type.GetGenericArguments().Select(TypeDisplayName)) + ">";
    }

    private static string TypeKind(Type type)
    {
        if (typeof(MulticastDelegate).IsAssignableFrom(type.BaseType))
        {
            return "delegate";
        }

        return type.IsEnum ? "enum" : type.IsValueType ? "struct" : type.IsInterface ? "interface" : "class";
    }

    private static string DisplayName(Type type)
    {
        return ProjectTypeDisplayName(RawDisplayName(type), type.IsEnum);
    }

    private static string RawDisplayName(Type type)
    {
        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    private static string ShortDisplayName(Type type)
    {
        var displayName = DisplayName(type);
        const string rootNamespace = "Electron2D.";
        return displayName.StartsWith(rootNamespace, StringComparison.Ordinal)
            ? displayName[rootNamespace.Length..]
            : displayName;
    }

    private static string TypeId(Type type)
    {
        return "T:" + XmlTypeName(type);
    }

    private static string XmlTypeName(Type type)
    {
        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    private static string ProjectTypeDisplayName(string displayName, bool isEnum)
    {
        const string rootNamespace = "Electron2D.";
        if (!isEnum || !displayName.StartsWith(rootNamespace, StringComparison.Ordinal))
        {
            return displayName;
        }

        const string suffix = "Enum";
        var lastDot = displayName.LastIndexOf('.');
        var nameStart = lastDot < 0 ? 0 : lastDot + 1;
        return displayName.EndsWith(suffix, StringComparison.Ordinal) &&
            displayName.Length > nameStart + suffix.Length
            ? displayName[..^suffix.Length]
            : displayName;
    }

    private static string ProjectMemberName(string name)
    {
        return name switch
        {
            "_GetMinimumSize" => "ComputeMinimumSize",
            "_GetTooltip" => "GetTooltipText",
            "_MakeCustomTooltip" => "MakeCustomTooltip",
            _ when name.Length > 1 && name[0] == '_' && char.IsUpper(name[1]) => name[1..],
            _ => name
        };
    }

    private static string MethodId(Type declaringType, MethodBase method, string methodName)
    {
        var name = methodName;
        if (method.IsGenericMethod)
        {
            name += "``" + method.GetGenericArguments().Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var parameters = method.GetParameters();
        var id = "M:" + XmlTypeName(declaringType) + "." + name;
        if (parameters.Length == 0)
        {
            return id;
        }

        return id + "(" + string.Join(",", parameters.Select(parameter => XmlParameterTypeName(parameter.ParameterType))) + ")";
    }

    private static string XmlParameterTypeName(Type type)
    {
        if (type.IsByRef)
        {
            return XmlParameterTypeName(type.GetElementType()!) + "@";
        }

        if (type.IsArray)
        {
            return XmlParameterTypeName(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericParameter)
        {
            return "`" + type.GenericParameterPosition.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (type.IsGenericType)
        {
            var definitionName = (type.GetGenericTypeDefinition().FullName ?? type.Name).Replace('+', '.');
            var tickIndex = definitionName.IndexOf('`', StringComparison.Ordinal);
            if (tickIndex >= 0)
            {
                definitionName = definitionName[..tickIndex];
            }

            return definitionName + "{" + string.Join(",", type.GetGenericArguments().Select(XmlParameterTypeName)) + "}";
        }

        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    private static string CrefDisplay(string cref)
    {
        var value = cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;
        return value.Replace('+', '.');
    }

    private static bool Named(Type type, params string[] names)
    {
        var shortName = ShortDisplayName(type);
        return names.Any(name =>
            string.Equals(shortName, name, StringComparison.Ordinal) ||
            shortName.StartsWith(name + ".", StringComparison.Ordinal));
    }

    private static string StatusLabel(string status)
    {
        return status switch
        {
            "supported" => "Supported",
            "partial" => "Partial",
            "experimental" => "Experimental",
            "planned" => "Planned",
            "deferred" => "Deferred",
            "unsupported" => "Unsupported",
            "unapproved" => "Unapproved",
            _ => status
        };
    }

    private static string ProfileDecision(string status)
    {
        return status switch
        {
            "supported" => "approved",
            "deferred" => "deferred",
            "unsupported" => "unsupported",
            _ => "unapproved"
        };
    }

    private static string ParityLabel(string parity)
    {
        return parity switch
        {
            "profile_approved" => "Profile approved",
            "not_verified" => "Not verified",
            _ => parity
        };
    }

    private static string TypePageName(string value)
    {
        return Slug(value) + ".md";
    }

    private static string PageStem(string fileName)
    {
        return fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^".md".Length]
            : fileName;
    }

    private static string Anchor(string value)
    {
        return Slug(value).ToLowerInvariant();
    }

    private static string Slug(string value)
    {
        var builder = new StringBuilder();
        foreach (var c in value)
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '-');
        }

        return builder.ToString().Trim('-');
    }

    private static string EscapeTable(string value)
    {
        return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static Dictionary<string, string> NormalizePages(Dictionary<string, string> pages)
    {
        return pages.ToDictionary(pair => pair.Key, pair => NormalizeNewlines(pair.Value), StringComparer.Ordinal);
    }

    private static string NormalizeNewlines(string text)
    {
        return Normalize(text).TrimEnd() + "\n";
    }

    private static ApiWikiProfile ReadProfile(JsonElement profile)
    {
        return new ApiWikiProfile(
            RequiredString(profile, "name"),
            RequiredString(profile, "status"),
            RequiredString(profile, "parity"),
            profile.GetProperty("outOfProfile").GetBoolean(),
            RequiredString(profile, "godotReference"),
            OptionalBool(profile, "editorOnly"),
            RequiredString(profile, "notes"));
    }

    private static string ReadStrictParityEvidenceStatus(JsonElement manifest)
    {
        return manifest.TryGetProperty("strictParityEvidence", out var evidence) && evidence.ValueKind == JsonValueKind.Object
            ? RequiredString(evidence, "status")
            : string.Empty;
    }

    private static bool OptionalBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.True;
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private sealed record WikiRenderResult(IReadOnlyDictionary<string, string> Pages);

    private sealed record ApiWikiCategory(string PageFileName, string Title, string Description, Func<Type, bool> Matches);

    private sealed record WikiReflectionContext(
        IReadOnlyList<Type> Types,
        IReadOnlyDictionary<string, XElement> Docs,
        IReadOnlyDictionary<string, ApiWikiProfile> Profiles,
        string StrictParityEvidenceStatus);

    private sealed record ApiWikiReflectionMember(string Kind, string DisplayName, string Signature, string XmlId);

    private sealed record ApiWikiProfile(string Name, string Status, string Parity, bool OutOfProfile, string GodotReference, bool EditorOnly, string Notes);

    private sealed record ApiWikiCompatibilityRow(string FullName, string Status, string Decision, string Rationale);

    private sealed record ApiWikiParameter(string Name, string Type);

    private sealed record ApiWikiMember(string Name, string Kind, string Signature, string Summary, IReadOnlyList<ApiWikiParameter> Parameters)
    {
        public static ApiWikiMember FromManifest(JsonElement member)
        {
            var parameters = member.TryGetProperty("parameters", out var parameterArray) && parameterArray.ValueKind == JsonValueKind.Array
                ? parameterArray.EnumerateArray()
                    .Select(parameter => new ApiWikiParameter(RequiredString(parameter, "name"), RequiredString(parameter, "type")))
                    .ToArray()
                : [];
            return new ApiWikiMember(
                RequiredString(member, "name"),
                RequiredString(member, "kind"),
                RequiredString(member, "signature"),
                RequiredString(member, "summary"),
                parameters);
        }
    }

    private sealed record ApiWikiType(
        string FullName,
        string Namespace,
        string Name,
        string Kind,
        string BaseType,
        IReadOnlyList<string> Interfaces,
        string Summary,
        string Category,
        ApiWikiProfile Profile,
        IReadOnlyList<ApiWikiMember> Members)
    {
        public static ApiWikiType FromManifest(JsonElement type)
        {
            var interfaces = type.TryGetProperty("interfaces", out var interfacesElement) && interfacesElement.ValueKind == JsonValueKind.Array
                ? interfacesElement.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(value => value.Length > 0)
                    .ToArray()
                : [];
            var members = type.TryGetProperty("members", out var membersElement) && membersElement.ValueKind == JsonValueKind.Array
                ? membersElement.EnumerateArray().Select(ApiWikiMember.FromManifest).ToArray()
                : [];

            return new ApiWikiType(
                RequiredString(type, "fullName"),
                RequiredString(type, "namespace"),
                RequiredString(type, "name"),
                RequiredString(type, "kind"),
                RequiredString(type, "baseType"),
                interfaces,
                RequiredString(type, "summary"),
                RequiredString(type, "category"),
                ReadProfile(type.GetProperty("profile")),
                members);
        }
    }

    private string ResolveRepositoryOrAbsolutePath(string path)
    {
        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar)));
    }

    private string ToRepositoryPath(string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private static BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("update", "update wiki --check", "error", code, message, Path: path);
    }

    private static WikiArguments Parse(string[] args)
    {
        var check = false;
        string? output = null;
        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--check":
                    check = true;
                    break;
                case "--output" when i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]):
                    output = args[++i];
                    break;
                default:
                    return new WikiArguments(false, check, output, "Expected: update wiki [--check] [--output <path>].");
            }
        }

        return new WikiArguments(true, check, output, string.Empty);
    }

    private sealed record WikiArguments(bool Succeeded, bool Check, string? OutputPath, string ErrorMessage);
}
