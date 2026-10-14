using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal readonly record struct NativeRasterGlyph(byte[] Pixels, int Width, int Height, float Left, float Top, bool Colored, bool LCD = false);

internal sealed unsafe partial class NativeFontPrecision
{
    // Cache-miss rasterization owns its returned RGBA bytes. Phase is applied to the outline, not the final texture position.
    internal NativeRasterGlyph Rasterize(uint glyph, FontHinting hinting, int phase26Dot6 = 0, int outlineRadius26Dot6 = 0, bool disableEmbeddedBitmaps = true, bool forceAutohinter = false, FontAntialiasing antialiasing = FontAntialiasing.Gray, FontLCDSubpixelLayout layout = FontLCDSubpixelLayout.HorizontalRGB)
    {
        EnsureOwner();
        if (glyph >= GlyphCount) throw new ArgumentOutOfRangeException(nameof(glyph));
        if ((uint)phase26Dot6 >= 64) throw new ArgumentOutOfRangeException(nameof(phase26Dot6));
        if ((uint)outlineRadius26Dot6 > 16384 * 64) throw new ArgumentOutOfRangeException(nameof(outlineRadius26Dot6));
        var mode = antialiasing switch
        {
            FontAntialiasing.None => 2,
            FontAntialiasing.LCD when layout is FontLCDSubpixelLayout.HorizontalRGB or FontLCDSubpixelLayout.HorizontalBGR => 3,
            FontAntialiasing.LCD when layout is FontLCDSubpixelLayout.VerticalRGB or FontLCDSubpixelLayout.VerticalBGR => 4,
            _ => 0
        };
        var reverse = layout is FontLCDSubpixelLayout.HorizontalBGR or FontLCDSubpixelLayout.VerticalBGR;
        var flags = hinting switch { FontHinting.None => NoHinting, FontHinting.Light => LightHinting, _ => 0 };
        if (forceAutohinter) flags |= 1 << 5;
        if (outlineRadius26Dot6 > 0 || disableEmbeddedBitmaps && (FaceFlags & (1 << 14)) == 0) flags |= 8;
        else if ((FaceFlags & (1 << 14)) != 0) flags |= 1 << 20;
        var delta = new FTVector { X = new CLong(phase26Dot6) };
        nint detached = 0, stroker = 0;
        FTSetTransform(_face, null, &delta);
        try
        {
            CheckFT(FTLoadGlyph(_face, glyph, flags));
            var slot = (GlyphSlotRecord*)((FaceRecord*)_face)->Glyph;
            ApplyOutlineVariation(slot);
            var radiusPixels = outlineRadius26Dot6 / 64d;
            if (slot->Metrics.Width.Value / 64d + radiusPixels * 2 > 16384 || slot->Metrics.Height.Value / 64d + radiusPixels * 2 > 16384)
                throw new InvalidOperationException("Font raster dimensions exceed the canvas texture range.");
            if (outlineRadius26Dot6 == 0)
            {
                CheckFT(FTRenderGlyph((nint)slot, mode));
                return CopyBitmap(slot->Bitmap, slot->BitmapLeft, slot->BitmapTop, reverse);
            }
            CheckFT(FTGetGlyph((nint)slot, out detached));
            CheckFT(FTStrokerNew(_library, out stroker));
            FTStrokerSet(stroker, new CLong(outlineRadius26Dot6), 0, 0, default); // Butt caps and round joins.
            CheckFT(FTGlyphStroke(ref detached, stroker, 1));
            CheckFT(FTGlyphToBitmap(ref detached, mode, null, 1));
            var bitmap = (BitmapGlyphRecord*)detached;
            return CopyBitmap(bitmap->Bitmap, bitmap->Left, bitmap->Top, reverse);
        }
        finally
        {
            if (detached != 0) FTDoneGlyph(detached);
            if (stroker != 0) FTStrokerDone(stroker);
            FTSetTransform(_face, null, null);
        }
    }

    private static NativeRasterGlyph CopyBitmap(RasterBitmap bitmap, int left, int top, bool reverse = false)
    {
        var horizontal = bitmap.PixelMode == 5; var vertical = bitmap.PixelMode == 6;
        if (horizontal && bitmap.Width % 3 != 0 || vertical && bitmap.Rows % 3 != 0) throw new InvalidOperationException("LCD bitmap dimensions must contain complete component triples.");
        var logicalWidth = horizontal ? bitmap.Width / 3 : bitmap.Width; var logicalHeight = vertical ? bitmap.Rows / 3 : bitmap.Rows;
        if (logicalWidth > 16384 || logicalHeight > 16384) throw new InvalidOperationException("Font raster dimensions exceed the canvas texture range.");
        var width = (int)logicalWidth; var height = (int)logicalHeight;
        var colored = bitmap.PixelMode == 7;
        if (width == 0 || height == 0) return new([], width, height, left, top, colored, horizontal || vertical);
        if (bitmap.Buffer == null) throw new InvalidOperationException("A nonempty glyph has no raster buffer.");
        var rowBytes = bitmap.PixelMode switch { 1 => (width + 7) / 8, 2 or 6 => width, 3 => (width + 3) / 4, 4 => (width + 1) / 2, 5 => checked(width * 3), 7 => checked(width * 4), _ => throw new NotSupportedException("The glyph bitmap pixel mode is not supported.") };
        if (Math.Abs((long)bitmap.Pitch) < rowBytes) throw new InvalidOperationException("The glyph bitmap pitch is smaller than its rows.");
        if (bitmap.PixelMode == 2 && bitmap.GrayLevels < 2) throw new InvalidOperationException("The glyph bitmap has an invalid gray scale.");
        var pixels = new byte[checked(width * height * 4)];
        for (var y = 0; y < height; y++)
        {
            var source = bitmap.Buffer + (vertical ? y * 3 : y) * (nint)bitmap.Pitch;
            for (var x = 0; x < width; x++)
            {
                var destination = (y * width + x) * 4;
                if (horizontal || vertical)
                {
                    var first = reverse ? 2 : 0; var last = reverse ? 0 : 2;
                    pixels[destination] = horizontal ? source[x * 3 + first] : source[x + first * (nint)bitmap.Pitch];
                    pixels[destination + 1] = horizontal ? source[x * 3 + 1] : source[x + (nint)bitmap.Pitch];
                    pixels[destination + 2] = horizontal ? source[x * 3 + last] : source[x + last * (nint)bitmap.Pitch];
                    pixels[destination + 3] = 255;
                }
                else if (colored)
                {
                    var alpha = source[x * 4 + 3];
                    pixels[destination] = Unpremultiply(source[x * 4 + 2], alpha);
                    pixels[destination + 1] = Unpremultiply(source[x * 4 + 1], alpha);
                    pixels[destination + 2] = Unpremultiply(source[x * 4], alpha);
                    pixels[destination + 3] = alpha;
                }
                else
                {
                    var alpha = bitmap.PixelMode switch
                    {
                        1 => (source[x >> 3] & (0x80 >> (x & 7))) != 0 ? 255 : 0,
                        2 => source[x] * 255 / (bitmap.GrayLevels - 1),
                        3 => ((source[x >> 2] >> (6 - (x & 3) * 2)) & 3) * 85,
                        _ => ((source[x >> 1] >> (4 - (x & 1) * 4)) & 15) * 17
                    };
                    pixels[destination] = pixels[destination + 1] = pixels[destination + 2] = 255;
                    pixels[destination + 3] = (byte)alpha;
                }
            }
        }
        return new(pixels, width, height, left, top, colored, horizontal || vertical);
    }

    private static byte Unpremultiply(byte component, byte alpha) => alpha == 0 ? (byte)0 : (byte)Math.Min(255, (component * 255 + alpha / 2) / alpha);

    [StructLayout(LayoutKind.Sequential)] private struct FTVector { internal CLong X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct FTMatrix { internal CLong XX, XY, YX, YY; }
    [StructLayout(LayoutKind.Sequential)]
    private struct RasterBitmap
    {
        internal uint Rows, Width;
        internal int Pitch;
        internal byte* Buffer;
        internal ushort GrayLevels;
        internal byte PixelMode, PaletteMode;
        internal nint Palette;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct GlyphRecord
    {
        internal nint Library, Class;
        internal uint Format;
        internal FTVector Advance;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapGlyphRecord
    {
        internal GlyphRecord Root;
        internal int Left, Top;
        internal RasterBitmap Bitmap;
    }

    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Set_Transform"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FTSetTransform(nint face, FTMatrix* matrix, FTVector* delta);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Render_Glyph"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTRenderGlyph(nint slot, int mode);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Get_Glyph"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTGetGlyph(nint slot, out nint glyph);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Done_Glyph"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FTDoneGlyph(nint glyph);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Stroker_New"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTStrokerNew(nint library, out nint stroker);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Stroker_Set"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FTStrokerSet(nint stroker, CLong radius, int cap, int join, CLong miterLimit);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Stroker_Done"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FTStrokerDone(nint stroker);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Glyph_Stroke"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTGlyphStroke(ref nint glyph, nint stroker, int destroy);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Glyph_To_Bitmap"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTGlyphToBitmap(ref nint glyph, int mode, FTVector* origin, int destroy);
}
