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

public sealed class ShaderSourceImportCacheTests
{
    [Fact]
    public void ShaderSourceImporterWritesPlatformArtifactsAndIosDoesNotRequireRuntimeCompilation()
    {
        using var project = ShaderImportTestProject.Create(new FakeCanvasShaderCompiler());
        project.WriteText("shaders/water.e2shader", ValidShaderCode);
        project.WriteText("shaders/water.e2shader.e2import.json", "{ \"targets\": [ \"Windows\", \"Ios\" ] }");

        var report = project.CreatePipeline().ImportAll();

        var item = Assert.Single(report.Items);
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, item.Status);
        var metadata = project.ReadShaderMetadata(item);

        Assert.False(metadata.RequiresRuntimeCompilation);
        Assert.Empty(metadata.Diagnostics);
        Assert.Equal(4, metadata.Stages.Count);
        Assert.True(metadata.HasPrecompiledArtifacts(Electron2D.CanvasShaderTargetPlatform.Ios));
        Assert.Contains(metadata.Stages, stage =>
            stage.TargetPlatform == Electron2D.CanvasShaderTargetPlatform.Ios &&
            stage.Stage == Electron2D.CanvasShaderStage.Vertex &&
            stage.EntryPoint == "VSMain" &&
            Encoding.UTF8.GetString(stage.Bytecode) == "Ios:Vertex:VSMain");
    }

    [Fact]
    public void ShaderSourceImporterStoresDiagnosticsWithFileLineColumn()
    {
        using var project = ShaderImportTestProject.Create(new FakeCanvasShaderCompiler
        {
            FailureText = "res://shaders/bad.e2shader(7,13): error X3000: unexpected token"
        });
        project.WriteText("shaders/bad.e2shader", ValidShaderCode);
        project.WriteText("shaders/bad.e2shader.e2import.json", "{ \"targets\": [ \"Windows\" ] }");

        var report = project.CreatePipeline().ImportAll();

        var metadata = project.ReadShaderMetadata(Assert.Single(report.Items));
        Assert.True(metadata.RequiresRuntimeCompilation);
        Assert.Empty(metadata.Stages);
        var diagnostic = Assert.Single(metadata.Diagnostics);
        Assert.Equal("res://shaders/bad.e2shader", diagnostic.FilePath);
        Assert.Equal(7, diagnostic.Line);
        Assert.Equal(13, diagnostic.Column);
        Assert.Equal(Electron2D.CanvasShaderStage.Vertex, diagnostic.Stage);
        Assert.Equal(Electron2D.CanvasShaderTargetPlatform.Windows, diagnostic.TargetPlatform);
    }

    [Fact]
    public void ShaderSourceImporterTracksSidecarAsDependency()
    {
        using var project = ShaderImportTestProject.Create(new FakeCanvasShaderCompiler());
        project.WriteText("shaders/water.e2shader", ValidShaderCode);
        project.WriteText("shaders/water.e2shader.e2import.json", "{ \"targets\": [ \"Windows\" ] }");

        var first = project.CreatePipeline().ImportAll();
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, Assert.Single(first.Items).Status);

        var second = project.CreatePipeline().ImportAll();
        Assert.Equal(Electron2D.ResourceImportItemStatus.UpToDate, Assert.Single(second.Items).Status);

        project.WriteText("shaders/water.e2shader.e2import.json", "{ \"targets\": [ \"Windows\", \"Ios\" ] }");

        var third = project.CreatePipeline().ImportAll();
        var item = Assert.Single(third.Items);
        Assert.Equal(Electron2D.ResourceImportItemStatus.Imported, item.Status);
        Assert.Equal(Electron2D.ResourceImportReason.DependencyChanged, item.Reason);
        Assert.True(project.ReadShaderMetadata(item).HasPrecompiledArtifacts(Electron2D.CanvasShaderTargetPlatform.Ios));
    }

    private const string ValidShaderCode = """
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
        """;

    private sealed class ShaderImportTestProject : IDisposable
    {
        private readonly Electron2D.ICanvasShaderCompiler compiler;

        private ShaderImportTestProject(string root, Electron2D.ICanvasShaderCompiler compiler)
        {
            this.compiler = compiler;
            Root = root;
            SourceRoot = Path.Combine(root, "sources");
            CacheRoot = Path.Combine(root, ".electron2d", "import-cache");
            Directory.CreateDirectory(SourceRoot);
            Directory.CreateDirectory(CacheRoot);
        }

        public string Root { get; }

        public string SourceRoot { get; }

        public string CacheRoot { get; }

        public static ShaderImportTestProject Create(Electron2D.ICanvasShaderCompiler compiler)
        {
            return new ShaderImportTestProject(Path.Combine(
                Path.GetTempPath(),
                "Electron2D.ShaderImportTests",
                Guid.NewGuid().ToString("N")), compiler);
        }

        public Electron2D.ResourceImportPipeline CreatePipeline()
        {
            return new Electron2D.ResourceImportPipeline(new Electron2D.ResourceImportOptions(
                Root,
                SourceRoot,
                CacheRoot,
                [new Electron2D.ShaderSourceImporter(compiler)]));
        }

        public Electron2D.ShaderImportMetadata ReadShaderMetadata(Electron2D.ResourceImportItemReport item)
        {
            return Electron2D.ShaderImportMetadataTextSerializer.Deserialize(File.ReadAllText(Assert.Single(item.CacheFiles)));
        }

        public void WriteText(string relativePath, string text)
        {
            var path = Path.Combine(SourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class FakeCanvasShaderCompiler : Electron2D.ICanvasShaderCompiler
    {
        public string? FailureText { get; init; }

        public Electron2D.CanvasShaderCompileResult Compile(Electron2D.CanvasShaderCompileRequest request)
        {
            if (FailureText is not null)
            {
                return Electron2D.CanvasShaderCompileResult.Failure(FailureText);
            }

            return Electron2D.CanvasShaderCompileResult.FromBytecode(
                Encoding.UTF8.GetBytes($"{request.TargetPlatform}:{request.Stage}:{request.EntryPoint}"));
        }
    }
}
