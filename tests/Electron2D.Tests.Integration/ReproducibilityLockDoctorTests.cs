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
using System.Reflection;
using System.Text.Json;
using Electron2D.ProjectSystem;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ReproducibilityLockDoctorTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void EmptyProjectTemplateIncludesGlobalJsonAndReproducibilityLock()
    {
        var templateRoot = Path.Combine(FindRepositoryRoot(), "data", "templates", "electron2d-empty");
        var globalJsonPath = Path.Combine(templateRoot, "global.json");
        var lockPath = Path.Combine(templateRoot, "electron2d.lock.json");

        Assert.True(File.Exists(globalJsonPath), "Template must include global.json.");
        Assert.True(File.Exists(lockPath), "Template must include electron2d.lock.json.");

        using var globalJson = JsonDocument.Parse(File.ReadAllText(globalJsonPath));
        Assert.Equal("10.0.101", globalJson.RootElement.GetProperty("sdk").GetProperty("version").GetString());
        Assert.Equal("latestFeature", globalJson.RootElement.GetProperty("sdk").GetProperty("rollForward").GetString());

        var lockText = File.ReadAllText(lockPath);
        using var lockJson = JsonDocument.Parse(lockText);
        var lockRoot = lockJson.RootElement;
        Assert.Equal(
            [
                "$schema",
                "format",
                "schemaVersion",
                "engine",
                "dotnet",
                "nuget",
                "nativeRuntime",
                "assetImporters",
                "project",
                "exportTemplates",
                "signing"
            ],
            lockRoot.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal("https://electron2d.dev/schemas/project-system/electron2d-lock.schema.json", lockRoot.GetProperty("$schema").GetString());
        Assert.Equal("Electron2D.ReproducibilityLock", lockRoot.GetProperty("format").GetString());
        Assert.Equal(1, lockRoot.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("0.1.0-preview", lockRoot.GetProperty("engine").GetProperty("version").GetString());
        Assert.Equal("net10.0", lockRoot.GetProperty("dotnet").GetProperty("targetFramework").GetString());
        Assert.Equal("referencesOnly", lockRoot.GetProperty("signing").GetProperty("mode").GetString());
    }

    [Fact]
    public void ReproducibilityLockSchemaListsRequiredTopLevelFields()
    {
        var schemaPath = Path.Combine(FindRepositoryRoot(), "data", "schemas", "project-system", "electron2d-lock.schema.json");

        Assert.True(File.Exists(schemaPath), "Lock schema must be published under data/schemas/project-system.");

        using var schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        var root = schema.RootElement;
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
        Assert.Equal("Electron2D reproducibility lock schema", root.GetProperty("title").GetString());
        Assert.Equal("object", root.GetProperty("type").GetString());
        Assert.Equal(
            [
                "$schema",
                "format",
                "schemaVersion",
                "engine",
                "dotnet",
                "nuget",
                "nativeRuntime",
                "assetImporters",
                "project",
                "exportTemplates",
                "signing"
            ],
            root.GetProperty("required").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray());
    }

    [Fact]
    public void ReproducibilityLockVerifierAcceptsTemplateAndRejectsMissingLock()
    {
        var projectRoot = CopyTemplateToTemporaryProject("verifier");

        try
        {
            var verifierType = Type.GetType(
                "Electron2D.ProjectSystem.ProjectReproducibilityLockVerifier, Electron2D.ProjectSystem",
                throwOnError: false);
            Assert.NotNull(verifierType);
            var verify = verifierType.GetMethod("Verify", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(verify);

            var validResult = InvokeVerifier(verify, projectRoot);
            Assert.True(ReadSucceeded(validResult), FormatDiagnostics(ReadDiagnostics(validResult)));
            Assert.Empty(ReadDiagnostics(validResult));

            File.Delete(Path.Combine(projectRoot, "electron2d.lock.json"));

            var missingLockResult = InvokeVerifier(verify, projectRoot);
            Assert.False(ReadSucceeded(missingLockResult));
            Assert.Contains(ReadDiagnostics(missingLockResult), diagnostic => diagnostic.Code == "E2D-DOCTOR-0001");
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public void DoctorJsonOutputReportsReadOnlyEnvironmentChecksWithoutMutatingProject()
    {
        var projectRoot = CopyTemplateToTemporaryProject("doctor-read-only");

        try
        {
            var before = ReadProjectFileSnapshot(projectRoot);
            var result = RunCli(
                CliExecutionContext.ForTests(FixedInstant),
                "doctor",
                "--project",
                projectRoot,
                "--format",
                "json");
            var after = ReadProjectFileSnapshot(projectRoot);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(before, after);
            Assert.Empty(result.Error);

            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            Assert.Equal("doctor", root.GetProperty("command").GetString());
            Assert.True(root.GetProperty("succeeded").GetBoolean());
            Assert.Equal("none", root.GetProperty("route").GetString());
            Assert.Empty(root.GetProperty("changedFiles").EnumerateArray());
            Assert.Empty(root.GetProperty("dirtyDocuments").EnumerateArray());
            Assert.Equal("doctor.environment", root.GetProperty("data").GetProperty("mode").GetString());
            Assert.NotEqual("blocked", root.GetProperty("data").GetProperty("summary").GetProperty("status").GetString());

            var checks = root.GetProperty("data").GetProperty("checks")
                .EnumerateArray()
                .Select(check => check.GetProperty("id").GetString() ?? string.Empty)
                .ToArray();
            Assert.Equal(
                [
                    "dotnetSdk",
                    "electron2d",
                    "nativeRuntime",
                    "androidSdk",
                    "androidNdk",
                    "xcode",
                    "exportTemplates",
                    "graphicsCapabilities",
                    "signing"
                ],
                checks);
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public void DoctorJsonOutputRedactsSigningEnvironmentSecretValues()
    {
        var projectRoot = CopyTemplateToTemporaryProject("doctor-signing");
        const string variableName = "E2D_TEST_SIGNING_SECRET";
        const string secretValue = "super-sensitive-doctor-value";
        var previousValue = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, secretValue);
            File.WriteAllText(
                Path.Combine(projectRoot, "export_presets.e2export.json"),
                """
                {
                  "format": "Electron2D.ExportPresets",
                  "formatVersion": 1,
                  "presets": [
                    {
                      "name": "android-release",
                      "target": "AndroidArm64",
                      "configuration": "Release",
                      "runtimeIdentifier": "android-arm64",
                      "selfContained": true,
                      "rendererProfile": "Automatic",
                      "outputDirectory": "exports/android",
                      "includeDebugSymbols": false,
                      "signing": {
                        "required": true,
                        "identity": "android-release",
                        "credentialReference": "env:E2D_TEST_SIGNING_SECRET"
                      }
                    }
                  ]
                }
                """);
            var before = ReadProjectFileSnapshot(projectRoot);

            var result = RunCli(
                CliExecutionContext.ForTests(FixedInstant),
                "doctor",
                "--project",
                projectRoot,
                "--format",
                "json");
            var after = ReadProjectFileSnapshot(projectRoot);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(before, after);
            Assert.DoesNotContain(secretValue, result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("super-sensitive", result.Output, StringComparison.Ordinal);
            Assert.Contains("env:E2D_TEST_SIGNING_SECRET", result.Output, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previousValue);
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    private static CliRunResult RunCli(CliExecutionContext context, params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Electron2DCommandLine.Run(args, output, error, context);

        return new CliRunResult(exitCode, output.ToString(), error.ToString());
    }

    private static string CopyTemplateToTemporaryProject(string name)
    {
        var repositoryRoot = FindRepositoryRoot();
        var templateRoot = Path.Combine(repositoryRoot, "data", "templates", "electron2d-empty");
        var projectRoot = Path.Combine(Path.GetTempPath(), "Electron2D-ReproducibilityTests", name, Guid.NewGuid().ToString("N"));
        CopyDirectory(templateRoot, projectRoot);
        return projectRoot;
    }

    private static void CopyDirectory(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);
        foreach (var directory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, file)), overwrite: false);
        }
    }

    private static IReadOnlyDictionary<string, string> ReadProjectFileSnapshot(string projectRoot)
    {
        return Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToDictionary(
                path => Path.GetRelativePath(projectRoot, path).Replace(Path.DirectorySeparatorChar, '/'),
                File.ReadAllText,
                StringComparer.Ordinal);
    }

    private static object InvokeVerifier(MethodInfo verify, string projectRoot)
    {
        return verify.Invoke(null, [projectRoot])
            ?? throw new InvalidOperationException("ProjectReproducibilityLockVerifier.Verify returned null.");
    }

    private static bool ReadSucceeded(object result)
    {
        return (bool?)result.GetType().GetProperty("Succeeded")?.GetValue(result)
            ?? throw new InvalidOperationException("Verifier result must expose Succeeded.");
    }

    private static IReadOnlyList<StructuredDiagnostic> ReadDiagnostics(object result)
    {
        return result.GetType().GetProperty("Diagnostics")?.GetValue(result) as IReadOnlyList<StructuredDiagnostic>
            ?? throw new InvalidOperationException("Verifier result must expose Diagnostics.");
    }

    private static string FormatDiagnostics(IEnumerable<StructuredDiagnostic> diagnostics)
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

    private sealed record CliRunResult(int ExitCode, string Output, string Error);
}
