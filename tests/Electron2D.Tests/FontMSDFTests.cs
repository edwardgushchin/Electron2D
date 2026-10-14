using System.Diagnostics;
using Electron2D;

internal static partial class FontMSDFTests
{
    private static FontGlyph Glyph(Font font, int size = 24, int outline = 0, char character = 'A')
    { var data = font.PrimaryData!; return data.GetGlyph(data.GetGlyphIndex(character), size, outline, 1); }
    private static byte[] Pixels(FontGlyph glyph)
    {
        using var image = glyph.Texture!.GetImage()!; var source = image.GetData();
        var region = glyph.Region ?? new(0, 0, image.Width, image.Height); var width = (int)region.Size.X; var height = (int)region.Size.Y;
        var result = new byte[width * height * 4];
        for (var y = 0; y < height; y++) source.AsSpan(((int)region.Position.Y + y) * image.Width * 4 + (int)region.Position.X * 4, width * 4).CopyTo(result.AsSpan(y * width * 4));
        return result;
    }
    internal static void RunManaged()
    {
        using var empty = new FontFile(); var changes = 0; empty.Changed += _ => changes++;
        Check(!empty.MultichannelSignedDistanceField && empty.MSDFSize == 48 && empty.MSDFPixelRange == 16 && empty.GetCacheCount() == 0, "Deferred defaults.");
        empty.MultichannelSignedDistanceField = false; empty.MSDFSize = 48; empty.MSDFPixelRange = 16; Check(changes == 0, "Equal preferences are silent.");
        empty.MultichannelSignedDistanceField = true; empty.MSDFSize = 32; empty.MSDFPixelRange = 8; Check(empty.GetCacheCount() == 0 && changes == 3, "Deferred preferences do not initialize a face.");
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        var metrics = font.GetStringSize("Ag"); var previous = Glyph(font); var data = font.PrimaryData!;
        using (font.BeginRead())
        {
            font.MultichannelSignedDistanceField = true; var glyph = Glyph(font);
            Check(glyph.MSDFPixelRange == 16 && glyph.MSDFScale == .5f && !glyph.LCD && !glyph.Colored, "Executable native distance field.");
            Check(data.GetGlyph(data.GetGlyphIndex('A'), 24, 0, 1).MSDFPixelRange == 0 && previous.Texture!.GetImage() is { } oldImage && DisposeImage(oldImage), "Active old source/pixels remain valid.");
        }
        Check(font.GetStringSize("Ag").IsEqualApprox(metrics), "Shaping metrics remain independent.");
        var small = Glyph(font, 24); var large = Glyph(font, 96, 3);
        Check(ReferenceEquals(small.Texture, large.Texture) && small.Region == large.Region && large.Size.IsEqualApprox(small.Size * 4) && large.Offset.IsEqualApprox(small.Offset * 4) && large.MSDFOutline == 3, "Sizes/outlines share canonical encoded pixels.");
        var phasePosition = new Vector2(20.375f, 40.25f); var source = font.PrimaryData!;
        var phased = source.GetGlyph(source.GetGlyphIndex('A'), 24, 0, 12, phasePosition, out var position, 20);
        Check(position == phasePosition && ReferenceEquals(phased.Texture, small.Texture), "MSDF preserves placement and ignores all oversampling/phase sources.");
        var pixels = Pixels(small); Check(Enumerable.Range(0, pixels.Length / 4).Any(i => pixels[i * 4] != pixels[i * 4 + 1] || pixels[i * 4 + 1] != pixels[i * 4 + 2]), "Channels contain independent distances.");
        Check(Enumerable.Range(0, pixels.Length / 4).Any(i => pixels[i * 4 + 3] is > 0 and < 255), "Alpha contains true distance.");
        Check(Glyph(font, character: ' ').Texture == null, "Empty outlines remain advancing whitespace.");
        Check(font.GetSizeCacheList(0).Contains(new Vector2i(48, 0)) && font.GetTextureCount(0, new(24, 3)) == font.GetTextureCount(0, new(96, 0)), "Public indexed API shares the canonical size/zero-outline cache.");
        var index = font.GetGlyphIndex(24, 'A', 0); Check(font.GetGlyphSize(0, new(24, 3), index) == font.GetGlyphSize(0, new(96, 0), index), "Indexed geometry remains in source pixels.");
        using var variation = new FontVariation { BaseFont = font }; Check(Glyph(variation).MSDFPixelRange == 16, "Variation inherits generation policy.");
        font.MSDFPixelRange = 8; Check(Glyph(variation).MSDFPixelRange == 8, "Source changes retire realized variants.");
        using var copy = (FontFile)font.Duplicate(); Check(copy.MultichannelSignedDistanceField && copy.MSDFSize == 48 && copy.MSDFPixelRange == 8 && Glyph(copy).MSDFPixelRange == 8, "Duplicate preserves executable settings.");
        using var system = new SystemFont { MultichannelSignedDistanceField = true, MSDFSize = 32, MSDFPixelRange = 8 };
        Check(Glyph(system).MSDFPixelRange == 8 && Glyph(system).MSDFScale == .75f, "Selected system source consumes preferences.");
        using var systemCopy = (SystemFont)system.Duplicate(); Check(Glyph(systemCopy).MSDFPixelRange == 8, "System duplicate rematches with executable preferences.");
        using var variable = new FontFile { Data = FontTestFixtures.Variable, MultichannelSignedDistanceField = true };
        using var variableVariant = new FontVariation { BaseFont = variable }; variableVariant.SetVariationOpenType(new() { ["wght"] = 900 });
        Check(!Pixels(Glyph(variable)).AsSpan().SequenceEqual(Pixels(Glyph(variableVariant))), "Variable native outlines reach distance generation.");
        using var bitmap = FontSupportTests.Bitmap('A', 6, Colors.Cyan); bitmap.MSDFSize = 8; bitmap.MultichannelSignedDistanceField = true;
        Check(Glyph(bitmap, 8).MSDFPixelRange == 16 && Glyph(bitmap, 32, 2).Size == Glyph(bitmap, 8).Size * 4, "Authored bitmap regions can explicitly carry encoded distances at the canonical source size.");
        using var fallbackSource = new FontFile { Data = FontTestFixtures.OpenSans, MultichannelSignedDistanceField = true };
        if (fallbackSource.PrimaryData!.FindSystemFallback(0x1F600, "") is { } emoji) { var fallbackGlyph = emoji.GetGlyph(emoji.GetGlyphIndex(0x1F600), 24, 0, 1); Check(fallbackGlyph.Texture != null && fallbackGlyph.MSDFPixelRange == (emoji.MultichannelSignedDistanceField ? 16 : 0), "Owned fallback executes the applicable outline or ordinary color/bitmap role."); }
        font.SetCacheAscent(0, 24, 100); Check(font.GetCacheAscent(0, 24) == 50 && font.GetCacheAscent(0, 96) == 200, "Authored canonical metrics scale to queried sizes.");
        font.SetCacheScale(0, 24, 2); Check(font.GetCacheScale(0, 24) == 1 && font.GetCacheScale(0, 96) == 4, "Indexed canonical scale is projected to requested size.");
        using var budget = new FontFile { Data = FontTestFixtures.OpenSans, MultichannelSignedDistanceField = true, MSDFPixelRange = 16384 }; Reject<InvalidOperationException>(() => Glyph(budget)); Check(budget.GetTextureCount(0, new(48, 0)) == 0, "Native pixel-budget failure publishes no generated page.");
        font.GenerateMIPMaps = true; using (var image = Glyph(font).Texture!.GetImage()!) Check(image.HasMIPMaps, "Distance page mipmap ownership remains executable.");
        Storage(copy, systemCopy);
        font.Changed += Fail; Reject<Marker>(() => font.MSDFPixelRange = 4); font.Changed -= Fail; Check(font.MSDFPixelRange == 4 && Glyph(font).MSDFPixelRange == 4, "Observer failure keeps committed policy.");
        font.MSDFSize = 0; Check(font.MSDFSize == 0, "Preferences retain raw integer values."); Reject<InvalidOperationException>(() => Glyph(font));
        font.MSDFSize = 48; font.MSDFPixelRange = int.MaxValue; Reject<InvalidOperationException>(() => Glyph(font));
        font.ResetState(); system.ResetState(); Check(!font.MultichannelSignedDistanceField && font.MSDFSize == 48 && font.MSDFPixelRange == 16 && !system.MultichannelSignedDistanceField && system.MSDFSize == 48 && system.MSDFPixelRange == 16, "Reset restores the complete family.");
        empty.Dispose(); Reject<ObjectDisposedException>(() => empty.MSDFSize = 48);
        Console.WriteLine("MSDF native channels, source reuse, geometry, publication, metrics, variations, indexed cache and storage passed.");
    }
    private static bool DisposeImage(Image image) { image.Dispose(); return true; }
    private static void Fail(Resource _) => throw new Marker();
    private sealed class Marker : Exception { }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
