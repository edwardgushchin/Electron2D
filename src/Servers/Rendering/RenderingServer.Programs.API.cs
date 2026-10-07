namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Creates a caller-owned canvas program initialized with the built-in fragment shader.</summary>
    /// <returns>A unique shader identity owned until FreeRID or renderer teardown.</returns>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static RID ShaderCreate() => RequireService().ShaderCreateCore();

    /// <summary>Creates a caller-owned programmable material, initially using ordinary canvas colors.</summary>
    /// <returns>A unique material identity owned until FreeRID or renderer teardown.</returns>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static RID MaterialCreate() => RequireService().MaterialCreateCore();

    /// <summary>Replaces owned shader bytecode transactionally through the existing SPIR-V interface validator.</summary>
    /// <param name="shader">Live owned/borrowed shader identity as allowed by the operation; mutations require caller ownership.</param>
    /// <param name="bytecode">Copied compiled fragment SPIR-V in the accepted canvas interface, at most 16 MiB.</param>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void ShaderSetSPIRV(RID shader, ReadOnlySpan<byte> bytecode) => RequireService().ShaderSetSPIRVCore(shader, bytecode);

    /// <summary>Returns an independent copy of an owned or borrowed compiled canvas program.</summary>
    /// <param name="shader">Live owned/borrowed shader identity as allowed by the operation; mutations require caller ownership.</param>
    /// <returns>An independent copied SPIR-V byte array.</returns>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static byte[] ShaderGetSPIRV(RID shader) => RequireService().ShaderGetSPIRVCore(shader);

    /// <summary>Sets diagnostic metadata used in later bytecode validation errors; does not load a file.</summary>
    /// <param name="shader">Live owned/borrowed shader identity as allowed by the operation; mutations require caller ownership.</param>
    /// <param name="path">Non-null diagnostic hint; empty clears it.</param>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void ShaderSetPathHint(RID shader, string path) => RequireService().ShaderSetPathHintCore(shader, path);

    /// <summary>Returns the immutable typed reflected user-parameter catalog of a live canvas program.</summary>
    /// <param name="shader">Live owned/borrowed shader identity as allowed by the operation; mutations require caller ownership.</param>
    /// <returns>The immutable typed user-parameter descriptors for the current program.</returns>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static IReadOnlyList<PropertyDescriptor> GetShaderParameterList(RID shader) => RequireService().GetShaderParameterListCore(shader);

    /// <summary>Sets an owned program default sampled texture, borrowing its live identity.</summary>
    /// <param name="shader">Live owned/borrowed shader identity as allowed by the operation; mutations require caller ownership.</param>
    /// <param name="name">Exact case-sensitive reflected user parameter name.</param>
    /// <param name="texture">Borrowed live texture RID or empty to clear/select default.</param>
    /// <param name="index">Zero; sampled texture arrays retain their own unsupported dependency.</param>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void ShaderSetDefaultTextureParameter(RID shader, string name, RID texture, int index = 0) => RequireService().ShaderSetDefaultTextureParameterCore(shader, name, texture, index);

    /// <summary>Returns a live program default texture identity, or empty when unset.</summary>
    /// <param name="shader">Live owned/borrowed shader identity as allowed by the operation; mutations require caller ownership.</param>
    /// <param name="name">Exact case-sensitive reflected user parameter name.</param>
    /// <param name="index">Zero; sampled texture arrays retain their own unsupported dependency.</param>
    /// <returns>The borrowed default texture identity, or empty when unset.</returns>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static RID ShaderGetDefaultTextureParameter(RID shader, string name, int index = 0) => RequireService().ShaderGetDefaultTextureParameterCore(shader, name, index);

    /// <summary>Selects a borrowed live program for an owned material; empty restores ordinary canvas drawing.</summary>
    /// <param name="material">Live material identity; mutations require ownership and a programmable material.</param>
    /// <param name="shader">Live owned/borrowed shader identity as allowed by the operation; mutations require caller ownership.</param>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void MaterialSetShader(RID material, RID shader) => RequireService().MaterialSetShaderCore(material, shader);

    /// <summary>Updates one reflected scalar/vector/matrix parameter in an owned material.</summary>
    /// <typeparam name="T">Unmanaged scalar/vector/matrix type matching the existing typed material contract.</typeparam>
    /// <param name="material">Live material identity; mutations require ownership and a programmable material.</param>
    /// <param name="name">Exact case-sensitive reflected user parameter name.</param>
    /// <param name="value">Typed finite value matching the reflected uniform.</param>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void MaterialSetParam<T>(RID material, string name, T value) where T : unmanaged => RequireService().MaterialSetParamCore(material, name, value);

    /// <summary>Copies a complete fixed-size uniform array into an owned material after validation.</summary>
    /// <typeparam name="T">Unmanaged scalar/vector/matrix type matching the existing typed material contract.</typeparam>
    /// <param name="material">Live material identity; mutations require ownership and a programmable material.</param>
    /// <param name="name">Exact case-sensitive reflected user parameter name.</param>
    /// <param name="values">Copied finite array values with exactly the reflected length.</param>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void MaterialSetParam<T>(RID material, string name, ReadOnlySpan<T> values) where T : unmanaged => RequireService().MaterialSetParamCore(material, name, values);

    /// <summary>Sets a borrowed sampled texture override; empty restores the program default.</summary>
    /// <param name="material">Live material identity; mutations require ownership and a programmable material.</param>
    /// <param name="name">Exact case-sensitive reflected user parameter name.</param>
    /// <param name="texture">Borrowed live texture RID or empty to clear/select default.</param>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void MaterialSetParam(RID material, string name, RID texture) => RequireService().MaterialSetParamTextureCore(material, name, texture);

    /// <summary>Reads a reflected typed scalar/vector/matrix from a live programmable material.</summary>
    /// <typeparam name="T">Unmanaged scalar/vector/matrix type matching the existing typed material contract.</typeparam>
    /// <param name="material">Live material identity; mutations require ownership and a programmable material.</param>
    /// <param name="name">Exact case-sensitive reflected user parameter name.</param>
    /// <returns>The stored typed parameter value after any required program-layout migration.</returns>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static T MaterialGetParam<T>(RID material, string name) where T : unmanaged => RequireService().MaterialGetParamCore<T>(material, name);

    /// <summary>Returns an independent copy of a reflected fixed-size uniform array.</summary>
    /// <typeparam name="T">Unmanaged scalar/vector/matrix type matching the existing typed material contract.</typeparam>
    /// <param name="material">Live material identity; mutations require ownership and a programmable material.</param>
    /// <param name="name">Exact case-sensitive reflected user parameter name.</param>
    /// <returns>An independent typed array with the reflected element count.</returns>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static T[] MaterialGetParamArray<T>(RID material, string name) where T : unmanaged => RequireService().MaterialGetParamArrayCore<T>(material, name);

    /// <summary>Copies a reflected fixed-size uniform array into exact-length caller storage without prepared allocation.</summary>
    /// <typeparam name="T">Unmanaged scalar/vector/matrix type matching the existing typed material contract.</typeparam>
    /// <param name="material">Live material identity; mutations require ownership and a programmable material.</param>
    /// <param name="name">Exact case-sensitive reflected user parameter name.</param>
    /// <param name="destination">Caller-owned storage with exactly the reflected array length.</param>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void MaterialGetParam<T>(RID material, string name, Span<T> destination) where T : unmanaged => RequireService().MaterialGetParamCore(material, name, destination);

    /// <summary>Returns the explicit sampled-texture override identity; empty means program default selection.</summary>
    /// <param name="material">Live material identity; mutations require ownership and a programmable material.</param>
    /// <param name="name">Exact case-sensitive reflected user parameter name.</param>
    /// <returns>The borrowed explicit texture identity; empty selects the shader default.</returns>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static RID MaterialGetParam(RID material, string name) => RequireService().MaterialGetParamTextureCore(material, name);

    /// <summary>Selects a live material RID for native replay without changing authored CanvasItem.Material; empty selects ordinary drawing.</summary>
    /// <param name="item">Live owned or source canvas item identity.</param>
    /// <param name="material">A live owned or borrowed programmable or built-in material identity, or empty.</param>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetMaterial(RID item, RID material) => RequireService().CanvasItemSetMaterialCore(item, material);

}
