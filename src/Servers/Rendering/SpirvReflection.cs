using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

// Member reflection uses the SPIRV-Cross C library already shipped by the SDL shadercross package.
// All reflected pointers belong to a single context and are copied before that context is destroyed.
internal static unsafe partial class SpirvReflection
{
    private const string Library = "spirv-cross-c-shared";
    private const int Binding = 33, DescriptorSet = 34;

    internal static ShaderProgram Read(byte[] code, bool fragment)
    {
        if (CreateContext(out var context) != 0 || context == 0)
            throw new InvalidOperationException("Could not create the SPIR-V reflection context.");
        using var owner = new RenderHandle(context, DestroyContext);
        void Check(int result)
        {
            if (result != 0) throw new ArgumentException("SPIR-V reflection failed: " + Marshal.PtrToStringUTF8(GetError(context)), nameof(code));
        }
        nint ir;
        fixed (byte* p = code) Check(Parse(context, (nint)p, (nuint)(code.Length / 4), out ir));
        Check(CreateCompiler(context, 0, ir, 1, out var compiler));
        Check(GetActiveVariables(compiler, out var active));
        Check(CreateResources(compiler, out var resources, active));
        // Storage and push constants must not silently bypass material integration.
        foreach (var kind in new[] { 2, 5, 6, 8, 9, 12, 14, 15 })
        {
            Check(GetResources(resources, kind, out _, out var count));
            if (count != 0) throw new NotSupportedException("Canvas storage and push-constant resources are not integrated yet.");
        }
        Check(GetResources(resources, 1, out var pointer, out var bufferCount));
        if (bufferCount > 4) throw new NotSupportedException("Canvas shaders support at most four uniform buffers per stage.");
        var sizes = new int[(int)bufferCount];
        var uniforms = new Dictionary<string, ShaderUniform>(StringComparer.Ordinal);
        foreach (var resource in new ReadOnlySpan<ReflectedResource>((void*)pointer, (int)bufferCount))
        {
            if (HasDecoration(compiler, resource.Id, DescriptorSet) == 0 || HasDecoration(compiler, resource.Id, Binding) == 0 ||
                GetDecoration(compiler, resource.Id, DescriptorSet) != (fragment ? 3u : 1u))
                throw new NotSupportedException($"Uniform buffers require an explicit binding and descriptor set {(fragment ? 3 : 1)}.");
            var binding = GetDecoration(compiler, resource.Id, Binding);
            if (binding >= bufferCount || sizes[binding] != 0)
                throw new NotSupportedException("Uniform buffer bindings must be unique and contiguous, starting at zero.");
            var type = GetType(compiler, resource.BaseTypeId);
            if (GetArrayDimensions(GetType(compiler, resource.TypeId)) != 0)
                throw new NotSupportedException("Arrays of uniform buffers are not supported by the canvas interface.");
            Check(GetStructSize(compiler, type, out var size));
            if (size == 0 || size > 16384) throw new NotSupportedException("Each canvas uniform buffer must contain between 1 and 16384 bytes.");
            sizes[binding] = checked(((int)size + 15) / 16 * 16);
            var occupied = new bool[sizes[binding]];
            for (uint member = 0; member < GetMemberCount(type); member++)
            {
                var name = Marshal.PtrToStringUTF8(GetMemberName(compiler, resource.BaseTypeId, member));
                if (string.IsNullOrWhiteSpace(name) || uniforms.ContainsKey(name))
                    throw new NotSupportedException("Material uniforms require nonblank names unique across all buffers; preserve names when compiling SPIR-V.");
                var field = GetType(compiler, GetMemberType(type, member));
                var width = GetVectorSize(field);
                var dimensions = GetArrayDimensions(field);
                var matrix = GetBaseType(field) == 13 && width == 2 && GetColumns(field) == 2;
                if (name == "TIME" && (!fragment || GetBaseType(field) != 13 || GetBitWidth(field) != 32 || width != 1 || GetColumns(field) != 1 || dimensions != 0))
                    throw new NotSupportedException("The built-in TIME requires a non-array float32 scalar in a fragment uniform buffer.");
                if (GetBitWidth(field) != 32 || GetColumns(field) != 1 && !matrix || dimensions > 1)
                    throw new NotSupportedException($"Uniform '{name}' requires a supported 32-bit scalar/vector, float2x2 matrix or a fixed one-dimensional array; other matrices and nested structs are not integrated yet.");
                var valueType = matrix ? typeof(Transform) : (GetBaseType(field), width) switch
                {
                    (13, 1) => typeof(float),
                    (13, 2) => typeof(Vector2),
                    (13, 3) => typeof(Color),
                    (13, 4) => typeof(Vector4),
                    (7, 1) => typeof(int),
                    (7, 2) => typeof(Vector2I),
                    (7, 4) => typeof(Vector4I),
                    (8, 1) => typeof(uint),
                    (8, 2) => typeof(Vector2I),
                    (8, 4) => typeof(Vector4I),
                    _ => throw new NotSupportedException($"Uniform '{name}' has no integrated typed material mapping.")
                };
                Check(GetMemberOffset(compiler, type, member, out var offset));
                var matrixStride = 0;
                var rowMajor = false;
                if (matrix)
                {
                    rowMajor = HasMemberDecoration(compiler, resource.BaseTypeId, member, 4) != 0;
                    if (rowMajor == (HasMemberDecoration(compiler, resource.BaseTypeId, member, 5) != 0) ||
                        HasMemberDecoration(compiler, resource.BaseTypeId, member, 7) == 0)
                        throw new NotSupportedException($"Matrix '{name}' requires one explicit storage order and MatrixStride.");
                    var reflectedStride = GetMemberDecoration(compiler, resource.BaseTypeId, member, 7);
                    if (reflectedStride < 16 || reflectedStride > 16368 || reflectedStride % 16 != 0)
                        throw new NotSupportedException($"Matrix '{name}' must use std140 matrix stride within the buffer limit.");
                    matrixStride = (int)reflectedStride;
                }
                var elementSize = matrix ? matrixStride + 8 : (int)width * 4;
                var length = 0;
                uint stride = (uint)elementSize;
                if (dimensions == 1)
                {
                    if (ArrayIsLiteral(field, 0) == 0 || GetArrayLength(field, 0) is 0 or > 1024)
                        throw new NotSupportedException($"Uniform '{name}' requires an array with a fixed length from 1 to 1024.");
                    length = (int)GetArrayLength(field, 0);
                    Check(GetArrayStride(compiler, type, member, out stride));
                    if (stride < elementSize || stride % 16 != 0)
                        throw new NotSupportedException($"Uniform '{name}' must use std140 array stride.");
                }
                var alignment = dimensions == 1 || matrix || width == 3 ? 16 : elementSize;
                var extent = (ulong)offset + (ulong)(Math.Max(1, length) - 1) * stride + (uint)elementSize;
                if (offset % alignment != 0 || extent > size)
                    throw new NotSupportedException($"Uniform '{name}' must fit inside its buffer with std140 alignment.");
                Check(GetMemberSize(compiler, type, member, out var memberSize));
                if ((ulong)offset + memberSize > (ulong)sizes[binding])
                    throw new ArgumentException($"Uniform '{name}' extends outside its buffer.", nameof(code));
                for (var i = (int)offset; i < (int)offset + (int)memberSize; i++)
                {
                    if (occupied[i]) throw new ArgumentException($"Uniform '{name}' overlaps another member.", nameof(code));
                    occupied[i] = true;
                }
                uniforms.Add(name, new(name, valueType, (int)binding, (int)offset, elementSize, length, (int)stride, GetBaseType(field) == 8, matrixStride, rowMajor));
            }
        }
        Check(GetResources(resources, 7, out var combinedPointer, out var combinedCount));
        Check(GetResources(resources, 10, out var imagePointer, out var imageCount));
        Check(GetResources(resources, 11, out var samplerPointer, out var samplerCount));
        if (combinedCount != 0 && (imageCount != 0 || samplerCount != 0) || imageCount != samplerCount)
            throw new NotSupportedException("Use combined sampled images or separate images paired one-to-one with samplers.");
        var textureCount = combinedCount + imageCount;
        if (textureCount > 16 || !fragment && textureCount != 0)
            throw new NotSupportedException("The canvas fragment interface supports at most sixteen texture bindings; vertex textures are not integrated.");
        var textures = new ShaderTexture[(int)textureCount];
        uint TextureBinding(ReflectedResource resource)
        {
            if (HasDecoration(compiler, resource.Id, DescriptorSet) == 0 || HasDecoration(compiler, resource.Id, Binding) == 0 ||
                GetDecoration(compiler, resource.Id, DescriptorSet) != 2)
                throw new NotSupportedException("Fragment texture/sampler resources require explicit bindings in descriptor set 2.");
            var slot = GetDecoration(compiler, resource.Id, Binding);
            if (slot >= textureCount) throw new NotSupportedException("Texture bindings must be contiguous, starting at zero.");
            if (GetArrayDimensions(GetType(compiler, resource.TypeId)) != 0)
                throw new NotSupportedException("Texture binding arrays are not integrated yet; use individual named bindings.");
            return slot;
        }
        foreach (var resource in new ReadOnlySpan<ReflectedResource>((void*)(combinedCount != 0 ? combinedPointer : imagePointer), (int)textureCount))
        {
            var slot = TextureBinding(resource);
            var name = Marshal.PtrToStringUTF8(resource.Name);
            if (name == "TIME") throw new NotSupportedException("The built-in TIME is a float32 uniform, not a texture parameter.");
            if (name == "TEXTURE" && slot != 0) throw new NotSupportedException("The built-in TEXTURE requires binding zero in descriptor set 2.");
            if (string.IsNullOrWhiteSpace(name) || uniforms.ContainsKey(name) || textures.Any(t => t is not null && t.Name == name) || textures[slot] is not null)
                throw new NotSupportedException("Texture names and bindings must be unique and must not collide with material uniforms.");
            var imageType = GetType(compiler, resource.BaseTypeId);
            var sampleType = GetType(compiler, GetSampledType(imageType));
            if (GetImageDimension(imageType) != 1 || ImageIsArray(imageType) != 0 || ImageIsDepth(imageType) != 0 ||
                ImageIsMultisampled(imageType) != 0 || ImageIsStorage(imageType) != 0 || GetBaseType(sampleType) != 13 || GetBitWidth(sampleType) != 32)
                throw new NotSupportedException($"Texture '{name}' requires an ordinary non-array, non-depth 2D float-sampled image.");
            textures[slot] = new(name, (int)slot);
        }
        var samplerSlots = new bool[(int)samplerCount];
        foreach (var resource in new ReadOnlySpan<ReflectedResource>((void*)samplerPointer, (int)samplerCount))
        {
            var slot = TextureBinding(resource);
            if (samplerSlots[slot]) throw new NotSupportedException("Each texture requires exactly one sampler at the same binding.");
            samplerSlots[slot] = true;
        }
        if (imageCount != 0)
        {
            Check(BuildCombinedSamplers(compiler));
            Check(GetCombinedSamplers(compiler, out var pairs, out var pairCount));
            foreach (var pair in new ReadOnlySpan<CombinedSampler>((void*)pairs, (int)pairCount))
                if (GetDecoration(compiler, pair.Image, Binding) != GetDecoration(compiler, pair.Sampler, Binding))
                    throw new NotSupportedException("An image must be sampled with a sampler at the same binding.");
        }
        uniforms.Remove("TIME", out var timeUniform);
        return new ShaderProgram(code, sizes, uniforms, textures, timeUniform);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ReflectedResource { internal uint Id, BaseTypeId, TypeId; internal nint Name; }

    [StructLayout(LayoutKind.Sequential)]
    private struct CombinedSampler { internal uint Id, Image, Sampler; }

    [LibraryImport(Library, EntryPoint = "spvc_context_create"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int CreateContext(out nint context);
    [LibraryImport(Library, EntryPoint = "spvc_context_destroy"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void DestroyContext(nint context);
    [LibraryImport(Library, EntryPoint = "spvc_context_get_last_error_string"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint GetError(nint context);
    [LibraryImport(Library, EntryPoint = "spvc_context_parse_spirv"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Parse(nint context, nint words, nuint count, out nint ir);
    [LibraryImport(Library, EntryPoint = "spvc_context_create_compiler"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int CreateCompiler(nint context, int backend, nint ir, int capture, out nint compiler);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_get_active_interface_variables"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int GetActiveVariables(nint compiler, out nint active);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_create_shader_resources_for_active_variables"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int CreateResources(nint compiler, out nint resources, nint active);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_build_combined_image_samplers"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int BuildCombinedSamplers(nint compiler);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_get_combined_image_samplers"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int GetCombinedSamplers(nint compiler, out nint samplers, out nuint count);
    [LibraryImport(Library, EntryPoint = "spvc_resources_get_resource_list_for_type"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int GetResources(nint resources, int kind, out nint pointer, out nuint count);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_has_decoration"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial byte HasDecoration(nint compiler, uint id, int decoration);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_get_decoration"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint GetDecoration(nint compiler, uint id, int decoration);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_has_member_decoration"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial byte HasMemberDecoration(nint compiler, uint type, uint member, int decoration);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_get_member_decoration"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint GetMemberDecoration(nint compiler, uint type, uint member, int decoration);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_get_type_handle"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint GetType(nint compiler, uint id);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_num_array_dimensions"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint GetArrayDimensions(nint type);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_basetype"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int GetBaseType(nint type);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_bit_width"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint GetBitWidth(nint type);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_vector_size"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint GetVectorSize(nint type);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_columns"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint GetColumns(nint type);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_num_member_types"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint GetMemberCount(nint type);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_member_type"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint GetMemberType(nint type, uint member);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_get_member_name"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint GetMemberName(nint compiler, uint type, uint member);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_get_declared_struct_size"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int GetStructSize(nint compiler, nint type, out nuint size);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_get_declared_struct_member_size"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int GetMemberSize(nint compiler, nint type, uint member, out nuint size);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_type_struct_member_offset"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int GetMemberOffset(nint compiler, nint type, uint member, out uint offset);
    [LibraryImport(Library, EntryPoint = "spvc_type_array_dimension_is_literal"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial byte ArrayIsLiteral(nint type, uint dimension);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_array_dimension"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint GetArrayLength(nint type, uint dimension);
    [LibraryImport(Library, EntryPoint = "spvc_compiler_type_struct_member_array_stride"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int GetArrayStride(nint compiler, nint type, uint member, out uint stride);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_image_sampled_type"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint GetSampledType(nint type);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_image_dimension"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int GetImageDimension(nint type);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_image_arrayed"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial byte ImageIsArray(nint type);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_image_is_depth"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial byte ImageIsDepth(nint type);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_image_multisampled"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial byte ImageIsMultisampled(nint type);
    [LibraryImport(Library, EntryPoint = "spvc_type_get_image_is_storage"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial byte ImageIsStorage(nint type);
}
