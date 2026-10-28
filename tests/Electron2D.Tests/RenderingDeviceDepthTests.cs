using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;
using static Electron2D.RenderingDevice;

internal static class RenderingDeviceDepthTests
{
    private static RID Image(RenderingDevice device, DataFormat format, TextureUsageBits usage, uint layers = 1, uint mips = 1)
    {
        using var description = new RDTextureFormat { Width = 16, Height = 16, Format = format, UsageBits = usage, TextureType = layers == 1 ? TextureType.Texture : TextureType.TextureArray, ArrayLayers = layers, MIPMaps = mips };
        using var view = new RDTextureView(); return device.TextureCreate(description, view);
    }
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        ComputeArrays(device);
        foreach (var language in new[] { "GLSL", "HLSL" }) Exercise(device, language);
        Canvas();
        Console.WriteLine("Device depth: native occlusion, two-sided stencil operations, comparison sampling, layer/mip initialization, copied state and zero allocation passed on " + device.GetDeviceName());
    }
    private static void Exercise(RenderingDevice device, string language)
    {
        var depthUsage = TextureUsageBits.DepthStencilAttachment | TextureUsageBits.Sampling;
        Check(device.TextureIsFormatSupportedForUsage(DataFormat.D16UNORM, depthUsage), "The native minimum depth format is executable.");
        Check(!device.TextureIsFormatSupportedForUsage(DataFormat.D16UNORMS8UINT, depthUsage), "No native packed D16/S8 adapter is claimed.");
        Check(!device.TextureIsFormatSupportedForUsage(DataFormat.D32SFLOAT, depthUsage | TextureUsageBits.CanUpdate), "Raw depth transfers are not advertised.");
        Check(!device.TextureIsFormatSupportedForUsage(DataFormat.R8G8B8A8UNORM, depthUsage), "Color formats cannot pretend to own a depth aspect.");
        var stencilFormat = device.TextureIsFormatSupportedForUsage(DataFormat.D24UNORMS8UINT, depthUsage) ? DataFormat.D24UNORMS8UINT : DataFormat.D32SFLOATS8UINT;
        var depth = Image(device, stencilFormat, depthUsage);
        var color = Image(device, DataFormat.R8G8B8A8UNORM, TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom);
        var framebuffer = device.FramebufferCreate([depth, color]); var colorFramebuffer = device.FramebufferCreate([color]);
        Reject<ArgumentException>(() => device.FramebufferCreate([depth, depth]));
        using var depthAttachment = new RDAttachmentFormat { Format = stencilFormat, UsageFlags = depthUsage };
        using var colorAttachment = new RDAttachmentFormat { Format = DataFormat.R8G8B8A8UNORM, UsageFlags = TextureUsageBits.ColorAttachment };
        var layout = device.FramebufferFormatCreate([depthAttachment, colorAttachment]);
        Check(device.FramebufferGetFormat(device.FramebufferCreate([depth, color], layout)) == layout, "Explicit depth-first layouts preserve order and color slots.");
        Reject<ArgumentException>(() => device.FramebufferCreate([color, depth], layout));
        Reject<InvalidOperationException>(() => device.TextureGetData(depth, 0));
        Reject<InvalidOperationException>(() => device.TextureClear(depth, Colors.White, 0, 1, 0, 1));
        using var module = RenderingDeviceGraphicsTests.Load("DeviceDepth" + language); RenderingDeviceGraphicsTests.Export(module, "DeviceDepth" + language); var shader = device.ShaderCreateFromSPIRV(module);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        using var state = new RDPipelineDepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, DepthCompareOperator = CompareOperator.Less };
        Check(state.FrontOpPass == StencilOperation.Zero && (int)StencilOperation.Keep == 0, "Pinned stencil property defaults and enum ordinals remain distinct.");
        RID Pipeline(RDPipelineDepthStencilState s, RDPipelineRasterizationState? r = null, RID? target = null) => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target ?? framebuffer), -1, RenderPrimitive.Triangles, r ?? raster, blend, depthStencilState: s);
        var less = Pipeline(state); state.DepthCompareOperator = CompareOperator.Never;
        var never = Pipeline(state); state.EnableDepthWrite = false; state.DepthCompareOperator = CompareOperator.Always; var noWrite = Pipeline(state);
        state.EnableDepthTest = false; state.EnableDepthWrite = true; var writeWithoutTest = Pipeline(state);
        state.EnableDepthWrite = false; var plain = Pipeline(state);
        var uniform = device.UniformBufferCreate(32); using var descriptor = new RDUniform { Binding = 7, UniformType = UniformType.UniformBuffer }; descriptor.AddID(uniform); var set = device.UniformSetCreate([descriptor], shader, 5);
        var values = new float[8]; var bytes = new byte[16 * 16 * 4];
        void Parameters(Color tint, float z, bool reverse = false, float layer = 0, float compare = 0)
        { values[0] = tint.R; values[1] = tint.G; values[2] = tint.B; values[3] = tint.A; values[4] = z; values[5] = reverse ? 1 : 0; values[6] = layer; values[7] = compare; device.BufferUpdate(uniform, 0, 32, MemoryMarshal.AsBytes(values.AsSpan())); }
        long Begin(bool clear = true, float z = 1, int stencil = 0) => device.DrawListBegin(framebuffer, clear ? DrawFlags.ClearAll : 0, clear ? [Colors.Black] : default, clearDepthValue: z, clearStencilValue: stencil);
        void Draw(long list, RID pipeline) { device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 5); device.DrawListDraw(list, false, 1, 3); }
        void Finish() { device.DrawListEnd(); device.TextureGetData(color, 0, bytes.AsSpan()); }
        void Pixel(int red, int green, int blue, string message) { var i = (8 * 16 + 8) * 4; Check(Math.Abs(bytes[i] - red) <= 1 && Math.Abs(bytes[i + 1] - green) <= 1 && Math.Abs(bytes[i + 2] - blue) <= 1, language + ": " + message + " actual=" + bytes[i] + "," + bytes[i + 1] + "," + bytes[i + 2]); }
        Parameters(Colors.Red, .25f); var list = Begin(); Draw(list, less); Finish(); Pixel(255, 0, 0, "near depth writes pass");
        Parameters(Colors.Blue, .75f); list = Begin(false); Draw(list, less); Finish(); Pixel(255, 0, 0, "far depth rejects and preserves prior color/depth");
        list = Begin(); Draw(list, less); Finish(); Pixel(0, 0, 255, "depth clear restores the far draw");
        Parameters(Colors.Red, .1f); list = Begin(false); Draw(list, never); Finish(); Pixel(0, 0, 255, "copied never comparison rejects");
        list = Begin(); Draw(list, noWrite); Finish(); Parameters(Colors.Blue, .75f); list = Begin(false); Draw(list, less); Finish(); Pixel(0, 0, 255, "disabled depth writes preserve cleared depth");
        Parameters(Colors.Red, .1f); list = Begin(); Draw(list, writeWithoutTest); Finish(); Parameters(Colors.Blue, .75f); list = Begin(false); Draw(list, less); Finish(); Pixel(0, 0, 255, "depth writes require the depth test");
        using var enabled = new RDPipelineDepthStencilState { EnableDepthTest = true };
        Reject<ArgumentException>(() => Pipeline(enabled, target: colorFramebuffer));
        enabled.DepthCompareOperator = CompareOperator.Max; Reject<ArgumentOutOfRangeException>(() => Pipeline(enabled)); enabled.DepthCompareOperator = CompareOperator.Always;
        Reject<ArgumentOutOfRangeException>(() => device.DrawListBegin(framebuffer, clearDepthValue: float.NaN));
        Reject<ArgumentOutOfRangeException>(() => device.DrawListBegin(framebuffer, clearStencilValue: 256));
        Reject<ArgumentException>(() => device.DrawListBegin(framebuffer, DrawFlags.ClearDepth | DrawFlags.IgnoreDepth));
        // Verify stencil via a following equality-gated color draw rather than unsupported raw aspect readback.
        int Result(StencilOperation op, int initial, int reference) => op switch { StencilOperation.Keep => initial, StencilOperation.Zero => 0, StencilOperation.Replace => reference, StencilOperation.IncrementAndClamp => Math.Min(255, initial + 1), StencilOperation.DecrementAndClamp => Math.Max(0, initial - 1), StencilOperation.Invert => initial ^ 255, StencilOperation.IncrementAndWrap => (initial + 1) & 255, _ => (initial - 1) & 255 };
        RDPipelineDepthStencilState StencilState(int reference, CompareOperator compare = CompareOperator.Always) => new() { EnableStencil = true, FrontOpCompare = compare, BackOpCompare = compare, FrontOpReference = reference, BackOpReference = reference, FrontOpCompareMask = -1, BackOpCompareMask = 255, FrontOpWriteMask = -1, BackOpWriteMask = 255, FrontOpPass = StencilOperation.Keep, BackOpPass = StencilOperation.Keep, FrontOpFail = StencilOperation.Keep, BackOpFail = StencilOperation.Keep, FrontOpDepthFail = StencilOperation.Keep, BackOpDepthFail = StencilOperation.Keep };
        for (var operation = 0; operation < 8; operation++)
            foreach (var reverse in new[] { false, true })
                foreach (var outcome in new[] { 0, 1, 2 })
                {
                    var op = (StencilOperation)operation; var initial = operation is 3 or 6 ? 255 : operation is 4 or 7 ? 0 : 42; const int reference = 7;
                    using var write = StencilState(reference, outcome == 1 ? CompareOperator.Never : CompareOperator.Always);
                    write.EnableDepthTest = outcome == 2; write.DepthCompareOperator = CompareOperator.Never;
                    if (outcome == 0) write.FrontOpPass = write.BackOpPass = op; else if (outcome == 1) write.FrontOpFail = write.BackOpFail = op; else write.FrontOpDepthFail = write.BackOpDepthFail = op;
                    using var read = StencilState(Result(op, initial, reference), CompareOperator.Equal); var writer = Pipeline(write); var reader = Pipeline(read);
                    Parameters(Colors.Red, .5f, reverse); list = Begin(stencil: initial); Draw(list, writer); device.DrawListEnd();
                    Parameters(Colors.Blue, .5f, reverse); list = Begin(false); Draw(list, reader); Finish(); Pixel(0, 0, 255, $"stencil {op} outcome={outcome} reverse={reverse}");
                    device.FreeRID(writer); device.FreeRID(reader);
                }
        foreach (var reverse in new[] { false, true })
        {
            using var distinct = StencilState(7); distinct.FrontOpPass = StencilOperation.Replace; distinct.BackOpPass = StencilOperation.Invert;
            using var readerState = StencilState(reverse ? 7 : 253, CompareOperator.Equal);
            var writer = Pipeline(distinct); var reader = Pipeline(readerState);
            Parameters(Colors.Red, .5f, reverse); list = Begin(stencil: 2); Draw(list, writer); device.DrawListEnd();
            Parameters(Colors.Blue, .5f, reverse); list = Begin(false); Draw(list, reader); Finish(); Pixel(0, 0, 255, "distinct front/back operations reverse=" + reverse);
            device.FreeRID(writer); device.FreeRID(reader);
        }
        using (var masked = StencilState(10))
        using (var maskedRead = StencilState(250, CompareOperator.Equal))
        {
            masked.FrontOpPass = masked.BackOpPass = StencilOperation.Replace; masked.FrontOpWriteMask = masked.BackOpWriteMask = 15;
            var writer = Pipeline(masked); var reader = Pipeline(maskedRead);
            Parameters(Colors.Red, .5f); list = Begin(stencil: 240); Draw(list, writer); device.DrawListEnd();
            Parameters(Colors.Blue, .5f); list = Begin(false); Draw(list, reader); Finish(); Pixel(0, 0, 255, "stencil write masks preserve untouched bits");
        }
        for (var comparisonOperator = 0; comparisonOperator < 8; comparisonOperator++)
        {
            using var comparisonState = new RDPipelineDepthStencilState { EnableDepthTest = true, DepthCompareOperator = (CompareOperator)comparisonOperator };
            var comparisonPipeline = Pipeline(comparisonState);
            foreach (var incoming in new[] { .25f, .5f, .75f })
            {
                var pass = (CompareOperator)comparisonOperator switch { CompareOperator.Never => false, CompareOperator.Less => incoming < .5f, CompareOperator.Equal => incoming == .5f, CompareOperator.LessOrEqual => incoming <= .5f, CompareOperator.Greater => incoming > .5f, CompareOperator.NotEqual => incoming != .5f, CompareOperator.GreaterOrEqual => incoming >= .5f, _ => true };
                Parameters(Colors.Blue, incoming); list = Begin(z: .5f); Draw(list, comparisonPipeline); Finish(); Pixel(0, 0, pass ? 255 : 0, "depth comparison " + comparisonOperator + " incoming=" + incoming);
            }
            device.FreeRID(comparisonPipeline);
        }
        using var bias = new RDPipelineRasterizationState { DepthBiasEnabled = true, DepthBiasConstantFactor = 1e8f };
        using var biasDepth = new RDPipelineDepthStencilState { EnableDepthTest = true, DepthCompareOperator = CompareOperator.Less };
        var biased = Pipeline(biasDepth, bias); Parameters(Colors.Blue, .25f); list = Begin(z: .5f); Draw(list, biased); Finish(); Pixel(0, 0, 0, "constant depth bias rejects an otherwise passing fragment");
        bias.DepthBiasEnabled = false; var unbiased = Pipeline(biasDepth, bias); list = Begin(z: .5f); Draw(list, unbiased); Finish(); Pixel(0, 0, 255, "disabled bias preserves incoming depth");
        bias.DepthBiasEnabled = true; bias.DepthBiasClamp = .001f; var clampedBias = Pipeline(biasDepth, bias); list = Begin(z: .5f); Draw(list, clampedBias); Finish(); Pixel(0, 0, 255, "bias clamp bounds a large constant offset");
        bias.DepthBiasConstantFactor = -1e8f; bias.DepthBiasClamp = 0; var negativeBias = Pipeline(biasDepth, bias); Parameters(Colors.Blue, .75f); list = Begin(z: .5f); Draw(list, negativeBias); Finish(); Pixel(0, 0, 255, "negative constant bias passes an otherwise rejected fragment");
        bias.DepthBiasClamp = -.001f; var negativeClamp = Pipeline(biasDepth, bias); list = Begin(z: .5f); Draw(list, negativeClamp); Finish(); Pixel(0, 0, 0, "negative bias clamp floors the bias");
        bias.DepthBiasConstantFactor = 0; bias.DepthBiasClamp = 0; bias.DepthBiasSlopeFactor = -2; var sloped = Pipeline(biasDepth, bias);
        Parameters(Colors.Blue, .5f, layer: .2f); list = Begin(z: .5f); Draw(list, sloped); Finish(); Pixel(0, 0, 255, "negative slope depth bias shifts the interpolated primitive");
        using var asymmetric = StencilState(7); asymmetric.BackOpReference = 8; device.FreeRID(Pipeline(asymmetric));
        using var culled = new RDPipelineRasterizationState { CullMode = PolygonCullMode.Front }; asymmetric.FrontOpReference = 999; asymmetric.FrontOpPass = asymmetric.BackOpPass = StencilOperation.Replace;
        var culledPipeline = Pipeline(asymmetric, culled); using var backRead = StencilState(8, CompareOperator.Equal); var backReader = Pipeline(backRead);
        Parameters(Colors.Red, .5f); list = Begin(); Draw(list, culledPipeline); device.DrawListEnd(); Parameters(Colors.Blue, .5f); list = Begin(false); Draw(list, backReader); Finish(); Pixel(0, 0, 255, "front culling applies back-face masks/reference"); device.FreeRID(culledPipeline); asymmetric.FrontOpReference = 7;
        asymmetric.BackOpReference = 7; asymmetric.BackOpCompareMask = 127; device.FreeRID(Pipeline(asymmetric)); asymmetric.BackOpCompareMask = 255; asymmetric.BackOpWriteMask = 127; device.FreeRID(Pipeline(asymmetric));
        using var invalidStencil = StencilState(256); Reject<ArgumentOutOfRangeException>(() => Pipeline(invalidStencil));
        using var shadowModule = RenderingDeviceGraphicsTests.Load("DeviceShadow" + language); RenderingDeviceGraphicsTests.Export(shadowModule, "DeviceShadow" + language); var shadow = device.ShaderCreateFromSPIRV(shadowModule);
        var shadowSet = device.UniformSetCreate([descriptor], shadow, 5);
        using var compareDescription = new RDSamplerState { EnableCompare = true, CompareOp = CompareOperator.Less }; var comparison = device.SamplerCreate(compareDescription); compareDescription.CompareOp = CompareOperator.Never;
        using var sampledDepth = new RDUniform { Binding = 13, UniformType = UniformType.SamplerWithTexture }; sampledDepth.AddID(comparison); sampledDepth.AddID(depth); var comparisonSet = device.UniformSetCreate([sampledDepth], shadow, 2);
        using var regularDescription = new RDSamplerState(); var regular = device.SamplerCreate(regularDescription); sampledDepth.ClearIDs(); sampledDepth.AddID(regular); sampledDepth.AddID(depth); Reject<ArgumentException>(() => device.UniformSetCreate([sampledDepth], shadow, 2));
        sampledDepth.ClearIDs(); sampledDepth.AddID(comparison); sampledDepth.AddID(color); Reject<ArgumentException>(() => device.UniformSetCreate([sampledDepth], shadow, 2));
        var shadowPipeline = device.RenderPipelineCreate(shadow, device.FramebufferGetFormat(colorFramebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        void Sample(float reference)
        {
            Parameters(Colors.White, 0, compare: reference); var sampleList = device.DrawListBegin(colorFramebuffer, DrawFlags.ClearColorAll, [Colors.Black]);
            device.DrawListBindRenderPipeline(sampleList, shadowPipeline); device.DrawListBindUniformSet(sampleList, shadowSet, 5); device.DrawListBindUniformSet(sampleList, comparisonSet, 2); device.DrawListDraw(sampleList, false, 1, 3); Finish();
        }
        list = Begin(z: .5f); device.DrawListEnd(); Sample(.25f); Pixel(255, 255, 255, "comparison sampler passes against stored depth"); Sample(.75f); Pixel(0, 0, 0, "comparison sampler rejects and copied sampler state is retained");
        var shadowFeedbackPipeline = device.RenderPipelineCreate(shadow, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        list = Begin(); device.DrawListBindRenderPipeline(list, shadowFeedbackPipeline); device.DrawListBindUniformSet(list, shadowSet, 5); device.DrawListBindUniformSet(list, comparisonSet, 2); Reject<InvalidOperationException>(() => device.DrawListDraw(list, false, 1, 3)); device.DrawListEnd();
        void Warm()
        { Parameters(Colors.Red, .25f); var warmList = device.DrawListBegin(framebuffer, DrawFlags.ClearDepth | DrawFlags.ClearStencil, clearDepthValue: 1); Draw(warmList, less); Finish(); }
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        for (var n = 0; n < 16; n++) Warm(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var n = 0; n < 32; n++) Warm(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine(language + " depth: 32 prepared draw/readback cycles, " + allocated + " managed bytes.");
        Check(allocated == 0, "Prepared depth/stencil recording and caller-owned color readback allocate zero managed bytes: " + allocated);
        foreach (var format in new[] { DataFormat.D16UNORM, DataFormat.X8D24UNORMPACK32, DataFormat.D32SFLOAT })
        {
            if (!device.TextureIsFormatSupportedForUsage(format, depthUsage)) continue;
            var image = Image(device, format, depthUsage); var target = device.FramebufferCreate([color, image]);
            var pipeline = Pipeline(enabled, target: target); Parameters(Colors.Blue, .5f);
            var formatList = device.DrawListBegin(target, DrawFlags.ClearAll, [Colors.Black]); Draw(formatList, pipeline); Finish(); Pixel(0, 0, 255, "native depth format " + format);
            device.FreeRID(pipeline); device.FreeRID(target); device.FreeRID(image);
        }
        if (language == "GLSL")
        {
            using var mismatchModule = RenderingDeviceGraphicsTests.Load("DeviceDepthMismatch"); RenderingDeviceGraphicsTests.Export(mismatchModule, "DeviceDepthMismatch"); Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(mismatchModule));
            using var depthOnlyModule = RenderingDeviceGraphicsTests.Load("DeviceDepthOnly"); RenderingDeviceGraphicsTests.Export(depthOnlyModule, "DeviceDepthOnly"); var depthOnlyShader = device.ShaderCreateFromSPIRV(depthOnlyModule);
            var depthOnlyTarget = device.FramebufferCreate([depth]); using var emptyBlend = new RDPipelineColorBlendState();
            using var depthOnlyState = new RDPipelineDepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, DepthCompareOperator = CompareOperator.Always };
            var depthOnlyPipeline = device.RenderPipelineCreate(depthOnlyShader, device.FramebufferGetFormat(depthOnlyTarget), -1, RenderPrimitive.Triangles, raster, emptyBlend, depthStencilState: depthOnlyState);
            var depthOnlyUniform = device.UniformSetCreate([descriptor], depthOnlyShader, 5);
            Parameters(Colors.White, .25f); var depthOnlyList = device.DrawListBegin(depthOnlyTarget, DrawFlags.ClearAll);
            device.DrawListBindRenderPipeline(depthOnlyList, depthOnlyPipeline); device.DrawListBindUniformSet(depthOnlyList, depthOnlyUniform, 5); device.DrawListDraw(depthOnlyList, false, 1, 3); device.DrawListEnd(); Sample(.5f); Pixel(0, 0, 0, "depth-only pass feeds comparison sampling without a color target");

            using var arrayModule = RenderingDeviceGraphicsTests.Load("DeviceDepthArray"); RenderingDeviceGraphicsTests.Export(arrayModule, "DeviceDepthArray"); var arrayShader = device.ShaderCreateFromSPIRV(arrayModule);
            var array = Image(device, DataFormat.D16UNORM, depthUsage, 2, 3); using var arrayDescriptor = new RDUniform { Binding = 13, UniformType = UniformType.SamplerWithTexture }; arrayDescriptor.AddID(regular); arrayDescriptor.AddID(array);
            var arraySet = device.UniformSetCreate([arrayDescriptor], arrayShader, 2); var arrayUniform = device.UniformSetCreate([descriptor], arrayShader, 5);
            var arrayPipeline = device.RenderPipelineCreate(arrayShader, device.FramebufferGetFormat(colorFramebuffer), -1, RenderPrimitive.Triangles, raster, blend);
            foreach (var layer in new[] { 0, 1 }) foreach (var mip in new[] { 0, 1, 2 })
                {
                    Parameters(Colors.White, 0, layer: layer, compare: mip); var arrayList = device.DrawListBegin(colorFramebuffer, DrawFlags.ClearColorAll, [Colors.White]);
                    device.DrawListBindRenderPipeline(arrayList, arrayPipeline); device.DrawListBindUniformSet(arrayList, arrayUniform, 5); device.DrawListBindUniformSet(arrayList, arraySet, 2); device.DrawListDraw(arrayList, false, 1, 3); Finish(); Pixel(0, 0, 0, $"depth array initialization layer={layer} mip={mip}");
                }
            arrayDescriptor.ClearIDs(); arrayDescriptor.AddID(comparison); arrayDescriptor.AddID(array); Reject<ArgumentException>(() => device.UniformSetCreate([arrayDescriptor], arrayShader, 2));
            device.FreeRID(array);
        }
        device.FreeRID(depth); Check(!device.FramebufferIsValid(framebuffer), "Freeing borrowed depth invalidates its framebuffer."); Reject<ArgumentException>(() => device.DrawListBegin(framebuffer));
    }
    private static void ComputeArrays(RenderingDevice device)
    {
        var depth = Image(device, DataFormat.D16UNORM, TextureUsageBits.DepthStencilAttachment | TextureUsageBits.Sampling, 2);
        using var samplerState = new RDSamplerState { EnableCompare = true, CompareOp = CompareOperator.Greater }; var sampler = device.SamplerCreate(samplerState);
        using var input = new RDUniform { Binding = 13, UniformType = UniformType.SamplerWithTexture }; input.AddID(sampler); input.AddID(depth);
        foreach (var language in new[] { "GLSL", "HLSL" })
        {
            using var stream = typeof(RenderingDeviceDepthTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalDepth" + language + ".comp.spv")!;
            using var memory = new MemoryStream(); stream.CopyTo(memory); using var module = new RDShaderSPIRV { BytecodeCompute = memory.ToArray() };
            var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var output = device.StorageBufferCreate(8);
            using var destination = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; destination.AddID(output);
            var inputs = device.UniformSetCreate([input], shader, 2); var outputs = device.UniformSetCreate([destination], shader, 3);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, inputs, 2); device.ComputeListBindUniformSet(list, outputs, 3); device.ComputeListDispatch(list, 2, 1, 1); device.ComputeListEnd();
            var data = MemoryMarshal.Cast<byte, float>(device.BufferGetData(output)); Check(data[0] == 1 && data[1] == 1, language + " compute comparison sampling reads both depth-array layers.");
            var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_DEPTH_OUTPUT");
            if (directory is not null)
            {
                var remap = typeof(RenderingDevice).GetMethod("Remap", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
                var result = remap.Invoke(null, [module.BytecodeCompute, ShaderStage.Compute])!; var code = (byte[])result.GetType().GetField("Item1")!.GetValue(result)!;
                File.WriteAllBytes(System.IO.Path.Combine(directory, "LocalDepth" + language + ".comp.spv"), code);
            }
        }
        device.FreeRID(depth);
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var texture = new TextureRD(); var root = new Window { Size = new(32, 32) }; var frames = 0;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!; RenderingServer.SetDefaultClearColor(Colors.Black);
            var color = Image(device, DataFormat.R8G8B8A8UNORM, TextureUsageBits.ColorAttachment | TextureUsageBits.Sampling);
            var depth = Image(device, DataFormat.D16UNORM, TextureUsageBits.DepthStencilAttachment); var target = device.FramebufferCreate([color, depth]);
            using var module = RenderingDeviceGraphicsTests.Load("DeviceDepthGLSL"); RenderingDeviceGraphicsTests.Export(module, "DeviceDepthGLSL"); var shader = device.ShaderCreateFromSPIRV(module);
            using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
            using var state = new RDPipelineDepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, DepthCompareOperator = CompareOperator.Less };
            var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state);
            float[] values = [1, 0, 0, 1, .25f, 0, 0, 0]; var uniform = device.UniformBufferCreate(32, MemoryMarshal.AsBytes(values.AsSpan()));
            using var descriptor = new RDUniform { Binding = 7, UniformType = UniformType.UniformBuffer }; descriptor.AddID(uniform); var set = device.UniformSetCreate([descriptor], shader, 5);
            var list = device.DrawListBegin(target, DrawFlags.ClearAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 5); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            values[0] = 0; values[2] = 1; values[4] = .75f; device.BufferUpdate(uniform, 0, 32, MemoryMarshal.AsBytes(values.AsSpan()));
            list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 5); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            texture.TextureRDRID = color; root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured;
        };
        void Captured()
        {
            if (++frames < 3) return;
            using var screenshot = root.GetTexture().GetImage()!; var pixel = screenshot.GetPixel(8, 8);
            Check(pixel.R > .99f && pixel.G < .01f && pixel.B < .01f, "Native depth occlusion survives direct retained texture consumption by rendered canvas.");
            var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_DEPTH_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); screenshot.SavePNG(System.IO.Path.Combine(directory, "canvas.png")); }
            root.Tree!.Quit();
        }
        Engine.Run(root);
    }

}
