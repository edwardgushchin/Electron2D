using Electron2D;
using static TextureArrayTests;

internal static partial class ProceduralMeshTests
{
    private static Vector2[] Generated(Mesh.PrimitiveType primitive) => primitive switch
    {
        Mesh.PrimitiveType.Points => [new(12.5f, 12.5f), new(24, 0), new(0, 24), new(24.5f, 24.5f)],
        Mesh.PrimitiveType.Lines or Mesh.PrimitiveType.LineStrip => [new(0, 12.5f), new(24, 12.5f), new(0, 24), new(24, 24)],
        _ => [Vector2.Zero, new(24, 0), new(0, 24), new(24, 24)]
    };
    private sealed class RIDDraw : Entity
    {
        internal RID Resource; internal Transform Command = Transform.Identity;
        protected override void OnDraw() { if (Resource.IsValid()) DrawMesh(Resource, transform: Command); }
    }
    private sealed class Box : Entity { protected override void OnDraw() => DrawRect(new(-4, -4, 40, 40), Colors.White); }
    internal static void RunNative(string profile)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, profile == "compatibility" ? "compatibility" : "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); ProjectSettings.Set(ProjectSettings.SnapVerticesToPixel, profile.Contains("snap"));
        using var shader = Program(profile.Contains("hlsl") ? "HLSL" : "GLSL", profile.Contains("modern"));
        using var mapPixels = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); mapPixels.Fill(Colors.Black); using var map = ImageTexture.CreateFromImage(mapPixels); shader.SetDefaultTextureParameter("position_map", map);
        using var controlShader = Shader.CreateFromSPIRV(Module("DeviceVertexIDGLSL.frag.spv")); using var controlMaterial = new ShaderMaterial { Shader = controlShader };
        using var window = new Window { Size = new(1152, 480), UseHDR2D = profile.Contains("hdr"), MSAA2D = profile.Contains("msaa") ? Viewport.MSAA.Samples4X : Viewport.MSAA.Disabled };
        var resources = new List<Resource>(); var materials = new List<ShaderMaterial>(); var comparisons = new List<(Vector2 Origin, Vector2 Delta, bool Area, string Name)>(); var samples = new List<(Vector2 Point, Color Color, string Name)>();
        ShaderMaterial Material(Mesh.PrimitiveType primitive)
        {
            var material = new ShaderMaterial { Shader = shader }; material.SetShaderParameter<Vector2>("generated", Generated(primitive).AsSpan()); material.SetShaderParameter("homogeneous_w", profile.Contains("homogeneous") ? 1f : 0f); materials.Add(material); return material;
        }
        ArrayMesh Control(ArrayMesh source, Mesh.PrimitiveType primitive)
        {
            var data = source.SurfaceGetArrays(0); var n = data.VertexCount == 0 ? data.Indices.Max() + 1 : data.VertexCount; var points = Generated(primitive); data.Vertices = Enumerable.Range(0, n).Select(i => points[i % 4]).ToArray(); data.VertexCount = 0;
            if (data.Custom0 is { } custom) data.Colors = Enumerable.Range(0, n).Select(i => new Color(1 - MeshCustomCodec.Read(custom.Format, custom.Data.AsSpan(i * MeshCustomCodec.Stride(custom.Format))).X, 1, 1, 1)).ToArray();
            var result = new ArrayMesh(); result.AddSurfaceFromArrays(primitive, data, lods: source.SurfaceGetLODs(0), flags: data.SkinSlots == 8 ? Mesh.ArrayFormat.Use8BoneWeights : 0); resources.Add(result); return result;
        }
        (MeshInstance Proc, MeshInstance Control) Pair(string name, Mesh.PrimitiveType primitive, ArrayMesh source, int column, int row, Transform? basis = null, Node? parent = null)
        {
            resources.Add(source); var origin = new Vector2(column * 192 + 40, row * 88 + 32); var delta = new Vector2(80, 0); var pose = basis ?? Transform.Identity; pose.Origin = origin;
            var node = new MeshInstance { Mesh = source, Material = Material(primitive), Transform = pose }; var reference = new MeshInstance { Mesh = Control(source, primitive), Material = controlMaterial, Transform = new(pose.X, pose.Y, origin + delta) };
            (parent ?? window).AddChild(node); (parent ?? window).AddChild(reference); comparisons.Add((origin, delta, primitive is Mesh.PrimitiveType.Triangles or Mesh.PrimitiveType.TriangleStrip, name));
            var sample = primitive == Mesh.PrimitiveType.Points ? new Vector2(12.5f, 12.5f) : primitive is Mesh.PrimitiveType.Lines or Mesh.PrimitiveType.LineStrip ? new Vector2(6, 12.5f) : new Vector2(8, 8);
            samples.Add((pose * sample, Colors.White, name)); return (node, reference);
        }
        var primitives = Enum.GetValues<Mesh.PrimitiveType>(); for (var i = 0; i < primitives.Length; i++) Pair(primitives[i].ToString(), primitives[i], CreateMesh(primitives[i]), i, 0);
        Pair("index only", Mesh.PrimitiveType.Triangles, CreateMesh(data: new() { Indices = [0, 1, 2] }), 5, 0);
        Pair("sequential", Mesh.PrimitiveType.Triangles, CreateMesh(data: new() { VertexCount = 6 }), 0, 1);
        Pair("32-bit source IDs", Mesh.PrimitiveType.Triangles, CreateMesh(data: new() { VertexCount = 65539, Indices = [65536, 65537, 65538] }), 1, 1);
        Pair("reflected affine", Mesh.PrimitiveType.Triangles, CreateMesh(), 2, 1, new(new(-.8f, .25f), new(.2f, 1.1f), Vector2.Zero));
        var packedSource = CreateMesh(); var packedData = packedSource.ExportPacked(0); packedData.AABB = packedSource.CustomAABB; packedSource.Dispose(); var packed = new ArrayMesh(); packed.AppendPacked(ArrayMesh.PreparePacked(packedData, 0));
        var group = new CanvasGroup { FitMargin = 0, ClearMargin = 0 }; window.AddChild(group); Pair("packed bounds in fitted group", Mesh.PrimitiveType.Triangles, packed, 3, 1, parent: group);
        var skinnedData = Data(); skinnedData.Bones = new int[32]; skinnedData.Weights = new float[32]; for (var i = 0; i < 4; i++) skinnedData.Weights[i * 8 + 7] = 1;
        var skin = Pair("eighth-slot affine skin", Mesh.PrimitiveType.Triangles, CreateMesh(data: skinnedData, flags: Empty | Mesh.ArrayFormat.Use8BoneWeights), 4, 1);
        var rigs = new List<(Skeleton Rig, MeshInstance Node)>(); var bones = new List<Bone>();
        foreach (var node in new[] { skin.Proc, skin.Control })
        { var rig = new Skeleton { Position = node.Position }; var bone = new Bone { Rest = Transform.Identity }; bone.SetAutocalculateLengthAndAngle(false); bone.SetLength(24); rig.AddChild(bone); window.AddChild(rig); rigs.Add((rig, node)); bones.Add(bone); bone.Transform = new(.2f, new(2, 1)); }
        // Background comparisons also detect a strip that accidentally bridges two expanded instances.
        for (var i = 0; i < primitives.Length; i++)
        {
            var primitive = primitives[i]; var source = CreateMesh(primitive); resources.Add(source); var control = Control(source, primitive); var origin = new Vector2(i * 192 + 40, 2 * 88 + 32); var delta = new Vector2(80, 0);
            var procInstances = new MultiMesh { Mesh = source, UseColors = true, InstanceCount = 2 }; var referenceInstances = new MultiMesh { Mesh = control, UseColors = true, InstanceCount = 2 }; resources.Add(procInstances); resources.Add(referenceInstances);
            for (var n = 0; n < 2; n++) { var transform = new Transform(0, new(n * 40, 0)); procInstances.SetInstanceTransform2D(n, transform); referenceInstances.SetInstanceTransform2D(n, transform); procInstances.SetInstanceColor(n, Colors.White); referenceInstances.SetInstanceColor(n, Colors.White); }
            var proc = new MultiMeshInstance { MultiMesh = procInstances, Material = Material(primitive), Position = origin }; var reference = new MultiMeshInstance { MultiMesh = referenceInstances, Material = controlMaterial, Position = origin + delta };
            // Force the strip fallback through a fitted group; other primitives retain hardware instances.
            if (primitive is Mesh.PrimitiveType.LineStrip or Mesh.PrimitiveType.TriangleStrip) { var fitted = new CanvasGroup { FitMargin = 0, ClearMargin = 0 }; fitted.AddChild(proc); fitted.AddChild(reference); window.AddChild(fitted); } else { window.AddChild(proc); window.AddChild(reference); }
            comparisons.Add((origin, delta, false, "instances " + primitive)); var point = primitive == Mesh.PrimitiveType.Points ? new Vector2(12.5f, 12.5f) : primitive is Mesh.PrimitiveType.Lines or Mesh.PrimitiveType.LineStrip ? new Vector2(6, 12.5f) : new Vector2(8, 8); samples.Add((origin + point, Colors.White, "instances " + primitive));
        }
        var owner = new RIDDraw { Position = new(40, 3 * 88 + 32), Material = Material(Mesh.PrimitiveType.Triangles), Command = new(new(.9f, .15f), new(-.2f, 1.1f), new(3, 2)) }; window.AddChild(owner);
        using var ownedSource = CreateMesh(); var controlOwned = Control(ownedSource, Mesh.PrimitiveType.Triangles); var ownedPose = owner.Transform * owner.Command; window.AddChild(new MeshInstance { Mesh = controlOwned, Material = controlMaterial, Transform = new(ownedPose.X, ownedPose.Y, ownedPose.Origin + new Vector2(80, 0)) }); comparisons.Add((owner.Position, new(80, 0), true, "owned RID and command transform")); samples.Add((ownedPose * new Vector2(8, 8), Colors.White, "owned RID"));
        var maskSource = CreateMesh(); resources.Add(maskSource); var maskControl = Control(maskSource, Mesh.PrimitiveType.Triangles); var maskOrigin = new Vector2(192 + 40, 3 * 88 + 32);
        var mask = new MeshInstance { Mesh = maskSource, Material = Material(Mesh.PrimitiveType.Triangles), Position = maskOrigin, ClipChildren = ClipChildrenMode.AndDraw }; mask.AddChild(new Box()); window.AddChild(mask);
        var referenceMask = new MeshInstance { Mesh = maskControl, Material = controlMaterial, Position = maskOrigin + new Vector2(80, 0), ClipChildren = ClipChildrenMode.AndDraw }; referenceMask.AddChild(new Box()); window.AddChild(referenceMask); comparisons.Add((maskOrigin, new(80, 0), true, "generated owner mask")); samples.Add((maskOrigin + new Vector2(8, 8), Colors.White, "generated mask"));
        var fittedSource = CreateMesh(); resources.Add(fittedSource); fittedSource.SurfaceSetMaterial(0, Material(Mesh.PrimitiveType.Triangles)); var fittedControl = Control(fittedSource, Mesh.PrimitiveType.Triangles); var fitOrigin = new Vector2(2 * 192 + 40, 3 * 88 + 32);
        var ownGroup = new MeshGroup { Resource = fittedSource, Position = fitOrigin, FitMargin = 0, ClearMargin = 0 }; ownGroup.AddChild(new Box()); window.AddChild(ownGroup); var referenceGroup = new MeshGroup { Resource = fittedControl, Position = fitOrigin + new Vector2(80, 0), FitMargin = 0, ClearMargin = 0 }; referenceGroup.AddChild(new Box()); window.AddChild(referenceGroup); comparisons.Add((fitOrigin, new(80, 0), true, "generated group owner")); samples.Add((fitOrigin + new Vector2(8, 8), Colors.White, "generated group"));
        var attributeData = Data(); attributeData.Custom0 = MeshCustomTests.Channel(MeshCustomFormat.RFloat, new(1, 0, 0, 1), 4); attributeData.UVs = [Vector2.Zero, Vector2.Right, Vector2.Down, Vector2.One]; var attributeSource = CreateMesh(data: attributeData); var attributes = Pair("non-position attributes", Mesh.PrimitiveType.Triangles, attributeSource, 3, 3); samples[^1] = (samples[^1].Point, Colors.Cyan, samples[^1].Name);
        var attributePatch = new byte[48]; for (var i = 0; i < 4; i++) System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(attributePatch.AsSpan(i * 12 + 8), .5f);
        if (profile.EndsWith("-lit", StringComparison.Ordinal)) { window.AddChild(new CanvasModulate { Color = new(.5f, .5f, .5f, 1) }); window.AddChild(new DirectionalLight { Energy = .5f }); }
        RID owned = default; var frame = 0; long before = 0, active = 0, idle = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); foreach (var (rig, node) in rigs) node.AttachSkeleton(rig.GetSkeleton());
            var description = ownedSource.ExportPacked(0); description.AABB = ownedSource.CustomAABB; owned = RenderingServer.MeshCreateFromSurfaces([description]); Check(RenderingServer.MeshGetSurface(owned, 0).VertexData.Length == 0, "Owned packed positions remain absent."); owner.Resource = owned; owner.QueueRedraw();
            var appended = RenderingServer.MeshCreate(); RenderingServer.MeshAddSurfaceFromArrays(appended, Mesh.PrimitiveType.Triangles, Data(), Empty); RenderingServer.MeshAddSurface(appended, description); Check(RenderingServer.MeshGetSurfaceCount(appended) == 2 && RenderingServer.MeshGetSurface(appended, 1).VertexCount == 4, "Typed and packed owned append retain independent invocation counts."); RenderingServer.FreeRID(appended);
            Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.MeshAddSurface(owned, description))).GetAwaiter().GetResult(); Reject<InvalidOperationException>(() => RenderingServer.MeshAddSurface(ownedSource.GetRID(), description));
            using (var custom = new GeneratedMesh { Manual = new(-2, -3, 40, 50) }) { Check(RenderingServer.MeshGetSurface(custom.GetRID(), 0).AABB == custom.Manual, "Custom procedural packed export retains authored bounds."); custom.Callback = custom.EmitChanged; Reject<InvalidOperationException>(() => RenderingServer.MeshGetSurface(custom.GetRID(), 0)); custom.Callback = null; Check(RenderingServer.MeshGetSurface(custom.GetRID(), 0).VertexCount == 4, "Custom procedural export recovers after a revision-changing bounds callback."); }
            window.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); if (frame is >= 16 and < 80) { foreach (var material in materials) material.SetShaderParameter("offset", new Vector2((frame & 1), 0)); foreach (var bone in bones) bone.Position = new(2 + (frame & 1), 1); } };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame is >= 16 and < 80) active += GC.GetAllocatedBytesForCurrentThread() - before; if (frame is >= 80 and < 144) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frame == 144) { Check(active == 0 && idle == 0, $"Procedural managed frame bytes: active={active}, idle={idle}."); RenderingServer.FreeRID(owned); window.Tree!.Quit(); }
                if (frame > 2) return; using var image = window.GetTexture().GetImage()!;
                foreach (var sample in samples)
                {
                    // Native point/line edge coverage can differ by one pixel from CPU-expanded quads after clip rounding.
                    var radius = sample.Name.EndsWith("Points", StringComparison.Ordinal) || sample.Name.EndsWith("Lines", StringComparison.Ordinal) || sample.Name.EndsWith("LineStrip", StringComparison.Ordinal) ? 1 : 0; var matched = false;
                    for (var y = -radius; y <= radius; y++) for (var x = -radius; x <= radius; x++) { var color = image.GetPixel((int)MathF.Floor(sample.Point.X) + x, (int)MathF.Floor(sample.Point.Y) + y); matched |= MathF.Abs(color.R - sample.Color.R) < .04f && MathF.Abs(color.G - sample.Color.G) < .04f && MathF.Abs(color.B - sample.Color.B) < .04f; }
                    Check(matched, $"Generated {sample.Name} sample near {sample.Point} ({profile}).");
                }
                foreach (var comparison in comparisons) Compare(image, comparison.Origin, comparison.Delta, comparison.Area, comparison.Name);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, $"procedural-{profile}-{frame}.png")); }
                if (frame == 1)
                {
                    var code = Module($"DeviceProcedural{(profile.Contains("hlsl") ? "HLSL" : "GLSL")}.vert.spv"); shader.SetSPIRV(profile.Contains("modern") ? MeshVertexIDTests.Modern(code) : code, Module("DeviceVertexIDGLSL.frag.spv"));
                    attributeSource.SurfaceUpdateAttributeRegion(0, 0, attributePatch); var colorPatch = new byte[16]; for (var i = 0; i < 4; i++) { colorPatch[i * 4] = profile.Contains("hdr") ? (byte)187 : (byte)127; colorPatch[i * 4 + 1] = colorPatch[i * 4 + 2] = colorPatch[i * 4 + 3] = 255; } ((ArrayMesh)attributes.Control.Mesh!).SurfaceUpdateAttributeRegion(0, 0, InterleaveControl(colorPatch)); samples[^1] = (samples[^1].Point, new(.5f, 1, 1, 1), samples[^1].Name);
                }
            };
        };
        try
        {
            if (profile == "compatibility") { Reject<NotSupportedException>(() => Engine.Run(window)); Console.WriteLine("Procedural shader drawing rejects compatibility."); return; }
            Check(Engine.Run(window) == 0 && frame == 144, "Procedural native execution completed.");
        }
        finally { window.Dispose(); foreach (var resource in resources) resource.Dispose(); foreach (var material in materials) material.Dispose(); }
        Console.WriteLine($"Procedural native {profile}: five topologies/count modes/affine skin/instances/strips/owned RID/packed groups/masks/group owner/reload; active={active}, idle={idle}.");
    }
    private static byte[] InterleaveControl(byte[] colors)
    {
        var bytes = new byte[64]; for (var i = 0; i < 4; i++) { colors.AsSpan(i * 4, 4).CopyTo(bytes.AsSpan(i * 16)); System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 16 + 12), 1); }
        return bytes;
    }
    private static void Compare(Image image, Vector2 origin, Vector2 delta, bool area, string name)
    {
        var interior = 0; var background = 0;
        var instanced = name.StartsWith("instances ", StringComparison.Ordinal);
        for (var y = -26; y < 34; y++) for (var x = instanced ? -4 : -26; x < (instanced ? 68 : 35); x++)
            {
                var px = (int)origin.X + x; var py = (int)origin.Y + y; if (px < 1 || py < 1) continue;
                var rx = px + (int)delta.X; var ry = py + (int)delta.Y; var expected = image.GetPixel(rx, ry); var same = true;
                for (var dy = -1; dy <= 1 && same; dy++) for (var dx = -1; dx <= 1; dx++) if (MathF.Abs(image.GetPixel(rx + dx, ry + dy).R - expected.R) > .03f) { same = false; break; }
                var intensity = MathF.Max(expected.R, MathF.Max(expected.G, expected.B));
                if (!same || intensity is > .03f and < .9f) continue;
                var actual = image.GetPixel(px, py); Check(MathF.Abs(actual.R - expected.R) < .04f && MathF.Abs(actual.G - expected.G) < .04f && MathF.Abs(actual.B - expected.B) < .04f, $"Procedural/reference {name} at {x},{y}: {actual} versus {expected}."); if (intensity > .9f) interior++; else background++;
            }
        Check(background > 100 && (!area || interior > 80), $"Independent position geometry supplies usable {name} interior/background pixels.");
    }
    private sealed class MeshGroup : CanvasGroup
    {
        internal Mesh Resource = null!;
        protected override void OnDraw() => DrawMesh(Resource);
    }
}
