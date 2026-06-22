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
namespace Electron2D.Editor.Run;

internal sealed class EditorRunDiagnosticStore
{
    private readonly List<EditorRunDiagnostic> diagnostics = new();

    public IReadOnlyList<EditorRunDiagnostic> Diagnostics => diagnostics;

    public void AddRange(IEnumerable<EditorRunDiagnostic> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        diagnostics.AddRange(values);
    }

    public IReadOnlyList<EditorRunDiagnostic> LoadShaderDiagnostics(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        var cacheRoot = Path.Combine(Path.GetFullPath(projectPath), ".electron2d", "import-cache");
        if (!Directory.Exists(cacheRoot))
        {
            return [];
        }

        var shaderDiagnostics = new List<EditorRunDiagnostic>();
        foreach (var file in Directory.EnumerateFiles(cacheRoot, "shader.e2shader.json", SearchOption.AllDirectories))
        {
            try
            {
                var metadata = ShaderImportMetadataTextSerializer.Deserialize(File.ReadAllText(file));
                shaderDiagnostics.AddRange(metadata.Diagnostics.Select(EditorRunDiagnostic.FromShader));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException)
            {
                shaderDiagnostics.Add(new EditorRunDiagnostic(
                    EditorRunDiagnosticSource.Shader,
                    "Error",
                    "ShaderMetadata",
                    file,
                    0,
                    0,
                    exception.Message));
            }
        }

        AddRange(shaderDiagnostics);
        return shaderDiagnostics;
    }
}
