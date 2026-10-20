using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceImageAtomicTests
{
    internal static void Canvas(string language)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var texture = new TextureRD(); var root = new Window { Size = new(64, 64) }; RenderingDevice device = null!;
        RID a = default, b = default, counterPipeline = default, colorPipeline = default; (RID Set, uint Index)[] counterSets = [], colorSets = []; var frames = 0; long before = 0;
        root.Ready += _ =>
        {
            device = RenderingServer.GetRenderingDevice()!; var authoring = language == "GLSL" ? ShaderLanguage.GLSL : ShaderLanguage.HLSL;
            using var counterModule = Compile(device, language == "GLSL" ? """
                #version 450
                layout(local_size_x=32)in;layout(set=3,binding=7,r32ui)uniform uimage2D counters[2];
                void main(){imageAtomicAdd(counters[gl_GlobalInvocationID.x%2],ivec2(0),1);}
                """ : """
                [[vk::binding(7,3)]] RWTexture2D<uint> counters[2];
                [numthreads(32,1,1)]void main(uint3 id:SV_DispatchThreadID){InterlockedAdd(counters[id.x%2][int2(0,0)],1);}
                """, "CanvasCounter", authoring);
            using var colorModule = Compile(device, language == "GLSL" ? """
                #version 450
                layout(local_size_x=1)in;layout(set=3,binding=7,r32ui)uniform readonly uimage2D counters[2];layout(set=4,binding=2,rgba8)uniform writeonly image2D outputImage;
                void main(){vec4 color=imageLoad(counters[1],ivec2(0)).x==16?vec4(0,1,0,1):vec4(1,0,0,1);imageStore(outputImage,ivec2(0),color);}
                """ : """
                [[vk::binding(7,3)]] RWTexture2D<uint> counters[2];[[vk::binding(2,4),vk::image_format("rgba8")]] RWTexture2D<float4> outputImage;
                [numthreads(1,1,1)]void main(){outputImage[int2(0,0)]=counters[1][int2(0,0)]==16?float4(0,1,0,1):float4(1,0,0,1);}
                """, "CanvasColor", authoring);
            var counterShader = device.ShaderCreateFromSPIRV(counterModule); var colorShader = device.ShaderCreateFromSPIRV(colorModule);
            counterPipeline = device.ComputePipelineCreate(counterShader); colorPipeline = device.ComputePipelineCreate(colorShader); a = Image(device, false); b = Image(device, false);
            counterSets = [(Set(device, counterShader, 3, 7, UniformType.Image, a, b), 3)];
            using var format = new RDTextureFormat { Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.Storage }; using var view = new RDTextureView(); var output = device.TextureCreate(format, view);
            colorSets = [(Set(device, colorShader, 3, 7, UniformType.Image, a, b), 3), (Set(device, colorShader, 4, 2, UniformType.Image, output), 4)];
            texture.TextureRDRID = output; root.AddChild(new Sprite { Texture = texture, Centered = false, Position = new(8, 8), Scale = new(16, 16) });
            RenderingServer.FramePreDraw += Generate; RenderingServer.FramePostDraw += Captured;
        };
        Engine.Run(root); Check(root.IsDisposed && device.IsDisposed, "Atomic resources and borrowed canvas output retire with their retained renderer.");
        Console.WriteLine(language + " atomic image counts reach retained canvas; 32 full owner frames allocate zero managed bytes.");
        void Generate()
        {
            if (frames >= 8) before = GC.GetAllocatedBytesForCurrentThread(); device.TextureClear(a, default, 0, 1, 0, 1); device.TextureClear(b, default, 0, 1, 0, 1);
            Dispatch(device, counterPipeline, frames % 2 == 0 ? 1u : 2u, counterSets); Dispatch(device, colorPipeline, 1, colorSets);
        }
        void Captured()
        {
            if (frames >= 8) Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared atomic clear/count/color/canvas submission allocates zero managed owner bytes.");
            if (frames is 0 or 39)
            {
                using var pixels = root.GetTexture().GetImage()!; var pixel = pixels.GetPixel(9, 9); var green = frames % 2 == 0;
                Check(Math.Abs(pixel.R - (green ? 0 : 1)) < .01f && Math.Abs(pixel.G - (green ? 1 : 0)) < .01f && pixel.B < .01f, "Canvas samples the current atomic count through an ordered GPU color pass.");
            }
            if (++frames != 40) return; RenderingServer.FramePreDraw -= Generate; RenderingServer.FramePostDraw -= Captured; root.Tree!.Quit();
        }
    }
}
