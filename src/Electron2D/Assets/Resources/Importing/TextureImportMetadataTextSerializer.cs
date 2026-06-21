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

internal static class TextureImportMetadataTextSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(TextureImportMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return WriteMetadata(metadata).ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    public static TextureImportMetadata Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            return ReadMetadata(ExpectObject(JsonNode.Parse(text), "Texture import metadata"));
        }
        catch (FormatException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new FormatException("Texture import metadata JSON text is malformed.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException("Texture import metadata JSON text is malformed.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException("Texture import metadata JSON text is malformed.", exception);
        }
    }

    private static JsonObject WriteMetadata(TextureImportMetadata metadata)
    {
        return new JsonObject
        {
            ["format"] = TextureImportMetadata.FormatName,
            ["version"] = TextureImportMetadata.CurrentVersion,
            ["source"] = metadata.SourcePath,
            ["uid"] = metadata.UidText,
            ["imageFormat"] = metadata.Format.ToString(),
            ["width"] = metadata.Width,
            ["height"] = metadata.Height,
            ["hasAlpha"] = metadata.HasAlpha,
            ["hasMipmaps"] = metadata.HasMipmaps,
            ["mipmapCount"] = metadata.MipmapCount,
            ["sampling"] = WriteSampling(metadata.Sampling),
            ["atlas"] = WriteAtlas(metadata.AtlasRegions),
            ["platforms"] = WritePlatforms(metadata.PlatformVariants)
        };
    }

    private static TextureImportMetadata ReadMetadata(JsonObject root)
    {
        var formatName = ReadString(root, "format", "Texture import metadata format");
        if (formatName != TextureImportMetadata.FormatName)
        {
            throw new FormatException($"Texture import metadata format '{formatName}' is not supported.");
        }

        var version = ReadInt32(root, "version", "Texture import metadata version");
        if (version != TextureImportMetadata.CurrentVersion)
        {
            throw new FormatException($"Texture import metadata version '{version}' is not supported.");
        }

        var uidText = ReadString(root, "uid", "Texture import metadata uid");
        var uid = ResourceUid.TextToId(uidText);
        if (uid == ResourceUid.InvalidId)
        {
            throw new FormatException($"Texture import metadata uid '{uidText}' is invalid.");
        }

        return new TextureImportMetadata(
            ReadString(root, "source", "Texture import metadata source"),
            uid,
            ReadEnum<TextureImageFormat>(root, "imageFormat", "Texture import image format"),
            ReadInt32(root, "width", "Texture import width"),
            ReadInt32(root, "height", "Texture import height"),
            ReadBool(root, "hasAlpha", "Texture import alpha flag"),
            ReadBool(root, "hasMipmaps", "Texture import mipmap flag"),
            ReadInt32(root, "mipmapCount", "Texture import mipmap count"),
            ReadSampling(ExpectObject(ReadRequiredProperty(root, "sampling", "Texture import sampling"), "Texture import sampling")),
            ReadAtlas(ReadArray(root, "atlas", "Texture import atlas regions")),
            ReadPlatforms(ReadArray(root, "platforms", "Texture import platform variants")));
    }

    private static JsonObject WriteSampling(TextureSamplingOptions sampling)
    {
        return new JsonObject
        {
            ["filter"] = sampling.FilterMode.ToString(),
            ["repeat"] = sampling.RepeatMode.ToString()
        };
    }

    private static TextureSamplingOptions ReadSampling(JsonObject sampling)
    {
        return new TextureSamplingOptions(
            ReadEnum<TextureFilterMode>(sampling, "filter", "Texture import filter"),
            ReadEnum<TextureRepeatMode>(sampling, "repeat", "Texture import repeat"));
    }

    private static JsonArray WriteAtlas(IEnumerable<TextureAtlasRegionMetadata> atlasRegions)
    {
        var result = new JsonArray();
        foreach (var region in atlasRegions.OrderBy(region => region.Name, StringComparer.Ordinal))
        {
            result.Add((JsonNode)new JsonObject
            {
                ["name"] = region.Name,
                ["region"] = WriteRect(region.Region),
                ["margin"] = WriteRect(region.Margin),
                ["filterClip"] = region.FilterClip
            });
        }

        return result;
    }

    private static IReadOnlyList<TextureAtlasRegionMetadata> ReadAtlas(JsonArray regions)
    {
        var result = new List<TextureAtlasRegionMetadata>();
        foreach (var node in regions)
        {
            var region = ExpectObject(node, "Texture import atlas region");
            result.Add(new TextureAtlasRegionMetadata(
                ReadString(region, "name", "Texture import atlas region name"),
                ReadRect(ExpectObject(ReadRequiredProperty(region, "region", "Texture import atlas region rectangle"), "Texture import atlas region rectangle")),
                ReadRect(ExpectObject(ReadRequiredProperty(region, "margin", "Texture import atlas region margin"), "Texture import atlas region margin")),
                ReadBool(region, "filterClip", "Texture import atlas filterClip")));
        }

        return result;
    }

    private static JsonArray WritePlatforms(IEnumerable<TexturePlatformVariant> platformVariants)
    {
        var result = new JsonArray();
        foreach (var variant in platformVariants.OrderBy(variant => variant.Name, StringComparer.Ordinal))
        {
            result.Add((JsonNode)new JsonObject
            {
                ["name"] = variant.Name,
                ["format"] = variant.Format,
                ["quality"] = variant.Quality
            });
        }

        return result;
    }

    private static IReadOnlyList<TexturePlatformVariant> ReadPlatforms(JsonArray variants)
    {
        var result = new List<TexturePlatformVariant>();
        foreach (var node in variants)
        {
            var variant = ExpectObject(node, "Texture import platform variant");
            result.Add(new TexturePlatformVariant(
                ReadString(variant, "name", "Texture import platform variant name"),
                ReadString(variant, "format", "Texture import platform variant format"),
                ReadInt32(variant, "quality", "Texture import platform variant quality")));
        }

        return result;
    }

    private static JsonObject WriteRect(Rect2 rect)
    {
        return new JsonObject
        {
            ["x"] = rect.Position.X,
            ["y"] = rect.Position.Y,
            ["width"] = rect.Size.X,
            ["height"] = rect.Size.Y
        };
    }

    private static Rect2 ReadRect(JsonObject rect)
    {
        return new Rect2(
            ReadSingle(rect, "x", "Texture import rect x"),
            ReadSingle(rect, "y", "Texture import rect y"),
            ReadSingle(rect, "width", "Texture import rect width"),
            ReadSingle(rect, "height", "Texture import rect height"));
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

    private static float ReadSingle(JsonObject value, string propertyName, string description)
    {
        var node = ReadRequiredProperty(value, propertyName, description);
        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<float>(out var result))
        {
            throw new FormatException($"{description} must be a JSON number.");
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
}
