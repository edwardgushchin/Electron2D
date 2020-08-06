namespace Electron2D.Binding.Box2D.Dynamics
{
    /// <summary>
    /// This is an internal structure.
    /// </summary>
    public struct TimeStep
    {
        public float Dt; // time step

        public float InvDt; // inverse time step (0 if dt == 0).

        public float DtRatio; // dt * inv_dt0

        public int VelocityIterations;

        public int PositionIterations;

        public bool WarmStarting;
    }
}