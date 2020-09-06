/*
	Copyright (c) 2019-2020 Edward Gushchin.
	Licensed under the Apache License, Version 2.0
*/

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Electron2D.Graphics;

namespace Electron2D
{
    public static class Math
    {
		/// <summary>
        /// <para>The difference between 1.0 and the next representable f64/double precision number.</para>
		/// <para>
		/// Beware:
        /// This value is different from System.Double.Epsilon, which is the smallest, positive, denormalized f64/double.
		/// </para>
        /// </summary>
		public const double Epsilon = Double.Epsilon;

		/// <summary>
        /// Double precision constant for positive infinity.
        /// </summary>
        public const double Infinity = Double.PositiveInfinity;

		/// <summary>
        /// Double precision constant for negative infinity.
        /// </summary>
        public const double NegativeInfinity = Double.NegativeInfinity;

		/// <summary>The mathematical constant pi. Approximately 3.14. This is a f64/double precision constant.</summary>
        public const double PI = System.Math.PI;

		/// <summary>Radians-to-degrees conversion constant.</summary>
		public static double Rad2Deg = 360 / (PI * 2);

		/// <summary>Degrees-to-radians conversion constant.</summary>
		public static double Deg2Rad = (PI * 2) / 360;

		public static double Sin(double f) { return System.Math.Sin(f); }

        // Returns the cosine of angle /f/ in radians.
        public static double Cos(double f) { return System.Math.Cos(f); }

        // Returns the tangent of angle /f/ in radians.
        public static double Tan(double f) { return System.Math.Tan(f); }

        // Returns the arc-sine of /f/ - the angle in radians whose sine is /f/.
        public static double Asin(double f) { return System.Math.Asin(f); }

        // Returns the arc-cosine of /f/ - the angle in radians whose cosine is /f/.
        public static double Acos(double f) { return System.Math.Acos(f); }

        // Returns the arc-tangent of /f/ - the angle in radians whose tangent is /f/.
        public static double Atan(double f) { return System.Math.Atan(f); }

        // Returns the angle in radians whose ::ref::Tan is @@y/x@@.
        public static double Atan2(double y, double x) { return System.Math.Atan2(y, x); }

        // Returns square root of /f/.
        public static double Sqrt(double f) { return System.Math.Sqrt(f); }

        // Returns the absolute value of /f/.
        public static double Abs(double f) { return System.Math.Abs(f); }

        // Returns the absolute value of /value/.
        public static int Abs(int value) { return System.Math.Abs(value); }

        public static double Min(double a, double b) { return a < b ? a : b; }
        // Returns the smallest of two or more values.
        public static double Min(params double[] values)
        {
            int len = values.Length;
            if (len == 0)
                return 0;
            double m = values[0];
            for (int i = 1; i < len; i++)
            {
                if (values[i] < m)
                    m = values[i];
            }
            return m;
        }

        public static int Min(int a, int b) { return a < b ? a : b; }
        // Returns the smallest of two or more values.
        public static int Min(params int[] values)
        {
            int len = values.Length;
            if (len == 0)
                return 0;
            int m = values[0];
            for (int i = 1; i < len; i++)
            {
                if (values[i] < m)
                    m = values[i];
            }
            return m;
        }
        public static double Max(double a, double b) { return a > b ? a : b; }
        // Returns largest of two or more values.
        public static double Max(params double[] values)
        {
            int len = values.Length;
            if (len == 0)
                return 0;
            double m = values[0];
            for (int i = 1; i < len; i++)
            {
                if (values[i] > m)
                    m = values[i];
            }
            return m;
        }

        public static int Max(int a, int b) { return a > b ? a : b; }
        // Returns the largest of two or more values.
        public static int Max(params int[] values)
        {
            int len = values.Length;
            if (len == 0)
                return 0;
            int m = values[0];
            for (int i = 1; i < len; i++)
            {
                if (values[i] > m)
                    m = values[i];
            }
            return m;
        }

        // Returns /f/ raised to power /p/.
        public static double Pow(double f, double p) { return System.Math.Pow(f, p); }

        // Returns e raised to the specified power.
        public static double Exp(double power) { return System.Math.Exp(power); }

        // Returns the logarithm of a specified number in a specified base.
        public static double Log(double f, double p) { return System.Math.Log(f, p); }

        // Returns the natural (base e) logarithm of a specified number.
        public static double Log(double f) { return System.Math.Log(f); }

        // Returns the base 10 logarithm of a specified number.
        public static double Log10(double f) { return System.Math.Log10(f); }

        // Returns the smallest integer greater to or equal to /f/.
        public static double Ceil(double f) { return System.Math.Ceiling(f); }

        // Returns the largest integer smaller to or equal to /f/.
        public static double Floor(double f) { return System.Math.Floor(f); }

        // Returns /f/ rounded to the nearest integer.
        public static double Round(double f) { return System.Math.Round(f); }

        // Returns the smallest integer greater to or equal to /f/.
        public static int CeilToInt(double f) { return (int)System.Math.Ceiling(f); }

        // Returns the largest integer smaller to or equal to /f/.
        public static int FloorToInt(double f) { return (int)Math.Floor(f); }

        // Returns /f/ rounded to the nearest integer.
        public static int RoundToInt(double f) { return (int)Math.Round(f); }

        // Returns the sign of /f/.
        public static double Sign(double f) { return f >= 0 ? 1 : -1; }

        // Clamps a value between a minimum float and maximum float value.
        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            else if (value > max)
                return max;
            return value;
        }

        // Clamps value between min and max and returns value.
        // Set the position of the transform to be that of the time
        // but never less than 1 or more than 3
        //
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            else if (value > max)
                return max;
            return value;
        }

        // Clamps value between 0 and 1 and returns value
        public static double Clamp01(double value)
        {
            if (value < 0)
                return 0;
            else if (value > 1)
                return 1;
            else
                return value;
        }

        // Interpolates between /a/ and /b/ by /t/. /t/ is clamped between 0 and 1.
        public static double Lerp(double a, double b, double t)
        {
            return a + ((b - a) * Clamp01(t));
        }

        // Interpolates between /a/ and /b/ by /t/ without clamping the interpolant.
        public static double LerpUnclamped(double a, double b, double t)
        {
            return a + ((b - a) * t);
        }

        // Same as ::ref::Lerp but makes sure the values interpolate correctly when they wrap around 360 degrees.
        public static double LerpAngle(double a, double b, double t)
        {
            double delta = Repeat((b - a), 360);
            if (delta > 180)
                delta -= 360;
            return a + (delta * Clamp01(t));
        }

        // Moves a value /current/ towards /target/.
        static public double MoveTowards(double current, double target, double maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta)
                return target;
            return current + (Math.Sign(target - current) * maxDelta);
        }

        // Same as ::ref::MoveTowards but makes sure the values interpolate correctly when they wrap around 360 degrees.
        static public double MoveTowardsAngle(double current, double target, double maxDelta)
        {
            double deltaAngle = DeltaAngle(current, target);
            if (-maxDelta < deltaAngle && deltaAngle < maxDelta)
                return target;
            target = current + deltaAngle;
            return MoveTowards(current, target, maxDelta);
        }

        // Interpolates between /min/ and /max/ with smoothing at the limits.
        public static double SmoothStep(double from, double to, double t)
        {
            t = Math.Clamp01(t);
            t = (-2 * t * t * t) + (3 * t * t);
            return (to * t) + (from * (1 - t));
        }

        //*undocumented
        public static double Gamma(double value, double absmax, double gamma)
        {
            bool negative = value < 0F;
            double absval = Abs(value);
            if (absval > absmax)
                return negative ? -absval : absval;

            double result = Pow(absval / absmax, gamma) * absmax;
            return negative ? -result : result;
        }

        // Compares two floating point values if they are similar.
        public static bool Approximately(float a, float b)
        {
            // If a or b is zero, compare that the other is less or equal to epsilon.
            // If neither a or b are 0, then find an epsilon that is good for
            // comparing numbers at the maximum magnitude of a and b.
            // Floating points have about 7 significant digits, so
            // 1.000001f can be represented while 1.0000001f is rounded to zero,
            // thus we could use an epsilon of 0.000001f for comparing values close to 1.
            // We multiply this epsilon by the biggest magnitude of a and b.
            return Abs(b - a) < Max(0.000001f * Max(Abs(a), Abs(b)), Epsilon * 8);
        }

        public static double SmoothDamp(double current, double target, ref double currentVelocity, double smoothTime, double maxSpeed)
        {
            double deltaTime = Time.DeltaTime;
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        public static double SmoothDamp(double current, double target, ref double currentVelocity, double smoothTime)
        {
            double deltaTime = Time.DeltaTime;
            const double maxSpeed = Math.Infinity;
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        // Gradually changes a value towards a desired goal over time.
        public static double SmoothDamp(double current, double target, ref double currentVelocity, double smoothTime,  double maxSpeed, double deltaTime)
        {
            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = Math.Max(0.0001, smoothTime);
            double omega = 2 / smoothTime;

            double x = omega * deltaTime;
            double exp = 1 / (1 + x + (0.48 * x * x) + (0.235 * x * x * x));
            double change = current - target;
            double originalTo = target;

            // Clamp maximum speed
            double maxChange = maxSpeed * smoothTime;
            change = Math.Clamp(change, -maxChange, maxChange);
            target = current - change;

            double temp = (currentVelocity + (omega * change)) * deltaTime;
            currentVelocity = (currentVelocity - (omega * temp)) * exp;
            double output = target + ((change + temp) * exp);

            // Prevent overshooting
            if (originalTo - current > 0 == output > originalTo)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }

            return output;
        }

        public static double SmoothDampAngle(double current, double target, ref double currentVelocity, double smoothTime, double maxSpeed)
        {
            double deltaTime = Time.DeltaTime;
            return SmoothDampAngle(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        public static double SmoothDampAngle(double current, double target, ref double currentVelocity, double smoothTime)
        {
            double deltaTime = Time.DeltaTime;
            const double maxSpeed = Math.Infinity;
            return SmoothDampAngle(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        // Gradually changes an angle given in degrees towards a desired goal angle over time.
        public static double SmoothDampAngle(double current, double target, ref double currentVelocity, double smoothTime, double maxSpeed, double deltaTime)
        {
            target = current + DeltaAngle(current, target);
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        // Loops the value t, so that it is never larger than length and never smaller than 0.
        public static double Repeat(double t, double length)
        {
            return Clamp(t - (Math.Floor(t / length) * length), 0, length);
        }

        // PingPongs the value t, so that it is never larger than length and never smaller than 0.
        public static double PingPong(double t, double length)
        {
            t = Repeat(t, length * 2);
            return length - Math.Abs(t - length);
        }

        // Calculates the ::ref::Lerp parameter between of two values.
        public static double InverseLerp(double a, double b, double value)
        {
            if (a != b)
                return Clamp01((value - a) / (b - a));
            else
                return 0;
        }

        // Calculates the shortest difference between two given angles.
        public static double DeltaAngle(double current, double target)
        {
            double delta = Math.Repeat((target - current), 360);
            if (delta > 180)
                delta -= 360;
            return delta;
        }

        // Infinite Line Intersection (line1 is p1-p2 and line2 is p3-p4)
        internal static bool LineIntersection(Vector p1, Vector p2, Vector p3, Vector p4, ref Vector result)
        {
            double bx = p2.X - p1.X;
            double by = p2.Y - p1.Y;
            double dx = p4.X - p3.X;
            double dy = p4.Y - p3.Y;
            double bDotDPerp = (bx * dy) - (by * dx);
            if (bDotDPerp == 0)
            {
                return false;
            }
            double cx = p3.X - p1.X;
            double cy = p3.Y - p1.Y;
            double t = ((cx * dy) - (cy * dx)) / bDotDPerp;

            result.X = p1.X + (t * bx);
            result.Y = p1.Y + (t * by);
            return true;
        }

        // Line Segment Intersection (line1 is p1-p2 and line2 is p3-p4)
        internal static bool LineSegmentIntersection(Vector p1, Vector p2, Vector p3, Vector p4, ref Vector result)
        {
            double bx = p2.X - p1.X;
            double by = p2.Y - p1.Y;
            double dx = p4.X - p3.X;
            double dy = p4.Y - p3.Y;
            double bDotDPerp = bx * dy - by * dx;
            if (bDotDPerp == 0)
            {
                return false;
            }
            double cx = p3.X - p1.X;
            double cy = p3.Y - p1.Y;
            double t = ((cx * dy) - (cy * dx)) / bDotDPerp;
            if (t < 0 || t > 1)
            {
                return false;
            }
            double u = ((cx * by) - (cy * bx)) / bDotDPerp;
            if (u < 0 || u > 1)
            {
                return false;
            }

            result.X = p1.X + (t * bx);
            result.Y = p1.Y + (t * by);
            return true;
        }

        static internal long RandomToLong(System.Random r)
        {
            var buffer = new byte[8];
            r.NextBytes(buffer);
            return (long)(System.BitConverter.ToUInt64(buffer, 0) & System.Int64.MaxValue);
        }
    }
}
