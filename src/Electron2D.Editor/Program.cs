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
using Electron2D.Editor.ProjectManagement;
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

        Console.Error.WriteLine("Usage: Electron2D.Editor [--smoke] [--project-manager-smoke <work-root> --user-data-dir <user-data-dir>] [--scene-tree-dock-smoke <work-root>] [--viewport-2d-smoke <work-root>]");
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
            var templateRoot = Path.Combine(FindRepositoryRoot(), "templates", "electron2d-empty");
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "templates", "electron2d-empty")) &&
                File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        while (workingDirectory is not null)
        {
            if (Directory.Exists(Path.Combine(workingDirectory.FullName, "templates", "electron2d-empty")) &&
                File.Exists(Path.Combine(workingDirectory.FullName, "src", "Electron2D.sln")))
            {
                return workingDirectory.FullName;
            }

            workingDirectory = workingDirectory.Parent;
        }

        throw new InvalidOperationException("Electron2D repository root was not found.");
    }
}
