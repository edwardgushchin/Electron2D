using Electron2D;

internal static partial class DistanceFieldChecks
{
    private sealed partial class Scene
    {
        private void Geometry(Texture texture)
        {
            var mesh = Own(new ArrayMesh()); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData
            { Vertices = [new(0, 0), new(64, 0), new(64, 16), new(0, 16)], UVs = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)], Indices = [0, 1, 2, 0, 2, 3] });
            var rid = RenderingServer.CanvasItemCreate(); _items.Add(rid); RenderingServer.CanvasItemSetParent(rid, _canvas); RenderingServer.CanvasItemSetDefaultTextureFilter(rid, TextureFilter.Nearest);
            RenderingServer.CanvasItemSetDistanceFieldMode(rid, true); RenderingServer.CanvasItemAddMesh(rid, mesh.GetRID(), new Transform(0, new(8, 80)), Tint, texture.GetRID()); Probes(8, 80, 0, true, "mesh");
            var multi = Own(new MultiMesh { Mesh = mesh, InstanceCount = 2 }); multi.SetInstanceTransform2D(0, new Transform(0, new(0, 0))); multi.SetInstanceTransform2D(1, new Transform(0, new(80, 0)));
            var instances = RenderingServer.CanvasItemCreate(); _items.Add(instances); RenderingServer.CanvasItemSetParent(instances, _canvas); RenderingServer.CanvasItemSetDefaultTextureFilter(instances, TextureFilter.Nearest);
            RenderingServer.CanvasItemSetTransform(instances, new Transform(0, new(88, 80))); RenderingServer.CanvasItemSetModulate(instances, Tint); RenderingServer.CanvasItemSetDistanceFieldMode(instances, true);
            RenderingServer.CanvasItemAddMultiMesh(instances, multi.GetRID(), texture.GetRID()); Probes(88, 80, 0, true, "instances/0"); Probes(168, 80, 0, true, "instances/1");
            var gpu = Own(new MultiMesh { Mesh = mesh, InstanceCount = 1, CustomAABB = new(0, 0, 80, 16) });
            gpu.SetInstanceTransform2D(0, Transform.Identity);
            var buffer = RenderingServer.MultiMeshGetBufferRDRID(gpu.GetRID()); var device = RenderingServer.GetRenderingDevice()!;
            var data = gpu.Buffer; data[3] = 8; device.BufferUpdate(buffer, 0, checked((uint)data.Length * 4), System.Runtime.InteropServices.MemoryMarshal.AsBytes(data.AsSpan()));
            Check(gpu.GetInstanceTransform2D(0).Origin == Vector2.Zero, "GPU writes do not alter authored instance poses.");
            var gpuItem = _gpuItem = RenderingServer.CanvasItemCreate(); _items.Add(gpuItem); RenderingServer.CanvasItemSetParent(gpuItem, _canvas); RenderingServer.CanvasItemSetDefaultTextureFilter(gpuItem, TextureFilter.Nearest);
            RenderingServer.CanvasItemSetTransform(gpuItem, new Transform(0, new(160, 104))); RenderingServer.CanvasItemSetModulate(gpuItem, Tint); RenderingServer.CanvasItemSetDistanceFieldMode(gpuItem, true);
            RenderingServer.CanvasItemAddMultiMesh(gpuItem, gpu.GetRID(), texture.GetRID()); Probes(168, 104, 0, true, "GPU-authored-instance");
            var process = Own(new ParticleProcessMaterial { Gravity = Vector2.Zero });
            var particles = _particle = new GPUParticles
            {
                Texture = texture,
                ProcessMaterial = process,
                Amount = 1,
                Lifetime = 1000,
                Emitting = false,
                FixedFPS = 0,
                Interpolate = false,
                LocalCoords = true,
                Position = new(40, 112),
                Scale = new(16, 16),
                Modulate = Tint
            };
            _nodes.Add(particles); AddChild(particles); RenderingServer.CanvasItemSetDefaultTextureFilter(particles.GetCanvasItem(), TextureFilter.Nearest);
            RenderingServer.CanvasItemSetDistanceFieldMode(particles.GetCanvasItem(), true); particles.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, GPUParticles.EmitFlags.Position);
            Probes(8, 104, 0, true, "GPU-particle");
            var atlas = Own(new AtlasTexture { Atlas = texture, Region = new(0, 0, 4, 1) }); Add(atlas, 248, 80, true);
        }
    }
}
