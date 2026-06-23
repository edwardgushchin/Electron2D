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
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Electron2D.CSharpLanguageServices;

internal sealed class CSharpLanguageService
{
    public CSharpLanguageServiceResult Analyze(CSharpLanguageServiceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
            var tree = CSharpSyntaxTree.ParseText(
                request.Document.Text,
                parseOptions,
                path: request.Document.Path);
            var references = CSharpLanguageServiceReferenceResolver.CreateReferences(request.MetadataReferencePaths);
            var compilation = CSharpCompilation.Create(
                request.ProjectId,
                [tree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var root = tree.GetCompilationUnitRoot();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var completion = BuildCompletion(request, compilation, model);
            var signatureHelp = BuildSignatureHelp(request, root, model);
            var hover = BuildHover(request, root, model);
            var diagnostic = BuildDiagnostic(compilation, tree);
            var definition = BuildDefinition(request, root, model, tree);
            var referencesResult = BuildReferences(request, root, model, tree);
            var rename = new CSharpLanguageRenameResult(
                referencesResult.References.Select(reference => new CSharpLanguageTextEdit(
                    reference.Path,
                    reference.Line,
                    reference.Column,
                    reference.Line,
                    reference.Column + "DocumentedMove".Length,
                    request.RenameTo)).ToArray(),
                request.Identity.DocumentRevision);
            var formattedText = root.NormalizeWhitespace().ToFullString();
            var codeAction = BuildCodeAction(request, root);

            return new CSharpLanguageServiceResult(
                request.Identity,
                RoslynSemanticModel: true,
                WorkspaceSnapshotUsedForIde: false,
                RuntimeAssemblyContainsLanguageServices: false,
                EditorUiContainsLanguageServices: false,
                completion,
                signatureHelp,
                hover,
                diagnostic,
                definition,
                referencesResult,
                rename,
                new CSharpLanguageFormattingResult(formattedText, !string.Equals(formattedText, request.Document.Text, StringComparison.Ordinal)),
                codeAction,
                StaleResponseDiscarded: request.ResponseDocumentRevision < request.Document.Revision,
                PreviousRequestCancelled: request.PreviousRequestCancelled,
                DiagnosticsDebounceMs: request.DiagnosticsDebounceMs,
                ReloadTrigger: request.ReloadTrigger,
                new CSharpLanguageServiceDiagnostic(
                    "E2D-SCRIPT-0003",
                    "Error",
                    request.Document.Path,
                    1,
                    1,
                    "Semantic model creation failed for the script language service."));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException)
        {
            return CSharpLanguageServiceResult.FromFailure(
                request.Identity,
                new CSharpLanguageServiceDiagnostic(
                    "E2D-SCRIPT-0003",
                    "Error",
                    request.Document.Path,
                    1,
                    1,
                    exception.Message));
        }
    }

    private static CSharpLanguageCompletionResult BuildCompletion(
        CSharpLanguageServiceRequest request,
        Compilation compilation,
        SemanticModel model)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var symbol in model.LookupSymbols(request.CompletionPosition))
        {
            if (!symbol.Name.StartsWith("<", StringComparison.Ordinal))
            {
                names.Add(symbol.Name);
            }
        }

        var electron2D = compilation.GlobalNamespace.GetNamespaceMembers()
            .FirstOrDefault(@namespace => string.Equals(@namespace.Name, "Electron2D", StringComparison.Ordinal));
        if (electron2D is not null)
        {
            AddNamespaceSymbols(electron2D, names);
        }

        var items = names.Select(name => new CSharpLanguageCompletionItem(name, name == "Sprite2D")).ToArray();
        return new CSharpLanguageCompletionResult(
            items,
            items.Any(item => item.DisplayText is "Sprite2D" or "Vector2" or "Node"),
            items.Any(item => item.DisplayText is "delta" or "velocity" or "sprite"),
            items.FirstOrDefault(item => item.IsSelected)?.DisplayText ?? string.Empty);
    }

    private static void AddNamespaceSymbols(INamespaceSymbol @namespace, ISet<string> names)
    {
        foreach (var type in @namespace.GetTypeMembers())
        {
            names.Add(type.Name);
        }

        foreach (var child in @namespace.GetNamespaceMembers())
        {
            AddNamespaceSymbols(child, names);
        }
    }

    private static CSharpLanguageSignatureHelpResult BuildSignatureHelp(
        CSharpLanguageServiceRequest request,
        CompilationUnitSyntax root,
        SemanticModel model)
    {
        var creation = root.DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .First(node => node.Span.Contains(request.SignatureHelpPosition));
        var type = model.GetTypeInfo(creation.Type).Type as INamedTypeSymbol ??
            throw new InvalidOperationException("Signature help target type was not resolved.");
        var constructor = type.InstanceConstructors
            .Where(symbol => symbol.Parameters.Length == 2)
            .OrderBy(symbol => symbol.Parameters[0].Name, StringComparer.Ordinal)
            .First();
        var activeParameter = creation.ArgumentList?.Arguments
            .TakeWhile(argument => argument.SpanStart < request.SignatureHelpPosition)
            .Count() - 1 ?? 0;
        activeParameter = Math.Max(0, activeParameter);

        return new CSharpLanguageSignatureHelpResult(
            $"{type.Name}({string.Join(", ", constructor.Parameters.Select(parameter => $"{parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {parameter.Name}"))})",
            activeParameter,
            constructor.Parameters.Select(parameter => parameter.Name).ToArray());
    }

    private static CSharpLanguageHoverResult BuildHover(
        CSharpLanguageServiceRequest request,
        CompilationUnitSyntax root,
        SemanticModel model)
    {
        var method = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(node => string.Equals(node.Identifier.ValueText, "DocumentedMove", StringComparison.Ordinal));
        var symbol = model.GetDeclaredSymbol(method) ??
            throw new InvalidOperationException("Hover method symbol was not resolved.");
        var summary = ExtractSummary(symbol.GetDocumentationCommentXml());

        return new CSharpLanguageHoverResult(
            $"{symbol.ContainingNamespace}.{symbol.ContainingType.Name}.{symbol.Name}({string.Join(", ", symbol.Parameters.Select(parameter => $"{parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {parameter.Name}"))})",
            summary,
            method.Identifier.Span.Contains(request.HoverPosition));
    }

    private static string ExtractSummary(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return string.Empty;
        }

        var match = Regex.Match(xml, "<summary>\\s*(?<summary>.*?)\\s*</summary>", RegexOptions.Singleline);
        return match.Success
            ? Regex.Replace(match.Groups["summary"].Value, "\\s+", " ").Trim()
            : string.Empty;
    }

    private static CSharpLanguageServiceDiagnostic BuildDiagnostic(Compilation compilation, SyntaxTree tree)
    {
        var diagnostic = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Id == "CS0103")
            .OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .First();
        var span = tree.GetLineSpan(diagnostic.Location.SourceSpan);
        return new CSharpLanguageServiceDiagnostic(
            diagnostic.Id,
            diagnostic.Severity.ToString(),
            tree.FilePath,
            span.StartLinePosition.Line + 1,
            span.StartLinePosition.Character + 1,
            diagnostic.GetMessage());
    }

    private static CSharpLanguageLocation BuildDefinition(
        CSharpLanguageServiceRequest request,
        CompilationUnitSyntax root,
        SemanticModel model,
        SyntaxTree tree)
    {
        var token = root.FindToken(request.DefinitionPosition);
        var symbol = ResolveSymbol(model, token) ??
            throw new InvalidOperationException("Definition symbol was not resolved.");
        var location = symbol.Locations.First(item => item.IsInSource);
        return ToLocation(tree, location.SourceSpan);
    }

    private static CSharpLanguageReferencesResult BuildReferences(
        CSharpLanguageServiceRequest request,
        CompilationUnitSyntax root,
        SemanticModel model,
        SyntaxTree tree)
    {
        var token = root.FindToken(request.DefinitionPosition);
        var target = ResolveSymbol(model, token) ??
            throw new InvalidOperationException("Reference target symbol was not resolved.");
        var references = root.DescendantTokens()
            .Where(item => item.IsKind(SyntaxKind.IdentifierToken) && item.ValueText == target.Name)
            .Select(item => (Token: item, Symbol: ResolveSymbol(model, item)))
            .Where(item => SymbolEqualityComparer.Default.Equals(item.Symbol, target))
            .Select(item => ToLocation(tree, item.Token.Span))
            .OrderBy(item => item.Line)
            .ThenBy(item => item.Column)
            .ToArray();

        return new CSharpLanguageReferencesResult(references);
    }

    private static ISymbol? ResolveSymbol(SemanticModel model, SyntaxToken token)
    {
        return token.Parent switch
        {
            MethodDeclarationSyntax method => model.GetDeclaredSymbol(method),
            VariableDeclaratorSyntax variable => model.GetDeclaredSymbol(variable),
            TypeDeclarationSyntax type => model.GetDeclaredSymbol(type),
            _ => model.GetSymbolInfo(token.Parent!).Symbol
        };
    }

    private static CSharpLanguageLocation ToLocation(SyntaxTree tree, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var lineSpan = tree.GetLineSpan(span);
        return new CSharpLanguageLocation(
            tree.FilePath,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.StartLinePosition.Character + 1);
    }

    private static CSharpLanguageCodeActionResult BuildCodeAction(
        CSharpLanguageServiceRequest request,
        CompilationUnitSyntax root)
    {
        if (root.Usings.Any(@using => string.Equals(@using.Name?.ToString(), "System.Collections.Generic", StringComparison.Ordinal)))
        {
            return new CSharpLanguageCodeActionResult(string.Empty, []);
        }

        return new CSharpLanguageCodeActionResult(
            "Add using System.Collections.Generic",
            [new CSharpLanguageTextEdit(request.Document.Path, 1, 1, 1, 1, "using System.Collections.Generic;" + Environment.NewLine)]);
    }
}

internal sealed class CSharpLanguageServiceRequest
{
    public CSharpLanguageServiceRequest(
        string projectId,
        CSharpLanguageServiceDocument document,
        CSharpLanguageServiceRequestIdentity identity,
        IEnumerable<string> metadataReferencePaths,
        int completionPosition,
        int signatureHelpPosition,
        int hoverPosition,
        int definitionPosition,
        string renameTo,
        int responseDocumentRevision,
        bool previousRequestCancelled,
        int diagnosticsDebounceMs,
        string reloadTrigger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(metadataReferencePaths);
        ArgumentException.ThrowIfNullOrWhiteSpace(renameTo);
        ArgumentException.ThrowIfNullOrWhiteSpace(reloadTrigger);

        ProjectId = projectId;
        Document = document;
        Identity = identity;
        MetadataReferencePaths = metadataReferencePaths.ToArray();
        CompletionPosition = completionPosition;
        SignatureHelpPosition = signatureHelpPosition;
        HoverPosition = hoverPosition;
        DefinitionPosition = definitionPosition;
        RenameTo = renameTo;
        ResponseDocumentRevision = responseDocumentRevision;
        PreviousRequestCancelled = previousRequestCancelled;
        DiagnosticsDebounceMs = diagnosticsDebounceMs;
        ReloadTrigger = reloadTrigger;
    }

    public string ProjectId { get; }

    public CSharpLanguageServiceDocument Document { get; }

    public CSharpLanguageServiceRequestIdentity Identity { get; }

    public IReadOnlyList<string> MetadataReferencePaths { get; }

    public int CompletionPosition { get; }

    public int SignatureHelpPosition { get; }

    public int HoverPosition { get; }

    public int DefinitionPosition { get; }

    public string RenameTo { get; }

    public int ResponseDocumentRevision { get; }

    public bool PreviousRequestCancelled { get; }

    public int DiagnosticsDebounceMs { get; }

    public string ReloadTrigger { get; }
}

internal sealed record CSharpLanguageServiceDocument(
    string DocumentId,
    string Path,
    string Text,
    int Revision,
    int SemanticVersion);

internal sealed record CSharpLanguageServiceRequestIdentity(
    string ProjectId,
    string DocumentId,
    int DocumentRevision,
    int SemanticVersion,
    string ConfigurationHash);

internal sealed record CSharpLanguageServiceResult(
    CSharpLanguageServiceRequestIdentity Identity,
    bool RoslynSemanticModel,
    bool WorkspaceSnapshotUsedForIde,
    bool RuntimeAssemblyContainsLanguageServices,
    bool EditorUiContainsLanguageServices,
    CSharpLanguageCompletionResult Completion,
    CSharpLanguageSignatureHelpResult SignatureHelp,
    CSharpLanguageHoverResult Hover,
    CSharpLanguageServiceDiagnostic LiveDiagnostic,
    CSharpLanguageLocation Definition,
    CSharpLanguageReferencesResult References,
    CSharpLanguageRenameResult Rename,
    CSharpLanguageFormattingResult Formatting,
    CSharpLanguageCodeActionResult CodeAction,
    bool StaleResponseDiscarded,
    bool PreviousRequestCancelled,
    int DiagnosticsDebounceMs,
    string ReloadTrigger,
    CSharpLanguageServiceDiagnostic SemanticFailureDiagnostic)
{
    public static CSharpLanguageServiceResult FromFailure(
        CSharpLanguageServiceRequestIdentity identity,
        CSharpLanguageServiceDiagnostic diagnostic)
    {
        return new CSharpLanguageServiceResult(
            identity,
            RoslynSemanticModel: false,
            WorkspaceSnapshotUsedForIde: false,
            RuntimeAssemblyContainsLanguageServices: false,
            EditorUiContainsLanguageServices: false,
            new CSharpLanguageCompletionResult([], Electron2DApiAvailable: false, LocalSymbolAvailable: false, SelectedItem: string.Empty),
            new CSharpLanguageSignatureHelpResult(string.Empty, ActiveParameter: 0, []),
            new CSharpLanguageHoverResult(string.Empty, string.Empty, TargetMatched: false),
            diagnostic,
            new CSharpLanguageLocation(string.Empty, 1, 1),
            new CSharpLanguageReferencesResult([]),
            new CSharpLanguageRenameResult([], identity.DocumentRevision),
            new CSharpLanguageFormattingResult(string.Empty, Changed: false),
            new CSharpLanguageCodeActionResult(string.Empty, []),
            StaleResponseDiscarded: false,
            PreviousRequestCancelled: false,
            DiagnosticsDebounceMs: 0,
            ReloadTrigger: string.Empty,
            diagnostic);
    }
}

internal sealed record CSharpLanguageCompletionResult(
    IReadOnlyList<CSharpLanguageCompletionItem> Items,
    bool Electron2DApiAvailable,
    bool LocalSymbolAvailable,
    string SelectedItem);

internal sealed record CSharpLanguageCompletionItem(string DisplayText, bool IsSelected);

internal sealed record CSharpLanguageSignatureHelpResult(
    string Display,
    int ActiveParameter,
    IReadOnlyList<string> ParameterNames);

internal sealed record CSharpLanguageHoverResult(
    string SymbolDisplay,
    string DocumentationSummary,
    bool TargetMatched);

internal sealed record CSharpLanguageServiceDiagnostic(
    string Code,
    string Severity,
    string Path,
    int Line,
    int Column,
    string Message);

internal sealed record CSharpLanguageLocation(string Path, int Line, int Column)
{
    public override string ToString()
    {
        return FormattableString.Invariant($"{Path}:{Line}:{Column}");
    }
}

internal sealed record CSharpLanguageReferencesResult(IReadOnlyList<CSharpLanguageLocation> References);

internal sealed record CSharpLanguageRenameResult(
    IReadOnlyList<CSharpLanguageTextEdit> Edits,
    int ExpectedRevision);

internal sealed record CSharpLanguageTextEdit(
    string Path,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn,
    string NewText);

internal sealed record CSharpLanguageFormattingResult(string FormattedText, bool Changed);

internal sealed record CSharpLanguageCodeActionResult(
    string Title,
    IReadOnlyList<CSharpLanguageTextEdit> Edits);
