using System.Diagnostics.CodeAnalysis;

namespace Electron2D;

public sealed partial class Script
{
    /// <summary>Registers a compiled Node source with immutable class RPC declarations and its typed property schema.</summary>
    /// <typeparam name="TNode">Concrete project Node role.</typeparam>
    /// <param name="id">Stable portable class identity.</param><param name="sourcePath">Source path proven by the compiled portable PDB.</param>
    /// <param name="factory">Static exact default constructor, or null to require an explicit typed constructor at each New call.</param><param name="rpcConfiguration">Copied declarations with distinct IDs and compatible receiver roles.</param>
    /// <param name="properties">Copied user-defined typed property descriptors.</param>
    /// <remarks>Source/factory validation is identical to RegisterNode. Class declarations are inherited by actual Node
    /// instances and execute through their ordinary multiplayer interface without constructing metadata prototypes.
    /// Derived IDs override base declarations. Shared tokens and their captured state live with the compiled registration.
    /// Source text edits do not alter this compiled configuration. A null factory registers metadata only;
    /// explicit typed New construction remains available for concrete types.</remarks>
    /// <exception cref="ArgumentException">A factory, source, schema, declaration role, duplicate ID or method budget is invalid.</exception>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="InvalidOperationException">The class identity/type/path is already registered.</exception>
    /// <exception cref="NotSupportedException">Matching compiled assembly and portable symbols are unavailable.</exception>
    [RequiresAssemblyFiles("Compiled source registration requires a file-backed assembly and matching portable PDB.")]
    [RequiresUnreferencedCode("Compiled source registration inspects the complete project method, event and constant declarations.")]
    public static void RegisterNodeWithRPC<TNode>(string id, string sourcePath, Func<TNode>? factory, ReadOnlySpan<RPCRegistration> rpcConfiguration, params PropertyDescriptor[] properties) where TNode : Node
    {
        Register(typeof(TNode), id, sourcePath, factory is null ? null : () => factory(), properties,
            factory is null ? null : () => ResourceFileTypes.RegisterNode(id, factory), factory?.Target, rpcConfiguration);
    }

    /// <summary>Registers live compiled Node binding code with immutable typed RPC declarations.</summary>
    /// <typeparam name="TNode">Stable Node receiver role.</typeparam><typeparam name="TCode">Project code declaration.</typeparam>
    /// <param name="id">Portable source identity.</param><param name="sourcePath">Checked compiled source document.</param>
    /// <param name="factory">Static exact default owner constructor, or null.</param><param name="bind">Installs owner state and typed callbacks.</param>
    /// <param name="clear">Retires state/callbacks, including after partial installation.</param>
    /// <param name="rpcConfiguration">Copied declarations with distinct IDs and compatible owner roles.</param><param name="properties">Typed owner schema.</param>
    /// <remarks>Uses the same validation/lifetime as RegisterBinding. Active code declarations precede unshadowed owner-class defaults.
    /// Policies on these live defaults and removals follow wire IDs across binding replacement; explicit standalone methods retain their own lifetime.
    /// Participants must agree on each ID's codec. Source edits do not replace installed declarations.</remarks>
    /// <exception cref="ArgumentException">Code, callbacks, factory, source, schema or RPC declarations are invalid.</exception>
    /// <exception cref="ArgumentNullException">A required callback is null.</exception>
    /// <exception cref="InvalidOperationException">Identity, type or source is already registered.</exception>
    /// <exception cref="NotSupportedException">Matching compiled assembly and portable symbols are unavailable.</exception>
    [RequiresAssemblyFiles("Compiled source registration requires a file-backed assembly and matching portable PDB.")]
    [RequiresUnreferencedCode("Compiled source registration inspects the complete project method, event and constant declarations.")]
    public static void RegisterBindingWithRPC<TNode, TCode>(string id, string sourcePath, Func<TNode>? factory, Action<TNode> bind, Action<TNode> clear,
        ReadOnlySpan<RPCRegistration> rpcConfiguration, params PropertyDescriptor[] properties) where TNode : Node where TCode : class
        => RegisterBindingCore<TNode, TCode>(id, sourcePath, factory, bind, clear, rpcConfiguration, properties);

    /// <summary>Registers an abstract compiled Node source with inherited class RPC declarations and typed metadata.</summary>
    /// <typeparam name="TNode">Abstract project Node role.</typeparam><param name="id">Stable script identity.</param>
    /// <param name="sourcePath">Source path proven by portable PDB.</param><param name="rpcConfiguration">Copied declarations with distinct IDs and compatible receiver roles.</param>
    /// <param name="properties">Copied user-defined typed schema.</param>
    /// <remarks>No factory is created. Descendant nodes use these declarations unless a more derived registration
    /// supplies the same ID. Later base registration is observed by subsequent queries and invocations.</remarks>
    /// <exception cref="ArgumentException">The role is concrete, source/schema is invalid, or RPC declarations are incompatible.</exception>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="InvalidOperationException">The class identity/type/path is already registered.</exception>
    /// <exception cref="NotSupportedException">Matching compiled assembly and portable symbols are unavailable.</exception>
    [RequiresAssemblyFiles("Compiled source registration requires a file-backed assembly and matching portable PDB.")]
    [RequiresUnreferencedCode("Compiled source registration inspects the complete project method, event and constant declarations.")]
    public static void RegisterAbstractWithRPC<TNode>(string id, string sourcePath, ReadOnlySpan<RPCRegistration> rpcConfiguration, params PropertyDescriptor[] properties) where TNode : Node
    {
        if (!typeof(TNode).IsAbstract) throw new ArgumentException("The script type must be abstract.");
        Register(typeof(TNode), id, sourcePath, null, properties, null, null, rpcConfiguration);
    }

    /// <summary>Returns the compiled class's effective typed RPC declarations without constructing an instance.</summary>
    /// <returns>A caller-owned snapshot of shared immutable method tokens and copied policies; empty for unbound or non-Node roles.</returns>
    /// <remarks>Most-derived declarations come first, followed by unshadowed registered bases in declaration order.
    /// Live code/base declarations precede unshadowed stable owner-class defaults.
    /// Instance RPCConfig overrides/removals affect only that Node. Editable source text does not change installed declarations.</remarks>
    /// <exception cref="ObjectDisposedException">This script resource is disposed.</exception>
    public RPCRegistration[] GetRPCConfig()
    {
        ScriptRegistration? record; lock (_gate) { ThrowIfDisposed(); record = _registration; }
        return record is null ? [] : GetClassRPCConfig(record.InstanceType, record.BindingOwner is null ? null : record.Type);
    }
    internal static RPCRegistration[] GetClassRPCConfig(Type type, Type? sourceType = null)
    {
        lock (CatalogGate)
        {
            List<RPCRegistration>? result = null; HashSet<uint>? ids = null;
            Add(sourceType ?? type);
            if (sourceType is not null) Add(type);
            return result?.ToArray() ?? [];
            void Add(Type declaration)
            {
                for (var current = declaration; current is not null && current.Assembly != typeof(Script).Assembly; current = current.BaseType)
                    if (ByType.TryGetValue(current, out var record))
                        foreach (var (id, entry) in record.RPC)
                            if (entry.Method.Accepts(type) && (ids ??= []).Add(id)) (result ??= []).Add(entry);
            }
        }
    }
    internal static bool TryGetClassRPC(Type type, uint id, out RPCRegistration registration, Type? sourceType = null)
    {
        lock (CatalogGate)
        {
            if (sourceType is not null && FindClassRPC(sourceType, type, id, out registration)) return true;
            return FindClassRPC(type, type, id, out registration);
        }
    }
    internal static bool TryGetBindingRPC(Type type, Type sourceType, uint id, out RPCRegistration registration)
    { lock (CatalogGate) return FindClassRPC(sourceType, type, id, out registration); }
    private static bool FindClassRPC(Type declaration, Type receiver, uint id, out RPCRegistration registration)
    {
        for (var current = declaration; current is not null && current.Assembly != typeof(Script).Assembly; current = current.BaseType)
            if (ByType.TryGetValue(current, out var record) && record.RPC.TryGetValue(id, out registration) && registration.Method.Accepts(receiver)) return true;
        registration = default; return false;
    }
}
