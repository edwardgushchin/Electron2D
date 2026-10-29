using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class NativeFontPrecision
{
    internal NativeRasterGlyph RasterizeMSDF(uint glyph, FontHinting hinting, int pixelRange, bool forceAutohinter)
    {
        EnsureOwner();
        if (_bitmap != null || (FaceFlags & 1) == 0) throw new NotSupportedException("MSDF generation requires a scalable outline face.");
        if (glyph >= GlyphCount) throw new ArgumentOutOfRangeException(nameof(glyph));
        if (pixelRange <= 0 || pixelRange > 16384) throw new InvalidOperationException("MSDF pixel range must be between 1 and 16384 before generation.");
        var flags = 8 | (hinting switch { FontHinting.None => NoHinting, FontHinting.Light => LightHinting, _ => 0 });
        if (forceAutohinter) flags |= 1 << 5;
        FTSetTransform(_face, null, null); CheckFT(FTLoadGlyph(_face, glyph, flags));
        var slot = (GlyphSlotRecord*)((FaceRecord*)_face)->Glyph;
        if (slot->Format != 0x6f75746c || slot->Outline.Points == 0) return new([], 0, 0, 0, 0, false);
        ApplyOutlineVariation(slot);
        var context = new MSDFContext(slot->Outline.Points); var handle = GCHandle.Alloc(context);
        try
        {
            var callbacks = new FTOutlineFunctions { Move = &MSDFMoveTo, Line = &MSDFLineTo, Conic = &MSDFConicTo, Cubic = &MSDFCubicTo };
            var error = FTOutlineDecompose(&slot->Outline, &callbacks, GCHandle.ToIntPtr(handle));
            if (context.Error != null) throw new InvalidOperationException("MSDF outline decomposition failed.", context.Error);
            CheckFT(error);
        }
        finally { handle.Free(); }
        byte* pixels = null;
        try
        {
            fixed (MSDFEdge* edges = CollectionsMarshal.AsSpan(context.Edges))
            {
                NativeTextBreak.PrepareMemory();
                var status = GenerateMSDF(edges, context.Edges.Count, pixelRange, FTOutlineGetOrientation(&slot->Outline) == 1 ? 1 : 0, out pixels, out var width, out var height, out var left, out var top);
                if (status == 3) throw new OutOfMemoryException("MSDF generation exhausted native memory.");
                if (status != 0) throw new InvalidOperationException($"MSDF generation rejected the outline or pixel budget ({status}).");
                if (width == 0 || height == 0) return new([], 0, 0, 0, 0, false);
                if (pixels == null || width < 0 || height < 0 || width > 16384 || height > 16384 || (long)width * height * 4 > 64 * 1024 * 1024)
                    throw new InvalidOperationException("MSDF generation returned an invalid image.");
                return new(new ReadOnlySpan<byte>(pixels, checked(width * height * 4)).ToArray(), width, height, (float)left, (float)top, false);
            }
        }
        finally { if (pixels != null) FreeMSDF(pixels); }
    }
    private sealed class MSDFContext(int capacity)
    {
        internal readonly List<MSDFEdge> Edges = new(capacity);
        internal int Contour;
        internal double X, Y;
        internal Exception? Error;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MSDFEdge
    {
        internal int Kind, Contour;
        internal double AX, AY, BX, BY, CX, CY, DX, DY;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct FTOutlineFunctions
    {
        internal delegate* unmanaged[Cdecl]<FTVector*, nint, int> Move, Line;
        internal delegate* unmanaged[Cdecl]<FTVector*, FTVector*, nint, int> Conic;
        internal delegate* unmanaged[Cdecl]<FTVector*, FTVector*, FTVector*, nint, int> Cubic;
        internal int Shift;
        internal CLong Delta;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MSDFMoveTo(FTVector* to, nint user)
    {
        var c = (MSDFContext)GCHandle.FromIntPtr(user).Target!;
        c.Contour = c.Edges.Count == 0 ? 0 : c.Edges[^1].Contour + 1; c.X = to->X.Value / 60f; c.Y = to->Y.Value / 60f; return 0;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MSDFLineTo(FTVector* to, nint user) => AddMSDFEdge(1, to, null, null, user);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MSDFConicTo(FTVector* control, FTVector* to, nint user) => AddMSDFEdge(2, control, to, null, user);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MSDFCubicTo(FTVector* control1, FTVector* control2, FTVector* to, nint user) => AddMSDFEdge(3, control1, control2, to, user);
    private static int AddMSDFEdge(int kind, FTVector* b, FTVector* c, FTVector* d, nint user)
    {
        var context = (MSDFContext)GCHandle.FromIntPtr(user).Target!;
        try
        {
            var edge = new MSDFEdge { Kind = kind, Contour = context.Contour, AX = context.X, AY = context.Y, BX = b->X.Value / 60f, BY = b->Y.Value / 60f };
            if (c != null) { edge.CX = c->X.Value / 60f; edge.CY = c->Y.Value / 60f; }
            if (d != null) { edge.DX = d->X.Value / 60f; edge.DY = d->Y.Value / 60f; }
            var x = kind == 3 ? edge.DX : kind == 2 ? edge.CX : edge.BX; var y = kind == 3 ? edge.DY : kind == 2 ? edge.CY : edge.BY;
            if (kind != 1 || x != context.X || y != context.Y) context.Edges.Add(edge);
            context.X = x; context.Y = y; return 0;
        }
        catch (Exception error) { context.Error = error; return 1; }
    }
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Outline_Decompose"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTOutlineDecompose(FTOutline* outline, FTOutlineFunctions* functions, nint user);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Outline_Get_Orientation"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTOutlineGetOrientation(FTOutline* outline);
    [LibraryImport(NativeLibraries.TextBreakLibrary, EntryPoint = "e2d_font_msdf_generate"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int GenerateMSDF(MSDFEdge* edges, int count, int range, int reverse, out byte* pixels, out int width, out int height, out double left, out double top);
    [LibraryImport(NativeLibraries.TextBreakLibrary, EntryPoint = "e2d_font_msdf_free"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FreeMSDF(byte* pixels);
}
