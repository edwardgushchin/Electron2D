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
namespace Electron2D.Editor.Inspector;

internal sealed class Inspector
{
    private readonly Dictionary<string, InspectorPropertyDescriptor> descriptors;
    private readonly InspectorUndoRedoStack undoRedo = new();
    private Electron2D.SceneFileDocument document;
    private int selectedNodeId;

    public Inspector(
        Electron2D.SceneFileDocument document,
        int selectedNodeId,
        IEnumerable<InspectorPropertyDescriptor> descriptors)
    {
        this.document = document ?? throw new ArgumentNullException(nameof(document));
        this.selectedNodeId = selectedNodeId;
        this.descriptors = ValidateDescriptors(descriptors);
        _ = GetSelectedNode();
    }

    public bool CanUndo => undoRedo.CanUndo;

    public bool CanRedo => undoRedo.CanRedo;

    public Electron2D.SceneFileDocument Document => document;

    public IReadOnlyList<InspectorPropertySnapshot> GetProperties()
    {
        var node = GetSelectedNode();
        return descriptors.Values
            .OrderBy(descriptor => descriptor.Name, StringComparer.Ordinal)
            .Select(descriptor => new InspectorPropertySnapshot(
                descriptor,
                node.Properties.TryGetValue(descriptor.Name, out var value) ? value : descriptor.DefaultValue))
            .ToArray();
    }

    public void SetProperty(string name, Electron2D.SerializedPropertyValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var descriptor = GetDescriptor(name);
        EnsureValueMatchesDescriptor(descriptor, value);

        var before = document;
        document = WithSelectedNodeProperty(document, descriptor.Name, value);
        undoRedo.Push($"Set {descriptor.Name}", before, document);
    }

    public void ResetProperty(string name)
    {
        var descriptor = GetDescriptor(name);

        var before = document;
        document = WithSelectedNodeProperty(document, descriptor.Name, descriptor.DefaultValue);
        undoRedo.Push($"Reset {descriptor.Name}", before, document);
    }

    public void SetNestedResourceProperty(
        string resourcePropertyName,
        string nestedPropertyName,
        Electron2D.SerializedPropertyValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var descriptor = GetDescriptor(resourcePropertyName);
        if (descriptor.Kind != InspectorPropertyKind.Resource)
        {
            throw new InvalidOperationException($"Inspector property '{resourcePropertyName}' is not a resource reference.");
        }

        var node = GetSelectedNode();
        if (!node.Properties.TryGetValue(resourcePropertyName, out var resourceReference) ||
            resourceReference.Kind != Electron2D.SerializedPropertyValueKind.Resource ||
            resourceReference.ReferenceScope != Electron2D.SerializedResourceReferenceScope.Internal)
        {
            throw new InvalidOperationException($"Inspector property '{resourcePropertyName}' is not an internal resource reference.");
        }

        var resource = document.InternalResources.SingleOrDefault(item => item.Id == resourceReference.ReferenceId)
            ?? throw new InvalidOperationException($"Internal resource '{resourceReference.ReferenceId}' was not found.");
        var metadata = Electron2D.ResourceObjectMetadataRegistry.GetBySerializedTypeName(resource.Type);
        if (!metadata.Properties.Any(property => string.Equals(property.Name, nestedPropertyName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Nested resource property '{nestedPropertyName}' is not registered for '{resource.Type}'.");
        }

        var before = document;
        document = WithInternalResourceProperty(document, resource.Id, nestedPropertyName, value);
        undoRedo.Push($"Set {resourcePropertyName}.{nestedPropertyName}", before, document);
    }

    public void Undo()
    {
        document = undoRedo.Undo();
        _ = GetSelectedNode();
    }

    public void Redo()
    {
        document = undoRedo.Redo();
        _ = GetSelectedNode();
    }

    public void Save(string scenePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        var directory = Path.GetDirectoryName(Path.GetFullPath(scenePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(scenePath, Electron2D.SceneFileTextSerializer.Serialize(document));
    }

    private Electron2D.SceneFileNode GetSelectedNode()
    {
        return document.Nodes.SingleOrDefault(node => node.Id == selectedNodeId)
            ?? throw new InvalidOperationException($"Selected scene node '{selectedNodeId}' was not found.");
    }

    private InspectorPropertyDescriptor GetDescriptor(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return descriptors.TryGetValue(name, out var descriptor)
            ? descriptor
            : throw new InvalidOperationException($"Inspector property '{name}' is not editable.");
    }

    private static Dictionary<string, InspectorPropertyDescriptor> ValidateDescriptors(
        IEnumerable<InspectorPropertyDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var result = new Dictionary<string, InspectorPropertyDescriptor>(StringComparer.Ordinal);
        foreach (var descriptor in descriptors)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            if (!result.TryAdd(descriptor.Name, descriptor))
            {
                throw new ArgumentException($"Duplicate Inspector property descriptor '{descriptor.Name}'.", nameof(descriptors));
            }
        }

        return result;
    }

    private static void EnsureValueMatchesDescriptor(
        InspectorPropertyDescriptor descriptor,
        Electron2D.SerializedPropertyValue value)
    {
        var matches = descriptor.Kind switch
        {
            InspectorPropertyKind.Primitive => value.Kind == Electron2D.SerializedPropertyValueKind.Variant,
            InspectorPropertyKind.NodePath => value.Kind == Electron2D.SerializedPropertyValueKind.Variant &&
                value.VariantValue.VariantType == Electron2D.Variant.Type.NodePath,
            InspectorPropertyKind.Enum => value.Kind == Electron2D.SerializedPropertyValueKind.Enum,
            InspectorPropertyKind.Flags => value.Kind == Electron2D.SerializedPropertyValueKind.Enum,
            InspectorPropertyKind.Array => value.Kind == Electron2D.SerializedPropertyValueKind.Array,
            InspectorPropertyKind.Resource => value.Kind == Electron2D.SerializedPropertyValueKind.Resource,
            _ => false
        };

        if (!matches)
        {
            throw new InvalidOperationException(
                $"Serialized value kind '{value.Kind}' does not match Inspector property '{descriptor.Name}' kind '{descriptor.Kind}'.");
        }
    }

    private Electron2D.SceneFileDocument WithSelectedNodeProperty(
        Electron2D.SceneFileDocument source,
        string propertyName,
        Electron2D.SerializedPropertyValue value)
    {
        var nodes = source.Nodes
            .Select(node => node.Id == selectedNodeId ? node.WithProperties(WithProperty(node.Properties, propertyName, value)) : node)
            .ToArray();

        return new Electron2D.SceneFileDocument(source.ExternalReferences, source.InternalResources, nodes);
    }

    private static Electron2D.SceneFileDocument WithInternalResourceProperty(
        Electron2D.SceneFileDocument source,
        int resourceId,
        string propertyName,
        Electron2D.SerializedPropertyValue value)
    {
        var resources = source.InternalResources
            .Select(resource => resource.Id == resourceId
                ? new Electron2D.SerializedResourceEntry(resource.Id, resource.Type, WithProperty(resource.Properties, propertyName, value))
                : resource)
            .ToArray();

        return new Electron2D.SceneFileDocument(source.ExternalReferences, resources, source.Nodes);
    }

    private static IReadOnlyDictionary<string, Electron2D.SerializedPropertyValue> WithProperty(
        IReadOnlyDictionary<string, Electron2D.SerializedPropertyValue> properties,
        string propertyName,
        Electron2D.SerializedPropertyValue value)
    {
        var result = properties.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        result[propertyName] = value;
        return result;
    }
}
