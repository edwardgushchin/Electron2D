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
using System.Xml.Linq;

using Electron2D.Editor.ProjectManagement;

namespace Electron2D.Editor.Run;

internal static class EditorRunWorkflowSmoke
{
    private const string ProjectName = "RunWorkflowSmoke";

    public static EditorRunWorkflowSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);
        return RunAsync(workRoot).GetAwaiter().GetResult();
    }

    private static async Task<EditorRunWorkflowSmokeResult> RunAsync(string workRoot)
    {
        var repoRoot = FindRepositoryRoot();
        var templateRoot = Path.Combine(repoRoot, "data", "templates", "electron2d-empty");
        var manager = new EditorProjectManager(templateRoot);
        var creation = manager.CreateProject(new EditorProjectCreateOptions(
            ProjectName,
            Path.GetFullPath(workRoot),
            Electron2DRendererProfileSetting.Compatibility));
        var projectFile = Path.Combine(creation.ProjectPath, ProjectName + ".csproj");
        RewriteProjectToUseRuntimeProjectReference(projectFile, Path.Combine(repoRoot, "src", "Electron2D", "Electron2D.csproj"));

        var alternateScenePath = Path.Combine(creation.ProjectPath, "scenes", "alternate.scene.json");
        File.WriteAllText(alternateScenePath, "{ \"type\": \"Scene\", \"formatVersion\": 1, \"root\": { \"type\": \"Node\", \"name\": \"Alternate\" } }");

        var controller = new EditorRunController();
        WriteBrokenBuildFile(Path.Combine(creation.ProjectPath, "Scripts", "BrokenBuild.cs"));
        var buildFailure = controller.StartProject(creation.ProjectPath);
        File.Delete(Path.Combine(creation.ProjectPath, "Scripts", "BrokenBuild.cs"));
        var firstBuildDiagnostic = buildFailure.Diagnostics.First();

        WriteSceneRunProgram(Path.Combine(creation.ProjectPath, "Program.cs"));
        var projectRun = controller.StartProject(creation.ProjectPath);
        var projectExitCode = await controller.WaitForActiveSessionAsync().ConfigureAwait(false);
        var projectRunScene = ExtractLoadedScene(projectRun.Session?.OutputConsole.Text ?? string.Empty);

        var currentSceneRun = controller.StartCurrentScene(creation.ProjectPath, alternateScenePath);
        var currentSceneExitCode = await controller.WaitForActiveSessionAsync().ConfigureAwait(false);
        var currentSceneRunScene = ExtractLoadedScene(currentSceneRun.Session?.OutputConsole.Text ?? string.Empty);
        var currentSceneOverrideStable = LoadMainScene(creation.ProjectSettingsPath) == "scenes/main.scene.json";

        WriteRuntimeFailureProgram(Path.Combine(creation.ProjectPath, "Program.cs"));
        var failureRun = controller.StartProject(creation.ProjectPath);
        var failureExitCode = await controller.WaitForActiveSessionAsync().ConfigureAwait(false);
        var runtimeStackTraceContains = (failureRun.Session?.OutputConsole.Text ?? string.Empty)
            .Contains(nameof(InvalidOperationException), StringComparison.Ordinal) &&
            (failureRun.Session?.OutputConsole.Text ?? string.Empty).Contains(nameof(WriteRuntimeFailureProgram), StringComparison.Ordinal);

        WriteWaitingProgram(Path.Combine(creation.ProjectPath, "Program.cs"));
        var repeatedRunStopCycles = 0;
        bool stopRequested = false;
        bool stopObserved = false;
        for (var index = 0; index < 3; index++)
        {
            var waitRun = controller.StartProject(creation.ProjectPath);
            await Task.Delay(250).ConfigureAwait(false);
            _ = await controller.StopActiveSessionAsync().ConfigureAwait(false);
            stopRequested = waitRun.Session?.StopRequested == true;
            stopObserved = waitRun.Session?.StopObserved == true;
            if (stopRequested && stopObserved)
            {
                repeatedRunStopCycles++;
            }
        }

        WriteShaderMetadata(creation.ProjectPath);
        var shaderDiagnostics = controller.LoadShaderDiagnostics(creation.ProjectPath);
        var firstShaderDiagnostic = shaderDiagnostics.First();

        controller.FrameTiming.RecordFrame(TimeSpan.FromMilliseconds(16.67d));
        controller.FrameTiming.RecordFrame(TimeSpan.FromMilliseconds(33.33d));
        controller.FrameTiming.RecordFrame(TimeSpan.FromMilliseconds(16.67d));

        return new EditorRunWorkflowSmokeResult(
            creation.ProjectPath,
            creation.MainScenePath,
            alternateScenePath,
            buildFailure.Diagnostics.Count,
            firstBuildDiagnostic.Code,
            firstBuildDiagnostic.Line,
            firstBuildDiagnostic.Column,
            buildFailure.ProcessStarted,
            projectExitCode,
            projectRunScene,
            currentSceneExitCode,
            currentSceneRunScene,
            currentSceneOverrideStable,
            controller.OutputConsole.Contains("Electron2D empty scene loaded: scenes/main.scene.json"),
            controller.OutputConsole.Contains("Electron2D empty scene loaded: scenes/alternate.scene.json"),
            controller.OutputConsole.Lines.Count,
            failureExitCode,
            runtimeStackTraceContains,
            shaderDiagnostics.Count,
            firstShaderDiagnostic.FilePath,
            firstShaderDiagnostic.Line,
            firstShaderDiagnostic.Column,
            stopRequested,
            stopObserved,
            repeatedRunStopCycles,
            controller.ActiveSession is not null,
            controller.FrameTiming.Samples,
            controller.FrameTiming.LastFrameTimeMs,
            controller.FrameTiming.AverageFrameTimeMs,
            controller.FrameTiming.FramesPerSecond);
    }

    private static void RewriteProjectToUseRuntimeProjectReference(string projectFile, string runtimeProjectFile)
    {
        var document = XDocument.Load(projectFile);
        var itemGroups = document.Root?.Elements("ItemGroup").ToArray() ?? [];
        foreach (var packageReference in itemGroups
            .SelectMany(group => group.Elements("PackageReference"))
            .Where(reference => string.Equals((string?)reference.Attribute("Include"), "Electron2D", StringComparison.Ordinal))
            .ToArray())
        {
            packageReference.Remove();
        }

        var itemGroup = itemGroups.FirstOrDefault(group => group.Elements("ProjectReference").Any()) ??
            new XElement("ItemGroup");
        if (itemGroup.Parent is null)
        {
            document.Root?.Add(itemGroup);
        }

        itemGroup.Add(new XElement("ProjectReference", new XAttribute("Include", Path.GetFullPath(runtimeProjectFile))));
        document.Save(projectFile);
    }

    private static void WriteBrokenBuildFile(string path)
    {
        File.WriteAllText(path, """
            #error E2D_RUN_WORKFLOW_BUILD_FAILURE
            """);
    }

    private static void WriteSceneRunProgram(string path)
    {
        File.WriteAllText(path, """
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
            using System.Text.Json;

            var projectRoot = Environment.CurrentDirectory;
            var projectSettingsPath = Path.Combine(projectRoot, "project.e2d.json");
            using var projectSettingsStream = File.OpenRead(projectSettingsPath);
            using var projectSettings = JsonDocument.Parse(projectSettingsStream);
            var mainScene = projectSettings.RootElement.GetProperty("mainScene").GetString() ??
                throw new InvalidOperationException("Project main scene is missing.");
            var sceneToLoad = Environment.GetEnvironmentVariable("ELECTRON2D_CURRENT_SCENE");
            if (string.IsNullOrWhiteSpace(sceneToLoad))
            {
                sceneToLoad = mainScene;
            }

            var scenePath = Path.Combine(projectRoot, sceneToLoad.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(scenePath))
            {
                throw new FileNotFoundException("Scene file was not found.", scenePath);
            }

            Console.WriteLine("Electron2D empty scene loaded: " + sceneToLoad.Replace('\\', '/'));
            return 0;
            """);
    }

    private static void WriteRuntimeFailureProgram(string path)
    {
        File.WriteAllText(path, $$"""
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
            try
            {
                {{nameof(WriteRuntimeFailureProgram)}}();
                return 0;
            }
            catch (InvalidOperationException exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }

            static void {{nameof(WriteRuntimeFailureProgram)}}()
            {
                throw new InvalidOperationException("run workflow runtime failure");
            }
            """);
    }

    private static void WriteWaitingProgram(string path)
    {
        File.WriteAllText(path, """
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
            Console.WriteLine("RunWorkflowWaiting=True");
            while (true)
            {
                Thread.Sleep(100);
            }
            """);
    }

    private static void WriteShaderMetadata(string projectPath)
    {
        var sourcePath = "res://shaders/broken.e2shader";
        var uid = ResourceUid.CreateIdForPath(sourcePath);
        var metadata = new ShaderImportMetadata(
            sourcePath,
            uid,
            requiresRuntimeCompilation: true,
            stages: [],
            diagnostics:
            [
                CanvasShaderDiagnostic.Error(
                    sourcePath,
                    line: 1,
                    column: 1,
                    message: "Canvas shader source must declare `shader_type canvas_item;`.",
                    CanvasShaderStage.Vertex,
                    CanvasShaderTargetPlatform.Windows)
            ]);
        var cacheDirectory = Path.Combine(
            projectPath,
            ".electron2d",
            "import-cache",
            "resources",
            ResourceUid.IdToText(uid)["uid://".Length..]);
        Directory.CreateDirectory(cacheDirectory);
        File.WriteAllText(
            Path.Combine(cacheDirectory, "shader.e2shader.json"),
            ShaderImportMetadataTextSerializer.Serialize(metadata));
    }

    private static string ExtractLoadedScene(string output)
    {
        const string marker = "Electron2D empty scene loaded: ";
        foreach (var line in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith(marker, StringComparison.Ordinal))
            {
                return line[marker.Length..];
            }
        }

        return string.Empty;
    }

    private static string LoadMainScene(string projectSettingsPath)
    {
        var result = Electron2DSettingsStore.LoadProject(projectSettingsPath);
        if (!result.Succeeded || result.Settings is null)
        {
            throw new InvalidOperationException(string.Join(
                Environment.NewLine,
                result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        return result.Settings.MainScene;
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
