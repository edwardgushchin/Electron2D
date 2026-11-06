using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class StencilTexturesTests
{
    private static void Inputs(RenderingDevice device, ShaderLanguage language, TextureSamples samples, bool sampling)
    {
        var count = 1 << (int)samples;
        var fragment = language == ShaderLanguage.GLSL
            ? $"#version 450\nlayout(input_attachment_index=1,set=4,binding=3)uniform usubpassInput{(count == 1 ? "" : "MS")} source;layout(location=0)out uint value;void main(){{value=subpassLoad(source{(count == 1 ? "" : "," + (count - 1))}).r;}}"
            : $"[[vk::input_attachment_index(1)]][[vk::binding(3,4)]]SubpassInput{(count == 1 ? "" : "MS")}<uint> source;uint main():SV_Target0{{return source.SubpassLoad({(count == 1 ? "" : count - 1)});}}";
        var shader = Shader(device, "input-" + count, language, fragment);
        var source = Image(device, DataFormat.S8UINT, TextureUsageBits.DepthStencilAttachment | TextureUsageBits.InputAttachment | (sampling ? TextureUsageBits.Sampling : 0), samples, 3);
        var output = Image(device, DataFormat.R32UINT, TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom, layers: 3);
        using var view = new RDTextureView(); var slices = new RID[3]; var clears = new RID[3];
        for (uint i = 0; i < 3; i++)
        {
            slices[i] = device.TextureCreateSharedFromSlice(view, source, i, 0); clears[i] = device.FramebufferCreate([slices[i]]);
            device.DrawListBegin(clears[i], DrawFlags.ClearStencil, clearStencilValue: 71 + (int)i); device.DrawListEnd();
        }
        using var pass = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [RDFramebufferPass.AttachmentUnused, 0] };
        var target = device.FramebufferCreateMultipass([source, output], [pass], viewCount: 2);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        using var uniform = new RDUniform { Binding = 3, UniformType = UniformType.InputAttachment }; uniform.AddID(source); var set = device.UniformSetCreate([uniform], shader, 4);
        var bytes = new byte[64];
        void Draw()
        {
            var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 4); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(output, 1, bytes.AsSpan());
        }
        Draw();
        Check(MemoryMarshal.Cast<byte, uint>(device.TextureGetData(output, 0)).ToArray().All(v => v == 71) && MemoryMarshal.Cast<byte, uint>(bytes).ToArray().All(v => v == 72), "Unsigned stencil inputs select the corresponding multiview layer.");
        Check(device.TextureGetData(output, 2).All(v => v == 0), "Input snapshots leave extra output layers untouched.");
        for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw();
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Prepared stencil input snapshots/draw/span-readback allocate zero owner managed bytes.");
        device.DrawListBegin(clears[1], DrawFlags.ClearStencil, clearStencilValue: 203); device.DrawListEnd(); Draw();
        Check(MemoryMarshal.Cast<byte, uint>(bytes).ToArray().All(v => v == 203), "Stencil input snapshots refresh after writes.");
        using var feedback = new RDFramebufferPass { DepthAttachment = 0, InputAttachments = [1] };
        Reject<ArgumentException>(() => device.FramebufferCreateMultipass([source, slices[0]], [feedback]));
        var disjoint = device.FramebufferCreateMultipass([source, slices[1]], [feedback]); Check(device.FramebufferIsValid(disjoint), "Disjoint stencil input/output layers are legal."); device.FreeRID(disjoint);
        var queued = new byte[64]; var calls = 0; device.TextureGetDataAsync(output, 1, queued.AsMemory(), () => calls++);
        device.DrawListBegin(clears[1], DrawFlags.ClearStencil, clearStencilValue: 91); device.DrawListEnd(); Draw(); device.Submit(); device.Sync();
        Check(calls == 1 && MemoryMarshal.Cast<byte, uint>(queued).ToArray().All(v => v == 203), "Queued output retains request-time stencil input values.");
        var writerShader = Shader(device, "input-writer", language, language == ShaderLanguage.GLSL ? "#version 450\nvoid main(){}" : "void main(){}");
        using var first = new RDFramebufferPass { DepthAttachment = 0 }; using var noColors = new RDPipelineColorBlendState();
        using var state = new RDPipelineDepthStencilState { EnableStencil = true, FrontOpCompare = CompareOperator.Always, BackOpCompare = CompareOperator.Always, FrontOpPass = StencilOperation.Replace, BackOpPass = StencilOperation.Replace, FrontOpWriteMask = 255, BackOpWriteMask = 255, FrontOpReference = 119, BackOpReference = 119 };
        using var multisample = new RDPipelineMultisampleState { SampleCount = samples };
        var ordered = device.FramebufferCreateMultipass([source, output], [first, pass], viewCount: 2); var layout = device.FramebufferGetFormat(ordered);
        var writer = device.RenderPipelineCreate(writerShader, layout, -1, RenderPrimitive.Triangles, raster, noColors, depthStencilState: state, multisampleState: multisample);
        var reader = device.RenderPipelineCreate(shader, layout, -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1);
        var orderedList = device.DrawListBegin(ordered); device.DrawListBindRenderPipeline(orderedList, writer); device.DrawListDraw(orderedList, false, 1, 3); device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(orderedList, reader); device.DrawListBindUniformSet(orderedList, set, 4); device.DrawListDraw(orderedList, false, 1, 3); device.DrawListEnd();
        Check(MemoryMarshal.Cast<byte, uint>(device.TextureGetData(output, 1)).ToArray().All(v => v == 119), "Ordered stencil writes become exact unsigned inputs in the following pass/view.");
        foreach (var rid in new[] { ordered, writer, reader, writerShader }) device.FreeRID(rid);
        device.FreeRID(source); Check(!device.FramebufferIsValid(target), "Retiring stencil input owners invalidates borrowing framebuffers.");
        foreach (var rid in new[] { target, pipeline, set, output, shader }) device.FreeRID(rid); foreach (var clear in clears) device.FreeRID(clear);
    }
    private static void Operations(RenderingDevice device, ShaderLanguage language)
    {
        var shader = Shader(device, "operations", language, language == ShaderLanguage.GLSL ? "#version 450\nvoid main(){}" : "void main(){}");
        var image = Image(device, DataFormat.S8UINT, Roles | Transfers); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var blend = new RDPipelineColorBlendState();
        using var state = new RDPipelineDepthStencilState { EnableStencil = true, FrontOpWriteMask = 255, BackOpWriteMask = 255, FrontOpReference = 93, BackOpReference = 93 };
        foreach (var op in Enum.GetValues<StencilOperation>().Where(v => v < StencilOperation.Max)) foreach (var initial in new[] { 0, 17, 255 }) foreach (var pass in new[] { false, true })
                {
                    state.FrontOpCompare = state.BackOpCompare = pass ? CompareOperator.Always : CompareOperator.Never;
                    state.FrontOpPass = state.BackOpPass = pass ? op : StencilOperation.Keep; state.FrontOpFail = state.BackOpFail = pass ? StencilOperation.Keep : op;
                    var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state);
                    var list = device.DrawListBegin(target, DrawFlags.ClearStencil, clearStencilValue: initial); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
                    var expected = op switch { StencilOperation.Keep => initial, StencilOperation.Zero => 0, StencilOperation.Replace => 93, StencilOperation.IncrementAndClamp => Math.Min(initial + 1, 255), StencilOperation.DecrementAndClamp => Math.Max(initial - 1, 0), StencilOperation.Invert => initial ^ 255, StencilOperation.IncrementAndWrap => (initial + 1) & 255, StencilOperation.DecrementAndWrap => (initial - 1) & 255, _ => throw new InvalidOperationException() };
                    Check(device.TextureGetData(image, 0).All(v => v == expected), "Every stencil-only pass/fail operation preserves saturation and wrapping semantics."); device.FreeRID(pipeline);
                }
        state.EnableDepthWrite = true; Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state)); state.EnableDepthWrite = false;
        state.EnableDepthRange = true; Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state));
        foreach (var rid in new[] { target, image, shader }) device.FreeRID(rid);
    }
}
