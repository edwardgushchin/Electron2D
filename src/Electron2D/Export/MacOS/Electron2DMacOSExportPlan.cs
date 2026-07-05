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

internal sealed class Electron2DMacOSExportPlan
{
    public string ProjectFilePath { get; init; } = "";

    public Electron2DExportConfiguration Configuration { get; init; }

    public string RuntimeIdentifier { get; init; } = "osx-arm64";

    public string Architecture { get; init; } = "arm64";

    public bool SelfContained { get; init; }

    public string OutputDirectory { get; init; } = "";

    public string PublishOutputDirectory { get; init; } = "";

    public string AppBundlePath { get; init; } = "";

    public string ContentsDirectory { get; init; } = "";

    public string MacOSDirectory { get; init; } = "";

    public string ResourcesDirectory { get; init; } = "";

    public string ExecutablePath { get; init; } = "";

    public string InfoPlistPath { get; init; } = "";

    public string BundleName { get; init; } = "";

    public string ExecutableName { get; init; } = "";

    public string BundleIdentifier { get; init; } = "";

    public string[] PublishArguments { get; init; } = [];

    public Electron2DRendererProfileSetting RendererProfile { get; init; }

    public string GraphicsBackend { get; init; } = "metal";

    public string[] RequiredBundleFiles { get; init; } = [];

    public string[] UnsupportedRuntimeIdentifiers { get; init; } = [];

    public string X64Policy { get; init; } = "unsupported-in-0.1-preview";

    public bool IncludeDebugSymbols { get; init; }

    public bool SigningRequired { get; init; }

    public string SigningIdentity { get; init; } = "";

    public string SigningCredentialReference { get; init; } = "";

    public string[] CodesignArguments { get; init; } = [];
}
