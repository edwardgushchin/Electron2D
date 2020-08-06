namespace Electron2D.Binding.Box2D.Dynamics
{
    /// <summary>
    /// Profiling data. Times are in milliseconds.
    /// </summary>
    public struct Profile
    {
        public float Step;

        public float Collide;

        public float Solve;

        public float SolveInit;

        public float SolveVelocity;

        public float SolvePosition;

        public float Broadphase;

        public float SolveTOI;
    }
}