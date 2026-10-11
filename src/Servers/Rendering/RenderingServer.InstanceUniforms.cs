namespace Electron2D;

public sealed partial class RenderingServer
{
    private CanvasItem InstanceItem(RID rid)
    { EnsureOwner(); var item = RenderingCanvasItemRegistry.Resolve(rid); CheckCanvasItemOwner(item); return item; }
    /// <summary>Sets a copied scalar/vector instance uniform on a live scene or retained canvas item.</summary>
    /// <typeparam name="T">Supported scalar/vector carrier.</typeparam>
    /// <param name="instance">Live item identity.</param>
    /// <param name="parameter">Case-sensitive parameter name.</param>
    /// <param name="value">Copied override, or null to restore the shader default.</param>
    /// <exception cref="ArgumentException">The identity, name, type or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer, wrong owner or capture mutation.</exception>
    public static void CanvasItemSetInstanceShaderParameter<T>(RID instance, string parameter, T? value) where T : unmanaged
    { var server = RequireService(); _ = server.Change(instance, out var item); item.SetInstanceShaderParameter(parameter, value); }
    /// <summary>Reads a live item's copied override or current declared default.</summary>
    /// <typeparam name="T">Matching scalar/vector carrier.</typeparam>
    /// <param name="instance">Live item identity.</param>
    /// <param name="parameter">Case-sensitive parameter name.</param>
    /// <returns>The effective value, or null when absent.</returns>
    /// <exception cref="ArgumentException">The identity, name or type is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer or wrong owner.</exception>
    public static T? CanvasItemGetInstanceShaderParameter<T>(RID instance, string parameter) where T : unmanaged => RequireService().InstanceItem(instance).GetInstanceShaderParameter<T>(parameter);
    /// <summary>Reads the current instance-uniform declaration's default, ignoring overrides.</summary>
    /// <typeparam name="T">Matching scalar/vector carrier.</typeparam>
    /// <param name="instance">Live item identity.</param>
    /// <param name="parameter">Case-sensitive parameter name.</param>
    /// <returns>The declared default, or null when the current shader has no such Instance member.</returns>
    /// <exception cref="ArgumentException">The identity, name or type is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer or wrong owner.</exception>
    public static T? CanvasItemGetInstanceShaderParameterDefaultValue<T>(RID instance, string parameter) where T : unmanaged
    { var item = RequireService().InstanceItem(instance); return item.InstanceUniforms.Get<T>(parameter, item.GetInstanceProgram(parameter), defaults: true); }
    /// <summary>Lists typed instance-uniform descriptors of the item's effective shader.</summary>
    /// <param name="instance">Live scene or retained item identity.</param>
    /// <returns>Immutable parameter descriptors, empty without a shader Instance block.</returns>
    /// <exception cref="ArgumentException">The identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer or wrong owner.</exception>
    public static IReadOnlyList<PropertyDescriptor> CanvasItemGetInstanceShaderParameterList(RID instance) => Array.AsReadOnly(RequireService().InstanceItem(instance).InstanceParameterDescriptors().ToArray());
}
