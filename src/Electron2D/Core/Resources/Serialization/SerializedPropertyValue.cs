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

internal enum SerializedPropertyValueKind
{
    Variant,
    Enum,
    Nullable,
    Resource,
    Array,
    Dictionary
}

internal enum SerializedResourceReferenceScope
{
    External,
    Internal
}

internal sealed class SerializedPropertyDictionaryEntry
{
    public SerializedPropertyDictionaryEntry(SerializedPropertyValue key, SerializedPropertyValue value)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public SerializedPropertyValue Key { get; }

    public SerializedPropertyValue Value { get; }
}

internal sealed class SerializedPropertyValue
{
    private SerializedPropertyValue(
        SerializedPropertyValueKind kind,
        Variant variantValue,
        string enumType,
        string enumName,
        long enumValue,
        string nullableType,
        SerializedPropertyValue? nullableValue,
        SerializedResourceReferenceScope referenceScope,
        int referenceId,
        IEnumerable<SerializedPropertyValue>? items,
        IEnumerable<SerializedPropertyDictionaryEntry>? dictionaryEntries)
    {
        Kind = kind;
        VariantValue = variantValue;
        EnumType = enumType;
        EnumName = enumName;
        EnumValue = enumValue;
        NullableType = nullableType;
        NullableValue = nullableValue;
        ReferenceScope = referenceScope;
        ReferenceId = referenceId;
        Items = (items ?? Array.Empty<SerializedPropertyValue>()).ToArray();
        DictionaryEntries = (dictionaryEntries ?? Array.Empty<SerializedPropertyDictionaryEntry>()).ToArray();
    }

    public SerializedPropertyValueKind Kind { get; }

    public Variant VariantValue { get; }

    public string EnumType { get; }

    public string EnumName { get; }

    public long EnumValue { get; }

    public string NullableType { get; }

    public SerializedPropertyValue? NullableValue { get; }

    public SerializedResourceReferenceScope ReferenceScope { get; }

    public int ReferenceId { get; }

    public IReadOnlyList<SerializedPropertyValue> Items { get; }

    public IReadOnlyList<SerializedPropertyDictionaryEntry> DictionaryEntries { get; }

    public static SerializedPropertyValue FromVariant(Variant value)
    {
        _ = VariantTextSerializer.Serialize(value);
        return new SerializedPropertyValue(
            SerializedPropertyValueKind.Variant,
            value,
            string.Empty,
            string.Empty,
            0L,
            string.Empty,
            null,
            SerializedResourceReferenceScope.External,
            0,
            null,
            null);
    }

    public static SerializedPropertyValue FromEnum(Enum value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return FromEnum(
            value.GetType().FullName ?? value.GetType().Name,
            value.ToString(),
            Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture));
    }

    public static SerializedPropertyValue FromEnum(string enumType, string enumName, long enumValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enumType);
        ArgumentException.ThrowIfNullOrWhiteSpace(enumName);

        return new SerializedPropertyValue(
            SerializedPropertyValueKind.Enum,
            default,
            enumType,
            enumName,
            enumValue,
            string.Empty,
            null,
            SerializedResourceReferenceScope.External,
            0,
            null,
            null);
    }

    public static SerializedPropertyValue FromNullable(Type underlyingType, SerializedPropertyValue? value)
    {
        ArgumentNullException.ThrowIfNull(underlyingType);
        return FromNullable(underlyingType.FullName ?? underlyingType.Name, value);
    }

    public static SerializedPropertyValue FromNullable(string underlyingType, SerializedPropertyValue? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(underlyingType);

        return new SerializedPropertyValue(
            SerializedPropertyValueKind.Nullable,
            default,
            string.Empty,
            string.Empty,
            0L,
            underlyingType,
            value,
            SerializedResourceReferenceScope.External,
            0,
            null,
            null);
    }

    public static SerializedPropertyValue ExternalResource(int id)
    {
        return Resource(SerializedResourceReferenceScope.External, id);
    }

    public static SerializedPropertyValue InternalResource(int id)
    {
        return Resource(SerializedResourceReferenceScope.Internal, id);
    }

    public static SerializedPropertyValue FromArray(IEnumerable<SerializedPropertyValue> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new SerializedPropertyValue(
            SerializedPropertyValueKind.Array,
            default,
            string.Empty,
            string.Empty,
            0L,
            string.Empty,
            null,
            SerializedResourceReferenceScope.External,
            0,
            items,
            null);
    }

    public static SerializedPropertyValue FromDictionary(IEnumerable<SerializedPropertyDictionaryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return new SerializedPropertyValue(
            SerializedPropertyValueKind.Dictionary,
            default,
            string.Empty,
            string.Empty,
            0L,
            string.Empty,
            null,
            SerializedResourceReferenceScope.External,
            0,
            null,
            entries);
    }

    private static SerializedPropertyValue Resource(SerializedResourceReferenceScope scope, int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Resource reference id must be positive.", nameof(id));
        }

        return new SerializedPropertyValue(
            SerializedPropertyValueKind.Resource,
            default,
            string.Empty,
            string.Empty,
            0L,
            string.Empty,
            null,
            scope,
            id,
            null,
            null);
    }
}
