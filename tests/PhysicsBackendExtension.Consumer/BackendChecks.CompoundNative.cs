using Electron2D;

public static partial class BackendChecks
{
    public static void RunCompoundNative(string renderer)
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        PhysicsServerManager.RegisterServer("CompoundNative", () => new Solver());
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
            foreach (var name in new[] { "CPU", "GPU", "CompoundNative" })
            {
                using var world = new World(name); using var circle = new CircleShape { Radius = 6 };
                using var probe = new CircleShape { Radius = 1 };
                var window = new Window { Title = "Compound physics geometry", Size = new(256, 96), World = world };
                var body = new StaticBody { Position = new(120, 48), Rotation = Mathf.Pi / 2 };
                body.AddChild(new CollisionShape { Name = "Disabled", Shape = circle, Disabled = true });
                foreach (var side in new[] { -1, 1 })
                {
                    body.AddChild(new CollisionShape { Name = "Shape" + side, Shape = circle, Position = new(side * 24, 0) });
                    body.AddChild(new CompoundDisk { Name = "Disk" + side, Position = new(side * 24, 0) });
                }
                window.AddChild(body); var complete = false; var frames = 0;
                using var query = new PhysicsShapeQueryParameters { Shape = probe, Transform = new(0, new Vector2(120, 90)), Motion = new(0, -18), Margin = 0 };
                window.Ready += _ =>
                {
                    RenderingServer.SetDefaultClearColor(Colors.Black);
                    RenderingServer.FramePostDraw += () =>
                    {
                        if (++frames < 2) return;
                        var cast = world.DirectSpaceState.CastMotion(query); var analytic = 11f / 18;
                        Check(cast.SafeFraction <= analytic + .00001f && cast.UnsafeFraction >= analytic - .00001f &&
                            cast.UnsafeFraction - cast.SafeFraction <= 3f / 512 + .00001f,
                            "Native compound cast brackets geometric radii independently of rendering");
                        using var image = window.GetTexture().GetImage()!;
                        var scaleX = image.Width / (float)window.Size.X; var scaleY = image.Height / (float)window.Size.Y;
                        Check(image.GetPixel((int)(120 * scaleX), (int)(24 * scaleY)).R > .9f &&
                            image.GetPixel((int)(120 * scaleX), (int)(72 * scaleY)).R > .9f &&
                            image.GetPixel((int)(120 * scaleX), (int)(48 * scaleY)).R < .1f,
                            "The real renderer presents both transformed shapes and their empty compound gap");
                        image.SavePNG($"/tmp/e2d-compound-{renderer}-{name}.png"); complete = true; window.Tree!.Quit();
                    };
                };
                Check(Engine.Run(window) == 0 && complete && window.IsDisposed, "Native compound query/pixels/lifetime completed");
                Console.WriteLine($"Native compound physics={name}, renderer={renderer}: geometric cast, transformed pixels and lifetime passed.");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); }
    }
    private sealed class CompoundDisk : Entity
    { protected override void OnDraw() => DrawCircle(Vector2.Zero, 6, Colors.Red); }
}
