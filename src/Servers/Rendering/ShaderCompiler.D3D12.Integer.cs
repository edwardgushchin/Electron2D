using System.Runtime.InteropServices;
using System.Text;
using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static unsafe partial class ShaderCompiler
{
    internal static bool? D3D12IntegerSampling(nint device)
    {
        ulong native;
        try
        {
            // Older bridges may lack this query. D3D12 still validates the DXIL at pipeline creation.
            if (RenderingDevice.QueryDriverResource(device, 1, (uint)RenderingDevice.DriverResource.LogicalDevice, 0, out native) != 1 || native == 0)
                return null;
        }
        catch (EntryPointNotFoundException) { return null; }

        const uint shaderModel = 7, options14 = 43;
        const uint model67 = 0x67;
        var model = model67;
        uint* options = stackalloc uint[3]; // D3D12_FEATURE_DATA_D3D12_OPTIONS14
        var logical = (nint)native;
        var check = (delegate* unmanaged[Stdcall]<nint, uint, uint*, uint, int>)Methods(logical)[13]; // ID3D12Device::CheckFeatureSupport
        return check(logical, shaderModel, &model, sizeof(uint)) >= 0 && model >= model67 &&
            check(logical, options14, options, 3 * sizeof(uint)) >= 0 && options[0] != 0;
    }

    private static bool UsesIntegerSamples(byte[] code)
    {
        var instructions = Module.Read(code);
        var samples = instructions.Where(i => Op(i) is >= 87 and <= 94 or 96);
        if (!samples.Any()) return false;
        var integers = instructions.Where(i => Op(i) == 21).Select(i => i[1]).ToHashSet();
        var types = instructions.Where(i => Op(i) == 23 && integers.Contains(i[2])).Select(i => i[1]).ToHashSet();
        return samples.Any(i => integers.Contains(i[1]) || types.Contains(i[1]));
    }

    private static byte[] CompileIntegerDXIL(nint device, byte[] code, string shaderProfile)
    {
        if (D3D12IntegerSampling(device) == false)
            throw new NotSupportedException("Direct3D 12 integer texture sampling requires Shader Model 6.7 and Advanced Texture Operations.");
        var source = SpirvReflection.TranspileAdvancedHLSL(code);
        var compilerClass = new Guid("73e22d93-e6ce-47f3-b5bf-f0664f39c1b0");
        var compilerInterface = new Guid("228b4687-5a6a-4730-900c-9702b2203f54");
        Marshal.ThrowExceptionForHR(DxcCreateInstance(in compilerClass, in compilerInterface, out var compiler));
        try
        {
            var resultInterface = new Guid("58346cda-dde7-4497-9461-6f87af5e0659");
            var text = Encoding.UTF8.GetBytes(source);
            nint result;
            fixed (byte* pointer = text)
            fixed (char* entry = "main", entryFlag = "-E", profileFlag = "-T", profile = shaderProfile)
            {
                char** args = stackalloc char*[] { entryFlag, entry, profileFlag, profile };
                var buffer = new DxcBuffer { Pointer = pointer, Size = (nuint)text.Length, Encoding = 65001 };
                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, DxcBuffer*, char**, uint, nint, Guid*, nint*, int>)Methods(compiler)[3])(
                    compiler, &buffer, args, 4, 0, &resultInterface, &result));
            }
            try
            {
                int status;
                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, int*, int>)Methods(result)[3])(result, &status));
                var slot = status < 0 ? 5 : 4; // IDxcResult GetErrorBuffer / GetResult
                nint blob;
                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Methods(result)[slot])(result, &blob));
                if (blob == 0) throw new InvalidOperationException("DXC returned no Shader Model 6.7 output.");
                try
                {
                    var bytes = ((delegate* unmanaged[Stdcall]<nint, byte*>)Methods(blob)[3])(blob);
                    var length = ((delegate* unmanaged[Stdcall]<nint, nuint>)Methods(blob)[4])(blob);
                    if (length == 0 || length > MaximumBytecodeBytes)
                        throw new InvalidOperationException("Invalid Shader Model 6.7 compiler output size.");
                    var data = new ReadOnlySpan<byte>(bytes, (int)length);
                    if (status < 0) throw new NotSupportedException("Shader Model 6.7 compilation failed: " + Encoding.UTF8.GetString(data).TrimEnd('\0'));
                    return data.ToArray();
                }
                finally { Marshal.Release(blob); }
            }
            finally { Marshal.Release(result); }
        }
        finally { Marshal.Release(compiler); }
    }

    private static nint* Methods(nint instance) => *(nint**)instance;

    [StructLayout(LayoutKind.Sequential)]
    private struct DxcBuffer
    {
        internal byte* Pointer;
        internal nuint Size;
        internal uint Encoding;
    }

    [LibraryImport("dxcompiler")]
    private static partial int DxcCreateInstance(in Guid classID, in Guid interfaceID, out nint instance);
}
