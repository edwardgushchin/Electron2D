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

internal sealed class Electron2DExportPresetLoadResult
{
    private Electron2DExportPresetLoadResult(Electron2DExportPresetDocument? document, Electron2DExportDiagnostic[] diagnostics)
    {
        Document = document;
        Diagnostics = diagnostics;
    }

    public bool Succeeded => Document is not null && Diagnostics.Length == 0;

    public Electron2DExportPresetDocument? Document { get; }

    public Electron2DExportDiagnostic[] Diagnostics { get; }

    public static Electron2DExportPresetLoadResult Success(Electron2DExportPresetDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new Electron2DExportPresetLoadResult(document, []);
    }

    public static Electron2DExportPresetLoadResult Failure(Electron2DExportDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return new Electron2DExportPresetLoadResult(null, [diagnostic]);
    }
}
