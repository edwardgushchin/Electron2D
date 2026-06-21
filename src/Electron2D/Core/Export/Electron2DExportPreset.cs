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

internal sealed class Electron2DExportPreset
{
    public string Name { get; set; } = "export";

    public Electron2DExportTarget Target { get; set; } = Electron2DExportTarget.WindowsX64;

    public Electron2DExportConfiguration Configuration { get; set; } = Electron2DExportConfiguration.Debug;

    public string RuntimeIdentifier { get; set; } = "win-x64";

    public bool SelfContained { get; set; } = true;

    public Electron2DRendererProfileSetting RendererProfile { get; set; } = Electron2DRendererProfileSetting.Automatic;

    public string OutputDirectory { get; set; } = "exports";

    public bool IncludeDebugSymbols { get; set; }

    public Electron2DExportSigningSettings Signing { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new FormatException("Export preset name must be a non-empty string.");
        }

        if (!Enum.IsDefined(Target))
        {
            throw new FormatException($"Export target '{Target}' is not supported.");
        }

        if (!Enum.IsDefined(Configuration))
        {
            throw new FormatException($"Export configuration '{Configuration}' is not supported.");
        }

        if (string.IsNullOrWhiteSpace(RuntimeIdentifier))
        {
            throw new FormatException("Export runtime identifier must be a non-empty string.");
        }

        if (!Enum.IsDefined(RendererProfile))
        {
            throw new FormatException($"Export renderer profile '{RendererProfile}' is not supported.");
        }

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            throw new FormatException("Export output directory must be a non-empty string.");
        }

        ArgumentNullException.ThrowIfNull(Signing);
    }
}
