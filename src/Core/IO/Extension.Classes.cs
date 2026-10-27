using System.Runtime.CompilerServices;

namespace Electron2D;

public partial class Extension
{
    private readonly List<ExtensionClass> _classes = [];
    private bool _retiring;
    private int _creating;

    /// <summary>Registers a typed extension class during one initialization callback.</summary>
    /// <typeparam name="T">Stable CLR class shell; registered project parents must precede their descendants.</typeparam>
    /// <param name="classID">Unique portable class identity shared with resource/scene factories.</param>
    /// <param name="factory">Static fresh exact constructor, or null for an abstract parent.</param>
    /// <param name="bind">Installs this module generation's typed functions and defaults in the owner.</param>
    /// <param name="clear">Retires those functions, including after a partially failed bind.</param>
    /// <remarks>Parent bindings precede child bindings; clearing reverses that order. Callbacks own their side effects.
    /// Plain CLR construction alone is not an admitted extension instance. Factory creation and module replacement
    /// and deterministic instance disposal require the manager thread. Binding follows the complete constructor.
    /// Clear runs after an accepted pre-delete attempt or on module retirement. Existing property descriptors provide stored
    /// state; no dynamic invocation is introduced. Compiled script descendants keep their separate source association.</remarks>
    /// <exception cref="ArgumentException">Identity, factory or abstract/concrete role is invalid.</exception>
    /// <exception cref="ArgumentNullException">A required callback is null.</exception>
    /// <exception cref="InvalidOperationException">Registration is outside initialization, foreign, or conflicts with an existing class.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public void RegisterClass<T>(string classID, Func<T>? factory, Action<T> bind, Action<T> clear) where T : ElectronObject
    {
        ThrowIfDisposed(); ExtensionManager.Service.EnsureOwner();
        ArgumentException.ThrowIfNullOrWhiteSpace(classID);
        ArgumentNullException.ThrowIfNull(bind); ArgumentNullException.ThrowIfNull(clear);
        if (typeof(T).Assembly == typeof(Extension).Assembly) throw new ArgumentException("Extension classes must belong to a project assembly.", nameof(T));
        if (classID.Length > 256 || classID.Any(char.IsControl)) throw new ArgumentException("Class identity is not a portable token.", nameof(classID));
        if (factory?.Target is not null || typeof(T).IsAbstract != (factory is null)) throw new ArgumentException("A concrete class needs a static factory; an abstract parent has no factory.", nameof(factory));
        if (!Managed || _registeringLevel < 0) throw new InvalidOperationException("Extension classes register during module initialization.");
        var parentType = typeof(T).BaseType;
        var parent = parentType is null ? null : ResourceFileTypes.FindExtensionClass(parentType);
        if (parent is null && parentType is not null && parentType.Assembly != typeof(Extension).Assembly)
            throw new InvalidOperationException("Register the project parent before its extension descendant.");
        var definition = new ExtensionClass(this, _registeringLevel, classID, typeof(T), parent,
            () => factory is null ? throw new InvalidOperationException("An abstract extension class cannot instantiate.") : factory(),
            owner => bind((T)owner), owner => clear((T)owner));
        _classes.Add(definition);
        try { ResourceFileTypes.RegisterFactory(definition.FileType); ExtensionBindings.MarkRole(typeof(T)); }
        catch { _classes.Remove(definition); throw; }
    }

    /// <summary>Constructs and binds a fresh instance of a class owned by this module.</summary>
    /// <typeparam name="T">Expected typed result role.</typeparam><param name="classID">Registered exact class identity.</param>
    /// <returns>The fresh caller-owned instance after parent/child binding.</returns>
    /// <exception cref="ArgumentNullException">The class identity is null.</exception>
    /// <exception cref="InvalidOperationException">Class, result role, lifecycle or caller is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="Exception">Construction or binding fails; fresh-result cleanup is attempted.</exception>
    public T New<T>(string classID) where T : ElectronObject
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(classID); ExtensionManager.Service.EnsureOwner();
        var type = ResourceFileTypes.Find(classID);
        if (type?.ExtensionClass?.Owner != this || !typeof(T).IsAssignableFrom(type.Type))
            throw new InvalidOperationException("The module does not own a class of that result role.");
        return (T)type.Factory();
    }

    internal void EnsureClassCreation(int level)
    {
        ExtensionManager.Service.EnsureOwner(); ThrowIfDisposed();
        if (!Managed || !_open || _retiring || _highest < level) throw new InvalidOperationException("The extension class is unavailable.");
    }
    internal bool ClassAvailable(int level) => Volatile.Read(ref Managed) && Volatile.Read(ref _open) && !Volatile.Read(ref _retiring) && (Volatile.Read(ref _initializedLevels) & (1 << level)) != 0;
    internal void EnterClassCreation() => _creating++;
    internal void ExitClassCreation() => _creating--;
    internal void EnsureNoClassCreation()
    {
        if (_creating != 0) throw new InvalidOperationException("A module cannot retire during a participating class lifetime operation.");
    }
    private void RetireClasses(int level, ref List<Exception>? errors)
    {
        ExtensionBindings.ClearAffected(this, level, ref errors);
        for (var i = _classes.Count - 1; i >= 0; i--)
            if (_classes[i].Level == level) { ResourceFileTypes.RemoveExtensionClass(_classes[i]); _classes.RemoveAt(i); }
    }
}

internal sealed class ExtensionClass
{
    internal readonly Extension Owner;
    internal readonly int Level;
    internal readonly ResourceFileType FileType;
    internal readonly string? ParentID;
    internal readonly Type? ParentType;
    internal readonly Action<ElectronObject> Bind, Clear;
    internal ExtensionClass(Extension owner, int level, string id, Type type, ExtensionClass? parent,
        Func<ElectronObject> factory, Action<ElectronObject> bind, Action<ElectronObject> clear)
    {
        Owner = owner; Level = level; ParentID = parent?.FileType.ID; ParentType = parent?.FileType.Type;
        Bind = bind; Clear = clear; FileType = new(id, type, factory) { ExtensionClass = this };
    }
    internal ExtensionClass[] Chain()
    {
        List<ExtensionClass> chain = []; ExtensionClass? current = this;
        while (current is not null)
        {
            if (!ReferenceEquals(ResourceFileTypes.Find(current.FileType.ID), current.FileType)) throw new InvalidOperationException("An extension class was retired.");
            chain.Add(current);
            if (current.ParentID is null) break;
            var parent = ResourceFileTypes.Find(current.ParentID)?.ExtensionClass;
            if (parent is null || parent.FileType.Type != current.ParentType) throw new InvalidOperationException("An extension parent is unavailable or its CLR ABI changed.");
            current = parent;
        }
        chain.Reverse(); return chain.ToArray();
    }
    internal IDisposable BeginCreation()
    {
        var chain = Chain(); foreach (var item in chain) item.Owner.EnsureClassCreation(item.Level);
        var creation = new Creation(chain);
        foreach (var item in chain) item.Owner.EnterClassCreation();
        return creation;
    }
    private sealed class Creation(ExtensionClass[] chain) : IDisposable
    {
        public void Dispose() { foreach (var item in chain) item.Owner.ExitClassCreation(); }
    }
}

internal sealed class ExtensionBinding(string classID, Type type, Extension module)
{
    internal readonly string ClassID = classID;
    internal readonly Type Type = type;
    internal readonly WeakReference<Extension> Module = new(module);
    internal ExtensionClass[] Chain = [];
    private int _bound;
    internal void Bind(ElectronObject owner, ExtensionClass definition)
    {
        if (definition.FileType.Type != Type || !Type.IsInstanceOfType(owner)) throw new InvalidOperationException("Replacing a live CLR class ABI requires a restart.");
        using var creation = definition.BeginCreation();
        Chain = definition.Chain();
        foreach (var item in Chain) { _bound++; item.Bind(owner); }
    }
    internal void Clear(ElectronObject owner, ref List<Exception>? errors, bool disposing = false)
    {
        var retiring = Chain;
        foreach (var item in retiring) item.Owner.EnterClassCreation();
        try
        {
            while (_bound > 0)
            {
                var item = Chain[--_bound];
                if (disposing || !owner.IsDisposed) try { item.Clear(owner); } catch (Exception error) { (errors ??= []).Add(error); }
            }
            Chain = [];
        }
        finally { foreach (var item in retiring) item.Owner.ExitClassCreation(); }
    }
}

internal sealed record ExtensionInstanceState(WeakReference<ElectronObject> Owner, ExtensionBinding Binding,
    (string Name, Type Type, StoredPropertyValue Value)[] Properties, WeakReference<Extension>[] Participants);

internal static class ExtensionBindings
{
    private static readonly ConditionalWeakTable<ElectronObject, ExtensionBinding> Instances = new();
    private static readonly ConditionalWeakTable<Type, object> Roles = new();
    internal static void MarkRole(Type type) => Roles.GetValue(type, static _ => new());
    internal static bool IsExtensionRole(Type type) => Roles.TryGetValue(type, out _);
    private static ExtensionClass? Definition(Type type)
    {
        for (var current = type; current is not null && current.Assembly != typeof(Extension).Assembly; current = current.BaseType)
            if (IsExtensionRole(current)) return ResourceFileTypes.FindExtensionClass(current) ?? throw new InvalidOperationException("The extension base class is unavailable.");
        return null;
    }
    internal static IDisposable? BeginConstruction(Type type) => Definition(type)?.BeginCreation();
    internal static bool CanConstruct(Type type)
    {
        try { return Definition(type) is not { } definition || definition.Chain().All(item => item.Owner.ClassAvailable(item.Level)); }
        catch (InvalidOperationException) { return false; }
    }
    internal static void AttachConstructed(ElectronObject owner)
    {
        if (!Instances.TryGetValue(owner, out _) && Definition(owner.GetType()) is { } definition) Attach(owner, definition);
    }
    internal static IDisposable? BeginDisposal(ElectronObject owner)
    {
        if (!Instances.TryGetValue(owner, out var binding)) return null;
        ExtensionManager.Service.EnsureOwner();
        var chain = binding.Chain;
        var disposal = new Disposal(chain);
        foreach (var item in chain) item.Owner.EnterClassCreation();
        return disposal;
    }
    private sealed class Disposal(ExtensionClass[] chain) : IDisposable
    {
        public void Dispose() { foreach (var item in chain) item.Owner.ExitClassCreation(); }
    }
    internal static Exception? Detach(ElectronObject owner)
    {
        if (!Instances.TryGetValue(owner, out var binding)) return null;
        List<Exception>? errors = null;
        try { binding.Clear(owner, ref errors, disposing: true); }
        finally { Instances.Remove(owner); }
        return errors is null ? null : errors.Count == 1 ? errors[0] : new AggregateException("Extension class cleanup failed.", errors);
    }
    internal static void Attach(ElectronObject owner, ExtensionClass definition)
    {
        var binding = new ExtensionBinding(definition.FileType.ID, definition.FileType.Type, definition.Owner); Instances.Add(owner, binding);
        try { binding.Bind(owner, definition); }
        catch (Exception error)
        {
            List<Exception>? errors = [error]; binding.Clear(owner, ref errors); Instances.Remove(owner);
            if (errors!.Count > 1) throw new AggregateException(errors);
            throw;
        }
    }
    internal static List<ExtensionInstanceState> Capture(Extension module, int minimum)
    {
        EnsureRetirement(module, minimum);
        List<ExtensionInstanceState> states = [];
        foreach (var pair in Instances)
        {
            if (pair.Key.IsDisposed || !pair.Value.Chain.Any(c => c.Owner == module && c.Level >= minimum)) continue;
            var properties = pair.Key.GetPropertyList().Where(p => p.ShouldStore(pair.Key) && (!p.HasRevertValue || p.CanRevert(pair.Key)))
                .Select(p => (p.Name, p.ValueType, p.CaptureReloadValue(pair.Key))).ToArray();
            states.Add(new(new(pair.Key), pair.Value, properties, pair.Value.Chain.Select(c => new WeakReference<Extension>(c.Owner)).ToArray()));
        }
        return states;
    }
    internal static IDisposable Protect(Extension module, List<ExtensionInstanceState>? states)
    {
        HashSet<Extension> owners = [module];
        foreach (var pair in Instances)
            if (pair.Value.Chain.Any(c => c.Owner == module)) foreach (var item in pair.Value.Chain) owners.Add(item.Owner);
        if (states is not null)
            foreach (var state in states)
            {
                foreach (var reference in state.Participants) if (reference.TryGetTarget(out var owner) && owner.Managed) owners.Add(owner);
                foreach (var item in state.Binding.Chain) owners.Add(item.Owner);
            }
        var participants = owners.ToArray(); var scope = new Operation(participants);
        foreach (var owner in participants) owner.EnterClassCreation();
        return scope;
    }
    private sealed class Operation(Extension[] owners) : IDisposable
    {
        public void Dispose() { foreach (var owner in owners) owner.ExitClassCreation(); }
    }
    internal static void EnsureRetirement(Extension module, int minimum)
    {
        foreach (var pair in Instances)
            if (!pair.Key.IsDisposed && pair.Value.Chain.Any(c => c.Owner == module && c.Level >= minimum) && pair.Key is Node node)
                node.Tree?.EnsureExtensionReplacement();
    }
    internal static void ClearAffected(Extension module, int minimum, ref List<Exception>? errors)
    {
        foreach (var pair in Instances)
            if (pair.Value.Chain.Any(c => c.Owner == module && c.Level >= minimum)) pair.Value.Clear(pair.Key, ref errors);
    }
    internal static void Clear(List<ExtensionInstanceState> states, ref List<Exception>? errors)
    {
        foreach (var state in states) if (state.Owner.TryGetTarget(out var owner)) state.Binding.Clear(owner, ref errors);
    }
    internal static List<ExtensionInstanceState> Rebind(List<ExtensionInstanceState> states)
    {
        List<ExtensionInstanceState> surviving = [];
        foreach (var state in states)
        {
            if (!state.Owner.TryGetTarget(out var owner) || owner.IsDisposed) continue;
            var definition = ResourceFileTypes.Find(state.Binding.ClassID)?.ExtensionClass;
            if (definition is null || !state.Binding.Module.TryGetTarget(out var originalModule) || !ReferenceEquals(definition.Owner, originalModule)) continue;
            // A removed registered ancestor retires its descendants too.
            if (!ParentAvailable(definition)) continue;
            state.Binding.Bind(owner, definition); surviving.Add(state);
        }
        return surviving;
    }
    private static bool ParentAvailable(ExtensionClass definition)
    {
        while (definition.ParentID is { } id)
        {
            var parent = ResourceFileTypes.Find(id)?.ExtensionClass;
            if (parent is null) return false;
            definition = parent;
        }
        return true;
    }
    internal static void Restore(List<ExtensionInstanceState> states)
    {
        foreach (var state in states)
        {
            if (!state.Owner.TryGetTarget(out var owner) || owner.IsDisposed) continue;
            var properties = owner.GetPropertyList().ToDictionary(p => p.Name, StringComparer.Ordinal);
            foreach (var (name, type, value) in state.Properties)
                if (properties.TryGetValue(name, out var property) && property.IsStored && property.ValueType == type && !property.MatchesStoredValue(owner, value))
                    property.RestoreStoredValue(owner, value, static resource => resource);
        }
    }
    internal static void Notify(List<ExtensionInstanceState> states, ref List<Exception>? errors)
    {
        foreach (var state in states)
            if (state.Owner.TryGetTarget(out var owner) && !owner.IsDisposed)
                try { owner.Notify(ElectronObject.NotificationExtensionReloaded); } catch (Exception error) { (errors ??= []).Add(error); }
    }
}
