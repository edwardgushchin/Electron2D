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

public sealed class EditorScriptLanguageServicesTests
{
    [Fact]
    [Trait("Category", "Baseline")]
    public async Task ScriptLanguageServicesSmokeRunWritesRoslynModelAndVisualAcceptanceArtifacts()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-script-language-services-");

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
            startInfo.ArgumentList.Add("--script-language-services-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Editor Script language services smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor script language services smoke passed", output);
            Assert.Equal("Electron2D.CSharpLanguageServices", lines["AssemblyBoundary"]);
            Assert.Equal("True", lines["RoslynSemanticModel"]);
            Assert.Equal("False", lines["RuntimeAssemblyContainsLanguageServices"]);
            Assert.Equal("False", lines["EditorUiContainsLanguageServices"]);
            Assert.Equal("False", lines["WorkspaceSnapshotUsedForIde"]);
            Assert.Equal("script-language-smoke-project", lines["ProjectId"]);
            Assert.Equal("doc-script-language-hero", lines["DocumentId"]);
            Assert.Equal("7", lines["DocumentRevision"]);
            Assert.Equal("3", lines["SemanticVersion"]);
            Assert.Equal("lang-services-hash", lines["ConfigurationHash"]);
            Assert.Equal("True", lines["CompletionContainsElectron2DApi"]);
            Assert.Equal("True", lines["CompletionContainsLocalSymbol"]);
            Assert.Equal("Sprite2D", lines["CompletionSelectedItem"]);
            Assert.Equal("Vector2(float x, float y)", lines["SignatureHelpDisplay"]);
            Assert.Equal("1", lines["SignatureHelpActiveParameter"]);
            Assert.Equal("Smoke.Scripts.HeroController.DocumentedMove(float delta)", lines["HoverSymbol"]);
            Assert.Equal("True", lines["HoverDocumentationContainsXmlSummary"]);
            Assert.Equal("CS0103", lines["LiveDiagnosticCode"]);
            Assert.Equal("Error", lines["LiveDiagnosticSeverity"]);
            Assert.Equal("Scripts/HeroController.cs", lines["LiveDiagnosticPath"]);
            Assert.Equal("15", lines["LiveDiagnosticLine"]);
            Assert.Equal("9", lines["LiveDiagnosticColumn"]);
            Assert.Equal("Scripts/HeroController.cs:10:17", lines["DefinitionTarget"]);
            Assert.Equal("2", lines["ReferencesCount"]);
            Assert.Equal("2", lines["RenameEditCount"]);
            Assert.Equal("7", lines["RenameExpectedRevision"]);
            Assert.Equal("True", lines["FormattingChanged"]);
            Assert.Equal("Add using System.Collections.Generic", lines["CodeActionTitle"]);
            Assert.Equal("True", lines["StaleResponseDiscarded"]);
            Assert.Equal("True", lines["PreviousRequestCancelled"]);
            Assert.Equal("250", lines["DiagnosticsDebounceMs"]);
            Assert.Equal("PackageReference", lines["ReloadTrigger"]);
            Assert.Equal("E2D-SCRIPT-0003", lines["SemanticFailureDiagnosticCode"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);

            var statePath = lines["StatePath"];
            var screenshotPath = lines["ScreenshotPath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(statePath), $"Missing language services state artifact: {statePath}");
            Assert.True(File.Exists(screenshotPath), $"Missing language services screenshot artifact: {screenshotPath}");
            Assert.True(File.Exists(analysisPath), $"Missing language services visual analysis artifact: {analysisPath}");

            var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
            Assert.Equal(1280, width);
            Assert.Equal(720, height);

            using var analysis = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var data = analysis.RootElement;

            Assert.Equal("Electron2D.ScriptLanguageServicesVisualAnalysis", data.GetProperty("format").GetString());
            Assert.Equal("automated-script-language-services-harness", data.GetProperty("harness").GetString());
            Assert.Equal("Script", data.GetProperty("selectedWorkspace").GetString());
            Assert.Equal(0, data.GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, data.GetProperty("forbiddenUiMatches").GetArrayLength());
            Assert.True(data.GetProperty("clickableControlCount").GetInt32() >= 16);
            Assert.True(data.GetProperty("screenshotReviewed").GetBoolean());
            Assert.True(data.GetProperty("completion").GetProperty("visible").GetBoolean());
            Assert.Equal("Sprite2D", data.GetProperty("completion").GetProperty("selectedItem").GetString());
            Assert.True(data.GetProperty("completion").GetProperty("keyboardSelectionVisible").GetBoolean());
            Assert.True(data.GetProperty("hover").GetProperty("visible").GetBoolean());
            Assert.True(data.GetProperty("hover").GetProperty("documentationVisible").GetBoolean());
            Assert.True(data.GetProperty("diagnostics").GetProperty("visible").GetBoolean());
            Assert.Equal("CS0103", data.GetProperty("diagnostics").GetProperty("code").GetString());
            Assert.Equal("Vector2(float x, float y)", data.GetProperty("signatureHelp").GetProperty("display").GetString());
            Assert.Equal(1, data.GetProperty("signatureHelp").GetProperty("activeParameter").GetInt32());
            Assert.True(data.GetProperty("stale").GetProperty("discarded").GetBoolean());
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
