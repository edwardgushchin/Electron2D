#nullable disable
// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Box2D.NET
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct B2FloatW
    {
        private Vector256<float> _value;
        public ref float X => ref this[0];
        public ref float Y => ref this[1];
        public ref float Z => ref this[2];
        public ref float W => ref this[3];
        public ref float E => ref this[4];
        public ref float F => ref this[5];
        public ref float G => ref this[6];
        public ref float H => ref this[7];

        public B2FloatW(float x, float y, float z, float w) => _value = Vector256.Create(x, y, z, w, 0, 0, 0, 0);
        public B2FloatW(float x, float y, float z, float w, float e, float f, float g, float h) => _value = Vector256.Create(x, y, z, w, e, f, g, h);
        public ref float this[int index] => ref AsSpan()[index];
        public Span<float> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<Vector256<float>, float>(ref _value), 8);
    }
}
