using System.Numerics;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Collision
{
    /// <summary>
    /// Input parameters for b2ShapeCast
    /// </summary>
    public struct ShapeCastInput
    {
        public DistanceProxy ProxyA;

        public DistanceProxy ProxyB;

        public Transform TransformA;

        public Transform TransformB;

        public Vector2 TranslationB;
    }
}