using System.Buffers.Binary;
using Electron2D;

internal static partial class ShaderSamplerChecks
{
    private sealed class CoordinateTick(Action tick) : Node { protected override void OnProcess(double delta) => tick(); }
    private static void CoordinatesCanvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(32, 32), Borderless = true };
        var resources = new List<Resource>(); var frames = 0;
        T Own<T>(T value) where T : Resource { resources.Add(value); return value; }
        root.AddChild(new CoordinateTick(() => RenderingServer.ForceDraw(swapBuffers: false)));
        root.Ready += _ =>
        {
            RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.SetPixel(0, 0, Colors.Red); image.SetPixel(1, 0, Colors.Green); image.SetPixel(0, 1, Colors.Blue); image.SetPixel(1, 1, Colors.White);
            var texture = Own(ImageTexture.CreateFromImage(image)); var shader = Own(Load("BlitCoordsGLSL", true)); var material = Own(new ShaderMaterial { Shader = shader }); var target = Own(new DrawableTexture());
            using var state = CoordinateState(); shader.SetTextureSamplerState("SOURCE_TEXTURE0", state);
            CoordinateArchive(shader);
            material.SetShaderParameter("Coordinates", new Vector4(1.5f, .5f, 0, 0)); target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Transparent); target.BlitRect(new(0, 0, 4, 4), texture, material: material);
            using (var result = target.GetImage()!) Match(result.GetPixel(2, 2), Colors.Green, "native fragment texel coordinates");
            image.GenerateMIPMaps(); using var mip = ImageTexture.CreateFromImage(image);
            Reject<ArgumentException>(() => target.BlitRect(new(0, 0, 4, 4), mip, material: material));
            target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Transparent); target.BlitRect(new(0, 0, 4, 4), texture, material: material);
            using (var result = target.GetImage()!) Match(result.GetPixel(2, 2), Colors.Green, "binding failure leaves native blit usable");
            using var fetchShader = Load("BlitCoordsFetchGLSL", true); fetchShader.SetTextureSamplerState("SOURCE_TEXTURE0", state); using var fetchMaterial = new ShaderMaterial { Shader = fetchShader };
            target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Transparent); target.BlitRect(new(0, 0, 4, 4), mip, material: fetchMaterial);
            using (var result = target.GetImage()!) Match(result.GetPixel(2, 2), new Color(.5f, .5f, .5f, 1), "samplerless blit ignores coordinate mode and reads mip one");
            using var lodShader = Load("BlitCoordsLODGLSL", true); lodShader.SetTextureSamplerState("SOURCE_TEXTURE0", state);
            using var lodMaterial = new ShaderMaterial { Shader = lodShader }; target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Transparent); target.BlitRect(new(0, 0, 4, 4), texture, material: lodMaterial);
            using (var result = target.GetImage()!) Match(result.GetPixel(2, 2), Colors.Black, "native LOD query in single-level texel coordinates");
            using var implicitShader = Load("BlitGLSL", true); Reject<ArgumentException>(() => implicitShader.SetTextureSamplerState("SOURCE_TEXTURE0", state));
            var vertex = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "CoordsSampleGLSL.vert.glsl.spv"));
            var fragment = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "CoordsColorGLSL.frag.glsl.spv"));
            var vertexShader = Own(Shader.CreateFromSPIRV(vertex, fragment)); vertexShader.SetTextureSamplerState("pixels", state); vertexShader.SetDefaultTextureParameter("pixels", texture);
            var vertexMaterial = Own(new ShaderMaterial { Shader = vertexShader }); root.AddChild(new Quad(texture, 8) { Position = new(2, 2), Material = vertexMaterial });
            using var sharedFragment = Load("CoordsCanvasGLSL"); var shared = Own(Shader.CreateFromSPIRV(vertex, sharedFragment.GetSPIRV())); shared.SetTextureSamplerState("pixels", state); shared.SetDefaultTextureParameter("pixels", texture);
            var sharedMaterial = Own(new ShaderMaterial { Shader = shared }); sharedMaterial.SetShaderParameter("Coordinates", new Vector4(1.5f, .5f, 0, 0)); root.AddChild(new Quad(texture, 8) { Position = new(14, 2), Material = sharedMaterial });
            using var unsafeFragment = Load("CanvasGLSL"); using var conflict = Shader.CreateFromSPIRV(vertex, unsafeFragment.GetSPIRV()); Reject<ArgumentException>(() => conflict.SetTextureSamplerState("pixels", state));
            RenderingServer.FramePostDraw += Frame;
        };
        try { Engine.Run(root); }
        finally { foreach (var value in resources.AsEnumerable().Reverse()) value.Dispose(); }
        Console.WriteLine("Native texel coordinates: writable-image fragment, vertex-only and shared vertex/fragment pixels, metadata archives, implicit-operation guards and mip-view failure/recovery passed.");
        void Frame()
        {
            if (++frames < 4) return;
            using var result = root.GetTexture().GetImage()!; Match(result.GetPixel(6, 6), Colors.Green, "vertex texel coordinate sample"); Match(result.GetPixel(18, 6), Colors.Green, "shared vertex/fragment texel coordinate sample");
            RenderingServer.FramePostDraw -= Frame; root.Tree!.Quit();
        }
    }
    private static void CoordinatesGlyphGuard()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var shader = Load("CoordsGlyphGLSL"); using var state = CoordinateState(); shader.SetTextureSamplerState("TEXTURE", state);
        using var material = new ShaderMaterial { Shader = shader }; using var root = new Window { Size = new(32, 32), Borderless = true };
        using var font = (FontFile)ThemeDB.FallbackFont!.Duplicate(); font.MultichannelSignedDistanceField = true; font.MSDFSize = 32; font.MSDFPixelRange = 8;
        var label = new Label("X") { Material = material, Position = new(2, 2), Size = new(24, 24) }; label.AddThemeFontOverride("font", font); root.AddChild(label); var frames = 0;
        root.AddChild(new CoordinateTick(() => { if (++frames > 8) root.Tree!.Quit(); RenderingServer.ForceDraw(swapBuffers: false); }));
        root.Ready += _ => RenderingServer.RenderLoopEnabled = false;
        var rejected = false;
        try { Engine.Run(root); }
        catch (Exception error) when (error.ToString().Contains("derived native shader binding", StringComparison.Ordinal)) { rejected = true; }
        Check(rejected && root.IsDisposed && !RenderingServer.IsAvailable, $"Implicit glyph sampling introduced by native composition rejects before drawing and retires the renderer: rejected={rejected}, disposed={root.IsDisposed}, renderer={RenderingServer.IsAvailable}, frames={frames}.");
        Console.WriteLine("Derived glyph shader rejects unnormalized implicit sampling before native drawing; renderer/root retirement passed.");
    }

    private static void CoordinateArchive(Shader shader)
    {
        using var copy = (Shader)shader.Duplicate(); using var state = copy.GetTextureSamplerState("SOURCE_TEXTURE0")!; Check(state.UnnormalizedUVW && state.MaxLOD == 0, "Shader duplication preserves coordinate mode.");
        var descriptor = (PropertyDescriptor<Shader, byte[]>)shader.GetPropertyList().Single(p => p.Name == "SamplerSettings"); var bytes = descriptor.GetValue(shader); Check(BinaryPrimitives.ReadInt32LittleEndian(bytes) == 3, "Coordinate metadata has an explicit version.");
        var previous = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(previous, 2); Reject<InvalidDataException>(() => descriptor.SetValue(shader, previous));
        using var preserved = shader.GetTextureSamplerState("SOURCE_TEXTURE0")!; Check(preserved.UnnormalizedUVW, "Old-version flag rejection preserves the prior complete table.");
        var folder = Directory.CreateTempSubdirectory("e2d-coordinate-");
        try
        {
            var path = System.IO.Path.Combine(folder.FullName, "coordinates.e2dres"); ResourceSaver.Save(shader, path); using var loaded = ResourceLoader.Load<Shader>(path, ResourceLoader.CacheMode.IgnoreDeep); using var restored = loaded.GetTextureSamplerState("SOURCE_TEXTURE0")!; Check(restored.UnnormalizedUVW && restored.MaxLOD == 0, "Physical archive restores executable texel-coordinate settings.");
        }
        finally { folder.Delete(recursive: true); }
    }
}
