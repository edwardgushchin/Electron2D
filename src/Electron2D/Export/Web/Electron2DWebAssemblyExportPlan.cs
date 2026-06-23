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

internal sealed class Electron2DWebAssemblyExportPlan
{
    public string ProjectFilePath { get; init; } = string.Empty;

    public Electron2DExportConfiguration Configuration { get; init; }

    public string RuntimeIdentifier { get; init; } = "browser-wasm";

    public bool SelfContained { get; init; } = true;

    public string OutputDirectory { get; init; } = "exports/web";

    public string WebRootDirectory { get; init; } = string.Empty;

    public string FrameworkDirectory { get; init; } = string.Empty;

    public string AssetsDirectory { get; init; } = string.Empty;

    public string IndexHtmlPath { get; init; } = string.Empty;

    public string LoaderScriptPath { get; init; } = string.Empty;

    public string WebManifestPath { get; init; } = string.Empty;

    public string[] PublishArguments { get; init; } = [];

    public Electron2DRendererProfileSetting RendererProfile { get; init; }

    public string GraphicsBackend { get; init; } = "web-automatic";

    public string[] RequiredFiles { get; init; } = [];

    public string[] BrowserPolicies { get; init; } = [];

    public string[] SmokeCriteria { get; init; } = [];

    public bool IncludeDebugSymbols { get; init; }

    public bool SigningRequired { get; init; }

    public string AudioPolicy { get; init; } = "userGestureRequired";

    public string FilesystemPolicy { get; init; } = "browserSandbox";
}
