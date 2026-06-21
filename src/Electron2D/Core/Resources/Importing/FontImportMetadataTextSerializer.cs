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

internal static class FontImportMetadataTextSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(FontImportMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return WriteMetadata(metadata).ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    public static FontImportMetadata Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            return ReadMetadata(ExpectObject(JsonNode.Parse(text), "Font import metadata"));
        }
        catch (FormatException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new FormatException("Font import metadata JSON text is malformed.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException("Font import metadata JSON text is malformed.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException("Font import metadata JSON text is malformed.", exception);
        }
    }

    private static JsonObject WriteMetadata(FontImportMetadata metadata)
    {
        return new JsonObject
        {
            ["format"] = FontImportMetadata.FormatName,
            ["version"] = FontImportMetadata.CurrentVersion,
            ["source"] = metadata.SourcePath,
            ["uid"] = metadata.UidText,
            ["fontFormat"] = metadata.Format.ToString(),
            ["familyName"] = metadata.FamilyName,
            ["styleName"] = metadata.StyleName,
            ["fullName"] = metadata.FullName,
            ["postScriptName"] = metadata.PostScriptName,
            ["fallbacks"] = WriteFallbacks(metadata.FallbackFontPaths),
            ["rasterization"] = WriteRasterization(metadata.Rasterization)
        };
    }

    private static FontImportMetadata ReadMetadata(JsonObject root)
    {
        var formatName = ReadString(root, "format", "Font import metadata format");
        if (formatName != FontImportMetadata.FormatName)
        {
            throw new FormatException($"Font import metadata format '{formatName}' is not supported.");
        }

        var version = ReadInt32(root, "version", "Font import metadata version");
        if (version != FontImportMetadata.CurrentVersion)
        {
            throw new FormatException($"Font import metadata version '{version}' is not supported.");
        }

        var uidText = ReadString(root, "uid", "Font import metadata uid");
        var uid = ResourceUid.TextToId(uidText);
        if (uid == ResourceUid.InvalidId)
        {
            throw new FormatException($"Font import metadata uid '{uidText}' is invalid.");
        }

        return new FontImportMetadata(
            ReadString(root, "source", "Font import metadata source"),
            uid,
            ReadEnum<FontSourceFormat>(root, "fontFormat", "Font import source format"),
            ReadString(root, "familyName", "Font import family name"),
            ReadString(root, "styleName", "Font import style name"),
            ReadString(root, "fullName", "Font import full name"),
            ReadString(root, "postScriptName", "Font import PostScript name"),
            ReadFallbacks(ReadArray(root, "fallbacks", "Font import fallback fonts")),
            ReadRasterization(ExpectObject(ReadRequiredProperty(root, "rasterization", "Font import rasterization"), "Font import rasterization")));
    }

    private static JsonArray WriteFallbacks(IEnumerable<string> fallbackPaths)
    {
        var result = new JsonArray();
        foreach (var path in fallbackPaths.OrderBy(path => path, StringComparer.Ordinal))
        {
            result.Add(path);
        }

        return result;
    }

    private static IReadOnlyList<string> ReadFallbacks(JsonArray fallbacks)
    {
        var result = new List<string>();
        foreach (var node in fallbacks)
        {
            result.Add(ReadString(ReadRequiredNode(node, "Font fallback path"), "Font fallback path"));
        }

        return result;
    }

    private static JsonObject WriteRasterization(FontRasterizationSettings rasterization)
    {
        return new JsonObject
        {
            ["mode"] = rasterization.Mode.ToString(),
            ["baseSize"] = rasterization.BaseSize,
            ["sdfSpread"] = rasterization.SdfSpread
        };
    }

    private static FontRasterizationSettings ReadRasterization(JsonObject rasterization)
    {
        return new FontRasterizationSettings(
            ReadEnum<FontRasterizationMode>(rasterization, "mode", "Font rasterization mode"),
            ReadInt32(rasterization, "baseSize", "Font rasterization base size"),
            ReadInt32(rasterization, "sdfSpread", "Font rasterization SDF spread"));
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
        return ReadString(ReadRequiredProperty(value, propertyName, description), description);
    }

    private static string ReadString(JsonNode node, string description)
    {
        if (node is not JsonValue jsonValue ||
            !jsonValue.TryGetValue<string>(out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"{description} must be a non-empty JSON string.");
        }

        return value;
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

    private static TEnum ReadEnum<TEnum>(JsonObject value, string propertyName, string description)
        where TEnum : struct
    {
        var text = ReadString(value, propertyName, description);
        return Enum.TryParse<TEnum>(text, ignoreCase: false, out var result)
            ? result
            : throw new FormatException($"{description} value '{text}' is not supported.");
    }
}
