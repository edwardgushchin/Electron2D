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

    /// <summary>Atomically replaces both copied canvas stages of a caller-owned shader.</summary>
    /// <param name="shader">The shader RID owned by the current renderer session.</param>
    /// <param name="vertexBytecode">The authored vertex module.</param><param name="fragmentBytecode">The matching fragment module.</param>
    /// <remarks>Both stages validate before publication; path hints annotate failures. Shared material parameters retain one typed namespace.
    /// Vertex gl_VertexIndex/SV_VertexID preserves the original mesh surface index, including LODs, expanded primitives
    /// and instances. Other canvas geometry retains native stream indices; authored deformation retains CPU bounds.</remarks>
    /// <exception cref="ArgumentException">The identity, code or shared interface is invalid.</exception>
    /// <exception cref="NotSupportedException">A stage uses an unsupported canvas profile.</exception>
    /// <exception cref="InvalidOperationException">The shader is borrowed, the renderer is unavailable or owner/submission rules are violated.</exception>
    public static void ShaderSetSPIRV(RID shader, ReadOnlySpan<byte> vertexBytecode, ReadOnlySpan<byte> fragmentBytecode) => RequireService().ShaderSetSPIRVCore(shader, vertexBytecode, fragmentBytecode);
    /// <summary>Copies a selected authored stage from a live owned or borrowed shader.</summary>
    /// <param name="shader">The live shader RID.</param><param name="stage">Vertex or Fragment.</param>
    /// <returns>The copied module; empty vertex bytes select the renderer's built-in vertex program.</returns>
    /// <exception cref="ArgumentException">The identity or stage value is invalid.</exception>
    /// <exception cref="NotSupportedException">The selected stage is outside the canvas domain.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or access is off-owner.</exception>
    public static byte[] ShaderGetSPIRV(RID shader, RenderingDevice.ShaderStage stage) => RequireService().ShaderGetSPIRVCore(shader, stage);

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
    /// <param name="index">Zero for a scalar binding, otherwise the zero-based fixed descriptor array element.</param>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void ShaderSetDefaultTextureParameter(RID shader, string name, RID texture, int index = 0) => RequireService().ShaderSetDefaultTextureParameterCore(shader, name, texture, index);

    /// <summary>Returns a live program default texture identity, or empty when unset.</summary>
    /// <param name="shader">Live owned/borrowed shader identity as allowed by the operation; mutations require caller ownership.</param>
    /// <param name="name">Exact case-sensitive reflected user parameter name.</param>
    /// <param name="index">Zero for a scalar binding, otherwise the zero-based fixed descriptor array element.</param>
    /// <returns>The borrowed default texture identity, or empty when unset.</returns>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static RID ShaderGetDefaultTextureParameter(RID shader, string name, int index = 0) => RequireService().ShaderGetDefaultTextureParameterCore(shader, name, index);

    /// <summary>Copies explicit sampled-image filtering/addressing into an owned shader.</summary>
    /// <param name="shader">A live shader identity owned by this service.</param>
    /// <param name="name">The exact reflected sampled binding name.</param>
    /// <param name="state">Caller-owned configuration to copy, or null to restore the ordinary policy.</param>
    /// <param name="index">The descriptor-array element, zero for a single image.</param>
    /// <remarks>Uses Shader.SetTextureSamplerState validation, persistence and compatible-reload semantics.
    /// Borrowed shader identities remain readable but cannot be changed through this service operation.</remarks>
    /// <exception cref="ArgumentException">The identity, binding, index or configuration is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or ownership/submission rules reject mutation.</exception>
    /// <exception cref="NotSupportedException">The configuration requires another sampler/image capability.</exception>
    /// <exception cref="ObjectDisposedException">A supplied configuration is disposed.</exception>
    public static void ShaderSetTextureSamplerState(RID shader, string name, RDSamplerState? state, int index = 0) => RequireService().ShaderSetTextureSamplerStateCore(shader, name, state, index);

    /// <summary>Copies the explicit sampler state of a live owned or borrowed shader.</summary>
    /// <param name="shader">A live owned or borrowed shader identity.</param><param name="name">The exact reflected binding name.</param>
    /// <param name="index">The descriptor-array element, zero for a single image.</param>
    /// <returns>A caller-owned disposable state, or null for the ordinary binding policy.</returns>
    /// <exception cref="ArgumentException">The identity, binding or index is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or the caller is not its owner.</exception>
    public static RDSamplerState? ShaderGetTextureSamplerState(RID shader, string name, int index = 0) => RequireService().ShaderGetTextureSamplerStateCore(shader, name, index);

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

    /// <summary>Replaces a numeric mat3/mat4 parameter or entire fixed array in an owned material.</summary>
    /// <param name="material">A live owned programmable material identity.</param>
    /// <param name="name">Exact case-sensitive material member name.</param>
    /// <param name="values">Copied finite column-major values, nine/sixteen per matrix, concatenated for arrays.</param>
    /// <exception cref="ArgumentException">An identity, matrix role, component count or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void MaterialSetMatrixParam(RID material, string name, ReadOnlySpan<float> values) => RequireService().MaterialSetMatrixParamCore(material, name, values);

    /// <summary>Returns an independent column-major copy of a numeric mat3/mat4 parameter or fixed array.</summary>
    /// <param name="material">A live programmable material identity; borrowed identities permit reads.</param>
    /// <param name="name">Exact case-sensitive material member name.</param>
    /// <returns>Nine/sixteen values per matrix, concatenated for arrays; each matrix initially contains identity.</returns>
    /// <exception cref="ArgumentException">An identity or matrix role is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/lifetime rules reject the operation.</exception>
    public static float[] MaterialGetMatrixParam(RID material, string name) => RequireService().MaterialGetMatrixParamCore(material, name);

    /// <summary>Copies a numeric mat3/mat4 parameter or fixed array without managed allocation after preparation.</summary>
    /// <param name="material">A live programmable material identity; borrowed identities permit reads.</param>
    /// <param name="name">Exact case-sensitive material member name.</param>
    /// <param name="destination">Exact-size column-major destination, nine/sixteen values per matrix, concatenated for arrays.</param>
    /// <exception cref="ArgumentException">An identity, matrix role or destination length is invalid; the destination remains unchanged.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/lifetime rules reject the operation.</exception>
    public static void MaterialGetMatrixParam(RID material, string name, Span<float> destination) => RequireService().MaterialGetMatrixParamCore(material, name, destination);

    /// <summary>Sets a borrowed sampled texture override; an empty texture clears any named material parameter override.</summary>
    /// <param name="material">Live material identity; mutations require ownership and a programmable material.</param>
    /// <param name="name">Exact case-sensitive reflected user parameter name.</param>
    /// <param name="texture">Borrowed live texture RID, or empty to reset a numeric, matrix, array or image parameter to its shader default.</param>
    /// <exception cref="ArgumentException">An identity, parameter, destination or payload is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void MaterialSetParam(RID material, string name, RID texture) => RequireService().MaterialSetParamTextureCore(material, name, texture);

    /// <summary>Atomically replaces all borrowed sampled-image array overrides; empty elements select shader defaults.</summary>
    /// <param name="material">Live owned programmable material identity.</param>
    /// <param name="name">Exact reflected descriptor array name.</param>
    /// <param name="textures">Copied ordinary or layered image identities with exactly the reflected element count.</param>
    /// <exception cref="ArgumentException">An identity, image role, scalar type, descriptor shape or count differs.</exception>
    /// <exception cref="InvalidOperationException">The service, owner, submission state or reentrant validation rejects the operation.</exception>
    public static void MaterialSetParam(RID material, string name, ReadOnlySpan<RID> textures) => RequireService().MaterialSetParamTextureArrayCore(material, name, textures);

    /// <summary>Returns a copied array of borrowed explicit sampled-image override identities.</summary>
    /// <param name="material">Live programmable material identity.</param>
    /// <param name="name">Exact reflected descriptor array name.</param>
    /// <returns>One identity per element; empty identities select the respective shader defaults.</returns>
    /// <exception cref="ArgumentException">The identity, name or descriptor shape differs.</exception>
    /// <exception cref="InvalidOperationException">The service or owner state rejects the operation.</exception>
    public static RID[] MaterialGetParamTextureArray(RID material, string name) => RequireService().MaterialGetParamTextureArrayCore(material, name);

    /// <summary>Copies sampled-image overrides into exact-length caller storage without prepared allocation.</summary>
    /// <param name="material">Live programmable material identity.</param>
    /// <param name="name">Exact reflected descriptor array name.</param>
    /// <param name="destination">Exact-length identity storage; empty entries select shader defaults.</param>
    /// <exception cref="ArgumentException">An identity, name, descriptor shape or destination length differs.</exception>
    /// <exception cref="InvalidOperationException">The service or owner state rejects the operation.</exception>
    public static void MaterialGetParam(RID material, string name, Span<RID> destination) => RequireService().MaterialGetParamTextureArrayCore(material, name, destination);

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
