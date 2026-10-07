using System.Reflection;
using System.Text;

namespace Electron2D;

/// <summary>Describes a registered compiled C# Node or Resource and its source, typed schema and construction factory.</summary>
/// <remarks>Project code remains in the consumer assembly and executes ordinary typed callbacks. Source edits do not
/// replace CLR classes. Loading validates compiler source checksums; build/restart and live reassignment remain explicit.</remarks>
public sealed partial class Script : Resource
{
    internal const int MaximumSourceBytes = 8 * 1024 * 1024;
    private static readonly UTF8Encoding SourceEncoding = new(false, true);
    private readonly object _gate = new();
    private ScriptRegistration? _registration;
    private string _sourceCode = "";
    /// <summary>Creates an unbound source asset; registration/loading supplies its compiled class association.</summary>
    public Script() { }
    /// <summary>Gets or replaces source text without changing its compiled class implementation.</summary>
    /// <value>Empty initially; strict UTF-8 source is bounded to eight MiB.</value>
    /// <exception cref="ArgumentNullException">Text is null.</exception><exception cref="ArgumentException">Text is invalid UTF-16 or exceeds the encoded budget.</exception>
    public string SourceCode { get { lock (_gate) { ThrowIfDisposed(); return _sourceCode; } } set { ArgumentNullException.ThrowIfNull(value); if (SourceEncoding.GetByteCount(value) > MaximumSourceBytes) throw new ArgumentException("Script source exceeds its byte budget.", nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_sourceCode == value) return; _sourceCode = value; } EmitChanged(); } }
    /// <summary>Gets the stable registered compiled class identity.</summary><value>Empty when unbound.</value>
    public string CompiledTypeID { get { lock (_gate) { ThrowIfDisposed(); return _registration?.ID ?? ""; } } }
    /// <summary>Gets the selected compiled CLR type without constructing an instance.</summary><value>Null when unbound.</value>
    public Type? CompiledType { get { lock (_gate) { ThrowIfDisposed(); return _registration?.Type; } } }
    /// <summary>Reports whether a concrete exact construction factory is available.</summary><returns>False for unbound or abstract assets.</returns>
    public bool CanInstantiate() { lock (_gate) { ThrowIfDisposed(); return _registration?.Factory != null; } }
    /// <summary>Reports whether source text is nonempty.</summary><returns>Whether SourceCode contains text.</returns>
    public bool HasSourceCode() => SourceCode.Length > 0;
    /// <summary>Reports whether the associated compiled class is abstract.</summary><returns>False when unbound.</returns>
    public bool IsAbstract() => CompiledType?.IsAbstract ?? false;
    /// <summary>Reports the compiled class's editor tool declaration.</summary><returns>Whether the class carries ToolAttribute.</returns>
    public bool IsTool() => CompiledType?.IsDefined(typeof(ToolAttribute), inherit: true) ?? false;
    /// <summary>Returns the declared global class name.</summary><returns>The CLR class name with GlobalClassAttribute, or empty.</returns>
    public string GetGlobalName() { var type = CompiledType; return type?.IsDefined(typeof(GlobalClassAttribute), inherit: false) == true ? type.Name : ""; }
    /// <summary>Returns the first native engine class inherited by the compiled project class.</summary><returns>Native CLR base type, or null when unbound.</returns>
    public Type? GetInstanceBaseType() { var type = CompiledType?.BaseType; while (type != null && type.Assembly != typeof(Script).Assembly) type = type.BaseType; return type; }
    /// <summary>Loads the directly inherited registered script asset, when the project base class has one.</summary><returns>Borrowed cached base Script, or null for a native/unregistered base.</returns>
    public Script? GetBaseScript() { var type = CompiledType?.BaseType; var record = type == null ? null : FindType(type); return record == null ? null : ResourceLoader.Load<Script>(record.Path); }
    /// <summary>Creates a caller-owned instance through the registered exact factory.</summary><typeparam name="T">Expected compiled Node/Resource role.</typeparam><returns>The actual project class instance.</returns>
    /// <exception cref="InvalidOperationException">No factory exists or the expected role is incompatible.</exception><exception cref="Exception">The project constructor/schema hook fails.</exception>
    public T New<T>() where T : ElectronObject => New<T>(null);
    /// <summary>Creates a typed instance using a direct C# constructor expression, including constructor arguments.</summary>
    /// <typeparam name="T">Expected project object role.</typeparam><param name="constructor">Typed constructor, or null for the registered factory.</param><returns>Caller-owned exact compiled instance.</returns>
    /// <remarks>The constructor is an explicit cold operation. It does not introduce string invocation or a frame dispatcher.</remarks>
    public T New<T>(Func<T>? constructor) where T : ElectronObject
    {
        ScriptRegistration record; lock (_gate) { ThrowIfDisposed(); record = _registration ?? throw new InvalidOperationException("The script is unbound."); }
        if (!typeof(T).IsAssignableFrom(record.Type) || record.Type.IsAbstract || constructor == null && record.Factory == null) throw new InvalidOperationException("The script cannot construct the expected role.");
        var instance = constructor != null ? constructor() : record.Factory!();
        try
        {
            if (instance == null || instance.IsDisposed || instance.GetType() != record.Type) throw new InvalidOperationException("Script factory must return a live exact class instance.");
            var actual = instance.GetPropertyList().Where(p => p.OwnerType.Assembly != typeof(Script).Assembly).ToArray(); var schema = Properties(record);
            if (actual.Length != schema.Length || actual.Any(p => !schema.Any(s => s.Name == p.Name && s.OwnerType == p.OwnerType && s.ValueType == p.ValueType && s.IsStored == p.IsStored && s.IsReadOnly == p.IsReadOnly))) throw new InvalidOperationException("Constructed script property schema differs from registration.");
            return (T)instance;
        }
        catch { instance?.Dispose(); throw; }
    }
    private static PropertyDescriptor[] Properties(ScriptRegistration record)
    {
        var properties = new List<PropertyDescriptor>(); var type = record.Type;
        while (type != null && type.Assembly != typeof(Script).Assembly) { if (FindType(type) is { } entry) foreach (var property in entry.Properties) if (!properties.Any(p => p.Name == property.Name)) properties.Add(property); type = type.BaseType; }
        return properties.ToArray();
    }
    /// <summary>Returns copied typed project property metadata, including registered script bases.</summary><returns>Descriptors with no untyped access channel.</returns>
    public PropertyDescriptor[] GetScriptPropertyList() { lock (_gate) { ThrowIfDisposed(); return _registration == null ? [] : Properties(_registration); } }
    /// <summary>Reads a typed declared default in an existing compatible owner context.</summary>
    /// <typeparam name="TOwner">Compiled owner class.</typeparam><typeparam name="TValue">Exact property value type.</typeparam>
    /// <param name="property">Registered typed descriptor.</param><param name="owner">Live owner whose lifetime retains borrowed defaults.</param><returns>The declared revert/default value.</returns>
    /// <exception cref="ArgumentException">Owner or descriptor is unrelated.</exception><exception cref="InvalidOperationException">No declared default exists.</exception>
    public TValue GetPropertyDefaultValue<TOwner, TValue>(PropertyDescriptor<TOwner, TValue> property, TOwner owner) where TOwner : ElectronObject
    {
        ArgumentNullException.ThrowIfNull(property); ArgumentNullException.ThrowIfNull(owner);
        if (CompiledType?.IsInstanceOfType(owner) != true || !GetScriptPropertyList().Contains(property)) throw new ArgumentException("Default query requires a registered descriptor and compatible owner.");
        if (!property.TryGetRevertValue(owner, out var value)) throw new InvalidOperationException("The property has no declared default."); return value;
    }
    private IEnumerable<Type> ProjectTypes() { var type = CompiledType; while (type != null && type.Assembly != typeof(Script).Assembly) { yield return type; type = type.BaseType; } }
    /// <summary>Returns declared project methods and inherited project methods without invoking them.</summary><returns>Copied CLR method metadata, ordered by name/signature.</returns>
    public MethodInfo[] GetScriptMethodList() => ProjectTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)).Where(m => !m.IsSpecialName && !m.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))).OrderBy(m => m.Name, StringComparer.Ordinal).ThenBy(m => m.ToString(), StringComparer.Ordinal).ToArray();
    /// <summary>Reports a project method name across the compiled script inheritance chain.</summary><param name="methodName">Nonnull metadata name.</param><returns>Whether such a method is declared.</returns>
    public bool HasScriptMethod(string methodName) { ArgumentNullException.ThrowIfNull(methodName); return GetScriptMethodList().Any(m => m.Name == methodName); }
    /// <summary>Returns copied CLR event metadata for project-defined signals.</summary><returns>Project event declarations including project bases.</returns>
    public EventInfo[] GetScriptSignalList() => ProjectTypes().SelectMany(t => t.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)).OrderBy(e => e.Name, StringComparer.Ordinal).ToArray();
    /// <summary>Reports a project event name across script inheritance.</summary><param name="signalName">Nonnull event name.</param><returns>Whether the signal is declared.</returns>
    public bool HasScriptSignal(string signalName) { ArgumentNullException.ThrowIfNull(signalName); return GetScriptSignalList().Any(e => e.Name == signalName); }
    /// <summary>Copies constants of one exact caller-selected value type.</summary><typeparam name="TValue">Requested constant type.</typeparam><returns>Names and typed values, including project bases.</returns>
    public Dictionary<string, TValue> GetScriptConstantMap<TValue>()
    {
        if (typeof(TValue) == typeof(object)) throw new ArgumentException("Constant queries require a concrete value type.");
        var result = new Dictionary<string, TValue>(StringComparer.Ordinal);
        foreach (var type in ProjectTypes()) foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)) if (field.IsLiteral && field.FieldType == typeof(TValue) && !result.ContainsKey(field.Name)) result.Add(field.Name, (TValue)(field.FieldType.IsEnum ? Enum.ToObject(field.FieldType, field.GetRawConstantValue()!) : field.GetRawConstantValue())!);
        return result;
    }
    internal static string DecodeSource(byte[] bytes) => SourceEncoding.GetString(bytes.AsSpan(bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0));
    private void BindID(string id) { lock (_gate) { ThrowIfDisposed(); _registration = id.Length == 0 ? null : FindID(id); } }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var descriptor in base.GetPropertyDescriptors()) yield return descriptor;
        yield return new PropertyDescriptor<Script, string>(nameof(CompiledTypeID), s => s.CompiledTypeID, (s, v) => s.BindID(v), _ => "", stored: true);
        yield return new PropertyDescriptor<Script, string>(nameof(SourceCode), s => s.SourceCode, (s, v) => s.SourceCode = v, _ => "", stored: true);
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new Script();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { ScriptRegistration? registration; string source; lock (_gate) { ThrowIfDisposed(); registration = _registration; source = _sourceCode; } var copy = (Script)target; lock (copy._gate) { copy.ThrowIfDisposed(); copy._registration = registration; copy._sourceCode = source; } }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_gate) { _registration = null; _sourceCode = ""; } base.Dispose(disposing); }
}

/// <summary>Marks a compiled project class as an editor tool class for script metadata and authoring hosts.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class ToolAttribute : Attribute
{ /// <summary>Creates a tool-class declaration.</summary>
    public ToolAttribute() { }
}
/// <summary>Declares a compiled project class's global script name using its CLR class name.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GlobalClassAttribute : Attribute
{ /// <summary>Creates a global class declaration.</summary>
    public GlobalClassAttribute() { }
}
