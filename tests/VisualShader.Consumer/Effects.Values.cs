using Electron2D;

internal static partial class GraphChecks
{
    private static readonly Vector3[] EffectColors =
    [
        Vector3.Zero, Vector3.One, new(.2f,.7f,.4f), new(.75f,.2f,.35f), new(.25f,.5f,.75f), new(.5f,.5f,.5f),
        new(-.25f,.5f,1.5f), new(-.4f,-.2f,-.3f), new(.0031307f,.0031308f,.0031309f), new(.04044f,.04045f,.04046f),
        new(.99f,1,1.01f), new(1e-12f,2e-12f,3e-12f), new(1,.25f,.5f), new(-.25f,.8f,.7f),
        new(MathF.BitDecrement(.5f),.5f,MathF.BitIncrement(.5f)), new(1,0,0), new(0,1,0), new(0,0,1)
    ];
    private static readonly (Vector3 A, Vector3 B)[] EffectBlends =
    [
        (new(.2f,.5f,.8f),new(.7f,.3f,.4f)), (Vector3.Zero,new(.25f,.5f,.75f)),
        (Vector3.One,new(.25f,.5f,.75f)), (new(-.25f,.75f,1.5f),new(.5f,.25f,.75f)),
        (new(MathF.BitDecrement(.5f),.5f,MathF.BitIncrement(.5f)),new(.1f,.9f,-.25f)),
        (new(.1f,.3f,.7f),new(-.25f,1.25f,-.5f)), (new(.8f,.3f,.7f),new(.25f,.75f,.5f))
    ];
    private static readonly (Vector2 UV, Vector2 Scale, Vector2 Pivot)[] EffectUVs =
    [
        (new(.2f,.7f),Vector2.One,Vector2.Zero), (new(.2f,.7f),new(2,.5f),new(.25f,-.125f)),
        (new(-.25f,1.25f),new(-2,0),new(.5f,.5f)), (new(1.5f,-2),new(.5f,-1),new(-.25f,.75f)),
        (Vector2.Zero,Vector2.Zero,new(.5f,.5f)), (new(.75f,.25f),new(1.5f,.25f),new(.2f,.8f)),
        (new(.25f,.75f),new(-1,2),new(-.5f,.125f))
    ];
    private static readonly (Vector2 UV, Vector2 Center, float Zoom, float Repeat)[] EffectPolars =
    [
        (new(.75f,.75f),new(.5f,.5f),1,1), (new(.25f,.75f),new(.5f,.5f),1,1),
        (new(.25f,.25f),new(.5f,.5f),1,1), (new(.75f,.25f),new(.5f,.5f),1,1),
        (new(1.25f,-.5f),new(.2f,.8f),2,-3), (new(-.25f,.75f),Vector2.Zero,-1,.5f),
        (new(.2f,.7f),new(.5f,.5f),0,0), (new(.8f,.1f),new(.4f,.2f),.25f,2)
    ];
    private static Vector3 EffectColor(int function, Vector3 c)
    {
        switch (function)
        {
            case 0: var maximum = MathF.Max(c.X, MathF.Max(c.Y, c.Z)); return new(maximum, maximum, maximum);
            case 1:
                var hue = (c.X - MathF.Floor(c.X)) * 6; var index = (int)MathF.Floor(hue); var fraction = hue - index;
                var low = c.Z * (1 - c.Y); var down = c.Z * (1 - fraction * c.Y); var up = c.Z * (1 - (1 - fraction) * c.Y);
                return index switch { 0 => new(c.Z, up, low), 1 => new(down, c.Z, low), 2 => new(low, c.Z, up), 3 => new(low, down, c.Z), 4 => new(up, low, c.Z), _ => new(c.Z, low, down) };
            case 2:
                var value = MathF.Max(c.X, MathF.Max(c.Y, c.Z)); var chroma = value - MathF.Min(c.X, MathF.Min(c.Y, c.Z)); var denominator = 6 * chroma + 1e-10f;
                var angle = value == c.X ? c.Y >= c.Z ? (c.Y - c.Z) / denominator : 1 - (c.Z - c.Y) / denominator
                    : value == c.Y ? 1f / 3 + (c.Z - c.X) / denominator : 2f / 3 + (c.X - c.Y) / denominator;
                return new(angle, chroma / (value + 1e-10f), value);
            case 3: return new(.393f * c.X + .769f * c.Y + .189f * c.Z, .349f * c.X + .686f * c.Y + .168f * c.Z, .272f * c.X + .534f * c.Y + .131f * c.Z);
            default:
                float Channel(float x) => function == 4 ? x < .0031308f ? 12.92f * x : 1.055f * MathF.Pow(x, 1f / 2.4f) - .055f
                    : x < .04045f ? x / 12.92f : MathF.Pow((x + .055f) / 1.055f, 2.4f);
                return new(Channel(c.X), Channel(c.Y), Channel(c.Z));
        }
    }
    private static Vector3 EffectBlend(int operation, Vector3 a, Vector3 b)
    {
        float Channel(float x, float y) => operation switch
        {
            0 => 1 - (1 - x) * (1 - y),
            1 => MathF.Abs(x - y),
            2 => MathF.Min(x, y),
            3 => MathF.Max(x, y),
            4 => x < .5f ? 2 * x * y : 1 - 2 * (1 - y) * (1 - x),
            5 => x / (1 - y),
            6 => 1 - (1 - x) / y,
            7 => x < .5f ? x * (y + .5f) : 1 - (1 - x) * (1.5f - y),
            _ => x < .5f ? 2 * x * y : 1 - 2 * (1 - x) * (1 - y)
        };
        return new(Channel(a.X, b.X), Channel(a.Y, b.Y), Channel(a.Z, b.Z));
    }
    private static Vector2 EffectCoordinates(int function, Vector2 uv, Vector2 scale, Vector2 pivot) => function == 0
        ? new(uv.X + pivot.X * scale.X, uv.Y + pivot.Y * scale.Y)
        : new((uv.X - pivot.X) * scale.X + pivot.X, (uv.Y - pivot.Y) * scale.Y + pivot.Y);
    private static Vector2 EffectPolar(Vector2 uv, Vector2 center, float zoom, float repeat)
    {
        var x = (double)uv.X - center.X; var y = (double)uv.Y - center.Y;
        var radius = Math.Sqrt(x * x + y * y) * 2 * zoom; var angle = Math.Atan2(y, x) / (2 * Math.PI) * repeat;
        return new((float)(radius - Math.Floor(radius)), (float)(angle - Math.Floor(angle)));
    }
    private static Vector3 EffectTexture(Vector2 uv) => new(Math.Clamp((int)MathF.Floor(uv.X * 4), 0, 3) * 64f / 255,
        Math.Clamp((int)MathF.Floor(uv.Y * 4), 0, 3) * 64f / 255, 128f / 255);
    private static Vector3 EffectVertexPixel(Func<Vector2, Vector3> evaluate)
    {
        const float weight = 8.5f / 16; var first = evaluate(Vector2.Zero); var last = evaluate(Vector2.One);
        return new(first.X + (last.X - first.X) * weight, first.Y + (last.Y - first.Y) * weight, first.Z + (last.Z - first.Z) * weight);
    }
    private static Color EffectExpected(string name, bool hdr, int state = 0)
    {
        var p = name.Split('-'); var stage = int.Parse(p[^1]); Vector3 value;
        if (p[1] == "color") value = EffectColor(int.Parse(p[2]), EffectColors[int.Parse(p[3])]);
        else if (p[1] == "blend") { var c = EffectBlends[int.Parse(p[3])]; value = EffectBlend(int.Parse(p[2]), c.A, c.B); }
        else if (p[1] == "uv")
        {
            var c = EffectUVs[int.Parse(p[3])]; var result = EffectCoordinates(int.Parse(p[2]), c.UV, c.Scale, c.Pivot); value = new(result.X, result.Y, 0);
        }
        else if (p[1] == "polar") { var c = EffectPolars[int.Parse(p[2])]; var result = EffectPolar(c.UV, c.Center, c.Zoom, c.Repeat); value = new(result.X, result.Y, 0); }
        else if (p[1] == "implicit")
        {
            var function = int.Parse(p[2]); Vector3 At(Vector2 uv)
            {
                var result = function < 2 ? EffectCoordinates(function, uv, new(1.5f, -.5f), new(.125f, .25f)) : EffectPolar(uv, new(.5f, .5f), 1, 1);
                return new(result.X, result.Y, 0);
            }
            value = stage == 0 ? EffectVertexPixel(At) : At(new(8.5f / 16, 8.5f / 16));
        }
        else if (p[1] == "absence") value = new(2 * (8.5f / 16), 2 * (8.5f / 16), 0);
        else if (p[1] == "helper")
            value = stage == 0 ? EffectVertexPixel(uv => EffectColor(2, EffectTexture(uv))) : EffectColor(2, EffectTexture(new(8.5f / 16, 8.5f / 16)));
        else
        {
            var operation = int.Parse(p[2]); var shift = state == 0 ? new Vector2(.0625f, -.0625f) : new Vector2(-.1875f, .1875f);
            Vector3 At(Vector2 uv)
            {
                var result = operation < 2 ? EffectCoordinates(operation, uv, new(.5f, .5f), shift) : EffectPolar(uv, shift + new Vector2(.25f, .25f), .5f, 1);
                return new(result.X, result.Y, 0);
            }
            var coordinates = stage == 0 ? EffectVertexPixel(At) : At(new(8.5f / 16, 8.5f / 16));
            value = EffectBlend(0, EffectColor(3, EffectTexture(new(coordinates.X, coordinates.Y))), new(.1f, .2f, .3f));
        }
        if (hdr) value = new((float)(Half)value.X, (float)(Half)value.Y, (float)(Half)value.Z);
        return new(value.X, value.Y, value.Z, 1);
    }
    private static bool EffectPixelsMatch(Color actual, Color expected, bool hdr)
    {
        bool Channel(float a, float e)
        {
            var tolerance = .0021f;
            if (hdr)
            {
                var magnitude = (Half)MathF.Abs(e); var bits = BitConverter.HalfToUInt16Bits(magnitude);
                var adjacent = BitConverter.UInt16BitsToHalf((ushort)(bits + 1));
                // Floating target conversions may choose either adjacent representation.
                tolerance = MathF.Max(tolerance, (float)adjacent - (float)magnitude + 1e-6f);
            }
            return float.IsFinite(a) && MathF.Abs(a - e) <= tolerance;
        }
        return Channel(actual.R, expected.R) && Channel(actual.G, expected.G) && Channel(actual.B, expected.B);
    }

}
