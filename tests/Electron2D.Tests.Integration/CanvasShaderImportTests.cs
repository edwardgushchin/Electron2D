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
using System.Text;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class CanvasShaderImportTests
{
    [Fact]
    public void PipelineCompilesVertexAndFragmentStagesForDesktopAndIosTargets()
    {
        var compiler = new FakeCanvasShaderCompiler();
        var shader = new Electron2D.Shader
        {
            Code = ValidCanvasShaderCode
        };
        var request = new Electron2D.CanvasShaderImportRequest(
            "res://shaders/water.e2shader",
            shader,
            new[]
            {
                Electron2D.CanvasShaderTargetPlatform.Windows,
                Electron2D.CanvasShaderTargetPlatform.Ios
            });

        var result = Electron2D.CanvasShaderImportPipeline.Import(request, compiler);

        Assert.True(result.Success);
        Assert.False(result.RequiresRuntimeCompilation);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(4, result.CompiledStages.Count);
        Assert.Contains(result.CompiledStages, item =>
            item.Stage == Electron2D.CanvasShaderStage.Vertex &&
            item.TargetPlatform == Electron2D.CanvasShaderTargetPlatform.Ios &&
            item.EntryPoint == "VSMain");
        Assert.Contains(result.CompiledStages, item =>
            item.Stage == Electron2D.CanvasShaderStage.Fragment &&
            item.TargetPlatform == Electron2D.CanvasShaderTargetPlatform.Ios &&
            item.EntryPoint == "PSMain");
        Assert.Equal(
            new[]
            {
                (Electron2D.CanvasShaderStage.Vertex, Electron2D.CanvasShaderTargetPlatform.Windows, "VSMain"),
                (Electron2D.CanvasShaderStage.Fragment, Electron2D.CanvasShaderTargetPlatform.Windows, "PSMain"),
                (Electron2D.CanvasShaderStage.Vertex, Electron2D.CanvasShaderTargetPlatform.Ios, "VSMain"),
                (Electron2D.CanvasShaderStage.Fragment, Electron2D.CanvasShaderTargetPlatform.Ios, "PSMain")
            },
            compiler.Requests.Select(item => (item.Stage, item.TargetPlatform, item.EntryPoint)).ToArray());
    }

    [Fact]
    public void PipelineMapsCompilerDiagnosticsToFileLineColumnStageAndTarget()
    {
        var compiler = new FakeCanvasShaderCompiler
        {
            FailureText = "res://shaders/bad.e2shader(7,13): error X3000: unexpected token"
        };
        var shader = new Electron2D.Shader
        {
            Code = ValidCanvasShaderCode
        };
        var request = new Electron2D.CanvasShaderImportRequest(
            "res://shaders/bad.e2shader",
            shader,
            new[] { Electron2D.CanvasShaderTargetPlatform.Windows });

        var result = Electron2D.CanvasShaderImportPipeline.Import(request, compiler);

        Assert.False(result.Success);
        Assert.True(result.RequiresRuntimeCompilation);
        Assert.Empty(result.CompiledStages);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(Electron2D.CanvasShaderDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("res://shaders/bad.e2shader", diagnostic.FilePath);
        Assert.Equal(7, diagnostic.Line);
        Assert.Equal(13, diagnostic.Column);
        Assert.Equal(Electron2D.CanvasShaderStage.Vertex, diagnostic.Stage);
        Assert.Equal(Electron2D.CanvasShaderTargetPlatform.Windows, diagnostic.TargetPlatform);
        Assert.Contains("unexpected token", diagnostic.Message, StringComparison.Ordinal);
    }

    private const string ValidCanvasShaderCode = """
        shader_type canvas_item;
        vertex_entry VSMain;
        fragment_entry PSMain;

        struct VSInput
        {
            float2 position : POSITION;
        };

        float4 VSMain(VSInput input) : SV_Position
        {
            return float4(input.position, 0.0, 1.0);
        }

        float4 PSMain() : SV_Target
        {
            return float4(1.0, 1.0, 1.0, 1.0);
        }
        """;

    private sealed class FakeCanvasShaderCompiler : Electron2D.ICanvasShaderCompiler
    {
        public List<Electron2D.CanvasShaderCompileRequest> Requests { get; } = new();

        public string? FailureText { get; init; }

        public Electron2D.CanvasShaderCompileResult Compile(Electron2D.CanvasShaderCompileRequest request)
        {
            Requests.Add(request);
            if (FailureText is not null)
            {
                return Electron2D.CanvasShaderCompileResult.Failure(FailureText);
            }

            return Electron2D.CanvasShaderCompileResult.FromBytecode(
                Encoding.UTF8.GetBytes($"{request.TargetPlatform}:{request.Stage}:{request.EntryPoint}"));
        }
    }
}
