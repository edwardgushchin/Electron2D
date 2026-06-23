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

internal sealed class Electron2DAndroidExportPlan
{
    public string ProjectFilePath { get; init; } = string.Empty;

    public Electron2DExportConfiguration Configuration { get; init; }

    public string RuntimeIdentifier { get; init; } = "android-arm64";

    public string Abi { get; init; } = "arm64-v8a";

    public string PackageFormat { get; init; } = "apk";

    public string DotnetCommand { get; init; } = "build";

    public bool SelfContained { get; init; } = true;

    public string OutputDirectory { get; init; } = "exports/android/debug";

    public string StagingDirectory { get; init; } = string.Empty;

    public string ArtifactsDirectory { get; init; } = string.Empty;

    public string SmokeDirectory { get; init; } = string.Empty;

    public string AndroidProjectFilePath { get; init; } = string.Empty;

    public string MainActivityPath { get; init; } = string.Empty;

    public string ManifestPath { get; init; } = string.Empty;

    public string ExportMetadataPath { get; init; } = string.Empty;

    public string ProjectAssetsDirectory { get; init; } = string.Empty;

    public string[] BuildArguments { get; init; } = [];

    public Electron2DRendererProfileSetting RendererProfile { get; init; }

    public string GraphicsBackend { get; init; } = "mobile-automatic";

    public string MobileGraphicsProfile { get; init; } = "android-mobile";

    public string FallbackPolicy { get; init; } = "compatibility-renderer-fallback";

    public string[] RequiredFiles { get; init; } = [];

    public string[] MobilePolicies { get; init; } = [];

    public string[] SmokeCriteria { get; init; } = [];

    public bool IncludeDebugSymbols { get; init; }

    public bool SigningRequired { get; init; }

    public string SigningIdentity { get; init; } = string.Empty;

    public string SigningCredentialReference { get; init; } = string.Empty;

    public string Orientation { get; init; } = "landscape";
}
