using System.Numerics;
using System.Runtime.CompilerServices;

namespace Electron2D.Binding.Box2D.Common
{
    public struct Matrix2x2
    {
        public Vector2 Ex;

        public Vector2 Ey;

        /// <summary>
        /// The default constructor does nothing (for performance).
        /// Construct this matrix using columns.
        /// </summary>
        public Matrix2x2(Vector2 c1, Vector2 c2)
        {
            Ex = c1;
            Ey = c2;
        }

        /// <summary>
        /// Construct this matrix using scalars.
        /// </summary>
        public Matrix2x2(float a11, float a12, float a21, float a22)
        {
            Ex.X = a11;
            Ex.Y = a21;
            Ey.X = a12;
            Ey.Y = a22;
        }

        /// <summary>
        /// Initialize this matrix using columns.
        /// </summary>
        public void Set(Vector2 c1, Vector2 c2)
        {
            Ex = c1;
            Ey = c2;
        }

        /// <summary>
        /// Set this to the identity matrix.
        /// </summary>
        public void SetIdentity()
        {
            Ex.X = 1.0f;
            Ey.X = 0.0f;
            Ex.Y = 0.0f;
            Ey.Y = 1.0f;
        }

        /// <summary>
        /// Set this matrix to all zeros.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetZero()
        {
            Ex.X = 0.0f;
            Ey.X = 0.0f;
            Ex.Y = 0.0f;
            Ey.Y = 0.0f;
        }

        public Matrix2x2 GetInverse()
        {
            var a = Ex.X;
            var b = Ey.X;
            var c = Ex.Y;
            var d = Ey.Y;

            var det = (a * d) - (b * c);
            if (!det.Equals(0.0f))
            {
                det = 1.0f / det;
            }

            var B = new Matrix2x2();
            B.Ex.X = det * d;
            B.Ey.X = -det * b;
            B.Ex.Y = -det * c;
            B.Ey.Y = det * a;
            return B;
        }

        /// <summary>
        /// Solve A * x = b, where b is a column vector. This is more efficient
        /// than computing the inverse in one-shot cases.
        /// </summary>
        public Vector2 Solve(Vector2 b)
        {
            var a11 = Ex.X;
            var a12 = Ey.X;
            var a21 = Ex.Y;
            var a22 = Ey.Y;
            var det = (a11 * a22) - (a12 * a21);
            if (!det.Equals(0.0f))
            {
                det = 1.0f / det;
            }

            var x = new Vector2 {X = det * ((a22 * b.X) - (a12 * b.Y)), Y = det * ((a11 * b.Y) - (a21 * b.X)) };
            return x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix2x2 operator +(Matrix2x2 A, Matrix2x2 B)
        {
            return new Matrix2x2(A.Ex + B.Ex, A.Ey + B.Ey);
        }
    }
}