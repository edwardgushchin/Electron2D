using Electron2D;

internal static class CurveTests
{
    internal static void Run()
    {
        Scalar(); Spatial(); CopiesAndScene(); FailureAndConcurrency();
        Console.WriteLine("Scalar and spatial curve checks passed.");
    }

    private static void Scalar()
    {
        using var c = new Curve(); var trace = new List<string>();
        c.Changed += _ => trace.Add("changed"); c.PropertyListChanged += _ => trace.Add("list");
        c.DomainChanged += () => trace.Add("domain"); c.RangeChanged += () => trace.Add("range");
        void Expect(Action edit, string expected) { trace.Clear(); edit(); Check(string.Join(',', trace) == expected, "Scalar events: " + expected); }
        Check(c.PointCount == 0 && c.BakeResolution == 100 && c.MinDomain == 0 && c.MaxDomain == 1 && c.MinValue == 0 && c.MaxValue == 1, "Scalar defaults.");
        Check(c.Sample(.5f) == 0 && c.SampleBaked(.5f) == 0 && c.GetDomainRange() == 1 && c.GetValueRange() == 1, "Empty sampling and ranges.");
        Expect(() => c.BakeResolution = 3, ""); Expect(() => c.MinValue = 0, "range"); Expect(() => c.MaxDomain = 1, "changed,domain");
        Expect(() => c.AddPoint(new(1, 1)), "changed,list"); Expect(() => c.AddPoint(new(0, 0)), "changed,list");
        Near(c.Sample(.25f), .15625f); Near(c.SampleBaked(.25f), .25f);
        Near(c.Sample(-1), 0); Near(c.Sample(2), 1); Near(c.SampleBaked(float.MinValue), 0); Near(c.SampleBaked(float.MaxValue), 1);
        c.SetPointRightMode(0, Curve.TangentMode.Linear); c.SetPointLeftMode(1, Curve.TangentMode.Linear);
        Near(c.GetPointRightTangent(0), 1); Near(c.GetPointLeftTangent(1), 1); Near(c.Sample(.25f), .25f);
        c.SetPointValue(1, 2); Near(c.GetPointRightTangent(0), 2); Near(c.Sample(.25f), .5f);
        Check(c.GetPointPosition(1).Y == 2 && c.MaxValue == 1, "SetPointValue does not clamp or change limits.");
        c.SetPointOffset(1, 1); Check(c.GetPointPosition(1).Y == 1, "Reinsertion clamps value.");
        Expect(() => c.SetPointRightTangent(0, 6), "changed"); Check(c.GetPointRightMode(0) == Curve.TangentMode.Free, "Tangent setter switches its side to Free.");
        c.SetPointLeftTangent(1, -6); Check(c.Sample(.5f) > c.MaxValue, "Tangent overshoot is preserved.");
        Expect(() => c.CleanDupes(), ""); Check(c.PointCount == 2, "CleanDupes preserves separated points.");
        c.AddPoint(new(.5f, .5f)); c.AddPoint(new(.5f + Mathf.Epsilon * .25f, .8f));
        Expect(c.CleanDupes, "changed"); Check(c.PointCount == 3, "Only adjacent near-duplicates are removed.");
        c.ClearPoints(); c.AddPoint(new(0, 0), rightMode: Curve.TangentMode.Linear);
        c.AddPoint(new(.5f, 1), leftMode: Curve.TangentMode.Linear, rightMode: Curve.TangentMode.Linear);
        c.AddPoint(new(1, 0), leftMode: Curve.TangentMode.Linear);
        Near(c.GetPointRightTangent(0), 2); Near(c.GetPointLeftTangent(2), -2);
        c.RemovePoint(1); Near(c.GetPointRightTangent(0), 0); Near(c.GetPointLeftTangent(1), 0);
        Check(c.Sample(.25f) == 0, "Deletion recalculates linear slopes to surviving neighbors.");
        c.ClearPoints(); Expect(() => c.PointCount = 3, "changed,changed,changed,list");
        Check(Enumerable.Range(0, 3).All(i => c.GetPointPosition(i) == Vector2.Zero), "Empty growth uses zero offset for every point.");
        Expect(() => c.PointCount = 4, "changed,list"); Check(c.GetPointPosition(3).X == 1, "Later growth uses maximum offset.");
        Expect(() => c.PointCount = 2, "changed,list"); Expect(() => c.PointCount = 2, "");
        Expect(c.ClearPoints, "changed,list"); Expect(c.ClearPoints, "");
        c.MinDomain = -2; c.MaxDomain = 2; c.MinValue = -4; c.MaxValue = 4;
        c.AddPoint(new(-99, -99)); c.AddPoint(new(99, 99)); Check(c.GetPointPosition(0) == new Vector2(-2, -4) && c.GetPointPosition(1) == new Vector2(2, 4), "Insertion clamps both axes.");
        c.MinDomain = 1; c.MaxDomain = -1; c.MinValue = 3; c.MaxValue = -3;
        Check(c.MinDomain == -2 && c.MaxDomain == 2 && c.MinValue == -4 && c.MaxValue == 4, "Limits retain existing points.");
        c.BakeResolution = 1; Near(c.SampleBaked(-100), 4); c.BakeResolution = 9; c.Bake(); Near(c.SampleBaked(0), 0);
        c.ResetState(); Near(c.SampleBaked(0), 0);
        c.AddPoint(new(0, 2)); var moved = c.SetPointOffset(2, -1); Check(moved == 1 && c.GetPointPosition(moved).X == -1, "Horizontal movement returns new sorted index.");
        var descriptor = c.GetPropertyList().OfType<PropertyDescriptor<Curve, Vector2>>().Single(p => p.Name == "Point[1].Position");
        descriptor.SetValue(c, new(.5f, 3)); Check(c.GetPointPosition(2) == new Vector2(.5f, 3), "Typed indexed position moves and changes value.");
        Check(!c.GetPropertyList().Any(p => p.Name == "Point[0].LeftMode") && !c.GetPropertyList().Any(p => p.Name == "Point[2].RightMode"), "Tooling hides unavailable outer handles.");
        Reject<ArgumentOutOfRangeException>(() => c.BakeResolution = 0); Reject<ArgumentOutOfRangeException>(() => c.BakeResolution = 1001);
        Reject<ArgumentOutOfRangeException>(() => c.PointCount = -1); Reject<ArgumentOutOfRangeException>(() => c.Sample(float.NaN));
        Reject<ArgumentOutOfRangeException>(() => c.SampleBaked(float.PositiveInfinity)); Reject<ArgumentOutOfRangeException>(() => c.MinDomain = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => c.AddPoint(Vector2.Zero, leftMode: Curve.TangentMode.Count));
        Reject<ArgumentOutOfRangeException>(() => c.GetPointLeftTangent(-1)); Reject<ArgumentOutOfRangeException>(() => c.SetPointRightMode(0, (Curve.TangentMode)10));
        Reject<ArgumentOutOfRangeException>(() => c.SetPointLeftTangent(0, float.NaN)); Reject<ArgumentOutOfRangeException>(() => c.RemovePoint(99));
        c.Bake(); for (var i = 0; i < 100; i++) { c.Sample(.2f); c.SampleBaked(.2f); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) { c.Sample(.2f); c.SampleBaked(.2f); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warm scalar sampling allocates nothing.");
        using var wide = new Curve { MaxDomain = 1e38f, BakeResolution = 101 };
        wide.AddPoint(Vector2.Zero, rightMode: Curve.TangentMode.Linear); wide.AddPoint(new(1e38f, 1), leftMode: Curve.TangentMode.Linear);
        Near(wide.Sample(5e37f), .5f); Near(wide.SampleBaked(5e37f), .5f);
    }

    private static void Spatial()
    {
        using var p = new PathCurve(); var trace = new List<string>(); p.Changed += _ => trace.Add("changed"); p.PropertyListChanged += _ => trace.Add("list");
        Check(p.PointCount == 0 && p.BakeInterval == 5 && p.GetBakedLength() == 0 && p.GetBakedPoints().Length == 0 && p.Tessellate().Length == 0 && p.TessellateEvenLength().Length == 0, "Spatial defaults.");
        Reject<InvalidOperationException>(() => p.Sample(0, 0)); Reject<InvalidOperationException>(() => p.SampleBaked()); Reject<InvalidOperationException>(() => p.GetClosestPoint(Vector2.Zero));
        p.AddPoint(new(3, 4)); Check(string.Join(',', trace) == "changed,list", "Spatial add notifications."); trace.Clear();
        Near(p.Sample(20, -100), new(3, 4)); Near(p.SampleBaked(), new(3, 4)); Near(p.GetClosestPoint(new(90, 90)), new(3, 4));
        Check(p.Tessellate().Length == 1 && p.TessellateEvenLength().Length == 0 && p.GetClosestOffset(Vector2.Zero) == 0 && p.SampleBakedWithRotation().Origin == new Vector2(3, 4), "Single-point semantics.");
        p.ClearPoints(); p.AddPoint(Vector2.Zero, outHandle: new(10, 0)); p.AddPoint(new(30, 0), inHandle: new(-10, 0));
        Near(p.Sample(0, .25f), new(7.5f, 0)); Near(p.Sample(0, -.5f), new(-15, 0)); Near(p.Samplef(.5f), new(15, 0));
        Near(p.Sample(-1, .5f), Vector2.Zero); Near(p.Sample(9, .5f), new(30, 0)); Near(p.Samplef(-1), Vector2.Zero);
        var baked = p.GetBakedPoints(); Check(baked.Length == 9, "Straight length baking uses power-of-two subdivision.");
        Near(p.GetBakedLength(), 30); Near(p.SampleBaked(15), new(15, 0)); Near(p.SampleBaked(15, true), new(15, 0));
        Near(p.SampleBaked(-2), Vector2.Zero); Near(p.SampleBaked(99), new(30, 0));
        var posture = p.SampleBakedWithRotation(15); Near(posture.X, Vector2.Right); Near(posture.Y, Vector2.Down); Near(posture.Origin, new(15, 0));
        Near(p.GetClosestPoint(new(13, 7)), new(13, 0)); Near(p.GetClosestOffset(new(13, 7)), 13);
        baked[0] = new(999, 999); Check(p.GetBakedPoints()[0] == Vector2.Zero, "Baked arrays do not expose cache storage.");
        Check(p.Tessellate().Length == 2 && p.TessellateEvenLength(0).Length == 2 && p.TessellateEvenLength(5, 4).Length == 9, "Tessellation density and depth.");
        p.SetPointOut(0, new(0, 20)); p.SetPointIn(1, new(0, 20)); Near(p.Sample(0, .5f), new(15, 15));
        var angular = p.Tessellate(0, 4); Check(angular.Length == 3 && angular[1] == new Vector2(15, 15), "Angular depth zero still evaluates its midpoint.");
        var coarse = p.GetBakedLength(); p.BakeInterval = .1f; var fine = p.GetBakedLength(); Check(fine > coarse && fine > 40 && fine < 50, "Length converges when cache density increases.");
        p.SetPointPosition(1, Vector2.Zero); Check(p.GetBakedLength() > 0 && p.GetBakedPoints().Length > 2, "Closed nonconstant segment must not collapse to its chord.");
        p.SetPointOut(0, new(10, 0)); p.SetPointIn(1, new(-10, 0)); Check(p.Sample(0, .5f) == Vector2.Zero && p.GetBakedLength() > 10, "Closed segment with coincident midpoint retains both lobes.");
        p.ClearPoints(); p.PointCount = 3; p.SetPointPosition(2, new(20, 0)); p.BakeInterval = 5;
        Near(p.GetClosestPoint(new(12, 3)), new(12, 0)); Near(p.GetClosestOffset(new(12, 3)), 12);
        Check(p.SampleBakedWithRotation().IsFinite(), "Zero-length leading interval has finite posture.");
        p.PointCount = 2; Near(p.GetClosestPoint(new(5, 6)), Vector2.Zero); Near(p.GetClosestOffset(new(5, 6)), 0); Check(p.SampleBakedWithRotation() == Transform.Identity, "Entirely degenerate curve stays well-defined.");
        p.AddPoint(new(2, 3), index: 100); Check(p.GetPointPosition(2) == new Vector2(2, 3), "Past-end insertion appends.");
        p.GetPropertyList().OfType<PropertyDescriptor<PathCurve, Vector2>>().Single(d => d.Name == "Point[1].Out").SetValue(p, new(1, 2));
        Check(p.GetPointOut(1) == new Vector2(1, 2), "Typed handle descriptors.");
        Reject<ArgumentOutOfRangeException>(() => p.BakeInterval = 0); Reject<ArgumentOutOfRangeException>(() => p.Tessellate(21));
        Reject<ArgumentOutOfRangeException>(() => p.TessellateEvenLength(toleranceLength: float.NaN)); Reject<ArgumentOutOfRangeException>(() => p.Tessellate(toleranceDegrees: -1));
        Reject<ArgumentOutOfRangeException>(() => p.Samplef(float.PositiveInfinity)); Reject<ArgumentOutOfRangeException>(() => p.GetPointIn(-1));
        Reject<ArgumentException>(() => p.SetPointOut(0, new(float.NaN, 0))); Reject<ArgumentException>(() => p.GetClosestOffset(new(float.NaN, 0)));
        p.GetBakedLength(); for (var i = 0; i < 100; i++) { p.SampleBaked(1); p.SampleBakedWithRotation(1); p.GetClosestOffset(Vector2.One); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) { p.SampleBaked(1); p.SampleBakedWithRotation(1); p.GetClosestOffset(Vector2.One); }
        Check(before == GC.GetAllocatedBytesForCurrentThread(), "Warm spatial queries allocate nothing.");
        p.SetPointPosition(0, new(float.MaxValue, 0)); p.SetPointOut(0, new(float.MaxValue, 0)); Reject<InvalidOperationException>(() => p.GetBakedLength());
        p.SetPointPosition(0, Vector2.Zero); p.SetPointOut(0, Vector2.Zero); Check(float.IsFinite(p.GetBakedLength()), "Failed bake does not poison a later valid cache.");
    }

    private static void CopiesAndScene()
    {
        using var scalar = new Curve { MinDomain = -2, MaxDomain = 3, MinValue = -4, MaxValue = 5, BakeResolution = 77 };
        scalar.AddPoint(new(-1, 2), 3, 4); scalar.AddPoint(new(2, 3), 5, 6); scalar.SetPointValue(1, 99);
        using var path = new PathCurve { BakeInterval = 2 }; path.AddPoint(Vector2.Zero, new(-2, 0), new(3, 0)); path.AddPoint(new(9, 2), new(-4, 0), new(5, 0));
        foreach (var deep in new[] { false, true })
        {
            using var s = (Curve)scalar.Duplicate(deep); using var p = (PathCurve)path.Duplicate(deep);
            Check(s.MinDomain == -2 && s.MaxDomain == 3 && s.MinValue == -4 && s.MaxValue == 5 && s.BakeResolution == 77 && s.GetPointPosition(1).Y == 99 && s.GetPointRightTangent(1) == 6, "Scalar copy retains exact state without setter reclamping.");
            Check(p.BakeInterval == 2 && p.GetPointIn(1) == new Vector2(-4, 0) && p.GetPointOut(1) == new Vector2(5, 0), "Spatial copy retains handles and bake policy.");
            s.ClearPoints(); p.ClearPoints(); Check(scalar.PointCount == 2 && path.PointCount == 2, "Copies own point containers.");
        }
        using var target = new Curve(); target.CopyFromResource(scalar); Check(target.GetPointPosition(1).Y == 99, "CopyFromResource preserves exact scalar state.");
        using var targetPath = new PathCurve(); targetPath.CopyFromResource(path); Check(targetPath.GetBakedPoints().SequenceEqual(path.GetBakedPoints()), "Spatial CopyFromResource rebuilds matching cache.");
        using var node = new CurveConsumer { Scalar = scalar, Path = path }; using var packed = new PackedScene(); packed.Pack(node);
        using var shared = (CurveConsumer)packed.Instantiate(); Check(ReferenceEquals(shared.Path, path) && ReferenceEquals(shared.Scalar, scalar), "Packed resources are borrowed by default.");
        scalar.ResourceLocalToScene = path.ResourceLocalToScene = true; packed.Pack(node); using var local = (CurveConsumer)packed.Instantiate();
        var sLocal = local.Scalar!; var pLocal = local.Path!; Check(!ReferenceEquals(sLocal, scalar) && !ReferenceEquals(pLocal, path), "Scene-local curves duplicate independently.");
        using (var tree = new SceneTree(local)) { local.ProcessEnabled = true; tree.ProcessFrame(.5); Check(local.Position == pLocal.SampleBaked(sLocal.Sample(.5f)), "Scene consumer executes both resource APIs."); }
        Check(sLocal.IsDisposed && pLocal.IsDisposed && !scalar.IsDisposed && !path.IsDisposed, "Root owns only local resource copies.");
    }

    private static void FailureAndConcurrency()
    {
        using var scalar = new Curve(); using var path = new PathCurve(); scalar.AddPoint(Vector2.Zero); scalar.AddPoint(Vector2.One); path.AddPoint(Vector2.Zero); path.AddPoint(new(10, 0));
        Action<Resource> fail = _ => throw new ApplicationException("curve observer"); scalar.Changed += fail; Reject<ApplicationException>(() => scalar.SetPointValue(1, 2)); scalar.Changed -= fail;
        Check(scalar.GetPointPosition(1).Y == 2, "Scalar callback exception retains committed edit."); path.Changed += fail; Reject<ApplicationException>(() => path.SetPointIn(1, new(-1, 0))); path.Changed -= fail;
        Check(path.GetPointIn(1) == new Vector2(-1, 0), "Spatial callback exception retains edit.");
        Task.WaitAll(Task.Run(() => { for (var i = 0; i < 300; i++) { scalar.SetPointValue(1, i % 3); path.SetPointIn(1, new(-i % 3, 0)); } }),
            Task.Run(() => { for (var i = 0; i < 300; i++) { Check(float.IsFinite(scalar.SampleBaked(.5f)), "Concurrent scalar sample."); Check(path.SampleBaked(2).IsFinite(), "Concurrent path sample."); } }));
        using var dying = new Curve(); dying.Changed += c => c.Dispose(); dying.PointCount = 3; Check(dying.IsDisposed, "Disposal during growing point notifications stops safely.");
        using var dyingPath = new PathCurve(); dyingPath.Changed += c => c.Dispose(); dyingPath.PointCount = 3; Check(dyingPath.IsDisposed, "Path disposal during growth stops safely.");
        scalar.Dispose(); path.Dispose(); Reject<ObjectDisposedException>(() => scalar.Sample(0)); Reject<ObjectDisposedException>(() => path.GetBakedLength());
    }

    private sealed class CurveConsumer : Entity
    {
        internal Curve? Scalar;
        internal PathCurve? Path;
        protected override void OnProcess(double delta) { Position = Path!.SampleBaked(Scalar!.Sample((float)delta)); }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(new PropertyDescriptor[]
        {
            new PropertyDescriptor<CurveConsumer, Curve?>(nameof(Scalar), n => n.Scalar, (n, v) => n.Scalar = v, stored: true),
            new PropertyDescriptor<CurveConsumer, PathCurve?>(nameof(Path), n => n.Path, (n, v) => n.Path = v, stored: true),
        });
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new CurveConsumer();
    }
    private static void Near(float actual, float expected) => Check(Math.Abs(actual - expected) < .0001f, $"Expected {expected}, got {actual}.");
    private static void Near(Vector2 actual, Vector2 expected) { Near(actual.X, expected.X); Near(actual.Y, expected.Y); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
