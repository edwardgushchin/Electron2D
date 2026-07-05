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

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Electron2D.Build;

internal sealed class RepositoryReadmeVerifier(string repositoryRoot)
{
    private const string ReadmeRelativePath = "README.md";
    private const string ExpectedTagline = "Agent-native cross-platform 2D game engine";
    private const string DarkReadmeAsset = "data/assets/branding/readme/electron2d_readme_dark.svg";
    private const string LightReadmeAsset = "data/assets/branding/readme/electron2d_readme_light.svg";

    private static readonly string[] RequiredBadgeFragments =
    [
        "img.shields.io/github/contributors/edwardgushchin/Electron2D",
        "img.shields.io/github/last-commit/edwardgushchin/Electron2D",
        "img.shields.io/badge/license-MIT-green",
        "img.shields.io/badge/.NET-10-512BD4",
        "img.shields.io/badge/C%23-14-239120",
        "img.shields.io/badge/version-0.1--preview-blue"
    ];

    private static readonly (string Anchor, string Heading)[] RequiredSections =
    [
        ("about", "## \U0001F9ED About"),
        ("features", "## \u2728 Features"),
        ("platforms", "## \U0001F5A5\uFE0F Platforms"),
        ("installation", "## \U0001F4E6 Installation"),
        ("quick-start", "## \U0001F680 Quick Start"),
        ("documentation", "## \U0001F4DA Documentation"),
        ("examples", "## \U0001F3AE Examples"),
        ("feedback-and-contributing", "## \U0001F4AC Feedback and Contributing"),
        ("contributors", "## \U0001F465 Contributors"),
        ("license", "## \U0001F4C4 License")
    ];

    private static readonly (string Anchor, string Label)[] RequiredNavigationLinks =
    [
        ("about", "About"),
        ("features", "Features"),
        ("platforms", "Platforms"),
        ("installation", "Installation"),
        ("quick-start", "Quick Start"),
        ("documentation", "Documentation"),
        ("examples", "Examples"),
        ("feedback-and-contributing", "Feedback"),
        ("license", "License")
    ];

    private static readonly string[] RequiredFeatureLabels =
    [
        "Agent-native workflow",
        "Trello-style task board",
        "Built-in editor",
        "C# scripting",
        "Node-based scenes",
        "2D rendering",
        "2D physics",
        "Asset workflow",
        "Cross-platform runtime"
    ];

    private static readonly string[] RequiredPlatformRows =
    [
        "| Platform | Editor | Runtime |",
        "| Windows | \u2705 Done | \u2705 Done |",
        "| Linux | \u2705 Done | \u2705 Done |",
        "| macOS | \u2705 Done | \u2705 Done |",
        "| Android | \u274C Not planned | \u2705 Done |",
        "| iOS | \u274C Not planned | \U0001F553 Planned |",
        "| Web | \u274C Not planned | \U0001F553 Planned |"
    ];

    private static readonly string[] ForbiddenFragments =
    [
        "C#-first",
        "baseline",
        "release gate",
        "dry-run",
        "small and medium 2D games",
        "project-local metadata",
        "primary runtime targets for the preview line",
        "TASKS.md",
        "PowerShell",
        ".ps1",
        "pwsh",
        "Verify-",
        "verify readme",
        "verify docs",
        "dotnet run --project eng/Electron2D.Build",
        "UI-heavy",
        "UI heavy",
        "reference-game",
        "ReferenceGame",
        "electron2d-empty"
    ];

    private static readonly string[] ForbiddenDocumentationLinks =
    [
        "docs/README.md",
        ".github/wiki/",
        "docs/documentation/repository-readme.md",
        "docs/releases/",
        "docs/verdicts/",
        "TASKS.md"
    ];

    public IReadOnlyList<BuildDiagnostic> Verify()
    {
        var diagnostics = new List<BuildDiagnostic>();
        var readmePath = Path.Combine(repositoryRoot, ReadmeRelativePath);
        if (!File.Exists(readmePath))
        {
            diagnostics.Add(CreateError("E2D-BUILD-README-FILE-MISSING", "README.md was not found.", ReadmeRelativePath));
            return diagnostics;
        }

        var readme = File.ReadAllText(readmePath, Encoding.UTF8).Replace("\r\n", "\n").Replace('\r', '\n');

        VerifyLogo(readme, diagnostics);
        VerifyBadges(readme, diagnostics);
        VerifyNavigation(readme, diagnostics);
        VerifySections(readme, diagnostics);
        VerifyStarCallout(readme, diagnostics);
        VerifyFeatures(readme, diagnostics);
        VerifyPlatforms(readme, diagnostics);
        VerifyInstallation(readme, diagnostics);
        VerifyQuickStart(readme, diagnostics);
        VerifyDocumentationSection(readme, diagnostics);
        VerifyExamples(readme, diagnostics);
        VerifyFeedbackAndLicense(readme, diagnostics);
        VerifyForbiddenContent(readme, diagnostics);
        VerifyVisibleTagline(readme, diagnostics);

        if (diagnostics.Count == 0)
        {
            diagnostics.Add(new BuildDiagnostic(
                "verify",
                "verify readme",
                "info",
                "E2D-BUILD-README-VERIFY-PASSED",
                "README.md satisfies the repository README contract.",
                Path: ReadmeRelativePath));
        }

        return diagnostics;
    }

    private static void VerifyLogo(string readme, List<BuildDiagnostic> diagnostics)
    {
        RequireContains(readme, "<picture>", "E2D-BUILD-README-LOGO-MISSING", "README hero must use a centered picture logo.", diagnostics);
        RequireContains(readme, DarkReadmeAsset, "E2D-BUILD-README-LOGO-ASSET-MISSING", "README must reference the dark brand SVG.", diagnostics);
        RequireContains(readme, LightReadmeAsset, "E2D-BUILD-README-LOGO-ASSET-MISSING", "README must reference the light brand SVG.", diagnostics);
        RequireContains(readme, "<img alt=\"Electron2D\"", "E2D-BUILD-README-LOGO-ALT-MISSING", "README logo must keep the Electron2D alt text.", diagnostics);
    }

    private static void VerifyBadges(string readme, List<BuildDiagnostic> diagnostics)
    {
        foreach (var fragment in RequiredBadgeFragments)
        {
            RequireContains(readme, fragment, "E2D-BUILD-README-BADGE-MISSING", $"README is missing required badge fragment: {fragment}.", diagnostics);
        }
    }

    private static void VerifyNavigation(string readme, List<BuildDiagnostic> diagnostics)
    {
        foreach (var (anchor, label) in RequiredNavigationLinks)
        {
            RequireContains(
                readme,
                $"<a href=\"#{anchor}\">{label}</a>",
                "E2D-BUILD-README-NAVIGATION-MISSING",
                $"README navigation is missing link '{label}' to '#{anchor}'.",
                diagnostics);
        }
    }

    private static void VerifySections(string readme, List<BuildDiagnostic> diagnostics)
    {
        foreach (var (anchor, heading) in RequiredSections)
        {
            RequireContains(
                readme,
                $"<a id=\"{anchor}\"></a>",
                "E2D-BUILD-README-ANCHOR-MISSING",
                $"README is missing explicit anchor '{anchor}'.",
                diagnostics);
            RequireContains(
                readme,
                heading,
                "E2D-BUILD-README-SECTION-MISSING",
                $"README is missing required section heading '{heading}'.",
                diagnostics);
        }
    }

    private static void VerifyStarCallout(string readme, List<BuildDiagnostic> diagnostics)
    {
        var visibleText = ExtractVisibleMarkdownText(readme);
        RequireContains(
            visibleText,
            "\u2B50 Star us on GitHub - it motivates us a lot!",
            "E2D-BUILD-README-STAR-CALLOUT-MISSING",
            "README is missing the required centered star callout.",
            diagnostics);
    }

    private static void VerifyFeatures(string readme, List<BuildDiagnostic> diagnostics)
    {
        var previousIndex = -1;
        foreach (var label in RequiredFeatureLabels)
        {
            var index = readme.IndexOf($"- **{label}**", StringComparison.Ordinal);
            if (index < 0)
            {
                diagnostics.Add(CreateError("E2D-BUILD-README-FEATURE-MISSING", $"README is missing required feature '{label}'."));
                continue;
            }

            if (index <= previousIndex)
            {
                diagnostics.Add(CreateError("E2D-BUILD-README-FEATURE-ORDER", $"README feature '{label}' is out of order."));
            }

            previousIndex = index;
        }

        RequireContains(
            readme,
            "Build and run games on Windows, Linux, macOS and Android. iOS and Web are planned as future runtime targets.",
            "E2D-BUILD-README-RUNTIME-WORDING",
            "README Cross-platform runtime wording does not match the public contract.",
            diagnostics);
        RequireContains(readme, "shared task columns, cards, assignees, labels, review states and editor-visible project context", "E2D-BUILD-README-TASK-BOARD-WORDING", "README Trello-style task board wording does not match the public contract.", diagnostics);
    }

    private static void VerifyPlatforms(string readme, List<BuildDiagnostic> diagnostics)
    {
        foreach (var row in RequiredPlatformRows)
        {
            RequireContains(readme, row, "E2D-BUILD-README-PLATFORM-ROW-MISSING", $"README platforms table is missing row: {row}.", diagnostics);
        }
    }

    private static void VerifyInstallation(string readme, List<BuildDiagnostic> diagnostics)
    {
        RequireContains(readme, "git clone https://github.com/edwardgushchin/Electron2D.git", "E2D-BUILD-README-INSTALLATION-MISSING", "README installation command is missing git clone.", diagnostics);
        RequireContains(readme, "cd Electron2D", "E2D-BUILD-README-INSTALLATION-MISSING", "README installation command is missing repository directory change.", diagnostics);
        RequireContains(readme, "dotnet build src/Electron2D.sln -c Release", "E2D-BUILD-README-INSTALLATION-MISSING", "README installation command is missing release build.", diagnostics);
    }

    private static void VerifyQuickStart(string readme, List<BuildDiagnostic> diagnostics)
    {
        RequireContains(
            readme,
            "dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -c Release",
            "E2D-BUILD-README-QUICK-START-MISSING",
            "README Quick Start must launch the editor.",
            diagnostics);
    }

    private static void VerifyDocumentationSection(string readme, List<BuildDiagnostic> diagnostics)
    {
        var section = GetSection(readme, "## \U0001F4DA Documentation");
        if (section.Length == 0)
        {
            diagnostics.Add(CreateError("E2D-BUILD-README-DOCUMENTATION-SECTION-MISSING", "README Documentation section was not found."));
            return;
        }

        var urls = Regex.Matches(section, @"https?://[^\s)]+", RegexOptions.CultureInvariant)
            .Select(match => match.Value)
            .ToArray();
        if (urls.Length != 1 || !string.Equals(urls[0], "https://github.com/edwardgushchin/Electron2D/wiki", StringComparison.Ordinal))
        {
            diagnostics.Add(CreateError("E2D-BUILD-README-DOCUMENTATION-LINK", "README Documentation section must contain only the Electron2D GitHub Wiki link."));
        }

        foreach (var fragment in ForbiddenDocumentationLinks)
        {
            if (readme.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                diagnostics.Add(CreateError("E2D-BUILD-README-DOCUMENTATION-LINK", $"README must not link to internal documentation target: {fragment}."));
            }
        }
    }

    private static void VerifyExamples(string readme, List<BuildDiagnostic> diagnostics)
    {
        RequireContains(
            readme,
            "- **[Platformer](https://github.com/edwardgushchin/Electron2D/tree/main/examples/platformer)** - A 2D platformer example built with Electron2D.",
            "E2D-BUILD-README-PLATFORMER-EXAMPLE",
            "README Examples section must contain only the public Platformer example link.",
            diagnostics);

        var examples = GetSection(readme, "## \U0001F3AE Examples");
        if (examples.Split('\n').Count(line => line.TrimStart().StartsWith("- ", StringComparison.Ordinal)) != 1)
        {
            diagnostics.Add(CreateError("E2D-BUILD-README-EXAMPLES-COUNT", "README Examples section must contain exactly one list item."));
        }
    }

    private static void VerifyFeedbackAndLicense(string readme, List<BuildDiagnostic> diagnostics)
    {
        RequireContains(readme, "https://github.com/edwardgushchin/Electron2D/issues", "E2D-BUILD-README-FEEDBACK-LINK-MISSING", "README must link to GitHub Issues.", diagnostics);
        RequireContains(readme, "https://github.com/edwardgushchin/Electron2D/pulls", "E2D-BUILD-README-FEEDBACK-LINK-MISSING", "README must link to GitHub Pull Requests.", diagnostics);
        RequireContains(readme, "[CONTRIBUTING.md](CONTRIBUTING.md)", "E2D-BUILD-README-CONTRIBUTING-LINK-MISSING", "README must link to CONTRIBUTING.md.", diagnostics);
        RequireContains(readme, "https://github.com/edwardgushchin/Electron2D/graphs/contributors", "E2D-BUILD-README-CONTRIBUTORS-LINK-MISSING", "README must link to the contributors graph.", diagnostics);
        RequireContains(readme, "[MIT License](LICENSE)", "E2D-BUILD-README-LICENSE-LINK-MISSING", "README must link to the MIT license.", diagnostics);
    }

    private static void VerifyForbiddenContent(string readme, List<BuildDiagnostic> diagnostics)
    {
        foreach (var fragment in ForbiddenFragments)
        {
            if (readme.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                diagnostics.Add(CreateError("E2D-BUILD-README-FORBIDDEN-CONTENT", $"README contains forbidden public wording or command fragment: {fragment}."));
            }
        }

        if (Regex.IsMatch(readme, @"\bT-\d{4}\b", RegexOptions.CultureInvariant))
        {
            diagnostics.Add(CreateError("E2D-BUILD-README-FORBIDDEN-CONTENT", "README must not contain repository task IDs."));
        }

        if (Regex.IsMatch(readme, @"(?m)^##\s+.*\bStatus\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        {
            diagnostics.Add(CreateError("E2D-BUILD-README-FORBIDDEN-SECTION", "README must not contain a Status section."));
        }

        if (Regex.IsMatch(readme, @"(?m)^##\s+.*\bRoadmap\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        {
            diagnostics.Add(CreateError("E2D-BUILD-README-FORBIDDEN-SECTION", "README must not contain a Roadmap section."));
        }
    }

    private void VerifyVisibleTagline(string readme, List<BuildDiagnostic> diagnostics)
    {
        var visibleText = ExtractVisibleMarkdownText(readme);
        var visibleTaglineCount = CountOccurrences(visibleText, ExpectedTagline);
        var assetTaglineContribution = VerifyReadmeAssetTaglines(readme, diagnostics);
        var renderedCount = visibleTaglineCount + assetTaglineContribution;

        if (renderedCount != 1)
        {
            diagnostics.Add(CreateError(
                "E2D-BUILD-README-TAGLINE-COUNT",
                $"README rendered visible content must contain tagline '{ExpectedTagline}' exactly once; found {renderedCount}."));
        }
    }

    private int VerifyReadmeAssetTaglines(string readme, List<BuildDiagnostic> diagnostics)
    {
        var allowlist = new HashSet<string>(StringComparer.Ordinal)
        {
            DarkReadmeAsset,
            LightReadmeAsset
        };
        var referencedSvgPaths = Regex.Matches(readme, @"data/assets/branding/readme/[^""'\s>]+\.svg", RegexOptions.CultureInvariant)
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var path in referencedSvgPaths)
        {
            if (!allowlist.Contains(path))
            {
                diagnostics.Add(CreateError("E2D-BUILD-README-ASSET-NOT-ALLOWED", $"README references non-allowlisted README SVG asset: {path}."));
            }
        }

        var taglineAssets = 0;
        foreach (var path in allowlist.Where(path => readme.Contains(path, StringComparison.Ordinal)))
        {
            if (!RepositoryPaths.TryResolveRepositoryPath(repositoryRoot, path, out var fullPath) || !File.Exists(fullPath))
            {
                diagnostics.Add(CreateError("E2D-BUILD-README-ASSET-MISSING", $"README brand asset was not found: {path}.", path));
                continue;
            }

            XDocument document;
            try
            {
                document = XDocument.Load(fullPath);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Xml.XmlException or IOException)
            {
                diagnostics.Add(CreateError("E2D-BUILD-README-ASSET-INVALID", $"README brand asset could not be parsed as SVG XML: {path}.", path));
                continue;
            }

            var labels = document
                .Descendants()
                .Select(element => element.Attribute("aria-label")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToArray();
            var count = labels.Count(label => string.Equals(NormalizeWhitespace(label), ExpectedTagline, StringComparison.OrdinalIgnoreCase));
            if (count != 1)
            {
                diagnostics.Add(CreateError("E2D-BUILD-README-ASSET-TAGLINE", $"README brand asset must contain exactly one visible tagline aria-label: {path}.", path));
            }
            else
            {
                taglineAssets++;
            }
        }

        return taglineAssets > 0 ? 1 : 0;
    }

    private static string ExtractVisibleMarkdownText(string markdown)
    {
        var text = Regex.Replace(markdown, @"(?s)<!--.*?-->", " ");
        text = Regex.Replace(text, @"!\[[^\]]*\]\([^)]+\)", " ");
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");
        text = Regex.Replace(text, @"<[^>]+>", " ");
        text = text.Replace("`", string.Empty).Replace("*", string.Empty).Replace("_", string.Empty);
        text = text.Replace("|", " ").Replace("#", " ");
        return NormalizeWhitespace(WebUtility.HtmlDecode(text));
    }

    private static string GetSection(string readme, string heading)
    {
        var start = readme.IndexOf(heading, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        var nextAnchor = readme.IndexOf("\n<a id=\"", start + heading.Length, StringComparison.Ordinal);
        var nextHeading = readme.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
        var end = new[] { nextAnchor, nextHeading }
            .Where(index => index >= 0)
            .DefaultIfEmpty(readme.Length)
            .Min();
        return readme[start..end];
    }

    private static void RequireContains(
        string text,
        string fragment,
        string code,
        string message,
        List<BuildDiagnostic> diagnostics)
    {
        if (text.IndexOf(fragment, StringComparison.Ordinal) < 0)
        {
            diagnostics.Add(CreateError(code, message));
        }
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
    }

    private static BuildDiagnostic CreateError(string code, string message, string? path = ReadmeRelativePath)
    {
        return new BuildDiagnostic(
            "verify",
            "verify readme",
            "error",
            code,
            message,
            Path: path);
    }
}
