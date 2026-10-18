using System.Runtime.InteropServices;
using System.Text;
using Electron2D;

// Use the packaged DXC frontend without SDL's descriptor-array flattening, which loses logical parameter shapes.
internal static unsafe partial class ShaderSourceTypes
{
    private static void Release(nint instance) { if (instance != 0) ((delegate* unmanaged<nint, uint>)Slot(instance, 2))(instance); }
    private static byte[] Bytes(nint blob)
    {
        var size = ((delegate* unmanaged<nint, nuint>)Slot(blob, 4))(blob);
        if (size > 16 * 1024 * 1024) throw new ArgumentException("HLSL compiler output exceeds 16 MiB.");
        var pointer = ((delegate* unmanaged<nint, nint>)Slot(blob, 3))(blob);
        return new ReadOnlySpan<byte>((void*)pointer, checked((int)size)).ToArray();
    }
    internal static bool HasDescriptorArrays(byte[] code)
    {
        var instructions = Electron2D.MaterialLightingProgram.Module.Read(code);
        var arrays = instructions.Where(i => Electron2D.MaterialLightingProgram.Op(i) is 28 or 29).Select(i => i[1]).ToHashSet();
        var pointers = instructions.Where(i => Electron2D.MaterialLightingProgram.Op(i) == 32 && i[2] == 0 && arrays.Contains(i[3])).Select(i => i[1]).ToHashSet();
        return instructions.Any(i => Electron2D.MaterialLightingProgram.Op(i) == 59 && pointers.Contains(i[1]));
    }
    internal static byte[] CompileHLSL(string source, RenderingDevice.ShaderStage stage, string? includeDirectory)
    {
        nint compiler = 0, utilities = 0, include = 0, result = 0, errors = 0, output = 0;
        var strings = new List<nint>();
        try
        {
            var compilerClass = new Guid("73e22d93-e6ce-47f3-b5bf-f0664f39c1b0"); var compilerInterface = new Guid("228b4687-5a6a-4730-900c-9702b2203f54");
            Check(DxcCreateInstance(in compilerClass, in compilerInterface, out compiler));
            var utilsClass = new Guid("6245d6af-66e0-48fd-80b4-4d271796748c"); var utilsInterface = new Guid("4605c4cb-2019-492a-ada4-65f20bb7d67f");
            Check(DxcCreateInstance(in utilsClass, in utilsInterface, out utilities));
            Check(((delegate* unmanaged<nint, nint*, int>)Slot(utilities, 9))(utilities, &include));
            var profile = stage == RenderingDevice.ShaderStage.Compute ? "cs_6_0" : stage == RenderingDevice.ShaderStage.Vertex ? "vs_6_0" : "ps_6_0";
            var arguments = new List<string> { "-E", "main", "-T", profile, "-HV", "2021", "-spirv", "-fspv-preserve-bindings", "-fspv-preserve-interface", "-encoding", "utf8" };
            if (includeDirectory is not null) { arguments.Add("-I"); arguments.Add(includeDirectory); }
            foreach (var argument in arguments)
            {
                // DXC uses native wchar_t: UTF-16 on Windows, UTF-32 on Unix hosts.
                var bytes = (OperatingSystem.IsWindows() ? Encoding.Unicode : Encoding.UTF32).GetBytes(argument + '\0');
                var pointer = Marshal.AllocHGlobal(bytes.Length); strings.Add(pointer); Marshal.Copy(bytes, 0, pointer, bytes.Length);
            }
            var pointers = strings.ToArray(); var text = Encoding.UTF8.GetBytes(source); var resultInterface = new Guid("58346cda-dde7-4497-9461-6f87af5e0659");
            fixed (byte* sourcePointer = text)
            fixed (nint* args = pointers)
            {
                var buffer = new DXCBuffer { Pointer = (nint)sourcePointer, Size = (nuint)text.Length, Encoding = 65001 };
                Check(((delegate* unmanaged<nint, DXCBuffer*, nint*, uint, nint, Guid*, nint*, int>)Slot(compiler, 3))(compiler, &buffer, args, (uint)pointers.Length, include, &resultInterface, &result));
            }
            var status = 0;
            Check(((delegate* unmanaged<nint, int*, int>)Slot(result, 3))(result, &status));
            Check(((delegate* unmanaged<nint, nint*, int>)Slot(result, 5))(result, &errors));
            var diagnostic = errors == 0 ? "" : Encoding.UTF8.GetString(Bytes(errors)).TrimEnd('\0');
            if (status < 0) throw new ArgumentException("HLSL compilation failed:\n" + diagnostic, nameof(source));
            if (!string.IsNullOrWhiteSpace(diagnostic)) Console.Error.WriteLine(diagnostic);
            Check(((delegate* unmanaged<nint, nint*, int>)Slot(result, 4))(result, &output));
            if (output == 0) throw new InvalidOperationException("HLSL compiler returned no compiled object.");
            return Bytes(output);
        }
        finally
        {
            Release(output); Release(errors); Release(result); Release(include); Release(utilities); Release(compiler);
            foreach (var pointer in strings) Marshal.FreeHGlobal(pointer);
        }
    }
}
