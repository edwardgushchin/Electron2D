using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class NativeFontPrecision
{
    private float _embolden;
    private Transform _variationTransform = Transform.Identity;
    internal Dictionary<uint, FontVariationAxis> VariationAxes { get; private set; } = [];
    internal Color[][] Palettes { get; private set; } = [];
    internal string[] PaletteNames { get; private set; } = [];
    private void ReadVariationMetadata()
    {
        var table = GetSFNTTable(0x66766172); // fvar: public SFNT table access.
        if (table.Length != 0 && table.Length < 16) throw new InvalidDataException("Truncated font variation axes.");
        if (table.Length >= 16)
        {
            var offset = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(4)); var count = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(8)); var size = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(10));
            if (size < 20 || (long)offset + (long)count * size > table.Length) throw new InvalidDataException("Malformed font variation axes.");
            for (var i = 0; i < count; i++) { var row = table.AsSpan(offset + i * size, size); var tag = BinaryPrimitives.ReadUInt32BigEndian(row); var min = BinaryPrimitives.ReadInt32BigEndian(row[4..]) / 65536f; var def = BinaryPrimitives.ReadInt32BigEndian(row[8..]) / 65536f; var max = BinaryPrimitives.ReadInt32BigEndian(row[12..]) / 65536f; if (min > def || def > max || !VariationAxes.TryAdd(tag, new(min, max, def))) throw new InvalidDataException("Invalid font variation axis."); }
        }
        var error = FTPaletteDataGet(_face, out var data);
        if (error != 0) { if ((FaceFlags & (1 << 14)) != 0 && GetSFNTTable(0x4350414c).Length > 0) CheckFT(error); return; }
        if (data.Count == 0) return;
        Palettes = new Color[data.Count][]; PaletteNames = new string[data.Count];
        for (ushort i = 0; i < data.Count; i++)
        {
            CheckFT(FTPaletteSelect(_face, i, out var colors)); var result = new Color[data.Entries];
            if (colors == null && result.Length > 0) throw new InvalidDataException("Missing font palette colors.");
            for (var j = 0; j < result.Length; j++) result[j] = new(colors[j].Red / 255f, colors[j].Green / 255f, colors[j].Blue / 255f, colors[j].Alpha / 255f);
            Palettes[i] = result; PaletteNames[i] = data.NameIDs == null || data.NameIDs[i] == ushort.MaxValue ? "" : SFNTName(data.NameIDs[i]);
        }
        CheckFT(FTPaletteSelect(_face, 0, out _));
    }
    private string SFNTName(ushort id)
    {
        var count = FTGetSFNTNameCount(_face);
        for (uint i = 0; i < count; i++) { CheckFT(FTGetSFNTName(_face, i, out var name)); if (name.NameID != id || name.Text == null || name.Length > 65536) continue; var bytes = new ReadOnlySpan<byte>(name.Text, (int)name.Length); if (name.PlatformID is 0 or 3 && bytes.Length % 2 == 0) return System.Text.Encoding.BigEndianUnicode.GetString(bytes); if (name.PlatformID == 1) return System.Text.Encoding.Latin1.GetString(bytes); }
        return "";
    }
    internal void ConfigureInstance(FontInstance instance)
    {
        EnsureOwner(); _embolden = instance.Embolden; _variationTransform = instance.Transform;
        if (VariationAxes.Count > 0)
        {
            var coordinates = new CLong[VariationAxes.Count]; var variations = new HBVariation[VariationAxes.Count]; var index = 0;
            foreach (var pair in VariationAxes) { var value = instance.Coordinates.TryGetValue(pair.Key, out var requested) ? Math.Clamp(requested, pair.Value.Minimum, pair.Value.Maximum) : pair.Value.Default; coordinates[index] = new((int)Math.Clamp(Math.Round(value * 65536d), int.MinValue, int.MaxValue)); variations[index++] = new(pair.Key, value); }
            fixed (CLong* values = coordinates) CheckFT(FTSetVarDesignCoordinates(_face, (uint)coordinates.Length, values));
            fixed (HBVariation* values = variations) { HBFontSetVariations(_parentFont, values, (uint)variations.Length); HBFontSetVariations(_font, values, (uint)variations.Length); }
            _size = 0; SetSize(16);
        }
        if (Palettes.Length > 0)
        {
            var index = (ushort)Math.Clamp(instance.PaletteIndex, 0, Palettes.Length - 1); CheckFT(FTPaletteSelect(_face, index, out var colors));
            for (var i = 0; i < Math.Min(instance.CustomColors.Length, Palettes[index].Length); i++) { var color = instance.CustomColors[i]; if (color == Colors.Transparent) continue; colors[i] = new() { Red = Byte(color.R), Green = Byte(color.G), Blue = Byte(color.B), Alpha = Byte(color.A) }; }
        }
    }
    private static byte Byte(float value) => (byte)MathF.Round(Math.Clamp(value, 0, 1) * 255);
    private void ApplyOutlineVariation(GlyphSlotRecord* slot)
    {
        if (slot->Format != 0x6f75746c) return; // Outline format.
        if (_embolden != 0)
        {
            var strength = _embolden * _size * 4d;
            if (strength < int.MinValue || strength > int.MaxValue) throw new InvalidOperationException("Outline strength exceeds the portable native fixed range.");
            CheckFT(FTOutlineEmbolden(&slot->Outline, new CLong((int)strength)));
        }
        if (_variationTransform.X != Vector2.Right || _variationTransform.Y != Vector2.Down)
        {
            var matrix = new FTMatrix { XX = Fixed(_variationTransform.X.X), XY = Fixed(_variationTransform.X.Y), YX = Fixed(_variationTransform.Y.X), YY = Fixed(_variationTransform.Y.Y) };
            FTOutlineTransform(&slot->Outline, &matrix);
        }
        FTOutlineGetCBox(&slot->Outline, out var bounds);
        if ((bounds.XMax.Value - (double)bounds.XMin.Value) / 64 > 16384 || (bounds.YMax.Value - (double)bounds.YMin.Value) / 64 > 16384)
            throw new InvalidOperationException("Transformed font raster dimensions exceed the canvas texture range.");
    }
    private static CLong Fixed(float value) => new(checked((int)(value * 65536d)));
    [StructLayout(LayoutKind.Sequential)] private struct FTBBox { internal CLong XMin, YMin, XMax, YMax; }
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Outline_Get_CBox"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FTOutlineGetCBox(FTOutline* outline, out FTBBox bounds);
    [StructLayout(LayoutKind.Sequential)] private struct FTColor { internal byte Blue, Green, Red, Alpha; }
    [StructLayout(LayoutKind.Sequential)] private struct FTPaletteData { internal ushort Count; internal ushort* NameIDs; internal ushort* Flags; internal ushort Entries; internal ushort* EntryNameIDs; }
    [StructLayout(LayoutKind.Sequential)] private struct FTSFNTName { internal ushort PlatformID, EncodingID, LanguageID, NameID; internal byte* Text; internal uint Length; }
    [StructLayout(LayoutKind.Sequential)] private readonly struct HBVariation(uint tag, float value) { internal readonly uint Tag = tag; internal readonly float Value = value; }
    [StructLayout(LayoutKind.Sequential)] private struct FTOutline { internal short Contours, Points; internal FTVector* Vectors; internal byte* Tags; internal short* ContourEnds; internal int Flags; }
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Set_Var_Design_Coordinates"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTSetVarDesignCoordinates(nint face, uint count, CLong* values);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Palette_Data_Get"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTPaletteDataGet(nint face, out FTPaletteData data);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Palette_Select"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTPaletteSelect(nint face, ushort index, out FTColor* colors);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Get_Sfnt_Name_Count"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial uint FTGetSFNTNameCount(nint face);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Get_Sfnt_Name"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTGetSFNTName(nint face, uint index, out FTSFNTName name);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Outline_Embolden"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FTOutlineEmbolden(FTOutline* outline, CLong strength);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "FT_Outline_Transform"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FTOutlineTransform(FTOutline* outline, FTMatrix* matrix);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_font_set_variations"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void HBFontSetVariations(nint font, HBVariation* values, uint count);
}
