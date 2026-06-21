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

internal sealed class FontImporter : IResourceImporter
{
    private const string SidecarSuffix = ".e2import.json";

    public string Name => "Electron2D.Font";

    public bool CanImport(ResourceImportSourceFile source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) ||
            source.Extension.Equals(".otf", StringComparison.OrdinalIgnoreCase);
    }

    public ResourceImportOutput Import(ResourceImportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var source = FontMetadataReader.Read(context.Source.AbsolutePath);
        var sidecar = FontImportSettings.ReadIfExists(
            context.Source.AbsolutePath + SidecarSuffix,
            context.Source.ResourcePath + SidecarSuffix);
        var uid = ResourceUid.CreateIdForPath(context.Source.ResourcePath);
        var metadata = new FontImportMetadata(
            context.Source.ResourcePath,
            uid,
            source.Format,
            source.FamilyName,
            source.StyleName,
            source.FullName,
            source.PostScriptName,
            sidecar.Fallbacks,
            sidecar.Rasterization);
        var dependencies = sidecar.Exists
            ? new[] { sidecar.ResourcePath }.Concat(sidecar.Fallbacks).ToArray()
            : sidecar.Fallbacks.ToArray();

        return new ResourceImportOutput(
            uid,
            "Electron2D.Font",
            [ResourceImportArtifact.FromUtf8Text("font.e2font.json", FontImportMetadataTextSerializer.Serialize(metadata))],
            dependencies);
    }

    private sealed class FontImportSettings
    {
        private FontImportSettings(
            bool exists,
            string resourcePath,
            IReadOnlyList<string> fallbacks,
            FontRasterizationSettings rasterization)
        {
            Exists = exists;
            ResourcePath = resourcePath;
            Fallbacks = fallbacks;
            Rasterization = rasterization;
        }

        public bool Exists { get; }

        public string ResourcePath { get; }

        public IReadOnlyList<string> Fallbacks { get; }

        public FontRasterizationSettings Rasterization { get; }

        public static FontImportSettings ReadIfExists(string absolutePath, string resourcePath)
        {
            if (!File.Exists(absolutePath))
            {
                return new FontImportSettings(
                    exists: false,
                    resourcePath,
                    Array.Empty<string>(),
                    FontRasterizationSettings.DefaultBitmap);
            }

            try
            {
                return Read(resourcePath, ExpectObject(JsonNode.Parse(File.ReadAllText(absolutePath)), "Font import sidecar"));
            }
            catch (FormatException)
            {
                throw;
            }
            catch (JsonException exception)
            {
                throw new FormatException("Font import sidecar JSON text is malformed.", exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new FormatException("Font import sidecar JSON text is malformed.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("Font import sidecar JSON text is malformed.", exception);
            }
        }

        private static FontImportSettings Read(string resourcePath, JsonObject root)
        {
            var fallbacks = root.TryGetPropertyValue("fallbacks", out var fallbackNode)
                ? ReadStringArray(ExpectArray(fallbackNode, "Font import sidecar fallbacks"))
                : Array.Empty<string>();
            var rasterization = root.TryGetPropertyValue("rasterization", out var rasterizationNode)
                ? ReadRasterization(ExpectObject(rasterizationNode, "Font import sidecar rasterization"))
                : FontRasterizationSettings.DefaultBitmap;

            return new FontImportSettings(
                exists: true,
                resourcePath,
                fallbacks,
                rasterization);
        }

        private static FontRasterizationSettings ReadRasterization(JsonObject rasterization)
        {
            var mode = ReadOptionalEnum(rasterization, "mode", FontRasterizationMode.Bitmap);
            var baseSize = ReadOptionalInt32(rasterization, "baseSize", defaultValue: 16);
            var spread = ReadOptionalInt32(rasterization, "sdfSpread", mode == FontRasterizationMode.Sdf ? 8 : 0);
            return new FontRasterizationSettings(mode, baseSize, spread);
        }

        private static IReadOnlyList<string> ReadStringArray(JsonArray values)
        {
            var result = new List<string>();
            foreach (var node in values)
            {
                if (node is not JsonValue jsonValue ||
                    !jsonValue.TryGetValue<string>(out var value) ||
                    string.IsNullOrWhiteSpace(value))
                {
                    throw new FormatException("Font import fallback path must be a non-empty JSON string.");
                }

                result.Add(value);
            }

            return result.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        }

        private static JsonObject ExpectObject(JsonNode? node, string description)
        {
            return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
        }

        private static JsonArray ExpectArray(JsonNode? node, string description)
        {
            return node as JsonArray ?? throw new FormatException($"{description} must be a JSON array.");
        }

        private static int ReadOptionalInt32(JsonObject value, string propertyName, int defaultValue)
        {
            if (!value.TryGetPropertyValue(propertyName, out var node) || node is null)
            {
                return defaultValue;
            }

            if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<int>(out var result))
            {
                throw new FormatException($"Font import sidecar '{propertyName}' must be a JSON integer.");
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
                throw new FormatException($"Font import sidecar '{propertyName}' value is not supported.");
            }

            return result;
        }
    }
}
