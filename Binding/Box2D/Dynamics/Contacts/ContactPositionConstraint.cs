using System.Numerics;
using Electron2D.Binding.Box2D.Collision.Collider;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Dynamics.Contacts
{
    public struct ContactPositionConstraint
    {
        /// <summary>
        /// Size <see cref="Settings.MaxManifoldPoints"/>
        /// </summary>
        public FixedArray2<Vector2> LocalPoints;

        public int IndexA;

        public int IndexB;

        public float InvIa, InvIb;

        public float InvMassA, InvMassB;

        public Vector2 LocalCenterA, LocalCenterB;

        public Vector2 LocalNormal;

        public Vector2 LocalPoint;

        public int PointCount;

        public float RadiusA, RadiusB;

        public ManifoldType Type;
    }
}