using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static class DepthTransfersTests
{
    private static int _checks;
    private static string? _input, _output;
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static RID Shader(RenderingDevice device, string name, RDShaderSource source)
    {
        using (source)
        {
            if (_input is not null) return device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".bin")));
            using var module = device.ShaderCompileSPIRVFromSource(source, false);
            Check(module.CompileErrorVertex + module.CompileErrorFragment + module.CompileErrorCompute == "", name + ": " + module.CompileErrorVertex + module.CompileErrorFragment + module.CompileErrorCompute);
            if (_output is not null) { Directory.CreateDirectory(_output); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".bin"), device.ShaderCompileBinaryFromSPIRV(module, name)); foreach (var stage in new[] { ShaderStage.Vertex, ShaderStage.Fragment, ShaderStage.Compute }) { var code = module.GetStageBytecode(stage); if (code.Length != 0) File.WriteAllBytes(System.IO.Path.Combine(_output, name + "." + stage + ".spv"), code); } }
            return device.ShaderCreateFromSPIRV(module, name);
        }
    }
    private const string Vertex = """
        #version 450
        void main() {
            vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));
            gl_Position=vec4(p[gl_VertexIndex],0,1);
        }
        """;
    private const TextureUsageBits Roles = TextureUsageBits.DepthStencilAttachment | TextureUsageBits.Sampling | TextureUsageBits.CanUpdate | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | TextureUsageBits.CPURead;
    private static bool Stencil(DataFormat format) => format is DataFormat.D24UNORMS8UINT or DataFormat.D32SFLOATS8UINT;
    private static int Bytes(DataFormat format) => format == DataFormat.D16UNORM ? 2 : format == DataFormat.D32SFLOATS8UINT ? 8 : 4;
    private static RID Image(RenderingDevice device, DataFormat encoding, bool array = false, uint mips = 3, uint size = 4, byte[][]? data = null, TextureUsageBits usage = Roles)
    {
        using var format = new RDTextureFormat { Width = size, Height = size, Format = encoding, TextureType = array ? TextureType.TextureArray : TextureType.Texture, ArrayLayers = array ? 2u : 1u, MIPMaps = mips, UsageBits = usage };
        using var view = new RDTextureView(); return device.TextureCreate(format, view, data ?? []);
    }
    private static byte[] Payload(DataFormat format, int phase, uint size = 4, uint mips = 3)
    {
        var stride = Bytes(format); var count = 0;
        for (var mip = 0; mip < mips; mip++) { var extent = (int)Math.Max(1, size >> mip); count += extent * extent; }
        var bytes = new byte[count * stride];
        for (var i = 0; i < count; i++)
        {
            var value = ((i + phase) % 9) / 8f; var at = i * stride;
            if (format == DataFormat.D16UNORM) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at), (ushort)MathF.Round(value * ushort.MaxValue));
            else if (format is DataFormat.X8D24UNORMPACK32 or DataFormat.D24UNORMS8UINT) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at), (uint)MathF.Round(value * 0xffffff));
            else BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(at), value);
            if (Stencil(format)) bytes[at + (format == DataFormat.D24UNORMS8UINT ? 3 : 4)] = (byte)((i * 37 + phase * 13) & 255);
            if (format == DataFormat.X8D24UNORMPACK32) bytes[at + 3] = 0xc3;
            if (format == DataFormat.D32SFLOATS8UINT) bytes.AsSpan(at + 5, 3).Fill(0xc3);
        }
        return bytes;
    }
    private static byte[] Canonical(DataFormat format, byte[] data)
    {
        var result = data.ToArray();
        for (var at = 0; at < result.Length; at += Bytes(format)) { if (format == DataFormat.X8D24UNORMPACK32) result[at + 3] = 0; if (format == DataFormat.D32SFLOATS8UINT) result.AsSpan(at + 5, 3).Clear(); }
        return result;
    }
    private static float Depth(DataFormat format, ReadOnlySpan<byte> data) => format switch
    {
        DataFormat.D16UNORM => BinaryPrimitives.ReadUInt16LittleEndian(data) / (float)ushort.MaxValue,
        DataFormat.X8D24UNORMPACK32 or DataFormat.D24UNORMS8UINT => (BinaryPrimitives.ReadUInt32LittleEndian(data) & 0xffffff) / (float)0xffffff,
        _ => BinaryPrimitives.ReadSingleLittleEndian(data)
    };
    private static RDUniform Uniform(int binding, UniformType kind, params RID[] ids)
    { var uniform = new RDUniform { Binding = binding, UniformType = kind }; foreach (var id in ids) uniform.AddID(id); return uniform; }
    internal static void Run(string? binaryDirectory = null)
    {
        _input = binaryDirectory; _output = Environment.GetEnvironmentVariable("ELECTRON2D_DEPTH_TRANSFERS_OUTPUT");
        using var device = RenderingServer.CreateLocalRenderingDevice();
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DEPTH_ADAPTER_MISSING") == "1")
        {
            Check(!device.TextureIsFormatSupportedForUsage(DataFormat.D32SFLOAT, Roles), "A missing adapter does not advertise depth transfers.");
            var depth = Image(device, DataFormat.D32SFLOAT, usage: TextureUsageBits.DepthStencilAttachment | TextureUsageBits.Sampling);
            Check(device.TextureIsValid(depth), "Native depth rendering remains available without the transfer adapter.");
            Reject<NotSupportedException>(() => Image(device, DataFormat.D32SFLOAT));
            Reject<NotSupportedException>(() => Image(device, DataFormat.D32SFLOAT, data: [Payload(DataFormat.D32SFLOAT, 1)], usage: TextureUsageBits.DepthStencilAttachment));
            Console.WriteLine($"Depth transfer adapter refusal PASS {_checks}"); return;
        }
        foreach (var format in new[] { DataFormat.D16UNORM, DataFormat.X8D24UNORMPACK32, DataFormat.D32SFLOAT, DataFormat.D24UNORMS8UINT, DataFormat.D32SFLOATS8UINT })
        {
            if (!device.TextureIsFormatSupportedForUsage(format, Roles)) { Console.WriteLine("Native depth format unavailable: " + format); continue; }
            Roundtrip(device, format); if (Stencil(format)) Graphics(device, format);
        }
        Negatives(device);
        Check(_checks > 50, "The host executed the complete depth transfer profile.");
        Console.WriteLine($"Depth transfers PASS {_checks}; {(_input is null ? "source/SPIR-V" : "binary")} on {device.GetDeviceName()}");
    }
    private static void Roundtrip(RenderingDevice device, DataFormat format)
    {
        var first = Payload(format, 1); var second = Payload(format, 2); var expected = Canonical(format, first);
        var oddData = Payload(format, 5, 3, 2); var odd = Image(device, format, mips: 2, size: 3, data: [oddData]);
        Check(device.TextureGetData(odd, 0).AsSpan().SequenceEqual(Canonical(format, oddData)), format + " odd dimensions preserve tight mip bytes across aligned native planes.");
        var source = Image(device, format, array: true, data: [first, second]); first.AsSpan().Fill(0x42);
        Check(device.TextureGetData(source, 0).AsSpan().SequenceEqual(expected), format + " initial upload copies the exact depth/stencil encoding and canonicalizes padding.");
        Check(device.TextureGetData(source, 1).AsSpan().SequenceEqual(Canonical(format, second)), format + " layer and mip-chain bytes remain independent.");
        var third = Payload(format, 3); var fourth = Payload(format, 4); byte[]? before = null; var after = new byte[expected.Length]; var callbacks = new List<int>();
        device.TextureGetDataAsync(source, 0, data => { before = data; callbacks.Add(1); });
        device.TextureUpdate(source, 0, third); device.TextureGetDataAsync(source, 0, after.AsMemory(), () => callbacks.Add(2)); device.TextureUpdate(source, 0, fourth);
        Check(callbacks.Count == 0, "Depth recording does not invoke queued callbacks."); device.Submit(); device.Sync();
        Check(callbacks.SequenceEqual([1, 2]) && before!.AsSpan().SequenceEqual(expected) && after.AsSpan().SequenceEqual(Canonical(format, third)), format + " independent asynchronous snapshots retain request-time data and FIFO.");
        Check(device.TextureGetData(source, 0).AsSpan().SequenceEqual(Canonical(format, fourth)), "Later updates remain visible after earlier queued snapshots.");
        var target = Image(device, format, array: true); var zero = new byte[expected.Length]; Check(device.TextureGetData(target, 1).AsSpan().SequenceEqual(zero), "Empty depth allocation initializes every layer, mip and stencil aspect.");
        device.TextureCopy(source, target, Vector3.Zero, new(2, 2, 0), new(2, 2, 1), 0, 0, 0, 1);
        var corner = zero.ToArray(); var current = Canonical(format, fourth); var stride = Bytes(format);
        for (var row = 0; row < 2; row++) current.AsSpan(row * 4 * stride, 2 * stride).CopyTo(corner.AsSpan(((row + 2) * 4 + 2) * stride));
        Check(device.TextureGetData(target, 1).AsSpan().SequenceEqual(corner), format + " rectangular copy preserves both aspects and neighboring subresources.");
        device.TextureCopy(source, source, Vector3.Zero, new(2, 2, 0), new(2, 2, 1), 0, 0, 0, 0);
        for (var row = 0; row < 2; row++) current.AsSpan(row * 4 * stride, 2 * stride).CopyTo(current.AsSpan(((row + 2) * 4 + 2) * stride));
        Check(device.TextureGetData(source, 0).AsSpan().SequenceEqual(current), "Disjoint same-subresource depth copies use retained scratch storage.");
        using var view = new RDTextureView(); var slice = device.TextureCreateSharedFromSlice(view, source, 1, 1, 1); var selected = Payload(format, 6, 2, 1); device.TextureUpdate(slice, 0, selected);
        var updatedSecond = Canonical(format, second); Canonical(format, selected).CopyTo(updatedSecond, 16 * stride);
        Check(device.TextureGetData(source, 1).AsSpan().SequenceEqual(updatedSecond), "Updating a shared depth slice modifies only the selected original mip/layer.");
        Check(device.TextureGetData(slice, 0).AsSpan().SequenceEqual(Canonical(format, selected)), "Depth slice readback exposes its tight selected extent.");
        var copiedSlice = Image(device, format, mips: 1, size: 2); device.TextureCopy(slice, copiedSlice, Vector3.Zero, Vector3.Zero, new(2, 2, 1), 0, 0, 0, 0);
        Check(device.TextureGetData(copiedSlice, 0).AsSpan().SequenceEqual(Canonical(format, selected)), "Depth copies map shared-view coordinates to the original selected layer/mip.");
        var alias = device.TextureCreateSharedFromSlice(view, source, 1, 1, 1); Reject<ArgumentException>(() => device.TextureCopy(slice, alias, Vector3.Zero, Vector3.Zero, new(1, 1, 1), 0, 0, 0, 0));
        device.TextureCopy(source, source, Vector3.Zero, Vector3.Zero, new(2, 2, 1), 1, 1, 1, 0); Canonical(format, selected).CopyTo(current, 16 * stride);
        Check(device.TextureGetData(source, 0).AsSpan().SequenceEqual(current), "Same-image cross-layer copies preserve independent aspect layouts.");
        device.TextureCopy(source, source, Vector3.Zero, new(2, 2, 0), new(2, 2, 1), 1, 0, 0, 0);
        for (var row = 0; row < 2; row++) Canonical(format, selected).AsSpan(row * 2 * stride, 2 * stride).CopyTo(current.AsSpan(((row + 2) * 4 + 2) * stride));
        Check(device.TextureGetData(source, 0).AsSpan().SequenceEqual(current), "Same-image cross-mip copies restore both nonoverlapping subresource layouts.");
        Sample(device, format, slice, Depth(format, selected));
        var destination = new byte[expected.Length]; var completed = 0; Action complete = () => completed++;
        void Execute()
        {
            device.TextureUpdate(source, 0, fourth); device.TextureCopy(source, source, Vector3.Zero, new(2, 2, 0), new(2, 2, 1), 0, 0, 0, 0); device.TextureCopy(source, target, Vector3.Zero, Vector3.Zero, new(4, 4, 1), 0, 0, 0, 0);
            device.TextureGetData(target, 0, destination.AsSpan()); device.TextureGetDataAsync(source, 0, after.AsMemory(), complete); device.Submit(); device.Sync();
        }
        for (var i = 0; i < 16; i++) Execute(); var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) Execute(); Check(GC.GetAllocatedBytesForCurrentThread() == allocated && completed == 48, "Prepared depth upload/copy/span/Memory readbacks allocate zero owner-thread managed bytes.");
        var later = false; device.TextureGetDataAsync(source, 0, _ => throw new InvalidOperationException("depth callback failure")); device.TextureGetDataAsync(source, 0, after.AsMemory(), () => later = true); device.Submit(); Reject<AggregateException>(device.Sync); Check(later, "Failing depth callbacks do not suppress later completed snapshots."); device.Sync();
        device.TextureGetDataAsync(slice, 0, _ => completed++); device.FreeRID(source); Check(!device.TextureIsValid(slice) && completed == 49, "Pending reads complete before depth owner and shared-view retirement.");
        Console.WriteLine(format + ": raw transfers, views, FIFO, copy and 32 zero-owner-byte cycles passed.");
    }
    private static void Sample(RenderingDevice device, DataFormat format, RID source, float expected)
    {
        var shader = Shader(device, "sample-depth", new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1) in;layout(set=0,binding=0) uniform sampler2D depth;layout(set=0,binding=1,std430) buffer R{float v;}r;void main(){r.v=texelFetch(depth,ivec2(0),0).r;}" });
        using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var result = device.StorageBufferCreate(4); using var a = Uniform(0, UniformType.SamplerWithTexture, sampler, source); using var b = Uniform(1, UniformType.StorageBuffer, result);
        var set = device.UniformSetCreate([a, b], shader, 0); var pipeline = device.ComputePipelineCreate(shader); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        Check(Math.Abs(BinaryPrimitives.ReadSingleLittleEndian(device.BufferGetData(result)) - expected) < .00002f, format + " native depth sampling consumes the uploaded selected-view bytes.");
    }
    private static void Graphics(RenderingDevice device, DataFormat format)
    {
        var data = Payload(format, 0, 4, 1); var stride = Bytes(format); void Fill(float depth, byte stencil) { for (var i = 0; i < 16; i++) { var part = data.AsSpan(i * stride); if (format == DataFormat.D24UNORMS8UINT) BinaryPrimitives.WriteUInt32LittleEndian(part, (uint)MathF.Round(depth * 0xffffff)); else BinaryPrimitives.WriteSingleLittleEndian(part, depth); part[format == DataFormat.D24UNORMS8UINT ? 3 : 4] = stencil; } }
        Fill(.75f, 7); var depthImage = Image(device, format, mips: 1, data: [data]);
        using var colorInfo = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var color = device.TextureCreate(colorInfo, view); var framebuffer = device.FramebufferCreate([color, depthImage]);
        var shader = Shader(device, "stencil-depth", new RDShaderSource { SourceVertex = Vertex.Replace("vec4(p[gl_VertexIndex],0,1)", "vec4(p[gl_VertexIndex],.5,1)"), SourceFragment = "#version 450\nlayout(location=0) out vec4 color;void main(){color=vec4(1,0,0,1);}" });
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        using var depth = new RDPipelineDepthStencilState { EnableDepthTest = true, DepthCompareOperator = CompareOperator.Less, EnableStencil = true, FrontOpCompareMask = 255, BackOpCompareMask = 255, FrontOpCompare = CompareOperator.Equal, BackOpCompare = CompareOperator.Equal, FrontOpReference = 7, BackOpReference = 7, FrontOpPass = StencilOperation.Keep, BackOpPass = StencilOperation.Keep, FrontOpFail = StencilOperation.Keep, BackOpFail = StencilOperation.Keep, FrontOpDepthFail = StencilOperation.Keep, BackOpDepthFail = StencilOperation.Keep };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: depth);
        void Draw(bool red) { var list = device.DrawListBegin(framebuffer, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); var pixels = device.TextureGetData(color, 0); Check(pixels[0] == (red ? 255 : 0), format + " uploaded depth and stencil drive actual native tests."); }
        Draw(true); Fill(.25f, 7); device.TextureUpdate(depthImage, 0, data); Draw(false); Fill(.75f, 9); device.TextureUpdate(depthImage, 0, data); Draw(false);
        Check(device.TextureGetData(depthImage, 0).AsSpan().SequenceEqual(Canonical(format, data)), "Graphics tests preserve both encoded source aspects when writes are disabled.");
        if (_output is not null) { using var pixels = Electron2D.Image.CreateFromData(4, 4, false, Electron2D.Image.Format.Rgba8, device.TextureGetData(color, 0)); pixels.SavePNG(System.IO.Path.Combine(_output, format + ".png")); }
    }
    private static void Negatives(RenderingDevice device)
    {
        Check(!device.TextureIsFormatSupportedForUsage(DataFormat.D16UNORMS8UINT, Roles), "Unsupported D16/S8 remains unavailable.");
        using var view = new RDTextureView(); using var invalid = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.D32SFLOAT, UsageBits = Roles };
        Reject<ArgumentException>(() => device.TextureCreate(invalid, view, [new byte[63]])); invalid.TextureType = TextureType.TextureArray; invalid.ArrayLayers = 2; Reject<ArgumentException>(() => device.TextureCreate(invalid, view, [new byte[64]])); invalid.TextureType = TextureType.Texture; invalid.ArrayLayers = 1;
        var source = Image(device, DataFormat.D32SFLOAT); var data = Payload(DataFormat.D32SFLOAT, 1);
        foreach (var bad in new[] { float.NaN, float.PositiveInfinity, -.001f, 1.001f }) { BinaryPrimitives.WriteSingleLittleEndian(data, bad); Reject<ArgumentOutOfRangeException>(() => device.TextureUpdate(source, 0, data)); }
        Check(device.TextureGetData(source, 0).All(x => x == 0), "Invalid depth values do not mutate existing native pixels.");
        var other = Image(device, DataFormat.D16UNORM); Reject<ArgumentException>(() => device.TextureCopy(source, other, Vector3.Zero, Vector3.Zero, new(1, 1, 1), 0, 0, 0, 0));
        using var colorInfo = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo }; var color = device.TextureCreate(colorInfo, view); Reject<ArgumentException>(() => device.TextureCopy(source, color, Vector3.Zero, Vector3.Zero, new(1, 1, 1), 0, 0, 0, 0));
        Reject<ArgumentException>(() => device.TextureCopy(source, source, Vector3.Zero, Vector3.Zero, new(1, 1, 1), 0, 0, 0, 0));
        Reject<ArgumentOutOfRangeException>(() => device.TextureCopy(source, other, new(4, 0, 0), Vector3.Zero, new(1, 1, 1), 0, 0, 0, 0));
        Reject<NotSupportedException>(() => device.TextureClear(source, Colors.Red, 0, 1, 0, 1)); Reject<ArgumentException>(() => device.TextureGetData(source, 0, new byte[83].AsSpan()));
        var noUpdate = Image(device, DataFormat.D32SFLOAT, usage: TextureUsageBits.DepthStencilAttachment | TextureUsageBits.CanCopyFrom); Reject<InvalidOperationException>(() => device.TextureUpdate(noUpdate, 0, new byte[84]));
        var noRead = Image(device, DataFormat.D32SFLOAT, usage: TextureUsageBits.DepthStencilAttachment | TextureUsageBits.CanUpdate); Reject<InvalidOperationException>(() => device.TextureGetData(noRead, 0)); Reject<InvalidOperationException>(() => device.TextureGetDataAsync(noRead, 0, _ => { }));
        Reject<InvalidOperationException>(() => device.TextureCopy(source, noUpdate, Vector3.Zero, Vector3.Zero, new(1, 1, 1), 0, 0, 0, 0));
        var list = device.ComputeListBegin(); try { Reject<InvalidOperationException>(() => device.TextureUpdate(source, 0, new byte[84])); Reject<InvalidOperationException>(() => device.TextureGetData(source, 0)); } finally { device.ComputeListEnd(); }
        using var separate = RenderingServer.CreateLocalRenderingDevice(); var cancel = Image(separate, DataFormat.D32SFLOAT); var invoked = false; separate.TextureGetDataAsync(cancel, 0, _ => invoked = true); separate.Dispose(); Check(!invoked, "Disposing a device cancels pending depth readback callbacks.");
    }
}
