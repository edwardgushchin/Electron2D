using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Electron2D;

internal enum NativeTextDirection { LTR = 4, RTL = 5, TTB = 6, BTT = 7 }

[StructLayout(LayoutKind.Sequential)]
internal readonly struct NativeFontFeature(uint tag, uint value, uint start = 0, uint end = uint.MaxValue)
{
    internal readonly uint Tag = tag, Value = value, Start = start, End = end;
}

internal readonly record struct NativeShapedGlyph(uint GlyphIndex, uint Cluster, int XAdvance, int YAdvance, int XOffset, int YOffset, uint Flags = 0);

// Owns a public FreeType face and HarfBuzz table/font/buffer handles on one text-worker thread.
// Positions remain signed 26.6 values until the caller applies layout and rounding policies.
internal sealed unsafe partial class NativeFontPrecision : IDisposable
{
    private const int NoHinting = 2, VerticalLayout = 16, LightHinting = 1 << 16;
    private readonly int _ownerThread = Environment.CurrentManagedThreadId;
    private nint _library, _face, _hbFace, _parentFont, _font, _buffer;
    private byte* _data;
    private CallbackState* _callbacks;
    private NativeShapedGlyph[] _glyphs = [];
    private string? _languageText;
    private nint _language;
    private float _size;

    internal string FamilyName { get; private set; } = string.Empty;
    internal string StyleName { get; private set; } = string.Empty;
    internal int FaceCount { get; private set; }
    internal int GlyphCount { get; private set; }
    internal int UnitsPerEm { get; private set; }
    internal long FaceFlags { get; private set; }
    internal long FaceStyleFlags { get; private set; }
    internal float Ascent { get; private set; }
    internal float Descent { get; private set; }
    internal float Height => Ascent + Descent;
    internal float LineHeight { get; private set; }
    internal float UnderlinePosition { get; private set; }
    internal float UnderlineThickness { get; private set; }

    internal NativeFontPrecision(byte[] immutableData, int faceIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(immutableData);
        ArgumentOutOfRangeException.ThrowIfNegative(faceIndex);
        if (immutableData.Length == 0) throw new ArgumentException("Font data is empty.", nameof(immutableData));
        try
        {
            CheckFT(FTInit(out _library));
            _data = (byte*)NativeMemory.Alloc((nuint)immutableData.Length);
            immutableData.CopyTo(new Span<byte>(_data, immutableData.Length));
            CheckFT(FTNewMemoryFace(_library, _data, new CLong(immutableData.Length), new CLong(faceIndex), out _face));
            var face = (FaceRecord*)_face;
            if ((face->Flags.Value & 8) == 0) throw new NotSupportedException("This font requires an SFNT table source.");
            FaceCount = checked((int)face->Count.Value);
            GlyphCount = checked((int)face->GlyphCount.Value);
            UnitsPerEm = face->UnitsPerEm;
            FaceFlags = face->Flags.Value;
            FaceStyleFlags = face->StyleFlags.Value;
            FamilyName = Marshal.PtrToStringUTF8(face->FamilyName) ?? string.Empty;
            StyleName = Marshal.PtrToStringUTF8(face->StyleName) ?? string.Empty;
            _callbacks = (CallbackState*)NativeMemory.AllocZeroed((nuint)sizeof(CallbackState));
            _callbacks->Face = _face;
            _hbFace = HBFaceCreate((nint)(delegate* unmanaged[Cdecl]<nint, uint, nint, nint>)&ReferenceTable, (nint)_callbacks, 0);
            if (_hbFace == 0 || HBFaceGetGlyphCount(_hbFace) == 0) throw new ArgumentException("Font has no usable shaping tables.", nameof(immutableData));
            _parentFont = HBFontCreate(_hbFace);
            HBOTFontSetFuncs(_parentFont);
            _font = HBFontCreateSubFont(_parentFont);
            var funcs = HBFontFuncsCreate();
            if (funcs == 0 || _parentFont == 0 || _font == 0) throw new OutOfMemoryException();
            try
            {
                HBFontFuncsSetHorizontalAdvance(funcs, (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, nint, int>)&HorizontalAdvance, 0, 0);
                HBFontFuncsSetVerticalAdvance(funcs, (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, nint, int>)&VerticalAdvance, 0, 0);
                HBFontFuncsSetGlyphExtents(funcs, (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, HBGlyphExtents*, nint, int>)&GlyphExtents, 0, 0);
                HBFontFuncsSetVerticalOrigin(funcs, (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, int*, int*, nint, int>)&VerticalOrigin, 0, 0);
                HBFontSetFuncs(_font, funcs, (nint)_callbacks, 0);
            }
            finally { HBFontFuncsDestroy(funcs); }
            _buffer = HBBufferCreate();
            if (_buffer == 0 || HBBufferAllocationSuccessful(_buffer) == 0) throw new OutOfMemoryException();
            SetSize(16); ReadVariationMetadata();
        }
        catch { Release(); throw; }
    }

    internal void SetSize(float pixels)
    {
        EnsureOwner();
        if (!float.IsFinite(pixels) || pixels <= 0 || pixels * 64d > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(pixels));
        if (_size == pixels) return;
        if (_bitmap != null) { SetBitmapSize(pixels); return; }
        var size = checked((int)Math.Round(pixels * 64d));
        if (size == 0) throw new ArgumentOutOfRangeException(nameof(pixels));
        CheckFT(FTSetCharSize(_face, new CLong(size), new CLong(size), 72, 72));
        var face = (FaceRecord*)_face;
        var metrics = ((SizeRecord*)face->Size)->Metrics;
        var xScale = checked((int)(((ulong)metrics.XScale.Value * face->UnitsPerEm + 32768) >> 16));
        var yScale = checked((int)(((ulong)metrics.YScale.Value * face->UnitsPerEm + 32768) >> 16));
        HBFontSetScale(_parentFont, xScale, yScale);
        HBFontSetScale(_font, xScale, yScale);
        Ascent = metrics.Ascent.Value / 64f;
        Descent = -metrics.Descent.Value / 64f;
        LineHeight = metrics.Height.Value / 64f;
        UnderlinePosition = -FTMulFix(new CLong(face->UnderlinePosition), metrics.YScale).Value / 64f;
        UnderlineThickness = FTMulFix(new CLong(face->UnderlineThickness), metrics.YScale).Value / 64f;
        _size = pixels;
    }

    // The returned view is invalidated by the next Shape call on this instance.
    internal ReadOnlySpan<NativeShapedGlyph> Shape(ReadOnlySpan<uint> scalars, NativeTextDirection direction,
        uint scriptTag = 0, string language = "", ReadOnlySpan<NativeFontFeature> features = default) =>
        Shape(scalars, 0, scalars.Length, direction, scriptTag, language, features);

    // Keep the full paragraph available to shaping even when a font/script run is only a subrange.
    // Cluster values remain indices in scalars, including start; feature ranges use that same indexing.
    internal ReadOnlySpan<NativeShapedGlyph> Shape(ReadOnlySpan<uint> scalars, int start, int count, NativeTextDirection direction,
        uint scriptTag = 0, string language = "", ReadOnlySpan<NativeFontFeature> features = default)
    {
        EnsureOwner();
        if ((uint)start > (uint)scalars.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if ((uint)count > (uint)(scalars.Length - start)) throw new ArgumentOutOfRangeException(nameof(count));
        ArgumentNullException.ThrowIfNull(language);
        if (direction < NativeTextDirection.LTR || direction > NativeTextDirection.BTT) throw new ArgumentOutOfRangeException(nameof(direction));
        foreach (var scalar in scalars)
            if (!Rune.IsValid(scalar)) throw new ArgumentException("Text contains an invalid Unicode scalar.", nameof(scalars));
        if (!string.Equals(_languageText, language, StringComparison.Ordinal))
        {
            if (language.Contains('\0')) throw new ArgumentException("Language must not contain NUL.", nameof(language));
            _language = HBLanguageFromString(language, -1);
            _languageText = language;
        }
        _callbacks->Error = 0;
        HBBufferClear(_buffer);
        HBBufferSetDirection(_buffer, direction);
        HBBufferSetScript(_buffer, scriptTag);
        HBBufferSetLanguage(_buffer, _language);
        HBBufferSetFlags(_buffer, 0x80u | (start == 0 ? 1u : 0u) | (start + count == scalars.Length ? 2u : 0u));
        fixed (uint* text = scalars) HBBufferAddUTF32(_buffer, text, scalars.Length, (uint)start, count);
        HBBufferGuessProperties(_buffer);
        fixed (NativeFontFeature* values = features) HBShape(_font, _buffer, values, (uint)features.Length);
        if (_callbacks->Error != 0) CheckFT(_callbacks->Error);
        if (HBBufferAllocationSuccessful(_buffer) == 0) throw new OutOfMemoryException();
        var infos = HBBufferGetGlyphInfos(_buffer, out var glyphCount);
        var positions = HBBufferGetGlyphPositions(_buffer, out var positionCount);
        if (glyphCount != positionCount || glyphCount > int.MaxValue) throw new InvalidOperationException("Inconsistent shaped glyph data.");
        if (_glyphs.Length < glyphCount) Array.Resize(ref _glyphs, Math.Max(checked((int)glyphCount), Math.Max(16, _glyphs.Length * 2)));
        for (var i = 0; i < (int)glyphCount; i++)
            _glyphs[i] = new(infos[i].Codepoint, infos[i].Cluster, positions[i].XAdvance, positions[i].YAdvance, positions[i].XOffset, positions[i].YOffset, infos[i].Mask & 7u);
        return _glyphs.AsSpan(0, (int)glyphCount);
    }

    internal uint GetGlyphIndex(uint scalar, uint variationSelector = 0)
    {
        EnsureOwner();
        if (!Rune.IsValid(scalar) || variationSelector != 0 && !Rune.IsValid(variationSelector)) throw new ArgumentOutOfRangeException(nameof(scalar));
        if (_bitmap != null) return variationSelector == 0 && _bitmap.HasGlyph(scalar) ? scalar : 0;
        return variationSelector == 0 ? FTGetCharIndex(_face, new CULong(scalar)) : FTGetCharVariantIndex(_face, new CULong(scalar), new CULong(variationSelector));
    }

    internal Vector2 GetKerning(uint left, uint right)
    {
        EnsureOwner(); if (_bitmap != null) return Vector2.Zero;
        if (left >= GlyphCount || right >= GlyphCount) throw new ArgumentOutOfRangeException(nameof(left));
        CheckFT(FTGetKerning(_face, left, right, 0, out var delta)); return new(delta.X.Value / 64f, delta.Y.Value / 64f);
    }
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Get_Kerning"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTGetKerning(nint face, uint left, uint right, uint mode, out FTVector delta);
    internal int GetGlyphAdvance(uint glyph, bool vertical = false)
    {
        EnsureOwner();
        if (glyph >= GlyphCount) throw new ArgumentOutOfRangeException(nameof(glyph));
        if (_bitmap != null) return BitmapAdvance(glyph, vertical);
        CheckFT(FTGetAdvance(_face, glyph, NoHinting | (vertical ? VerticalLayout : 0), out var advance));
        return ConvertAdvance(advance.Value, vertical);
    }

    // Unhinted outline metrics in local pixels; Y grows downward from the baseline.
    internal Rect2 GetGlyphBounds(uint glyph) => ReadGlyphBounds(glyph, NoHinting);

    // Raster metrics use the same light hint target as the default text glyph rasterizer.
    internal Rect2 GetRasterBounds(uint glyph, bool noHinting = false) => ReadGlyphBounds(glyph, noHinting ? NoHinting : LightHinting);
    internal Rect2 GetRasterBounds(uint glyph, FontHinting hinting) => ReadGlyphBounds(glyph, hinting switch
    {
        FontHinting.None => NoHinting,
        FontHinting.Light => LightHinting,
        _ => 0
    });

    private Rect2 ReadGlyphBounds(uint glyph, int flags)
    {
        EnsureOwner();
        if (glyph >= GlyphCount) throw new ArgumentOutOfRangeException(nameof(glyph));
        if (_bitmap != null) return BitmapBounds(glyph);
        CheckFT(FTLoadGlyph(_face, glyph, flags));
        var metrics = ((GlyphSlotRecord*)((FaceRecord*)_face)->Glyph)->Metrics;
        return new Rect2(metrics.BearingX.Value / 64f, -metrics.BearingY.Value / 64f, metrics.Width.Value / 64f, metrics.Height.Value / 64f);
    }

    internal byte[] GetSFNTTable(uint tag)
    {
        EnsureOwner();
        if (_bitmap != null) return [];
        CULong size = default;
        var error = FTLoadSFNTTable(_face, new CULong(tag), default, null, ref size);
        if (error == 0x8E) return []; // The optional table is absent.
        CheckFT(error);
        var result = new byte[checked((int)size.Value)];
        fixed (byte* data = result) CheckFT(FTLoadSFNTTable(_face, new CULong(tag), default, data, ref size));
        if (size.Value != (nuint)result.Length) throw new InvalidOperationException("Font table size changed while reading it.");
        return result;
    }

    internal string GetSupportedChars()
    {
        EnsureOwner();
        if (_bitmap != null) return BitmapCharacters();
        var result = new StringBuilder();
        Span<char> encoded = stackalloc char[2];
        var character = FTGetFirstChar(_face, out var glyph);
        while (glyph != 0)
        {
            if (character.Value <= 0x10FFFF && Rune.TryCreate((uint)character.Value, out var rune))
                result.Append(encoded[..rune.EncodeToUtf16(encoded)]);
            var next = FTGetNextChar(_face, character, out glyph);
            if (glyph != 0 && next.Value <= character.Value) throw new InvalidOperationException("Font character map did not advance.");
            character = next;
        }
        return result.ToString();
    }

    private void EnsureOwner()
    {
        ObjectDisposedException.ThrowIf(_font == 0, this);
        if (Environment.CurrentManagedThreadId != _ownerThread) throw new InvalidOperationException("Font precision operations must run on their creating text thread.");
    }

    public void Dispose()
    {
        if (_font == 0) return;
        EnsureOwner();
        Release();
        GC.SuppressFinalize(this);
    }

    ~NativeFontPrecision() => Release();

    private void Release()
    {
        if (_buffer != 0) HBBufferDestroy(_buffer);
        if (_font != 0) HBFontDestroy(_font);
        if (_parentFont != 0) HBFontDestroy(_parentFont);
        if (_hbFace != 0) HBFaceDestroy(_hbFace);
        if (_face != 0) FTDoneFace(_face);
        if (_library != 0) FTDone(_library);
        if (_bitmapHandle.IsAllocated) _bitmapHandle.Free();
        NativeMemory.Free(_callbacks);
        NativeMemory.Free(_data);
        _buffer = _font = _parentFont = _hbFace = _face = _library = 0;
        _callbacks = null;
        _data = null;
    }

    private static void CheckFT(int error)
    {
        if (error != 0) throw new InvalidOperationException($"Font precision operation failed with native error {error}.");
    }

    private static int ConvertAdvance(nint advance, bool vertical) => checked((int)((vertical ? -advance + 512 : Math.Abs(advance) + 512) >> 10));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int HorizontalAdvance(nint font, nint data, uint glyph, nint userData) => ReadAdvance((CallbackState*)data, glyph, false);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int VerticalAdvance(nint font, nint data, uint glyph, nint userData) => ReadAdvance((CallbackState*)data, glyph, true);

    private static int ReadAdvance(CallbackState* state, uint glyph, bool vertical)
    {
        try
        {
            if (state->Bitmap != 0) return BitmapOwner(state).BitmapAdvance(glyph, vertical);
            var error = FTGetAdvance(state->Face, glyph, NoHinting | (vertical ? VerticalLayout : 0), out var advance);
            if (error != 0) { state->Error = error; return 0; }
            return ConvertAdvance(advance.Value, vertical);
        }
        catch { state->Error = -1; return 0; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int GlyphExtents(nint font, nint data, uint glyph, HBGlyphExtents* extents, nint userData)
    {
        var state = (CallbackState*)data;
        try
        {
            if (state->Bitmap != 0)
            {
                var bounds = BitmapOwner(state).BitmapBounds(glyph);
                extents->XBearing = checked((int)(bounds.Position.X * 64)); extents->YBearing = checked((int)(-bounds.Position.Y * 64));
                extents->Width = checked((int)(bounds.Size.X * 64)); extents->Height = checked((int)(-bounds.Size.Y * 64)); return 1;
            }
            var error = FTLoadGlyph(state->Face, glyph, NoHinting);
            if (error != 0) { state->Error = error; return 0; }
            var metrics = ((GlyphSlotRecord*)((FaceRecord*)state->Face)->Glyph)->Metrics;
            extents->XBearing = checked((int)metrics.BearingX.Value);
            extents->YBearing = checked((int)metrics.BearingY.Value);
            extents->Width = checked((int)metrics.Width.Value);
            extents->Height = checked(-(int)metrics.Height.Value);
            return 1;
        }
        catch { state->Error = -1; return 0; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int VerticalOrigin(nint font, nint data, uint glyph, int* x, int* y, nint userData)
    {
        var state = (CallbackState*)data;
        try
        {
            var error = FTLoadGlyph(state->Face, glyph, NoHinting);
            if (error != 0) { state->Error = error; return 0; }
            var metrics = ((GlyphSlotRecord*)((FaceRecord*)state->Face)->Glyph)->Metrics;
            *x = checked((int)(metrics.BearingX.Value - metrics.VerticalBearingX.Value));
            *y = checked((int)(metrics.BearingY.Value + metrics.VerticalBearingY.Value));
            return 1;
        }
        catch { state->Error = -1; return 0; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nint ReferenceTable(nint face, uint tag, nint data)
    {
        var state = (CallbackState*)data;
        byte* table = null;
        try
        {
            CULong size = default;
            if (FTLoadSFNTTable(state->Face, new CULong(tag), default, null, ref size) != 0 || size.Value == 0) return HBBlobGetEmpty();
            if (size.Value > int.MaxValue) { state->Error = -1; return HBBlobGetEmpty(); }
            table = (byte*)NativeMemory.Alloc(size.Value);
            var error = FTLoadSFNTTable(state->Face, new CULong(tag), default, table, ref size);
            if (error != 0) { state->Error = error; NativeMemory.Free(table); return HBBlobGetEmpty(); }
            return HBBlobCreate(table, (uint)size.Value, 2, (nint)table, (nint)(delegate* unmanaged[Cdecl]<nint, void>)&FreeTable);
        }
        catch { NativeMemory.Free(table); state->Error = -1; return HBBlobGetEmpty(); }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void FreeTable(nint data) => NativeMemory.Free((void*)data);

    [StructLayout(LayoutKind.Sequential)] private struct CallbackState { internal nint Face, Bitmap; internal int Error; }
    [StructLayout(LayoutKind.Sequential)] private struct Generic { internal nint Data, Finalizer; }
    // C long fields/arguments follow LLP64, LP64 and ILP32. Return scalar registers, not struct-return ABI.
    private static CULong CharResult(nuint value) => new(OperatingSystem.IsWindows() ? (nuint)(uint)value : value);
    private static CULong FTGetFirstChar(nint face, out uint glyph) => CharResult(FTGetFirstCharNative(face, out glyph));
    private static CULong FTGetNextChar(nint face, CULong character, out uint glyph) => CharResult(FTGetNextCharNative(face, character, out glyph));
    private static CLong FTMulFix(CLong value, CLong scale)
    {
        var result = FTMulFixNative(value, scale);
        return new(OperatingSystem.IsWindows() ? (nint)(int)result : result);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct FaceRecord
    {
        internal CLong Count, Index, Flags, StyleFlags, GlyphCount;
        internal nint FamilyName, StyleName;
        internal int FixedSizes;
        internal nint AvailableSizes;
        internal int CharMapCount;
        internal nint CharMaps;
        internal Generic Generic;
        internal CLong MinX, MinY, MaxX, MaxY;
        internal ushort UnitsPerEm;
        internal short Ascender, Descender, Height, MaxAdvanceWidth, MaxAdvanceHeight, UnderlinePosition, UnderlineThickness;
        internal nint Glyph, Size, CharMap;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct SizeMetrics
    {
        internal ushort XPpem, YPpem;
        internal CLong XScale, YScale, Ascent, Descent, Height, MaxAdvance;
    }
    [StructLayout(LayoutKind.Sequential)] private struct SizeRecord { internal nint Face; internal Generic Generic; internal SizeMetrics Metrics; }
    [StructLayout(LayoutKind.Sequential)] private struct GlyphMetrics { internal CLong Width, Height, BearingX, BearingY, Advance, VerticalBearingX, VerticalBearingY, VerticalAdvance; }
    [StructLayout(LayoutKind.Sequential)]
    private struct GlyphSlotRecord
    {
        internal nint Library, Face, Next;
        internal uint Index;
        internal Generic Generic;
        internal GlyphMetrics Metrics;
        internal CLong LinearHorizontalAdvance, LinearVerticalAdvance;
        internal FTVector Advance;
        internal uint Format;
        internal RasterBitmap Bitmap;
        internal int BitmapLeft, BitmapTop;
        internal FTOutline Outline;
    }
    [StructLayout(LayoutKind.Sequential)] private struct HBGlyphExtents { internal int XBearing, YBearing, Width, Height; }
    [StructLayout(LayoutKind.Sequential)] private struct GlyphInfo { internal uint Codepoint, Mask, Cluster, Var1, Var2; }
    [StructLayout(LayoutKind.Sequential)] private struct GlyphPosition { internal int XAdvance, YAdvance, XOffset, YOffset; internal uint Var; }

    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Init_FreeType"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTInit(out nint library);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Done_FreeType"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTDone(nint library);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_New_Memory_Face"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTNewMemoryFace(nint library, byte* data, CLong size, CLong faceIndex, out nint face);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Done_Face"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTDoneFace(nint face);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Set_Char_Size"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTSetCharSize(nint face, CLong width, CLong height, uint horizontalDPI, uint verticalDPI);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Get_Char_Index"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial uint FTGetCharIndex(nint face, CULong character);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Get_First_Char"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nuint FTGetFirstCharNative(nint face, out uint glyph);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Get_Next_Char"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nuint FTGetNextCharNative(nint face, CULong character, out uint glyph);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Face_GetCharVariantIndex"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial uint FTGetCharVariantIndex(nint face, CULong character, CULong selector);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Get_Advance"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTGetAdvance(nint face, uint glyph, int flags, out CLong advance);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Load_Glyph"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTLoadGlyph(nint face, uint glyph, int flags);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_MulFix"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint FTMulFixNative(CLong value, CLong scale);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Load_Sfnt_Table"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTLoadSFNTTable(nint face, CULong tag, CLong offset, byte* buffer, ref CULong length);
    // Pointer-sized ABI parameters also work with Mono/WASM's interpreter signature table.
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_face_create_for_tables"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint HBFaceCreate(nint callback, nint data, nint destroy);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_face_destroy"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFaceDestroy(nint face);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_face_get_glyph_count"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial uint HBFaceGetGlyphCount(nint face);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_blob_create"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint HBBlobCreate(byte* data, uint length, int mode, nint owner, nint destroy);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_blob_get_empty"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint HBBlobGetEmpty();
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_create"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint HBFontCreate(nint face);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_create_sub_font"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint HBFontCreateSubFont(nint parent);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_ot_font_set_funcs"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBOTFontSetFuncs(nint font);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_destroy"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontDestroy(nint font);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_set_scale"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontSetScale(nint font, int x, int y);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_funcs_create"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint HBFontFuncsCreate();
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_funcs_destroy"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontFuncsDestroy(nint funcs);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_set_funcs"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontSetFuncs(nint font, nint funcs, nint data, nint destroy);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_funcs_set_glyph_h_advance_func"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontFuncsSetHorizontalAdvance(nint funcs, nint callback, nint data, nint destroy);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_funcs_set_glyph_v_advance_func"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontFuncsSetVerticalAdvance(nint funcs, nint callback, nint data, nint destroy);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_funcs_set_glyph_extents_func"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontFuncsSetGlyphExtents(nint funcs, nint callback, nint data, nint destroy);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_funcs_set_glyph_v_origin_func"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontFuncsSetVerticalOrigin(nint funcs, nint callback, nint data, nint destroy);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_create"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint HBBufferCreate();
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_destroy"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBBufferDestroy(nint buffer);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_clear_contents"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBBufferClear(nint buffer);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_allocation_successful"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int HBBufferAllocationSuccessful(nint buffer);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_set_direction"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBBufferSetDirection(nint buffer, NativeTextDirection direction);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_set_script"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBBufferSetScript(nint buffer, uint script);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_set_language"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBBufferSetLanguage(nint buffer, nint language);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_set_flags"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBBufferSetFlags(nint buffer, uint flags);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_guess_segment_properties"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBBufferGuessProperties(nint buffer);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_add_utf32"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBBufferAddUTF32(nint buffer, uint* text, int length, uint itemOffset, int itemLength);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_language_from_string", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint HBLanguageFromString(string language, int length);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_shape"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBShape(nint font, nint buffer, NativeFontFeature* features, uint count);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_get_glyph_infos"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial GlyphInfo* HBBufferGetGlyphInfos(nint buffer, out uint count);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_buffer_get_glyph_positions"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial GlyphPosition* HBBufferGetGlyphPositions(nint buffer, out uint count);
}
