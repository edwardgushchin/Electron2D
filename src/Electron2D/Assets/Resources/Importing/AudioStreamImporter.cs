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

internal sealed class AudioStreamImporter : IResourceImporter
{
    private const string SidecarSuffix = ".e2import.json";

    public string Name => "Electron2D.AudioStream";

    public bool CanImport(ResourceImportSourceFile source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
            source.Extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase);
    }

    public ResourceImportOutput Import(ResourceImportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var source = AudioMetadataReader.Read(context.Source.AbsolutePath);
        var sidecar = AudioImportSettings.ReadIfExists(
            context.Source.AbsolutePath + SidecarSuffix,
            context.Source.ResourcePath + SidecarSuffix,
            source.LengthSeconds);
        var uid = ResourceUid.CreateIdForPath(context.Source.ResourcePath);
        var metadata = new AudioImportMetadata(
            context.Source.ResourcePath,
            uid,
            source.Format,
            sidecar.Mode,
            source.SampleRate,
            source.ChannelCount,
            source.BitsPerSample,
            source.SampleCount,
            source.LengthSeconds,
            sidecar.Loop,
            sidecar.PlatformPackages);

        var dependencies = sidecar.Exists
            ? new[] { sidecar.ResourcePath }
            : Array.Empty<string>();

        return new ResourceImportOutput(
            uid,
            "Electron2D.AudioStream",
            [ResourceImportArtifact.FromUtf8Text("audio.e2audio.json", AudioImportMetadataTextSerializer.Serialize(metadata))],
            dependencies);
    }

    private sealed class AudioImportSettings
    {
        private AudioImportSettings(
            bool exists,
            string resourcePath,
            AudioImportMode mode,
            AudioLoopMetadata loop,
            IReadOnlyList<AudioPlatformPackage> platformPackages)
        {
            Exists = exists;
            ResourcePath = resourcePath;
            Mode = mode;
            Loop = loop;
            PlatformPackages = platformPackages;
        }

        public bool Exists { get; }

        public string ResourcePath { get; }

        public AudioImportMode Mode { get; }

        public AudioLoopMetadata Loop { get; }

        public IReadOnlyList<AudioPlatformPackage> PlatformPackages { get; }

        public static AudioImportSettings ReadIfExists(string absolutePath, string resourcePath, float sourceLengthSeconds)
        {
            if (!File.Exists(absolutePath))
            {
                return new AudioImportSettings(
                    exists: false,
                    resourcePath,
                    AudioImportMode.Static,
                    AudioLoopMetadata.Disabled(sourceLengthSeconds),
                    Array.Empty<AudioPlatformPackage>());
            }

            try
            {
                return Read(
                    resourcePath,
                    sourceLengthSeconds,
                    ExpectObject(JsonNode.Parse(File.ReadAllText(absolutePath)), "Audio import sidecar"));
            }
            catch (FormatException)
            {
                throw;
            }
            catch (JsonException exception)
            {
                throw new FormatException("Audio import sidecar JSON text is malformed.", exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new FormatException("Audio import sidecar JSON text is malformed.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("Audio import sidecar JSON text is malformed.", exception);
            }
        }

        private static AudioImportSettings Read(string resourcePath, float sourceLengthSeconds, JsonObject root)
        {
            var mode = ReadOptionalEnum(root, "mode", AudioImportMode.Static);
            var loop = root.TryGetPropertyValue("loop", out var loopNode)
                ? ReadLoop(ExpectObject(loopNode, "Audio import sidecar loop"), sourceLengthSeconds)
                : AudioLoopMetadata.Disabled(sourceLengthSeconds);
            var platforms = root.TryGetPropertyValue("platforms", out var platformsNode)
                ? ReadPlatforms(ExpectArray(platformsNode, "Audio import sidecar platforms"))
                : Array.Empty<AudioPlatformPackage>();

            return new AudioImportSettings(
                exists: true,
                resourcePath,
                mode,
                loop,
                platforms);
        }

        private static AudioLoopMetadata ReadLoop(JsonObject loop, float sourceLengthSeconds)
        {
            return new AudioLoopMetadata(
                ReadOptionalBool(loop, "enabled", defaultValue: false),
                ReadOptionalSingle(loop, "begin", defaultValue: 0f),
                ReadOptionalSingle(loop, "end", defaultValue: sourceLengthSeconds));
        }

        private static IReadOnlyList<AudioPlatformPackage> ReadPlatforms(JsonArray packages)
        {
            var result = new List<AudioPlatformPackage>();
            foreach (var node in packages)
            {
                var package = ExpectObject(node, "Audio import platform package");
                result.Add(new AudioPlatformPackage(
                    ReadString(package, "name", "Audio import platform package name"),
                    ReadString(package, "packaging", "Audio import platform package mode")));
            }

            return result;
        }

        private static JsonObject ExpectObject(JsonNode? node, string description)
        {
            return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
        }

        private static JsonArray ExpectArray(JsonNode? node, string description)
        {
            return node as JsonArray ?? throw new FormatException($"{description} must be a JSON array.");
        }

        private static JsonNode ReadRequiredProperty(JsonObject value, string propertyName, string description)
        {
            return value.TryGetPropertyValue(propertyName, out var node)
                ? node ?? throw new FormatException($"{description} is missing.")
                : throw new FormatException($"{description} is missing.");
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

        private static bool ReadOptionalBool(JsonObject value, string propertyName, bool defaultValue)
        {
            if (!value.TryGetPropertyValue(propertyName, out var node) || node is null)
            {
                return defaultValue;
            }

            if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<bool>(out var result))
            {
                throw new FormatException($"Audio import sidecar '{propertyName}' must be a JSON Boolean.");
            }

            return result;
        }

        private static float ReadOptionalSingle(JsonObject value, string propertyName, float defaultValue)
        {
            if (!value.TryGetPropertyValue(propertyName, out var node) || node is null)
            {
                return defaultValue;
            }

            if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<double>(out var result))
            {
                throw new FormatException($"Audio import sidecar '{propertyName}' must be a JSON number.");
            }

            return (float)result;
        }

        private static TEnum ReadOptionalEnum<TEnum>(JsonObject value, string propertyName, TEnum defaultValue)
            where TEnum : struct
        {
            if (!value.TryGetPropertyValue(propertyName, out var node) || node is null)
            {
                return defaultValue;
            }

            if (node is not JsonValue jsonValue ||
                !jsonValue.TryGetValue<string>(out var text) ||
                !Enum.TryParse<TEnum>(text, ignoreCase: false, out var result))
            {
                throw new FormatException($"Audio import sidecar '{propertyName}' value is not supported.");
            }

            return result;
        }
    }
}
