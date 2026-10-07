namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Gets whether a native service is currently available.</summary>
    /// <value>True while a service is published; this observation does not reserve its lifetime.</value>
    public static bool IsAvailable => Service is not null;

    private static RenderingServer RequireService() => Service ?? throw new InvalidOperationException("The RenderingServer service is not active.");

    /// <summary>Creates an owned empty two-dimensional mesh.</summary>
    /// <returns>A logical mesh identity owned until FreeRID or renderer shutdown.</returns>
    /// <exception cref="InvalidOperationException">The renderer is off-owner or submitting.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static RID MeshCreate() => RequireService().MeshCreateCore();

    /// <summary>Adds copied typed surface channels to an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    /// <param name="primitive">Primitive topology.</param>
    /// <param name="arrays">Borrowed arrays copied before mutation.</param>
    /// <param name="flags">Supported two-dimensional update policies.</param>
    /// <exception cref="ArgumentException">The identity or geometry is invalid.</exception>
    /// <exception cref="InvalidOperationException">The mesh is borrowed or owned by another renderer.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MeshAddSurfaceFromArrays(RID mesh, Mesh.PrimitiveType primitive, MeshSurfaceData arrays, Mesh.ArrayFormat flags = Mesh.ArrayFormat.None) => RequireService().MeshAddSurfaceFromArraysCore(mesh, primitive, arrays, flags);

    /// <summary>Gets a live mesh's surface count.</summary>
    /// <param name="mesh">Borrowed or owned mesh identity.</param>
    /// <returns>The current surface count.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int MeshGetSurfaceCount(RID mesh) => RequireService().MeshGetSurfaceCountCore(mesh);

    /// <summary>Gets copied typed channels from one mesh surface.</summary>
    /// <param name="mesh">Borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>Independent caller-owned channels.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static MeshSurfaceData MeshSurfaceGetArrays(RID mesh, int surface) => RequireService().MeshSurfaceGetArraysCore(mesh, surface);

    /// <summary>Removes every surface from an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MeshClear(RID mesh) => RequireService().MeshClearCore(mesh);

    /// <summary>Removes one surface from an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    /// <param name="surface">Existing zero-based index.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MeshSurfaceRemove(RID mesh, int surface) => RequireService().MeshSurfaceRemoveCore(mesh, surface);

    /// <summary>Updates packed position bytes, including partial records, in an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    /// <param name="surface">Existing zero-based index.</param>
    /// <param name="offset">Byte offset into two-float positions.</param>
    /// <param name="data">Little-endian X/Y records.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MeshSurfaceUpdateVertexRegion(RID mesh, int surface, int offset, ReadOnlySpan<byte> data) => RequireService().MeshSurfaceUpdateVertexRegionCore(mesh, surface, offset, data);

    /// <summary>Updates packed attribute bytes, including partial records, in an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    /// <param name="surface">Existing zero-based index.</param>
    /// <param name="offset">Byte offset into color/UV records.</param>
    /// <param name="data">Present RGBA8 UNORM bytes followed by present little-endian UV floats.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MeshSurfaceUpdateAttributeRegion(RID mesh, int surface, int offset, ReadOnlySpan<byte> data) => RequireService().MeshSurfaceUpdateAttributeRegionCore(mesh, surface, offset, data);

    /// <summary>Gets the primary two-dimensional vertex stride for a supported surface format.</summary>
    /// <param name="format">Surface format mask.</param>
    /// <param name="vertexCount">Nonnegative surface vertex count.</param>
    /// <returns>Eight bytes for positions, zero when no vertex channel exists.</returns>
    /// <exception cref="NotSupportedException">The format requires unsupported channels or non-2D vertices.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int MeshSurfaceGetFormatVertexStride(Mesh.ArrayFormat format, int vertexCount) => RequireService().MeshSurfaceGetFormatVertexStrideCore(format, vertexCount);

    /// <summary>Gets the packed color/UV stride for a supported format.</summary>
    /// <param name="format">Surface format mask.</param>
    /// <param name="vertexCount">Nonnegative surface vertex count.</param>
    /// <returns>Four bytes for color plus eight bytes for primary UV when present.</returns>
    /// <exception cref="NotSupportedException">The format requires unsupported channels or non-2D vertices.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int MeshSurfaceGetFormatAttributeStride(Mesh.ArrayFormat format, int vertexCount) => RequireService().MeshSurfaceGetFormatAttributeStrideCore(format, vertexCount);

    /// <summary>Gets a live surface's channel and policy mask.</summary>
    /// <param name="mesh">Live borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>The supported surface format.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Mesh.ArrayFormat MeshSurfaceGetFormat(RID mesh, int surface) => RequireService().MeshSurfaceGetFormatCore(mesh, surface);

    /// <summary>Gets one surface's primitive topology.</summary>
    /// <param name="mesh">Live borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>The topology.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Mesh.PrimitiveType MeshSurfaceGetPrimitiveType(RID mesh, int surface) => RequireService().MeshSurfaceGetPrimitiveTypeCore(mesh, surface);

    /// <summary>Gets the surface vertex count.</summary>
    /// <param name="mesh">Live borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>The vertex count.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int MeshSurfaceGetArrayLen(RID mesh, int surface) => RequireService().MeshSurfaceGetArrayLenCore(mesh, surface);

    /// <summary>Gets the explicit surface index count.</summary>
    /// <param name="mesh">Live borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>Zero for sequential vertices.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int MeshSurfaceGetArrayIndexLen(RID mesh, int surface) => RequireService().MeshSurfaceGetArrayIndexLenCore(mesh, surface);

    /// <summary>Assigns a borrowed surface material on an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <param name="material">Borrowed live material or null.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MeshSurfaceSetMaterial(RID mesh, int surface, Material? material) => RequireService().MeshSurfaceSetMaterialCore(mesh, surface, material);

    /// <summary>Gets a borrowed material from a live surface.</summary>
    /// <param name="mesh">Live borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>The borrowed material or null.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Material? MeshSurfaceGetMaterial(RID mesh, int surface) => RequireService().MeshSurfaceGetMaterialCore(mesh, surface);

    /// <summary>Creates owned empty two-dimensional instance storage.</summary>
    /// <returns>A logical identity valid until FreeRID or renderer teardown.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static RID MultiMeshCreate() => RequireService().MultiMeshCreateCore();

    /// <summary>Allocates copied packed instance records for an owned resource.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="instances">Nonnegative capacity.</param>
    /// <param name="useColors">Include four-float color multipliers.</param>
    /// <param name="useCustomData">Include four-float shader data.</param>
    /// <param name="useIndirect">False for retained canvas storage; true requires a future writable GPU command-buffer backend.</param>
    /// <exception cref="NotSupportedException">Indirect GPU instance commands are requested.</exception>
    /// <remarks>Transforms are always two-dimensional. Equal capacity and flags preserve storage.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshAllocateData(RID multiMesh, int instances, bool useColors = false, bool useCustomData = false, bool useIndirect = false) => RequireService().MultiMeshAllocateDataCore(multiMesh, instances, useColors, useCustomData, useIndirect);

    /// <summary>Gets the two-dimensional local visibility rectangle of a live instance resource.</summary>
    /// <param name="multiMesh">Live owned or borrowed instance identity.</param>
    /// <returns>The manual or computed rectangle of its visible prefix.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Rect2 MultiMeshGetAABB(RID multiMesh) => RequireService().MultiMeshGetAABBCore(multiMesh);

    /// <summary>Gets the authored manual local visibility rectangle.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <returns>The stored rectangle; zero selects computed bounds.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Rect2 MultiMeshGetCustomAABB(RID multiMesh) => RequireService().MultiMeshGetCustomAABBCore(multiMesh);

    /// <summary>Sets the manual visibility rectangle of owned instance storage.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="aabb">Finite nonnegative two-dimensional local rectangle.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshSetCustomAABB(RID multiMesh, Rect2 aabb) => RequireService().MultiMeshSetCustomAABBCore(multiMesh, aabb);

    /// <summary>Returns the allocated instance count.</summary>
    /// <param name="multiMesh">Live owned or borrowed instance identity.</param>
    /// <returns>The allocated capacity.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int MultiMeshGetInstanceCount(RID multiMesh) => RequireService().MultiMeshGetInstanceCountCore(multiMesh);

    /// <summary>Gets the stored visible-prefix policy.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <returns>Minus one for all instances, or the visible prefix count.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int MultiMeshGetVisibleInstances(RID multiMesh) => RequireService().MultiMeshGetVisibleInstancesCore(multiMesh);

    /// <summary>Changes the visible prefix of an owned resource without reallocating.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="visible">Minus one or a count through capacity.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshSetVisibleInstances(RID multiMesh, int visible) => RequireService().MultiMeshSetVisibleInstancesCore(multiMesh, visible);

    /// <summary>Returns copied packed instance records.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <returns>Caller-owned eight-float transforms and optional channels.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static float[] MultiMeshGetBuffer(RID multiMesh) => RequireService().MultiMeshGetBufferCore(multiMesh);

    /// <summary>Copies whole finite packed records into owned storage.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="buffer">Whole packed buffer matching capacity and flags.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshSetBuffer(RID multiMesh, ReadOnlySpan<float> buffer) => RequireService().MultiMeshSetBufferCore(multiMesh, buffer);

    /// <summary>Copies explicit current and previous packed presentation records.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="bufferCurrent">Whole current buffer.</param>
    /// <param name="bufferPrevious">Whole previous buffer.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshSetBufferInterpolated(RID multiMesh, ReadOnlySpan<float> bufferCurrent, ReadOnlySpan<float> bufferPrevious) => RequireService().MultiMeshSetBufferInterpolatedCore(multiMesh, bufferCurrent, bufferPrevious);

    /// <summary>Assigns a borrowed mesh to owned instance storage.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="mesh">Live mesh RID or an empty identity to clear it.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshSetMesh(RID multiMesh, RID mesh) => RequireService().MultiMeshSetMeshCore(multiMesh, mesh);

    /// <summary>Gets the borrowed mesh identity used by an instance resource.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <returns>The live mesh RID or an empty identity.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static RID MultiMeshGetMesh(RID multiMesh) => RequireService().MultiMeshGetMeshCore(multiMesh);

    /// <summary>Sets one owned instance's finite local transform.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <param name="transform">Finite two-dimensional transform.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshInstanceSetTransform2D(RID multiMesh, int index, Transform transform) => RequireService().MultiMeshInstanceSetTransform2DCore(multiMesh, index, transform);

    /// <summary>Gets one instance's current local transform.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <returns>The logical current transform.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Transform MultiMeshInstanceGetTransform2D(RID multiMesh, int index) => RequireService().MultiMeshInstanceGetTransform2DCore(multiMesh, index);

    /// <summary>Sets one owned instance color multiplier.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <param name="color">Finite four-component multiplier.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshInstanceSetColor(RID multiMesh, int index, Color color) => RequireService().MultiMeshInstanceSetColorCore(multiMesh, index, color);

    /// <summary>Gets one stored instance color multiplier.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <returns>The raw current color.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Color MultiMeshInstanceGetColor(RID multiMesh, int index) => RequireService().MultiMeshInstanceGetColorCore(multiMesh, index);

    /// <summary>Sets one owned instance's raw shader components.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <param name="customData">Finite four-component value.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshInstanceSetCustomData(RID multiMesh, int index, Color customData) => RequireService().MultiMeshInstanceSetCustomDataCore(multiMesh, index, customData);

    /// <summary>Gets one stored four-component shader value.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <returns>The raw current shader data.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Color MultiMeshInstanceGetCustomData(RID multiMesh, int index) => RequireService().MultiMeshInstanceGetCustomDataCore(multiMesh, index);

    /// <summary>Resets one owned instance's previous presentation record.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshInstanceResetPhysicsInterpolation(RID multiMesh, int index) => RequireService().MultiMeshInstanceResetPhysicsInterpolationCore(multiMesh, index);

    /// <summary>Resets all previous presentation records in owned storage.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshInstancesResetPhysicsInterpolation(RID multiMesh) => RequireService().MultiMeshInstancesResetPhysicsInterpolationCore(multiMesh);

    /// <summary>Enables or disables presentation interpolation of owned packed records.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="interpolated">Whether scene physics snapshots contribute to rendering.</param>
    /// <remarks>Changing the policy resets previous records to current values without changing logical data.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshSetPhysicsInterpolated(RID multiMesh, bool interpolated) => RequireService().MultiMeshSetPhysicsInterpolatedCore(multiMesh, interpolated);

    /// <summary>Changes the basis interpolation quality of owned storage.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="quality">Fast component or High angular interpolation.</param>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MultiMeshSetPhysicsInterpolationQuality(RID multiMesh, MultiMesh.PhysicsInterpolationQuality quality) => RequireService().MultiMeshSetPhysicsInterpolationQualityCore(multiMesh, quality);

    /// <summary>Returns the borrowed live texture identity associated with a scene viewport.</summary>
    /// <param name="viewport">A live identity returned by Viewport.GetViewportRID.</param>
    /// <returns>The stable resource texture RID.</returns>
    /// <exception cref="ArgumentException">The identity does not resolve to a live viewport.</exception>
    /// <exception cref="InvalidOperationException">The call is off the renderer owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static RID ViewportGetTexture(RID viewport) => RequireService().ViewportGetTextureCore(viewport);

    /// <summary>Creates a caller-owned two-dimensional rendering texture from copied image pixels.</summary>
    /// <param name="image">A live nonempty image in a supported sampling format.</param>
    /// <returns>A stable texture RID valid until FreeRID or renderer shutdown.</returns>
    /// <exception cref="ArgumentNullException">The image is null.</exception>
    /// <exception cref="ObjectDisposedException">The image or renderer is disposed.</exception>
    /// <exception cref="ArgumentException">The image is empty or exceeds supported texture dimensions.</exception>
    /// <exception cref="NotSupportedException">The image's sampling format is not integrated.</exception>
    /// <exception cref="InvalidOperationException">The renderer is off-owner, submitting geometry or shutting down.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static RID Texture2DCreate(Image image) => RequireService().Texture2DCreateCore(image);

    /// <summary>Creates an owned four-by-four RGBA8 magenta/black checkerboard texture.</summary>
    /// <returns>A texture RID that can be drawn, replaced and freed like an ordinary two-dimensional texture.</returns>
    /// <exception cref="InvalidOperationException">The renderer is off-owner, submitting geometry or shutting down.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static RID Texture2DPlaceholderCreate() => RequireService().Texture2DPlaceholderCreateCore();

    /// <summary>Creates an owned alias of an existing rendering texture.</summary>
    /// <param name="baseTexture">A live resource-owned or server-owned texture RID, including another proxy.</param>
    /// <returns>A stable owned proxy RID; its source pixels remain borrowed.</returns>
    /// <remarks>Retained commands sample the current source without copying pixels or native texture storage.
    /// Freeing the source or an intermediate proxy leaves this identity alive but with no image/draw output.
    /// Freeing the proxy never frees its source. Empty source resources use their rendering placeholder.</remarks>
    /// <exception cref="ArgumentException">The source RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The renderer is off-owner, submitting or shutting down.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static RID TextureProxyCreate(RID baseTexture) => RequireService().TextureProxyCreateCore(baseTexture);

    /// <summary>Retargets an owned proxy to a live non-proxy texture without changing its identity.</summary>
    /// <param name="texture">A live proxy RID owned by this renderer.</param>
    /// <param name="proxyTo">A live non-proxy resource-owned or server-owned source RID.</param>
    /// <remarks>Validation precedes publication. Current pixels, logical size, format and diagnostic path follow
    /// the new source; existing retained destination geometry stays fixed. Cyclic/proxy targets are rejected.</remarks>
    /// <exception cref="ArgumentException">A RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The destination is not an owned proxy, the source is a proxy,
    /// or the renderer is off-owner, submitting or shutting down.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void TextureProxyUpdate(RID texture, RID proxyTo) => RequireService().TextureProxyUpdateCore(texture, proxyTo);

    /// <summary>Transfers an owned texture's pixels and metadata into another stable owned identity.</summary>
    /// <param name="texture">The destination RID whose identity and retained draw references survive.</param>
    /// <param name="byTexture">The replacement RID, consumed after the transfer.</param>
    /// <remarks>Both RIDs must belong to this renderer. Replacing a texture with itself does nothing.
    /// Different dimensions, formats and mipmap state are allowed; old recorded geometry remains fixed.</remarks>
    /// <exception cref="ArgumentException">A RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">A RID is resource-owned or the renderer is off-owner/submitting.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void TextureReplace(RID texture, RID byTexture) => RequireService().TextureReplaceCore(texture, byTexture);

    /// <summary>Returns an independent image copy of a live rendering texture.</summary>
    /// <param name="texture">A caller-owned or resource-owned texture RID.</param>
    /// <returns>A caller-owned image, including checkerboard pixels for an uninitialized resource texture;
    /// null for a live proxy whose source has been released.</returns>
    /// <remarks>Uses original backing pixels, including the full source of an atlas view. Does not stall the GPU.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The renderer is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Image? Texture2DGet(RID texture) => RequireService().Texture2DGetCore(texture);

    /// <summary>Updates an owned two-dimensional texture while preserving its allocation parameters.</summary>
    /// <param name="texture">A live texture RID owned by this rendering server.</param>
    /// <param name="image">A live image with matching dimensions, format and mipmap state.</param>
    /// <param name="layer">Zero for the current two-dimensional texture profile.</param>
    /// <remarks>Input is copied before commit; subsequent retained draws see the new pixels.</remarks>
    /// <exception cref="ArgumentException">The RID or image configuration is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The layer is not zero.</exception>
    /// <exception cref="NotSupportedException">The image requires an unsupported format.</exception>
    /// <exception cref="ArgumentNullException">The image is null.</exception>
    /// <exception cref="ObjectDisposedException">The image or renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The RID is resource-owned, or the renderer is off-owner/submitting.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void Texture2DUpdate(RID texture, Image image, int layer = 0) => RequireService().Texture2DUpdateCore(texture, image, layer);

    /// <summary>Returns a texture's source pixel format.</summary>
    /// <param name="texture">A live caller-owned or resource-owned texture RID.</param>
    /// <returns>The backing format, or RGBA8 for an uninitialized resource's rendering placeholder.</returns>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The renderer is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Image.Format TextureGetFormat(RID texture) => RequireService().TextureGetFormatCore(texture);

    /// <summary>Changes an owned texture's logical drawing size without resampling pixels.</summary>
    /// <param name="texture">A live server-owned texture RID.</param>
    /// <param name="width">Logical width from one through 16384 pixels.</param>
    /// <param name="height">Logical height from one through 16384 pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is outside the supported range.</exception>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The texture is resource-owned or the renderer is off-owner/submitting.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void TextureSetSizeOverride(RID texture, int width, int height) => RequireService().TextureSetSizeOverrideCore(texture, width, height);

    /// <summary>Sets diagnostic path metadata for an owned rendering texture.</summary>
    /// <param name="texture">A live server-owned texture RID.</param>
    /// <param name="path">Diagnostic path; it does not load an image.</param>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The texture is resource-owned or the renderer is off-owner/submitting.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void TextureSetPath(RID texture, string path) => RequireService().TextureSetPathCore(texture, path);

    /// <summary>Returns texture path metadata.</summary>
    /// <param name="texture">A live owned or resource-owned texture RID.</param>
    /// <returns>Owned diagnostic path, or the resource's existing ResourcePath.</returns>
    /// <exception cref="ArgumentException">The RID is stale or not a texture.</exception>
    /// <exception cref="InvalidOperationException">The renderer is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static string TextureGetPath(RID texture) => RequireService().TextureGetPathCore(texture);

    /// <summary>Releases a caller-owned rendering texture, mesh, instance, palette, canvas or canvas-item identity.</summary>
    /// <param name="rid">A live supported RID owned by this renderer.</param>
    /// <remarks>Resource-owned identities must be released by their resource. Retained commands stop drawing freed
    /// server textures; drawing a disposed mesh reports the borrowed-resource lifetime error. Freed palettes
    /// restore unskinned geometry. Freeing a canvas/item disconnects its render children without freeing their identities; they can be reparented. Borrowed scene canvas/skeleton identities remain scene-owned. Active palette replay forbids free.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or not a supported rendering resource.</exception>
    /// <exception cref="InvalidOperationException">The owner is different or the renderer is off-owner/submitting.</exception>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void FreeRID(RID rid) => RequireService().FreeRIDCore(rid);

    /// <summary>Gets or sets whether Engine.Run submits canvas frames.</summary>
    /// <value>True by default. Disabling retains existing commands and pending redraw requests.</value>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static bool RenderLoopEnabled
    {
        get => RequireService().RenderLoopEnabledCore;
        set => RequireService().RenderLoopEnabledCore = value;
    }

    /// <summary>Returns the active rendering method after startup and fallback selection.</summary>
    /// <returns>gpu or compatibility.</returns>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static string GetCurrentRenderingMethod() => RequireService().GetCurrentRenderingMethodCore();

    /// <summary>Returns the native driver selected by the active rendering backend.</summary>
    /// <returns>The SDL GPU or SDL_Renderer driver name.</returns>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static string GetCurrentRenderingDriverName() => RequireService().GetCurrentRenderingDriverNameCore();

    /// <summary>Gets the color used to clear the root framebuffer.</summary>
    /// <returns>The current clear color, initialized from ProjectSettings.DefaultClearColor.</returns>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Color GetDefaultClearColor() => RequireService().GetDefaultClearColorCore();

    /// <summary>Changes the root framebuffer clear color for subsequent frames.</summary>
    /// <param name="color">A finite color; normalized framebuffer channels clamp to zero through one.</param>
    /// <exception cref="ArgumentException">The color is not finite.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void SetDefaultClearColor(Color color) => RequireService().SetDefaultClearColorCore(color);

    /// <summary>Occurs before the scene's canvas commands are prepared for a frame.</summary>
    /// <remarks>Runs after animated textures advance and emit frame-change notifications, synchronously on the
    /// owner thread within the scene execution barrier. A failing subscriber
    /// aborts this frame and Engine.Run cleans up before propagating the error.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action? FramePreDraw
    {
        add => RequireService().FramePreDrawCore += value;
        remove => RequireService().FramePreDrawCore -= value;
    }

    /// <summary>Occurs after the canvas frame is submitted to the active backend.</summary>
    /// <remarks>Submission does not imply the GPU has completed or the compositor has displayed the frame.
    /// Runs synchronously on the owner thread. Frame or event-pump re-entry is rejected.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action? FramePostDraw
    {
        add => RequireService().FramePostDrawCore += value;
        remove => RequireService().FramePostDrawCore -= value;
    }

}
