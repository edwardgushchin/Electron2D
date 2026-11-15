using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceSamplerTests
{
    internal static void RunLOD()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        var driver = Environment.GetEnvironmentVariable("SDL_GPU_DRIVER");
        Check(driver is "vulkan" or "direct3d12", "Select SDL_GPU_DRIVER=vulkan or direct3d12.");
        var pointMipAnisotropy = true;
        if (driver == "direct3d12")
        {
            Reject<NotSupportedException>(() => device.GetDriverResource(DriverResource.QueueFamily, default, 0));
            pointMipAnisotropy = ReadPointMipAnisotropy(device);
        }
        else
        {
            _ = device.GetDriverResource(DriverResource.QueueFamily, default, 0);
        }
        Console.WriteLine($"{driver}: sampler RID on {device.GetDeviceName()}.");

        using var source = new RDShaderSource
        {
            Language = ShaderLanguage.GLSL,
            SourceVertex = """
                #version 450
                void main() {
                    vec2 point = vec2((gl_VertexIndex << 1) & 2, gl_VertexIndex & 2);
                    gl_Position = vec4(point * 2 - 1, 0, 1);
                }
                """,
            SourceFragment = """
                #version 450
                layout(set=3,binding=7) uniform sampler2D image;
                layout(set=2,binding=1,std140) uniform Parameters { vec4 footprint; };
                layout(location=0) out vec4 color;
                void main() {
                    color = texture(image, (gl_FragCoord.xy - vec2(4.5)) * footprint.xy + vec2(0.5));
                }
                """,
        };
        using var module = device.ShaderCompileSPIRVFromSource(source, false);
        Check(module.CompileErrorVertex == "" && module.CompileErrorFragment == "", module.CompileErrorVertex + module.CompileErrorFragment);
        var shader = device.ShaderCreateFromSPIRV(module);

        const int targetSize = 8, baseSize = 16;
        using var format = new RDTextureFormat { Width = baseSize, Height = baseSize, MIPMaps = 5, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.Sampling };
        using var view = new RDTextureView();
        // Solid red at level zero and green at every coarser level identify the sampled mip independently of filtering.
        var pixels = new byte[(16 * 16 + 8 * 8 + 4 * 4 + 2 * 2 + 1) * 4];
        for (int texel = 0; texel < pixels.Length / 4; texel++)
        {
            pixels[texel * 4 + (texel < baseSize * baseSize ? 0 : 1)] = 255;
            pixels[texel * 4 + 3] = 255;
        }
        var texture = device.TextureCreate(format, view, [pixels]);
        format.Width = format.Height = targetSize;
        format.MIPMaps = 1;
        format.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom;
        var color = device.TextureCreate(format, view);
        var framebuffer = device.FramebufferCreate([color]);
        using var raster = new RDPipelineRasterizationState();
        using var attachment = new RDPipelineColorBlendStateAttachment();
        using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        var uniform = device.UniformBufferCreate(16);
        var parameters = Set(device, shader, 2, 1, UniformType.UniformBuffer, uniform);

        // The isotropic footprints are one and two texels per pixel. The elongated footprint spans 0.5 by 8 texels.
        using var state = new RDSamplerState();
        Sample("bias zero at LOD 0", 1f / baseSize, 1f / baseSize, true);
        state.LODBias = 1;
        Sample("bias +1 at LOD 0", 1f / baseSize, 1f / baseSize, false);
        state.LODBias = 0;
        Sample("bias zero at LOD 1", 2f / baseSize, 2f / baseSize, false);
        state.LODBias = -1;
        Sample("bias -1 at LOD 1", 2f / baseSize, 2f / baseSize, true);
        state.LODBias = 0;
        state.MinFilter = state.MagFilter = state.MIPFilter = SamplerFilter.Linear;
        Sample("isotropic linear mip", .5f / baseSize, 8f / baseSize, false);
        state.UseAnisotropy = true;
        state.AnisotropyMax = 16;
        Sample("anisotropic linear mip", .5f / baseSize, 8f / baseSize, true);
        state.MIPFilter = SamplerFilter.Nearest;
        Sample("requested anisotropic point mip", .5f / baseSize, 8f / baseSize, pointMipAnisotropy);

        void Sample(string label, float horizontal, float vertical, bool red)
        {
            ReadOnlySpan<float> footprint = [horizontal, vertical, 0, 0];
            device.BufferUpdate(uniform, 0, 16, MemoryMarshal.AsBytes(footprint));
            var sampler = device.SamplerCreate(state);
            using var input = new RDUniform { Binding = 7, UniformType = UniformType.SamplerWithTexture };
            input.AddID(sampler);
            input.AddID(texture);
            var set = device.UniformSetCreate([input], shader, 3);
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Black]);
            device.DrawListBindRenderPipeline(list, pipeline);
            device.DrawListBindUniformSet(list, parameters, 2);
            device.DrawListBindUniformSet(list, set, 3);
            device.DrawListDraw(list, false, 1, 3);
            device.DrawListEnd();
            var pixel = device.TextureGetData(color, 0).AsSpan((4 * targetSize + 4) * 4, 4);
            ReadOnlySpan<byte> expected = red ? [255, 0, 0, 255] : [0, 255, 0, 255];
            Console.WriteLine($"{label}: actual RGBA {pixel[0]},{pixel[1]},{pixel[2]},{pixel[3]}, expected {expected[0]},{expected[1]},0,255.");
            Check(pixel.SequenceEqual(expected), "Sampler RID pixel differs for " + label + ".");
            device.FreeRID(sampler);
            Check(!device.UniformSetIsValid(set), "A set borrows the sampler RID.");
            device.FreeRID(set);
        }
    }

    private static unsafe bool ReadPointMipAnisotropy(RenderingDevice device)
    {
        var path = Environment.GetEnvironmentVariable("ELECTRON2D_D3D12_DRIVER_FIXTURE")
            ?? throw new InvalidOperationException("Set ELECTRON2D_D3D12_DRIVER_FIXTURE to the SDK driver resource probe.");
        var fixture = NativeLibrary.Load(path);
        try
        {
            var check = (delegate* unmanaged[Cdecl]<ulong, int*, int>)NativeLibrary.GetExport(fixture, "CheckAnisotropicPointMip");
            int supported;
            int status = check(device.GetDriverResource(DriverResource.LogicalDevice, default, 0), &supported);
            Console.WriteLine($"D3D12_FEATURE_D3D12_OPTIONS19 HRESULT=0x{status:X8}, AnisoFilterWithPointMipSupported={supported != 0}.");
            return supported != 0;
        }
        finally { NativeLibrary.Free(fixture); }
    }
}
