using Electron2D;

internal static class ObjectPropertyTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread,
            Environment.GetEnvironmentVariable("ELECTRON2D_TEST_THREADED_COLLIDER") == "1");
        Lifetime(); Discovery();
        using var probe = new Probe();
        Check(!probe.PropertyCanRevert(Probe.ValueProperty) && Probe.ValueProperty.TryGetRevertValue(probe, out var value) && value == 7,
            "Default availability is distinct from a changed value.");
        Probe.ValueProperty.SetValue(probe, 9); probe.Default = 11;
        Check(Probe.ValueProperty.GetValue(probe) == 9 && probe.PropertyCanRevert(Probe.ValueProperty), "Delegates observe live state and defaults.");
        probe.RevertProperty(Probe.ValueProperty); Check(probe.Value == 11 && !probe.PropertyCanRevert(Probe.ValueProperty), "Revert uses the current default.");
        probe.Default = -1; Reject<ArgumentOutOfRangeException>(() => probe.RevertProperty(Probe.ValueProperty)); Check(probe.Value == 11, "Validation rejects before writing.");
        probe.Default = 7; probe.FailRevert = true; Reject<ArithmeticException>(() => probe.RevertProperty(Probe.ValueProperty)); Check(probe.Value == 11, "Factory failure does not write."); probe.FailRevert = false;
        probe.FailGet = true; Reject<ArithmeticException>(() => Probe.ValueProperty.GetValue(probe)); Reject<ArithmeticException>(() => probe.PropertyCanRevert(Probe.ValueProperty)); probe.FailGet = false;
        probe.FailSet = true; Reject<ArithmeticException>(() => Probe.ValueProperty.SetValue(probe, 12)); Check(probe.Value == 12, "Committed setter state remains visible after its exception."); probe.FailSet = false;
        probe.ForceNoRevert = true; Check(!probe.PropertyCanRevert(Probe.ValueProperty) && Probe.ValueProperty.CanRevert(probe), "The owner can customize tooling's revert policy.");
        probe.RevertProperty(Probe.ValueProperty); Check(probe.Value == 7, "Policy inspection does not suppress an explicit descriptor write.");
        var readOnly = new PropertyDescriptor<Probe, int>("ReadOnly", p => p.Value);
        Check(!readOnly.CanRevert(probe) && !readOnly.TryGetRevertValue(probe, out value) && value == 0, "Absent defaults are explicit.");
        Reject<InvalidOperationException>(() => readOnly.SetValue(probe, 1)); Reject<InvalidOperationException>(() => readOnly.Revert(probe));
        Reject<ArgumentException>(() => new PropertyDescriptor<Probe, int>("Bad", p => p.Value, revertValue: p => 7));
        Reject<ArgumentException>(() => new PropertyDescriptor<Probe, int>("Bad", p => p.Value, stored: true));
        Reject<ArgumentNullException>(() => Probe.ValueProperty.GetValue(null!)); Reject<ArgumentNullException>(() => Probe.ValueProperty.SetValue(null!, 1));
        Reject<ArgumentNullException>(() => Probe.ValueProperty.TryGetRevertValue(null!, out _)); Reject<ArgumentNullException>(() => probe.PropertyCanRevert(null!));
        using var wrong = new Node(); Reject<ArgumentException>(() => wrong.PropertyCanRevert(Probe.ValueProperty)); Reject<ArgumentException>(() => wrong.RevertProperty(Probe.ValueProperty));
        using var arrays = new Probe(); var array = new PropertyDescriptor<Probe, int[]>("Array", p => p.Items, (p, v) => p.Items = v, p => p.DefaultItems);
        Check(!array.CanRevert(arrays), "Array reversion compares elements."); arrays.Items[0] = 3; Check(array.CanRevert(arrays), "Changed elements differ."); array.Revert(arrays);
        Check(ReferenceEquals(arrays.Items, arrays.DefaultItems), "Descriptor delegates define value ownership without a hidden copy.");
        var nullable = new PropertyDescriptor<Probe, string?>("Nullable", p => p.Text, (p, v) => p.Text = v, _ => null);
        Check(nullable.TryGetRevertValue(arrays, out var text) && text is null && !nullable.CanRevert(arrays), "A null default is available.");
        arrays.Text = "changed"; nullable.Revert(arrays); Check(arrays.Text is null, "Null defaults restore normally.");
        Physics(backend);
        Console.WriteLine($"Object properties {backend}: discovery, live delegates/defaults, validation/errors, teardown inspection and 128 warm/128 measured full frames at 0/0 B passed.");
    }

    private static void Lifetime()
    {
        var probe = new Probe(); var inspected = false;
        probe.BeforeDelete = () =>
        {
            Check(probe.IsDisposed && Probe.ValueProperty.GetValue(probe) == 7 && probe.GetIndexed(Probe.ValueProperty) == 7,
                "Typed reads remain available on the teardown thread.");
            Check(Probe.ValueProperty.TryGetRevertValue(probe, out var value) && value == 7 && !probe.PropertyCanRevert(Probe.ValueProperty), "Teardown can inspect defaults.");
            Reject<ObjectDisposedException>(() => Probe.ValueProperty.SetValue(probe, 8)); Reject<ObjectDisposedException>(() => probe.RevertProperty(Probe.ValueProperty));
            Task.Run(() =>
            {
                Reject<ObjectDisposedException>(() => Probe.ValueProperty.GetValue(probe)); Reject<ObjectDisposedException>(() => Probe.ValueProperty.TryGetRevertValue(probe, out _));
                Reject<ObjectDisposedException>(() => Probe.ValueProperty.CanRevert(probe)); Reject<ObjectDisposedException>(() => probe.GetPropertyList());
            }).GetAwaiter().GetResult(); inspected = true;
        };
        probe.Dispose(); Check(inspected, "Pre-delete inspection actually ran.");
        Reject<ObjectDisposedException>(() => Probe.ValueProperty.GetValue(probe)); Reject<ObjectDisposedException>(() => Probe.ValueProperty.TryGetRevertValue(probe, out _));
        Reject<ObjectDisposedException>(() => probe.PropertyCanRevert(Probe.ValueProperty)); Reject<ObjectDisposedException>(() => probe.RevertProperty(Probe.ValueProperty));
    }

    private static void Discovery()
    {
        using var probe = new Probe(); var first = probe.GetPropertyList();
        Check(first.Count(p => p.Name == "Value") == 1 && first.Contains(Probe.ValueProperty), "Custom descriptors append to inherited metadata.");
        Reject<NotSupportedException>(() => ((IList<PropertyDescriptor>)first).Add(Probe.ValueProperty));
        probe.Hide = true; var second = probe.GetPropertyList(); Check(!second.Contains(Probe.ValueProperty) && first.Contains(Probe.ValueProperty), "Filtering does not mutate a previous snapshot.");
        probe.Hide = false; probe.Replace = true; Check(probe.GetPropertyList().Contains(Probe.Replacement), "Validation may replace a compatible descriptor."); probe.Replace = false;
        probe.Mode = 1; Reject<InvalidOperationException>(() => probe.GetPropertyList()); probe.Mode = 2; Reject<ArgumentNullException>(() => probe.GetPropertyList());
        probe.Mode = 3; Reject<ArgumentException>(() => probe.GetPropertyList()); probe.Mode = 4; Reject<ArithmeticException>(() => probe.GetPropertyList()); probe.Mode = 0;
        var notices = 0; probe.PropertyListChanged += _ => notices++; probe.Refresh(); Check(notices == 1, "Structural notification is synchronous.");
        void Failure(ElectronObject _) => throw new ArithmeticException(); probe.PropertyListChanged += Failure;
        Reject<ArithmeticException>(probe.Refresh); Check(notices == 2, "Callback failure preserves prior deliveries."); probe.PropertyListChanged -= Failure;
    }

    private static void Physics(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 1 }; var body = new ProbeBody { CanSleep = false, GravityScale = 0, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); using var tree = new SceneTree(root);
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Selected numerical backend without fallback.");
        var list = body.GetPropertyList(); var mass = list.OfType<PropertyDescriptor<RigidBody, float>>().Single(p => p.Name == nameof(RigidBody.Mass));
        var velocity = list.OfType<PropertyDescriptor<RigidBody, Vector2>>().Single(p => p.Name == nameof(RigidBody.LinearVelocity));
        Check(mass.IsStored && !mass.IsReadOnly && mass.OwnerType == typeof(RigidBody) && mass.ValueType == typeof(float), "Production physical descriptors retain exact metadata.");
        mass.SetValue(body, 2); Check(body.Mass == 2 && body.PropertyCanRevert(mass), "Descriptor writes reach physical properties.");
        Reject<ArgumentOutOfRangeException>(() => mass.SetValue(body, 0)); Check(body.Mass == 2, "Physical range rejection preserves state."); body.RevertProperty(mass); Check(body.Mass == 1, "Physical reversion restores mass.");
        Task.Run(() => { Reject<InvalidOperationException>(() => velocity.GetValue(body)); Reject<InvalidOperationException>(() => velocity.SetValue(body, Vector2.Zero)); }).GetAwaiter().GetResult();
        var capture = body.BeginSceneCapture(); try { Reject<InvalidOperationException>(() => mass.SetValue(body, 2)); Reject<InvalidOperationException>(() => body.RevertProperty(mass)); } finally { Node.EndSceneCapture(capture); }
        body.DuringIntegration = () =>
        {
            Check(mass.GetValue(body) == 1 && mass.TryGetRevertValue(body, out var revert) && revert == 1, "Inspection remains valid during integration.");
            mass.SetValue(body, 2); Check(body.Mass == 2, "Post-solver integration accepts the ordinary mass setter.");
            body.RevertProperty(mass); Check(body.Mass == 1, "Explicit restoration retains the same allowed callback behavior.");
        };
        tree.PhysicsFrame(.01); Check(body.Calls == 1 && body.Mass == 1, "The permitted post-solver edits commit once through ordinary setters.");
        var failing = new PropertyDescriptor<ProbeBody, int>("Failure", _ => throw new ArithmeticException());
        body.DuringIntegration = () => failing.GetValue(body); Reject<AggregateException>(() => tree.PhysicsFrame(.01));
        Check(body.Calls == 2, "A descriptor callback error is collected without a repeated physical callback.");
        body.DuringIntegration = null; tree.PhysicsFrame(.01); Check(body.Calls == 3, "The next step remains usable after a property callback error.");
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) Frame(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all, "Typed inspection, writes, reversion and full steps allocate zero warm owner/all-thread bytes.");
        Check(body.Position.X > 0, "Measured frames retain physical motion."); tree.Dispose(); Reject<ObjectDisposedException>(() => velocity.GetValue(body));
        void Frame()
        {
            Check(mass.GetValue(body) == 1 && mass.TryGetRevertValue(body, out var revert) && revert == 1 && !body.PropertyCanRevert(mass), "Live mass/default inspection.");
            velocity.SetValue(body, new(1, 0)); tree.PhysicsFrame(.01); Check(velocity.GetValue(body).X > .9f && body.PropertyCanRevert(velocity), "Physical velocity remains live."); body.RevertProperty(velocity);
        }
    }

    private sealed class Probe : ElectronObject
    {
        internal static readonly PropertyDescriptor<Probe, int> ValueProperty = new("Value", p => p.FailGet ? throw new ArithmeticException() : p.Value,
            (p, v) => { p.Value = v; if (p.FailSet) throw new ArithmeticException(); }, p => p.FailRevert ? throw new ArithmeticException() : p.Default, (_, v) => v >= 0);
        internal static readonly PropertyDescriptor<Probe, int> Replacement = new("Value", p => p.Value);
        internal int Value = 7, Default = 7, Mode; internal int[] Items = [1, 2], DefaultItems = [1, 2]; internal string? Text;
        internal bool FailGet, FailSet, FailRevert, ForceNoRevert, Hide, Replace; internal Action? BeforeDelete;
        internal void Refresh() => NotifyPropertyListChanged();
        protected override void OnNotification(int what) { if (what == 1) BeforeDelete?.Invoke(); }
        protected override bool CanRevertProperty(PropertyDescriptor property) => !ForceNoRevert && base.CanRevertProperty(property);
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => Mode switch
        {
            1 => [ValueProperty, ValueProperty],
            2 => [null!],
            3 => [new PropertyDescriptor<Node, int>("Wrong", _ => 0)],
            4 => throw new ArithmeticException(),
            _ => base.GetPropertyDescriptors().Append(ValueProperty)
        };
        protected override PropertyDescriptor? ValidateProperty(PropertyDescriptor property) => property == ValueProperty ? Hide ? null : Replace ? Replacement : property : base.ValidateProperty(property);
    }
    private sealed class ProbeBody : RigidBody
    {
        internal Action? DuringIntegration; internal int Calls;
        protected override void IntegrateForces(PhysicsDirectBodyState state) { Calls++; DuringIntegration?.Invoke(); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
