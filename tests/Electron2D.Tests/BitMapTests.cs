using Electron2D;

internal static class BitMapTests
{
    internal static void Run()
    {
        using var mask = new BitMap();
        Check(mask.GetSize() == new Vector2i(0, 0) && mask.GetTrueBitCount() == 0, "Default mask must be empty.");
        Reject<ArgumentOutOfRangeException>(() => mask.Create(new Vector2i(0, 2)));
        mask.Create(new Vector2i(4, 3));
        mask.SetBitRect(new RectI(-1, -1, 3, 3), true);
        Check(mask.GetTrueBitCount() == 4 && mask.GetBitv(new Vector2i(1, 1)) && !mask.GetBit(2, 2), "Clipped rectangular writes must preserve bit order.");
        mask.SetBitv(new Vector2i(1, 1), false);
        Check(mask.GetTrueBitCount() == 3, "Vector writes must clear the chosen bit.");
        Reject<ArgumentOutOfRangeException>(() => mask.GetBit(4, 0));
        Reject<ArgumentOutOfRangeException>(() => mask.SetBit(-1, 0, true));
        using (var bitmapImage = mask.ConvertToImage())
            Check(bitmapImage.PixelFormat == Image.Format.L8 && bitmapImage.GetPixel(0, 0).R == 1f && bitmapImage.GetPixel(3, 2).R == 0f,
                "Mask pixels must map to L8 white and black.");

        using (var duplicate = (BitMap)mask.Duplicate())
        {
            duplicate.SetBit(3, 2, true);
            Check(!mask.GetBit(3, 2) && duplicate.GetTrueBitCount() == 4, "Resource duplication must own a separate bit buffer.");
        }
        using (var derived = new DerivedBitMap())
        {
            derived.Create(new Vector2i(1, 1));
            Reject<NotSupportedException>(() => derived.Duplicate());
        }

        using (var alpha = Image.CreateFromData(3, 1, false, Image.Format.Rgba8,
            [10, 20, 30, 25, 10, 20, 30, 26, 10, 20, 30, 255]))
        {
            mask.CreateFromImageAlpha(alpha, 0.1f);
            Check(mask.GetSize() == new Vector2i(3, 1) && mask.GetTrueBitCount() == 2 && !mask.GetBit(0, 0),
                "Alpha equality must remain transparent.");
            Check(alpha.PixelFormat == Image.Format.Rgba8, "Alpha conversion must not mutate the source image.");
        }

        mask.Create(new Vector2i(5, 5));
        mask.SetBit(2, 2, true);
        mask.GrowMask(1, new RectI(0, 0, 5, 5));
        Check(mask.GetTrueBitCount() == 5 && mask.GetBit(2, 1) && !mask.GetBit(1, 1), "Positive radius must use a circular neighborhood.");
        mask.GrowMask(-1, new RectI(0, 0, 5, 5));
        Check(mask.GetTrueBitCount() == 1 && mask.GetBit(2, 2), "Erosion must use the original snapshot.");
        mask.SetBitRect(new RectI(0, 0, 5, 5), true);
        mask.GrowMask(-1, new RectI(1, 1, 3, 3));
        Check(mask.GetBit(0, 0) && !mask.GetBit(1, 1) && mask.GetBit(2, 2), "Erosion must preserve outside-region pixels and treat its border as unset.");

        mask.Create(new Vector2i(4, 4));
        mask.SetBitRect(new RectI(1, 1, 2, 2), true);
        var polygons = mask.OpaqueToPolygons(new RectI(0, 0, 4, 4), 0);
        Check(polygons.Length == 1 && polygons[0].Length == 4 &&
            polygons[0].Contains(new Vector2(1, 1)) && polygons[0].Contains(new Vector2(3, 3)),
            "A two-by-two island must trace one four-corner contour.");
        var clipped = mask.OpaqueToPolygons(new RectI(2, 2, 2, 2), 0);
        Check(clipped.Length == 1 && clipped[0].Contains(new Vector2(0, 0)), "Clipped contour coordinates must be relative to the inspected rectangle.");
        Check(mask.OpaqueToPolygons(new RectI(10, 10, 2, 2)).Length == 0, "Disjoint regions must return no polygons.");
        Reject<ArgumentOutOfRangeException>(() => mask.OpaqueToPolygons(new RectI(0, 0, 4, 4), float.NaN));

        mask.Create(new Vector2i(8, 8));
        mask.SetBit(1, 1, true);
        mask.SetBit(6, 6, true);
        Check(mask.OpaqueToPolygons(new RectI(0, 0, 8, 8), 0).Length == 2, "Disconnected islands must produce separate polygons.");
        mask.Resize(new Vector2i(16, 16));
        Check(mask.GetTrueBitCount() == 8 && mask.GetBit(2, 2) && !mask.GetBit(4, 4), "Nearest-neighbor upscaling must preserve independent islands.");
        mask.Resize(new Vector2i(8, 8));
        Check(mask.GetTrueBitCount() == 2 && mask.GetBit(1, 1), "Nearest-neighbor downscaling must recover sampled pixels.");
        Reject<ArgumentOutOfRangeException>(() => mask.Resize(new Vector2i(0, 4)));

        mask.Create(new Vector2i(4, 4));
        mask.SetBit(1, 1, true);
        mask.SetBit(2, 2, true);
        var diagonal = mask.OpaqueToPolygons(new RectI(0, 0, 4, 4), 0);
        Check(diagonal.Length == 2 && diagonal.All(polygon => polygon.Length >= 3),
            "Diagonal contact must split at the ambiguous marching-squares crossing.");

        mask.Create(new Vector2i(8, 8));
        mask.SetBitRect(new RectI(1, 1, 6, 6), true);
        mask.SetBitRect(new RectI(3, 3, 2, 2), false);
        var ring = mask.OpaqueToPolygons(new RectI(0, 0, 8, 8), 2);
        Check(ring.Length > 0 && ring.All(polygon => polygon.Length >= 3),
            "A ring and its reduced contour must terminate without degenerate polygons.");

        for (var pattern = 0; pattern < 512; pattern++)
        {
            mask.Create(new Vector2i(3, 3));
            for (var bit = 0; bit < 9; bit++)
                if ((pattern & (1 << bit)) != 0) mask.SetBit(bit % 3, bit / 3, true);
            var contours = mask.OpaqueToPolygons(new RectI(0, 0, 3, 3), 0);
            Check(contours.All(polygon => polygon.Length >= 3), "Every small-mask topology must return only valid contours.");
        }

        using (var observed = new BitMap())
        {
            observed.Create(new Vector2i(1, 1));
            observed.Changed += _ =>
            {
                Check(observed.GetBit(0, 0), "A change handler must see committed state without a held mask lock.");
                throw new ApplicationException("observer");
            };
            Reject<ApplicationException>(() => observed.SetBit(0, 0, true));
            Check(observed.GetBit(0, 0), "An observer failure must not revert committed bits.");
        }

        mask.Dispose();
        Reject<ObjectDisposedException>(() => mask.GetSize());
        Console.WriteLine("BitMap storage, image alpha, morphology, contours, copy and lifetime checks passed.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    private sealed class DerivedBitMap : BitMap { }
}
