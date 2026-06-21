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

internal static class CanvasShaderImportPipeline
{
    private static readonly Regex ShaderTypePattern = new(
        "^shader_type\\s+(?<mode>[A-Za-z_][A-Za-z0-9_]*)\\s*;\\s*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex VertexEntryPattern = new(
        "^vertex_entry\\s+(?<entry>[A-Za-z_][A-Za-z0-9_]*)\\s*;\\s*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex FragmentEntryPattern = new(
        "^fragment_entry\\s+(?<entry>[A-Za-z_][A-Za-z0-9_]*)\\s*;\\s*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static CanvasShaderImportResult Import(
        CanvasShaderImportRequest request,
        ICanvasShaderCompiler compiler)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(compiler);

        var source = ParseSource(request);
        if (source.Diagnostic is not null)
        {
            return Failure(source.Diagnostic);
        }

        var compiledStages = new List<CanvasShaderCompiledStage>();
        foreach (var target in request.TargetPlatforms)
        {
            foreach (var stage in source.GetStages())
            {
                var compileRequest = new CanvasShaderCompileRequest(
                    request.FilePath,
                    source.HlslSource,
                    stage.EntryPoint,
                    stage.Stage,
                    target);
                var compileResult = compiler.Compile(compileRequest);
                if (!compileResult.Success)
                {
                    return Failure(CanvasShaderDiagnostic.FromCompilerOutput(
                        request.FilePath,
                        compileResult.DiagnosticText,
                        stage.Stage,
                        target));
                }

                compiledStages.Add(new CanvasShaderCompiledStage(
                    stage.Stage,
                    target,
                    stage.EntryPoint,
                    compileResult.Bytecode));
            }
        }

        return new CanvasShaderImportResult(
            success: true,
            requiresRuntimeCompilation: false,
            Array.Empty<CanvasShaderDiagnostic>(),
            compiledStages);
    }

    private static CanvasShaderImportResult Failure(CanvasShaderDiagnostic diagnostic)
    {
        return new CanvasShaderImportResult(
            success: false,
            requiresRuntimeCompilation: true,
            new[] { diagnostic },
            Array.Empty<CanvasShaderCompiledStage>());
    }

    private static ParsedShaderSource ParseSource(CanvasShaderImportRequest request)
    {
        var lines = NormalizeLineEndings(request.Shader.Code).Split('\n');
        var hlslLines = new string[lines.Length];
        string? mode = null;
        string? vertexEntry = null;
        string? fragmentEntry = null;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var trimmed = line.Trim();
            var lineNumber = index + 1;

            var shaderType = ShaderTypePattern.Match(trimmed);
            if (shaderType.Success)
            {
                mode = shaderType.Groups["mode"].Value;
                hlslLines[index] = string.Empty;
                continue;
            }

            var vertex = VertexEntryPattern.Match(trimmed);
            if (vertex.Success)
            {
                vertexEntry = vertex.Groups["entry"].Value;
                hlslLines[index] = string.Empty;
                continue;
            }

            var fragment = FragmentEntryPattern.Match(trimmed);
            if (fragment.Success)
            {
                fragmentEntry = fragment.Groups["entry"].Value;
                hlslLines[index] = string.Empty;
                continue;
            }

            hlslLines[index] = line;

            if (trimmed.StartsWith("shader_type", StringComparison.Ordinal) ||
                trimmed.StartsWith("vertex_entry", StringComparison.Ordinal) ||
                trimmed.StartsWith("fragment_entry", StringComparison.Ordinal))
            {
                return ParsedShaderSource.WithError(CanvasShaderDiagnostic.Error(
                    request.FilePath,
                    lineNumber,
                    1,
                    $"Invalid canvas shader header directive: {trimmed}"));
            }
        }

        if (!string.Equals(mode, "canvas_item", StringComparison.Ordinal))
        {
            return ParsedShaderSource.WithError(CanvasShaderDiagnostic.Error(
                request.FilePath,
                0,
                0,
                "Canvas shader source must declare `shader_type canvas_item;`."));
        }

        if (string.IsNullOrWhiteSpace(vertexEntry))
        {
            return ParsedShaderSource.WithError(CanvasShaderDiagnostic.Error(
                request.FilePath,
                0,
                0,
                "Canvas shader source must declare `vertex_entry <name>;`."));
        }

        if (string.IsNullOrWhiteSpace(fragmentEntry))
        {
            return ParsedShaderSource.WithError(CanvasShaderDiagnostic.Error(
                request.FilePath,
                0,
                0,
                "Canvas shader source must declare `fragment_entry <name>;`."));
        }

        return new ParsedShaderSource(
            string.Join('\n', hlslLines),
            vertexEntry,
            fragmentEntry,
            diagnostic: null);
    }

    private static string NormalizeLineEndings(string source)
    {
        return source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private sealed class ParsedShaderSource
    {
        public ParsedShaderSource(
            string hlslSource,
            string vertexEntryPoint,
            string fragmentEntryPoint,
            CanvasShaderDiagnostic? diagnostic)
        {
            HlslSource = hlslSource;
            VertexEntryPoint = vertexEntryPoint;
            FragmentEntryPoint = fragmentEntryPoint;
            Diagnostic = diagnostic;
        }

        public string HlslSource { get; }

        public string VertexEntryPoint { get; }

        public string FragmentEntryPoint { get; }

        public CanvasShaderDiagnostic? Diagnostic { get; }

        public static ParsedShaderSource WithError(CanvasShaderDiagnostic diagnostic)
        {
            return new ParsedShaderSource(string.Empty, string.Empty, string.Empty, diagnostic);
        }

        public IEnumerable<(CanvasShaderStage Stage, string EntryPoint)> GetStages()
        {
            yield return (CanvasShaderStage.Vertex, VertexEntryPoint);
            yield return (CanvasShaderStage.Fragment, FragmentEntryPoint);
        }
    }
}
