using Electron2D;
using static PhysicsDebugTests;

internal static partial class PhysicsProjectSelectionTests
{
    internal static void RunNative(string renderer)
    {
        var engine = ProjectSettings.Get(ProjectSettings.Physics2DEngine);
        var fallback = ProjectSettings.Get(ProjectSettings.Physics2DAllowCPUFallback);
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var interpolation = ProjectSettings.Get(ProjectSettings.PhysicsInterpolation);
        var ticks = Engine.PhysicsTicksPerSecond; var fps = Engine.MaxFPS;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
            ProjectSettings.Set(ProjectSettings.Physics2DAllowCPUFallback, false);
            ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, false);
            Engine.PhysicsTicksPerSecond = 60; Engine.MaxFPS = 120;
            foreach (var name in new[] { "CPU", "GPU" })
            {
                ProjectSettings.Set(ProjectSettings.Physics2DEngine, name);
                using var shape = new CircleShape { Radius = 5 };
                var window = new Window { Title = "Project physics selection", Size = new(256, 96) };
                var body = new RigidBody
                {
                    Position = new(20, 40),
                    GravityScale = 0,
                    CanSleep = false,
                    LinearVelocity = new(120, 0),
                    LinearDamp = 0,
                    LinearDampMode = RigidBody.DampMode.Replace
                };
                body.AddChild(new CollisionShape { Shape = shape }); body.AddChild(new ProjectDisk()); window.AddChild(body);
                var completed = false; var frames = 0;
                window.Ready += _ =>
                {
                    var tree = window.Tree!; var selected = body.GetWorld()!;
                    Check(selected.RequestedPhysicsBackendName == name && selected.PhysicsBackendName == name &&
                        selected.PhysicsFallbackReason is null, "Native implicit window selects project physics independently of rendering");
                    RenderingServer.SetDefaultClearColor(Colors.Black);
                    RenderingServer.FramePostDraw += () =>
                    {
                        Check(++frames < 600, "Native project physics window made progress");
                        if (tree.PhysicsFrameCount < 4) return;
                        Check(MathF.Abs(body.Position.X - (20 + 2 * tree.PhysicsFrameCount)) < .002f,
                            "Native project-selected body follows the fixed simulation ticks");
                        using var pixels = RenderingServer.Service!.Readback();
                        var pixel = pixels.GetPixel((int)MathF.Round(body.Position.X * pixels.Width / window.Size.X),
                            (int)MathF.Round(body.Position.Y * pixels.Height / window.Size.Y));
                        Check(pixel.R > .9f && pixel.G < .1f, "The real renderer presents the project-selected moving body");
                        pixels.SavePNG($"/tmp/e2d-project-physics-{renderer}-{name}.png");
                        completed = true; tree.Quit();
                    };
                };
                Check(Engine.Run(window) == 0 && completed && window.IsDisposed, "Native project selection completed and released its window");
                Console.WriteLine($"Project physics real window: physics={name}, renderer={renderer}, ticks/motion/pixels/lifetime passed.");
            }
        }
        finally
        {
            Engine.PhysicsTicksPerSecond = ticks; Engine.MaxFPS = fps;
            ProjectSettings.Set(ProjectSettings.Physics2DEngine, engine); ProjectSettings.Set(ProjectSettings.Physics2DAllowCPUFallback, fallback);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, interpolation);
        }
    }
    private sealed class ProjectDisk : Entity
    { protected override void OnDraw() => DrawCircle(Vector2.Zero, 5, Colors.Red); }
}
