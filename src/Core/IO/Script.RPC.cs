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
    /// <returns>A caller-owned snapshot of shared immutable method tokens and copied policies; empty for unbound or Resource roles.</returns>
    /// <remarks>Most-derived declarations come first, followed by unshadowed registered bases in declaration order.
    /// Instance RPCConfig overrides/removals affect only that Node. Editable source text does not change compiled declarations.</remarks>
    /// <exception cref="ObjectDisposedException">This script resource is disposed.</exception>
    public RPCRegistration[] GetRPCConfig()
    {
        Type? type; lock (_gate) { ThrowIfDisposed(); type = _registration?.Type; }
        return type is null ? [] : GetClassRPCConfig(type);
    }
    internal static RPCRegistration[] GetClassRPCConfig(Type type)
    {
        lock (CatalogGate)
        {
            List<RPCRegistration>? result = null; HashSet<uint>? ids = null;
            for (var current = type; current is not null && current.Assembly != typeof(Script).Assembly; current = current.BaseType)
                if (ByType.TryGetValue(current, out var record))
                    foreach (var (id, entry) in record.RPC)
                        if ((ids ??= []).Add(id)) (result ??= []).Add(entry);
            return result?.ToArray() ?? [];
        }
    }
    internal static bool TryGetClassRPC(Type type, uint id, out RPCRegistration registration)
    {
        lock (CatalogGate)
        {
            for (var current = type; current is not null && current.Assembly != typeof(Script).Assembly; current = current.BaseType)
                if (ByType.TryGetValue(current, out var record) && record.RPC.TryGetValue(id, out registration)) return true;
            registration = default; return false;
        }
    }
}
