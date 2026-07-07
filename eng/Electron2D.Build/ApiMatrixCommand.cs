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
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Electron2D.Build;

internal sealed class ApiMatrixCommand(string repositoryRoot, JsonDiagnosticSink diagnostics)
{
    private const string Baseline = "4.7-stable";
    private const string GeneratorVersion = "T-0242";
    private static readonly Regex GeneratedClassIdentityPattern = new(@"^@?[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex CSharpMemberIdentifierPattern = new(@"^_?[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private const string WindowsAbsolutePathPlaceholder = "<windows-absolute-path>";
    private static readonly StringComparer FileSystemPathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private static readonly StringComparison FileSystemPathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    private static readonly HashSet<string> AllowedCSharpOperatorNames = new(StringComparer.Ordinal)
    {
        "operator -",
        "operator !=",
        "operator []",
        "operator *",
        "operator **",
        "operator /",
        "operator &",
        "operator +",
        "operator <",
        "operator <<",
        "operator <=",
        "operator >",
        "operator >>",
        "operator >=",
        "operator %",
        "operator ^",
        "operator ==",
        "operator |",
        "operator ~",
        "operator unary-",
        "operator unary+"
    };

    private static readonly HashSet<string> CSharpReservedKeywords = new(StringComparer.Ordinal)
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while"
    };

    private static readonly string[] GeneratedOutputDirectories =
    [
        "data/api/godot-4.7/classes",
        "data/api/godot-4.7/index",
        "data/api/electron2d/classes",
        "data/api/electron2d/index"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length < 2)
        {
            WriteInvalidArguments("Expected: api fetch-godot --version <version>, api generate-matrix, or api generate-class-packets.");
            return RepositoryBuildExitCodes.Failed;
        }

        return args[1] switch
        {
            "fetch-godot" => await FetchGodotAsync(args, cancellationToken).ConfigureAwait(false),
            "generate-matrix" => GenerateMatrix(args),
            "generate-class-packets" => GenerateClassPackets(args),
            _ => UnknownApiCommand(args[1])
        };
    }

    private async Task<int> FetchGodotAsync(string[] args, CancellationToken cancellationToken)
    {
        var parse = ParseFetchArguments(args);
        if (!parse.Succeeded)
        {
            WriteInvalidArguments(parse.ErrorMessage);
            return RepositoryBuildExitCodes.Failed;
        }

        if (!string.Equals(parse.Version, Baseline, StringComparison.Ordinal))
        {
            diagnostics.Write(new BuildDiagnostic(
                "api",
                "api fetch-godot",
                "error",
                "E2D-BUILD-API-GODOT-VERSION",
                $"Unsupported Godot API baseline '{parse.Version}'. Expected '{Baseline}'."));
            return RepositoryBuildExitCodes.Failed;
        }

        var outputPath = ResolveRepositoryOrAbsolutePath(parse.OutputPath ?? Path.Combine(".temp", "api", "godot-4.7", "source"));
        if (parse.SourcePath is not null)
        {
            var sourcePath = ResolveRepositoryOrAbsolutePath(parse.SourcePath);
            if (!Directory.Exists(sourcePath))
            {
                diagnostics.Write(new BuildDiagnostic("api", "api fetch-godot", "error", "E2D-BUILD-API-GODOT-SOURCE-MISSING", "Godot source directory was not found.", Path: ToRepositoryPath(sourcePath)));
                return RepositoryBuildExitCodes.Failed;
            }

            try
            {
                ClearGodotApiInputDirectory(outputPath);
                CopyGodotApiInputFiles(sourcePath, outputPath);
                EnsureCSharpSnapshot(outputPath);
                diagnostics.Write(new BuildDiagnostic("api", "api fetch-godot", "info", "E2D-BUILD-API-GODOT-FETCHED", "Godot API source snapshot was copied.", Path: ToRepositoryPath(outputPath)));
                return RepositoryBuildExitCodes.Success;
            }
            catch (ApiMatrixGenerationException ex)
            {
                diagnostics.Write(new BuildDiagnostic("api", "api fetch-godot", "error", ex.Code, ex.Message, Path: ex.DiagnosticPath));
                return RepositoryBuildExitCodes.Failed;
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or System.Xml.XmlException)
            {
                diagnostics.Write(new BuildDiagnostic("api", "api fetch-godot", "error", "E2D-BUILD-API-GODOT-FETCH-FAILED", $"Godot API source snapshot could not be copied: {ex.Message}.", Path: ToRepositoryPath(outputPath)));
                return RepositoryBuildExitCodes.Failed;
            }
        }

        try
        {
            await DownloadGodotSourceAsync(parse.Version!, outputPath, cancellationToken).ConfigureAwait(false);
            EnsureCSharpSnapshot(outputPath);
            diagnostics.Write(new BuildDiagnostic("api", "api fetch-godot", "info", "E2D-BUILD-API-GODOT-FETCHED", "Godot API source snapshot was fetched.", Path: ToRepositoryPath(outputPath)));
            return RepositoryBuildExitCodes.Success;
        }
        catch (ApiMatrixGenerationException ex)
        {
            diagnostics.Write(new BuildDiagnostic("api", "api fetch-godot", "error", ex.Code, ex.Message, Path: ex.DiagnosticPath));
            return RepositoryBuildExitCodes.Failed;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or JsonException or System.Xml.XmlException)
        {
            diagnostics.Write(new BuildDiagnostic("api", "api fetch-godot", "error", "E2D-BUILD-API-GODOT-FETCH-FAILED", $"Godot API source snapshot could not be fetched: {ex.Message}.", Path: ToRepositoryPath(outputPath)));
            return RepositoryBuildExitCodes.Failed;
        }
    }

    private int GenerateMatrix(string[] args)
    {
        var parse = ParseGenerateArguments(args, "api generate-matrix");
        if (!parse.Succeeded)
        {
            WriteInvalidArguments(parse.ErrorMessage);
            return RepositoryBuildExitCodes.Failed;
        }

        var outputs = BuildOutputs(parse);
        if (!outputs.Succeeded)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var selected = outputs.Files
            .Where(file => file.RelativePath.EndsWith("/index/classes.json", StringComparison.Ordinal))
            .ToArray();
        return WriteOrCheckFiles(
            "api generate-matrix",
            parse.Check,
            selected,
            "E2D-BUILD-API-MATRIX-GENERATED",
            "API matrix indexes were generated.",
            "E2D-BUILD-API-MATRIX-CHECK-PASSED",
            "API matrix indexes are synchronized.");
    }

    private int GenerateClassPackets(string[] args)
    {
        var parse = ParseGenerateArguments(args, "api generate-class-packets");
        if (!parse.Succeeded)
        {
            WriteInvalidArguments(parse.ErrorMessage);
            return RepositoryBuildExitCodes.Failed;
        }

        var outputs = BuildOutputs(parse);
        if (!outputs.Succeeded)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        return WriteOrCheckFiles(
            "api generate-class-packets",
            parse.Check,
            outputs.Files,
            "E2D-BUILD-API-CLASS-PACKETS-GENERATED",
            "API class packets were generated.",
            "E2D-BUILD-API-CLASS-PACKETS-CHECK-PASSED",
            "Generated API packets are synchronized.");
    }

    private ApiOutputBuildResult BuildOutputs(GenerateArguments args)
    {
        var godotSource = ResolveGodotSourcePath(args.GodotSourcePath);
        if (!Directory.Exists(godotSource))
        {
            diagnostics.Write(new BuildDiagnostic("api", args.Step, "error", "E2D-BUILD-API-GODOT-SOURCE-MISSING", "Godot API source snapshot was not found. Run api fetch-godot --version 4.7-stable first.", Path: ToRepositoryPath(godotSource)));
            return ApiOutputBuildResult.Failed;
        }

        var electronManifest = ResolveRepositoryOrAbsolutePath(args.Electron2DManifestPath ?? "data/api/electron2d-api-manifest.json");
        if (!File.Exists(electronManifest))
        {
            diagnostics.Write(new BuildDiagnostic("api", args.Step, "error", "E2D-BUILD-API-ELECTRON2D-MANIFEST-MISSING", "Electron2D API manifest was not found.", Path: ToRepositoryPath(electronManifest)));
            return ApiOutputBuildResult.Failed;
        }

        try
        {
            var files = new List<ApiGeneratedFile>();
            var csharpSnapshot = ReadGodotCSharpSnapshot(godotSource);
            var godotClasses = ReadGodotClasses(godotSource, csharpSnapshot).OrderBy(packet => packet.Class.Name, StringComparer.Ordinal).ToArray();
            var electronClasses = ReadElectron2DClasses(electronManifest).OrderBy(packet => packet.Class.Name, StringComparer.Ordinal).ToArray();

            foreach (var packet in godotClasses)
            {
                AddPacketFiles(files, "data/api/godot-4.7/classes", packet);
            }

            foreach (var packet in electronClasses)
            {
                AddPacketFiles(files, "data/api/electron2d/classes", packet);
            }

            files.Add(new ApiGeneratedFile("data/api/godot-4.7/index/classes.json", Serialize(CreateIndex("godot", godotClasses))));
            files.Add(new ApiGeneratedFile("data/api/electron2d/index/classes.json", Serialize(CreateIndex("electron2d", electronClasses))));
            ValidateGeneratedFiles(files);
            return new ApiOutputBuildResult(true, files);
        }
        catch (ApiMatrixGenerationException ex)
        {
            diagnostics.Write(new BuildDiagnostic("api", args.Step, "error", ex.Code, ex.Message, Path: ex.DiagnosticPath));
            return ApiOutputBuildResult.Failed;
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException or System.Xml.XmlException)
        {
            diagnostics.Write(new BuildDiagnostic("api", args.Step, "error", "E2D-BUILD-API-GENERATE-FAILED", $"API matrix could not be generated: {ex.Message}."));
            return ApiOutputBuildResult.Failed;
        }
    }

    private int WriteOrCheckFiles(
        string step,
        bool check,
        IReadOnlyList<ApiGeneratedFile> files,
        string successCode,
        string successMessage,
        string checkSuccessCode,
        string checkSuccessMessage)
    {
        if (check)
        {
            var failed = false;
            var expectedPaths = files.Select(file => file.RelativePath).ToHashSet(StringComparer.Ordinal);
            foreach (var file in files)
            {
                var path = ResolveGeneratedOutputPath(file.RelativePath);
                if (!File.Exists(path))
                {
                    diagnostics.Write(new BuildDiagnostic("api", step, "error", "E2D-BUILD-API-CLASS-PACKET-MISSING", "Generated API packet is missing.", Path: file.RelativePath));
                    failed = true;
                    continue;
                }

                if (!string.Equals(Normalize(File.ReadAllText(path, Encoding.UTF8)), Normalize(file.Content), StringComparison.Ordinal))
                {
                    diagnostics.Write(new BuildDiagnostic("api", step, "error", "E2D-BUILD-API-CLASS-PACKET-STALE", "Generated API packet is stale.", Path: file.RelativePath));
                    failed = true;
                }
            }

            foreach (var extraPath in EnumerateExistingGeneratedFiles(files))
            {
                if (expectedPaths.Contains(extraPath))
                {
                    continue;
                }

                diagnostics.Write(new BuildDiagnostic("api", step, "error", "E2D-BUILD-API-CLASS-PACKET-EXTRA", "Generated API packet is extra.", Path: extraPath));
                failed = true;
            }

            if (failed)
            {
                return RepositoryBuildExitCodes.Failed;
            }

            diagnostics.Write(new BuildDiagnostic("api", step, "info", checkSuccessCode, checkSuccessMessage));
            return RepositoryBuildExitCodes.Success;
        }

        ClearGeneratedOutputDirectories(files);
        foreach (var file in files)
        {
            var path = ResolveGeneratedOutputPath(file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, Normalize(file.Content), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        diagnostics.Write(new BuildDiagnostic("api", step, "info", successCode, successMessage));
        return RepositoryBuildExitCodes.Success;
    }

    private void ValidateGeneratedFiles(IReadOnlyList<ApiGeneratedFile> files)
    {
        var seen = new HashSet<string>(FileSystemPathComparer);
        foreach (var file in files)
        {
            var path = ResolveGeneratedOutputPath(file.RelativePath);
            if (!seen.Add(path))
            {
                throw InvalidGeneratedOutputPath(file.RelativePath, "canonical path duplicates another generated output");
            }
        }
    }

    private string ResolveGeneratedOutputPath(string relativePath)
    {
        if (!IsSafeGeneratedRelativePath(relativePath))
        {
            throw InvalidGeneratedOutputPath(relativePath, "relative path contains a rooted path, separator escape, empty segment, or dot segment");
        }

        var path = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsAllowedGeneratedOutputPath(path))
        {
            throw InvalidGeneratedOutputPath(relativePath, "canonical path is outside generated API output directories");
        }

        return path;
    }

    private static bool IsSafeGeneratedRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.Contains('\\', StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath))
        {
            return false;
        }

        return relativePath.Split('/').All(segment =>
            segment.Length > 0 &&
            !string.Equals(segment, ".", StringComparison.Ordinal) &&
            !string.Equals(segment, "..", StringComparison.Ordinal));
    }

    private bool IsAllowedGeneratedOutputPath(string path)
    {
        return GeneratedOutputDirectories
            .Select(directory => Path.GetFullPath(Path.Combine(repositoryRoot, directory.Replace('/', Path.DirectorySeparatorChar))))
            .Any(directory => IsPathInDirectory(path, directory));
    }

    private static bool IsPathInDirectory(string path, string directory)
    {
        var normalizedDirectory = Path.TrimEndingDirectorySeparator(directory) + Path.DirectorySeparatorChar;
        return path.StartsWith(normalizedDirectory, FileSystemPathComparison);
    }

    private IEnumerable<string> EnumerateExistingGeneratedFiles(IReadOnlyList<ApiGeneratedFile> expectedFiles)
    {
        foreach (var directory in expectedFiles
            .Select(file => Path.GetDirectoryName(ResolveGeneratedOutputPath(file.RelativePath))!)
            .Distinct(FileSystemPathComparer))
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.api.json", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(directory, "*.api.md", SearchOption.TopDirectoryOnly))
                .Concat(Directory.EnumerateFiles(directory, "classes.json", SearchOption.TopDirectoryOnly)))
            {
                yield return ToRepositoryPath(file);
            }
        }
    }

    private IReadOnlyList<ApiClassPacket> ReadGodotClasses(string sourceRoot, GodotCSharpSnapshot csharpSnapshot)
    {
        var xmlFiles = EnumerateGodotXmlFiles(sourceRoot).Order(StringComparer.Ordinal).ToArray();
        if (xmlFiles.Length == 0)
        {
            throw new InvalidOperationException("Godot XML class inputs were not found.");
        }

        return xmlFiles.Select(path => ReadGodotClass(sourceRoot, path, csharpSnapshot)).ToArray();
    }

    private ApiClassPacket ReadGodotClass(string sourceRoot, string path, GodotCSharpSnapshot csharpSnapshot)
    {
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidOperationException($"Godot class XML has no root element: {ToRepositoryPath(path)}.");
        var className = RequiredAttribute(root, "name", path);
        ValidateGodotSourceClassIdentity(path, className);
        csharpSnapshot.Classes.TryGetValue(className, out var csharpClass);
        if (csharpSnapshot.HasFile && csharpClass is null)
        {
            throw InvalidCSharpSnapshot(csharpSnapshot.Path!, $"class '{className}' is missing from the C# snapshot");
        }

        var csharpClassName = csharpClass?.CSharpName ?? className;
        var baseType = OptionalAttribute(root, "inherits");
        var constructors = new List<ApiMember>();
        var members = new List<ApiMember>();
        var rawMembers = new List<ApiMember>();
        var signals = new List<ApiMember>();
        var constants = new List<ApiConstant>();
        var operators = new List<ApiMember>();
        var virtualMethods = new List<ApiMember>();
        var enumGroups = new Dictionary<string, List<ApiConstant>>(StringComparer.Ordinal);
        var fullTypeName = $"Godot.{csharpClassName}";

        foreach (var constructor in root.Element("constructors")?.Elements("constructor") ?? [])
        {
            var name = RequiredAttribute(constructor, "name", path);
            var returnType = OptionalAttribute(constructor.Element("return"), "type") ?? csharpClassName;
            var parameters = constructor.Elements("param").Select(ReadParameter).ToArray();
            var csharpMember = ResolveCSharpMember(csharpSnapshot, csharpClass, className, "Constructor", name, returnType, parameters);
            constructors.Add(new ApiMember(
                csharpMember.Name,
                "Constructor",
                csharpMember.Signature,
                returnType,
                parameters,
                NormalizeDocumentation(constructor.Element("description")?.Value),
                null,
                null,
                name,
                CreateXmlDocId('M', fullTypeName, "#ctor", parameters),
                ToSourcePath(sourceRoot, path)));
        }

        foreach (var method in root.Element("methods")?.Elements("method") ?? [])
        {
            var name = RequiredAttribute(method, "name", path);
            var returnType = OptionalAttribute(method.Element("return"), "type") ?? "void";
            var parameters = method.Elements("param").Select(ReadParameter).ToArray();
            var csharpMember = ResolveCSharpMember(csharpSnapshot, csharpClass, className, "Method", name, returnType, parameters);
            var member = new ApiMember(
                csharpMember.Name,
                "Method",
                csharpMember.Signature,
                returnType,
                parameters,
                NormalizeDocumentation(method.Element("description")?.Value),
                null,
                null,
                name,
                CreateXmlDocId('M', fullTypeName, csharpMember.Name, parameters),
                ToSourcePath(sourceRoot, path));
            members.Add(member);
            if (IsVirtualMethod(method))
            {
                virtualMethods.Add(member);
            }
        }

        foreach (var property in root.Element("members")?.Elements("member") ?? [])
        {
            var name = RequiredAttribute(property, "name", path);
            var type = OptionalAttribute(property, "type") ?? string.Empty;
            var csharpMember = ResolveCSharpPropertyMember(csharpSnapshot, csharpClass, className, name, type);
            if (csharpMember is null)
            {
                rawMembers.Add(new ApiMember(
                    name,
                    "RawProperty",
                    null,
                    type,
                    [],
                    NormalizeDocumentation(property.Value),
                    null,
                    OptionalAttribute(property, "default"),
                    name,
                    null,
                    ToSourcePath(sourceRoot, path)));
                continue;
            }

            members.Add(new ApiMember(
                csharpMember.Name,
                "Property",
                csharpMember.Signature,
                type,
                [],
                NormalizeDocumentation(property.Value),
                null,
                OptionalAttribute(property, "default"),
                name,
                CreateXmlDocId('P', fullTypeName, csharpMember.Name, []),
                ToSourcePath(sourceRoot, path)));
        }

        foreach (var signal in root.Element("signals")?.Elements("signal") ?? [])
        {
            var name = RequiredAttribute(signal, "name", path);
            var parameters = signal.Elements("param").Select(ReadParameter).ToArray();
            var csharpMember = ResolveCSharpMember(csharpSnapshot, csharpClass, className, "Signal", name, "void", parameters);
            signals.Add(new ApiMember(
                csharpMember.Name,
                "Signal",
                csharpMember.Signature,
                null,
                parameters,
                NormalizeDocumentation(signal.Element("description")?.Value),
                null,
                null,
                name,
                CreateXmlDocId('E', fullTypeName, csharpMember.Name, parameters),
                ToSourcePath(sourceRoot, path)));
        }

        foreach (var constant in root.Element("constants")?.Elements("constant") ?? [])
        {
            var apiConstant = new ApiConstant(
                RequiredAttribute(constant, "name", path),
                OptionalAttribute(constant, "value"),
                OptionalAttribute(constant, "enum"),
                OptionalAttribute(constant, "enum") is null ? "Constant" : "EnumValue",
                NormalizeDocumentation(constant.Value),
                ToSourcePath(sourceRoot, path));
            constants.Add(apiConstant);
            if (!string.IsNullOrWhiteSpace(apiConstant.Enum))
            {
                if (!enumGroups.TryGetValue(apiConstant.Enum, out var values))
                {
                    values = [];
                enumGroups.Add(apiConstant.Enum, values);
                }

                values.Add(apiConstant);
            }
        }

        foreach (var apiOperator in root.Element("operators")?.Elements("operator") ?? [])
        {
            var name = RequiredAttribute(apiOperator, "name", path);
            var returnType = OptionalAttribute(apiOperator.Element("return"), "type") ?? "void";
            var parameters = apiOperator.Elements("param").Select(ReadParameter).ToArray();
            var csharpMember = ResolveCSharpMember(csharpSnapshot, csharpClass, className, "Operator", name, returnType, parameters);
            operators.Add(new ApiMember(
                csharpMember.Name,
                "Operator",
                csharpMember.Signature,
                returnType,
                parameters,
                NormalizeDocumentation(apiOperator.Element("description")?.Value),
                null,
                null,
                name,
                CreateXmlDocId('M', fullTypeName, csharpMember.Name, parameters),
                ToSourcePath(sourceRoot, path)));
        }

        ValidateMemberIdentities(className, constructors, members, rawMembers, signals, operators, virtualMethods);

        var relativePath = ToSourcePath(sourceRoot, path);
        return new ApiClassPacket(
            1,
            "godot",
            Baseline,
            GeneratorVersion,
            new ApiClassInfo(
                csharpClassName,
                "Godot",
                $"Godot.{csharpClassName}",
                "class",
                baseType,
                [],
                className,
                relativePath,
                $"https://docs.godotengine.org/en/4.7/classes/class_{className.ToLowerInvariant()}.html"),
            constructors,
            members,
            rawMembers,
            signals,
            enumGroups.Select(group => new ApiEnum(group.Key, group.Value.OrderBy(value => value.Name, StringComparer.Ordinal).ToArray())).OrderBy(group => group.Name, StringComparer.Ordinal).ToArray(),
            constants,
            operators,
            virtualMethods,
            CreateGodotSourceInputs(sourceRoot, relativePath, path));
    }

    private IReadOnlyList<ApiClassPacket> ReadElectron2DClasses(string manifestPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
        if (!ApiManifestCommand.TryGetArray(document.RootElement, "types", out var types))
        {
            throw new JsonException("Electron2D API manifest is missing types array.");
        }

        var packets = new List<ApiClassPacket>();
        foreach (var type in types.EnumerateArray())
        {
            var name = RequiredString(type, "name");
            var fullName = RequiredString(type, "fullName");
            var typeKind = OptionalString(type, "kind") ?? "class";
            var members = new List<ApiMember>();
            if (type.TryGetProperty("members", out var memberArray) && memberArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var member in memberArray.EnumerateArray())
                {
                    members.Add(new ApiMember(
                        RequiredString(member, "name"),
                        RequiredString(member, "kind"),
                        OptionalString(member, "signature"),
                        OptionalString(member, "returnType"),
                        ReadManifestParameters(member),
                        OptionalString(member, "summary"),
                        OptionalString(member, "value"),
                        null,
                        OptionalString(member, "godotName"),
                        OptionalString(member, "xmlDocId"),
                        "data/api/electron2d-api-manifest.json"));
                }
            }

            members = members.Select(NormalizeElectron2DMember).ToList();
            var virtualMethods = members.Where(IsElectron2DVirtualMethod).ToArray();
            var godotReference = TryReadProfileGodotReference(type) ?? name;
            packets.Add(new ApiClassPacket(
                1,
                "electron2d",
                OptionalString(document.RootElement, "godotBaseline") ?? Baseline,
                GeneratorVersion,
                new ApiClassInfo(
                    name,
                    OptionalString(type, "namespace") ?? "Electron2D",
                    fullName,
                    typeKind,
                    OptionalString(type, "baseType"),
                    ReadStringArray(type, "interfaces"),
                    godotReference,
                    "data/api/electron2d-api-manifest.json",
                    null),
                members.Where(member => string.Equals(member.Kind, "Constructor", StringComparison.Ordinal)).ToArray(),
                members,
                [],
                members.Where(member => string.Equals(member.Kind, "Signal", StringComparison.Ordinal)).ToArray(),
                [],
                members.Where(member => IsElectron2DConstant(member, fullName, typeKind)).Select(member => new ApiConstant(member.Name, member.Value, null, member.Kind, member.Summary, member.SourcePath)).ToArray(),
                members.Where(member => string.Equals(member.Kind, "Operator", StringComparison.Ordinal)).ToArray(),
                virtualMethods,
                [new ApiSourceInput("data/api/electron2d-api-manifest.json", ComputeSha256(manifestPath))]));
        }

        return packets;
    }

    private static ApiMember NormalizeElectron2DMember(ApiMember member)
    {
        if (IsLegacyElectron2DOperator(member))
        {
            return member with { Kind = "Operator" };
        }

        if (IsLegacyElectron2DConstant(member))
        {
            return member with { Kind = "Constant" };
        }

        return member;
    }

    private static bool IsElectron2DConstant(ApiMember member, string declaringTypeFullName, string declaringTypeKind)
    {
        return string.Equals(member.Kind, "Constant", StringComparison.Ordinal) ||
            string.Equals(member.Kind, "EnumValue", StringComparison.Ordinal) ||
            IsElectron2DValueSingletonConstant(member, declaringTypeFullName, declaringTypeKind);
    }

    private static bool IsElectron2DValueSingletonConstant(ApiMember member, string declaringTypeFullName, string declaringTypeKind)
    {
        if (!string.Equals(declaringTypeKind, "struct", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(member.Value) ||
            !string.Equals(member.ReturnType, declaringTypeFullName, StringComparison.Ordinal))
        {
            return false;
        }

        if (string.Equals(member.Kind, "Field", StringComparison.Ordinal))
        {
            return member.Signature?.StartsWith("public static ", StringComparison.Ordinal) == true;
        }

        return string.Equals(member.Kind, "Property", StringComparison.Ordinal) &&
            member.Signature?.StartsWith("public static ", StringComparison.Ordinal) == true &&
            member.Signature.Contains("{ get; }", StringComparison.Ordinal);
    }

    private static bool IsLegacyElectron2DOperator(ApiMember member)
    {
        return string.Equals(member.Kind, "Method", StringComparison.Ordinal) &&
            (member.Name.StartsWith("op_", StringComparison.Ordinal) ||
                ExtractXmlDocMemberName(member.XmlDocId).StartsWith("op_", StringComparison.Ordinal));
    }

    private static bool IsLegacyElectron2DConstant(ApiMember member)
    {
        return string.Equals(member.Kind, "Field", StringComparison.Ordinal) &&
            member.Signature?.StartsWith("public const ", StringComparison.Ordinal) == true;
    }

    private static bool IsElectron2DVirtualMethod(ApiMember member)
    {
        if (!string.Equals(member.Kind, "Method", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(member.XmlDocId) ||
            !member.XmlDocId.StartsWith("M:", StringComparison.Ordinal))
        {
            return false;
        }

        return ExtractXmlDocMemberName(member.XmlDocId).StartsWith("_", StringComparison.Ordinal);
    }

    private static string ExtractXmlDocMemberName(string? xmlDocId)
    {
        if (string.IsNullOrWhiteSpace(xmlDocId))
        {
            return string.Empty;
        }

        var openParameterList = xmlDocId.IndexOf('(', StringComparison.Ordinal);
        var memberId = openParameterList >= 0 ? xmlDocId[..openParameterList] : xmlDocId;
        var lastDot = memberId.LastIndexOf('.');
        return lastDot >= 0 && lastDot + 1 < memberId.Length
            ? memberId[(lastDot + 1)..]
            : string.Empty;
    }

    private static IReadOnlyList<ApiParameter> ReadManifestParameters(JsonElement member)
    {
        if (!member.TryGetProperty("parameters", out var parameters) || parameters.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return parameters.EnumerateArray()
            .Select(parameter => new ApiParameter(
                OptionalString(parameter, "name") ?? string.Empty,
                OptionalString(parameter, "type") ?? string.Empty,
                OptionalString(parameter, "defaultValue")))
            .ToArray();
    }

    private static ApiParameter ReadParameter(XElement parameter)
    {
        return new ApiParameter(
            OptionalAttribute(parameter, "name") ?? string.Empty,
            OptionalAttribute(parameter, "type") ?? string.Empty,
            OptionalAttribute(parameter, "default"));
    }

    private static bool IsVirtualMethod(XElement method)
    {
        var qualifiers = OptionalAttribute(method, "qualifiers");
        return qualifiers?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("virtual", StringComparer.Ordinal) == true;
    }

    private static string CreateXmlDocId(char prefix, string fullTypeName, string memberName, IReadOnlyList<ApiParameter> parameters)
    {
        var builder = new StringBuilder()
            .Append(prefix)
            .Append(':')
            .Append(fullTypeName)
            .Append('.')
            .Append(memberName);
        if (parameters.Count > 0)
        {
            builder
                .Append('(')
                .Append(string.Join(",", parameters.Select(parameter => string.IsNullOrWhiteSpace(parameter.Type) ? "object" : parameter.Type)))
                .Append(')');
        }

        return builder.ToString();
    }

    private static void ValidateMemberIdentities(
        string className,
        params IReadOnlyList<ApiMember>[] sections)
    {
        foreach (var section in sections)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in section)
            {
                var key = $"{member.Kind}|{member.Name}|{member.Signature}|{member.XmlDocId}";
                if (!seen.Add(key))
                {
                    throw new InvalidOperationException($"Godot API class '{className}' contains a duplicate generated member identity: {key}.");
                }
            }
        }
    }

    private string ToApiPath(string path)
    {
        return ToRepositoryPath(path);
    }

    private ApiMatrixGenerationException InvalidCSharpSnapshot(string path, string message)
    {
        return new ApiMatrixGenerationException(
            "E2D-BUILD-API-CSHARP-SNAPSHOT-INVALID",
            $"Godot C# API snapshot is incompatible: {message}.",
            ToApiPath(path));
    }

    private ApiMatrixGenerationException InvalidGodotSource(string path, string message)
    {
        return new ApiMatrixGenerationException(
            "E2D-BUILD-API-GODOT-SOURCE-INVALID",
            $"Godot API source snapshot is invalid: {message}.",
            ToApiPath(path));
    }

    private static ApiMatrixGenerationException InvalidGeneratedOutputPath(string relativePath, string message)
    {
        return new ApiMatrixGenerationException(
            "E2D-BUILD-API-GENERATED-PATH-INVALID",
            $"Generated API output path is invalid: {message}.",
            relativePath);
    }

    private void ValidateCSharpSnapshotClassIdentity(string path, string identity, string context)
    {
        if (!IsSafeGeneratedClassIdentity(identity))
        {
            throw InvalidCSharpSnapshot(path, $"{context} '{identity}' is not a safe generated class identity");
        }
    }

    private void ValidateGodotSourceClassIdentity(string path, string identity)
    {
        if (!IsSafeGeneratedClassIdentity(identity))
        {
            throw InvalidGodotSource(path, $"class name '{identity}' is not a safe generated class identity");
        }
    }

    private static bool IsSafeGeneratedClassIdentity(string value)
    {
        return !string.IsNullOrEmpty(value) &&
            string.Equals(value, value.Trim(), StringComparison.Ordinal) &&
            GeneratedClassIdentityPattern.IsMatch(value);
    }

    private string RequiredSnapshotString(JsonElement element, string propertyName, string path)
    {
        var value = OptionalString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw InvalidCSharpSnapshot(path, $"required string property '{propertyName}' is missing");
        }

        return value;
    }

    private IReadOnlyList<ApiParameter> ReadSnapshotParameters(JsonElement member, string path)
    {
        if (!member.TryGetProperty("parameters", out var parameters))
        {
            return [];
        }

        if (parameters.ValueKind != JsonValueKind.Array)
        {
            throw InvalidCSharpSnapshot(path, "member parameters must be an array");
        }

        return parameters.EnumerateArray()
            .Select(parameter =>
            {
                if (parameter.ValueKind != JsonValueKind.Object)
                {
                    throw InvalidCSharpSnapshot(path, "member parameter entries must be objects");
                }

                return new ApiParameter(
                    RequiredSnapshotString(parameter, "name", path),
                    RequiredSnapshotString(parameter, "type", path),
                    OptionalString(parameter, "defaultValue"));
            })
            .ToArray();
    }

    private void ValidateCSharpSnapshotMembers(string path, string className, IReadOnlyList<GodotCSharpMember> members)
    {
        var seen = new Dictionary<string, GodotCSharpMember>(StringComparer.Ordinal);
        var projections = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var member in members)
        {
            ValidateCSharpSnapshotMemberProjection(path, className, member);
            var key = CreateCSharpBindingKey(member.Kind, member.GodotName, member.Parameters);
            if (seen.TryGetValue(key, out var previous))
            {
                var previousProjection = CreateCSharpProjectionKey(previous);
                var currentProjection = CreateCSharpProjectionKey(member);
                var conflictKind = string.Equals(previousProjection, currentProjection, StringComparison.Ordinal)
                    ? "duplicate"
                    : "conflicting";
                throw InvalidCSharpSnapshot(path, $"class '{className}' contains {conflictKind} member binding '{key}'");
            }

            seen.Add(key, member);
            var projectionKey = CreateCSharpProjectionKey(member);
            if (projections.TryGetValue(projectionKey, out var previousBindingKey))
            {
                throw InvalidCSharpSnapshot(path, $"class '{className}' contains duplicate C# member projection '{projectionKey}' for bindings '{previousBindingKey}' and '{key}'");
            }

            projections.Add(projectionKey, key);
        }
    }

    private void ValidateCSharpSnapshotMemberProjection(string path, string className, GodotCSharpMember member)
    {
        if (!IsSafeCSharpMemberProjectionName(member.Kind, member.Name))
        {
            throw InvalidCSharpSnapshot(path, $"class '{className}' member '{member.GodotName}' has unsafe C# member name '{member.Name}'");
        }

        if (!IsSafeCSharpMemberSignature(member.Kind, member.Name, member.Signature))
        {
            throw InvalidCSharpSnapshot(path, $"class '{className}' member '{member.GodotName}' has unsafe C# signature '{member.Signature}'");
        }
    }

    private static bool IsUnmappableGodotXmlProperty(string godotName, string returnType)
    {
        var csharpName = ToGodotCSharpMemberName(godotName);
        var signature = $"public {returnType} {csharpName} {{ get; set; }}";
        return !IsSafeCSharpMemberProjectionName("Property", csharpName) ||
            !IsSafeCSharpMemberSignature("Property", csharpName, signature);
    }

    private static bool IsSafeCSharpMemberProjectionName(string kind, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return kind switch
        {
            "Constructor" => IsSafeGeneratedClassIdentity(name),
            "Operator" => AllowedCSharpOperatorNames.Contains(name),
            _ => CSharpMemberIdentifierPattern.IsMatch(name)
        };
    }

    private static bool IsSafeCSharpMemberSignature(string kind, string name, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature) ||
            signature.Contains('\\', StringComparison.Ordinal) ||
            signature.Contains("{index}", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (ContainsUnescapedCSharpKeywordParameter(signature))
        {
            return false;
        }

        return string.Equals(kind, "Operator", StringComparison.Ordinal) && AllowedCSharpOperatorNames.Contains(name) ||
            !signature.Contains('/', StringComparison.Ordinal);
    }

    private static bool ContainsUnescapedCSharpKeywordParameter(string signature)
    {
        var open = signature.IndexOf('(', StringComparison.Ordinal);
        var close = signature.LastIndexOf(')');
        if (open < 0 || close <= open + 1)
        {
            return false;
        }

        foreach (var parameter in SplitCSharpParameterList(signature[(open + 1)..close]))
        {
            var name = ReadCSharpParameterName(parameter);
            if (name.Length == 0 || name.StartsWith('@'))
            {
                continue;
            }

            if (CSharpReservedKeywords.Contains(name))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> SplitCSharpParameterList(string value)
    {
        var start = 0;
        var angleDepth = 0;
        for (var i = 0; i < value.Length; i++)
        {
            switch (value[i])
            {
                case '<':
                    angleDepth++;
                    break;
                case '>' when angleDepth > 0:
                    angleDepth--;
                    break;
                case ',' when angleDepth == 0:
                    yield return value[start..i].Trim();
                    start = i + 1;
                    break;
            }
        }

        yield return value[start..].Trim();
    }

    private static string ReadCSharpParameterName(string declaration)
    {
        var trimmed = declaration.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var lastSpace = trimmed.LastIndexOf(' ');
        var name = lastSpace >= 0 ? trimmed[(lastSpace + 1)..] : trimmed;
        return name.TrimEnd(',', ')');
    }

    private static string CreateCSharpBindingKey(string kind, string godotName, IReadOnlyList<ApiParameter> parameters)
    {
        return $"{kind}|{godotName}|{string.Join(",", parameters.Select(parameter => NormalizeCSharpBindingType(parameter.Type)))}";
    }

    private static string CreateCSharpProjectionKey(GodotCSharpMember member)
    {
        return $"{member.Name}|{member.Signature}";
    }

    private static string NormalizeCSharpBindingType(string type)
    {
        return type.Trim();
    }

    private static bool ParametersMatch(IReadOnlyList<ApiParameter> left, IReadOnlyList<ApiParameter> right)
    {
        if (left.Count == 0 && right.Count == 0)
        {
            return true;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(NormalizeCSharpBindingType(left[i].Type), NormalizeCSharpBindingType(right[i].Type), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private GodotCSharpSnapshot ReadGodotCSharpSnapshot(string sourceRoot)
    {
        var path = Path.Combine(sourceRoot, "csharp_api.json");
        if (!File.Exists(path))
        {
            return new GodotCSharpSnapshot(false, false, path, new Dictionary<string, GodotCSharpClass>(StringComparer.Ordinal));
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw InvalidCSharpSnapshot(path, "root element must be an object");
        }

        var baseline = OptionalString(document.RootElement, "baseline");
        if (!string.Equals(baseline, Baseline, StringComparison.Ordinal))
        {
            throw InvalidCSharpSnapshot(path, $"baseline must be '{Baseline}'");
        }

        var sourceKind = OptionalString(document.RootElement, "sourceKind");
        var isSynthetic = string.Equals(sourceKind, "synthetic-godot-xml", StringComparison.Ordinal);

        if (!document.RootElement.TryGetProperty("classes", out var classes) || classes.ValueKind != JsonValueKind.Array)
        {
            throw InvalidCSharpSnapshot(path, "classes array is required");
        }

        var result = new Dictionary<string, GodotCSharpClass>(StringComparer.Ordinal);
        var csharpNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in classes.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw InvalidCSharpSnapshot(path, "class entries must be objects");
            }

            var name = RequiredSnapshotString(item, "name", path);
            ValidateCSharpSnapshotClassIdentity(path, name, "class name");
            var csharpName = ReadCSharpClassName(item, path, name);
            if (!csharpNames.Add(csharpName))
            {
                throw InvalidCSharpSnapshot(path, $"classes array contains duplicate C# class name '{csharpName}'");
            }

            var members = new List<GodotCSharpMember>();
            if (!item.TryGetProperty("members", out var memberArray) || memberArray.ValueKind != JsonValueKind.Array)
            {
                throw InvalidCSharpSnapshot(path, $"class '{name}' must contain a members array");
            }

            foreach (var member in memberArray.EnumerateArray())
            {
                if (member.ValueKind != JsonValueKind.Object)
                {
                    throw InvalidCSharpSnapshot(path, $"class '{name}' member entries must be objects");
                }

                var godotName = RequiredSnapshotString(member, "godotName", path);
                var memberName = RequiredSnapshotString(member, "name", path);
                var kind = RequiredSnapshotString(member, "kind", path);
                var signature = RequiredSnapshotString(member, "signature", path);
                var parameters = ReadSnapshotParameters(member, path);
                members.Add(new GodotCSharpMember(godotName, memberName, kind, signature, parameters));
            }

            ValidateCSharpSnapshotMembers(path, name, members);
            if (!result.TryAdd(name, new GodotCSharpClass(name, csharpName, members)))
            {
                throw InvalidCSharpSnapshot(path, $"classes array contains duplicate class '{name}'");
            }
        }

        return new GodotCSharpSnapshot(true, isSynthetic, path, result);
    }

    private string ReadCSharpClassName(JsonElement item, string path, string godotClassName)
    {
        if (!item.TryGetProperty("csharpName", out var value))
        {
            ValidateCSharpSnapshotClassIdentity(path, godotClassName, $"class '{godotClassName}' fallback csharpName");
            return godotClassName;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw InvalidCSharpSnapshot(path, $"class '{godotClassName}' csharpName must be a string");
        }

        var csharpName = value.GetString();
        if (string.IsNullOrWhiteSpace(csharpName))
        {
            throw InvalidCSharpSnapshot(path, $"class '{godotClassName}' csharpName must not be empty");
        }

        ValidateCSharpSnapshotClassIdentity(path, csharpName, $"class '{godotClassName}' csharpName");
        return csharpName;
    }

    private GodotCSharpMember ResolveCSharpMember(
        GodotCSharpSnapshot csharpSnapshot,
        GodotCSharpClass? csharpClass,
        string className,
        string kind,
        string godotName,
        string returnType,
        IReadOnlyList<ApiParameter> parameters)
    {
        var matches = csharpClass?.Members.Where(item =>
            string.Equals(item.Kind, kind, StringComparison.Ordinal) &&
            string.Equals(item.GodotName, godotName, StringComparison.Ordinal) &&
            ParametersMatch(item.Parameters, parameters)).ToArray();
        if (matches is { Length: 1 })
        {
            return matches[0];
        }

        if (csharpSnapshot.HasFile)
        {
            var key = CreateCSharpBindingKey(kind, godotName, parameters);
            var message = matches is { Length: > 1 }
                ? $"class '{className}' contains multiple C# snapshot entries for member binding '{key}'"
                : $"class '{className}' is missing C# snapshot member binding '{key}'";
            throw InvalidCSharpSnapshot(csharpSnapshot.Path!, message);
        }

        var csharpName = string.Equals(kind, "Operator", StringComparison.Ordinal)
            ? godotName
            : ToGodotCSharpMemberName(godotName);
        var signature = kind switch
        {
            "Constructor" => $"public {csharpName}({string.Join(", ", parameters.Select(FormatParameterSignature))})",
            "Property" => $"public {returnType} {csharpName} {{ get; set; }}",
            "Operator" => $"public {returnType} {godotName}({string.Join(", ", parameters.Select(FormatParameterSignature))})",
            "Signal" => $"public event {csharpName}Handler {csharpName}",
            _ => FormatMethodSignature(returnType, csharpName, parameters)
        };
        return new GodotCSharpMember(godotName, csharpName, kind, signature, parameters);
    }

    private GodotCSharpMember? ResolveCSharpPropertyMember(
        GodotCSharpSnapshot csharpSnapshot,
        GodotCSharpClass? csharpClass,
        string className,
        string godotName,
        string returnType)
    {
        var matches = csharpClass?.Members.Where(item =>
            string.Equals(item.Kind, "Property", StringComparison.Ordinal) &&
            string.Equals(item.GodotName, godotName, StringComparison.Ordinal) &&
            ParametersMatch(item.Parameters, [])).ToArray();
        if (matches is { Length: 1 })
        {
            return matches[0];
        }

        if (IsUnmappableGodotXmlProperty(godotName, returnType))
        {
            return null;
        }

        if (csharpSnapshot.HasFile)
        {
            var key = CreateCSharpBindingKey("Property", godotName, []);
            var message = matches is { Length: > 1 }
                ? $"class '{className}' contains multiple C# snapshot entries for member binding '{key}'"
                : $"class '{className}' is missing C# snapshot member binding '{key}'";
            throw InvalidCSharpSnapshot(csharpSnapshot.Path!, message);
        }

        var csharpName = ToGodotCSharpMemberName(godotName);
        var signature = $"public {returnType} {csharpName} {{ get; set; }}";
        return new GodotCSharpMember(godotName, csharpName, "Property", signature, []);
    }

    private IReadOnlyList<ApiSourceInput> CreateGodotSourceInputs(string sourceRoot, string relativePath, string xmlPath)
    {
        var inputs = new List<ApiSourceInput>
        {
            new(relativePath, ComputeSha256(xmlPath))
        };
        var csharpSnapshot = Path.Combine(sourceRoot, "csharp_api.json");
        if (File.Exists(csharpSnapshot))
        {
            inputs.Add(new ApiSourceInput("csharp_api.json", ComputeSha256(csharpSnapshot)));
        }

        return inputs;
    }

    private static ApiIndex CreateIndex(string source, IReadOnlyList<ApiClassPacket> packets)
    {
        return new ApiIndex(
            1,
            source,
            Baseline,
            GeneratorVersion,
            packets.Select(packet => new ApiIndexClass(packet.Class.Name, packet.Class.FullName, packet.Class.BaseType, packet.Members.Count, PacketJsonPath(source, packet.Class.Name))).ToArray());
    }

    private static void AddPacketFiles(List<ApiGeneratedFile> files, string outputDirectory, ApiClassPacket packet)
    {
        files.Add(new ApiGeneratedFile($"{outputDirectory}/{packet.Class.Name}.api.json", Serialize(packet)));
    }

    private static string PacketJsonPath(string source, string className)
    {
        return source == "godot"
            ? $"data/api/godot-4.7/classes/{className}.api.json"
            : $"data/api/electron2d/classes/{className}.api.json";
    }

    private async Task DownloadGodotSourceAsync(string version, string outputPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputPath);
        ClearGodotApiInputDirectory(outputPath);
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        await using var stream = await client.GetStreamAsync($"https://codeload.github.com/godotengine/godot/zip/refs/tags/{version}", cancellationToken).ConfigureAwait(false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            var relative = StripArchiveRoot(entry.FullName);
            if (relative.Length == 0 || entry.FullName.EndsWith('/'))
            {
                continue;
            }

            if (!IsGodotApiInput(relative))
            {
                continue;
            }

            var targetPath = Path.Combine(outputPath, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            entry.ExtractToFile(targetPath, overwrite: true);
        }
    }

    private void EnsureCSharpSnapshot(string sourceRoot)
    {
        var path = Path.Combine(sourceRoot, "csharp_api.json");
        if (File.Exists(path))
        {
            _ = ReadGodotCSharpSnapshot(sourceRoot);
            return;
        }

        WriteCSharpSnapshot(sourceRoot);
    }

    private void WriteCSharpSnapshot(string sourceRoot)
    {
        var classes = EnumerateGodotXmlFiles(sourceRoot)
            .Order(StringComparer.Ordinal)
            .Select(path =>
            {
                var document = XDocument.Load(path);
                var root = document.Root;
                var name = root?.Attribute("name")?.Value ?? Path.GetFileNameWithoutExtension(path);
                ValidateGodotSourceClassIdentity(path, name);
                return new
                {
                    name,
                    csharpName = name,
                    @namespace = "Godot",
                    members = ReadGodotCSharpSnapshotMembers(root).ToArray()
                };
            })
            .ToArray();
        var snapshot = new
        {
            baseline = Baseline,
            sourceKind = "synthetic-godot-xml",
            classes
        };
        var path = Path.Combine(sourceRoot, "csharp_api.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Serialize(snapshot), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static IEnumerable<object> ReadGodotCSharpSnapshotMembers(XElement? root)
    {
        if (root is null)
        {
            yield break;
        }

        foreach (var constructor in root.Element("constructors")?.Elements("constructor") ?? [])
        {
            var name = constructor.Attribute("name")?.Value ?? string.Empty;
            var parameters = constructor.Elements("param").Select(ReadParameter).ToArray();
            yield return new
            {
                name = ToGodotCSharpMemberName(name),
                godotName = name,
                kind = "Constructor",
                signature = $"public {ToGodotCSharpMemberName(name)}({string.Join(", ", parameters.Select(FormatParameterSignature))})",
                parameters
            };
        }

        foreach (var method in root.Element("methods")?.Elements("method") ?? [])
        {
            var name = method.Attribute("name")?.Value ?? string.Empty;
            var csharpName = ToGodotCSharpMemberName(name);
            var parameters = method.Elements("param").Select(ReadParameter).ToArray();
            yield return new
            {
                name = csharpName,
                godotName = name,
                kind = "Method",
                signature = FormatMethodSignature(method.Element("return")?.Attribute("type")?.Value ?? "void", csharpName, parameters),
                parameters
            };
        }

        foreach (var property in root.Element("members")?.Elements("member") ?? [])
        {
            var name = property.Attribute("name")?.Value ?? string.Empty;
            var type = property.Attribute("type")?.Value ?? "object";
            if (IsUnmappableGodotXmlProperty(name, type))
            {
                continue;
            }

            var csharpName = ToGodotCSharpMemberName(name);
            var signature = $"public {type} {csharpName} {{ get; set; }}";
            yield return new
            {
                name = csharpName,
                godotName = name,
                kind = "Property",
                signature,
                parameters = Array.Empty<ApiParameter>()
            };
        }

        foreach (var signal in root.Element("signals")?.Elements("signal") ?? [])
        {
            var name = signal.Attribute("name")?.Value ?? string.Empty;
            var csharpName = ToGodotCSharpMemberName(name);
            var parameters = signal.Elements("param").Select(ReadParameter).ToArray();
            yield return new
            {
                name = csharpName,
                godotName = name,
                kind = "Signal",
                signature = $"public event {csharpName}Handler {csharpName}",
                parameters
            };
        }

        foreach (var apiOperator in root.Element("operators")?.Elements("operator") ?? [])
        {
            var name = apiOperator.Attribute("name")?.Value ?? string.Empty;
            var parameters = apiOperator.Elements("param").Select(ReadParameter).ToArray();
            yield return new
            {
                name,
                godotName = name,
                kind = "Operator",
                signature = $"public {apiOperator.Element("return")?.Attribute("type")?.Value ?? "void"} {name}({string.Join(", ", parameters.Select(FormatParameterSignature))})",
                parameters
            };
        }
    }

    private static string FormatMethodSignature(string returnType, string name, IReadOnlyList<ApiParameter> parameters)
    {
        return $"public {returnType} {name}({string.Join(", ", parameters.Select(FormatParameterSignature))})";
    }

    private static string FormatParameterSignature(ApiParameter parameter)
    {
        var type = string.IsNullOrWhiteSpace(parameter.Type) ? "object" : parameter.Type;
        var name = string.IsNullOrWhiteSpace(parameter.Name) ? "arg" : EscapeCSharpIdentifier(ToCamelCase(parameter.Name));
        return $"{type} {name}";
    }

    private static string EscapeCSharpIdentifier(string name)
    {
        return CSharpReservedKeywords.Contains(name) ? $"@{name}" : name;
    }

    private static string ToPascalCase(string value)
    {
        var builder = new StringBuilder();
        foreach (var part in value.Split('_', StringSplitOptions.RemoveEmptyEntries))
        {
            builder.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
            {
                builder.Append(part[1..]);
            }
        }

        return builder.Length == 0 ? value : builder.ToString();
    }

    private static string ToGodotCSharpMemberName(string value)
    {
        if (value.StartsWith('_') && value.Length > 1)
        {
            return "_" + ToPascalCase(value[1..]);
        }

        return ToPascalCase(value);
    }

    private static string ToCamelCase(string value)
    {
        var pascalCase = ToPascalCase(value);
        if (pascalCase.Length == 0)
        {
            return value;
        }

        return char.ToLowerInvariant(pascalCase[0]) + pascalCase[1..];
    }

    private string ResolveGodotSourcePath(string? explicitPath)
    {
        if (explicitPath is not null)
        {
            return ResolveRepositoryOrAbsolutePath(explicitPath);
        }

        var trackedSource = ResolveRepositoryOrAbsolutePath("data/api/godot-4.7/source");
        return Directory.Exists(trackedSource)
            ? trackedSource
            : ResolveRepositoryOrAbsolutePath(Path.Combine(".temp", "api", "godot-4.7", "source"));
    }

    private static IEnumerable<string> EnumerateGodotXmlFiles(string sourceRoot)
    {
        var docClasses = Path.Combine(sourceRoot, "doc", "classes");
        if (Directory.Exists(docClasses))
        {
            foreach (var file in Directory.EnumerateFiles(docClasses, "*.xml", SearchOption.TopDirectoryOnly))
            {
                yield return file;
            }
        }

        var modules = Path.Combine(sourceRoot, "modules");
        if (Directory.Exists(modules))
        {
            foreach (var directory in Directory.EnumerateDirectories(modules, "doc_classes", SearchOption.AllDirectories))
            {
                foreach (var file in Directory.EnumerateFiles(directory, "*.xml", SearchOption.TopDirectoryOnly))
                {
                    yield return file;
                }
            }
        }
    }

    private static bool IsGodotApiInput(string relativePath)
    {
        return (relativePath.StartsWith("doc/classes/", StringComparison.Ordinal) && relativePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) ||
            (relativePath.StartsWith("modules/", StringComparison.Ordinal) && relativePath.Contains("/doc_classes/", StringComparison.Ordinal) && relativePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(relativePath, "extension_api.json", StringComparison.Ordinal) ||
            string.Equals(relativePath, "csharp_api.json", StringComparison.Ordinal);
    }

    private static string StripArchiveRoot(string path)
    {
        var normalized = path.Replace('\\', '/');
        var separator = normalized.IndexOf('/');
        return separator < 0 ? string.Empty : normalized[(separator + 1)..];
    }

    private static string RequiredAttribute(XElement element, string name, string path)
    {
        return OptionalAttribute(element, name) ?? throw new InvalidOperationException($"Required XML attribute '{name}' is missing in {path}.");
    }

    private static string? OptionalAttribute(XElement? element, string name)
    {
        return element?.Attribute(name)?.Value;
    }

    private static string RequiredString(JsonElement element, string name)
    {
        return OptionalString(element, name) ?? throw new JsonException($"Required JSON property '{name}' is missing.");
    }

    private static string? OptionalString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString() ?? string.Empty).ToArray();
    }

    private static string? TryReadProfileGodotReference(JsonElement type)
    {
        if (type.TryGetProperty("profile", out var profile) && profile.ValueKind == JsonValueKind.Object)
        {
            return OptionalString(profile, "godotReference");
        }

        return null;
    }

    private string ToSourcePath(string sourceRoot, string path)
    {
        return Path.GetRelativePath(sourceRoot, path).Replace('\\', '/');
    }

    private string ResolveRepositoryOrAbsolutePath(string path)
    {
        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar)));
    }

    private string ToRepositoryPath(string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions) + "\n";
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private static string NormalizeDocumentation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Replace("https://docs.godotengine.org/en/stable/", "https://docs.godotengine.org/en/4.7/", StringComparison.Ordinal);
        return MaskWindowsAbsolutePaths(normalized);
    }

    private static string MaskWindowsAbsolutePaths(string value)
    {
        StringBuilder? builder = null;
        var lastCopied = 0;
        var index = 0;
        while (index < value.Length)
        {
            if (!IsWindowsAbsolutePathStart(value, index))
            {
                index++;
                continue;
            }

            var end = FindWindowsAbsolutePathEnd(value, index + 3);
            builder ??= new StringBuilder(value.Length);
            builder.Append(value, lastCopied, index - lastCopied);
            builder.Append(WindowsAbsolutePathPlaceholder);
            lastCopied = end;
            index = end;
        }

        if (builder is null)
        {
            return value;
        }

        builder.Append(value, lastCopied, value.Length - lastCopied);
        return builder.ToString();
    }

    private static bool IsWindowsAbsolutePathStart(string value, int index)
    {
        if (index + 2 >= value.Length ||
            !IsAsciiLetter(value[index]) ||
            value[index + 1] != ':' ||
            !IsWindowsPathSeparator(value[index + 2]))
        {
            return false;
        }

        return index == 0 || !IsPathPrefixCharacter(value[index - 1]);
    }

    private static int FindWindowsAbsolutePathEnd(string value, int index)
    {
        var end = index;
        while (end < value.Length)
        {
            var current = value[end];
            if (IsWindowsPathTerminator(value, end))
            {
                break;
            }

            if (char.IsWhiteSpace(current) && !WhitespaceContinuesWindowsPath(value, end))
            {
                break;
            }

            if (current == ')' && (end + 1 >= value.Length || !IsWindowsPathSeparator(value[end + 1])))
            {
                break;
            }

            end++;
        }

        while (end > index && char.IsWhiteSpace(value[end - 1]))
        {
            end--;
        }

        return end;
    }

    private static bool IsWindowsPathTerminator(string value, int index)
    {
        return value[index] is '[' or ']' or '"' or '\'' or '<' or '>' or '\r' or '\n';
    }

    private static bool WhitespaceContinuesWindowsPath(string value, int whitespaceIndex)
    {
        var lookahead = whitespaceIndex;
        while (lookahead < value.Length && char.IsWhiteSpace(value[lookahead]))
        {
            lookahead++;
        }

        if (lookahead >= value.Length ||
            value[lookahead] is '-' or '[' or ']' or '"' or '\'' or '<' or '>')
        {
            return false;
        }

        for (var i = lookahead; i < value.Length; i++)
        {
            if (IsWindowsPathTerminator(value, i))
            {
                return false;
            }

            if (char.IsWhiteSpace(value[i]) && NextNonWhitespaceIs(value, i, '-'))
            {
                return false;
            }

            if (IsWindowsPathSeparator(value[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool NextNonWhitespaceIs(string value, int index, char expected)
    {
        var lookahead = index;
        while (lookahead < value.Length && char.IsWhiteSpace(value[lookahead]))
        {
            lookahead++;
        }

        return lookahead < value.Length && value[lookahead] == expected;
    }

    private static bool IsAsciiLetter(char value)
    {
        return value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    private static bool IsWindowsPathSeparator(char value)
    {
        return value is '\\' or '/';
    }

    private static bool IsPathPrefixCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value is '_' or '-' or '.' or '/' or '\\';
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void CopyGodotApiInputFiles(string sourcePath, string outputPath)
    {
        foreach (var file in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourcePath, file).Replace('\\', '/');
            if (!IsGodotApiInput(relative))
            {
                continue;
            }

            var target = Path.Combine(outputPath, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static void ClearGodotApiInputDirectory(string outputPath)
    {
        if (!Directory.Exists(outputPath))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(outputPath, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(outputPath, file).Replace('\\', '/');
            if (IsGodotApiInput(relative))
            {
                File.Delete(file);
            }
        }
    }

    private void ClearGeneratedOutputDirectories(IReadOnlyList<ApiGeneratedFile> files)
    {
        foreach (var directory in files
            .Select(file => Path.GetDirectoryName(ResolveGeneratedOutputPath(file.RelativePath))!)
            .Distinct(FileSystemPathComparer))
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.api.json", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(directory, "*.api.md", SearchOption.TopDirectoryOnly))
                .Concat(Directory.EnumerateFiles(directory, "classes.json", SearchOption.TopDirectoryOnly)))
            {
                File.Delete(file);
            }
        }
    }

    private static FetchArguments ParseFetchArguments(string[] args)
    {
        string? version = null;
        string? source = null;
        string? output = null;
        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--version" when i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]):
                    version = args[++i];
                    break;
                case "--source" when i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]):
                    source = args[++i];
                    break;
                case "--output" when i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]):
                    output = args[++i];
                    break;
                default:
                    return new FetchArguments(false, version, source, output, "Expected: api fetch-godot --version <version> [--source <path>] [--output <path>].");
            }
        }

        return string.IsNullOrWhiteSpace(version)
            ? new FetchArguments(false, version, source, output, "Expected: api fetch-godot --version <version> [--source <path>] [--output <path>].")
            : new FetchArguments(true, version, source, output, string.Empty);
    }

    private static GenerateArguments ParseGenerateArguments(string[] args, string step)
    {
        var check = false;
        string? godotSource = null;
        string? manifest = null;
        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--check":
                    check = true;
                    break;
                case "--godot-source" when i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]):
                    godotSource = args[++i];
                    break;
                case "--electron2d-manifest" when i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]):
                    manifest = args[++i];
                    break;
                default:
                    return new GenerateArguments(false, step, check, godotSource, manifest, $"Expected: {step} [--check] [--godot-source <path>] [--electron2d-manifest <path>].");
            }
        }

        return new GenerateArguments(true, step, check, godotSource, manifest, string.Empty);
    }

    private int UnknownApiCommand(string command)
    {
        diagnostics.Write(new BuildDiagnostic("api", "api", "error", "E2D-BUILD-CLI-UNKNOWN-COMMAND", $"Unknown API command '{command}'."));
        return RepositoryBuildExitCodes.Failed;
    }

    private void WriteInvalidArguments(string message)
    {
        diagnostics.Write(new BuildDiagnostic("api", "api", "error", "E2D-BUILD-CLI-INVALID-ARGUMENTS", message));
    }

    private sealed record FetchArguments(bool Succeeded, string? Version, string? SourcePath, string? OutputPath, string ErrorMessage);
    private sealed record GenerateArguments(bool Succeeded, string Step, bool Check, string? GodotSourcePath, string? Electron2DManifestPath, string ErrorMessage);
    private sealed record ApiOutputBuildResult(bool Succeeded, IReadOnlyList<ApiGeneratedFile> Files)
    {
        public static ApiOutputBuildResult Failed { get; } = new(false, []);
    }

    private sealed record ApiGeneratedFile(string RelativePath, string Content);
    private sealed record ApiClassPacket(int SchemaVersion, string Source, string Baseline, string GeneratorVersion, ApiClassInfo Class, IReadOnlyList<ApiMember> Constructors, IReadOnlyList<ApiMember> Members, IReadOnlyList<ApiMember> RawMembers, IReadOnlyList<ApiMember> Signals, IReadOnlyList<ApiEnum> Enums, IReadOnlyList<ApiConstant> Constants, IReadOnlyList<ApiMember> Operators, IReadOnlyList<ApiMember> VirtualMethods, IReadOnlyList<ApiSourceInput> SourceInputs);
    private sealed record ApiClassInfo(string Name, string Namespace, string FullName, string Kind, string? BaseType, IReadOnlyList<string> Interfaces, string GodotReference, string DocumentationPath, string? DocumentationUrl);
    private sealed record ApiMember(string Name, string Kind, string? Signature, string? ReturnType, IReadOnlyList<ApiParameter> Parameters, string? Summary, string? Value, string? DefaultValue, string? GodotName, string? XmlDocId, string SourcePath);
    private sealed record ApiParameter(string Name, string Type, string? DefaultValue);
    private sealed record ApiConstant(string Name, string? Value, string? Enum, string? Kind, string? Summary, string SourcePath);
    private sealed record ApiEnum(string Name, IReadOnlyList<ApiConstant> Values);
    private sealed record ApiSourceInput(string Path, string Sha256);
    private sealed record ApiIndex(int SchemaVersion, string Source, string Baseline, string GeneratorVersion, IReadOnlyList<ApiIndexClass> Classes);
    private sealed record ApiIndexClass(string Name, string FullName, string? BaseType, int MemberCount, string JsonPath);
    private sealed record GodotCSharpSnapshot(bool HasFile, bool IsSynthetic, string? Path, IReadOnlyDictionary<string, GodotCSharpClass> Classes);
    private sealed record GodotCSharpClass(string Name, string CSharpName, IReadOnlyList<GodotCSharpMember> Members);
    private sealed record GodotCSharpMember(string GodotName, string Name, string Kind, string? Signature, IReadOnlyList<ApiParameter> Parameters);

    private sealed class ApiMatrixGenerationException(string code, string message, string? diagnosticPath = null) : Exception(message)
    {
        public string Code { get; } = code;
        public string? DiagnosticPath { get; } = diagnosticPath;
    }
}
