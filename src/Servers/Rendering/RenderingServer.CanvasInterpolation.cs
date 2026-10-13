namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Selects physics-tick presentation interpolation for a retained canvas item.</summary>
    /// <param name="item">A live scene or caller-owned item.</param><param name="interpolated">Whether to interpolate while its scene enables physics interpolation.</param>
    /// <remarks>Caller-owned items default to enabled; scene items follow their authored node policy until overridden.
    /// History is renderer-session state and does not change authored getters or PackedScene data. A changed authored
    /// interpolation policy republishes affected scene items. Transform writes update the current endpoint; reset
    /// after a discontinuous move to remove its presentation trail.</remarks>
    /// <exception cref="ArgumentException">The identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or owner/capture/lifetime rules reject mutation.</exception>
    public static void CanvasItemSetInterpolated(RID item, bool interpolated) => RequireService().Change(item, out _).Interpolated = interpolated;
    /// <summary>Replaces an item's previous presentation endpoint with its current retained transform.</summary>
    /// <param name="item">A live scene or caller-owned item.</param>
    /// <remarks>The reset applies only to this item; independently interpolated descendants retain their own histories.</remarks>
    /// <exception cref="ArgumentException">The identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or owner/capture/lifetime rules reject mutation.</exception>
    public static void CanvasItemResetPhysicsInterpolation(RID item)
    { var state = RequireService().Change(item, out var node); state.Interpolation.Reset(state.Transform ?? node.GetVisualTransform()); if (state.Transform is null && state.Interpolated is null) node.ResetVisualInterpolationSnapshot(); }
    /// <summary>Left-multiplies both presentation endpoints and the current retained transform by a finite coordinate change.</summary>
    /// <param name="item">A live scene or caller-owned item.</param><param name="transform">The finite coordinate change, including singular transforms.</param>
    /// <remarks>Supports shifting origins without creating a new interpolation trail. Authored scene transforms remain unchanged.
    /// Nonfinite inputs or products reject before committing any endpoint or retained transform.</remarks>
    /// <exception cref="ArgumentException">The identity, transform or a resulting matrix is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or owner/capture/lifetime rules reject mutation.</exception>
    public static void CanvasItemTransformPhysicsInterpolation(RID item, Transform transform)
    { if (!transform.IsFinite()) throw new ArgumentException("Interpolation transforms must be finite.", nameof(transform)); var state = RequireService().Change(item, out var node); state.Transform = state.Interpolation.Rebase(transform, state.Transform ?? node.GetVisualTransform()); }

    internal void CanvasItemPhysicsTick(SceneTree tree, bool start)
    {
        if (!ReferenceEquals(CanvasSceneTree, tree)) return;
        List<Exception>? errors = null;
        foreach (var item in _ownedCanvasItems) Capture(item);
        foreach (var weak in _serverSceneItems) if (weak.TryGetTarget(out var item) && ReferenceEquals(item.Tree, tree)) Capture(item);
        if (errors is not null) throw new AggregateException("Canvas interpolation capture failed.", errors);
        void Capture(CanvasItem item)
        {
            if (item.IsDisposed || item.ServerState is not { } state) return;
            try { state.Interpolation.Tick(state.Transform ?? item.GetVisualTransform(), start); }
            catch (Exception error) { (errors ??= []).Add(error); }
        }
    }
    internal void ResetCanvasItemInterpolation(SceneTree tree)
    {
        if (!ReferenceEquals(CanvasSceneTree, tree)) return;
        List<Exception>? errors = null;
        foreach (var item in _ownedCanvasItems) Reset(item);
        foreach (var weak in _serverSceneItems) if (weak.TryGetTarget(out var item) && ReferenceEquals(item.Tree, tree)) Reset(item);
        if (errors is not null) throw new AggregateException("Canvas interpolation reset failed.", errors);
        void Reset(CanvasItem item)
        {
            if (item.IsDisposed || item.ServerState is not { } state) return;
            try { state.Interpolation.Reset(state.Transform ?? item.GetVisualTransform()); }
            catch (Exception error) { (errors ??= []).Add(error); }
        }
    }
}
