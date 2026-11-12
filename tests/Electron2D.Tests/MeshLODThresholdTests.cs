using Electron2D;
using static TextureArrayTests;

internal static class MeshLODThresholdTests
{
    internal static void Run(string backend)
    {
        var setting = ProjectSettings.MeshLODThreshold;
        var previous = ProjectSettings.Get(setting);
        try
        {
            ProjectSettings.Set(setting, 2);
            if (backend == "managed") VerifyStorage();
            else VerifyPixels(backend);
        }
        finally { ProjectSettings.Set(setting, previous); }
    }

    private static void VerifyStorage()
    {
        var setting = ProjectSettings.MeshLODThreshold;
        using var root = new Window();
        Check(root.MeshLODThreshold == 1, "Detached viewport starts at one pixel.");
        foreach (var invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            Reject<ArgumentOutOfRangeException>(() => root.MeshLODThreshold = invalid);
            Reject<ArgumentException>(() => ProjectSettings.Set(setting, invalid));
        }
        Check(root.MeshLODThreshold == 1 && ProjectSettings.Get(setting) == 2, "Rejected thresholds preserve both values.");
        const string feature = "mesh_lod_threshold_test";
        ProjectSettings.SetFeatureOverride(setting, feature, 3);
        try { Check(ProjectSettings.GetWithOverride(setting, [feature]) == 3, "The typed setting supports feature overrides."); }
        finally { ProjectSettings.ClearFeatureOverride(setting, feature); }
        using var implicitDuplicate = (Window)root.Duplicate();
        using var scene = new PackedScene();
        scene.Pack(root);
        using var implicitPacked = (Window)scene.Instantiate();

        root.MeshLODThreshold = 1;
        using var authoredDuplicate = (Window)root.Duplicate();
        scene.Pack(root);
        using var authoredPacked = (Window)scene.Instantiate();
        foreach (var implicitRoot in new[] { implicitDuplicate, implicitPacked })
        {
            implicitRoot.ApplyProjectMeshLODThreshold();
            Check(implicitRoot.MeshLODThreshold == 2, "An unassigned packed or duplicated root reads the startup value.");
        }
        foreach (var authoredRoot in new[] { authoredDuplicate, authoredPacked })
        {
            authoredRoot.ApplyProjectMeshLODThreshold();
            Check(authoredRoot.MeshLODThreshold == 1, "An explicit default survives packing and duplication.");
        }
        using var viewport = new SubViewport();
        using var tree = new SceneTree(viewport);
        Reject<InvalidOperationException>(() => Task.Run(() => viewport.MeshLODThreshold = 0).GetAwaiter().GetResult());
        viewport.MeshLODThreshold = 0;
        Check(viewport.MeshLODThreshold == 0, "An attached owner can disable automatic LOD.");
        tree.Dispose();
        Reject<ObjectDisposedException>(() => _ = viewport.MeshLODThreshold);
        Console.WriteLine("Viewport mesh LOD settings, explicit defaults, packing, duplication and guards passed.");
    }

    private static void VerifyPixels(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var mesh = MeshLODTests.Mesh();
        using var window = new Window { Size = new(96, 64) };
        var child = new SubViewport { Size = new(96, 64), RenderTargetUpdateMode = ViewportUpdateMode.Always };
        window.AddChild(new MeshInstance { Mesh = mesh, Position = new(4, 4) });
        window.AddChild(child);
        child.AddChild(new MeshInstance { Mesh = mesh, Position = new(4, 4) });
        var checkedFrames = false;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend without fallback.");
            Check(window.MeshLODThreshold == 2 && child.MeshLODThreshold == 1, "The project setting applies only to the root window.");
            RenderingServer.SetDefaultClearColor(Colors.Black);
            using var rootTexture = window.GetTexture();
            using var childTexture = child.GetTexture();
            RenderingServer.ForceDraw(swapBuffers: false);
            CheckPixel(rootTexture, 56, 12, false);
            CheckPixel(rootTexture, 8, 28, true);
            CheckPixel(childTexture, 56, 12, true);

            window.MeshLODThreshold = 0;
            child.MeshLODThreshold = 4;
            RenderingServer.ForceDraw(swapBuffers: false);
            CheckPixel(rootTexture, 56, 12, true);
            CheckPixel(childTexture, 56, 12, false);
            CheckPixel(childTexture, 8, 28, false);
            CheckPixel(childTexture, 24, 12, true);
            checkedFrames = true;
            Console.WriteLine($"Viewport LOD pixels passed on {RenderingServer.GetCurrentRenderingDriverName()}.");
            window.Tree!.Quit();
        };
        Check(Engine.Run(window) == 0 && checkedFrames, "Independent native LOD targets completed.");
    }

    private static void CheckPixel(ViewportTexture texture, int x, int y, bool visible)
    {
        using var image = texture.GetImage()!;
        var pixel = image.GetPixel(x, y);
        Check(visible ? pixel.R > .9f : pixel.R < .1f, $"LOD pixel at ({x}, {y}) was {pixel}.");
    }
}
