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
using System.Text.Json;

namespace Electron2D.ManagedDebugging;

internal static class ManagedDebugAdapterSelection
{
    public static ManagedDebugAdapterInfo LoadFromRepository(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var manifestPath = Path.Combine(
            Path.GetFullPath(repositoryRoot),
            "data",
            "debugging",
            "dotnet-debug-adapter-selection.json");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;
        var selected = root.GetProperty("selectedAdapter");
        var dap = selected.GetProperty("dap");
        var capabilities = root.GetProperty("capabilityMatrix");
        var arguments = dap.GetProperty("arguments")
            .EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty)
            .Where(value => value.Length > 0)
            .ToArray();

        return new ManagedDebugAdapterInfo(
            selected.GetProperty("id").GetString() ?? throw new InvalidDataException("Selected debug adapter id is missing."),
            selected.GetProperty("releaseTag").GetString() ?? throw new InvalidDataException("Selected debug adapter release tag is missing."),
            dap.GetProperty("boundary").GetString() ?? throw new InvalidDataException("Selected debug adapter DAP boundary is missing."),
            arguments,
            capabilities.GetProperty("restartStrategy").GetString() ?? "editor-managed-disconnect-and-relaunch");
    }
}
