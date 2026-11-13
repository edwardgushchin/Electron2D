using Electron2D;

internal static class NavigationRouteSelectionTests
{
    private static readonly Vector2 Origin = new(5, 5);
    private static readonly Vector2 Target = new(55, 45);

    internal static void Run(string mode = "all")
    {
        foreach (var api in mode == "all" ? new[] { "map", "query", "agent" } : new[] { mode })
        {
            foreach (var startCost in new[] { 1f, 0f })
            {
                using var fixture = new Fixture(new SubViewport(), api, startCost);
                using var tree = new SceneTree(fixture.Root);
                tree.PhysicsFrame(0);
                var path = fixture.Actor.ReadPath();
                Check(path.Contains(new Vector2(25, 45)) && !path.Contains(new Vector2(25, 5)),
                    $"{api}: distinct positions on the shared directed portal retain the cheaper continuation (start cost {startCost}). Actual: {string.Join(";", path)}");
                if (api == "map")
                    Check(NavigationServer.MapGetPath(fixture.Actor.GetWorld()!.NavigationMap, Origin, Target, false).Contains(new Vector2(25, 45)),
                        "Edge-centered map paths select the same winning corridor.");
                if (api == "query")
                {
                    Check(fixture.Actor.Result.PathRIDs.SequenceEqual(new[] { fixture.Regions[0].GetRID(), fixture.Near.GetRID(), fixture.Regions[1].GetRID(), fixture.Regions[2].GetRID(), fixture.Exit.GetRID(), fixture.Regions[3].GetRID() }),
                        "Winning route retains region/link provenance in parallel with raw points.");
                    Check(MathF.Abs(fixture.Actor.Result.PathLength - (40 + MathF.Sqrt(1300))) < .0001f,
                        "Raw world length is independent of weighted route cost (20 or 10).");
                }
            }
        }
        if (mode == "all" || mode == "query") TiesAndCycles();
        Console.WriteLine($"Navigation route selection passed: {mode}.");
    }

    private static void TiesAndCycles()
    {
        using var fixture = new Fixture(new SubViewport(), "query", 0);
        using var tree = new SceneTree(fixture.Root);
        foreach (var region in fixture.Regions) region.TravelCost = 0;
        tree.PhysicsFrame(0);
        var first = fixture.Actor.ReadPath();
        Check(first.Contains(new Vector2(25, 5)), "Equal complete costs retain the first accepted route in published polygon order.");
        for (var i = 0; i < 16; i++)
            Check(fixture.Actor.ReadPath().SequenceEqual(first), "Equal-priority queue ordering is repeatable.");
        fixture.Far.Bidirectional = true;
        fixture.Near.Bidirectional = true;
        fixture.Exit.Enabled = false;
        tree.PhysicsFrame(0);
        Check(fixture.Actor.ReadPath()[^1] == new Vector2(40, 45),
            "Zero-cost cycles terminate and rebuild to the nearest reached entry's walkable frontier.");
        fixture.Actor.Parameters.PathSearchMaxPolygons = 1;
        Check(fixture.Actor.ReadPath()[^1] == new Vector2(10, 45), "Polygon limits still stop exploration at the start surface.");
        fixture.Actor.Parameters.PathSearchMaxPolygons = 0;
        fixture.Actor.Parameters.ExcludedRegions = [fixture.Regions[1].GetRID()];
        Check(fixture.Actor.ReadPath()[^1] == new Vector2(10, 45), "Region exclusion also removes both incoming link transitions.");
    }

    internal static void RunHost(string api)
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 60;
        using var fixture = new Fixture(new Window { Size = new(64, 64) }, api, 1);
        var window = (Window)fixture.Root;
        var frames = 0;
        var finished = 0;
        fixture.Actor.Agent.NavigationFinished += () => finished++;
        fixture.Actor.PhysicsProcessEnabled = true;
        window.Ready += _ =>
        {
            fixture.ConfigureMap();
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                Check(++frames < 240, "The selected route finishes within 240 rendered frames.");
                if (!fixture.Actor.Done) return;
                Check(RenderingServer.GetCurrentRenderingMethod() == backend && RenderingServer.GetCurrentRenderingDriverName() != "software", "The requested hardware renderer is active.");
                Check(fixture.Actor.Path.Contains(new Vector2(25, 45)) && !fixture.Actor.Path.Contains(new Vector2(25, 5)) && fixture.Actor.Teleports == 2,
                    "Actual movement executes the two winning link transitions and the shared portal.");
                if (api == "agent") Check(finished == 1 && fixture.Actor.Agent.IsTargetReached(), "Agent completion and target events follow real movement.");
                using var image = window.GetTexture().GetImage()!;
                Check(image.GetPixel(55, 45) == Colors.Yellow && image.GetPixel(5, 5) == Colors.Black,
                    "The rendered actor leaves the origin and reaches the chosen route's target.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_NAVIGATION_SNAPSHOT") is { } capture) image.SavePNG(capture);
                window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && fixture.Actor.IsDisposed && RenderingServer.Service is null, "Rendered route host retires its scene and renderer.");
        Console.WriteLine($"Navigation route movement/rendering passed: {api}/{backend}, {frames} frames.");
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly Node Root;
        internal readonly NavigationRegion[] Regions;
        internal readonly NavigationLink Far;
        internal readonly NavigationLink Near;
        internal readonly NavigationLink Exit;
        internal readonly Traveler Actor;
        private readonly NavigationPolygon[] _polygons =
        [
            NavigationTests.Rectangle(0, 0, 10, 50), NavigationTests.Rectangle(20, 0, 10, 50),
            NavigationTests.Rectangle(30, 0, 10, 50), NavigationTests.Rectangle(50, 40, 10, 10)
        ];

        internal Fixture(Node root, string api, float startCost)
        {
            Root = root;
            Regions = _polygons.Select(p => new NavigationRegion { NavigationPolygon = p }).ToArray();
            Regions[0].TravelCost = startCost;
            foreach (var region in Regions)
            {
                NavigationServer.RegionSetUseAsyncIterations(region.GetRID(), false);
                root.AddChild(region);
            }
            Far = Link(Origin, new(25, 5));
            Near = Link(new(5, 15), new(25, 45));
            Exit = Link(new(35, 45), Target);
            Actor = new Traveler(api, Near, Exit) { Position = Origin };
            root.AddChild(Actor);
            root.Ready += _ => ConfigureMap();
        }

        internal void ConfigureMap()
        {
            var map = Actor.GetWorld()!.NavigationMap;
            NavigationServer.MapSetUseAsyncIterations(map, false);
            NavigationServer.MapSetUseEdgeConnections(map, false);
        }

        private NavigationLink Link(Vector2 from, Vector2 to)
        {
            var link = new NavigationLink { StartPosition = from, EndPosition = to, Bidirectional = false, TravelCost = 0 };
            Root.AddChild(link);
            return link;
        }

        public void Dispose()
        {
            Root.Dispose();
            foreach (var polygon in _polygons) polygon.Dispose();
        }
    }

    private sealed class Traveler : Entity
    {
        private readonly string _api;
        private readonly NavigationLink[] _links;
        private int _next = 1;
        internal readonly NavigationAgent Agent = new()
        {
            PathDesiredDistance = .1f,
            TargetDesiredDistance = .1f,
            PathPostprocessing = NavigationPathQueryParameters.PathPostProcessing.None,
            PathSearchMaxPolygons = 7
        };
        internal readonly NavigationPathQueryParameters Parameters = new()
        {
            StartPosition = Origin,
            TargetPosition = Target,
            PostProcessing = NavigationPathQueryParameters.PathPostProcessing.None,
            PathSearchMaxPolygons = 7
        };
        internal readonly NavigationPathQueryResult Result = new();
        internal Vector2[] Path = [];
        internal int Teleports;
        internal bool Done;

        internal Traveler(string api, NavigationLink near, NavigationLink exit)
        {
            _api = api;
            _links = [near, exit];
            if (api == "agent") Agent.TargetPosition = Target;
            AddChild(Agent);
            Agent.LinkReached += waypoint =>
            {
                Check(Teleports < 2 && waypoint.Owner == _links[Teleports], "Agent link events preserve winning owners and order.");
                Position = waypoint.LinkExitPosition!.Value;
                Teleports++;
            };
        }

        internal Vector2[] ReadPath()
        {
            if (_api == "map") return NavigationServer.MapGetPath(GetWorld()!.NavigationMap, Origin, Target, true);
            if (_api == "agent") { Agent.GetNextPathPosition(); return Agent.GetCurrentNavigationPath(); }
            Parameters.Map = GetWorld()!.NavigationMap;
            NavigationServer.QueryPath(Parameters, Result);
            return Result.Path;
        }

        protected override void OnPhysicsProcess(double delta)
        {
            if (Path.Length == 0) Path = ReadPath();
            if (_api == "agent")
            {
                var before = Teleports;
                var next = Agent.GetNextPathPosition();
                if (before == Teleports) Position = Position.MoveToward(next, 1);
                Done = Agent.IsNavigationFinished();
            }
            else if (_next < Path.Length)
            {
                Position = Position.MoveToward(Path[_next], 1);
                if (Position == Path[_next])
                {
                    if (Teleports < 2 && Position == _links[Teleports].StartPosition)
                    {
                        Position = _links[Teleports++].EndPosition;
                        _next++;
                    }
                    _next++;
                }
                Done = _next == Path.Length;
            }
        }

        protected override void OnDraw() => DrawRect(new(-1, -1, 3, 3), Colors.Yellow);
        protected override void Dispose(bool disposing)
        {
            if (disposing) { Parameters.Dispose(); Result.Dispose(); }
            base.Dispose(disposing);
        }
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
