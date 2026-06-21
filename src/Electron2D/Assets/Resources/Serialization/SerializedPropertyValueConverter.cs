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

internal static class SerializedPropertyValueConverter
{
    public static SerializedPropertyValue FromValue<TValue>(TValue value)
    {
        var declaredType = typeof(TValue);
        var nullableType = Nullable.GetUnderlyingType(declaredType);
        if (nullableType is not null)
        {
            return SerializedPropertyValue.FromNullable(
                nullableType,
                value is null ? null : FromObject(value, nullableType));
        }

        return FromObject(value, declaredType);
    }

    public static TValue ToValue<TValue>(SerializedPropertyValue value)
    {
        return (TValue)ToObject(value, typeof(TValue))!;
    }

    public static SerializedPropertyValue FromArray<TElement>(IEnumerable<TElement> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return SerializedPropertyValue.FromArray(values.Select(FromValue));
    }

    public static TElement[] ToArray<TElement>(SerializedPropertyValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Kind != SerializedPropertyValueKind.Array)
        {
            throw new FormatException($"Serialized value kind '{value.Kind}' cannot be converted to an array.");
        }

        return value.Items.Select(ToValue<TElement>).ToArray();
    }

    public static SerializedPropertyValue FromDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> values)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(values);
        return SerializedPropertyValue.FromDictionary(
            values.Select(pair => new SerializedPropertyDictionaryEntry(
                FromValue(pair.Key),
                FromValue(pair.Value))));
    }

    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(SerializedPropertyValue value)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Kind != SerializedPropertyValueKind.Dictionary)
        {
            throw new FormatException($"Serialized value kind '{value.Kind}' cannot be converted to a dictionary.");
        }

        var dictionary = new Dictionary<TKey, TValue>();
        foreach (var entry in value.DictionaryEntries)
        {
            var key = ToValue<TKey>(entry.Key);
            dictionary.Add(key, ToValue<TValue>(entry.Value));
        }

        return dictionary;
    }

    private static SerializedPropertyValue FromObject(object? value, Type declaredType)
    {
        ArgumentNullException.ThrowIfNull(declaredType);

        if (value is null)
        {
            return SerializedPropertyValue.FromVariant(default);
        }

        if (declaredType.IsEnum || value.GetType().IsEnum)
        {
            return SerializedPropertyValue.FromEnum((Enum)value);
        }

        if (value is Resource)
        {
            throw new InvalidOperationException("Resource object properties require an explicit resource reference slot.");
        }

        return SerializedPropertyValue.FromVariant(Variant.CreateFrom(value));
    }

    private static object? ToObject(SerializedPropertyValue value, Type targetType)
    {
        var nullableType = Nullable.GetUnderlyingType(targetType);
        if (nullableType is not null)
        {
            if (value.Kind != SerializedPropertyValueKind.Nullable)
            {
                throw new FormatException($"Serialized value for '{targetType.FullName}' must be Nullable.");
            }

            return value.NullableValue is null ? null : ToObject(value.NullableValue, nullableType);
        }

        if (targetType.IsEnum)
        {
            if (value.Kind == SerializedPropertyValueKind.Enum)
            {
                return Enum.ToObject(targetType, value.EnumValue);
            }

            if (value.Kind == SerializedPropertyValueKind.Variant)
            {
                return Enum.ToObject(targetType, value.VariantValue.AsInt64());
            }
        }

        if (value.Kind != SerializedPropertyValueKind.Variant)
        {
            throw new FormatException($"Serialized value kind '{value.Kind}' cannot be converted to '{targetType.FullName}'.");
        }

        if (!targetType.IsValueType && value.VariantValue.IsNil())
        {
            return null;
        }

        return ReadVariantAs(value.VariantValue, targetType);
    }

    private static object? ReadVariantAs(Variant variant, Type targetType)
    {
        if (targetType == typeof(Variant))
        {
            return variant;
        }

        if (targetType == typeof(bool))
        {
            return variant.AsBool();
        }

        if (targetType == typeof(sbyte))
        {
            return checked((sbyte)variant.AsInt64());
        }

        if (targetType == typeof(byte))
        {
            return checked((byte)variant.AsInt64());
        }

        if (targetType == typeof(short))
        {
            return checked((short)variant.AsInt64());
        }

        if (targetType == typeof(ushort))
        {
            return checked((ushort)variant.AsInt64());
        }

        if (targetType == typeof(int))
        {
            return variant.AsInt32();
        }

        if (targetType == typeof(uint))
        {
            return checked((uint)variant.AsInt64());
        }

        if (targetType == typeof(long))
        {
            return variant.AsInt64();
        }

        if (targetType == typeof(ulong))
        {
            return checked((ulong)variant.AsInt64());
        }

        if (targetType == typeof(float))
        {
            return checked((float)variant.AsDouble());
        }

        if (targetType == typeof(double))
        {
            return variant.AsDouble();
        }

        if (targetType == typeof(string))
        {
            return variant.AsString();
        }

        if (targetType == typeof(Vector2))
        {
            return variant.AsVector2();
        }

        if (targetType == typeof(Vector2I))
        {
            return variant.AsVector2I();
        }

        if (targetType == typeof(Rect2))
        {
            return variant.AsRect2();
        }

        if (targetType == typeof(Rect2I))
        {
            return variant.AsRect2I();
        }

        if (targetType == typeof(Transform2D))
        {
            return variant.AsTransform2D();
        }

        if (targetType == typeof(Color))
        {
            return variant.AsColor();
        }

        if (targetType == typeof(StringName))
        {
            return variant.AsStringName();
        }

        if (targetType == typeof(NodePath))
        {
            return variant.AsNodePath();
        }

        throw new ArgumentException(
            $"Type '{targetType.FullName}' is not supported by the serialized property converter.",
            nameof(targetType));
    }
}
