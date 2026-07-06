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
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal readonly record struct ProjectTemplateCreateOptions(
    string TemplateRoot,
    string ProjectName,
    string ProjectsRoot,
    string RendererProfile,
    bool InitializeGit);

internal sealed class ProjectTemplateCreateResult
{
    public ProjectTemplateCreateResult(
        string projectName,
        string projectPath,
        string projectSettingsPath,
        string mainScenePath,
        string rendererProfile,
        bool gitInitialized,
        string taskBoardPath,
        string agentInstructionsPath,
        int starterSkillCount,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        ProjectName = projectName;
        ProjectPath = projectPath;
        ProjectSettingsPath = projectSettingsPath;
        MainScenePath = mainScenePath;
        RendererProfile = rendererProfile;
        GitInitialized = gitInitialized;
        TaskBoardPath = taskBoardPath;
        AgentInstructionsPath = agentInstructionsPath;
        StarterSkillCount = starterSkillCount;
        Diagnostics = diagnostics.ToArray();
    }

    public string ProjectName { get; }

    public string ProjectPath { get; }

    public string ProjectSettingsPath { get; }

    public string MainScenePath { get; }

    public string RendererProfile { get; }

    public bool GitInitialized { get; }

    public string TaskBoardPath { get; }

    public string AgentInstructionsPath { get; }

    public int StarterSkillCount { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }
}

internal static class ProjectTemplateCreator
{
    private const string TemplateProjectName = "Electron2D.Empty";
    private const string EngineVersion = "0.1-preview";
    private const string DotNetSdkVersion = "10.0.101";

    private static readonly string[] StarterSkillNames =
    [
        "electron2d-scene",
        "electron2d-gameplay-code",
        "electron2d-resource-import",
        "electron2d-run-test",
        "electron2d-export"
    ];

    public static ProjectTemplateCreateResult Create(ProjectTemplateCreateOptions options)
    {
        var projectName = ValidateProjectName(options.ProjectName);
        var templateRoot = Path.GetFullPath(RequirePath(options.TemplateRoot, "Template root"));
        var projectsRoot = Path.GetFullPath(RequirePath(options.ProjectsRoot, "Projects root"));
        var rendererProfile = ValidateRendererProfile(options.RendererProfile);
        var projectPath = Path.Combine(projectsRoot, projectName);

        ValidateTemplateRoot(templateRoot);
        if (Directory.Exists(projectPath) && Directory.EnumerateFileSystemEntries(projectPath).Any())
        {
            throw new InvalidOperationException($"Project directory is not empty: {projectPath}");
        }

        Directory.CreateDirectory(projectPath);
        CopyTemplate(templateRoot, projectPath);
        RewriteProjectFiles(projectPath, projectName, rendererProfile);
        var surface = EnsureAgentReadySurface(projectPath, projectName, rendererProfile, DateTimeOffset.UtcNow);
        var diagnostics = new List<StructuredDiagnostic>();
        var gitInitialized = options.InitializeGit && TryInitializeGit(projectPath, diagnostics);

        return new ProjectTemplateCreateResult(
            projectName,
            projectPath,
            Path.Combine(projectPath, projectName + ".e2d"),
            ResolveProjectPath(projectPath, "scenes/main.scene.json"),
            rendererProfile,
            gitInitialized,
            surface.TaskBoardPath,
            surface.AgentInstructionsPath,
            surface.StarterSkillCount,
            diagnostics);
    }

    private static (string TaskBoardPath, string AgentInstructionsPath, int StarterSkillCount) EnsureAgentReadySurface(
        string projectPath,
        string projectName,
        string rendererProfile,
        DateTimeOffset createdAtUtc)
    {
        WriteFile(Path.Combine(projectPath, ".gitignore"), CreateGitIgnore());
        var agentsPath = Path.Combine(projectPath, "AGENTS.md");
        WriteFile(agentsPath, CreateAgentInstructions(projectName, rendererProfile));
        var starterSkillCount = WriteStarterSkills(projectPath);
        var taskBoardPath = WriteInitialProjectTasks(projectPath, createdAtUtc);
        return (taskBoardPath, agentsPath, starterSkillCount);
    }

    private static string WriteInitialProjectTasks(string projectPath, DateTimeOffset createdAtUtc)
    {
        var tasksRoot = Path.Combine(projectPath, ".electron2d", "tasks");
        Directory.CreateDirectory(tasksRoot);

        var task = new ProjectTask
        {
            TaskId = "welcome",
            Title = "Open the project and run the first scene",
            Description = "Verify that the newly created Electron2D project opens, validates, builds and runs before adding gameplay.",
            Status = ProjectTaskStatus.Backlog,
            Readiness = TaskReadiness.Ready,
            Priority = "P1",
            Rank = "1000",
            CreatedBy = "project-template",
            CreatedAt = createdAtUtc,
            UpdatedAt = createdAtUtc,
            AcceptanceState = ProjectTaskAcceptanceState.Open
        };
        task.Labels.Add("starter");
        task.AcceptanceCriteria.Add(new AcceptanceCriterion(
            "validate-build-run",
            "Run e2d validate, dotnet build and e2d run or the Editor Game workspace without errors.",
            AcceptanceCriterionState.Open,
            []));
        task.Activity.Add(new TaskActivityEntry(
            "activity-template-created",
            "project-template",
            PrincipalKind.System,
            createdAtUtc,
            TaskActivityKind.Decision,
            "Initial starter task created by the Electron2D project template."));

        var board = new TaskBoard(
            "board-main",
            [
                new TaskBoardColumn(ProjectTaskStatus.Backlog, ["welcome"]),
                new TaskBoardColumn(ProjectTaskStatus.Ready, []),
                new TaskBoardColumn(ProjectTaskStatus.InProgress, []),
                new TaskBoardColumn(ProjectTaskStatus.Blocked, []),
                new TaskBoardColumn(ProjectTaskStatus.Review, []),
                new TaskBoardColumn(ProjectTaskStatus.AwaitingAcceptance, []),
                new TaskBoardColumn(ProjectTaskStatus.Done, []),
                new TaskBoardColumn(ProjectTaskStatus.Cancelled, [])
            ]);

        WriteFile(Path.Combine(tasksRoot, "welcome.e2task"), ProjectTaskSerializer.Serialize(task));
        var boardPath = Path.Combine(tasksRoot, "board.e2tasks");
        WriteFile(boardPath, ProjectTaskSerializer.SerializeBoard(board));
        return boardPath;
    }

    private static int WriteStarterSkills(string projectPath)
    {
        foreach (var skillName in StarterSkillNames)
        {
            var path = Path.Combine(projectPath, ".codex", "skills", skillName, "SKILL.md");
            WriteFile(path, CreateSkill(skillName));
        }

        return StarterSkillNames.Length;
    }

    private static void RewriteProjectFiles(string projectPath, string projectName, string rendererProfile)
    {
        var namespaceName = ToCSharpNamespace(projectName);
        var sourceProjectPath = Path.Combine(projectPath, TemplateProjectName + ".csproj");
        var targetProjectPath = Path.Combine(projectPath, projectName + ".csproj");
        if (File.Exists(sourceProjectPath))
        {
            File.Move(sourceProjectPath, targetProjectPath);
        }

        var scriptsDirectory = Directory.EnumerateDirectories(projectPath, "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path => string.Equals(Path.GetFileName(path), "Scripts", StringComparison.Ordinal));
        var targetScriptsDirectory = Path.Combine(projectPath, "scripts");
        if (scriptsDirectory is not null)
        {
            var temporaryScriptsDirectory = Path.Combine(projectPath, "__scripts_tmp");
            if (Directory.Exists(temporaryScriptsDirectory))
            {
                Directory.Delete(temporaryScriptsDirectory, recursive: true);
            }

            Directory.Move(scriptsDirectory, temporaryScriptsDirectory);
            Directory.Move(temporaryScriptsDirectory, targetScriptsDirectory);
        }

        var sourceSettingsPath = Path.Combine(projectPath, "project.e2d.json");
        var targetSettingsPath = Path.Combine(projectPath, projectName + ".e2d");
        if (File.Exists(sourceSettingsPath))
        {
            File.Move(sourceSettingsPath, targetSettingsPath);
        }

        ReplaceText(Path.Combine(projectPath, "Program.cs"), TemplateProjectName, namespaceName);
        ReplaceText(targetProjectPath, "project.e2d.json", projectName + ".e2d");
        ReplaceText(targetProjectPath, "Scripts", "scripts");
        ReplaceText(Path.Combine(projectPath, "scripts", "MainScene.cs"), TemplateProjectName, namespaceName);
        ReplaceText(Path.Combine(projectPath, "README.md"), "Electron2D Empty Project", projectName);
        ReplaceText(Path.Combine(projectPath, "scenes", "main.scene.json"), "Scripts/MainScene.cs", "scripts/MainScene.cs");

        var projectManifest = ReadJsonObject(targetSettingsPath);
        projectManifest["name"] = projectName;
        projectManifest["rendererProfile"] = rendererProfile;
        EmbedProjectSections(projectPath, projectManifest, rendererProfile);
        WriteJsonObject(targetSettingsPath, projectManifest);
    }

    private static void CopyTemplate(string templateRoot, string projectPath)
    {
        foreach (var directory in Directory.EnumerateDirectories(templateRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(templateRoot, directory);
            if (IsTemplateMetadataPath(relativePath))
            {
                continue;
            }

            if (IsGeneratedProjectPath(relativePath))
            {
                continue;
            }

            Directory.CreateDirectory(Path.Combine(projectPath, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(templateRoot, file);
            if (IsTemplateMetadataPath(relativePath))
            {
                continue;
            }

            if (IsGeneratedProjectPath(relativePath))
            {
                continue;
            }

            var destination = Path.Combine(projectPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? projectPath);
            File.Copy(file, destination, overwrite: false);
        }
    }

    private static bool TryInitializeGit(string projectPath, List<StructuredDiagnostic> diagnostics)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo("git", "init")
            {
                WorkingDirectory = projectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (!process.Start())
            {
                diagnostics.Add(Warning("Git initialization could not start. The project files were created, but the repository must be initialized manually."));
                return false;
            }

            if (!process.WaitForExit(5000))
            {
                process.Kill(entireProcessTree: true);
                diagnostics.Add(Warning("Git initialization timed out. The project files were created, but the repository must be initialized manually."));
                return false;
            }

            if (process.ExitCode == 0 && Directory.Exists(Path.Combine(projectPath, ".git")))
            {
                return true;
            }

            var error = process.StandardError.ReadToEnd().Trim();
            diagnostics.Add(Warning(string.IsNullOrWhiteSpace(error)
                ? "Git initialization failed. The project files were created, but the repository must be initialized manually."
                : $"Git initialization failed: {error}"));
            return false;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            diagnostics.Add(Warning($"Git initialization is unavailable: {exception.Message}"));
            return false;
        }
    }

    private static StructuredDiagnostic Warning(string message)
    {
        var definition = DiagnosticCodeRegistry.Get("E2D-PROJECT-0003");
        return StructuredDiagnostic.Create(
            definition.Code,
            definition.Severity,
            definition.Category,
            message,
            location: null,
            relatedLocations: [],
            suggestedFixes: []);
    }

    private static void EmbedProjectSections(string projectPath, JsonObject projectManifest, string rendererProfile)
    {
        projectManifest["exportPresets"] ??= new JsonObject
        {
            ["format"] = "Electron2D.ExportPresets",
            ["formatVersion"] = 1,
            ["presets"] = new JsonArray()
        };

        var lockPath = Path.Combine(projectPath, "electron2d.lock.json");
        if (File.Exists(lockPath))
        {
            var lockRoot = ReadJsonObject(lockPath);
            if (lockRoot["project"] is JsonObject project)
            {
                project["rendererProfile"] = rendererProfile;
            }

            projectManifest["reproducibilityLock"] = lockRoot;
            File.Delete(lockPath);
        }
    }

    private static JsonObject ReadJsonObject(string path)
    {
        return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ??
            throw new FormatException($"JSON root must be an object: {path}");
    }

    private static void WriteJsonObject(string path, JsonObject root)
    {
        WriteFile(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");
    }

    private static void ReplaceText(string path, string oldValue, string newValue)
    {
        if (!File.Exists(path))
        {
            return;
        }

        File.WriteAllText(path, File.ReadAllText(path).Replace(oldValue, newValue, StringComparison.Ordinal));
    }

    private static void WriteFile(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, text.ReplaceLineEndings("\n"));
    }

    private static string CreateGitIgnore()
    {
        return """
        bin/
        obj/
        .electron2d/build/
        .electron2d/import-cache/
        .electron2d/workspaces/
        .electron2d/context/
        .electron2d/session/
        .electron2d/user/
        """;
    }

    private static string CreateAgentInstructions(string projectName, string rendererProfile)
    {
        return $$"""
        # Agent Instructions

        This is an Electron2D 0.1-preview project.

        - Project name: `{{projectName}}`
        - .NET SDK: `.NET {{DotNetSdkVersion}}`
        - Renderer profile: `{{rendererProfile}}`

        Use these commands from the project root:

        - `e2d validate --project .`
        - `dotnet build`
        - `dotnet test`
        - `e2d run --project .`
        - `e2d export --project .`
        - `e2d api compare-godot <type>`

        Project structure:

        - `{{projectName}}.e2d` stores project settings, export presets and reproducibility metadata.
        - `scenes/` stores scene files.
        - `scripts/` stores C# gameplay code.
        - `.electron2d/tasks/` stores ProjectTaskManager task documents.
        - `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/context/`, `.electron2d/session/` and `.electron2d/user/` are generated or local-only working directories.

        Rules for agents:

        - Prefer the active Editor session through MCP or Tooling when the project is open in Electron2D.Editor.
        - Do not edit `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/context/`, `.electron2d/session/` or `.electron2d/user/` by hand.
        - Keep stable UID values intact unless the documented operation intentionally creates a new object.
        - Run `e2d validate --project .` after changing project files.
        - Do not use external API members outside the approved Electron2D Godot 4.7 public API contract. Use `e2d api compare-godot <type>` as the strict verifier when in doubt.
        - Use ProjectTaskManager through Editor, Tooling or MCP. Do not edit task storage files directly.
        - Link changes, tests, diagnostics, jobs and artifacts to the active task when the workflow exposes that operation.
        - Submit completed agent work for human acceptance with `task_submit_for_acceptance`; do not mark work as accepted for the user.
        """;
    }

    private static string CreateSkill(string skillName)
    {
        var description = skillName switch
        {
            "electron2d-scene" => "Create and update Electron2D scenes with stable UID values and project validation.",
            "electron2d-gameplay-code" => "Write C# gameplay code that stays inside the approved Electron2D Godot 4.7 public API contract.",
            "electron2d-resource-import" => "Import textures, fonts, audio and other resources without editing generated import cache files.",
            "electron2d-run-test" => "Validate, build, run and test an Electron2D project with structured diagnostics.",
            "electron2d-export" => "Prepare export presets and verify production package contents.",
            _ => "Work with an Electron2D project."
        };
        var body = skillName switch
        {
            "electron2d-scene" => "Use scene files under `scenes/`, preserve existing UID values, and run `e2d validate --project .` after structural changes.",
            "electron2d-gameplay-code" => "Use C# files under `scripts/`, prefer Electron2D APIs from the approved Godot 4.7 public API contract, and verify uncertain APIs with `e2d api compare-godot <type>`.",
            "electron2d-resource-import" => "Add source assets to project folders, let Electron2D rebuild `.electron2d/import-cache/`, and never edit cache artifacts by hand.",
            "electron2d-run-test" => "Run the narrowest useful check first, then `dotnet build`, scene tests or `e2d run --project .` when the change affects runtime behavior.",
            "electron2d-export" => "Use explicit export presets, keep signing credentials out of project files, and confirm `.electron2d/tasks/` is not included in production packages.",
            _ => "Follow the project `AGENTS.md` instructions."
        };

        return $$"""
        ---
        name: {{skillName}}
        description: {{description}}
        ---

        # {{skillName}}

        {{body}}
        """;
    }

    private static string ValidateProjectName(string projectName)
    {
        var normalized = RequirePath(projectName, "Project name").Trim();
        if (normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Project name contains characters that cannot be used in a directory name.", nameof(projectName));
        }

        return normalized;
    }

    private static string ValidateRendererProfile(string rendererProfile)
    {
        var normalized = RequirePath(rendererProfile, "Renderer profile").Trim();
        return normalized is "Automatic" or "Standard" or "Compatibility"
            ? normalized
            : throw new ArgumentException("Renderer profile must be Automatic, Standard or Compatibility.", nameof(rendererProfile));
    }

    private static string RequirePath(string value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{description} must be a non-empty string.");
        }

        return value;
    }

    private static void ValidateTemplateRoot(string templateRoot)
    {
        if (!Directory.Exists(templateRoot))
        {
            throw new InvalidOperationException($"Project template directory was not found: {templateRoot}");
        }

        var projectSettings = ResolveTemplateProjectSettingsPath(templateRoot);
        if (projectSettings is null)
        {
            throw new InvalidOperationException($"Project template manifest was not found in: {templateRoot}");
        }
    }

    private static string? ResolveTemplateProjectSettingsPath(string templateRoot)
    {
        var named = Directory.EnumerateFiles(templateRoot, "*.e2d", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault();
        if (named is not null)
        {
            return named;
        }

        var legacy = Path.Combine(templateRoot, "project.e2d.json");
        return File.Exists(legacy) ? legacy : null;
    }

    private static bool IsTemplateMetadataPath(string relativePath)
    {
        return relativePath.Equals(".template.config", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith(".template.config" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith(".template.config" + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGeneratedProjectPath(string relativePath)
    {
        return relativePath.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("bin" + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            relativePath.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("obj" + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            relativePath.Equals(Path.Combine(".electron2d", "build"), StringComparison.OrdinalIgnoreCase) ||
            relativePath.Equals(".electron2d" + Path.DirectorySeparatorChar + "build", StringComparison.OrdinalIgnoreCase) ||
            relativePath.Equals(".electron2d" + Path.AltDirectorySeparatorChar + "build", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith(".electron2d" + Path.DirectorySeparatorChar + "build" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith(".electron2d" + Path.AltDirectorySeparatorChar + "build" + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToCSharpNamespace(string projectName)
    {
        var builder = new StringBuilder(projectName.Length);
        foreach (var character in projectName)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(character);
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '.')
            {
                builder.Append('_');
            }
        }

        if (builder.Length == 0 || char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    private static string ResolveProjectPath(string projectPath, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(projectPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
