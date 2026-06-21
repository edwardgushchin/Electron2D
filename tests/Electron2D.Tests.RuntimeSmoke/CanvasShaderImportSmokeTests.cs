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

namespace Electron2D.Tests.RuntimeSmoke;

public sealed class CanvasShaderImportSmokeTests
{
    [Fact]
    public void ImportedIosCanvasShaderDoesNotRequireRuntimeCompilation()
    {
        var shader = new Electron2D.Shader
        {
            Code = """
                shader_type canvas_item;
                vertex_entry VSMain;
                fragment_entry PSMain;

                float4 VSMain(float2 position : POSITION) : SV_Position
                {
                    return float4(position, 0.0, 1.0);
                }

                float4 PSMain() : SV_Target
                {
                    return float4(1.0, 1.0, 1.0, 1.0);
                }
                """
        };

        var result = Electron2D.CanvasShaderImportPipeline.Import(
            new Electron2D.CanvasShaderImportRequest(
                "res://shaders/mobile.e2shader",
                shader,
                new[] { Electron2D.CanvasShaderTargetPlatform.Ios }),
            new SmokeShaderCompiler());

        Assert.True(result.Success);
        Assert.False(result.RequiresRuntimeCompilation);
        Assert.Equal(2, result.CompiledStages.Count);
        Assert.All(result.CompiledStages, stage =>
        {
            Assert.Equal(Electron2D.CanvasShaderTargetPlatform.Ios, stage.TargetPlatform);
            Assert.NotEmpty(stage.Bytecode);
        });
    }

    private sealed class SmokeShaderCompiler : Electron2D.ICanvasShaderCompiler
    {
        public Electron2D.CanvasShaderCompileResult Compile(Electron2D.CanvasShaderCompileRequest request)
        {
            return Electron2D.CanvasShaderCompileResult.FromBytecode(
                Encoding.UTF8.GetBytes($"{request.TargetPlatform}:{request.Stage}:{request.EntryPoint}"));
        }
    }
}
