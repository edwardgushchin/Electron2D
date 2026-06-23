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
using System.Diagnostics;
using Xunit;

namespace Electron2D.Tests.Integration;

[Collection(RuntimeWindowCollection.Name)]
public sealed class UiHeavyReferenceProjectTests
{
    [Fact]
    public void UiHeavyReferenceProjectHasEditorProjectContract()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "ui-heavy-reference");

        foreach (var relativePath in RequiredFiles)
        {
            Assert.True(
                File.Exists(Path.Combine(projectRoot, relativePath)),
                $"UI-heavy reference game is missing required file: {relativePath}");
        }

        var settings = Electron2D.Electron2DSettingsStore.LoadProject(Path.Combine(projectRoot, "project.e2d.json"));
        Assert.True(settings.Succeeded, FormatSettingsDiagnostics(settings.Diagnostics));
        Assert.NotNull(settings.Settings);
        Assert.Equal("Electron2D.UiHeavyReference", settings.Settings.Name);
        Assert.Equal("scenes/menu.scene.json", settings.Settings.MainScene);
        Assert.Equal(
            new[] { "accept", "cancel", "next_card", "previous_card", "switch_locale" },
            settings.Settings.InputActions.Select(action => action.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());

        var exportPresets = Electron2D.ExportPresetStore.Load(Path.Combine(projectRoot, "export_presets.e2export.json"));
        Assert.True(exportPresets.Succeeded, FormatExportDiagnostics(exportPresets.Diagnostics));
        Assert.NotNull(exportPresets.Document);
        Assert.Equal(
            [
                "AndroidArm64",
                "IosArm64",
                "LinuxX64",
                "MacOSArm64",
                "WebAssemblyBrowser",
                "WindowsX64"
            ],
            exportPresets.Document.Presets
                .Select(preset => preset.Target.ToString())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(target => target, StringComparer.Ordinal)
                .ToArray());
        Assert.Contains(
            exportPresets.Document.Presets,
            preset => preset.Target.ToString() == "AndroidArm64" && preset.RendererProfile.ToString() == "Compatibility");

        var taskBoard = ReadJson(Path.Combine(projectRoot, ".electron2d", "tasks", "board.e2tasks"));
        Assert.Equal("Electron2D.TaskBoard", taskBoard.RootElement.GetProperty("format").GetString());
        Assert.DoesNotContain(
            Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories),
            path => Path.GetFileName(path).Equals("TASKS.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UiHeavyReferenceScriptCoversRequiredUiGameplaySubsystems()
    {
        var root = FindRepositoryRoot();
        var scriptPath = Path.Combine(root, "examples", "ui-heavy-reference", "Scripts", "CardPuzzleGame.cs");

        Assert.True(File.Exists(scriptPath), $"UI-heavy reference script is missing: {scriptPath}");

        var script = File.ReadAllText(scriptPath);
        foreach (var requiredText in new[]
        {
            "Control",
            "VBoxContainer",
            "GridContainer",
            "Panel",
            "Label",
            "Button",
            "TextureButton",
            "CheckBox",
            "Slider",
            "ProgressBar",
            "TextureRect",
            "NinePatchRect",
            "ImageTexture.LoadFromFile",
            "ItemList",
            "Translation",
            "TranslationServer",
            "Input.IsActionJustPressed(\"accept\")",
            "Input.IsActionJustPressed(\"cancel\")",
            "Input.IsActionJustPressed(\"next_card\")",
            "Input.IsActionJustPressed(\"previous_card\")",
            "Input.IsActionJustPressed(\"switch_locale\")",
            "InputEventScreenTouch",
            "SaveProgress",
            "ShowMenuScene",
            "ShowGameScene",
            "ShowResultScene",
            "Compatibility"
        })
        {
            Assert.Contains(requiredText, script, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("ReferenceTexture2D", script, StringComparison.Ordinal);
    }

    [Fact]
    public void UiHeavyReferenceVerifierExists()
    {
        var root = FindRepositoryRoot();
        var verifierPath = Path.Combine(root, "tools", "Verify-UiHeavyReference.ps1");

        Assert.True(File.Exists(verifierPath), $"UI-heavy reference verifier is missing: {verifierPath}");

        var verifier = File.ReadAllText(verifierPath);
        Assert.Contains("Electron2D.UiHeavyReference.csproj", verifier, StringComparison.Ordinal);
        Assert.Contains("dotnet build", verifier, StringComparison.Ordinal);
        Assert.Contains("e2d run", verifier, StringComparison.Ordinal);
        Assert.Contains("e2d validate", verifier, StringComparison.Ordinal);
        Assert.Contains("Compatibility", verifier, StringComparison.Ordinal);
        Assert.Contains(".electron2d/tasks", verifier, StringComparison.Ordinal);
    }

    [Fact]
    public void UiHeavyReferencePlayableUsesOnlyElectron2DApi()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "ui-heavy-reference");
        var programPath = Path.Combine(projectRoot, "Program.cs");
        var source = File.ReadAllText(Path.Combine(projectRoot, "Scripts", "CardPuzzleGame.cs"));

        Assert.False(File.Exists(programPath), "UI-heavy reference must be launched by e2d run, not by a user Program.cs bootstrap.");
        Assert.DoesNotContain("Electron2DRuntimeHost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectRuntimeRunner", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Electron2DApplication", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Electron2DRunOptions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Electron2DRunResult", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.ReadKey", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL3", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateWindow", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FRAME ui-heavy-reference", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UiHeavyReferencePlayableScriptRunsGameLoop()
    {
        var root = FindRepositoryRoot();
        var projectRoot = Path.Combine(root, "examples", "ui-heavy-reference");
        var screenshotPath = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-UiHeavyReferencePlayableTests",
            Guid.NewGuid().ToString("N"),
            "ui-heavy-reference-playable.png");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(Path.Combine(root, "src", "Electron2D.Cli", "Electron2D.Cli.csproj"));
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectRoot);
        startInfo.ArgumentList.Add("--play-script");
        startInfo.ArgumentList.Add("play,next,accept,locale,next,accept,result,save,quit");
        startInfo.ArgumentList.Add("--screenshot");
        startInfo.ArgumentList.Add(screenshotPath);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start UI-heavy reference playable script.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await WaitForExitAsync(process, TimeSpan.FromSeconds(60));
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"UI-heavy reference playable script failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

        var lines = ParseMachineReadableOutput(output);
        Assert.Equal("playable", lines["Mode"]);
        Assert.Equal("True", lines["Playable"]);
        Assert.True(int.Parse(lines["FramesAdvanced"], System.Globalization.CultureInfo.InvariantCulture) >= 6);
        Assert.Equal("9", lines["CommandsApplied"]);
        Assert.Equal("result", lines["Scene"]);
        Assert.Equal("ru", lines["Locale"]);
        Assert.True(int.Parse(lines["Score"], System.Globalization.CultureInfo.InvariantCulture) > 0);
        Assert.True(int.Parse(lines["Moves"], System.Globalization.CultureInfo.InvariantCulture) >= 2);
        Assert.False(string.IsNullOrWhiteSpace(lines["SelectedCard"]));
        Assert.True(File.Exists(lines["SavePath"]), $"Missing playable save path: {lines["SavePath"]}");
        Assert.Equal("True", lines["WindowCreated"]);
        Assert.Equal("True", lines["WindowShown"]);
        Assert.Equal("True", lines["FramePresented"]);
        Assert.True(int.Parse(lines["InputEventsDispatched"], System.Globalization.CultureInfo.InvariantCulture) >= 0);
        Assert.True(int.Parse(lines["DrawCommands"], System.Globalization.CultureInfo.InvariantCulture) > 0);
        Assert.Equal(screenshotPath, lines["ScreenshotPath"]);
        Assert.True(File.Exists(screenshotPath), $"Missing playable screenshot path: {screenshotPath}");
        Assert.DoesNotContain("FRAME", output, StringComparison.Ordinal);

        var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
        Assert.True(width >= 640, $"Expected a normal playable screenshot width, got {width}.");
        Assert.True(height >= 360, $"Expected a normal playable screenshot height, got {height}.");
    }

    private static readonly string[] RequiredFiles =
    [
        "Electron2D.UiHeavyReference.csproj",
        "Scripts/CardPuzzleGame.cs",
        "project.e2d.json",
        "electron2d.lock.json",
        "global.json",
        "export_presets.e2export.json",
        "scenes/menu.scene.json",
        "scenes/game.scene.json",
        "scenes/result.scene.json",
        "resources/ui-heavy-reference.manifest.json",
        ".electron2d/tasks/board.e2tasks",
        ".electron2d/tasks/ui-heavy-acceptance.e2task"
    ];

    private static JsonDocument ReadJson(string path)
    {
        Assert.True(File.Exists(path), $"JSON file is missing: {path}");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static Dictionary<string, string> ParseMachineReadableOutput(string output)
    {
        return output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
    }

    private static async Task WaitForExitAsync(Process process, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new TimeoutException($"Process did not exit within {timeout.TotalSeconds:0} seconds.");
        }
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] bytes)
    {
        Assert.True(bytes.Length >= 24, "PNG is too small.");
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }, bytes[..8]);
        var width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
        var height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
        return (width, height);
    }

    private static string FormatSettingsDiagnostics(IEnumerable<Electron2D.Electron2DSettingsDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }

    private static string FormatExportDiagnostics(IEnumerable<Electron2D.Electron2DExportDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
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
