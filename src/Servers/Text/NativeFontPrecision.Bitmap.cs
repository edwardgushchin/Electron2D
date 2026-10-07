using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Electron2D;

internal sealed unsafe partial class NativeFontPrecision
{
    private FontCache? _bitmap;
    private GCHandle _bitmapHandle;
    internal NativeFontPrecision(FontCache bitmap)
    {
        _bitmap = bitmap;
        try
        {
            _callbacks = (CallbackState*)NativeMemory.AllocZeroed((nuint)sizeof(CallbackState));
            _bitmapHandle = GCHandle.Alloc(this); _callbacks->Bitmap = GCHandle.ToIntPtr(_bitmapHandle);
            _hbFace = HBFaceCreate((nint)(delegate* unmanaged[Cdecl]<nint, uint, nint, nint>)&BitmapReferenceTable, 0, 0);
            HBFaceSetGlyphCount(_hbFace, 0x110000); HBFaceSetUpem(_hbFace, 64);
            _font = HBFontCreate(_hbFace);
            var funcs = HBFontFuncsCreate();
            if (_font == 0 || funcs == 0) throw new OutOfMemoryException();
            try
            {
                HBFontFuncsSetNominalGlyph(funcs, (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, uint*, nint, int>)&BitmapNominalGlyph, 0, 0);
                HBFontFuncsSetHorizontalAdvance(funcs, (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, nint, int>)&HorizontalAdvance, 0, 0);
                HBFontFuncsSetVerticalAdvance(funcs, (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, nint, int>)&VerticalAdvance, 0, 0);
                HBFontFuncsSetGlyphExtents(funcs, (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, HBGlyphExtents*, nint, int>)&GlyphExtents, 0, 0);
                HBFontSetFuncs(_font, funcs, (nint)_callbacks, 0);
            }
            finally { HBFontFuncsDestroy(funcs); }
            _buffer = HBBufferCreate(); if (_buffer == 0) throw new OutOfMemoryException();
            FaceCount = 1; GlyphCount = 0x110000; SetSize(16);
        }
        catch { Release(); throw; }
    }
    private static NativeFontPrecision BitmapOwner(CallbackState* state) => (NativeFontPrecision)GCHandle.FromIntPtr(state->Bitmap).Target!;
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int BitmapNominalGlyph(nint font, nint data, uint scalar, uint* glyph, nint userData)
    {
        var state = (CallbackState*)data;
        try { var owner = BitmapOwner(state); *glyph = owner._bitmap!.HasGlyph(scalar) ? scalar : 0; return *glyph == 0 ? 0 : 1; }
        catch { state->Error = -1; *glyph = 0; return 0; }
    }
    private void SetBitmapSize(float pixels)
    {
        var size = checked((int)Math.Round(pixels));
        var record = _bitmap!.Select(size); var factor = _bitmap.Factor(pixels);
        Ascent = float.IsNaN(record?.Metrics[0] ?? float.NaN) ? 0 : record!.Metrics[0] * factor;
        Descent = float.IsNaN(record?.Metrics[1] ?? float.NaN) ? 0 : record!.Metrics[1] * factor;
        UnderlinePosition = float.IsNaN(record?.Metrics[2] ?? float.NaN) ? 0 : record!.Metrics[2] * factor;
        UnderlineThickness = float.IsNaN(record?.Metrics[3] ?? float.NaN) ? 0 : record!.Metrics[3] * factor;
        LineHeight = Height; HBFontSetScale(_font, checked((int)(pixels * 64)), checked((int)(pixels * 64))); _size = pixels;
    }
    private int BitmapAdvance(uint glyph, bool vertical)
    {
        var advance = _bitmap!.Advance(glyph, checked((int)Math.Round(_size)));
        return checked((int)Math.Round((vertical ? -advance.Y : advance.X) * 64));
    }
    private Rect2 BitmapBounds(uint glyph)
    {
        var record = _bitmap!.Select(checked((int)Math.Round(_size)));
        var entry = record?.Glyphs.GetValueOrDefault(glyph) ?? default;
        var factor = _bitmap.Factor(_size);
        return new(entry.Offset * factor, entry.Size * factor);
    }
    private string BitmapCharacters()
    {
        var text = new StringBuilder();
        foreach (var scalar in _bitmap!.Sizes.Where(p => p.Key.Y == 0).SelectMany(p => p.Value.Glyphs.Keys).Distinct().Order())
            if (Rune.TryCreate(scalar, out var rune)) text.Append(rune.ToString());
        return text.ToString();
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nint BitmapReferenceTable(nint face, uint tag, nint data) => HBBlobGetEmpty();
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_face_set_glyph_count"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFaceSetGlyphCount(nint face, uint count);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_face_set_upem"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFaceSetUpem(nint face, uint upem);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_funcs_set_nominal_glyph_func"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontFuncsSetNominalGlyph(nint funcs, nint callback, nint data, nint destroy);
}
