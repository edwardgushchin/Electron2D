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
namespace Electron2D.Editor.Shell;

internal sealed class ShellLayout
{
    public const int DefaultViewportWidth = 1280;
    public const int DefaultViewportHeight = 720;

    private static readonly string[] ForbiddenTokens = ["3D", "AssetLib", "GDScript", ".gd", "Node3D"];
    private readonly Dictionary<string, ShellWorkspaceState> workspaceStates;

    private ShellLayout(
        string selectedWorkspace,
        bool bottomPanelCollapsed,
        int leftDockWidth,
        int rightDockWidth,
        int bottomPanelHeight,
        IReadOnlyList<string> documentTabs,
        Dictionary<string, ShellWorkspaceState> workspaceStates,
        ShellStartupProject? startupProject)
    {
        SelectedWorkspace = selectedWorkspace;
        BottomPanelCollapsed = bottomPanelCollapsed;
        LeftDockWidth = leftDockWidth;
        RightDockWidth = rightDockWidth;
        BottomPanelHeight = bottomPanelHeight;
        DocumentTabs = documentTabs;
        this.workspaceStates = workspaceStates;
        StartupProject = startupProject;
    }

    public IReadOnlyList<string> MenuItems { get; } = ["Scene", "Project", "Debug", "Editor", "Help"];

    public IReadOnlyList<string> WorkspaceSwitcher { get; } = ["2D", "Script", "Game", "Tasks"];

    public IReadOnlyList<string> LeftDocks { get; } = ["Scene", "FileSystem"];

    public IReadOnlyList<string> RightDocks { get; } = ["Inspector", "Node"];

    public IReadOnlyList<string> BottomPanelTabs { get; } = ["Output", "Debugger", "Agent", "Diagnostics", "Search", "Animation", "Audio", "Tests"];

    public IReadOnlyList<ShellShortcut> Shortcuts { get; } =
    [
        new("F5", "run-project", "Run project"),
        new("F6", "run-current-scene", "Run current scene"),
        new("F7", "script-workspace-or-build", "Switch to Script workspace or build current code context"),
        new("F8", "stop-or-pause", "Stop or pause active play/debug session"),
        new("Ctrl+S", "save-current-document", "Save current document"),
        new("Ctrl+Shift+S", "save-all", "Save all documents"),
        new("Ctrl+F", "search-current", "Search active document or panel"),
        new("Ctrl+Shift+F", "search-project", "Search project"),
        new("Ctrl+Z", "undo", "Undo active document or workspace command"),
        new("Ctrl+Y", "redo", "Redo active document or workspace command"),
        new("Ctrl+P", "quick-open-project-file", "Quick open project file"),
        new("Ctrl+G", "go-to-line", "Go to line in Script workspace")
    ];

    public string SelectedWorkspace { get; private set; }

    public bool BottomPanelCollapsed { get; private set; }

    public int LeftDockWidth { get; }

    public int RightDockWidth { get; }

    public int BottomPanelHeight { get; }

    public IReadOnlyList<string> DocumentTabs { get; }

    public IReadOnlyDictionary<string, ShellWorkspaceState> WorkspaceStates => workspaceStates;

    public ShellStartupProject? StartupProject { get; }

    public bool ProjectLoaded => StartupProject is not null;

    public string ProjectName => StartupProject?.ProjectName ?? string.Empty;

    public string ProjectPath => StartupProject?.ProjectPath ?? string.Empty;

    public string ProjectSettingsPath => StartupProject?.ProjectSettingsPath ?? string.Empty;

    public string MainScenePath => StartupProject?.MainScenePath ?? string.Empty;

    public static ShellLayout CreateDefault()
    {
        var workspaceStates = new Dictionary<string, ShellWorkspaceState>(StringComparer.Ordinal)
        {
            ["2D"] = new("2D", "Player", 64, 96, 1.5d, ["res://scenes/main.e2scene.json"]),
            ["Script"] = new("Script", "Player", 0, 42, 1d, ["Scripts/PlayerController.cs", "Scripts/EnemyController.cs"]),
            ["Game"] = new("Game", "RuntimeRoot", 0, 0, 1d, ["res://scenes/main.e2scene.json"]),
            ["Tasks"] = new("Tasks", "T-0157", 0, 0, 1d, ["T-0157"])
        };

        return new ShellLayout(
            "2D",
            bottomPanelCollapsed: true,
            leftDockWidth: 250,
            rightDockWidth: 300,
            bottomPanelHeight: 128,
            documentTabs: ["main.e2scene.json", "PlayerController.cs", "T-0157"],
            workspaceStates,
            startupProject: null);
    }

    public static ShellLayout CreateForProject(ShellStartupProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(project.ProjectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(project.ProjectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(project.ProjectSettingsPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(project.MainScenePath);

        var mainSceneDocument = Path.GetFileName(project.MainScenePath);
        var mainSceneResourcePath = ToProjectResourcePath(project.ProjectPath, project.MainScenePath);
        var workspaceStates = new Dictionary<string, ShellWorkspaceState>(StringComparer.Ordinal)
        {
            ["2D"] = new("2D", mainSceneDocument, 0, 0, 1d, [mainSceneResourcePath]),
            ["Script"] = new("Script", project.ProjectName, 0, 0, 1d, []),
            ["Game"] = new("Game", project.ProjectName, 0, 0, 1d, [mainSceneResourcePath]),
            ["Tasks"] = new("Tasks", project.ProjectName, 0, 0, 1d, [])
        };

        return new ShellLayout(
            "2D",
            bottomPanelCollapsed: true,
            leftDockWidth: 250,
            rightDockWidth: 300,
            bottomPanelHeight: 128,
            documentTabs: [mainSceneDocument],
            workspaceStates,
            project);
    }

    public static ShellLayout FromState(ShellLayoutState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var defaults = CreateDefault();
        var workspaces = new Dictionary<string, ShellWorkspaceState>(StringComparer.Ordinal);
        foreach (var workspace in defaults.WorkspaceSwitcher)
        {
            workspaces[workspace] = state.WorkspaceStates.SingleOrDefault(item => item.Workspace == workspace)
                ?? defaults.workspaceStates[workspace];
        }

        var selectedWorkspace = defaults.WorkspaceSwitcher.Contains(state.SelectedWorkspace, StringComparer.Ordinal)
            ? state.SelectedWorkspace
            : defaults.SelectedWorkspace;

        return new ShellLayout(
            selectedWorkspace,
            state.BottomPanelCollapsed,
            state.LeftDockWidth > 0 ? state.LeftDockWidth : defaults.LeftDockWidth,
            state.RightDockWidth > 0 ? state.RightDockWidth : defaults.RightDockWidth,
            state.BottomPanelHeight > 0 ? state.BottomPanelHeight : defaults.BottomPanelHeight,
            state.DocumentTabs.Length > 0 ? state.DocumentTabs : defaults.DocumentTabs,
            workspaces,
            startupProject: null);
    }

    public void SwitchWorkspace(string workspace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspace);
        if (!WorkspaceSwitcher.Contains(workspace, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"Editor shell workspace '{workspace}' is not supported by the 0.1.0 layout.");
        }

        SelectedWorkspace = workspace;
    }

    public void ToggleBottomPanel()
    {
        BottomPanelCollapsed = !BottomPanelCollapsed;
    }

    public ShellWorkspaceState GetWorkspaceState(string workspace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspace);
        return workspaceStates.TryGetValue(workspace, out var state)
            ? state
            : throw new InvalidOperationException($"Editor shell workspace '{workspace}' does not have saved state.");
    }

    public ShellLayoutState CaptureState()
    {
        return new ShellLayoutState(
            SelectedWorkspace,
            BottomPanelCollapsed,
            LeftDockWidth,
            RightDockWidth,
            BottomPanelHeight,
            DocumentTabs.ToArray(),
            WorkspaceSwitcher.Select(GetWorkspaceState).ToArray());
    }

    public IReadOnlyList<ShellRegion> CreateVisualRegions()
    {
        const int topMenuHeight = 42;
        const int topControlsHeight = 38;
        const int documentTabHeight = 30;
        var topHeight = topMenuHeight + topControlsHeight + documentTabHeight;
        var bottomTop = DefaultViewportHeight - BottomPanelHeight;
        var centerX = LeftDockWidth;
        var centerWidth = DefaultViewportWidth - LeftDockWidth - RightDockWidth;
        var centerHeight = bottomTop - topHeight;
        var rightX = DefaultViewportWidth - RightDockWidth;

        var regions = new List<ShellRegion>();
        var menuX = 12;
        foreach (var item in MenuItems)
        {
            regions.Add(new ShellRegion("Menu", item, menuX, 10, 88, 24, Clickable: true));
            menuX += 92;
        }

        var workspaceX = 500;
        foreach (var workspace in WorkspaceSwitcher)
        {
            regions.Add(new ShellRegion("WorkspaceSwitcher", workspace, workspaceX, 10, 92, 28, Clickable: true));
            workspaceX += 98;
        }

        regions.Add(new ShellRegion("RunControls", "Run Scene", 920, 10, 112, 28, Clickable: true));
        regions.Add(new ShellRegion("RunControls", "Run Project", 1040, 10, 128, 28, Clickable: true));
        regions.Add(new ShellRegion("RunControls", "Stop", 1176, 10, 72, 28, Clickable: true));

        var tabX = 12;
        foreach (var tab in DocumentTabs)
        {
            var width = Math.Max(116, (tab.Length * 8) + 28);
            regions.Add(new ShellRegion("DocumentTabs", tab, tabX, topMenuHeight + 10, width, 24, Clickable: true));
            tabX += width + 8;
        }

        var sceneHeight = 294;
        regions.Add(new ShellRegion("LeftDock", "Scene", 0, topHeight, LeftDockWidth, sceneHeight, Clickable: true));
        regions.Add(new ShellRegion("LeftDock", "FileSystem", 0, topHeight + sceneHeight, LeftDockWidth, centerHeight - sceneHeight, Clickable: true));
        regions.Add(new ShellRegion("CenterWorkspace", "Active workspace: " + SelectedWorkspace, centerX, topHeight, centerWidth, centerHeight, Clickable: true));
        var inspectorHeight = Math.Max(180, centerHeight - 72);
        regions.Add(new ShellRegion("RightDock", "Inspector", rightX, topHeight, RightDockWidth, inspectorHeight, Clickable: true));
        regions.Add(new ShellRegion("RightDock", "Node", rightX, topHeight + inspectorHeight, RightDockWidth, centerHeight - inspectorHeight, Clickable: true));
        regions.Add(new ShellRegion("BottomPanel", "Bottom Panel", 0, bottomTop, DefaultViewportWidth, BottomPanelHeight, Clickable: true));

        var bottomTabX = 12;
        foreach (var tab in BottomPanelTabs)
        {
            var width = Math.Max(82, (tab.Length * 8) + 28);
            regions.Add(new ShellRegion("BottomPanelTab", tab, bottomTabX, bottomTop + 10, width, 26, Clickable: true));
            bottomTabX += width + 8;
        }

        regions.Add(new ShellRegion("BottomPanelToggle", BottomPanelCollapsed ? "Expand" : "Collapse", DefaultViewportWidth - 100, bottomTop + 10, 82, 26, Clickable: true));

        return regions;
    }

    public IReadOnlyList<string> FindForbiddenUiMatches()
    {
        var visibleText = MenuItems
            .Concat(WorkspaceSwitcher)
            .Concat(LeftDocks)
            .Concat(RightDocks)
            .Concat(BottomPanelTabs)
            .Concat(DocumentTabs)
            .Concat(CreateVisualRegions().Select(region => region.Label));

        return FindForbiddenMatches(visibleText).ToArray();
    }

    public IReadOnlyList<string> FindForbiddenShortcutMatches()
    {
        return FindForbiddenMatches(Shortcuts.SelectMany(shortcut => new[] { shortcut.Action, shortcut.Description })).ToArray();
    }

    private static string ToProjectResourcePath(string projectPath, string filePath)
    {
        var relativePath = Path.GetRelativePath(projectPath, filePath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
        return "res://" + relativePath;
    }

    private static IEnumerable<string> FindForbiddenMatches(IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            foreach (var token in ForbiddenTokens)
            {
                if (value.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    yield return $"{token}:{value}";
                }
            }
        }
    }
}

internal sealed record ShellStartupProject(
    string ProjectName,
    string ProjectPath,
    string ProjectSettingsPath,
    string MainScenePath);

internal sealed record ShellRegion(
    string Area,
    string Label,
    int X,
    int Y,
    int Width,
    int Height,
    bool Clickable);

internal sealed record ShellWorkspaceState(
    string Workspace,
    string Selection,
    int ScrollX,
    int ScrollY,
    double Zoom,
    string[] OpenDocuments);

internal sealed record ShellLayoutState(
    string SelectedWorkspace,
    bool BottomPanelCollapsed,
    int LeftDockWidth,
    int RightDockWidth,
    int BottomPanelHeight,
    string[] DocumentTabs,
    ShellWorkspaceState[] WorkspaceStates);

internal sealed record ShellShortcut(
    string Gesture,
    string Action,
    string Description);
