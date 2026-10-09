using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal PhysicsSleepSettings SleepSettings { get; private set; }
    internal void SetSleepSettings(in PhysicsSleepSettings settings)
    {
        EnsureQueryAccess(); settings.Validate();
        if (SleepSettings == settings) return;
        SleepSettings = settings;
        var world = b2GetWorldFromId(_worldID);
        world.sleepAngularThreshold = settings.AngularThreshold; world.timeToSleep = settings.TimeToSleep;
        for (var i = 0; i < world.bodies.count; i++)
        {
            var body = world.bodies.data[i];
            if (body.id < 0) continue;
            body.sleepThreshold = settings.LinearThreshold * MetersPerUnit;
            body.sleepTime = 0;
            if (body.type != B2BodyType.b2_staticBody) b2WakeBody(world, body);
        }
    }
}
