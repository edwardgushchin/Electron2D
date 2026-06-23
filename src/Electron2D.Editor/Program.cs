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
using Electron2D.Editor.Inspector;
using Electron2D.Editor.AgentWorkspace;
using Electron2D.Editor.FileSystemDock;
using Electron2D.Editor.ProjectTasks;
using Electron2D.Editor.ProjectSettings;
using Electron2D.Editor.ProjectManagement;
using Electron2D.Editor.Run;
using Electron2D.Editor.Scripting;
using Electron2D.Editor.SceneTreeDock;
using Electron2D.Editor.Shell;
using Electron2D.Editor.SpecializedEditors;
using Electron2D.Editor.Viewport2D;

namespace Electron2D.Editor;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            return RunEditorWindow();
        }

        if (args is ["--smoke"])
        {
            return RunOnce(isSmoke: true);
        }

        if (args is ["--window-smoke", var windowSmokeWorkRoot])
        {
            return RunWindowSmoke(windowSmokeWorkRoot);
        }

        if (args is ["--project-manager-smoke", var workRoot, "--user-data-dir", var userDataRoot])
        {
            return RunProjectManagerSmoke(workRoot, userDataRoot);
        }

        if (args is ["--open-project-smoke", var projectFilePath, "--user-data-dir", var openProjectUserDataRoot])
        {
            return RunOpenProjectSmoke(projectFilePath, openProjectUserDataRoot);
        }

        if (args is ["--open-project-window-smoke", var openProjectWindowFilePath, var openProjectWindowWorkRoot, "--user-data-dir", var openProjectWindowUserDataRoot])
        {
            return RunOpenProjectWindowSmoke(openProjectWindowFilePath, openProjectWindowWorkRoot, openProjectWindowUserDataRoot);
        }

        if (args.Length == 1 && Electron2D.ProjectFileLocator.IsProjectFilePath(args[0]))
        {
            return RunEditorWindow(args[0]);
        }

        if (args is ["--scene-tree-dock-smoke", var sceneTreeDockWorkRoot])
        {
            return RunSceneTreeDockSmoke(sceneTreeDockWorkRoot);
        }

        if (args is ["--viewport-2d-smoke", var viewport2DWorkRoot])
        {
            return RunViewport2DSmoke(viewport2DWorkRoot);
        }

        if (args is ["--inspector-smoke", var inspectorWorkRoot])
        {
            return RunInspectorSmoke(inspectorWorkRoot);
        }

        if (args is ["--file-system-dock-smoke", var fileSystemDockWorkRoot])
        {
            return RunFileSystemDockSmoke(fileSystemDockWorkRoot);
        }

        if (args is ["--script-workflow-smoke", var scriptWorkflowWorkRoot])
        {
            return RunScriptWorkflowSmoke(scriptWorkflowWorkRoot);
        }

        if (args is ["--script-workspace-smoke", var scriptWorkspaceWorkRoot])
        {
            return RunScriptWorkspaceSmoke(scriptWorkspaceWorkRoot);
        }

        if (args is ["--script-language-services-smoke", var scriptLanguageServicesWorkRoot])
        {
            return RunScriptLanguageServicesSmoke(scriptLanguageServicesWorkRoot);
        }

        if (args is ["--managed-debugger-smoke", var managedDebuggerWorkRoot])
        {
            return RunManagedDebuggerSmoke(managedDebuggerWorkRoot);
        }

        if (args is ["--script-debug-tooling-smoke", var scriptDebugToolingWorkRoot])
        {
            return RunScriptDebugToolingSmoke(scriptDebugToolingWorkRoot);
        }

        if (args is ["--run-workflow-smoke", var runWorkflowWorkRoot])
        {
            return RunRunWorkflowSmoke(runWorkflowWorkRoot);
        }

        if (args is ["--shell-layout-smoke", var shellLayoutWorkRoot])
        {
            return RunShellLayoutSmoke(shellLayoutWorkRoot);
        }

        if (args is ["--agent-workspace-panel-smoke", var agentWorkspacePanelWorkRoot])
        {
            return RunAgentWorkspacePanelSmoke(agentWorkspacePanelWorkRoot);
        }

        if (args is ["--tasks-board-smoke", var tasksBoardWorkRoot])
        {
            return RunProjectTasksBoardSmoke(tasksBoardWorkRoot);
        }

        if (args is ["--project-settings-smoke", var projectSettingsWorkRoot])
        {
            return RunProjectSettingsSmoke(projectSettingsWorkRoot);
        }

        if (args is ["--specialized-editors-smoke", var specializedEditorsWorkRoot])
        {
            return RunSpecializedEditorsSmoke(specializedEditorsWorkRoot);
        }

        Console.Error.WriteLine("Usage: Electron2D.Editor [<ProjectName>.e2d] [--smoke] [--window-smoke <work-root>] [--project-manager-smoke <work-root> --user-data-dir <user-data-dir>] [--open-project-smoke <ProjectName>.e2d --user-data-dir <user-data-dir>] [--open-project-window-smoke <ProjectName>.e2d <work-root> --user-data-dir <user-data-dir>] [--scene-tree-dock-smoke <work-root>] [--viewport-2d-smoke <work-root>] [--inspector-smoke <work-root>] [--file-system-dock-smoke <work-root>] [--script-workflow-smoke <work-root>] [--script-workspace-smoke <work-root>] [--script-language-services-smoke <work-root>] [--managed-debugger-smoke <work-root>] [--script-debug-tooling-smoke <work-root>] [--run-workflow-smoke <work-root>] [--shell-layout-smoke <work-root>] [--agent-workspace-panel-smoke <work-root>] [--tasks-board-smoke <work-root>] [--project-settings-smoke <work-root>] [--specialized-editors-smoke <work-root>]");
        return 2;
    }

    private static int RunEditorWindow(string? projectFilePath = null)
    {
        try
        {
            ShellStartupProject? startupProject = null;
            if (!string.IsNullOrWhiteSpace(projectFilePath))
            {
                startupProject = ToStartupProject(OpenProjectForWindow(projectFilePath));
            }

            return WindowHost.RunInteractive(startupProject);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static ProjectOpenResult OpenProjectForWindow(string projectFilePath, string? userSettingsPath = null)
    {
        var settingsPath = userSettingsPath ?? GetDefaultUserSettingsPath();
        var templateRoot = Path.Combine(AppContext.BaseDirectory, "open-existing-project-template-not-used");
        var manager = new ProjectManager(templateRoot);
        var openResult = manager.OpenProject(projectFilePath, settingsPath);
        if (!openResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, openResult.Diagnostics));
        }

        return openResult;
    }

    private static string GetDefaultUserSettingsPath()
    {
        var userDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Electron2D",
            "Editor");
        return Path.Combine(userDataRoot, "user.e2settings.json");
    }

    private static ShellStartupProject ToStartupProject(ProjectOpenResult openResult)
    {
        if (!openResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, openResult.Diagnostics));
        }

        return new ShellStartupProject(
            openResult.ProjectName,
            openResult.ProjectPath,
            openResult.ProjectSettingsPath,
            openResult.MainScenePath);
    }

    private static int RunOnce(bool isSmoke)
    {
        var application = new Application();
        var result = application.Start();

        if (isSmoke)
        {
            Console.WriteLine("Electron2D.Editor smoke passed");
        }
        else
        {
            Console.WriteLine("Electron2D.Editor bootstrap passed");
        }

        Console.WriteLine($"Runtime={result.RuntimeAssemblyName}");
        Console.WriteLine($"Root={result.RootName}");
        Console.WriteLine($"ViewportSize={result.ViewportSize.X}x{result.ViewportSize.Y}");
        Console.WriteLine($"UiRoot={result.UiRootTypeName}");
        Console.WriteLine($"ChildCount={result.UiRootChildCount}");
        Console.WriteLine($"RenderingProfile={result.RenderingProfile}");

        return 0;
    }

    private static int RunProjectManagerSmoke(string workRoot, string userDataRoot)
    {
        try
        {
            var templateRoot = Path.Combine(FindRepositoryRoot(), "data", "templates", "electron2d-empty");
            var userSettingsPath = Path.Combine(Path.GetFullPath(userDataRoot), "user.e2settings.json");
            var manager = new ProjectManager(templateRoot);
            var result = manager.RunSmoke(workRoot, userSettingsPath);
            var succeeded = result.SdkCheck.Available;

            Console.WriteLine(succeeded
                ? "Electron2D.Editor project manager smoke passed"
                : "Electron2D.Editor project manager smoke failed");
            Console.WriteLine($"ProjectName={result.ProjectName}");
            Console.WriteLine($"ProjectPath={result.ProjectPath}");
            Console.WriteLine($"ProjectSettingsPath={result.ProjectSettingsPath}");
            Console.WriteLine($"MainScenePath={result.MainScenePath}");
            Console.WriteLine($"RendererProfile={result.RendererProfile}");
            Console.WriteLine($"UserSettingsPath={result.UserSettingsPath}");
            Console.WriteLine($"SdkAvailable={result.SdkCheck.Available}");
            Console.WriteLine($"SdkVersion={result.SdkCheck.Version}");
            Console.WriteLine($"RecentProjects={result.RecentProjectCount}");

            return succeeded ? 0 : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunSceneTreeDockSmoke(string workRoot)
    {
        try
        {
            var result = SceneTreeDockSmoke.Run(workRoot);

            Console.WriteLine("Electron2D.Editor scene tree dock smoke passed");
            Console.WriteLine($"ScenePath={result.ScenePath}");
            Console.WriteLine($"NodeCount={result.NodeCount}");
            Console.WriteLine($"InvalidOwnerCount={result.InvalidOwnerCount}");
            Console.WriteLine($"UndoAvailable={result.UndoAvailable}");
            Console.WriteLine($"UndoRestored={result.UndoRestored}");
            Console.WriteLine($"RedoRemoved={result.RedoRemoved}");
            Console.WriteLine($"TreeRootText={result.TreeRootText}");
            Console.WriteLine($"ScenePaths={result.ScenePaths}");

            return result.InvalidOwnerCount == 0 && result.UndoAvailable && result.UndoRestored && result.RedoRemoved ? 0 : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunViewport2DSmoke(string workRoot)
    {
        try
        {
            var result = Viewport2DSmoke.Run(workRoot);

            Console.WriteLine("Electron2D.Editor 2D viewport smoke passed");
            Console.WriteLine($"Pan={Format(result.Pan)}");
            Console.WriteLine($"Zoom={Format(result.Zoom)}");
            Console.WriteLine($"Selected={result.Selected}");
            Console.WriteLine($"PlayerPosition={Format(result.PlayerPosition)}");
            Console.WriteLine($"EnemyPosition={Format(result.EnemyPosition)}");
            Console.WriteLine($"PlayerRotation={Format(result.PlayerRotationDegrees)}");
            Console.WriteLine($"EnemyRotation={Format(result.EnemyRotationDegrees)}");
            Console.WriteLine($"PlayerScale={Format(result.PlayerScale)}");
            Console.WriteLine($"EnemyScale={Format(result.EnemyScale)}");
            Console.WriteLine($"SelectionBounds={Format(result.SelectionBounds)}");
            Console.WriteLine($"CollisionOverlays={result.CollisionOverlays}");
            Console.WriteLine($"CameraPreview={Format(result.CameraPreview)}");
            Console.WriteLine($"WorldUnderCursorStable={result.WorldUnderCursorStable}");

            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunInspectorSmoke(string workRoot)
    {
        try
        {
            var result = InspectorSmoke.Run(workRoot);

            Console.WriteLine("Electron2D.Editor inspector smoke passed");
            Console.WriteLine($"ScenePath={result.ScenePath}");
            Console.WriteLine($"PropertyCount={result.PropertyCount}");
            Console.WriteLine($"ExportedProperties={result.ExportedProperties}");
            Console.WriteLine($"SerializedHealth={result.SerializedHealth}");
            Console.WriteLine($"SerializedName={result.SerializedName}");
            Console.WriteLine($"UndoName={result.UndoName}");
            Console.WriteLine($"RedoName={result.RedoName}");
            Console.WriteLine($"SerializedMode={result.SerializedMode}");
            Console.WriteLine($"SerializedFlags={result.SerializedFlags}");
            Console.WriteLine($"SerializedTags={result.SerializedTags}");
            Console.WriteLine($"SerializedPath={result.SerializedPath}");
            Console.WriteLine($"ResourceReference={result.ResourceReference}");
            Console.WriteLine($"NestedMaxHealth={result.NestedMaxHealth}");
            Console.WriteLine($"RoundTripStable={result.RoundTripStable}");

            return result.PropertyCount == result.ExportedProperties && result.RoundTripStable ? 0 : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunFileSystemDockSmoke(string workRoot)
    {
        try
        {
            var result = FileSystemDockSmoke.Run(workRoot);

            Console.WriteLine("Electron2D.Editor file system dock smoke passed");
            Console.WriteLine($"ScenePath={result.ScenePath}");
            Console.WriteLine($"InitialItemCount={result.InitialItemCount}");
            Console.WriteLine($"FolderCreated={result.FolderCreated}");
            Console.WriteLine($"MovedFileExists={result.MovedFileExists}");
            Console.WriteLine($"RenamedResourcePath={result.RenamedResourcePath}");
            Console.WriteLine($"MovedResourcePath={result.MovedResourcePath}");
            Console.WriteLine($"UidBefore={result.UidBefore}");
            Console.WriteLine($"UidAfter={result.UidAfter}");
            Console.WriteLine($"UidStable={result.UidStable}");
            Console.WriteLine($"SceneExternalReferencePath={result.SceneExternalReferencePath}");
            Console.WriteLine($"SceneExternalReferenceUid={result.SceneExternalReferenceUid}");
            Console.WriteLine($"DraggedNodeType={result.DraggedNodeType}");
            Console.WriteLine($"SearchResults={result.SearchResults}");
            Console.WriteLine($"ImportErrorCount={result.ImportErrorCount}");
            Console.WriteLine($"ImportErrorPath={result.ImportErrorPath}");
            Console.WriteLine($"ImportErrorVisible={result.ImportErrorVisible}");
            Console.WriteLine($"LiveImportStatusVisible={result.LiveImportStatusVisible}");
            Console.WriteLine($"RoundTripStable={result.RoundTripStable}");

            return result.FolderCreated &&
                result.MovedFileExists &&
                result.UidStable &&
                result.ImportErrorVisible &&
                result.LiveImportStatusVisible &&
                result.RoundTripStable
                ? 0
                : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunScriptWorkflowSmoke(string workRoot)
    {
        try
        {
            var result = ScriptWorkflowSmoke.Run(workRoot);

            Console.WriteLine("Electron2D.Editor script workflow smoke passed");
            Console.WriteLine($"ProjectPath={result.ProjectPath}");
            Console.WriteLine($"ScenePath={result.ScenePath}");
            Console.WriteLine($"ScriptPath={result.ScriptPath}");
            Console.WriteLine($"CreatedScriptExists={result.CreatedScriptExists}");
            Console.WriteLine($"OpenedScript={result.OpenedScript}");
            Console.WriteLine($"DirtyBeforeSave={result.DirtyBeforeSave}");
            Console.WriteLine($"DirtyAfterSave={result.DirtyAfterSave}");
            Console.WriteLine($"AttachedNodeType={result.AttachedNodeType}");
            Console.WriteLine($"CompilerErrorCount={result.CompilerErrorCount}");
            Console.WriteLine($"FirstCompilerErrorCode={result.FirstCompilerErrorCode}");
            Console.WriteLine($"FirstCompilerErrorLine={result.FirstCompilerErrorLine}");
            Console.WriteLine($"FirstCompilerErrorColumn={result.FirstCompilerErrorColumn}");
            Console.WriteLine($"FixedBuildSucceeded={result.FixedBuildSucceeded}");
            Console.WriteLine($"RunExitCode={result.RunExitCode}");
            Console.WriteLine($"RunOutputContainsMessage={result.RunOutputContainsMessage}");
            Console.WriteLine($"SceneRoundTripStable={result.SceneRoundTripStable}");
            Console.WriteLine($"RerunAfterRebuild={result.RerunAfterRebuild}");

            return result.CreatedScriptExists &&
                result.OpenedScript &&
                result.DirtyBeforeSave &&
                !result.DirtyAfterSave &&
                result.CompilerErrorCount > 0 &&
                result.FixedBuildSucceeded &&
                result.RerunAfterRebuild &&
                result.SceneRoundTripStable
                ? 0
                : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunOpenProjectSmoke(string projectFilePath, string userDataRoot)
    {
        try
        {
            var templateRoot = Path.Combine(FindRepositoryRoot(), "data", "templates", "electron2d-empty");
            var userSettingsPath = Path.Combine(Path.GetFullPath(userDataRoot), "user.e2settings.json");
            var manager = new ProjectManager(templateRoot);
            var result = manager.OpenProject(projectFilePath, userSettingsPath);
            if (!result.Succeeded)
            {
                Console.Error.WriteLine(string.Join(Environment.NewLine, result.Diagnostics));
                return 1;
            }

            Console.WriteLine("Electron2D.Editor open project smoke passed");
            Console.WriteLine($"ProjectName={result.ProjectName}");
            Console.WriteLine($"ProjectPath={result.ProjectPath}");
            Console.WriteLine($"ProjectSettingsPath={result.ProjectSettingsPath}");
            Console.WriteLine($"MainScenePath={result.MainScenePath}");
            Console.WriteLine($"MainSceneLoaded={File.Exists(result.MainScenePath)}");
            Console.WriteLine($"RecentProjects={result.RecentProjectCount}");
            return File.Exists(result.MainScenePath) ? 0 : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunOpenProjectWindowSmoke(string projectFilePath, string workRoot, string userDataRoot)
    {
        try
        {
            var userSettingsPath = Path.Combine(Path.GetFullPath(userDataRoot), "user.e2settings.json");
            var openResult = OpenProjectForWindow(projectFilePath, userSettingsPath);
            var result = WindowSmoke.RunProjectStartupSmoke(workRoot, ToStartupProject(openResult));

            Console.WriteLine("Electron2D.Editor open project window smoke passed");
            Console.WriteLine($"ProjectName={openResult.ProjectName}");
            Console.WriteLine($"ProjectPath={openResult.ProjectPath}");
            Console.WriteLine($"ProjectSettingsPath={openResult.ProjectSettingsPath}");
            Console.WriteLine($"MainScenePath={openResult.MainScenePath}");
            Console.WriteLine($"ProjectLoaded={result.ProjectLoaded}");
            Console.WriteLine($"MainSceneLoaded={File.Exists(openResult.MainScenePath)}");
            Console.WriteLine($"RecentProjects={openResult.RecentProjectCount}");
            Console.WriteLine($"DocumentTabs={Join(result.DocumentTabs)}");
            Console.WriteLine($"GameDocuments={Join(result.GameDocuments)}");
            Console.WriteLine($"WindowTitle={result.WindowTitle}");
            Console.WriteLine($"WindowCreated={result.WindowCreated}");
            Console.WriteLine($"WindowShown={result.WindowShown}");
            Console.WriteLine($"FramePresented={result.FramePresented}");
            Console.WriteLine($"EventPumpObserved={result.EventPumpObserved}");
            Console.WriteLine($"PointerInteractionObserved={result.PointerInteractionObserved}");
            Console.WriteLine($"KeyboardInteractionObserved={result.KeyboardInteractionObserved}");
            Console.WriteLine($"RuntimeControlTree={result.RuntimeControlTree}");
            Console.WriteLine($"VisualHarnessRemoved={result.VisualHarnessRemoved}");
            Console.WriteLine($"RuntimeUiRendering={result.RuntimeUiRendering}");
            Console.WriteLine($"RuntimeUiInputDispatch={result.RuntimeUiInputDispatch}");
            Console.WriteLine($"RenderSource={result.RenderSource}");
            Console.WriteLine($"InputDispatchSource={result.InputDispatchSource}");
            Console.WriteLine($"DrawCommands={result.DrawCommands}");
            Console.WriteLine($"RedDominantPixelRatio={result.RedDominantPixelRatio.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            Console.WriteLine($"SelectedWorkspace={result.SelectedWorkspace}");
            Console.WriteLine($"WindowSize={result.WindowWidth}x{result.WindowHeight}");
            Console.WriteLine($"WindowPixelSize={result.PixelWidth}x{result.PixelHeight}");
            Console.WriteLine($"VideoDriver={result.VideoDriver}");
            Console.WriteLine($"FrameCount={result.FrameCount}");
            Console.WriteLine($"TextOverflowCount={result.TextOverflowCount}");
            Console.WriteLine($"ClickableControlCount={result.ClickableControlCount}");
            Console.WriteLine($"ForbiddenUiMatches={result.ForbiddenUiMatchCount}");
            Console.WriteLine($"ScreenshotReviewed={result.ScreenshotReviewed}");
            Console.WriteLine($"ScreenshotPath={result.ScreenshotPath}");
            Console.WriteLine($"AnalysisPath={result.AnalysisPath}");

            return result.ProjectLoaded &&
                result.WindowCreated &&
                result.WindowShown &&
                result.FramePresented &&
                result.EventPumpObserved &&
                result.RuntimeControlTree &&
                result.VisualHarnessRemoved &&
                result.RuntimeUiRendering &&
                result.RuntimeUiInputDispatch &&
                string.Equals(result.RenderSource, RuntimeFrameRenderer.RenderSource, StringComparison.Ordinal) &&
                string.Equals(result.InputDispatchSource, RuntimeFrameRenderer.InputDispatchSource, StringComparison.Ordinal) &&
                result.DrawCommands >= 16 &&
                result.RedDominantPixelRatio < 0.20d &&
                result.KeyboardInteractionObserved &&
                result.TextOverflowCount == 0 &&
                result.ForbiddenUiMatchCount == 0 &&
                result.ScreenshotReviewed &&
                File.Exists(openResult.MainScenePath)
                    ? 0
                    : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunWindowSmoke(string workRoot)
    {
        try
        {
            var result = WindowSmoke.RunSmoke(workRoot);

            Console.WriteLine("Electron2D.Editor window smoke passed");
            Console.WriteLine($"WindowTitle={result.WindowTitle}");
            Console.WriteLine($"WindowCreated={result.WindowCreated}");
            Console.WriteLine($"WindowShown={result.WindowShown}");
            Console.WriteLine($"FramePresented={result.FramePresented}");
            Console.WriteLine($"EventPumpObserved={result.EventPumpObserved}");
            Console.WriteLine($"PointerInteractionObserved={result.PointerInteractionObserved}");
            Console.WriteLine($"KeyboardInteractionObserved={result.KeyboardInteractionObserved}");
            Console.WriteLine($"RuntimeControlTree={result.RuntimeControlTree}");
            Console.WriteLine($"VisualHarnessRemoved={result.VisualHarnessRemoved}");
            Console.WriteLine($"RuntimeUiRendering={result.RuntimeUiRendering}");
            Console.WriteLine($"RuntimeUiInputDispatch={result.RuntimeUiInputDispatch}");
            Console.WriteLine($"RenderSource={result.RenderSource}");
            Console.WriteLine($"InputDispatchSource={result.InputDispatchSource}");
            Console.WriteLine($"DrawCommands={result.DrawCommands}");
            Console.WriteLine($"RedDominantPixelRatio={result.RedDominantPixelRatio.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            Console.WriteLine($"SelectedWorkspace={result.SelectedWorkspace}");
            Console.WriteLine($"WindowSize={result.WindowWidth}x{result.WindowHeight}");
            Console.WriteLine($"WindowPixelSize={result.PixelWidth}x{result.PixelHeight}");
            Console.WriteLine($"VideoDriver={result.VideoDriver}");
            Console.WriteLine($"FrameCount={result.FrameCount}");
            Console.WriteLine($"TextOverflowCount={result.TextOverflowCount}");
            Console.WriteLine($"ClickableControlCount={result.ClickableControlCount}");
            Console.WriteLine($"ForbiddenUiMatches={result.ForbiddenUiMatchCount}");
            Console.WriteLine($"ScreenshotReviewed={result.ScreenshotReviewed}");
            Console.WriteLine($"ScreenshotPath={result.ScreenshotPath}");
            Console.WriteLine($"AnalysisPath={result.AnalysisPath}");

            return result.WindowCreated &&
                result.WindowShown &&
                result.FramePresented &&
                result.EventPumpObserved &&
                result.RuntimeControlTree &&
                result.VisualHarnessRemoved &&
                result.RuntimeUiRendering &&
                result.RuntimeUiInputDispatch &&
                string.Equals(result.RenderSource, RuntimeFrameRenderer.RenderSource, StringComparison.Ordinal) &&
                string.Equals(result.InputDispatchSource, RuntimeFrameRenderer.InputDispatchSource, StringComparison.Ordinal) &&
                result.DrawCommands >= 16 &&
                result.RedDominantPixelRatio < 0.20d &&
                result.PointerInteractionObserved &&
                result.KeyboardInteractionObserved &&
                result.TextOverflowCount == 0 &&
                result.ForbiddenUiMatchCount == 0 &&
                result.ScreenshotReviewed
                ? 0
                : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunScriptWorkspaceSmoke(string workRoot)
    {
        try
        {
            var result = ScriptWorkspaceSmoke.Run(workRoot);
            var snapshot = result.Snapshot;
            var document = snapshot.ActiveDocument;
            var surface = snapshot.EditorSurface;
            var search = snapshot.Search;
            var commands = snapshot.Commands;
            var textBuffer = snapshot.TextBuffer;
            var identity = snapshot.SnapshotIdentity;

            Console.WriteLine("Electron2D.Editor script workspace smoke passed");
            Console.WriteLine($"WorkspaceSwitcher={Join(snapshot.WorkspaceSwitcher)}");
            Console.WriteLine($"SelectedWorkspace={snapshot.SelectedWorkspace}");
            Console.WriteLine($"PrerequisiteManifestClosed={snapshot.PrerequisiteManifestClosed}");
            Console.WriteLine($"PrerequisiteManifest={Join(snapshot.PrerequisiteManifest)}");
            Console.WriteLine($"CreatedFile={snapshot.FileOperations.CreatedFile}");
            Console.WriteLine($"RenamedFile={snapshot.FileOperations.RenamedFile}");
            Console.WriteLine($"DeletedFile={snapshot.FileOperations.DeletedFile}");
            Console.WriteLine($"OpenTabs={Join(snapshot.DisplayTabs)}");
            Console.WriteLine($"ActiveTab={snapshot.Tabs.Single(tab => tab.IsActive).Path}");
            Console.WriteLine($"LineNumberCount={surface.LineNumberCount}");
            Console.WriteLine($"SyntaxTokens={Join(surface.SyntaxTokens)}");
            Console.WriteLine($"AutoIndentation={surface.AutoIndentation}");
            Console.WriteLine($"TabsSpaces={surface.TabsSpaces}");
            Console.WriteLine($"BracketMatching={surface.BracketMatching}");
            Console.WriteLine($"QuoteMatching={surface.QuoteMatching}");
            Console.WriteLine($"CodeFolding={surface.CodeFolding}");
            Console.WriteLine($"CurrentLine={surface.CurrentLine}");
            Console.WriteLine($"Caret={surface.CaretLine},{surface.CaretColumn}");
            Console.WriteLine($"Selection={surface.Selection}");
            Console.WriteLine($"SearchQuery={search.SearchQuery}");
            Console.WriteLine($"ReplacePreview={search.ReplacePreview}");
            Console.WriteLine($"ProjectSearchResults={search.ProjectSearchResults}");
            Console.WriteLine($"GoToLine={search.GoToLine}");
            Console.WriteLine($"ClipboardRoundTrip={commands.ClipboardRoundTrip}");
            Console.WriteLine($"UndoRedoRoundTrip={commands.UndoRedoRoundTrip}");
            Console.WriteLine($"SaveFile={commands.SaveFile}");
            Console.WriteLine($"SaveAll={commands.SaveAll}");
            Console.WriteLine($"DocumentId={document.DocumentId}");
            Console.WriteLine($"DocumentPath={document.Path}");
            Console.WriteLine($"DocumentRevision={document.Revision.Value}");
            Console.WriteLine($"PersistedRevision={document.PersistedRevision.Value}");
            Console.WriteLine($"DirtyState={document.IsDirty}");
            Console.WriteLine($"DiagnosticCount={document.Diagnostics.Count}");
            Console.WriteLine($"SemanticVersion={document.SemanticVersion}");
            Console.WriteLine($"CodeDocumentChangedEvents={textBuffer.CodeDocumentChangedEvents}");
            Console.WriteLine($"OperationJournalEntriesForTyping={textBuffer.OperationJournalEntriesForTyping}");
            Console.WriteLine($"TextBufferUndoAvailable={textBuffer.TextBufferUndoAvailable}");
            Console.WriteLine($"WorkspaceUndoGroupId={textBuffer.WorkspaceUndoGroupId}");
            Console.WriteLine($"AgentSaveConflictDiagnostic={textBuffer.AgentSaveConflictDiagnostic}");
            Console.WriteLine($"ExternalMergeResult={snapshot.ExternalChange.MergeResult}");
            Console.WriteLine($"ExternalConflictMarker={snapshot.ExternalChange.ConflictMarker}");
            Console.WriteLine($"InputSnapshotId={identity.InputSnapshotId}");
            Console.WriteLine($"InputWorkspaceRevision={identity.InputWorkspaceRevision.Value}");
            Console.WriteLine($"InputContentRevision={identity.InputContentRevision.Value}");
            Console.WriteLine($"InputBuildConfigurationHash={identity.InputBuildConfigurationHash}");
            Console.WriteLine($"ScreenshotReviewed={result.ScreenshotReviewed}");
            Console.WriteLine($"StatePath={result.StatePath}");
            Console.WriteLine($"ScreenshotPath={result.ScreenshotPath}");
            Console.WriteLine($"AnalysisPath={result.AnalysisPath}");

            return result.TextOverflowCount == 0 &&
                result.ForbiddenUiMatchCount == 0 &&
                result.ClickableControlCount >= 16 &&
                snapshot.PrerequisiteManifestClosed &&
                document.IsDirty &&
                textBuffer.OperationJournalEntriesForTyping == 0 &&
                textBuffer.TextBufferUndoAvailable &&
                snapshot.ExternalChange.ConflictMarker &&
                result.ScreenshotReviewed
                ? 0
                : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunProjectSettingsSmoke(string workRoot)
    {
        try
        {
            var result = ProjectSettingsSmoke.Run(workRoot);

            Console.WriteLine("Electron2D.Editor project settings smoke passed");
            Console.WriteLine($"ProjectPath={result.ProjectPath}");
            Console.WriteLine($"ProjectSettingsPath={result.ProjectSettingsPath}");
            Console.WriteLine($"ExportPresetsPath={result.ExportPresetsPath}");
            Console.WriteLine($"MainScene={result.MainScene}");
            Console.WriteLine($"RendererProfile={result.RendererProfile}");
            Console.WriteLine($"PhysicsTicksPerSecond={result.PhysicsTicksPerSecond}");
            Console.WriteLine($"DisplaySize={result.DisplaySize}");
            Console.WriteLine($"Fullscreen={result.Fullscreen}");
            Console.WriteLine($"InputActions={result.InputActions}");
            Console.WriteLine($"ExportPresets={result.ExportPresets}");
            Console.WriteLine($"ProjectSettingsWritten={result.ProjectSettingsWritten}");
            Console.WriteLine($"InputMapRoundTrip={result.InputMapRoundTrip}");
            Console.WriteLine($"ExportPresetsRoundTrip={result.ExportPresetsRoundTrip}");
            Console.WriteLine($"WindowCreated={result.WindowCreated}");
            Console.WriteLine($"WindowShown={result.WindowShown}");
            Console.WriteLine($"FramePresented={result.FramePresented}");
            Console.WriteLine($"EventPumpObserved={result.EventPumpObserved}");
            Console.WriteLine($"PointerInteractionObserved={result.PointerInteractionObserved}");
            Console.WriteLine($"KeyboardInteractionObserved={result.KeyboardInteractionObserved}");
            Console.WriteLine($"TextOverflowCount={result.TextOverflowCount}");
            Console.WriteLine($"ClickableControlCount={result.ClickableControlCount}");
            Console.WriteLine($"ForbiddenUiMatches={result.ForbiddenUiMatchCount}");
            Console.WriteLine($"ScreenshotReviewed={result.ScreenshotReviewed}");
            Console.WriteLine($"ScreenshotPath={result.ScreenshotPath}");
            Console.WriteLine($"AnalysisPath={result.AnalysisPath}");

            return result.ProjectSettingsWritten &&
                result.InputMapRoundTrip &&
                result.ExportPresetsRoundTrip &&
                result.WindowCreated &&
                result.WindowShown &&
                result.FramePresented &&
                result.PointerInteractionObserved &&
                result.KeyboardInteractionObserved &&
                result.TextOverflowCount == 0 &&
                result.ForbiddenUiMatchCount == 0 &&
                result.ScreenshotReviewed
                ? 0
                : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunSpecializedEditorsSmoke(string workRoot)
    {
        try
        {
            var result = SpecializedEditorsSmoke.Run(workRoot);

            Console.WriteLine("Electron2D.Editor specialized editors smoke passed");
            Console.WriteLine($"ProjectPath={result.ProjectPath}");
            Console.WriteLine($"ProjectSettingsPath={result.ProjectSettingsPath}");
            Console.WriteLine($"SpriteFramesPath={result.SpriteFramesPath}");
            Console.WriteLine($"TileSetPath={result.TileSetPath}");
            Console.WriteLine($"AnimationPath={result.AnimationPath}");
            Console.WriteLine($"ScenePath={result.ScenePath}");
            Console.WriteLine($"SpriteAnimations={result.SpriteAnimations}");
            Console.WriteLine($"TileMapUsedRect={result.TileMapUsedRect}");
            Console.WriteLine($"AnimationTracks={result.AnimationTracks}");
            Console.WriteLine($"SpriteFramesRoundTrip={result.SpriteFramesRoundTrip}");
            Console.WriteLine($"TileMapRoundTrip={result.TileMapRoundTrip}");
            Console.WriteLine($"AnimationTimelineRoundTrip={result.AnimationTimelineRoundTrip}");
            Console.WriteLine($"SceneRoundTrip={result.SceneRoundTrip}");
            Console.WriteLine($"WindowCreated={result.WindowCreated}");
            Console.WriteLine($"WindowShown={result.WindowShown}");
            Console.WriteLine($"FramePresented={result.FramePresented}");
            Console.WriteLine($"EventPumpObserved={result.EventPumpObserved}");
            Console.WriteLine($"PointerInteractionObserved={result.PointerInteractionObserved}");
            Console.WriteLine($"KeyboardInteractionObserved={result.KeyboardInteractionObserved}");
            Console.WriteLine($"TextOverflowCount={result.TextOverflowCount}");
            Console.WriteLine($"ClickableControlCount={result.ClickableControlCount}");
            Console.WriteLine($"ForbiddenUiMatches={result.ForbiddenUiMatches}");
            Console.WriteLine($"ScreenshotReviewed={result.ScreenshotReviewed}");
            Console.WriteLine($"ScreenshotPath={result.ScreenshotPath}");
            Console.WriteLine($"AnalysisPath={result.AnalysisPath}");

            return result.SpriteFramesRoundTrip &&
                result.TileMapRoundTrip &&
                result.AnimationTimelineRoundTrip &&
                result.SceneRoundTrip &&
                result.WindowCreated &&
                result.WindowShown &&
                result.FramePresented &&
                result.PointerInteractionObserved &&
                result.KeyboardInteractionObserved &&
                result.TextOverflowCount == 0 &&
                result.ForbiddenUiMatches == 0 &&
                result.ScreenshotReviewed
                ? 0
                : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunScriptLanguageServicesSmoke(string workRoot)
    {
        try
        {
            var result = ScriptLanguageServicesSmoke.Run(workRoot);
            var language = result.LanguageServices;
            var diagnostic = language.LiveDiagnostic;

            Console.WriteLine("Electron2D.Editor script language services smoke passed");
            Console.WriteLine($"AssemblyBoundary={typeof(Electron2D.CSharpLanguageServices.CSharpLanguageService).Assembly.GetName().Name}");
            Console.WriteLine($"RoslynSemanticModel={language.RoslynSemanticModel}");
            Console.WriteLine($"RuntimeAssemblyContainsLanguageServices={language.RuntimeAssemblyContainsLanguageServices}");
            Console.WriteLine($"EditorUiContainsLanguageServices={language.EditorUiContainsLanguageServices}");
            Console.WriteLine($"WorkspaceSnapshotUsedForIde={language.WorkspaceSnapshotUsedForIde}");
            Console.WriteLine($"ProjectId={language.Identity.ProjectId}");
            Console.WriteLine($"DocumentId={language.Identity.DocumentId}");
            Console.WriteLine($"DocumentRevision={language.Identity.DocumentRevision}");
            Console.WriteLine($"SemanticVersion={language.Identity.SemanticVersion}");
            Console.WriteLine($"ConfigurationHash={language.Identity.ConfigurationHash}");
            Console.WriteLine($"CompletionContainsElectron2DApi={language.Completion.Electron2DApiAvailable}");
            Console.WriteLine($"CompletionContainsLocalSymbol={language.Completion.LocalSymbolAvailable}");
            Console.WriteLine($"CompletionSelectedItem={language.Completion.SelectedItem}");
            Console.WriteLine($"SignatureHelpDisplay={language.SignatureHelp.Display}");
            Console.WriteLine($"SignatureHelpActiveParameter={language.SignatureHelp.ActiveParameter}");
            Console.WriteLine($"HoverSymbol={language.Hover.SymbolDisplay}");
            Console.WriteLine($"HoverDocumentationContainsXmlSummary={language.Hover.DocumentationSummary.Contains("Moves hero", StringComparison.Ordinal)}");
            Console.WriteLine($"LiveDiagnosticCode={diagnostic.Code}");
            Console.WriteLine($"LiveDiagnosticSeverity={diagnostic.Severity}");
            Console.WriteLine($"LiveDiagnosticPath={diagnostic.Path}");
            Console.WriteLine($"LiveDiagnosticLine={diagnostic.Line}");
            Console.WriteLine($"LiveDiagnosticColumn={diagnostic.Column}");
            Console.WriteLine($"DefinitionTarget={language.Definition}");
            Console.WriteLine($"ReferencesCount={language.References.References.Count}");
            Console.WriteLine($"RenameEditCount={language.Rename.Edits.Count}");
            Console.WriteLine($"RenameExpectedRevision={language.Rename.ExpectedRevision}");
            Console.WriteLine($"FormattingChanged={language.Formatting.Changed}");
            Console.WriteLine($"CodeActionTitle={language.CodeAction.Title}");
            Console.WriteLine($"StaleResponseDiscarded={language.StaleResponseDiscarded}");
            Console.WriteLine($"PreviousRequestCancelled={language.PreviousRequestCancelled}");
            Console.WriteLine($"DiagnosticsDebounceMs={language.DiagnosticsDebounceMs}");
            Console.WriteLine($"ReloadTrigger={language.ReloadTrigger}");
            Console.WriteLine($"SemanticFailureDiagnosticCode={language.SemanticFailureDiagnostic.Code}");
            Console.WriteLine($"ScreenshotReviewed={result.ScreenshotReviewed}");
            Console.WriteLine($"StatePath={result.StatePath}");
            Console.WriteLine($"ScreenshotPath={result.ScreenshotPath}");
            Console.WriteLine($"AnalysisPath={result.AnalysisPath}");

            return language.RoslynSemanticModel &&
                language.Completion.Electron2DApiAvailable &&
                language.Completion.LocalSymbolAvailable &&
                string.Equals(language.Completion.SelectedItem, "Sprite2D", StringComparison.Ordinal) &&
                string.Equals(diagnostic.Code, "CS0103", StringComparison.Ordinal) &&
                language.StaleResponseDiscarded &&
                result.TextOverflowCount == 0 &&
                result.ForbiddenUiMatchCount == 0 &&
                result.ClickableControlCount >= 16 &&
                result.ScreenshotReviewed
                    ? 0
                    : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunManagedDebuggerSmoke(string workRoot)
    {
        try
        {
            var result = ManagedDebuggerSmoke.Run(workRoot);
            var state = result.State;
            var breakpoint = state.Breakpoint;
            var argument = state.Arguments.Single(variable => variable.Name == "delta");
            var local = state.Locals.Single(variable => variable.Name == "speed");
            var watch = state.Watches.Single();

            Console.WriteLine("Electron2D.Editor managed debugger smoke passed");
            Console.WriteLine($"ManagedDebuggerAssembly={state.GetType().Assembly.GetName().Name}");
            Console.WriteLine($"AdapterId={state.Adapter.AdapterId}");
            Console.WriteLine($"AdapterReleaseTag={state.Adapter.ReleaseTag}");
            Console.WriteLine($"DapBoundary={state.Adapter.Boundary}");
            Console.WriteLine($"AdapterArguments={Join(state.Adapter.Arguments)}");
            Console.WriteLine($"DapInitialize={state.DapTranscript.Has("initialize")}");
            Console.WriteLine($"DapLaunch={state.DapTranscript.Has("launch")}");
            Console.WriteLine($"DapAttach={state.DapTranscript.Has("attach")}");
            Console.WriteLine($"DapSetBreakpoints={state.DapTranscript.Has("setBreakpoints")}");
            Console.WriteLine($"DapStoppedBreakpoint={state.DapTranscript.Has("stopped:breakpoint")}");
            Console.WriteLine($"DapThreads={state.DapTranscript.Has("threads")}");
            Console.WriteLine($"DapStackTrace={state.DapTranscript.Has("stackTrace")}");
            Console.WriteLine($"DapScopes={state.DapTranscript.Has("scopes")}");
            Console.WriteLine($"DapVariables={state.DapTranscript.Has("variables")}");
            Console.WriteLine($"DapPause={state.DapTranscript.Has("pause")}");
            Console.WriteLine($"DapContinue={state.DapTranscript.Has("continue")}");
            Console.WriteLine($"DapStepInto={state.DapTranscript.Has("stepIn")}");
            Console.WriteLine($"DapStepOver={state.DapTranscript.Has("next")}");
            Console.WriteLine($"DapStepOut={state.DapTranscript.Has("stepOut")}");
            Console.WriteLine($"RestartStrategy={state.Adapter.RestartStrategy}");
            Console.WriteLine($"BreakpointId={breakpoint.BreakpointId}");
            Console.WriteLine($"BreakpointDocumentId={breakpoint.DocumentId}");
            Console.WriteLine($"BreakpointSourceAnchor={breakpoint.SourceAnchor}");
            Console.WriteLine($"BreakpointEnabled={breakpoint.Enabled}");
            Console.WriteLine($"BreakpointVerified={breakpoint.Verified}");
            Console.WriteLine($"BreakpointResolvedLine={breakpoint.ResolvedLine}");
            Console.WriteLine($"BreakpointResolvedColumn={breakpoint.ResolvedColumn}");
            Console.WriteLine($"BreakpointLastBoundSnapshotId={breakpoint.LastBoundSnapshotId}");
            Console.WriteLine($"BreakpointAdapterMessage={breakpoint.AdapterMessage}");
            Console.WriteLine($"BreakpointPersisted={state.BreakpointPersisted}");
            Console.WriteLine($"BreakpointSurvivesRestart={state.BreakpointSurvivesRestart}");
            Console.WriteLine($"BreakpointExcludedFromSnapshot={state.BreakpointExcludedFromSnapshot}");
            Console.WriteLine($"BreakpointRenamedPath={state.RenamedBreakpoint.SourceAnchor.Path}");
            Console.WriteLine($"BreakpointRebasedLine={state.RebasedBreakpoint.SourceAnchor.Line}");
            Console.WriteLine($"AmbiguousBreakpointVerified={state.AmbiguousBreakpoint.Verified}");
            Console.WriteLine($"DebugBuildPortablePdb={state.DebugBuildPortablePdb}");
            Console.WriteLine($"SnapshotId={state.SnapshotId}");
            Console.WriteLine($"AttachedProcessId={state.AttachedProcessId}");
            Console.WriteLine($"CurrentExecutionLine={state.CurrentExecutionLine}");
            Console.WriteLine($"ThreadCount={state.Threads.Count}");
            Console.WriteLine($"SelectedFrame={state.StackFrames[0].Display}");
            Console.WriteLine($"ArgumentValue={argument.Name}={argument.Value}");
            Console.WriteLine($"LocalValue={local.Name}={local.Value}");
            Console.WriteLine($"WatchExpression={watch.Expression}");
            Console.WriteLine($"WatchValue={watch.Value}");
            Console.WriteLine($"ExceptionType={state.Exception.Type}");
            Console.WriteLine($"StaleAfterCodeEdit={state.StaleAfterCodeEdit}");
            Console.WriteLine($"RemoteAndroidIosExcluded={state.RemoteAndroidIosExcluded}");
            Console.WriteLine($"RemoteWebAssemblyExcluded={state.RemoteWebAssemblyExcluded}");
            Console.WriteLine($"ScreenshotReviewed={result.ScreenshotReviewed}");
            Console.WriteLine($"StatePath={result.StatePath}");
            Console.WriteLine($"ScreenshotPath={result.ScreenshotPath}");
            Console.WriteLine($"AnalysisPath={result.AnalysisPath}");

            return state.Adapter.AdapterId == "netcoredbg" &&
                state.DapTranscript.Has("initialize") &&
                state.DapTranscript.Has("launch") &&
                state.DapTranscript.Has("attach") &&
                state.DapTranscript.Has("stopped:breakpoint") &&
                breakpoint.Verified &&
                state.BreakpointPersisted &&
                state.BreakpointSurvivesRestart &&
                state.BreakpointExcludedFromSnapshot &&
                state.DebugBuildPortablePdb &&
                state.Threads.Count >= 2 &&
                state.Locals.Count > 0 &&
                state.Arguments.Count > 0 &&
                state.Watches.Count > 0 &&
                state.StaleAfterCodeEdit &&
                state.RemoteAndroidIosExcluded &&
                state.RemoteWebAssemblyExcluded &&
                result.TextOverflowCount == 0 &&
                result.ForbiddenUiMatchCount == 0 &&
                result.ClickableControlCount >= 18 &&
                result.ScreenshotReviewed
                    ? 0
                    : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunScriptDebugToolingSmoke(string workRoot)
    {
        try
        {
            var result = ScriptDebugToolingSmoke.Run(workRoot);
            var diagnostic = result.Diagnostics.Diagnostic;
            var completion = result.Completions.CompletionItems.Single(item => item.IsSelected);
            var local = result.Locals.Variables.Single(variable => variable.Name == "speed");
            var argument = result.Arguments.Variables.Single(variable => variable.Name == "delta");
            var watchDefinition = result.WatchDefinitions.Watches.Single(watch => watch.Expression == "hero.Health");
            var watchEvaluation = result.WatchEvaluations.Watches.Single(watch => watch.Expression == "hero.Health");

            Console.WriteLine("Electron2D.Editor script/debug tooling smoke passed");
            Console.WriteLine($"SelectedWorkspace={result.SelectedWorkspace}");
            Console.WriteLine($"ScriptOperation={result.ScriptMutation.Operation.OperationKind}");
            Console.WriteLine($"DiagnosticCode={diagnostic?.Code}");
            Console.WriteLine($"CompletionSelected={completion.DisplayText}");
            Console.WriteLine($"BreakpointId={result.Breakpoint.Breakpoint?.BreakpointId}");
            Console.WriteLine($"ThreadCount={result.DebugSession.Threads.Count}");
            Console.WriteLine($"StackThreadCount={result.Stack.StacksByThread.Count}");
            Console.WriteLine($"LocalValue={local.Name}={local.Value}");
            Console.WriteLine($"ArgumentValue={argument.Name}={argument.Value}");
            Console.WriteLine($"WatchDefinition={watchDefinition.Expression}");
            Console.WriteLine($"WatchEvaluation={watchEvaluation.Expression}={watchEvaluation.Value}");
            Console.WriteLine($"CurrentTask={result.CurrentTask}");
            Console.WriteLine($"LinkedTransactions={Join(result.LinkedTransactions)}");
            Console.WriteLine($"LinkedJobs={Join(result.LinkedJobs)}");
            Console.WriteLine($"LinkedArtifacts={Join(result.LinkedArtifacts)}");
            Console.WriteLine($"ScreenshotReviewed={result.ScreenshotReviewed}");
            Console.WriteLine($"StatePath={result.StatePath}");
            Console.WriteLine($"ScreenshotPath={result.ScreenshotPath}");
            Console.WriteLine($"AnalysisPath={result.AnalysisPath}");

            return result.ScriptMutation.Succeeded &&
                string.Equals(diagnostic?.Code, "CS0103", StringComparison.Ordinal) &&
                string.Equals(completion.DisplayText, "Sprite2D", StringComparison.Ordinal) &&
                result.Breakpoint.Breakpoint is not null &&
                result.DebugSession.Threads.Count >= 2 &&
                result.Stack.StacksByThread.Count >= 2 &&
                string.Equals(local.Value, "240", StringComparison.Ordinal) &&
                string.Equals(argument.Value, "0.016", StringComparison.Ordinal) &&
                string.Equals(watchEvaluation.Value, "100", StringComparison.Ordinal) &&
                result.TextOverflowCount == 0 &&
                result.ForbiddenUiMatchCount == 0 &&
                result.ClickableControlCount >= 24 &&
                result.ScreenshotReviewed
                    ? 0
                    : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunRunWorkflowSmoke(string workRoot)
    {
        try
        {
            var result = RunWorkflowSmoke.Run(workRoot);

            Console.WriteLine("Electron2D.Editor run workflow smoke passed");
            Console.WriteLine($"ProjectPath={result.ProjectPath}");
            Console.WriteLine($"MainScenePath={result.MainScenePath}");
            Console.WriteLine($"AlternateScenePath={result.AlternateScenePath}");
            Console.WriteLine($"BuildDiagnosticCount={result.BuildDiagnosticCount}");
            Console.WriteLine($"BuildFirstCode={result.BuildFirstCode}");
            Console.WriteLine($"BuildFirstLine={result.BuildFirstLine}");
            Console.WriteLine($"BuildFirstColumn={result.BuildFirstColumn}");
            Console.WriteLine($"BuildFailureStartedProcess={result.BuildFailureStartedProcess}");
            Console.WriteLine($"ProjectRunExitCode={result.ProjectRunExitCode}");
            Console.WriteLine($"ProjectRunScene={result.ProjectRunScene}");
            Console.WriteLine($"CurrentSceneRunExitCode={result.CurrentSceneRunExitCode}");
            Console.WriteLine($"CurrentSceneRunScene={result.CurrentSceneRunScene}");
            Console.WriteLine($"CurrentSceneOverrideStable={result.CurrentSceneOverrideStable}");
            Console.WriteLine($"OutputContainsProjectRun={result.OutputContainsProjectRun}");
            Console.WriteLine($"OutputContainsCurrentSceneRun={result.OutputContainsCurrentSceneRun}");
            Console.WriteLine($"OutputLineCount={result.OutputLineCount}");
            Console.WriteLine($"RuntimeFailureExitCode={result.RuntimeFailureExitCode}");
            Console.WriteLine($"RuntimeStackTraceContains={result.RuntimeStackTraceContains}");
            Console.WriteLine($"ShaderDiagnosticCount={result.ShaderDiagnosticCount}");
            Console.WriteLine($"ShaderDiagnosticFile={result.ShaderDiagnosticFile}");
            Console.WriteLine($"ShaderDiagnosticLine={result.ShaderDiagnosticLine}");
            Console.WriteLine($"ShaderDiagnosticColumn={result.ShaderDiagnosticColumn}");
            Console.WriteLine($"StopRequested={result.StopRequested}");
            Console.WriteLine($"StopObserved={result.StopObserved}");
            Console.WriteLine($"RepeatedRunStopCycles={result.RepeatedRunStopCycles}");
            Console.WriteLine($"ActiveSessionAfterStop={result.ActiveSessionAfterStop}");
            Console.WriteLine($"FrameSamples={result.FrameSamples}");
            Console.WriteLine($"LastFrameTimeMs={Format(result.LastFrameTimeMs)}");
            Console.WriteLine($"AverageFrameTimeMs={Format(result.AverageFrameTimeMs)}");
            Console.WriteLine($"FramesPerSecond={Format(result.FramesPerSecond)}");

            return result.ProjectRunExitCode == 0 &&
                result.CurrentSceneRunExitCode == 0 &&
                result.RuntimeFailureExitCode == 1 &&
                !result.BuildFailureStartedProcess &&
                result.CurrentSceneOverrideStable &&
                result.OutputContainsProjectRun &&
                result.OutputContainsCurrentSceneRun &&
                result.RuntimeStackTraceContains &&
                result.ShaderDiagnosticCount > 0 &&
                result.StopRequested &&
                result.StopObserved &&
                result.RepeatedRunStopCycles == 3 &&
                !result.ActiveSessionAfterStop &&
                result.FrameSamples > 0
                ? 0
                : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunShellLayoutSmoke(string workRoot)
    {
        try
        {
            var result = ShellSmoke.Run(workRoot);

            Console.WriteLine("Electron2D.Editor shell layout smoke passed");
            Console.WriteLine($"MenuItems={Join(result.MenuItems)}");
            Console.WriteLine($"WorkspaceSwitcher={Join(result.WorkspaceSwitcher)}");
            Console.WriteLine($"LeftDocks={Join(result.LeftDocks)}");
            Console.WriteLine($"RightDocks={Join(result.RightDocks)}");
            Console.WriteLine($"BottomPanelTabs={Join(result.BottomPanelTabs)}");
            Console.WriteLine($"SelectedWorkspace={result.SelectedWorkspace}");
            Console.WriteLine($"BottomPanelCollapseRoundTrip={result.BottomPanelCollapseRoundTrip}");
            Console.WriteLine($"PersistenceRoundTripStable={result.PersistenceRoundTripStable}");
            Console.WriteLine($"WorkspaceStateRoundTripStable={result.WorkspaceStateRoundTripStable}");
            Console.WriteLine($"ForbiddenUiMatches={result.ForbiddenUiMatches}");
            Console.WriteLine($"ForbiddenShortcutMatches={result.ForbiddenShortcutMatches}");
            Console.WriteLine("VisualHarnessPresent=False");
            Console.WriteLine($"ScreenshotReviewed={result.ScreenshotReviewed}");
            Console.WriteLine($"TwoDSelection={result.TwoDSelection}");
            Console.WriteLine($"TwoDScroll={result.TwoDScroll}");
            Console.WriteLine($"TwoDZoom={result.TwoDZoom}");
            Console.WriteLine($"ScriptDocuments={Join(result.ScriptDocuments)}");
            Console.WriteLine($"GameDocuments={Join(result.GameDocuments)}");
            Console.WriteLine($"TasksDocuments={Join(result.TasksDocuments)}");
            Console.WriteLine($"StatePath={result.StatePath}");
            Console.WriteLine($"AnalysisPath={result.AnalysisPath}");

            return result.BottomPanelCollapseRoundTrip &&
                result.PersistenceRoundTripStable &&
                result.WorkspaceStateRoundTripStable &&
                result.ForbiddenUiMatches == 0 &&
                result.ForbiddenShortcutMatches == 0 &&
                result.ScreenshotReviewed
                ? 0
                : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunAgentWorkspacePanelSmoke(string workRoot)
    {
        try
        {
            var result = AgentWorkspacePanelSmoke.Run(workRoot);
            var snapshot = result.Snapshot;
            var job = snapshot.ActiveJob;
            var input = job.InputIdentity;

            Console.WriteLine("Electron2D.Editor agent workspace panel smoke passed");
            Console.WriteLine($"AgentSessionId={snapshot.Session.AgentSessionId}");
            Console.WriteLine($"ProfileId={snapshot.Session.ProfileId}");
            Console.WriteLine($"ConnectionState={snapshot.Session.ConnectionState}");
            Console.WriteLine($"HandshakeState={snapshot.Session.HandshakeState}");
            Console.WriteLine($"Route={snapshot.Session.Route}");
            Console.WriteLine($"LastAction={snapshot.Session.LastAction}");
            Console.WriteLine($"CurrentTask={snapshot.Task.TaskId}");
            Console.WriteLine($"TaskStatus={snapshot.Task.Status}");
            Console.WriteLine($"AcceptanceState={snapshot.Task.AcceptanceState}");
            Console.WriteLine($"LinkedTransactions={Join(snapshot.Task.LinkedTransactions)}");
            Console.WriteLine($"LinkedJobs={Join(snapshot.Task.LinkedJobs)}");
            Console.WriteLine($"LinkedDiagnostics={Join(snapshot.Task.LinkedDiagnostics)}");
            Console.WriteLine($"LinkedArtifacts={Join(snapshot.Task.LinkedArtifacts)}");
            Console.WriteLine($"ChangedObjects={Join(snapshot.ChangedObjects.Select(FormatChangedObject))}");
            Console.WriteLine($"DiagnosticFields={Join(snapshot.DiagnosticFields)}");
            Console.WriteLine($"JobKind={job.Kind}");
            Console.WriteLine($"JobState={job.State}");
            Console.WriteLine($"JobProgressPercent={job.ProgressPercent}");
            Console.WriteLine($"CanCancel={job.CanCancel}");
            Console.WriteLine($"InputSnapshotId={input.InputSnapshotId}");
            Console.WriteLine($"InputWorkspaceRevision={input.InputWorkspaceRevision.Value}");
            Console.WriteLine($"InputContentRevision={input.InputContentRevision.Value}");
            Console.WriteLine($"InputBuildConfigurationHash={input.InputBuildConfigurationHash}");
            Console.WriteLine($"StaleMarkers={Join(job.StaleMarkers)}");
            Console.WriteLine($"GroupedUndoAvailable={snapshot.GroupedUndoAvailable}");
            Console.WriteLine($"UndoGroupId={snapshot.UndoGroupId}");
            Console.WriteLine($"AwaitingAcceptanceActionAvailable={snapshot.AwaitingAcceptanceActionAvailable}");
            Console.WriteLine($"DoneActionAvailable={snapshot.DoneActionAvailable}");
            Console.WriteLine($"DockPlacement={snapshot.DockState.Placement}");
            Console.WriteLine($"DockPersisted={snapshot.DockState.Persisted}");
            Console.WriteLine($"VisibleWorkspaces={Join(snapshot.DockState.VisibleWorkspaces)}");
            Console.WriteLine($"ScreenshotReviewed={result.ScreenshotReviewed}");
            Console.WriteLine($"StatePath={result.StatePath}");
            Console.WriteLine($"ScreenshotPath={result.ScreenshotPath}");
            Console.WriteLine($"AnalysisPath={result.AnalysisPath}");

            return result.TextOverflowCount == 0 &&
                result.ForbiddenActionMatchCount == 0 &&
                snapshot.AwaitingAcceptanceActionAvailable &&
                !snapshot.DoneActionAvailable &&
                snapshot.GroupedUndoAvailable &&
                result.ScreenshotReviewed
                ? 0
                : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunProjectTasksBoardSmoke(string workRoot)
    {
        try
        {
            var result = ProjectTasksBoardSmoke.Run(workRoot);
            var snapshot = result.Snapshot;
            var selected = snapshot.SelectedTask;
            var details = snapshot.Details;

            Console.WriteLine("Electron2D.Editor project tasks board smoke passed");
            Console.WriteLine($"WorkspaceSwitcher={Join(snapshot.WorkspaceSwitcher)}");
            Console.WriteLine($"SelectedWorkspace={snapshot.SelectedWorkspace}");
            Console.WriteLine($"Columns={Join(snapshot.Columns.Select(column => column.Label))}");
            Console.WriteLine($"TaskIds={Join(snapshot.VisibleTaskIds)}");
            Console.WriteLine($"SelectedTaskId={selected.TaskId}");
            Console.WriteLine($"SelectedTaskTitle={selected.Title}");
            Console.WriteLine($"SelectedTaskPriority={selected.Priority}");
            Console.WriteLine($"SelectedTaskLabels={Join(selected.Labels)}");
            Console.WriteLine($"SelectedTaskAssignee={selected.Assignee}");
            Console.WriteLine($"SelectedTaskReadiness={selected.Readiness}");
            Console.WriteLine($"ManualBlockingReasons={Join(snapshot.ManualBlockingReasons)}");
            Console.WriteLine($"DependencyBlockingReasons={Join(snapshot.DependencyBlockingReasons)}");
            Console.WriteLine($"InspectorTitle={details.InspectorTitle}");
            Console.WriteLine($"DescriptionVisible={details.DescriptionVisible}");
            Console.WriteLine($"AcceptanceCriteriaVisible={details.AcceptanceCriteriaVisible}");
            Console.WriteLine($"SubtasksVisible={details.SubtasksVisible}");
            Console.WriteLine($"ActivityKinds={Join(details.ActivityKinds.Select(kind => kind.ToString()))}");
            Console.WriteLine($"LinkedTransactions={Join(details.LinkedTransactions)}");
            Console.WriteLine($"LinkedJobs={Join(details.LinkedJobs)}");
            Console.WriteLine($"LinkedDiagnostics={Join(details.LinkedDiagnostics)}");
            Console.WriteLine($"LinkedArtifacts={Join(details.LinkedArtifacts)}");
            Console.WriteLine($"LinkedObjects={Join(details.LinkedObjects)}");
            Console.WriteLine($"DragDropIntent={FormatTaskDragDrop(snapshot.DragDropIntent)}");
            Console.WriteLine($"DragDropAllowed={snapshot.DragDropIntent.Allowed}");
            Console.WriteLine($"RejectedDropDiagnostic={snapshot.DragDropIntent.RejectedDiagnosticCode}");
            Console.WriteLine($"RankRoundTripStable={snapshot.RankRoundTripStable}");
            Console.WriteLine($"ArchiveViewAvailable={snapshot.ArchiveViewAvailable}");
            Console.WriteLine($"ArchivedHiddenFromBoard={snapshot.ArchivedHiddenFromBoard}");
            Console.WriteLine($"HardDeleteRequiresConfirmation={snapshot.HardDeleteRequiresConfirmation}");
            Console.WriteLine($"HumanAcceptActionUsesTrustedContext={snapshot.HumanAcceptActionUsesTrustedContext}");
            Console.WriteLine($"AgentAcceptActionAvailable={snapshot.AgentAcceptActionAvailable}");
            Console.WriteLine($"ReviewStatesDiffer={snapshot.ReviewStatesDiffer}");
            Console.WriteLine($"Filters={Join(snapshot.Filters)}");
            Console.WriteLine($"WorkspaceEventRevision={snapshot.WorkspaceEventRevision.Value}");
            Console.WriteLine($"WorksWithoutAi={snapshot.WorksWithoutAi}");
            Console.WriteLine($"ScreenshotReviewed={result.ScreenshotReviewed}");
            Console.WriteLine($"StatePath={result.StatePath}");
            Console.WriteLine($"ScreenshotPath={result.ScreenshotPath}");
            Console.WriteLine($"AnalysisPath={result.AnalysisPath}");

            return result.TextOverflowCount == 0 &&
                result.ForbiddenUiMatchCount == 0 &&
                result.ClickableControlCount >= 16 &&
                snapshot.RankRoundTripStable &&
                snapshot.ArchiveViewAvailable &&
                snapshot.ArchivedHiddenFromBoard &&
                snapshot.HardDeleteRequiresConfirmation &&
                snapshot.HumanAcceptActionUsesTrustedContext &&
                !snapshot.AgentAcceptActionAvailable &&
                snapshot.WorksWithoutAi &&
                result.ScreenshotReviewed
                ? 0
                : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static string Format(Electron2D.Vector2 value)
    {
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{Format(value.X)},{Format(value.Y)}");
    }

    private static string Format(Electron2D.Rect2 value)
    {
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{Format(value.Position.X)},{Format(value.Position.Y)},{Format(value.Size.X)},{Format(value.Size.Y)}");
    }

    private static string Format(float value)
    {
        return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Format(double value)
    {
        return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Join(IEnumerable<string> values)
    {
        return string.Join('|', values);
    }

    private static string FormatChangedObject(AgentWorkspaceChangedObject changedObject)
    {
        return $"{AgentWorkspaceVisualHarness.KindPrefix(changedObject.Kind)}:{changedObject.NavigationTarget}";
    }

    private static string FormatTaskDragDrop(ProjectTasksDragDropIntent intent)
    {
        return $"{intent.TaskId}:{intent.SourceStatus}->{intent.TargetStatus}@{intent.TargetRank}";
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "data", "templates", "electron2d-empty")) &&
                File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        while (workingDirectory is not null)
        {
            if (Directory.Exists(Path.Combine(workingDirectory.FullName, "data", "templates", "electron2d-empty")) &&
                File.Exists(Path.Combine(workingDirectory.FullName, "src", "Electron2D.sln")))
            {
                return workingDirectory.FullName;
            }

            workingDirectory = workingDirectory.Parent;
        }

        throw new InvalidOperationException("Electron2D repository root was not found.");
    }
}
