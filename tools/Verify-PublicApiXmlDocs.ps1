<#
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
#>
param(
    [switch]$FailOnIssues
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src/Electron2D/Electron2D.csproj'
$workRoot = Join-Path $repoRoot '.temp/public-api-xml-docs'
$xmlPath = Join-Path $workRoot 'Electron2D.xml'
$reportPath = Join-Path $workRoot 'public-api-xml-docs-report.txt'
$inspectorRoot = Join-Path $workRoot 'Inspector'
$inspectorProject = Join-Path $inspectorRoot 'Inspector.csproj'
$inspectorSource = Join-Path $inspectorRoot 'Program.cs'

New-Item -ItemType Directory -Force -Path $workRoot, $inspectorRoot | Out-Null

$buildOutput = & dotnet build $projectPath --no-restore `
    '-p:GenerateDocumentationFile=true' `
    "-p:DocumentationFile=$xmlPath" 2>&1
$buildExitCode = $LASTEXITCODE
$buildText = $buildOutput -join [Environment]::NewLine
if ($buildExitCode -ne 0) {
    Write-Host $buildText
    exit $buildExitCode
}

$assemblyPath = Join-Path $repoRoot 'src/Electron2D/bin/Debug/net10.0/Electron2D.dll'
if (-not (Test-Path -LiteralPath $assemblyPath)) {
    throw "Built assembly was not found: $assemblyPath"
}

if (-not (Test-Path -LiteralPath $xmlPath)) {
    throw "XML documentation file was not generated: $xmlPath"
}

@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath $inspectorProject -Encoding UTF8

@'
using System.Reflection;
using System.Text;
using System.Xml.Linq;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: Inspector <assemblyPath> <xmlPath> <reportPath>");
    return 2;
}

var assemblyPath = args[0];
var xmlPath = args[1];
var reportPath = args[2];
var assembly = Assembly.LoadFrom(assemblyPath);
var xml = XDocument.Load(xmlPath);
var members = xml.Root?.Element("members")?.Elements("member")
    .Where(item => item.Attribute("name") is not null)
    .ToDictionary(item => item.Attribute("name")!.Value, item => item, StringComparer.Ordinal)
    ?? new Dictionary<string, XElement>(StringComparer.Ordinal);

var issues = new List<Issue>();
var checkedSymbols = 0;

foreach (var type in assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
{
    if (type.Assembly != assembly)
    {
        continue;
    }

    CheckSymbol("type", TypeId(type), type, requireThreadSafety: !type.IsEnum);
    CheckTypeParameters(TypeId(type), type.GetGenericArguments());

    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .OrderBy(field => field.Name, StringComparer.Ordinal))
    {
        if (field.IsSpecialName)
        {
            continue;
        }

        var id = "F:" + TypeName(type) + "." + field.Name;
        CheckSymbol(field.IsLiteral && type.IsEnum ? "enum-value" : "field", id, field, requireThreadSafety: false);
    }

    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .OrderBy(property => property.Name, StringComparer.Ordinal))
    {
        var id = "P:" + TypeName(type) + "." + property.Name;
        var element = CheckSymbol("property", id, property, requireThreadSafety: true);
        CheckValue(id, element);
        CheckParameters(id, element, property.GetIndexParameters());
    }

    foreach (var @event in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .OrderBy(@event => @event.Name, StringComparer.Ordinal))
    {
        var id = "E:" + TypeName(type) + "." + @event.Name;
        CheckSymbol("event", id, @event, requireThreadSafety: true);
    }

    foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .OrderBy(constructor => constructor.ToString(), StringComparer.Ordinal))
    {
        var id = MethodId(type, constructor, "#ctor");
        var element = CheckSymbol("constructor", id, constructor, requireThreadSafety: true);
        CheckParameters(id, element, constructor.GetParameters());
    }

    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(method => !method.IsSpecialName)
        .OrderBy(method => method.Name, StringComparer.Ordinal)
        .ThenBy(method => method.ToString(), StringComparer.Ordinal))
    {
        var id = MethodId(type, method, method.Name);
        var element = CheckSymbol("method", id, method, requireThreadSafety: true);
        CheckParameters(id, element, method.GetParameters());
        CheckTypeParameters(id, method.GetGenericArguments());
        if (method.ReturnType != typeof(void))
        {
            CheckReturns(id, element);
        }
    }
}

foreach (var item in members)
{
    var text = NormalizeText(item.Value.Value);
    if (ContainsForbiddenPublicWording(text))
    {
        issues.Add(new Issue("forbidden-wording", item.Key, "XML documentation contains forbidden public wording."));
    }

    if (ContainsPlaceholder(text))
    {
        issues.Add(new Issue("placeholder", item.Key, "XML documentation contains TODO/TBD placeholder text."));
    }
}

var report = new StringBuilder();
report.AppendLine("Electron2D public API XML documentation report");
report.AppendLine("Checked symbols: " + checkedSymbols.ToString(System.Globalization.CultureInfo.InvariantCulture));
report.AppendLine("Issues: " + issues.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
foreach (var issue in issues.OrderBy(issue => issue.Symbol, StringComparer.Ordinal).ThenBy(issue => issue.Code, StringComparer.Ordinal))
{
    report.AppendLine(issue.Code + ": " + issue.Symbol + " - " + issue.Message);
}

Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);
Console.Write(report.ToString());
return issues.Count == 0 ? 0 : 1;

XElement? CheckSymbol(string kind, string id, MemberInfo member, bool requireThreadSafety)
{
    checkedSymbols++;
    var element = FindMember(id);
    if (element is null)
    {
        issues.Add(new Issue("missing-doc", id, "Missing XML documentation for public " + kind + "."));
        return null;
    }

    if (IsBlank(element.Element("summary")))
    {
        issues.Add(new Issue("missing-summary", id, "Missing non-empty <summary>."));
    }

    if (requireThreadSafety && IsBlank(element.Element("threadsafety")))
    {
        issues.Add(new Issue("missing-threadsafety", id, "Missing non-empty <threadsafety>."));
    }

    if (IsBlank(element.Element("since")))
    {
        issues.Add(new Issue("missing-since", id, "Missing non-empty <since>."));
    }

    CheckSummaryShape(id, element.Element("summary"));
    CheckOptionalTextElement(id, "remarks", element.Element("remarks"));
    CheckOptionalTextElement(id, "threadsafety", element.Element("threadsafety"));
    CheckOptionalTextElement(id, "since", element.Element("since"));
    CheckSeeAlsoReferences(id, element);
    CheckExceptionReferences(id, element);
    CheckInheritDoc(id, element);

    return element;
}

void CheckParameters(string id, XElement? element, ParameterInfo[] parameters)
{
    if (element is null)
    {
        return;
    }

    foreach (var parameter in parameters)
    {
        var documented = element.Elements("param")
            .Any(item => string.Equals(item.Attribute("name")?.Value, parameter.Name, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(item.Value));
        if (!documented)
        {
            issues.Add(new Issue("missing-param", id, "Missing <param> for parameter '" + parameter.Name + "'."));
        }
    }
}

void CheckTypeParameters(string id, Type[] genericArguments)
{
    var element = FindMember(id);
    if (element is null)
    {
        return;
    }

    foreach (var argument in genericArguments)
    {
        var documented = element.Elements("typeparam")
            .Any(item => string.Equals(item.Attribute("name")?.Value, argument.Name, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(item.Value));
        if (!documented)
        {
            issues.Add(new Issue("missing-typeparam", id, "Missing <typeparam> for type parameter '" + argument.Name + "'."));
        }
    }
}

void CheckReturns(string id, XElement? element)
{
    if (element is not null && IsBlank(element.Element("returns")))
    {
        issues.Add(new Issue("missing-returns", id, "Missing non-empty <returns>."));
    }

    if (element is not null)
    {
        CheckOptionalTextElement(id, "returns", element.Element("returns"));
    }
}

void CheckValue(string id, XElement? element)
{
    if (element is not null && IsBlank(element.Element("value")))
    {
        issues.Add(new Issue("missing-value", id, "Missing non-empty <value>."));
    }

    if (element is not null)
    {
        CheckOptionalTextElement(id, "value", element.Element("value"));
    }
}

void CheckSummaryShape(string id, XElement? summary)
{
    if (summary is null)
    {
        return;
    }

    CheckOptionalTextElement(id, "summary", summary);
    var normalized = NormalizeText(summary.Value);
    var sentenceCount = normalized.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    if (sentenceCount > 1 && !summary.Elements("para").Any())
    {
        issues.Add(new Issue("summary-missing-para", id, "Multi-sentence <summary> must use <para> blocks."));
    }
}

void CheckOptionalTextElement(string id, string tagName, XElement? element)
{
    if (element is null)
    {
        return;
    }

    if (string.IsNullOrWhiteSpace(element.Value))
    {
        issues.Add(new Issue("empty-" + tagName, id, "Element <" + tagName + "> must not be empty when present."));
    }

    var text = NormalizeText(element.Value);
    if (ContainsForbiddenPublicWording(text))
    {
        issues.Add(new Issue("forbidden-wording", id, "Element <" + tagName + "> contains forbidden public wording."));
    }

    if (ContainsPlaceholder(text))
    {
        issues.Add(new Issue("placeholder", id, "Element <" + tagName + "> contains TODO/TBD placeholder text."));
    }
}

void CheckSeeAlsoReferences(string id, XElement element)
{
    foreach (var seeAlso in element.Elements("seealso"))
    {
        var cref = seeAlso.Attribute("cref")?.Value;
        var href = seeAlso.Attribute("href")?.Value;
        if (string.IsNullOrWhiteSpace(cref) && string.IsNullOrWhiteSpace(href))
        {
            issues.Add(new Issue("missing-seealso-reference", id, "<seealso> must include a cref or href target."));
        }
    }
}

void CheckExceptionReferences(string id, XElement element)
{
    foreach (var exception in element.Elements("exception"))
    {
        if (string.IsNullOrWhiteSpace(exception.Attribute("cref")?.Value))
        {
            issues.Add(new Issue("missing-exception-cref", id, "<exception> must include a cref target."));
        }

        if (string.IsNullOrWhiteSpace(exception.Value))
        {
            issues.Add(new Issue("empty-exception", id, "<exception> must explain when the exception is thrown."));
        }
    }
}

void CheckInheritDoc(string id, XElement element)
{
    if (element.Descendants("inheritdoc").Any() || element.Elements("inheritdoc").Any())
    {
        issues.Add(new Issue("inheritdoc", id, "Public API documentation must not rely on bare <inheritdoc /> in generated XML output."));
    }
}

XElement? FindMember(string id)
{
    if (members.TryGetValue(id, out var exact))
    {
        return exact;
    }

    var parameterIndex = id.IndexOf('(', StringComparison.Ordinal);
    if (parameterIndex > 0)
    {
        var prefix = id.Substring(0, parameterIndex);
        return members.FirstOrDefault(item => item.Key.StartsWith(prefix + "(", StringComparison.Ordinal)).Value;
    }

    return members.FirstOrDefault(item => item.Key.StartsWith(id + "(", StringComparison.Ordinal)).Value;
}

static string TypeId(Type type) => "T:" + TypeName(type);

static string TypeName(Type type)
{
    var name = type.FullName ?? type.Name;
    return name.Replace('+', '.');
}

static string MethodId(Type declaringType, MethodBase method, string methodName)
{
    var name = methodName;
    if (method.IsGenericMethod)
    {
        name += "``" + method.GetGenericArguments().Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    var parameters = method.GetParameters();
    var id = "M:" + TypeName(declaringType) + "." + name;
    if (parameters.Length == 0)
    {
        return id;
    }

    return id + "(" + string.Join(",", parameters.Select(parameter => ParameterTypeName(parameter.ParameterType))) + ")";
}

static string ParameterTypeName(Type type)
{
    if (type.IsByRef)
    {
        return ParameterTypeName(type.GetElementType()!) + "@";
    }

    if (type.IsArray)
    {
        return ParameterTypeName(type.GetElementType()!) + "[]";
    }

    if (type.IsGenericParameter)
    {
        return "`" + type.GenericParameterPosition.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    if (type.IsGenericType)
    {
        var definitionName = (type.GetGenericTypeDefinition().FullName ?? type.Name).Replace('+', '.');
        var tickIndex = definitionName.IndexOf('`', StringComparison.Ordinal);
        if (tickIndex >= 0)
        {
            definitionName = definitionName.Substring(0, tickIndex);
        }

        return definitionName + "{" + string.Join(",", type.GetGenericArguments().Select(ParameterTypeName)) + "}";
    }

    return (type.FullName ?? type.Name).Replace('+', '.');
}

static bool IsBlank(XElement? element) => element is null || string.IsNullOrWhiteSpace(element.Value);

static string NormalizeText(string value) => value.Replace("\r", " ").Replace("\n", " ");

static bool ContainsPlaceholder(string value)
{
    return value.Contains("TODO", StringComparison.OrdinalIgnoreCase)
        || value.Contains("TBD", StringComparison.OrdinalIgnoreCase);
}

static bool ContainsForbiddenPublicWording(string value)
{
    return value.Contains("SDL", StringComparison.Ordinal)
        || value.Contains("SDL3", StringComparison.Ordinal)
        || value.Contains("SDL_GPU", StringComparison.Ordinal)
        || value.Contains("SDL_Renderer", StringComparison.Ordinal)
        || value.Contains("SDL_ttf", StringComparison.Ordinal)
        || value.Contains("SDL_mixer", StringComparison.Ordinal)
        || value.Contains("SDL_shadercross", StringComparison.Ordinal)
        || value.Contains("Simple DirectMedia", StringComparison.OrdinalIgnoreCase)
        || value.Contains("Godot-like", StringComparison.OrdinalIgnoreCase)
        || value.Contains("Godot-подоб", StringComparison.OrdinalIgnoreCase);
}

public sealed record Issue(string Code, string Symbol, string Message);
'@ | Set-Content -LiteralPath $inspectorSource -Encoding UTF8

$inspectorOutput = & dotnet run --project $inspectorProject -- $assemblyPath $xmlPath $reportPath 2>&1
$inspectorExitCode = $LASTEXITCODE
$inspectorText = $inspectorOutput -join [Environment]::NewLine
Write-Host $inspectorText

if ($FailOnIssues -and $inspectorExitCode -ne 0) {
    Write-Host "Public API XML documentation verification failed. Report: $reportPath"
    exit $inspectorExitCode
}

if ($inspectorExitCode -ne 0) {
    Write-Host "Public API XML documentation report contains issues. Report: $reportPath"
    Write-Host 'Run with -FailOnIssues to make issues fail the command.'
    exit 0
}

Write-Host "Public API XML documentation verification passed. Report: $reportPath"
