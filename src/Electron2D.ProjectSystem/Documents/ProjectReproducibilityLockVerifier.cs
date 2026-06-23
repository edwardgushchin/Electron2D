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
using System.Xml.Linq;

namespace Electron2D.ProjectSystem;

internal sealed class ProjectReproducibilityLockVerificationResult
{
    public ProjectReproducibilityLockVerificationResult(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded => Diagnostics.Count == 0;

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }
}

internal static class ProjectReproducibilityLockVerifier
{
    internal const string LockFileName = "electron2d.lock.json";
    internal const string GlobalJsonFileName = "global.json";
    internal const string Format = "Electron2D.ReproducibilityLock";
    internal const int SchemaVersion = 1;
    internal const string SchemaUri = "https://electron2d.dev/schemas/project-system/electron2d-lock.schema.json";

    private static readonly string[] TopLevelOrder =
    [
        "$schema",
        "format",
        "schemaVersion",
        "engine",
        "dotnet",
        "nuget",
        "nativeRuntime",
        "assetImporters",
        "project",
        "exportTemplates",
        "signing"
    ];

    private static readonly string[] SecretFieldFragments =
    [
        "password",
        "token",
        "secret",
        "private",
        "certificate",
        "credential",
        "keystore"
    ];

    public static ProjectReproducibilityLockVerificationResult Verify(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        var fullRoot = Path.GetFullPath(projectRoot);
        var diagnostics = new List<StructuredDiagnostic>();
        var globalJsonPath = Path.Combine(fullRoot, GlobalJsonFileName);
        var lockPath = Path.Combine(fullRoot, LockFileName);

        var globalJson = ReadRequiredJson(globalJsonPath, GlobalJsonFileName, diagnostics);
        var lockJson = ReadLockJson(fullRoot, lockPath, diagnostics);
        if (globalJson is null || lockJson is null)
        {
            return new ProjectReproducibilityLockVerificationResult(diagnostics);
        }

        ValidateTopLevelOrder(lockJson, diagnostics);
        ValidateNoForbiddenValues(lockJson, "$", null, LockFileName, diagnostics);
        ValidateGlobalJson(globalJson, lockJson, diagnostics);
        ValidateLockShape(lockJson, diagnostics);
        ValidateProjectPackageReference(fullRoot, lockJson, diagnostics);

        return new ProjectReproducibilityLockVerificationResult(diagnostics);
    }

    private static JsonObject? ReadLockJson(
        string projectRoot,
        string legacyLockPath,
        List<StructuredDiagnostic> diagnostics)
    {
        if (File.Exists(legacyLockPath))
        {
            return ReadRequiredJson(legacyLockPath, LockFileName, diagnostics);
        }

        var projectFile = ResolveProjectFile(projectRoot);
        if (projectFile is null)
        {
            diagnostics.Add(CreateDiagnostic(
                "Project reproducibility metadata is required either in electron2d.lock.json or in the main .e2d file.",
                LockFileName));
            return null;
        }

        var relativeProjectFile = Path.GetRelativePath(projectRoot, projectFile).Replace(Path.DirectorySeparatorChar, '/');
        try
        {
            var projectJson = JsonNode.Parse(File.ReadAllText(projectFile)) as JsonObject ??
                throw new FormatException($"{relativeProjectFile} root must be a JSON object.");
            if (projectJson["reproducibilityLock"] is JsonObject lockJson)
            {
                return lockJson;
            }

            diagnostics.Add(CreateDiagnostic(
                $"{relativeProjectFile} must contain reproducibilityLock when electron2d.lock.json is not present.",
                relativeProjectFile));
            return null;
        }
        catch (Exception exception) when (exception is JsonException or FormatException or IOException or UnauthorizedAccessException)
        {
            diagnostics.Add(CreateDiagnostic(
                $"{relativeProjectFile} could not be read as a valid JSON object: {exception.Message}",
                relativeProjectFile));
            return null;
        }
    }

    private static string? ResolveProjectFile(string projectRoot)
    {
        var named = Path.Combine(projectRoot, Path.GetFileName(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + ".e2d");
        if (File.Exists(named))
        {
            return named;
        }

        return Directory.EnumerateFiles(projectRoot, "*.e2d", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static JsonObject? ReadRequiredJson(
        string path,
        string relativePath,
        List<StructuredDiagnostic> diagnostics)
    {
        if (!File.Exists(path))
        {
            diagnostics.Add(CreateDiagnostic(
                $"{relativePath} is required for reproducible Electron2D projects.",
                relativePath));
            return null;
        }

        try
        {
            return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ??
                throw new FormatException($"{relativePath} root must be a JSON object.");
        }
        catch (Exception exception) when (exception is JsonException or FormatException or IOException or UnauthorizedAccessException)
        {
            diagnostics.Add(CreateDiagnostic(
                $"{relativePath} could not be read as a valid JSON object: {exception.Message}",
                relativePath));
            return null;
        }
    }

    private static void ValidateTopLevelOrder(JsonObject lockJson, List<StructuredDiagnostic> diagnostics)
    {
        var actual = lockJson.Select(property => property.Key).ToArray();
        if (!actual.SequenceEqual(TopLevelOrder, StringComparer.Ordinal))
        {
            diagnostics.Add(CreateDiagnostic(
                $"{LockFileName} must use the stable top-level field order from the reproducibility specification.",
                LockFileName));
        }
    }

    private static void ValidateGlobalJson(
        JsonObject globalJson,
        JsonObject lockJson,
        List<StructuredDiagnostic> diagnostics)
    {
        var sdk = ReadObject(globalJson, "sdk", GlobalJsonFileName, "$.sdk", diagnostics);
        var dotnet = ReadObject(lockJson, "dotnet", LockFileName, "$.dotnet", diagnostics);
        if (sdk is null || dotnet is null)
        {
            return;
        }

        var globalVersion = ReadString(sdk, "version", GlobalJsonFileName, "$.sdk.version", diagnostics);
        var globalRollForward = ReadString(sdk, "rollForward", GlobalJsonFileName, "$.sdk.rollForward", diagnostics);
        var lockVersion = ReadString(dotnet, "sdkVersion", LockFileName, "$.dotnet.sdkVersion", diagnostics);
        var lockRollForward = ReadString(dotnet, "rollForward", LockFileName, "$.dotnet.rollForward", diagnostics);

        if (globalVersion is not null && lockVersion is not null &&
            !string.Equals(globalVersion, lockVersion, StringComparison.Ordinal))
        {
            diagnostics.Add(CreateDiagnostic(
                $"{LockFileName} dotnet.sdkVersion must match global.json sdk.version.",
                LockFileName));
        }

        if (globalRollForward is not null && lockRollForward is not null &&
            !string.Equals(globalRollForward, lockRollForward, StringComparison.Ordinal))
        {
            diagnostics.Add(CreateDiagnostic(
                $"{LockFileName} dotnet.rollForward must match global.json sdk.rollForward.",
                LockFileName));
        }
    }

    private static void ValidateLockShape(JsonObject lockJson, List<StructuredDiagnostic> diagnostics)
    {
        RequireString(lockJson, "$schema", SchemaUri, LockFileName, "$.$schema", diagnostics);
        RequireString(lockJson, "format", Format, LockFileName, "$.format", diagnostics);
        RequireInt32(lockJson, "schemaVersion", SchemaVersion, LockFileName, "$.schemaVersion", diagnostics);

        var engine = ReadObject(lockJson, "engine", LockFileName, "$.engine", diagnostics);
        var dotnet = ReadObject(lockJson, "dotnet", LockFileName, "$.dotnet", diagnostics);
        var nuget = ReadObject(lockJson, "nuget", LockFileName, "$.nuget", diagnostics);
        var nativeRuntime = ReadObject(lockJson, "nativeRuntime", LockFileName, "$.nativeRuntime", diagnostics);
        var assetImporters = ReadObject(lockJson, "assetImporters", LockFileName, "$.assetImporters", diagnostics);
        var project = ReadObject(lockJson, "project", LockFileName, "$.project", diagnostics);
        var exportTemplates = ReadObject(lockJson, "exportTemplates", LockFileName, "$.exportTemplates", diagnostics);
        var signing = ReadObject(lockJson, "signing", LockFileName, "$.signing", diagnostics);

        if (engine is not null)
        {
            ReadString(engine, "version", LockFileName, "$.engine.version", diagnostics);
        }

        if (dotnet is not null)
        {
            ReadString(dotnet, "sdkVersion", LockFileName, "$.dotnet.sdkVersion", diagnostics);
            ReadString(dotnet, "rollForward", LockFileName, "$.dotnet.rollForward", diagnostics);
            ReadString(dotnet, "targetFramework", LockFileName, "$.dotnet.targetFramework", diagnostics);
        }

        ValidatePackageSection(nuget, "nuget", diagnostics);
        ValidatePackageSection(nativeRuntime, "nativeRuntime", diagnostics);

        if (assetImporters is not null)
        {
            ReadString(assetImporters, "texture", LockFileName, "$.assetImporters.texture", diagnostics);
            ReadString(assetImporters, "font", LockFileName, "$.assetImporters.font", diagnostics);
            ReadString(assetImporters, "audio", LockFileName, "$.assetImporters.audio", diagnostics);
            ReadString(assetImporters, "shader", LockFileName, "$.assetImporters.shader", diagnostics);
        }

        if (project is not null)
        {
            ReadString(project, "rendererProfile", LockFileName, "$.project.rendererProfile", diagnostics);
            ReadString(project, "physicsBackendVersion", LockFileName, "$.project.physicsBackendVersion", diagnostics);
            ReadString(project, "serializationSchemaVersion", LockFileName, "$.project.serializationSchemaVersion", diagnostics);
        }

        if (exportTemplates is not null)
        {
            ReadString(exportTemplates, "version", LockFileName, "$.exportTemplates.version", diagnostics);
        }

        if (signing is not null)
        {
            RequireString(signing, "mode", "referencesOnly", LockFileName, "$.signing.mode", diagnostics);
        }
    }

    private static void ValidatePackageSection(JsonObject? section, string sectionName, List<StructuredDiagnostic> diagnostics)
    {
        if (section is null)
        {
            return;
        }

        var packages = ReadArray(section, "packages", LockFileName, $"$.{sectionName}.packages", diagnostics);
        if (packages is null)
        {
            return;
        }

        if (packages.Count == 0)
        {
            diagnostics.Add(CreateDiagnostic(
                $"{LockFileName} {sectionName}.packages must contain at least one package.",
                LockFileName));
            return;
        }

        for (var index = 0; index < packages.Count; index++)
        {
            if (packages[index] is not JsonObject package)
            {
                diagnostics.Add(CreateDiagnostic(
                    $"{LockFileName} {sectionName}.packages[{index}] must be an object.",
                    LockFileName));
                continue;
            }

            ReadString(package, "id", LockFileName, $"$.{sectionName}.packages[{index}].id", diagnostics);
            ReadString(package, "version", LockFileName, $"$.{sectionName}.packages[{index}].version", diagnostics);
        }
    }

    private static void ValidateProjectPackageReference(
        string projectRoot,
        JsonObject lockJson,
        List<StructuredDiagnostic> diagnostics)
    {
        var engine = ReadObject(lockJson, "engine", LockFileName, "$.engine", diagnostics);
        var expectedVersion = engine is null
            ? null
            : ReadString(engine, "version", LockFileName, "$.engine.version", diagnostics);
        if (expectedVersion is null)
        {
            return;
        }

        var projectFile = Directory.EnumerateFiles(projectRoot, "*.csproj", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault();
        if (projectFile is null)
        {
            diagnostics.Add(CreateDiagnostic(
                "Project root must contain a .csproj file with an Electron2D package reference.",
                LockFileName));
            return;
        }

        var relativeProjectPath = Path.GetRelativePath(projectRoot, projectFile).Replace(Path.DirectorySeparatorChar, '/');
        try
        {
            var document = XDocument.Load(projectFile);
            var electron2DPackage = document.Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "PackageReference", StringComparison.Ordinal))
                .FirstOrDefault(element => string.Equals((string?)element.Attribute("Include"), "Electron2D", StringComparison.Ordinal));
            var packageVersion = (string?)electron2DPackage?.Attribute("Version")
                ?? electron2DPackage?.Elements().FirstOrDefault(element => string.Equals(element.Name.LocalName, "Version", StringComparison.Ordinal))?.Value;
            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                diagnostics.Add(CreateDiagnostic(
                    "Project .csproj must reference the Electron2D package with an explicit Version.",
                    relativeProjectPath));
                return;
            }

            if (!string.Equals(packageVersion, expectedVersion, StringComparison.Ordinal))
            {
                diagnostics.Add(CreateDiagnostic(
                    "Project .csproj Electron2D package version must match electron2d.lock.json engine.version.",
                    relativeProjectPath));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            diagnostics.Add(CreateDiagnostic(
                $"Project .csproj could not be read for reproducibility validation: {exception.Message}",
                relativeProjectPath));
        }
    }

    private static void ValidateNoForbiddenValues(
        JsonNode? node,
        string jsonPath,
        string? propertyName,
        string relativePath,
        List<StructuredDiagnostic> diagnostics)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (name, child) in obj)
                {
                    if (SecretFieldFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                    {
                        diagnostics.Add(CreateDiagnostic(
                            $"{LockFileName} must not store secret-bearing field '{name}' at {jsonPath}.{name}.",
                            relativePath));
                    }

                    ValidateNoForbiddenValues(child, $"{jsonPath}.{name}", name, relativePath, diagnostics);
                }

                break;
            case JsonArray array:
                for (var index = 0; index < array.Count; index++)
                {
                    ValidateNoForbiddenValues(array[index], $"{jsonPath}[{index}]", propertyName, relativePath, diagnostics);
                }

                break;
            case JsonValue value when value.TryGetValue<string>(out var text) && IsMachineLocalAbsolutePath(text):
                diagnostics.Add(CreateDiagnostic(
                    $"{LockFileName} value at {jsonPath} must not contain a machine-local absolute path.",
                    relativePath));
                break;
        }
    }

    private static JsonObject? ReadObject(
        JsonObject root,
        string propertyName,
        string relativePath,
        string jsonPath,
        List<StructuredDiagnostic> diagnostics)
    {
        if (root.TryGetPropertyValue(propertyName, out var node) && node is JsonObject obj)
        {
            return obj;
        }

        diagnostics.Add(CreateDiagnostic(
            $"{relativePath} property {jsonPath} must be a JSON object.",
            relativePath));
        return null;
    }

    private static JsonArray? ReadArray(
        JsonObject root,
        string propertyName,
        string relativePath,
        string jsonPath,
        List<StructuredDiagnostic> diagnostics)
    {
        if (root.TryGetPropertyValue(propertyName, out var node) && node is JsonArray array)
        {
            return array;
        }

        diagnostics.Add(CreateDiagnostic(
            $"{relativePath} property {jsonPath} must be a JSON array.",
            relativePath));
        return null;
    }

    private static string? ReadString(
        JsonObject root,
        string propertyName,
        string relativePath,
        string jsonPath,
        List<StructuredDiagnostic> diagnostics)
    {
        if (root.TryGetPropertyValue(propertyName, out var node) &&
            node is JsonValue value &&
            value.TryGetValue<string>(out var text) &&
            !string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        diagnostics.Add(CreateDiagnostic(
            $"{relativePath} property {jsonPath} must be a non-empty string.",
            relativePath));
        return null;
    }

    private static void RequireString(
        JsonObject root,
        string propertyName,
        string expected,
        string relativePath,
        string jsonPath,
        List<StructuredDiagnostic> diagnostics)
    {
        var actual = ReadString(root, propertyName, relativePath, jsonPath, diagnostics);
        if (actual is not null && !string.Equals(actual, expected, StringComparison.Ordinal))
        {
            diagnostics.Add(CreateDiagnostic(
                $"{relativePath} property {jsonPath} must be '{expected}'.",
                relativePath));
        }
    }

    private static void RequireInt32(
        JsonObject root,
        string propertyName,
        int expected,
        string relativePath,
        string jsonPath,
        List<StructuredDiagnostic> diagnostics)
    {
        if (!root.TryGetPropertyValue(propertyName, out var node) ||
            node is not JsonValue value ||
            !value.TryGetValue<int>(out var actual))
        {
            diagnostics.Add(CreateDiagnostic(
                $"{relativePath} property {jsonPath} must be the integer {expected}.",
                relativePath));
            return;
        }

        if (actual != expected)
        {
            diagnostics.Add(CreateDiagnostic(
                $"{relativePath} property {jsonPath} must be {expected}.",
                relativePath));
        }
    }

    private static bool IsMachineLocalAbsolutePath(string value)
    {
        if (value.StartsWith("res://", StringComparison.Ordinal) ||
            value.StartsWith("uid://", StringComparison.Ordinal) ||
            value.StartsWith("env:", StringComparison.Ordinal) ||
            Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http")
        {
            return false;
        }

        return value.StartsWith("/", StringComparison.Ordinal) ||
            value.StartsWith("\\\\", StringComparison.Ordinal) ||
            value.Length >= 3 &&
            char.IsAsciiLetter(value[0]) &&
            value[1] == ':' &&
            value[2] is '\\' or '/';
    }

    private static StructuredDiagnostic CreateDiagnostic(string message, string? relativePath)
    {
        return StructuredDiagnostic.Create(
            "E2D-DOCTOR-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            message,
            relativePath is null ? null : new DiagnosticLocation(relativePath),
            relatedLocations: [],
            suggestedFixes: []);
    }
}
