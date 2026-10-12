using Electron2D;

public static partial class BackendChecks
{
    private static void ResourceReleaseEvents(PhysicsServer.Backend? backend)
    {
        if (backend is null) PhysicsServerManager.RegisterServer("ResourceEvents", () => new Solver());
        var space = backend is { } selected ? PhysicsServer.SpaceCreate(selected) : PhysicsServer.SpaceCreate("ResourceEvents");
        var body = PhysicsServer.BodyCreate();
        var areas = new[] { PhysicsServer.AreaCreate(), PhysicsServer.AreaCreate() };
        using var circle = new CircleShape { Radius = 3 };
        var entered = 0; var exited = 0;
        try
        {
            PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetSpace(body, space);
            foreach (var area in areas)
            {
                PhysicsServer.AreaAddShape(area, circle.GetRID()); PhysicsServer.AreaSetSpace(area, space);
                PhysicsServer.AreaSetMonitorCallback(area, (status, other, _, _, _) =>
                {
                    if (other != body) return;
                    if (status == PhysicsServer.AreaBodyStatus.Added) { entered++; return; }
                    exited++;
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(body, space));
                    throw new InvalidOperationException("Release observer failure");
                });
            }
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(entered == 2, "Both real overlapping sensor memberships are established before release");
            var id = body;
            var errors = ReleaseErrors(() => PhysicsServer.FreeRID(body));
            body = default;
            Check(exited == 2 && errors.Count(error => error.Message == "Release observer failure") == 2,
                $"Departure observers all execute and retain errors: exits={exited}, errors={string.Join(',', errors.Select(error => error.Message))}");
            Reject<ArgumentException>(() => PhysicsServer.BodyGetSpace(id));
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(exited == 2, "Committed departures are not repeated by the next interval");
        }
        finally
        {
            foreach (var area in areas) { PhysicsServer.AreaSetMonitorCallback(area, null); PhysicsServer.FreeRID(area); }
            if (body.IsValid()) PhysicsServer.FreeRID(body);
            PhysicsServer.FreeRID(space);
        }
    }
}
