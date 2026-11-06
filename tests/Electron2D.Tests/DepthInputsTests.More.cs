using System.Buffers.Binary;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class DepthInputsTests
{
    private static RID Pipeline(RenderingDevice device, RID shader, RID framebuffer)
    {
        using var raster = new RDPipelineRasterizationState(); using var color = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [color] };
        return device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
    }
    private static RID InputSet(RenderingDevice device, RID shader, RID input)
    { using var uniform = new RDUniform { Binding = 3, UniformType = UniformType.InputAttachment }; uniform.AddID(input); return device.UniformSetCreate([uniform], shader, 4); }
    private static void Draw(RenderingDevice device, RID framebuffer, RID pipeline, RID set)
    {
        var list = device.DrawListBegin(framebuffer); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 4); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
    }
    private static void Constant(byte[] data, float expected, string message)
    { for (var i = 0; i < 16; i++) Check(Math.Abs(BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(i * 4)) - expected) < .00002f, message); }
    private static void Layered(RenderingDevice device, ShaderLanguage language, TextureSamples samples)
    {
        using var description = new RDTextureFormat { Width = 4, Height = 4, TextureType = TextureType.TextureArray, ArrayLayers = 3, Format = DataFormat.D32SFLOATS8UINT, Samples = samples, UsageBits = Copies | TextureUsageBits.Sampling | TextureUsageBits.InputAttachment | TextureUsageBits.DepthStencilAttachment };
        using var view = new RDTextureView(); var source = device.TextureCreate(description, view); var slices = new RID[3]; var depthFrames = new RID[3];
        void Clear(int layer, float depth, int stencil)
        { device.DrawListBegin(depthFrames[layer], DrawFlags.ClearDepth | DrawFlags.ClearStencil, clearDepthValue: depth, clearStencilValue: stencil); device.DrawListEnd(); }
        for (uint layer = 0; layer < 3; layer++) { slices[layer] = device.TextureCreateSharedFromSlice(view, source, layer, 0); depthFrames[layer] = device.FramebufferCreate([slices[layer]]); Clear((int)layer, .25f + layer / 8f, 41 + (int)layer); }
        description.Format = DataFormat.R32SFLOAT; description.Samples = TextureSamples.Samples1; description.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom; var output = device.TextureCreate(description, view);
        if (samples == TextureSamples.Samples1)
        {
            using var feedback = new RDFramebufferPass { DepthAttachment = 0, InputAttachments = [1] };
            Reject<ArgumentException>(() => device.FramebufferCreateMultipass([source, slices[0]], [feedback]));
            var disjoint = device.FramebufferCreateMultipass([source, slices[1]], [feedback]); Check(device.FramebufferIsValid(disjoint), "Disjoint physical depth input/output layers remain legal."); device.FreeRID(disjoint);
        }
        using var pass = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [RDFramebufferPass.AttachmentUnused, 0] };
        var framebuffer = device.FramebufferCreateMultipass([source, output], [pass], viewCount: 2); var shader = Reader(device, language, samples); var pipeline = Pipeline(device, shader, framebuffer); var set = InputSet(device, shader, source);
        Draw(device, framebuffer, pipeline, set); Constant(device.TextureGetData(output, 0), .25f, "View zero reads layer zero depth."); Constant(device.TextureGetData(output, 1), .375f, "View one reads its own depth."); Constant(device.TextureGetData(output, 2), 0, "Extra output layers remain untouched.");
        var bytes = new byte[64]; void Cycle() { Draw(device, framebuffer, pipeline, set); device.TextureGetData(output, 0, bytes.AsSpan()); }
        for (var i = 0; i < 16; i++) Cycle(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Cycle(); Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Layered input snapshots and readback allocate zero owner managed bytes.");
        Clear(1, .875f, 91); Draw(device, framebuffer, pipeline, set); Constant(device.TextureGetData(output, 1), .875f, "Input snapshots refresh after depth writes.");
        var outputSlice = device.TextureCreateSharedFromSlice(view, output, 2, 0); var slicedFrame = device.FramebufferCreateMultipass([slices[1], outputSlice], [pass]); var slicedPipeline = Pipeline(device, shader, slicedFrame); var slicedSet = InputSet(device, shader, slices[1]);
        Draw(device, slicedFrame, slicedPipeline, slicedSet); Constant(device.TextureGetData(output, 2), .875f, "A shared input and output address their original physical layers.");
        var nested = device.TextureCreateSharedFromSlice(view, slices[1], 0, 0); var nestedFrame = device.FramebufferCreateMultipass([nested, outputSlice], [pass]); var nestedSet = InputSet(device, shader, nested);
        Draw(device, nestedFrame, slicedPipeline, nestedSet); Constant(device.TextureGetData(output, 2), .25f, "Nested factories select original-owner coordinates.");
        var snapshot = new byte[64]; var completed = 0; device.TextureGetDataAsync(output, 2, snapshot.AsMemory(), () => completed++);
        Clear(0, .625f, 63); Draw(device, nestedFrame, slicedPipeline, nestedSet); device.Submit(); device.Sync(); Check(completed == 1, "Queued input-output snapshot completes once."); Constant(snapshot, .25f, "Queued output preserves request-time input contents.");
        if (samples == TextureSamples.Samples1) { var sourceBytes = device.TextureGetData(source, 1); Check(sourceBytes[4] == 91 && BinaryPrimitives.ReadSingleLittleEndian(sourceBytes) == .875f, "Depth input snapshots preserve the original stencil and depth."); }
        Draw(device, nestedFrame, slicedPipeline, nestedSet); device.FreeRID(source); Constant(device.TextureGetData(output, 2), .625f, "Recorded reads survive source retirement."); Check(!device.FramebufferIsValid(framebuffer) && !device.FramebufferIsValid(slicedFrame), "Retiring a depth source invalidates its borrowing framebuffers.");
        device.FreeRID(output); device.FreeRID(framebuffer); device.FreeRID(slicedFrame); device.FreeRID(nestedFrame); device.FreeRID(shader); device.FreeRID(pipeline); device.FreeRID(slicedPipeline); device.FreeRID(set); device.FreeRID(slicedSet); device.FreeRID(nestedSet); foreach (var depthFrame in depthFrames) device.FreeRID(depthFrame);
    }
    private static void InputOnlyAndArrays(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.D16UNORM, UsageBits = TextureUsageBits.InputAttachment | TextureUsageBits.CanUpdate };
        using var view = new RDTextureView(); var data = new byte[32]; for (var i = 0; i < 16; i++) BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(i * 2), 16384);
        var source = device.TextureCreate(format, view, [data]); using var captured = device.TextureGetFormat(source); Check(captured.UsageBits == format.UsageBits, "Input-only depth does not gain public render/copy permissions.");
        Reject<ArgumentException>(() => device.FramebufferCreate([source])); Reject<InvalidOperationException>(() => device.TextureGetData(source, 0));
        format.Format = DataFormat.D32SFLOATS8UINT; format.UsageBits = TextureUsageBits.InputAttachment | TextureUsageBits.Sampling | TextureUsageBits.CanUpdate; var otherData = new byte[128];
        for (var i = 0; i < 16; i++) { BinaryPrimitives.WriteSingleLittleEndian(otherData.AsSpan(i * 8), .75f); otherData[i * 8 + 4] = 71; }
        var other = device.TextureCreate(format, view, [otherData]);
        var fragment = "#version 450\nlayout(input_attachment_index=0,set=3,binding=9)uniform subpassInput inputs[2];layout(push_constant)uniform P{int pick;}p;layout(location=0)out float value;void main(){value=p.pick==0?subpassLoad(inputs[0]).r:subpassLoad(inputs[1]).r;}";
        var shader = Shader(device, "input-array", Vertex(ShaderLanguage.GLSL), fragment, ShaderLanguage.GLSL);
        format.Format = DataFormat.R32SFLOAT; format.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom; var output = device.TextureCreate(format, view);
        using var pass = new RDFramebufferPass { ColorAttachments = [2], InputAttachments = [0, 1] }; var framebuffer = device.FramebufferCreateMultipass([source, other, output], [pass]); var pipeline = Pipeline(device, shader, framebuffer);
        using var input = new RDUniform { Binding = 9, UniformType = UniformType.InputAttachment }; input.AddID(source); input.AddID(other); var set = device.UniformSetCreate([input], shader, 3);
        void Execute(int pick)
        {
            var list = device.DrawListBegin(framebuffer); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 3); Span<byte> push = stackalloc byte[4]; BinaryPrimitives.WriteInt32LittleEndian(push, pick); device.DrawListSetPushConstant(list, push, 4); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        }
        Execute(0); Constant(device.TextureGetData(output, 0), 16384f / 65535, "A descriptor-array depth input preserves normalized lanes."); Execute(1); Constant(device.TextureGetData(output, 0), .75f, "A descriptor-array depth input preserves floating lanes.");
        for (var i = 0; i < 16; i++) BinaryPrimitives.WriteSingleLittleEndian(otherData.AsSpan(i * 8), .5f); device.TextureUpdate(other, 0, otherData); Execute(1); Constant(device.TextureGetData(output, 0), .5f, "CPU updates feed later depth input snapshots.");
        Reject<InvalidOperationException>(() => Task.Run(() => device.TextureUpdate(other, 0, otherData)).GetAwaiter().GetResult());
        var listID = device.DrawListBegin(framebuffer); Reject<InvalidOperationException>(() => device.TextureUpdate(other, 0, otherData)); device.DrawListEnd();
        device.FreeRID(source); device.FreeRID(other); device.FreeRID(output); device.FreeRID(framebuffer); device.FreeRID(shader); device.FreeRID(pipeline); device.FreeRID(set);
        format.Format = DataFormat.D32SFLOAT; format.Samples = TextureSamples.Samples4; format.UsageBits = TextureUsageBits.DepthStencilAttachment | TextureUsageBits.CanCopyFrom; var producer = device.TextureCreate(format, view); var producerFrame = device.FramebufferCreate([producer]);
        device.DrawListBegin(producerFrame, DrawFlags.ClearDepth, clearDepthValue: .625f); device.DrawListEnd(); format.UsageBits = TextureUsageBits.InputAttachment | TextureUsageBits.CanCopyTo; var copied = device.TextureCreate(format, view);
        device.TextureCopy(producer, copied, default, default, new Vector3(4, 4, 1), 0, 0, 0, 0); format.Samples = TextureSamples.Samples1; format.Format = DataFormat.R32SFLOAT; format.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom; var copiedOutput = device.TextureCreate(format, view);
        using var copiedPass = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [RDFramebufferPass.AttachmentUnused, 0] }; var copiedFrame = device.FramebufferCreateMultipass([copied, copiedOutput], [copiedPass]); var copiedShader = Reader(device, ShaderLanguage.GLSL, TextureSamples.Samples4); var copiedPipeline = Pipeline(device, copiedShader, copiedFrame); var copiedSet = InputSet(device, copiedShader, copied);
        Draw(device, copiedFrame, copiedPipeline, copiedSet); Constant(device.TextureGetData(copiedOutput, 0), .625f, "Input-only MS depth accepts native copies without gaining a render role."); Reject<ArgumentException>(() => device.FramebufferCreate([copied]));
        device.FreeRID(producer); device.FreeRID(producerFrame); device.FreeRID(copied); device.FreeRID(copiedOutput); device.FreeRID(copiedFrame); device.FreeRID(copiedShader); device.FreeRID(copiedPipeline); device.FreeRID(copiedSet);
    }
    private static void ColorInputs(RenderingDevice device)
    {
        foreach (var encoding in new[] { DataFormat.R8G8B8A8UNORM, DataFormat.R8G8B8A8SRGB })
            foreach (var sampling in new[] { false, true })
            {
                using var format = new RDTextureFormat { Width = 4, Height = 4, Format = encoding, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.InputAttachment | (sampling ? TextureUsageBits.Sampling : 0) }; using var view = new RDTextureView();
                Check(device.TextureIsFormatSupportedForUsage(encoding, format.UsageBits), "Readonly color inputs query sampled format capability."); var source = device.TextureCreate(format, view);
                format.Format = DataFormat.R32SFLOAT; format.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom; var output = device.TextureCreate(format, view);
                using var first = new RDFramebufferPass { ColorAttachments = [0] }; using var second = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [RDFramebufferPass.AttachmentUnused, 0] }; var framebuffer = device.FramebufferCreateMultipass([source, output], [first, second]);
                var shader = Reader(device, ShaderLanguage.GLSL, TextureSamples.Samples1); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
                var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1); var set = InputSet(device, shader, source);
                var list = device.DrawListBegin(framebuffer, DrawFlags.ClearColor0, [new Color(.25f, .25f, .25f, 1)]); device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 4); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
                var result = device.TextureGetData(output, 0); Check(Math.Abs(BinaryPrimitives.ReadSingleLittleEndian(result) - .25f) < .003f, "Linear/sRGB readonly input preserves its native sampled interpretation.");
                var alias = device.TextureCreateShared(view, source); using var aliasPass = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [RDFramebufferPass.AttachmentUnused, 0] }; var aliasFrame = device.FramebufferCreateMultipass([alias, output], [aliasPass]); var aliasPipeline = Pipeline(device, shader, aliasFrame); var aliasSet = InputSet(device, shader, alias);
                Draw(device, aliasFrame, aliasPipeline, aliasSet); Check(Math.Abs(BinaryPrimitives.ReadSingleLittleEndian(device.TextureGetData(output, 0)) - .25f) < .003f, "Readonly sRGB/linear shared views use the same physical sampled capability."); device.FreeRID(aliasFrame); device.FreeRID(aliasPipeline); device.FreeRID(aliasSet);
                device.FreeRID(source); device.FreeRID(output); device.FreeRID(framebuffer); device.FreeRID(shader); device.FreeRID(pipeline); device.FreeRID(set);
            }
    }
}
