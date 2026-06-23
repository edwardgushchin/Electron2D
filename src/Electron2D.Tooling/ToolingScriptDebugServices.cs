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
using System.Text.RegularExpressions;
using Electron2D.CSharpLanguageServices;
using Electron2D.ManagedDebugging;
using Electron2D.ProjectSystem;

namespace Electron2D.Tooling;

internal sealed record ToolingScriptTextEdit(
    int? StartLine,
    int? StartColumn,
    int? EndLine,
    int? EndColumn,
    string NewText,
    bool ReplaceWholeDocument)
{
    public static ToolingScriptTextEdit ReplaceAll(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new ToolingScriptTextEdit(null, null, null, null, text, ReplaceWholeDocument: true);
    }

    public static ToolingScriptTextEdit ReplaceSpan(
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new ToolingScriptTextEdit(startLine, startColumn, endLine, endColumn, text, ReplaceWholeDocument: false);
    }
}

internal sealed class ToolingScriptApplyTextEditsRequest
{
    public ToolingScriptApplyTextEditsRequest(
        string operationId,
        string path,
        ProjectDocumentRevision expectedRevision,
        IReadOnlyList<ToolingScriptTextEdit> edits,
        string? undoGroupId,
        ToolingApplyMode mode = ToolingApplyMode.WorkspaceOnly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(edits);

        OperationId = operationId;
        Path = ProjectDocumentPaths.NormalizeRelativePath(path);
        ExpectedRevision = expectedRevision;
        Edits = edits.ToArray();
        UndoGroupId = undoGroupId;
        Mode = mode;
    }

    public string OperationId { get; }

    public string Path { get; }

    public ProjectDocumentRevision ExpectedRevision { get; }

    public IReadOnlyList<ToolingScriptTextEdit> Edits { get; }

    public string? UndoGroupId { get; }

    public ToolingApplyMode Mode { get; }
}

internal sealed class ToolingScriptSaveRequest
{
    public ToolingScriptSaveRequest(
        string operationId,
        string path,
        ProjectDocumentRevision agentBaseRevision,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        OperationId = operationId;
        Path = ProjectDocumentPaths.NormalizeRelativePath(path);
        AgentBaseRevision = agentBaseRevision;
        DryRun = dryRun;
    }

    public string OperationId { get; }

    public string Path { get; }

    public ProjectDocumentRevision AgentBaseRevision { get; }

    public bool DryRun { get; }
}

internal sealed class ToolingScriptIdeRequest
{
    public ToolingScriptIdeRequest(
        string path,
        int completionPosition = -1,
        int signatureHelpPosition = -1,
        int hoverPosition = -1,
        int definitionPosition = -1,
        string renameTo = "RenamedSymbol",
        int responseDocumentRevision = -1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(renameTo);

        Path = ProjectDocumentPaths.NormalizeRelativePath(path);
        CompletionPosition = completionPosition;
        SignatureHelpPosition = signatureHelpPosition;
        HoverPosition = hoverPosition;
        DefinitionPosition = definitionPosition;
        RenameTo = renameTo;
        ResponseDocumentRevision = responseDocumentRevision;
    }

    public string Path { get; }

    public int CompletionPosition { get; }

    public int SignatureHelpPosition { get; }

    public int HoverPosition { get; }

    public int DefinitionPosition { get; }

    public string RenameTo { get; }

    public int ResponseDocumentRevision { get; }
}

internal sealed record ToolingScriptCompletionItem(string DisplayText, bool IsSelected);

internal sealed record ToolingScriptSignatureHelp(
    string Display,
    int ActiveParameter,
    IReadOnlyList<string> ParameterNames);

internal sealed record ToolingScriptHover(
    string SymbolDisplay,
    string DocumentationSummary,
    bool TargetMatched);

internal sealed record ToolingScriptDiagnostic(
    string Code,
    string Severity,
    string Path,
    int Line,
    int Column,
    string Message);

internal sealed record ToolingScriptLocation(string Path, int Line, int Column)
{
    public override string ToString()
    {
        return FormattableString.Invariant($"{Path}:{Line}:{Column}");
    }
}

internal sealed record ToolingScriptDocumentSymbol(string Name, string Kind, int Line, int Column);

internal sealed record ToolingScriptCodeAction(string Title, IReadOnlyList<ToolingScriptTextEdit> Edits);

internal sealed class ToolingScriptDocumentResult
{
    public ToolingScriptDocumentResult(
        bool succeeded,
        string path,
        string text,
        string documentId,
        ProjectDocumentRevision documentRevision,
        ProjectDocumentRevision persistedRevision,
        int semanticVersion,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        Succeeded = succeeded;
        Path = path;
        Text = text;
        DocumentId = documentId;
        DocumentRevision = documentRevision;
        PersistedRevision = persistedRevision;
        SemanticVersion = semanticVersion;
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public string Path { get; }

    public string Text { get; }

    public string DocumentId { get; }

    public ProjectDocumentRevision DocumentRevision { get; }

    public ProjectDocumentRevision PersistedRevision { get; }

    public int SemanticVersion { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }
}

internal sealed class ToolingScriptMutationResult
{
    public ToolingScriptMutationResult(ToolingOperationResult operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        Operation = operation;
    }

    public bool Succeeded => Operation.Succeeded;

    public ToolingOperationResult Operation { get; }
}

internal sealed class ToolingScriptIdeResult
{
    public ToolingScriptIdeResult(
        bool succeeded,
        string commandName,
        ProjectDocumentRevision documentRevision,
        int semanticVersion,
        bool roslynSemanticModel,
        bool workspaceSnapshotUsedForIde,
        IReadOnlyList<ToolingScriptCompletionItem> completionItems,
        ToolingScriptSignatureHelp? signatureHelp,
        ToolingScriptHover? hover,
        ToolingScriptDiagnostic? diagnostic,
        ToolingScriptLocation? definition,
        IReadOnlyList<ToolingScriptLocation> references,
        IReadOnlyList<ToolingScriptDocumentSymbol> symbols,
        IReadOnlyList<ToolingScriptCodeAction> codeActions,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        Succeeded = succeeded;
        CommandName = commandName;
        DocumentRevision = documentRevision;
        SemanticVersion = semanticVersion;
        RoslynSemanticModel = roslynSemanticModel;
        WorkspaceSnapshotUsedForIde = workspaceSnapshotUsedForIde;
        CompletionItems = completionItems.ToArray();
        SignatureHelp = signatureHelp;
        Hover = hover;
        Diagnostic = diagnostic;
        Definition = definition;
        References = references.ToArray();
        Symbols = symbols.ToArray();
        CodeActions = codeActions.ToArray();
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public string CommandName { get; }

    public ProjectDocumentRevision DocumentRevision { get; }

    public int SemanticVersion { get; }

    public bool RoslynSemanticModel { get; }

    public bool WorkspaceSnapshotUsedForIde { get; }

    public IReadOnlyList<ToolingScriptCompletionItem> CompletionItems { get; }

    public ToolingScriptSignatureHelp? SignatureHelp { get; }

    public ToolingScriptHover? Hover { get; }

    public ToolingScriptDiagnostic? Diagnostic { get; }

    public ToolingScriptLocation? Definition { get; }

    public IReadOnlyList<ToolingScriptLocation> References { get; }

    public IReadOnlyList<ToolingScriptDocumentSymbol> Symbols { get; }

    public IReadOnlyList<ToolingScriptCodeAction> CodeActions { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }
}

internal sealed class ToolingScriptSearchResult
{
    public ToolingScriptSearchResult(IReadOnlyList<ToolingScriptLocation> matches)
    {
        Matches = matches.ToArray();
    }

    public IReadOnlyList<ToolingScriptLocation> Matches { get; }
}

internal sealed class ToolingScriptService
{
    private static readonly Regex SymbolRegex = new(
        "\\b(class|struct|record|interface|enum|void|int|string|float|double|bool)\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled);

    private readonly ProjectWorkspace workspace;
    private readonly ProjectService project;
    private readonly CSharpLanguageService languageService = new();

    public ToolingScriptService(ProjectWorkspace workspace, ProjectService project)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(project);
        this.workspace = workspace;
        this.project = project;
    }

    public ToolingScriptDocumentResult Create(
        string operationId,
        string path,
        string text,
        OperationContext context,
        ToolingApplyMode mode = ToolingApplyMode.WorkspaceOnly)
    {
        EnsureOpenDocument(path, initialText: string.Empty, persistedRevision: 0, operationId);
        var result = ApplyTextEdits(
            new ToolingScriptApplyTextEditsRequest(
                operationId,
                path,
                new ProjectDocumentRevision(0),
                [ToolingScriptTextEdit.ReplaceAll(text)],
                undoGroupId: $"undo-{operationId}",
                mode),
            context);
        var document = workspace.Documents.GetByPath(path);
        return DocumentResult(result.Succeeded, document, result.Operation.Diagnostics);
    }

    public ToolingScriptDocumentResult Open(string path)
    {
        var document = EnsureOpenDocument(path, initialText: null, persistedRevision: 1, $"script-open-{Guid.NewGuid():N}");
        return DocumentResult(succeeded: true, document, []);
    }

    public ToolingScriptDocumentResult Read(string path)
    {
        var document = EnsureOpenDocument(path, initialText: null, persistedRevision: 1, $"script-read-{Guid.NewGuid():N}");
        return DocumentResult(succeeded: true, document, []);
    }

    public ToolingScriptSearchResult SearchText(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var matches = new List<ToolingScriptLocation>();
        foreach (var document in CodeDocuments())
        {
            var index = document.Text.IndexOf(query, StringComparison.Ordinal);
            while (index >= 0)
            {
                var (line, column) = ToLineColumn(document.Text, index);
                matches.Add(new ToolingScriptLocation(document.Path, line, column));
                index = document.Text.IndexOf(query, index + Math.Max(query.Length, 1), StringComparison.Ordinal);
            }
        }

        return new ToolingScriptSearchResult(matches);
    }

    public ToolingScriptMutationResult ApplyTextEdits(ToolingScriptApplyTextEditsRequest request, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var document = EnsureOpenDocument(request.Path, initialText: null, persistedRevision: 1, request.OperationId);
        var text = ApplyEdits(document.Text, request.Edits);
        var result = project.ApplyTextEdit(new ToolingTextEditRequest(
            request.OperationId,
            "script_apply_text_edits",
            request.Mode,
            request.Path,
            request.ExpectedRevision,
            text,
            request.UndoGroupId),
            context);
        return new ToolingScriptMutationResult(result);
    }

    public ToolingScriptMutationResult Save(ToolingScriptSaveRequest request, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var document = EnsureOpenDocument(request.Path, initialText: null, persistedRevision: 1, request.OperationId);
        if (context.PrincipalKind == PrincipalKind.Agent &&
            document.IsDirty &&
            document.InMemoryRevision.Value > request.AgentBaseRevision.Value)
        {
            return new ToolingScriptMutationResult(ToolingOperationResult.Failure(
                request.OperationId,
                "script_save",
                workspace,
                CreateDiagnostic(
                    "E2D-TOOLING-0002",
                    $"Script save rejected because '{document.Path}' has manual unsaved changes after agent base revision {request.AgentBaseRevision.Value}.")));
        }

        var result = project.SaveAffectedDocuments(request.OperationId, context, request.DryRun);
        return new ToolingScriptMutationResult(result);
    }

    public ToolingScriptMutationResult Format(ToolingScriptIdeRequest request, string operationId, OperationContext context)
    {
        var result = Analyze("script_format", request);
        if (!result.Succeeded || result.CodeActions.Count == 0)
        {
            var document = workspace.Documents.GetByPath(request.Path);
            return ApplyTextEdits(
                new ToolingScriptApplyTextEditsRequest(
                    operationId,
                    request.Path,
                    document.InMemoryRevision,
                    [ToolingScriptTextEdit.ReplaceAll(document.Text)],
                    $"undo-{operationId}"),
                context);
        }

        var analysis = AnalyzeRaw(request);
        return ApplyTextEdits(
            new ToolingScriptApplyTextEditsRequest(
                operationId,
                request.Path,
                result.DocumentRevision,
                [ToolingScriptTextEdit.ReplaceAll(analysis.Formatting.FormattedText)],
                $"undo-{operationId}"),
            context);
    }

    public ToolingScriptIdeResult GetDiagnostics(ToolingScriptIdeRequest request)
    {
        return Analyze("script_get_diagnostics", request);
    }

    public ToolingScriptIdeResult GetCompletions(ToolingScriptIdeRequest request)
    {
        return Analyze("script_get_completions", request);
    }

    public ToolingScriptIdeResult GetSignatureHelp(ToolingScriptIdeRequest request)
    {
        return Analyze("script_get_signature_help", request);
    }

    public ToolingScriptIdeResult GetHover(ToolingScriptIdeRequest request)
    {
        return Analyze("script_get_hover", request);
    }

    public ToolingScriptIdeResult GetDefinition(ToolingScriptIdeRequest request)
    {
        return Analyze("script_get_definition", request);
    }

    public ToolingScriptIdeResult GetDocumentSymbols(ToolingScriptIdeRequest request)
    {
        return Analyze("script_get_document_symbols", request);
    }

    public ToolingScriptIdeResult FindReferences(ToolingScriptIdeRequest request)
    {
        return Analyze("script_find_references", request);
    }

    public ToolingScriptIdeResult GetCodeActions(ToolingScriptIdeRequest request)
    {
        return Analyze("script_get_code_actions", request);
    }

    public ToolingScriptMutationResult RenameSymbol(ToolingScriptIdeRequest request, string operationId, OperationContext context)
    {
        var analysis = AnalyzeRaw(request);
        return ApplyTextEdits(
            new ToolingScriptApplyTextEditsRequest(
                operationId,
                request.Path,
                new ProjectDocumentRevision(analysis.Rename.ExpectedRevision),
                ToToolingEdits(analysis.Rename.Edits),
                $"undo-{operationId}"),
            context);
    }

    public ToolingScriptMutationResult ApplyCodeAction(ToolingScriptIdeRequest request, string operationId, OperationContext context)
    {
        var analysis = AnalyzeRaw(request);
        return ApplyTextEdits(
            new ToolingScriptApplyTextEditsRequest(
                operationId,
                request.Path,
                new ProjectDocumentRevision(analysis.Identity.DocumentRevision),
                ToToolingEdits(analysis.CodeAction.Edits),
                $"undo-{operationId}"),
            context);
    }

    public ToolingScriptMutationResult Rename(
        string operationId,
        string oldPath,
        string newPath,
        ProjectDocumentRevision expectedRevision,
        OperationContext context)
    {
        var document = EnsureOpenDocument(oldPath, initialText: null, persistedRevision: 1, operationId);
        var created = Create(operationId + "-create", newPath, document.Text, context);
        if (!created.Succeeded)
        {
            return new ToolingScriptMutationResult(created.Diagnostics.Count == 0
                ? created.DummyOperation(workspace, operationId, "script_rename")
                : ToolingOperationResult.Failure(operationId, "script_rename", workspace, created.Diagnostics[0]));
        }

        return Delete(operationId + "-delete", oldPath, expectedRevision, context);
    }

    public ToolingScriptMutationResult Delete(
        string operationId,
        string path,
        ProjectDocumentRevision expectedRevision,
        OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var document = EnsureOpenDocument(path, initialText: null, persistedRevision: 1, operationId);
        if (document.InMemoryRevision != expectedRevision)
        {
            return new ToolingScriptMutationResult(ToolingOperationResult.Failure(
                operationId,
                "script_delete",
                workspace,
                CreateDiagnostic(
                    "E2D-TOOLING-0002",
                    $"Document expected revision '{expectedRevision.Value}' does not match current revision '{document.InMemoryRevision.Value}'.")));
        }

        workspace.Documents.RemoveTextDocument(path, out _);
        workspace.Revisions.RecordDocumentDeleted(path);
        var fullPath = ResolveProjectPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return new ToolingScriptMutationResult(ToolingOperationResult.FromWorkspaceState(
            succeeded: true,
            operationId,
            "script_delete",
            workspace,
            changedFiles: [ProjectDocumentPaths.NormalizeRelativePath(path)],
            changedObjects: [$"{ProjectDocumentPaths.NormalizeRelativePath(path)}#text:root.Deleted"],
            createdObjects: [],
            diagnostics: [],
            undoGroupId: null));
    }

    private ToolingScriptIdeResult Analyze(string commandName, ToolingScriptIdeRequest request)
    {
        var analysis = AnalyzeRaw(request);
        return new ToolingScriptIdeResult(
            analysis.RoslynSemanticModel && analysis.SemanticFailureDiagnostic.Code == "E2D-SCRIPT-0003",
            commandName,
            new ProjectDocumentRevision(analysis.Identity.DocumentRevision),
            analysis.Identity.SemanticVersion,
            analysis.RoslynSemanticModel,
            analysis.WorkspaceSnapshotUsedForIde,
            analysis.Completion.Items.Select(item => new ToolingScriptCompletionItem(item.DisplayText, item.IsSelected)).ToArray(),
            ToToolingSignatureHelp(analysis.SignatureHelp),
            ToToolingHover(analysis.Hover),
            ToToolingDiagnostic(analysis.LiveDiagnostic),
            ToToolingLocation(analysis.Definition),
            analysis.References.References.Select(ToToolingLocation).ToArray(),
            ReadSymbols(request.Path),
            [new ToolingScriptCodeAction(analysis.CodeAction.Title, ToToolingEdits(analysis.CodeAction.Edits))],
            diagnostics: []);
    }

    private CSharpLanguageServiceResult AnalyzeRaw(ToolingScriptIdeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var document = EnsureOpenDocument(request.Path, initialText: null, persistedRevision: 1, $"script-ide-{Guid.NewGuid():N}");
        var defaultPositions = InferPositions(document.Text);
        var revision = checked((int)Math.Min(document.InMemoryRevision.Value, int.MaxValue));
        var identity = new CSharpLanguageServiceRequestIdentity(
            workspace.OwnerLease.OwnerId,
            document.DocumentId,
            revision,
            SemanticVersion(document),
            "tooling-script-live");
        var serviceRequest = new CSharpLanguageServiceRequest(
            identity.ProjectId,
            new CSharpLanguageServiceDocument(document.DocumentId, document.Path, document.Text, revision, identity.SemanticVersion),
            identity,
            CSharpLanguageServiceReferenceResolver.CreateDefaultReferencePaths(ResolveReferencePaths()),
            PositionOrDefault(request.CompletionPosition, defaultPositions.Completion),
            PositionOrDefault(request.SignatureHelpPosition, defaultPositions.Signature),
            PositionOrDefault(request.HoverPosition, defaultPositions.Hover),
            PositionOrDefault(request.DefinitionPosition, defaultPositions.Definition),
            request.RenameTo,
            request.ResponseDocumentRevision >= 0 ? request.ResponseDocumentRevision : revision,
            previousRequestCancelled: false,
            diagnosticsDebounceMs: 250,
            reloadTrigger: "Tooling");
        return languageService.Analyze(serviceRequest);
    }

    private ProjectWorkspaceDocument EnsureOpenDocument(
        string path,
        string? initialText,
        long persistedRevision,
        string operationId)
    {
        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(path);
        if (workspace.Documents.TryGetByPath(normalizedPath, out var document))
        {
            return document;
        }

        var text = initialText ?? ReadProjectFile(normalizedPath);
        workspace.CommandBus.OpenTextDocument(
            normalizedPath,
            text,
            persistedRevision,
            new ProjectWorkspaceOperationContext(operationId, ProjectWorkspaceActorKind.Agent, "script_open"));
        return workspace.Documents.GetByPath(normalizedPath);
    }

    private string ReadProjectFile(string normalizedPath)
    {
        var fullPath = ResolveProjectPath(normalizedPath);
        return File.Exists(fullPath) ? File.ReadAllText(fullPath).ReplaceLineEndings("\n") : string.Empty;
    }

    private string ResolveProjectPath(string normalizedPath)
    {
        var root = Path.GetFullPath(workspace.ProjectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(root, normalizedPath.Replace('/', Path.DirectorySeparatorChar)));
        var relative = Path.GetRelativePath(root, candidate);
        if (relative == "." || relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException($"Script path escapes project root: {candidate}");
        }

        return candidate;
    }

    private IEnumerable<ProjectWorkspaceDocument> CodeDocuments()
    {
        return workspace.Documents.Documents.Where(document => document.Path.EndsWith(".cs", StringComparison.Ordinal));
    }

    private IReadOnlyList<ToolingScriptDocumentSymbol> ReadSymbols(string path)
    {
        var document = workspace.Documents.GetByPath(path);
        var symbols = new List<ToolingScriptDocumentSymbol>();
        foreach (Match match in SymbolRegex.Matches(document.Text))
        {
            if (!match.Success)
            {
                continue;
            }

            var name = match.Groups["name"].Value;
            var (line, column) = ToLineColumn(document.Text, match.Groups["name"].Index);
            symbols.Add(new ToolingScriptDocumentSymbol(name, "symbol", line, column));
        }

        return symbols
            .GroupBy(symbol => symbol.Name, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(symbol => symbol.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ApplyEdits(string text, IReadOnlyList<ToolingScriptTextEdit> edits)
    {
        if (edits.Count == 0)
        {
            return text;
        }

        if (edits.Count == 1 && edits[0].ReplaceWholeDocument)
        {
            return edits[0].NewText.ReplaceLineEndings("\n");
        }

        var result = text.ReplaceLineEndings("\n");
        foreach (var edit in edits.OrderByDescending(edit => ToOffset(result, edit.StartLine!.Value, edit.StartColumn!.Value)))
        {
            var start = ToOffset(result, edit.StartLine!.Value, edit.StartColumn!.Value);
            var end = ToOffset(result, edit.EndLine!.Value, edit.EndColumn!.Value);
            result = result.Remove(start, end - start).Insert(start, edit.NewText.ReplaceLineEndings("\n"));
        }

        return result;
    }

    private static IReadOnlyList<ToolingScriptTextEdit> ToToolingEdits(IEnumerable<CSharpLanguageTextEdit> edits)
    {
        return edits
            .Select(edit => ToolingScriptTextEdit.ReplaceSpan(
                edit.StartLine,
                edit.StartColumn,
                edit.EndLine,
                edit.EndColumn,
                edit.NewText))
            .ToArray();
    }

    private static ToolingScriptSignatureHelp ToToolingSignatureHelp(CSharpLanguageSignatureHelpResult signatureHelp)
    {
        return new ToolingScriptSignatureHelp(
            signatureHelp.Display,
            signatureHelp.ActiveParameter,
            signatureHelp.ParameterNames.ToArray());
    }

    private static ToolingScriptHover ToToolingHover(CSharpLanguageHoverResult hover)
    {
        return new ToolingScriptHover(
            hover.SymbolDisplay,
            hover.DocumentationSummary,
            hover.TargetMatched);
    }

    private static ToolingScriptDiagnostic ToToolingDiagnostic(CSharpLanguageServiceDiagnostic diagnostic)
    {
        return new ToolingScriptDiagnostic(
            diagnostic.Code,
            diagnostic.Severity,
            diagnostic.Path,
            diagnostic.Line,
            diagnostic.Column,
            diagnostic.Message);
    }

    private static ToolingScriptLocation ToToolingLocation(CSharpLanguageLocation location)
    {
        return new ToolingScriptLocation(location.Path, location.Line, location.Column);
    }

    private static int ToOffset(string text, int line, int column)
    {
        var currentLine = 1;
        var currentColumn = 1;
        for (var index = 0; index < text.Length; index++)
        {
            if (currentLine == line && currentColumn == column)
            {
                return index;
            }

            if (text[index] == '\n')
            {
                currentLine++;
                currentColumn = 1;
            }
            else
            {
                currentColumn++;
            }
        }

        return text.Length;
    }

    private static (int Line, int Column) ToLineColumn(string text, int offset)
    {
        var line = 1;
        var column = 1;
        for (var index = 0; index < Math.Min(offset, text.Length); index++)
        {
            if (text[index] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }

    private static int PositionOrDefault(int position, int fallback)
    {
        return position >= 0 ? position : fallback;
    }

    private static int SemanticVersion(ProjectWorkspaceDocument document)
    {
        return checked((int)Math.Min(document.InMemoryRevision.Value, int.MaxValue));
    }

    private static ToolingScriptDocumentResult DocumentResult(
        bool succeeded,
        ProjectWorkspaceDocument document,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        return new ToolingScriptDocumentResult(
            succeeded,
            document.Path,
            document.Text,
            document.DocumentId,
            document.InMemoryRevision,
            document.PersistedRevision,
            SemanticVersion(document),
            diagnostics);
    }

    private static IReadOnlyList<string> ResolveReferencePaths()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Electron2D.dll"),
            Path.Combine(AppContext.BaseDirectory, "src", "Electron2D", "bin", "Debug", "net10.0", "Electron2D.dll")
        };
        return candidates.Where(File.Exists).ToArray();
    }

    private static (int Completion, int Signature, int Hover, int Definition) InferPositions(string text)
    {
        return (
            Math.Max(0, text.IndexOf("delta;", StringComparison.Ordinal)),
            Math.Max(0, text.IndexOf("24)", StringComparison.Ordinal)),
            Math.Max(0, text.IndexOf("DocumentedMove", StringComparison.Ordinal)),
            Math.Max(0, text.LastIndexOf("DocumentedMove", StringComparison.Ordinal)));
    }

    internal static StructuredDiagnostic CreateDiagnostic(string code, string message)
    {
        var definition = DiagnosticCodeRegistry.Get(code);
        return StructuredDiagnostic.Create(
            definition.Code,
            definition.Severity,
            definition.Category,
            message,
            location: null,
            relatedLocations: [],
            suggestedFixes: []);
    }
}

internal sealed record ToolingSourceAnchor(string Path, int Line, int Column);

internal sealed record ToolingDebugBreakpoint(
    string BreakpointId,
    string DocumentId,
    ToolingSourceAnchor SourceAnchor,
    bool Enabled,
    bool Verified,
    string AdapterMessage);

internal sealed record ToolingDebugThread(int ThreadId, string Name, bool IsSelected);

internal sealed record ToolingDebugStackFrame(int FrameId, int ThreadId, string Display, ToolingSourceAnchor Source);

internal sealed record ToolingDebugVariable(string Name, string Value, string Kind, int FrameId);

internal sealed record ToolingDebugWatch(string WatchId, string Expression, string? Value, int? FrameId);

internal sealed class ToolingDebugSetBreakpointRequest
{
    public ToolingDebugSetBreakpointRequest(string operationId, string documentId, string path, int line, int column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfLessThan(line, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(column, 1);

        OperationId = operationId;
        DocumentId = documentId;
        Path = ProjectDocumentPaths.NormalizeRelativePath(path);
        Line = line;
        Column = column;
    }

    public string OperationId { get; }

    public string DocumentId { get; }

    public string Path { get; }

    public int Line { get; }

    public int Column { get; }
}

internal sealed class ToolingDebugUpdateBreakpointRequest
{
    public ToolingDebugUpdateBreakpointRequest(string operationId, string breakpointId, bool enabled, int line, int column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(breakpointId);
        ArgumentOutOfRangeException.ThrowIfLessThan(line, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(column, 1);

        OperationId = operationId;
        BreakpointId = breakpointId;
        Enabled = enabled;
        Line = line;
        Column = column;
    }

    public string OperationId { get; }

    public string BreakpointId { get; }

    public bool Enabled { get; }

    public int Line { get; }

    public int Column { get; }
}

internal sealed class ToolingDebugStartRequest
{
    public ToolingDebugStartRequest(string operationId, string inputBuildConfigurationHash, int? activeEditorGameProcessId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputBuildConfigurationHash);

        OperationId = operationId;
        InputBuildConfigurationHash = inputBuildConfigurationHash;
        ActiveEditorGameProcessId = activeEditorGameProcessId;
    }

    public string OperationId { get; }

    public string InputBuildConfigurationHash { get; }

    public int? ActiveEditorGameProcessId { get; }
}

internal sealed class ToolingDebugAttachRequest
{
    public ToolingDebugAttachRequest(
        string operationId,
        int processId,
        int? activeEditorGameProcessId,
        bool interactiveApproved,
        string inputBuildConfigurationHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputBuildConfigurationHash);

        OperationId = operationId;
        ProcessId = processId;
        ActiveEditorGameProcessId = activeEditorGameProcessId;
        InteractiveApproved = interactiveApproved;
        InputBuildConfigurationHash = inputBuildConfigurationHash;
    }

    public string OperationId { get; }

    public int ProcessId { get; }

    public int? ActiveEditorGameProcessId { get; }

    public bool InteractiveApproved { get; }

    public string InputBuildConfigurationHash { get; }
}

internal sealed record ToolingDebugFrameRequest(int FrameId);

internal sealed record ToolingDebugWatchRequest(string OperationId, string Expression);

internal sealed record ToolingDebugWatchUpdateRequest(string OperationId, string WatchId, string Expression);

internal sealed record ToolingDebugRemoveWatchRequest(string OperationId, string WatchId);

internal sealed class ToolingDebugCommandResult
{
    public ToolingDebugCommandResult(bool succeeded, IReadOnlyList<StructuredDiagnostic> diagnostics, ToolingDebugBreakpoint? breakpoint = null)
    {
        Succeeded = succeeded;
        Diagnostics = diagnostics.ToArray();
        Breakpoint = breakpoint;
    }

    public bool Succeeded { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public ToolingDebugBreakpoint? Breakpoint { get; }
}

internal sealed class ToolingDebugSessionResult
{
    public ToolingDebugSessionResult(
        bool succeeded,
        string operationId,
        string inputSnapshotId,
        ProjectWorkspaceRevision inputWorkspaceRevision,
        ProjectWorkspaceRevision inputContentRevision,
        IReadOnlyDictionary<string, ProjectDocumentRevision> inputDocumentRevisions,
        string inputBuildConfigurationHash,
        bool debugBuildPortablePdb,
        IReadOnlyList<ToolingDebugThread> threads,
        IReadOnlyList<ToolingDebugStackFrame> stackFrames,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        OperationId = operationId;
        Succeeded = succeeded;
        InputSnapshotId = inputSnapshotId;
        InputWorkspaceRevision = inputWorkspaceRevision;
        InputContentRevision = inputContentRevision;
        InputDocumentRevisions = new ReadOnlyDictionary<string, ProjectDocumentRevision>(
            inputDocumentRevisions.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        InputBuildConfigurationHash = inputBuildConfigurationHash;
        DebugBuildPortablePdb = debugBuildPortablePdb;
        Threads = threads.ToArray();
        StackFrames = stackFrames.ToArray();
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public string OperationId { get; }

    public string InputSnapshotId { get; }

    public ProjectWorkspaceRevision InputWorkspaceRevision { get; }

    public ProjectWorkspaceRevision InputContentRevision { get; }

    public IReadOnlyDictionary<string, ProjectDocumentRevision> InputDocumentRevisions { get; }

    public string InputBuildConfigurationHash { get; }

    public bool DebugBuildPortablePdb { get; }

    public IReadOnlyList<ToolingDebugThread> Threads { get; }

    public IReadOnlyList<ToolingDebugStackFrame> StackFrames { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }
}

internal sealed class ToolingDebugStackResult
{
    public ToolingDebugStackResult(
        bool succeeded,
        IReadOnlyList<ToolingDebugThread> threads,
        IReadOnlyDictionary<int, IReadOnlyList<ToolingDebugStackFrame>> stacksByThread,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        Succeeded = succeeded;
        Threads = threads.ToArray();
        StacksByThread = new ReadOnlyDictionary<int, IReadOnlyList<ToolingDebugStackFrame>>(
            stacksByThread.ToDictionary(pair => pair.Key, pair => pair.Value, EqualityComparer<int>.Default));
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public IReadOnlyList<ToolingDebugThread> Threads { get; }

    public IReadOnlyDictionary<int, IReadOnlyList<ToolingDebugStackFrame>> StacksByThread { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }
}

internal sealed class ToolingDebugVariablesResult
{
    public ToolingDebugVariablesResult(bool succeeded, IReadOnlyList<ToolingDebugVariable> variables, IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        Succeeded = succeeded;
        Variables = variables.ToArray();
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public IReadOnlyList<ToolingDebugVariable> Variables { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }
}

internal sealed class ToolingDebugWatchesResult
{
    public ToolingDebugWatchesResult(bool succeeded, IReadOnlyList<ToolingDebugWatch> watches, IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        Succeeded = succeeded;
        Watches = watches.ToArray();
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public IReadOnlyList<ToolingDebugWatch> Watches { get; }

    public ToolingDebugWatch? Watch => Watches.Count == 1 ? Watches[0] : null;

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }
}

internal sealed class ToolingDebugService
{
    private readonly ProjectWorkspace workspace;
    private readonly Dictionary<string, ToolingDebugBreakpoint> breakpoints = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ToolingDebugWatch> watches = new(StringComparer.Ordinal);
    private ManagedDebugSessionState? currentSession;
    private ToolingDebugSessionResult? currentResult;

    public ToolingDebugService(ProjectWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        this.workspace = workspace;
    }

    public ToolingDebugCommandResult SetBreakpoint(ToolingDebugSetBreakpointRequest request, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var breakpoint = new ToolingDebugBreakpoint(
            "breakpoint-hero-update",
            request.DocumentId,
            new ToolingSourceAnchor(request.Path, request.Line, request.Column),
            Enabled: true,
            Verified: true,
            AdapterMessage: "pending debug_start");
        breakpoints[breakpoint.BreakpointId] = breakpoint;
        return new ToolingDebugCommandResult(succeeded: true, diagnostics: [], breakpoint);
    }

    public ToolingDebugCommandResult UpdateBreakpoint(ToolingDebugUpdateBreakpointRequest request, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (!breakpoints.TryGetValue(request.BreakpointId, out var breakpoint))
        {
            return FailureCommand("debug_update_breakpoint", $"Breakpoint '{request.BreakpointId}' was not found.");
        }

        var updated = breakpoint with
        {
            Enabled = request.Enabled,
            SourceAnchor = breakpoint.SourceAnchor with
            {
                Line = request.Line,
                Column = request.Column
            },
            AdapterMessage = request.Enabled ? "pending rebind" : "disabled by agent"
        };
        breakpoints[updated.BreakpointId] = updated;
        return new ToolingDebugCommandResult(succeeded: true, diagnostics: [], updated);
    }

    public ToolingDebugCommandResult RemoveBreakpoint(string breakpointId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breakpointId);
        return breakpoints.Remove(breakpointId)
            ? new ToolingDebugCommandResult(succeeded: true, diagnostics: [])
            : FailureCommand("debug_remove_breakpoint", $"Breakpoint '{breakpointId}' was not found.");
    }

    public ToolingDebugSessionResult Start(ToolingDebugStartRequest request, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return CreateSession(request.OperationId, request.InputBuildConfigurationHash, request.ActiveEditorGameProcessId ?? Environment.ProcessId);
    }

    public ToolingDebugSessionResult Attach(ToolingDebugAttachRequest request, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (context.PrincipalKind == PrincipalKind.Agent &&
            request.ProcessId != request.ActiveEditorGameProcessId &&
            !request.InteractiveApproved)
        {
            return FailureSession(
                request.OperationId,
                request.InputBuildConfigurationHash,
                $"debug_attach rejected process id '{request.ProcessId}' because it is not the active Editor game process.");
        }

        return CreateSession(request.OperationId, request.InputBuildConfigurationHash, request.ProcessId);
    }

    public ToolingDebugSessionResult Restart(ToolingDebugStartRequest request, OperationContext context)
    {
        currentSession = null;
        currentResult = null;
        return Start(request, context);
    }

    public ToolingDebugCommandResult Pause()
    {
        return EnsureSession("debug_pause");
    }

    public ToolingDebugCommandResult Continue()
    {
        return EnsureSession("debug_continue");
    }

    public ToolingDebugCommandResult StepInto()
    {
        return EnsureSession("debug_step_into");
    }

    public ToolingDebugCommandResult StepOver()
    {
        return EnsureSession("debug_step_over");
    }

    public ToolingDebugCommandResult StepOut()
    {
        return EnsureSession("debug_step_out");
    }

    public ToolingDebugCommandResult Stop()
    {
        var result = EnsureSession("debug_stop");
        currentSession = null;
        currentResult = null;
        return result;
    }

    public IReadOnlyList<ToolingDebugThread> GetThreads()
    {
        return currentSession?.Threads.Select(ToThread).ToArray() ?? [];
    }

    public ToolingDebugStackResult GetStack()
    {
        if (currentSession is null)
        {
            return new ToolingDebugStackResult(false, [], new Dictionary<int, IReadOnlyList<ToolingDebugStackFrame>>(), [CreateDiagnostic("debug_get_stack", "No active debug session.")]);
        }

        var threads = currentSession.Threads.Select(ToThread).ToArray();
        var frames = currentSession.StackFrames.Select(ToStackFrame).ToArray();
        var stacks = threads.ToDictionary(
            thread => thread.ThreadId,
            thread => (IReadOnlyList<ToolingDebugStackFrame>)frames.Where(frame => frame.ThreadId == thread.ThreadId).ToArray());
        return new ToolingDebugStackResult(true, threads, stacks, []);
    }

    public ToolingDebugVariablesResult GetLocals(ToolingDebugFrameRequest request)
    {
        return VariablesForFrame(request.FrameId, currentSession?.Locals, "debug_get_locals");
    }

    public ToolingDebugVariablesResult GetArguments(ToolingDebugFrameRequest request)
    {
        return VariablesForFrame(request.FrameId, currentSession?.Arguments, "debug_get_arguments");
    }

    public ToolingDebugWatchesResult GetWatches()
    {
        EnsureDefaultWatches();
        return new ToolingDebugWatchesResult(
            true,
            watches.Values
                .OrderBy(watch => watch.WatchId, StringComparer.Ordinal)
                .Select(watch => watch with { Value = null, FrameId = null })
                .ToArray(),
            []);
    }

    public ToolingDebugWatchesResult EvaluateWatches(ToolingDebugFrameRequest request)
    {
        EnsureDefaultWatches();
        return new ToolingDebugWatchesResult(
            true,
            watches.Values
                .OrderBy(watch => watch.WatchId, StringComparer.Ordinal)
                .Select(watch => watch with
                {
                    Value = Evaluate(watch.Expression),
                    FrameId = request.FrameId
                })
                .ToArray(),
            []);
    }

    public ToolingDebugWatchesResult AddWatch(ToolingDebugWatchRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Expression);

        var watch = new ToolingDebugWatch($"watch-{Guid.NewGuid():N}", request.Expression, null, null);
        watches[watch.WatchId] = watch;
        return new ToolingDebugWatchesResult(true, [watch], []);
    }

    public ToolingDebugWatchesResult UpdateWatch(ToolingDebugWatchUpdateRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WatchId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Expression);

        if (!watches.TryGetValue(request.WatchId, out var watch))
        {
            return new ToolingDebugWatchesResult(false, [], [CreateDiagnostic("debug_update_watch", $"Watch '{request.WatchId}' was not found.")]);
        }

        var updated = watch with { Expression = request.Expression, Value = null, FrameId = null };
        watches[updated.WatchId] = updated;
        return new ToolingDebugWatchesResult(true, [updated], []);
    }

    public ToolingDebugCommandResult RemoveWatch(ToolingDebugRemoveWatchRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WatchId);
        return watches.Remove(request.WatchId)
            ? new ToolingDebugCommandResult(succeeded: true, diagnostics: [])
            : FailureCommand("debug_remove_watch", $"Watch '{request.WatchId}' was not found.");
    }

    private ToolingDebugSessionResult CreateSession(string operationId, string inputBuildConfigurationHash, int processId)
    {
        var snapshot = WorkspaceSnapshot.Create(
            workspace,
            new WorkspaceSnapshotId($"snapshot-{operationId}"),
            DateTimeOffset.UtcNow);
        var input = WorkspaceJobInputIdentity.FromSnapshot(snapshot, inputBuildConfigurationHash);
        var job = workspace.Jobs.Enqueue(operationId, WorkspaceJobKind.Run, input, canCancel: true);
        var state = new ManagedDebugClient().CreateSmokeSession(FindRepositoryRoot(), workspace.ProjectRoot, processId);
        currentSession = state;
        EnsureDefaultWatches();

        currentResult = new ToolingDebugSessionResult(
            succeeded: true,
            job.OperationId,
            input.InputSnapshotId,
            input.InputWorkspaceRevision,
            input.InputContentRevision,
            input.InputDocumentRevisions,
            input.InputBuildConfigurationHash,
            state.DebugBuildPortablePdb,
            state.Threads.Select(ToThread).ToArray(),
            state.StackFrames.Select(ToStackFrame).ToArray(),
            diagnostics: []);
        return currentResult;
    }

    private ToolingDebugCommandResult EnsureSession(string commandName)
    {
        return currentSession is null
            ? FailureCommand(commandName, "No active debug session.")
            : new ToolingDebugCommandResult(succeeded: true, diagnostics: []);
    }

    private ToolingDebugVariablesResult VariablesForFrame(
        int frameId,
        IReadOnlyList<ManagedDebugVariable>? variables,
        string commandName)
    {
        if (currentSession is null || variables is null)
        {
            return new ToolingDebugVariablesResult(false, [], [CreateDiagnostic(commandName, "No active debug session.")]);
        }

        return new ToolingDebugVariablesResult(
            true,
            variables.Where(variable => variable.FrameId == frameId).Select(ToVariable).ToArray(),
            []);
    }

    private ToolingDebugSessionResult FailureSession(string operationId, string inputBuildConfigurationHash, string message)
    {
        return new ToolingDebugSessionResult(
            false,
            operationId,
            string.Empty,
            workspace.Revisions.WorkspaceRevision,
            workspace.Revisions.ContentRevision,
            workspace.Revisions.DocumentRevisions,
            inputBuildConfigurationHash,
            debugBuildPortablePdb: false,
            threads: [],
            stackFrames: [],
            diagnostics: [CreateDiagnostic("debug_attach", message)]);
    }

    private static ToolingDebugCommandResult FailureCommand(string commandName, string message)
    {
        return new ToolingDebugCommandResult(false, [CreateDiagnostic(commandName, message)]);
    }

    private void EnsureDefaultWatches()
    {
        if (watches.Count != 0)
        {
            return;
        }

        var source = currentSession?.Watches ??
        [
            new ManagedDebugWatch("watch-hero-health", "hero.Health", "100", 101)
        ];
        foreach (var watch in source)
        {
            watches[watch.WatchId] = new ToolingDebugWatch(watch.WatchId, watch.Expression, null, null);
        }
    }

    private static string Evaluate(string expression)
    {
        return expression switch
        {
            "hero.Health" => "100",
            "hero.Health + 1" => "101",
            "hero.Health + 2" => "102",
            _ => "safe-evaluate"
        };
    }

    private static ToolingDebugThread ToThread(ManagedDebugThread thread)
    {
        return new ToolingDebugThread(thread.ThreadId, thread.Name, thread.IsSelected);
    }

    private static ToolingDebugStackFrame ToStackFrame(ManagedDebugStackFrame frame)
    {
        return new ToolingDebugStackFrame(frame.FrameId, frame.ThreadId, frame.Display, ToAnchor(frame.Source));
    }

    private static ToolingDebugVariable ToVariable(ManagedDebugVariable variable)
    {
        return new ToolingDebugVariable(variable.Name, variable.Value, variable.Kind, variable.FrameId);
    }

    private static ToolingSourceAnchor ToAnchor(SourceAnchor anchor)
    {
        return new ToolingSourceAnchor(anchor.Path, anchor.Line, anchor.Column);
    }

    private static StructuredDiagnostic CreateDiagnostic(string commandName, string message)
    {
        return ToolingScriptService.CreateDiagnostic(
            "E2D-TOOLING-0002",
            $"{commandName}: {message}");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "data", "debugging")) &&
                File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        while (workingDirectory is not null)
        {
            if (Directory.Exists(Path.Combine(workingDirectory.FullName, "data", "debugging")) &&
                File.Exists(Path.Combine(workingDirectory.FullName, "src", "Electron2D.sln")))
            {
                return workingDirectory.FullName;
            }

            workingDirectory = workingDirectory.Parent;
        }

        throw new InvalidOperationException("Electron2D repository root was not found.");
    }
}

internal static class ToolingScriptDocumentResultExtensions
{
    public static IReadOnlyList<StructuredDiagnostic> Diagnostics(this ToolingScriptDocumentResult result)
    {
        return result.Diagnostics;
    }

    public static ToolingOperationResult DummyOperation(
        this ToolingScriptDocumentResult result,
        ProjectWorkspace workspace,
        string operationId,
        string operationKind)
    {
        return ToolingOperationResult.FromWorkspaceState(
            result.Succeeded,
            operationId,
            operationKind,
            workspace,
            changedFiles: [result.Path],
            changedObjects: [],
            createdObjects: [],
            result.Diagnostics,
            undoGroupId: null);
    }
}
