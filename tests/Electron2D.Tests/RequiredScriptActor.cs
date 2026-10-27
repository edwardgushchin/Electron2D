using Electron2D;

internal sealed class RequiredScriptActor : RigidBody
{
    internal static readonly string SourcePath = ScriptActor.Source();
    internal static RequiredScriptActor? Captured;
    internal readonly Script? ConstructorSource;
    internal readonly ulong ConstructorIdentity;
    internal readonly int Speed;
    internal int Initializations, Frames;
    public RequiredScriptActor(int speed, Exception? failure = null)
    {
        Captured = this; Initializations++; Speed = speed;
        ConstructorSource = GetScript(); ConstructorIdentity = InstanceID;
        GravityScale = 0; CanSleep = false; PhysicsProcessEnabled = true;
        if (failure is not null) throw failure;
    }
    protected override void OnPhysicsProcess(double delta)
    {
        Frames++; LinearVelocity = new(Speed, 0);
    }
}
