using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void Addresses(string mode)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        if (mode == "address-unavailable") { AddressesUnavailable(device); return; }
        if (mode == "address-no-atomics") { AddressNoAtomics(device); return; }
        if (mode == "address-readonly") { AddressReadOnly(device, true); return; }
        Check(device.HasFeature(Features.SupportsBufferDeviceAddress), "Native address feature was not enabled.");
        using var code = Code("address", "glsl");
        var shader = mode == "address-source" ? Shader(device, "address", "glsl", true) : device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code));
        var pipeline = device.ComputePipelineCreate(shader);
        Span<byte> push = stackalloc byte[12];
        foreach (var role in new[] { 0, 1, 2, 3 })
        {
            var bits = BufferCreationBits.DeviceAddress;
            var buffer = role == 0 ? device.StorageBufferCreate(16, creationBits: bits) : role == 1 ? device.VertexBufferCreate(16, creationBits: bits) : role == 2 ? device.IndexBufferCreate(4, IndexBufferFormat.UInt32, creationBits: bits) : device.UniformBufferCreate(16, creationBits: bits);
            var address = device.BufferGetDeviceAddress(buffer); Check(address != 0, "Nonzero address required.");
            BinaryPrimitives.WriteUInt64LittleEndian(push, address); BinaryPrimitives.WriteUInt32LittleEndian(push[8..], 7);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListSetPushConstant(list, push, 12); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
            var result = device.BufferGetData(buffer); Check(BitConverter.ToUInt32(result) == 14 && BitConverter.ToUInt32(result, 4) == 42, "Pointer writes/order failed.");
            device.BufferClear(buffer, 0, 16); device.BufferUpdate(buffer, 0, 4, BitConverter.GetBytes(100u));
            var copy = device.StorageBufferCreate(16); device.BufferCopy(copy, buffer, 4, 4, 4);
            list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListSetPushConstant(list, push, 12); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
            result = device.BufferGetData(buffer); Check(BitConverter.ToUInt32(result) == 107 && BitConverter.ToUInt32(result, 4) == 321 && device.BufferGetDeviceAddress(buffer) == address, "Transfers changed address or ordering.");
            if (role == 3) AddressUniform(device, buffer);
            AddressGraphics(device, buffer);
            device.FreeRID(buffer); Reject<ArgumentException>(() => device.BufferGetDeviceAddress(buffer)); device.FreeRID(copy);
        }
        AddressEdges(device, code);
        AddressWarm(device, pipeline);
        AddressSpecialization(device, shader);
        AddressList(device);
        AddressCancellation(device, code);
        AddressReadOnly(device, false);
        AddressAtomic(device);
        AddressNarrow(device);
        AddressUniformBoundary(device);
        device.FreeRID(pipeline); device.FreeRID(shader);
        Console.WriteLine("PASS physical pointers write stable storage, vertex, index and uniform allocations across dispatch/clear/update/copy/readback/free.");
    }
    private static void AddressUniformBoundary(RenderingDevice device)
    {
        Check(device.LimitGet(Limit.MaxUniformBufferSize) == 4096, "Uniform capability exceeds the pinned descriptor range.");
        Reject<ArgumentOutOfRangeException>(() => device.UniformBufferCreate(4100)); Reject<ArgumentOutOfRangeException>(() => device.UniformBufferCreate(4100, creationBits: BufferCreationBits.DeviceAddress));
        using var code = Code("address-boundary", "glsl"); var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.ComputePipelineCreate(shader); var output = device.StorageBufferCreate(16);
        foreach (var bits in new[] { (BufferCreationBits)0, BufferCreationBits.DeviceAddress })
        {
            var buffer = device.UniformBufferCreate(4096, creationBits: bits); device.BufferUpdate(buffer, 4080, 16, MemoryMarshal.AsBytes(new uint[] { 123, 234, 345, 456 }.AsSpan()));
            using var input = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; input.AddID(buffer); using var result = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; result.AddID(output); var set = device.UniformSetCreate([input, result], shader, 0);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); var data = device.BufferGetData(output); Check(BinaryPrimitives.ReadUInt32LittleEndian(data) == 123 && BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(12)) == 456, "Last uniform descriptor words were not readable."); device.FreeRID(set); device.FreeRID(buffer);
        }
        device.FreeRID(output); device.FreeRID(pipeline); device.FreeRID(shader); Console.WriteLine("PASS exact 4 KiB host/native uniform descriptor boundary and oversized rejection.");
    }
    private static void AddressNarrow(RenderingDevice device)
    {
        using var code = Code("address-narrow", "glsl"); var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.ComputePipelineCreate(shader); var head = device.StorageBufferCreate(16, creationBits: BufferCreationBits.DeviceAddress); var next = device.StorageBufferCreate(16, creationBits: BufferCreationBits.DeviceAddress);
        Span<byte> data = stackalloc byte[16]; data.Clear(); BinaryPrimitives.WriteUInt64LittleEndian(data, device.BufferGetDeviceAddress(next)); device.BufferUpdate(head, 0, 16, data); BinaryPrimitives.WriteUInt64LittleEndian(data, device.BufferGetDeviceAddress(head)); data[8] = 7; BinaryPrimitives.WriteInt16LittleEndian(data[10..], -123); BinaryPrimitives.WriteUInt16LittleEndian(data[12..], BitConverter.HalfToUInt16Bits((Half)1.5f)); device.BufferUpdate(next, 0, 16, data);
        Span<byte> push = stackalloc byte[8]; BinaryPrimitives.WriteUInt64LittleEndian(push, device.BufferGetDeviceAddress(head)); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListSetPushConstant(list, push, 8); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(head, data);
        Check(data[8] == 9 && BinaryPrimitives.ReadInt16LittleEndian(data[10..]) == -125 && BinaryPrimitives.ReadUInt16LittleEndian(data[12..]) == BitConverter.HalfToUInt16Bits((Half)2), "Recursive physical byte/Int16/Half payload failed."); device.FreeRID(head); device.FreeRID(next); device.FreeRID(pipeline); device.FreeRID(shader); Console.WriteLine("PASS recursive physical byte, Int16 and Half payloads preserve packed data.");
    }
    private static void AddressNoAtomics(RenderingDevice device)
    {
        using var atomic = Code("address-atomic", "glsl"); Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(atomic)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(atomic));
        using var ordinary = Code("address", "glsl"); var shader = device.ShaderCreateFromSPIRV(ordinary); AddressSpecialization(device, shader); device.FreeRID(shader); Console.WriteLine("PASS absent buffer Int64 atomics rejects physical atomic modules while ordinary pointers execute.");
    }
    private static void AddressAtomic(RenderingDevice device)
    {
        using var code = Code("address-atomic", "glsl"); var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.ComputePipelineCreate(shader); var buffer = device.StorageBufferCreate(16, creationBits: BufferCreationBits.DeviceAddress);
        Span<byte> push = stackalloc byte[8]; BinaryPrimitives.WriteUInt64LittleEndian(push, device.BufferGetDeviceAddress(buffer)); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListSetPushConstant(list, push, 8); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); var result = device.BufferGetData(buffer);
        Check(BinaryPrimitives.ReadUInt64LittleEndian(result) == 8 && BinaryPrimitives.ReadUInt64LittleEndian(result.AsSpan(8)) == 7, "Physical 64-bit atomic returns or contention failed."); device.FreeRID(buffer); device.FreeRID(pipeline); device.FreeRID(shader); Console.WriteLine("PASS concurrent physical 64-bit atomics use the native buffer feature.");
    }
    private static void AddressReadOnly(RenderingDevice device, bool storesUnavailable)
    {
        var buffer = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(new uint[] { 17, 29, 0, 0 }.AsSpan()), creationBits: BufferCreationBits.DeviceAddress); using var code = Code("address-read", "glsl"); var shader = device.ShaderCreateFromSPIRV(code);
        using var format = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        Span<byte> push = stackalloc byte[8]; BinaryPrimitives.WriteUInt64LittleEndian(push, device.BufferGetDeviceAddress(buffer)); var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListSetPushConstant(list, push, 8); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); var pixels = MemoryMarshal.Cast<byte, float>(device.TextureGetData(image, 0)); Check(pixels[0] == 17 && pixels[1] == 29, "Readonly physical loads failed.");
        if (storesUnavailable) { using var writer = Code("address-graphics", "glsl"); Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(writer)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(writer)); }
        device.FreeRID(pipeline); device.FreeRID(target); device.FreeRID(image); device.FreeRID(shader); device.FreeRID(buffer); Console.WriteLine("PASS readonly physical graphics loads need no stage-store feature.");
    }
    private static void AddressCancellation(RenderingDevice survivor, RDShaderSPIRV code)
    {
        using (var cancelled = RenderingServer.CreateLocalRenderingDevice())
        {
            var buffer = cancelled.UniformBufferCreate(16, creationBits: BufferCreationBits.DeviceAddress); var shader = cancelled.ShaderCreateFromSPIRV(code); var pipeline = cancelled.ComputePipelineCreate(shader);
            Span<byte> push = stackalloc byte[12]; BinaryPrimitives.WriteUInt64LittleEndian(push, cancelled.BufferGetDeviceAddress(buffer)); BinaryPrimitives.WriteUInt32LittleEndian(push[8..], 3);
            var list = cancelled.ComputeListBegin(); cancelled.ComputeListBindComputePipeline(list, pipeline); cancelled.ComputeListSetPushConstant(list, push, 12); cancelled.ComputeListDispatch(list, 1, 1, 1); cancelled.ComputeListEnd();
            Reject<ArgumentException>(() => survivor.BufferGetDeviceAddress(buffer));
        }
        var after = survivor.StorageBufferCreate(16, BitConverter.GetBytes(55u), creationBits: BufferCreationBits.DeviceAddress); Check(BitConverter.ToUInt32(survivor.BufferGetData(after)) == 55, "Cancelled pointer commands damaged the surviving shared device."); survivor.FreeRID(after);
        var wrongKind = survivor.ShaderCreateFromSPIRV(code); Reject<ArgumentException>(() => survivor.BufferGetDeviceAddress(wrongKind)); survivor.FreeRID(wrongKind);
        Console.WriteLine("PASS cancelled pointer commands, foreign and non-buffer RIDs, and surviving shared device.");
    }
    private static void AddressList(RenderingDevice device)
    {
        using var code = Code("address-list", "glsl"); var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.ComputePipelineCreate(shader); var buffers = new RID[9]; var addresses = new ulong[9];
        for (var i = 0; i < buffers.Length; i++) { buffers[i] = device.StorageBufferCreate(16, creationBits: BufferCreationBits.DeviceAddress); addresses[i] = device.BufferGetDeviceAddress(buffers[i]); }
        Span<byte> node = stackalloc byte[16]; node.Clear();
        for (var i = 0; i < buffers.Length; i++) { BinaryPrimitives.WriteUInt64LittleEndian(node, addresses[(i + 1) % 9]); BinaryPrimitives.WriteUInt32LittleEndian(node[8..], (uint)i + 1); device.BufferUpdate(buffers[i], 0, 16, node); }
        device.Submit(); device.Sync();
        Span<byte> push = stackalloc byte[8]; BinaryPrimitives.WriteUInt64LittleEndian(push, addresses[0]); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListSetPushConstant(list, push, 8); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        var result = device.BufferGetData(buffers[0]); Check(BinaryPrimitives.ReadUInt64LittleEndian(result) == addresses[2] && BinaryPrimitives.ReadUInt32LittleEndian(result.AsSpan(8)) == 45, "Nine-buffer pointer traversal or GPU-written address failed.");
        foreach (var buffer in buffers) device.FreeRID(buffer); device.FreeRID(pipeline); device.FreeRID(shader); Console.WriteLine("PASS nine descriptor-free linked buffers and GPU pointer rewiring.");
    }
    private static void AddressSpecialization(RenderingDevice device, RID shader)
    {
        using var factor = new RDPipelineSpecializationConstant<uint> { ConstantID = 0, Value = 2 }; var pipeline = device.ComputePipelineCreate(shader, [factor]); var buffer = device.StorageBufferCreate(16, creationBits: BufferCreationBits.DeviceAddress);
        Span<byte> push = stackalloc byte[12]; BinaryPrimitives.WriteUInt64LittleEndian(push, device.BufferGetDeviceAddress(buffer)); BinaryPrimitives.WriteUInt32LittleEndian(push[8..], 7);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListSetPushConstant(list, push, 12); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        var copy = device.StorageBufferCreate(16); device.BufferCopy(buffer, copy, 0, 0, 16); device.FreeRID(buffer); Check(BitConverter.ToUInt32(device.BufferGetData(copy)) == 14, "Specialized physical pointer or queued free lost execution."); device.FreeRID(copy); device.FreeRID(pipeline);
    }
    private static void AddressWarm(RenderingDevice device, RID pipeline)
    {
        var buffer = device.StorageBufferCreate(16, creationBits: BufferCreationBits.DeviceAddress); var address = device.BufferGetDeviceAddress(buffer); Span<byte> output = stackalloc byte[16];
        void Cycle(Span<byte> result)
        {
            Span<byte> push = stackalloc byte[12]; BinaryPrimitives.WriteUInt64LittleEndian(push, address); BinaryPrimitives.WriteUInt32LittleEndian(push[8..], 1);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListSetPushConstant(list, push, 12); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(buffer, result);
        }
        for (var i = 0; i < 16; i++) Cycle(output);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Cycle(output); var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(bytes == 0 && BitConverter.ToUInt32(output) == 48, "Prepared address execution allocated owner bytes or lost writes: " + bytes); device.FreeRID(buffer); Console.WriteLine("PASS 32 prepared address cycles allocate zero owner-thread managed bytes.");
    }
    private static void AddressUniform(RenderingDevice device, RID buffer)
    {
        using var code = Code("address-uniform-chain", "glsl"); var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.ComputePipelineCreate(shader);
        var output = device.StorageBufferCreate(16); using var input = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; input.AddID(buffer);
        using var result = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; result.AddID(output); var set = device.UniformSetCreate([input, result], shader, 0);
        var host = device.UniformBufferCreate(16, BitConverter.GetBytes(5u)); using var other = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; other.AddID(host); var hostSet = device.UniformSetCreate([other, result], shader, 0);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListBindUniformSet(list, hostSet, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        var bytes = device.BufferGetData(output); Check(BitConverter.ToUInt32(bytes) == 107 && BitConverter.ToUInt32(bytes, 4) == 321, "Native uniform did not observe pointer writes or restore host slots.");
        device.FreeRID(hostSet); device.FreeRID(host); device.FreeRID(set); device.FreeRID(output); device.FreeRID(pipeline); device.FreeRID(shader);
        AddressUniformGraphics(device, buffer);
    }
    private static void AddressUniformGraphics(RenderingDevice device, RID buffer)
    {
        using var code = Code("address-uniform", "glsl"); var shader = device.ShaderCreateFromSPIRV(code);
        using var input = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; input.AddID(buffer); var set = device.UniformSetCreate([input], shader, 0);
        using var format = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        var pixels = MemoryMarshal.Cast<byte, float>(device.TextureGetData(image, 0)); Check(pixels[0] == 107 && pixels[1] == 321, "Graphics native uniform did not observe pointer writes.");
        device.FreeRID(pipeline); device.FreeRID(target); device.FreeRID(image); device.FreeRID(set); device.FreeRID(shader);
    }
    private static void AddressGraphics(RenderingDevice device, RID buffer)
    {
        using var code = Code("address-graphics", "glsl"); var shader = device.ShaderCreateFromSPIRV(code);
        using var format = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        Span<byte> push = stackalloc byte[8]; BinaryPrimitives.WriteUInt64LittleEndian(push, device.BufferGetDeviceAddress(buffer));
        var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListSetPushConstant(list, push, 8); device.DrawListDraw(list, false, 1, 3); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        var bytes = device.BufferGetData(buffer); Check(BitConverter.ToUInt32(bytes) == 109 && BitConverter.ToUInt32(bytes, 4) == 331, "Vertex/fragment physical writes or inter-draw ordering failed.");
        device.FreeRID(pipeline); device.FreeRID(target); device.FreeRID(image); device.FreeRID(shader);
    }
    private static void AddressEdges(RenderingDevice device, RDShaderSPIRV code)
    {
        foreach (var missing in new[] { 14u, 17u })
        {
            var malformed = code.BytecodeCompute.ToArray(); var words = MemoryMarshal.Cast<byte, uint>(malformed.AsSpan());
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
                if ((words[at] & 65535) == missing && (missing == 14 || words[at + 1] == 5347)) { words[at + 1] = missing == 14 ? 0u : 1u; break; }
            using var invalid = new RDShaderSPIRV { BytecodeCompute = malformed }; Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(invalid)); Reject<ArgumentException>(() => device.ShaderCompileBinaryFromSPIRV(invalid));
        }
        var ordinary = device.StorageBufferCreate(4); Check(device.BufferGetDeviceAddress(ordinary) == 0, "Ordinary address must be zero."); device.FreeRID(ordinary);
        Reject<ArgumentOutOfRangeException>(() => device.HasFeature((Features)(-1)));
        Reject<ArgumentOutOfRangeException>(() => device.StorageBufferCreate(4, creationBits: (BufferCreationBits)4));
        Reject<ArgumentOutOfRangeException>(() => device.UniformBufferCreate(16, creationBits: (BufferCreationBits)4));
        var storageUniform = device.UniformBufferCreate(16, creationBits: BufferCreationBits.AsStorage); Check(device.BufferGetDeviceAddress(storageUniform) == 0, "Storage-only uniform must not acquire an address.");
        device.BufferUpdate(storageUniform, 0, 8, MemoryMarshal.AsBytes(new uint[] { 107, 321 }.AsSpan())); using var chainCode = Code("chain", "glsl"); var chain = device.ShaderCreateFromSPIRV(chainCode); var chainPipeline = device.ComputePipelineCreate(chain); var chainSet = Set(device, chain, storageUniform);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, chainPipeline); device.ComputeListBindUniformSet(list, chainSet, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); Check(BitConverter.ToUInt32(device.BufferGetData(storageUniform), 8) == 428, "Uniform storage role did not execute."); AddressUniform(device, storageUniform); device.FreeRID(chainSet); device.FreeRID(chainPipeline); device.FreeRID(chain); device.FreeRID(storageUniform);
        var callbackBuffer = device.UniformBufferCreate(16, BitConverter.GetBytes(33u), BufferCreationBits.DeviceAddress); var calls = 0;
        device.BufferGetDataAsync(callbackBuffer, b => { Check(BitConverter.ToUInt32(b) == 33, "Native uniform async readback failed."); calls++; }); device.Submit(); device.Sync(); Check(calls == 1, "Native uniform callback missing."); device.FreeRID(callbackBuffer);
    }
    private static void AddressesUnavailable(RenderingDevice device)
    {
        Check(!device.HasFeature(Features.SupportsBufferDeviceAddress), "Masked address capability must be false.");
        Reject<NotSupportedException>(() => device.StorageBufferCreate(16, creationBits: BufferCreationBits.DeviceAddress));
        Reject<NotSupportedException>(() => device.VertexBufferCreate(16, creationBits: BufferCreationBits.DeviceAddress));
        Reject<NotSupportedException>(() => device.IndexBufferCreate(4, IndexBufferFormat.UInt32, creationBits: BufferCreationBits.DeviceAddress));
        Reject<NotSupportedException>(() => device.UniformBufferCreate(16, creationBits: BufferCreationBits.DeviceAddress));
        using var code = Code("address", "glsl"); Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code));
        var ordinary = device.StorageBufferCreate(16); device.FreeRID(ordinary); Console.WriteLine("PASS unavailable address capability refuses resources and pointer shaders before publication.");
    }

}
