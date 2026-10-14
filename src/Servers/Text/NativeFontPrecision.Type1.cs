using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class NativeFontPrecision
{
    private bool IsSFNT => (FaceFlags & 8) != 0;
    private void AttachMetrics(byte[]? metrics)
    {
        if (metrics == null || metrics.Length == 0) return;
        if (IsSFNT) throw new InvalidDataException("Auxiliary Type 1 metrics require a non-SFNT outline face.");
        fixed (byte* bytes = metrics)
        {
            var args = new FTOpenArguments { Flags = 1, Memory = bytes, Length = new CLong(metrics.Length) };
            CheckFT(FTAttachStream(_face, &args));
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct FTOpenArguments
    {
        internal uint Flags;
        internal byte* Memory;
        internal CLong Length;
        internal nint Path, Stream, Driver;
        internal int ParameterCount;
        internal nint Parameters;
    }
    private void ConfigureOutlineCallbacks(nint funcs)
    {
        HBFontFuncsSetNominalGlyph(funcs, (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, uint*, nint, int>)&OutlineNominalGlyph, 0, 0);
        HBFontFuncsSetVariationGlyph(funcs, (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, uint, uint*, nint, int>)&OutlineVariationGlyph, 0, 0);
        HBFontFuncsSetHorizontalKerning(funcs, (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, uint, nint, int>)&OutlineHorizontalKerning, 0, 0);
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OutlineNominalGlyph(nint font, nint data, uint scalar, uint* glyph, nint user)
    {
        var state = (CallbackState*)data;
        try { *glyph = FTGetCharIndex(state->Face, new CULong(scalar)); return *glyph != 0 ? 1 : 0; }
        catch { state->Error = -1; *glyph = 0; return 0; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OutlineVariationGlyph(nint font, nint data, uint scalar, uint selector, uint* glyph, nint user)
    {
        var state = (CallbackState*)data;
        try { *glyph = FTGetCharVariantIndex(state->Face, new CULong(scalar), new CULong(selector)); return *glyph != 0 ? 1 : 0; }
        catch { state->Error = -1; *glyph = 0; return 0; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OutlineHorizontalKerning(nint font, nint data, uint left, uint right, nint user)
    {
        var state = (CallbackState*)data;
        try
        {
            var error = FTGetKerning(state->Face, left, right, 1, out var delta);
            if (error != 0) { state->Error = error; return 0; }
            return checked((int)delta.X.Value);
        }
        catch { state->Error = -1; return 0; }
    }
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_funcs_set_variation_glyph_func"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontFuncsSetVariationGlyph(nint funcs, nint callback, nint data, nint destroy);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_funcs_set_glyph_h_kerning_func"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontFuncsSetHorizontalKerning(nint funcs, nint callback, nint data, nint destroy);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Attach_Stream"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTAttachStream(nint face, FTOpenArguments* args);
}
