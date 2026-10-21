namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Reads the compiled default of a scalar, vector or float2x2 shader uniform.</summary>
    /// <typeparam name="T">The supported unmanaged carrier matching the declared logical type.</typeparam>
    /// <param name="shader">A live owned or borrowed shader identity.</param>
    /// <param name="name">The exact case-sensitive numeric uniform name.</param>
    /// <returns>The authored default, or the implicit zero/identity value when no explicit default is present.</returns>
    /// <remarks>Material overrides do not affect this query. Color/rectangle aliases and boolean masks follow
    /// the typed material contract. Float2x2 uses Transform with zero Origin. Renderer-owned inputs are excluded.</remarks>
    /// <exception cref="ArgumentException">The identity, name, type or scalar/array shape is invalid.</exception>
    /// <exception cref="InvalidOperationException">The rendering service is unavailable or the caller is not its owner.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed during the query.</exception>
    public static T ShaderGetParameterDefault<T>(RID shader, string name) where T : unmanaged => RequireService().ShaderGetParameterDefaultCore<T>(shader, name);

    /// <summary>Returns an independent copy of a fixed scalar/vector/float2x2 default array.</summary>
    /// <typeparam name="T">The supported unmanaged carrier matching each declared element.</typeparam>
    /// <param name="shader">A live owned or borrowed shader identity.</param>
    /// <param name="name">The exact case-sensitive numeric array name.</param>
    /// <returns>One default value per reflected array element.</returns>
    /// <exception cref="ArgumentException">The identity, name, type or array shape is invalid.</exception>
    /// <exception cref="InvalidOperationException">The rendering service is unavailable or the caller is not its owner.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed during the query.</exception>
    public static T[] ShaderGetParameterDefaultArray<T>(RID shader, string name) where T : unmanaged => RequireService().ShaderGetParameterDefaultArrayCore<T>(shader, name);

    /// <summary>Copies a fixed scalar/vector/float2x2 default array into caller storage.</summary>
    /// <typeparam name="T">The supported unmanaged carrier matching each declared element.</typeparam>
    /// <param name="shader">A live owned or borrowed shader identity.</param>
    /// <param name="name">The exact case-sensitive numeric array name.</param>
    /// <param name="destination">Storage with exactly the reflected element count; validation precedes writes.</param>
    /// <remarks>Prepared copies allocate no managed memory.</remarks>
    /// <exception cref="ArgumentException">The identity, name, type, array shape or destination length is invalid.</exception>
    /// <exception cref="InvalidOperationException">The rendering service is unavailable or the caller is not its owner.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed during the query.</exception>
    public static void ShaderGetParameterDefault<T>(RID shader, string name, Span<T> destination) where T : unmanaged => RequireService().ShaderGetParameterDefaultCore(shader, name, destination);

    /// <summary>Returns copied column-major defaults for a numeric mat3/mat4 declaration or its fixed array.</summary>
    /// <param name="shader">A live owned or borrowed shader identity.</param>
    /// <param name="name">The exact case-sensitive matrix parameter name.</param>
    /// <returns>Nine/sixteen components per matrix, concatenated for arrays.</returns>
    /// <exception cref="ArgumentException">The identity, name or matrix role is invalid.</exception>
    /// <exception cref="InvalidOperationException">The rendering service is unavailable or the caller is not its owner.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed during the query.</exception>
    public static float[] ShaderGetMatrixParameterDefault(RID shader, string name) => RequireService().ShaderGetMatrixParameterDefaultCore(shader, name);

    /// <summary>Copies column-major mat3/mat4 defaults into caller storage.</summary>
    /// <param name="shader">A live owned or borrowed shader identity.</param>
    /// <param name="name">The exact case-sensitive matrix parameter name.</param>
    /// <param name="destination">Exactly nine/sixteen components per matrix, including every array element.</param>
    /// <remarks>Validation precedes writes; prepared copies allocate no managed memory.</remarks>
    /// <exception cref="ArgumentException">The identity, name, matrix role or destination length is invalid.</exception>
    /// <exception cref="InvalidOperationException">The rendering service is unavailable or the caller is not its owner.</exception>
    /// <exception cref="ObjectDisposedException">The shader is disposed during the query.</exception>
    public static void ShaderGetMatrixParameterDefault(RID shader, string name, Span<float> destination) => RequireService().ShaderGetMatrixParameterDefaultCore(shader, name, destination);
}
