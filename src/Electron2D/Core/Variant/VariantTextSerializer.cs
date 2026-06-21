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

using VariantArray = Electron2D.Collections.Array;
using VariantDictionary = Electron2D.Collections.Dictionary;

namespace Electron2D;

internal static class VariantTextSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public static string Serialize(Variant variant)
    {
        return WriteVariant(variant).ToJsonString(SerializerOptions);
    }

    public static Variant Deserialize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            return ReadVariant(JsonNode.Parse(text));
        }
        catch (JsonException exception)
        {
            throw new FormatException("Variant JSON text is malformed.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException("Variant JSON text is malformed.", exception);
        }
    }

    private static JsonObject WriteVariant(Variant variant)
    {
        var result = new JsonObject
        {
            ["type"] = variant.VariantType.ToString()
        };

        switch (variant.VariantType)
        {
            case Variant.Type.Nil:
                result["value"] = null;
                break;
            case Variant.Type.Bool:
                result["value"] = variant.AsBool();
                break;
            case Variant.Type.Int:
                result["value"] = variant.AsInt64();
                break;
            case Variant.Type.Float:
                result["value"] = variant.AsDouble();
                break;
            case Variant.Type.String:
                result["value"] = variant.AsString();
                break;
            case Variant.Type.Vector2:
                result["value"] = WriteVector2(variant.AsVector2());
                break;
            case Variant.Type.Vector2I:
                result["value"] = WriteVector2I(variant.AsVector2I());
                break;
            case Variant.Type.Rect2:
                result["value"] = WriteRect2(variant.AsRect2());
                break;
            case Variant.Type.Rect2I:
                result["value"] = WriteRect2I(variant.AsRect2I());
                break;
            case Variant.Type.Transform2D:
                result["value"] = WriteTransform2D(variant.AsTransform2D());
                break;
            case Variant.Type.Color:
                result["value"] = WriteColor(variant.AsColor());
                break;
            case Variant.Type.StringName:
                result["value"] = variant.AsStringName().ToString();
                break;
            case Variant.Type.NodePath:
                result["value"] = variant.AsNodePath().ToString();
                break;
            case Variant.Type.Array:
                result["value"] = WriteArray(variant.AsArray());
                break;
            case Variant.Type.Dictionary:
                result["value"] = WriteDictionary(variant.AsDictionary());
                break;
            case Variant.Type.Rid:
            case Variant.Type.Object:
            case Variant.Type.Callable:
                throw new InvalidOperationException(
                    $"Variant type '{variant.VariantType}' is not serializable by the Electron2D 0.1 stable Variant format.");
            default:
                throw new InvalidOperationException($"Variant type '{variant.VariantType}' is not supported.");
        }

        return result;
    }

    private static Variant ReadVariant(JsonNode? node)
    {
        var variantObject = ExpectObject(node, "Variant JSON value");
        var typeName = ReadString(variantObject, "type", "Variant type");

        if (!Enum.TryParse<Variant.Type>(typeName, ignoreCase: false, out var variantType))
        {
            throw new FormatException($"Variant type '{typeName}' is not supported by Electron2D 0.1 stable Variant format.");
        }

        var value = ReadRequiredProperty(variantObject, "value", "Variant value");

        return variantType switch
        {
            Variant.Type.Nil => default,
            Variant.Type.Bool => ReadBool(value, "Bool value"),
            Variant.Type.Int => ReadInt64(value, "Int value"),
            Variant.Type.Float => ReadDouble(value, "Float value"),
            Variant.Type.String => ReadString(value, "String value"),
            Variant.Type.Vector2 => ReadVector2(value),
            Variant.Type.Vector2I => ReadVector2I(value),
            Variant.Type.Rect2 => ReadRect2(value),
            Variant.Type.Rect2I => ReadRect2I(value),
            Variant.Type.Transform2D => ReadTransform2D(value),
            Variant.Type.Color => ReadColor(value),
            Variant.Type.StringName => new StringName(ReadString(value, "StringName value")),
            Variant.Type.NodePath => new NodePath(ReadString(value, "NodePath value")),
            Variant.Type.Array => ReadArray(value),
            Variant.Type.Dictionary => ReadDictionary(value),
            Variant.Type.Rid or Variant.Type.Object or Variant.Type.Callable => throw new FormatException(
                $"Variant type '{variantType}' is not serializable by the Electron2D 0.1 stable Variant format."),
            _ => throw new FormatException($"Variant type '{variantType}' is not supported.")
        };
    }

    private static JsonObject WriteVector2(Vector2 vector)
    {
        return new JsonObject
        {
            ["x"] = vector.X,
            ["y"] = vector.Y
        };
    }

    private static JsonObject WriteVector2I(Vector2I vector)
    {
        return new JsonObject
        {
            ["x"] = vector.X,
            ["y"] = vector.Y
        };
    }

    private static JsonObject WriteRect2(Rect2 rect)
    {
        return new JsonObject
        {
            ["position"] = WriteVector2(rect.Position),
            ["size"] = WriteVector2(rect.Size)
        };
    }

    private static JsonObject WriteRect2I(Rect2I rect)
    {
        return new JsonObject
        {
            ["position"] = WriteVector2I(rect.Position),
            ["size"] = WriteVector2I(rect.Size)
        };
    }

    private static JsonObject WriteTransform2D(Transform2D transform)
    {
        return new JsonObject
        {
            ["x"] = WriteVector2(transform.X),
            ["y"] = WriteVector2(transform.Y),
            ["origin"] = WriteVector2(transform.Origin)
        };
    }

    private static JsonObject WriteColor(Color color)
    {
        return new JsonObject
        {
            ["r"] = color.R,
            ["g"] = color.G,
            ["b"] = color.B,
            ["a"] = color.A
        };
    }

    private static JsonArray WriteArray(VariantArray array)
    {
        var result = new JsonArray();
        foreach (var value in array)
        {
            result.Add((JsonNode)WriteVariant(value));
        }

        return result;
    }

    private static JsonArray WriteDictionary(VariantDictionary dictionary)
    {
        var entries = dictionary
            .Select(pair => new SerializedDictionaryEntry(Serialize(pair.Key), Serialize(pair.Value)))
            .OrderBy(entry => entry.Key, StringComparer.Ordinal);

        var result = new JsonArray();
        foreach (var entry in entries)
        {
            result.Add((JsonNode)new JsonObject
            {
                ["key"] = JsonNode.Parse(entry.Key),
                ["value"] = JsonNode.Parse(entry.Value)
            });
        }

        return result;
    }

    private static Vector2 ReadVector2(JsonNode? node)
    {
        var value = ExpectObject(node, "Vector2 value");
        return new Vector2(
            ReadSingle(value, "x", "Vector2.x"),
            ReadSingle(value, "y", "Vector2.y"));
    }

    private static Vector2I ReadVector2I(JsonNode? node)
    {
        var value = ExpectObject(node, "Vector2I value");
        return new Vector2I(
            ReadInt32(value, "x", "Vector2I.x"),
            ReadInt32(value, "y", "Vector2I.y"));
    }

    private static Rect2 ReadRect2(JsonNode? node)
    {
        var value = ExpectObject(node, "Rect2 value");
        return new Rect2(
            ReadVector2(ReadRequiredProperty(value, "position", "Rect2.position")),
            ReadVector2(ReadRequiredProperty(value, "size", "Rect2.size")));
    }

    private static Rect2I ReadRect2I(JsonNode? node)
    {
        var value = ExpectObject(node, "Rect2I value");
        return new Rect2I(
            ReadVector2I(ReadRequiredProperty(value, "position", "Rect2I.position")),
            ReadVector2I(ReadRequiredProperty(value, "size", "Rect2I.size")));
    }

    private static Transform2D ReadTransform2D(JsonNode? node)
    {
        var value = ExpectObject(node, "Transform2D value");
        return new Transform2D(
            ReadVector2(ReadRequiredProperty(value, "x", "Transform2D.x")),
            ReadVector2(ReadRequiredProperty(value, "y", "Transform2D.y")),
            ReadVector2(ReadRequiredProperty(value, "origin", "Transform2D.origin")));
    }

    private static Color ReadColor(JsonNode? node)
    {
        var value = ExpectObject(node, "Color value");
        return new Color(
            ReadSingle(value, "r", "Color.r"),
            ReadSingle(value, "g", "Color.g"),
            ReadSingle(value, "b", "Color.b"),
            ReadSingle(value, "a", "Color.a"));
    }

    private static VariantArray ReadArray(JsonNode? node)
    {
        var values = ExpectArray(node, "Array value");
        var result = new VariantArray();

        foreach (var value in values)
        {
            result.Add(ReadVariant(value));
        }

        return result;
    }

    private static VariantDictionary ReadDictionary(JsonNode? node)
    {
        var entries = ExpectArray(node, "Dictionary value");
        var result = new VariantDictionary();

        foreach (var entryNode in entries)
        {
            var entry = ExpectObject(entryNode, "Dictionary entry");
            var key = ReadVariant(ReadRequiredProperty(entry, "key", "Dictionary entry key"));
            var value = ReadVariant(ReadRequiredProperty(entry, "value", "Dictionary entry value"));

            try
            {
                result.Add(key, value);
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("Dictionary value contains a duplicate key.", exception);
            }
        }

        return result;
    }

    private static JsonObject ExpectObject(JsonNode? node, string context)
    {
        return node as JsonObject ?? throw new FormatException($"{context} must be a JSON object.");
    }

    private static JsonArray ExpectArray(JsonNode? node, string context)
    {
        return node as JsonArray ?? throw new FormatException($"{context} must be a JSON array.");
    }

    private static JsonNode? ReadRequiredProperty(JsonObject obj, string propertyName, string context)
    {
        return obj.TryGetPropertyValue(propertyName, out var value)
            ? value
            : throw new FormatException($"{context} is missing required property '{propertyName}'.");
    }

    private static bool ReadBool(JsonNode? node, string context)
    {
        return ReadValue<bool>(node, context);
    }

    private static int ReadInt32(JsonObject obj, string propertyName, string context)
    {
        return ReadValue<int>(ReadRequiredProperty(obj, propertyName, context), context);
    }

    private static long ReadInt64(JsonNode? node, string context)
    {
        return ReadValue<long>(node, context);
    }

    private static float ReadSingle(JsonObject obj, string propertyName, string context)
    {
        return ReadValue<float>(ReadRequiredProperty(obj, propertyName, context), context);
    }

    private static double ReadDouble(JsonNode? node, string context)
    {
        return ReadValue<double>(node, context);
    }

    private static string ReadString(JsonObject obj, string propertyName, string context)
    {
        return ReadString(ReadRequiredProperty(obj, propertyName, context), context);
    }

    private static string ReadString(JsonNode? node, string context)
    {
        return ReadValue<string>(node, context);
    }

    private static T ReadValue<T>(JsonNode? node, string context)
    {
        if (node is null)
        {
            throw new FormatException($"{context} must not be null.");
        }

        try
        {
            return node.GetValue<T>();
        }
        catch (InvalidOperationException exception)
        {
            throw new FormatException($"{context} has an invalid JSON value.", exception);
        }
        catch (FormatException exception)
        {
            throw new FormatException($"{context} has an invalid JSON value.", exception);
        }
    }

    private readonly record struct SerializedDictionaryEntry(string Key, string Value);
}
