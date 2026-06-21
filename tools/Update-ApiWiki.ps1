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
    [string]$OutputPath,
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src/Electron2D/Electron2D.csproj'
$workRoot = Join-Path $repoRoot '.temp/api-wiki'
$xmlPath = Join-Path $workRoot 'Electron2D.xml'
$expectedWikiRoot = Join-Path $workRoot 'expected-wiki'
$generatorRoot = Join-Path $workRoot 'Generator'
$generatorProject = Join-Path $generatorRoot 'Generator.csproj'
$generatorSource = Join-Path $generatorRoot 'Program.cs'
$hasOutputPath = $PSBoundParameters.ContainsKey('OutputPath') -and -not [string]::IsNullOrWhiteSpace($OutputPath)
$targetWikiRoot = if ($hasOutputPath) {
    if ([System.IO.Path]::IsPathRooted($OutputPath)) {
        [System.IO.Path]::GetFullPath($OutputPath)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputPath))
    }
}
else {
    Join-Path $workRoot 'generated-wiki'
}

New-Item -ItemType Directory -Force -Path $workRoot, $expectedWikiRoot, $generatorRoot | Out-Null

$buildOutput = & dotnet build $projectPath --no-restore `
    '-p:GenerateDocumentationFile=true' `
    "-p:DocumentationFile=$xmlPath" 2>&1
$buildExitCode = $LASTEXITCODE
if ($buildExitCode -ne 0) {
    Write-Host ($buildOutput -join [Environment]::NewLine)
    exit $buildExitCode
}

$assemblyPath = Join-Path $repoRoot 'src/Electron2D/bin/Debug/net10.0/Electron2D.dll'
if (-not (Test-Path -LiteralPath $assemblyPath)) {
    throw "Built assembly was not found: $assemblyPath"
}

if (-not (Test-Path -LiteralPath $xmlPath)) {
    throw "XML documentation file was not generated: $xmlPath"
}

if (Test-Path -LiteralPath $expectedWikiRoot) {
    Remove-Item -LiteralPath $expectedWikiRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $expectedWikiRoot | Out-Null

@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath $generatorProject -Encoding UTF8

@'
using System.Reflection;
using System.Text;
using System.Xml.Linq;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: Generator <assemblyPath> <xmlPath> <wikiRoot>");
    return 2;
}

var assemblyPath = args[0];
var xmlPath = args[1];
var wikiRoot = args[2];
var assembly = Assembly.LoadFrom(assemblyPath);
var xml = XDocument.Load(xmlPath);
var docs = xml.Root?.Element("members")?.Elements("member")
    .Where(item => item.Attribute("name") is not null)
    .ToDictionary(item => item.Attribute("name")!.Value, item => item, StringComparer.Ordinal)
    ?? new Dictionary<string, XElement>(StringComparer.Ordinal);

var publicTypes = assembly.GetExportedTypes()
    .Where(type => type.Assembly == assembly)
    .OrderBy(type => DisplayName(type), StringComparer.Ordinal)
    .ToArray();

var categories = ApiCategories();
var pageFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var type in publicTypes)
{
    if (!pageFileNames.Add(PageFileName(type)))
    {
        throw new InvalidOperationException("Duplicate generated Wiki page name: " + PageFileName(type));
    }
}

var uncategorizedTypes = publicTypes
    .Where(type => !categories.Any(category => category.Matches(type)))
    .Select(ShortDisplayName)
    .ToArray();
if (uncategorizedTypes.Length > 0)
{
    throw new InvalidOperationException("Public types are missing API categories: " + string.Join(", ", uncategorizedTypes));
}

Directory.CreateDirectory(wikiRoot);
WriteText("Home.md", RenderHome(publicTypes, categories));
WriteText("_Sidebar.md", RenderSidebar());
WriteText("_Footer.md", RenderFooter(publicTypes));
WriteText("API-by-Category.md", RenderApiByCategory(publicTypes, categories));
WriteText("API-Reference.md", RenderApiReference(publicTypes));
foreach (var category in categories)
{
    WriteText(category.PageFileName, RenderCategoryPage(category, publicTypes));
}

foreach (var type in publicTypes)
{
    WriteText(PageFileName(type), RenderTypePage(type));
}

return 0;

string RenderHome(IReadOnlyList<Type> types, IReadOnlyList<ApiCategory> categories)
{
    var builder = NewGeneratedPage();
    builder.AppendLine("Electron2D is an AI-friendly 2D game engine for C# and .NET. The `0.1.0 Preview` line focuses on a clean runtime API, deterministic project tooling and documentation that can be read by both developers and coding agents.");
    builder.AppendLine();
    builder.AppendLine("This Wiki is the public documentation hub for the preview API surface. It is generated from the compiled runtime assembly and XML documentation comments, so the reference follows the code that ships in the engine package.");
    builder.AppendLine();
    builder.AppendLine("## What is Electron2D?");
    builder.AppendLine();
    builder.AppendLine("Electron2D provides a compact 2D runtime model: objects, nodes, resources, scenes, 2D math, input events, rendering-facing nodes, animation, UI/text primitives and an initial 2D physics surface. The project is being built for desktop-first development with a release path toward packaged tools, editor workflows and agent-assisted game creation.");
    builder.AppendLine();
    builder.AppendLine("## Start here");
    builder.AppendLine();
    builder.AppendLine("- [API by Category](API-by-Category) - browse the runtime API by domain.");
    builder.AppendLine("- [Complete API Index](API-Reference) - alphabetical index of every public type.");
    builder.AppendLine("- [API Compatibility](API-Compatibility) - preview support status and planned surface.");
    builder.AppendLine();
    builder.AppendLine("## API documentation");
    builder.AppendLine();
    builder.AppendLine("| Area | Description | Types |");
    builder.AppendLine("| --- | --- | ---: |");
    foreach (var category in categories)
    {
        var count = CountTypes(category, types);
        builder.AppendLine("| [" + EscapeTable(category.Title) + "](" + PageStem(category.PageFileName) + ") | " + EscapeTable(category.Description) + " | `" + count.ToString(System.Globalization.CultureInfo.InvariantCulture) + "` |");
    }
    builder.AppendLine();
    builder.AppendLine("## Preview status");
    builder.AppendLine();
    builder.AppendLine("| Item | Status |");
    builder.AppendLine("| --- | --- |");
    builder.AppendLine("| Runtime API | `" + types.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + "` public types generated from the current assembly |");
    builder.AppendLine("| Documentation source | XML comments and compiled public surface |");
    builder.AppendLine("| Navigation | Category pages, complete index and focused common API sidebar |");
    builder.AppendLine("| Release line | `0.1.0 Preview` |");
    builder.AppendLine();
    builder.AppendLine("## AI-friendly workflow");
    builder.AppendLine();
    builder.AppendLine("The documentation is generated in a stable structure so humans and coding agents can link to the same pages, compare public types, and avoid guessing which preview APIs exist.");
    builder.AppendLine();
    builder.AppendLine("The compatibility page marks which APIs are supported, partial, experimental, planned or intentionally excluded for this preview release.");
    return builder.ToString();
}

string RenderApiReference(IReadOnlyList<Type> types)
{
    var builder = NewPage("API Reference");
    builder.AppendLine();
    builder.AppendLine("Complete generated public type index for the Electron2D runtime assembly.");
    builder.AppendLine();
    builder.AppendLine("- [API by Category](API-by-Category)");
    builder.AppendLine("- [API Compatibility](API-Compatibility)");
    builder.AppendLine();
    builder.AppendLine("## Type Index");
    builder.AppendLine();

    foreach (var group in types.GroupBy(type => type.Namespace ?? string.Empty).OrderBy(group => group.Key, StringComparer.Ordinal))
    {
        builder.AppendLine("### `" + (group.Key.Length == 0 ? "<global>" : group.Key) + "`");
        builder.AppendLine();
        builder.AppendLine("| Type | Kind | Summary |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var type in group)
        {
            builder.AppendLine("| [" + EscapeTable(ShortDisplayName(type)) + "](" + PageLink(type) + ") | " +
                EscapeTable(TypeKind(type)) + " | " + EscapeTable(PlainSummary(TypeId(type))) + " |");
        }
        builder.AppendLine();
    }

    return builder.ToString();
}

string RenderApiByCategory(IReadOnlyList<Type> types, IReadOnlyList<ApiCategory> categories)
{
    var builder = NewPage("API by Category");
    builder.AppendLine();
    builder.AppendLine("Browse the generated runtime API by domain. Use the complete index when you already know a type name.");
    builder.AppendLine();
    builder.AppendLine("| Category | Description | Types |");
    builder.AppendLine("| --- | --- | ---: |");
    foreach (var category in categories)
    {
        builder.AppendLine("| [" + EscapeTable(category.Title) + "](" + PageStem(category.PageFileName) + ") | " +
            EscapeTable(category.Description) + " | `" +
            CountTypes(category, types).ToString(System.Globalization.CultureInfo.InvariantCulture) + "` |");
    }

    return builder.ToString();
}

string RenderCategoryPage(ApiCategory category, IReadOnlyList<Type> types)
{
    var categoryTypes = types
        .Where(category.Matches)
        .OrderBy(type => ShortDisplayName(type), StringComparer.Ordinal)
        .ToArray();

    var builder = NewPage(category.Title);
    builder.AppendLine();
    builder.AppendLine(category.Description);
    builder.AppendLine();
    builder.AppendLine("This category currently contains `" + categoryTypes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "` public types.");
    builder.AppendLine();
    builder.AppendLine("| Type | Kind | Summary |");
    builder.AppendLine("| --- | --- | --- |");
    foreach (var type in categoryTypes)
    {
        builder.AppendLine("| [" + EscapeTable(ShortDisplayName(type)) + "](" + PageLink(type) + ") | " +
            EscapeTable(TypeKind(type)) + " | " + EscapeTable(PlainSummary(TypeId(type))) + " |");
    }

    return builder.ToString();
}

string RenderSidebar()
{
    var builder = NewGeneratedPage();
    builder.AppendLine();
    builder.AppendLine("# Electron2D");
    builder.AppendLine();
    builder.AppendLine("- [Home](Home)");
    builder.AppendLine("- [API by Category](API-by-Category)");
    builder.AppendLine("- [Complete API Index](API-Reference)");
    builder.AppendLine("- [API Compatibility](API-Compatibility)");
    builder.AppendLine();
    builder.AppendLine("## API Areas");
    builder.AppendLine();
    foreach (var category in categories)
    {
        builder.AppendLine("- [" + category.Title + "](" + PageStem(category.PageFileName) + ")");
    }
    builder.AppendLine();
    builder.AppendLine("## Common API");
    builder.AppendLine();
    builder.AppendLine("- [Object](Object)");
    builder.AppendLine("- [Node](Node)");
    builder.AppendLine("- [Node2D](Node2D)");
    builder.AppendLine("- [SceneTree](SceneTree)");
    builder.AppendLine("- [Resource](Resource)");
    builder.AppendLine("- [Vector2](Vector2)");
    builder.AppendLine("- [Input](Input)");
    builder.AppendLine("- [Sprite2D](Sprite2D)");
    builder.AppendLine("- [Area2D](Area2D)");
    builder.AppendLine("- [RigidBody2D](RigidBody2D)");
    return builder.ToString();
}

string RenderFooter(IReadOnlyList<Type> types)
{
    var builder = NewGeneratedPage();
    builder.AppendLine();
    builder.AppendLine("Electron2D `0.1.0 Preview` API reference. Generated from `" + types.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + "` public runtime types.");
    return builder.ToString();
}

IReadOnlyList<ApiCategory> ApiCategories() =>
[
    new ApiCategory(
        "API-Core.md",
        "Core",
        "Object lifetime, identity, names, callable values and low-level result types.",
        type => Named(type, "Object", "RefCounted", "Callable", "Error", "ConnectFlags", "Rid", "StringName")),
    new ApiCategory(
        "API-Scene-Tree.md",
        "Scene Tree",
        "Nodes, node paths, scene packing and scene traversal.",
        type => Named(type, "Node", "Node2D", "NodePath", "PackedScene", "SceneTree")),
    new ApiCategory(
        "API-Resources.md",
        "Resources",
        "Resource base types and stable resource identifiers.",
        type => Named(type, "Resource", "ResourceUid")),
    new ApiCategory(
        "API-Math-and-Data.md",
        "Math and Data",
        "2D math value types, color, random number generation, variants and collection values.",
        type => Named(type, "Mathf", "Vector2", "Vector2I", "Rect2", "Rect2I", "Transform2D", "Color", "RandomNumberGenerator", "Variant") ||
            ShortDisplayName(type).StartsWith("Collections.", StringComparison.Ordinal)),
    new ApiCategory(
        "API-Input.md",
        "Input",
        "Input state, input maps, keyboard, mouse, gamepad and touch event types.",
        type => Named(type, "Input", "InputMap", "InputEvent", "Key", "KeyLocation", "MouseButton", "MouseButtonMask", "JoyAxis", "JoyButton") ||
            ShortDisplayName(type).StartsWith("InputEvent", StringComparison.Ordinal)),
    new ApiCategory(
        "API-Display-and-Localization.md",
        "Display and Localization",
        "Display state, orientation requests, virtual keyboard state and translations.",
        type => Named(type, "DisplayServer", "Translation", "TranslationServer")),
    new ApiCategory(
        "API-Rendering.md",
        "Rendering",
        "2D drawing nodes, textures, tile maps, viewports, cameras, materials, shaders and rendering server state.",
        type => Named(type, "AtlasTexture", "Texture2D", "TileData", "TileMapLayer", "TileSet", "TileSetAtlasSource", "TileSetSource", "CanvasItem", "CanvasLayer", "Camera2D", "Viewport", "ViewportTexture", "Sprite2D", "RenderingServer", "Material", "Shader", "ShaderMaterial")),
    new ApiCategory(
        "API-Animation-and-Tweening.md",
        "Animation and Tweening",
        "Frame animation, animation resources, playback nodes and tween sequences.",
        type => Named(type, "AnimatedSprite2D", "Animation", "AnimationLibrary", "AnimationPlayer", "SpriteFrames", "Tween", "Tweener", "CallbackTweener", "IntervalTweener", "PropertyTweener")),
    new ApiCategory(
        "API-Audio.md",
        "Audio",
        "Audio resources, playback nodes and audio server state.",
        type => Named(type, "AudioServer", "AudioStream", "AudioStreamPlayer", "AudioStreamPlayer2D")),
    new ApiCategory(
        "API-Physics.md",
        "Physics",
        "2D physics bodies, areas, shapes, query parameters, collisions and physics server boundaries.",
        type => Named(type, "World2D", "Area2D", "CollisionObject2D", "CollisionShape2D", "Shape2D", "CapsuleShape2D", "CircleShape2D", "ConcavePolygonShape2D", "ConvexPolygonShape2D", "RectangleShape2D", "SegmentShape2D", "PhysicsBody2D", "PhysicsDirectSpaceState2D", "PhysicsMaterial", "PhysicsPointQueryParameters2D", "PhysicsRayQueryParameters2D", "PhysicsServer2D", "PhysicsShapeQueryParameters2D", "RayCast2D", "StaticBody2D", "RigidBody2D", "CharacterBody2D", "KinematicCollision2D")),
    new ApiCategory(
        "API-UI-and-Text.md",
        "UI and Text",
        "UI controls, labels, fonts and text alignment values.",
        type => Named(type, "Control", "FocusMode", "Label", "Font", "HorizontalAlignment", "MouseFilter", "VerticalAlignment")),
    new ApiCategory(
        "API-Scripting-Metadata.md",
        "Scripting Metadata",
        "Attributes used by scripts, serialization metadata and editor-facing script annotations.",
        type => Named(type, "ExportAttribute", "SignalAttribute", "ToolAttribute")),
];

bool Named(Type type, params string[] names)
{
    var shortName = ShortDisplayName(type);
    return names.Any(name =>
        string.Equals(shortName, name, StringComparison.Ordinal) ||
        shortName.StartsWith(name + ".", StringComparison.Ordinal));
}

ApiCategory CategoryFor(Type type, IReadOnlyList<ApiCategory> allCategories) =>
    allCategories.First(category => category.Matches(type));

int CountTypes(ApiCategory category, IReadOnlyList<Type> types) =>
    types.Count(category.Matches);

string RenderTypePage(Type type)
{
    var builder = NewPage(ShortDisplayName(type));
    var typeDoc = FindDoc(TypeId(type));
    var category = CategoryFor(type, categories);
    builder.AppendLine();
    builder.AppendLine("| Field | Value |");
    builder.AppendLine("| --- | --- |");
    builder.AppendLine("| Full name | `" + DisplayName(type) + "` |");
    builder.AppendLine("| Namespace | `" + (type.Namespace ?? string.Empty) + "` |");
    builder.AppendLine("| Kind | `" + TypeKind(type) + "` |");
    builder.AppendLine("| Category | [" + EscapeTable(category.Title) + "](" + PageStem(category.PageFileName) + ") |");
    builder.AppendLine();
    builder.AppendLine("## Overview");
    builder.AppendLine();
    AppendDocSection(builder, typeDoc?.Element("summary"));
    builder.AppendLine();
    builder.AppendLine("## Syntax");
    builder.AppendLine();
    builder.AppendLine("```csharp");
    builder.AppendLine(TypeDeclaration(type));
    builder.AppendLine("```");

    AppendOptionalDocBlock(builder, "Remarks", typeDoc?.Element("remarks"));
    AppendOptionalDocBlock(builder, "Thread Safety", typeDoc?.Element("threadsafety"));
    AppendOptionalDocBlock(builder, "Since", typeDoc?.Element("since"));
    AppendSeeAlso(builder, typeDoc);

    var members = GetDocumentedMembers(type);
    if (members.Count > 0)
    {
        builder.AppendLine();
        builder.AppendLine("## Members");
        builder.AppendLine();
        builder.AppendLine("| Member | Kind | Summary |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var member in members)
        {
            builder.AppendLine("| [`" + EscapeTable(member.DisplayName) + "`](#" + Anchor(member.DisplayName) + ") | " +
                EscapeTable(member.Kind) + " | " + EscapeTable(PlainSummary(member.XmlId)) + " |");
        }

        builder.AppendLine();
        builder.AppendLine("## Member Details");
        foreach (var member in members)
        {
            AppendMemberDetails(builder, member);
        }
    }

    return builder.ToString();
}

void AppendMemberDetails(StringBuilder builder, ApiMember member)
{
    var doc = FindDoc(member.XmlId);
    builder.AppendLine();
    builder.AppendLine("### " + member.DisplayName);
    builder.AppendLine();
    builder.AppendLine("Kind: `" + member.Kind + "`");
    builder.AppendLine();
    builder.AppendLine("```csharp");
    builder.AppendLine(member.Signature);
    builder.AppendLine("```");

    AppendOptionalDocBlock(builder, "Summary", doc?.Element("summary"));
    AppendOptionalDocBlock(builder, "Remarks", doc?.Element("remarks"));
    AppendParameters(builder, doc, "Parameters", "param", "name");
    AppendParameters(builder, doc, "Type Parameters", "typeparam", "name");
    AppendOptionalDocBlock(builder, "Returns", doc?.Element("returns"));
    AppendOptionalDocBlock(builder, "Value", doc?.Element("value"));
    AppendExceptions(builder, doc);
    AppendOptionalDocBlock(builder, "Thread Safety", doc?.Element("threadsafety"));
    AppendOptionalDocBlock(builder, "Since", doc?.Element("since"));
    AppendSeeAlso(builder, doc);
}

IReadOnlyList<ApiMember> GetDocumentedMembers(Type type)
{
    var members = new List<ApiMember>();

    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(field => !field.IsSpecialName)
        .OrderBy(field => field.Name, StringComparer.Ordinal))
    {
        var id = "F:" + XmlTypeName(type) + "." + field.Name;
        members.Add(new ApiMember(
            field.IsLiteral && type.IsEnum ? "Enum value" : "Field",
            field.Name,
            FieldSignature(field),
            id));
    }

    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .OrderBy(property => property.Name, StringComparer.Ordinal))
    {
        var id = "P:" + XmlTypeName(type) + "." + property.Name;
        members.Add(new ApiMember("Property", property.Name, PropertySignature(property), id));
    }

    foreach (var @event in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .OrderBy(@event => @event.Name, StringComparer.Ordinal))
    {
        var id = "E:" + XmlTypeName(type) + "." + @event.Name;
        members.Add(new ApiMember("Event", @event.Name, EventSignature(@event), id));
    }

    foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .OrderBy(constructor => constructor.ToString(), StringComparer.Ordinal))
    {
        var id = MethodId(type, constructor, "#ctor");
        members.Add(new ApiMember("Constructor", ConstructorDisplayName(type, constructor), ConstructorSignature(type, constructor), id));
    }

    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(method => !method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal))
        .OrderBy(method => method.Name, StringComparer.Ordinal)
        .ThenBy(method => method.ToString(), StringComparer.Ordinal))
    {
        var id = MethodId(type, method, method.Name);
        members.Add(new ApiMember("Method", MethodDisplayName(method), MethodSignature(method), id));
    }

    return members;
}

void AppendOptionalDocBlock(StringBuilder builder, string heading, XElement? element)
{
    if (element is null || string.IsNullOrWhiteSpace(element.Value))
    {
        return;
    }

    builder.AppendLine();
    builder.AppendLine("#### " + heading);
    builder.AppendLine();
    AppendDocSection(builder, element);
}

void AppendDocSection(StringBuilder builder, XElement? element)
{
    if (element is null || string.IsNullOrWhiteSpace(element.Value))
    {
        builder.AppendLine("No XML documentation text was provided.");
        return;
    }

    var text = Markdown(element).Trim();
    builder.AppendLine(string.IsNullOrWhiteSpace(text) ? "No XML documentation text was provided." : text);
}

void AppendParameters(StringBuilder builder, XElement? doc, string heading, string elementName, string attributeName)
{
    var parameters = doc?.Elements(elementName)
        .Where(item => !string.IsNullOrWhiteSpace(item.Attribute(attributeName)?.Value) && !string.IsNullOrWhiteSpace(item.Value))
        .ToArray() ?? Array.Empty<XElement>();
    if (parameters.Length == 0)
    {
        return;
    }

    builder.AppendLine();
    builder.AppendLine("#### " + heading);
    builder.AppendLine();
    foreach (var parameter in parameters)
    {
        builder.AppendLine("- `" + parameter.Attribute(attributeName)!.Value + "`: " + Markdown(parameter).Trim());
    }
}

void AppendExceptions(StringBuilder builder, XElement? doc)
{
    var exceptions = doc?.Elements("exception")
        .Where(item => !string.IsNullOrWhiteSpace(item.Attribute("cref")?.Value) && !string.IsNullOrWhiteSpace(item.Value))
        .ToArray() ?? Array.Empty<XElement>();
    if (exceptions.Length == 0)
    {
        return;
    }

    builder.AppendLine();
    builder.AppendLine("#### Exceptions");
    builder.AppendLine();
    foreach (var exception in exceptions)
    {
        builder.AppendLine("- `" + CrefDisplay(exception.Attribute("cref")!.Value) + "`: " + Markdown(exception).Trim());
    }
}

void AppendSeeAlso(StringBuilder builder, XElement? doc)
{
    var seeAlso = doc?.Elements("seealso")
        .Select(item => item.Attribute("cref")?.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => CrefDisplay(value!))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToArray() ?? Array.Empty<string>();
    if (seeAlso.Length == 0)
    {
        return;
    }

    builder.AppendLine();
    builder.AppendLine("#### See Also");
    builder.AppendLine();
    foreach (var item in seeAlso)
    {
        builder.AppendLine("- `" + item + "`");
    }
}

StringBuilder NewPage(string title)
{
    var builder = NewGeneratedPage();
    builder.AppendLine();
    builder.AppendLine("[Home](Home) | [API by Category](API-by-Category) | [Complete API Index](API-Reference) | [API Compatibility](API-Compatibility)");
    builder.AppendLine();
    builder.AppendLine("<!-- Page title: " + title + " -->");
    return builder;
}

StringBuilder NewGeneratedPage()
{
    var builder = new StringBuilder();
    builder.AppendLine("<!-- Generated by tools/Update-ApiWiki.ps1. Do not edit by hand. -->");
    return builder;
}

void WriteText(string relativePath, string content)
{
    var path = Path.Combine(wikiRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, NormalizeNewlines(content), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

string NormalizeNewlines(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd() + "\n";

XElement? FindDoc(string id)
{
    if (docs.TryGetValue(id, out var exact))
    {
        return exact;
    }

    var parameterIndex = id.IndexOf('(', StringComparison.Ordinal);
    if (parameterIndex > 0)
    {
        var prefix = id.Substring(0, parameterIndex);
        return docs.FirstOrDefault(item => item.Key.StartsWith(prefix + "(", StringComparison.Ordinal)).Value;
    }

    return docs.FirstOrDefault(item => item.Key.StartsWith(id + "(", StringComparison.Ordinal)).Value;
}

string PlainSummary(string id)
{
    var summary = FindDoc(id)?.Element("summary");
    if (summary is null || string.IsNullOrWhiteSpace(summary.Value))
    {
        return string.Empty;
    }

    return NormalizeWhitespace(Markdown(summary));
}

string Markdown(XElement element)
{
    var builder = new StringBuilder();
    foreach (var node in element.Nodes())
    {
        AppendMarkdownNode(builder, node);
    }

    return NormalizeParagraphSpacing(builder.ToString());
}

void AppendMarkdownNode(StringBuilder builder, XNode node)
{
    if (node is XText text)
    {
        builder.Append(text.Value);
        return;
    }

    if (node is not XElement element)
    {
        return;
    }

    switch (element.Name.LocalName)
    {
        case "para":
            builder.AppendLine();
            builder.AppendLine();
            foreach (var child in element.Nodes())
            {
                AppendMarkdownNode(builder, child);
            }
            builder.AppendLine();
            break;
        case "see":
        case "seealso":
            var cref = element.Attribute("cref")?.Value;
            builder.Append(cref is null ? element.Value : "`" + CrefDisplay(cref) + "`");
            break;
        case "paramref":
        case "typeparamref":
            var name = element.Attribute("name")?.Value;
            builder.Append(string.IsNullOrWhiteSpace(name) ? element.Value : "`" + name + "`");
            break;
        case "c":
            builder.Append("`" + element.Value.Trim() + "`");
            break;
        default:
            foreach (var child in element.Nodes())
            {
                AppendMarkdownNode(builder, child);
            }
            break;
    }
}

string NormalizeParagraphSpacing(string value)
{
    var lines = value.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
    var result = new List<string>();
    var previousBlank = false;
    foreach (var rawLine in lines)
    {
        var line = NormalizeWhitespace(rawLine);
        var blank = string.IsNullOrWhiteSpace(line);
        if (blank && previousBlank)
        {
            continue;
        }

        result.Add(line);
        previousBlank = blank;
    }

    return string.Join(Environment.NewLine, result).Trim();
}

string NormalizeWhitespace(string value)
{
    var builder = new StringBuilder();
    var inWhitespace = false;
    foreach (var c in value.Trim())
    {
        if (char.IsWhiteSpace(c))
        {
            if (!inWhitespace)
            {
                builder.Append(' ');
            }
            inWhitespace = true;
        }
        else
        {
            builder.Append(c);
            inWhitespace = false;
        }
    }

    return builder.ToString();
}

static string TypeDeclaration(Type type)
{
    if (typeof(MulticastDelegate).IsAssignableFrom(type.BaseType))
    {
        var invoke = type.GetMethod("Invoke")!;
        return "public delegate " + TypeDisplayName(invoke.ReturnType) + " " + type.Name + "(" + Parameters(invoke.GetParameters()) + ");";
    }

    var modifiers = new List<string> { "public" };
    if (type.IsAbstract && type.IsSealed)
    {
        modifiers.Add("static");
    }
    else
    {
        if (type.IsAbstract && !type.IsInterface)
        {
            modifiers.Add("abstract");
        }

        if (type.IsSealed && !type.IsValueType && !type.IsEnum)
        {
            modifiers.Add("sealed");
        }
    }

    var kind = type.IsInterface ? "interface" : type.IsEnum ? "enum" : type.IsValueType ? "struct" : "class";
    var baseTypes = new List<string>();
    if (type.BaseType is not null && type.BaseType != typeof(object) && !type.IsEnum && !type.IsValueType)
    {
        baseTypes.Add(TypeDisplayName(type.BaseType));
    }
    baseTypes.AddRange(type.GetInterfaces().Where(iface => iface.IsPublic).Select(TypeDisplayName).OrderBy(name => name, StringComparer.Ordinal));

    return string.Join(" ", modifiers) + " " + kind + " " + TypeDisplayName(type) +
        (baseTypes.Count == 0 ? string.Empty : " : " + string.Join(", ", baseTypes));
}

static string FieldSignature(FieldInfo field)
{
    var modifiers = field.IsLiteral ? "public const " : field.IsStatic ? "public static " : "public ";
    return modifiers + TypeDisplayName(field.FieldType) + " " + field.Name;
}

static string PropertySignature(PropertyInfo property)
{
    var accessors = new List<string>();
    if (property.GetMethod is not null && property.GetMethod.IsPublic)
    {
        accessors.Add("get;");
    }
    if (property.SetMethod is not null && property.SetMethod.IsPublic)
    {
        accessors.Add("set;");
    }

    var indexParameters = property.GetIndexParameters();
    var name = indexParameters.Length == 0
        ? property.Name
        : "this[" + Parameters(indexParameters) + "]";
    return "public " + TypeDisplayName(property.PropertyType) + " " + name + " { " + string.Join(" ", accessors) + " }";
}

static string EventSignature(EventInfo @event) => "public event " + TypeDisplayName(@event.EventHandlerType!) + " " + @event.Name;

static string ConstructorSignature(Type type, ConstructorInfo constructor) => "public " + TypeDisplayName(type) + "(" + Parameters(constructor.GetParameters()) + ")";

static string MethodSignature(MethodInfo method)
{
    var modifiers = method.IsStatic ? "public static " : "public ";
    return modifiers + TypeDisplayName(method.ReturnType) + " " + MethodDisplayName(method) + "(" + Parameters(method.GetParameters()) + ")";
}

static string ConstructorDisplayName(Type type, ConstructorInfo constructor) => TypeDisplayName(type) + "(" + Parameters(constructor.GetParameters(), includeTypesOnly: true) + ")";

static string MethodDisplayName(MethodInfo method) => method.Name + "(" + Parameters(method.GetParameters(), includeTypesOnly: true) + ")";

static string Parameters(ParameterInfo[] parameters, bool includeTypesOnly = false)
{
    return string.Join(", ", parameters.Select(parameter =>
        includeTypesOnly
            ? TypeDisplayName(parameter.ParameterType)
            : TypeDisplayName(parameter.ParameterType) + " " + parameter.Name));
}

static string TypeDisplayName(Type type)
{
    if (type.IsByRef)
    {
        return TypeDisplayName(type.GetElementType()!) + "&";
    }

    if (type.IsArray)
    {
        return TypeDisplayName(type.GetElementType()!) + "[]";
    }

    if (type.IsGenericParameter)
    {
        return type.Name;
    }

    var nullableType = Nullable.GetUnderlyingType(type);
    if (nullableType is not null)
    {
        return TypeDisplayName(nullableType) + "?";
    }

    if (!type.IsGenericType)
    {
        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    var definitionName = (type.GetGenericTypeDefinition().FullName ?? type.Name).Replace('+', '.');
    var tickIndex = definitionName.IndexOf('`', StringComparison.Ordinal);
    if (tickIndex >= 0)
    {
        definitionName = definitionName.Substring(0, tickIndex);
    }

    return definitionName + "<" + string.Join(", ", type.GetGenericArguments().Select(TypeDisplayName)) + ">";
}

static string TypeKind(Type type)
{
    if (typeof(MulticastDelegate).IsAssignableFrom(type.BaseType))
    {
        return "delegate";
    }

    return type.IsEnum ? "enum" : type.IsValueType ? "struct" : type.IsInterface ? "interface" : "class";
}

static string DisplayName(Type type) => (type.FullName ?? type.Name).Replace('+', '.');

static string PageFileName(Type type) => Slug(ShortDisplayName(type)) + ".md";

static string PageLink(Type type) => Slug(ShortDisplayName(type));

static string PageStem(string fileName) => fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
    ? fileName.Substring(0, fileName.Length - ".md".Length)
    : fileName;

static string ShortDisplayName(Type type)
{
    var displayName = DisplayName(type);
    const string rootNamespace = "Electron2D.";
    return displayName.StartsWith(rootNamespace, StringComparison.Ordinal)
        ? displayName.Substring(rootNamespace.Length)
        : displayName;
}

static string Slug(string value)
{
    var builder = new StringBuilder();
    foreach (var c in value)
    {
        if (char.IsLetterOrDigit(c))
        {
            builder.Append(c);
        }
        else
        {
            builder.Append('-');
        }
    }

    return builder.ToString().Trim('-');
}

static string Anchor(string value) => Slug(value).ToLowerInvariant();

static string TypeId(Type type) => "T:" + XmlTypeName(type);

static string XmlTypeName(Type type) => (type.FullName ?? type.Name).Replace('+', '.');

static string MethodId(Type declaringType, MethodBase method, string methodName)
{
    var name = methodName;
    if (method.IsGenericMethod)
    {
        name += "``" + method.GetGenericArguments().Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    var parameters = method.GetParameters();
    var id = "M:" + XmlTypeName(declaringType) + "." + name;
    if (parameters.Length == 0)
    {
        return id;
    }

    return id + "(" + string.Join(",", parameters.Select(parameter => XmlParameterTypeName(parameter.ParameterType))) + ")";
}

static string XmlParameterTypeName(Type type)
{
    if (type.IsByRef)
    {
        return XmlParameterTypeName(type.GetElementType()!) + "@";
    }

    if (type.IsArray)
    {
        return XmlParameterTypeName(type.GetElementType()!) + "[]";
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

        return definitionName + "{" + string.Join(",", type.GetGenericArguments().Select(XmlParameterTypeName)) + "}";
    }

    return (type.FullName ?? type.Name).Replace('+', '.');
}

static string CrefDisplay(string cref)
{
    var value = cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;
    return value.Replace('+', '.');
}

static string EscapeTable(string value)
{
    return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ").Trim();
}

public sealed record ApiMember(string Kind, string DisplayName, string Signature, string XmlId);

public sealed record ApiCategory(string PageFileName, string Title, string Description, Func<Type, bool> Matches);
'@ | Set-Content -LiteralPath $generatorSource -Encoding UTF8

$generatorOutput = & dotnet run --project $generatorProject -- $assemblyPath $xmlPath $expectedWikiRoot 2>&1
$generatorExitCode = $LASTEXITCODE
if ($generatorExitCode -ne 0) {
    Write-Host ($generatorOutput -join [Environment]::NewLine)
    exit $generatorExitCode
}

$generatedMarker = '<!-- Generated by tools/Update-ApiWiki.ps1. Do not edit by hand. -->'
$expectedFiles = Get-ChildItem -LiteralPath $expectedWikiRoot -File -Recurse | Sort-Object FullName

function Get-RelativeUnixPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $rootPath = [System.IO.Path]::GetFullPath($Root)
    $candidatePath = [System.IO.Path]::GetFullPath($Path)
    if (-not $rootPath.EndsWith([System.IO.Path]::DirectorySeparatorChar) -and
        -not $rootPath.EndsWith([System.IO.Path]::AltDirectorySeparatorChar)) {
        $rootPath += [System.IO.Path]::DirectorySeparatorChar
    }

    $rootUri = [System.Uri]::new($rootPath)
    $candidateUri = [System.Uri]::new($candidatePath)
    $relativeUri = $rootUri.MakeRelativeUri($candidateUri)
    return [System.Uri]::UnescapeDataString($relativeUri.ToString()).Replace('\', '/')
}

if ($Check) {
    if (-not $hasOutputPath) {
        $requiredGeneratedPages = @('Home.md', '_Sidebar.md', '_Footer.md', 'API-by-Category.md', 'API-Reference.md')
        foreach ($requiredGeneratedPage in $requiredGeneratedPages) {
            $requiredGeneratedPath = Join-Path $expectedWikiRoot $requiredGeneratedPage
            if (-not (Test-Path -LiteralPath $requiredGeneratedPath)) {
                throw "Generated GitHub Wiki page is missing from the generator output: $requiredGeneratedPage"
            }
        }

        Write-Host "GitHub Wiki API reference generation verified. Generated pages: $($expectedFiles.Count)."
        exit 0
    }

    $wikiRoot = $targetWikiRoot
    $issues = New-Object 'System.Collections.Generic.List[string]'
    if (-not (Test-Path -LiteralPath $wikiRoot)) {
        $issues.Add("GitHub Wiki clone was not found: $wikiRoot")
    }

    $compatibilityPath = Join-Path $wikiRoot 'API-Compatibility.md'
    if (-not (Test-Path -LiteralPath $compatibilityPath)) {
        $issues.Add('GitHub Wiki compatibility page is missing: API-Compatibility.md')
    }
    else {
        $compatibilityText = Get-Content -LiteralPath $compatibilityPath -Raw
        $firstVisibleLine = $compatibilityText -split "`r?`n" |
            Where-Object {
                $line = $_.Trim()
                -not [string]::IsNullOrWhiteSpace($line) -and
                -not ($line.StartsWith('<!--') -and $line.EndsWith('-->'))
            } |
            Select-Object -First 1
        if ($firstVisibleLine -eq '# API Compatibility') {
            $issues.Add('GitHub Wiki compatibility page must not repeat the GitHub page title as a top-level heading.')
        }
    }

    foreach ($expectedFile in $expectedFiles) {
        $relativePath = Get-RelativeUnixPath -Root $expectedWikiRoot -Path $expectedFile.FullName
        $actualPath = Join-Path $wikiRoot $relativePath
        if (-not (Test-Path -LiteralPath $actualPath)) {
            $issues.Add("Missing generated Wiki page: $relativePath")
            continue
        }

        $expectedText = [System.IO.File]::ReadAllText($expectedFile.FullName).Replace("`r`n", "`n").Replace("`r", "`n")
        $actualText = [System.IO.File]::ReadAllText($actualPath).Replace("`r`n", "`n").Replace("`r", "`n")
        if (-not [System.String]::Equals($expectedText, $actualText, [System.StringComparison]::Ordinal)) {
            $issues.Add("Out-of-date generated Wiki page: $relativePath")
        }
    }

    $expectedRelativePaths = $expectedFiles |
        ForEach-Object { Get-RelativeUnixPath -Root $expectedWikiRoot -Path $_.FullName }

    if (Test-Path -LiteralPath $wikiRoot) {
        foreach ($actualFile in Get-ChildItem -LiteralPath $wikiRoot -File -Recurse) {
            $text = Get-Content -LiteralPath $actualFile.FullName -Raw
            if ($text.IndexOf($generatedMarker, [System.StringComparison]::Ordinal) -lt 0) {
                continue
            }

            $relativePath = Get-RelativeUnixPath -Root $wikiRoot -Path $actualFile.FullName
            if ($expectedRelativePaths -notcontains $relativePath) {
                $issues.Add("Stale generated Wiki page: $relativePath")
            }
        }
    }

    if ($issues.Count -gt 0) {
        Write-Host 'GitHub Wiki API reference verification failed.'
        foreach ($issue in $issues) {
            Write-Host "- $issue"
        }
        exit 1
    }

    Write-Host "GitHub Wiki API reference verification passed for $wikiRoot. Generated pages: $($expectedFiles.Count)."
    exit 0
}

$wikiRoot = $targetWikiRoot
New-Item -ItemType Directory -Force -Path $wikiRoot | Out-Null

$expectedRelativePaths = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
foreach ($expectedFile in $expectedFiles) {
    $relativePath = Get-RelativeUnixPath -Root $expectedWikiRoot -Path $expectedFile.FullName
    $expectedRelativePaths.Add($relativePath) | Out-Null
    $targetPath = Join-Path $wikiRoot $relativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $targetPath) | Out-Null
    Copy-Item -LiteralPath $expectedFile.FullName -Destination $targetPath -Force
}

foreach ($actualFile in Get-ChildItem -LiteralPath $wikiRoot -File -Recurse) {
    $text = Get-Content -LiteralPath $actualFile.FullName -Raw
    if ($text.IndexOf($generatedMarker, [System.StringComparison]::Ordinal) -lt 0) {
        continue
    }

    $relativePath = Get-RelativeUnixPath -Root $wikiRoot -Path $actualFile.FullName
    if (-not $expectedRelativePaths.Contains($relativePath)) {
        Remove-Item -LiteralPath $actualFile.FullName
    }
}

Write-Host "GitHub Wiki API reference updated at $wikiRoot. Generated pages: $($expectedFiles.Count)."
