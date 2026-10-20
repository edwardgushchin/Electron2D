using Electron2D;
using static TextureArrayTests;

internal static partial class TextureRedrawTests
{
    private sealed class GeometryPatch(ArrayMesh mesh, MultiMesh instances, RID first, RID flagged) : Entity
    {
        internal int Mode;
        internal bool Offscreen = true;
        protected override void OnDraw()
        {
            if (Mode == 1) DrawMesh(mesh.GetRID(), flagged);
            else if (Mode == 2) DrawMultiMesh(instances.GetRID(), flagged);
            else
            {
                DrawTextureRect(first, new(0, 0, 16, 16), false);
                DrawTextureRect(flagged, new(Offscreen ? 500 : 20, 0, 16, 16), false);
            }
        }
    }
    private static void Geometry(Window root, Patch patch, ImageTexture texture)
    {
        patch.Visible = false;
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.White));
        using var instances = new MultiMesh { Mesh = mesh, InstanceCount = 1 }; instances.SetInstanceTransform2D(0, Transform.Identity);
        var a = RenderingServer.TextureProxyCreate(texture.GetRID()); var b = RenderingServer.TextureProxyCreate(texture.GetRID()); var outer = RenderingServer.TextureProxyCreate(b);
        var node = new GeometryPatch(mesh, instances, a, outer) { Position = new(8, 8) }; root.AddChild(node);
        RenderingServer.TextureSetForceRedrawIfVisible(b, true); Settle(); Check(!RenderingServer.HasChanged(), "Batch merging preserves distinct proxy policies for offscreen geometry sharing one native image.");
        node.Offscreen = false; node.QueueRedraw(); Settle(); Check(RenderingServer.HasChanged(), "The distinct flagged proxy starts requests when moved onscreen.");
        for (var mode = 1; mode <= 2; mode++)
        {
            node.Mode = mode; node.QueueRedraw(); Settle(); Check(RenderingServer.HasChanged(), "Mesh and multimesh keep the logical proxy source after native image resolution.");
            node.Visible = false; Settle(); Check(!RenderingServer.HasChanged(), "Hidden mesh and multimesh bindings stop requests."); node.Visible = true;
        }
        RenderingServer.TextureSetForceRedrawIfVisible(b, false); root.RemoveChild(node); node.Dispose(); RenderingServer.FreeRID(outer); RenderingServer.FreeRID(b); RenderingServer.FreeRID(a); patch.Visible = true;
    }
}
