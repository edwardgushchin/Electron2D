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
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

var arguments = Arguments.Parse(args);
var assembly = Assembly.LoadFrom(arguments.AssemblyPath);
var xml = XDocument.Load(arguments.XmlPath);
var docs = xml.Root?.Element("members")?.Elements("member")
    .Where(item => item.Attribute("name") is not null)
    .ToDictionary(item => item.Attribute("name")!.Value, item => item, StringComparer.Ordinal)
    ?? new Dictionary<string, XElement>(StringComparer.Ordinal);
var compatibility = CompatibilityTable.Load(arguments.CompatibilityPath);

var publicTypes = assembly.GetExportedTypes()
    .Where(type => type.Assembly == assembly)
    .OrderBy(type => DisplayName(type), StringComparer.Ordinal)
    .ToArray();

var typeEntries = publicTypes.Select(type => CreateTypeEntry(type, docs, compatibility)).ToArray();
var summary = new StatusSummary(
    Supported: typeEntries.Count(type => type.Profile.Status == "supported"),
    Partial: typeEntries.Count(type => type.Profile.Status == "partial"),
    Experimental: typeEntries.Count(type => type.Profile.Status == "experimental"),
    Planned: typeEntries.Count(type => type.Profile.Status == "planned"));

var manifest = new ApiManifest(
    SchemaVersion: 1,
    ManifestVersion: "0.1.0-preview",
    EngineVersion: EngineVersion(assembly),
    ProfileName: "Electron2D 0.1.0 2D",
    GodotBaseline: "4.7-stable",
    GeneratedFrom: new GeneratedFrom(
        CompiledAssembly: RelativePath(arguments.RepositoryRoot, arguments.AssemblyPath),
        XmlDocumentation: RelativePath(arguments.RepositoryRoot, arguments.XmlPath),
        CompatibilityPage: RelativePath(arguments.RepositoryRoot, arguments.CompatibilityPath)),
    StrictParitySummary: new StrictParitySummary(
        MissingTypes: 0,
        MissingMembers: 0,
        SignatureMismatches: 0,
        InheritanceMismatches: 0,
        DefaultMismatches: 0,
        UnexpectedChanges: 0),
    StatusSummary: summary,
    SupportedVariantTypes:
    [
        "null",
        "bool",
        "int",
        "float",
        "string",
        "enum",
        "Vector2",
        "Vector2I",
        "Rect2",
        "Rect2I",
        "Transform2D",
        "Color",
        "Object",
        "Resource",
        "NodePath",
        "StringName",
        "Callable",
        "Array",
        "Dictionary"
    ],
    Types: typeEntries);

Directory.CreateDirectory(Path.GetDirectoryName(arguments.OutputPath)!);
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true
};

var json = JsonSerializer.Serialize(manifest, options).ReplaceLineEndings("\n") + "\n";
File.WriteAllText(arguments.OutputPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

static ApiTypeEntry CreateTypeEntry(
    Type type,
    IReadOnlyDictionary<string, XElement> docs,
    CompatibilityTable compatibility)
{
    var fullName = DisplayName(type);
    var typeDocId = TypeId(type);
    var typeDoc = FindDoc(docs, typeDocId);
    var compatibilityEntry = compatibility.GetRequired(fullName);
    var profile = CreateProfile(compatibilityEntry);
    var members = GetMembers(type, docs, profile)
        .OrderBy(member => member.Id, StringComparer.Ordinal)
        .ToArray();

    return new ApiTypeEntry(
        Id: "electron2d://api/type/" + fullName,
        FullName: fullName,
        Namespace: type.Namespace ?? string.Empty,
        Name: ShortDisplayName(type),
        Kind: TypeKind(type),
        BaseType: BaseTypeName(type),
        Interfaces: type.GetInterfaces()
            .Where(item => item.IsPublic)
            .Select(TypeDisplayName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray(),
        XmlDocId: typeDocId,
        Summary: PlainSummary(typeDoc),
        Category: CategoryFor(type),
        Profile: profile,
        Members: members);
}

static string EngineVersion(Assembly assembly)
{
    var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    if (!string.IsNullOrWhiteSpace(informationalVersion))
    {
        var metadataIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        return metadataIndex < 0 ? informationalVersion : informationalVersion[..metadataIndex];
    }

    return assembly.GetName().Version?.ToString() ?? "0.1.0-preview";
}

static ApiProfile CreateProfile(CompatibilityEntry entry)
{
    var status = entry.Status.ToLowerInvariant();
    return new ApiProfile(
        Name: "Electron2D 0.1.0 2D",
        Status: status,
        Parity: status == "supported" ? "parity_verified" : "not_verified",
        OutOfProfile: status != "supported",
        GodotReference: entry.Reference,
        Notes: entry.Notes);
}

static IReadOnlyList<ApiMemberEntry> GetMembers(
    Type type,
    IReadOnlyDictionary<string, XElement> docs,
    ApiProfile profile)
{
    const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    var members = new List<ApiMemberEntry>();

    foreach (var field in type.GetFields(flags).OrderBy(field => field.Name, StringComparer.Ordinal))
    {
        var kind = type.IsEnum ? "EnumValue" : "Field";
        var signature = FieldSignature(field);
        var xmlDocId = "F:" + XmlTypeName(type) + "." + field.Name;
        var doc = FindDoc(docs, xmlDocId);
        members.Add(new ApiMemberEntry(
            Id: MemberId(type, kind, field.Name),
            DeclaringType: DisplayName(type),
            Name: field.Name,
            Kind: kind,
            Signature: signature,
            ReturnType: TypeDisplayName(field.FieldType),
            Parameters: [],
            XmlDocId: xmlDocId,
            Summary: PlainSummary(doc),
            Profile: profile));
    }

    foreach (var property in type.GetProperties(flags).OrderBy(property => property.Name, StringComparer.Ordinal))
    {
        var xmlDocId = "P:" + XmlTypeName(type) + "." + property.Name;
        var doc = FindDoc(docs, xmlDocId);
        members.Add(new ApiMemberEntry(
            Id: MemberId(type, "Property", property.Name),
            DeclaringType: DisplayName(type),
            Name: property.Name,
            Kind: "Property",
            Signature: PropertySignature(property),
            ReturnType: TypeDisplayName(property.PropertyType),
            Parameters: property.GetIndexParameters().Select(CreateParameter).ToArray(),
            XmlDocId: xmlDocId,
            Summary: PlainSummary(doc),
            Profile: profile));
    }

    foreach (var eventInfo in type.GetEvents(flags).OrderBy(item => item.Name, StringComparer.Ordinal))
    {
        var xmlDocId = "E:" + XmlTypeName(type) + "." + eventInfo.Name;
        var doc = FindDoc(docs, xmlDocId);
        members.Add(new ApiMemberEntry(
            Id: MemberId(type, "Event", eventInfo.Name),
            DeclaringType: DisplayName(type),
            Name: eventInfo.Name,
            Kind: "Event",
            Signature: EventSignature(eventInfo),
            ReturnType: TypeDisplayName(eventInfo.EventHandlerType!),
            Parameters: [],
            XmlDocId: xmlDocId,
            Summary: PlainSummary(doc),
            Profile: profile));
    }

    foreach (var constructor in type.GetConstructors(flags).OrderBy(ConstructorDisplayName, StringComparer.Ordinal))
    {
        var xmlDocId = MethodId(type, constructor, "#ctor");
        var doc = FindDoc(docs, xmlDocId);
        members.Add(new ApiMemberEntry(
            Id: MemberId(type, "Constructor", ConstructorDisplayName(constructor)),
            DeclaringType: DisplayName(type),
            Name: type.Name,
            Kind: "Constructor",
            Signature: ConstructorSignature(type, constructor),
            ReturnType: null,
            Parameters: constructor.GetParameters().Select(CreateParameter).ToArray(),
            XmlDocId: xmlDocId,
            Summary: PlainSummary(doc),
            Profile: profile));
    }

    foreach (var method in type.GetMethods(flags)
        .Where(method => !method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal))
        .OrderBy(MethodDisplayName, StringComparer.Ordinal))
    {
        var xmlDocId = MethodId(type, method, method.Name);
        var doc = FindDoc(docs, xmlDocId);
        members.Add(new ApiMemberEntry(
            Id: MemberId(type, "Method", MethodDisplayName(method)),
            DeclaringType: DisplayName(type),
            Name: method.Name,
            Kind: "Method",
            Signature: MethodSignature(method),
            ReturnType: TypeDisplayName(method.ReturnType),
            Parameters: method.GetParameters().Select(CreateParameter).ToArray(),
            XmlDocId: xmlDocId,
            Summary: PlainSummary(doc),
            Profile: profile));
    }

    return members;
}

static ApiParameterEntry CreateParameter(ParameterInfo parameter)
{
    return new ApiParameterEntry(
        Name: parameter.Name ?? string.Empty,
        Type: TypeDisplayName(parameter.ParameterType),
        IsNullable: IsNullableParameter(parameter),
        HasDefaultValue: parameter.HasDefaultValue,
        DefaultValue: NormalizeDefaultValue(parameter));
}

static string MemberId(Type declaringType, string kind, string signatureKey)
{
    return "electron2d://api/member/" +
        DisplayName(declaringType) +
        "/" +
        kind +
        "/" +
        Uri.EscapeDataString(signatureKey);
}

static object? NormalizeDefaultValue(ParameterInfo parameter)
{
    if (!parameter.HasDefaultValue)
    {
        return null;
    }

    var value = parameter.DefaultValue;
    if (value is null || value == DBNull.Value)
    {
        return null;
    }

    if (value is string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
    {
        return value;
    }

    return Convert.ToString(value, CultureInfo.InvariantCulture);
}

static bool IsNullableParameter(ParameterInfo parameter)
{
    var type = parameter.ParameterType;
    return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}

static string? BaseTypeName(Type type)
{
    if (type.BaseType is null || type.BaseType == typeof(object) || type.IsValueType || type.IsEnum)
    {
        return null;
    }

    return TypeDisplayName(type.BaseType);
}

static string CategoryFor(Type type)
{
    foreach (var category in ApiCategories())
    {
        if (category.Matches(type))
        {
            return category.Title;
        }
    }

    throw new InvalidOperationException("Public type is missing an API category: " + DisplayName(type));
}

static IReadOnlyList<ApiCategory> ApiCategories() =>
[
    new ApiCategory("Core", type => Named(type, "Object", "RefCounted", "Callable", "Error", "ConnectFlags", "Rid", "StringName")),
    new ApiCategory("Scene Tree", type => Named(type, "Node", "Node2D", "NodePath", "PackedScene", "SceneTree")),
    new ApiCategory("Resources", type => Named(type, "Resource", "ResourceUid")),
    new ApiCategory("Math and Data", type => Named(type, "Mathf", "Vector2", "Vector2I", "Rect2", "Rect2I", "Transform2D", "Color", "RandomNumberGenerator", "Variant") ||
        ShortDisplayName(type).StartsWith("Collections.", StringComparison.Ordinal)),
    new ApiCategory("Input", type => Named(type, "Input", "InputMap", "InputEvent", "Key", "KeyLocation", "MouseButton", "MouseButtonMask", "JoyAxis", "JoyButton") ||
        ShortDisplayName(type).StartsWith("InputEvent", StringComparison.Ordinal)),
    new ApiCategory("Display and Localization", type => Named(type, "DisplayServer", "Translation", "TranslationServer")),
    new ApiCategory("Rendering", type => Named(type, "AtlasTexture", "Texture2D", "TileData", "TileMapLayer", "TileSet", "TileSetAtlasSource", "TileSetSource", "CanvasItem", "CanvasLayer", "Camera2D", "Viewport", "ViewportTexture", "Sprite2D", "RenderingServer", "Material", "Shader", "ShaderMaterial")),
    new ApiCategory("Animation and Tweening", type => Named(type, "AnimatedSprite2D", "Animation", "AnimationLibrary", "AnimationPlayer", "SpriteFrames", "Tween", "Tweener", "CallbackTweener", "IntervalTweener", "PropertyTweener")),
    new ApiCategory("Audio", type => Named(type, "AudioServer", "AudioStream", "AudioStreamPlayer", "AudioStreamPlayer2D")),
    new ApiCategory("Physics", type => Named(type, "World2D", "Area2D", "CollisionObject2D", "CollisionShape2D", "Shape2D", "CapsuleShape2D", "CircleShape2D", "ConcavePolygonShape2D", "ConvexPolygonShape2D", "RectangleShape2D", "SegmentShape2D", "PhysicsBody2D", "PhysicsDirectSpaceState2D", "PhysicsMaterial", "PhysicsPointQueryParameters2D", "PhysicsRayQueryParameters2D", "PhysicsServer2D", "PhysicsShapeQueryParameters2D", "RayCast2D", "StaticBody2D", "RigidBody2D", "CharacterBody2D", "KinematicCollision2D")),
    new ApiCategory("UI and Text", type => Named(type, "BaseButton", "BoxContainer", "BoxContainerAlignmentMode", "Button", "CenterContainer", "CheckBox", "Container", "Control", "FocusMode", "GridContainer", "GrowDirection", "HBoxContainer", "ItemList", "Label", "LineEdit", "MarginContainer", "NinePatchRect", "Panel", "PopupMenu", "ProgressBar", "Range", "ScrollContainer", "ScrollHintMode", "ScrollMode", "SizeFlags", "Slider", "StyleBox", "StyleBoxFlat", "TabContainer", "TextureButton", "TextureRect", "Theme", "Tree", "TreeItem", "VBoxContainer", "Font", "HorizontalAlignment", "MouseFilter", "VerticalAlignment")),
    new ApiCategory("Scripting Metadata", type => Named(type, "ExportAttribute", "SignalAttribute", "ToolAttribute")),
];

static bool Named(Type type, params string[] names)
{
    var shortName = ShortDisplayName(type);
    return names.Any(name =>
        string.Equals(shortName, name, StringComparison.Ordinal) ||
        shortName.StartsWith(name + ".", StringComparison.Ordinal));
}

static XElement? FindDoc(IReadOnlyDictionary<string, XElement> docs, string id)
{
    if (docs.TryGetValue(id, out var exact))
    {
        return exact;
    }

    var parameterIndex = id.IndexOf('(', StringComparison.Ordinal);
    if (parameterIndex > 0)
    {
        var prefix = id[..parameterIndex];
        return docs.FirstOrDefault(item => item.Key.StartsWith(prefix + "(", StringComparison.Ordinal)).Value;
    }

    return docs.FirstOrDefault(item => item.Key.StartsWith(id + "(", StringComparison.Ordinal)).Value;
}

static string PlainSummary(XElement? doc)
{
    var summary = doc?.Element("summary");
    if (summary is null || string.IsNullOrWhiteSpace(summary.Value))
    {
        return string.Empty;
    }

    return NormalizeWhitespace(Markdown(summary));
}

static string Markdown(XElement element)
{
    var builder = new StringBuilder();
    foreach (var node in element.Nodes())
    {
        AppendMarkdownNode(builder, node);
    }

    return NormalizeParagraphSpacing(builder.ToString());
}

static void AppendMarkdownNode(StringBuilder builder, XNode node)
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

static string NormalizeParagraphSpacing(string value)
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

static string NormalizeWhitespace(string value)
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

static string EventSignature(EventInfo eventInfo) => "public event " + TypeDisplayName(eventInfo.EventHandlerType!) + " " + eventInfo.Name;

static string ConstructorSignature(Type type, ConstructorInfo constructor) => "public " + TypeDisplayName(type) + "(" + Parameters(constructor.GetParameters()) + ")";

static string MethodSignature(MethodInfo method)
{
    var modifiers = method.IsStatic ? "public static " : "public ";
    return modifiers + TypeDisplayName(method.ReturnType) + " " + MethodDisplayName(method) + "(" + Parameters(method.GetParameters()) + ")";
}

static string ConstructorDisplayName(ConstructorInfo constructor) => ".ctor(" + Parameters(constructor.GetParameters(), includeTypesOnly: true) + ")";

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
        definitionName = definitionName[..tickIndex];
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

static string ShortDisplayName(Type type)
{
    var displayName = DisplayName(type);
    const string rootNamespace = "Electron2D.";
    return displayName.StartsWith(rootNamespace, StringComparison.Ordinal)
        ? displayName[rootNamespace.Length..]
        : displayName;
}

static string TypeId(Type type) => "T:" + XmlTypeName(type);

static string XmlTypeName(Type type) => (type.FullName ?? type.Name).Replace('+', '.');

static string MethodId(Type declaringType, MethodBase method, string methodName)
{
    var name = methodName;
    if (method.IsGenericMethod)
    {
        name += "``" + method.GetGenericArguments().Length.ToString(CultureInfo.InvariantCulture);
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
        return "`" + type.GenericParameterPosition.ToString(CultureInfo.InvariantCulture);
    }

    if (type.IsGenericType)
    {
        var definitionName = (type.GetGenericTypeDefinition().FullName ?? type.Name).Replace('+', '.');
        var tickIndex = definitionName.IndexOf('`', StringComparison.Ordinal);
        if (tickIndex >= 0)
        {
            definitionName = definitionName[..tickIndex];
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

static string RelativePath(string root, string path)
{
    var relative = Path.GetRelativePath(root, path);
    return relative.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
}

public sealed record ApiManifest(
    int SchemaVersion,
    string ManifestVersion,
    string EngineVersion,
    string ProfileName,
    string GodotBaseline,
    GeneratedFrom GeneratedFrom,
    StrictParitySummary StrictParitySummary,
    StatusSummary StatusSummary,
    IReadOnlyList<string> SupportedVariantTypes,
    IReadOnlyList<ApiTypeEntry> Types);

public sealed record GeneratedFrom(
    string CompiledAssembly,
    string XmlDocumentation,
    string CompatibilityPage);

public sealed record StrictParitySummary(
    int MissingTypes,
    int MissingMembers,
    int SignatureMismatches,
    int InheritanceMismatches,
    int DefaultMismatches,
    int UnexpectedChanges);

public sealed record StatusSummary(
    int Supported,
    int Partial,
    int Experimental,
    int Planned);

public sealed record ApiTypeEntry(
    string Id,
    string FullName,
    string Namespace,
    string Name,
    string Kind,
    string? BaseType,
    IReadOnlyList<string> Interfaces,
    string XmlDocId,
    string Summary,
    string Category,
    ApiProfile Profile,
    IReadOnlyList<ApiMemberEntry> Members);

public sealed record ApiMemberEntry(
    string Id,
    string DeclaringType,
    string Name,
    string Kind,
    string Signature,
    string? ReturnType,
    IReadOnlyList<ApiParameterEntry> Parameters,
    string XmlDocId,
    string Summary,
    ApiProfile Profile);

public sealed record ApiParameterEntry(
    string Name,
    string Type,
    bool IsNullable,
    bool HasDefaultValue,
    object? DefaultValue);

public sealed record ApiProfile(
    string Name,
    string Status,
    string Parity,
    bool OutOfProfile,
    string GodotReference,
    string Notes);

public sealed record ApiCategory(string Title, Func<Type, bool> Matches);

public sealed record CompatibilityEntry(
    string Api,
    string Reference,
    string Status,
    string Notes);

public sealed class CompatibilityTable
{
    private readonly Dictionary<string, CompatibilityEntry> entries;

    private CompatibilityTable(Dictionary<string, CompatibilityEntry> entries)
    {
        this.entries = entries;
    }

    public static CompatibilityTable Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("API compatibility page was not found.", path);
        }

        var entries = new Dictionary<string, CompatibilityEntry>(StringComparer.Ordinal);
        var rowPattern = new Regex(
            "^\\|\\s*`(?<api>[^`]+)`\\s*\\|\\s*(?<reference>.*?)\\s*\\|\\s*(?<status>Supported|Partial|Experimental|Planned)\\s*\\|\\s*(?<notes>.*?)\\s*\\|\\s*$",
            RegexOptions.Compiled);
        foreach (var line in File.ReadLines(path))
        {
            var match = rowPattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var api = NormalizeApiName(match.Groups["api"].Value.Trim());
            entries[api] = new CompatibilityEntry(
                Api: api,
                Reference: StripMarkdown(match.Groups["reference"].Value),
                Status: match.Groups["status"].Value.Trim(),
                Notes: StripMarkdown(match.Groups["notes"].Value));
        }

        return new CompatibilityTable(entries);
    }

    public CompatibilityEntry GetRequired(string fullName)
    {
        var normalized = NormalizeApiName(fullName);
        if (entries.TryGetValue(normalized, out var entry))
        {
            return entry;
        }

        throw new InvalidOperationException("Public type is missing from API-Compatibility.md: " + fullName);
    }

    private static string NormalizeApiName(string value) => value.Replace('+', '.');

    private static string StripMarkdown(string value)
    {
        return Regex.Replace(value.Replace("`", string.Empty), "\\[([^\\]]+)\\]\\([^)]+\\)", "$1").Trim();
    }
}

public sealed class Arguments
{
    public required string RepositoryRoot { get; init; }

    public required string AssemblyPath { get; init; }

    public required string XmlPath { get; init; }

    public required string CompatibilityPath { get; init; }

    public required string OutputPath { get; init; }

    public static Arguments Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            var name = args[index];
            if (!name.StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
            {
                throw new ArgumentException("Expected --name value arguments.");
            }

            values[name[2..]] = args[++index];
        }

        return new Arguments
        {
            RepositoryRoot = Required(values, "repo-root"),
            AssemblyPath = Required(values, "assembly"),
            XmlPath = Required(values, "xml"),
            CompatibilityPath = Required(values, "compatibility"),
            OutputPath = Required(values, "output")
        };
    }

    private static string Required(IReadOnlyDictionary<string, string> values, string name)
    {
        if (!values.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Missing required argument --" + name + ".");
        }

        return Path.GetFullPath(value);
    }
}
