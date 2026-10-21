using Electron2D;

public static partial class BackendChecks
{
    public static void RunEventMembership(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { EventMembership(backend); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void EventMembership(PhysicsServer.Backend? backend)
    {
        foreach (var source in new CollisionObject[] { new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone(), new Area() })
        {
            var pointers = PointerMembership(source);
            Check(pointers.All(x => x.IsConnected), "Every body/Area sibling exposes attached pointer wrappers");
            source.Dispose(); Check(pointers.All(x => !x.IsConnected), "Every body/Area sibling clears pointer membership on disposal");
            foreach (var token in pointers) token.Dispose();
        }
        if (backend is null) PhysicsServerManager.RegisterServer("EventMembership", () => new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("EventMembership");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Membership frames use the requested backend without fallback");
        using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 1 }; using var sensor = new CircleShape { Radius = 4 };
        var area = new Area { Gravity = 0 }; area.AddChild(new CollisionShape { Shape = sensor }); root.AddChild(area);
        var body = new RigidBody { CanSleep = false, GravityScale = 0, ContactMonitor = true, MaxContactsReported = 8 };
        body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
        var wall = new StaticBody { Position = new(.8f, 0) }; wall.AddChild(new CollisionShape { Shape = circle }); root.AddChild(wall);
        using var tree = new SceneTree(root); var ownerThread = Environment.CurrentManagedThreadId;
        var entered = 0; var exited = 0; var shapes = 0; var bodyContacts = 0;
        EventConnection[] tokens =
        [
            EventConnection.Subscribe<Area>(h => area.AreaEntered += h, h => area.AreaEntered -= h, static _ => { }),
            EventConnection.Subscribe<Area>(h => area.AreaExited += h, h => area.AreaExited -= h, static _ => { }),
            EventConnection.Subscribe<Entity>(h => area.BodyEntered += h, h => area.BodyEntered -= h, other => { OnOwner(); if (ReferenceEquals(other, body)) entered++; }),
            EventConnection.Subscribe<Entity>(h => area.BodyExited += h, h => area.BodyExited -= h, other => { OnOwner(); if (ReferenceEquals(other, body)) exited++; }),
            EventConnection.Subscribe<RID, Area?, int, int>(h => area.AreaShapeEntered += h, h => area.AreaShapeEntered -= h, static (_, _, _, _) => { }),
            EventConnection.Subscribe<RID, Area?, int, int>(h => area.AreaShapeExited += h, h => area.AreaShapeExited -= h, static (_, _, _, _) => { }),
            EventConnection.Subscribe<RID, Entity?, int, int>(h => area.BodyShapeEntered += h, h => area.BodyShapeEntered -= h, (_, other, otherShape, localShape) => { OnOwner(); if (ReferenceEquals(other, body)) { Check(otherShape == 0 && localShape == 0, "Current membership preserves complete shape payloads"); shapes++; } }),
            EventConnection.Subscribe<RID, Entity?, int, int>(h => area.BodyShapeExited += h, h => area.BodyShapeExited -= h, static (_, _, _, _) => { }),
            EventConnection.Subscribe<Node>(h => body.BodyEntered += h, h => body.BodyEntered -= h, _ => { OnOwner(); bodyContacts++; }),
            EventConnection.Subscribe<Node>(h => body.BodyExited += h, h => body.BodyExited -= h, static _ => { }),
            EventConnection.Subscribe<RID, Node, int, int>(h => body.BodyShapeEntered += h, h => body.BodyShapeEntered -= h, static (_, _, _, _) => { }),
            EventConnection.Subscribe<RID, Node, int, int>(h => body.BodyShapeExited += h, h => body.BodyShapeExited -= h, static (_, _, _, _) => { }),
            EventConnection.Subscribe<RigidBody>(h => body.SleepingStateChanged += h, h => body.SleepingStateChanged -= h, static _ => { }),
            .. PointerMembership(body)
        ];
        for (var i = 0; i < 256; i++) Frame(i);
        var beforeEntered = entered; var beforeExited = exited; var beforeShapes = shapes; var beforeContacts = bodyContacts;
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(i);
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0 && entered - beforeEntered == 64 && exited - beforeExited == 64 && shapes - beforeShapes == 64 && bodyContacts - beforeContacts == 64,
            "Every measured physical event and membership query preserves strict zero owner/all-thread bytes");
        var pending = new Queue<Action>(); var deferredCalls = 0;
        using var deferred = EventConnection.Subscribe<Entity>(h => area.BodyEntered += h, h => area.BodyEntered -= h, _ => deferredCalls++, defer: pending.Enqueue);
        Frame(0); Check(pending.Count == 1 && deferred.IsConnected, "A real physical emission accepts one deferred callback");
        area.TreeExiting += static _ => throw new ArithmeticException("membership teardown");
        try { area.Dispose(); throw new InvalidOperationException("Expected publisher teardown error"); }
        catch (ArithmeticException) { }
        catch (AggregateException error) { Check(error.Flatten().InnerExceptions.Any(x => x is ArithmeticException), "Teardown preserves the original user failure"); }
        Check(area.IsDisposed && !deferred.IsConnected && tokens.Take(8).All(x => !x.IsConnected) && tokens.Skip(8).All(x => x.IsConnected), "Failed Area teardown clears only that publisher's current membership");
        pending.Dequeue()(); Check(deferredCalls == 1, "Publisher clearing does not cancel accepted deferred work");
        body.Dispose(); Check(tokens.All(x => !x.IsConnected), "Body teardown clears contact/sleep and inherited pointer wrappers");
        foreach (var token in tokens) token.Dispose();
        tree.PhysicsFrame(1d / 144);
        Console.WriteLine($"Event membership on {backend?.ToString() ?? "registered"}: 18 live physical accessors, six pointer-owner siblings, failed teardown, captured deferred delivery and 128 full frames {own}/{all} B passed.");
        void OnOwner() => Check(Environment.CurrentManagedThreadId == ownerThread, "Physical event callbacks remain on the owner");
        void Frame(int i)
        {
            foreach (var token in tokens) Check(token.IsConnected, "All prepared physical wrappers retain current membership before stepping");
            body.Position = new(i % 2 == 0 ? 0 : 20, 0); body.LinearVelocity = Vector2.Zero;
            tree.PhysicsFrame(1d / 144);
            foreach (var token in tokens) Check(token.IsConnected, "All prepared physical wrappers retain current membership after stepping");
            Check(body.GlobalTransform.IsFinite() && (i % 2 != 0 || body.GetContactCount() > 0), "Actual moving-body contact state remains published");
        }
    }

    private static EventConnection[] PointerMembership(CollisionObject source) =>
    [
        EventConnection.Subscribe<Node, InputEvent, int>(h => source.InputEvent += h, h => source.InputEvent -= h, static (_, _, _) => { }),
        EventConnection.Subscribe(h => source.MouseEntered += h, h => source.MouseEntered -= h, static () => { }),
        EventConnection.Subscribe(h => source.MouseExited += h, h => source.MouseExited -= h, static () => { }),
        EventConnection.Subscribe<int>(h => source.MouseShapeEntered += h, h => source.MouseShapeEntered -= h, static _ => { }),
        EventConnection.Subscribe<int>(h => source.MouseShapeExited += h, h => source.MouseShapeExited -= h, static _ => { })
    ];
}
