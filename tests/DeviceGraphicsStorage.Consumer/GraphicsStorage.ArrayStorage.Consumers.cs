using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void ArrayStorageReadOnly(RenderingDevice device, string language, string mode)
    {
        using var code = Code("array-storage-read", language);
        if (language == "hlsl")
        {
            if (mode == "array-storage-no-stores") Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code));
            // The HLSL RWTexture declaration retains write permission; external readonly SPIR-V supplies NonWritable.
            code.BytecodeVertex = ReadOnlyArrayModule(code.BytecodeVertex); code.BytecodeFragment = ReadOnlyArrayModule(code.BytecodeFragment);
        }
        var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code));
        using var format = new RDTextureFormat { Width = 2, Height = 2, ArrayLayers = 1, TextureType = TextureType.TextureArray, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Storage };
        using var view = new RDTextureView(); float[] data = [.2f, .4f, .8f, 1, .2f, .4f, .8f, 1, .2f, .4f, .8f, 1, .2f, .4f, .8f, 1]; var image = device.TextureCreate(format, view, [MemoryMarshal.AsBytes(data.AsSpan()).ToArray()]);
        using var uniform = ViewUniform(UniformType.Image, 0, image); var set = device.UniformSetCreate([uniform], shader, 0);
        format.TextureType = TextureType.Texture; format.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom; var color = device.TextureCreate(format, view); var target = device.FramebufferCreate([color]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(color, 0, MemoryMarshal.AsBytes(data.AsSpan())); Check(data[0] == .2f && data[1] == .4f && data[2] == .8f && data[3] == 1, "Pure readonly arrays execute native image loads/queries without Sampling or a graphics store feature.");
        if (mode == "array-storage-no-stores") { using var writer = Code("array-storage", language); Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(writer)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(writer)); }
        foreach (var rid in new[] { pipeline, set, shader, target, color, image }) device.FreeRID(rid); Console.WriteLine("PASS " + language + " readonly native storage arrays, one-layer extent and independent store-feature guards.");
    }
    private static byte[] ReadOnlyArrayModule(byte[] bytes)
    {
        var words = MemoryMarshal.Cast<byte, uint>(bytes.AsSpan()).ToArray(); var variables = new List<uint>(); var types = new HashSet<uint>(); var pointers = new HashSet<uint>(); var firstType = -1;
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var op = words[at] & 65535;
            if (op is >= 19 and <= 39 && firstType < 0) firstType = at;
            if (op == 25 && words[at + 5] == 1 && words[at + 7] == 2) types.Add(words[at + 1]);
            if (op == 32 && types.Contains(words[at + 3])) pointers.Add(words[at + 1]);
            if (op == 59 && pointers.Contains(words[at + 1])) variables.Add(words[at + 2]);
        }
        Check(variables.Count == 1 && firstType > 0, "The readonly fixture has exactly one storage array variable.");
        var result = words[..firstType].ToList(); foreach (var variable in variables) result.AddRange([(3u << 16) | 71, variable, 24]); result.AddRange(words[firstType..]); return MemoryMarshal.AsBytes(result.ToArray().AsSpan()).ToArray();
    }
    private static void ArrayStorageCanvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(32, 16) }; using var single = new TextureArrayRD(); using var multiple = new TextureArrayRD(); using var authored = new TextureArray();
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgbaf); image.Fill(Colors.Black); authored.CreateFromImages([image, image, image]);
        using var whiteImage = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); whiteImage.Fill(Colors.White); using var white = ImageTexture.CreateFromImage(whiteImage); using var canvas = Electron2D.Shader.CreateFromSPIRV(File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "array-storage.canvas.glsl.spv"))); using var first = new ShaderMaterial { Shader = canvas }; using var second = new ShaderMaterial { Shader = canvas };
        var frame = 0; RID owned = default, borrowed = default, shader = default, pipeline = default, a = default, b = default;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!; using var format = new RDTextureFormat { Width = 4, Height = 4, ArrayLayers = 1, MIPMaps = 2, TextureType = TextureType.TextureArray, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.Storage | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); owned = device.TextureCreate(format, view); borrowed = RenderingServer.TextureGetRDTexture(authored.GetRID());
            using var code = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "array-canvas.comp.glsl.spv")) }; shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code)); pipeline = device.ComputePipelineCreate(shader);
            using var one = ViewUniform(UniformType.Image, 0, owned); using var many = ViewUniform(UniformType.Image, 0, borrowed); a = device.UniformSetCreate([one], shader, 0); b = device.UniformSetCreate([many], shader, 0); Write(device, a, 1); Write(device, b, 3);
            single.TextureRDRID = owned; multiple.TextureRDRID = borrowed; first.SetShaderLayeredParameter("pixels", single); second.SetShaderLayeredParameter("pixels", multiple); root.AddChild(new Patch(white) { Material = first }); root.AddChild(new Patch(white) { Material = second, Position = new(16, 0) }); RenderingServer.FramePostDraw += Captured;
        };
        void Write(RenderingDevice device, RID set, uint layers) { var list = device.ComputeListBegin(); try { device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 4, 4, layers); } finally { device.ComputeListEnd(); } }
        void Captured()
        {
            if (++frame < 3) return; var device = RenderingServer.GetRenderingDevice()!;
            using var screenshot = root.GetTexture().GetImage()!; foreach (var sample in new[] { (8, .2f), (24, .6f) }) { var pixel = screenshot.GetPixel(sample.Item1, 8); Check(Math.Abs(pixel.R - sample.Item2) < .01f && Math.Abs(pixel.G - .4f) < .01f && Math.Abs(pixel.B - .8f) < .01f, "GPU array stores reach real retained canvas sampling."); }
            using var raw = single.GetLayerData(0)!; Check(raw.Width == 4 && !raw.HasMIPMaps && single.HasMIPMaps() && Math.Abs(raw.GetPixel(1, 1).R - .2f) < .0001f, "One-layer array snapshots preserve their partial native mip chain.");
            if (frame == 3) { authored.UpdateLayer(image, 2); Write(device, b, 3); Check(device.TextureIsValid(borrowed), "Compatible authored array updates preserve the storage view identity."); return; }
            RenderingServer.FramePostDraw -= Captured; authored.Dispose(); Check(!device.TextureIsValid(borrowed) && !device.UniformSetIsValid(b) && multiple.GetLayers() == 0, "Authored array retirement expires private storage views and consumers."); device.FreeRID(owned); Check(single.GetLayers() == 0 && !device.UniformSetIsValid(a), "Owned array retirement invalidates borrowed metadata and storage sets."); foreach (var rid in new[] { a, b, pipeline, shader, borrowed }) device.FreeRID(rid); root.Tree!.Quit();
        }
        Engine.Run(root); Console.WriteLine("PASS array storage canvas: one/three layers, retained native sampling, copied snapshots, authored update/expiry and renderer recreation.");
    }
}
