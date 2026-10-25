using Electron2D;
using System.Runtime.InteropServices;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void RenderSurface(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(768, 768), Title = "Canvas surface outputs", UseHDR2D = args.Contains("hdr") };
        var scene = new SurfaceScene(folder); window.AddChild(scene); Engine.Run(window);
        Check(scene.Complete && window.IsDisposed && !RenderingServer.IsAvailable, "Surface host retires its renderer.");
    }
    private sealed class SurfacePatch(ArrayMesh mesh, MultiMesh? multi, Texture texture, int kind, bool flip, RID particles = default) : Entity
    {
        protected override void OnDraw()
        {
            DrawSetTransformMatrix(SurfaceScene.Draw);
            if (particles.IsValid()) RenderingServer.CanvasItemAddParticles(GetCanvasItem(), particles, texture.GetRID());
            else if (kind == 0) DrawTextureRect(texture, new(0, 0, flip ? -16 : 16, 16), false);
            else if (multi is not null) DrawMultiMesh(multi, texture);
            else DrawMesh(mesh, texture, SurfaceScene.Local);
        }
    }
    private readonly record struct SurfaceProbe(string Name, int Kind, Transform NormalModel, Transform Geometry, bool NormalTexture, bool Flip);
    private sealed class SurfaceScene(string folder) : Node
    {
        internal static readonly Transform Draw = new(new(.9f, .125f), new(-.1f, 1), Vector2.Zero);
        internal static readonly Transform Local = new(new(.8f, .1f), new(.2f, .9f), new(1, 1));
        private static readonly Transform Canvas = new(new(1, .3f), new(-.25f, 1.1f), new(128, 24));
        private readonly List<Resource> _owned = []; private readonly List<RID> _rids = []; private readonly List<SurfaceProbe> _probes = [];
        private DirectionalLight _first = null!, _second = null!; private PointLight _point = null!;
        private CanvasModulate _modulate = null!; private LightOccluder _occluder = null!; private ShaderMaterial _depthMaterial = null!;
        private CanvasTexture _depthTexture = null!; private ImageTexture _normalImage = null!; private bool _depthNormal = true;
        private int _frame; private long _owner, _all; private float _depth = .5f; internal bool Complete;
        private T KeepSurface<T>(T resource) where T : Resource { _owned.Add(resource); return resource; }
        private ArrayMesh Quad(bool skin = false, bool procedural = false)
        {
            var mesh = KeepSurface(new ArrayMesh());
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new()
            {
                Vertices = procedural ? [] : [new(0, 0), new(16, 0), new(16, 16), new(0, 16)],
                VertexCount = 4,
                UVs = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)],
                Indices = [0, 1, 2, 0, 2, 3],
                Bones = skin ? new int[16] : [],
                Weights = skin ? [1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0] : []
            }, flags: procedural ? Mesh.ArrayFormat.UseEmptyVertexArray : 0);
            if (procedural) mesh.CustomAABB = new(-128, -128, 512, 512); return mesh;
        }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            ImageTexture Pixel(Color color)
            {
                using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(color);
                return KeepSurface(ImageTexture.CreateFromImage(image));
            }
            var white = Pixel(Colors.White); _normalImage = Pixel(new(.8f, .5f, .2f));
            var normal = KeepSurface(new CanvasTexture { DiffuseTexture = white, NormalTexture = _normalImage });
            _depthTexture = KeepSurface(new CanvasTexture { DiffuseTexture = white, NormalTexture = _normalImage });
            var mesh = Quad(); var skinMesh = Quad(true); var procedural = Quad(procedural: true);
            _first = new DirectionalLight { Height = 1, Energy = .25f };
            _second = new DirectionalLight { Height = 1, Energy = .5f, Enabled = false };
            _point = new PointLight { Texture = white, Position = new(128, 128), Height = 32, TextureScale = 2048, Energy = .25f, Enabled = false };
            _modulate = new CanvasModulate { Color = Colors.Black };
            AddChild(_first); AddChild(_second); AddChild(_point); AddChild(_modulate);
            var polygon = KeepSurface(new OccluderPolygon { Closed = false, Polygon = [new(16, 0), new(16, 768)] });
            _occluder = new LightOccluder { Occluder = polygon, Visible = false }; AddChild(_occluder);
            var process = KeepSurface(ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "support/surface-process.e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
            var processMaterial = KeepSurface(new ShaderMaterial { Shader = process });
            for (var row = 0; row < SurfacePrograms.Length; row++)
            {
                var name = SurfacePrograms[row];
                var shader = KeepSurface(ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "surface-" + name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
                var material = KeepSurface(new ShaderMaterial { Shader = shader }); if (name == "parameter") { _depthMaterial = material; material.SetShaderParameter("surface_depth", _depth); }
                Check(!shader.GetShaderUniformList().Any(p => p.Name is "NORMAL_MODEL_BASIS" or "NORMAL_CANVAS_BASIS"), "Normal bases stay outside material properties.");
                for (var kind = name == "procedural" ? 8 : 0; kind < (name == "procedural" ? 9 : 10); kind++)
                {
                    if (kind == 8 && name != "procedural") continue;
                    var item = new Transform(new(1.2f, .1f), new(.2f, .8f), new(4 + kind * 64, 4 + row * 24));
                    var geometry = item * Draw; var normalModel = geometry; var hasNormal = name is "depth-only" or "sample";
                    Texture texture = name == "depth-only" ? _depthTexture : hasNormal ? normal : white; MultiMesh? multi = null; RID particles = default;
                    var actualMesh = kind is 2 or 6 or 7 ? skinMesh : kind == 8 ? procedural : mesh;
                    var pose = new Transform(new(.8f, .15f), new(-.2f, 1.1f), new(5, 1));
                    if (kind is 2 or 3 or 4 or 8)
                    {
                        multi = KeepSurface(new MultiMesh { Mesh = actualMesh, InstanceCount = 1 }); multi.SetInstanceTransform2D(0, pose);
                        if (kind == 4)
                        {
                            var storage = RenderingServer.MultiMeshGetBufferRDRID(multi.GetRID()); var values = multi.Buffer;
                            values[0] += .25f; values[3] += 2;
                            RenderingServer.GetRenderingDevice()!.BufferUpdate(storage, 0, (uint)(values.Length * sizeof(float)), MemoryMarshal.AsBytes(values.AsSpan()));
                            pose.X.X += .25f; pose.Origin.X += 2;
                            Check(multi.GetInstanceTransform2D(0).X.X == .8f, "GPU writes leave the CPU pose snapshot unchanged.");
                        }
                        geometry *= pose;
                    }
                    else if (kind is 5 or 7 or 9)
                    {
                        particles = RenderingServer.ParticlesCreate(); _rids.Add(particles);
                        RenderingServer.ParticlesSetAmount(particles, 1); RenderingServer.ParticlesSetEmitting(particles, false); RenderingServer.ParticlesSetLifetime(particles, 1000);
                        RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetInterpolate(particles, false);
                        RenderingServer.ParticlesSetUseLocalCoordinates(particles, kind != 9); RenderingServer.ParticlesSetCustomAABB(particles, new(-64, -64, 128, 128));
                        RenderingServer.ParticlesSetProcessMaterial(particles, processMaterial.GetRID()); RenderingServer.ParticlesSetDrawPasses(particles, 1); RenderingServer.ParticlesSetDrawPassMesh(particles, 0, actualMesh.GetRID());
                        if (kind == 7) { RenderingServer.ParticlesSetTrailBindPoses(particles, [Transform.Identity, Transform.Identity]); RenderingServer.ParticlesSetTrails(particles, true, .5f); }
                        var emission = new Transform(new(1.5f, .2f), new(.3f, .8f), new(2, 3));
                        if (kind == 9) RenderingServer.ParticlesSetEmissionTransform(particles, emission);
                        RenderingServer.ParticlesEmit(particles, kind == 9 ? emission * pose : pose, Vector2.Zero, Colors.White, Colors.White, 31); RenderingServer.ParticlesRequestProcessTime(particles, .125f);
                        geometry *= pose;
                    }
                    else if (kind != 0) { geometry *= Local; normalModel *= Local; }
                    var patch = new SurfacePatch(actualMesh, multi, texture, kind, name == "depth-only", particles) { Transform = item, Material = material }; AddChild(patch);
                    if (kind == 6)
                    {
                        var palette = RenderingServer.SkeletonCreate(); _rids.Add(palette); RenderingServer.SkeletonAllocateData(palette, 1);
                        RenderingServer.SkeletonSetBaseTransform2D(palette, item); var bone = new Transform(.2f, new Vector2(2, 1));
                        RenderingServer.SkeletonBoneSetTransform2D(palette, 0, bone); RenderingServer.CanvasItemAttachSkeleton(patch.GetCanvasItem(), palette);
                        if (name != "matrix") geometry *= bone;
                    }
                    _probes.Add(new(name, kind, normalModel, geometry, hasNormal, kind == 0 && name == "depth-only"));
                }
            }
            RenderingServer.FramePostDraw += AfterSurface;
        }
        protected override void OnProcess(double delta)
        {
            if (_frame is >= 30 and < 94)
            {
                GetWindow()!.CanvasTransform = (_frame & 1) == 0 ? Canvas : Transform.Identity;
                _depth = (_frame & 1) == 0 ? .5f : 1f; _depthMaterial.SetShaderParameter("surface_depth", _depth);
            }
        }
        private void AfterSurface()
        {
            _frame++;
            if (_frame is 2 or 5 or 8 or 11 or 14 or 17 or 22 or 25 or 28 or 29) VerifySurface();
            if (_frame == 3) _second.Enabled = true;
            if (_frame == 6) { _first.Enabled = _second.Enabled = false; _modulate.Color = Colors.White; }
            if (_frame == 9) { _first.Enabled = true; _modulate.Color = Colors.Black; GetWindow()!.CanvasTransform = Canvas; }
            if (_frame == 12) { _first.Enabled = false; _point.Enabled = true; GetWindow()!.CanvasTransform = Transform.Identity; }
            if (_frame == 15) { _point.ShadowEnabled = true; _occluder.Visible = true; }
            if (_frame == 18) { _point.Enabled = false; _first.Enabled = true; _occluder.Visible = false; }
            if (_frame == 20) foreach (var shader in _owned.OfType<VisualShader>().Where(s => s.GetMode() == Shader.Mode.CanvasItem)) shader.SetSPIRV(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex), shader.GetSPIRV());
            if (_frame == 23) _first.Energy = 0;
            if (_frame == 26) _first.Energy = .25f;
            if (_frame == 27) { _depthTexture.NormalTexture = null; _depthNormal = false; }
            if (_frame == 28) { _depthTexture.NormalTexture = _normalImage; _depthNormal = true; }
            if (_frame is 30 or 110) { _owner = GC.GetAllocatedBytesForCurrentThread(); _all = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 94 or 174)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all;
                Console.WriteLine($"Canvas surfaces {(_frame == 94 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} managed bytes.");
                Check(owner == 0 && all == 0, "Prepared surface transport allocates no managed bytes."); VerifySurface();
                if (_frame == 174) { Complete = true; Tree!.Quit(); }
            }
        }
        private Vector3 SurfaceNormal(SurfaceProbe probe)
        {
            var name = probe.Name;
            var vector = name == "depth-only" && !_depthNormal ? new Vector3(0, 0, 1) : name switch
            {
                "depth-only-empty" or "light-vertex" or "shadow-vertex" => new Vector3(0, 0, 1),
                "depth-only" => new Vector3(probe.Flip ? -.6f : .6f, -1f / 255, MathF.Sqrt(1 - .36f - 1f / 65025)),
                "sample" => new Vector3(.6f, -1f / 255, .2f),
                "custom" => new Vector3(.25f, .5f, .75f),
                "depth" or "combined" => new Vector3(.3f, 0, .9f),
                "depth-zero" => new Vector3(0, 0, 1),
                "depth-negative" => new Vector3(-.6f, 0, 1.2f),
                "depth-over" => new Vector3(1.2f, 0, .6f),
                "parameter" => new Vector3(.6f * _depth, 0, 1 - .2f * _depth),
                _ => new Vector3(.6f, 0, .8f)
            };
            var model = probe.NormalModel; var canvas = GetWindow()!.CanvasTransform;
            var itemXY = model.X.Normalized() * vector.X + model.Y.Normalized() * vector.Y;
            var viewXY = canvas.X.Normalized() * itemXY.X + canvas.Y.Normalized() * itemXY.Y;
            return new Vector3(viewXY.X, viewXY.Y, vector.Z).Normalized();
        }
        private void VerifySurface()
        {
            using var image = GetWindow()!.GetTexture().GetImage()!;
            foreach (var probe in _probes)
            {
                var pos = GetWindow()!.CanvasTransform * (probe.Geometry * new Vector2(6, 6)); var x = (int)pos.X; var y = (int)pos.Y;
                var normal = SurfaceNormal(probe); var automatic = probe.Name.StartsWith("auto-", StringComparison.Ordinal);
                var count = _frame == 5 ? 2 : 1;
                Vector3 rgb = probe.Name is "light-vertex" or "shadow-vertex" ? new(.25f, .5f, .75f) : normal * .5f + Vector3.One * .5f;
                rgb *= .5f * count;
                if (automatic)
                {
                    var vertex = new Vector3(x + .5f, y + .5f, 0);
                    var direction = _frame is 14 or 17 ? (new Vector3(128, 128, 32) - vertex).Normalized() : new Vector3(0, 0, 1);
                    var energy = _frame == 25 ? 0 : _frame == 5 ? .75f : .25f;
                    rgb = Vector3.One * MathF.Max(0, normal.Dot(direction)) * energy;
                }
                if (_frame == 8) rgb = Vector3.One;
                if (_frame == 17 && x < 16 && probe.Name is not ("shadow-vertex" or "combined")) rgb = Vector3.Zero;
                var actual = image.GetPixel(x, y);
                Check(MathF.Abs(actual.R - rgb.X) < .008f && MathF.Abs(actual.G - rgb.Y) < .008f && MathF.Abs(actual.B - rgb.Z) < .008f,
                    $"Surface {probe.Name}/{probe.Kind}, frame {_frame}, pixel {x},{y}: {actual}, expected {rgb}.");
                if (probe.Name == "discard")
                {
                    var right = GetWindow()!.CanvasTransform * (probe.Geometry * new Vector2(12, 6));
                    var discarded = image.GetPixel((int)right.X, (int)right.Y);
                    Check(discarded.R < .008f && discarded.G < .008f && discarded.B < .008f, "Discard preserves the surface/light epilogue boundary.");
                }
            }
            Console.WriteLine($"Surface phase {_frame}: {_probes.Count} actual geometry/light pixel observations passed.");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RenderingServer.FramePostDraw -= AfterSurface;
                if (RenderingServer.IsAvailable) foreach (var rid in _rids) RenderingServer.FreeRID(rid);
            }
            base.Dispose(disposing);
            if (disposing) for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose();
        }
    }
}
