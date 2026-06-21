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

internal sealed class ShaderImportMetadata
{
    public const string FormatName = "Electron2D.ShaderImportMetadata";
    public const int CurrentVersion = 1;

    public ShaderImportMetadata(
        string sourcePath,
        long uid,
        bool requiresRuntimeCompilation,
        IEnumerable<ShaderImportCompiledStage> stages,
        IEnumerable<CanvasShaderDiagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(diagnostics);

        if (uid <= 0 || uid == ResourceUid.InvalidId)
        {
            throw new ArgumentException("Shader import UID must be positive.", nameof(uid));
        }

        SourcePath = sourcePath;
        Uid = uid;
        RequiresRuntimeCompilation = requiresRuntimeCompilation;
        Stages = stages
            .OrderBy(stage => stage.TargetPlatform)
            .ThenBy(stage => stage.Stage)
            .ThenBy(stage => stage.EntryPoint, StringComparer.Ordinal)
            .ToArray();
        Diagnostics = diagnostics
            .OrderBy(diagnostic => diagnostic.FilePath, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Line)
            .ThenBy(diagnostic => diagnostic.Column)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();
    }

    public string SourcePath { get; }

    public long Uid { get; }

    public string UidText => ResourceUid.IdToText(Uid);

    public bool RequiresRuntimeCompilation { get; }

    public IReadOnlyList<ShaderImportCompiledStage> Stages { get; }

    public IReadOnlyList<CanvasShaderDiagnostic> Diagnostics { get; }

    public bool HasPrecompiledArtifacts(CanvasShaderTargetPlatform targetPlatform)
    {
        return Stages.Any(stage => stage.TargetPlatform == targetPlatform && stage.Stage == CanvasShaderStage.Vertex) &&
            Stages.Any(stage => stage.TargetPlatform == targetPlatform && stage.Stage == CanvasShaderStage.Fragment);
    }
}

internal sealed class ShaderImportCompiledStage
{
    public ShaderImportCompiledStage(
        CanvasShaderStage stage,
        CanvasShaderTargetPlatform targetPlatform,
        string entryPoint,
        byte[] bytecode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryPoint);
        ArgumentNullException.ThrowIfNull(bytecode);

        Stage = stage;
        TargetPlatform = targetPlatform;
        EntryPoint = entryPoint;
        Bytecode = bytecode.ToArray();
    }

    public CanvasShaderStage Stage { get; }

    public CanvasShaderTargetPlatform TargetPlatform { get; }

    public string EntryPoint { get; }

    public byte[] Bytecode { get; }
}
