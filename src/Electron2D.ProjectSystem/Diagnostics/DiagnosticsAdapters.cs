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
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal static class DiagnosticStreamEventJsonSerializer
{
    public static JsonObject WriteEvent(
        string eventName,
        string producer,
        DateTimeOffset timestampUtc,
        IEnumerable<StructuredDiagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(producer);
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new JsonObject
        {
            ["schemaVersion"] = 1,
            ["event"] = eventName,
            ["producer"] = producer,
            ["timestampUtc"] = timestampUtc.ToUniversalTime().ToString("O"),
            ["diagnostics"] = DiagnosticJsonSerializer.ToJsonArray(diagnostics)
        };
    }
}

internal static class DiagnosticSarifSerializer
{
    public static JsonObject WriteRun(
        string toolName,
        string informationUri,
        IEnumerable<StructuredDiagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(informationUri);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var diagnosticItems = diagnostics.ToArray();
        var rules = new JsonArray();
        foreach (var definition in diagnosticItems
            .Select(diagnostic => DiagnosticCodeRegistry.Get(diagnostic.Code))
            .DistinctBy(definition => definition.Code)
            .OrderBy(definition => definition.Code, StringComparer.Ordinal))
        {
            rules.Add(new JsonObject
            {
                ["id"] = definition.Code,
                ["name"] = definition.Code,
                ["shortDescription"] = new JsonObject
                {
                    ["text"] = definition.Title
                },
                ["helpUri"] = definition.DocumentationUri
            });
        }

        var results = new JsonArray();
        foreach (var diagnostic in diagnosticItems)
        {
            results.Add(WriteResult(diagnostic));
        }

        return new JsonObject
        {
            ["$schema"] = "https://json.schemastore.org/sarif-2.1.0.json",
            ["version"] = "2.1.0",
            ["runs"] = new JsonArray
            {
                new JsonObject
                {
                    ["tool"] = new JsonObject
                    {
                        ["driver"] = new JsonObject
                        {
                            ["name"] = toolName,
                            ["informationUri"] = informationUri,
                            ["rules"] = rules
                        }
                    },
                    ["results"] = results
                }
            }
        };
    }

    private static JsonObject WriteResult(StructuredDiagnostic diagnostic)
    {
        var result = new JsonObject
        {
            ["ruleId"] = diagnostic.Code,
            ["level"] = ToSarifLevel(diagnostic.Severity),
            ["message"] = new JsonObject
            {
                ["text"] = diagnostic.Message
            },
            ["properties"] = new JsonObject
            {
                ["electron2dDiagnostic"] = Clone(DiagnosticJsonSerializer.ToJson(diagnostic)),
                ["electron2dSuggestedFixes"] = Clone(DiagnosticJsonSerializer.ToJson(diagnostic)["suggestedFixes"]!)
            }
        };

        if (diagnostic.Location?.File is not null)
        {
            result["locations"] = new JsonArray
            {
                WriteLocation(diagnostic.Location)
            };
        }

        return result;
    }

    private static JsonObject WriteLocation(DiagnosticLocation location)
    {
        var region = new JsonObject();
        if (location.Line is not null)
        {
            region["startLine"] = location.Line;
        }

        if (location.Column is not null)
        {
            region["startColumn"] = location.Column;
        }

        return new JsonObject
        {
            ["physicalLocation"] = new JsonObject
            {
                ["artifactLocation"] = new JsonObject
                {
                    ["uri"] = location.File
                },
                ["region"] = region
            }
        };
    }

    private static string ToSarifLevel(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "note",
            DiagnosticSeverity.Hint => "note",
            _ => "note"
        };
    }

    private static JsonNode Clone(JsonNode node)
    {
        return JsonNode.Parse(node.ToJsonString()) ?? throw new InvalidOperationException("Diagnostic JSON node could not be cloned.");
    }
}
