using System.Runtime.CompilerServices;
using Electron2D;

internal sealed class NativeCallbackReverse
{
    internal static readonly string SourcePath = Source();
    internal static readonly ConditionalWeakTable<RigidBody, NativeCallbackReverse> States = new();
    internal int Physics, Integrations, Releases;
    private static RigidBody Create() => new();
    internal static void Register() => Script.RegisterBinding<RigidBody, NativeCallbackReverse>("NativeCallbacks.Reverse", SourcePath, Create, Bind, Clear);
    private static void Bind(RigidBody owner)
    {
        var state = new NativeCallbackReverse(); States.Add(owner, state);
        owner.SetScriptCallbacks(new RigidBodyScriptCallbacks<RigidBody> { PhysicsProcess = state.Tick, IntegrateForces = state.Integrate, Retired = state.Retire });
    }
    private static void Clear(RigidBody owner) { }
    private void Tick(RigidBody owner, double delta) { Physics++; owner.LinearVelocity = new(-3, 0); }
    private void Integrate(RigidBody owner, PhysicsDirectBodyState state) { Integrations++; state.LinearVelocity = new(state.LinearVelocity.X, -.5f); }
    private void Retire(RigidBody owner) { States.Remove(owner); Releases++; }
    private static string Source([CallerFilePath] string path = "") => path;
}
