using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class CameraPreviewContracts
{
    internal static int Run(string output, string backend)
    {
        Directory.CreateDirectory(output);
        var project = Directory.CreateTempSubdirectory("electron2d-camera-preview-");
        try
        {
            SaveScene(project.FullName);
            using var session = ProjectSession.Open(project.FullName, "scene.e2dscene", "Game.cs");
            using var client = session.Connect("camera-preview");
            RunPreview(client, output, backend);
            using var source = ResourceLoader.Load<PackedScene>(IOPath.Combine(project.FullName, "scene.e2dscene"), ResourceLoader.CacheMode.Ignore);
            using var instance = source.Instantiate();
            var authored = (Camera)instance.GetNode("Camera");
            Check(authored.Enabled && authored.ProcessMode == ProcessMode.Inherit &&
                authored.EditorDrawScreen && authored.EditorDrawLimits && authored.EditorDrawDragMargin,
                "Projection and live edits preserve the original camera settings.");
            var disabled = (Camera)instance.GetNode("NoScreen");
            Check(!disabled.EditorDrawScreen, "A stored false overrides the enabled default.");
            Console.WriteLine($"Camera editor preview passed ({backend}).");
            return 0;
        }
        finally
        {
            Engine.IsEditorHint = false;
            project.Delete(recursive: true);
        }
    }

    private static void SaveScene(string project)
    {
        using var scene = new Entity { Name = "Scene" };
        var camera = new Camera
        {
            Name = "Camera",
            Position = new(80, 60),
            Zoom = new(2, 2),
            LimitLeft = 20,
            LimitTop = 15,
            LimitRight = 170,
            LimitBottom = 110,
            DragLeftMargin = .5f,
            DragTopMargin = .5f,
            DragRightMargin = .5f,
            DragBottomMargin = .5f,
            EditorDrawLimits = true,
            EditorDrawDragMargin = true
        };
        var hidden = new Camera { Name = "NoScreen", EditorDrawScreen = false };
        Check(camera.EditorDrawScreen && !hidden.EditorDrawLimits && !hidden.EditorDrawDragMargin, "Camera overlay defaults.");
        scene.AddChild(camera);
        scene.AddChild(hidden);
        camera.Owner = hidden.Owner = scene;
        ResourceSaver.SaveScene(scene, IOPath.Combine(project, "scene.e2dscene"), SaverFlags.BundleResources);
        File.WriteAllText(IOPath.Combine(project, "Game.cs"), "return 0;");
    }

    private static void RunPreview(SessionClient client, string output, string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.DefaultClearColor, Colors.Black);
        Engine.IsEditorHint = true;
        using var root = new Window { Title = "Electron2D camera preview", Size = new(240, 160) };
        var requestedDriver = Environment.GetEnvironmentVariable(backend == "gpu" ? "SDL_GPU_DRIVER" : "SDL_RENDER_DRIVER");
        var container = new SubViewportContainer { Size = new(192, 128), TextureFilter = TextureFilter.Nearest };
        var viewport = new SubViewport { Size = new(192, 128), RenderTargetUpdateMode = ViewportUpdateMode.Always };
        var preview = client.CreatePreview(client.InspectScene().Revision);
        var camera = (Camera)preview.GetNode("Camera");
        Check(camera.EditorDrawScreen && camera.EditorDrawLimits && camera.EditorDrawDragMargin &&
            !camera.Enabled && camera.ProcessMode == ProcessMode.Disabled, "The public session creates a passive copy with the stored overlays.");
        Check(!((Camera)preview.GetNode("NoScreen")).EditorDrawScreen, "The passive copy preserves an explicit false.");
        root.AddChild(container);
        container.AddChild(viewport);
        viewport.AddChild(preview);
        var other = new Entity();
        var outside = new Camera { Enabled = false, Position = new(100, 60), Zoom = new(4, 4) };
        other.AddChild(outside);
        viewport.AddChild(other);
        using var texture = viewport.GetTexture();
        var phase = 0;
        root.Ready += _ =>
        {
            var driver = RenderingServer.GetCurrentRenderingDriverName();
            Check(RenderingServer.GetCurrentRenderingMethod() == backend &&
                (string.IsNullOrEmpty(requestedDriver) || driver == requestedDriver),
                $"The preview uses the requested backend and driver: {backend}/{requestedDriver}, observed {driver}.");
            Console.WriteLine($"Camera preview renderer: {backend}/{driver}.");
            root.Tree!.EditedSceneRoot = preview;
            RenderingServer.FramePostDraw += VerifyFrame;
        };
        try
        {
            Check(Engine.Run(root) == 0 && phase == 8, "Native preview ran all eight verification frames.");
        }
        finally
        {
            if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= VerifyFrame;
        }

        void VerifyFrame()
        {
            Check(viewport.GetCamera() is null && viewport.CanvasTransform == Transform.Identity,
                "The projected camera leaves the editor view passive.");
            using var frame = texture.GetImage() ?? throw new InvalidOperationException("The editor preview has no completed pixels.");
            switch (phase++)
            {
                case 0:
                    Check(Ink(frame, 80, 28, Outline.Screen) && Ink(frame, 80, 15, Outline.Limits) &&
                        Ink(frame, 80, 44, Outline.Margin), "All three authored outlines reach the preview image.");
                    Check(!Ink(frame, 100, 44, Outline.Screen), "A camera outside the edited subtree contributes no outline.");
                    frame.SavePNG(IOPath.Combine(output, $"camera-preview-{backend}-authored.png"));
                    camera.LimitEnabled = false;
                    camera.EditorDrawScreen = false;
                    camera.DragTopMargin = .25f;
                    break;
                case 1:
                    Check(!Ink(frame, 80, 15, Outline.Limits) && !Ink(frame, 80, 28, Outline.Screen) &&
                        Ink(frame, 80, 52, Outline.Margin) && !Ink(frame, 80, 44, Outline.Margin),
                        "Live edits remove old lines and update the asymmetric margin.");
                    viewport.Size2DOverride = new(160, 100);
                    camera.EditorDrawScreen = true;
                    break;
                case 2:
                    Check(Ink(frame, 80, 35, Outline.Screen) && !Ink(frame, 80, 28, Outline.Screen),
                        "The screen uses the changed logical viewport size.");
                    camera.Scale = new(2, 1);
                    camera.Rotation = MathF.PI / 2;
                    camera.IgnoreRotation = false;
                    camera.Offset = new(5, 7);
                    break;
                case 3:
                    Check(Ink(frame, 60, 67, Outline.Screen) && Ink(frame, 85, 47, Outline.Margin) &&
                        !Ink(frame, 80, 35, Outline.Screen),
                        "Rotation and offset move the geometry without inheriting the camera scale twice.");
                    frame.SavePNG(IOPath.Combine(output, $"camera-preview-{backend}-rotated.png"));
                    camera.DragLeftMargin = float.MaxValue;
                    break;
                case 4:
                    Check(Ink(frame, 60, 67, Outline.Screen) && !Ink(frame, 85, 47, Outline.Margin),
                        "Overflow in one outline leaves other valid drawing available.");
                    Engine.IsEditorHint = false;
                    break;
                case 5:
                    Check(!Ink(frame, 60, 67, Outline.Screen), "Runtime mode clears retained authoring outlines.");
                    Engine.IsEditorHint = true;
                    root.Tree!.EditedSceneRoot = other;
                    break;
                case 6:
                    Check(!Ink(frame, 60, 67, Outline.Screen) && Ink(frame, 100, 48, Outline.Screen),
                        "Changing the edited scene moves overlay eligibility.");
                    root.Tree!.EditedSceneRoot = preview;
                    break;
                case 7:
                    Check(Ink(frame, 60, 67, Outline.Screen) && !Ink(frame, 100, 48, Outline.Screen),
                        "Reselecting restores the first camera and clears the other camera.");
                    RenderingServer.FramePostDraw -= VerifyFrame;
                    root.Tree!.Quit();
                    break;
            }
        }
    }

    private enum Outline { Screen, Limits, Margin }

    private static bool Ink(Image image, int x, int y, Outline outline)
    {
        for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++)
            {
                var color = image.GetPixel(x + dx, y + dy);
                var matches = outline switch
                {
                    Outline.Screen => color.R > .45f && color.B > .45f && color.G < .4f,
                    Outline.Limits => color.R > .45f && color.G > .45f && color.B < .4f,
                    Outline.Margin => color.G > .45f && color.B > .45f && color.R < .4f,
                    _ => false
                };
                if (matches) return true;
            }
        return false;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
