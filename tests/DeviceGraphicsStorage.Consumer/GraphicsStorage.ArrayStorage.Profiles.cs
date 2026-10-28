using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void ArrayStorageProfiles(RenderingDevice device, string mode)
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_ARRAY_STORAGE_OUTPUT") ?? throw new InvalidOperationException("Supply the qualified array-binary directory."); Directory.CreateDirectory(directory);
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            using var compute = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "array-storage.comp." + language + ".spv")) }; using var graphics = Code("array-storage", language); using var read = Code("array-storage-read", language);
            foreach (var item in new[] { ("compute", compute), ("graphics", graphics), ("readonly", read) })
            {
                var file = System.IO.Path.Combine(directory, item.Item1 + "." + language + ".e2dshader");
                if (mode == "array-storage-export") { File.WriteAllBytes(file, device.ShaderCompileBinaryFromSPIRV(item.Item2)); continue; }
                if (mode == "array-storage-failure") continue;
                Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(item.Item2)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(item.Item2));
                var placeholder = device.ShaderCreatePlaceholder(); Reject<NotSupportedException>(() => device.ShaderCreateFromBytecode(File.ReadAllBytes(file), placeholder)); using var ordinary = Code("read", language); Check(device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(ordinary), placeholder) == placeholder, "Refused array binaries preserve placeholder identity and retry."); device.FreeRID(placeholder);
            }
        }
        using var format = new RDTextureFormat { Width = 4, Height = 4, ArrayLayers = 1, TextureType = TextureType.TextureArray, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.ColorAttachment | TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo }; using var view = new RDTextureView(); var array = device.TextureCreate(format, view);
        if (mode == "array-storage-failure")
        {
            using var code = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "array-canvas.comp.glsl.spv")) }; var shader = device.ShaderCreateFromSPIRV(code); using var uniform = ViewUniform(UniformType.Image, 0, array); var before = device.GetMemoryUsage(MemoryType.Textures); for (var i = 0; i < 3; i++) Reject<InvalidOperationException>(() => device.UniformSetCreate([uniform], shader, 0)); Check(device.GetMemoryUsage(MemoryType.Textures) == before, "Failed private storage-view creation rolls back image lifetime without duplicating payload."); device.FreeRID(shader);
        }
        device.TextureClear(array, new(.25f, .5f, .75f, 1), 0, 1, 0, 1); var data = device.TextureGetData(array, 0); Check(BitConverter.ToSingle(data) == .25f && BitConverter.ToSingle(data, 4) == .5f, "Array adapter refusal preserves ordinary texture operations."); device.FreeRID(array);
        if (mode == "array-storage-legacy") { Reject<NotSupportedException>(() => ViewCompute(device, "glsl", false)); ImageWrites(device, "glsl", false); }
        Console.WriteLine("PASS " + mode + ": compute/graphics/readonly array pre-publication guards, placeholder retry, view rollback and surviving clear/readback.");
    }
}
