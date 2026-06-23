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
using System.Text.Json.Nodes;

namespace Electron2D.ManagedDebugging;

internal sealed class BreakpointStore
{
    private readonly string storePath;

    public BreakpointStore(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        storePath = Path.Combine(Path.GetFullPath(projectRoot), ".electron2d", "user", "breakpoints.e2debug");
    }

    public string StorePath => storePath;

    public void Save(IReadOnlyList<ManagedBreakpoint> breakpoints)
    {
        ArgumentNullException.ThrowIfNull(breakpoints);

        Directory.CreateDirectory(Path.GetDirectoryName(storePath)!);
        var array = new JsonArray();
        foreach (var breakpoint in breakpoints)
        {
            array.Add(new JsonObject
            {
                ["breakpointId"] = breakpoint.BreakpointId,
                ["documentId"] = breakpoint.DocumentId,
                ["path"] = breakpoint.SourceAnchor.Path,
                ["line"] = breakpoint.SourceAnchor.Line,
                ["column"] = breakpoint.SourceAnchor.Column,
                ["enabled"] = breakpoint.Enabled,
                ["verified"] = breakpoint.Verified,
                ["resolvedLine"] = breakpoint.ResolvedLine,
                ["resolvedColumn"] = breakpoint.ResolvedColumn,
                ["lastBoundSnapshotId"] = breakpoint.LastBoundSnapshotId,
                ["adapterMessage"] = breakpoint.AdapterMessage
            });
        }

        var root = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["kind"] = "Electron2D.ManagedBreakpoints",
            ["breakpoints"] = array
        };
        File.WriteAllText(
            storePath,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");
    }

    public IReadOnlyList<ManagedBreakpoint> Load()
    {
        if (!File.Exists(storePath))
        {
            return [];
        }

        using var document = JsonDocument.Parse(File.ReadAllText(storePath));
        return document.RootElement.GetProperty("breakpoints")
            .EnumerateArray()
            .Select(ReadBreakpoint)
            .ToArray();
    }

    public ManagedBreakpoint RenameDocument(ManagedBreakpoint breakpoint, string newPath)
    {
        ArgumentNullException.ThrowIfNull(breakpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPath);

        return breakpoint with
        {
            SourceAnchor = breakpoint.SourceAnchor with { Path = newPath }
        };
    }

    public ManagedBreakpoint Rebase(ManagedBreakpoint breakpoint, int lineDelta, bool ambiguous)
    {
        ArgumentNullException.ThrowIfNull(breakpoint);

        var anchor = breakpoint.SourceAnchor with
        {
            Line = Math.Max(1, breakpoint.SourceAnchor.Line + lineDelta)
        };
        return breakpoint with
        {
            SourceAnchor = anchor,
            Verified = !ambiguous && breakpoint.Verified,
            AdapterMessage = ambiguous ? "source anchor rebase is ambiguous" : breakpoint.AdapterMessage
        };
    }

    private static ManagedBreakpoint ReadBreakpoint(JsonElement element)
    {
        return new ManagedBreakpoint(
            element.GetProperty("breakpointId").GetString() ?? string.Empty,
            element.GetProperty("documentId").GetString() ?? string.Empty,
            new SourceAnchor(
                element.GetProperty("path").GetString() ?? string.Empty,
                element.GetProperty("line").GetInt32(),
                element.GetProperty("column").GetInt32()),
            element.GetProperty("enabled").GetBoolean(),
            element.GetProperty("verified").GetBoolean(),
            ReadNullableInt(element, "resolvedLine"),
            ReadNullableInt(element, "resolvedColumn"),
            element.TryGetProperty("lastBoundSnapshotId", out var snapshotId) ? snapshotId.GetString() : null,
            element.GetProperty("adapterMessage").GetString() ?? string.Empty);
    }

    private static int? ReadNullableInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetInt32()
            : null;
    }
}
