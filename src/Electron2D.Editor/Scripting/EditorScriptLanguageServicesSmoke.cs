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
using Electron2D.CSharpLanguageServices;

namespace Electron2D.Editor.Scripting;

internal static class EditorScriptLanguageServicesSmoke
{
    public static EditorScriptLanguageServicesSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var fullWorkRoot = Path.GetFullPath(workRoot);
        Directory.CreateDirectory(fullWorkRoot);

        var marked = CreateMarkedText();
        var document = new CSharpLanguageServiceDocument(
            "doc-script-language-hero",
            "Scripts/HeroController.cs",
            marked.Text,
            Revision: 7,
            SemanticVersion: 3);
        var identity = new CSharpLanguageServiceRequestIdentity(
            "script-language-smoke-project",
            document.DocumentId,
            document.Revision,
            document.SemanticVersion,
            "lang-services-hash");
        var service = new CSharpLanguageService();
        var references = CSharpLanguageServiceReferenceResolver.CreateDefaultReferencePaths([typeof(Node).Assembly.Location]);
        var request = new CSharpLanguageServiceRequest(
            identity.ProjectId,
            document,
            identity,
            references,
            marked.Positions["completion"],
            marked.Positions["signature"],
            marked.Positions["hover"],
            marked.Positions["definition"],
            "MoveHero",
            responseDocumentRevision: 6,
            previousRequestCancelled: true,
            diagnosticsDebounceMs: 250,
            reloadTrigger: "PackageReference");
        var result = service.Analyze(request);

        var statePath = Path.Combine(fullWorkRoot, "script-language-services.state.json");
        File.WriteAllText(
            statePath,
            WriteState(result).ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");

        var visual = EditorScriptLanguageServicesVisualHarness.WriteArtifacts(
            result,
            Path.Combine(fullWorkRoot, "visual"));

        return new EditorScriptLanguageServicesSmokeResult(
            result,
            statePath,
            visual.ScreenshotPath,
            visual.AnalysisPath,
            visual.TextOverflowCount,
            visual.ClickableControlCount,
            visual.ForbiddenUiMatchCount,
            visual.ScreenshotReviewed);
    }

    private static JsonObject WriteState(CSharpLanguageServiceResult result)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.ScriptLanguageServicesState",
            ["assemblyBoundary"] = typeof(CSharpLanguageService).Assembly.GetName().Name,
            ["projectId"] = result.Identity.ProjectId,
            ["documentId"] = result.Identity.DocumentId,
            ["documentRevision"] = result.Identity.DocumentRevision,
            ["semanticVersion"] = result.Identity.SemanticVersion,
            ["configurationHash"] = result.Identity.ConfigurationHash,
            ["completionSelectedItem"] = result.Completion.SelectedItem,
            ["signatureHelpDisplay"] = result.SignatureHelp.Display,
            ["hoverSymbol"] = result.Hover.SymbolDisplay,
            ["liveDiagnosticCode"] = result.LiveDiagnostic.Code,
            ["definitionTarget"] = result.Definition.ToString(),
            ["referencesCount"] = result.References.References.Count,
            ["renameEditCount"] = result.Rename.Edits.Count,
            ["formattingChanged"] = result.Formatting.Changed,
            ["codeActionTitle"] = result.CodeAction.Title,
            ["staleResponseDiscarded"] = result.StaleResponseDiscarded
        };
    }

    private static EditorScriptLanguageServicesMarkedText CreateMarkedText()
    {
        const string text = """
using Electron2D;

namespace Smoke.Scripts;

public sealed class HeroController : Node
{
    /// <summary>
    /// Moves hero with delta.
    /// </summary>
    public void [[hover]]DocumentedMove(float delta)
    {
        var velocity = new Vector2(12, 24[[signature]]);
        var sprite = new Sprite2D();
        [[definition]]DocumentedMove(delta);
        MissingSymbol();
        var completionProbe = [[completion]]delta;
    }

    public void UseList()
    {
        List<int> scores = new();
    }
}
""";

        var positions = new Dictionary<string, int>(StringComparer.Ordinal);
        var current = text;
        while (true)
        {
            var start = current.IndexOf("[[", StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = current.IndexOf("]]", start, StringComparison.Ordinal);
            if (end < 0)
            {
                throw new FormatException("Language-services smoke marker is missing its closing token.");
            }

            var name = current[(start + 2)..end];
            positions.Add(name, start);
            current = current.Remove(start, end - start + 2);
        }

        return new EditorScriptLanguageServicesMarkedText(current.ReplaceLineEndings("\n"), positions);
    }
}

internal sealed record EditorScriptLanguageServicesMarkedText(
    string Text,
    IReadOnlyDictionary<string, int> Positions);
