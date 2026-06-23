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
using Microsoft.CodeAnalysis;

namespace Electron2D.CSharpLanguageServices;

internal static class CSharpLanguageServiceReferenceResolver
{
    private static readonly string[] TrustedPlatformAssemblies =
    [
        "System.Private.CoreLib.dll",
        "System.Runtime.dll",
        "System.Console.dll",
        "System.Collections.dll",
        "System.Linq.dll",
        "System.Private.Uri.dll",
        "netstandard.dll"
    ];

    public static IReadOnlyList<string> CreateDefaultReferencePaths(IEnumerable<string> additionalReferencePaths)
    {
        ArgumentNullException.ThrowIfNull(additionalReferencePaths);

        var result = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var trustedPlatformAssemblies = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator) ?? [];
        foreach (var path in trustedPlatformAssemblies)
        {
            if (TrustedPlatformAssemblies.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
            {
                result.Add(path);
            }
        }

        foreach (var path in additionalReferencePaths)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                result.Add(Path.GetFullPath(path));
            }
        }

        return result.ToArray();
    }

    public static IReadOnlyList<MetadataReference> CreateReferences(IEnumerable<string> referencePaths)
    {
        ArgumentNullException.ThrowIfNull(referencePaths);
        return referencePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}
