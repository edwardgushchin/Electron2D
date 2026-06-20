/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace Electron2D;

public static class Mathf
{
    public const float E = MathF.E;
    public const float Epsilon = 0.00001f;
    public const float Pi = MathF.PI;
    public const float Tau = MathF.PI * 2f;

    public static float Clamp(float value, float min, float max)
    {
        return Math.Clamp(value, min, max);
    }

    public static int Clamp(int value, int min, int max)
    {
        return Math.Clamp(value, min, max);
    }

    public static float DegToRad(float degrees)
    {
        return degrees * (Pi / 180f);
    }

    public static float RadToDeg(float radians)
    {
        return radians * (180f / Pi);
    }

    public static int FloorToInt(float value)
    {
        return (int)MathF.Floor(value);
    }

    public static int CeilToInt(float value)
    {
        return (int)MathF.Ceiling(value);
    }

    public static int RoundToInt(float value)
    {
        return (int)MathF.Round(value, MidpointRounding.AwayFromZero);
    }

    public static float Lerp(float from, float to, float weight)
    {
        return from + ((to - from) * weight);
    }

    public static float InverseLerp(float from, float to, float weight)
    {
        return from == to ? 0f : (weight - from) / (to - from);
    }

    public static float MoveToward(float from, float to, float delta)
    {
        var difference = to - from;
        if (MathF.Abs(difference) <= delta)
        {
            return to;
        }

        return from + (MathF.Sign(difference) * delta);
    }

    public static int PosMod(int value, int modulo)
    {
        var result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    public static float PosMod(float value, float modulo)
    {
        var result = value % modulo;
        return result < 0f ? result + modulo : result;
    }

    public static float Snapped(float value, float step)
    {
        return step == 0f ? value : MathF.Round(value / step) * step;
    }

    public static bool IsEqualApprox(float a, float b)
    {
        if (a == b)
        {
            return true;
        }

        var tolerance = Epsilon * MathF.Abs(a);
        if (tolerance < Epsilon)
        {
            tolerance = Epsilon;
        }

        return MathF.Abs(a - b) < tolerance;
    }

    public static bool IsZeroApprox(float value)
    {
        return MathF.Abs(value) < Epsilon;
    }

    public static bool IsFinite(float value)
    {
        return float.IsFinite(value);
    }
}
