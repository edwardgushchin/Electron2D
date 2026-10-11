using Electron2D;
using static TextureArrayTests;

internal static class InstanceUniformTests
{
    private sealed class Patch : Entity
    {
        internal int Recordings;
        protected override void OnDraw() { Recordings++; DrawRect(new(0, 0, 12, 12), Colors.White); }
    }
    private static void Values(CanvasItem item, Color tint)
    {
        item.SetInstanceShaderParameter<Color>("tint", tint);
        item.SetInstanceShaderParameter<bool>("enabled", true);
        item.SetInstanceShaderParameter<Vector2i>("channel", new(1, 1));
        item.SetInstanceShaderParameter<uint>("number", 2u);
    }
    internal static void Run()
    {
        Shapes();
        using (var buffer = new StreamPeerBuffer())
        {
            var data = new byte[1048577]; data[^1] = 37; var codec = ResourceFileTypes.Codec<byte[]>(); codec.Write(buffer, data); buffer.Seek(0);
            Check(codec.Read(buffer).SequenceEqual(data), "Portable bytecode buffers retain payloads over one MiB within the sixteen MiB shader budget.");
            buffer.DataArray = [16, 0, 0, 0]; Reject<System.IO.InvalidDataException>(() => codec.Read(buffer));
        }
        foreach (var fixture in new[] { "BooleansGLSL", "MatricesGLSL", "ValuesGLSL", "ArrayGLSL" })
        {
            using var shader = Load(fixture); using var material = new ShaderMaterial { Shader = shader }; using var item = new Entity { Material = material };
            item.SetInstanceShaderParameter<float>("authored", .25f); Archive(item);
        }
        foreach (var name in new[] { "ItemUniformsGLSL", "ItemUniformsHLSL" })
        {
            using var shader = Load(name); using var material = new ShaderMaterial { Shader = shader };
            material.SetShaderParameter("scale", 1f);
            Check(shader.GetShaderUniformList().Count == 1 && material.GetPropertyList().All(p => !p.Name.EndsWith("/tint", StringComparison.Ordinal)), "Instance fields stay outside shared material parameters.");
            Reject<ArgumentException>(() => material.SetShaderParameter("tint", Colors.Red));
            using var item = new Entity(); Values(item, Colors.Red); item.Material = material;
            Check(item.GetInstanceShaderParameter<Color>("tint") == Colors.Red && item.GetInstanceShaderParameter<bool>("enabled") == true, "Values authored before material assignment bind by name and logical type.");
            item.SetInstanceShaderParameter<Color>("tint", null);
            Check(item.GetInstanceShaderParameter<Vector4>("tint") == default(Vector4), "Null resets the current declaration to zero.");
            Check(item.GetInstanceShaderParameter<float>("TINT") is null, "Names are case sensitive.");
            Reject<ArgumentException>(() => item.SetInstanceShaderParameter<float>("number", 1));
            Reject<ArgumentException>(() => item.SetInstanceShaderParameter<float>("other", float.NaN));
            Reject<ArgumentException>(() => item.SetInstanceShaderParameter<Transform>("other", Transform.Identity));
            Reject<ArgumentException>(() => item.GetInstanceShaderParameter<float>(" "));
            Values(item, Colors.Blue);
            var originalCode = shader.GetSPIRV();
            using var reordered = Load("ItemUniformsReordered"); shader.SetSPIRV(reordered.GetSPIRV());
            Check(item.GetInstanceShaderParameter<Color>("tint") == Colors.Blue && item.GetInstanceShaderParameter<uint>("number") == 2u, "Reload migrates matching values across offsets and bindings.");
            Archive(item);
            shader.SetSPIRV(originalCode);
            Native(shader, material);
        }
        using var detached = new Entity();
        detached.SetInstanceShaderParameter<Vector3>("v3", new(1, 2, 3));
        detached.SetInstanceShaderParameter<Vector4i>("v4i", new(1, 2, 3, 4));
        detached.SetInstanceShaderParameter<Vector3i>("v3i", new(5, 6, 7));
        detached.SetInstanceShaderParameter<Rect2>("rect", new(1, 2, 3, 4));
        detached.SetInstanceShaderParameter<float>("scalar", .25f);
        Archive(detached);
        Console.WriteLine("Instance uniforms: HLSL/GLSL, scene/server/mesh, defaults, reload, archives and native independent pixels.");
    }
    private static void Shapes()
    {
        using var shader = Load("ItemUniformTypes"); using var material = new ShaderMaterial { Shader = shader }; using var item = new Entity { Material = material };
        var program = shader.GetProgram(); Check(program.InstanceUniforms.Count == 16, "Sixteen instance fields are accepted.");
        void Set<T>(string name, T value, T expected) where T : unmanaged
        {
            item.SetInstanceShaderParameter<T>(name, value); Check(EqualityComparer<T>.Default.Equals(item.GetInstanceShaderParameter<T>(name)!.Value, expected), "Typed scalar/vector read: " + name);
            var uniform = program.InstanceUniforms[name]; var buffers = item.InstanceUniforms.Prepare(program);
            Check(EqualityComparer<T>.Default.Equals(uniform.Read<T>(buffers[uniform.Buffer]!.AsSpan(uniform.Offset, uniform.ElementSize)), expected), "Native instance buffer layout: " + name);
        }
        Set("f", .25f, .25f); Set("v2", new Vector2(.5f, .75f), new Vector2(.5f, .75f));
        Set("v3", new Vector3(1, 2, 3), new Vector3(1, 2, 3)); Set("v4", new Rect2(1, 2, 3, 4), new Rect2(1, 2, 3, 4));
        Set("i", -3, -3); Set("u", uint.MaxValue, uint.MaxValue);
        Set("i2", new Vector2i(-1, 2), new Vector2i(-1, 2)); Set("i3", new Vector3i(-1, 2, 3), new Vector3i(-1, 2, 3)); Set("i4", new Vector4i(-1, 2, 3, 4), new Vector4i(-1, 2, 3, 4));
        Set("u2", new Vector2i(-1, 2), new Vector2i(-1, 2)); Set("u3", new Vector3i(-1, 2, 3), new Vector3i(-1, 2, 3)); Set("u4", new Vector4i(-1, 2, 3, 4), new Vector4i(-1, 2, 3, 4));
        Set("b", true, true); Set("b2", 7, 3); Set("b3", 5, 5); Set("b4", -2, 14);
        Reject<ArgumentException>(() => item.SetInstanceShaderParameter<Vector2i>("b2", new(1, 0)));
        Archive(item);
        var code = shader.GetSPIRV();
        foreach (var fixture in new[] { "ItemUniformArray", "ItemUniformMatrix", "ItemUniformReserved", "ItemUniformOverflow" })
        {
            using var input = typeof(InstanceUniformTests).Assembly.GetManifestResourceStream("TestShaders." + fixture + ".spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes);
            Reject<NotSupportedException>(() => shader.SetSPIRV(bytes.ToArray())); Check(shader.GetSPIRV().SequenceEqual(code), "Invalid instance shape preserves the current shader.");
        }
        var changedUniforms = new Dictionary<string, ShaderUniform>(program.InstanceUniforms, StringComparer.Ordinal) { ["u2"] = program.InstanceUniforms["u2"] with { Unsigned = false }, ["b2"] = program.InstanceUniforms["b2"] with { BooleanWidth = 3, ElementSize = 12 } };
        var changed = new ShaderProgram(code, program.BufferSizes, new(StringComparer.Ordinal), instanceUniforms: changedUniforms);
        Check(item.InstanceUniforms.Get<Vector2i>("u2", changed) == default(Vector2i) && item.InstanceUniforms.Get<int>("b2", changed) == 0, "Same carrier cannot migrate unsignedness or boolean width.");
    }
    private static object? Value(PropertyDescriptor property, Entity item) { return property.GetType().GetMethod("GetValue")!.Invoke(property, [item]); }
    private static void Archive(Entity item)
    {
        using var scene = new PackedScene(); scene.Pack(item);
        using (var clone = (Entity)scene.Instantiate())
            foreach (var property in item.GetPropertyList().Where(p => p.Name.StartsWith("instance_shader_parameters/", StringComparison.Ordinal)))
                Check(Equals(Value(property, item), Value(property, clone)), "PackedScene restores typed instance values.");
        foreach (var flags in new[] { SaverFlags.None, SaverFlags.Compress | SaverFlags.SaveBigEndian })
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-instances-" + Guid.NewGuid().ToString("N") + ".e2dscene");
            try
            {
                ResourceSaver.Save(scene, path, flags); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var copy = (Entity)loaded.Instantiate();
                if (item.Material is ShaderMaterial original && copy.Material is ShaderMaterial restored)
                {
                    Check(original.Shader!.GetSPIRV().SequenceEqual(restored.Shader!.GetSPIRV()), "File material graph restores compiled shader bytes.");
                    foreach (var property in original.GetPropertyList().Where(p => p.Name.StartsWith("shader_parameter/", StringComparison.Ordinal) && !typeof(Resource).IsAssignableFrom(p.ValueType)))
                        Check(System.Collections.StructuralComparisons.StructuralEqualityComparer.Equals(property.GetType().GetMethod("GetValue")!.Invoke(property, [original]), property.GetType().GetMethod("GetValue")!.Invoke(property, [restored])), "Shared numeric/array/matrix parameters restore after shader loading.");
                }
                foreach (var property in item.GetPropertyList().Where(p => p.Name.StartsWith("instance_shader_parameters/", StringComparison.Ordinal)))
                    Check(Equals(Value(property, item), Value(property, copy)), "File scene archives restore nullable scalar/vector values with or without a shader.");
            }
            finally { System.IO.File.Delete(path); }
        }
    }
    private static void Native(Shader shader, ShaderMaterial material)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Size = new(256, 128) };
        var light = new DirectionalLight { Enabled = false, Energy = .25f }; root.AddChild(light);
        var red = new Patch { Name = "Red", Material = material, Position = new(4, 4) }; Values(red, Colors.Red); root.AddChild(red);
        var blue = new Patch { Name = "Blue", Material = material, Position = new(20, 4) }; Values(blue, Colors.Blue); root.AddChild(blue);
        var parent = new Entity { Material = material, Position = new(36, 4) }; root.AddChild(parent);
        var inherited = new Patch { UseParentMaterial = true }; Values(inherited, Colors.Green); parent.AddChild(inherited);
        using var alternateShader = Load("ItemUniformsHLSL"); using var alternate = new ShaderMaterial { Shader = alternateShader }; alternate.SetShaderParameter("scale", 1f);
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.White)); mesh.SurfaceSetMaterial(0, material); var secondSurface = MeshTests.Quad(Colors.White); for (var i = 0; i < secondSurface.Vertices.Length; i++) secondSurface.Vertices[i] += new Vector2(0, 24); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, secondSurface); mesh.SurfaceSetMaterial(1, alternate);
        var meshItem = new MeshInstance { Mesh = mesh, Position = new(52, 4) }; Values(meshItem, Colors.Yellow); root.AddChild(meshItem);
        using var instances = new MultiMesh { Mesh = mesh, InstanceCount = 2 }; instances.SetInstanceTransform2D(0, new(0, new(0, 0))); instances.SetInstanceTransform2D(1, new(0, new(16, 0)));
        var many = new MultiMeshInstance { MultiMesh = instances, Position = new(84, 4) }; Values(many, Colors.Magenta); root.AddChild(many);
        var group = new CanvasGroup { Material = material, Position = new(156, 4) }; Values(group, Colors.Yellow); group.AddChild(new Patch()); root.AddChild(group);
        using var tileImage = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); tileImage.Fill(Colors.White); using var tileTexture = ImageTexture.CreateFromImage(tileImage);
        using var tileAtlas = new TileSetAtlasSource { Texture = tileTexture }; tileAtlas.CreateTile(default); tileAtlas.GetTileData(default, 0).Material = material;
        using var tileSet = new TileSet(); tileSet.AddSource(tileAtlas, 1);
        var tiles = new TileMapLayer { TileSet = tileSet, Position = new(196, 12), CollisionEnabled = false, NavigationEnabled = false }; Values(tiles, Colors.Cyan); tiles.SetCell(default, 1, default(Vector2i)); root.AddChild(tiles);
        RID canvas = default, serverItem = default; var frame = 0; long before = 0, allocated = 0;
        root.Ready += readyNode =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            canvas = RenderingServer.CanvasCreate(); RenderingServer.ViewportAttachCanvas(root.GetViewportRID(), canvas);
            serverItem = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(serverItem, canvas); RenderingServer.CanvasItemSetMaterial(serverItem, material.GetRID());
            RenderingServer.CanvasItemAddRect(serverItem, new(132, 4, 12, 12), Colors.White);
            RenderingServer.CanvasItemSetInstanceShaderParameter<Color>(serverItem, "tint", Colors.Cyan);
            RenderingServer.CanvasItemSetInstanceShaderParameter<bool>(serverItem, "enabled", true);
            RenderingServer.CanvasItemSetInstanceShaderParameter<Vector2i>(serverItem, "channel", new(1, 1));
            RenderingServer.CanvasItemSetInstanceShaderParameter<uint>(serverItem, "number", 2);
            Check(RenderingServer.CanvasItemGetInstanceShaderParameterList(meshItem.GetCanvasItem()).Count == 4 && RenderingServer.CanvasItemGetInstanceShaderParameterList(serverItem).Count == 4 && RenderingServer.CanvasItemGetInstanceShaderParameterList(serverItem).Any(p => p.Name == "enabled" && p.ValueType == typeof(bool?)), "Server descriptors preserve parameter names and logical booleans.");
            Check(RenderingServer.CanvasItemGetInstanceShaderParameterDefaultValue<Color>(serverItem, "tint") == new Color(0, 0, 0, 0) && RenderingServer.CanvasItemGetInstanceShaderParameter<Color>(serverItem, "tint") == Colors.Cyan, "Declared defaults stay independent of live overrides.");
            Check(RenderingServer.CanvasItemGetInstanceShaderParameterDefaultValue<float>(serverItem, "missing") is null, "Absent declared default is null.");
            Reject<ArgumentException>(() => RenderingServer.CanvasItemGetInstanceShaderParameter<float>(material.GetRID(), "scale"));
            Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.CanvasItemGetInstanceShaderParameter<Color>(serverItem, "tint"))).GetAwaiter().GetResult();
            root.Tree!.ProcessFrameStarted += tree => { if (frame >= 24) { before = GC.GetAllocatedBytesForCurrentThread(); red.SetInstanceShaderParameter<Color>("tint", Colors.Red); _ = red.GetInstanceShaderParameter<Color>("tint"); } };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame < 5)
                {
                    using var pixels = root.GetTexture().GetImage()!;
                    var scale = frame == 4 ? .3125f : 1f;
                    Equal(pixels.GetPixel(7, 7), (frame == 1 ? Colors.Black : frame >= 2 ? Colors.Green : Colors.Red) * scale);
                    Equal(pixels.GetPixel(23, 7), Colors.Blue * scale); Equal(pixels.GetPixel(39, 7), Colors.Green * scale);
                    Equal(pixels.GetPixel(57, 7), Colors.Yellow * scale); Equal(pixels.GetPixel(89, 7), Colors.Magenta * scale); Equal(pixels.GetPixel(105, 7), Colors.Magenta * scale);
                    Equal(pixels.GetPixel(135, 7), Colors.Cyan * scale); Equal(pixels.GetPixel(159, 7), Colors.Yellow * (frame == 4 ? .25f : 1f));
                    var tilePoint = tiles.Position + tiles.MapToLocal(default); Equal(pixels.GetPixel((int)tilePoint.X, (int)tilePoint.Y), Colors.Cyan * scale);
                    Check(RenderingServer.CanvasItemGetInstanceShaderParameterList(tiles.GetCanvasItem()).Count == 4, "Tile layer discovers generated tile materials.");
                    Equal(pixels.GetPixel(57, 31), Colors.Yellow * scale); Equal(pixels.GetPixel(89, 31), Colors.Magenta * scale);
                    if (frame == 0) red.SetInstanceShaderParameter<Color>("tint", null);
                    if (frame == 1) RenderingServer.CanvasItemSetInstanceShaderParameter<Color>(red.GetCanvasItem(), "tint", Colors.Green);
                    if (frame == 2) { var code = shader.GetSPIRV(); shader.SetSPIRV(code); }
                    if (frame == 3) { light.Enabled = true; material.SetShaderParameter("scale", .5f); alternate.SetShaderParameter("scale", .5f); }
                }
                if (++frame == 56) { Check(allocated == 0, $"Prepared instance parameter updates/rendering allocated {allocated} bytes."); Check(red.Recordings == 1 && blue.Recordings == 1, "Uniform edits do not rerecord geometry."); root.Tree!.Quit(); }
            };
        };
        Check(Engine.Run(root) == 0 && frame == 56 && root.IsDisposed, "Instance native lifecycle completes.");
        Console.WriteLine($"Prepared per-item uniform rendering: {allocated} bytes, 24 warm +32 measured frames.");
    }
    private static void Equal(Color actual, Color expected) => Check(Math.Abs(actual.R - expected.R) <= .025f && Math.Abs(actual.G - expected.G) <= .025f && Math.Abs(actual.B - expected.B) <= .025f, $"Instance pixel {actual} differs from {expected}.");
}
