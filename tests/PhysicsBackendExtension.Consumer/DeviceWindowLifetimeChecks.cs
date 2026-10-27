using Electron2D;

internal static class DeviceWindowLifetimeChecks
{
    internal static void Run(bool renderFirst, bool renderRetiresFirst, bool threaded)
    {
        var rendering = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var separate = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
            using var background = new World(PhysicsServer.Backend.CPU);
            using var window = new Window { Title = "GPU space/window lifetime", Size = new(320, 240), World = background };
            using var circle = new CircleShape { Radius = 4 };
            RID body = default, space = default;
            var initialized = false;
            try
            {
                if (!renderFirst) Prepare();
                window.Ready += _ =>
                {
                    if (renderFirst) Prepare();
                    if (PhysicsServer.SpaceGetBackend(space) != PhysicsServer.Backend.GPU || RenderingServer.GetCurrentRenderingMethod() != "gpu")
                        throw new InvalidOperationException("The selected GPU space and native GPU renderer must both execute.");
                    initialized = true;
                    Console.WriteLine($"Space/render initialized: render-first={renderFirst}, render-retires-first={renderRetiresFirst}, threaded={threaded}.");
                    if (!renderRetiresFirst) RetireSpace();
                    window.Tree!.Quit();
                };
                if (Engine.Run(window) != 0 || !initialized)
                    throw new InvalidOperationException("The native world/window lifecycle did not initialize and finish.");
                if (renderRetiresFirst)
                {
                    PhysicsServer.SpaceStep(space, 1d / 60);
                    Check(2);
                    Console.WriteLine("The GPU space remains usable after native renderer retirement.");
                    RetireSpace();
                }
                Console.WriteLine("GPU space/window retirement completed.");

                void Prepare()
                {
                    space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU);
                    PhysicsServer.SpaceSetActive(space, true);
                    PhysicsServer.AreaSetGravity(space, 0);
                    PhysicsServer.AreaSetLinearDamp(space, 0);
                    body = PhysicsServer.BodyCreate();
                    PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
                    PhysicsServer.BodyAddShape(body, circle.GetRID());
                    PhysicsServer.BodySetSpace(body, space);
                    PhysicsServer.BodySetLinearVelocity(body, new(60, 0));
                    PhysicsServer.SpaceStep(space, 1d / 60);
                    Check(1);
                }
                void Check(float expected)
                {
                    // At 60 scene units/s each 1/60-second tick advances one unit; .01 allows float conversion.
                    var pose = PhysicsServer.BodyGetTransform(body);
                    if (!pose.IsFinite() || MathF.Abs(pose.Origin.X - expected) > .01f)
                        throw new InvalidOperationException("The independent GPU body did not advance its public pose.");
                }
                void RetireSpace()
                {
                    PhysicsServer.FreeRID(body); body = default;
                    PhysicsServer.FreeRID(space); space = default;
                    Console.WriteLine("Space retired.");
                }
            }
            finally
            {
                if (body != default) PhysicsServer.FreeRID(body);
                if (space != default) PhysicsServer.FreeRID(space);
            }
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, rendering);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        }
    }
}
