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
namespace Electron2D;

internal static class TileSetResourceMetadata
{
    public static void Register()
    {
        ResourceObjectMetadataRegistry.Register(
            ResourceObjectTypeMetadata.Create(
                typeof(TileSet).FullName!,
                () => new TileSet(),
                [
                    ResourceObjectPropertyMetadata.CreateSerialized<TileSet>(
                        "sources",
                        CaptureSources,
                        RestoreSources),
                    ResourceObjectPropertyMetadata.Create<TileSet, Vector2I>(
                        "tile_size",
                        resource => resource.TileSize,
                        (resource, value) => resource.TileSize = value)
                ]));

        ResourceObjectMetadataRegistry.Register(
            ResourceObjectTypeMetadata.Create(
                typeof(TileSetAtlasSource).FullName!,
                () => new TileSetAtlasSource(),
                [
                    ResourceObjectPropertyMetadata.CreateSerialized<TileSetAtlasSource>(
                        "tiles",
                        CaptureAtlasTiles,
                        RestoreAtlasTiles),
                    ResourceObjectPropertyMetadata.Create<TileSetAtlasSource, Vector2I>(
                        "texture_region_size",
                        resource => resource.TextureRegionSize,
                        (resource, value) => resource.TextureRegionSize = value)
                ]));
    }

    private static SerializedPropertyValue CaptureSources(TileSet tileSet)
    {
        return Array(tileSet.EnumerateSources().Select(source =>
        {
            if (source.Value is not TileSetAtlasSource atlasSource)
            {
                throw new InvalidOperationException(
                    $"Tile source type '{source.Value.GetType().FullName}' is not supported by the 0.1-preview serializer.");
            }

            return Object(
                Property("id", Value(source.Key)),
                Property("type", Value(typeof(TileSetAtlasSource).FullName!)),
                Property("texture_region_size", Value(atlasSource.TextureRegionSize)),
                Property("tiles", CaptureAtlasTiles(atlasSource)));
        }));
    }

    private static void RestoreSources(TileSet tileSet, SerializedPropertyValue value)
    {
        tileSet.Clear();
        foreach (var sourceValue in ReadArray(value, "tile set sources"))
        {
            var source = ReadObject(sourceValue, "tile set source");
            var type = ReadString(Required(source, "type", "tile set source"), "tile set source type");
            if (!string.Equals(type, typeof(TileSetAtlasSource).FullName, StringComparison.Ordinal))
            {
                throw new FormatException($"Tile source type '{type}' is not supported.");
            }

            var atlasSource = new TileSetAtlasSource
            {
                TextureRegionSize = ReadVector2I(Required(source, "texture_region_size", "tile set source"), "texture region size")
            };
            RestoreAtlasTiles(atlasSource, Required(source, "tiles", "tile set source"));
            tileSet.AddSource(atlasSource, ReadInt32(Required(source, "id", "tile set source"), "tile source id"));
        }
    }

    private static SerializedPropertyValue CaptureAtlasTiles(TileSetAtlasSource source)
    {
        return Array(source.EnumerateTiles().Select(tile =>
            Object(
                Property("alternative_tile", Value(tile.AlternativeTile)),
                Property("atlas_coords", Value(tile.AtlasCoords)),
                Property("collision_polygons", CaptureCollisionPolygons(tile.Data)),
                Property("modulate", Value(tile.Data.Modulate)),
                Property("size_in_atlas", Value(tile.SizeInAtlas)),
                Property("texture_origin", Value(tile.Data.TextureOrigin)),
                Property("z_index", Value(tile.Data.ZIndex)))));
    }

    private static void RestoreAtlasTiles(TileSetAtlasSource source, SerializedPropertyValue value)
    {
        foreach (var tileValue in ReadArray(value, "atlas tiles"))
        {
            var tile = ReadObject(tileValue, "atlas tile");
            var alternativeTile = ReadInt32(Required(tile, "alternative_tile", "atlas tile"), "alternative tile");
            if (alternativeTile != 0)
            {
                throw new FormatException("Only the default alternative tile is supported by the 0.1-preview serializer.");
            }

            var atlasCoords = ReadVector2I(Required(tile, "atlas_coords", "atlas tile"), "atlas coordinates");
            source.CreateTile(atlasCoords, ReadVector2I(Required(tile, "size_in_atlas", "atlas tile"), "size in atlas"));

            var tileData = source.GetTileData(atlasCoords, alternativeTile)
                ?? throw new FormatException($"Atlas tile '{atlasCoords}' was not restored.");
            tileData.Modulate = ReadColor(Required(tile, "modulate", "atlas tile"), "tile modulate");
            tileData.TextureOrigin = ReadVector2(Required(tile, "texture_origin", "atlas tile"), "tile texture origin");
            tileData.ZIndex = ReadInt32(Required(tile, "z_index", "atlas tile"), "tile z-index");
            RestoreCollisionPolygons(
                tileData,
                Required(tile, "collision_polygons", "atlas tile"));
        }
    }

    private static SerializedPropertyValue CaptureCollisionPolygons(TileData tileData)
    {
        return Array(tileData.EnumerateCollisionPolygons().Select(polygon =>
            Object(
                Property("layer_id", Value(polygon.LayerId)),
                Property("one_way", Value(polygon.OneWay)),
                Property("one_way_margin", Value(polygon.OneWayMargin)),
                Property("points", Array(polygon.Points.Select(Value))),
                Property("polygon_index", Value(polygon.PolygonIndex)))));
    }

    private static void RestoreCollisionPolygons(TileData tileData, SerializedPropertyValue value)
    {
        var polygons = ReadArray(value, "tile collision polygons")
            .Select(polygonValue => ReadObject(polygonValue, "tile collision polygon"))
            .Select(polygon => new TileCollisionPolygonSnapshot(
                ReadInt32(Required(polygon, "layer_id", "tile collision polygon"), "tile collision layer"),
                ReadInt32(Required(polygon, "polygon_index", "tile collision polygon"), "tile collision polygon index"),
                ReadVector2Array(Required(polygon, "points", "tile collision polygon"), "tile collision polygon points"),
                ReadBool(Required(polygon, "one_way", "tile collision polygon"), "tile collision one-way flag"),
                ReadFloat(Required(polygon, "one_way_margin", "tile collision polygon"), "tile collision one-way margin")))
            .ToArray();

        foreach (var group in polygons.GroupBy(static polygon => polygon.LayerId))
        {
            tileData.SetCollisionPolygonsCount(group.Key, group.Max(static polygon => polygon.PolygonIndex) + 1);
        }

        foreach (var polygon in polygons)
        {
            tileData.SetCollisionPolygonPoints(polygon.LayerId, polygon.PolygonIndex, polygon.Points);
            tileData.SetCollisionPolygonOneWay(polygon.LayerId, polygon.PolygonIndex, polygon.OneWay);
            tileData.SetCollisionPolygonOneWayMargin(polygon.LayerId, polygon.PolygonIndex, polygon.OneWayMargin);
        }
    }

    private static SerializedPropertyDictionaryEntry Property(string name, SerializedPropertyValue value)
    {
        return new SerializedPropertyDictionaryEntry(Value(name), value);
    }

    private static SerializedPropertyValue Object(params SerializedPropertyDictionaryEntry[] properties)
    {
        return SerializedPropertyValue.FromDictionary(properties);
    }

    private static SerializedPropertyValue Array(IEnumerable<SerializedPropertyValue> values)
    {
        return SerializedPropertyValue.FromArray(values);
    }

    private static SerializedPropertyValue Value(string value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(bool value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(int value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(float value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(Vector2 value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(Vector2I value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(Color value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static IReadOnlyList<SerializedPropertyValue> ReadArray(SerializedPropertyValue value, string context)
    {
        if (value.Kind != SerializedPropertyValueKind.Array)
        {
            throw new FormatException($"Serialized {context} must be an array.");
        }

        return value.Items;
    }

    private static IReadOnlyDictionary<string, SerializedPropertyValue> ReadObject(SerializedPropertyValue value, string context)
    {
        if (value.Kind != SerializedPropertyValueKind.Dictionary)
        {
            throw new FormatException($"Serialized {context} must be a dictionary.");
        }

        return value.DictionaryEntries.ToDictionary(
            entry => entry.Key.VariantValue.AsString(),
            entry => entry.Value,
            StringComparer.Ordinal);
    }

    private static SerializedPropertyValue Required(
        IReadOnlyDictionary<string, SerializedPropertyValue> values,
        string name,
        string context)
    {
        return values.TryGetValue(name, out var value)
            ? value
            : throw new FormatException($"Serialized {context} is missing '{name}'.");
    }

    private static string ReadString(SerializedPropertyValue value, string context)
    {
        try
        {
            return SerializedPropertyValueConverter.ToValue<string>(value);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
        {
            throw new FormatException($"Serialized {context} must be a string.", exception);
        }
    }

    private static bool ReadBool(SerializedPropertyValue value, string context)
    {
        try
        {
            return SerializedPropertyValueConverter.ToValue<bool>(value);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
        {
            throw new FormatException($"Serialized {context} must be a boolean.", exception);
        }
    }

    private static int ReadInt32(SerializedPropertyValue value, string context)
    {
        try
        {
            return SerializedPropertyValueConverter.ToValue<int>(value);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException or OverflowException)
        {
            throw new FormatException($"Serialized {context} must be a 32-bit integer.", exception);
        }
    }

    private static float ReadFloat(SerializedPropertyValue value, string context)
    {
        try
        {
            return SerializedPropertyValueConverter.ToValue<float>(value);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException or OverflowException)
        {
            throw new FormatException($"Serialized {context} must be a floating-point value.", exception);
        }
    }

    private static Vector2 ReadVector2(SerializedPropertyValue value, string context)
    {
        try
        {
            return SerializedPropertyValueConverter.ToValue<Vector2>(value);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
        {
            throw new FormatException($"Serialized {context} must be a Vector2.", exception);
        }
    }

    private static Vector2I ReadVector2I(SerializedPropertyValue value, string context)
    {
        try
        {
            return SerializedPropertyValueConverter.ToValue<Vector2I>(value);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
        {
            throw new FormatException($"Serialized {context} must be a Vector2I.", exception);
        }
    }

    private static Color ReadColor(SerializedPropertyValue value, string context)
    {
        try
        {
            return SerializedPropertyValueConverter.ToValue<Color>(value);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
        {
            throw new FormatException($"Serialized {context} must be a Color.", exception);
        }
    }

    private static Vector2[] ReadVector2Array(SerializedPropertyValue value, string context)
    {
        return ReadArray(value, context)
            .Select(item => ReadVector2(item, context))
            .ToArray();
    }

    private readonly record struct TileCollisionPolygonSnapshot(
        int LayerId,
        int PolygonIndex,
        Vector2[] Points,
        bool OneWay,
        float OneWayMargin);
}
