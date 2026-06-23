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

internal static class ExportPresetStore
{
    private const string Format = "Electron2D.ExportPresets";
    private const int FormatVersion = 1;
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static void Save(string path, Electron2DExportPresetDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);
        document.Validate();
        WriteText(path, Serialize(document));
    }

    public static Electron2DExportPresetLoadResult Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try
        {
            return Electron2DExportPresetLoadResult.Success(Deserialize(File.ReadAllText(path)));
        }
        catch (Electron2DExportPresetFormatException exception)
        {
            return Electron2DExportPresetLoadResult.Failure(
                new Electron2DExportDiagnostic(
                    exception.Code,
                    exception.Message,
                    Electron2DExportDiagnosticSeverity.Error,
                    exception.PresetName));
        }
        catch (JsonException exception)
        {
            return Electron2DExportPresetLoadResult.Failure(
                new Electron2DExportDiagnostic(
                    "E2D-EXPORT-PRESET-JSON-0001",
                    exception.Message,
                    Electron2DExportDiagnosticSeverity.Error,
                    string.Empty));
        }
        catch (FormatException exception)
        {
            return Electron2DExportPresetLoadResult.Failure(
                new Electron2DExportDiagnostic(
                    "E2D-EXPORT-PRESET-0002",
                    exception.Message,
                    Electron2DExportDiagnosticSeverity.Error,
                    string.Empty));
        }
        catch (IOException exception)
        {
            return Electron2DExportPresetLoadResult.Failure(
                new Electron2DExportDiagnostic(
                    "E2D-EXPORT-PRESET-IO-0001",
                    exception.Message,
                    Electron2DExportDiagnosticSeverity.Error,
                    string.Empty));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Electron2DExportPresetLoadResult.Failure(
                new Electron2DExportDiagnostic(
                    "E2D-EXPORT-PRESET-IO-0001",
                    exception.Message,
                    Electron2DExportDiagnosticSeverity.Error,
                    string.Empty));
        }
    }

    public static Electron2DExportPresetLoadResult LoadFromProjectFile(string projectFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectFilePath);
        try
        {
            var projectRoot = ExpectObject(JsonNode.Parse(File.ReadAllText(projectFilePath)), "Project file");
            if (!projectRoot.TryGetPropertyValue("exportPresets", out var exportPresetsNode) ||
                exportPresetsNode is null)
            {
                return Electron2DExportPresetLoadResult.Failure(
                    new Electron2DExportDiagnostic(
                        "E2D-EXPORT-PRESET-0002",
                        "Project file is missing embedded exportPresets.",
                        Electron2DExportDiagnosticSeverity.Error,
                        string.Empty));
            }

            return Electron2DExportPresetLoadResult.Success(DeserializeNode(exportPresetsNode));
        }
        catch (Electron2DExportPresetFormatException exception)
        {
            return Electron2DExportPresetLoadResult.Failure(
                new Electron2DExportDiagnostic(
                    exception.Code,
                    exception.Message,
                    Electron2DExportDiagnosticSeverity.Error,
                    exception.PresetName));
        }
        catch (JsonException exception)
        {
            return Electron2DExportPresetLoadResult.Failure(
                new Electron2DExportDiagnostic(
                    "E2D-EXPORT-PRESET-JSON-0001",
                    exception.Message,
                    Electron2DExportDiagnosticSeverity.Error,
                    string.Empty));
        }
        catch (FormatException exception)
        {
            return Electron2DExportPresetLoadResult.Failure(
                new Electron2DExportDiagnostic(
                    "E2D-EXPORT-PRESET-0002",
                    exception.Message,
                    Electron2DExportDiagnosticSeverity.Error,
                    string.Empty));
        }
        catch (IOException exception)
        {
            return Electron2DExportPresetLoadResult.Failure(
                new Electron2DExportDiagnostic(
                    "E2D-EXPORT-PRESET-IO-0001",
                    exception.Message,
                    Electron2DExportDiagnosticSeverity.Error,
                    string.Empty));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Electron2DExportPresetLoadResult.Failure(
                new Electron2DExportDiagnostic(
                    "E2D-EXPORT-PRESET-IO-0001",
                    exception.Message,
                    Electron2DExportDiagnosticSeverity.Error,
                    string.Empty));
        }
    }

    private static string Serialize(Electron2DExportPresetDocument document)
    {
        var root = new JsonObject
        {
            ["format"] = Format,
            ["formatVersion"] = FormatVersion,
            ["presets"] = new JsonArray(document.Presets
                .OrderBy(preset => preset.Name, StringComparer.Ordinal)
                .Select(preset => (JsonNode)WritePreset(preset))
                .ToArray())
        };

        return root.ToJsonString(IndentedOptions);
    }

    private static Electron2DExportPresetDocument Deserialize(string text)
    {
        return DeserializeNode(JsonNode.Parse(text));
    }

    private static Electron2DExportPresetDocument DeserializeNode(JsonNode? node)
    {
        var root = ExpectObject(node, "Export presets");
        ValidateFormat(root);
        var presets = ReadArray(root, "presets", "Export presets")
            .Select(ReadPreset)
            .ToArray();
        var document = new Electron2DExportPresetDocument
        {
            Presets = presets
        };
        document.Validate();
        return document;
    }

    private static JsonObject WritePreset(Electron2DExportPreset preset)
    {
        preset.Validate();
        return new JsonObject
        {
            ["name"] = preset.Name,
            ["target"] = preset.Target.ToString(),
            ["configuration"] = preset.Configuration.ToString(),
            ["runtimeIdentifier"] = preset.RuntimeIdentifier,
            ["selfContained"] = preset.SelfContained,
            ["rendererProfile"] = preset.RendererProfile.ToString(),
            ["outputDirectory"] = preset.OutputDirectory,
            ["includeDebugSymbols"] = preset.IncludeDebugSymbols,
            ["signing"] = new JsonObject
            {
                ["required"] = preset.Signing.Required,
                ["identity"] = preset.Signing.Identity ?? string.Empty,
                ["credentialReference"] = preset.Signing.CredentialReference ?? string.Empty
            }
        };
    }

    private static Electron2DExportPreset ReadPreset(JsonNode? node)
    {
        var preset = ExpectObject(node, "Export preset");
        return new Electron2DExportPreset
        {
            Name = ReadString(preset, "name", "Export preset name"),
            Target = ReadEnum<Electron2DExportTarget>(preset, "target", "Export target"),
            Configuration = ReadEnum<Electron2DExportConfiguration>(preset, "configuration", "Export configuration"),
            RuntimeIdentifier = ReadString(preset, "runtimeIdentifier", "Export runtime identifier"),
            SelfContained = ReadBoolean(preset, "selfContained", "Export self-contained flag"),
            RendererProfile = ReadEnum<Electron2DRendererProfileSetting>(preset, "rendererProfile", "Export renderer profile"),
            OutputDirectory = ReadString(preset, "outputDirectory", "Export output directory"),
            IncludeDebugSymbols = ReadBoolean(preset, "includeDebugSymbols", "Export debug symbols flag"),
            Signing = ReadSigning(ReadObject(preset, "signing", "Export signing settings"))
        };
    }

    private static Electron2DExportSigningSettings ReadSigning(JsonObject signing)
    {
        return new Electron2DExportSigningSettings
        {
            Required = ReadBoolean(signing, "required", "Export signing required flag"),
            Identity = ReadOptionalString(signing, "identity", "Export signing identity"),
            CredentialReference = ReadOptionalString(signing, "credentialReference", "Export signing credential reference")
        };
    }

    private static void ValidateFormat(JsonObject root)
    {
        var format = ReadString(root, "format", "Export presets format");
        if (format != Format)
        {
            throw new FormatException($"Export presets format '{format}' is not supported.");
        }

        var version = ReadInt32(root, "formatVersion", "Export presets format version");
        if (version != FormatVersion)
        {
            throw new FormatException($"Export presets format version '{version}' is not supported.");
        }
    }

    private static JsonObject ReadObject(JsonObject obj, string propertyName, string description)
    {
        return ExpectObject(ReadRequiredProperty(obj, propertyName, description), description);
    }

    private static JsonArray ReadArray(JsonObject obj, string propertyName, string description)
    {
        return ReadRequiredProperty(obj, propertyName, description) as JsonArray ??
            throw new FormatException($"{description} must be a JSON array.");
    }

    private static JsonNode ReadRequiredProperty(JsonObject obj, string propertyName, string description)
    {
        return obj.TryGetPropertyValue(propertyName, out var node) && node is not null
            ? node
            : throw new FormatException($"{description} is missing required property '{propertyName}'.");
    }

    private static string ReadString(JsonObject obj, string propertyName, string description)
    {
        var node = ReadRequiredProperty(obj, propertyName, description);
        if (node is not JsonValue value || !value.TryGetValue<string>(out var result) || string.IsNullOrWhiteSpace(result))
        {
            throw new FormatException($"{description} must be a non-empty JSON string.");
        }

        return result;
    }

    private static string ReadOptionalString(JsonObject obj, string propertyName, string description)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return string.Empty;
        }

        if (node is not JsonValue value || !value.TryGetValue<string>(out var result))
        {
            throw new FormatException($"{description} must be a JSON string.");
        }

        return result ?? string.Empty;
    }

    private static int ReadInt32(JsonObject obj, string propertyName, string description)
    {
        var node = ReadRequiredProperty(obj, propertyName, description);
        if (node is not JsonValue value || !value.TryGetValue<int>(out var result))
        {
            throw new FormatException($"{description} must be a JSON integer.");
        }

        return result;
    }

    private static bool ReadBoolean(JsonObject obj, string propertyName, string description)
    {
        var node = ReadRequiredProperty(obj, propertyName, description);
        if (node is not JsonValue value || !value.TryGetValue<bool>(out var result))
        {
            throw new FormatException($"{description} must be a JSON boolean.");
        }

        return result;
    }

    private static TEnum ReadEnum<TEnum>(JsonObject obj, string propertyName, string description)
        where TEnum : struct, Enum
    {
        var value = ReadString(obj, propertyName, description);
        return Enum.TryParse<TEnum>(value, ignoreCase: false, out var result) && Enum.IsDefined(result)
            ? result
            : throw new FormatException($"{description} '{value}' is not supported.");
    }

    private static JsonObject ExpectObject(JsonNode? node, string description)
    {
        return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
    }

    private static void WriteText(string path, string text)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, text.Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
