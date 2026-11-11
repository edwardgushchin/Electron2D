using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class LightWorkspaceTests
{
    private static readonly Color Ambient = new(.1f, .1f, .1f);
    private static readonly Color Red = new(.35f, .1f, .1f);
    private static readonly Color Blue = new(.1f, .1f, .3f);
    private static readonly Color RedBlue = new(.35f, .1f, .3f);

    internal static int Run(string output, string backend)
    {
        Directory.CreateDirectory(output);
        var temporary = Directory.CreateTempSubdirectory("electron2d-light-preview-");
        try
        {
            var project = SessionProject.Create(IOPath.Combine(temporary.FullName, "Game"), AppContext.BaseDirectory);
            var path = IOPath.Combine(project.Root, project.SceneFile);
            SaveScene(path);
            var disk = File.ReadAllBytes(path);
            using var session = project.OpenSession();
            using var regular = new FontFile();
            regular.LoadDynamicFont(IOPath.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
            using var mono = new SystemFont { FontNames = ["monospace"] };
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            ProjectSettings.Set(ProjectSettings.DefaultClearColor, Colors.Black);
            Engine.IsEditorHint = backend == "compatibility";
            using var window = new Window { Title = "Electron2D light preview", Size = new(1280, 900) };
            var workspace = new ProjectWorkspace(project, session, AppContext.BaseDirectory, regular, mono);
            window.AddChild(workspace);
            workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            var viewport = (SubViewport)workspace.FindChild("SceneViewport", owned: false)!;
            var preview = viewport.GetChild(0);
            var point = (PointLight)preview.GetNode("Point");
            var group = preview.GetNode("Group");
            var directional = (DirectionalLight)group.GetNode("Directional");
            Check(point.EditorOnly && directional.EditorOnly && ReferenceEquals(point.Owner, preview) &&
                ReferenceEquals(directional.Owner, preview), "The packed preview retains both policies and remaps authoring owners.");
            try
            {
                if (backend == "gpu") RunGPU(window, viewport, preview, point, group, directional, output);
                else RunCompatibility(window, viewport, preview, point, directional);
            }
            finally
            {
                workspace.RetireJobsAsync().GetAwaiter().GetResult();
            }
            Check(File.ReadAllBytes(path).AsSpan().SequenceEqual(disk), "Preview edits preserve the exact authored scene.");
            using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
            using var restored = packed.Instantiate();
            Check(restored.GetNode("Point") is PointLight { EditorOnly: true, Enabled: true } &&
                restored.GetNode("Group/Directional") is DirectionalLight { EditorOnly: true, Enabled: true },
                "Preview edits preserve the authored light policies.");
            return 0;
        }
        finally
        {
            Engine.IsEditorHint = false;
            temporary.Delete(recursive: true);
        }
    }


    private static void SaveScene(string path)
    {
        using var scene = new Entity { Name = "Scene" };
        using var image = Image.CreateEmpty(64, 32, false, Image.Format.Rgba8);
        image.Fill(Colors.White);
        using var mask = ImageTexture.CreateFromImage(image);
        using var contour = new OccluderPolygon { Closed = false, Polygon = [new(16, 0), new(16, 32)] };
        var group = new Entity { Name = "Group" };
        var directional = new DirectionalLight { Name = "Directional", EditorOnly = true, Color = Colors.Blue, Energy = .2f };
        foreach (var node in new Node[]
        {
            new Polygon { Color = Colors.White, Vertices = [new(4, 4), new(60, 4), new(60, 28), new(4, 28)] },
            new CanvasModulate { Color = Ambient },
            new PointLight { Name = "Point", EditorOnly = true, Position = new(32, 16), Texture = mask, Color = Colors.Red, Energy = .25f, ShadowEnabled = true },
            new LightOccluder { Occluder = contour },
            group
        })
        {
            scene.AddChild(node);
            node.Owner = scene;
        }
        group.AddChild(directional);
        directional.Owner = scene;
        ResourceSaver.SaveScene(scene, path, SaverFlags.BundleResources);
    }

    private static void RunGPU(Window window, SubViewport viewport, Node preview, PointLight point, Node group,
        DirectionalLight directional, string output)
    {
        Engine.IsEditorHint = false;
        var phase = -1;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "The native GPU renderer is active.");
            Console.WriteLine($"Editor lighting renderer: {RenderingServer.GetCurrentRenderingDriverName()}.");
            RenderingServer.FramePostDraw += VerifyFrame;
        };
        Check(Engine.Run(window) == 0 && phase == 10, "Every native light eligibility transition completed.");
        Console.WriteLine("Editor light pixels, shadows, live owner and policy changes passed.");

        void VerifyFrame()
        {
            var tree = window.Tree!;
            if (phase == -1)
            {
                phase++;
                return;
            }
            using var frame = viewport.GetTexture().GetImage() ?? throw new InvalidOperationException("The preview has no light pixels.");
            switch (phase++)
            {
                case 0:
                    Check(ReferenceEquals(tree.EditedSceneRoot, preview), "The workspace selects its passive projection.");
                    Pixels(frame, Ambient, Ambient);
                    Engine.IsEditorHint = true;
                    tree.EditedSceneRoot = null;
                    break;
                case 1:
                    Pixels(frame, Ambient, Ambient);
                    tree.EditedSceneRoot = preview;
                    break;
                case 2:
                    Pixels(frame, Blue, RedBlue);
                    frame.SavePNG(IOPath.Combine(output, "light-preview-edited.png"));
                    directional.Owner = group;
                    break;
                case 3:
                    Pixels(frame, Ambient, Red);
                    directional.Owner = preview;
                    point.Enabled = false;
                    break;
                case 4:
                    Pixels(frame, Blue, Blue);
                    point.Enabled = true;
                    point.Visible = false;
                    directional.Enabled = false;
                    break;
                case 5:
                    Pixels(frame, Ambient, Ambient);
                    point.Visible = true;
                    point.ShadowEnabled = false;
                    point.EditorOnly = false;
                    Engine.IsEditorHint = false;
                    tree.EditedSceneRoot = null;
                    break;
                case 6:
                    Pixels(frame, Red, Red);
                    point.EditorOnly = true;
                    break;
                case 7:
                    Pixels(frame, Ambient, Ambient);
                    frame.SavePNG(IOPath.Combine(output, "light-preview-runtime.png"));
                    Engine.IsEditorHint = true;
                    directional.Enabled = true;
                    tree.EditedSceneRoot = directional;
                    break;
                case 8:
                    Pixels(frame, Blue, Blue);
                    group.RemoveChild(directional);
                    directional.Dispose();
                    break;
                case 9:
                    Check(tree.EditedSceneRoot is null, "Removing the selected light clears the scope.");
                    Pixels(frame, Ambient, Ambient);
                    RenderingServer.FramePostDraw -= VerifyFrame;
                    tree.Quit();
                    break;
            }
        }
    }

    private static void RunCompatibility(Window window, SubViewport viewport, Node preview, PointLight point, DirectionalLight directional)
    {
        var frames = 0;
        window.Ready += _ => RenderingServer.FramePostDraw += VerifyFrame;
        Check(Engine.Run(window) == 0 && frames == 2, "The compatibility workspace renders without an active light pass.");
        Console.WriteLine("Compatibility workspace shows the diffuse preview and suppresses lighting on the copy.");

        void VerifyFrame()
        {
            if (++frames < 2) return;
            Check(RenderingServer.GetCurrentRenderingMethod() == "compatibility", "The requested compatibility renderer is active.");
            var status = (Label)window.FindChild("PreviewStatus", owned: false)!;
            Check(status.Text.Contains("lighting requires the GPU renderer", StringComparison.Ordinal),
                "The workspace explains why lighting is unavailable on this renderer.");
            Check(ReferenceEquals(window.Tree!.EditedSceneRoot, preview) && !point.Enabled && !directional.Enabled,
                "The actual workspace mutes the copied lights and retains the selected preview.");
            using var frame = viewport.GetTexture().GetImage() ?? throw new InvalidOperationException("The diffuse preview has no pixels.");
            Pixels(frame, Ambient, Ambient);
            RenderingServer.FramePostDraw -= VerifyFrame;
            window.Tree.Quit();
        }
    }


    private static void Pixels(Image frame, Color left, Color right)
    {
        Pixel(12, left);
        Pixel(48, right);

        void Pixel(int x, Color expected)
        {
            var actual = frame.GetPixel(x * frame.Width / 320, 16 * frame.Height / 180);
            Check(MathF.Abs(actual.R - expected.R) < .012f && MathF.Abs(actual.G - expected.G) < .012f &&
                MathF.Abs(actual.B - expected.B) < .012f, $"Pixel ({x},16) expected {expected}, got {actual}.");
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
