#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Electron2DBufferTools.Streams;

/// <summary>
/// An interface for types acting as sources for <see cref="Span{T}"/> instances.
/// </summary>
internal interface ISpanOwner
{
    /// <summary>
    /// Gets the length of the underlying memory area.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Gets a <see cref="Span{T}"/> instance wrapping the underlying memory area.
    /// </summary>
    Span<byte> Span { get; }

    /// <summary>
    /// Gets a <see cref="Memory{T}"/> instance wrapping the underlying memory area.
    /// </summary>
    Memory<byte> Memory { get; }
}
