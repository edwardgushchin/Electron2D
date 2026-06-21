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

internal static class ShaderImportMetadataTextSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(ShaderImportMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return WriteMetadata(metadata).ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    public static ShaderImportMetadata Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            return ReadMetadata(ExpectObject(JsonNode.Parse(text), "Shader import metadata"));
        }
        catch (FormatException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new FormatException("Shader import metadata JSON text is malformed.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException("Shader import metadata JSON text is malformed.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException("Shader import metadata JSON text is malformed.", exception);
        }
    }

    private static JsonObject WriteMetadata(ShaderImportMetadata metadata)
    {
        return new JsonObject
        {
            ["format"] = ShaderImportMetadata.FormatName,
            ["version"] = ShaderImportMetadata.CurrentVersion,
            ["source"] = metadata.SourcePath,
            ["uid"] = metadata.UidText,
            ["requiresRuntimeCompilation"] = metadata.RequiresRuntimeCompilation,
            ["stages"] = WriteStages(metadata.Stages),
            ["diagnostics"] = WriteDiagnostics(metadata.Diagnostics)
        };
    }

    private static ShaderImportMetadata ReadMetadata(JsonObject root)
    {
        var formatName = ReadString(root, "format", "Shader import metadata format");
        if (formatName != ShaderImportMetadata.FormatName)
        {
            throw new FormatException($"Shader import metadata format '{formatName}' is not supported.");
        }

        var version = ReadInt32(root, "version", "Shader import metadata version");
        if (version != ShaderImportMetadata.CurrentVersion)
        {
            throw new FormatException($"Shader import metadata version '{version}' is not supported.");
        }

        var uidText = ReadString(root, "uid", "Shader import metadata uid");
        var uid = ResourceUid.TextToId(uidText);
        if (uid == ResourceUid.InvalidId)
        {
            throw new FormatException($"Shader import metadata uid '{uidText}' is invalid.");
        }

        return new ShaderImportMetadata(
            ReadString(root, "source", "Shader import metadata source"),
            uid,
            ReadBool(root, "requiresRuntimeCompilation", "Shader import runtime compilation flag"),
            ReadStages(ReadArray(root, "stages", "Shader import compiled stages")),
            ReadDiagnostics(ReadArray(root, "diagnostics", "Shader import diagnostics")));
    }

    private static JsonArray WriteStages(IEnumerable<ShaderImportCompiledStage> stages)
    {
        var result = new JsonArray();
        foreach (var stage in stages)
        {
            result.Add(new JsonObject
            {
                ["stage"] = stage.Stage.ToString(),
                ["target"] = stage.TargetPlatform.ToString(),
                ["entryPoint"] = stage.EntryPoint,
                ["bytecode"] = Convert.ToBase64String(stage.Bytecode)
            });
        }

        return result;
    }

    private static IReadOnlyList<ShaderImportCompiledStage> ReadStages(JsonArray stages)
    {
        var result = new List<ShaderImportCompiledStage>();
        foreach (var node in stages)
        {
            var stage = ExpectObject(node, "Shader import compiled stage");
            result.Add(new ShaderImportCompiledStage(
                ReadEnum<CanvasShaderStage>(stage, "stage", "Shader import stage"),
                ReadEnum<CanvasShaderTargetPlatform>(stage, "target", "Shader import target"),
                ReadString(stage, "entryPoint", "Shader import entry point"),
                Convert.FromBase64String(ReadString(stage, "bytecode", "Shader import bytecode"))));
        }

        return result;
    }

    private static JsonArray WriteDiagnostics(IEnumerable<CanvasShaderDiagnostic> diagnostics)
    {
        var result = new JsonArray();
        foreach (var diagnostic in diagnostics)
        {
            result.Add(new JsonObject
            {
                ["severity"] = diagnostic.Severity.ToString(),
                ["file"] = diagnostic.FilePath,
                ["line"] = diagnostic.Line,
                ["column"] = diagnostic.Column,
                ["message"] = diagnostic.Message,
                ["stage"] = diagnostic.Stage?.ToString(),
                ["target"] = diagnostic.TargetPlatform?.ToString()
            });
        }

        return result;
    }

    private static IReadOnlyList<CanvasShaderDiagnostic> ReadDiagnostics(JsonArray diagnostics)
    {
        var result = new List<CanvasShaderDiagnostic>();
        foreach (var node in diagnostics)
        {
            var diagnostic = ExpectObject(node, "Shader import diagnostic");
            result.Add(new CanvasShaderDiagnostic(
                ReadEnum<CanvasShaderDiagnosticSeverity>(diagnostic, "severity", "Shader diagnostic severity"),
                ReadString(diagnostic, "file", "Shader diagnostic file"),
                ReadInt32(diagnostic, "line", "Shader diagnostic line"),
                ReadInt32(diagnostic, "column", "Shader diagnostic column"),
                ReadString(diagnostic, "message", "Shader diagnostic message"),
                ReadNullableEnum<CanvasShaderStage>(diagnostic, "stage"),
                ReadNullableEnum<CanvasShaderTargetPlatform>(diagnostic, "target")));
        }

        return result;
    }

    private static JsonObject ExpectObject(JsonNode? node, string description)
    {
        return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
    }

    private static JsonArray ReadArray(JsonObject value, string propertyName, string description)
    {
        return ReadRequiredProperty(value, propertyName, description) as JsonArray ??
            throw new FormatException($"{description} must be a JSON array.");
    }

    private static JsonNode ReadRequiredProperty(JsonObject value, string propertyName, string description)
    {
        return value.TryGetPropertyValue(propertyName, out var node)
            ? ReadRequiredNode(node, description)
            : throw new FormatException($"{description} is missing.");
    }

    private static JsonNode ReadRequiredNode(JsonNode? node, string description)
    {
        return node ?? throw new FormatException($"{description} is missing.");
    }

    private static string ReadString(JsonObject value, string propertyName, string description)
    {
        var node = ReadRequiredProperty(value, propertyName, description);
        if (node is not JsonValue jsonValue ||
            !jsonValue.TryGetValue<string>(out var result) ||
            string.IsNullOrWhiteSpace(result))
        {
            throw new FormatException($"{description} must be a non-empty JSON string.");
        }

        return result;
    }

    private static int ReadInt32(JsonObject value, string propertyName, string description)
    {
        var node = ReadRequiredProperty(value, propertyName, description);
        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<int>(out var result))
        {
            throw new FormatException($"{description} must be a JSON integer.");
        }

        return result;
    }

    private static bool ReadBool(JsonObject value, string propertyName, string description)
    {
        var node = ReadRequiredProperty(value, propertyName, description);
        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<bool>(out var result))
        {
            throw new FormatException($"{description} must be a JSON Boolean.");
        }

        return result;
    }

    private static TEnum ReadEnum<TEnum>(JsonObject value, string propertyName, string description)
        where TEnum : struct
    {
        var text = ReadString(value, propertyName, description);
        return Enum.TryParse<TEnum>(text, ignoreCase: false, out var result)
            ? result
            : throw new FormatException($"{description} value '{text}' is not supported.");
    }

    private static TEnum? ReadNullableEnum<TEnum>(JsonObject value, string propertyName)
        where TEnum : struct
    {
        if (!value.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return null;
        }

        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<string>(out var text))
        {
            return null;
        }

        return Enum.TryParse<TEnum>(text, ignoreCase: false, out var result) ? result : null;
    }
}
