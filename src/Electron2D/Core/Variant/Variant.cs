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
using System.Globalization;
using System.Runtime.CompilerServices;

using VariantArray = Electron2D.Collections.Array;
using VariantDictionary = Electron2D.Collections.Dictionary;

namespace Electron2D;

/// <summary>
/// Stores one value from the closed Electron2D 0.1.0 Preview Variant type set.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="Variant"/> is the Electron2D dynamic value carrier used by
/// engine APIs that cannot be represented as a single static C# type. It is a
/// value type; <c>default(Variant)</c> represents <see cref="Type.Nil"/>.
/// </para>
///
/// <para>
/// Electron2D 0.1.0 Preview intentionally supports a closed preview set:
/// primitive values, 2D math types, identity handles, <see cref="Object"/>
/// instances, <see cref="Callable"/>, and Electron2D collections. 3D types,
/// packed arrays, <c>Signal</c>, editor-only values, and arbitrary CLR objects
/// are not part of this release contract.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Independent Variant values are safe to read from any thread. If a Variant
/// stores a mutable collection, thread safety follows that collection instance.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
public readonly struct Variant : IEquatable<Variant>
{
    private readonly Type _variantType;
    private readonly object? _value;

    private Variant(Type variantType, object? value)
    {
        _variantType = variantType;
        _value = value;
    }

    /// <summary>
    /// Identifies the type currently stored in a <see cref="Variant"/>.
    /// </summary>
    ///
    /// <remarks>
    /// The list is closed for Electron2D 0.1.0 Preview. Values not listed here
    /// are not supported by this version of the runtime.
    /// </remarks>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    public enum Type
    {
        /// <summary>
        /// Represents a null-like value.
        /// </summary>
        Nil = 0,

        /// <summary>
        /// Represents a Boolean value.
        /// </summary>
        Bool = 1,

        /// <summary>
        /// Represents a signed 64-bit integer value.
        /// </summary>
        Int = 2,

        /// <summary>
        /// Represents a 64-bit floating-point value.
        /// </summary>
        Float = 3,

        /// <summary>
        /// Represents a UTF-16 .NET string value.
        /// </summary>
        String = 4,

        /// <summary>
        /// Represents a <see cref="Vector2"/> value.
        /// </summary>
        Vector2 = 5,

        /// <summary>
        /// Represents a <see cref="Vector2I"/> value.
        /// </summary>
        Vector2I = 6,

        /// <summary>
        /// Represents a <see cref="Rect2"/> value.
        /// </summary>
        Rect2 = 7,

        /// <summary>
        /// Represents a <see cref="Rect2I"/> value.
        /// </summary>
        Rect2I = 8,

        /// <summary>
        /// Represents a <see cref="Transform2D"/> value.
        /// </summary>
        Transform2D = 9,

        /// <summary>
        /// Represents a <see cref="Color"/> value.
        /// </summary>
        Color = 10,

        /// <summary>
        /// Represents a <see cref="StringName"/> value.
        /// </summary>
        StringName = 11,

        /// <summary>
        /// Represents a <see cref="NodePath"/> value.
        /// </summary>
        NodePath = 12,

        /// <summary>
        /// Represents a <see cref="Rid"/> value.
        /// </summary>
        Rid = 13,

        /// <summary>
        /// Represents an <see cref="Electron2D.Object"/> or derived instance.
        /// </summary>
        Object = 14,

        /// <summary>
        /// Represents a <see cref="Callable"/> value.
        /// </summary>
        Callable = 15,

        /// <summary>
        /// Represents an <see cref="VariantDictionary"/> value.
        /// </summary>
        Dictionary = 16,

        /// <summary>
        /// Represents an <see cref="VariantArray"/> value.
        /// </summary>
        Array = 17
    }

    /// <summary>
    /// Gets the type stored in this variant.
    /// </summary>
    public Type VariantType => _variantType;

    /// <summary>
    /// Gets the boxed value stored in this variant.
    /// </summary>
    ///
    /// <remarks>
    /// Numeric values are normalized before boxing: integer-compatible values
    /// are boxed as <see cref="long"/> and floating-point values are boxed as
    /// <see cref="double"/>.
    /// </remarks>
    public object? Obj => _value;

    /// <summary>
    /// Creates a Variant from any supported Electron2D 0.1.0 Preview value.
    /// </summary>
    ///
    /// <param name="value">The value to store.</param>
    ///
    /// <returns>A Variant containing <paramref name="value"/>.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is not part of the closed Variant
    /// type set.
    /// </exception>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when an unsigned 64-bit integer cannot be represented by the
    /// signed 64-bit Variant integer type.
    /// </exception>
    public static Variant CreateFrom(object? value)
    {
        return value switch
        {
            null => default,
            Variant variant => variant,
            bool boolValue => new Variant(Type.Bool, boolValue),
            sbyte sbyteValue => FromInt64(sbyteValue),
            byte byteValue => FromInt64(byteValue),
            short shortValue => FromInt64(shortValue),
            ushort ushortValue => FromInt64(ushortValue),
            int intValue => FromInt64(intValue),
            uint uintValue => FromInt64(uintValue),
            long longValue => FromInt64(longValue),
            ulong ulongValue => FromUInt64(ulongValue),
            float floatValue => FromDouble(floatValue),
            double doubleValue => FromDouble(doubleValue),
            string stringValue => new Variant(Type.String, stringValue),
            Enum enumValue => FromEnum(enumValue),
            Vector2 vector2Value => new Variant(Type.Vector2, vector2Value),
            Vector2I vector2IValue => new Variant(Type.Vector2I, vector2IValue),
            Rect2 rect2Value => new Variant(Type.Rect2, rect2Value),
            Rect2I rect2IValue => new Variant(Type.Rect2I, rect2IValue),
            Transform2D transform2DValue => new Variant(Type.Transform2D, transform2DValue),
            Color colorValue => new Variant(Type.Color, colorValue),
            StringName stringNameValue => new Variant(Type.StringName, stringNameValue),
            NodePath nodePathValue => new Variant(Type.NodePath, nodePathValue),
            Rid ridValue => new Variant(Type.Rid, ridValue),
            Callable callableValue => new Variant(Type.Callable, callableValue),
            VariantDictionary dictionaryValue => new Variant(Type.Dictionary, dictionaryValue),
            VariantArray arrayValue => new Variant(Type.Array, arrayValue),
            Object objectValue => new Variant(Type.Object, objectValue),
            _ => throw new ArgumentException(
                $"Type '{value.GetType().FullName}' is not supported by Electron2D.Variant 0.1.0 Preview.",
                nameof(value))
        };
    }

    /// <summary>
    /// Creates a Variant from a supported value while preserving generic call sites.
    /// </summary>
    ///
    /// <param name="value">The value to store.</param>
    /// <typeparam name="T">The static C# type of the value.</typeparam>
    ///
    /// <returns>A Variant containing <paramref name="value"/>.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <typeparamref name="T"/> or the runtime value is not part of
    /// the closed Variant type set.
    /// </exception>
    public static Variant From<T>(T value)
    {
        return CreateFrom(value);
    }

    /// <summary>
    /// Returns whether this variant stores <see cref="Type.Nil"/>.
    /// </summary>
    ///
    /// <returns><c>true</c> for nil variants; otherwise, <c>false</c>.</returns>
    public bool IsNil()
    {
        return _variantType == Type.Nil;
    }

    /// <summary>
    /// Converts this variant to a supported C# type.
    /// </summary>
    ///
    /// <typeparam name="T">The requested C# type.</typeparam>
    ///
    /// <returns>The stored value converted to <typeparamref name="T"/>.</returns>
    ///
    /// <exception cref="InvalidCastException">
    /// Thrown when the stored Variant type does not match <typeparamref name="T"/>.
    /// </exception>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <typeparamref name="T"/> is not a supported Variant target type.
    /// </exception>
    public T As<T>()
    {
        var targetType = typeof(T);

        if (targetType == typeof(Variant))
        {
            return (T)(object)this;
        }

        if (targetType.IsEnum)
        {
            var value = AsInt64();
            return (T)Enum.ToObject(targetType, value);
        }

        if (targetType == typeof(bool))
        {
            return (T)(object)AsBool();
        }

        if (targetType == typeof(sbyte))
        {
            return (T)(object)checked((sbyte)AsInt64());
        }

        if (targetType == typeof(byte))
        {
            return (T)(object)checked((byte)AsInt64());
        }

        if (targetType == typeof(short))
        {
            return (T)(object)checked((short)AsInt64());
        }

        if (targetType == typeof(ushort))
        {
            return (T)(object)checked((ushort)AsInt64());
        }

        if (targetType == typeof(int))
        {
            return (T)(object)AsInt32();
        }

        if (targetType == typeof(uint))
        {
            return (T)(object)checked((uint)AsInt64());
        }

        if (targetType == typeof(long))
        {
            return (T)(object)AsInt64();
        }

        if (targetType == typeof(ulong))
        {
            return (T)(object)checked((ulong)AsInt64());
        }

        if (targetType == typeof(float))
        {
            return (T)(object)checked((float)AsDouble());
        }

        if (targetType == typeof(double))
        {
            return (T)(object)AsDouble();
        }

        if (targetType == typeof(string))
        {
            return (T)(object)AsString();
        }

        if (targetType == typeof(Vector2))
        {
            return (T)(object)AsVector2();
        }

        if (targetType == typeof(Vector2I))
        {
            return (T)(object)AsVector2I();
        }

        if (targetType == typeof(Rect2))
        {
            return (T)(object)AsRect2();
        }

        if (targetType == typeof(Rect2I))
        {
            return (T)(object)AsRect2I();
        }

        if (targetType == typeof(Transform2D))
        {
            return (T)(object)AsTransform2D();
        }

        if (targetType == typeof(Color))
        {
            return (T)(object)AsColor();
        }

        if (targetType == typeof(StringName))
        {
            return (T)(object)AsStringName();
        }

        if (targetType == typeof(NodePath))
        {
            return (T)(object)AsNodePath();
        }

        if (targetType == typeof(Rid))
        {
            return (T)(object)AsRid();
        }

        if (targetType == typeof(Callable))
        {
            return (T)(object)AsCallable();
        }

        if (targetType == typeof(VariantDictionary))
        {
            return (T)(object)AsDictionary();
        }

        if (targetType == typeof(VariantArray))
        {
            return (T)(object)AsArray();
        }

        if (typeof(Object).IsAssignableFrom(targetType))
        {
            var value = AsObject();
            if (value is T typedObject)
            {
                return typedObject;
            }

            throw new InvalidCastException(
                $"Variant stores Object value of type '{value?.GetType().FullName ?? "<null>"}', not '{targetType.FullName}'.");
        }

        throw new ArgumentException(
            $"Type '{targetType.FullName}' is not a supported Electron2D.Variant target type.",
            nameof(T));
    }

    /// <summary>
    /// Reads the stored Boolean value.
    /// </summary>
    ///
    /// <returns>The stored <see cref="bool"/> value.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Bool"/>.</exception>
    public bool AsBool()
    {
        return Expect<bool>(Type.Bool);
    }

    /// <summary>
    /// Reads the stored signed 64-bit integer value.
    /// </summary>
    ///
    /// <returns>The stored <see cref="long"/> value.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Int"/>.</exception>
    public long AsInt64()
    {
        return Expect<long>(Type.Int);
    }

    /// <summary>
    /// Reads the stored integer value as a signed 32-bit integer.
    /// </summary>
    ///
    /// <returns>The stored integer converted to <see cref="int"/>.</returns>
    ///
    /// <exception cref="InvalidCastException">
    /// Thrown when this variant is not <see cref="Type.Int"/> or the value does
    /// not fit in <see cref="int"/>.
    /// </exception>
    public int AsInt32()
    {
        var value = AsInt64();
        if (value < int.MinValue || value > int.MaxValue)
        {
            throw new InvalidCastException($"Variant Int value '{value}' cannot be represented as Int32.");
        }

        return (int)value;
    }

    /// <summary>
    /// Reads the stored 64-bit floating-point value.
    /// </summary>
    ///
    /// <returns>The stored <see cref="double"/> value.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Float"/>.</exception>
    public double AsDouble()
    {
        return Expect<double>(Type.Float);
    }

    /// <summary>
    /// Reads the stored string value.
    /// </summary>
    ///
    /// <returns>The stored string.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.String"/>.</exception>
    public string AsString()
    {
        return Expect<string>(Type.String);
    }

    /// <summary>
    /// Reads the stored <see cref="Vector2"/> value.
    /// </summary>
    ///
    /// <returns>The stored vector.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Vector2"/>.</exception>
    public Vector2 AsVector2()
    {
        return Expect<Vector2>(Type.Vector2);
    }

    /// <summary>
    /// Reads the stored <see cref="Vector2I"/> value.
    /// </summary>
    ///
    /// <returns>The stored integer vector.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Vector2I"/>.</exception>
    public Vector2I AsVector2I()
    {
        return Expect<Vector2I>(Type.Vector2I);
    }

    /// <summary>
    /// Reads the stored <see cref="Rect2"/> value.
    /// </summary>
    ///
    /// <returns>The stored rectangle.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Rect2"/>.</exception>
    public Rect2 AsRect2()
    {
        return Expect<Rect2>(Type.Rect2);
    }

    /// <summary>
    /// Reads the stored <see cref="Rect2I"/> value.
    /// </summary>
    ///
    /// <returns>The stored integer rectangle.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Rect2I"/>.</exception>
    public Rect2I AsRect2I()
    {
        return Expect<Rect2I>(Type.Rect2I);
    }

    /// <summary>
    /// Reads the stored <see cref="Transform2D"/> value.
    /// </summary>
    ///
    /// <returns>The stored transform.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Transform2D"/>.</exception>
    public Transform2D AsTransform2D()
    {
        return Expect<Transform2D>(Type.Transform2D);
    }

    /// <summary>
    /// Reads the stored <see cref="Color"/> value.
    /// </summary>
    ///
    /// <returns>The stored color.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Color"/>.</exception>
    public Color AsColor()
    {
        return Expect<Color>(Type.Color);
    }

    /// <summary>
    /// Reads the stored <see cref="StringName"/> value.
    /// </summary>
    ///
    /// <returns>The stored string name.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.StringName"/>.</exception>
    public StringName AsStringName()
    {
        return Expect<StringName>(Type.StringName);
    }

    /// <summary>
    /// Reads the stored <see cref="NodePath"/> value.
    /// </summary>
    ///
    /// <returns>The stored node path.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.NodePath"/>.</exception>
    public NodePath AsNodePath()
    {
        return Expect<NodePath>(Type.NodePath);
    }

    /// <summary>
    /// Reads the stored <see cref="Rid"/> value.
    /// </summary>
    ///
    /// <returns>The stored resource identifier.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Rid"/>.</exception>
    public Rid AsRid()
    {
        return Expect<Rid>(Type.Rid);
    }

    /// <summary>
    /// Reads the stored object reference.
    /// </summary>
    ///
    /// <returns>The stored <see cref="Electron2D.Object"/> or derived instance.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Object"/>.</exception>
    public Object? AsObject()
    {
        return Expect<Object>(Type.Object);
    }

    /// <summary>
    /// Reads the stored <see cref="Callable"/> value.
    /// </summary>
    ///
    /// <returns>The stored callable.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Callable"/>.</exception>
    public Callable AsCallable()
    {
        return Expect<Callable>(Type.Callable);
    }

    /// <summary>
    /// Reads the stored Electron2D array reference.
    /// </summary>
    ///
    /// <returns>The stored <see cref="VariantArray"/> instance.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Array"/>.</exception>
    public VariantArray AsArray()
    {
        return Expect<VariantArray>(Type.Array);
    }

    /// <summary>
    /// Reads the stored Electron2D dictionary reference.
    /// </summary>
    ///
    /// <returns>The stored <see cref="VariantDictionary"/> instance.</returns>
    ///
    /// <exception cref="InvalidCastException">Thrown when this variant is not <see cref="Type.Dictionary"/>.</exception>
    public VariantDictionary AsDictionary()
    {
        return Expect<VariantDictionary>(Type.Dictionary);
    }

    /// <summary>
    /// Tests two variants for equality.
    /// </summary>
    ///
    /// <param name="other">The variant to compare with this value.</param>
    ///
    /// <returns><c>true</c> if both variants have the same type and value; otherwise, <c>false</c>.</returns>
    public bool Equals(Variant other)
    {
        if (_variantType != other._variantType)
        {
            return false;
        }

        return _variantType switch
        {
            Type.Nil => true,
            Type.Object or Type.Dictionary or Type.Array => ReferenceEquals(_value, other._value),
            _ => EqualityComparer<object?>.Default.Equals(_value, other._value)
        };
    }

    /// <summary>
    /// Tests whether an object is an equal Variant.
    /// </summary>
    ///
    /// <param name="obj">The object to compare with this value.</param>
    ///
    /// <returns><c>true</c> when <paramref name="obj"/> is an equal Variant; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Variant other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this Variant.
    /// </summary>
    ///
    /// <returns>A hash code suitable for dictionary keys.</returns>
    public override int GetHashCode()
    {
        return _variantType switch
        {
            Type.Nil => HashCode.Combine(_variantType),
            Type.Object or Type.Dictionary or Type.Array => HashCode.Combine(
                _variantType,
                _value is null ? 0 : RuntimeHelpers.GetHashCode(_value)),
            _ => HashCode.Combine(_variantType, _value)
        };
    }

    /// <summary>
    /// Returns a diagnostic string representation of the stored value.
    /// </summary>
    ///
    /// <returns>A string representation of this Variant.</returns>
    public override string ToString()
    {
        return _variantType == Type.Nil
            ? "<null>"
            : Convert.ToString(_value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    /// <summary>
    /// Tests two variants for equality.
    /// </summary>
    ///
    /// <param name="left">The left variant.</param>
    /// <param name="right">The right variant.</param>
    ///
    /// <returns><c>true</c> if both variants are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Variant left, Variant right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Tests two variants for inequality.
    /// </summary>
    ///
    /// <param name="left">The left variant.</param>
    /// <param name="right">The right variant.</param>
    ///
    /// <returns><c>true</c> if the variants are different; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Variant left, Variant right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Converts a Boolean value to a Variant.
    /// </summary>
    public static implicit operator Variant(bool value)
    {
        return new Variant(Type.Bool, value);
    }

    /// <summary>
    /// Converts an 8-bit signed integer value to a Variant.
    /// </summary>
    public static implicit operator Variant(sbyte value)
    {
        return FromInt64(value);
    }

    /// <summary>
    /// Converts an 8-bit unsigned integer value to a Variant.
    /// </summary>
    public static implicit operator Variant(byte value)
    {
        return FromInt64(value);
    }

    /// <summary>
    /// Converts a 16-bit signed integer value to a Variant.
    /// </summary>
    public static implicit operator Variant(short value)
    {
        return FromInt64(value);
    }

    /// <summary>
    /// Converts a 16-bit unsigned integer value to a Variant.
    /// </summary>
    public static implicit operator Variant(ushort value)
    {
        return FromInt64(value);
    }

    /// <summary>
    /// Converts a 32-bit signed integer value to a Variant.
    /// </summary>
    public static implicit operator Variant(int value)
    {
        return FromInt64(value);
    }

    /// <summary>
    /// Converts a 32-bit unsigned integer value to a Variant.
    /// </summary>
    public static implicit operator Variant(uint value)
    {
        return FromInt64(value);
    }

    /// <summary>
    /// Converts a 64-bit signed integer value to a Variant.
    /// </summary>
    public static implicit operator Variant(long value)
    {
        return FromInt64(value);
    }

    /// <summary>
    /// Converts a 64-bit unsigned integer value to a Variant.
    /// </summary>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> cannot be represented by a signed
    /// 64-bit Variant integer.
    /// </exception>
    public static implicit operator Variant(ulong value)
    {
        return FromUInt64(value);
    }

    /// <summary>
    /// Converts a 32-bit floating-point value to a Variant.
    /// </summary>
    public static implicit operator Variant(float value)
    {
        return FromDouble(value);
    }

    /// <summary>
    /// Converts a 64-bit floating-point value to a Variant.
    /// </summary>
    public static implicit operator Variant(double value)
    {
        return FromDouble(value);
    }

    /// <summary>
    /// Converts a string value to a Variant.
    /// </summary>
    public static implicit operator Variant(string? value)
    {
        return CreateFrom(value);
    }

    /// <summary>
    /// Converts a <see cref="Vector2"/> value to a Variant.
    /// </summary>
    public static implicit operator Variant(Vector2 value)
    {
        return new Variant(Type.Vector2, value);
    }

    /// <summary>
    /// Converts a <see cref="Vector2I"/> value to a Variant.
    /// </summary>
    public static implicit operator Variant(Vector2I value)
    {
        return new Variant(Type.Vector2I, value);
    }

    /// <summary>
    /// Converts a <see cref="Rect2"/> value to a Variant.
    /// </summary>
    public static implicit operator Variant(Rect2 value)
    {
        return new Variant(Type.Rect2, value);
    }

    /// <summary>
    /// Converts a <see cref="Rect2I"/> value to a Variant.
    /// </summary>
    public static implicit operator Variant(Rect2I value)
    {
        return new Variant(Type.Rect2I, value);
    }

    /// <summary>
    /// Converts a <see cref="Transform2D"/> value to a Variant.
    /// </summary>
    public static implicit operator Variant(Transform2D value)
    {
        return new Variant(Type.Transform2D, value);
    }

    /// <summary>
    /// Converts a <see cref="Color"/> value to a Variant.
    /// </summary>
    public static implicit operator Variant(Color value)
    {
        return new Variant(Type.Color, value);
    }

    /// <summary>
    /// Converts a <see cref="StringName"/> value to a Variant.
    /// </summary>
    public static implicit operator Variant(StringName value)
    {
        return new Variant(Type.StringName, value);
    }

    /// <summary>
    /// Converts a <see cref="NodePath"/> value to a Variant.
    /// </summary>
    public static implicit operator Variant(NodePath value)
    {
        return new Variant(Type.NodePath, value);
    }

    /// <summary>
    /// Converts a <see cref="Rid"/> value to a Variant.
    /// </summary>
    public static implicit operator Variant(Rid value)
    {
        return new Variant(Type.Rid, value);
    }

    /// <summary>
    /// Converts a <see cref="Callable"/> value to a Variant.
    /// </summary>
    public static implicit operator Variant(Callable value)
    {
        return new Variant(Type.Callable, value);
    }

    /// <summary>
    /// Converts an object reference to a Variant.
    /// </summary>
    public static implicit operator Variant(Object? value)
    {
        return CreateFrom(value);
    }

    /// <summary>
    /// Converts an Electron2D array reference to a Variant.
    /// </summary>
    public static implicit operator Variant(VariantArray? value)
    {
        return CreateFrom(value);
    }

    /// <summary>
    /// Converts an Electron2D dictionary reference to a Variant.
    /// </summary>
    public static implicit operator Variant(VariantDictionary? value)
    {
        return CreateFrom(value);
    }

    private static Variant FromInt64(long value)
    {
        return new Variant(Type.Int, value);
    }

    private static Variant FromUInt64(ulong value)
    {
        if (value > long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"{nameof(UInt64)} value cannot be represented by Electron2D.Variant Int.");
        }

        return FromInt64((long)value);
    }

    private static Variant FromDouble(double value)
    {
        return new Variant(Type.Float, value);
    }

    private static Variant FromEnum(Enum value)
    {
        try
        {
            return FromInt64(Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Enum value '{value}' cannot be represented by Electron2D.Variant Int.");
        }
    }

    private T Expect<T>(Type expected)
    {
        if (_variantType != expected)
        {
            throw new InvalidCastException($"Variant stores {_variantType}, not {expected}.");
        }

        return (T)_value!;
    }
}
