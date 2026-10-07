namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>The default number of bone/weight slots per skinned vertex; Use8BoneWeights selects twice this count.</summary>
    public const int ArrayWeightsSize = 4;
    private readonly List<RID> _ownedSkeletonRIDs = [];
    /// <summary>Creates an empty caller-owned two-dimensional skin palette.</summary>
    /// <returns>A stable RID released by FreeRID or renderer shutdown.</returns>
    /// <exception cref="InvalidOperationException">The native service is unavailable or called off-owner/during submission.</exception>
    public static RID SkeletonCreate() => RequireService().SkeletonCreateCore();
    /// <summary>Replaces owned palette storage with zero transforms; equal capacity preserves values.</summary>
    /// <param name="skeleton">Caller-owned palette identity.</param>
    /// <param name="bones">Nonnegative transform count.</param>
    /// <remarks>All palettes are two-dimensional; no dimensional selector is exposed. Allocation commits after successful preparation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The bone count is negative.</exception>
    /// <exception cref="InvalidOperationException">Ownership or renderer/scene owner/submission rules reject mutation.</exception>
    public static void SkeletonAllocateData(RID skeleton, int bones) => RequireService().SkeletonAllocateDataCore(skeleton, bones);
    /// <summary>Returns the number of transforms in a live owned or scene palette.</summary>
    /// <param name="skeleton">Live palette identity.</param>
    /// <returns>Nonnegative count; scene palettes follow actual rig membership.</returns>
    /// <exception cref="ArgumentException">The identity is absent or disposed.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or read off-owner.</exception>
    public static int SkeletonGetBoneCount(RID skeleton) => RequireService().SkeletonGetBoneCountCore(skeleton);
    /// <summary>Sets a finite skin deformation transform in owned storage.</summary>
    /// <param name="skeleton">Caller-owned palette.</param>
    /// <param name="bone">Existing zero-based index.</param>
    /// <param name="transform">Finite transform, including singular or reflected bases.</param>
    /// <exception cref="ArgumentException">The palette is absent or the transform is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The bone index is absent.</exception>
    /// <exception cref="InvalidOperationException">Ownership or renderer owner/submission rules reject mutation.</exception>
    public static void SkeletonBoneSetTransform2D(RID skeleton, int bone, Transform transform) => RequireService().SkeletonBoneSetTransform2DCore(skeleton, bone, transform);
    /// <summary>Returns one stored deformation transform from owned or scene storage.</summary>
    /// <param name="skeleton">Live palette.</param>
    /// <param name="bone">Existing zero-based index.</param>
    /// <returns>Stored transform; scene palettes supply actual pose multiplied by inverse rest.</returns>
    /// <exception cref="ArgumentException">The identity is absent or disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or read off-owner.</exception>
    public static Transform SkeletonBoneGetTransform2D(RID skeleton, int bone) => RequireService().SkeletonBoneGetTransform2DCore(skeleton, bone);
    /// <summary>Sets the finite palette base transform in canvas coordinates.</summary>
    /// <param name="skeleton">Caller-owned palette.</param>
    /// <param name="baseTransform">Canvas basis; singular bases retain data but omit deformation.</param>
    /// <exception cref="ArgumentException">The identity is absent or the base is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Ownership or renderer owner/submission rules reject mutation.</exception>
    public static void SkeletonSetBaseTransform2D(RID skeleton, Transform baseTransform) => RequireService().SkeletonSetBaseTransform2DCore(skeleton, baseTransform);
    /// <summary>Attaches a borrowed palette to a live scene canvas item for retained mesh skin replay.</summary>
    /// <param name="item">Scene-owned CanvasItem.GetCanvasItem identity.</param>
    /// <param name="skeleton">Live owned/scene palette, or an empty RID to detach.</param>
    /// <remarks>The attachment is transient, does not own the palette and is not inherited by canvas children.
    /// Freed palettes leave the original unskinned geometry. Scene palettes must share viewport/canvas layer with their consumer.</remarks>
    /// <exception cref="ArgumentException">The item or nonempty palette identity is absent.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/submission rules reject mutation.</exception>
    public static void CanvasItemAttachSkeleton(RID item, RID skeleton) => RequireService().CanvasItemAttachSkeletonCore(item, skeleton);

    internal RID SkeletonCreateCore() { EnsureTextureChange(); var rid = RenderingSkeletonRegistry.RegisterOwned(this); _ownedSkeletonRIDs.Add(rid); return rid; }
    internal void SkeletonAllocateDataCore(RID skeleton, int bones) { EnsureTextureChange(); ArgumentOutOfRangeException.ThrowIfNegative(bones); var palette = RenderingSkeletonRegistry.Owned(skeleton, this); palette.EnsureWritable(); if (palette.Transforms.Length != bones) palette.Transforms = new Transform[bones]; }
    internal int SkeletonGetBoneCountCore(RID skeleton) { EnsureOwner(); return RenderingSkeletonRegistry.Read(skeleton).Length; }
    internal Transform SkeletonBoneGetTransform2DCore(RID skeleton, int bone) { EnsureOwner(); var data = RenderingSkeletonRegistry.Read(skeleton); if ((uint)bone >= (uint)data.Length) throw new ArgumentOutOfRangeException(nameof(bone)); return data[bone]; }
    internal void SkeletonBoneSetTransform2DCore(RID skeleton, int bone, Transform transform) { EnsureTextureChange(); if (!transform.IsFinite()) throw new ArgumentException("Palette transforms must be finite.", nameof(transform)); var palette = RenderingSkeletonRegistry.Owned(skeleton, this); palette.EnsureWritable(); if ((uint)bone >= (uint)palette.Transforms.Length) throw new ArgumentOutOfRangeException(nameof(bone)); palette.Transforms[bone] = transform; }
    internal void SkeletonSetBaseTransform2DCore(RID skeleton, Transform transform) { EnsureTextureChange(); if (!transform.IsFinite()) throw new ArgumentException("Palette bases must be finite.", nameof(transform)); var palette = RenderingSkeletonRegistry.Owned(skeleton, this); palette.EnsureWritable(); palette.Base = transform; }
    internal void CanvasItemAttachSkeletonCore(RID item, RID skeleton) { EnsureTextureChange(); var target = RenderingCanvasItemRegistry.Resolve(item); if (skeleton.IsValid()) RenderingSkeletonRegistry.Read(skeleton); target.AttachSkeleton(skeleton); }
    private void ReleaseOwnedSkeletons() { foreach (var rid in _ownedSkeletonRIDs) RenderingSkeletonRegistry.Remove(rid); _ownedSkeletonRIDs.Clear(); }
}
