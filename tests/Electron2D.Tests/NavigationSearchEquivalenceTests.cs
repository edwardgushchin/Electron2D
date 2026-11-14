using Electron2D;
using System.Text.Json;

internal static class NavigationSearchEquivalenceTests
{
    internal static void Run()
    {
        foreach (var translated in new[] { false, true }) SlantedCycle(translated);
        Diamond(false);
        Diamond(true);
        Diamond(false, true);
        Diamond(true, true);
        TraveledCostTie();
        Console.WriteLine("Navigation slanted zero-cost cycles and independent tied corridor/provenance cases passed.");
    }

    private static void SlantedCycle(bool translated)
    {
        var center = translated ? new Vector2(32, 32) : Vector2.Zero;
        Vector2[] rays = [new(8, 4), new(3, 9), new(-4, 8), new(-9, 3), new(-8, -4), new(-3, -9), new(4, -8), new(9, -3)];
        using var scene = new TestScene(center + (rays[0] + rays[1]) * .4f, new(100, 70));
        var fan = new List<NavigationRegion>();
        for (var i = 0; i < rays.Length; i++)
        {
            var a = rays[i];
            var b = rays[(i + 1) % rays.Length];
            Check(a.X != 0 && a.Y != 0 && a.Dot(b) > 0 && a.Cross(b) != 0,
                "Every shared radial portal is slanted and consecutive projections contract without an immediate obtuse clamp.");
            fan.Add(scene.Surface([center, center + a, center + b], 0));
        }
        var island = scene.Surface([new(90, 60), new(110, 60), new(110, 80), new(90, 80)], 0);

        // Independently exercise repeated projections around the geometric cycle, without a step cap.
        var seen = new HashSet<Vector2>();
        var point = scene.Start;
        while (seen.Add(point))
            for (var i = 1; i <= rays.Length; i++)
                point = Project(point, center, center + rays[i % rays.Length]);
        Check(seen.Count > 2, "This cycle produces new entry positions on successive traversals.");

        scene.Verify((path, owners) =>
        {
            Check(path.Length > 0 && path.All(p => p.IsFinite()) && path[^1] != scene.Target,
                "Unbounded zero-cost search exhausts the slanted fan and returns a finite reachable frontier.");
            Check(fan.Any(r => NavigationServer.RegionGetClosestPoint(r.GetRID(), path[^1]).IsEqualApprox(path[^1])),
                "The fallback endpoint lies on the reached fan, not on the disconnected island.");
            Check(!owners.Contains(island.GetRID()), "Fallback provenance excludes the disconnected target region.");
        });
        Console.WriteLine($"Slanted cycle translated={translated}: {seen.Count} distinct full projection rounds; unlimited polygon/distance search completed through both queries and agent.");
    }

    private static Vector2 Project(Vector2 p, Vector2 a, Vector2 b)
    {
        var x = (double)b.X - a.X;
        var y = (double)b.Y - a.Y;
        var t = Math.Clamp((((double)p.X - a.X) * x + ((double)p.Y - a.Y) * y) / (x * x + y * y), 0, 1);
        return new((float)(a.X + x * t), (float)(a.Y + y * t));
    }

    private static void Diamond(bool lowerFirst, bool bottomEdgeFirst = false)
    {
        using var scene = new TestScene(new(5, 15), new(35, 15));
        Vector2[] startVertices = [new(0, 10), new(10, 10), new(10, 20), new(0, 20)];
        scene.Surface(bottomEdgeFirst ? [.. startVertices.Skip(2), .. startVertices.Take(2)] : startVertices);
        scene.Rectangle(30, 10, 10, 10);
        var first = Branch(lowerFirst);
        var second = Branch(!lowerFirst);
        var name = (lowerFirst ? "diamond-lower-first" : "diamond-upper-first") + (bottomEdgeFirst ? "-bottom-edge-first" : "");
        var selected = lowerFirst == bottomEdgeFirst ? first : second;
        var rejected = lowerFirst == bottomEdgeFirst ? second : first;
        // The independent pinned execution selects the first authored exit, even after region-order reversal.
        scene.Verify((path, owners) =>
        {
            Check(path[^1] == scene.Target && path.Any(p => bottomEdgeFirst ? p.Y >= 20 : p.Y <= 10) &&
                !path.Any(p => bottomEdgeFirst ? p.Y <= 10 : p.Y >= 20),
                "Equal complete geometric costs preserve the independently selected authored exit: " + name + " " + string.Join(';', path));
            if (owners.Length > 0)
                Check(selected.All(r => owners.Contains(r.GetRID())) && rejected.All(r => !owners.Contains(r.GetRID())),
                    "The tied corridor retains the selected branch's exact region provenance.");
        }, name);
        Console.WriteLine($"Pinned diamond lowerFirst={lowerFirst}, bottomEdgeFirst={bottomEdgeFirst}: corridor and provenance matched.");

        NavigationRegion[] Branch(bool lower)
        {
            var y = lower ? 20 : 0;
            return [scene.Rectangle(0, y, 10, 10), scene.Rectangle(10, y, 20, 10), scene.Rectangle(30, y, 10, 10)];
        }
    }

    private static void TraveledCostTie()
    {
        using var scene = new TestScene(Vector2.Zero, new(30, 0));
        scene.Rectangle(-1, -1, 23, 2);
        scene.Rectangle(29, -1, 2, 2);
        var early = scene.Link(new(10, 0), scene.Target);
        var late = scene.Link(new(20, 0), scene.Target);
        // Both f values are exactly 30 with minimum multiplier 1; g=20 must beat g=10 despite later publication.
        scene.Verify((path, owners) =>
        {
            Check(path.Contains(new Vector2(20, 0)) && !path.Contains(new Vector2(10, 0)),
                "Equal estimated totals prefer greater accumulated cost before polygon/discovery order.");
            if (owners.Length > 0)
                Check(owners.Contains(late.GetRID()) && !owners.Contains(early.GetRID()), "The greater-g branch retains its exact link identity.");
        }, "greater-traveled-cost");
        Console.WriteLine("Collinear link alternatives: f=30 for both, later g=20 wins over earlier g=10.");
    }

    private sealed class TestScene : IDisposable
    {
        internal readonly Vector2 Start;
        internal readonly Vector2 Target;
        private readonly SubViewport _root = new();
        private readonly Entity _actor;
        private readonly NavigationAgent _agent = new()
        {
            PathSearchMaxPolygons = 0,
            PathSearchMaxDistance = 0,
            PathPostprocessing = NavigationPathQueryParameters.PathPostProcessing.None
        };
        private readonly List<NavigationPolygon> _polygons = [];
        private readonly List<RID> _owners = [];
        private int _regionCount;

        internal TestScene(Vector2 start, Vector2 target)
        {
            Start = start;
            Target = target;
            _actor = new() { Position = start };
            _actor.AddChild(_agent);
            _root.AddChild(_actor);
            _root.Ready += _ =>
            {
                NavigationServer.MapSetUseAsyncIterations(_root.World!.NavigationMap, false);
                NavigationServer.MapSetUseEdgeConnections(_root.World.NavigationMap, false);
            };
        }

        internal NavigationRegion Surface(Vector2[] vertices, float cost = 1)
        {
            var polygon = new NavigationPolygon();
            polygon.SetVertices(vertices);
            polygon.AddPolygon(Enumerable.Range(0, vertices.Length).ToArray());
            _polygons.Add(polygon);
            var region = new NavigationRegion { NavigationPolygon = polygon, TravelCost = cost };
            NavigationServer.RegionSetUseAsyncIterations(region.GetRID(), false);
            _root.AddChild(region);
            _owners.Add(region.GetRID());
            _regionCount++;
            return region;
        }

        internal NavigationRegion Rectangle(float x, float y, float width, float height) =>
            Surface([new(x, y), new(x + width, y), new(x + width, y + height), new(x, y + height)]);

        internal NavigationLink Link(Vector2 from, Vector2 to)
        {
            var link = new NavigationLink { StartPosition = from, EndPosition = to, Bidirectional = false };
            _root.AddChild(link);
            _owners.Add(link.GetRID());
            return link;
        }

        internal void Verify(Action<Vector2[], RID[]> inspect, string? referenceCase = null)
        {
            using var tree = new SceneTree(_root);
            tree.PhysicsFrame(0);
            var map = _root.World!.NavigationMap;
            Check(NavigationServer.MapGetRegions(map).SequenceEqual(_owners.Take(_regionCount)) &&
                NavigationServer.MapGetLinks(map).SequenceEqual(_owners.Skip(_regionCount)),
                "Map membership preserves attachment order after prior fixtures recycled server dictionary slots.");
            JsonElement reference = default;
            if (referenceCase is not null)
            {
                using var stream = typeof(NavigationSearchEquivalenceTests).Assembly.GetManifestResourceStream("TestNavigation.search-ties-reference.json")!;
                using var document = JsonDocument.Parse(stream);
                Check(document.RootElement.GetProperty("commit").GetString() == "ed1daf0bf001b61586d9930840f2f1394092c079", "Independent fixture pin.");
                reference = document.RootElement.GetProperty("cases").EnumerateArray().First(c => c.GetProperty("name").GetString() == referenceCase).Clone();
            }
            foreach (var optimize in new[] { false, true })
            {
                var path = NavigationServer.MapGetPath(map, Start, Target, optimize);
                inspect(path, []);
                if (referenceCase is not null) Compare(path, reference.GetProperty(optimize ? "optimized_path" : "raw_path"));
            }
            using var parameters = new NavigationPathQueryParameters
            {
                Map = map,
                StartPosition = Start,
                TargetPosition = Target,
                PathSearchMaxPolygons = 0,
                PathSearchMaxDistance = 0,
                PostProcessing = referenceCase is null ? NavigationPathQueryParameters.PathPostProcessing.None : NavigationPathQueryParameters.PathPostProcessing.EdgeCentered
            };
            using var result = new NavigationPathQueryResult();
            NavigationServer.QueryPath(parameters, result);
            inspect(result.Path, result.PathRIDs);
            if (referenceCase is not null)
            {
                var pinned = reference.GetProperty("limited_paths")[0];
                Compare(result.Path, pinned.GetProperty("points"));
                var points = pinned.GetProperty("points").EnumerateArray().Select(Point).ToArray();
                var ids = pinned.GetProperty("regions").EnumerateArray().Select(v => v.GetInt32()).ToArray();
                var expected = ids.Where((id, i) => i == 0 || points[i] != points[i - 1] || id != ids[i - 1]).Select(i => _owners[i]);
                Check(result.PathRIDs.SequenceEqual(expected), "The complete query provenance matches independent pinned execution.");
            }
            _agent.PathPostprocessing = parameters.PostProcessing;
            _agent.TargetPosition = Target;
            _agent.GetNextPathPosition();
            inspect(_agent.GetCurrentNavigationPath(), _agent.GetCurrentNavigationResult().PathRIDs);
            Check(_agent.GetCurrentNavigationPath().SequenceEqual(result.Path) &&
                _agent.GetCurrentNavigationResult().PathRIDs.SequenceEqual(result.PathRIDs), "Independent agent query preserves corridor and provenance.");
            var first = _owners[0];
            NavigationServer.RegionSetMap(first, default);
            NavigationServer.RegionSetMap(first, map);
            NavigationServer.Synchronize();
            var expectedRegions = _owners.Take(_regionCount).ToArray();
            expectedRegions[0] = expectedRegions[^1];
            expectedRegions[^1] = first;
            Check(NavigationServer.MapGetRegions(map).SequenceEqual(expectedRegions),
                "Detaching swaps the last member into its slot, and reattaching appends it.");
        }

        private static Vector2 Point(JsonElement p) => new(p[0].GetSingle(), p[1].GetSingle());
        private static void Compare(Vector2[] actual, JsonElement data)
        {
            var expected = new List<Vector2>();
            foreach (var value in data.EnumerateArray())
            {
                var p = Point(value);
                if (expected.Count == 0 || expected[^1] != p) expected.Add(p);
            }
            Check(actual.Length == expected.Count && actual.Zip(expected).All(p => p.First.IsEqualApprox(p.Second)),
                "Pinned path comparison: " + string.Join(';', actual) + " expected " + string.Join(';', expected));
        }

        public void Dispose()
        {
            _root.Dispose();
            foreach (var polygon in _polygons) polygon.Dispose();
        }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
