using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceImageAtomicTests
{
    private static void Grid(RenderingDevice device, ShaderLanguage language)
    {
        using var module = Compile(device, language == ShaderLanguage.GLSL ? """
            #version 450
            layout(local_size_x=4,local_size_y=3)in;layout(set=3,binding=7,r32ui)uniform uimage2D images[2];
            void main(){uvec2 id=gl_GlobalInvocationID.xy;imageAtomicAdd(images[(id.x/4)%2],ivec2(id.x%4,id.y),1);}
            """ : """
            [[vk::binding(7,3)]] RWTexture2D<uint> images[2];
            [numthreads(4,3,1)]void main(uint3 id:SV_DispatchThreadID){InterlockedAdd(images[(id.x/4)%2][int2(id.x%4,id.y)],1);}
            """, "Grid", language);
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        using var format = new RDTextureFormat { Width = 4, Height = 3, Format = DataFormat.R32UINT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.StorageAtomic | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView();
        uint[] values = Enumerable.Range(1, 12).Select(i => (uint)i).ToArray(); var initial = MemoryMarshal.AsBytes(values.AsSpan()).ToArray(); var a = device.TextureCreate(format, view, [initial]); var b = device.TextureCreate(format, view, [initial]);
        var images = Set(device, shader, 3, 7, UniformType.Image, a, b); Dispatch(device, pipeline, 4, (images, 3));
        foreach (var image in new[] { a, b })
        { var actual = MemoryMarshal.Cast<byte, uint>(device.TextureGetData(image, 0).AsSpan()); for (var i = 0; i < values.Length; i++) Check(actual[i] == values[i] + 2, "Atomic coordinates preserve each distinct cell in a non-square two-dimensional grid."); }
        Console.WriteLine(language + ": 48 concurrent spatial atomic updates preserve both 4x3 grids.");
    }

    private static void Mixed(RenderingDevice device, ShaderLanguage language)
    {
        using var module = Compile(device, language == ShaderLanguage.GLSL ? """
            #version 450
            layout(local_size_x=32)in;layout(set=3,binding=7,r32ui)uniform uimage2D images[2];layout(set=4,binding=2,r32ui)uniform uimageBuffer buffers[2];
            void main(){uint i=gl_GlobalInvocationID.x%2;imageAtomicAdd(images[i],ivec2(0),1);imageAtomicAdd(buffers[i],0,1);memoryBarrierImage();}
            """ : """
            [[vk::binding(7,3)]] RWTexture2D<uint> images[2];[[vk::binding(2,4)]] RWBuffer<uint> buffers[2];
            [numthreads(32,1,1)]void main(uint3 id:SV_DispatchThreadID){uint i=id.x%2;InterlockedAdd(images[i][int2(0,0)],1);InterlockedAdd(buffers[i][0],1);DeviceMemoryBarrier();}
            """, "Mixed", language);
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var images = new[] { Image(device, false), Image(device, false) }; var buffers = new[] { device.TextureBufferCreate(1, DataFormat.R32UINT), device.TextureBufferCreate(1, DataFormat.R32UINT) };
        var imageSet = Set(device, shader, 3, 7, UniformType.Image, images); var bufferSet = Set(device, shader, 4, 2, UniformType.ImageBuffer, buffers); Dispatch(device, pipeline, 2, (imageSet, 3), (bufferSet, 4));
        for (var i = 0; i < 2; i++) Check(BitConverter.ToUInt32(device.TextureGetData(images[i], 0)) == 32 && BitConverter.ToUInt32(device.BufferGetData(buffers[i])) == 32, "Image and formatted-buffer array atomics share one actual compute pass with their memory domains retained.");
        Console.WriteLine(language + ": mixed image/texel-array atomics and barriers passed.");
    }
}
