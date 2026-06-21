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

internal sealed class TextureImageImporter : IResourceImporter
{
    private const string SidecarSuffix = ".e2import.json";

    public string Name => "Electron2D.TextureImage";

    public bool CanImport(ResourceImportSourceFile source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            source.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            source.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    public ResourceImportOutput Import(ResourceImportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var image = TextureImageMetadataReader.Read(context.Source.AbsolutePath);
        var sidecar = TextureImportSettings.ReadIfExists(
            context.Source.AbsolutePath + SidecarSuffix,
            context.Source.ResourcePath + SidecarSuffix);
        var hasMipmaps = sidecar.Mipmaps ||
            sidecar.Sampling.FilterMode is TextureFilterMode.NearestWithMipmaps or TextureFilterMode.LinearWithMipmaps;
        var mipmapCount = hasMipmaps
            ? TextureImportMetadata.CalculateFullMipmapCount(image.Width, image.Height)
            : 0;
        var uid = ResourceUid.CreateIdForPath(context.Source.ResourcePath);
        var metadata = new TextureImportMetadata(
            context.Source.ResourcePath,
            uid,
            image.Format,
            image.Width,
            image.Height,
            image.HasAlpha,
            hasMipmaps,
            mipmapCount,
            sidecar.Sampling,
            sidecar.AtlasRegions,
            sidecar.PlatformVariants);

        var dependencies = sidecar.Exists
            ? new[] { sidecar.ResourcePath }
            : Array.Empty<string>();

        return new ResourceImportOutput(
            uid,
            "Electron2D.Texture2D",
            [ResourceImportArtifact.FromUtf8Text("texture.e2tex.json", TextureImportMetadataTextSerializer.Serialize(metadata))],
            dependencies);
    }

    private sealed class TextureImportSettings
    {
        private TextureImportSettings(
            bool exists,
            string resourcePath,
            TextureSamplingOptions sampling,
            bool mipmaps,
            IReadOnlyList<TextureAtlasRegionMetadata> atlasRegions,
            IReadOnlyList<TexturePlatformVariant> platformVariants)
        {
            Exists = exists;
            ResourcePath = resourcePath;
            Sampling = sampling;
            Mipmaps = mipmaps;
            AtlasRegions = atlasRegions;
            PlatformVariants = platformVariants;
        }

        public bool Exists { get; }

        public string ResourcePath { get; }

        public TextureSamplingOptions Sampling { get; }

        public bool Mipmaps { get; }

        public IReadOnlyList<TextureAtlasRegionMetadata> AtlasRegions { get; }

        public IReadOnlyList<TexturePlatformVariant> PlatformVariants { get; }

        public static TextureImportSettings ReadIfExists(string absolutePath, string resourcePath)
        {
            if (!File.Exists(absolutePath))
            {
                return new TextureImportSettings(
                    exists: false,
                    resourcePath,
                    TextureSamplingOptions.Default,
                    mipmaps: false,
                    Array.Empty<TextureAtlasRegionMetadata>(),
                    Array.Empty<TexturePlatformVariant>());
            }

            try
            {
                return Read(resourcePath, ExpectObject(JsonNode.Parse(File.ReadAllText(absolutePath)), "Texture import sidecar"));
            }
            catch (FormatException)
            {
                throw;
            }
            catch (JsonException exception)
            {
                throw new FormatException("Texture import sidecar JSON text is malformed.", exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new FormatException("Texture import sidecar JSON text is malformed.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("Texture import sidecar JSON text is malformed.", exception);
            }
        }

        private static TextureImportSettings Read(string resourcePath, JsonObject root)
        {
            var filter = ReadOptionalEnum(root, "filter", TextureFilterMode.Linear);
            var repeat = ReadOptionalEnum(root, "repeat", TextureRepeatMode.Disabled);
            var mipmaps = ReadOptionalBool(root, "mipmaps", defaultValue: false);
            var atlas = root.TryGetPropertyValue("atlas", out var atlasNode)
                ? ReadAtlas(ExpectArray(atlasNode, "Texture import sidecar atlas"))
                : Array.Empty<TextureAtlasRegionMetadata>();
            var platforms = root.TryGetPropertyValue("platforms", out var platformsNode)
                ? ReadPlatforms(ExpectArray(platformsNode, "Texture import sidecar platforms"))
                : Array.Empty<TexturePlatformVariant>();

            return new TextureImportSettings(
                exists: true,
                resourcePath,
                new TextureSamplingOptions(filter, repeat),
                mipmaps,
                atlas,
                platforms);
        }

        private static IReadOnlyList<TextureAtlasRegionMetadata> ReadAtlas(JsonArray regions)
        {
            var result = new List<TextureAtlasRegionMetadata>();
            foreach (var node in regions)
            {
                var region = ExpectObject(node, "Texture import atlas region");
                var margin = region.TryGetPropertyValue("margin", out var marginNode)
                    ? ReadRect(ExpectObject(marginNode, "Texture import atlas region margin"))
                    : default;

                result.Add(new TextureAtlasRegionMetadata(
                    ReadString(region, "name", "Texture import atlas region name"),
                    ReadRect(ExpectObject(ReadRequiredProperty(region, "region", "Texture import atlas region rectangle"), "Texture import atlas region rectangle")),
                    margin,
                    ReadOptionalBool(region, "filterClip", defaultValue: false)));
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
                    ReadOptionalInt32(variant, "quality", defaultValue: 100)));
            }

            return result;
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

        private static float ReadSingle(JsonObject value, string propertyName, string description)
        {
            var node = ReadRequiredProperty(value, propertyName, description);
            if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<float>(out var result))
            {
                throw new FormatException($"{description} must be a JSON number.");
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
                throw new FormatException($"Texture import sidecar '{propertyName}' must be a JSON Boolean.");
            }

            return result;
        }

        private static int ReadOptionalInt32(JsonObject value, string propertyName, int defaultValue)
        {
            if (!value.TryGetPropertyValue(propertyName, out var node) || node is null)
            {
                return defaultValue;
            }

            if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<int>(out var result))
            {
                throw new FormatException($"Texture import sidecar '{propertyName}' must be a JSON integer.");
            }

            return result;
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
                throw new FormatException($"Texture import sidecar '{propertyName}' value is not supported.");
            }

            return result;
        }
    }
}
