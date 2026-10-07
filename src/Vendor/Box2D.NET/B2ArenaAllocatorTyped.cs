#nullable disable
// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Buffers;
using static Box2D.NET.B2Arrays;

namespace Box2D.NET
{
    // This is a stack-like arena allocator used for fast per step allocations.
    // You must nest allocate/free pairs. The code will Debug.Assert
    // if you try to interleave multiple allocate/free pairs.
    // This allocator uses the heap if space is insufficient.
    // I could remove the need to free entries individually.
    internal class B2ArenaAllocatorTyped<T> : IB2ArenaAllocatable where T : new()
    {
        public ArraySegment<T> data;
        public int capacity { get; set; }
        public int index { get; set; }
        public int allocation { get; set; }
        public int maxAllocation { get; set; }

        public B2Array<B2ArenaEntry<T>> entries;

        // These arena types gather existing graph objects; their slots are pointers, not owned objects.
        internal static readonly bool BorrowedReferences = typeof(T) == typeof(B2ContactSim) || typeof(T) == typeof(B2JointSim);

        internal void Reserve(int minimumCapacity)
        {
            if (minimumCapacity <= capacity) return;
            if (allocation != 0) throw new InvalidOperationException("An in-use arena cannot grow.");
            var next = b2Alloc<T>(minimumCapacity, initializeElements: false);
            data.AsSpan().CopyTo(next);
            if (!typeof(T).IsValueType && !BorrowedReferences)
                for (var i = capacity; i < minimumCapacity; i++) next[i] = new T();
            b2Free(data.Array, capacity);
            data = next;
            capacity = minimumCapacity;
        }

        public int Grow()
        {
            // Stack must not be in use
            B2_ASSERT(allocation == 0);

            if (maxAllocation > capacity)
            {
                Reserve(maxAllocation + maxAllocation / 2);
            }

            return capacity;
        }

        public void Destroy()
        {
            b2Array_Destroy(ref entries);
            b2Free(data, capacity);

            data = null;
            capacity = 0;
            index = 0;
            allocation = 0;
            maxAllocation = 0;
        }

        public void Abort()
        {
            for (int i = 0; i < entries.count; i++)
                if (entries.data[i].usedMalloc) b2Free(entries.data[i].data.Array, entries.data[i].size);
            b2Array_Clear(ref entries);
            index = allocation = 0;
        }
    }
}
