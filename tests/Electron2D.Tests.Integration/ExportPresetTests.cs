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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ExportPresetTests
{
    [Fact]
    public void ExportPresetsRoundTripDeterministicJson()
    {
        var directory = CreateTemporaryDirectory("electron2d-export-presets-");
        var path = Path.Combine(directory, "export_presets.e2export.json");

        try
        {
            var document = new Electron2D.Electron2DExportPresetDocument
            {
                Presets =
                [
                    new Electron2D.Electron2DExportPreset
                    {
                        Name = "windows-debug",
                        Target = Electron2D.Electron2DExportTarget.WindowsX64,
                        Configuration = Electron2D.Electron2DExportConfiguration.Debug,
                        RuntimeIdentifier = "win-x64",
                        SelfContained = true,
                        RendererProfile = Electron2D.Electron2DRendererProfileSetting.Compatibility,
                        OutputDirectory = "exports/windows",
                        IncludeDebugSymbols = true
                    },
                    new Electron2D.Electron2DExportPreset
                    {
                        Name = "android-release",
                        Target = Electron2D.Electron2DExportTarget.AndroidArm64,
                        Configuration = Electron2D.Electron2DExportConfiguration.Release,
                        RuntimeIdentifier = "android-arm64",
                        SelfContained = true,
                        RendererProfile = Electron2D.Electron2DRendererProfileSetting.Automatic,
                        OutputDirectory = "exports/android",
                        Signing = new Electron2D.Electron2DExportSigningSettings
                        {
                            Required = true,
                            Identity = "android-release",
                            CredentialReference = "env:E2D_ANDROID_KEYSTORE"
                        }
                    }
                ]
            };

            Electron2D.Electron2DExportPresetStore.Save(path, document);

            var firstWrite = File.ReadAllText(path);
            var result = Electron2D.Electron2DExportPresetStore.Load(path);

            Assert.True(result.Succeeded, FormatDiagnostics(result.Diagnostics));
            Assert.NotNull(result.Document);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(new[] { "android-release", "windows-debug" }, result.Document.Presets.Select(preset => preset.Name));
            Assert.Equal(Electron2D.Electron2DExportTarget.AndroidArm64, result.Document.Presets[0].Target);
            Assert.Equal(Electron2D.Electron2DExportConfiguration.Release, result.Document.Presets[0].Configuration);
            Assert.Equal("android-arm64", result.Document.Presets[0].RuntimeIdentifier);
            Assert.True(result.Document.Presets[0].Signing.Required);
            Assert.Equal("env:E2D_ANDROID_KEYSTORE", result.Document.Presets[0].Signing.CredentialReference);

            Electron2D.Electron2DExportPresetStore.Save(path, result.Document);

            Assert.Equal(firstWrite, File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DuplicateExportPresetNamesFailClosedWithDiagnostics()
    {
        var directory = CreateTemporaryDirectory("electron2d-duplicate-export-presets-");
        var path = Path.Combine(directory, "export_presets.e2export.json");

        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "format": "Electron2D.ExportPresets",
                  "formatVersion": 1,
                  "presets": [
                    {
                      "name": "duplicate",
                      "target": "WindowsX64",
                      "configuration": "Debug",
                      "runtimeIdentifier": "win-x64",
                      "selfContained": true,
                      "rendererProfile": "Compatibility",
                      "outputDirectory": "exports/windows",
                      "includeDebugSymbols": true,
                      "signing": { "required": false, "identity": "", "credentialReference": "" }
                    },
                    {
                      "name": "duplicate",
                      "target": "LinuxX64",
                      "configuration": "Release",
                      "runtimeIdentifier": "linux-x64",
                      "selfContained": true,
                      "rendererProfile": "Standard",
                      "outputDirectory": "exports/linux",
                      "includeDebugSymbols": false,
                      "signing": { "required": false, "identity": "", "credentialReference": "" }
                    }
                  ]
                }
                """);

            var result = Electron2D.Electron2DExportPresetStore.Load(path);

            Assert.False(result.Succeeded);
            Assert.Null(result.Document);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "E2D-EXPORT-PRESET-0001");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DesktopPresetValidationSucceedsWhenDotnetSdkIsAvailable()
    {
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "windows-debug",
            Target = Electron2D.Electron2DExportTarget.WindowsX64,
            Configuration = Electron2D.Electron2DExportConfiguration.Debug,
            RuntimeIdentifier = "win-x64",
            OutputDirectory = "exports/windows",
            RendererProfile = Electron2D.Electron2DRendererProfileSetting.Compatibility
        };
        var environment = new Electron2D.Electron2DExportToolchainEnvironment
        {
            DotnetSdkAvailable = true
        };

        var result = Electron2D.Electron2DExportToolchainValidator.Validate(preset, environment);

        Assert.True(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void AndroidReleasePresetValidationFailsClosedWithoutSdkNdkAndSigning()
    {
        var preset = new Electron2D.Electron2DExportPreset
        {
            Name = "android-release",
            Target = Electron2D.Electron2DExportTarget.AndroidArm64,
            Configuration = Electron2D.Electron2DExportConfiguration.Release,
            RuntimeIdentifier = "android-arm64",
            OutputDirectory = "exports/android",
            Signing = new Electron2D.Electron2DExportSigningSettings
            {
                Required = true,
                Identity = "android-release",
                CredentialReference = "env:SECRET_KEYSTORE"
            }
        };
        var environment = new Electron2D.Electron2DExportToolchainEnvironment
        {
            DotnetSdkAvailable = false,
            AndroidSdkPath = "",
            AndroidNdkPath = "",
            SigningIdentityAvailable = false,
            SigningCredentialReferenceAvailable = false
        };

        var result = Electron2D.Electron2DExportToolchainValidator.Validate(preset, environment);

        Assert.False(result.Succeeded);
        Assert.Equal(
            new[]
            {
                "E2D-EXPORT-DOTNET-0001",
                "E2D-EXPORT-ANDROID-0001",
                "E2D-EXPORT-ANDROID-0002",
                "E2D-EXPORT-ANDROID-0016",
                "E2D-EXPORT-SIGNING-0001",
                "E2D-EXPORT-SIGNING-0002"
            },
            result.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.All(result.Diagnostics, diagnostic => Assert.DoesNotContain("SECRET_KEYSTORE", diagnostic.Message, StringComparison.Ordinal));
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string FormatDiagnostics(IEnumerable<Electron2D.Electron2DExportDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }
}
