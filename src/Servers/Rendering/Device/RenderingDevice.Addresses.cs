using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    /// <summary>Optional executing device capabilities.</summary>
    public enum Features
    {
        /// <summary>Native spatial scaling through MetalFX.</summary>
        SupportsMetalFXSpatial = 3,
        /// <summary>Native temporal scaling through MetalFX.</summary>
        SupportsMetalFXTemporal = 4,
        /// <summary>Stable buffer addresses and physical-storage pointer shaders.</summary>
        SupportsBufferDeviceAddress = 6,
        /// <summary>Signed and unsigned R32 image atomics.</summary>
        SupportsImageAtomic32Bit = 7,
        /// <summary>Inline acceleration-structure queries.</summary>
        SupportsRayQuery = 11,
        /// <summary>Native high-dynamic-range presentation.</summary>
        SupportsHDROutput = 13
    }
    /// <summary>Reports enabled capabilities of this device and its integrated adapters.</summary>
    /// <param name="feature">A defined optional feature.</param>
    /// <returns>True when the device can execute the feature. Scaling and ray queries currently have no integrated adapter. HDR reports the renderer main output; local devices have no presentation surface.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The feature is undefined.</exception>
    public bool HasFeature(Features feature)
    {
        Access();
        return feature switch
        {
            Features.SupportsBufferDeviceAddress => SDL.GetBooleanProperty(SDL.GetGPUDeviceProperties(Device), GPUDeviceFactory.AddressProperty, false),
            Features.SupportsImageAtomic32Bit => TextureIsFormatSupportedForUsage(DataFormat.R32SINT, TextureUsageBits.Storage | TextureUsageBits.StorageAtomic) && TextureIsFormatSupportedForUsage(DataFormat.R32UINT, TextureUsageBits.Storage | TextureUsageBits.StorageAtomic),
            Features.SupportsHDROutput => _serverOwner?.DeviceHasHDROutput ?? false,
            Features.SupportsMetalFXSpatial or Features.SupportsMetalFXTemporal or Features.SupportsRayQuery => false,
            _ => throw new ArgumentOutOfRangeException(nameof(feature))
        };
    }
    /// <summary>Returns the stable GPU address of an address-enabled vertex, index, storage or uniform buffer.</summary>
    /// <param name="buffer">A live buffer on this device.</param>
    /// <returns>The unsigned device address, or zero when the buffer was created without the address role.</returns>
    /// <remarks>The address belongs to this device and expires when its buffer is freed. Shaders own pointer bounds, alignment and invocation race freedom. Keep every referenced buffer alive through execution; addresses in copied data do not extend resource lifetime.</remarks>
    /// <exception cref="ArgumentException">The identity is not a live ordinary buffer of this device.</exception>
    public ulong BufferGetDeviceAddress(RID buffer)
    {
        var entry = Get(buffer);
        if (entry.Kind is not (Kind.Storage or Kind.Vertex or Kind.Index or Kind.Uniform)) throw new ArgumentException("An ordinary buffer is required.", nameof(buffer));
        return entry.DeviceAddress;
    }
    private RenderHandle CreateBuffer(Entry entry, SDL.GPUBufferUsageFlags usage, BufferCreationBits creation)
    {
        if ((creation & ~(BufferCreationBits.AsStorage | BufferCreationBits.DeviceAddress)) != 0) throw new ArgumentOutOfRangeException(nameof(creation));
        var info = new SDL.GPUBufferCreateInfo { Size = entry.Size, Usage = usage };
        if (entry.Kind != Kind.Uniform && (creation & BufferCreationBits.DeviceAddress) == 0) return new(SDL.CreateGPUBuffer(Device, info), h => SDL.ReleaseGPUBuffer(Device, h), _context.Handle);
        if ((creation & BufferCreationBits.DeviceAddress) != 0 && !HasFeature(Features.SupportsBufferDeviceAddress)) throw new NotSupportedException("Buffer addresses require an enabled native address allocation and shader adapter.");
        if (entry.Kind == Kind.Uniform)
        {
            try { if (GPUDeviceFactory.ReadAddressSupport(Device, 2) != 1) throw new NotSupportedException("Native uniform storage requires its allocation and descriptor adapter."); }
            catch (EntryPointNotFoundException error) { throw new NotSupportedException("Native uniform storage requires the current native descriptor adapter.", error); }
        }
        var buffer = NativeAddressCreate(Device, 1, in info, (entry.Kind == Kind.Uniform ? 1u : 0u) | ((creation & BufferCreationBits.DeviceAddress) != 0 ? 2u : 0u), out var address);
        if (buffer == 0) throw GPUPhysicsDevice.Failure("create addressable buffer");
        entry.DeviceAddress = address;
        return new(buffer, h => NativeAddressDestroy(Device, 1, h), _context.Handle);
    }
    private void PrepareAddressBuffers()
    {
        if (NativeAddressesPrepare(Device, 1, _command) != 1) throw new InvalidOperationException("Addressable buffers could not be retained and synchronized.");
    }
    private void CollectAddressBuffers()
    {
        if (SDL.GetPointerProperty(SDL.GetGPUDeviceProperties(Device), "Electron2D.gpu.address_buffers", 0) != 0) NativeAddressDestroy(Device, 1, 0);
    }
    private readonly nint[] _addressUniforms = new nint[12];
    private uint _addressUniformMask;
    private static bool HostUniform(Entry entry) => entry.Kind == Kind.Uniform && entry.Handle is null;
    private bool NativeUniformsUsed(Entry program)
    {
        foreach (var binding in program.Layout)
            if (binding.Native >= 0 && binding.Type == UniformType.UniformBuffer && _sets.TryGetValue(binding.Set, out var set) && set.Bindings.TryGetValue(binding.Slot, out var ids) && _entries.TryGetValue(ids[ResourceID(binding)], out var buffer) && buffer.Handle is not null) return true;
        return false;
    }
    private void BindAddressUniform(Entry buffer, ShaderStage stage, int slot)
    {
        var index = (stage == ShaderStage.Compute ? 2 : stage == ShaderStage.Vertex ? 0 : 1) * 4 + slot;
        if (NativeAddressUniform(Device, 1, _command, (uint)index / 4, (uint)slot, buffer.NativeHandle, ref _addressUniforms[index]) != 1) throw new InvalidOperationException("Native uniform storage could not be bound.");
        _addressUniformMask |= 1u << index;
    }
    private void RestoreAddressUniforms()
    {
        for (var index = 0; index < 12; index++) if ((_addressUniformMask & (1u << index)) != 0)
            {
                if (NativeAddressUniform(Device, 1, _command, (uint)index / 4, (uint)index % 4, 0, ref _addressUniforms[index]) != 1) throw new InvalidOperationException("Native uniform binding could not be restored.");
                _addressUniformMask &= ~(1u << index); _addressUniforms[index] = 0;
            }
    }
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_address_uniform")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeAddressUniform(nint device, uint abi, nint commands, uint stage, uint slot, nint buffer, ref nint previous);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_address_create")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeAddressCreate(nint device, uint abi, in SDL.GPUBufferCreateInfo info, uint roles, out ulong address);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_address_destroy")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void NativeAddressDestroy(nint device, uint abi, nint buffer);
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_addresses_prepare")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeAddressesPrepare(nint device, uint abi, nint commands);
}
