using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    /// <summary>Selects a native object or driver-specific value exposed by this device.</summary>
    public enum DriverResource
    {
        /// <summary>The selected logical device.</summary>
        LogicalDevice = 0,
        /// <summary>The selected physical device.</summary>
        PhysicalDevice = 1,
        /// <summary>The top-level driver instance.</summary>
        TopmostObject = 2,
        /// <summary>The command queue used by the device.</summary>
        CommandQueue = 3,
        /// <summary>The native queue-family index where the driver has queue families. Zero can be valid.</summary>
        QueueFamily = 4,
        /// <summary>The active native image of a live texture.</summary>
        Texture = 5,
        /// <summary>The native sampled image view or its CPU shader-resource descriptor handle.</summary>
        TextureView = 6,
        /// <summary>The native data-format code of that image view.</summary>
        TextureDataFormat = 7,
        /// <summary>The native sampler object or its CPU descriptor handle.</summary>
        Sampler = 8,
        /// <summary>A retained native descriptor set when the backend exposes one.</summary>
        UniformSet = 9,
        /// <summary>The active native buffer allocation.</summary>
        Buffer = 10,
        /// <summary>The native compute pipeline.</summary>
        ComputePipeline = 11,
        /// <summary>The native graphics pipeline.</summary>
        RenderPipeline = 12,
    }

    /// <summary>Gets the active native identifier or numeric value for a driver resource.</summary>
    /// <param name="resource">The native resource kind to query.</param>
    /// <param name="rid">The live resource on this device. Device and queue queries ignore this identity.</param>
    /// <param name="index">Reserved argument. Its value is ignored.</param>
    /// <returns>The native handle bits or numeric code reported by the current driver.</returns>
    /// <remarks>The result borrows the current driver object and does not pin it, submit work or transfer ownership.
    /// Image and buffer handles can change when storage cycles or moves. A borrowed viewport texture follows its completed
    /// image. External use requires the driver's lifetime and synchronization rules.
    /// Direct3D 12 exposes read-only CPU descriptor handles for sampled views and samplers. Keep the owning resource
    /// live while using the descriptor and do not free or overwrite it. Direct3D 12 has no queue-family value.
    /// Logical uniform sets do not own a native descriptor set independently of recorded commands.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="resource"/> is outside the defined resource kinds.</exception>
    /// <exception cref="ArgumentException">The resource RID is stale, foreign or of the wrong kind.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner, access occurs during canvas submission, or the live native object is unavailable.</exception>
    /// <exception cref="NotSupportedException">The selected backend, installed native module or resource kind lacks a driver query.</exception>
    /// <exception cref="ObjectDisposedException">This device is disposed.</exception>
    public ulong GetDriverResource(DriverResource resource, RID rid, int index)
    {
        Access();
        if ((uint)resource > (uint)DriverResource.RenderPipeline)
            throw new ArgumentOutOfRangeException(nameof(resource));

        nint handle = 0;
        if (resource >= DriverResource.Texture)
        {
            if (!_entries.TryGetValue(rid, out var entry) || !entry.Alive || !MatchesDriverResource(resource, entry.Kind))
                throw new ArgumentException("The RID is not a live resource of the requested kind on this device.", nameof(rid));
            if (resource == DriverResource.UniformSet)
                throw new NotSupportedException("The backend allocates native descriptor sets for recorded commands.");
            if (resource == DriverResource.ComputePipeline) entry = PipelineProgram(entry);
            if (entry.Handle is null && entry.Borrowed is null)
                throw new InvalidOperationException("The live resource has no published native allocation.");
            handle = entry.NativeHandle;
        }

        int status;
        ulong value;
        try { status = QueryDriverResource(Device, 1, (uint)resource, handle, out value); }
        catch (Exception error) when (error is EntryPointNotFoundException or DllNotFoundException)
        {
            throw new NotSupportedException("The native module does not provide driver resource queries.", error);
        }

        if (status == 0) throw new NotSupportedException("The selected GPU backend does not expose this native resource kind.");
        if (status != 1) throw new InvalidOperationException("The driver did not report a live native resource.");
        return value;
    }

    private static bool MatchesDriverResource(DriverResource resource, Kind kind) => resource switch
    {
        DriverResource.Texture or DriverResource.TextureView or DriverResource.TextureDataFormat => kind == Kind.Texture,
        DriverResource.Sampler => kind == Kind.Sampler,
        DriverResource.UniformSet => kind == Kind.Set,
        DriverResource.Buffer => kind is Kind.Storage or Kind.Uniform or Kind.Texel or Kind.Vertex or Kind.Index,
        DriverResource.ComputePipeline => kind == Kind.Pipeline,
        DriverResource.RenderPipeline => kind == Kind.GraphicsPipeline,
        _ => false,
    };

    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_driver_resource")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int QueryDriverResource(nint device, uint abi, uint resource, nint handle, out ulong value);
}
