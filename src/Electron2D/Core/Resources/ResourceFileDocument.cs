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

internal sealed class ResourceFileDocument
{
    public const string FormatName = "Electron2D.ResourceFile";
    public const int CurrentVersion = 1;

    public ResourceFileDocument(
        long uid,
        string type,
        string path,
        IEnumerable<ResourceFileExternalReference>? externalReferences = null,
        IEnumerable<ResourceFileInternalResource>? internalResources = null,
        IReadOnlyDictionary<string, Variant>? properties = null)
    {
        if (uid <= 0 || uid == ResourceUid.InvalidId)
        {
            throw new ArgumentException("Resource file UID must be a positive valid value.", nameof(uid));
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

    public IReadOnlyList<ResourceFileInternalResource> InternalResources { get; }

    public IReadOnlyDictionary<string, Variant> Properties { get; }

    private static IReadOnlyList<ResourceFileExternalReference> CopyExternalReferences(
        IEnumerable<ResourceFileExternalReference>? references)
    {
        return references?.ToArray() ?? Array.Empty<ResourceFileExternalReference>();
    }

    private static IReadOnlyList<ResourceFileInternalResource> CopyInternalResources(
        IEnumerable<ResourceFileInternalResource>? resources)
    {
        return resources?.ToArray() ?? Array.Empty<ResourceFileInternalResource>();
    }

    private static IReadOnlyDictionary<string, Variant> CopyProperties(IReadOnlyDictionary<string, Variant>? properties)
    {
        var copy = new Dictionary<string, Variant>(StringComparer.Ordinal);
        if (properties is not null)
        {
            foreach (var (key, value) in properties)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key);
                copy.Add(key, value);
            }
        }

        return new ReadOnlyDictionary<string, Variant>(copy);
    }
}

internal sealed class ResourceFileExternalReference
{
    public ResourceFileExternalReference(int id, long uid, string path, string type)
    {
        if (id <= 0)
        {
            throw new ArgumentException("External reference id must be positive.", nameof(id));
        }

        if (uid <= 0 || uid == ResourceUid.InvalidId)
        {
            throw new ArgumentException("External reference UID must be positive.", nameof(uid));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        Id = id;
        Uid = uid;
        Path = path;
        Type = type;
    }

    public int Id { get; }

    public long Uid { get; }

    public string UidText => ResourceUid.IdToText(Uid);

    public string Path { get; }

    public string Type { get; }
}

internal sealed class ResourceFileInternalResource
{
    public ResourceFileInternalResource(int id, string type, IReadOnlyDictionary<string, Variant>? properties = null)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Internal resource id must be positive.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        Id = id;
        Type = type;
        Properties = CopyProperties(properties);
    }

    public int Id { get; }

    public string Type { get; }

    public IReadOnlyDictionary<string, Variant> Properties { get; }

    private static IReadOnlyDictionary<string, Variant> CopyProperties(IReadOnlyDictionary<string, Variant>? properties)
    {
        var copy = new Dictionary<string, Variant>(StringComparer.Ordinal);
        if (properties is not null)
        {
            foreach (var (key, value) in properties)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key);
                copy.Add(key, value);
            }
        }

        return new ReadOnlyDictionary<string, Variant>(copy);
    }
}
