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
using System.Text.RegularExpressions;

namespace Electron2D;

internal sealed class CanvasShaderDiagnostic
{
    private static readonly Regex CompilerLocationPattern = new(
        "^(?<file>.+)\\((?<line>\\d+),(?<column>\\d+)\\):\\s*(?<message>.*)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public CanvasShaderDiagnostic(
        CanvasShaderDiagnosticSeverity severity,
        string filePath,
        int line,
        int column,
        string message,
        CanvasShaderStage? stage = null,
        CanvasShaderTargetPlatform? targetPlatform = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(line);
        ArgumentOutOfRangeException.ThrowIfNegative(column);

        Severity = severity;
        FilePath = filePath ?? string.Empty;
        Line = line;
        Column = column;
        Message = message ?? string.Empty;
        Stage = stage;
        TargetPlatform = targetPlatform;
    }

    public CanvasShaderDiagnosticSeverity Severity { get; }

    public string FilePath { get; }

    public int Line { get; }

    public int Column { get; }

    public string Message { get; }

    public CanvasShaderStage? Stage { get; }

    public CanvasShaderTargetPlatform? TargetPlatform { get; }

    public static CanvasShaderDiagnostic Error(
        string filePath,
        int line,
        int column,
        string message,
        CanvasShaderStage? stage = null,
        CanvasShaderTargetPlatform? targetPlatform = null)
    {
        return new CanvasShaderDiagnostic(
            CanvasShaderDiagnosticSeverity.Error,
            filePath,
            line,
            column,
            message,
            stage,
            targetPlatform);
    }

    public static CanvasShaderDiagnostic FromCompilerOutput(
        string filePath,
        string compilerOutput,
        CanvasShaderStage stage,
        CanvasShaderTargetPlatform targetPlatform)
    {
        var text = compilerOutput ?? string.Empty;
        var match = CompilerLocationPattern.Match(text);
        if (!match.Success)
        {
            return Error(filePath, 0, 0, text, stage, targetPlatform);
        }

        var line = int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var column = int.Parse(match.Groups["column"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var message = match.Groups["message"].Value;
        var severity = message.TrimStart().StartsWith("warning", StringComparison.OrdinalIgnoreCase)
            ? CanvasShaderDiagnosticSeverity.Warning
            : CanvasShaderDiagnosticSeverity.Error;

        return new CanvasShaderDiagnostic(
            severity,
            match.Groups["file"].Value,
            line,
            column,
            message,
            stage,
            targetPlatform);
    }
}
