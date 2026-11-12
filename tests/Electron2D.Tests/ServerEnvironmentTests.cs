using Electron2D;

internal static class ServerEnvironmentTests
{
    private static readonly Vector2i Size = new(48, 32);

    internal static void Run(string profile)
    {
        if (profile == "managed")
        {
            using var world = new World();
            using var authored = new CanvasEnvironment();
            Reject<InvalidOperationException>(() => RenderingServer.EnvironmentCreate());
            Reject<InvalidOperationException>(() => RenderingServer.WorldSetEnvironment(world.Canvas, authored.GetRID()));
            Reject<InvalidOperationException>(() => RenderingServer.EnvironmentSetCameraID(authored.GetRID(), 0));
            Check(authored.BackgroundCameraFeedID == 1, "An inactive renderer cannot mutate a borrowed environment.");
            Console.WriteLine("Server environment managed: inactive session guards passed.");
            return;
        }

        Native(profile == "compatibility");
    }

    private static void Native(bool compatibility)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var shared = new World();
        using var other = new World();
        using var sceneEnvironment = new CanvasEnvironment { BackgroundMode = CanvasBackgroundMode.Color, BackgroundColor = Colors.Green };
        using var borrowed = new CanvasEnvironment { BackgroundMode = CanvasBackgroundMode.Color, BackgroundColor = Colors.Yellow };
        using var feed = new CameraFeed { FeedTransform = Transform.Identity };
        using var source = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        source.Fill(Colors.Cyan);

        RID retired = default;
        var passed = false;
        using (var root = new Window { Title = "Server canvas environment", Size = Size, World = shared })
        {
            var child = AddViewport(root, shared);
            var independent = AddViewport(root, other);
            root.AddChild(new WorldEnvironment { Environment = sceneEnvironment });
            using var rootTexture = root.GetTexture();
            using var childTexture = child.GetTexture();
            using var otherTexture = independent.GetTexture();

            root.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.ForceDraw();
                Expect(rootTexture, Colors.Green, "Initial scene selection");
                Expect(otherTexture, Colors.Black, "Independent world has no selection");

                var environment = RenderingServer.EnvironmentCreate();
                Check(environment.IsValid() && environment != sceneEnvironment.GetRID(), "An owned RID is distinct from a borrowed RID.");
                RenderingServer.EnvironmentSetBackground(environment, CanvasBackgroundMode.Color);
                RenderingServer.EnvironmentSetBGColor(environment, Colors.Red);
                RenderingServer.WorldSetEnvironment(shared.Canvas, environment);
                RenderingServer.ForceDraw();
                Expect(rootTexture, Colors.Red, "Owned selection takes precedence");
                Expect(childTexture, Colors.Red, "Both viewports see the world's selection");
                Expect(otherTexture, Colors.Black, "Independent world remains isolated");

                Reject<ArgumentException>(() => RenderingServer.WorldSetEnvironment(shared.Canvas, shared.Canvas));
                var standalone = RenderingServer.CanvasCreate();
                Reject<ArgumentException>(() => RenderingServer.WorldSetEnvironment(standalone, environment));
                RenderingServer.FreeRID(standalone);
                RID expiredWorld;
                using (var detached = new World())
                {
                    expiredWorld = detached.Canvas;
                    RenderingServer.WorldSetEnvironment(expiredWorld, environment);
                }
                Reject<ArgumentException>(() => RenderingServer.WorldSetEnvironment(expiredWorld, environment));
                Reject<InvalidOperationException>(() => RenderingServer.FreeRID(sceneEnvironment.GetRID()));
                Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.WorldSetEnvironment(shared.Canvas, default)).GetAwaiter().GetResult());
                Reject<ArgumentOutOfRangeException>(() => RenderingServer.EnvironmentSetCameraID(environment, -1));
                RenderingServer.ForceDraw();
                Expect(rootTexture, Colors.Red, "Invalid bindings leave the current override in place");

                RenderingServer.EnvironmentSetTonemap(environment, CanvasEnvironment.ToneMapper.Linear, .5f, 1);
                RenderingServer.ForceDraw();
                var halfRed = new Color(.5f, 0, 0, 1).LinearToSRGB();
                Expect(childTexture, compatibility ? Colors.Red : halfRed, "Owned settings use the active native effect path");
                RenderingServer.EnvironmentSetTonemap(environment, CanvasEnvironment.ToneMapper.Linear, 1, 1);

                RenderingServer.ViewportSetEnvironmentMode(root.GetViewportRID(), RenderingServer.ViewportEnvironmentMode.Disabled);
                RenderingServer.ForceDraw();
                Expect(rootTexture, Colors.Black, "Disabled root bypasses its override");
                Expect(childTexture, Colors.Black, "Inherited disable bypasses its override");
                RenderingServer.ViewportSetEnvironmentMode(child.GetViewportRID(), RenderingServer.ViewportEnvironmentMode.Enabled);
                RenderingServer.ForceDraw();
                Expect(childTexture, Colors.Red, "Enabled child uses the override independently");
                RenderingServer.ViewportSetEnvironmentMode(root.GetViewportRID(), RenderingServer.ViewportEnvironmentMode.Enabled);

                independent.World = shared;
                RenderingServer.ForceDraw();
                Expect(otherTexture, Colors.Red, "Changing viewport world selects the new world immediately");
                independent.World = other;
                RenderingServer.ForceDraw();
                Expect(otherTexture, Colors.Black, "Returning to the independent world restores its selection");
                RenderingServer.WorldSetEnvironment(other.Canvas, environment);
                RenderingServer.ForceDraw();
                Expect(otherTexture, Colors.Red, "One environment can serve both worlds");

                CameraServer.AddFeed(feed);
                try
                {
                    RenderingServer.EnvironmentSetBackground(environment, CanvasBackgroundMode.CameraFeed);
                    RenderingServer.EnvironmentSetCameraID(environment, feed.GetID());
                    RenderingServer.ForceDraw();
                    Check(!feed.FeedIsActive, "The server setter does not activate the selected feed.");
                    Expect(rootTexture, Colors.Black, "Inactive camera falls back to ordinary clear");
                    feed.FeedIsActive = true;
                    feed.SetRGBImage(source);
                    CameraServer.Process();
                    RenderingServer.ForceDraw();
                    Expect(childTexture, Colors.Cyan, "Owned camera selection produces actual pixels");
                    Expect(otherTexture, Colors.Cyan, "Camera selection is visible through the second binding");
                    RenderingServer.EnvironmentSetCameraID(environment, 0);
                    RenderingServer.ForceDraw();
                    Expect(rootTexture, Colors.Black, "Zero selects no camera");
                }
                finally
                {
                    CameraServer.RemoveFeed(feed);
                    feed.FeedIsActive = false;
                }

                RenderingServer.FreeRID(environment);
                Reject<ArgumentException>(() => RenderingServer.EnvironmentSetBackground(environment, CanvasBackgroundMode.Color));
                Reject<ArgumentException>(() => RenderingServer.FreeRID(environment));
                RenderingServer.ForceDraw();
                Expect(rootTexture, Colors.Green, "Free restores the authored scene selection");
                Expect(otherTexture, Colors.Black, "Free retires the override from both worlds");

                using var temporary = new CanvasEnvironment { BackgroundMode = CanvasBackgroundMode.Color, BackgroundColor = Colors.Blue };
                RenderingServer.WorldSetEnvironment(shared.Canvas, temporary.GetRID());
                RenderingServer.EnvironmentSetCameraID(temporary.GetRID(), feed.GetID());
                Check(temporary.BackgroundCameraFeedID == feed.GetID(), "The camera setter updates a borrowed environment.");
                RenderingServer.ForceDraw();
                Expect(rootTexture, Colors.Blue, "Borrowed selection is supported");
                temporary.Dispose();
                RenderingServer.ForceDraw();
                Expect(childTexture, Colors.Green, "Disposing borrowed state restores the scene selection");

                RenderingServer.WorldSetEnvironment(shared.Canvas, borrowed.GetRID());
                RenderingServer.ForceDraw();
                Expect(rootTexture, Colors.Yellow, "An override can replace disposed state");
                RenderingServer.WorldSetEnvironment(shared.Canvas, default);
                RenderingServer.ForceDraw();
                Expect(rootTexture, Colors.Green, "Empty RID explicitly restores scene selection");

                for (var i = 0; i < 32; i++) RenderingServer.ForceDraw();
                RenderingServer.WorldSetEnvironment(shared.Canvas, borrowed.GetRID());
                for (var i = 0; i < 32; i++) RenderingServer.ForceDraw();
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) RenderingServer.ForceDraw();
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                Check(bytes == 0, $"Prepared server environment frames allocated {bytes} owner bytes.");

                retired = RenderingServer.EnvironmentCreate();
                RenderingServer.WorldSetEnvironment(other.Canvas, retired);
                passed = true;
                root.Tree!.Quit();
            };

            Check(Engine.Run(root) == 0 && passed, "Native environment scenario completed.");
        }

        using var reopened = new Window { Title = "New canvas environment session", Size = Size, World = shared };
        using var reopenedTexture = reopened.GetTexture();
        var checkedShutdown = false;
        reopened.Ready += _ =>
        {
            Reject<ArgumentException>(() => RenderingServer.EnvironmentSetBGColor(retired, Colors.White));
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.ForceDraw();
            Expect(reopenedTexture, Colors.Black, "The previous session's borrowed override has retired.");
            var fresh = RenderingServer.EnvironmentCreate();
            RenderingServer.WorldSetEnvironment(shared.Canvas, fresh);
            RenderingServer.ForceDraw();
            Expect(reopenedTexture, Colors.Black, "A fresh environment defaults to session clear.");
            RenderingServer.EnvironmentSetBackground(fresh, CanvasBackgroundMode.Color);
            RenderingServer.EnvironmentSetBGColor(fresh, Colors.Blue);
            RenderingServer.ForceDraw();
            Expect(reopenedTexture, Colors.Blue, "A server-only world renders without a scene environment node.");
            RenderingServer.FreeRID(fresh);
            Check(!borrowed.IsDisposed, "Closing the server preserves borrowed resources.");
            checkedShutdown = true;
            reopened.Tree!.Quit();
        };
        Check(Engine.Run(reopened) == 0 && checkedShutdown, "Reopening proves session-scoped ownership.");
        Console.WriteLine($"Server environment {(compatibility ? "Compatibility" : "GPU")}: world binding, camera pixels, inherited mode, retirement, and 64 warmed frames (0 owner bytes) passed.");
    }

    private static SubViewport AddViewport(Window root, World world)
    {
        var viewport = new SubViewport { Size = Size, World = world, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        root.AddChild(viewport);
        return viewport;
    }

    private static void Expect(ViewportTexture texture, Color expected, string label)
    {
        using var image = texture.GetImage()!;
        var actual = image.GetPixel(16, 16);
        const float epsilon = .012f;
        Check(MathF.Abs(actual.R - expected.R) < epsilon && MathF.Abs(actual.G - expected.G) < epsilon &&
            MathF.Abs(actual.B - expected.B) < epsilon && MathF.Abs(actual.A - expected.A) < epsilon,
            $"{label}: expected {expected}, read {actual}.");
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
