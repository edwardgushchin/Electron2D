#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Electron2DBufferTools.Helpers;

namespace Electron2DBufferTools;

/// <summary>
/// Helpers for working with the <see cref="HashCode"/> type.
/// </summary>
internal static class HashCodeExtensions
{
    /// <summary>
    /// Adds a sequence of <typeparamref name="T"/> values to the hash code.
    /// </summary>
    /// <typeparam name="T">The type of elements in the input <see cref="ReadOnlySpan{T}"/> instance.</typeparam>
    /// <param name="hashCode">The input <see cref="HashCode"/> instance.</param>
    /// <param name="span">The input <see cref="ReadOnlySpan{T}"/> instance.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<T>(ref this HashCode hashCode, ReadOnlySpan<T> span)
        where T : notnull
    {
        int hash = HashCode<T>.CombineValues(span);

        hashCode.Add(hash);
    }
}
