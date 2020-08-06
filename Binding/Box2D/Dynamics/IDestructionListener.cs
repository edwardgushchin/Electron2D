using Electron2D.Binding.Box2D.Dynamics.Joints;

namespace Electron2D.Binding.Box2D.Dynamics
{
    public interface IDestructionListener
    {
        /// <summary>
        /// Called when any joint is about to be destroyed due
        /// to the destruction of one of its attached bodies.
        /// </summary>
        void SayGoodbye(Joint joint);

        /// <summary>
        /// Called when any fixture is about to be destroyed due
        /// to the destruction of its parent body.
        /// </summary>
        void SayGoodbye(Fixture fixture);
    }
}