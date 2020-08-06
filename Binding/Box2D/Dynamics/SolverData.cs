using System;

namespace Electron2D.Binding.Box2D.Dynamics
{
    /// <summary>
    /// Solver Data
    /// </summary>
    public ref struct SolverData
    {
        public readonly TimeStep Step;

        public readonly Position[] Positions;

        public readonly Velocity[] Velocities;

        public SolverData(TimeStep step, Position[] positions, Velocity[] velocities)
        {
            Step = step;
            Positions = positions;
            Velocities = velocities;
        }
    }
}