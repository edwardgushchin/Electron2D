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
using Electron2D.Editor.FileSystemDock;
using Electron2D.Editor.ProjectManagement;
using Electron2D.Editor.Run;
using Electron2D.Editor.Scripting;
using Electron2D.Editor.SceneTreeDock;
using Electron2D.Editor.Viewport2D;

namespace Electron2D.Editor;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            return RunOnce(isSmoke: false);
        }

        if (args is ["--smoke"])
        {
            return RunOnce(isSmoke: true);
        }

        if (args is ["--project-manager-smoke", var workRoot, "--user-data-dir", var userDataRoot])
        {
            return RunProjectManagerSmoke(workRoot, userDataRoot);
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

        if (args is ["--run-workflow-smoke", var runWorkflowWorkRoot])
        {
            return RunRunWorkflowSmoke(runWorkflowWorkRoot);
        }

        Console.Error.WriteLine("Usage: Electron2D.Editor [--smoke] [--project-manager-smoke <work-root> --user-data-dir <user-data-dir>] [--scene-tree-dock-smoke <work-root>] [--viewport-2d-smoke <work-root>] [--inspector-smoke <work-root>] [--file-system-dock-smoke <work-root>] [--script-workflow-smoke <work-root>] [--run-workflow-smoke <work-root>]");
        return 2;
    }

    private static int RunOnce(bool isSmoke)
    {
        var application = new EditorApplication();
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
            var manager = new EditorProjectManager(templateRoot);
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
            var result = EditorSceneTreeDockSmoke.Run(workRoot);

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
            var result = EditorViewport2DSmoke.Run(workRoot);

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
            var result = EditorInspectorSmoke.Run(workRoot);

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
            var result = EditorFileSystemDockSmoke.Run(workRoot);

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
            var result = EditorScriptWorkflowSmoke.Run(workRoot);

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

    private static int RunRunWorkflowSmoke(string workRoot)
    {
        try
        {
            var result = EditorRunWorkflowSmoke.Run(workRoot);

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
