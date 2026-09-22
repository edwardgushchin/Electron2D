using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Electron2D;

const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

var entries = new List<ApiEntry>();
foreach (var type in typeof(ElectronObject).Assembly.GetTypes()
             .Where(VisibleType)
             .OrderBy(type => type.FullName, StringComparer.Ordinal))
{
    var typeId = TypeName(type);
    entries.Add(new ApiEntry($"T:{typeId}", "type", typeId, type.Name, TypeDeclaration(type), VisibilityType(type),
        type.BaseType is null ? null : TypeName(type.BaseType),
        type.GetInterfaces().Select(TypeName).Order(StringComparer.Ordinal).ToArray()));

    foreach (var member in type.GetMembers(Declared))
    {
        switch (member)
        {
            case ConstructorInfo constructor when VisibleMethod(constructor):
                Add("constructor", constructor, constructor.GetParameters(), null, null);
                break;
            case MethodInfo method when VisibleMethod(method) && (!method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal)):
                Add(method.Name.StartsWith("op_", StringComparison.Ordinal) ? "operator" : "method",
                    method, method.GetParameters(), TypeName(method.ReturnType), null);
                break;
            case PropertyInfo property when VisibleProperty(property):
                Add(property.GetIndexParameters().Length > 0 ? "indexer" : "property", property,
                    property.GetIndexParameters(), TypeName(property.PropertyType), null);
                break;
            case EventInfo eventInfo when VisibleEvent(eventInfo):
                Add("event", eventInfo, [], TypeName(eventInfo.EventHandlerType!), null);
                break;
            case FieldInfo field when VisibleField(field) && !(type.IsEnum && field.Name == "value__"):
                Add(type.IsEnum ? "enumValue" : field.IsLiteral ? "constant" : "field", field, [],
                    TypeName(field.FieldType), field.IsLiteral ? Value(field.GetRawConstantValue()) : null);
                break;
        }
    }

    void Add(string kind, MemberInfo member, ParameterInfo[] parameters, string? returnType, string? value)
    {
        var parameterInfo = parameters.Select(parameter => new ApiParameter(
            parameter.Name ?? "", TypeName(parameter.ParameterType), parameter.IsOptional ? Value(parameter.DefaultValue) : null)).ToArray();
        var signature = Declaration(member, parameters, returnType);
        var methodName = member is MethodInfo method && method.IsGenericMethodDefinition
            ? $"{member.Name}`{method.GetGenericArguments().Length}"
            : member.Name;
        var id = $"{kind}:{typeId}.{methodName}({string.Join(',', parameters.Select(parameter => TypeName(parameter.ParameterType)))})";
        entries.Add(new ApiEntry(id, kind, typeId, member.Name, signature, VisibilityMember(member),
            null, null, parameterInfo, returnType, value));
    }
}

if (entries.Select(entry => entry.Id).Distinct(StringComparer.Ordinal).Count() != entries.Count)
    throw new InvalidOperationException("The public API inventory contains duplicate member IDs.");

var json = JsonSerializer.Serialize(entries.OrderBy(entry => entry.Id, StringComparer.Ordinal),
    new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
if (args.Length > 0)
    File.WriteAllText(args[0], json + Environment.NewLine);
else
    Console.WriteLine(json);

static bool VisibleType(Type type) => type.DeclaringType is null
    ? type.IsPublic
    : VisibleType(type.DeclaringType) && (type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem);

static bool VisibleMethod(MethodBase method) => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

static bool VisibleField(FieldInfo field) => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

static bool VisibleProperty(PropertyInfo property) =>
    (property.GetMethod is not null && VisibleMethod(property.GetMethod)) ||
    (property.SetMethod is not null && VisibleMethod(property.SetMethod));

static bool VisibleEvent(EventInfo eventInfo) =>
    (eventInfo.AddMethod is not null && VisibleMethod(eventInfo.AddMethod)) ||
    (eventInfo.RemoveMethod is not null && VisibleMethod(eventInfo.RemoveMethod));

static string VisibilityType(Type type) => type.IsNestedFamily ? "protected" : type.IsNestedFamORAssem ? "protected internal" : "public";

static string VisibilityMember(MemberInfo member) => member switch
{
    FieldInfo field => VisibilityAccess(field.IsPublic, field.IsFamilyOrAssembly),
    MethodBase method => VisibilityAccess(method.IsPublic, method.IsFamilyOrAssembly),
    PropertyInfo property => VisibilityMember(VisibleMethod(property.GetMethod ?? property.SetMethod!)
        ? (property.GetMethod ?? property.SetMethod!) : property.SetMethod!),
    EventInfo eventInfo => VisibilityMember(eventInfo.AddMethod ?? eventInfo.RemoveMethod!),
    _ => "public"
};

static string VisibilityAccess(bool isPublic, bool isFamilyOrAssembly) => isPublic ? "public" : isFamilyOrAssembly ? "protected internal" : "protected";

static string TypeName(Type type)
{
    if (type.IsByRef) return $"ref {TypeName(type.GetElementType()!)}";
    if (type.IsArray) return $"{TypeName(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
    if (type.IsGenericParameter) return type.Name;
    var name = (type.FullName ?? type.Name).Replace('+', '.');
    if (!type.IsGenericType) return name;
    var tick = name.IndexOf('`');
    return $"{name[..tick]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
}

static string TypeDeclaration(Type type)
{
    var kind = type.IsEnum ? "enum" : type.IsInterface ? "interface" : type.IsValueType ? "struct" : type.BaseType == typeof(MulticastDelegate) ? "delegate" : "class";
    var modifier = type.IsClass && type.IsAbstract && type.IsSealed ? "static " :
        type.IsClass && type.IsAbstract ? "abstract " :
        type.IsClass && type.IsSealed && kind == "class" ? "sealed " : "";
    return $"{VisibilityType(type)} {modifier}{kind} {TypeName(type)}";
}

static string Declaration(MemberInfo member, ParameterInfo[] parameters, string? returnType)
{
    var modifier = member switch
    {
        MethodBase method when method.IsStatic => " static",
        MethodInfo method when method.IsAbstract => " abstract",
        MethodInfo method when method.GetBaseDefinition() != method => " override",
        MethodInfo method when method.IsVirtual => " virtual",
        FieldInfo field when field.IsLiteral => " const",
        FieldInfo field when field.IsInitOnly => field.IsStatic ? " static readonly" : " readonly",
        FieldInfo field when field.IsStatic => " static",
        PropertyInfo property when (property.GetMethod ?? property.SetMethod)!.IsStatic => " static",
        EventInfo eventInfo when (eventInfo.AddMethod ?? eventInfo.RemoveMethod)!.IsStatic => " static",
        _ => ""
    };
    var arguments = string.Join(", ", parameters.Select(parameter =>
        $"{(parameter.IsOut ? "out " : parameter.ParameterType.IsByRef ? "ref " : "")}{TypeName(parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType)} {parameter.Name}" +
        (parameter.IsOptional ? $" = {Value(parameter.DefaultValue)}" : "")));
    var prefix = $"{VisibilityMember(member)}{modifier} ";
    return member switch
    {
        ConstructorInfo => $"{prefix}{member.DeclaringType!.Name}({arguments})",
        MethodInfo method => $"{prefix}{returnType} {member.Name}" +
            (method.IsGenericMethodDefinition ? $"<{string.Join(", ", method.GetGenericArguments().Select(TypeName))}>" : "") +
            $"({arguments})",
        PropertyInfo property when parameters.Length > 0 => $"{prefix}{returnType} this[{arguments}] {{ {Accessors(property)} }}",
        PropertyInfo property => $"{prefix}{returnType} {property.Name} {{ {Accessors(property)} }}",
        EventInfo => $"{prefix}event {returnType} {member.Name}",
        FieldInfo field => $"{prefix}{returnType} {field.Name}" + (field.IsLiteral ? $" = {Value(field.GetRawConstantValue())}" : ""),
        _ => member.Name
    };
}

static string Accessors(PropertyInfo property)
{
    var visibility = VisibilityMember(property);
    return (property.GetMethod is null || !VisibleMethod(property.GetMethod) ? "" :
            (VisibilityMember(property.GetMethod) == visibility ? "" : VisibilityMember(property.GetMethod) + " ") + "get; ") +
        (property.SetMethod is null || !VisibleMethod(property.SetMethod) ? "" :
            (VisibilityMember(property.SetMethod) == visibility ? "" : VisibilityMember(property.SetMethod) + " ") + "set;");
}

static string Value(object? value) => value switch
{
    null => "null",
    string text => JsonSerializer.Serialize(text),
    char character => $"'{character}'",
    bool boolean => boolean ? "true" : "false",
    float number => number.ToString("R", CultureInfo.InvariantCulture) + "f",
    double number => number.ToString("R", CultureInfo.InvariantCulture),
    decimal number => number.ToString(CultureInfo.InvariantCulture) + "m",
    _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null"
};

record ApiEntry(string Id, string Kind, string DeclaringType, string Name, string Signature, string Visibility,
    string? BaseType = null, string[]? Interfaces = null, ApiParameter[]? Parameters = null,
    string? ReturnType = null, string? Value = null);

record ApiParameter(string Name, string Type, string? Default);
