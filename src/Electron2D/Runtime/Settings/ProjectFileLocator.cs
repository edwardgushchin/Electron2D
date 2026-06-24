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

internal static class ProjectFileLocator
{
    public const string ProjectExtension = ".e2d";
    public const string LegacyProjectFileName = "project.e2d.json";

    public static bool IsProjectFilePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return string.Equals(Path.GetExtension(path), ProjectExtension, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileName(path), LegacyProjectFileName, StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveProjectFilePath(string projectRoot)
    {
        if (TryResolveProjectFilePath(projectRoot, out var projectFilePath))
        {
            return projectFilePath;
        }

        throw new FileNotFoundException($"Project file was not found in '{Path.GetFullPath(projectRoot)}'.");
    }

    public static bool TryResolveProjectFilePath(string projectRoot, out string projectFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        var fullRoot = Path.GetFullPath(projectRoot);
        if (!Directory.Exists(fullRoot))
        {
            projectFilePath = string.Empty;
            return false;
        }

        var projectFiles = Directory.EnumerateFiles(fullRoot, "*" + ProjectExtension, SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var namedProjectFileName = Path.GetFileName(fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + ProjectExtension;
        var namedProjectFile = projectFiles.FirstOrDefault(path =>
            string.Equals(Path.GetFileName(path), namedProjectFileName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(namedProjectFile))
        {
            projectFilePath = namedProjectFile;
            return true;
        }

        if (projectFiles.Length > 0)
        {
            projectFilePath = projectFiles[0];
            return true;
        }

        var legacyProjectFile = Path.Combine(fullRoot, LegacyProjectFileName);
        if (File.Exists(legacyProjectFile))
        {
            projectFilePath = legacyProjectFile;
            return true;
        }

        projectFilePath = string.Empty;
        return false;
    }
}
