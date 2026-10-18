using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal static unsafe partial class ShaderCompiler
{
    private static ShaderProgram ValidateParticleInterface(ReadOnlySpan<byte> bytecode,
        IReadOnlyDictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)>? sourceTypes)
    {
        ValidateBytecodeLength(bytecode); var code = bytecode.ToArray(); ValidateStructure(code, false, particles: true);
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); var localSizes = 0;
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 0xffff;
            if (op == 16 && count >= 3 && words[at + 2] == 17)
            {
                if (++localSizes != 1 || count != 6 || words[at + 3] != 64 || words[at + 4] != 1 || words[at + 5] != 1)
                    throw new NotSupportedException("Particle programs require one literal 64 by 1 by 1 local size.");
            }
            if (op == 331) throw new NotSupportedException("Particle workgroup dimensions cannot use specialization constants.");
            at += count;
        }
        if (localSizes != 1) throw new NotSupportedException("Particle programs require a fixed local size.");
        var program = SpirvReflection.Read(code, false, sourceTypes, particles: true); program.Mode = Shader.Mode.Particles;
        Run(() =>
        {
            fixed (byte* pointer = program.NativeCode)
            {
                var memory = ShaderCross.ReflectComputeSPIRV((nint)pointer, (nuint)program.NativeCode.Length, 0);
                if (memory == 0) throw new ArgumentException("Particle compute reflection failed: " + SDL.GetError());
                try
                {
                    var info = Marshal.PtrToStructure<ShaderCross.ComputePipelineMetadata>(memory);
                    if (info.ThreadCountX != 64 || info.ThreadCountY != 1 || info.ThreadCountZ != 1 || info.NumReadOnlyStorageBuffers != 0 || info.NumReadOnlyStorageTextures != 0 || info.NumReadWriteStorageTextures != 0 || info.NumReadWriteStorageBuffers is < 1 or > 2 || info.NumUniformBuffers != program.BufferSizes.Length || info.NumSamplers != program.Textures.Length)
                        throw new NotSupportedException("Particle compute resources or local size do not match the process profile.");
                }
                finally { SDL.Free(memory); }
            }
            return true;
        });
        return program;
    }
}
