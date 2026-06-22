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
using Electron2D.Editor.Scripting;

namespace Electron2D.Editor.Run;

internal sealed class EditorRunDiagnostic
{
    public EditorRunDiagnostic(
        EditorRunDiagnosticSource source,
        string severity,
        string code,
        string filePath,
        int line,
        int column,
        string message,
        string stackTrace = "")
    {
        Source = source;
        Severity = severity ?? string.Empty;
        Code = code ?? string.Empty;
        FilePath = filePath ?? string.Empty;
        Line = line;
        Column = column;
        Message = message ?? string.Empty;
        StackTrace = stackTrace ?? string.Empty;
    }

    public EditorRunDiagnosticSource Source { get; }

    public string Severity { get; }

    public string Code { get; }

    public string FilePath { get; }

    public int Line { get; }

    public int Column { get; }

    public string Message { get; }

    public string StackTrace { get; }

    public static EditorRunDiagnostic FromCompiler(EditorProjectDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return new EditorRunDiagnostic(
            EditorRunDiagnosticSource.Compiler,
            diagnostic.Severity.ToString(),
            diagnostic.Code,
            diagnostic.File,
            diagnostic.Line,
            diagnostic.Column,
            diagnostic.Message);
    }

    public static EditorRunDiagnostic FromShader(CanvasShaderDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return new EditorRunDiagnostic(
            EditorRunDiagnosticSource.Shader,
            diagnostic.Severity.ToString(),
            "Shader",
            diagnostic.FilePath,
            diagnostic.Line,
            diagnostic.Column,
            diagnostic.Message);
    }
}
