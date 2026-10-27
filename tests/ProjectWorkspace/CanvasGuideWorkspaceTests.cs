using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class CanvasGuideWorkspaceTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    internal static int Run(string output, string renderer)
    {
        Directory.CreateDirectory(output);
        var oldHint = Engine.IsEditorHint; Engine.IsEditorHint = true;
        var project = SessionProject.Create(IOPath.Combine(output, "Game"), AppContext.BaseDirectory);
        using (var root = new Node { Name = "Scene" })
        {
            Node[] guides = [new Marker { Name = "Anchor", Position = new(96, 72), GizmoExtents = 20 },
                new ReferenceRect { Name = "Border", Position = new(160, 48), Size = new(48, 40), BorderWidth = 3 },
                new VisibleOnScreenNotifier { Name = "Region", Position = new(240, 72) }];
            foreach (var node in guides) { root.AddChild(node); node.Owner = root; }
            ResourceSaver.SaveScene(root, IOPath.Combine(project.Root, project.SceneFile));
        }
        File.WriteAllText(IOPath.Combine(project.Root, project.CodeFile), "using Electron2D.Editor;\nreturn SessionRunHost.Run(args, CreateGame);\nstatic SessionScenario CreateGame() => new Game();\nsealed class Game : SessionScenario { }\n");
        var disk = File.ReadAllBytes(IOPath.Combine(project.Root, project.SceneFile));
        using var session = project.OpenSession(); using var author = session.Connect("canvas guide author");
        var anchor = author.InspectScene().Nodes.Single(n => n.Name == "Anchor");
        using (var copy = author.CreatePreview(author.InspectScene().Revision))
            Check(copy.GetNode("Anchor") is Marker { ProcessMode: ProcessMode.Disabled, GizmoExtents: 20 } &&
                copy.GetNode("Border") is ReferenceRect { ProcessMode: ProcessMode.Disabled, EditorOnly: true } &&
                copy.GetNode("Region") is VisibleOnScreenNotifier { ProcessMode: ProcessMode.Disabled, ShowRect: true }, "Passive preview preserves guide identity/settings");
        using var regular = new FontFile(); regular.LoadDynamicFont(IOPath.Combine(AppContext.BaseDirectory, "Assets/IBMPlexSans-Regular.ttf"));
        using var mono = new SystemFont { FontNames = ["monospace"] };
        using var window = new Window { Size = new(1280, 900), Title = "Electron2D canvas authoring verification" };
        var workspace = new ProjectWorkspace(project, session, AppContext.BaseDirectory, regular, mono);
        window.AddChild(workspace); workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        var viewport = (SubViewport)workspace.FindChild("SceneViewport", owned: false)!;
        var frame = 0; var before = 0f; var complete = false; Exception? failure = null;
        window.Ready += _ => RenderingServer.FramePostDraw += Drawn;
        try
        {
            var exit = Engine.Run(window); if (failure is not null) throw failure;
            Check(exit == 0 && complete, "Native workspace guide sequence completed");
        }
        finally
        {
            if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Drawn;
            Engine.IsEditorHint = oldHint; workspace.RetireJobsAsync().GetAwaiter().GetResult();
        }
        Check(File.ReadAllBytes(IOPath.Combine(project.Root, project.SceneFile)).SequenceEqual(disk), "Unsaved guide authoring preserves exact scene bytes");
        Console.WriteLine($"Canvas workspace {renderer}: passive typed guides, actual subviewport/window pixels, shared unsaved movement, reentry, editor-role clear/restore and unchanged disk passed.");
        return 0;

        void Drawn()
        {
            try
            {
                frame++;
                if (frame == 2)
                {
                    Check(ReferenceEquals(window.Tree!.EditedSceneRoot, viewport.GetChild(0)), "Workspace owns its existing preview");
                    before = RedLeft(); Capture("before");
                    using var image = viewport.GetTexture().GetImage()!;
                    Check(At(image, 160, 60).R > .8f && At(image, 240, 72).B > At(image, 220, 72).B + .05f, "Border and translucent detection area appear in the actual editor canvas");
                    var pose = anchor.Transform!.Value; pose.Origin.X += 36;
                    author.Apply("unsaved anchor movement", new SessionTransformEdit(anchor.ID, anchor.Revision, pose));
                }
                else if (frame == 3)
                {
                    Check(Math.Abs(RedLeft() - before - 36) <= 2 && author.InspectScene().IsDirty, "Preview follows the shared unsaved guide position"); Capture("after");
                    window.RemoveChild(workspace); window.AddChild(workspace); workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                }
                else if (frame == 4)
                {
                    Check(ReferenceEquals(window.Tree!.EditedSceneRoot, viewport.GetChild(0)) && Math.Abs(RedLeft() - before - 36) <= 2, "Reentry restores the guide preview");
                    Engine.IsEditorHint = false;
                }
                else if (frame == 5)
                {
                    using var image = viewport.GetTexture().GetImage()!;
                    Check(At(image, 140, 72).R < .7f && At(image, 160, 60).R < .7f, "Role change clears retained marker and border commands");
                    Engine.IsEditorHint = true;
                }
                else if (frame == 6)
                {
                    Check(Math.Abs(RedLeft() - before - 36) <= 2, "Role restoration redraws guides");
                    using var image = window.GetTexture().GetImage()!; image.SavePNG(IOPath.Combine(output, "workspace.png")); complete = true; window.Tree!.Quit();
                }
                Check(frame < 12, "Bounded workspace run");
            }
            catch (Exception error) { failure = error; window.Tree!.Quit(1); }
        }
        Color At(Image image, int x, int y) => image.GetPixel(x * image.Width / viewport.Size.X, y * image.Height / viewport.Size.Y);
        float RedLeft()
        {
            using var image = viewport.GetTexture().GetImage()!;
            var left = int.MaxValue; var count = 0;
            for (var y = 65; y <= 78; y++) for (var x = 60; x < 153; x++)
                { var c = At(image, x, y); if (c.R > .85f && c.G < .3f && c.B < .4f) { left = Math.Min(left, x); count++; } }
            Check(count >= 10, "Actual editor preview contains positive X marker pixels"); return left;
        }
        void Capture(string name) { using var image = viewport.GetTexture().GetImage()!; image.SavePNG(IOPath.Combine(output, name + ".png")); }
    }
}
