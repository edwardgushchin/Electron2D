using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceDescriptorArrayTests
{
    internal static void Canvas(string language)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var texture = new TextureRD(); var root = new Window { Size = new(64, 64) }; RenderingDevice device = null!; RID input = default, pipeline = default, read = default, write = default;
        var frames = 0; long before = 0; byte[] tint = [0, 255, 0, 255];
        root.Ready += _ =>
        {
            device = RenderingServer.GetRenderingDevice()!;
            using var module = Compile(device, language == "GLSL" ? """
                #version 450
                layout(local_size_x=1)in;layout(set=3,binding=7)uniform samplerBuffer inputs[2];layout(set=4,binding=2,rgba8)uniform writeonly image2D targets[2];
                void main(){uint i=gl_GlobalInvocationID.x;imageStore(targets[i],ivec2(0),texelFetch(inputs[i],0));}
                """ : """
                [[vk::binding(7,3)]] Buffer<float4> inputs[2];[[vk::binding(2,4),vk::image_format("rgba8")]] RWTexture2D<float4> targets[2];
                [numthreads(1,1,1)]void main(uint3 id:SV_DispatchThreadID){targets[id.x][int2(0,0)]=inputs[id.x].Load(0);}
                """, "Canvas", language == "GLSL" ? ShaderLanguage.GLSL : ShaderLanguage.HLSL);
            var shader = device.ShaderCreateFromSPIRV(module); pipeline = device.ComputePipelineCreate(shader);
            var first = device.TextureBufferCreate(1, DataFormat.R8G8B8A8UNORM, [0, 0, 255, 255]); input = device.TextureBufferCreate(1, DataFormat.R8G8B8A8UNORM, tint);
            using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state);
            read = Set(device, shader, 3, 7, language == "GLSL" ? UniformType.SamplerWithTextureBuffer : UniformType.TextureBuffer, language == "GLSL" ? [sampler, first, sampler, input] : [first, input]);
            var a = Image(device, 0, TextureUsageBits.Sampling | TextureUsageBits.Storage); var b = Image(device, 0, TextureUsageBits.Sampling | TextureUsageBits.Storage); write = Set(device, shader, 4, 2, UniformType.Image, a, b);
            texture.TextureRDRID = b; root.AddChild(new Sprite { Texture = texture, Centered = false, Position = new(8, 8), Scale = new(16, 16) });
            RenderingServer.FramePreDraw += Generate; RenderingServer.FramePostDraw += Captured;
        };
        Engine.Run(root); Check(root.IsDisposed && device.IsDisposed, "Array resources retire with their retained renderer.");
        Console.WriteLine(language + " descriptor arrays reach retained canvas; 32 full frames allocate zero managed owner bytes.");
        void Generate()
        {
            if (frames >= 8) before = GC.GetAllocatedBytesForCurrentThread(); var green = frames % 2 == 0; tint[0] = green ? (byte)0 : (byte)255; tint[1] = green ? (byte)255 : (byte)0;
            device.BufferUpdate(input, 0, 4, tint); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, read, 3); device.ComputeListBindUniformSet(list, write, 4); device.ComputeListDispatch(list, 2, 1, 1); device.ComputeListEnd();
        }
        void Captured()
        {
            if (frames >= 8) Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared array compute/canvas submission allocates zero managed owner bytes.");
            if (frames is 0 or 39) { using var pixels = root.GetTexture().GetImage()!; var pixel = pixels.GetPixel(9, 9); Check(Math.Abs(pixel.R - tint[0] / 255f) < .01f && Math.Abs(pixel.G - tint[1] / 255f) < .01f && pixel.B < .01f, "Retained canvas samples the current nonzero output array element."); }
            if (++frames != 40) return; RenderingServer.FramePreDraw -= Generate; RenderingServer.FramePostDraw -= Captured; root.Tree!.Quit();
        }
    }
}
