using System.Runtime.CompilerServices;
using Electron2D;

internal sealed class NativeCallbackForward
{
    internal static readonly string SourcePath = Source();
    internal static readonly ConditionalWeakTable<RigidBody, NativeCallbackForward> States = new();
    internal static bool FailBind, FailClear, FailRetired;
    internal static readonly Exception BindError = new IOException("native bind"), ClearError = new IOException("native clear"), RetiredError = new IOException("native retired");
    internal int Physics, Integrations, Ready, Enter, Exit, Inputs, Pointer, MouseIn, MouseOut, ShapeIn, ShapeOut, Notifications, Releases;
    internal bool Veto;
    internal PhysicsDirectBodyState? Borrowed;
    private static RigidBody Create() => new();
    internal static void Register() => Script.RegisterBinding<RigidBody, NativeCallbackForward>("NativeCallbacks.Forward", SourcePath, Create, Bind, Clear);
    internal static void Bind(RigidBody owner)
    {
        var state = new NativeCallbackForward(); States.Add(owner, state);
        owner.SetScriptCallbacks(new RigidBodyScriptCallbacks<RigidBody>
        {
            EnterTree = state.OnEnter,
            ExitTree = state.OnExit,
            Ready = state.OnReady,
            PhysicsProcess = state.OnPhysics,
            IntegrateForces = state.Integrate,
            Input = state.OnInput,
            InputEvent = state.OnPointer,
            MouseEnter = state.OnMouseIn,
            MouseExit = state.OnMouseOut,
            MouseShapeEnter = state.OnShapeIn,
            MouseShapeExit = state.OnShapeOut,
            Notification = state.Notify,
            Retired = state.Retire
        });
        if (FailBind) throw BindError;
    }
    private static void Clear(RigidBody owner) { if (FailClear) throw ClearError; }
    private void OnEnter(RigidBody owner) => Enter++;
    private void OnExit(RigidBody owner) { if (owner.GetScript() is null) throw new InvalidOperationException("Exit lost its live script source."); Exit++; }
    private void OnReady(RigidBody owner) => Ready++;
    private void OnPhysics(RigidBody owner, double delta) { Physics++; owner.LinearVelocity = new(2, 0); }
    private void Integrate(RigidBody owner, PhysicsDirectBodyState state) { Integrations++; Borrowed = state; state.LinearVelocity = new(state.LinearVelocity.X, .25f); }
    private void OnInput(RigidBody owner, InputEvent value) => Inputs++;
    private void OnPointer(RigidBody owner, Viewport view, InputEvent value, int shape) { if (shape != 0) throw new InvalidOperationException("Logical shape index."); Pointer++; }
    private void OnMouseIn(RigidBody owner) => MouseIn++;
    private void OnMouseOut(RigidBody owner) => MouseOut++;
    private void OnShapeIn(RigidBody owner, int shape) => ShapeIn++;
    private void OnShapeOut(RigidBody owner, int shape) => ShapeOut++;
    private void Notify(RigidBody owner, int what, bool reversed)
    { Notifications++; if (what == ElectronObject.NotificationPreDelete && Veto) { Veto = false; owner.CancelDisposal(); } }
    private void Retire(RigidBody owner)
    {
        States.Remove(owner); Releases++;
        var before = Notifications; owner.Notify(7300); if (Notifications != before) throw new InvalidOperationException("Retired notification slot remains installed.");
        if (FailRetired) throw RetiredError;
    }
    private static string Source([CallerFilePath] string path = "") => path;
}
