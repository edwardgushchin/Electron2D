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
using System.Collections.ObjectModel;

namespace Electron2D;

internal sealed class SerializedResourceDocument
{
    public const string FormatName = "Electron2D.SerializedResource";
    public const int CurrentVersion = 1;

    public SerializedResourceDocument(
        long uid,
        string type,
        string path,
        IEnumerable<ResourceFileExternalReference>? externalReferences = null,
        IEnumerable<SerializedResourceEntry>? internalResources = null,
        IReadOnlyDictionary<string, SerializedPropertyValue>? properties = null)
    {
        if (uid <= 0 || uid == ResourceUid.InvalidId)
        {
            throw new ArgumentException("Serialized resource UID must be positive.", nameof(uid));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Uid = uid;
        Type = type;
        Path = path;
        ExternalReferences = CopyExternalReferences(externalReferences);
        InternalResources = CopyInternalResources(internalResources);
        Properties = CopyProperties(properties);
    }

    public long Uid { get; }

    public string UidText => ResourceUid.IdToText(Uid);

    public string Type { get; }

    public string Path { get; }

    public IReadOnlyList<ResourceFileExternalReference> ExternalReferences { get; }

    public IReadOnlyList<SerializedResourceEntry> InternalResources { get; }

    public IReadOnlyDictionary<string, SerializedPropertyValue> Properties { get; }

    private static IReadOnlyList<ResourceFileExternalReference> CopyExternalReferences(
        IEnumerable<ResourceFileExternalReference>? references)
    {
        return (references ?? Array.Empty<ResourceFileExternalReference>())
            .OrderBy(reference => reference.Id)
            .ToArray();
    }

    private static IReadOnlyList<SerializedResourceEntry> CopyInternalResources(
        IEnumerable<SerializedResourceEntry>? resources)
    {
        return (resources ?? Array.Empty<SerializedResourceEntry>())
            .OrderBy(resource => resource.Id)
            .ToArray();
    }

    internal static IReadOnlyDictionary<string, SerializedPropertyValue> CopyProperties(
        IReadOnlyDictionary<string, SerializedPropertyValue>? properties)
    {
        var copy = new Dictionary<string, SerializedPropertyValue>(StringComparer.Ordinal);
        if (properties is not null)
        {
            foreach (var (key, value) in properties)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key);
                copy.Add(key, value ?? throw new ArgumentNullException(nameof(properties)));
            }
        }

        return new ReadOnlyDictionary<string, SerializedPropertyValue>(copy);
    }
}

internal sealed class SerializedResourceEntry
{
    public SerializedResourceEntry(
        int id,
        string type,
        IReadOnlyDictionary<string, SerializedPropertyValue>? properties = null)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Serialized internal resource id must be positive.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        Id = id;
        Type = type;
        Properties = SerializedResourceDocument.CopyProperties(properties);
    }

    public int Id { get; }

    public string Type { get; }

    public IReadOnlyDictionary<string, SerializedPropertyValue> Properties { get; }
}
