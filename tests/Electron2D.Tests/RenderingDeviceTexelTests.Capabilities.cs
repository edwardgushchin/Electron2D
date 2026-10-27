using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class RenderingDeviceTexelTests
{
    private static HashSet<uint> Capabilities(byte[] code)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); var result = new HashSet<uint>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16)) if ((words[at] & 65535) == 17) result.Add(words[at + 1]);
        return result;
    }
    private static void DerivedCapabilities(RenderingDevice device)
    {
        var type = typeof(RenderingDevice).Assembly.GetType("Electron2D.TexelBufferProgram")!; var lower = type.GetMethod("Lower", BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var mixed in new[] { false, true })
        {
            using var source = new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1)in;layout(set=3,binding=7)writeonly uniform imageBuffer texels[2];layout(set=0,binding=0)uniform Choice{int selected;};" + (mixed ? "layout(set=4,binding=8)writeonly uniform image2D nativeImage;" : "") + "void main(){imageStore(texels[selected],0,vec4(1));" + (mixed ? "imageStore(nativeImage,ivec2(0),vec4(1));" : "") + "}" };
            using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute); var before = module.BytecodeCompute;
            Check(Capabilities(before).Contains(56) && Capabilities(before).Contains(5305), "The original fixture requires formatless stores and dynamic texel descriptor indexing.");
            var result = lower.Invoke(null, [before, false])!; var code = (byte[])result.GetType().GetField("Item1")!.GetValue(result)!; var actual = Capabilities(code);
            Check(!actual.Overlaps(new uint[] { 46, 47, 5304, 5305, 5311, 5312 }) && actual.Contains(56) == mixed, "Derived raw-buffer modules remove texel-only capabilities and preserve an ordinary formatless image requirement.");
            Check(module.BytecodeCompute.SequenceEqual(before), "Capability cleanup preserves the original exported module.");
            if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_OUTPUT") is { } path) { Directory.CreateDirectory(path); File.WriteAllBytes(System.IO.Path.Combine(path, "Capabilities" + mixed + ".spv"), code); }
        }
        using (var source = new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1)in;layout(set=3,binding=7,r16f)uniform imageBuffer texels;void main(){imageStore(texels,0,vec4(1));}" })
        using (var module = device.ShaderCompileSPIRVFromSource(source, false))
        {
            Check(module.CompileErrorCompute == "", module.CompileErrorCompute); Check(Capabilities(module.BytecodeCompute).Contains(49), "Extended texel format fixture declares its original image capability.");
            var lowered = lower.Invoke(null, [module.BytecodeCompute, false])!; var code = (byte[])lowered.GetType().GetField("Item1")!.GetValue(lowered)!; Check(!Capabilities(code).Contains(49), "A derived raw buffer does not retain the extended storage-image format feature.");
        }
        foreach (var mixed in new[] { false, true })
        {
            using var source = new RDShaderSource { SourceCompute = "#version 450\n#extension GL_EXT_nonuniform_qualifier:require\nlayout(local_size_x=1)in;layout(set=3,binding=7)writeonly uniform imageBuffer texels[2];layout(set=0,binding=0)uniform Choice{int selected;};" + (mixed ? "layout(set=4,binding=8)writeonly uniform image2D nativeImage[2];" : "") + "void main(){imageStore(texels[nonuniformEXT(selected)],0,vec4(1));" + (mixed ? "imageStore(nativeImage[nonuniformEXT(selected)],ivec2(0),vec4(1));" : "") + "}" };
            using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute); var original = Capabilities(module.BytecodeCompute); Check(original.Contains(5301) && original.Contains(5312), "The source declares nonuniform texel descriptor indexing.");
            var lowered = lower.Invoke(null, [module.BytecodeCompute, false])!; var code = (byte[])lowered.GetType().GetField("Item1")!.GetValue(lowered)!; var actual = Capabilities(code); Check(!actual.Contains(5312) && actual.Contains(5301) == mixed && actual.Contains(5309) == mixed, "Scalarized texel choices remove NonUniform while an ordinary indexed image preserves its requirement.");
            if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_OUTPUT") is { } path) File.WriteAllBytes(System.IO.Path.Combine(path, "NonUniformCapabilities" + mixed + ".spv"), code);
        }
        var prefix = "#version 450\nlayout(set=3,binding=7)writeonly uniform imageBuffer texels[2];layout(push_constant)uniform Push{int selected;};";
        using (var source = new RDShaderSource { SourceVertex = prefix + "void main(){imageStore(texels[selected],gl_VertexIndex,vec4(1));vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);}", SourceFragment = prefix + "layout(location=0)out vec4 color;void main(){imageStore(texels[selected],int(gl_FragCoord.x),vec4(1));color=vec4(1);}" })
        using (var module = device.ShaderCompileSPIRVFromSource(source, false))
        {
            Check(module.CompileErrorVertex == "" && module.CompileErrorFragment == "", module.CompileErrorVertex + module.CompileErrorFragment); Export(module, "CoreGraphicsVertex", RenderingDevice.ShaderStage.Vertex); Export(module, "CoreGraphicsFragment", RenderingDevice.ShaderStage.Fragment);
        }
        Console.WriteLine("Texel-only native capabilities removed; mixed ordinary image requirement preserved.");
    }
}
