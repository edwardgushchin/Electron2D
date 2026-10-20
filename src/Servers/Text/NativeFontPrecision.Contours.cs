namespace Electron2D;

internal sealed unsafe partial class NativeFontPrecision
{
    internal FontGlyphContours GetGlyphContours(uint glyph, bool forceAutohinter)
    {
        EnsureOwner();
        if (_bitmap != null || (FaceFlags & 1) == 0) return new([], [], false);
        glyph &= 0xFFFFFF;
        if (glyph >= GlyphCount) throw new ArgumentOutOfRangeException(nameof(glyph));
        FTSetTransform(_face, null, null); CheckFT(FTLoadGlyph(_face, glyph, 8 | (forceAutohinter ? 1 << 5 : 0)));
        var slot = (GlyphSlotRecord*)((FaceRecord*)_face)->Glyph;
        if (slot->Format != 0x6F75746C || slot->Outline.Points == 0) return new([], [], false);
        ApplyOutlineVariation(slot);
        var outline = slot->Outline;
        if (outline.Points < 0 || outline.Contours < 0 || outline.Vectors == null || outline.Tags == null || outline.ContourEnds == null)
            throw new InvalidDataException("The native glyph outline is invalid.");
        var points = new FontContourPoint[outline.Points]; var contours = new int[outline.Contours];
        for (var i = 0; i < points.Length; i++)
        {
            var tag = (TextServer.ContourPointTag)(outline.Tags[i] & 3);
            if (!Enum.IsDefined(tag)) throw new InvalidDataException("The native glyph contour tag is invalid.");
            points[i] = new(new((float)(outline.Vectors[i].X.Value / 60d), (float)(-outline.Vectors[i].Y.Value / 60d)), tag);
        }
        var last = -1;
        for (var i = 0; i < contours.Length; i++)
        { var end = outline.ContourEnds[i]; if (end <= last || end >= points.Length) throw new InvalidDataException("The native contour range is invalid."); contours[i] = last = end; }
        if (last != points.Length - 1) throw new InvalidDataException("The native outline has unowned points.");
        return new(points, contours, FTOutlineGetOrientation(&slot->Outline) == 1);
    }
}
