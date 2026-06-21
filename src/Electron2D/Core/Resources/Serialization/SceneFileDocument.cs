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

internal sealed class SceneFileDocument
{
    public const string FormatName = "Electron2D.SceneFile";
    public const int CurrentVersion = 1;

    public SceneFileDocument(
        IEnumerable<ResourceFileExternalReference>? externalReferences,
        IEnumerable<SerializedResourceEntry>? internalResources,
        IEnumerable<SceneFileNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        ExternalReferences = (externalReferences ?? Array.Empty<ResourceFileExternalReference>())
            .OrderBy(reference => reference.Id)
            .ToArray();
        InternalResources = (internalResources ?? Array.Empty<SerializedResourceEntry>())
            .OrderBy(resource => resource.Id)
            .ToArray();
        Nodes = nodes.OrderBy(node => node.Id).ToArray();

        if (Nodes.Count == 0)
        {
            throw new ArgumentException("Scene file must contain at least one node.", nameof(nodes));
        }
    }

    public IReadOnlyList<ResourceFileExternalReference> ExternalReferences { get; }

    public IReadOnlyList<SerializedResourceEntry> InternalResources { get; }

    public IReadOnlyList<SceneFileNode> Nodes { get; }
}

internal sealed class SceneFileNode
{
    public SceneFileNode(
        int id,
        string type,
        string name,
        int? parentId,
        int? ownerId,
        IEnumerable<string>? persistentGroups = null,
        IReadOnlyDictionary<string, SerializedPropertyValue>? properties = null)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Scene node id must be positive.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        Id = id;
        Type = type;
        Name = name ?? string.Empty;
        ParentId = parentId;
        OwnerId = ownerId;
        PersistentGroups = (persistentGroups ?? Array.Empty<string>())
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(group => group, StringComparer.Ordinal)
            .ToArray();
        Properties = SerializedResourceDocument.CopyProperties(properties);
    }

    public int Id { get; }

    public string Type { get; }

    public string Name { get; }

    public int? ParentId { get; }

    public int? OwnerId { get; }

    public IReadOnlyList<string> PersistentGroups { get; }

    public IReadOnlyDictionary<string, SerializedPropertyValue> Properties { get; }

    public SceneFileNode WithProperties(IReadOnlyDictionary<string, SerializedPropertyValue> properties)
    {
        return new SceneFileNode(Id, Type, Name, ParentId, OwnerId, PersistentGroups, properties);
    }
}
