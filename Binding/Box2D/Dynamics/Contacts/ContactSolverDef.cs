using System;

namespace Electron2D.Binding.Box2D.Dynamics.Contacts
{
    public ref struct ContactSolverDef
    {
        public readonly TimeStep Step;

        public readonly int ContactCount;

        public readonly Contact[] Contacts;

        public readonly Position[] Positions;

        public readonly Velocity[] Velocities;

        public ContactSolverDef(TimeStep step, int contactCount, Contact[] contacts, Position[] positions, Velocity[] velocities)
        {
            Step = step;
            Contacts = contacts;
            ContactCount = contactCount;
            Positions = positions;
            Velocities = velocities;
        }
    }
}