using System.Buffers.Binary;
using Electron2D;
using static TextureArrayTests;

internal static partial class MeshPackedTests
{
    private static MeshSurfaceBuffers NativePayload(int first = 0, bool changed = false, bool compressed = false, Color? color = null, bool skin = false, bool lod = false, int shapes = 0)
    {
        using var source = new ArrayMesh(); for (var i = 0; i < shapes; i++) source.AddBlendShape("Shape" + i);
        var data = MeshFrameTests.Data();
        for (var channel = 0; channel < 4; channel++) data.SetCustom(channel, MeshCustomTests.Channel((MeshCustomFormat)(first + channel), new(changed ? 0 : 1, 0, 0, 1)));
        if (color is { } tint) data.Colors = Enumerable.Repeat(tint, 4).ToArray();
        if (skin) { data.Bones = new int[32]; data.Weights = new float[32]; for (var i = 0; i < 4; i++) { data.Bones[i * 8 + 7] = 7; data.Weights[i * 8 + 7] = 1; } }
        var targets = Enumerable.Range(0, shapes).Select(_ => new MeshSurfaceData { Vertices = (Vector2[])data.Vertices.Clone(), Normals = (Vector3[])data.Normals.Clone(), Tangents = (Vector4[])data.Tangents.Clone() }).ToArray();
        source.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data, targets, lod ? new Dictionary<float, int[]> { [.5f] = [0, 1, 2] } : null, (compressed ? Mesh.ArrayFormat.CompressAttributes : 0) | (skin ? Mesh.ArrayFormat.Use8BoneWeights : 0));
        return source.ExportPacked(0);
    }
    internal static void Native(string profile)
    {
        var backend = profile == "hlsl" ? "gpu" : profile; var language = profile == "hlsl" ? "HLSL" : "GLSL";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var shader = backend == "gpu" ? PackedShader(language) : null; using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        using var window = new Window { Size = new(192, 128) };
        var first = new PackedDraw { Position = new(8, 8) }; var second = new PackedDraw { Position = new(40, 8) }; var compressed = new PackedDraw { Position = new(72, 8) }; var levels = new PackedDraw { Position = new(8, 48) }; var skinned = new PackedDraw { Position = new(96, 48) };
        foreach (var node in new[] { first, second, compressed, levels, skinned }) window.AddChild(node);
        var a = NativePayload(shapes: 2); var b = NativePayload(4, shapes: 2); a.Material = b.Material = material;
        var plain = NativePayload(compressed: true, color: Colors.Blue); var levelData = NativePayload(color: Colors.Yellow, lod: true); var skinData = NativePayload(compressed: true, color: Colors.Cyan, skin: true);
        var active = NativePayload(changed: true, shapes: 2).AttributeData; var idle = (byte[])a.AttributeData.Clone();
        var shifted = (byte[])a.VertexData.Clone(); for (var i = 0; i < 4; i++) BinaryPrimitives.WriteSingleLittleEndian(shifted.AsSpan(i * 8), BinaryPrimitives.ReadSingleLittleEndian(shifted.AsSpan(i * 8)) + 4);
        var triangle = new byte[12]; for (var i = 0; i < 6; i++) BinaryPrimitives.WriteUInt16LittleEndian(triangle.AsSpan(i * 2), (ushort)(i % 3));
        var rids = new RID[5]; RID palette = default; var frame = 0; long before = 0, activeBytes = 0, idleBytes = 0; var recorded = new int[3];
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Packed host selects the requested backend without fallback.");
            var service = RenderingServer.Service!; var owned = (List<RID>)typeof(RenderingServer).GetField("_ownedMeshRIDs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(service)!; var prior = owned.Count;
            Reject<ArgumentException>(() => RenderingServer.MeshCreateFromSurfaces([a, Minimal()], 2)); Check(owned.Count == prior, "An invalid later surface publishes no owned identity.");
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.MeshCreateFromSurfaces([], -1)); Reject<ArgumentOutOfRangeException>(() => RenderingServer.MeshCreateFromSurfaces([], 4097)); Reject<ArgumentNullException>(() => RenderingServer.MeshCreateFromSurfaces(null!));
            rids[0] = RenderingServer.MeshCreateFromSurfaces([], 2); Check(RenderingServer.MeshGetBlendShapeCount(rids[0]) == 2, "The explicit batch shape count is retained even before adding a surface.");
            RenderingServer.MeshAddSurface(rids[0], a); rids[1] = RenderingServer.MeshCreateFromSurfaces([b], 2); rids[2] = RenderingServer.MeshCreateFromSurfaces([plain]); rids[3] = RenderingServer.MeshCreateFromSurfaces([levelData]); rids[4] = RenderingServer.MeshCreateFromSurfaces([skinData]);
            Same(a, RenderingServer.MeshGetSurface(rids[0], 0)); Same(b, RenderingServer.MeshGetSurface(rids[1], 0));
            Check(RenderingServer.MeshSurfaceGetBlendShapeArrays(rids[0], 0).Length == 2, "Both packed shape blocks reach the existing typed query.");
            using var borrowed = new ArrayMesh(); borrowed.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshFrameTests.Data());
            Check(RenderingServer.MeshGetSurface(borrowed.GetRID(), 0).VertexCount == 4, "Borrowed resource identities export complete packed data.");
            Reject<InvalidOperationException>(() => RenderingServer.MeshAddSurface(borrowed.GetRID(), a)); Reject<ArgumentException>(() => RenderingServer.MeshAddSurface(rids[0], Minimal())); Check(RenderingServer.MeshGetSurfaceCount(rids[0]) == 1, "Failed append preserves count and geometry.");
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.MeshGetSurface(rids[0], 1));
            Task.Run(() => { Reject<InvalidOperationException>(() => RenderingServer.MeshGetSurface(rids[0], 0)); Reject<InvalidOperationException>(() => RenderingServer.MeshCreateFromSurfaces([])); Reject<InvalidOperationException>(() => RenderingServer.MeshAddSurface(rids[0], a)); }).GetAwaiter().GetResult();
            CustomPacked();
            first.MeshRID = rids[0]; second.MeshRID = rids[1]; compressed.MeshRID = rids[2]; levels.MeshRID = rids[3]; skinned.MeshRID = rids[4];
            palette = RenderingServer.SkeletonCreate(); RenderingServer.SkeletonAllocateData(palette, 8); RenderingServer.SkeletonBoneSetTransform2D(palette, 7, new(0, new(4, 0))); RenderingServer.CanvasItemAttachSkeleton(skinned.GetCanvasItem(), palette);
            var boundsProbe = new CanvasMesh(RenderingMeshRegistry.Resolve(rids[4]), Transform.Identity, Colors.White);
            skinned.Scale = new(-2, .5f); RenderingServer.SkeletonBoneSetTransform2D(palette, 7, new(0, new(8, 10)));
            Check(boundsProbe.GetDrawBounds(skinned) == new Rect2(-4, 20, 16, 16), "Bone bounds retain the mirrored nonuniform owner basis.");
            skinned.Transform = new Transform(Vector2.Zero, Vector2.Zero, new Vector2(96, 48)); Check(boundsProbe.GetDrawBounds(skinned) == skinData.AABB, "Singular owner bases use authored surface bounds.");
            skinned.Scale = Vector2.One; RenderingServer.SkeletonBoneSetTransform2D(palette, 7, new(0, new(4, 0)));
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frame == 2) RenderingServer.MeshSurfaceUpdateAttributeRegion(rids[0], 0, 0, active);
                if (frame == 4) RenderingServer.MeshSurfaceUpdateVertexRegion(rids[0], 0, 0, shifted);
                if (frame == 6) RenderingServer.MeshSurfaceUpdateIndexRegion(rids[0], 0, 0, triangle);
                if (frame == 8) RenderingServer.MeshSurfaceSetMaterial(rids[0], 0, null);
                if (frame == 10) RenderingServer.MeshSurfaceSetMaterial(rids[0], 0, material);
                if (frame == 12) levels.Scale = new(4, 4);
                if (frame == 14) RenderingServer.SkeletonBoneSetTransform2D(palette, 7, new(0, new(8, 0)));
                if (frame >= 24 && frame < 88)
                {
                    RenderingServer.MeshSurfaceUpdateAttributeRegion(rids[0], 0, 0, (frame & 1) == 0 ? active : idle);
                    RenderingServer.MeshSurfaceUpdateVertexRegion(rids[0], 0, 0, (frame & 1) == 0 ? shifted : a.VertexData);
                    RenderingServer.MeshSurfaceUpdateIndexRegion(rids[0], 0, 0, (frame & 1) == 0 ? triangle : a.IndexData);
                    RenderingServer.SkeletonBoneSetTransform2D(palette, 7, new(0, new((frame & 1) == 0 ? 4 : 8, 0)));
                }
            };
            RenderingServer.FramePostDraw += () =>
            {
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before; if (frame >= 24 && frame < 88) activeBytes += allocated; else if (frame >= 88) idleBytes += allocated;
                if (frame is 0 or 2 or 4 or 6 or 8 or 10 or 12 or 14)
                {
                    using var image = window.GetTexture().GetImage()!; var selected = backend == "compatibility" || frame == 8 ? Colors.White : frame == 0 ? Colors.Green : Colors.Magenta;
                    PackedPixel(image, frame < 4 ? 16 : 24, 12, selected); PackedPixel(image, 48, 12, backend == "compatibility" ? Colors.White : Colors.Green); PackedPixel(image, 80, 16, Colors.Blue);
                    if (frame >= 4) PackedPixel(image, 9, 16, Colors.Black);
                    if (frame >= 6) PackedPixel(image, 15, 21, Colors.Black);
                    if (frame < 12) PackedPixel(image, 10, 61, Colors.Black); else PackedPixel(image, 12, 100, Colors.Yellow);
                    if (frame < 14) PackedPixel(image, 102, 56, Colors.Cyan); else { PackedPixel(image, 101, 56, Colors.Black); PackedPixel(image, 110, 56, Colors.Cyan); }
                    var bounds = service.DebugCanvasItemGetRectCore(skinned.GetCanvasItem()); Check(bounds.Position.X == (frame < 14 ? 4 : 8) && bounds.Size == new Vector2(16, 16), "Packed bone rectangles drive actual palette-aware canvas bounds.");
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } captures) { Directory.CreateDirectory(captures); image.SavePNG(System.IO.Path.Combine(captures, $"packed-{profile}-{frame}.png")); }
                }
                if (frame == 16) { recorded[0] = first.Calls; recorded[1] = second.Calls; recorded[2] = skinned.Calls; }
                if (frame >= 24 && (first.Calls != recorded[0] || second.Calls != recorded[1] || skinned.Calls != recorded[2])) throw new InvalidOperationException($"Packed redraw callbacks at frame {frame}: {first.Calls}/{second.Calls}/{skinned.Calls}.");
                if (++frame == 152) window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && frame == 152 && activeBytes == 0 && idleBytes == 0, $"Packed {profile} 64 active/64 idle complete owner frames: {activeBytes}/{idleBytes} B.");
        foreach (var rid in rids) Check(!RenderingMeshRegistry.Contains(rid), "Renderer shutdown retires every packed mesh identity.");
        Check(!RenderingSkeletonRegistry.Contains(palette), "Renderer shutdown retires packed skin palette.");
        Console.WriteLine($"Packed {profile}: public batch/append/export, shape count, guards, custom recovery, actual channel/LOD/skin/material pixels and bounds; 64 active/64 idle complete owner frames 0 B passed.");
    }
    private static Shader PackedShader(string language)
    {
        using var stream = typeof(MeshPackedTests).Assembly.GetManifestResourceStream("Electron2D.Device.MeshCustom" + language + ".frag.spv")!; using var bytes = new MemoryStream(); stream.CopyTo(bytes); return Shader.CreateFromSPIRV(bytes.GetBuffer().AsSpan(0, (int)bytes.Length));
    }
    private static void CustomPacked()
    {
        using var custom = new PackedCustom(); var rid = custom.GetRID();
        custom.Callback = () => RenderingServer.MeshGetSurface(rid, 0); Reject<InvalidOperationException>(() => RenderingServer.MeshGetSurface(rid, 0));
        custom.Callback = custom.Touch; Reject<InvalidOperationException>(() => RenderingServer.MeshGetSurface(rid, 0)); custom.Callback = null;
        Check(RenderingServer.MeshGetSurface(rid, 0).VertexCount == 4, "Custom source retries after recursive/revision failure.");
        custom.Callback = custom.Dispose; Reject<ObjectDisposedException>(() => RenderingServer.MeshGetSurface(rid, 0)); Reject<ArgumentException>(() => RenderingServer.MeshGetSurface(rid, 0));
    }
    private static void PackedPixel(Image image, int x, int y, Color expected) { var actual = image.GetPixel(x, y); Check(Math.Abs(actual.R - expected.R) < .05f && Math.Abs(actual.G - expected.G) < .05f && Math.Abs(actual.B - expected.B) < .05f, $"Packed pixel {x}/{y}: {actual}, expected {expected}."); }
    private sealed class PackedDraw : Entity
    {
        internal RID MeshRID; internal int Calls;
        protected override void OnDraw() { Calls++; DrawMesh(MeshRID); }
    }
    private sealed class PackedCustom : Mesh
    {
        internal Action? Callback;
        internal void Touch() => EmitChanged();
        protected override int OnGetSurfaceCount() => 1;
        protected override int OnSurfaceGetArrayLen(int surfaceIndex) => 4;
        protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => 6;
        protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => null;
        protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material) => throw new NotSupportedException();
        protected override Mesh.PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => Mesh.PrimitiveType.Triangles;
        protected override Mesh.ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Use2DVertices | Mesh.ArrayFormat.Normal | Mesh.ArrayFormat.Tangent | Mesh.ArrayFormat.TexUV | Mesh.ArrayFormat.Index;
        protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) { Callback?.Invoke(); return MeshFrameTests.Data(); }
    }
}
