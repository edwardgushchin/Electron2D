using Electron2D;
using IOPath = System.IO.Path;

internal static class CanvasGuideContracts
{
    internal static void Run(string profile)
    {
        var fixtureOutput = Environment.GetEnvironmentVariable("E2D_GUIDE_OUTPUT");
        var output = IOPath.GetFullPath(fixtureOutput ?? IOPath.Combine(IOPath.GetTempPath(), "Electron2D-canvas-guides-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(output);
        try
        {
            if (profile == "reload") { VerifyScene(); return; }
            Contracts();
            if (profile != "managed") Native(profile);
        }
        finally { if (fixtureOutput is null) Directory.Delete(output, true); }

        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Reject<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name);
        }
        void Contracts()
        {
            using var root = new Node { Name = "Scene" };
            var marker = new Marker { Name = "Anchor" }; var border = new ReferenceRect { Name = "Border" };
            var notifier = new VisibleOnScreenNotifier { Name = "Region" }; var enabler = new VisibleOnScreenEnabler { Name = "Enabler", EnableNodePath = "" };
            foreach (var node in new Node[] { marker, border, notifier, enabler }) { root.AddChild(node); node.Owner = root; }
            Check(marker.GizmoExtents == 10 && border.BorderColor == Colors.Red && border.BorderWidth == 1 && border.EditorOnly && notifier.ShowRect && enabler.ShowRect, "Guide defaults and inherited flag");
            marker.GizmoExtents = -24; marker.Position = new(5, 7); marker.Rotation = .25f;
            border.BorderWidth = -4; Check(border.BorderWidth == 0, "Negative border width clamps");
            border.BorderWidth = 7; border.BorderColor = new(2, -.25f, .5f, .75f); border.EditorOnly = false; border.Size = new(40, 30);
            notifier.ShowRect = false; notifier.Rect = new(10, 8, -20, -16); enabler.ShowRect = false;
            foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Reject<ArgumentException>(() => marker.GizmoExtents = invalid);
                Reject<ArgumentException>(() => border.BorderWidth = invalid);
                Reject<ArgumentException>(() => border.BorderColor = new(invalid, 0, 0));
            }
            Check(marker.GizmoExtents == -24 && border.BorderWidth == 7 && border.BorderColor.R == 2, "Rejected values retain authored state");
            Check(marker.GetPropertyList().Single(p => p.Name == nameof(Marker.GizmoExtents)).IsStored &&
                border.GetPropertyList().Count(p => p.IsStored && p.Name is nameof(ReferenceRect.BorderWidth) or nameof(ReferenceRect.BorderColor) or nameof(ReferenceRect.EditorOnly)) == 3 &&
                enabler.GetPropertyList().Single(p => p.Name == nameof(VisibleOnScreenNotifier.ShowRect)).IsStored, "Exact stored property metadata");
            using (var copy = root.Duplicate()) Verify(copy);
            using (var packed = new PackedScene()) { packed.Pack(root); using var copy = packed.Instantiate(); Verify(copy); }
            ResourceSaver.SaveScene(root, IOPath.Combine(output, "guides.e2dscene")); VerifyScene();
            var probe = new CaptureProbe
            {
                Probe = () =>
            {
                Reject<InvalidOperationException>(() => marker.GizmoExtents = 1);
                Reject<InvalidOperationException>(() => border.BorderColor = Colors.Blue);
                Reject<InvalidOperationException>(() => border.BorderWidth = 1);
                Reject<InvalidOperationException>(() => border.EditorOnly = true);
                Reject<InvalidOperationException>(() => notifier.ShowRect = true);
            }
            };
            root.AddChild(probe); probe.Owner = root;
            using (var captured = new PackedScene()) captured.Pack(root);
            Check(probe.Checked, "Scene capture rejects every new authored setter");
            using var tree = new SceneTree(root);
            Check(Task.Run(() => ReadMarker()).GetAwaiter().GetResult() is InvalidOperationException, "Attached reads require scene owner");
            Check(Task.Run(() => MutateBorder()).GetAwaiter().GetResult() is InvalidOperationException, "Attached writes require scene owner");
            marker.Dispose(); border.Dispose(); notifier.Dispose();
            Reject<ObjectDisposedException>(() => marker.GizmoExtents = 1); Reject<ObjectDisposedException>(() => _ = border.EditorOnly); Reject<ObjectDisposedException>(() => notifier.ShowRect = true);
            Console.WriteLine("Canvas guides: defaults, signed extents, clamp, nonfinite rejection, metadata, duplication, packing/file reload, owner and disposed checks passed.");
            Exception? ReadMarker() { try { _ = marker.GizmoExtents; return null; } catch (Exception error) { return error; } }
            Exception? MutateBorder() { try { border.BorderWidth = 9; return null; } catch (Exception error) { return error; } }
        }
        void VerifyScene()
        {
            using var packed = ResourceLoader.Load<PackedScene>(IOPath.Combine(output, "guides.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
            using var copy = packed.Instantiate(); Verify(copy); Console.WriteLine("Canvas guide file identities and stored values passed.");
        }
        void Verify(Node copy)
        {
            Check(copy.GetNode("Anchor") is Marker { GizmoExtents: -24, Position: { X: 5, Y: 7 } }, "Marker factory and spatial state");
            Check(copy.GetNode("Border") is ReferenceRect { BorderWidth: 7, EditorOnly: false, BorderColor: { R: 2, G: -.25f }, Size: { X: 40, Y: 30 } }, "ReferenceRect factory and layout state");
            Check(copy.GetNode("Region") is VisibleOnScreenNotifier { ShowRect: false, Rect: { Size: { X: -20, Y: -16 } } } &&
                copy.GetNode("Enabler") is VisibleOnScreenEnabler { ShowRect: false }, "Notifier and enabler inherited stored flag");
        }
        void Native(string renderer)
        {
            Engine.IsEditorHint = false; Engine.MaxFPS = 120;
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            using var window = new Window { Size = new(256, 192), Title = "Electron2D canvas guide verification" };
            var marker = new Marker { Position = new(48, 48), GizmoExtents = 16 };
            var border = new ReferenceRect { Position = new(96, 32), Size = new(40, 32), BorderWidth = 4 };
            var notifier = new VisibleOnScreenNotifier { Position = new(176, 48), Rect = new(-12, -10, 24, 20) };
            window.AddChild(marker); window.AddChild(border); window.AddChild(notifier);
            var frame = 0; var complete = false; Exception? failure = null; var draws = 0; var quietDraws = 0; long allocatedBefore = 0;
            marker.Draw += _ => draws++; border.Draw += _ => draws++; notifier.Draw += _ => draws++;
            window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += Drawn; };
            try { var exit = Engine.Run(window); if (failure is not null) throw failure; Check(exit == 0 && complete, "Native guide frame sequence completed"); }
            finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Drawn; Engine.IsEditorHint = false; }
            Console.WriteLine($"Canvas guides {renderer}: editor/runtime transitions, four axis colors, fills, border/resize, hide/show, transform/reentry, flag-independent detection and retained-frame reuse passed.");

            void Drawn()
            {
                try
                {
                    frame++;
                    if (frame <= 9)
                    {
                        using var image = window.GetTexture().GetImage()!;
                        switch (frame)
                        {
                            case 1:
                                Pixel(image, 56, 48, Colors.Black); Pixel(image, 96, 48, Colors.Black); Pixel(image, 176, 48, Colors.Black);
                                Check(notifier.IsOnScreen(), "Runtime detection works without guide pixels");
                                Engine.IsEditorHint = true; break;
                            case 2:
                                Near(image, 56, 48, new(.96f, .2f, .32f)); Near(image, 40, 48, new(.48f, .1f, .16f));
                                Near(image, 48, 56, new(.53f, .84f, .01f)); Near(image, 48, 40, new(.265f, .42f, .005f));
                                Pixel(image, 96, 48, Colors.Red); Pixel(image, 116, 48, Colors.Black); Pixel(image, 176, 48, new(.2f, .1f, .2f));
                                image.SavePNG(IOPath.Combine(output, renderer + "-editor.png"));
                                notifier.ShowRect = false; border.EditorOnly = false; Engine.IsEditorHint = false; break;
                            case 3:
                                Pixel(image, 56, 48, Colors.Black); Pixel(image, 96, 48, Colors.Red); Pixel(image, 176, 48, Colors.Black);
                                Check(notifier.IsOnScreen(), "ShowRect has no effect on detection");
                                Engine.IsEditorHint = true; marker.GizmoExtents = 0; border.BorderWidth = 0; break;
                            case 4:
                                Pixel(image, 56, 48, Colors.Black); Pixel(image, 96, 48, Colors.Black); Pixel(image, 176, 48, Colors.Black);
                                marker.GizmoExtents = -16; border.BorderWidth = 6; border.BorderColor = Colors.Cyan; border.Size = new(56, 32);
                                notifier.ShowRect = true; notifier.Rect = new(12, 10, -24, -20); notifier.SelfModulate = new(.5f, 1, 1, .5f); break;
                            case 5:
                                Near(image, 40, 48, new(.96f, .2f, .32f)); Near(image, 56, 48, new(.48f, .1f, .16f));
                                Pixel(image, 152, 48, Colors.Cyan); Pixel(image, 176, 48, new(.05f, .05f, .1f));
                                border.EditorOnly = true; marker.Hide(); border.Hide(); notifier.Hide(); Engine.IsEditorHint = false; break;
                            case 6:
                                Pixel(image, 40, 48, Colors.Black); Pixel(image, 152, 48, Colors.Black); Pixel(image, 176, 48, Colors.Black);
                                marker.Show(); border.Show(); notifier.Show(); break;
                            case 7:
                                Pixel(image, 40, 48, Colors.Black); Pixel(image, 152, 48, Colors.Black); Pixel(image, 176, 48, Colors.Black);
                                window.RemoveChild(marker); window.AddChild(marker); marker.Rotation = Mathf.Pi / 2; marker.Scale = new(2, 1);
                                notifier.Rect = new(20, 0, 12, 12); Engine.IsEditorHint = true; break;
                            case 8:
                                Near(image, 48, 32, new(.96f, .2f, .32f)); Pixel(image, 176, 48, Colors.Black); Pixel(image, 200, 52, new(.05f, .05f, .1f));
                                Engine.IsEditorHint = false; break;
                            case 9:
                                Pixel(image, 48, 32, Colors.Black); Pixel(image, 152, 48, Colors.Black); Pixel(image, 200, 52, Colors.Black);
                                quietDraws = draws; break;
                        }
                    }
                    if (frame == 32)
                    {
                        Check(draws == quietDraws, "Unchanged guides reuse retained commands without repeated recording");
                        marker.GizmoExtents = marker.GizmoExtents; notifier.Rect = notifier.Rect; quietDraws = draws;
                    }
                    if (frame == 33) Check(draws == quietDraws + 2, "Explicit extent/rectangle reassignment requests fresh drawing");
                    if (frame == 97) allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                    if (frame >= 33 && frame <= 160)
                    {
                        Engine.IsEditorHint = (frame & 1) == 0; marker.GizmoExtents = (frame & 1) == 0 ? 16 : -16;
                        border.BorderWidth = (frame & 1) == 0 ? 3 : 5; notifier.ShowRect = (frame & 1) == 0;
                    }
                    if (frame == 161)
                    {
                        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                        Check(allocated == 0, "64 warm active full guide frames allocated bytes=" + allocated);
                        Console.WriteLine("64 warm active full canvas guide frames: managed bytes=" + allocated); complete = true; window.Tree!.Quit();
                    }
                    Check(frame < 170, "Bounded native run");
                }
                catch (Exception error) { failure = error; window.Tree!.Quit(1); }
            }
            void Pixel(Image image, int x, int y, Color expected) => Check(Matches(image.GetPixel(x, y), expected), $"Frame {frame} pixel ({x},{y}): {image.GetPixel(x, y)} expected {expected}");
            void Near(Image image, int x, int y, Color expected)
            {
                for (var yy = y - 1; yy <= y + 1; yy++) for (var xx = x - 1; xx <= x + 1; xx++) if (Matches(image.GetPixel(xx, yy), expected)) return;
                throw new InvalidOperationException($"Frame {frame}: missing axis color near ({x},{y})");
            }
            bool Matches(Color actual, Color expected) => Math.Abs(actual.R - expected.R) < .025f && Math.Abs(actual.G - expected.G) < .025f && Math.Abs(actual.B - expected.B) < .025f;
        }
    }

    private sealed class CaptureProbe : Node
    {
        internal Action? Probe;
        internal bool Checked;
        protected override Func<Node> CreateSceneInstanceFactory() => CreateProbe;
        private static Node CreateProbe() => new CaptureProbe();
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
        {
            foreach (var property in base.GetPropertyDescriptors()) yield return property;
            yield return new PropertyDescriptor<CaptureProbe, int>("Guard", c => c.ReadGuard(), (c, v) => { }, _ => 0, stored: true);
        }
        private int ReadGuard() { Probe?.Invoke(); Checked = true; return 0; }
    }

}
