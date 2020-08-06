using System.Diagnostics;
using System.Runtime.CompilerServices;
using Electron2D.Binding.Box2D.Collision;
using Electron2D.Binding.Box2D.Collision.Collider;
using Electron2D.Binding.Box2D.Collision.Shapes;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Dynamics.Contacts
{
    internal class CircleContact : Contact
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void Evaluate(ref Manifold manifold, in Transform xfA, Transform xfB)
        {
            CollisionUtils.CollideCircles(
                ref manifold,
                (CircleShape)FixtureA.Shape,
                xfA,
                (CircleShape)FixtureB.Shape,
                xfB);
        }
    }

    internal class CircleContactFactory : IContactFactory
    {
        private readonly ContactPool<CircleContact> _pool = new ContactPool<CircleContact>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Contact Create(Fixture fixtureA, int indexA, Fixture fixtureB, int indexB)
        {
            System.Diagnostics.Debug.Assert(fixtureA.ShapeType == ShapeType.Circle);
            System.Diagnostics.Debug.Assert(fixtureB.ShapeType == ShapeType.Circle);
            var contact = _pool.Get();
            contact.Initialize(fixtureA, 0, fixtureB, 0);
            return contact;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy(Contact contact)
        {
            _pool.Return((CircleContact)contact);
        }
    }
}