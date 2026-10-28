using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    /// <summary>Selects an effective graphics, compute or resource limit on this device.</summary>
    public enum Limit
    {
        /// <summary>Maximum simultaneously populated logical descriptor sets; set numbers remain sparse.</summary>
        MaxBoundUniformSets = 0,
        /// <summary>Maximum color targets in one native framebuffer pass.</summary>
        MaxFramebufferColorAttachments = 1,
        /// <summary>Maximum sampled image descriptors in one logical set across graphics stages.</summary>
        MaxTexturesPerUniformSet = 2,
        /// <summary>Maximum sampler descriptors in one logical set across graphics stages.</summary>
        MaxSamplersPerUniformSet = 3,
        /// <summary>Maximum storage buffer descriptors in one logical set across graphics stages.</summary>
        MaxStorageBuffersPerUniformSet = 4,
        /// <summary>Maximum storage image descriptors in one logical set across graphics stages.</summary>
        MaxStorageImagesPerUniformSet = 5,
        /// <summary>Maximum uniform buffer descriptors in one logical set across graphics stages.</summary>
        MaxUniformBuffersPerUniformSet = 6,
        /// <summary>Maximum ordinary unsigned index value; enabled restart markers retain their own role.</summary>
        MaxDrawIndexedIndex = 7,
        /// <summary>Maximum framebuffer height in pixels.</summary>
        MaxFramebufferHeight = 8,
        /// <summary>Maximum framebuffer width in pixels.</summary>
        MaxFramebufferWidth = 9,
        /// <summary>Maximum color image array layers; depth initialization has its separate 256-layer bound.</summary>
        MaxTextureArrayLayers = 10,
        /// <summary>Maximum ordinary image width or height in texels, subject to format and byte capacity.</summary>
        MaxTextureSize2D = 12,
        /// <summary>Maximum sampled image slots in one shader stage.</summary>
        MaxTexturesPerShaderStage = 15,
        /// <summary>Maximum sampler slots in one shader stage.</summary>
        MaxSamplersPerShaderStage = 16,
        /// <summary>Maximum storage buffer slots in one shader stage.</summary>
        MaxStorageBuffersPerShaderStage = 17,
        /// <summary>Maximum storage image slots in one shader stage.</summary>
        MaxStorageImagesPerShaderStage = 18,
        /// <summary>Maximum uniform slots in one stage, including a slot used by lowered push data.</summary>
        MaxUniformBuffersPerShaderStage = 19,
        /// <summary>Maximum copied push data in bytes.</summary>
        MaxPushConstantSize = 20,
        /// <summary>Maximum ordinary uniform buffer capacity in bytes.</summary>
        MaxUniformBufferSize = 21,
        /// <summary>Maximum byte offset of an attribute, subject to its format and stride.</summary>
        MaxVertexInputAttributeOffset = 22,
        /// <summary>Maximum vertex attribute count and location capacity.</summary>
        MaxVertexInputAttributes = 23,
        /// <summary>Maximum contiguous vertex input bindings.</summary>
        MaxVertexInputBindings = 24,
        /// <summary>Maximum vertex input byte stride.</summary>
        MaxVertexInputBindingStride = 25,
        /// <summary>Native uniform suballocation alignment in bytes; public uniform RIDs begin at zero.</summary>
        MinUniformBufferOffsetAlignment = 26,
        /// <summary>Maximum native compute workgroup shared storage in bytes.</summary>
        MaxComputeSharedMemorySize = 27,
        /// <summary>Maximum direct or GPU-authored indirect X workgroup count.</summary>
        MaxComputeWorkgroupCountX = 28,
        /// <summary>Maximum direct or GPU-authored indirect Y workgroup count.</summary>
        MaxComputeWorkgroupCountY = 29,
        /// <summary>Maximum direct or GPU-authored indirect Z workgroup count.</summary>
        MaxComputeWorkgroupCountZ = 30,
        /// <summary>Maximum product of local workgroup dimensions.</summary>
        MaxComputeWorkgroupInvocations = 31,
        /// <summary>Maximum local X workgroup dimension.</summary>
        MaxComputeWorkgroupSizeX = 32,
        /// <summary>Maximum local Y workgroup dimension.</summary>
        MaxComputeWorkgroupSizeY = 33,
        /// <summary>Maximum local Z workgroup dimension.</summary>
        MaxComputeWorkgroupSizeZ = 34,
        /// <summary>Maximum viewport width in pixels.</summary>
        MaxViewportDimensionsX = 35,
        /// <summary>Maximum viewport height in pixels.</summary>
        MaxViewportDimensionsY = 36,
    }

    private ulong[]? _deviceLimits;
    private ulong[] DeviceLimits => _deviceLimits ??= ReadDeviceLimits(Device);
    private ulong[]? ValidationLimits => _context.Driver == "vulkan" ? DeviceLimits : null;

    /// <summary>Reads the selected native limit after applying the executing resource profile.</summary>
    /// <param name="limit">A defined two-dimensional resource, graphics or compute category.</param>
    /// <returns>The maximum count, extent or byte quantity; the alignment category returns a minimum alignment.</returns>
    /// <remarks>The immutable snapshot is prepared once per device. Queries do not submit or wait for GPU work.
    /// Sparse logical set IDs are remapped; MaxBoundUniformSets limits their populated count, not their numeric IDs.
    /// Per-set budgets span both graphics stages; per-stage and shared native category constraints also apply.
    /// Format, role, simultaneous resources, available memory and shader compilation can impose additional restrictions.
    /// Indirect group counts, GPU-produced indices and shared workgroup storage remain the shader author's responsibility.
    /// The current native adapter requires pinned SDL 3.4.18 Vulkan; other drivers reject explicitly.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined.</exception>
    /// <exception cref="NotSupportedException">The native driver or core has no compatible limit adapter.</exception>
    /// <exception cref="InvalidOperationException">The native limit snapshot is inconsistent.</exception>
    public long LimitGet(Limit limit)
    {
        Access();
        if (!Enum.IsDefined(limit)) throw new ArgumentOutOfRangeException(nameof(limit));
        return checked((long)DeviceLimits[(int)limit]);
    }

    private static ulong[] ReadDeviceLimits(nint device)
    {
        var values = new ulong[37];
        fixed (ulong* output = values)
        {
            var result = ReadNativeLimits(device, 1, output, 37);
            if (result == 0) throw new NotSupportedException("Device limit queries require the pinned SDL 3.4.18 Vulkan adapter.");
            if (result != 1) throw new InvalidOperationException("The native device returned an inconsistent limit snapshot.");
        }
        return EffectiveDeviceLimits(values);
    }

    internal static ulong[] EffectiveDeviceLimits(ulong[] values)
    {
        if (values.Length != 37 || values.Where((v, i) => i is not (11 or 13 or 14)).Any(v => v == 0 || v > long.MaxValue))
            throw new InvalidOperationException("The native device returned an inconsistent limit snapshot.");
        values[15] = values[16] = Math.Min(Math.Min(values[15], values[16]), 16);
        foreach (var i in new[] { 17, 18 }) values[i] = Math.Min(values[i], 8);
        values[19] = Math.Min(values[19], 4);
        for (var i = 2; i <= 6; i++) values[i] = Math.Min(values[i], 2 * values[i + 13]);
        // Native descriptor sets are private; each populated logical set needs at least one public descriptor.
        values[0] = values[2] + values[3] + values[4] + values[5] + values[6];
        values[1] = Math.Min(values[1], 8);
        values[20] = Math.Min(values[21], MaximumPushBytes);
        // The pinned Vulkan backend binds dynamic uniform descriptors to 4 KiB sections.
        values[21] = Math.Min(values[21], 4096);
        values[23] = Math.Min(values[23], 16); values[24] = Math.Min(values[24], 16);
        values[25] = Math.Min(values[25], 2048); values[22] = Math.Min(values[22], values[25] - 1);
        for (var i = 28; i <= 30; i++) values[i] = Math.Min(values[i], 65535);
        for (var i = 31; i <= 34; i++) values[i] = Math.Min(values[i], 1024);
        foreach (var i in new[] { 8, 9, 10, 12, 35, 36 }) values[i] = Math.Min(values[i], int.MaxValue);
        values[8] = Math.Min(values[8], values[12]); values[9] = Math.Min(values[9], values[12]);
        values[35] = Math.Min(values[35], values[9]); values[36] = Math.Min(values[36], values[8]);
        return values;
    }

    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_limits")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int ReadNativeLimits(nint device, uint abi, ulong* output, uint count);
}
