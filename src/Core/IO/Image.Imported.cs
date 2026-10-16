using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class Image
{
    internal static State ReadImportedBasis(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < 68 or > MaximumEncodedBytes) throw new InvalidDataException("Invalid imported Basis size.");
        var layout = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        var ktx = (layout & 0x80000000u) != 0; layout &= 0x7fffffffu;
        if (layout > 5) throw new InvalidDataException("Unknown imported Basis channel layout.");
        var state = ReadNativeTexture(bytes[4..].ToArray(), legacyBasis: !ktx, requireBasis: true, linearize: false);
        if ((state.Format == Format.Rgbh) != (layout == 5)) throw new InvalidDataException("Basis HDR data disagrees with its channel role.");
        if (layout == 3)
        {
            for (var i = 0; i < state.Data.Length; i += 4) state.Data[i + 1] = state.Data[i + 3];
        }
        return ConvertState(state, layout switch { 0 or 3 => Format.Rg8, 1 => Format.Rgb8, 2 => Format.Rgba8, 4 => Format.R8, _ => Format.Rgbh });
    }

    internal static int ImportedDataSize(int width, int height, Format format, bool mipmaps) => GetRequiredDataSize(width, height, format, mipmaps);
    internal static int ImportedMIPCount(int width, int height, bool mipmaps) => GetRequiredMIPMapCount(width, height, mipmaps);
    internal static Image FromImportedState(State state) { var image = new Image(); image.SetStateWithoutNotification(state); return image; }

    [LibraryImport(NativeLibraries.KTXLibrary, EntryPoint = "e2d_basis_create")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int BasisCreate(byte* source, ulong length, out KTXInfo info, out nint result);
}
