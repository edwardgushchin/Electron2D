using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void ArrayStorage(string mode)
    {
        if (mode == "array-storage-canvas") { ArrayStorageCanvas(); ArrayStorageCanvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Array storage device: " + device.GetDeviceName());
        if (mode is "array-storage-readonly" or "array-storage-no-stores") { foreach (var language in new[] { "glsl", "hlsl" }) { ArrayStorageReadOnly(device, language, mode); if (mode == "array-storage-no-stores") ArrayStorageCompute(device, language, false); } return; }
        if (mode is "array-storage-export" or "array-storage-unavailable" or "array-storage-legacy" or "array-storage-failure") { ArrayStorageProfiles(device, mode); return; }
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            try { ArrayStorageCompute(device, language, mode == "array-storage-source"); ArrayStorageGraphics(device, language, mode == "array-storage-source"); }
            catch (Exception error) { Console.Error.WriteLine(error); throw; }
        }
    }
    private static RID ArrayStorageShader(RenderingDevice device, string language, bool source)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "array-storage.comp." + language);
        if (source)
        {
            using var text = new RDShaderSource { Language = language == "glsl" ? ShaderLanguage.GLSL : ShaderLanguage.HLSL, SourceCompute = File.ReadAllText(path) }; using var code = device.ShaderCompileSPIRVFromSource(text); Check(code.CompileErrorCompute == "", code.CompileErrorCompute); return device.ShaderCreateFromSPIRV(code);
        }
        using var binary = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(path + ".spv") }; var placeholder = device.ShaderCreatePlaceholder(); Check(device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(binary), placeholder) == placeholder, "Array image binaries preserve placeholder identity."); return placeholder;
    }
    private static void ArrayStorageCompute(RenderingDevice device, string language, bool source)
    {
        const TextureUsageBits usage = TextureUsageBits.Storage | TextureUsageBits.Sampling | TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | TextureUsageBits.CanUpdate;
        using var format = new RDTextureFormat { Width = 4, Height = 4, ArrayLayers = 3, MIPMaps = 2, TextureType = TextureType.TextureArray, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = usage }; using var plain = new RDTextureView();
        float[] initial = new float[80]; initial.AsSpan().Fill(-7); var data = MemoryMarshal.AsBytes(initial.AsSpan()).ToArray(); var destination = device.TextureCreate(format, plain, [data, data, data]);
        format.Format = DataFormat.R32SFLOAT; format.AddShareableFormat(DataFormat.R32SFLOAT); format.AddShareableFormat(DataFormat.R32UINT); byte[][] inputs = new byte[3][];
        for (var layer = 0; layer < 3; layer++) { float[] values = new float[20]; values.AsSpan(0, 16).Fill(layer == 0 ? .25f : layer == 1 ? .5f : 0); values.AsSpan(16).Fill(layer == 0 ? .375f : layer == 1 ? .5f : .75f); inputs[layer] = MemoryMarshal.AsBytes(values.AsSpan()).ToArray(); }
        var original = device.TextureCreate(format, plain, inputs); using var integer = new RDTextureView { FormatOverride = DataFormat.R32UINT }; var counter = device.TextureCreateShared(integer, original); var reset = device.TextureCreateSharedFromSlice(integer, original, 2, 0);
        var shader = ArrayStorageShader(device, language, source); var pipeline = device.ComputePipelineCreate(shader); var output = device.StorageBufferCreate(48 * 16); using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState);
        using var write = ViewUniform(UniformType.Image, 0, destination); using var atomic = ViewUniform(UniformType.Image, 1, counter); using var sampled = ViewUniform(UniformType.SamplerWithTexture, 2, sampler, original); using var result = ViewUniform(UniformType.StorageBuffer, 3, output); var set = device.UniformSetCreate([write, atomic, sampled, result], shader, 0);
        using var wrong = ViewUniform(UniformType.Image, 1, original); Reject<ArgumentException>(() => device.UniformSetCreate([write, wrong, sampled, result], shader, 0));
        using var mapped = new RDTextureView { FormatOverride = DataFormat.R32UINT, SwizzleR = TextureSwizzle.B }; var nonidentity = device.TextureCreateShared(mapped, original); wrong.ClearIDs(); wrong.AddID(nonidentity); Reject<ArgumentException>(() => device.UniformSetCreate([write, wrong, sampled, result], shader, 0));
        var slice = device.TextureCreateSharedFromSlice(integer, original, 1, 0); wrong.ClearIDs(); wrong.AddID(slice); Reject<ArgumentException>(() => device.UniformSetCreate([write, wrong, sampled, result], shader, 0));
        float[][] pixels = [new float[80], new float[80], new float[80]]; uint[] counters = new uint[20], records = new uint[192];
        void Cycle()
        {
            device.TextureClear(reset, default, 0, 1, 0, 1); var list = device.ComputeListBegin();
            try { device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 3); }
            finally { device.ComputeListEnd(); }
            for (var layer = 0; layer < 3; layer++) device.TextureGetData(destination, (uint)layer, MemoryMarshal.AsBytes(pixels[layer].AsSpan())); device.TextureGetData(original, 2, MemoryMarshal.AsBytes(counters.AsSpan())); device.BufferGetData(output, MemoryMarshal.AsBytes(records.AsSpan()));
        }
        Cycle();
        for (var layer = 0; layer < 3; layer++)
        {
            for (var i = 0; i < 16; i++) { var index = (layer * 16 + i) * 4; Check(pixels[layer][i * 4] == .375f && pixels[layer][i * 4 + 1] == layer + 1 && pixels[layer][i * 4 + 2] == .5f && pixels[layer][i * 4 + 3] == 1, "Storage array coordinates preserve every layer and sampled sibling data."); Check(records[index + 1] == 3 && records[index + 2] == 3 && records[index + 3] == 2, "Native image array dimensions retain layers and independent sampled mip counts."); }
            Check(pixels[layer].AsSpan(64).IndexOfAnyExcept(-7f) < 0, "Array storage descriptors address one base mip and preserve higher mip contents.");
        }
        for (var i = 0; i < 16; i++) { var mask = (1u << (int)records[i * 4]) | (1u << (int)records[(16 + i) * 4]) | (1u << (int)records[(32 + i) * 4]); Check(mask == 7 && counters[i] == 3, "Concurrent array R32 atomics return each prior value exactly once."); } Check(counters.AsSpan(16).IndexOfAnyExcept(BitConverter.SingleToUInt32Bits(.75f)) < 0, "Array atomic views preserve the owner's other mips.");
        for (var i = 0; i < 16; i++) Cycle(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Cycle(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared array storage/sample/atomic/readback cycles allocate zero owner bytes.");
        device.FreeRID(original); Check(!device.UniformSetIsValid(set), "Original retirement invalidates array image sets borrowing a shared format view."); foreach (var rid in new[] { set, pipeline, shader, destination, output, sampler }) device.FreeRID(rid);
        Console.WriteLine("PASS " + language + " array storage compute: original image writes/loads/R32 atomics, sampled same-image sibling, exact layer/mip extents, copied binary lifetime and zero owner bytes.");
    }
}
