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

internal static class AudioImportMetadataTextSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(AudioImportMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return WriteMetadata(metadata).ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
    }

    public static AudioImportMetadata Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            return ReadMetadata(ExpectObject(JsonNode.Parse(text), "Audio import metadata"));
        }
        catch (FormatException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new FormatException("Audio import metadata JSON text is malformed.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException("Audio import metadata JSON text is malformed.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException("Audio import metadata JSON text is malformed.", exception);
        }
    }

    private static JsonObject WriteMetadata(AudioImportMetadata metadata)
    {
        return new JsonObject
        {
            ["format"] = AudioImportMetadata.FormatName,
            ["version"] = AudioImportMetadata.CurrentVersion,
            ["source"] = metadata.SourcePath,
            ["uid"] = metadata.UidText,
            ["audioFormat"] = metadata.Format.ToString(),
            ["mode"] = metadata.Mode.ToString(),
            ["sampleRate"] = metadata.SampleRate,
            ["channelCount"] = metadata.ChannelCount,
            ["bitsPerSample"] = metadata.BitsPerSample,
            ["sampleCount"] = metadata.SampleCount,
            ["length"] = metadata.LengthSeconds,
            ["loop"] = WriteLoop(metadata.Loop),
            ["platforms"] = WritePlatforms(metadata.PlatformPackages)
        };
    }

    private static AudioImportMetadata ReadMetadata(JsonObject root)
    {
        var formatName = ReadString(root, "format", "Audio import metadata format");
        if (formatName != AudioImportMetadata.FormatName)
        {
            throw new FormatException($"Audio import metadata format '{formatName}' is not supported.");
        }

        var version = ReadInt32(root, "version", "Audio import metadata version");
        if (version != AudioImportMetadata.CurrentVersion)
        {
            throw new FormatException($"Audio import metadata version '{version}' is not supported.");
        }

        var uidText = ReadString(root, "uid", "Audio import metadata uid");
        var uid = ResourceUid.TextToId(uidText);
        if (uid == ResourceUid.InvalidId)
        {
            throw new FormatException($"Audio import metadata uid '{uidText}' is invalid.");
        }

        return new AudioImportMetadata(
            ReadString(root, "source", "Audio import metadata source"),
            uid,
            ReadEnum<AudioSourceFormat>(root, "audioFormat", "Audio source format"),
            ReadEnum<AudioImportMode>(root, "mode", "Audio import mode"),
            ReadInt32(root, "sampleRate", "Audio sample rate"),
            ReadInt32(root, "channelCount", "Audio channel count"),
            ReadInt32(root, "bitsPerSample", "Audio bits per sample"),
            ReadInt64(root, "sampleCount", "Audio sample count"),
            ReadSingle(root, "length", "Audio length"),
            ReadLoop(ExpectObject(ReadRequiredProperty(root, "loop", "Audio loop"), "Audio loop")),
            ReadPlatforms(ReadArray(root, "platforms", "Audio platform packages")));
    }

    private static JsonObject WriteLoop(AudioLoopMetadata loop)
    {
        return new JsonObject
        {
            ["enabled"] = loop.Enabled,
            ["begin"] = loop.BeginSeconds,
            ["end"] = loop.EndSeconds
        };
    }

    private static AudioLoopMetadata ReadLoop(JsonObject loop)
    {
        return new AudioLoopMetadata(
            ReadBoolean(loop, "enabled", "Audio loop enabled"),
            ReadSingle(loop, "begin", "Audio loop begin"),
            ReadSingle(loop, "end", "Audio loop end"));
    }

    private static JsonArray WritePlatforms(IEnumerable<AudioPlatformPackage> packages)
    {
        var result = new JsonArray();
        foreach (var package in packages.OrderBy(package => package.Name, StringComparer.Ordinal))
        {
            result.Add(new JsonObject
            {
                ["name"] = package.Name,
                ["packaging"] = package.Packaging
            });
        }

        return result;
    }

    private static IReadOnlyList<AudioPlatformPackage> ReadPlatforms(JsonArray packages)
    {
        var result = new List<AudioPlatformPackage>();
        foreach (var node in packages)
        {
            var package = ExpectObject(node, "Audio platform package");
            result.Add(new AudioPlatformPackage(
                ReadString(package, "name", "Audio platform package name"),
                ReadString(package, "packaging", "Audio platform package mode")));
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
            !jsonValue.TryGetValue<string>(out var valueText) ||
            string.IsNullOrWhiteSpace(valueText))
        {
            throw new FormatException($"{description} must be a non-empty JSON string.");
        }

        return valueText;
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

    private static long ReadInt64(JsonObject value, string propertyName, string description)
    {
        var node = ReadRequiredProperty(value, propertyName, description);
        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<long>(out var result))
        {
            throw new FormatException($"{description} must be a JSON integer.");
        }

        return result;
    }

    private static float ReadSingle(JsonObject value, string propertyName, string description)
    {
        var node = ReadRequiredProperty(value, propertyName, description);
        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<double>(out var result))
        {
            throw new FormatException($"{description} must be a JSON number.");
        }

        return (float)result;
    }

    private static bool ReadBoolean(JsonObject value, string propertyName, string description)
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
