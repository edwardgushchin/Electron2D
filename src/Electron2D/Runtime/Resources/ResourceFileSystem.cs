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
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Electron2D;

internal static class ResourceFileSystem
{
    private const string ResourcePathPrefix = "res://";

    private static readonly AsyncLocal<IResourceProvider?> CurrentProvider = new();

    internal static IDisposable MountProjectRoot(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        return Mount(new ProjectRootResourceProvider(projectRoot));
    }

    internal static IDisposable MountPacks(string outputRoot, string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        return Mount(new PackResourceProvider(outputRoot, manifestPath));
    }

    internal static byte[] ReadAllBytes(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!IsResourcePath(path))
        {
            return File.ReadAllBytes(path);
        }

        var resourcePath = NormalizeResourcePath(path);
        var provider = CurrentProvider.Value ?? throw new FileNotFoundException(
            $"Resource path '{path}' cannot be read because no resource source is mounted.",
            path);
        return provider.ReadAllBytes(resourcePath) ??
            throw new FileNotFoundException($"Resource path '{path}' was not found.", path);
    }

    internal static string ReadAllText(string path)
    {
        return Encoding.UTF8.GetString(ReadAllBytes(path));
    }

    internal static bool IsResourcePath(string path)
    {
        return path.StartsWith(ResourcePathPrefix, StringComparison.Ordinal);
    }

    private static IDisposable Mount(IResourceProvider provider)
    {
        var previous = CurrentProvider.Value;
        CurrentProvider.Value = provider;
        return new ResourceScope(previous, provider);
    }

    private static string NormalizeResourcePath(string path)
    {
        if (!IsResourcePath(path))
        {
            throw new ArgumentException("Resource paths must start with res://.", nameof(path));
        }

        return NormalizeRelativePath(path[ResourcePathPrefix.Length..]);
    }

    private static string NormalizeRelativePath(string path)
    {
        var segments = path
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            throw new ArgumentException("Resource path must not be empty.", nameof(path));
        }

        if (segments.Any(segment => segment is "." or ".." || segment.Contains(':', StringComparison.Ordinal)))
        {
            throw new ArgumentException($"Resource path '{path}' must stay inside the mounted resource source.", nameof(path));
        }

        return string.Join('/', segments);
    }

    private interface IResourceProvider
    {
        byte[]? ReadAllBytes(string resourcePath);
    }

    private sealed class ProjectRootResourceProvider : IResourceProvider
    {
        private readonly string projectRoot;
        private readonly string projectRootWithSeparator;

        public ProjectRootResourceProvider(string projectRoot)
        {
            this.projectRoot = Path.GetFullPath(projectRoot);
            projectRootWithSeparator = this.projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
        }

        public byte[]? ReadAllBytes(string resourcePath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, resourcePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(projectRootWithSeparator, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            {
                return null;
            }

            return File.ReadAllBytes(fullPath);
        }
    }

    private sealed class PackResourceProvider : IResourceProvider, IDisposable
    {
        private readonly List<ZipArchive> archives = [];
        private readonly Dictionary<string, PackEntry> entries = new(StringComparer.Ordinal);
        private bool disposed;

        public PackResourceProvider(string outputRoot, string manifestPath)
        {
            var fullOutputRoot = Path.GetFullPath(outputRoot);
            using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (!manifest.RootElement.TryGetProperty("packs", out var packs) || packs.ValueKind != JsonValueKind.Array)
            {
                throw new FormatException("Resource pack manifest must contain a `packs` array.");
            }

            foreach (var pack in packs.EnumerateArray())
            {
                if (!pack.TryGetProperty("path", out var pathElement) || pathElement.ValueKind != JsonValueKind.String)
                {
                    throw new FormatException("Resource pack manifest item must contain a string `path`.");
                }

                var portablePath = pathElement.GetString() ?? string.Empty;
                var packPath = Path.GetFullPath(Path.Combine(fullOutputRoot, portablePath.Replace('/', Path.DirectorySeparatorChar)));
                if (!IsSameOrChildOf(fullOutputRoot, packPath))
                {
                    throw new FormatException($"Resource pack '{portablePath}' must stay inside the exported output.");
                }

                var archive = ZipFile.OpenRead(packPath);
                archives.Add(archive);
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    entries[NormalizeRelativePath(entry.FullName)] = new PackEntry(archive, entry.FullName);
                }
            }
        }

        public byte[]? ReadAllBytes(string resourcePath)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!entries.TryGetValue(resourcePath, out var packEntry))
            {
                return null;
            }

            var entry = packEntry.Archive.GetEntry(packEntry.EntryName);
            if (entry is null)
            {
                return null;
            }

            using var input = entry.Open();
            using var output = new MemoryStream();
            input.CopyTo(output);
            return output.ToArray();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            foreach (var archive in archives)
            {
                archive.Dispose();
            }
        }

        private static bool IsSameOrChildOf(string root, string candidate)
        {
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            var normalizedCandidate = Path.GetFullPath(candidate);
            return normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }

        private readonly record struct PackEntry(ZipArchive Archive, string EntryName);
    }

    private sealed class ResourceScope : IDisposable
    {
        private readonly IResourceProvider? previousProvider;
        private readonly IResourceProvider currentProvider;
        private bool disposed;

        public ResourceScope(IResourceProvider? previousProvider, IResourceProvider currentProvider)
        {
            this.previousProvider = previousProvider;
            this.currentProvider = currentProvider;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            CurrentProvider.Value = previousProvider;
            if (currentProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
