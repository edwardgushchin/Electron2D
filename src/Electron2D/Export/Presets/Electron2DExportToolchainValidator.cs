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
namespace Electron2D;

internal static class Electron2DExportToolchainValidator
{
    public static Electron2DExportValidationResult Validate(
        Electron2DExportPreset preset,
        Electron2DExportToolchainEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(environment);

        var diagnostics = new List<Electron2DExportDiagnostic>();
        try
        {
            preset.Validate();
        }
        catch (FormatException exception)
        {
            diagnostics.Add(Error("E2D-EXPORT-PRESET-0002", preset.Name, exception.Message));
            return new Electron2DExportValidationResult(diagnostics);
        }

        if (!environment.DotnetSdkAvailable)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-DOTNET-0001",
                preset.Name,
                $".NET SDK is required before export preset '{preset.Name}' can run."));
        }

        if (preset.Target == Electron2DExportTarget.AndroidArm64)
        {
            if (string.IsNullOrWhiteSpace(environment.AndroidSdkPath))
            {
                diagnostics.Add(Error(
                    "E2D-EXPORT-ANDROID-0001",
                    preset.Name,
                    $"Android SDK is required before export preset '{preset.Name}' can run."));
            }

            if (string.IsNullOrWhiteSpace(environment.AndroidNdkPath))
            {
                diagnostics.Add(Error(
                    "E2D-EXPORT-ANDROID-0002",
                    preset.Name,
                    $"Android NDK is required before export preset '{preset.Name}' can run."));
            }

            if (string.IsNullOrWhiteSpace(environment.JavaSdkPath))
            {
                diagnostics.Add(Error(
                    "E2D-EXPORT-ANDROID-0016",
                    preset.Name,
                    $"JDK 17 or newer is required before Android export preset '{preset.Name}' can run."));
            }
        }

        if (preset.Target == Electron2DExportTarget.IosArm64 && string.IsNullOrWhiteSpace(environment.XcodePath))
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-IOS-0013",
                preset.Name,
                $"Xcode is required before export preset '{preset.Name}' can run."));
        }

        if (preset.Target == Electron2DExportTarget.WebAssemblyBrowser && !environment.WebAssemblyBuildToolsAvailable)
        {
            diagnostics.Add(Error(
                "E2D-EXPORT-WEB-0001",
                preset.Name,
                $"WebAssembly build tools are required before export preset '{preset.Name}' can run."));
        }

        if (preset.Signing.Required)
        {
            if (!environment.SigningIdentityAvailable)
            {
                diagnostics.Add(Error(
                    "E2D-EXPORT-SIGNING-0001",
                    preset.Name,
                    $"Signing identity is required before export preset '{preset.Name}' can run."));
            }

            if (!environment.SigningCredentialReferenceAvailable)
            {
                diagnostics.Add(Error(
                    "E2D-EXPORT-SIGNING-0002",
                    preset.Name,
                    $"Signing credential reference is required before export preset '{preset.Name}' can run."));
            }
        }

        return new Electron2DExportValidationResult(diagnostics);
    }

    private static Electron2DExportDiagnostic Error(string code, string presetName, string message)
    {
        return new Electron2DExportDiagnostic(code, message, Electron2DExportDiagnosticSeverity.Error, presetName);
    }
}
