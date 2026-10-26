namespace Electron2D;

public sealed partial class AccessibilityServer
{
    /// <summary>Associates a producer's typed model or value with a live accessibility element.</summary>
    /// <typeparam name="T">The model or value type.</typeparam><param name="id">Live element.</param>
    /// <param name="meta">Value to retain; null clears the slot.</param>
    /// <remarks>The slot retains references without owning or disposing them. Value types are boxed on assignment.
    /// Replacement, clearing and element retirement release the previous reference. Metadata is process-local;
    /// it is neither copied into scenes nor published as a native attribute. Registered actions can retrieve it by identity.</remarks>
    /// <exception cref="NotSupportedException">No bridge is active.</exception><exception cref="KeyNotFoundException">The element is absent or retired.</exception><exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static void ElementSetMeta<T>(RID id, T? meta) => Require().Get(id).Metadata = meta;

    /// <summary>Gets the producer's model or value using CLR type compatibility.</summary>
    /// <typeparam name="T">The requested type; use a nullable value type to distinguish an empty slot from its default value.</typeparam>
    /// <param name="id">Live element.</param><returns>The retained value, or default when the slot is empty.</returns>
    /// <remarks>Reference values preserve identity; value types are copied. Retrieval performs no numeric conversion or cloning.</remarks>
    /// <exception cref="InvalidCastException">The nonempty slot is incompatible with the requested type.</exception>
    /// <exception cref="NotSupportedException">No bridge is active.</exception><exception cref="KeyNotFoundException">The element is absent or retired.</exception><exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static T? ElementGetMeta<T>(RID id)
    {
        var meta = Require().Get(id).Metadata;
        return meta is null ? default : (T)meta;
    }
}
