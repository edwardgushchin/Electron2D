using Electron2D;

public static partial class BackendChecks
{
    public static void RunPicking(PhysicsServer.Backend? backend = null)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("Picking", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("Picking");
        using var circle = new CircleShape { Radius = 8 };
        using var root = new SubViewport
        {
            World = world,
            Size = new(160, 120),
            PhysicsObjectPicking = true,
            CanvasTransform = new(0, new Vector2(2, 2), 0, new(4, 6))
        };
        var body = new PickingBody { Position = new(20, 20), GravityScale = 0, CanSleep = false };
        var area = new Area { Position = new(20, 20) };
        var layer = new CanvasLayer { Offset = new(40, 0) };
        body.AddChild(new CollisionShape { Shape = circle }); area.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(body); root.AddChild(layer); layer.AddChild(area);
        var areaInputs = 0; var areaEnters = 0; var areaExits = 0; var bodySignals = 0;
        InputEvent? borrowed = null;
        body.InputEvent += (viewport, input, slot) =>
        {
            Check(body.Callbacks == ++bodySignals && ReferenceEquals(viewport, root) && slot == 0,
                "Virtual body callback precedes its signal with the receiving viewport and logical index");
            borrowed = input;
        };
        area.InputEvent += (viewport, input, slot) =>
        {
            Check(ReferenceEquals(viewport, root) && slot == 0 && input is InputEventMouseMotion { Position: var point } && point == new Vector2(60, 20),
                "Area uses its exact canvas query while preserving event coordinates");
            areaInputs++;
        };
        area.MouseEntered += () => areaEnters++; area.MouseExited += () => areaExits++;
        using var tree = new SceneTree(root);
        Check(!body.InputPickable && area.InputPickable && PhysicsServer.SpaceGetBackend(world.Space) == (backend ?? PhysicsServer.Backend.Extension),
            "Inherited picking defaults and explicit selected ownership survive attachment");
        using var pointQuery = new PhysicsPointQueryParameters { Position = new(20, 20), CollideWithAreas = true };
        Check(world.DirectSpaceState!.IntersectPoint(pointQuery).Length == 1,
            "Root-canvas query includes the unpickable body and excludes the foreign canvas Area");
        pointQuery.CanvasInstanceID = layer.InstanceID;
        Check(world.DirectSpaceState.IntersectPoint(pointQuery).Single().ColliderRID == area.GetRID(), "Layer query resolves only the assigned Area canvas");
        Mouse(new(44, 46)); tree.PhysicsFrame(1d / 120);
        Check(body.Callbacks == 0, "Body false default suppresses pointer delivery without changing ordinary queries");
        Mouse(new(60, 20)); tree.PhysicsFrame(1d / 120);
        Check(areaInputs == 1 && areaEnters == 1, "Area true default delivers input and hover");
        area.InputPickable = false; tree.PhysicsFrame(1d / 120);
        Check(areaExits == 1 && areaInputs == 1, "Area disable clears stationary hover");
        body.InputPickable = true; Mouse(new(44, 46)); tree.PhysicsFrame(1d / 120);
        Check(body.Callbacks == 1 && body.Enters == 1 && body.ShapeEnters == 1 && borrowed is { IsDisposed: true },
            "Enabled body receives object/shape entry and owns its queued event copy only through delivery");
        if (solver is not null)
        {
            solver.PointHook = RejectPolicyWrites; solver.StepHook = RejectPolicyWrites;
            try { _ = world.DirectSpaceState.IntersectPoint(pointQuery); tree.PhysicsFrame(1d / 120); }
            finally { solver.PointHook = null; solver.StepHook = null; }
        }
        Task.Run(() => Reject<InvalidOperationException>(() => body.InputPickable = true)).GetAwaiter().GetResult();
        body.DisableOnInput = true; Mouse(new(44, 46)); tree.PhysicsFrame(1d / 120);
        Check(!body.InputPickable && body.Callbacks == 2 && bodySignals == 2, "Ordinary pointer callbacks may author pickability and still finish their signal");
        tree.PhysicsFrame(1d / 120);
        Check(body.Exits == 1 && body.ShapeExits == 1, "Callback edits retire object and logical-shape hover on the following pass");
        body.DisableOnInput = false;
        for (var i = 0; i < 128; i++) Frame(i);
        Collect(); GC.WaitForPendingFinalizers(); _ = GC.GetTotalAllocatedBytes(true);
        var beforeCallbacks = body.Callbacks; var beforeExits = body.Exits;
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(i);
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0 && body.Callbacks - beforeCallbacks == 64 && body.Exits - beforeExits == 64,
            $"Changed-pickability full frames require real passive transitions and zero allocations: {owner}/{all} B, transitions {body.Callbacks - beforeCallbacks}/{body.Exits - beforeExits}");
        Console.WriteLine($"Picking {backend?.ToString() ?? "registered"}: Body/Area policy, canvas input/hover, numerical guards and 128 changed-policy complete frames {owner}/{all} B passed.");

        void Mouse(Vector2 position)
        {
            using var input = new InputEventMouseMotion { Position = position, GlobalPosition = position };
            root.PushInput(input, true);
        }
        void Frame(int i)
        {
            body.InputPickable = area.InputPickable = (i & 1) == 0;
            tree.PhysicsFrame(1d / 120);
        }
        void RejectPolicyWrites()
        {
            Reject<InvalidOperationException>(() => body.InputPickable = body.InputPickable);
            Reject<InvalidOperationException>(() => area.InputPickable = area.InputPickable);
            Check(body.InputPickable && !area.InputPickable, "Numerical policy writes reject before shared authoring changes");
        }
    }

    private sealed class PickingBody : RigidBody
    {
        internal int Callbacks, Enters, Exits, ShapeEnters, ShapeExits;
        internal bool DisableOnInput;
        protected override void OnInputEvent(Viewport viewport, InputEvent inputEvent, int shapeIndex)
        { Callbacks++; if (DisableOnInput) InputPickable = false; }
        protected override void OnMouseEnter() => Enters++;
        protected override void OnMouseExit() => Exits++;
        protected override void OnMouseShapeEnter(int shapeIndex) => ShapeEnters++;
        protected override void OnMouseShapeExit(int shapeIndex) => ShapeExits++;
    }
}
