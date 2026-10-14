#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Electron2DBufferTools.Buffers;

/// <summary>
/// An <see langword="enum"/> that indicates a mode to use when allocating buffers.
/// </summary>
internal enum AllocationMode
{
    /// <summary>
    /// The default allocation mode for pooled memory (rented buffers are not cleared).
    /// </summary>
    Default,

    /// <summary>
    /// Clear pooled buffers when renting them.
    /// </summary>
    Clear
}
