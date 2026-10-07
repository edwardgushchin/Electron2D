namespace Electron2D;

internal sealed class CanvasMultiMesh(MultiMesh resource)
{
    private CanvasMesh? _mesh;
    internal void Set(MultiMesh value) { resource = value; }
    internal void Clear() { resource = null!; _mesh?.Clear(); }
    internal Rect2 Bounds() => resource.GetAABB();
    internal void ResetInterpolation() { if (resource is { IsDisposed: false }) resource.ResetInstancesPhysicsInterpolation(); }
    internal void Append(List<CanvasVertex> vertices, List<CanvasBatch> batches, Texture? texture, Transform transform, Color color,
        MaterialState? material, BlendMode blend, TextureFilter filter, TextureRepeat repeat, int anisotropy, Rect2i? clip, bool snap, float fraction, Vector2i? outputSize, CanvasItem? skinOwner = null)
    {
        lock (resource.Gate)
        {
            ObjectDisposedException.ThrowIf(resource.IsDisposed, resource);
            var mesh = resource.Mesh; if (mesh is null || resource.DrawCount == 0) return;
            var revision = resource.ChangeRevision;
            var bounds = transform * resource.GetPresentationBounds(fraction);
            ObjectDisposedException.ThrowIf(resource.IsDisposed, resource);
            if (resource.ChangeRevision != revision) throw new InvalidOperationException("Mesh callbacks changed instance storage during bounds preparation.");
            if (!bounds.IsFinite() || !bounds.End.IsFinite()) throw new ArithmeticException("Instance canvas bounds exceeded finite output.");
            // ponytail: skinned instances bypass rest-bound culling; prepare deformed bounds if measured offscreen replay cost warrants it.
            var skinned = skinOwner is not null && RenderingSkeletonRegistry.Contains(skinOwner.AttachedSkeleton);
            // Framebuffer points/lines and pixel snapping can extend beyond transformed vertex centers.
            bounds = bounds.Grow(1);
            if (!skinned && clip is { } scissor && !bounds.Intersects(new Rect2(scissor.Position, scissor.Size), includeBorders: true)) return;
            if (!skinned && outputSize is { } size && !bounds.Intersects(new Rect2(Vector2.Zero, size), includeBorders: true)) return;
            _mesh ??= new(mesh, Transform.Identity, Colors.White); _mesh.Set(mesh, Transform.Identity, Colors.White);
            _mesh.Append(vertices, batches, texture, transform, color, material, blend, filter, repeat, anisotropy, clip, snap, resource, fraction, skinOwner);
            ObjectDisposedException.ThrowIf(resource.IsDisposed, resource);
            if (resource.ChangeRevision != revision) throw new InvalidOperationException("Mesh callbacks changed instance storage during replay.");
        }
    }
}
