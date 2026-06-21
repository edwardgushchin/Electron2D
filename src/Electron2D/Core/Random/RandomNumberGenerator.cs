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
/// Represents the random number generator type.
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
public class RandomNumberGenerator : RefCounted
{
    private const ulong Multiplier = 6364136223846793005UL;
    private const ulong Increment = 1442695040888963407UL;

    private static long randomizeCounter;

    private ulong seed;
    private ulong state;

    /// <summary>
    /// Initializes a new instance of the RandomNumberGenerator type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RandomNumberGenerator" />
    ///
    public RandomNumberGenerator()
    {
        Randomize();
    }

    /// <summary>
    /// Gets or sets the seed value.
    /// </summary>
    ///
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current seed value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RandomNumberGenerator" />
    ///
    public ulong Seed
    {
        get
        {
            ThrowIfFreed();
            return seed;
        }
        set
        {
            ThrowIfFreed();
            SetSeed(value);
        }
    }

    /// <summary>
    /// Gets or sets the state value.
    /// </summary>
    ///
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current state value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RandomNumberGenerator" />
    ///
    public ulong State
    {
        get
        {
            ThrowIfFreed();
            return state;
        }
        set
        {
            ThrowIfFreed();
            state = value;
        }
    }

    /// <summary>
    /// Executes the randomize operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RandomNumberGenerator" />
    ///
    public void Randomize()
    {
        ThrowIfFreed();
        SetSeed(CreateRandomSeed());
    }

    /// <summary>
    /// Executes the randi operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
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
    /// <seealso cref="RandomNumberGenerator" />
    ///
    public uint Randi()
    {
        ThrowIfFreed();
        return NextUInt32();
    }

    /// <summary>
    /// Executes the randi range operation.
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
    /// <seealso cref="RandomNumberGenerator" />
    ///
    public int RandiRange(int from, int to)
    {
        ThrowIfFreed();

        var min = Math.Min((long)from, to);
        var max = Math.Max((long)from, to);
        var range = (ulong)(max - min) + 1UL;
        if (range == 1UL)
        {
            return (int)min;
        }

        var offset = NextBoundedUInt64(range);
        return (int)(min + (long)offset);
    }

    /// <summary>
    /// Executes the randf operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
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
    /// <seealso cref="RandomNumberGenerator" />
    ///
    public float Randf()
    {
        ThrowIfFreed();
        return Randi() / (float)uint.MaxValue;
    }

    /// <summary>
    /// Executes the randf range operation.
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
    /// <seealso cref="RandomNumberGenerator" />
    ///
    public float RandfRange(float from, float to)
    {
        ThrowIfFreed();

        if (from == to)
        {
            return from;
        }

        var min = MathF.Min(from, to);
        var max = MathF.Max(from, to);
        return min + ((max - min) * Randf());
    }

    /// <summary>
    /// Executes the randfn operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="mean">
    /// The mean value.
    /// </param>
    ///
    /// <param name="deviation">
    /// The deviation value.
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
    /// <seealso cref="RandomNumberGenerator" />
    ///
    public float Randfn(float mean = 0f, float deviation = 1f)
    {
        ThrowIfFreed();

        if (deviation == 0f)
        {
            return mean;
        }

        var unitA = MathF.Max(Randf(), float.Epsilon);
        var unitB = Randf();
        var magnitude = MathF.Sqrt(-2f * MathF.Log(unitA));
        var normal = magnitude * MathF.Cos(Mathf.Tau * unitB);
        return mean + (deviation * normal);
    }

    private static ulong CreateRandomSeed()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];

        try
        {
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return BitConverter.ToUInt64(bytes);
        }
        catch (Exception)
        {
            return unchecked(
                (ulong)DateTime.UtcNow.Ticks ^
                (ulong)Environment.TickCount64 ^
                (ulong)Interlocked.Increment(ref randomizeCounter));
        }
    }

    private void SetSeed(ulong value)
    {
        seed = value;
        state = 0UL;
        _ = NextUInt32();
        state = unchecked(state + value);
        _ = NextUInt32();
    }

    private uint NextUInt32()
    {
        var oldState = state;
        state = unchecked((oldState * Multiplier) + Increment);

        var xorshifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        var rotation = (int)(oldState >> 59);
        return (xorshifted >> rotation) | (xorshifted << ((-rotation) & 31));
    }

    private ulong NextUInt64()
    {
        return ((ulong)NextUInt32() << 32) | NextUInt32();
    }

    private ulong NextBoundedUInt64(ulong upperBoundExclusive)
    {
        var threshold = unchecked(0UL - upperBoundExclusive) % upperBoundExclusive;

        while (true)
        {
            var value = NextUInt64();
            if (value >= threshold)
            {
                return value % upperBoundExclusive;
            }
        }
    }
}
