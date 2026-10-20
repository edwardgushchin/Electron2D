using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceImageAtomicTests
{
    private static void Bounds(RenderingDevice device)
    {
        using var module = Compile(device, """
            #version 450
            layout(local_size_x=1)in;
            layout(set=3,binding=7,r32ui)uniform uimage2D images[2];
            layout(set=3,binding=8,r32ui)uniform uimage2D neighbor;
            layout(set=4,binding=2,std430)buffer Output{uint result[];};
            layout(set=5,binding=3,std430)readonly buffer Input{uint indices[];};
            void main(){for(uint i=0;i<4;i++)result[i]=imageAtomicAdd(images[indices[i]],ivec2(0),1);result[4]=imageAtomicAdd(neighbor,ivec2(0),0);}
            """, "Bounds", ShaderLanguage.GLSL);
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var a = Image(device, false); var b = Image(device, false); var neighbor = Image(device, false);
        using var array = new RDUniform { Binding = 7, UniformType = UniformType.Image }; array.AddID(a); array.AddID(b); using var adjacent = new RDUniform { Binding = 8, UniformType = UniformType.Image }; adjacent.AddID(neighbor);
        var images = device.UniformSetCreate([array, adjacent], shader, 3); array.ClearIDs(); adjacent.ClearIDs();
        var output = device.StorageBufferCreate(20); var write = Set(device, shader, 4, 2, UniformType.StorageBuffer, output); uint[] indices = [2, uint.MaxValue, 1, 0]; var input = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(indices.AsSpan())); var read = Set(device, shader, 5, 3, UniformType.StorageBuffer, input);
        device.TextureUpdate(a, 0, BitConverter.GetBytes(10)); device.TextureUpdate(b, 0, BitConverter.GetBytes(20)); device.TextureUpdate(neighbor, 0, BitConverter.GetBytes(99));
        Dispatch(device, pipeline, 1, (images, 3), (write, 4), (read, 5)); uint[] expected = [0, 0, 20, 10, 99]; Check(MemoryMarshal.Cast<byte, uint>(device.BufferGetData(output).AsSpan()).SequenceEqual(expected), "Invalid atomic selectors return zero and cannot modify the adjacent resource.");
        Check(BitConverter.ToUInt32(device.TextureGetData(a, 0)) == 11 && BitConverter.ToUInt32(device.TextureGetData(b, 0)) == 21 && BitConverter.ToUInt32(device.TextureGetData(neighbor, 0)) == 99, "Only selected valid image elements change.");
        using var atomicOnlyFormat = new RDTextureFormat { Format = DataFormat.R32UINT, UsageBits = TextureUsageBits.StorageAtomic }; using var view = new RDTextureView(); var atomicOnly = device.TextureCreate(atomicOnlyFormat, view);
        array.AddID(a); array.AddID(atomicOnly); adjacent.AddID(neighbor); Reject<ArgumentException>(() => device.UniformSetCreate([array, adjacent], shader, 3));
        using (var foreign = RenderingServer.CreateLocalRenderingDevice())
        { array.ClearIDs(); array.AddID(a); array.AddID(Image(foreign, false)); Reject<ArgumentException>(() => device.UniformSetCreate([array, adjacent], shader, 3)); }
        Reject<InvalidOperationException>(() => Task.Run(() => device.TextureGetData(a, 0)).GetAwaiter().GetResult());
        using var specializedSource = new RDShaderSource { SourceCompute = """
            #version 450
            layout(local_size_x=1)in;layout(constant_id=13)const uint count=9;
            layout(set=3,binding=7,r32ui)uniform uimage2D images[count];
            void main(){imageAtomicAdd(images[count-1],ivec2(0),1);}
            """ };
        using var specializedModule = device.ShaderCompileSPIRVFromSource(specializedSource, false); Check(specializedModule.CompileErrorCompute == "", specializedModule.CompileErrorCompute);
        var specializedShader = device.ShaderCreateFromSPIRV(specializedModule); Reject<NotSupportedException>(() => device.ComputePipelineCreate(specializedShader));
        using var count = new RDPipelineSpecializationConstant<uint> { ConstantID = 13, Value = 2 }; var specialized = device.ComputePipelineCreate(specializedShader, [count]); count.Value = 1;
        var specializedImages = Set(device, specializedShader, 3, 7, UniformType.Image, a, b); Dispatch(device, specialized, 1, (specializedImages, 3));
        Check(BitConverter.ToUInt32(device.TextureGetData(a, 0)) == 11 && BitConverter.ToUInt32(device.TextureGetData(b, 0)) == 22, "A copied typed override repairs atomic descriptor capacity and selects its resolved last element.");
        Console.WriteLine("Atomic selector bounds, copied descriptions, storage/foreign/thread roles and adjacent descriptor isolation passed.");
    }

    private static void MemorySemantics(RenderingDevice device)
    {
        using var module = Compile(device, """
            #version 450
            #extension GL_KHR_memory_scope_semantics:require
            layout(local_size_x=1)in;
            layout(set=3,binding=7,r32ui)uniform coherent volatile uimage2D images[2];
            layout(set=4,binding=2,std430)buffer Output{uint result[];};
            void main(){for(uint i=0;i<2;i++){
              imageAtomicStore(images[i],ivec2(0),17+i,gl_ScopeDevice,gl_StorageSemanticsImage,gl_SemanticsRelease);
              result[i*2]=imageAtomicLoad(images[i],ivec2(0),gl_ScopeDevice,gl_StorageSemanticsImage,gl_SemanticsAcquire);
              result[i*2+1]=imageAtomicCompSwap(images[i],ivec2(0),17+i,31+i,gl_ScopeDevice,gl_StorageSemanticsImage,gl_SemanticsAcquireRelease,gl_StorageSemanticsImage,gl_SemanticsAcquire);
            }
            imageAtomicStore(images[1],ivec2(0),0xffffffffu,gl_ScopeDevice,gl_StorageSemanticsImage,gl_SemanticsRelease);
            result[4]=imageAtomicAdd(images[1],ivec2(0),1u,gl_ScopeDevice,gl_StorageSemanticsImage,gl_SemanticsAcquireRelease);}
            """, "MemorySemantics", ShaderLanguage.GLSL);
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var a = Image(device, false); var b = Image(device, false); var images = Set(device, shader, 3, 7, UniformType.Image, a, b);
        var output = device.StorageBufferCreate(20); var write = Set(device, shader, 4, 2, UniformType.StorageBuffer, output); Dispatch(device, pipeline, 1, (images, 3), (write, 4)); uint[] expected = [17, 17, 18, 18, uint.MaxValue];
        Check(MemoryMarshal.Cast<byte, uint>(device.BufferGetData(output).AsSpan()).SequenceEqual(expected) && BitConverter.ToUInt32(device.TextureGetData(a, 0)) == 31 && BitConverter.ToUInt32(device.TextureGetData(b, 0)) == 0, "Atomic load/store and explicit acquire/release compare-exchange semantics execute over both images.");
        Console.WriteLine("Coherent/volatile atomic load/store, Device scope and acquire/release semantics passed.");
    }
}
