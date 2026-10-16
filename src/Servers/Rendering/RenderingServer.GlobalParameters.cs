namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Identifies the logical value consumed by a global canvas shader parameter.</summary>
    public enum GlobalShaderParameterType
    {
        /// <summary>Boolean scalar.</summary>
        Bool = 0,
        /// <summary>Two boolean components carried by an integer mask.</summary>
        BVec2 = 1,
        /// <summary>Three boolean components carried by an integer mask.</summary>
        BVec3 = 2,
        /// <summary>Four boolean components carried by an integer mask.</summary>
        BVec4 = 3,
        /// <summary>Signed integer scalar.</summary>
        Int = 4,
        /// <summary>Two signed integer components.</summary>
        IVec2 = 5,
        /// <summary>Three signed integer components.</summary>
        IVec3 = 6,
        /// <summary>Four signed integer components.</summary>
        IVec4 = 7,
        /// <summary>Integer position and size.</summary>
        Rect2i = 8,
        /// <summary>Unsigned integer scalar.</summary>
        UInt = 9,
        /// <summary>Two unsigned component bit patterns.</summary>
        UVec2 = 10,
        /// <summary>Three unsigned component bit patterns.</summary>
        UVec3 = 11,
        /// <summary>Four unsigned component bit patterns.</summary>
        UVec4 = 12,
        /// <summary>Single-precision scalar.</summary>
        Float = 13,
        /// <summary>Two floating-point components.</summary>
        Vec2 = 14,
        /// <summary>Three numeric floating-point components.</summary>
        Vec3 = 15,
        /// <summary>Four numeric floating-point components.</summary>
        Vec4 = 16,
        /// <summary>Literal RGBA components.</summary>
        Color = 17,
        /// <summary>Floating-point position and size.</summary>
        Rect2 = 18,
        /// <summary>Two-column numeric basis carried by Transform.</summary>
        Mat2 = 19,
        /// <summary>Nine column-major numeric matrix elements.</summary>
        Mat3 = 20,
        /// <summary>Sixteen column-major numeric matrix elements.</summary>
        Mat4 = 21,
        /// <summary>Planar basis and origin carried by Transform.</summary>
        Transform2D = 22,
        /// <summary>Borrowed ordinary image.</summary>
        Sampler2D = 24,
        /// <summary>Borrowed image array.</summary>
        Sampler2DArray = 25,
        /// <summary>Sentinel returned for an absent declaration.</summary>
        Max = 29,
    }

    private readonly ShaderGlobalParameters _globalParameters = new();
    internal static ShaderGlobalParameters? ActiveGlobalParameters => Service?._globalParameters;
    internal static IEnumerable<PropertyDescriptor> SceneGlobalDescriptors(ShaderGlobalParameters authored)
    {
        if (Service is not { } server) return [];
        server.EnsureOwner(); return server._globalParameters.DeclaredSceneDescriptors(authored);
    }
    internal static void SyncSceneGlobals(SceneTree tree)
    {
        if (Service is not { } server || !ReferenceEquals(server._window.Tree, tree)) return;
        server.EnsureTextureChange(); server._globalParameters.BindScene(tree.SelectedShaderGlobals?.Parameters);
    }
    internal static void ValidateSceneGlobal(ShaderGlobalsOverride node, string name, GlobalShaderParameterType kind)
    {
        if (Service is not { } server || node.RegisteredTree is not { } tree ||
            !ReferenceEquals(server._window.Tree, tree) || !ReferenceEquals(tree.SelectedShaderGlobals, node)) return;
        server.EnsureTextureChange(); if (kind != GlobalShaderParameterType.Max) server._globalParameters.ValidateSceneValue(name, kind);
    }
    internal static void RefreshSceneGlobal(ShaderGlobalsOverride node, string name)
    {
        if (Service is not { } server || node.RegisteredTree is not { } tree ||
            !ReferenceEquals(server._window.Tree, tree) || !ReferenceEquals(tree.SelectedShaderGlobals, node)) return;
        server.EnsureTextureChange();
        server._globalParameters.BindScene(node.Parameters); server._globalParameters.RefreshSceneValue(name);
    }
    private static ShaderGlobalParameters GlobalParameters(bool mutate = false)
    {
        var server = RequireService();
        if (mutate) server.EnsureTextureChange(); else server.EnsureOwner();
        return server._globalParameters;
    }

    /// <summary>Registers a typed global scalar, vector, rectangle, color or planar basis.</summary>
    /// <typeparam name="T">The concrete scalar, vector, rectangle, color or Transform carrier required by the role.</typeparam>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="type">The declared logical role.</param>
    /// <param name="value">Copied base value.</param>
    /// <remarks>Duplicate names reject. Globals belong to the renderer session. Color is literal RGBA; Mat2 uses the Transform basis, while Transform2D also retains Origin.</remarks>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterAdd<T>(string name, GlobalShaderParameterType type, T value) where T : unmanaged => GlobalParameters(true).Add(name, type, value);

    /// <summary>Registers a copied numeric global matrix.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="type">The declared logical role.</param>
    /// <param name="value">Nine Mat3 or sixteen Mat4 finite elements in column-major order.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterAdd(string name, GlobalShaderParameterType type, ReadOnlySpan<float> value) => GlobalParameters(true).AddMatrix(name, type, value);

    /// <summary>Registers a borrowed global ordinary texture.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="value">Borrowed image, or null until supplied. Removing the entry never disposes it.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterAdd(string name, Texture? value) => GlobalParameters(true).AddTexture(name, value, false);

    /// <summary>Registers a borrowed global image array.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="value">Borrowed array, or null until supplied. Removing the entry never disposes it.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterAdd(string name, TextureLayered? value) => GlobalParameters(true).AddTexture(name, value, true);

    /// <summary>Returns a copied typed base value, independently of its override.</summary>
    /// <typeparam name="T">The concrete scalar, vector, rectangle, color or Transform carrier required by the role.</typeparam>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <returns>The registered base value.</returns>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static T GlobalShaderParameterGet<T>(string name) where T : unmanaged => GlobalParameters().Get<T>(name);

    /// <summary>Returns copied column-major base matrix elements.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <returns>A new nine- or sixteen-element array.</returns>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static float[] GlobalShaderParameterGetMatrix(string name) => GlobalParameters().GetMatrix(name);

    /// <summary>Copies base matrix elements into caller storage.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="destination">Exact nine- or sixteen-element destination. A rejected size preserves it.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterGetMatrix(string name, Span<float> destination) => GlobalParameters().GetMatrix(name, destination);

    /// <summary>Returns the borrowed ordinary base texture, independently of its override.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <returns>The borrowed image, possibly null.</returns>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static Texture? GlobalShaderParameterGetTexture(string name) => (Texture?)GlobalParameters().GetTexture(name, false);

    /// <summary>Returns the borrowed base image array, independently of its override.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <returns>The borrowed array, possibly null.</returns>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static TextureLayered? GlobalShaderParameterGetLayeredTexture(string name) => (TextureLayered?)GlobalParameters().GetTexture(name, true);

    /// <summary>Lists the registered global names in ordinal order.</summary>
    /// <returns>An independent sorted array.</returns>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static string[] GlobalShaderParameterGetList() => GlobalParameters().Names();

    /// <summary>Returns the declared logical role of a global parameter.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <returns>Its role, or Max when absent.</returns>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static GlobalShaderParameterType GlobalShaderParameterGetType(string name) => GlobalParameters().GetType(name);

    /// <summary>Removes a global declaration and its override.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <remarks>Absent names are harmless. A shader still using a removed name rejects on its next submission.</remarks>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterRemove(string name) => GlobalParameters(true).Remove(name);

    /// <summary>Updates a typed global base value while retaining any override.</summary>
    /// <typeparam name="T">The concrete scalar, vector, rectangle, color or Transform carrier required by the role.</typeparam>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="value">Copied base value.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterSet<T>(string name, T value) where T : unmanaged => GlobalParameters(true).Set(name, value);

    /// <summary>Replaces copied base matrix elements while retaining any override.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="value">Exact finite column-major element sequence.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterSet(string name, ReadOnlySpan<float> value) => GlobalParameters(true).SetMatrix(name, value);

    /// <summary>Updates a borrowed ordinary base texture while retaining any override.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="value">Borrowed image, possibly null.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterSet(string name, Texture? value) => GlobalParameters(true).SetTexture(name, value, false);

    /// <summary>Updates a borrowed base image array while retaining any override.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="value">Borrowed image array, possibly null.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterSet(string name, TextureLayered? value) => GlobalParameters(true).SetTexture(name, value, true);

    /// <summary>Sets or clears a temporary typed global override.</summary>
    /// <typeparam name="T">The concrete scalar, vector, rectangle, color or Transform carrier required by the role.</typeparam>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="value">Copied effective value, or null to restore the current base value.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterSetOverride<T>(string name, T? value) where T : unmanaged => GlobalParameters(true).SetOverride(name, value);

    /// <summary>Sets or clears a copied numeric matrix override.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="value">Exact finite column-major elements; an empty span clears the override.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterSetMatrixOverride(string name, ReadOnlySpan<float> value) => GlobalParameters(true).SetMatrix(name, value, true);

    /// <summary>Sets or clears a borrowed ordinary texture override.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="value">Borrowed effective image, or null to restore the base image.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterSetOverride(string name, Texture? value) => GlobalParameters(true).SetTexture(name, value, false, true);

    /// <summary>Sets or clears a borrowed image-array override.</summary>
    /// <param name="name">Exact case-sensitive nonblank global name.</param>
    /// <param name="value">Borrowed effective array, or null to restore the base array.</param>
    /// <exception cref="ArgumentException">A name, type, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">No renderer is active, the caller is not its owner, or a mutation reenters native submission.</exception>
    public static void GlobalShaderParameterSetOverride(string name, TextureLayered? value) => GlobalParameters(true).SetTexture(name, value, true, true);

}
