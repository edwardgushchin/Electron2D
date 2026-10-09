using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal int SolverIterations { get; private set; }
    internal void SetSolverIterations(int value)
    {
        EnsureQueryAccess();
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (SolverIterations == value) return;
        SolverIterations = value; b2GetWorldFromId(_worldID).solverIterations = value;
        WakeDynamicBodies();
    }

    internal PhysicsContactSettings ContactSettings { get; private set; }
    internal void SetContactSettings(in PhysicsContactSettings settings)
    {
        EnsureQueryAccess(); settings.Validate(); if (ContactSettings == settings) return;
        ContactSettings = settings; var world = b2GetWorldFromId(_worldID);
        world.contactBias = settings.Bias; world.contactAllowedPenetration = settings.AllowedPenetration * MetersPerUnit;
        WakeDynamicBodies();
    }
    private void WakeDynamicBodies()
    {
        var world = b2GetWorldFromId(_worldID);
        for (var i = 0; i < world.bodies.count; i++)
        {
            var body = world.bodies.data[i];
            if (body.id < 0 || body.type != B2BodyType.b2_dynamicBody) continue;
            body.sleepTime = 0; b2WakeBody(world, body);
        }
    }
}
