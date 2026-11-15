using Electron2D;
using static Electron2D.RenderingDevice;

if (args.Length != 1 || args[0] is not ("vulkan" or "direct3d12"))
    throw new ArgumentException("Select the vulkan or direct3d12 profile.");

bool vulkan = args[0] == "vulkan";
CheckDescription();
using var device = RenderingServer.CreateLocalRenderingDevice();
if (vulkan)
    _ = device.GetDriverResource(DriverResource.QueueFamily, default, 0);
else
    Reject<NotSupportedException>(() => device.GetDriverResource(DriverResource.QueueFamily, default, 0));

Console.WriteLine($"{args[0]} | {device.GetDeviceName()}");
CheckLimits(device, vulkan);
CheckSnapshot(device, vulkan);
Console.WriteLine("RDTextureFormat defaults, texture limits, allocation, native views, readback and lifetime passed.");

static void CheckDescription()
{
    using var format = new RDTextureFormat();
    Check(format.Width == 1 && format.Height == 1 && format.ArrayLayers == 1 && format.MIPMaps == 1 &&
        format.Format == DataFormat.R8UNORM && format.TextureType == TextureType.Texture &&
        format.UsageBits == 0 && format.Samples == TextureSamples.Samples1 && !format.IsResolveBuffer, "Defaults.");

    format.Width = format.Height = format.ArrayLayers = format.MIPMaps = int.MaxValue;
    format.Format = DataFormat.R32UINT;
    format.TextureType = TextureType.TextureArray;
    format.UsageBits = TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom;

    foreach (var invalid in new[] { 0u, 1u + int.MaxValue, uint.MaxValue })
    {
        Reject<ArgumentOutOfRangeException>(() => format.Width = invalid);
        Reject<ArgumentOutOfRangeException>(() => format.Height = invalid);
        Reject<ArgumentOutOfRangeException>(() => format.ArrayLayers = invalid);
        Reject<ArgumentOutOfRangeException>(() => format.MIPMaps = invalid);
    }
    Reject<ArgumentOutOfRangeException>(() => format.Format = DataFormat.Max);
    Reject<ArgumentOutOfRangeException>(() => format.Format = (DataFormat)(-1));
    Reject<ArgumentOutOfRangeException>(() => format.TextureType = TextureType.Max);
    Reject<ArgumentOutOfRangeException>(() => format.TextureType = (TextureType)0);
    Reject<ArgumentOutOfRangeException>(() => format.UsageBits |= (TextureUsageBits)1024);
    Check(format.Width == int.MaxValue && format.Height == int.MaxValue &&
        format.ArrayLayers == int.MaxValue && format.MIPMaps == int.MaxValue &&
        format.Format == DataFormat.R32UINT && format.TextureType == TextureType.TextureArray &&
        format.UsageBits == (TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom), "Failed assignments preserve the description.");
}

static void CheckLimits(RenderingDevice device, bool vulkan)
{
    uint maxAxis = vulkan ? (uint)device.LimitGet(Limit.MaxTextureSize2D) : 16384;
    uint maxLayers = vulkan ? (uint)device.LimitGet(Limit.MaxTextureArrayLayers) : 2048;
    using var view = new RDTextureView();
    using var format = new RDTextureFormat();
    Reject<ArgumentException>(() => device.TextureCreate(format, view));
    format.UsageBits = TextureUsageBits.CanCopyFrom | TextureUsageBits.CanUpdate;

    format.Width = maxAxis;
    CheckAllocation(0, maxAxis);
    format.Width++;
    Reject<ArgumentOutOfRangeException>(() => device.TextureCreate(format, view));
    format.Width = 1;
    format.Height = maxAxis;
    CheckAllocation(0, maxAxis);
    format.Height++;
    Reject<ArgumentOutOfRangeException>(() => device.TextureCreate(format, view));
    format.Height = 1;
    format.ArrayLayers = 2;
    Reject<ArgumentException>(() => device.TextureCreate(format, view));
    format.TextureType = TextureType.TextureArray;
    format.ArrayLayers = maxLayers;
    CheckAllocation(maxLayers - 1, 1);
    format.ArrayLayers++;
    Reject<ArgumentOutOfRangeException>(() => device.TextureCreate(format, view));
    format.ArrayLayers = 1;
    CheckAllocation(0, 1);
    format.MIPMaps = 2;
    Reject<ArgumentException>(() => device.TextureCreate(format, view));
    format.MIPMaps = 1;
    format.UsageBits |= TextureUsageBits.DepthStencilAttachment;
    Check(!device.TextureIsFormatSupportedForUsage(format.Format, format.UsageBits), "Color encoding cannot be a depth target.");
    Reject<NotSupportedException>(() => device.TextureCreate(format, view));
    format.UsageBits = TextureUsageBits.CanCopyFrom;
    format.ArrayLayers = 1;
    format.Format = DataFormat.R32G32B32A32SFLOAT;
    format.Width = maxAxis;
    var rowBytes = maxAxis * 16ul;
    format.Height = (uint)((int.MaxValue + rowBytes) / rowBytes);
    Reject<ArgumentOutOfRangeException>(() => device.TextureCreate(format, view));
    Console.WriteLine($"Texture limits passed: {maxAxis} texels per axis, {maxLayers} layers.");

    void CheckAllocation(uint layer, uint bytes)
    {
        var rid = device.TextureCreate(format, view);
        using var reported = device.TextureGetFormat(rid);
        Check(reported.Width == format.Width && reported.Height == format.Height &&
            reported.ArrayLayers == format.ArrayLayers && reported.TextureType == format.TextureType &&
            reported.UsageBits == format.UsageBits, "Allocated boundary metadata.");
        var data = device.TextureGetData(rid, layer);
        Check(data.Length == bytes && data.All(value => value == 0), "Zero initialization at the allocation boundary.");
        device.FreeRID(rid);
    }
}

static void CheckSnapshot(RenderingDevice device, bool vulkan)
{
    const DataFormat storage = DataFormat.R8G8B8A8UNORM;
    const DataFormat srgb = DataFormat.R8G8B8A8SRGB;
    const TextureUsageBits usages = TextureUsageBits.Sampling | TextureUsageBits.CanUpdate | TextureUsageBits.CanCopyFrom;
    using var format = new RDTextureFormat
    {
        Width = 5,
        Height = 3,
        ArrayLayers = 2,
        MIPMaps = 3,
        TextureType = TextureType.TextureArray,
        Format = storage,
        UsageBits = usages
    };
    format.AddShareableFormat(srgb);
    using var view = new RDTextureView { FormatOverride = srgb };
    // 5×3, 2×1 and 1×1 RGBA texels, including an independently identifiable last mip.
    byte[] layer0 = Enumerable.Range(1, 72).Select(i => (byte)i).ToArray();
    byte[] layer1 = Enumerable.Range(129, 72).Select(i => (byte)i).ToArray();
    Reject<ArgumentException>(() => device.TextureCreate(format, view, [layer0, layer1]));
    format.AddShareableFormat(storage);
    format.UsageBits = 0;
    Reject<ArgumentException>(() => device.TextureCreate(format, view, [layer0, layer1]));
    format.UsageBits = usages;
    format.Width = (vulkan ? (uint)device.LimitGet(Limit.MaxTextureSize2D) : 16384u) + 1;
    Reject<ArgumentOutOfRangeException>(() => device.TextureCreate(format, view, [layer0, layer1]));
    format.Width = 5;
    format.MIPMaps++;
    Reject<ArgumentException>(() => device.TextureCreate(format, view, [layer0, layer1]));
    format.MIPMaps--;
    Reject<ArgumentException>(() => device.TextureCreate(format, view, [layer0]));
    Reject<ArgumentException>(() => device.TextureCreate(format, view, [layer0, layer1[..^1]]));
    var owner = device.TextureCreate(format, view, [layer0, layer1]);
    var unmodified = layer0.ToArray();
    Array.Fill(layer0, (byte)226);

    format.RemoveShareableFormat(srgb);
    format.Width = format.Height = format.ArrayLayers = format.MIPMaps = 1;
    format.TextureType = TextureType.Texture;
    format.Format = DataFormat.R32UINT;
    format.UsageBits = TextureUsageBits.Storage;
    view.FormatOverride = DataFormat.R32UINT;
    format.Dispose();
    view.Dispose();
    using var replacement = new RDTextureView { FormatOverride = storage };
    Reject<ObjectDisposedException>(() => device.TextureCreate(format, replacement));
    using var editable = device.TextureGetFormat(owner);
    editable.RemoveShareableFormat(srgb);
    editable.Width = 1;
    editable.Dispose();
    using var snapshot = device.TextureGetFormat(owner);
    CheckMetadata(snapshot, 5, 3, 2, 3, TextureType.TextureArray);
    Check(device.TextureGetData(owner, 0).SequenceEqual(unmodified) &&
        device.TextureGetData(owner, 1).SequenceEqual(layer1), "Owner copies the initial bytes of both mip chains.");

    using var srgbView = new RDTextureView { FormatOverride = srgb };
    var complete = device.TextureCreateShared(srgbView, owner);
    Reject<ArgumentOutOfRangeException>(() => device.TextureCreateSharedFromSlice(replacement, complete, 2, 0));
    Reject<ArgumentOutOfRangeException>(() => device.TextureCreateSharedFromSlice(replacement, complete, 1, 3));
    Reject<ArgumentOutOfRangeException>(() => device.TextureCreateSharedFromSlice(replacement, complete, 1, 1, uint.MaxValue));
    var slice = device.TextureCreateSharedFromSlice(replacement, complete, 1, 1, 2);
    using var completeFormat = device.TextureGetFormat(complete);
    using var sliceFormat = device.TextureGetFormat(slice);
    CheckMetadata(completeFormat, 5, 3, 2, 3, TextureType.TextureArray);
    CheckMetadata(sliceFormat, 2, 1, 1, 2, TextureType.Texture);
    var nativeImage = device.GetDriverResource(DriverResource.Texture, owner, 0);
    Check(nativeImage != 0 && device.GetDriverResource(DriverResource.Texture, complete, 0) == nativeImage &&
        device.GetDriverResource(DriverResource.Texture, slice, 0) == nativeImage, "Views share the live native allocation.");
    Check(device.GetDriverResource(DriverResource.TextureDataFormat, owner, 0) == (vulkan ? 43ul : 29ul) &&
        device.GetDriverResource(DriverResource.TextureDataFormat, slice, 0) == (vulkan ? 37ul : 28ul), "Native sRGB and linear view encodings.");

    using var other = RenderingServer.CreateLocalRenderingDevice();
    Reject<ArgumentException>(() => other.TextureGetFormat(owner));
    var buffer = device.StorageBufferCreate(4);
    Reject<ArgumentException>(() => device.TextureGetFormat(buffer));
    device.FreeRID(buffer);
    Task.Run(() => Reject<InvalidOperationException>(() => device.TextureGetFormat(owner))).GetAwaiter().GetResult();
    device.FreeRID(complete);
    Reject<ArgumentException>(() => device.TextureGetFormat(complete));
    Check(device.TextureIsValid(owner) && device.TextureIsShared(slice) &&
        device.TextureGetData(slice, 0).AsSpan().SequenceEqual(layer1.AsSpan(60)), "Slice data and sibling lifetime after freeing the intermediary.");
    device.TextureUpdate(owner, 0, layer0);
    Check(device.TextureGetData(owner, 0).SequenceEqual(layer0) &&
        device.TextureGetData(owner, 1).SequenceEqual(layer1), "Layer update preserves neighbor storage.");
    device.FreeRID(owner);
    Check(!device.TextureIsValid(owner) && !device.TextureIsValid(slice), "Original owner retirement invalidates dependent views.");
    Reject<ArgumentException>(() => device.TextureGetFormat(owner));
    Reject<ArgumentException>(() => device.TextureGetFormat(slice));
    CheckMetadata(sliceFormat, 2, 1, 1, 2, TextureType.Texture);
    var recreated = device.TextureCreate(snapshot, srgbView, [unmodified, layer1]);
    Check(device.TextureGetData(recreated, 0).SequenceEqual(unmodified), "The snapshot retains its own valid shareable-format list after retirement.");
    device.FreeRID(recreated);

    using var transferFormat = new RDTextureFormat
    {
        Width = 5,
        Height = 3,
        MIPMaps = 3,
        Format = DataFormat.R32UINT,
        UsageBits = TextureUsageBits.CanUpdate | TextureUsageBits.CPURead
    };
    using var defaultView = new RDTextureView();
    if (!device.TextureIsFormatSupportedForUsage(transferFormat.Format, transferFormat.UsageBits))
    {
        Reject<NotSupportedException>(() => device.TextureCreate(transferFormat, defaultView, [unmodified]));
        Console.WriteLine("R32UINT with CanUpdate | CPURead is unavailable on this device.");
    }
    else
    {
        var integer = device.TextureCreate(transferFormat, defaultView, [unmodified]);
        using var integerFormat = device.TextureGetFormat(integer);
        Check(integerFormat.Format == DataFormat.R32UINT && integerFormat.UsageBits == transferFormat.UsageBits &&
            device.TextureGetData(integer, 0).SequenceEqual(unmodified), "Supported integer transfer preserves its format, roles and bytes.");
        device.FreeRID(integer);
        Console.WriteLine("R32UINT with CanUpdate | CPURead preserved its metadata and bytes.");
    }
    transferFormat.Format = storage;
    var transferTexture = device.TextureCreate(transferFormat, defaultView, [unmodified]);
    var output = new byte[unmodified.Length];
    for (int i = 0; i < 16; i++) PreparedTransfer();
    long before = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < 32; i++) PreparedTransfer();
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Check(allocated == 0 && output.SequenceEqual(layer0), $"Prepared mip upload and span readback allocated {allocated} managed bytes.");
    device.FreeRID(transferTexture);
    Console.WriteLine("32 prepared RGBA8 mip uploads and span readbacks allocated 0 owner-thread managed bytes.");

    void PreparedTransfer()
    {
        device.TextureUpdate(transferTexture, 0, layer0);
        device.TextureGetData(transferTexture, 0, output.AsSpan());
    }

    void CheckMetadata(RDTextureFormat actual, uint width, uint height, uint layers, uint mips, TextureType type)
    {
        Check(actual.Width == width && actual.Height == height && actual.ArrayLayers == layers &&
            actual.MIPMaps == mips && actual.TextureType == type && actual.Format == storage &&
            actual.UsageBits == usages && actual.Samples == TextureSamples.Samples1 && !actual.IsResolveBuffer, "Stored format and view-local metadata.");
    }
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Reject<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T error) when (error.GetType() == typeof(T)) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}
