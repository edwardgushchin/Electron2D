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
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D;

internal sealed class ShaderSourceImporter : IResourceImporter
{
    private const string SidecarSuffix = ".e2import.json";
    private readonly ICanvasShaderCompiler compiler;

    public ShaderSourceImporter(ICanvasShaderCompiler compiler)
    {
        this.compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
    }

    public string Name => "Electron2D.ShaderSource";

    public bool CanImport(ResourceImportSourceFile source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Extension.Equals(".e2shader", StringComparison.OrdinalIgnoreCase);
    }

    public ResourceImportOutput Import(ResourceImportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var settings = ShaderImportSettings.ReadIfExists(
            context.Source.AbsolutePath + SidecarSuffix,
            context.Source.ResourcePath + SidecarSuffix);
        var shader = new Shader
        {
            Code = context.ReadSourceText()
        };
        var result = CanvasShaderImportPipeline.Import(
            new CanvasShaderImportRequest(context.Source.ResourcePath, shader, settings.TargetPlatforms),
            compiler);
        var uid = ResourceUid.CreateIdForPath(context.Source.ResourcePath);
        var metadata = new ShaderImportMetadata(
            context.Source.ResourcePath,
            uid,
            result.RequiresRuntimeCompilation,
            result.CompiledStages.Select(stage => new ShaderImportCompiledStage(
                stage.Stage,
                stage.TargetPlatform,
                stage.EntryPoint,
                stage.Bytecode)),
            result.Diagnostics);
        var dependencies = settings.Exists
            ? new[] { settings.ResourcePath }
            : Array.Empty<string>();

        return new ResourceImportOutput(
            uid,
            "Electron2D.Shader",
            [ResourceImportArtifact.FromUtf8Text("shader.e2shader.json", ShaderImportMetadataTextSerializer.Serialize(metadata))],
            dependencies);
    }

    private sealed class ShaderImportSettings
    {
        private static readonly CanvasShaderTargetPlatform[] DefaultTargets =
        [
            CanvasShaderTargetPlatform.Windows,
            CanvasShaderTargetPlatform.Linux,
            CanvasShaderTargetPlatform.MacOS,
            CanvasShaderTargetPlatform.Android,
            CanvasShaderTargetPlatform.Ios
        ];

        private ShaderImportSettings(bool exists, string resourcePath, IReadOnlyList<CanvasShaderTargetPlatform> targetPlatforms)
        {
            Exists = exists;
            ResourcePath = resourcePath;
            TargetPlatforms = targetPlatforms;
        }

        public bool Exists { get; }

        public string ResourcePath { get; }

        public IReadOnlyList<CanvasShaderTargetPlatform> TargetPlatforms { get; }

        public static ShaderImportSettings ReadIfExists(string absolutePath, string resourcePath)
        {
            if (!File.Exists(absolutePath))
            {
                return new ShaderImportSettings(false, resourcePath, DefaultTargets);
            }

            try
            {
                var root = ExpectObject(JsonNode.Parse(File.ReadAllText(absolutePath)), "Shader import sidecar");
                var targets = root.TryGetPropertyValue("targets", out var node)
                    ? ReadTargets(ExpectArray(node, "Shader import sidecar targets"))
                    : DefaultTargets;
                return new ShaderImportSettings(true, resourcePath, targets);
            }
            catch (FormatException)
            {
                throw;
            }
            catch (JsonException exception)
            {
                throw new FormatException("Shader import sidecar JSON text is malformed.", exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new FormatException("Shader import sidecar JSON text is malformed.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("Shader import sidecar JSON text is malformed.", exception);
            }
        }

        private static IReadOnlyList<CanvasShaderTargetPlatform> ReadTargets(JsonArray targets)
        {
            var result = new List<CanvasShaderTargetPlatform>();
            foreach (var node in targets)
            {
                if (node is not JsonValue jsonValue ||
                    !jsonValue.TryGetValue<string>(out var text) ||
                    !Enum.TryParse<CanvasShaderTargetPlatform>(text, ignoreCase: false, out var target))
                {
                    throw new FormatException("Shader import target value is not supported.");
                }

                result.Add(target);
            }

            return result.Count == 0
                ? throw new FormatException("Shader import target list must not be empty.")
                : result.Distinct().OrderBy(target => target).ToArray();
        }

        private static JsonObject ExpectObject(JsonNode? node, string description)
        {
            return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
        }

        private static JsonArray ExpectArray(JsonNode? node, string description)
        {
            return node as JsonArray ?? throw new FormatException($"{description} must be a JSON array.");
        }
    }
}
