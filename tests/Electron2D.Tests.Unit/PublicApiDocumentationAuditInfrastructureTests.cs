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
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class PublicApiDocumentationAuditInfrastructureTests
{
    [Fact]
    public void RepositoryContainsPublicApiDocumentationAuditGate()
    {
        var root = FindRepositoryRoot();
        var xmlVerifierPath = Path.Combine(root, "eng", "Electron2D.Build", "RepositoryWorkflowVerifiers.cs");
        var auditVerifierPath = xmlVerifierPath;
        var workflowPath = Path.Combine(root, ".github", "workflows", "ci.yml");
        var docPath = Path.Combine(root, "docs", "documentation", "public-api-xml-documentation.md");

        Assert.True(File.Exists(xmlVerifierPath), $"Missing XML documentation verifier: {xmlVerifierPath}");
        Assert.True(File.Exists(auditVerifierPath), $"Missing public API documentation audit verifier: {auditVerifierPath}");
        Assert.True(File.Exists(docPath), $"Missing public API documentation domain document: {docPath}");

        var xmlVerifier = File.ReadAllText(xmlVerifierPath);
        foreach (var requiredIssueCode in new[]
        {
            "PublicApiXmlDocsVerifier",
            "verify public-api-xml-docs",
            "missing-doc",
            "missing-summary",
            "summary-missing-para",
            "missing-param",
            "missing-typeparam",
            "missing-returns",
            "missing-value",
            "missing-threadsafety",
            "missing-since",
            "missing-seealso-reference",
            "missing-exception-cref",
            "forbidden-wording",
            "placeholder",
            "inheritdoc"
        })
        {
            Assert.Contains(requiredIssueCode, xmlVerifier, StringComparison.Ordinal);
        }

        var auditVerifier = File.ReadAllText(auditVerifierPath);
        foreach (var requiredFragment in new[]
        {
            "verify public-api-xml-docs",
            "fail-on-issues",
            "verify public-api-documentation",
            "wiki-path",
            "docs/documentation",
            "SDL_shadercross",
            "Godot-like",
            "PublicApiWikiVerifier"
        })
        {
            Assert.Contains(requiredFragment, auditVerifier, StringComparison.Ordinal);
        }

        var workflow = File.ReadAllText(workflowPath);
        Assert.Contains("dotnet run --project eng/Electron2D.Build -- verify public-api-xml-docs --fail-on-issues", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project eng/Electron2D.Build -- update wiki --output .github/wiki --check", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project eng/Electron2D.Build -- verify public-api-documentation --wiki-path .github/wiki", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("Verify-PublicApiXmlDocs.ps1", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("Verify-PublicApiDocumentationAudit.ps1", workflow, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
