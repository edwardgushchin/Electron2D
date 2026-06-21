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

internal sealed class ResourceImportManifest
{
    public const string FormatName = "Electron2D.ImportCache";
    public const int CurrentVersion = 1;

    public ResourceImportManifest(IEnumerable<ResourceImportManifestEntry>? entries = null)
    {
        Entries = (entries ?? Array.Empty<ResourceImportManifestEntry>())
            .OrderBy(entry => entry.SourcePath, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<ResourceImportManifestEntry> Entries { get; }
}

internal sealed class ResourceImportManifestEntry
{
    public ResourceImportManifestEntry(
        string sourcePath,
        long uid,
        string type,
        string importer,
        string sourceHash,
        IEnumerable<string> cacheFiles,
        IEnumerable<ResourceImportManifestDependency>? dependencies = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceHash);

        if (uid <= 0 || uid == ResourceUid.InvalidId)
        {
            throw new ArgumentException("Imported resource UID must be positive.", nameof(uid));
        }

        SourcePath = sourcePath;
        Uid = uid;
        Type = type;
        Importer = importer;
        SourceHash = sourceHash;
        CacheFiles = CopyStringList(cacheFiles, nameof(cacheFiles));
        Dependencies = (dependencies ?? Array.Empty<ResourceImportManifestDependency>())
            .OrderBy(dependency => dependency.Path, StringComparer.Ordinal)
            .ToArray();
    }

    public string SourcePath { get; }

    public long Uid { get; }

    public string UidText => ResourceUid.IdToText(Uid);

    public string Type { get; }

    public string Importer { get; }

    public string SourceHash { get; }

    public IReadOnlyList<string> CacheFiles { get; }

    public IReadOnlyList<ResourceImportManifestDependency> Dependencies { get; }

    private static IReadOnlyList<string> CopyStringList(IEnumerable<string> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);

        var copy = new List<string>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Cache file path must be non-empty.", parameterName);
            }

            copy.Add(value);
        }

        return new ReadOnlyCollection<string>(copy.OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }
}

internal sealed class ResourceImportManifestDependency
{
    public ResourceImportManifestDependency(string path, string contentHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        Path = path;
        ContentHash = contentHash;
    }

    public string Path { get; }

    public string ContentHash { get; }
}
