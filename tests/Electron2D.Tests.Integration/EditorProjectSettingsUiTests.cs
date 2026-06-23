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
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class EditorProjectSettingsUiTests
{
    [Fact]
    public async Task ProjectSettingsSmokeWritesProjectAndExportSettingsThroughRealWindowUi()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-editor-project-settings-");

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
            startInfo.ArgumentList.Add("--project-settings-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var waitTask = process.WaitForExitAsync();
            var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(45)));
            if (completed != waitTask)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("Editor project settings smoke did not exit within the expected time.");
            }

            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Project Settings smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor project settings smoke passed", output);
            Assert.Equal("True", lines["ProjectSettingsWritten"]);
            Assert.Equal("True", lines["InputMapRoundTrip"]);
            Assert.Equal("True", lines["ExportPresetsRoundTrip"]);
            Assert.Equal("scenes/settings-smoke.scene.json", lines["MainScene"]);
            Assert.Equal("Standard", lines["RendererProfile"]);
            Assert.Equal("120", lines["PhysicsTicksPerSecond"]);
            Assert.Equal("960x540", lines["DisplaySize"]);
            Assert.Equal("jump|dash", lines["InputActions"]);
            Assert.Equal("android-release|browser-debug|ios-release|linux-debug|macos-release|windows-debug", lines["ExportPresets"]);
            Assert.Equal("True", lines["WindowCreated"]);
            Assert.Equal("True", lines["WindowShown"]);
            Assert.Equal("True", lines["FramePresented"]);
            Assert.Equal("True", lines["PointerInteractionObserved"]);
            Assert.Equal("True", lines["KeyboardInteractionObserved"]);
            Assert.Equal("0", lines["TextOverflowCount"]);
            Assert.Equal("0", lines["ForbiddenUiMatches"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);

            var projectSettingsPath = lines["ProjectSettingsPath"];
            var exportPresetsPath = lines["ExportPresetsPath"];
            var screenshotPath = lines["ScreenshotPath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(projectSettingsPath), $"Missing project settings: {projectSettingsPath}");
            Assert.True(File.Exists(exportPresetsPath), $"Missing export presets: {exportPresetsPath}");
            Assert.True(File.Exists(screenshotPath), $"Missing screenshot: {screenshotPath}");
            Assert.True(File.Exists(analysisPath), $"Missing visual analysis: {analysisPath}");

            using var projectDocument = JsonDocument.Parse(File.ReadAllText(projectSettingsPath));
            var project = projectDocument.RootElement;
            Assert.Equal("Electron2D.ProjectSettings", project.GetProperty("format").GetString());
            Assert.Equal("scenes/settings-smoke.scene.json", project.GetProperty("mainScene").GetString());
            Assert.Equal("Standard", project.GetProperty("rendererProfile").GetString());
            Assert.Equal(120, project.GetProperty("physicsTicksPerSecond").GetInt32());
            Assert.Equal(960, project.GetProperty("display").GetProperty("windowWidth").GetInt32());
            Assert.Equal(540, project.GetProperty("display").GetProperty("windowHeight").GetInt32());
            Assert.False(project.GetProperty("display").GetProperty("fullscreen").GetBoolean());
            var inputActions = project.GetProperty("input").GetProperty("actions").EnumerateArray()
                .Select(action => action.GetProperty("name").GetString())
                .ToArray();
            Assert.Equal(new[] { "dash", "jump" }, inputActions.Order(StringComparer.Ordinal).ToArray());

            using var exportDocument = JsonDocument.Parse(File.ReadAllText(exportPresetsPath));
            var presets = exportDocument.RootElement.GetProperty("presets").EnumerateArray().ToArray();
            Assert.Equal("Electron2D.ExportPresets", exportDocument.RootElement.GetProperty("format").GetString());
            Assert.Equal(
                new[] { "AndroidArm64", "WebAssemblyBrowser", "IosArm64", "LinuxX64", "MacOSArm64", "WindowsX64" },
                presets.Select(preset => preset.GetProperty("target").GetString()).ToArray());
            Assert.All(presets, preset =>
            {
                var signing = preset.GetProperty("signing");
                Assert.DoesNotContain("password", signing.GetRawText(), StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("private", signing.GetRawText(), StringComparison.OrdinalIgnoreCase);
            });

            var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
            Assert.Equal(1280, width);
            Assert.Equal(720, height);

            using var analysisDocument = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var analysis = analysisDocument.RootElement;
            Assert.Equal("Electron2D.ProjectSettingsUiVisualAnalysis", analysis.GetProperty("format").GetString());
            Assert.True(analysis.GetProperty("window").GetProperty("actualWindow").GetBoolean());
            Assert.True(analysis.GetProperty("window").GetProperty("shown").GetBoolean());
            Assert.True(analysis.GetProperty("rendering").GetProperty("framePresented").GetBoolean());
            Assert.True(analysis.GetProperty("input").GetProperty("pointerInteractionObserved").GetBoolean());
            Assert.True(analysis.GetProperty("input").GetProperty("keyboardInteractionObserved").GetBoolean());
            Assert.Equal(0, analysis.GetProperty("layout").GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, analysis.GetProperty("layout").GetProperty("forbiddenUiMatches").GetArrayLength());
            Assert.True(analysis.GetProperty("layout").GetProperty("clickableControlCount").GetInt32() >= 8);
            Assert.Contains(
                analysis.GetProperty("layout").GetProperty("sectionLabels").EnumerateArray(),
                label => label.GetString() == "Export Presets");
            Assert.True(analysis.GetProperty("screenshotReviewed").GetBoolean());
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
        }
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
