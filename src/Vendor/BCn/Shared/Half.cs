#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
/// ================ BCnHalf.cs ====================
/// The code is free to use for any reason without any restrictions.
/// Ladislav Lang (2009), Joannes Vermorel (2017)

using System;
using System.Diagnostics;
using System.Globalization;

namespace Electron2DBlockCodecs.Shared
{
	/// <summary>
	/// Represents a half-precision floating point number. 
	/// </summary>
	/// <remarks>
	/// Note:
	///     BCnHalf is not fast enought and precision is also very bad, 
	///     so is should not be used for mathematical computation (use Single instead).
	///     The main advantage of BCnHalf type is lower memory cost: two bytes per number. 
	///     BCnHalf is typically used in graphical applications.
	///     
	/// Note: 
	///     All functions, where is used conversion half->float/float->half, 
	///     are approx. ten times slower than float->double/double->float, i.e. ~3ns on 2GHz CPU.
	///
	/// References:
	///     - Code retrieved from http://sourceforge.net/p/csharp-half/code/HEAD/tree/ on 2015-12-04
	///     - Fast BCnHalf Float Conversions, Jeroen van der Zijp, link: http://www.fox-toolkit.org/ftp/fasthalffloatconversion.pdf
	///     - IEEE 754 revision, link: http://grouper.ieee.org/groups/754/
	/// </remarks>
	[Serializable]
	internal struct BCnHalf : IComparable, IFormattable, IConvertible, IComparable<BCnHalf>, IEquatable<BCnHalf>
	{
		/// <summary>
		/// Internal representation of the half-precision floating-point number.
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal ushort Value;

		#region Constants
		/// <summary>
		/// Represents the smallest positive System.BCnHalf value greater than zero. This field is constant.
		/// </summary>
		public static readonly BCnHalf Epsilon = ToHalf(0x0001);
		/// <summary>
		/// Represents the largest possible value of System.BCnHalf. This field is constant.
		/// </summary>
		public static readonly BCnHalf MaxValue = ToHalf(0x7bff);
		/// <summary>
		/// Represents the smallest possible value of System.BCnHalf. This field is constant.
		/// </summary>
		public static readonly BCnHalf MinValue = ToHalf(0xfbff);
		/// <summary>
		/// Represents not a number (NaN). This field is constant.
		/// </summary>
		public static readonly BCnHalf NaN = ToHalf(0xfe00);
		/// <summary>
		/// Represents negative infinity. This field is constant.
		/// </summary>
		public static readonly BCnHalf NegativeInfinity = ToHalf(0xfc00);
		/// <summary>
		/// Represents positive infinity. This field is constant.
		/// </summary>
		public static readonly BCnHalf PositiveInfinity = ToHalf(0x7c00);
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of System.BCnHalf to the value of the specified single-precision floating-point number.
		/// </summary>
		/// <param name="value">The value to represent as a System.BCnHalf.</param>
		public BCnHalf(float value) { this = HalfHelper.SingleToHalf(value); }
		/// <summary>
		/// Initializes a new instance of System.BCnHalf to the value of the specified 32-bit signed integer.
		/// </summary>
		/// <param name="value">The value to represent as a System.BCnHalf.</param>
		public BCnHalf(int value) : this((float)value) { }
		/// <summary>
		/// Initializes a new instance of System.BCnHalf to the value of the specified 64-bit signed integer.
		/// </summary>
		/// <param name="value">The value to represent as a System.BCnHalf.</param>
		public BCnHalf(long value) : this((float)value) { }
		/// <summary>
		/// Initializes a new instance of System.BCnHalf to the value of the specified double-precision floating-point number.
		/// </summary>
		/// <param name="value">The value to represent as a System.BCnHalf.</param>
		public BCnHalf(double value) : this((float)value) { }
		/// <summary>
		/// Initializes a new instance of System.BCnHalf to the value of the specified decimal number.
		/// </summary>
		/// <param name="value">The value to represent as a System.BCnHalf.</param>
		public BCnHalf(decimal value) : this((float)value) { }
		/// <summary>
		/// Initializes a new instance of System.BCnHalf to the value of the specified 32-bit unsigned integer.
		/// </summary>
		/// <param name="value">The value to represent as a System.BCnHalf.</param>
		public BCnHalf(uint value) : this((float)value) { }
		/// <summary>
		/// Initializes a new instance of System.BCnHalf to the value of the specified 64-bit unsigned integer.
		/// </summary>
		/// <param name="value">The value to represent as a System.BCnHalf.</param>
		public BCnHalf(ulong value) : this((float)value) { }
		#endregion

		#region Numeric operators

		/// <summary>
		/// Returns the result of multiplying the specified System.BCnHalf value by negative one.
		/// </summary>
		/// <param name="half">A System.BCnHalf.</param>
		/// <returns>A System.BCnHalf with the value of half, but the opposite sign. -or- Zero, if half is zero.</returns>
		public static BCnHalf Negate(BCnHalf half) { return -half; }
		/// <summary>
		/// Adds two specified System.BCnHalf values.
		/// </summary>
		/// <param name="half1">A System.BCnHalf.</param>
		/// <param name="half2">A System.BCnHalf.</param>
		/// <returns>A System.BCnHalf value that is the sum of half1 and half2.</returns>
		public static BCnHalf Add(BCnHalf half1, BCnHalf half2) { return half1 + half2; }
		/// <summary>
		/// Subtracts one specified System.BCnHalf value from another.
		/// </summary>
		/// <param name="half1">A System.BCnHalf (the minuend).</param>
		/// <param name="half2">A System.BCnHalf (the subtrahend).</param>
		/// <returns>The System.BCnHalf result of subtracting half2 from half1.</returns>
		public static BCnHalf Subtract(BCnHalf half1, BCnHalf half2) { return half1 - half2; }
		/// <summary>
		/// Multiplies two specified System.BCnHalf values.
		/// </summary>
		/// <param name="half1">A System.BCnHalf (the multiplicand).</param>
		/// <param name="half2">A System.BCnHalf (the multiplier).</param>
		/// <returns>A System.BCnHalf that is the result of multiplying half1 and half2.</returns>
		public static BCnHalf Multiply(BCnHalf half1, BCnHalf half2) { return half1 * half2; }
		/// <summary>
		/// Divides two specified System.BCnHalf values.
		/// </summary>
		/// <param name="half1">A System.BCnHalf (the dividend).</param>
		/// <param name="half2">A System.BCnHalf (the divisor).</param>
		/// <returns>The System.BCnHalf that is the result of dividing half1 by half2.</returns>
		/// <exception cref="System.DivideByZeroException">half2 is zero.</exception>
		public static BCnHalf Divide(BCnHalf half1, BCnHalf half2) { return half1 / half2; }

		/// <summary>
		/// Returns the value of the System.BCnHalf operand (the sign of the operand is unchanged).
		/// </summary>
		/// <param name="half">The System.BCnHalf operand.</param>
		/// <returns>The value of the operand, half.</returns>
		public static BCnHalf operator +(BCnHalf half) { return half; }
		/// <summary>
		/// Negates the value of the specified System.BCnHalf operand.
		/// </summary>
		/// <param name="half">The System.BCnHalf operand.</param>
		/// <returns>The result of half multiplied by negative one (-1).</returns>
		public static BCnHalf operator -(BCnHalf half) { return HalfHelper.Negate(half); }
		/// <summary>
		/// Increments the System.BCnHalf operand by 1.
		/// </summary>
		/// <param name="half">The System.BCnHalf operand.</param>
		/// <returns>The value of half incremented by 1.</returns>
		public static BCnHalf operator ++(BCnHalf half) { return (BCnHalf)(half + 1f); }
		/// <summary>
		/// Decrements the System.BCnHalf operand by one.
		/// </summary>
		/// <param name="half">The System.BCnHalf operand.</param>
		/// <returns>The value of half decremented by 1.</returns>
		public static BCnHalf operator --(BCnHalf half) { return (BCnHalf)(half - 1f); }
		/// <summary>
		/// Adds two specified System.BCnHalf values.
		/// </summary>
		/// <param name="half1">A System.BCnHalf.</param>
		/// <param name="half2">A System.BCnHalf.</param>
		/// <returns>The System.BCnHalf result of adding half1 and half2.</returns>
		public static BCnHalf operator +(BCnHalf half1, BCnHalf half2) { return (BCnHalf)(half1 + (float)half2); }
		/// <summary>
		/// Subtracts two specified System.BCnHalf values.
		/// </summary>
		/// <param name="half1">A System.BCnHalf.</param>
		/// <param name="half2">A System.BCnHalf.</param>
		/// <returns>The System.BCnHalf result of subtracting half1 and half2.</returns>        
		public static BCnHalf operator -(BCnHalf half1, BCnHalf half2) { return (BCnHalf)(half1 - (float)half2); }
		/// <summary>
		/// Multiplies two specified System.BCnHalf values.
		/// </summary>
		/// <param name="half1">A System.BCnHalf.</param>
		/// <param name="half2">A System.BCnHalf.</param>
		/// <returns>The System.BCnHalf result of multiplying half1 by half2.</returns>
		public static BCnHalf operator *(BCnHalf half1, BCnHalf half2) { return (BCnHalf)(half1 * (float)half2); }
		/// <summary>
		/// Divides two specified System.BCnHalf values.
		/// </summary>
		/// <param name="half1">A System.BCnHalf (the dividend).</param>
		/// <param name="half2">A System.BCnHalf (the divisor).</param>
		/// <returns>The System.BCnHalf result of half1 by half2.</returns>
		public static BCnHalf operator /(BCnHalf half1, BCnHalf half2) { return (BCnHalf)(half1 / (float)half2); }
		/// <summary>
		/// Returns a value indicating whether two instances of System.BCnHalf are equal.
		/// </summary>
		/// <param name="half1">A System.BCnHalf.</param>
		/// <param name="half2">A System.BCnHalf.</param>
		/// <returns>true if half1 and half2 are equal; otherwise, false.</returns>
		public static bool operator ==(BCnHalf half1, BCnHalf half2) { return (!IsNaN(half1) && (half1.Value == half2.Value)); }
		/// <summary>
		/// Returns a value indicating whether two instances of System.BCnHalf are not equal.
		/// </summary>
		/// <param name="half1">A System.BCnHalf.</param>
		/// <param name="half2">A System.BCnHalf.</param>
		/// <returns>true if half1 and half2 are not equal; otherwise, false.</returns>
		public static bool operator !=(BCnHalf half1, BCnHalf half2) { return half1.Value != half2.Value; }
		/// <summary>
		/// Returns a value indicating whether a specified System.BCnHalf is less than another specified System.BCnHalf.
		/// </summary>
		/// <param name="half1">A System.BCnHalf.</param>
		/// <param name="half2">A System.BCnHalf.</param>
		/// <returns>true if half1 is less than half1; otherwise, false.</returns>
		public static bool operator <(BCnHalf half1, BCnHalf half2) { return half1 < (float)half2; }
		/// <summary>
		/// Returns a value indicating whether a specified System.BCnHalf is greater than another specified System.BCnHalf.
		/// </summary>
		/// <param name="half1">A System.BCnHalf.</param>
		/// <param name="half2">A System.BCnHalf.</param>
		/// <returns>true if half1 is greater than half2; otherwise, false.</returns>
		public static bool operator >(BCnHalf half1, BCnHalf half2) { return half1 > (float)half2; }
		/// <summary>
		/// Returns a value indicating whether a specified System.BCnHalf is less than or equal to another specified System.BCnHalf.
		/// </summary>
		/// <param name="half1">A System.BCnHalf.</param>
		/// <param name="half2">A System.BCnHalf.</param>
		/// <returns>true if half1 is less than or equal to half2; otherwise, false.</returns>
		public static bool operator <=(BCnHalf half1, BCnHalf half2) { return (half1 == half2) || (half1 < half2); }
		/// <summary>
		/// Returns a value indicating whether a specified System.BCnHalf is greater than or equal to another specified System.BCnHalf.
		/// </summary>
		/// <param name="half1">A System.BCnHalf.</param>
		/// <param name="half2">A System.BCnHalf.</param>
		/// <returns>true if half1 is greater than or equal to half2; otherwise, false.</returns>
		public static bool operator >=(BCnHalf half1, BCnHalf half2) { return (half1 == half2) || (half1 > half2); }
		#endregion

		#region Type casting operators
		/// <summary>
		/// Converts an 8-bit unsigned integer to a System.BCnHalf.
		/// </summary>
		/// <param name="value">An 8-bit unsigned integer.</param>
		/// <returns>A System.BCnHalf that represents the converted 8-bit unsigned integer.</returns>
		public static implicit operator BCnHalf(byte value) { return new BCnHalf((float)value); }
		/// <summary>
		/// Converts a 16-bit signed integer to a System.BCnHalf.
		/// </summary>
		/// <param name="value">A 16-bit signed integer.</param>
		/// <returns>A System.BCnHalf that represents the converted 16-bit signed integer.</returns>
		public static implicit operator BCnHalf(short value) { return new BCnHalf((float)value); }
		/// <summary>
		/// Converts a Unicode character to a System.BCnHalf.
		/// </summary>
		/// <param name="value">A Unicode character.</param>
		/// <returns>A System.BCnHalf that represents the converted Unicode character.</returns>
		public static implicit operator BCnHalf(char value) { return new BCnHalf((float)value); }
		/// <summary>
		/// Converts a 32-bit signed integer to a System.BCnHalf.
		/// </summary>
		/// <param name="value">A 32-bit signed integer.</param>
		/// <returns>A System.BCnHalf that represents the converted 32-bit signed integer.</returns>
		public static implicit operator BCnHalf(int value) { return new BCnHalf((float)value); }
		/// <summary>
		/// Converts a 64-bit signed integer to a System.BCnHalf.
		/// </summary>
		/// <param name="value">A 64-bit signed integer.</param>
		/// <returns>A System.BCnHalf that represents the converted 64-bit signed integer.</returns>
		public static implicit operator BCnHalf(long value) { return new BCnHalf((float)value); }
		/// <summary>
		/// Converts a single-precision floating-point number to a System.BCnHalf.
		/// </summary>
		/// <param name="value">A single-precision floating-point number.</param>
		/// <returns>A System.BCnHalf that represents the converted single-precision floating point number.</returns>
		public static explicit operator BCnHalf(float value) { return new BCnHalf(value); }
		/// <summary>
		/// Converts a double-precision floating-point number to a System.BCnHalf.
		/// </summary>
		/// <param name="value">A double-precision floating-point number.</param>
		/// <returns>A System.BCnHalf that represents the converted double-precision floating point number.</returns>
		public static explicit operator BCnHalf(double value) { return new BCnHalf((float)value); }
		/// <summary>
		/// Converts a decimal number to a System.BCnHalf.
		/// </summary>
		/// <param name="value">decimal number</param>
		/// <returns>A System.BCnHalf that represents the converted decimal number.</returns>
		public static explicit operator BCnHalf(decimal value) { return new BCnHalf((float)value); }
		/// <summary>
		/// Converts a System.BCnHalf to an 8-bit unsigned integer.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>An 8-bit unsigned integer that represents the converted System.BCnHalf.</returns>
		public static explicit operator byte(BCnHalf value) { return (byte)(float)value; }
		/// <summary>
		/// Converts a System.BCnHalf to a Unicode character.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>A Unicode character that represents the converted System.BCnHalf.</returns>
		public static explicit operator char(BCnHalf value) { return (char)(float)value; }
		/// <summary>
		/// Converts a System.BCnHalf to a 16-bit signed integer.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>A 16-bit signed integer that represents the converted System.BCnHalf.</returns>
		public static explicit operator short(BCnHalf value) { return (short)(float)value; }
		/// <summary>
		/// Converts a System.BCnHalf to a 32-bit signed integer.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>A 32-bit signed integer that represents the converted System.BCnHalf.</returns>
		public static explicit operator int(BCnHalf value) { return (int)(float)value; }
		/// <summary>
		/// Converts a System.BCnHalf to a 64-bit signed integer.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>A 64-bit signed integer that represents the converted System.BCnHalf.</returns>
		public static explicit operator long(BCnHalf value) { return (long)(float)value; }
		/// <summary>
		/// Converts a System.BCnHalf to a single-precision floating-point number.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>A single-precision floating-point number that represents the converted System.BCnHalf.</returns>
		public static implicit operator float(BCnHalf value) { return HalfHelper.HalfToSingle(value); }
		/// <summary>
		/// Converts a System.BCnHalf to a double-precision floating-point number.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>A double-precision floating-point number that represents the converted System.BCnHalf.</returns>
		public static implicit operator double(BCnHalf value) { return (float)value; }
		/// <summary>
		/// Converts a System.BCnHalf to a decimal number.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>A decimal number that represents the converted System.BCnHalf.</returns>
		public static explicit operator decimal(BCnHalf value) { return (decimal)(float)value; }
		/// <summary>
		/// Converts an 8-bit signed integer to a System.BCnHalf.
		/// </summary>
		/// <param name="value">An 8-bit signed integer.</param>
		/// <returns>A System.BCnHalf that represents the converted 8-bit signed integer.</returns>
		public static implicit operator BCnHalf(sbyte value) { return new BCnHalf((float)value); }
		/// <summary>
		/// Converts a 16-bit unsigned integer to a System.BCnHalf.
		/// </summary>
		/// <param name="value">A 16-bit unsigned integer.</param>
		/// <returns>A System.BCnHalf that represents the converted 16-bit unsigned integer.</returns>
		public static implicit operator BCnHalf(ushort value) { return new BCnHalf((float)value); }
		/// <summary>
		/// Converts a 32-bit unsigned integer to a System.BCnHalf.
		/// </summary>
		/// <param name="value">A 32-bit unsigned integer.</param>
		/// <returns>A System.BCnHalf that represents the converted 32-bit unsigned integer.</returns>
		public static implicit operator BCnHalf(uint value) { return new BCnHalf((float)value); }
		/// <summary>
		/// Converts a 64-bit unsigned integer to a System.BCnHalf.
		/// </summary>
		/// <param name="value">A 64-bit unsigned integer.</param>
		/// <returns>A System.BCnHalf that represents the converted 64-bit unsigned integer.</returns>
		public static implicit operator BCnHalf(ulong value) { return new BCnHalf((float)value); }
		/// <summary>
		/// Converts a System.BCnHalf to an 8-bit signed integer.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>An 8-bit signed integer that represents the converted System.BCnHalf.</returns>
		public static explicit operator sbyte(BCnHalf value) { return (sbyte)(float)value; }
		/// <summary>
		/// Converts a System.BCnHalf to a 16-bit unsigned integer.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>A 16-bit unsigned integer that represents the converted System.BCnHalf.</returns>
		public static explicit operator ushort(BCnHalf value) { return (ushort)(float)value; }
		/// <summary>
		/// Converts a System.BCnHalf to a 32-bit unsigned integer.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>A 32-bit unsigned integer that represents the converted System.BCnHalf.</returns>
		public static explicit operator uint(BCnHalf value) { return (uint)(float)value; }
		/// <summary>
		/// Converts a System.BCnHalf to a 64-bit unsigned integer.
		/// </summary>
		/// <param name="value">A System.BCnHalf to convert.</param>
		/// <returns>A 64-bit unsigned integer that represents the converted System.BCnHalf.</returns>
		public static explicit operator ulong(BCnHalf value) { return (ulong)(float)value; }
		#endregion

		/// <summary>
		/// Compares this instance to a specified System.BCnHalf object.
		/// </summary>
		/// <param name="other">A System.BCnHalf object.</param>
		/// <returns>
		/// A signed number indicating the relative values of this instance and value.
		/// Return Value Meaning Less than zero This instance is less than value. Zero
		/// This instance is equal to value. Greater than zero This instance is greater than value.
		/// </returns>
		public int CompareTo(BCnHalf other)
		{
			var result = 0;
			if (this < other)
			{
				result = -1;
			}
			else if (this > other)
			{
				result = 1;
			}
			else if (this != other)
			{
				if (!IsNaN(this))
				{
					result = 1;
				}
				else if (!IsNaN(other))
				{
					result = -1;
				}
			}

			return result;
		}
		/// <summary>
		/// Compares this instance to a specified System.Object.
		/// </summary>
		/// <param name="obj">An System.Object or null.</param>
		/// <returns>
		/// A signed number indicating the relative values of this instance and value.
		/// Return Value Meaning Less than zero This instance is less than value. Zero
		/// This instance is equal to value. Greater than zero This instance is greater
		/// than value. -or- value is null.
		/// </returns>
		/// <exception cref="System.ArgumentException">value is not a System.BCnHalf</exception>
		public int CompareTo(object obj)
		{
			var result = 0;
			if (obj == null)
			{
				result = 1;
			}
			else
			{
				if (obj is BCnHalf)
				{
					result = CompareTo((BCnHalf)obj);
				}
				else
				{
					throw new ArgumentException("Object must be of type BCnHalf.");
				}
			}

			return result;
		}
		/// <summary>
		/// Returns a value indicating whether this instance and a specified System.BCnHalf object represent the same value.
		/// </summary>
		/// <param name="other">A System.BCnHalf object to compare to this instance.</param>
		/// <returns>true if value is equal to this instance; otherwise, false.</returns>
		public bool Equals(BCnHalf other)
		{
			return ((other == this) || (IsNaN(other) && IsNaN(this)));
		}
		/// <summary>
		/// Returns a value indicating whether this instance and a specified System.Object
		/// represent the same type and value.
		/// </summary>
		/// <param name="obj">An System.Object.</param>
		/// <returns>true if value is a System.BCnHalf and equal to this instance; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			var result = false;
			if (obj is BCnHalf)
			{
				var half = (BCnHalf)obj;
				if ((half == this) || (IsNaN(half) && IsNaN(this)))
				{
					result = true;
				}
			}

			return result;
		}
		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>A 32-bit signed integer hash code.</returns>
		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
		/// <summary>
		/// Returns the System.TypeCode for value type System.BCnHalf.
		/// </summary>
		/// <returns>The enumerated constant (TypeCode)255.</returns>
		public TypeCode GetTypeCode()
		{
			return (TypeCode)255;
		}

		#region BitConverter & Math methods for BCnHalf
		/// <summary>
		/// Returns the specified half-precision floating point value as an array of bytes.
		/// </summary>
		/// <param name="value">The number to convert.</param>
		/// <returns>An array of bytes with length 2.</returns>
		public static byte[] GetBytes(BCnHalf value)
		{
			return BitConverter.GetBytes(value.Value);
		}
		/// <summary>
		/// Converts the value of a specified instance of System.BCnHalf to its equivalent binary representation.
		/// </summary>
		/// <param name="value">A System.BCnHalf value.</param>
		/// <returns>A 16-bit unsigned integer that contain the binary representation of value.</returns>        
		public static ushort GetBits(BCnHalf value)
		{
			return value.Value;
		}
		/// <summary>
		/// Returns a half-precision floating point number converted from two bytes
		/// at a specified position in a byte array.
		/// </summary>
		/// <param name="value">An array of bytes.</param>
		/// <param name="startIndex">The starting position within value.</param>
		/// <returns>A half-precision floating point number formed by two bytes beginning at startIndex.</returns>
		/// <exception cref="System.ArgumentException">
		/// startIndex is greater than or equal to the length of value minus 1, and is
		/// less than or equal to the length of value minus 1.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">value is null.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">startIndex is less than zero or greater than the length of value minus 1.</exception>
		public static BCnHalf ToHalf(byte[] value, int startIndex)
		{
			return ToHalf((ushort)BitConverter.ToInt16(value, startIndex));
		}
		/// <summary>
		/// Returns a half-precision floating point number converted from its binary representation.
		/// </summary>
		/// <param name="bits">Binary representation of System.BCnHalf value</param>
		/// <returns>A half-precision floating point number formed by its binary representation.</returns>
		public static BCnHalf ToHalf(ushort bits)
		{
			return new BCnHalf { Value = bits };
		}

		/// <summary>
		/// Returns a value indicating the sign of a half-precision floating-point number.
		/// </summary>
		/// <param name="value">A signed number.</param>
		/// <returns>
		/// A number indicating the sign of value. Number Description -1 value is less
		/// than zero. 0 value is equal to zero. 1 value is greater than zero.
		/// </returns>
		/// <exception cref="System.ArithmeticException">value is equal to System.BCnHalf.NaN.</exception>
		public static int Sign(BCnHalf value)
		{
			if (value < 0)
			{
				return -1;
			}
			else if (value > 0)
			{
				return 1;
			}
			else
			{
				if (value != 0)
				{
					throw new ArithmeticException("Function does not accept floating point Not-a-Number values.");
				}
			}

			return 0;
		}
		/// <summary>
		/// Returns the absolute value of a half-precision floating-point number.
		/// </summary>
		/// <param name="value">A number in the range System.BCnHalf.MinValue ≤ value ≤ System.BCnHalf.MaxValue.</param>
		/// <returns>A half-precision floating-point number, x, such that 0 ≤ x ≤System.BCnHalf.MaxValue.</returns>
		public static BCnHalf Abs(BCnHalf value)
		{
			return HalfHelper.Abs(value);
		}
		/// <summary>
		/// Returns the larger of two half-precision floating-point numbers.
		/// </summary>
		/// <param name="value1">The first of two half-precision floating-point numbers to compare.</param>
		/// <param name="value2">The second of two half-precision floating-point numbers to compare.</param>
		/// <returns>
		/// Parameter value1 or value2, whichever is larger. If value1, or value2, or both val1
		/// and value2 are equal to System.BCnHalf.NaN, System.BCnHalf.NaN is returned.
		/// </returns>
		public static BCnHalf Max(BCnHalf value1, BCnHalf value2)
		{
			return (value1 < value2) ? value2 : value1;
		}
		/// <summary>
		/// Returns the smaller of two half-precision floating-point numbers.
		/// </summary>
		/// <param name="value1">The first of two half-precision floating-point numbers to compare.</param>
		/// <param name="value2">The second of two half-precision floating-point numbers to compare.</param>
		/// <returns>
		/// Parameter value1 or value2, whichever is smaller. If value1, or value2, or both val1
		/// and value2 are equal to System.BCnHalf.NaN, System.BCnHalf.NaN is returned.
		/// </returns>
		public static BCnHalf Min(BCnHalf value1, BCnHalf value2)
		{
			return (value1 < value2) ? value1 : value2;
		}
		#endregion

		/// <summary>
		/// Returns a value indicating whether the specified number evaluates to not a number (System.BCnHalf.NaN).
		/// </summary>
		/// <param name="half">A half-precision floating-point number.</param>
		/// <returns>true if value evaluates to not a number (System.BCnHalf.NaN); otherwise, false.</returns>
		public static bool IsNaN(BCnHalf half)
		{
			return HalfHelper.IsNaN(half);
		}
		/// <summary>
		/// Returns a value indicating whether the specified number evaluates to negative or positive infinity.
		/// </summary>
		/// <param name="half">A half-precision floating-point number.</param>
		/// <returns>true if half evaluates to System.BCnHalf.PositiveInfinity or System.BCnHalf.NegativeInfinity; otherwise, false.</returns>
		public static bool IsInfinity(BCnHalf half)
		{
			return HalfHelper.IsInfinity(half);
		}
		/// <summary>
		/// Returns a value indicating whether the specified number evaluates to negative infinity.
		/// </summary>
		/// <param name="half">A half-precision floating-point number.</param>
		/// <returns>true if half evaluates to System.BCnHalf.NegativeInfinity; otherwise, false.</returns>
		public static bool IsNegativeInfinity(BCnHalf half)
		{
			return HalfHelper.IsNegativeInfinity(half);
		}
		/// <summary>
		/// Returns a value indicating whether the specified number evaluates to positive infinity.
		/// </summary>
		/// <param name="half">A half-precision floating-point number.</param>
		/// <returns>true if half evaluates to System.BCnHalf.PositiveInfinity; otherwise, false.</returns>
		public static bool IsPositiveInfinity(BCnHalf half)
		{
			return HalfHelper.IsPositiveInfinity(half);
		}

		#region String operations (Parse and ToString)
		/// <summary>
		/// Converts the string representation of a number to its System.BCnHalf equivalent.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <returns>The System.BCnHalf number equivalent to the number contained in value.</returns>
		/// <exception cref="System.ArgumentNullException">value is null.</exception>
		/// <exception cref="System.FormatException">value is not in the correct format.</exception>
		/// <exception cref="System.OverflowException">value represents a number less than System.BCnHalf.MinValue or greater than System.BCnHalf.MaxValue.</exception>
		public static BCnHalf Parse(string value)
		{
			return (BCnHalf)float.Parse(value, CultureInfo.InvariantCulture);
		}
		/// <summary>
		/// Converts the string representation of a number to its System.BCnHalf equivalent 
		/// using the specified culture-specific format information.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="provider">An System.IFormatProvider that supplies culture-specific parsing information about value.</param>
		/// <returns>The System.BCnHalf number equivalent to the number contained in s as specified by provider.</returns>
		/// <exception cref="System.ArgumentNullException">value is null.</exception>
		/// <exception cref="System.FormatException">value is not in the correct format.</exception>
		/// <exception cref="System.OverflowException">value represents a number less than System.BCnHalf.MinValue or greater than System.BCnHalf.MaxValue.</exception>
		public static BCnHalf Parse(string value, IFormatProvider provider)
		{
			return (BCnHalf)float.Parse(value, provider);
		}
		/// <summary>
		/// Converts the string representation of a number in a specified style to its System.BCnHalf equivalent.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="style">
		/// A bitwise combination of System.Globalization.NumberStyles values that indicates
		/// the style elements that can be present in value. A typical value to specify is
		/// System.Globalization.NumberStyles.Number.
		/// </param>
		/// <returns>The System.BCnHalf number equivalent to the number contained in s as specified by style.</returns>
		/// <exception cref="System.ArgumentNullException">value is null.</exception>
		/// <exception cref="System.ArgumentException">
		/// style is not a System.Globalization.NumberStyles value. -or- style is the
		/// System.Globalization.NumberStyles.AllowHexSpecifier value.
		/// </exception>
		/// <exception cref="System.FormatException">value is not in the correct format.</exception>
		/// <exception cref="System.OverflowException">value represents a number less than System.BCnHalf.MinValue or greater than System.BCnHalf.MaxValue.</exception>
		public static BCnHalf Parse(string value, NumberStyles style)
		{
			return (BCnHalf)float.Parse(value, style, CultureInfo.InvariantCulture);
		}
		/// <summary>
		/// Converts the string representation of a number to its System.BCnHalf equivalent 
		/// using the specified style and culture-specific format.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="style">
		/// A bitwise combination of System.Globalization.NumberStyles values that indicates
		/// the style elements that can be present in value. A typical value to specify is 
		/// System.Globalization.NumberStyles.Number.
		/// </param>
		/// <param name="provider">An System.IFormatProvider object that supplies culture-specific information about the format of value.</param>
		/// <returns>The System.BCnHalf number equivalent to the number contained in s as specified by style and provider.</returns>
		/// <exception cref="System.ArgumentNullException">value is null.</exception>
		/// <exception cref="System.ArgumentException">
		/// style is not a System.Globalization.NumberStyles value. -or- style is the
		/// System.Globalization.NumberStyles.AllowHexSpecifier value.
		/// </exception>
		/// <exception cref="System.FormatException">value is not in the correct format.</exception>
		/// <exception cref="System.OverflowException">value represents a number less than System.BCnHalf.MinValue or greater than System.BCnHalf.MaxValue.</exception>
		public static BCnHalf Parse(string value, NumberStyles style, IFormatProvider provider)
		{
			return (BCnHalf)float.Parse(value, style, provider);
		}
		/// <summary>
		/// Converts the string representation of a number to its System.BCnHalf equivalent.
		/// A return value indicates whether the conversion succeeded or failed.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="result">
		/// When this method returns, contains the System.BCnHalf number that is equivalent
		/// to the numeric value contained in value, if the conversion succeeded, or is zero
		/// if the conversion failed. The conversion fails if the s parameter is null,
		/// is not a number in a valid format, or represents a number less than System.BCnHalf.MinValue
		/// or greater than System.BCnHalf.MaxValue. This parameter is passed uninitialized.
		/// </param>
		/// <returns>true if s was converted successfully; otherwise, false.</returns>
		public static bool TryParse(string value, out BCnHalf result)
		{
			float f;
			if (float.TryParse(value, out f))
			{
				result = (BCnHalf)f;
				return true;
			}

			result = new BCnHalf();
			return false;
		}
		/// <summary>
		/// Converts the string representation of a number to its System.BCnHalf equivalent
		/// using the specified style and culture-specific format. A return value indicates
		/// whether the conversion succeeded or failed.
		/// </summary>
		/// <param name="value">The string representation of the number to convert.</param>
		/// <param name="style">
		/// A bitwise combination of System.Globalization.NumberStyles values that indicates
		/// the permitted format of value. A typical value to specify is System.Globalization.NumberStyles.Number.
		/// </param>
		/// <param name="provider">An System.IFormatProvider object that supplies culture-specific parsing information about value.</param>
		/// <param name="result">
		/// When this method returns, contains the System.BCnHalf number that is equivalent
		/// to the numeric value contained in value, if the conversion succeeded, or is zero
		/// if the conversion failed. The conversion fails if the s parameter is null,
		/// is not in a format compliant with style, or represents a number less than
		/// System.BCnHalf.MinValue or greater than System.BCnHalf.MaxValue. This parameter is passed uninitialized.
		/// </param>
		/// <returns>true if s was converted successfully; otherwise, false.</returns>
		/// <exception cref="System.ArgumentException">
		/// style is not a System.Globalization.NumberStyles value. -or- style 
		/// is the System.Globalization.NumberStyles.AllowHexSpecifier value.
		/// </exception>
		public static bool TryParse(string value, NumberStyles style, IFormatProvider provider, out BCnHalf result)
		{
			var parseResult = false;
			float f;
			if (float.TryParse(value, style, provider, out f))
			{
				result = (BCnHalf)f;
				parseResult = true;
			}
			else
			{
				result = new BCnHalf();
			}

			return parseResult;
		}
		/// <summary>
		/// Converts the numeric value of this instance to its equivalent string representation.
		/// </summary>
		/// <returns>A string that represents the value of this instance.</returns>
		public override string ToString()
		{
			return ((float)this).ToString(CultureInfo.InvariantCulture);
		}
		/// <summary>
		/// Converts the numeric value of this instance to its equivalent string representation
		/// using the specified culture-specific format information.
		/// </summary>
		/// <param name="formatProvider">An System.IFormatProvider that supplies culture-specific formatting information.</param>
		/// <returns>The string representation of the value of this instance as specified by provider.</returns>
		public string ToString(IFormatProvider formatProvider)
		{
			return ((float)this).ToString(formatProvider);
		}
		/// <summary>
		/// Converts the numeric value of this instance to its equivalent string representation, using the specified format.
		/// </summary>
		/// <param name="format">A numeric format string.</param>
		/// <returns>The string representation of the value of this instance as specified by format.</returns>
		public string ToString(string format)
		{
			return ((float)this).ToString(format, CultureInfo.InvariantCulture);
		}
		/// <summary>
		/// Converts the numeric value of this instance to its equivalent string representation 
		/// using the specified format and culture-specific format information.
		/// </summary>
		/// <param name="format">A numeric format string.</param>
		/// <param name="formatProvider">An System.IFormatProvider that supplies culture-specific formatting information.</param>
		/// <returns>The string representation of the value of this instance as specified by format and provider.</returns>
		/// <exception cref="System.FormatException">format is invalid.</exception>
		public string ToString(string format, IFormatProvider formatProvider)
		{
			return ((float)this).ToString(format, formatProvider);
		}
		#endregion

		#region IConvertible Members
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return this;
		}
		TypeCode IConvertible.GetTypeCode()
		{
			return GetTypeCode();
		}
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return Convert.ToBoolean(this);
		}
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return Convert.ToByte(this);
		}
		char IConvertible.ToChar(IFormatProvider provider)
		{
			throw new InvalidCastException(string.Format(CultureInfo.CurrentCulture, "Invalid cast from '{0}' to '{1}'.", "BCnHalf", "Char"));
		}
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException(string.Format(CultureInfo.CurrentCulture, "Invalid cast from '{0}' to '{1}'.", "BCnHalf", "DateTime"));
		}
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return Convert.ToDecimal(this);
		}
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return Convert.ToDouble(this);
		}
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return Convert.ToInt16(this);
		}
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return Convert.ToInt32(this);
		}
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return Convert.ToInt64(this);
		}
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return Convert.ToSByte(this);
		}
		string IConvertible.ToString(IFormatProvider provider)
		{
			return Convert.ToString(this, CultureInfo.InvariantCulture);
		}
		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			return (((float)this) as IConvertible).ToType(conversionType, provider);
		}
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return Convert.ToUInt16(this);
		}
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return Convert.ToUInt32(this);
		}
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return Convert.ToUInt64(this);
		}
		#endregion
	}
}

namespace Electron2DBlockCodecs.Shared
{
	/// <summary>
	/// Helper class for BCnHalf conversions and some low level operations.
	/// This class is internally used in the BCnHalf class.
	/// </summary>
	/// <remarks>
	/// References:
	///     - Code retrieved from http://sourceforge.net/p/csharp-half/code/HEAD/tree/ on 2015-12-04
	///     - Fast BCnHalf Float Conversions, Jeroen van der Zijp, link: http://www.fox-toolkit.org/ftp/fasthalffloatconversion.pdf
	/// </remarks>
	internal static class HalfHelper
	{
		private static readonly uint[] MantissaTable = GenerateMantissaTable();
		private static readonly uint[] ExponentTable = GenerateExponentTable();
		private static readonly ushort[] OffsetTable = GenerateOffsetTable();
		private static readonly ushort[] BaseTable = GenerateBaseTable();
		private static readonly sbyte[] ShiftTable = GenerateShiftTable();

		// Transforms the subnormal representation to a normalized one. 
		private static uint ConvertMantissa(int i)
		{
			var m = (uint)(i << 13); // Zero pad mantissa bits
			uint e = 0; // Zero exponent

			// While not normalized
			while ((m & 0x00800000) == 0)
			{
				e -= 0x00800000; // Decrement exponent (1<<23)
				m <<= 1; // Shift mantissa                
			}
			m &= unchecked((uint)~0x00800000); // Clear leading 1 bit
			e += 0x38800000; // Adjust bias ((127-14)<<23)
			return m | e; // Return combined number
		}

		private static uint[] GenerateMantissaTable()
		{
			var mantissaTable = new uint[2048];
			mantissaTable[0] = 0;
			for (var i = 1; i < 1024; i++)
			{
				mantissaTable[i] = ConvertMantissa(i);
			}
			for (var i = 1024; i < 2048; i++)
			{
				mantissaTable[i] = (uint)(0x38000000 + ((i - 1024) << 13));
			}

			return mantissaTable;
		}
		private static uint[] GenerateExponentTable()
		{
			var exponentTable = new uint[64];
			exponentTable[0] = 0;
			for (var i = 1; i < 31; i++)
			{
				exponentTable[i] = (uint)(i << 23);
			}
			exponentTable[31] = 0x47800000;
			exponentTable[32] = 0x80000000;
			for (var i = 33; i < 63; i++)
			{
				exponentTable[i] = (uint)(0x80000000 + ((i - 32) << 23));
			}
			exponentTable[63] = 0xc7800000;

			return exponentTable;
		}
		private static ushort[] GenerateOffsetTable()
		{
			var offsetTable = new ushort[64];
			offsetTable[0] = 0;
			for (var i = 1; i < 32; i++)
			{
				offsetTable[i] = 1024;
			}
			offsetTable[32] = 0;
			for (var i = 33; i < 64; i++)
			{
				offsetTable[i] = 1024;
			}

			return offsetTable;
		}
		private static ushort[] GenerateBaseTable()
		{
			var baseTable = new ushort[512];
			for (var i = 0; i < 256; ++i)
			{
				var e = (sbyte)(127 - i);
				if (e > 24)
				{ // Very small numbers map to zero
					baseTable[i | 0x000] = 0x0000;
					baseTable[i | 0x100] = 0x8000;
				}
				else if (e > 14)
				{ // Small numbers map to denorms
					baseTable[i | 0x000] = (ushort)(0x0400 >> (18 + e));
					baseTable[i | 0x100] = (ushort)((0x0400 >> (18 + e)) | 0x8000);
				}
				else if (e >= -15)
				{ // Normal numbers just lose precision
					baseTable[i | 0x000] = (ushort)((15 - e) << 10);
					baseTable[i | 0x100] = (ushort)(((15 - e) << 10) | 0x8000);
				}
				else if (e > -128)
				{ // Large numbers map to Infinity
					baseTable[i | 0x000] = 0x7c00;
					baseTable[i | 0x100] = 0xfc00;
				}
				else
				{ // Infinity and NaN's stay Infinity and NaN's
					baseTable[i | 0x000] = 0x7c00;
					baseTable[i | 0x100] = 0xfc00;
				}
			}

			return baseTable;
		}
		private static sbyte[] GenerateShiftTable()
		{
			var shiftTable = new sbyte[512];
			for (var i = 0; i < 256; ++i)
			{
				var e = (sbyte)(127 - i);
				if (e > 24)
				{ // Very small numbers map to zero
					shiftTable[i | 0x000] = 24;
					shiftTable[i | 0x100] = 24;
				}
				else if (e > 14)
				{ // Small numbers map to denorms
					shiftTable[i | 0x000] = (sbyte)(e - 1);
					shiftTable[i | 0x100] = (sbyte)(e - 1);
				}
				else if (e >= -15)
				{ // Normal numbers just lose precision
					shiftTable[i | 0x000] = 13;
					shiftTable[i | 0x100] = 13;
				}
				else if (e > -128)
				{ // Large numbers map to Infinity
					shiftTable[i | 0x000] = 24;
					shiftTable[i | 0x100] = 24;
				}
				else
				{ // Infinity and NaN's stay Infinity and NaN's
					shiftTable[i | 0x000] = 13;
					shiftTable[i | 0x100] = 13;
				}
			}

			return shiftTable;
		}

		public static unsafe float HalfToSingle(BCnHalf half)
		{
			var result = MantissaTable[OffsetTable[half.Value >> 10] + (half.Value & 0x3ff)] + ExponentTable[half.Value >> 10];
			return *(float*)&result;
		}
		public static unsafe BCnHalf SingleToHalf(float single)
		{
			var value = *(uint*)&single;

			var result = (ushort)(BaseTable[(value >> 23) & 0x1ff] + ((value & 0x007fffff) >> ShiftTable[value >> 23]));
			return BCnHalf.ToHalf(result);
		}

		public static BCnHalf Negate(BCnHalf half)
		{
			return BCnHalf.ToHalf((ushort)(half.Value ^ 0x8000));
		}
		public static BCnHalf Abs(BCnHalf half)
		{
			return BCnHalf.ToHalf((ushort)(half.Value & 0x7fff));
		}

		public static bool IsNaN(BCnHalf half)
		{
			return (half.Value & 0x7fff) > 0x7c00;
		}
		public static bool IsInfinity(BCnHalf half)
		{
			return (half.Value & 0x7fff) == 0x7c00;
		}
		public static bool IsPositiveInfinity(BCnHalf half)
		{
			return half.Value == 0x7c00;
		}
		public static bool IsNegativeInfinity(BCnHalf half)
		{
			return half.Value == 0xfc00;
		}
	}
}
