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
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class EditorProjectShellTests
{
    [Fact]
    public void EditorProjectIsPartOfSolutionAndUsesElectron2DRuntimeOnly()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");

        Assert.True(File.Exists(projectPath), "Electron2D.Editor project file must exist.");

        var solutionText = File.ReadAllText(Path.Combine(root, "src", "Electron2D.sln"));

        Assert.Contains("Electron2D.Editor", solutionText);

        var project = XDocument.Load(projectPath);
        Assert.Equal("WinExe", project.Root?.Element("PropertyGroup")?.Element("OutputType")?.Value);
        var packageReferences = project.Descendants("PackageReference")
            .Select(item => item.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        var projectReferences = project.Descendants("ProjectReference")
            .Select(item => item.Attribute("Include")?.Value?.Replace('\\', '/'))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        Assert.Contains("../Electron2D/Electron2D.csproj", projectReferences);
        Assert.DoesNotContain(packageReferences, package => package!.Contains("Avalonia", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(packageReferences, package => package!.Contains("WinForms", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(packageReferences, package => package!.Contains("WPF", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EditorProjectSmokeRunStartsOnElectron2D()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--smoke");

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"Editor smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");
        Assert.Contains("Electron2D.Editor smoke passed", output);
        Assert.Contains("Runtime=Electron2D", output);
        Assert.Contains("UiRoot=Electron2D.Panel", output);
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public async Task EditorWindowSmokeRunCreatesRealWindowAndWritesVisualArtifacts()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-editor-window-smoke-");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("--window-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var waitTask = process.WaitForExitAsync();
            var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(45)));
            if (completed != waitTask)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("Editor window smoke did not exit within the expected time.");
            }

            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Editor window smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor window smoke passed", output);
            Assert.Equal("Electron2D.Editor", lines["WindowTitle"]);
            Assert.Equal("True", lines["WindowCreated"]);
            Assert.Equal("True", lines["WindowShown"]);
            Assert.Equal("True", lines["FramePresented"]);
            Assert.Equal("True", lines["EventPumpObserved"]);
            Assert.Equal("True", lines["PointerInteractionObserved"]);
            Assert.Equal("True", lines["KeyboardInteractionObserved"]);
            Assert.Equal("True", lines["RuntimeControlTree"]);
            Assert.Equal("True", lines["VisualHarnessRemoved"]);
            Assert.Equal("True", lines["RuntimeUiRendering"]);
            Assert.Equal("True", lines["RuntimeUiInputDispatch"]);
            Assert.Equal("runtime-control-tree", lines["RenderSource"]);
            Assert.Equal("RuntimeHost", lines["InputDispatchSource"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);
            Assert.Equal("Tasks", lines["SelectedWorkspace"]);
            Assert.Equal("0", lines["TextOverflowCount"]);
            Assert.Equal("0", lines["ForbiddenUiMatches"]);
            Assert.False(lines.ContainsKey("ReattestedVisibleLayers"));
            Assert.True(int.Parse(lines["DrawCommands"], System.Globalization.CultureInfo.InvariantCulture) >= 16);

            var screenshotPath = lines["ScreenshotPath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(screenshotPath), $"Missing window screenshot artifact: {screenshotPath}");
            Assert.True(File.Exists(analysisPath), $"Missing window visual analysis artifact: {analysisPath}");

            var (width, height, rgba) = DecodePngRgba(File.ReadAllBytes(screenshotPath));
            Assert.Equal(1280, width);
            Assert.Equal(720, height);
            Assert.True(
                CalculateRedDominantPixelRatio(rgba) < 0.20d,
                "Editor window screenshot must not be the old red debug frame.");

            using var analysis = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var data = analysis.RootElement;

            Assert.Equal("Electron2D.EditorWindowSmokeAnalysis", data.GetProperty("format").GetString());
            Assert.True(data.GetProperty("window").GetProperty("actualWindow").GetBoolean());
            Assert.Equal("Electron2D.Editor", data.GetProperty("window").GetProperty("title").GetString());
            Assert.True(data.GetProperty("window").GetProperty("shown").GetBoolean());
            Assert.True(data.GetProperty("eventLoop").GetProperty("observed").GetBoolean());
            Assert.True(data.GetProperty("rendering").GetProperty("framePresented").GetBoolean());
            Assert.Equal("runtime-control-tree", data.GetProperty("rendering").GetProperty("source").GetString());
            Assert.True(data.GetProperty("rendering").GetProperty("runtimeUiRendering").GetBoolean());
            Assert.True(data.GetProperty("rendering").GetProperty("visualHarnessRemoved").GetBoolean());
            Assert.True(data.GetProperty("rendering").GetProperty("drawCommands").GetInt32() >= 16);
            Assert.True(data.GetProperty("rendering").GetProperty("redDominantPixelRatio").GetDouble() < 0.20d);
            Assert.True(data.GetProperty("input").GetProperty("pointerInteractionObserved").GetBoolean());
            Assert.True(data.GetProperty("input").GetProperty("keyboardInteractionObserved").GetBoolean());
            Assert.True(data.GetProperty("input").GetProperty("runtimeUiDispatch").GetBoolean());
            Assert.Equal("RuntimeHost", data.GetProperty("input").GetProperty("dispatchSource").GetString());
            Assert.Equal("Tasks", data.GetProperty("layout").GetProperty("selectedWorkspace").GetString());
            Assert.Equal(0, data.GetProperty("layout").GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, data.GetProperty("layout").GetProperty("forbiddenUiMatches").GetArrayLength());
            Assert.True(data.GetProperty("layout").GetProperty("clickableControlCount").GetInt32() >= 16);
            Assert.False(data.TryGetProperty("reattestedVisibleLayers", out _));
            Assert.True(data.GetProperty("screenshotReviewed").GetBoolean());
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
        }
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public async Task EditorWindowStartupSmokeLoadsProjectPassedAsE2DFileArgument()
    {
        var root = FindRepositoryRoot();
        var editorProjectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var referenceProjectFile = Path.Combine(root, "examples", "reference-platformer", "ReferencePlatformer.e2d");
        var referenceProjectRoot = Path.Combine(root, "examples", "reference-platformer");
        var referenceMainScene = Path.Combine(referenceProjectRoot, "scenes", "main.scene.json");
        var workRoot = CreateTemporaryDirectory("electron2d-editor-open-project-window-");
        var userDataRoot = CreateTemporaryDirectory("electron2d-editor-open-project-window-user-");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(editorProjectPath);
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("--open-project-window-smoke");
            startInfo.ArgumentList.Add(referenceProjectFile);
            startInfo.ArgumentList.Add(workRoot);
            startInfo.ArgumentList.Add("--user-data-dir");
            startInfo.ArgumentList.Add(userDataRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var waitTask = process.WaitForExitAsync();
            var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(45)));
            if (completed != waitTask)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("Editor open project window smoke did not exit within the expected time.");
            }

            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Editor open project window smoke failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor open project window smoke passed", output);
            Assert.Equal("ReferencePlatformer", lines["ProjectName"]);
            Assert.Equal(referenceProjectRoot, lines["ProjectPath"]);
            Assert.Equal(referenceProjectFile, lines["ProjectSettingsPath"]);
            Assert.Equal(referenceMainScene, lines["MainScenePath"]);
            Assert.Equal("True", lines["ProjectLoaded"]);
            Assert.Equal("True", lines["MainSceneLoaded"]);
            Assert.Equal("1", lines["RecentProjects"]);
            Assert.Equal("2D", lines["SelectedWorkspace"]);
            Assert.Equal("main.scene.json", lines["DocumentTabs"]);
            Assert.Equal("res://scenes/main.scene.json", lines["GameDocuments"]);
            Assert.Equal("True", lines["WindowCreated"]);
            Assert.Equal("True", lines["WindowShown"]);
            Assert.Equal("True", lines["FramePresented"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);
            Assert.Equal("True", lines["RuntimeUiRendering"]);
            Assert.Equal("True", lines["RuntimeUiInputDispatch"]);
            Assert.Equal("runtime-control-tree", lines["RenderSource"]);
            Assert.Equal("RuntimeHost", lines["InputDispatchSource"]);
            Assert.Equal("0", lines["TextOverflowCount"]);
            Assert.Equal("0", lines["ForbiddenUiMatches"]);
            Assert.True(File.Exists(lines["ScreenshotPath"]), $"Missing window screenshot artifact: {lines["ScreenshotPath"]}");
            Assert.True(File.Exists(lines["AnalysisPath"]), $"Missing window visual analysis artifact: {lines["AnalysisPath"]}");
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
            Directory.Delete(userDataRoot, recursive: true);
        }
    }

    [Fact]
    public void EditorWindowRuntimePathDoesNotUseShellVisualHarness()
    {
        var root = FindRepositoryRoot();
        var windowSource = File.ReadAllText(Path.Combine(root, "src", "Electron2D.Editor", "Shell", "WindowSmoke.cs"));
        var hostSource = File.ReadAllText(Path.Combine(root, "src", "Electron2D.Editor", "Shell", "WindowHost.cs"));
        var runtimeHostSource = File.ReadAllText(Path.Combine(root, "src", "Electron2D", "Runtime", "Application", "RuntimeHost.cs"));
        var programSource = File.ReadAllText(Path.Combine(root, "src", "Electron2D.Editor", "Program.cs"));
        var openProjectForWindowSource = ExtractMethodBody(programSource, "OpenProjectForWindow");

        Assert.DoesNotContain("ShellVisualHarness", windowSource, StringComparison.Ordinal);
        Assert.DoesNotContain("PresentCanvasForSmoke", windowSource, StringComparison.Ordinal);
        Assert.DoesNotContain("WindowSmoke.RunInteractive", programSource, StringComparison.Ordinal);
        Assert.DoesNotContain("FindRepositoryRoot", openProjectForWindowSource, StringComparison.Ordinal);
        Assert.Contains("SDL.PixelFormat.ABGR8888", runtimeHostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.PixelFormat.RGBA8888", runtimeHostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("int.MaxValue", hostSource, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorWindowRuntimePathUsesSharedRuntimeUiTreeAsRenderingAndInputSource()
    {
        var root = FindRepositoryRoot();
        var rendererSource = File.ReadAllText(Path.Combine(root, "src", "Electron2D.Editor", "Shell", "RuntimeFrameRenderer.cs"));
        var hostSource = File.ReadAllText(Path.Combine(root, "src", "Electron2D.Editor", "Shell", "WindowHost.cs"));
        var smokeSource = File.ReadAllText(Path.Combine(root, "src", "Electron2D.Editor", "Shell", "WindowSmoke.cs"));
        var applicationSource = File.ReadAllText(Path.Combine(root, "src", "Electron2D.Editor", "Application.cs"));
        var engineAssemblyInfo = File.ReadAllText(Path.Combine(root, "src", "Electron2D", "Properties", "AssemblyInfo.cs"));
        var pointerSmokeBody = ExtractMethodBody(smokeSource, "private static Func<Electron2D.RuntimeHostScriptedInputContext, IReadOnlyList<Electron2D.InputEvent>> CreatePointerSelectionProvider");

        Assert.Contains("new Electron2D.Button", applicationSource, StringComparison.Ordinal);
        Assert.Contains("Connect(\"pressed\"", applicationSource, StringComparison.Ordinal);
        Assert.Contains("InternalsVisibleTo(\"Electron2D.Editor\")", engineAssemblyInfo, StringComparison.Ordinal);

        Assert.Contains("Electron2D.RuntimeHost", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawArea(", rendererSource, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawCenterWorkspace", rendererSource, StringComparison.Ordinal);
        Assert.DoesNotContain(".FillRectangle(", rendererSource, StringComparison.Ordinal);
        Assert.DoesNotContain(".DrawRectangle(", rendererSource, StringComparison.Ordinal);
        Assert.DoesNotContain(".DrawText(", rendererSource, StringComparison.Ordinal);

        Assert.DoesNotContain("ShellRegion", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateVisualRegions()", hostSource, StringComparison.Ordinal);

        Assert.DoesNotContain(".DispatchInput(", pointerSmokeBody, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateVisualRegions()", pointerSmokeBody, StringComparison.Ordinal);
        Assert.DoesNotContain(".SwitchWorkspace(", pointerSmokeBody, StringComparison.Ordinal);
    }

    private static string ExtractMethodBody(string source, string methodName)
    {
        var methodIndex = source.IndexOf(methodName, StringComparison.Ordinal);
        Assert.True(methodIndex >= 0, $"Method '{methodName}' must exist.");

        var braceIndex = source.IndexOf('{', methodIndex);
        Assert.True(braceIndex >= 0, $"Method '{methodName}' must have a body.");

        var depth = 0;
        for (var index = braceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[braceIndex..(index + 1)];
                }
            }
        }

        throw new InvalidOperationException($"Method '{methodName}' body was not closed.");
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] bytes)
    {
        Assert.True(bytes.Length >= 24, "PNG must contain a signature and IHDR chunk.");
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47 }, bytes.Take(4).ToArray());
        Assert.Equal("IHDR", System.Text.Encoding.ASCII.GetString(bytes, 12, 4));

        return (
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)),
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
    }

    private static (int Width, int Height, byte[] Rgba) DecodePngRgba(byte[] bytes)
    {
        var (width, height) = ReadPngDimensions(bytes);
        using var idat = new MemoryStream();
        var offset = 8;
        while (offset + 12 <= bytes.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            var type = System.Text.Encoding.ASCII.GetString(bytes, offset + 4, 4);
            offset += 8;
            if (type == "IDAT")
            {
                idat.Write(bytes, offset, length);
            }

            offset += length + 4;
            if (type == "IEND")
            {
                break;
            }
        }

        idat.Position = 0;
        using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        zlib.CopyTo(raw);
        var scanlines = raw.ToArray();
        var stride = width * 4;
        Assert.Equal(height * (stride + 1), scanlines.Length);

        var rgba = new byte[width * height * 4];
        for (var row = 0; row < height; row++)
        {
            var scanlineOffset = row * (stride + 1);
            Assert.Equal(0, scanlines[scanlineOffset]);
            Buffer.BlockCopy(scanlines, scanlineOffset + 1, rgba, row * stride, stride);
        }

        return (width, height, rgba);
    }

    private static double CalculateRedDominantPixelRatio(byte[] rgba)
    {
        var redDominant = 0;
        var total = rgba.Length / 4;
        for (var index = 0; index < rgba.Length; index += 4)
        {
            var red = rgba[index];
            var green = rgba[index + 1];
            var blue = rgba[index + 2];
            if (red > 200 && red > green * 2 && red > blue * 2)
            {
                redDominant++;
            }
        }

        return (double)redDominant / total;
    }

    private static Dictionary<string, string> ParseMachineReadableOutput(string output)
    {
        return output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
