using Electron2D;

public static partial class BackendChecks
{
    public static void RunDebugDrawing()
    {
        var renderer = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var limit = ProjectSettings.Get(ProjectSettings.DebugCollisionMaxContacts);
        var color = ProjectSettings.Get(ProjectSettings.DebugCollisionContactColor);
        var outlines = ProjectSettings.Get(ProjectSettings.DebugCollisionDrawOutlines);
        try
        {
            ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, 1);
            ProjectSettings.Set(ProjectSettings.DebugCollisionContactColor, new Color(0, 1, 0, .5f));
            ProjectSettings.Set(ProjectSettings.DebugCollisionDrawOutlines, false);
            foreach (var method in new[] { "gpu", "compatibility" }) Draw(method);
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
            ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, limit);
            ProjectSettings.Set(ProjectSettings.DebugCollisionContactColor, color);
            ProjectSettings.Set(ProjectSettings.DebugCollisionDrawOutlines, outlines);
        }
    }

    private static void Draw(string renderer)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
        Solver? solver = null;
        var name = $"DebugDrawing-{renderer}";
        PhysicsServerManager.RegisterServer(name, () => solver = new());
        using var world = new World(name);
        using var shape = new CircleShape { Radius = 10 };
        var window = new Window { Title = "Registered solver contact diagnostics", Size = new(128, 128), World = world };
        var fixedBody = new StaticBody { Position = new(50, 50) };
        fixedBody.AddChild(new CollisionShape { Shape = shape, DebugColor = Colors.Transparent }); window.AddChild(fixedBody);
        var body = new RigidBody { Position = new(50, 65), GravityScale = 0, CanSleep = false, ConstantForce = new(0, -60) };
        body.AddChild(new CollisionShape { Shape = shape, DebugColor = Colors.Transparent }); window.AddChild(body);
        var stage = 0; var frames = 0; Vector2 sample = default;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); window.Tree!.DebugCollisionsHint = true;
            RenderingServer.FramePostDraw += () =>
            {
                Check(++frames < 600, $"Registered diagnostic drawing exceeded its frame budget: stage={stage}, samples={solver!.DebugLastCount}, steps={solver.Steps}");
                using var image = window.GetTexture().GetImage()!;
                if (stage == 0 && solver!.DebugLastCount == 1)
                {
                    sample = solver.DebugPoints[0];
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_REGISTERED_DEBUG_CAPTURE") is { } folder)
                    { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, $"registered-{renderer}.png")); }
                    Pixel(image, sample, .5f);
                    window.Tree.DebugCollisionsHint = false; stage++;
                }
                else if (stage == 1)
                {
                    Pixel(image, sample, 0); body.Position = new(50, 65);
                    window.Tree!.DebugCollisionsHint = true; stage++;
                }
                else if (stage == 2 && solver!.DebugLastCount == 1)
                { sample = solver.DebugPoints[0]; Pixel(image, sample, .5f); body.Position = new(100, 100); stage++; }
                else if (stage == 3 && solver!.DebugLastCount == 0)
                { Pixel(image, sample, 0); window.Tree!.Quit(); stage++; }
            };
        };
        Check(Engine.Run(window) == 0 && stage == 4, "Registered contact drawing and live lifecycle completed");
        world.Dispose();
        Check(solver!.DebugLimit == 0 && solver.Finished && solver.IsDisposed && !RenderingServer.IsAvailable && !DisplayServer.IsAvailable,
            "Registered diagnostic world and native canvas resources retire");
        Console.WriteLine($"Registered diagnostic pixels passed: renderer={renderer}, real circle contacts, toggle, separation and native retirement.");
        static void Pixel(Image image, Vector2 point, float expected)
        {
            var value = image.GetPixel((int)point.X, (int)point.Y);
            Check(Math.Abs(value.G - expected) < .05f && value.R < .04f && value.B < .04f,
                $"Registered contact pixel differs: {value}, expected green={expected}");
        }
    }
}
