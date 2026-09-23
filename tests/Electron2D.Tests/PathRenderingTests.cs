using Electron2D;
using ScenePath = Electron2D.Path;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyScenePaths(string backend)
    {
        using var horizontal = new PathCurve(); horizontal.AddPoint(Vector2.Zero); horizontal.AddPoint(new(32, 0));
        using var vertical = new PathCurve(); vertical.AddPoint(Vector2.Zero); vertical.AddPoint(new(0, 32));
        var path = new ScenePath { Curve = horizontal, Position = new(16, 16) };
        var follower = new PathFollow { CubicInterp = false };
        var box = new CanvasNode { DrawAction = n => n.DrawRect(new(-3, -3, 6, 6), Colors.Red) };
        follower.AddChild(box); path.AddChild(follower); var window = new Window { Size = new(80, 80) }; window.AddChild(path);
        var observer = new CanvasNode { Name = "observer" }; window.AddChild(observer); var stage = 0;
        observer.ReadyAction = _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black);
            renderer.FramePostDraw += () =>
            {
                using var pixels = renderer.Readback();
                switch (stage++)
                {
                    case 0:
                        Pixel(pixels, 16, 16, Colors.Red); follower.ProgressRatio = .5f; break;
                    case 1:
                        Pixel(pixels, 32, 16, Colors.Red); Pixel(pixels, 16, 16, Colors.Black); path.Curve = vertical; break;
                    case 2:
                        Pixel(pixels, 16, 32, Colors.Red); Pixel(pixels, 32, 16, Colors.Black);
                        Check(Math.Abs(follower.Rotation - Mathf.Pi / 2) < .001f, "Native path consumer follows the tangent.");
                        Task.Run(() => vertical.SetPointPosition(1, new(32, 0))).GetAwaiter().GetResult(); break;
                    case 3:
                        Pixel(pixels, 32, 16, Colors.Red); Pixel(pixels, 16, 32, Colors.Black); follower.HOffset = 4; break;
                    case 4:
                        Pixel(pixels, 36, 16, Colors.Red); path.Hide(); follower.Progress = 8; break;
                    case 5:
                        Pixel(pixels, 36, 16, Colors.Black); path.Show(); break;
                    case 6:
                        Pixel(pixels, 28, 16, Colors.Red); window.Tree!.Quit(); break;
                    default: throw new InvalidOperationException("Path test did not terminate.");
                }
            };
        };
        Check(Engine.Instance.Run(window) == 0 && stage == 7, "Native path run completed all readback stages.");
        Released(window); Check(!horizontal.IsDisposed && !vertical.IsDisposed, "Scene borrows curve resources.");
        Console.WriteLine($"Scene paths {backend} movement, worker update and visibility readback passed.");
    }
}
