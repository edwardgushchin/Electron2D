using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class ProcessModeWorkspaceTests
{
    internal static int Run(string output, string renderer)
    {
        Directory.CreateDirectory(output);
        var project = SessionProject.Create(IOPath.Combine(output, "Game"), AppContext.BaseDirectory);
        var sceneFile = IOPath.Combine(project.Root, project.SceneFile);
        using (var root = new Node { Name = "Scene" })
        {
            var branch = new Entity { Name = "Branch", ProcessMode = ProcessMode.Disabled };
            root.AddChild(branch);
            branch.Owner = root;
            Add(branch, new Entity { Name = "Inherited" });
            Add(branch, new Entity { Name = "Always", ProcessMode = ProcessMode.Always });
            Add(root, new Camera { Name = "Camera" });
            ResourceSaver.SaveScene(root, sceneFile);

            void Add(Node parent, Node child)
            {
                parent.AddChild(child);
                child.Owner = root;
            }
        }

        var disk = File.ReadAllBytes(sceneFile);
        using var session = project.OpenSession();
        using var regular = new FontFile();
        regular.LoadDynamicFont(IOPath.Combine(AppContext.BaseDirectory, "Assets/IBMPlexSans-Regular.ttf"));
        using var mono = new SystemFont { FontNames = ["monospace"] };
        using var window = new Window { Size = new(1280, 900), Title = "Electron2D preview processing verification" };
        var workspace = new ProjectWorkspace(project, session, AppContext.BaseDirectory, regular, mono);
        window.AddChild(workspace);
        workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var viewport = (SubViewport)workspace.FindChild("SceneViewport", owned: false)!;
        var sceneTree = (Tree)workspace.FindChild("SceneTree", owned: false)!;
        sceneTree.AddThemeColorOverride("font_disabled_color", Colors.Red);
        sceneTree.AddThemeColorOverride("font_selected_color", Colors.Green);
        sceneTree.AddThemeColorOverride("font_hovered_selected_color", Colors.Green);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 60;

        var oldHint = Engine.IsEditorHint;
        Engine.IsEditorHint = true;
        var frame = 0;
        var complete = false;
        Exception? failure = null;
        TreeItem? retainedRow = null;
        window.Ready += _ => RenderingServer.FramePostDraw += Drawn;
        try
        {
            var exit = Engine.Run(window);
            if (failure is not null) throw failure;
            Check(exit == 0 && complete, "The native editor processing sequence finished.");
        }
        finally
        {
            if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Drawn;
            Engine.IsEditorHint = oldHint;
            workspace.RetireJobsAsync().GetAwaiter().GetResult();
        }

        Check(File.ReadAllBytes(sceneFile).SequenceEqual(disk), "Live preview mode changes preserve the authored scene bytes.");
        Console.WriteLine($"PASS workspace {renderer}: live inherited and explicit processing, passive camera, selection, collapse, reentry and scene bytes.");
        return 0;

        void Drawn()
        {
            try
            {
                if (++frame < 4) return;
                Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "The requested renderer runs without fallback.");
                var preview = viewport.GetChild(0);
                Check(ReferenceEquals(window.Tree!.EditedSceneRoot, preview), "The attached preview owns the editor context.");
                var branch = preview.GetNode("Branch");
                var rootItem = sceneTree.GetRoot()!;
                var branchItem = rootItem.GetChild(0);
                var inheritedItem = branchItem.GetChild(0);
                var alwaysItem = branchItem.GetChild(1);
                var cameraItem = rootItem.GetChild(1);
                Check(branchItem.GetText(0) == "Branch" && inheritedItem.GetText(0) == "Inherited" &&
                    alwaysItem.GetText(0) == "Always" && cameraItem.GetText(0) == "Camera", "Preview row identity matches the authored hierarchy.");
                var disabledColor = sceneTree.GetThemeColor("font_disabled_color");

                if (frame == 4)
                {
                    Check(!Inactive(rootItem) && Inactive(branchItem) && Inactive(inheritedItem) && !Inactive(alwaysItem),
                        "Initial rows match inherited and explicit preview eligibility.");
                    Check(Inactive(cameraItem), "A passive camera's preview role is visible.");
                    Check(branchItem.GetCustomColor(0) == disabledColor && inheritedItem.GetCustomColor(0) == disabledColor,
                        "Inactive rows use the tree theme's disabled text color.");
                    branchItem.Select(0);
                    branchItem.Collapsed = true;
                    retainedRow = branchItem;
                }
                else if (frame == 5)
                {
                    Capture("inactive", branchItem, expectRed: true);
                    branch.ProcessMode = ProcessMode.Always;
                }
                else if (frame == 6)
                {
                    Check(ReferenceEquals(branchItem, retainedRow) && branchItem.Collapsed && ReferenceEquals(sceneTree.GetSelected(), branchItem),
                        "Refreshing the mode preserves row identity, collapse and selection.");
                    Check(!Inactive(branchItem) && !Inactive(inheritedItem) && !Inactive(alwaysItem) && Inactive(cameraItem),
                        "The next frame refreshes the whole inherited branch.");
                    Capture("active", branchItem, expectRed: false);
                    window.RemoveChild(workspace);
                    window.AddChild(workspace);
                    workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                }
                else if (frame == 7)
                {
                    Check(!Inactive(branchItem), "The reentered workspace retains the current preview state.");
                    branch.ProcessMode = ProcessMode.Disabled;
                }
                else
                {
                    Check(ReferenceEquals(branchItem, retainedRow) && Inactive(branchItem) && Inactive(inheritedItem),
                        "A reentered workspace still observes process-mode changes.");
                    complete = true;
                    window.Tree!.Quit();
                }
            }
            catch (Exception error)
            {
                failure = error;
                window.Tree!.Quit(1);
            }
        }

        void Capture(string state, TreeItem item, bool expectRed)
        {
            using var image = window.GetTexture().GetImage()!;
            var cell = sceneTree.GetItemAreaRect(item, 0);
            var origin = sceneTree.GetGlobalRect().Position + cell.Position;
            var visible = window.GetVisibleRect().Size;
            var scale = new Vector2(image.Width / visible.X, image.Height / visible.Y);
            var start = origin * scale;
            var end = (origin + cell.Size) * scale;
            var matching = 0;
            for (var y = Math.Max(0, (int)start.Y); y < Math.Min(image.Height, (int)end.Y); y++)
            {
                for (var x = Math.Max(0, (int)start.X); x < Math.Min(image.Width, (int)end.X); x++)
                {
                    var pixel = image.GetPixel(x, y);
                    if (expectRed ? pixel.R > .7f && pixel.G < .3f && pixel.B < .3f
                        : pixel.G > .7f && pixel.R < .3f && pixel.B < .3f)
                        matching++;
                }
            }
            Check(matching >= 12, $"The {state} rendered row contains its themed glyph pixels ({matching}).");
            image.SavePNG(IOPath.Combine(output, "workspace-" + state + ".png"));
            Console.WriteLine($"{state} row: {matching} matching pixels on {RenderingServer.GetCurrentRenderingDriverName()}.");
        }

        static bool Inactive(TreeItem item) => item.GetTooltipText(0) == "Processing inactive in the preview";
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
