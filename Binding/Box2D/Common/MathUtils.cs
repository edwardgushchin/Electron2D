using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Electron2D.Binding.Box2D.Common
{
    public static class MathUtils
    {
        /// <summary>
        /// Perform the cross product on two vectors. In 2D this produces a scalar.
        /// 叉积,axb=|a||b|·sinθ
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross(Vector2 a, Vector2 b)
        {
            return (a.X * b.Y) - (a.Y * b.X);
        }

        /// <summary>
        /// Perform the cross product on a vector and a scalar. In 2D this produces
        /// a vector.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Cross(Vector2 a, float s)
        {
            return new Vector2(s * a.Y, -s * a.X);
        }

        /// <summary>
        /// Perform the cross product on a scalar and a vector. In 2D this produces
        /// a vector.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Cross(float s, Vector2 a)
        {
            return new Vector2(-s * a.Y, s * a.X);
        }

        /// <summary>
        /// Multiply a matrix times a vector. If a rotation matrix is provided,
        /// then this transforms the vector from one frame to another.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mul(Matrix2x2 m, Vector2 v)
        {
            return new Vector2((m.Ex.X * v.X) + (m.Ey.X * v.Y), (m.Ex.Y * v.X) + (m.Ey.Y * v.Y));
        }

        /// <summary>
        /// Multiply a matrix transpose times a vector. If a rotation matrix is provided,
        /// then this transforms the vector from one frame to another (inverse transform).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MulT(Matrix2x2 m, Vector2 v)
        {
            return new Vector2(Vector2.Dot(v, m.Ex), Vector2.Dot(v, m.Ey));
        }

        /// <summary>
        /// A * B
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix2x2 Mul(Matrix2x2 a, Matrix2x2 b)
        {
            return new Matrix2x2(Mul(a, b.Ex), Mul(a, b.Ey));
        }

        /// <summary>
        /// A^T * B
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix2x2 MulT(Matrix2x2 a, Matrix2x2 b)
        {
            return new Matrix2x2(
                new Vector2(Vector2.Dot(a.Ex, b.Ex), Vector2.Dot(a.Ey, b.Ex)),
                new Vector2(Vector2.Dot(a.Ex, b.Ey), Vector2.Dot(a.Ey, b.Ey)));
        }

        /// <summary>
        /// Multiply a matrix times a vector.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Mul(Matrix3x3 m, Vector3 v)
        {
            return (v.X * m.Ex) + (v.Y * m.Ey) + (v.Z * m.Ez);
        }

        /// <summary>
        /// Multiply a matrix times a vector.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mul22(Matrix3x3 m, Vector2 v)
        {
            return new Vector2((m.Ex.X * v.X) + (m.Ey.X * v.Y), (m.Ex.Y * v.X) + (m.Ey.Y * v.Y));
        }

        /// <summary>
        /// Multiply two rotations: q * r
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rotation Mul(Rotation q, Rotation r)
        {
            // [qc -qs] * [rc -rs] = [qc*rc-qs*rs -qc*rs-qs*rc]
            // [qs  qc]   [rs  rc]   [qs*rc+qc*rs -qs*rs+qc*rc]
            // s = qs * rc + qc * rs
            // c = qc * rc - qs * rs
            return new Rotation((q.Sin * r.Cos) + (q.Cos * r.Sin), (q.Cos * r.Cos) - (q.Sin * r.Sin));
        }

        /// <summary>
        /// Transpose multiply two rotations: qT * r
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rotation MulT(Rotation q, Rotation r)
        {
            // [ qc qs] * [rc -rs] = [qc*rc+qs*rs -qc*rs+qs*rc]
            // [-qs qc]   [rs  rc]   [-qs*rc+qc*rs qs*rs+qc*rc]
            // s = qc * rs - qs * rc
            // c = qc * rc + qs * rs
            return new Rotation((q.Cos * r.Sin) - (q.Sin * r.Cos), (q.Cos * r.Cos) + (q.Sin * r.Sin));
        }

        /// <summary>
        /// Rotate a vector
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mul(Rotation q, Vector2 v)
        {
            return new Vector2((q.Cos * v.X) - (q.Sin * v.Y), (q.Sin * v.X) + (q.Cos * v.Y));
        }

        /// <summary>
        /// Inverse rotate a vector
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MulT(Rotation q, Vector2 v)
        {
            return new Vector2((q.Cos * v.X) + (q.Sin * v.Y), (-q.Sin * v.X) + (q.Cos * v.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mul(Transform T, Vector2 v)
        {
            var x = (T.Rotation.Cos * v.X) - (T.Rotation.Sin * v.Y) + T.Position.X;
            var y = (T.Rotation.Sin * v.X) + (T.Rotation.Cos * v.Y) + T.Position.Y;
            return new Vector2(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 MulT(Transform T, Vector2 v)
        {
            var px = v.X - T.Position.X;
            var py = v.Y - T.Position.Y;
            return new Vector2((T.Rotation.Cos * px) + (T.Rotation.Sin * py), (-T.Rotation.Sin * px) + (T.Rotation.Cos * py));
        }

        /// <summary>
        /// v2 = A.Rotation.Rot(B.Rotation.Rot(v1) + B.p) + A.p
        /// = (A.q * B.q).Rot(v1) + A.Rotation.Rot(B.p) + A.p
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform Mul(Transform A, Transform B)
        {
            return new Transform(Mul(A.Rotation, B.Position) + A.Position, Mul(A.Rotation, B.Rotation));
        }

        /// <summary>
        /// v2 = A.q' * (B.q * v1 + B.p - A.p)
        /// = A.q' * B.q * v1 + A.q' * (B.p - A.p)
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform MulT(Transform A, Transform B)
        {
            return new Transform(MulT(A.Rotation, B.Position - A.Position), MulT(A.Rotation, B.Rotation));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float a, float low, float high)
        {
            return a < low ? low : a > high ? high : a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(ref T a, ref T b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }

        /// <summary>
        /// "Next Largest Power of 2
        /// Given a binary integer value x, the next largest power of 2 can be computed by a SWAR algorithm
        /// that recursively "folds" the upper bits into the lower bits. This process yields a bit vector with
        /// the same most significant 1 as x, but all 1's below it. Adding 1 to that value yields the next
        /// largest power of 2. For a 32-bit value:"
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint NextPowerOfTwo(uint x)
        {
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(uint x)
        {
            var result = x > 0 && (x & (x - 1)) == 0;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public static int GetArraySize(int capacity)
        {
            var n = capacity - 1;

            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;
            return n < 0 ? 128 : n + 1;
        }
    }
}