using System.Numerics;

namespace Electron2D.Binding.Box2D.Collision
{
    /// <summary>
    /// Output results for b2ShapeCast
    /// </summary>
    public struct ShapeCastOutput
    {
        public Vector2 Point;

        public Vector2 Normal;

        public float Lambda;

        public int Iterations;
    }
}