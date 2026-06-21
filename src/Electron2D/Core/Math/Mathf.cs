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

/// <summary>
/// Represents the mathf type.
/// </summary>
///
/// <remarks>
/// This type is part of the Electron2D 0.1.0 Preview public API.
/// </remarks>
///
/// <threadsafety>
/// Instances of this type are not synchronized. Access them from the thread that owns the object unless the member documentation states otherwise.
/// </threadsafety>
///
/// <since>
/// This API is available since Electron2D 0.1.0 Preview.
/// </since>
///
public static class Mathf
{
    /// <summary>
    /// Represents the e value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this field as a stable value supplied by the declaring type.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public const float E = MathF.E;
    /// <summary>
    /// Represents the epsilon value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this field as a stable value supplied by the declaring type.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public const float Epsilon = 0.00001f;
    /// <summary>
    /// Represents the pi value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this field as a stable value supplied by the declaring type.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public const float Pi = MathF.PI;
    /// <summary>
    /// Represents the tau value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this field as a stable value supplied by the declaring type.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public const float Tau = MathF.PI * 2f;

    /// <summary>
    /// Executes the clamp operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <param name="min">
    /// The min value.
    /// </param>
    ///
    /// <param name="max">
    /// The max value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static float Clamp(float value, float min, float max)
    {
        return Math.Clamp(value, min, max);
    }

    /// <summary>
    /// Executes the clamp operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <param name="min">
    /// The min value.
    /// </param>
    ///
    /// <param name="max">
    /// The max value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static int Clamp(int value, int min, int max)
    {
        return Math.Clamp(value, min, max);
    }

    /// <summary>
    /// Executes the deg to rad operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="degrees">
    /// The degrees value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static float DegToRad(float degrees)
    {
        return degrees * (Pi / 180f);
    }

    /// <summary>
    /// Executes the rad to deg operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="radians">
    /// The radians value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static float RadToDeg(float radians)
    {
        return radians * (180f / Pi);
    }

    /// <summary>
    /// Executes the floor to int operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static int FloorToInt(float value)
    {
        return (int)MathF.Floor(value);
    }

    /// <summary>
    /// Executes the ceil to int operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static int CeilToInt(float value)
    {
        return (int)MathF.Ceiling(value);
    }

    /// <summary>
    /// Executes the round to int operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static int RoundToInt(float value)
    {
        return (int)MathF.Round(value, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Executes the lerp operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="from">
    /// The from value.
    /// </param>
    ///
    /// <param name="to">
    /// The to value.
    /// </param>
    ///
    /// <param name="weight">
    /// The weight value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static float Lerp(float from, float to, float weight)
    {
        return from + ((to - from) * weight);
    }

    /// <summary>
    /// Executes the inverse lerp operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="from">
    /// The from value.
    /// </param>
    ///
    /// <param name="to">
    /// The to value.
    /// </param>
    ///
    /// <param name="weight">
    /// The weight value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static float InverseLerp(float from, float to, float weight)
    {
        return from == to ? 0f : (weight - from) / (to - from);
    }

    /// <summary>
    /// Executes the move toward operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="from">
    /// The from value.
    /// </param>
    ///
    /// <param name="to">
    /// The to value.
    /// </param>
    ///
    /// <param name="delta">
    /// The elapsed time in seconds.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static float MoveToward(float from, float to, float delta)
    {
        var difference = to - from;
        if (MathF.Abs(difference) <= delta)
        {
            return to;
        }

        return from + (MathF.Sign(difference) * delta);
    }

    /// <summary>
    /// Executes the pos mod operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <param name="modulo">
    /// The modulo value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static int PosMod(int value, int modulo)
    {
        var result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    /// <summary>
    /// Executes the pos mod operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <param name="modulo">
    /// The modulo value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static float PosMod(float value, float modulo)
    {
        var result = value % modulo;
        return result < 0f ? result + modulo : result;
    }

    /// <summary>
    /// Executes the snapped operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <param name="step">
    /// The step value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static float Snapped(float value, float step)
    {
        return step == 0f ? value : MathF.Round(value / step) * step;
    }

    /// <summary>
    /// Checks whether equal approx is true.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="a">
    /// The a value.
    /// </param>
    ///
    /// <param name="b">
    /// The b value.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
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

    /// <summary>
    /// Checks whether zero approx is true.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static bool IsZeroApprox(float value)
    {
        return MathF.Abs(value) < Epsilon;
    }

    /// <summary>
    /// Checks whether finite is true.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="value">
    /// The value to use.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Mathf" />
    ///
    public static bool IsFinite(float value)
    {
        return float.IsFinite(value);
    }
}
