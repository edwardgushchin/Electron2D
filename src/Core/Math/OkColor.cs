// Copyright (c) 2021 Björn Ottosson
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace Electron2D;

internal static class OkColor
{
    private const float Pi = Mathf.Pi;

    internal readonly record struct Rgb(float R, float G, float B);

    internal readonly record struct Hsl(float H, float S, float L);

    private readonly record struct Lab(float L, float A, float B);

    private readonly record struct Lc(float L, float C);

    private readonly record struct St(float S, float T);

    private readonly record struct Cs(float C0, float CMid, float CMax);

    internal static Rgb ToSrgb(float hue, float saturation, float lightness)
    {
        if (lightness == 1f)
            return new Rgb(1f, 1f, 1f);
        if (lightness == 0f)
            return default;

        var a = Mathf.Cos(2f * Pi * hue);
        var b = Mathf.Sin(2f * Pi * hue);
        var l = ToeInverse(lightness);
        var cs = GetCs(l, a, b);
        const float middle = 0.8f;
        const float inverseMiddle = 1.25f;

        float chroma;
        if (saturation < middle)
        {
            var t = inverseMiddle * saturation;
            var k1 = middle * cs.C0;
            var k2 = 1f - (k1 / cs.CMid);
            chroma = t * k1 / (1f - (k2 * t));
        }
        else
        {
            var t = (saturation - middle) / (1f - middle);
            var k0 = cs.CMid;
            var k1 = (1f - middle) * cs.CMid * cs.CMid * inverseMiddle * inverseMiddle / cs.C0;
            var k2 = 1f - (k1 / (cs.CMax - cs.CMid));
            chroma = k0 + (t * k1 / (1f - (k2 * t)));
        }

        var rgb = OklabToLinearSrgb(new Lab(l, chroma * a, chroma * b));
        return new Rgb(ToSrgbChannel(rgb.R), ToSrgbChannel(rgb.G), ToSrgbChannel(rgb.B));
    }

    internal static Hsl FromSrgb(float red, float green, float blue)
    {
        if (red == 0f && green == 0f && blue == 0f)
            return default;

        var lab = LinearSrgbToOklab(new Rgb(
            FromSrgbChannel(red),
            FromSrgbChannel(green),
            FromSrgbChannel(blue)));
        var chroma = Mathf.Sqrt((lab.A * lab.A) + (lab.B * lab.B));
        if (chroma <= 0.0000001f)
            return new Hsl(0f, 0f, Toe(lab.L));

        var a = lab.A / chroma;
        var b = lab.B / chroma;
        var hue = 0.5f + (0.5f * Mathf.Atan2(-lab.B, -lab.A) / Pi);
        var cs = GetCs(lab.L, a, b);
        const float middle = 0.8f;
        const float inverseMiddle = 1.25f;

        float saturation;
        if (chroma < cs.CMid)
        {
            var k1 = middle * cs.C0;
            var k2 = 1f - (k1 / cs.CMid);
            var t = chroma / (k1 + (k2 * chroma));
            saturation = t * middle;
        }
        else
        {
            var k0 = cs.CMid;
            var k1 = (1f - middle) * cs.CMid * cs.CMid * inverseMiddle * inverseMiddle / cs.C0;
            var k2 = 1f - (k1 / (cs.CMax - cs.CMid));
            var t = (chroma - k0) / (k1 + (k2 * (chroma - k0)));
            saturation = middle + ((1f - middle) * t);
        }

        return new Hsl(hue, saturation, Toe(lab.L));
    }

    private static float ToSrgbChannel(float value) =>
        value <= 0.0031308f ? 12.92f * value : (1.055f * Mathf.Pow(value, 1f / 2.4f)) - 0.055f;

    private static float FromSrgbChannel(float value) =>
        value > 0.04045f ? Mathf.Pow((value + 0.055f) / 1.055f, 2.4f) : value / 12.92f;

    private static Lab LinearSrgbToOklab(Rgb color)
    {
        var l = (0.4122214708f * color.R) + (0.5363325363f * color.G) + (0.0514459929f * color.B);
        var m = (0.2119034982f * color.R) + (0.6806995451f * color.G) + (0.1073969566f * color.B);
        var s = (0.0883024619f * color.R) + (0.2817188376f * color.G) + (0.6299787005f * color.B);
        var cubeRootL = MathF.Cbrt(l);
        var cubeRootM = MathF.Cbrt(m);
        var cubeRootS = MathF.Cbrt(s);
        return new Lab(
            (0.2104542553f * cubeRootL) + (0.7936177850f * cubeRootM) - (0.0040720468f * cubeRootS),
            (1.9779984951f * cubeRootL) - (2.4285922050f * cubeRootM) + (0.4505937099f * cubeRootS),
            (0.0259040371f * cubeRootL) + (0.7827717662f * cubeRootM) - (0.8086757660f * cubeRootS));
    }

    private static Rgb OklabToLinearSrgb(Lab color)
    {
        var cubeRootL = color.L + (0.3963377774f * color.A) + (0.2158037573f * color.B);
        var cubeRootM = color.L - (0.1055613458f * color.A) - (0.0638541728f * color.B);
        var cubeRootS = color.L - (0.0894841775f * color.A) - (1.2914855480f * color.B);
        var l = cubeRootL * cubeRootL * cubeRootL;
        var m = cubeRootM * cubeRootM * cubeRootM;
        var s = cubeRootS * cubeRootS * cubeRootS;
        return new Rgb(
            (4.0767416621f * l) - (3.3077115913f * m) + (0.2309699292f * s),
            (-1.2684380046f * l) + (2.6097574011f * m) - (0.3413193965f * s),
            (-0.0041960863f * l) - (0.7034186147f * m) + (1.7076147010f * s));
    }

    private static float ComputeMaxSaturation(float a, float b)
    {
        float k0;
        float k1;
        float k2;
        float k3;
        float k4;
        float weightL;
        float weightM;
        float weightS;
        if (((-1.88170328f * a) - (0.80936493f * b)) > 1f)
        {
            k0 = 1.19086277f;
            k1 = 1.76576728f;
            k2 = 0.59662641f;
            k3 = 0.75515197f;
            k4 = 0.56771245f;
            weightL = 4.0767416621f;
            weightM = -3.3077115913f;
            weightS = 0.2309699292f;
        }
        else if (((1.81444104f * a) - (1.19445276f * b)) > 1f)
        {
            k0 = 0.73956515f;
            k1 = -0.45954404f;
            k2 = 0.08285427f;
            k3 = 0.12541070f;
            k4 = 0.14503204f;
            weightL = -1.2684380046f;
            weightM = 2.6097574011f;
            weightS = -0.3413193965f;
        }
        else
        {
            k0 = 1.35733652f;
            k1 = -0.00915799f;
            k2 = -1.15130210f;
            k3 = -0.50559606f;
            k4 = 0.00692167f;
            weightL = -0.0041960863f;
            weightM = -0.7034186147f;
            weightS = 1.7076147010f;
        }

        var saturation = k0 + (k1 * a) + (k2 * b) + (k3 * a * a) + (k4 * a * b);
        var coefficientL = (0.3963377774f * a) + (0.2158037573f * b);
        var coefficientM = (-0.1055613458f * a) - (0.0638541728f * b);
        var coefficientS = (-0.0894841775f * a) - (1.2914855480f * b);
        var rootL = 1f + (saturation * coefficientL);
        var rootM = 1f + (saturation * coefficientM);
        var rootS = 1f + (saturation * coefficientS);
        var l = rootL * rootL * rootL;
        var m = rootM * rootM * rootM;
        var s = rootS * rootS * rootS;
        var derivativeL = 3f * coefficientL * rootL * rootL;
        var derivativeM = 3f * coefficientM * rootM * rootM;
        var derivativeS = 3f * coefficientS * rootS * rootS;
        var secondDerivativeL = 6f * coefficientL * coefficientL * rootL;
        var secondDerivativeM = 6f * coefficientM * coefficientM * rootM;
        var secondDerivativeS = 6f * coefficientS * coefficientS * rootS;
        var value = (weightL * l) + (weightM * m) + (weightS * s);
        var derivative = (weightL * derivativeL) + (weightM * derivativeM) + (weightS * derivativeS);
        var secondDerivative =
            (weightL * secondDerivativeL) + (weightM * secondDerivativeM) + (weightS * secondDerivativeS);
        return saturation - (value * derivative / ((derivative * derivative) - (0.5f * value * secondDerivative)));
    }

    private static Lc FindCusp(float a, float b)
    {
        var saturation = ComputeMaxSaturation(a, b);
        var atMaximum = OklabToLinearSrgb(new Lab(1f, saturation * a, saturation * b));
        var lightness = MathF.Cbrt(1f / Mathf.Max(atMaximum.R, Mathf.Max(atMaximum.G, atMaximum.B)));
        return new Lc(lightness, lightness * saturation);
    }

    private static float FindGamutIntersection(float a, float b, float l1, float c1, float l0, Lc cusp)
    {
        if ((((l1 - l0) * cusp.C) - ((cusp.L - l0) * c1)) <= 0f)
            return cusp.C * l0 / ((c1 * cusp.L) + (cusp.C * (l0 - l1)));

        var intersection = cusp.C * (l0 - 1f) / ((c1 * (cusp.L - 1f)) + (cusp.C * (l0 - l1)));
        var deltaL = l1 - l0;
        var deltaC = c1;
        var coefficientL = (0.3963377774f * a) + (0.2158037573f * b);
        var coefficientM = (-0.1055613458f * a) - (0.0638541728f * b);
        var coefficientS = (-0.0894841775f * a) - (1.2914855480f * b);
        var derivativeL = deltaL + (deltaC * coefficientL);
        var derivativeM = deltaL + (deltaC * coefficientM);
        var derivativeS = deltaL + (deltaC * coefficientS);
        var l = (l0 * (1f - intersection)) + (intersection * l1);
        var c = intersection * c1;
        var rootL = l + (c * coefficientL);
        var rootM = l + (c * coefficientM);
        var rootS = l + (c * coefficientS);
        var cubeL = rootL * rootL * rootL;
        var cubeM = rootM * rootM * rootM;
        var cubeS = rootS * rootS * rootS;
        var firstL = 3f * derivativeL * rootL * rootL;
        var firstM = 3f * derivativeM * rootM * rootM;
        var firstS = 3f * derivativeS * rootS * rootS;
        var secondL = 6f * derivativeL * derivativeL * rootL;
        var secondM = 6f * derivativeM * derivativeM * rootM;
        var secondS = 6f * derivativeS * derivativeS * rootS;
        var red = (4.0767416621f * cubeL) - (3.3077115913f * cubeM) + (0.2309699292f * cubeS) - 1f;
        var redFirst = (4.0767416621f * firstL) - (3.3077115913f * firstM) + (0.2309699292f * firstS);
        var redSecond = (4.0767416621f * secondL) - (3.3077115913f * secondM) + (0.2309699292f * secondS);
        var redU = redFirst / ((redFirst * redFirst) - (0.5f * red * redSecond));
        var redT = redU >= 0f ? -red * redU : float.MaxValue;
        var green = (-1.2684380046f * cubeL) + (2.6097574011f * cubeM) - (0.3413193965f * cubeS) - 1f;
        var greenFirst = (-1.2684380046f * firstL) + (2.6097574011f * firstM) - (0.3413193965f * firstS);
        var greenSecond = (-1.2684380046f * secondL) + (2.6097574011f * secondM) - (0.3413193965f * secondS);
        var greenU = greenFirst / ((greenFirst * greenFirst) - (0.5f * green * greenSecond));
        var greenT = greenU >= 0f ? -green * greenU : float.MaxValue;
        var blue = (-0.0041960863f * cubeL) - (0.7034186147f * cubeM) + (1.7076147010f * cubeS) - 1f;
        var blueFirst = (-0.0041960863f * firstL) - (0.7034186147f * firstM) + (1.7076147010f * firstS);
        var blueSecond = (-0.0041960863f * secondL) - (0.7034186147f * secondM) + (1.7076147010f * secondS);
        var blueU = blueFirst / ((blueFirst * blueFirst) - (0.5f * blue * blueSecond));
        var blueT = blueU >= 0f ? -blue * blueU : float.MaxValue;
        return intersection + Mathf.Min(redT, Mathf.Min(greenT, blueT));
    }

    private static float Toe(float value)
    {
        const float k1 = 0.206f;
        const float k2 = 0.03f;
        const float k3 = (1f + k1) / (1f + k2);
        var adjusted = (k3 * value) - k1;
        return 0.5f * (adjusted + Mathf.Sqrt((adjusted * adjusted) + (4f * k2 * k3 * value)));
    }

    private static float ToeInverse(float value)
    {
        const float k1 = 0.206f;
        const float k2 = 0.03f;
        const float k3 = (1f + k1) / (1f + k2);
        return ((value * value) + (k1 * value)) / (k3 * (value + k2));
    }

    private static St ToSt(Lc cusp) => new(cusp.C / cusp.L, cusp.C / (1f - cusp.L));

    private static St GetStMiddle(float a, float b)
    {
        var saturation = 0.11516993f + (1f / (
            7.44778970f + (4.15901240f * b) +
            (a * (-2.19557347f + (1.75198401f * b) +
            (a * (-2.13704948f - (10.02301043f * b) +
            (a * (-4.24894561f + (5.38770819f * b) + (4.69891013f * a)))))))));
        var triangle = 0.11239642f + (1f / (
            1.61320320f - (0.68124379f * b) +
            (a * (0.40370612f + (0.90148123f * b) +
            (a * (-0.27087943f + (0.61223990f * b) +
            (a * (0.00299215f - (0.45399568f * b) - (0.14661872f * a)))))))));
        return new St(saturation, triangle);
    }

    private static Cs GetCs(float lightness, float a, float b)
    {
        var cusp = FindCusp(a, b);
        var maximum = FindGamutIntersection(a, b, lightness, 1f, lightness, cusp);
        var maximumSt = ToSt(cusp);
        var scale = maximum / Mathf.Min(lightness * maximumSt.S, (1f - lightness) * maximumSt.T);
        var middleSt = GetStMiddle(a, b);
        var middleA = lightness * middleSt.S;
        var middleB = (1f - lightness) * middleSt.T;
        var middle = 0.9f * scale * Mathf.Sqrt(Mathf.Sqrt(
            1f / ((1f / Mathf.Pow(middleA, 4f)) + (1f / Mathf.Pow(middleB, 4f)))));
        var zeroA = lightness * 0.4f;
        var zeroB = (1f - lightness) * 0.8f;
        var zero = Mathf.Sqrt(1f / ((1f / (zeroA * zeroA)) + (1f / (zeroB * zeroB))));
        return new Cs(zero, middle, maximum);
    }
}
