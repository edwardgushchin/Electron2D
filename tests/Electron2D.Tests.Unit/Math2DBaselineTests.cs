using System.Globalization;
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class Math2DBaselineTests
{
    [Fact]
    public void Vector2SupportsGodotLikeFloatingPointOperations()
    {
        var value = new Electron2D.Vector2(3f, 4f);

        Assert.Equal(new Electron2D.Vector2(4f, 6f), value + new Electron2D.Vector2(1f, 2f));
        Assert.Equal(new Electron2D.Vector2(2f, 2f), value - new Electron2D.Vector2(1f, 2f));
        Assert.Equal(new Electron2D.Vector2(-3f, -4f), -value);
        Assert.Equal(new Electron2D.Vector2(6f, 8f), value * 2f);
        Assert.Equal(new Electron2D.Vector2(6f, 8f), 2f * value);
        Assert.Equal(new Electron2D.Vector2(1.5f, 2f), value / 2f);
        Assert.Equal(5f, value.Length());
        Assert.Equal(25f, value.LengthSquared());
        Assert.Equal(11f, value.Dot(new Electron2D.Vector2(1f, 2f)));
        Assert.Equal(-2f, value.Cross(new Electron2D.Vector2(2f, 2f)));
        Assert.Equal(5f, value.DistanceTo(Electron2D.Vector2.Zero));
        Assert.Equal(new Electron2D.Vector2(0.6f, 0.8f), value.Normalized());
        Assert.True(value.Normalized().IsNormalized());
        Assert.Equal(Electron2D.Vector2.Zero, Electron2D.Vector2.Zero.Normalized());
        Assert.True(new Electron2D.Vector2(1f, 1f).IsEqualApprox(new Electron2D.Vector2(1f + (Electron2D.Mathf.Epsilon * 0.5f), 1f)));
        Assert.True(Electron2D.Vector2.Zero.IsZeroApprox());
        Assert.Equal(new Electron2D.Vector2(0.6f, 0.8f), Electron2D.Vector2.Zero.DirectionTo(value));
        Assert.Equal(new Electron2D.Vector2(1.5f, 2f), Electron2D.Vector2.Zero.Lerp(value, 0.5f));
        Assert.True(Electron2D.Vector2.Right.Rotated(Electron2D.Mathf.Pi * 0.5f).IsEqualApprox(Electron2D.Vector2.Down));
        Assert.Equal(new Electron2D.Vector2(1f, -1f), new Electron2D.Vector2(3f, -4f).Sign());
        Assert.Equal(new Electron2D.Vector2(2f, 3f), new Electron2D.Vector2(1.6f, 2.5f).Round());
        Assert.Equal(new Electron2D.Vector2(2f, 3f), new Electron2D.Vector2(1f, 5f).Clamp(new Electron2D.Vector2(2f, 0f), new Electron2D.Vector2(4f, 3f)));
    }

    [Fact]
    public void Vector2ISupportsExactIntegerOperationsAndConversions()
    {
        var value = new Electron2D.Vector2I(6, 4);

        Assert.Equal(new Electron2D.Vector2I(7, 6), value + new Electron2D.Vector2I(1, 2));
        Assert.Equal(new Electron2D.Vector2I(5, 2), value - new Electron2D.Vector2I(1, 2));
        Assert.Equal(new Electron2D.Vector2I(-6, -4), -value);
        Assert.Equal(new Electron2D.Vector2I(12, 8), value * 2);
        Assert.Equal(new Electron2D.Vector2I(3, 2), value / 2);
        Assert.Equal(new Electron2D.Vector2I(0, 1), value % new Electron2D.Vector2I(3, 3));
        Assert.Equal(52, value.LengthSquared());
        Assert.Equal(MathF.Sqrt(52f), value.Length());
        Assert.Equal(1.5f, value.Aspect());
        Assert.Equal(new Electron2D.Vector2I(1, -1), new Electron2D.Vector2I(9, -2).Sign());
        Assert.Equal(new Electron2D.Vector2I(2, 3), new Electron2D.Vector2I(1, 5).Clamp(new Electron2D.Vector2I(2, 0), new Electron2D.Vector2I(4, 3)));

        Electron2D.Vector2 asFloat = value;
        Assert.Equal(new Electron2D.Vector2(6f, 4f), asFloat);
        Assert.Equal(new Electron2D.Vector2I(3, -2), (Electron2D.Vector2I)new Electron2D.Vector2(3.8f, -2.2f));
    }

    [Fact]
    public void Rect2SupportsAxisAlignedGeometryOperations()
    {
        var rect = new Electron2D.Rect2(new Electron2D.Vector2(2f, 3f), new Electron2D.Vector2(4f, 5f));

        Assert.Equal(new Electron2D.Vector2(6f, 8f), rect.End);
        Assert.Equal(20f, rect.GetArea());
        Assert.Equal(new Electron2D.Vector2(4f, 5.5f), rect.GetCenter());
        Assert.True(rect.HasArea());
        Assert.True(rect.HasPoint(new Electron2D.Vector2(2f, 3f)));
        Assert.True(rect.HasPoint(new Electron2D.Vector2(5.999f, 7.999f)));
        Assert.False(rect.HasPoint(new Electron2D.Vector2(6f, 8f)));

        var other = new Electron2D.Rect2(4f, 5f, 8f, 8f);
        Assert.Equal(new Electron2D.Rect2(4f, 5f, 2f, 3f), rect.Intersection(other));
        Assert.True(rect.Intersects(other));
        Assert.False(rect.Encloses(other));
        Assert.Equal(new Electron2D.Rect2(2f, 3f, 10f, 10f), rect.Merge(other));
        Assert.Equal(new Electron2D.Rect2(1f, 2f, 6f, 7f), rect.Grow(1f));
        Assert.Equal(new Electron2D.Rect2(1f, 2f, 5f, 6f), new Electron2D.Rect2(6f, 8f, -5f, -6f).Abs());

        rect.End = new Electron2D.Vector2(10f, 11f);
        Assert.Equal(new Electron2D.Vector2(8f, 8f), rect.Size);
    }

    [Fact]
    public void Rect2ISupportsIntegerGeometryOperations()
    {
        var rect = new Electron2D.Rect2I(2, 3, 4, 5);

        Assert.Equal(new Electron2D.Vector2I(6, 8), rect.End);
        Assert.Equal(20, rect.GetArea());
        Assert.Equal(new Electron2D.Vector2I(4, 5), rect.GetCenter());
        Assert.True(rect.HasPoint(new Electron2D.Vector2I(2, 3)));
        Assert.False(rect.HasPoint(new Electron2D.Vector2I(6, 8)));
        Assert.Equal(new Electron2D.Rect2I(4, 5, 2, 3), rect.Intersection(new Electron2D.Rect2I(4, 5, 8, 8)));
        Assert.Equal(new Electron2D.Rect2I(1, 2, 5, 6), new Electron2D.Rect2I(6, 8, -5, -6).Abs());
    }

    [Fact]
    public void Transform2DSupportsPointTransformsAndInverse()
    {
        var identity = Electron2D.Transform2D.Identity;
        Assert.Equal(new Electron2D.Vector2(2f, 3f), identity.Xform(new Electron2D.Vector2(2f, 3f)));

        var translated = identity.Translated(new Electron2D.Vector2(10f, 20f));
        Assert.Equal(new Electron2D.Vector2(12f, 23f), translated.Xform(new Electron2D.Vector2(2f, 3f)));

        var rotated = new Electron2D.Transform2D(Electron2D.Mathf.Pi * 0.5f, Electron2D.Vector2.Zero);
        Assert.True(rotated.Xform(Electron2D.Vector2.Right).IsEqualApprox(Electron2D.Vector2.Down));

        var scaled = identity.Scaled(new Electron2D.Vector2(2f, 3f));
        Assert.Equal(new Electron2D.Vector2(4f, 9f), scaled.Xform(new Electron2D.Vector2(2f, 3f)));
        Assert.True(translated.AffineInverse().Xform(translated.Xform(new Electron2D.Vector2(7f, 8f))).IsEqualApprox(new Electron2D.Vector2(7f, 8f)));

        var combined = translated * scaled;
        Assert.Equal(translated.Xform(scaled.Xform(new Electron2D.Vector2(2f, 3f))), combined.Xform(new Electron2D.Vector2(2f, 3f)));
    }

    [Fact]
    public void ColorAndMathfSupportFormattingAndEdgeCases()
    {
        var color = new Electron2D.Color(0.25f, 0.5f, 0.75f, 1f);

        Assert.Equal("4080bfff", color.ToHtml());
        Assert.Equal("4080bf", color.ToHtml(includeAlpha: false));
        Assert.Equal(new Electron2D.Color(64f / 255f, 128f / 255f, 191f / 255f, 1f), Electron2D.Color.FromHtml("#4080bfff"));
        Assert.Equal(new Electron2D.Color(0.5f, 0.5f, 0.5f, 1f), Electron2D.Color.Black.Lerp(Electron2D.Color.White, 0.5f));
        Assert.Equal(new Electron2D.Color(0.55f, 0.6f, 0.65f, 1f), new Electron2D.Color(0.1f, 0.2f, 0.3f, 1f).Lightened(0.5f));
        Assert.Equal(new Electron2D.Color(0.05f, 0.1f, 0.15f, 1f), new Electron2D.Color(0.1f, 0.2f, 0.3f, 1f).Darkened(0.5f));

        Assert.Equal(5f, Electron2D.Mathf.Clamp(10f, 0f, 5f));
        Assert.Equal(2.5f, Electron2D.Mathf.Lerp(0f, 10f, 0.25f));
        Assert.Equal(0.25f, Electron2D.Mathf.InverseLerp(0f, 10f, 2.5f));
        Assert.Equal(5f, Electron2D.Mathf.MoveToward(0f, 10f, 5f));
        Assert.Equal(10f, Electron2D.Mathf.MoveToward(0f, 10f, 20f));
        Assert.Equal(Electron2D.Mathf.Pi, Electron2D.Mathf.DegToRad(180f));
        Assert.Equal(180f, Electron2D.Mathf.RadToDeg(Electron2D.Mathf.Pi));
        Assert.Equal(2, Electron2D.Mathf.PosMod(-1, 3));
        Assert.Equal(0.75f, Electron2D.Mathf.Snapped(0.8f, 0.25f));
        Assert.Throws<FormatException>(() => Electron2D.Color.FromHtml("bad"));
    }

    [Fact]
    public void MathTypesFormatWithInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

            Assert.Equal("(1.5, 2.25)", new Electron2D.Vector2(1.5f, 2.25f).ToString());
            Assert.Equal("(1, 2)", new Electron2D.Vector2I(1, 2).ToString());
            Assert.Equal("[P: (1.5, 2.25), S: (3.5, 4.75)]", new Electron2D.Rect2(1.5f, 2.25f, 3.5f, 4.75f).ToString());
            Assert.Equal("(0.25, 0.5, 0.75, 1)", new Electron2D.Color(0.25f, 0.5f, 0.75f, 1f).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
