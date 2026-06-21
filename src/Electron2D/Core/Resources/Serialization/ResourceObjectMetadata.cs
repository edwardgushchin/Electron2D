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

internal sealed class ResourceObjectPropertyMetadata
{
    private readonly Func<Resource, SerializedPropertyValue> _capture;
    private readonly Action<Resource, SerializedPropertyValue> _restore;

    private ResourceObjectPropertyMetadata(
        Type resourceType,
        Type valueType,
        string name,
        Func<Resource, SerializedPropertyValue> capture,
        Action<Resource, SerializedPropertyValue> restore)
    {
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new ArgumentException("Resource object property metadata name must not be empty.", nameof(name));
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _restore = restore ?? throw new ArgumentNullException(nameof(restore));
    }

    public string Name { get; }

    public Type ResourceType { get; }

    public Type ValueType { get; }

    public static ResourceObjectPropertyMetadata Create<TResource, TValue>(
        string name,
        Func<TResource, TValue> getter,
        Action<TResource, TValue> setter)
        where TResource : Resource
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);

        return new ResourceObjectPropertyMetadata(
            typeof(TResource),
            typeof(TValue),
            name,
            resource => SerializedPropertyValueConverter.FromValue(getter((TResource)resource)),
            (resource, value) => setter((TResource)resource, SerializedPropertyValueConverter.ToValue<TValue>(value)));
    }

    public static ResourceObjectPropertyMetadata CreateArray<TResource, TElement>(
        string name,
        Func<TResource, TElement[]> getter,
        Action<TResource, TElement[]> setter)
        where TResource : Resource
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);

        return new ResourceObjectPropertyMetadata(
            typeof(TResource),
            typeof(TElement[]),
            name,
            resource => SerializedPropertyValueConverter.FromArray(getter((TResource)resource)),
            (resource, value) => setter((TResource)resource, SerializedPropertyValueConverter.ToArray<TElement>(value)));
    }

    public static ResourceObjectPropertyMetadata CreateDictionary<TResource, TKey, TValue>(
        string name,
        Func<TResource, Dictionary<TKey, TValue>> getter,
        Action<TResource, Dictionary<TKey, TValue>> setter)
        where TResource : Resource
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);

        return new ResourceObjectPropertyMetadata(
            typeof(TResource),
            typeof(Dictionary<TKey, TValue>),
            name,
            resource => SerializedPropertyValueConverter.FromDictionary(getter((TResource)resource)),
            (resource, value) => setter((TResource)resource, SerializedPropertyValueConverter.ToDictionary<TKey, TValue>(value)));
    }

    public SerializedPropertyValue Capture(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        EnsureResourceType(resource);
        return _capture(resource);
    }

    public void Restore(Resource resource, SerializedPropertyValue value)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(value);
        EnsureResourceType(resource);
        _restore(resource, value);
    }

    private void EnsureResourceType(Resource resource)
    {
        if (resource.GetType() != ResourceType)
        {
            throw new InvalidOperationException(
                $"Metadata property '{Name}' targets '{ResourceType.FullName}', not '{resource.GetType().FullName}'.");
        }
    }
}

internal sealed class ResourceObjectTypeMetadata
{
    private readonly Func<Resource> _factory;

    private ResourceObjectTypeMetadata(
        Type resourceType,
        string serializedTypeName,
        Func<Resource> factory,
        IEnumerable<ResourceObjectPropertyMetadata> properties)
    {
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        SerializedTypeName = !string.IsNullOrWhiteSpace(serializedTypeName)
            ? serializedTypeName
            : throw new ArgumentException("Serialized resource type name must not be empty.", nameof(serializedTypeName));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Properties = ValidateProperties(resourceType, properties).ToArray();
    }

    public Type ResourceType { get; }

    public string SerializedTypeName { get; }

    public IReadOnlyList<ResourceObjectPropertyMetadata> Properties { get; }

    public static ResourceObjectTypeMetadata Create<TResource>(
        string serializedTypeName,
        Func<TResource> factory,
        IEnumerable<ResourceObjectPropertyMetadata> properties)
        where TResource : Resource
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new ResourceObjectTypeMetadata(
            typeof(TResource),
            serializedTypeName,
            () => factory(),
            properties);
    }

    public Resource CreateInstance()
    {
        var resource = _factory();
        if (resource.GetType() != ResourceType)
        {
            throw new InvalidOperationException(
                $"Resource metadata factory for '{SerializedTypeName}' returned '{resource.GetType().FullName}'.");
        }

        return resource;
    }

    private static IEnumerable<ResourceObjectPropertyMetadata> ValidateProperties(
        Type resourceType,
        IEnumerable<ResourceObjectPropertyMetadata> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var propertyList = properties.ToArray();
        foreach (var property in propertyList)
        {
            if (property.ResourceType != resourceType)
            {
                throw new ArgumentException(
                    $"Property metadata '{property.Name}' targets '{property.ResourceType.FullName}', not '{resourceType.FullName}'.",
                    nameof(properties));
            }
        }

        var duplicate = propertyList
            .GroupBy(property => property.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate resource object property metadata name '{duplicate.Key}'.", nameof(properties));
        }

        return propertyList.OrderBy(property => property.Name, StringComparer.Ordinal);
    }
}

internal static class ResourceObjectMetadataRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<Type, ResourceObjectTypeMetadata> ByResourceType = [];
    private static readonly Dictionary<string, ResourceObjectTypeMetadata> BySerializedTypeName = new(StringComparer.Ordinal);

    public static void Register(ResourceObjectTypeMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        lock (SyncRoot)
        {
            if (BySerializedTypeName.TryGetValue(metadata.SerializedTypeName, out var registeredByName) &&
                registeredByName.ResourceType != metadata.ResourceType)
            {
                throw new InvalidOperationException(
                    $"Serialized resource type name '{metadata.SerializedTypeName}' is already registered for '{registeredByName.ResourceType.FullName}'.");
            }

            if (ByResourceType.TryGetValue(metadata.ResourceType, out var registeredByType))
            {
                BySerializedTypeName.Remove(registeredByType.SerializedTypeName);
            }

            ByResourceType[metadata.ResourceType] = metadata;
            BySerializedTypeName[metadata.SerializedTypeName] = metadata;
        }
    }

    public static ResourceObjectTypeMetadata GetByResourceType(Type resourceType)
    {
        ArgumentNullException.ThrowIfNull(resourceType);

        lock (SyncRoot)
        {
            if (ByResourceType.TryGetValue(resourceType, out var metadata))
            {
                return metadata;
            }
        }

        throw new InvalidOperationException(
            $"AOT-safe metadata is not registered for Resource type '{resourceType.FullName}'.");
    }

    public static ResourceObjectTypeMetadata GetBySerializedTypeName(string serializedTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializedTypeName);

        lock (SyncRoot)
        {
            if (BySerializedTypeName.TryGetValue(serializedTypeName, out var metadata))
            {
                return metadata;
            }
        }

        throw new InvalidOperationException(
            $"AOT-safe metadata is not registered for serialized Resource type '{serializedTypeName}'.");
    }
}
