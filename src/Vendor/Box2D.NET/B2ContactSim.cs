#nullable disable
// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// The class manages contact between two shapes. A contact exists for each overlapping
    /// AABB in the broad-phase (except if filtered). Therefore a contact object may exist
    /// that has no contact points.
    internal class B2ContactSim
    {
        public int contactId;

#if DEBUG
        public int bodyIdA;
        public int bodyIdB;
#endif

        // Transient body indices
        public int bodySimIndexA;
        public int bodySimIndexB;

        public int shapeIdA;
        public int shapeIdB;

        public float invMassA;
        public float invIA;

        public float invMassB;
        public float invIB;

        public B2Manifold manifold;
        internal B2Vec2 surfaceLinearA, surfaceLinearB;
        internal float surfaceAngularA, surfaceAngularB;

        // Positive version identifies generated GPU geometry; negative version identifies a completed GPU solve.
        // The indexed source survives graph copies and is checked against its owner world.
        // An internal geometry replacement must clear the version; COM shifts and point pruning preserve it.
        internal long generatedManifoldVersion;
        internal int generatedManifoldIndex;

        // Mixed friction and restitution
        public float friction;
        public float restitution;
        public float rollingResistance;
        public float tangentSpeed;

        // b2ContactSimFlags
        public uint simFlags;

        public B2SimplexCache cache;

        public void CopyFrom(B2ContactSim other)
        {
            contactId = other.contactId;

#if DEBUG
            bodyIdA = other.bodyIdA;
            bodyIdB = other.bodyIdB;
#endif

            bodySimIndexA = other.bodySimIndexA;
            bodySimIndexB = other.bodySimIndexB;

            shapeIdA = other.shapeIdA;
            shapeIdB = other.shapeIdB;

            invMassA = other.invMassA;
            invIA = other.invIA;

            invMassB = other.invMassB;
            invIB = other.invIB;

            manifold = other.manifold;
            surfaceLinearA = other.surfaceLinearA; surfaceLinearB = other.surfaceLinearB;
            surfaceAngularA = other.surfaceAngularA; surfaceAngularB = other.surfaceAngularB;
            generatedManifoldVersion = other.generatedManifoldVersion;
            generatedManifoldIndex = other.generatedManifoldIndex;

            friction = other.friction;
            restitution = other.restitution;
            rollingResistance = other.rollingResistance;
            tangentSpeed = other.tangentSpeed;

            simFlags = other.simFlags;

            cache = other.cache;
        }
    }
}
