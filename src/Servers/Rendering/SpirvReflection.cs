using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

// Member reflection uses the SPIRV-Cross C library already shipped by the SDL shadercross package.
// All reflected pointers belong to a single context and are copied before that context is destroyed.
internal static unsafe partial class SpirvReflection
{
    private const string Library = "spirv-cross-c-shared";
    private const int Binding = 33, DescriptorSet = 34;

    internal static Dictionary<(uint Set, int Slot), uint> DeviceUniformSizes(byte[] code)
    {
        if (CreateContext(out var context) != 0 || context == 0) throw new InvalidOperationException("Could not create the device reflection context.");
        using var owner = new RenderHandle(context, DestroyContext);
        void Check(int result) { if (result != 0) throw new ArgumentException("Device reflection failed: " + Marshal.PtrToStringUTF8(GetError(context)), nameof(code)); }
        nint ir; fixed (byte* pointer = code) Check(Parse(context, (nint)pointer, (nuint)(code.Length / 4), out ir));
        Check(CreateCompiler(context, 0, ir, 1, out var compiler)); Check(GetActiveVariables(compiler, out var active)); Check(CreateResources(compiler, out var resources, active));
        Check(GetResources(resources, 1, out var fields, out var count));
        var result = new Dictionary<(uint Set, int Slot), uint>();
        foreach (var resource in new ReadOnlySpan<ReflectedResource>((void*)fields, checked((int)count)))
        {
            Check(GetStructSize(compiler, GetType(compiler, resource.BaseTypeId), out var bytes));
            if (bytes is 0 or > 16384) throw new NotSupportedException("Device uniform blocks require a positive size at most sixteen KiB.");
            result.Add((GetDecoration(compiler, resource.Id, DescriptorSet), checked((int)GetDecoration(compiler, resource.Id, Binding))), checked((uint)bytes));
        }
        return result;
    }

    internal static ShaderProgram Read(byte[] code, bool fragment,
        IReadOnlyDictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)>? sourceTypes = null, bool blit = false, bool particles = false)
    {
        var light = CanvasLightProgram.Read(code);
        var surface = CanvasSurfaceProgram.Read(code);
        if ((light is not null || surface is not null) && (!fragment || blit || particles)) throw new NotSupportedException("Authored surface/light functions belong to the canvas fragment domain.");
        var booleans = ReadBooleans(code);
        if (sourceTypes is not null && booleans.Count != 0)
            throw new ArgumentException("Source compilation must not supply existing shader type metadata.", nameof(code));
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
            if (count != 0 && !(particles && kind == 2)) throw new NotSupportedException("The shader domain does not support these storage or push-constant resources.");
        }
        if (particles) ValidateParticleStorage(compiler, resources);
        Check(GetResources(resources, 1, out var pointer, out var bufferCount));
        if (bufferCount > 4) throw new NotSupportedException("Canvas shaders support at most four uniform buffers per stage.");
        var sizes = new int[(int)bufferCount];
        var uniforms = new Dictionary<string, ShaderUniform>(StringComparer.Ordinal);
        var instanceBuffers = new HashSet<int>();
        var globalBuffers = new HashSet<int>();
        foreach (var resource in new ReadOnlySpan<ReflectedResource>((void*)pointer, (int)bufferCount))
        {
            if (HasDecoration(compiler, resource.Id, DescriptorSet) == 0 || HasDecoration(compiler, resource.Id, Binding) == 0 ||
                GetDecoration(compiler, resource.Id, DescriptorSet) != (particles ? 2u : fragment ? 3u : 1u))
                throw new NotSupportedException($"Uniform buffers require an explicit binding and descriptor set {(particles ? 2 : fragment ? 3 : 1)}.");
            var binding = GetDecoration(compiler, resource.Id, Binding);
            if (binding >= bufferCount || sizes[binding] != 0)
                throw new NotSupportedException("Uniform buffer bindings must be unique and contiguous, starting at zero.");
            var typeName = Marshal.PtrToStringUTF8(GetName(compiler, resource.BaseTypeId));
            if (typeName is "Globals" or "type.Globals") globalBuffers.Add((int)binding);
            if (typeName is "Instance" or "type.Instance") instanceBuffers.Add((int)binding);
            var type = GetType(compiler, resource.BaseTypeId);
            if (GetArrayDimensions(GetType(compiler, resource.TypeId)) != 0)
                throw new NotSupportedException("Arrays of uniform buffers are not supported by the canvas interface.");
            Check(GetStructSize(compiler, type, out var size));
            if (size == 0 || size > 16384) throw new NotSupportedException("Each canvas uniform buffer must contain between 1 and 16384 bytes.");
            sizes[binding] = checked(((int)size + 15) / 16 * 16);
            if (particles && binding == 0)
            {
                ValidateParticleFrame(compiler, type, typeName, size);
                continue;
            }
            var occupied = new bool[sizes[binding]];
            for (uint member = 0; member < GetMemberCount(type); member++)
            {
                var name = Marshal.PtrToStringUTF8(GetMemberName(compiler, resource.BaseTypeId, member));
                if (string.IsNullOrWhiteSpace(name) || uniforms.ContainsKey(name))
                    throw new NotSupportedException("Material uniforms require nonblank names unique across all buffers; preserve names when compiling SPIR-V.");
                var field = GetType(compiler, GetMemberType(type, member));
                var width = GetVectorSize(field);
                var dimensions = GetArrayDimensions(field);
                var columns = GetColumns(field);
                var matrix = GetBaseType(field) == 13 && columns is >= 2 and <= 4 && width is >= 2 and <= 4;
                if (name is "MODEL_MATRIX" or "CANVAS_MATRIX" or "SCREEN_MATRIX" &&
                    (fragment || blit || particles || GetBaseType(field) != 13 || GetBitWidth(field) != 32 || width != 4 || columns != 4 || dimensions != 0 || instanceBuffers.Contains((int)binding) || globalBuffers.Contains((int)binding)))
                    throw new NotSupportedException($"{name} requires a renderer-owned canvas-vertex non-array float32 matrix with four columns and rows.");
                if (name == "TIME" && (GetBaseType(field) != 13 || GetBitWidth(field) != 32 || width != 1 || GetColumns(field) != 1 || dimensions != 0))
                    throw new NotSupportedException("The built-in TIME requires a non-array float32 scalar in a stage uniform buffer.");
                if (name == "SCREEN_PIXEL_SIZE" && (GetBaseType(field) != 13 || GetBitWidth(field) != 32 || width != 2 || GetColumns(field) != 1 || dimensions != 0))
                    throw new NotSupportedException("The built-in SCREEN_PIXEL_SIZE requires a non-array float32 vector of width two in a stage uniform buffer.");
                if (name == "TEXTURE_PIXEL_SIZE" && (blit || particles || GetBaseType(field) != 13 || GetBitWidth(field) != 32 || width != 2 || columns != 1 || dimensions != 0))
                    throw new NotSupportedException("TEXTURE_PIXEL_SIZE requires a canvas-stage non-array float32 vector of width two.");
                if (name == "NORMAL_MAP_FLAGS" && (!fragment || blit || particles || GetBaseType(field) != 13 || GetBitWidth(field) != 32 || width != 4 || columns != 1 || dimensions != 0))
                    throw new NotSupportedException("NORMAL_MAP_FLAGS requires a canvas-fragment non-array float32 vector of width four.");
                if (name is "NORMAL_MODEL_BASIS" or "NORMAL_CANVAS_BASIS" or "REGION_RECT" or "CANVAS_SPECULAR" && (!fragment || blit || particles || GetBaseType(field) != 13 || GetBitWidth(field) != 32 || width != 4 || columns != 1 || dimensions != 0 || instanceBuffers.Contains((int)binding) || globalBuffers.Contains((int)binding)))
                    throw new NotSupportedException("Renderer-owned canvas fragment vectors require non-array float32 vectors of width four in ordinary renderer buffers.");
                if (name == "SDF_COORDS" && (GetBaseType(field) != 13 || GetBitWidth(field) != 32 || width != 4 || GetColumns(field) != 1 || dimensions != 0))
                    throw new NotSupportedException("SDF_COORDS requires a non-array float32 vector of width four in a stage uniform buffer.");
                if (name is "SCREEN_TEXTURE" or "SDF_TEXTURE" or "NORMAL_TEXTURE" or "SPECULAR_SHININESS_TEXTURE") throw new NotSupportedException("Renderer-owned texture names require sampled texture bindings.");
                if (GetBitWidth(field) != 32 || GetColumns(field) != 1 && !matrix || dimensions > 1)
                    throw new NotSupportedException($"Uniform '{name}' requires a supported 32-bit scalar/vector, float matrix with two through four columns/rows or a fixed one-dimensional array; nested structs are not supported.");
                booleans.Remove(name, out var logical);
                if (sourceTypes is not null && !sourceTypes.TryGetValue(((int)binding, name), out logical))
                    throw new NotSupportedException($"Source reflection did not retain uniform '{name}'.");
                if (logical.Width != 0 && (GetBaseType(field) != 8 || GetColumns(field) != 1 || width != logical.Width))
                    throw new ArgumentException($"Boolean metadata for '{name}' does not match its unsigned 32-bit storage.", nameof(code));
                var valueType = logical.Width != 0 ? (logical.Width == 1 ? typeof(bool) : typeof(int)) : matrix ? (width == 2 && columns == 2 ? typeof(Transform) : typeof(float[])) : (GetBaseType(field), width) switch
                {
                    (13, 1) => typeof(float),
                    (13, 2) => typeof(Vector2),
                    (13, 3) => typeof(Vector3),
                    (13, 4) => typeof(Vector4),
                    (7, 1) => typeof(int),
                    (7, 2) => typeof(Vector2i),
                    (7, 3) => typeof(Vector3i),
                    (7, 4) => typeof(Vector4i),
                    (8, 1) => typeof(uint),
                    (8, 2) => typeof(Vector2i),
                    (8, 3) => typeof(Vector3i),
                    (8, 4) => typeof(Vector4i),
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
                var elementSize = matrix ? ((int)(rowMajor ? width : columns) - 1) * matrixStride + (int)(rowMajor ? columns : width) * 4 : (int)width * 4;
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
                if (logical.Width != 0 && length != logical.Length)
                    throw new ArgumentException($"Boolean metadata for '{name}' does not match its array shape.", nameof(code));
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
                uniforms.Add(name, new(name, valueType, (int)binding, (int)offset, elementSize, length, (int)stride, GetBaseType(field) == 8, matrixStride, rowMajor, logical.Width, matrix && valueType != typeof(Transform) ? (int)columns : 0, matrix && valueType != typeof(Transform) ? (int)width : 0));
            }
        }
        Check(GetResources(resources, 7, out var combinedPointer, out var combinedCount));
        Check(GetResources(resources, 10, out var imagePointer, out var imageCount));
        Check(GetResources(resources, 11, out var samplerPointer, out var samplerCount));
        if (combinedCount != 0 && (imageCount != 0 || samplerCount != 0) || samplerCount > imageCount)
            throw new NotSupportedException("Use combined sampled images or separate images paired one-to-one with samplers.");
        var textureCount = combinedCount + imageCount;
        if (textureCount > 16)
            throw new NotSupportedException("Each canvas stage supports at most sixteen sampled image bindings.");
        var textures = new ShaderTexture[(int)textureCount];
        var unnormalizedUnsafe = CanvasSamplerArrays.CoordinateSamplingRequirements(code);
        uint TextureBinding(ReflectedResource resource)
        {
            if (HasDecoration(compiler, resource.Id, DescriptorSet) == 0 || HasDecoration(compiler, resource.Id, Binding) == 0 ||
                GetDecoration(compiler, resource.Id, DescriptorSet) != (particles ? 0u : fragment ? 2u : 0u))
                throw new NotSupportedException($"Sampled image resources require explicit bindings in descriptor set {(particles ? 0 : fragment ? 2 : 0)}.");
            var slot = GetDecoration(compiler, resource.Id, Binding);
            if (slot >= textureCount) throw new NotSupportedException("Texture bindings must be contiguous, starting at zero.");
            var type = GetType(compiler, resource.TypeId);
            var dimensions = GetArrayDimensions(type);
            if (dimensions > 1 || dimensions == 1 && (ArrayIsLiteral(type, 0) == 0 || GetArrayLength(type, 0) is 0 or > 16))
                throw new NotSupportedException("Sampled image bindings require scalar resources or fixed arrays of one through sixteen elements.");
            return slot;
        }
        foreach (var resource in new ReadOnlySpan<ReflectedResource>((void*)(combinedCount != 0 ? combinedPointer : imagePointer), (int)textureCount))
        {
            var slot = TextureBinding(resource);
            var name = Marshal.PtrToStringUTF8(resource.Name);
            if (name is "TIME" or "SCREEN_PIXEL_SIZE" or "SDF_COORDS" or "NORMAL_MAP_FLAGS" or "NORMAL_MODEL_BASIS" or "NORMAL_CANVAS_BASIS" or "REGION_RECT" or "CANVAS_SPECULAR" or "TEXTURE_PIXEL_SIZE" or "MODEL_MATRIX" or "CANVAS_MATRIX" or "SCREEN_MATRIX") throw new NotSupportedException($"The built-in {name} requires a renderer-owned uniform, not a texture parameter.");
            if (name == "TEXTURE" && slot != 0) throw new NotSupportedException("The built-in TEXTURE requires binding zero in its stage descriptor set.");
            if (string.IsNullOrWhiteSpace(name) || uniforms.ContainsKey(name) || textures.Any(t => t is not null && t.Name == name) || textures[slot] is not null)
                throw new NotSupportedException("Texture names and bindings must be unique and must not collide with material uniforms.");
            var imageType = GetType(compiler, resource.BaseTypeId);
            var sampleType = GetType(compiler, GetSampledType(imageType));
            if (GetImageDimension(imageType) != 1 || ImageIsDepth(imageType) != 0 ||
                ImageIsMultisampled(imageType) != 0 || ImageIsStorage(imageType) != 0 || GetBaseType(sampleType) is not (7 or 8 or 13) || GetBitWidth(sampleType) != 32)
                throw new NotSupportedException($"Texture '{name}' requires a non-depth 2D or image-array float, signed or unsigned scalar image.");
            var array = ImageIsArray(imageType) != 0;
            if (array && name is "TEXTURE" or "SCREEN_TEXTURE" or "SDF_TEXTURE" or "NORMAL_TEXTURE" or "SPECULAR_SHININESS_TEXTURE") throw new NotSupportedException("Renderer-owned samplers require ordinary 2D images.");
            if (name is "SCREEN_TEXTURE" or "SDF_TEXTURE" or "NORMAL_TEXTURE" or "SPECULAR_SHININESS_TEXTURE" && GetBaseType(sampleType) != 13) throw new NotSupportedException("Renderer-owned screen and distance images require float sampling.");
            var resourceType = GetType(compiler, resource.TypeId);
            var length = GetArrayDimensions(resourceType) == 0 ? 0 : (int)GetArrayLength(resourceType, 0);
            if (length != 0 && (name is "TEXTURE" or "SCREEN_TEXTURE" or "SDF_TEXTURE" or "NORMAL_TEXTURE" or "SPECULAR_SHININESS_TEXTURE" || name.StartsWith("GLOBAL_", StringComparison.Ordinal)))
                throw new NotSupportedException("Renderer-owned and global images require scalar descriptor bindings.");
            var blitSource = blit && name is "SOURCE_TEXTURE0" or "SOURCE_TEXTURE1" or "SOURCE_TEXTURE2" or "SOURCE_TEXTURE3";
            if (blitSource && (array || length != 0 || GetBaseType(sampleType) != 13)) throw new NotSupportedException("Blit source samplers require scalar float 2D images.");
            textures[slot] = new(name, (int)slot, array, GetBaseType(sampleType) switch { 7 => 1, 8 => 2, _ => 0 }, length, BlitSource: blitSource) { UnnormalizedUnsafe = unnormalizedUnsafe.GetValueOrDefault((particles ? 0u : fragment ? 2u : 0u, (int)slot)), UsesSampler = unnormalizedUnsafe.ContainsKey((particles ? 0u : fragment ? 2u : 0u, (int)slot)) };
        }
        if ((!fragment || blit || particles) && textures.Any(t => t.IsNormalTexture || t.IsSpecularTexture))
            throw new NotSupportedException("Canvas normal and specular images require the canvas fragment stage.");
        ReadImages(code, textures, fragment, !blit && !particles, uniforms);
        var samplerSlots = new bool[(int)textureCount];
        foreach (var resource in new ReadOnlySpan<ReflectedResource>((void*)samplerPointer, (int)samplerCount))
        {
            var slot = TextureBinding(resource);
            if (samplerSlots[slot]) throw new NotSupportedException("Each texture requires exactly one sampler at the same binding.");
            var type = GetType(compiler, resource.TypeId);
            var length = GetArrayDimensions(type) == 0 ? 0 : (int)GetArrayLength(type, 0);
            if (textures[slot].ArrayLength != length) throw new NotSupportedException("Paired images and samplers require the same descriptor array length.");
            samplerSlots[slot] = true;
        }
        var needsSampler = false;
        if (imageCount != 0 && textures.All(t => t.ArrayLength == 0))
        {
            Check(BuildDummySampler(compiler, out var dummy)); needsSampler = dummy != 0;
            Check(BuildCombinedSamplers(compiler));
            Check(GetCombinedSamplers(compiler, out var pairs, out var pairCount));
            foreach (var pair in new ReadOnlySpan<CombinedSampler>((void*)pairs, (int)pairCount))
                if (pair.Sampler != dummy && GetDecoration(compiler, pair.Image, Binding) != GetDecoration(compiler, pair.Sampler, Binding))
                    throw new NotSupportedException("An image must be sampled with a sampler at the same binding.");
        }
        textures = textures.SelectMany(texture => Enumerable.Range(0, texture.Count).Select(element => texture with { Element = element })).ToArray();
        if (textures.Length > 16) throw new NotSupportedException("Each canvas stage supports at most sixteen sampled image elements.");
        if (booleans.Count != 0) throw new ArgumentException("Shader type metadata names an absent or inactive uniform.", nameof(code));
        uniforms.Remove("TIME", out var timeUniform);
        uniforms.Remove("SCREEN_PIXEL_SIZE", out var screenPixelSizeUniform);
        uniforms.Remove("SDF_COORDS", out var sdfCoordsUniform);
        uniforms.Remove("NORMAL_MAP_FLAGS", out var normalMapFlagsUniform);
        uniforms.Remove("REGION_RECT", out var regionRectUniform);
        uniforms.Remove("CANVAS_SPECULAR", out var canvasSpecularUniform);
        uniforms.Remove("NORMAL_MODEL_BASIS", out var normalModelBasisUniform);
        uniforms.Remove("NORMAL_CANVAS_BASIS", out var normalCanvasBasisUniform);
        uniforms.Remove("TEXTURE_PIXEL_SIZE", out var texturePixelSizeUniform);
        uniforms.Remove("MODEL_MATRIX", out var modelMatrixUniform);
        uniforms.Remove("CANVAS_MATRIX", out var canvasMatrixUniform);
        uniforms.Remove("SCREEN_MATRIX", out var screenMatrixUniform);
        var instances = new Dictionary<string, ShaderUniform>(StringComparer.Ordinal);
        foreach (var pair in uniforms.ToArray()) if (instanceBuffers.Contains(pair.Value.Buffer))
            {
                if (pair.Value.ArrayLength != 0 || pair.Value.MatrixStride != 0) throw new NotSupportedException("Instance parameters support only scalar and vector values.");
                instances.Add(pair.Key, pair.Value); uniforms.Remove(pair.Key);
            }
        if (instances.Count > 16) throw new NotSupportedException("A shader supports at most sixteen instance parameters.");
        if (instanceBuffers.Any(b => timeUniform?.Buffer == b || screenPixelSizeUniform?.Buffer == b || sdfCoordsUniform?.Buffer == b || normalMapFlagsUniform?.Buffer == b || texturePixelSizeUniform?.Buffer == b))
            throw new NotSupportedException("Renderer-owned uniforms cannot be declared in the Instance block.");
        var globals = new Dictionary<string, ShaderUniform>(StringComparer.Ordinal);
        foreach (var pair in uniforms.ToArray()) if (globalBuffers.Contains(pair.Value.Buffer))
            {
                if (pair.Value.ArrayLength != 0) throw new NotSupportedException("Global parameters do not declare arrays.");
                ShaderGlobalParameters.ValidateName(pair.Key);
                globals.Add(pair.Key, pair.Value); uniforms.Remove(pair.Key);
            }
        if (globalBuffers.Any(b => timeUniform?.Buffer == b || screenPixelSizeUniform?.Buffer == b || sdfCoordsUniform?.Buffer == b || normalMapFlagsUniform?.Buffer == b || texturePixelSizeUniform?.Buffer == b))
            throw new NotSupportedException("Renderer-owned uniforms cannot be declared in Globals.");
        foreach (var texture in textures) if (texture.IsGlobal)
            {
                ShaderGlobalParameters.ValidateName(texture.GlobalName);
                if (string.IsNullOrWhiteSpace(texture.GlobalName) || globals.ContainsKey(texture.GlobalName) || uniforms.ContainsKey(texture.GlobalName) || instances.ContainsKey(texture.GlobalName))
                    throw new NotSupportedException("Global image names must be nonblank and cannot alias numeric or material fields.");
            }
        if (particles && (sizes.Length == 0 || sizes[0] != ParticleFrameLayout.ByteSize || instances.Count != 0 || screenPixelSizeUniform is not null || textures.Any(t => t.IsCanvasTexture || t.IsScreenTexture || t.IsNormalTexture || t.IsSpecularTexture)))
            throw new NotSupportedException("Particle programs require their renderer-owned frame and have no canvas command, screen or instance bindings.");
        ReadDefaults(code, uniforms, instances);
        ReadHints(code, uniforms, instances, fragment);
        var lowered = CanvasSamplerArrays.Lower(code, textures, needsSampler);
        return new ShaderProgram(code, sizes, uniforms, textures, timeUniform, screenPixelSizeUniform, sdfCoordsUniform, instances, globals)
        {
            NativeCode = CanvasSurfaceProgram.RemoveMarker(CanvasLightProgram.RemoveMarker(lowered, light), surface),
            ValidationCode = light is null && surface is null ? null : lowered,
            AuthoredLight = light,
            Surface = surface,
            NormalMapFlagsUniform = normalMapFlagsUniform,
            NormalModelBasisUniform = normalModelBasisUniform,
            NormalCanvasBasisUniform = normalCanvasBasisUniform,
            TexturePixelSizeUniform = texturePixelSizeUniform,
            RegionRectUniform = regionRectUniform,
            CanvasSpecularUniform = canvasSpecularUniform,
            ModelMatrixUniform = modelMatrixUniform,
            CanvasMatrixUniform = canvasMatrixUniform,
            ScreenMatrixUniform = screenMatrixUniform
        };
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
    [LibraryImport(Library, EntryPoint = "spvc_compiler_build_dummy_sampler_for_combined_images"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int BuildDummySampler(nint compiler, out uint id);
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
    [LibraryImport(Library, EntryPoint = "spvc_compiler_get_name"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint GetName(nint compiler, uint id);
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
