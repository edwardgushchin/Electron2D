using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceTexelTests
{
    private static void Transfers(RenderingDevice device)
    {
        byte[] initial = [1, 2, 3, 4, 5, 6]; var texels = device.TextureBufferCreate(2, DataFormat.R8G8B8UINT, initial);
        Check(device.BufferGetData(texels).SequenceEqual(initial), "Non-word texel capacity remains exact.");
        device.BufferUpdate(texels, 1, 3, [9, 8, 7]);
        Check(device.BufferGetData(texels).SequenceEqual(new byte[] { 1, 9, 8, 7, 5, 6 }), "Unaligned transfers preserve the private header.");
        var storage = device.StorageBufferCreate(8); device.BufferCopy(texels, storage, 1, 2, 4);
        Check(device.BufferGetData(storage).SequenceEqual(new byte[] { 0, 0, 9, 8, 7, 5, 0, 0 }), "Texel-to-storage copies preserve raw byte offsets.");
        device.BufferClear(texels, 1, 4); Check(device.BufferGetData(texels).SequenceEqual(new byte[] { 1, 0, 0, 0, 0, 6 }), "Clears preserve header and neighboring bytes.");
        device.BufferCopy(storage, texels, 2, 1, 4); var output = new byte[6]; var calls = 0; Action callback = () => calls++;
        device.BufferGetDataAsync(texels, output, callback); device.Submit(); device.Sync(); Check(calls == 1 && output.SequenceEqual(new byte[] { 1, 9, 8, 7, 5, 6 }), "Asynchronous texel snapshots use logical data offsets.");
        Reject<ArgumentOutOfRangeException>(() => device.TextureBufferCreate(0, DataFormat.R8UNORM));
        Reject<ArgumentOutOfRangeException>(() => device.TextureBufferCreate(uint.MaxValue, DataFormat.R32G32B32A32SFLOAT));
        Reject<ArgumentException>(() => device.TextureBufferCreate(2, DataFormat.R8G8UINT, [1]));
        Reject<NotSupportedException>(() => device.TextureBufferCreate(1, DataFormat.D32SFLOAT));
        Reject<NotSupportedException>(() => device.TextureBufferCreate(1, DataFormat.R64SFLOAT));
        Reject<ArgumentOutOfRangeException>(() => device.BufferGetData(texels, new byte[7].AsSpan()));
        device.FreeRID(texels); Reject<ArgumentException>(() => device.BufferGetData(texels)); device.FreeRID(storage);
    }
}
