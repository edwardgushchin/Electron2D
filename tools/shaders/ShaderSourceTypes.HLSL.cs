using System.Runtime.InteropServices;
using System.Text;
using SDL3;

internal static unsafe partial class ShaderSourceTypes
{
    internal static Dictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)> HLSL(string source, bool fragment, string? includeDirectory, byte[] bytecode)
    {
        var props = SDL.CreateProperties();
        if (props == 0) throw new InvalidOperationException(SDL.GetError());
        try
        {
            if (!SDL.SetBooleanProperty(props, ShaderCross.Props.HLSLSkipSPIRVRoundtripBoolean, true))
                throw new InvalidOperationException(SDL.GetError());
            var sourceBytes = Encoding.UTF8.GetBytes(source + '\0');
            var includeBytes = includeDirectory is null ? null : Encoding.UTF8.GetBytes(includeDirectory + '\0');
            fixed (byte* text = sourceBytes, include = includeBytes, entry = "main\0"u8,
                spirv = "__spirv__\0"u8, major = "__SPIRV_MAJOR_VERSION__\0"u8, minor = "__SPIRV_MINOR_VERSION__\0"u8,
                one = "1\0"u8, zero = "0\0"u8)
            {
                // Reflection must select the same preprocessor branches as the Vulkan 1.0 compilation.
                ShaderCross.HLSLDefine* defines = stackalloc ShaderCross.HLSLDefine[4];
                defines[0] = new() { Name = (nint)spirv, Value = (nint)one };
                defines[1] = new() { Name = (nint)major, Value = (nint)one };
                defines[2] = new() { Name = (nint)minor, Value = (nint)zero };
                defines[3] = default;
                var info = new ShaderCross.HLSLInfo
                {
                    Source = (nint)text,
                    Entrypoint = (nint)entry,
                    IncludeDir = (nint)include,
                    Defines = (nint)defines,
                    ShaderStage = fragment ? ShaderCross.ShaderStage.Fragment : ShaderCross.ShaderStage.Vertex,
                    Props = props
                };
                var memory = ShaderCross.CompileDXILFromHLSL(in info, out var size);
                if (memory == 0) throw new ArgumentException("HLSL type reflection compilation failed: " + SDL.GetError());
                try { return ReadDXIL(memory, size, BufferBindings(bytecode, fragment)); }
                finally { SDL.Free(memory); }
            }
        }
        finally { SDL.DestroyProperties(props); }
    }

    private static Dictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)> ReadDXIL(nint code, nuint length, Dictionary<string, int> bindings)
    {
        var classID = new Guid("6245D6AF-66E0-48FD-80B4-4D271796748C");
        var utilsID = new Guid("4605C4CB-2019-492A-ADA4-65F20BB7D67F");
        var reflectionID = new Guid("5A58797D-A72C-478D-8BA2-EFC6B0EFE88E");
        Check(DxcCreateInstance(in classID, in utilsID, out var utils));
        nint reflection = 0;
        try
        {
            var buffer = new DXCBuffer { Pointer = code, Size = length };
            Check(((delegate* unmanaged[Cdecl]<nint, DXCBuffer*, Guid*, nint*, int>)Slot(utils, 13))(utils, &buffer, &reflectionID, &reflection));
            ShaderDescription description;
            Check(((delegate* unmanaged[Cdecl]<nint, ShaderDescription*, int>)Slot(reflection, 3))(reflection, &description));
            var result = new Dictionary<(int, string), (int, int)>();
            for (uint index = 0; index < description.ConstantBuffers; index++)
            {
                var cb = ((delegate* unmanaged[Cdecl]<nint, uint, nint>)Slot(reflection, 4))(reflection, index);
                BufferDescription bufferDescription;
                Check(((delegate* unmanaged[Cdecl]<nint, BufferDescription*, int>)Slot(cb, 0))(cb, &bufferDescription));
                var bufferName = Marshal.PtrToStringUTF8(bufferDescription.Name) ?? throw new ArgumentException("HLSL reflection lost a buffer name.");
                if (!bindings.TryGetValue(bufferName, out var bindingPoint)) continue;
                for (uint member = 0; member < bufferDescription.Variables; member++)
                {
                    var variable = ((delegate* unmanaged[Cdecl]<nint, uint, nint>)Slot(cb, 1))(cb, member);
                    VariableDescription value;
                    Check(((delegate* unmanaged[Cdecl]<nint, VariableDescription*, int>)Slot(variable, 0))(variable, &value));
                    var type = ((delegate* unmanaged[Cdecl]<nint, nint>)Slot(variable, 1))(variable);
                    TypeDescription shape;
                    Check(((delegate* unmanaged[Cdecl]<nint, TypeDescription*, int>)Slot(type, 0))(type, &shape));
                    var name = Marshal.PtrToStringUTF8(value.Name) ?? throw new ArgumentException("HLSL reflection lost a uniform name.");
                    // ConstantBuffer<T> wraps the top-level fields in one reflected variable.
                    if (shape.Class == 5 && shape.Elements == 0 && bufferDescription.Variables == 1 &&
                        name == bufferName)
                    {
                        for (uint field = 0; field < shape.Members; field++)
                        {
                            var fieldType = ((delegate* unmanaged[Cdecl]<nint, uint, nint>)Slot(type, 1))(type, field);
                            TypeDescription fieldShape;
                            Check(((delegate* unmanaged[Cdecl]<nint, TypeDescription*, int>)Slot(fieldType, 0))(fieldType, &fieldShape));
                            var fieldName = ((delegate* unmanaged[Cdecl]<nint, uint, nint>)Slot(type, 3))(type, field);
                            Add(fieldName, fieldShape);
                        }
                    }
                    else Add(value.Name, shape);

                    void Add(nint namePointer, TypeDescription fieldShape)
                    {
                        var fieldName = Marshal.PtrToStringUTF8(namePointer) ?? throw new ArgumentException("HLSL reflection lost a uniform name.");
                        if (!result.TryAdd((bindingPoint, fieldName),
                            (fieldShape.Type == 1 ? checked((int)fieldShape.Columns) : 0, checked((int)fieldShape.Elements))))
                            throw new ArgumentException("Ambiguous source uniform type information.");
                    }
                }
            }
            return result;
        }
        finally
        {
            if (reflection != 0) ((delegate* unmanaged[Cdecl]<nint, uint>)Slot(reflection, 2))(reflection);
            ((delegate* unmanaged[Cdecl]<nint, uint>)Slot(utils, 2))(utils);
        }
    }

    private static Dictionary<string, int> BufferBindings(byte[] code, bool fragment)
    {
        // DXIL ignores Vulkan binding attributes. Match compiler resource names to the actual SPIR-V bindings.
        var words = MemoryMarshal.Cast<byte, uint>(code);
        var names = new Dictionary<uint, string>(); var sets = new Dictionary<uint, uint>(); var bindings = new Dictionary<uint, uint>();
        var buffers = new HashSet<uint>();
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 0xffff;
            if (count == 0 || count > words.Length - at) throw new ArgumentException("Malformed compiler reflection module.");
            var instruction = words.Slice(at, count);
            if (op == 5)
            {
                var bytes = MemoryMarshal.AsBytes(instruction[2..]); var end = bytes.IndexOf((byte)0);
                if (end < 0) throw new ArgumentException("Malformed compiler reflection string.");
                names[instruction[1]] = Encoding.UTF8.GetString(bytes[..end]);
            }
            if (op == 59 && instruction[3] == 2) buffers.Add(instruction[2]);
            if (op == 71 && count == 4)
            {
                if (instruction[2] == 33) bindings[instruction[1]] = instruction[3];
                if (instruction[2] == 34) sets[instruction[1]] = instruction[3];
            }
            at += count;
        }
        return buffers.Where(id => sets.TryGetValue(id, out var set) && set == (fragment ? 3 : 1) && bindings.ContainsKey(id))
            .ToDictionary(id => names[id], id => checked((int)bindings[id]), StringComparer.Ordinal);
    }

    private static nint Slot(nint instance, int index) => (*(nint**)instance)[index];
    private static void Check(int result)
    {
        if (result < 0) throw new ArgumentException($"DXC source type reflection failed (0x{result:X8}).");
    }

    [LibraryImport("dxcompiler", EntryPoint = "DxcCreateInstance")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int DxcCreateInstance(in Guid classID, in Guid interfaceID, out nint instance);

    [StructLayout(LayoutKind.Sequential)]
    private struct DXCBuffer { internal nint Pointer; internal nuint Size; internal uint Encoding; }
    [StructLayout(LayoutKind.Sequential)]
    private struct BufferDescription { internal nint Name; internal uint Type, Variables, Size, Flags; }
    [StructLayout(LayoutKind.Sequential)]
    private struct VariableDescription { internal nint Name; internal uint Offset, Size, Flags; internal nint Default; internal uint Texture, Textures, Sampler, Samplers; }
    [StructLayout(LayoutKind.Sequential)]
    private struct TypeDescription { internal uint Class, Type, Rows, Columns, Elements, Members, Offset; internal nint Name; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ShaderDescription
    {
        internal uint Version; internal nint Creator;
        internal uint Flags, ConstantBuffers, BoundResources, InputParameters, OutputParameters,
            InstructionCount, TempRegisterCount, TempArrayCount, DefCount, DclCount,
            TextureNormalInstructions, TextureLoadInstructions, TextureCompInstructions, TextureBiasInstructions, TextureGradientInstructions,
            FloatInstructionCount, IntInstructionCount, UintInstructionCount, StaticFlowControlCount, DynamicFlowControlCount,
            MacroInstructionCount, ArrayInstructionCount, CutInstructionCount, EmitInstructionCount, GSOutputTopology,
            GSMaxOutputVertexCount, InputPrimitive, PatchConstantParameters, GSInstanceCount, ControlPoints,
            HSOutputPrimitive, HSPartitioning, TessellatorDomain, BarrierInstructions, InterlockedInstructions, TextureStoreInstructions;
    }
}
