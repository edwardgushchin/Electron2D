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
var publicApiProfile = ManualApiProfile.Load(arguments.ProfilePath);

var publicTypes = assembly.GetExportedTypes()
    .Where(type => type.Assembly == assembly)
    .OrderBy(type => DisplayName(type), StringComparer.Ordinal)
    .ToArray();

var typeEntries = publicTypes.Select(type => CreateTypeEntry(type, docs, publicApiProfile)).ToArray();
var summary = new StatusSummary(
    Supported: typeEntries.Count(type => type.Profile.Status == "supported"),
    Partial: typeEntries.Count(type => type.Profile.Status == "partial"),
    Experimental: typeEntries.Count(type => type.Profile.Status == "experimental"),
    Planned: typeEntries.Count(type => type.Profile.Status == "planned"),
    Deferred: typeEntries.Count(type => type.Profile.Status == "deferred"),
    Unsupported: typeEntries.Count(type => type.Profile.Status == "unsupported"),
    Unapproved: typeEntries.Count(type => type.Profile.Status == "unapproved"));

var manifest = new ApiManifest(
    SchemaVersion: 1,
    ManifestVersion: "0.1-preview",
    EngineVersion: EngineVersion(assembly),
    ProfileName: "Electron2D 0.1-preview",
    GodotBaseline: "4.7-stable",
    GeneratedFrom: new GeneratedFrom(
        CompiledAssembly: RelativePath(arguments.RepositoryRoot, arguments.AssemblyPath),
        XmlDocumentation: RelativePath(arguments.RepositoryRoot, arguments.XmlPath),
        PublicApiProfile: RelativePath(arguments.RepositoryRoot, arguments.ProfilePath)),
    StrictParityEvidence: new StrictParityEvidence(
        Status: "not_verified",
        Reason: "The manual public API profile records owner-approved scope only; strict Godot 4.7 parity is verified by owning class tasks and final gates."),
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
    ManualApiProfile publicApiProfile)
{
    var fullName = DisplayName(type);
    var rawFullName = RawDisplayName(type);
    var typeDocId = TypeId(type);
    var typeDoc = FindDoc(docs, typeDocId);
    var profileDecision = publicApiProfile.Find(fullName, rawFullName);
    var profile = CreateProfile(profileDecision, fullName);
    var members = GetMembers(type, docs, profile, profileDecision)
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
        GodotApiScope: profileDecision?.GodotApiScope,
        GodotApiContract: profileDecision?.GodotApiContract,
        ElectronApiContract: profileDecision?.ElectronApiContract,
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

    return assembly.GetName().Version?.ToString() ?? "0.1-preview";
}

static ApiProfile CreateProfile(ProfileTypeDecision? entry, string fullName)
{
    if (entry is null)
    {
        return new ApiProfile(
            Name: "Electron2D 0.1-preview",
            Status: "unapproved",
            Parity: "not_verified",
            OutOfProfile: true,
            GodotReference: ShortTypeName(fullName),
            EditorOnly: false,
            Notes: "No manual public API profile decision exists for this exported runtime type.");
    }

    var status = entry.Decision switch
    {
        "approved" => "supported",
        "deferred" => "deferred",
        "unsupported" => "unsupported",
        _ => "unapproved"
    };
    return new ApiProfile(
        Name: "Electron2D 0.1-preview",
        Status: status,
        Parity: status == "supported" ? "profile_approved" : "not_verified",
        OutOfProfile: status != "supported",
        GodotReference: entry.GodotReference,
        EditorOnly: entry.EditorOnly,
        Notes: entry.Rationale);
}

static string ShortTypeName(string fullName)
{
    const string prefix = "Electron2D.";
    return fullName.StartsWith(prefix, StringComparison.Ordinal) ? fullName[prefix.Length..] : fullName;
}

static IReadOnlyList<ApiMemberEntry> GetMembers(
    Type type,
    IReadOnlyDictionary<string, XElement> docs,
    ApiProfile profile,
    ProfileTypeDecision? profileDecision)
{
    const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    var members = new List<ApiMemberEntry>();

    foreach (var field in type.GetFields(flags)
        .Where(field => !type.IsEnum || field.Name != "value__")
        .OrderBy(field => field.Name, StringComparer.Ordinal))
    {
        var kind = type.IsEnum ? "EnumValue" : field.IsLiteral ? "Constant" : "Field";
        var signature = FieldSignature(field);
        var xmlDocId = "F:" + XmlTypeName(type) + "." + field.Name;
        var doc = FindDoc(docs, xmlDocId);
        var fieldName = ProjectMemberName(field.Name);
        var memberProfile = ResolveMemberProfile(profile, profileDecision, kind, fieldName);
        members.Add(new ApiMemberEntry(
            Id: MemberId(type, kind, fieldName),
            DeclaringType: DisplayName(type),
            Name: fieldName,
            Kind: kind,
            Signature: signature,
            ReturnType: TypeDisplayName(field.FieldType),
            Value: FieldValue(field, type),
            Parameters: [],
            XmlDocId: xmlDocId,
            Summary: PlainSummary(doc),
            Profile: memberProfile.Profile,
            ElectronApiDecision: memberProfile.Decision));
    }

    foreach (var property in type.GetProperties(flags).OrderBy(property => property.Name, StringComparer.Ordinal))
    {
        var xmlDocId = "P:" + XmlTypeName(type) + "." + property.Name;
        var doc = FindDoc(docs, xmlDocId);
        var propertyName = ProjectMemberName(property.Name);
        var memberProfile = ResolveMemberProfile(profile, profileDecision, "Property", propertyName);
        members.Add(new ApiMemberEntry(
            Id: MemberId(type, "Property", propertyName),
            DeclaringType: DisplayName(type),
            Name: propertyName,
            Kind: "Property",
            Signature: PropertySignature(property),
            ReturnType: TypeDisplayName(property.PropertyType),
            Value: PropertyValue(property, type),
            Parameters: property.GetIndexParameters().Select(CreateParameter).ToArray(),
            XmlDocId: xmlDocId,
            Summary: PlainSummary(doc),
            Profile: memberProfile.Profile,
            ElectronApiDecision: memberProfile.Decision));
    }

    foreach (var eventInfo in type.GetEvents(flags).OrderBy(item => item.Name, StringComparer.Ordinal))
    {
        var xmlDocId = "E:" + XmlTypeName(type) + "." + eventInfo.Name;
        var doc = FindDoc(docs, xmlDocId);
        var eventName = ProjectMemberName(eventInfo.Name);
        var memberProfile = ResolveMemberProfile(profile, profileDecision, "Event", eventName);
        members.Add(new ApiMemberEntry(
            Id: MemberId(type, "Event", eventName),
            DeclaringType: DisplayName(type),
            Name: eventName,
            Kind: "Event",
            Signature: EventSignature(eventInfo),
            ReturnType: TypeDisplayName(eventInfo.EventHandlerType!),
            Value: null,
            Parameters: [],
            XmlDocId: xmlDocId,
            Summary: PlainSummary(doc),
            Profile: memberProfile.Profile,
            ElectronApiDecision: memberProfile.Decision));
    }

    foreach (var constructor in type.GetConstructors(flags).OrderBy(ConstructorDisplayName, StringComparer.Ordinal))
    {
        var xmlDocId = MethodId(type, constructor, "#ctor");
        var doc = FindDoc(docs, xmlDocId);
        var constructorName = ShortDisplayName(type).Split('.').Last();
        var memberProfile = ResolveMemberProfile(profile, profileDecision, "Constructor", constructorName);
        members.Add(new ApiMemberEntry(
            Id: MemberId(type, "Constructor", ConstructorDisplayName(constructor)),
            DeclaringType: DisplayName(type),
            Name: constructorName,
            Kind: "Constructor",
            Signature: ConstructorSignature(type, constructor),
            ReturnType: null,
            Value: null,
            Parameters: constructor.GetParameters().Select(CreateParameter).ToArray(),
            XmlDocId: xmlDocId,
            Summary: PlainSummary(doc),
            Profile: memberProfile.Profile,
            ElectronApiDecision: memberProfile.Decision));
    }

    foreach (var method in type.GetMethods(flags)
        .Where(method => !method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal))
        .OrderBy(MethodDisplayName, StringComparer.Ordinal))
    {
        var kind = method.Name.StartsWith("op_", StringComparison.Ordinal) ? "Operator" : "Method";
        var xmlDocId = MethodId(type, method, method.Name);
        var doc = FindDoc(docs, xmlDocId);
        var methodName = ProjectMemberName(method.Name);
        var memberProfile = ResolveMemberProfile(profile, profileDecision, kind, methodName);
        members.Add(new ApiMemberEntry(
            Id: MemberId(type, kind, MethodDisplayName(method)),
            DeclaringType: DisplayName(type),
            Name: methodName,
            Kind: kind,
            Signature: MethodSignature(method),
            ReturnType: TypeDisplayName(method.ReturnType),
            Value: null,
            Parameters: method.GetParameters().Select(CreateParameter).ToArray(),
            XmlDocId: xmlDocId,
            Summary: PlainSummary(doc),
            Profile: memberProfile.Profile,
            ElectronApiDecision: memberProfile.Decision));
    }

    return members;
}

static ResolvedMemberProfile ResolveMemberProfile(
    ApiProfile typeProfile,
    ProfileTypeDecision? typeDecision,
    string kind,
    string name)
{
    if (typeDecision is null || !string.Equals(typeDecision.GodotApiScope, "subset", StringComparison.Ordinal))
    {
        return new ResolvedMemberProfile(typeProfile, null);
    }

    var contract = typeDecision.ElectronApiContract;
    var decision = contract?.MemberDecisions.SingleOrDefault(candidate =>
        string.Equals(candidate.Kind, kind, StringComparison.Ordinal) &&
        string.Equals(candidate.Name, name, StringComparison.Ordinal));
    decision ??= new ElectronApiMemberDecision(
        Kind: kind,
        Name: name,
        Decision: contract?.DefaultMemberDecision ?? typeDecision.GodotApiContract?.DefaultMemberDecision ?? "unsupported",
        Compatibility: "unclassified",
        GodotName: null,
        Rationale: contract?.Rationale ?? "Exported subset member has no explicit electronApiContract decision.");

    var status = decision.Decision switch
    {
        "approved" => "supported",
        "deferred" => "deferred",
        "unsupported" => "unsupported",
        _ => "unapproved"
    };
    var parity = status == "supported"
        ? string.Equals(decision.Compatibility, "electronExtension", StringComparison.Ordinal)
            ? "not_applicable"
            : "profile_approved"
        : "not_verified";
    var profile = new ApiProfile(
        Name: typeProfile.Name,
        Status: status,
        Parity: parity,
        OutOfProfile: status != "supported",
        GodotReference: typeProfile.GodotReference,
        EditorOnly: typeProfile.EditorOnly,
        Notes: decision.Rationale);
    return new ResolvedMemberProfile(profile, decision);
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

static string? FieldValue(FieldInfo field, Type declaringType)
{
    if (field.IsLiteral)
    {
        return FormatReflectionValue(field.GetRawConstantValue());
    }

    if (field.IsStatic &&
        field.IsInitOnly &&
        IsPredefinedValueSingleton(declaringType, field.FieldType))
    {
        return FormatReflectionValue(field.GetValue(null));
    }

    return null;
}

static string? PropertyValue(PropertyInfo property, Type declaringType)
{
    if (property.GetMethod is null ||
        !property.GetMethod.IsStatic ||
        property.SetMethod is not null ||
        !IsPredefinedValueSingleton(declaringType, property.PropertyType))
    {
        return null;
    }

    try
    {
        return FormatReflectionValue(property.GetValue(null));
    }
    catch (Exception exception) when (exception is TargetInvocationException or TargetParameterCountException or MemberAccessException)
    {
        return null;
    }
}

static bool IsPredefinedValueSingleton(Type declaringType, Type valueType)
{
    return declaringType.IsValueType && valueType == declaringType;
}

static string? FormatReflectionValue(object? value)
{
    return value switch
    {
        null => null,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)
    };
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
    new ApiCategory("Core", type => Named(type, "ElectronObject", "Object", "RefCounted", "Callable", "Error", "ConnectFlags", "Rid", "StringName")),
    new ApiCategory("Scene Tree", type => Named(type, "Node", "Node2D", "NodePath", "PackedScene", "ProcessMode", "SceneTree")),
    new ApiCategory("Resources", type => Named(type, "Resource", "ResourceUid")),
    new ApiCategory("Math and Data", type => Named(type, "Mathf", "Vector2", "Vector2I", "Rect2", "Rect2I", "Transform2D", "Color", "RandomNumberGenerator", "Variant") ||
        ShortDisplayName(type).StartsWith("Collections.", StringComparison.Ordinal)),
    new ApiCategory("Input", type => Named(type, "Input", "InputMap", "InputEvent", "Key", "KeyLocation", "MouseButton", "MouseButtonMask", "JoyAxis", "JoyButton") ||
        ShortDisplayName(type).StartsWith("InputEvent", StringComparison.Ordinal)),
    new ApiCategory("Display and Localization", type => Named(type, "DisplayServer", "Translation", "TranslationServer")),
    new ApiCategory("Rendering", type => Named(type, "AtlasTexture", "ImageTexture", "Texture2D", "TileData", "TileMapLayer", "TileSet", "TileSetAtlasSource", "TileSetSource", "CanvasItem", "CanvasLayer", "Camera2D", "Viewport", "ViewportTexture", "Sprite2D", "RenderingServer", "Material", "Shader", "ShaderMaterial")),
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
    return modifiers + TypeDisplayName(field.FieldType) + " " + ProjectMemberName(field.Name);
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
        ? ProjectMemberName(property.Name)
        : "this[" + Parameters(indexParameters) + "]";
    var modifiers = property.GetMethod?.IsStatic == true || property.SetMethod?.IsStatic == true ? "public static " : "public ";
    return modifiers + TypeDisplayName(property.PropertyType) + " " + name + " { " + string.Join(" ", accessors) + " }";
}

static string EventSignature(EventInfo eventInfo) => "public event " + TypeDisplayName(eventInfo.EventHandlerType!) + " " + ProjectMemberName(eventInfo.Name);

static string ConstructorSignature(Type type, ConstructorInfo constructor) => "public " + TypeDisplayName(type) + "(" + Parameters(constructor.GetParameters()) + ")";

static string MethodSignature(MethodInfo method)
{
    var modifiers = method.IsStatic ? "public static " : "public ";
    return modifiers + TypeDisplayName(method.ReturnType) + " " + ProjectMemberName(method.Name) + "(" + Parameters(method.GetParameters()) + ")";
}

static string ConstructorDisplayName(ConstructorInfo constructor) => ".ctor(" + Parameters(constructor.GetParameters(), includeTypesOnly: true) + ")";

static string MethodDisplayName(MethodInfo method) => ProjectMemberName(method.Name) + "(" + Parameters(method.GetParameters(), includeTypesOnly: true) + ")";

static string Parameters(ParameterInfo[] parameters, bool includeTypesOnly = false)
{
    return string.Join(", ", parameters.Select(parameter =>
        includeTypesOnly
            ? TypeDisplayName(parameter.ParameterType)
            : TypeDisplayName(parameter.ParameterType) + " " + EscapeCSharpIdentifier(parameter.Name ?? "arg")));
}

static string EscapeCSharpIdentifier(string name)
{
    return IsCSharpReservedKeyword(name) ? "@" + name : name;
}

static bool IsCSharpReservedKeyword(string name)
{
    return name is
        "abstract" or
        "as" or
        "base" or
        "bool" or
        "break" or
        "byte" or
        "case" or
        "catch" or
        "char" or
        "checked" or
        "class" or
        "const" or
        "continue" or
        "decimal" or
        "default" or
        "delegate" or
        "do" or
        "double" or
        "else" or
        "enum" or
        "event" or
        "explicit" or
        "extern" or
        "false" or
        "finally" or
        "fixed" or
        "float" or
        "for" or
        "foreach" or
        "goto" or
        "if" or
        "implicit" or
        "in" or
        "int" or
        "interface" or
        "internal" or
        "is" or
        "lock" or
        "long" or
        "namespace" or
        "new" or
        "null" or
        "object" or
        "operator" or
        "out" or
        "override" or
        "params" or
        "private" or
        "protected" or
        "public" or
        "readonly" or
        "ref" or
        "return" or
        "sbyte" or
        "sealed" or
        "short" or
        "sizeof" or
        "stackalloc" or
        "static" or
        "string" or
        "struct" or
        "switch" or
        "this" or
        "throw" or
        "true" or
        "try" or
        "typeof" or
        "uint" or
        "ulong" or
        "unchecked" or
        "unsafe" or
        "ushort" or
        "using" or
        "virtual" or
        "void" or
        "volatile" or
        "while";
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
        return DisplayName(type);
    }

    var definitionName = DisplayName(type.GetGenericTypeDefinition());
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

static string DisplayName(Type type) => ProjectTypeDisplayName(RawDisplayName(type), type.IsEnum);

static string RawDisplayName(Type type) => (type.FullName ?? type.Name).Replace('+', '.');

static string ShortDisplayName(Type type)
{
    var displayName = DisplayName(type);
    const string rootNamespace = "Electron2D.";
    return displayName.StartsWith(rootNamespace, StringComparison.Ordinal)
        ? displayName[rootNamespace.Length..]
        : displayName;
}

static string ProjectTypeDisplayName(string displayName, bool isEnum)
{
    const string rootNamespace = "Electron2D.";
    if (!isEnum || !displayName.StartsWith(rootNamespace, StringComparison.Ordinal))
    {
        return displayName;
    }

    const string suffix = "Enum";
    var lastDot = displayName.LastIndexOf('.');
    var nameStart = lastDot < 0 ? 0 : lastDot + 1;
    return displayName.EndsWith(suffix, StringComparison.Ordinal) &&
        displayName.Length > nameStart + suffix.Length
        ? displayName[..^suffix.Length]
        : displayName;
}

static string ProjectMemberName(string name)
{
    return name switch
    {
        "_GetMinimumSize" => "ComputeMinimumSize",
        "_GetTooltip" => "GetTooltipText",
        "_MakeCustomTooltip" => "MakeCustomTooltip",
        _ when name.Length > 1 && name[0] == '_' && char.IsUpper(name[1]) => name[1..],
        _ => name
    };
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
    StrictParityEvidence StrictParityEvidence,
    StatusSummary StatusSummary,
    IReadOnlyList<string> SupportedVariantTypes,
    IReadOnlyList<ApiTypeEntry> Types);

public sealed record GeneratedFrom(
    string CompiledAssembly,
    string XmlDocumentation,
    string PublicApiProfile);

public sealed record StrictParityEvidence(
    string Status,
    string Reason);

public sealed record StatusSummary(
    int Supported,
    int Partial,
    int Experimental,
    int Planned,
    int Deferred,
    int Unsupported,
    int Unapproved);

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
    string? GodotApiScope,
    GodotApiContract? GodotApiContract,
    ElectronApiContract? ElectronApiContract,
    IReadOnlyList<ApiMemberEntry> Members);

public sealed record ApiMemberEntry(
    string Id,
    string DeclaringType,
    string Name,
    string Kind,
    string Signature,
    string? ReturnType,
    string? Value,
    IReadOnlyList<ApiParameterEntry> Parameters,
    string XmlDocId,
    string Summary,
    ApiProfile Profile,
    ElectronApiMemberDecision? ElectronApiDecision);

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
    bool EditorOnly,
    string Notes);

public sealed record ApiCategory(string Title, Func<Type, bool> Matches);

public sealed record ProfileTypeDecision(
    string FullName,
    string GodotReference,
    string Decision,
    string Rationale,
    bool EditorOnly,
    string? GodotApiScope,
    GodotApiContract? GodotApiContract,
    ElectronApiContract? ElectronApiContract);

public sealed record GodotApiContract(
    string Scope,
    string DefaultMemberDecision,
    string Rationale,
    IReadOnlyList<GodotApiMemberDecision> MemberDecisions,
    IReadOnlyList<GodotApiEnumValueDecision> EnumValueDecisions);

public sealed record GodotApiMemberDecision(
    string Selector,
    string Decision,
    string Rationale);

public sealed record GodotApiEnumValueDecision(
    string Enum,
    string Name,
    string Value,
    string Decision,
    string Rationale);

public sealed record ElectronApiContract(
    string Scope,
    string DefaultMemberDecision,
    string Rationale,
    IReadOnlyList<ElectronApiMemberDecision> MemberDecisions);

public sealed record ElectronApiMemberDecision(
    string Kind,
    string Name,
    string Decision,
    string Compatibility,
    string? GodotName,
    string Rationale);

public sealed record ResolvedMemberProfile(
    ApiProfile Profile,
    ElectronApiMemberDecision? Decision);

public sealed class ManualApiProfile
{
    private readonly Dictionary<string, ProfileTypeDecision> entries;

    private ManualApiProfile(Dictionary<string, ProfileTypeDecision> entries)
    {
        this.entries = entries;
    }

    public static ManualApiProfile Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Manual public API profile was not found.", path);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Manual public API profile root must be a JSON object.");
        }

        if (!root.TryGetProperty("types", out var types) || types.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Manual public API profile must contain a types array.");
        }

        var entries = new Dictionary<string, ProfileTypeDecision>(StringComparer.Ordinal);
        foreach (var type in types.EnumerateArray())
        {
            if (type.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("Manual public API profile type entry must be a JSON object.");
            }

            var fullName = RequiredString(type, "fullName");
            var decision = RequiredString(type, "decision");
            var godotApiScope = OptionalString(type, "godotApiScope");
            var godotApiContract = OptionalGodotApiContract(type);
            var electronApiContract = OptionalElectronApiContract(type);
            if (string.Equals(decision, "approved", StringComparison.Ordinal))
            {
                if (godotApiScope is not ("full" or "subset"))
                {
                    throw new InvalidOperationException("Approved manual public API profile type entry must declare godotApiScope=full|subset: " + fullName + ".");
                }

                if (string.Equals(godotApiScope, "subset", StringComparison.Ordinal) != (godotApiContract is not null))
                {
                    throw new InvalidOperationException("Manual public API subset scope and godotApiContract must be declared together: " + fullName + ".");
                }

                if (!string.Equals(godotApiScope, "subset", StringComparison.Ordinal) && electronApiContract is not null)
                {
                    throw new InvalidOperationException("electronApiContract is allowed only for a manual public API subset type: " + fullName + ".");
                }
            }
            else if (godotApiScope is not null || godotApiContract is not null || electronApiContract is not null)
            {
                throw new InvalidOperationException("Only approved manual public API profile types may declare Godot or Electron API contracts: " + fullName + ".");
            }

            entries[NormalizeApiName(fullName)] = new ProfileTypeDecision(
                FullName: fullName,
                GodotReference: RequiredString(type, "godotReference"),
                Decision: decision,
                Rationale: RequiredString(type, "rationale"),
                EditorOnly: OptionalBool(type, "editorOnly"),
                GodotApiScope: godotApiScope,
                GodotApiContract: godotApiContract,
                ElectronApiContract: electronApiContract);
        }

        return new ManualApiProfile(entries);
    }

    public ProfileTypeDecision? Find(string fullName, string? rawFullName = null)
    {
        if (entries.TryGetValue(NormalizeApiName(fullName), out var entry))
        {
            return entry;
        }

        return !string.IsNullOrWhiteSpace(rawFullName) && entries.TryGetValue(NormalizeApiName(rawFullName), out entry)
            ? entry
            : null;
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
        {
            return property.GetString()!;
        }

        throw new InvalidOperationException("Manual public API profile type entry is missing required string property: " + propertyName + ".");
    }

    private static bool OptionalBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new InvalidOperationException("Manual public API profile type entry property must be boolean when present: " + propertyName + ".")
        };
    }

    private static string? OptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException("Manual public API profile type entry property must be a non-empty string when present: " + propertyName + ".");
        }

        return property.GetString();
    }

    private static GodotApiContract? OptionalGodotApiContract(JsonElement type)
    {
        if (!type.TryGetProperty("godotApiContract", out var contract))
        {
            return null;
        }

        if (contract.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Manual public API profile type entry property godotApiContract must be an object when present.");
        }

        return new GodotApiContract(
            Scope: RequiredString(contract, "scope"),
            DefaultMemberDecision: RequiredString(contract, "defaultMemberDecision"),
            Rationale: RequiredString(contract, "rationale"),
            MemberDecisions: ReadMemberDecisions(contract),
            EnumValueDecisions: ReadEnumValueDecisions(contract));
    }

    private static ElectronApiContract? OptionalElectronApiContract(JsonElement type)
    {
        if (!type.TryGetProperty("electronApiContract", out var contract))
        {
            return null;
        }

        if (contract.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Manual public API profile type entry property electronApiContract must be an object when present.");
        }

        var scope = RequiredString(contract, "scope");
        var defaultDecision = RequiredString(contract, "defaultMemberDecision");
        if (!string.Equals(scope, "exportedMembers", StringComparison.Ordinal) ||
            defaultDecision is not ("deferred" or "unsupported"))
        {
            throw new InvalidOperationException("electronApiContract must use scope=exportedMembers and a deferred/unsupported default.");
        }

        if (!contract.TryGetProperty("memberDecisions", out var decisions) || decisions.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("electronApiContract must contain a memberDecisions array.");
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        var memberDecisions = decisions.EnumerateArray().Select(decision =>
        {
            var kind = RequiredString(decision, "kind");
            var name = RequiredString(decision, "name");
            var memberDecision = RequiredString(decision, "decision");
            var compatibility = RequiredString(decision, "compatibility");
            var godotName = OptionalString(decision, "godotName");
            if (memberDecision is not ("approved" or "deferred" or "unsupported") ||
                compatibility is not ("godotMember" or "godotEnumValue" or "electronExtension") ||
                !keys.Add(kind + "\n" + name) ||
                (string.Equals(compatibility, "electronExtension", StringComparison.Ordinal) == (godotName is not null)))
            {
                throw new InvalidOperationException("electronApiContract contains an invalid or duplicate member decision: " + kind + " " + name + ".");
            }

            return new ElectronApiMemberDecision(
                Kind: kind,
                Name: name,
                Decision: memberDecision,
                Compatibility: compatibility,
                GodotName: godotName,
                Rationale: RequiredString(decision, "rationale"));
        }).ToArray();

        return new ElectronApiContract(
            Scope: scope,
            DefaultMemberDecision: defaultDecision,
            Rationale: RequiredString(contract, "rationale"),
            MemberDecisions: memberDecisions);
    }

    private static IReadOnlyList<GodotApiMemberDecision> ReadMemberDecisions(JsonElement contract)
    {
        if (!contract.TryGetProperty("memberDecisions", out var decisions) || decisions.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Manual public API subset contract must contain a memberDecisions array.");
        }

        return decisions.EnumerateArray()
            .Select(decision => new GodotApiMemberDecision(
                Selector: RequiredString(decision, "selector"),
                Decision: RequiredString(decision, "decision"),
                Rationale: RequiredString(decision, "rationale")))
            .ToArray();
    }

    private static IReadOnlyList<GodotApiEnumValueDecision> ReadEnumValueDecisions(JsonElement contract)
    {
        if (!contract.TryGetProperty("enumValueDecisions", out var decisions) || decisions.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Manual public API subset contract must contain an enumValueDecisions array.");
        }

        return decisions.EnumerateArray()
            .Select(decision => new GodotApiEnumValueDecision(
                Enum: RequiredString(decision, "enum"),
                Name: RequiredString(decision, "name"),
                Value: RequiredString(decision, "value"),
                Decision: RequiredString(decision, "decision"),
                Rationale: RequiredString(decision, "rationale")))
            .ToArray();
    }

    private static string NormalizeApiName(string value) => value.Replace('+', '.');
}

public sealed class Arguments
{
    public required string RepositoryRoot { get; init; }

    public required string AssemblyPath { get; init; }

    public required string XmlPath { get; init; }

    public required string ProfilePath { get; init; }

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
            ProfilePath = Required(values, "profile"),
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
