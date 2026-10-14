#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Electron2DBufferTools.Buffers;

namespace Electron2DBufferTools.Streams;

/// <summary>
/// An <see cref="IBufferWriter{T}"/> implementation wrapping an <see cref="ArrayPoolBufferWriter{T}"/> instance.
/// </summary>
internal readonly struct ArrayBufferWriterOwner : IBufferWriter<byte>
{
    /// <summary>
    /// The wrapped <see cref="ArrayPoolBufferWriter{T}"/> array.
    /// </summary>
    private readonly ArrayPoolBufferWriter<byte> writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayBufferWriterOwner"/> struct.
    /// </summary>
    /// <param name="writer">The wrapped <see cref="ArrayPoolBufferWriter{T}"/> instance.</param>
    public ArrayBufferWriterOwner(ArrayPoolBufferWriter<byte> writer)
    {
        this.writer = writer;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        this.writer.Advance(count);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        return this.writer.GetMemory(sizeHint);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        return this.writer.GetSpan(sizeHint);
    }
}
