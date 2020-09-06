/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Electron2D.Graphics
{
    public struct Vector : IEquatable<Vector>, IFormattable
    {
        public Vector(float value) : this(value, value) { }

        public Vector(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; set; }

        public float Y { get; set; }

        public readonly bool Equals(Vector other)
        {
            return this.X == other.X && this.Y == other.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vector value1, Vector value2)
        {
            return (value1.X * value2.X) + (value1.Y * value2.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Min(Vector value1, Vector value2)
        {
            return new Vector(
                (value1.X < value2.X) ? value1.X : value2.X,
                (value1.Y < value2.Y) ? value1.Y : value2.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Max(Vector value1, Vector value2)
        {
            return new Vector(
                (value1.X > value2.X) ? value1.X : value2.X,
                (value1.Y > value2.Y) ? value1.Y : value2.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Abs(Vector value)
        {
            return new Vector(Math.Abs(value.X), Math.Abs(value.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector SquareRoot(Vector value)
        {
            return new Vector(Math.Sqrt(value.X), Math.Sqrt(value.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator +(Vector left, Vector right)
        {
            return new Vector(left.X + right.X, left.Y + right.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator -(Vector left, Vector right)
        {
            return new Vector(left.X - right.X, left.Y - right.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator *(Vector left, Vector right)
        {
            return new Vector(left.X * right.X, left.Y * right.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator *(float left, Vector right)
        {
            return new Vector(left, left) * right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator *(Vector left, float right)
        {
            return left * new Vector(right, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator /(Vector left, Vector right)
        {
            return new Vector(left.X / right.X, left.Y / right.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator /(Vector value1, float value2)
        {
            return value1 / new Vector(value2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator -(Vector value)
        {
            return Zero - value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector left, Vector right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector left, Vector right)
        {
            return !(left == right);
        }

        public static Vector Zero
        {
            get
            {
                return new Vector();
            }
        }

        public static Vector One
        {
            get
            {
                return new Vector(1.0f, 1.0f);
            }
        }

        public static Vector UnitX { get { return new Vector(1.0f, 0.0f); } }

        public static Vector UnitY { get { return new Vector(0.0f, 1.0f); } }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X.GetHashCode(), Y.GetHashCode());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals(object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (!(obj is Vector))
                return false;
            return Equals((Vector)obj);
        }

        public override readonly string ToString()
        {
            return ToString("G", CultureInfo.CurrentCulture);
        }

        public readonly string ToString(string format)
        {
            if (format is null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            return ToString(format, CultureInfo.CurrentCulture);
        }

        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (format is null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (formatProvider is null)
            {
                throw new ArgumentNullException(nameof(formatProvider));
            }

            StringBuilder sb = new StringBuilder();
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            sb.Append('<');
            sb.Append(this.X.ToString(format, formatProvider));
            sb.Append(separator);
            sb.Append(' ');
            sb.Append(this.Y.ToString(format, formatProvider));
            sb.Append('>');
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Length()
        {
            return Math.Sqrt(Dot(this, this));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float LengthSquared()
        {
            return Dot(this, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Vector value1, Vector value2)
        {
            Vector difference = value1 - value2;
            return Math.Sqrt(Vector.Dot(difference, difference));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSquared(Vector value1, Vector value2)
        {
            Vector difference = value1 - value2;
            return Vector.Dot(difference, difference);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Normalize(Vector value)
        {
            return value / value.Length();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Reflect(Vector vector, Vector normal)
        {
            float dot = Dot(vector, normal);
            return vector - (2f * dot * normal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Clamp(Vector value1, Vector min, Vector max)
        {
            // This compare order is very important!!!
            // We must follow HLSL behavior in the case user specified min value is bigger than max value.
            float x = value1.X;
            x = (min.X > x) ? min.X : x;  // max(x, minx)
            x = (max.X < x) ? max.X : x;  // min(x, maxx)

            float y = value1.Y;
            y = (min.Y > y) ? min.Y : y;  // max(y, miny)
            y = (max.Y < y) ? max.Y : y;  // min(y, maxy)

            return new Vector(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Lerp(Vector value1, Vector value2, float amount)
        {
            return new Vector(
                value1.X + ((value2.X - value1.X) * amount),
                value1.Y + ((value2.Y - value1.Y) * amount));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Add(Vector left, Vector right)
        {
            return left + right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Subtract(Vector left, Vector right)
        {
            return left - right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Multiply(Vector left, Vector right)
        {
            return left * right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Multiply(Vector left, float right)
        {
            return left * right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Multiply(float left, Vector right)
        {
            return left * right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Divide(Vector left, Vector right)
        {
            return left / right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Divide(Vector left, float divisor)
        {
            return left / divisor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Negate(Vector value)
        {
            return -value;
        }
    }
}