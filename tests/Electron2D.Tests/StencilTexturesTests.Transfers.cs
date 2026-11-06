using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class StencilTexturesTests
{
    private static void TransfersAndViews(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 3, Height = 5, ArrayLayers = 2, MIPMaps = 3, TextureType = TextureType.TextureArray, Format = DataFormat.S8UINT, UsageBits = Roles | Transfers | TextureUsageBits.CPURead }; using var view = new RDTextureView();
        var zero = device.TextureCreate(format, view); Check(device.TextureGetData(zero, 0).Length == 18 && device.TextureGetData(zero, 0).All(value => value == 0), "Every stencil mip initializes to zero with tight one-byte public data.");
        byte[] first = Enumerable.Range(0, 18).Select(i => (byte)(i * 13)).ToArray(), second = Enumerable.Range(0, 18).Select(i => (byte)(255 - i * 7)).ToArray();
        var image = device.TextureCreate(format, view, [first, second]); Check(device.TextureGetData(image, 0).SequenceEqual(first) && device.TextureGetData(image, 1).SequenceEqual(second), "Unaligned stencil mip chains preserve independent layer bytes.");
        using var actual = device.TextureGetFormat(image); Check(actual.Format == DataFormat.S8UINT && actual.Width == 3 && actual.MIPMaps == 3 && actual.ArrayLayers == 2, "Metadata retains exact stencil format and extent.");
        device.TextureUpdate(image, 0, second); Check(device.TextureGetData(image, 0).SequenceEqual(second), "Stencil mip updates preserve every byte.");
        var shared = device.TextureCreateShared(view, image); var slice = device.TextureCreateSharedFromSlice(view, shared, 1, 1);
        Check(device.TextureGetData(slice, 0).SequenceEqual(second.AsSpan(15, 2).ToArray()), "Nested shared stencil views select exact physical mip/layer data.");
        device.TextureUpdate(slice, 0, [19, 211]); second[15] = 19; second[16] = 211; Check(device.TextureGetData(image, 1).SequenceEqual(second), "Shared stencil update changes only the selected mip/layer.");
        var queued = new byte[18]; var callbacks = 0; device.TextureGetDataAsync(image, 1, queued.AsMemory(), () => callbacks++); device.TextureUpdate(image, 1, first); device.Submit(); device.Sync(); Check(callbacks == 1 && queued.SequenceEqual(second), "Queued stencil readback preserves request-time contents.");
        var copy = Image(device, DataFormat.S8UINT, Roles | Transfers); byte[] pattern = Enumerable.Range(0, 16).Select(i => (byte)(i + 1)).ToArray(); device.TextureUpdate(copy, 0, pattern);
        device.TextureCopy(copy, copy, new(0, 0, 0), new(2, 0, 0), new(2, 4, 1), 0, 0, 0, 0);
        for (var row = 0; row < 4; row++) { pattern[row * 4 + 2] = pattern[row * 4]; pattern[row * 4 + 3] = pattern[row * 4 + 1]; }
        Check(device.TextureGetData(copy, 0).SequenceEqual(pattern), "Disjoint stencil self-copy uses matching-aspect scratch.");
        Reject<ArgumentException>(() => device.TextureCopy(copy, copy, new(0, 0, 0), new(1, 0, 0), new(2, 4, 1), 0, 0, 0, 0));
        Reject<ArgumentException>(() => device.TextureUpdate(copy, 0, new byte[15])); Reject<ArgumentOutOfRangeException>(() => device.TextureGetData(copy, 1));
        using var alternate = new RDTextureView { FormatOverride = DataFormat.R8UINT }; Reject<ArgumentException>(() => device.TextureCreateShared(alternate, image));
        Check(!device.TextureIsFormatSupportedForUsage(DataFormat.S8UINT, TextureUsageBits.ColorAttachment) && !device.TextureIsFormatSupportedForUsage(DataFormat.S8UINT, TextureUsageBits.Storage), "Stencil has no substituted color/storage roles.");
        device.FreeRID(image); Check(!device.TextureIsValid(shared) && !device.TextureIsValid(slice), "Owner retirement invalidates stencil views.");
        Reject<ArgumentException>(() => device.FreeRID(slice)); Reject<ArgumentException>(() => device.FreeRID(shared));
        foreach (var rid in new[] { zero, copy }) device.FreeRID(rid);
    }
}
