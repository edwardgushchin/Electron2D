using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class StencilTexturesTests
{
    private static void Multisample(RenderingDevice device, ShaderLanguage language)
    {
        var fragment = language == ShaderLanguage.GLSL ? "#version 450\nvoid main(){if(gl_FragCoord.x>=2)discard;}" : "void main(float4 p:SV_Position){if(p.x>=2)discard;}";
        var shader = Shader(device, "masked-writer", language, fragment);
        using var raster = new RDPipelineRasterizationState(); using var blend = new RDPipelineColorBlendState(); using var view = new RDTextureView();
        using var state = new RDPipelineDepthStencilState { EnableStencil = true, FrontOpCompare = CompareOperator.Always, BackOpCompare = CompareOperator.Always, FrontOpPass = StencilOperation.Replace, BackOpPass = StencilOperation.Replace, FrontOpWriteMask = 255, BackOpWriteMask = 255 };
        foreach (var samples in new[] { TextureSamples.Samples2, TextureSamples.Samples4, TextureSamples.Samples8 })
        {
            var count = 1 << (int)samples;
            var source = Image(device, DataFormat.S8UINT, Roles | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo, samples, 2);
            var selected = device.TextureCreateSharedFromSlice(view, source, 1, 0); var target = device.FramebufferCreate([selected]); var layout = device.FramebufferGetFormat(target);
            var pipelines = new RID[count];
            using var sampleState = new RDPipelineMultisampleState { SampleCount = samples };
            for (var sample = 0; sample < count; sample++)
            {
                var masks = new uint[count]; masks[0] = 1u << sample; sampleState.SampleMasks = masks; state.FrontOpReference = state.BackOpReference = 40 + sample;
                pipelines[sample] = device.RenderPipelineCreate(shader, layout, -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state, multisampleState: sampleState);
            }
            sampleState.SampleMasks = new uint[count]; state.FrontOpReference = state.BackOpReference = 0;
            var list = device.DrawListBegin(target, DrawFlags.ClearStencil, clearStencilValue: 17);
            foreach (var pipeline in pipelines) { device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); }
            device.DrawListEnd();
            for (var sample = 0; sample < count; sample++)
            {
                var expected = Enumerable.Range(0, 16).Select(i => (byte)(i % 4 < 2 ? 40 + sample : 17)).ToArray(); Sample(device, language, selected, expected, sample);
            }
            device.TextureCopy(selected, selected, new(0, 0, 0), new(2, 0, 0), new(2, 4, 1), 0, 0, 0, 0);
            for (var sample = 0; sample < count; sample++) Sample(device, language, selected, Enumerable.Repeat((byte)(40 + sample), 16).ToArray(), sample);
            Reject<NotSupportedException>(() => device.TextureGetData(selected, 0));
            var resolved = Image(device, DataFormat.S8UINT, TextureUsageBits.DepthResolveAttachment | TextureUsageBits.Sampling | Transfers);
            device.TextureResolveMultisample(selected, resolved); Check(device.TextureGetData(resolved, 0).All(value => value == 40), "Stencil-only explicit resolve selects sample zero and preserves exact integer bytes.");
            var queued = new byte[16]; var calls = 0; device.TextureGetDataAsync(resolved, 0, queued.AsMemory(), () => calls++); device.TextureUpdate(resolved, 0, new byte[16]); device.Submit(); device.Sync(); Check(calls == 1 && queued.All(value => value == 40), "Queued stencil resolve output retains its request-time bytes.");
            var plain = Image(device, DataFormat.S8UINT, TextureUsageBits.DepthStencilAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo);
            Reject<ArgumentException>(() => device.TextureCopy(selected, plain, new(0, 0, 0), new(0, 0, 0), new(4, 4, 1), 0, 0, 0, 0));
            var automatic = device.FramebufferCreate([selected, resolved]);
            device.DrawListBegin(automatic, DrawFlags.ClearStencil, clearStencilValue: 237); device.DrawListEnd();
            Check(device.TextureGetData(resolved, 0).All(value => value == 237), "Inferred framebuffer stencil resolve executes without a depth aspect."); device.FreeRID(automatic);
            foreach (var pipeline in pipelines) device.FreeRID(pipeline); foreach (var rid in new[] { target, selected, source, resolved, plain }) device.FreeRID(rid);
        }
        device.FreeRID(shader);
    }
}
