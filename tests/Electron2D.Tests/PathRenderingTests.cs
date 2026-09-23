using Mathf = Electron2D.Mathf;
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

    private static void VerifyPathDiagnostics(string backend)
    {
        var settings = ProjectSettings.Instance; var savedColor = settings.Get(ProjectSettings.DebugPathsColor);
        try
        {
            settings.Set(ProjectSettings.DebugPathsColor, Colors.Red);
            using var curve = new PathCurve(); curve.AddPoint(Vector2.Zero); curve.AddPoint(new(40, 0));
            using var zero = new PathCurve(); zero.AddPoint(Vector2.Zero); zero.AddPoint(Vector2.Zero);
            using var single = new PathCurve(); single.AddPoint(Vector2.Zero);
            var path = new ScenePath { Curve = curve, Position = new(20.5f, 20.5f) };
            var window = new Window { Size = new(96, 96) }; window.AddChild(path);
            var observer = new CanvasNode { Name = "observer" }; window.AddChild(observer); var stage = 0;
            observer.ReadyAction = _ =>
            {
                var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black);
                var tree = window.Tree!; var software = renderer.GetCurrentRenderingDriverName() == "software";
                tree.EditedSceneRoot = window; var refreshes = 0; var titleEvents = 0;
                window.TitleChanged += () => titleEvents++;
                Action<SceneTree, Node> changed = (_, node) => { Check(node == window, "Title refresh identifies its window."); refreshes++; Check(titleEvents == refreshes - 1, "Warning refresh precedes TitleChanged."); };
                tree.NodeConfigurationWarningChanged += changed;
                window.Title = "Diagnostics"; window.Title = "Diagnostics"; Check(refreshes == 1, "Changed native title refreshes once.");
                Action<SceneTree, Node> fail = (_, _) => throw new ApplicationException("title diagnostics"); tree.NodeConfigurationWarningChanged += fail;
                var rejected = false; try { window.Title = "Committed"; } catch (ApplicationException) { rejected = true; }
                Check(rejected && window.Title == "Committed" && titleEvents == 1, "Native title commit survives warning failure.");
                tree.NodeConfigurationWarningChanged -= fail; tree.NodeConfigurationWarningChanged -= changed;

                settings.Set(ProjectSettings.DebugPathsColor, Colors.Blue); // Existing tree retains its sampled color.
                renderer.FramePostDraw += () =>
                {
                    using var pixels = renderer.Readback();
                    switch (stage++)
                    {
                        case 0: Pixel(pixels, 30, 20, Colors.Black); tree.DebugPathsHint = true; break;
                        case 1:
                            Pixel(pixels, 30, 20, Colors.Red); Pixel(pixels, 17, 17, software ? Colors.Black : Colors.Red); tree.DebugPathsHint = false; break;
                        case 2: Pixel(pixels, 30, 20, Colors.Black); Pixel(pixels, 17, 17, Colors.Black); tree.DebugPathsHint = true; path.Hide(); break;
                        case 3: Pixel(pixels, 30, 20, Colors.Black); path.Show(); break;
                        case 4: Pixel(pixels, 30, 20, Colors.Red); Task.Run(() => curve.SetPointPosition(1, new(0, 40))).GetAwaiter().GetResult(); break;
                        case 5: Pixel(pixels, 20, 30, Colors.Red); Pixel(pixels, 30, 20, Colors.Black); path.Position = new(40.5f, 20.5f); break;
                        case 6: Pixel(pixels, 40, 30, Colors.Red); Pixel(pixels, 20, 30, Colors.Black); path.Curve = null; break;
                        case 7: Pixel(pixels, 40, 30, Colors.Black); path.Curve = single; break;
                        case 8: Pixel(pixels, 40, 30, Colors.Black); path.Curve = zero; break;
                        case 9: Pixel(pixels, 40, 30, Colors.Black); path.Curve = curve; break;
                        case 10: Pixel(pixels, 40, 30, Colors.Red); window.RemoveChild(path); break;
                        case 11: Pixel(pixels, 40, 30, Colors.Black); window.AddChild(path); break;
                        case 12: Pixel(pixels, 40, 30, Colors.Red); path.Position = new(40, 20); break;
                        case 13:
                            var markerPixels = 0;
                            for (var y = 15; y < 19; y++) for (var x = 35; x < 39; x++)
                                    if (pixels.GetPixel(x, y).R > .9f) markerPixels++;
                            Check(markerPixels > 0, "Integer-position tangent marker reaches every backend."); window.Tree!.Quit(); break;
                        default: throw new InvalidOperationException("Path diagnostics did not terminate.");
                    }
                };
            };
            Check(Engine.Instance.Run(window) == 0 && stage == 14, "Path diagnostics completed all readbacks.");
            Released(window); Check(!curve.IsDisposed, "Diagnostics borrow curve resources.");
            Console.WriteLine($"Path diagnostics {backend} toggles, tangent markers, worker edits, transforms and visibility readback passed.");
        }
        finally { settings.Set(ProjectSettings.DebugPathsColor, savedColor); }
    }
}
