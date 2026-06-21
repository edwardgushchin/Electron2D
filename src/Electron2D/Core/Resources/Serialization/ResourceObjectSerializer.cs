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
using System.Collections;
using System.Reflection;

namespace Electron2D;

internal static class ResourceObjectSerializer
{
    public static SerializedResourceDocument Capture(Resource resource, string path)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return new SerializedResourceDocument(
            ResourceUid.CreateIdForPath(path),
            resource.GetType().FullName ?? resource.GetType().Name,
            path,
            properties: CaptureProperties(resource));
    }

    public static Resource Instantiate(SerializedResourceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var type = ResolveType(document.Type);
        if (!typeof(Resource).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"Serialized type '{document.Type}' is not a Resource.");
        }

        if (Activator.CreateInstance(type, nonPublic: true) is not Resource resource)
        {
            throw new InvalidOperationException($"Serialized type '{document.Type}' cannot be instantiated.");
        }

        resource.TakeOverPath(document.Path);
        foreach (var property in GetSerializableProperties(type))
        {
            if (!document.Properties.TryGetValue(property.Name, out var value))
            {
                continue;
            }

            property.SetValue(resource, SerializedPropertyValueConverter.ToClr(value, property.PropertyType));
        }

        return resource;
    }

    private static IReadOnlyDictionary<string, SerializedPropertyValue> CaptureProperties(Resource resource)
    {
        var properties = new Dictionary<string, SerializedPropertyValue>(StringComparer.Ordinal);
        foreach (var property in GetSerializableProperties(resource.GetType()))
        {
            properties.Add(property.Name, SerializedPropertyValueConverter.FromClr(
                property.GetValue(resource),
                property.PropertyType));
        }

        return properties;
    }

    private static IEnumerable<PropertyInfo> GetSerializableProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property =>
                property.GetMethod?.IsPublic == true &&
                property.SetMethod?.IsPublic == true &&
                property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal);
    }

    private static Type ResolveType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (type is not null)
            {
                return type;
            }
        }

        throw new InvalidOperationException($"Serialized type '{typeName}' cannot be resolved.");
    }

    private static class SerializedPropertyValueConverter
    {
        public static SerializedPropertyValue FromClr(object? value, Type declaredType)
        {
            ArgumentNullException.ThrowIfNull(declaredType);

            var nullableType = Nullable.GetUnderlyingType(declaredType);
            if (nullableType is not null)
            {
                return SerializedPropertyValue.FromNullable(
                    nullableType,
                    value is null ? null : FromClr(value, nullableType));
            }

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

            if (declaredType.IsArray && value is Array array)
            {
                var elementType = declaredType.GetElementType() ?? typeof(object);
                return SerializedPropertyValue.FromArray(array.Cast<object?>().Select(item => FromClr(item, elementType)));
            }

            if (value is IDictionary dictionary && TryGetDictionaryTypes(declaredType, out var keyType, out var valueType))
            {
                var entries = new List<SerializedPropertyDictionaryEntry>();
                foreach (DictionaryEntry entry in dictionary)
                {
                    entries.Add(new SerializedPropertyDictionaryEntry(
                        FromClr(entry.Key, keyType),
                        FromClr(entry.Value, valueType)));
                }

                return SerializedPropertyValue.FromDictionary(entries);
            }

            return SerializedPropertyValue.FromVariant(Variant.CreateFrom(value));
        }

        public static object? ToClr(SerializedPropertyValue value, Type targetType)
        {
            var nullableType = Nullable.GetUnderlyingType(targetType);
            if (nullableType is not null)
            {
                if (value.Kind != SerializedPropertyValueKind.Nullable)
                {
                    throw new FormatException($"Serialized value for '{targetType.FullName}' must be Nullable.");
                }

                return value.NullableValue is null ? null : ToClr(value.NullableValue, nullableType);
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

            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType() ?? typeof(object);
                var result = Array.CreateInstance(elementType, value.Items.Count);
                for (var index = 0; index < value.Items.Count; index++)
                {
                    result.SetValue(ToClr(value.Items[index], elementType), index);
                }

                return result;
            }

            if (typeof(IDictionary).IsAssignableFrom(targetType) &&
                TryGetDictionaryTypes(targetType, out var keyType, out var valueType))
            {
                if (Activator.CreateInstance(targetType) is not IDictionary result)
                {
                    throw new InvalidOperationException($"Dictionary type '{targetType.FullName}' cannot be instantiated.");
                }

                foreach (var entry in value.DictionaryEntries)
                {
                    var key = ToClr(entry.Key, keyType) ??
                        throw new FormatException("Serialized dictionary entry key must not be null.");
                    result.Add(key, ToClr(entry.Value, valueType));
                }

                return result;
            }

            if (value.Kind != SerializedPropertyValueKind.Variant)
            {
                throw new FormatException($"Serialized value kind '{value.Kind}' cannot be converted to '{targetType.FullName}'.");
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

        private static bool TryGetDictionaryTypes(Type type, out Type keyType, out Type valueType)
        {
            var dictionaryType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                ? type
                : type.GetInterfaces().FirstOrDefault(
                    iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>));

            if (dictionaryType is null)
            {
                keyType = typeof(object);
                valueType = typeof(object);
                return false;
            }

            var arguments = dictionaryType.GetGenericArguments();
            keyType = arguments[0];
            valueType = arguments[1];
            return true;
        }
    }
}
