using System.Diagnostics;
using Electron2D;
using Body = Electron2D.GPUPhysicsBodyStore.BodyHandle;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsSolverStoreTests
{
    internal static void Run()
    {
        VerifyImpact();
        VerifySpeculativeImpact();
        VerifyAngularAndSurfaceMotion();
        VerifyMaterials();
        VerifyRaysAndHistory();
        VerifyIncidentLists();
        VerifyFailure();
        VerifyBatchedStep();
        VerifyBatchedCapacityRetry();
        VerifyColoredContacts();
        for (var layout = 0; layout < 3; layout++) VerifyCoupledLayout(layout);
        VerifyStack();
        VerifyResidency(64);
        VerifyResidency(256);
    }
    private static Body Add(GPUPhysicsBodyStore store, Vector2 position = default, Vector2 velocity = default,
        Mode mode = Mode.Rigid, float mass = 1, float inertia = 0, float angular = 0) =>
        store.Add(new(mode, position, 0, velocity, angular, mass, inertia, CenterOfMass: Vector2.Zero, CanSleep: false));

    private static void VerifyImpact()
    {
        using var store = new GPUPhysicsBodyStore();
        using var circle = new CircleShape { Radius = 10 };
        var a = Add(store, velocity: new(100, 0), inertia: 50);
        var b = Add(store, new(20, 0), new(-100, 0), mass: 2, inertia: 100);
        var sa = store.AddShape(a, circle, friction: 0, bounce: 0.5f);
        var sb = store.AddShape(b, circle, friction: 0, bounce: 0.5f);
        var result = new GPUPhysicsBodyStore.Snapshot[2];
        store.SolveConstraints(1f / 60, bounceThreshold: 0); store.Read([a, b], result);
        Near(result[0].Velocity.X, -500f / 3, 0.001f, "Elastic light-body velocity");
        Near(result[1].Velocity.X, 100f / 3, 0.001f, "Elastic heavy-body velocity");
        Near(result[0].Velocity.X + 2 * result[1].Velocity.X, -100, 0.001f, "Linear momentum");
        Near(0.5f * result[0].Velocity.X * result[0].Velocity.X + result[1].Velocity.X * result[1].Velocity.X, 15000, 0.02f, "Elastic kinetic energy");
        Check(result[0].Position == Vector2.Zero && result[1].Position == new Vector2(20, 0), "Velocity solving does not advance pose.");
        store.SetShapeMaterial(sa, 0, 0.5f); store.SetShapeMaterial(sb, 0, -0.5f);
        store.SetVelocity(a, new(100, 0), 0); store.SetVelocity(b, new(-100, 0), 0);
        store.SolveConstraints(1f / 60, bounceThreshold: 0); store.Read([a, b], result);
        Near(result[0].Velocity.X, -100f / 3, 0.001f, "Absorbent material cancels bounce");
        Near(result[1].Velocity.X, -100f / 3, 0.001f, "Inelastic common velocity");
        store.SetShapeFilter(sb, 1, uint.MaxValue, true);
        store.SetVelocity(a, new(100, 0), 0); store.SetVelocity(b, new(-100, 0), 0);
        store.SolveConstraints(1f / 60); store.Read([a, b], result);
        Check(result[0].Velocity.X == 100 && result[1].Velocity.X == -100, "Sensors apply no contact impulse.");
        Reject<ArgumentOutOfRangeException>(() => store.Simulate(float.NaN, Vector2.Zero));
        Reject<ArgumentOutOfRangeException>(() => store.SolveConstraints(0));
        Reject<ArgumentOutOfRangeException>(() => store.Simulate(0.1f, Vector2.Zero, substeps: 0));
        store.Read([a, b], result);
        Console.WriteLine("Resident solver: elastic/inelastic mass ratios, momentum/energy, sensors and input guards passed.");
    }

    private static void VerifySpeculativeImpact()
    {
        using var store = new GPUPhysicsBodyStore { CaptureContactReports = true }; using var circle = new CircleShape { Radius = 10 };
        var bodies = new Body[6]; var states = new GPUPhysicsBodyStore.Snapshot[6];
        for (var pair = 0; pair < 3; pair++)
        {
            var gap = pair == 1 ? 4 : .5f; var speed = pair == 2 ? -100 : 100;
            bodies[pair * 2] = Add(store, new(0, pair * 100), new(speed, 0), inertia: 50);
            bodies[pair * 2 + 1] = Add(store, new(20 + gap, pair * 100), new(-speed, 0), mass: 2, inertia: 100);
            store.AddShape(bodies[pair * 2], circle, friction: 0, bounce: .5f);
            store.AddShape(bodies[pair * 2 + 1], circle, friction: 0, bounce: .5f);
        }
        store.Simulate(1f / 60, Vector2.Zero); store.Read(bodies, states);
        Near(states[0].Velocity.X, -500f / 3, .001f, "An approaching separated pair retains its elastic impact velocity");
        Near(states[1].Velocity.X, 100f / 3, .001f, "Separated unequal masses preserve momentum");
        Near(.5f * states[0].Velocity.X * states[0].Velocity.X + states[1].Velocity.X * states[1].Velocity.X, 15000, .02f, "Speculative elastic impact preserves kinetic energy");
        Near(states[2].Velocity.X, 100, .001f, "A future impact beyond the interval cannot rebound early");
        Near(states[3].Velocity.X, -100, .001f, "Future impact leaves both velocities unchanged");
        Near(states[4].Velocity.X, -100, .001f, "A separated receding pair cannot rebound");
        Near(states[5].Velocity.X, 100, .001f, "Receding bodies retain outgoing velocity");
        var reports = new GPUPhysicsBodyStore.ContactReport[4]; var counts = new int[2];
        store.ReadContactReports(bodies.AsSpan(0, 2), new[] { 2, 2 }, counts, reports);
        for (var body = 0; body < 2; body++)
        {
            Check(counts[body] > 0, "The speculative impact remains reportable after separation later in the frame");
            var impulse = Vector2.Zero;
            for (var point = 0; point < counts[body]; point++) impulse += reports[body * 2 + point].Impulse;
            Near(impulse.X, body == 0 ? states[0].Velocity.X - 100 : 2 * (states[1].Velocity.X + 100), .001f,
                "Completed-frame impulse agrees with actual momentum change");
            Near(impulse.Y, 0, .001f, "Head-on impact has no transverse impulse");
        }
        Console.WriteLine("Resident speculative restitution: incoming impact, unequal-mass momentum/energy, future and receding pairs passed.");
    }

    private static void VerifyAngularAndSurfaceMotion()
    {
        using var store = new GPUPhysicsBodyStore();
        using var circle = new CircleShape { Radius = 1 };
        var a = Add(store, velocity: new(10, 0), mass: 2, inertia: 50);
        var b = Add(store, new(1.8f, 5), mode: Mode.Static);
        store.AddShape(a, circle, new Transform(0, new(0, 5)), friction: 0);
        store.AddShape(b, circle, friction: 0);
        store.SolveConstraints(1f / 60);
        var result = new GPUPhysicsBodyStore.Snapshot[2]; store.Read([a, b], result);
        Near(result[0].Velocity.X, 5, 0.001f, "Offset normal impulse changes translation");
        Near(result[0].Velocity.Z, 1, 0.001f, "Offset normal impulse respects scene-unit inertia");
        Check(result[1].Position == new Vector2(1.8f, 5), "Static pose stays fixed.");
        store.Remove(a); store.Remove(b);
        using var tractionFloor = new RectangleShape { Size = new(200, 2) };
        a = Add(store, velocity: new(10, 10), mass: 2, inertia: 1);
        b = Add(store, new(0, 2), mode: Mode.Static);
        store.AddShape(a, circle); store.AddShape(b, tractionFloor);
        store.SolveConstraints(1f / 60); store.Read([a, b], result);
        Near(result[0].Velocity.X, 20f / 3, 0.001f, "Tangential impulse preserves rolling speed");
        Near(result[0].Velocity.Y, 0, 0.001f, "Floor stops the disc's normal motion");
        Near(result[0].Velocity.Z, 20f / 3, 0.001f, "Tangential Jacobian produces angular response");
        Near(2 * result[0].Velocity.X + result[0].Velocity.Z, 20, 0.001f, "Angular momentum about the contact is conserved");
        store.Remove(a); store.Remove(b);
        using var disc = new CircleShape { Radius = 2 };
        using var floor = new RectangleShape { Size = new(200, 10) };
        a = Add(store, new(-10, 0), mode: Mode.RigidLinear);
        b = Add(store, new(0, 7), new(20, 0), Mode.Static, angular: 1);
        store.AddShape(a, disc, friction: 4); store.AddShape(b, floor, friction: 4);
        store.SolveConstraints(1f / 60); store.Read([a, b], result);
        Near(result[0].Velocity.X, 25, 0.001f, "Static linear plus angular surface tangent velocity");
        Near(result[0].Velocity.Y, -10, 0.001f, "Static angular surface normal velocity");
        Check(result[0].Velocity.Z == 0 && result[1].Position == new Vector2(0, 7), "Rotation lock and stationary moving surface persist.");
        Console.WriteLine("Resident solver: inertia, offset impulses, rotation lock and stationary linear/angular surfaces passed.");
    }

    private static void VerifyMaterials()
    {
        using var store = new GPUPhysicsBodyStore();
        using var box = new RectangleShape { Size = new(20, 20) };
        using var floor = new RectangleShape { Size = new(2000, 10) };
        var a = Add(store, velocity: new(100, 10), mode: Mode.RigidLinear);
        var b = Add(store, new(0, 15), mode: Mode.Static);
        var sa = store.AddShape(a, box, friction: 0.1f);
        var sb = store.AddShape(b, floor, friction: 0.9f);
        var result = new GPUPhysicsBodyStore.Snapshot[2];
        store.SolveConstraints(1f / 60, iterations: 64); store.Read([a, b], result);
        Near(result[0].Velocity.X, 99, 0.002f, "Lower ordinary friction wins");
        Near(result[0].Velocity.Y, 0, 0.001f, "Face contacts stop normal motion");
        store.SetShapeMaterial(sb, -0.9f, 0); store.SetVelocity(a, new(100, 10), 0);
        store.SolveConstraints(1f / 60, iterations: 64); store.Read([a, b], result);
        Near(result[0].Velocity.X, 91, 0.002f, "Rough friction wins");
        store.SetShapeMaterial(sa, 1, 0); store.SetShapeMaterial(sb, 1, 0);
        store.SetVelocity(a, Vector2.Zero, 0); store.SetVelocity(b, new(80, 0), 0);
        for (var i = 0; i < 120; i++) store.Simulate(1f / 60, new(0, 980));
        store.Read([a, b], result);
        Near(result[0].Velocity.X, 80, 0.05f, "Friction carries a resting body on a stationary conveyor");
        Check(result[1].Position == new Vector2(0, 15) && result[0].Position.Y < 0.6f && result[0].Position.Y > -0.1f, "Gravity, contact support and stationary pose remain stable.");
        Console.WriteLine("Resident solver: signed material mixing, Coulomb friction and gravity-driven conveyor passed.");
    }

    private static void VerifyRaysAndHistory()
    {
        using var store = new GPUPhysicsBodyStore();
        using var ray = new SeparationRayShape { Length = 20 };
        using var floor = new RectangleShape { Size = new(200, 10) };
        var a = Add(store, velocity: new(20, 10), mode: Mode.RigidLinear, mass: 2);
        var b = Add(store, new(0, 20), mode: Mode.Static);
        var sa = store.AddShape(a, ray, friction: 0.2f, bounce: 0.5f);
        var sb = store.AddShape(b, floor, friction: 0.2f);
        var result = new GPUPhysicsBodyStore.Snapshot[1];
        store.SolveConstraints(1f / 60, margin: 0, correctionFactor: 0, bounceThreshold: 0); store.Read([a], result);
        Near(result[0].Velocity.Y, -5, 0.001f, "Directed ray supplies restitution impulse");
        Near(result[0].Velocity.X, 17, 0.001f, "Directed ray supplies Coulomb friction");
        foreach (var slide in new[] { false, true })
        {
            ray.SlideOnSlope = slide; store.SetPose(b, new(0, 20), 0.2f); store.SetVelocity(a, new(20, 10), 0);
            var points = new GPUPhysicsBodyStore.ContactPoint[1]; Check(store.ReadContacts(points) == 1, "Sloped ray contact exists.");
            var normal = new Vector2(points[0].Normal.X, points[0].Normal.Y); var tangent = new Vector2(normal.Y, -normal.X);
            var incoming = new Vector2(20, 10); var normalSpeed = incoming.Dot(normal); var tangentSpeed = incoming.Dot(tangent);
            store.SolveConstraints(1f / 60, margin: 0, correctionFactor: 0, bounceThreshold: 0); store.Read([a], result);
            var velocity = new Vector2(result[0].Velocity.X, result[0].Velocity.Y);
            Near(velocity.Dot(normal), -0.5f * normalSpeed, 0.001f, "Both ray slope modes preserve restitution along their directed normal");
            Near(velocity.Dot(tangent), tangentSpeed - 0.3f * normalSpeed, 0.001f, "Both ray slope modes preserve friction impulse");
        }
        store.Remove(a); store.Remove(b);
        using var circle = new CircleShape { Radius = 1 };
        a = Add(store, mode: Mode.RigidLinear); b = Add(store, new(0, 6), mode: Mode.Static);
        sa = store.AddShape(a, circle); sb = store.AddShape(b, floor);
        void Load() { for (var i = 0; i < 4; i++) store.Simulate(1f / 60, new(0, 980)); }
        Load(); store.SolveConstraints(1f / 240); Check(store.WarmStartedPointCount == 1, "Resting contact reuses its physical impulse.");
        Load(); circle.Radius = 1.01f; store.SolveConstraints(1f / 240);
        Check(store.WarmStartedPointCount == 0, "Geometry revision invalidates contact history.");
        Load(); store.SetShapeMaterial(sa, 0.25f, 0); store.SolveConstraints(1f / 240);
        Check(store.WarmStartedPointCount == 0, "Material revision invalidates contact history.");
        Load(); store.Read([a], result); store.SetPose(a, result[0].Position + new Vector2(0, 0.001f), result[0].Rotation); store.SolveConstraints(1f / 240);
        Check(store.WarmStartedPointCount == 0, "Explicit pose edits invalidate only their body's history.");
        Load(); store.SetVelocity(a, new(0, 0.001f), 0); store.SolveConstraints(1f / 240);
        Check(store.WarmStartedPointCount == 0, "Explicit velocity edits invalidate body history.");
        Load(); store.Simulate(1f / 120, new(0, 980)); store.Simulate(1f / 30, new(0, 980)); store.Read([a], result);
        Check(store.WarmStartedPointCount == 1 && MathF.Abs(result[0].Velocity.Y) < 0.01f, "History scales across changed step duration.");
        Load();
        for (var i = 0; i < 200; i++)
        {
            store.AddShape(Add(store, new(1000 + i * 8, 0)), circle);
            store.AddShape(Add(store, new(1000 + i * 8 + 2, 0)), circle);
        }
        store.SolveConstraints(1f / 240);
        Check(store.WarmStartedPointCount == 1, "Unrelated body creation and history-buffer/table growth preserve the prior contact.");
        Console.WriteLine("Resident solver: directed-ray response and history identity/edit/growth boundaries passed.");
    }

    private static void VerifyFailure()
    {
        using var store = new GPUPhysicsBodyStore();
        using var circle = new CircleShape { Radius = 1 };
        var a = Add(store, velocity: new(float.MaxValue / 2, 0), mass: 100);
        var b = Add(store, new(2, 0), new(-float.MaxValue / 2, 0), mass: 100);
        store.AddShape(a, circle, friction: 0, bounce: 1); store.AddShape(b, circle, friction: 0, bounce: 1);
        Reject<InvalidOperationException>(() => store.SolveConstraints(1f / 60));
        Reject<InvalidOperationException>(() => store.Simulate(1f / 60, Vector2.Zero));
        Reject<InvalidOperationException>(() => store.Read([a], new GPUPhysicsBodyStore.Snapshot[1]));
        Console.WriteLine("Resident solver rejects a nonrepresentable impulse and all subsequent state use.");
    }

    private static void VerifyBatchedStep()
    {
        using var store = new GPUPhysicsBodyStore();
        var body = Add(store, velocity: new(10, 0));
        var state = new GPUPhysicsBodyStore.Snapshot[1]; Body[] bodies = [body];
        for (var i = 0; i < 4; i++) store.Simulate(.1f, Vector2.Zero);
        var before = store.SubmissionCount;
        store.Simulate(.1f, Vector2.Zero); var batched = store.SubmissionCount - before;
        store.SeparateSimulationSubmissions = true; before = store.SubmissionCount;
        store.Simulate(.1f, Vector2.Zero); var separate = store.SubmissionCount - before;
        store.Read(bodies, state);
        Near(state[0].Position.X, 6, .001f, "Both submission modes retain all four position intervals");
        Near(state[0].Velocity.X, 10, .001f, "Batching preserves force-free velocity");
        Check(separate - batched == 8, "The four substeps save exactly eight velocity/position submissions");
        Check(store.PublishedActiveBodyCount == 1 && store.ActiveSimulationBodyCount == 1, "Final activity statistics survive batched integration");
        store.SeparateSimulationSubmissions = false; store.SetVelocity(body, Vector2.Zero, 0); store.SetCanSleep(body, true);
        store.SetSleepSettings(new(2, .1f, .05f));
        for (var i = 0; i < 10; i++) store.Simulate(.01f, Vector2.Zero);
        store.Read(bodies, state);
        Check(state[0].Sleeping && store.PublishedActiveBodyCount == 0, "Batched sleep publishes its final state and activity");
        using var failed = new GPUPhysicsBodyStore();
        var invalid = Add(failed, new(float.MaxValue * .75f, 0), new(float.MaxValue * .75f, 0));
        Reject<InvalidOperationException>(() => failed.Simulate(1, Vector2.Zero, substeps: 1));
        Check(failed.HasFailed, "Position-stage errors invalidate the combined submission");
        Reject<InvalidOperationException>(() => failed.Read([invalid], state));
        using var failedVelocity = new GPUPhysicsBodyStore(); using var circle = new CircleShape { Radius = 1 };
        var accelerated = Add(failedVelocity, velocity: new(float.MaxValue * .75f, 0)); failedVelocity.AddShape(accelerated, circle);
        var velocityError = false;
        try { failedVelocity.Simulate(1, new(float.MaxValue * .75f, 0), substeps: 1); }
        catch (InvalidOperationException error) { velocityError = error.Message.Contains("velocity work returned invalid state"); }
        Check(velocityError && failedVelocity.HasFailed, "Spatial batching validates its separate velocity error record");
        Reject<InvalidOperationException>(() => failedVelocity.Read([accelerated], state));
        Console.WriteLine($"Resident batched solver/position: {batched} versus {separate} submissions, complete motion/sleep/statistics and terminal position failure passed.");
    }

    private static void VerifyBatchedCapacityRetry()
    {
        using var store = new GPUPhysicsBodyStore(); using var circle = new CircleShape { Radius = 1 };
        var bodies = new Body[256]; var states = new GPUPhysicsBodyStore.Snapshot[bodies.Length];
        for (var i = 0; i < bodies.Length; i++)
        {
            bodies[i] = Add(store, new(i / 2 * 10 + i % 2 * 1.8f, 0));
            store.AddShape(bodies[i], circle, friction: 0);
        }
        store.Simulate(.1f, new(0, 980), margin: 0); store.Read(bodies, states);
        Check(store.PairCapacityRetries > 0 && store.ContactCapacityRetries > 0, "The first batched tick exercises pair and contact capacity recovery");
        foreach (var state in states)
        {
            Near(state.Velocity.Y, 98, .001f, "Capacity recovery cannot integrate gravity twice");
            Near(state.Position.Y, 6.125f, .001f, "Every body retains exactly four semi-implicit intervals through recovery");
        }
        Console.WriteLine("Resident batched integration survives pair/contact capacity recovery without repeating forces.");
    }

    private static void VerifyColoredContacts()
    {
        using var store = new GPUPhysicsBodyStore { CaptureContactReports = true }; using var circle = new CircleShape { Radius = 1 };
        var bodies = new Body[300]; var states = new GPUPhysicsBodyStore.Snapshot[bodies.Length];
        var limits = new int[bodies.Length]; Array.Fill(limits, 2); var counts = new int[bodies.Length]; var reports = new GPUPhysicsBodyStore.ContactReport[bodies.Length * 2];
        for (var i = 0; i < bodies.Length; i++)
        {
            bodies[i] = Add(store, new(i * 2, 0), i == 0 ? new(12, 0) : Vector2.Zero);
            store.AddShape(bodies[i], circle, friction: 0);
        }
        var pinA = Add(store, new(1000, 1000)); var pinB = Add(store, new(1000, 1000));
        store.AddJoint(new(PhysicsServer.JointType.Pin, pinA, pinB, Transform.Identity, Transform.Identity) { MaxForce = 1, MaxBias = 100 });
        store.SolveConstraints(1f / 60, iterations: 1, margin: 0); store.Read(bodies, states); store.ReadContactReports(bodies, limits, counts, reports);
        Check(store.ContactColorCount > 1 && store.ContactColorFallbacks == 0, "A coupled contact chain retains its device schedule in a world with capped joints");
        var momentum = Vector2.Zero; var energy = 0f;
        for (var i = 0; i < bodies.Length; i++)
        {
            var velocity = new Vector2(states[i].Velocity.X, states[i].Velocity.Y); momentum += velocity; energy += .5f * velocity.LengthSquared();
            var impulse = Vector2.Zero; for (var j = 0; j < counts[i]; j++) impulse += reports[i * 2 + j].Impulse;
            Near(impulse.X, velocity.X - (i == 0 ? 12 : 0), .002f, "Colored contact reports preserve each body's momentum change");
            Near(states[i].Position.X, i * 2, .001f, "Standalone colored velocity solving does not advance poses");
        }
        Near(momentum.X, 12, .002f, "Every contact color preserves total linear momentum");
        Check(energy <= 72.002f, "Inelastic colored constraints cannot add kinetic energy");
        Console.WriteLine($"Resident colored contacts: {store.ContactColorCount} colors/{store.ContactColorRounds} proposal rounds, momentum, energy and frame impulses passed.");
    }

    private static void VerifyCoupledLayout(int layout)
    {
        using var store = new GPUPhysicsBodyStore { CaptureContactReports = true, PackColoredContacts = layout > 0, PackSolverBodyState = layout > 1 };
        using var circle = new CircleShape { Radius = 1 };
        var a = Add(store, new(-2, 0), new(12, 0)); var b = Add(store); var c = Add(store, new(0, 10));
        store.AddShape(a, circle, friction: 0); store.AddShape(b, circle, friction: 0);
        store.AddJoint(new(PhysicsServer.JointType.Pin, b, c, Transform.Identity, new(0, new(0, -10))) { MaxForce = 1000, MaxBias = 100 });
        store.SolveConstraints(1f / 60, iterations: 1, margin: 0);
        var states = new GPUPhysicsBodyStore.Snapshot[3]; store.Read([a, b, c], states);
        Near(states[0].Velocity.X, 6, .001f, "Equal-mass inelastic impact shares the incoming velocity");
        Check(states[2].Velocity.X > .01f && states[2].Velocity.X < 6, "The coupled pin consumes the velocity produced by the contact sweep");
        Near(states[1].Velocity.X + states[2].Velocity.X, 6, .001f, "Joint gather preserves the contact's momentum contribution");
        Near(states[2].Position.Y, 10, .001f, "Packed velocity solving preserves world poses");
        var reports = new GPUPhysicsBodyStore.ContactReport[1]; var counts = new int[1];
        store.ReadContactReports([b], [1], counts, reports);
        Check(counts[0] == 1, "Only the collision contributes a contact report");
        Near(reports[0].Impulse.X, 6, .001f, "Unpacked reports contain contact impulses without the later joint impulse");
        Console.WriteLine($"Resident solver layout {layout}: coupled contact/joint velocity, momentum and report isolation passed.");
    }

    private static void VerifyStack()
    {
        using var store = new GPUPhysicsBodyStore();
        using var box = new RectangleShape { Size = new(20, 20) };
        using var floor = new RectangleShape { Size = new(400, 20) };
        store.AddShape(Add(store, new(0, 200), mode: Mode.Static), floor);
        var bodies = new Body[8]; var result = new GPUPhysicsBodyStore.Snapshot[8];
        for (var i = 0; i < bodies.Length; i++)
        {
            bodies[i] = Add(store, new(i == 7 ? 1 : 0, 180 - 20 * i), inertia: 800f / 12);
            store.AddShape(bodies[i], box);
        }
        for (var i = 0; i < 600; i++) store.Simulate(1f / 60, new(0, 980), iterations: 32);
        store.Read(bodies, result);
        var maxSpeed = result.Max(p => new Vector2(p.Velocity.X, p.Velocity.Y).Length());
        var maxError = result.Select((p, i) => MathF.Abs(p.Position.Y - (180 - 20 * i))).Max();
        Console.WriteLine($"Resident stack after 10 s: max speed={maxSpeed:F5}, max vertical error={maxError:F5}, top={result[7].Position}, bottom={result[0].Position}, warm={store.WarmStartedPointCount}.");
        Check(result.All(p => p.Position.IsFinite() && MathF.Abs(p.Position.X) < 5) && maxSpeed < 0.5f && maxError < 6,
            "Eight freely rotating offset boxes settle without falling through or unbounded energy.");
    }

    private static void VerifyIncidentLists()
    {
        const int count = 257;
        using var store = new GPUPhysicsBodyStore();
        using var plank = new RectangleShape { Size = new(count * 2 + 4, 2) };
        using var circle = new CircleShape { Radius = 1 };
        var body = Add(store, new(count - 1, 0), mode: Mode.RigidLinear, mass: count);
        store.AddShape(body, plank, friction: 0);
        var supports = new GPUPhysicsBodyStore.ShapeHandle[count];
        var result = new GPUPhysicsBodyStore.Snapshot[1];
        for (var i = 0; i < count; i++)
        {
            supports[i] = store.AddShape(Add(store, new(i * 2, 2), mode: Mode.Static), circle, friction: 0);
            if (i != 63 && i != count - 1) continue;
            store.SetVelocity(body, new(0, 10), 0);
            store.SolveConstraints(1f / 60, iterations: 1, margin: 0);
            store.Read([body], result);
            Check(store.ContactPointCount == i + 1, "Every support contributes one point, including after device growth.");
            Check(store.ContactColorCount == 0 && store.ContactColorFallbacks > 0, "High-degree bodies retain complete GPU Jacobi response when the color budget is exhausted");
            Near(result[0].Velocity.Y, 0, 0.002f, "All incident impulses contribute to the locked plank in one damped iteration");
        }
        for (var i = 0; i < count; i++) store.SetShapeFilter(supports[i], 1, uint.MaxValue, true);
        store.SetVelocity(body, new(0, 10), 0);
        store.SolveConstraints(1f / 60, margin: 0); store.Read([body], result);
        Near(result[0].Velocity.Y, 10, 0.001f, "A now-empty incident list cannot apply stale physical or warm impulses from sensor points");
        Console.WriteLine("Resident solver: 64/257 incident contacts, growth and all-sensor transition passed.");
    }

    private static void VerifyResidency(int side)
    {
        const int warmup = 384, samples = 256;
        var count = side * side;
        using var store = new GPUPhysicsBodyStore();
        using var circle = new CircleShape { Radius = 1 };
        using var floor = new RectangleShape { Size = new(side * 2 + 6, 2) };
        using var wall = new RectangleShape { Size = new(2, side * 2 + 6) };
        var floorY = side * 2 - 1;
        store.AddShape(Add(store, new(side - 1, floorY + 1), mode: Mode.Static), floor, friction: 0.3f);
        store.AddShape(Add(store, new(-3, side - 1), mode: Mode.Static), wall, friction: 0.3f);
        store.AddShape(Add(store, new(side * 2 + 1, side - 1), mode: Mode.Static), wall, friction: 0.3f);
        var bodies = new Body[count];
        for (var i = 0; i < count; i++)
        {
            bodies[i] = Add(store, new(i % side * 2, i / side * 2), inertia: 0.5f);
            store.AddShape(bodies[i], circle, friction: 0.3f);
        }
        for (var i = 0; i < warmup; i++) store.Simulate(1f / 120, new(0, 980), margin: 0.1f, allowedPenetration: 0.01f);
        store.ProfileSolverPasses = Environment.GetEnvironmentVariable("ELECTRON2D_PROFILE_RESIDENT_PASSES") == "1";
        var times = new double[samples]; var upload = store.UploadBytes; var download = store.ReadbackBytes; var uniforms = store.UniformBytes; var wait = store.WaitMS;
        var shapes = store.ShapeUploadBytes; var geometry = store.GeometryUploadBytes; var solver = store.SolverMS; var solverWait = store.SolverWaitMS;
        var allocation = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples; i++)
        {
            var start = Stopwatch.GetTimestamp(); store.Simulate(1f / 120, new(0, 980), margin: 0.1f, allowedPenetration: 0.01f);
            times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        allocation = GC.GetAllocatedBytesForCurrentThread() - allocation;
        Check(allocation == 0 && store.ShapeUploadBytes == shapes && store.GeometryUploadBytes == geometry, "Warmed resident response allocates no managed bytes or authored-state traffic.");
        Array.Sort(times);
        if (store.ProfileSolverPasses)
            Console.WriteLine($"Diagnostic fenced passes (submission overhead included), ms/tick: clear={store.SolverPassMS[0] / samples:F4}, prepare={store.SolverPassMS[1] / samples:F4}, update={store.SolverPassMS[2] / samples:F4}, gather={store.SolverPassMS[3] / samples:F4}, save={store.SolverPassMS[4] / samples:F4}, hash clear={store.SolverPassMS[5] / samples:F4}, hash insert={store.SolverPassMS[6] / samples:F4}, coloring={store.SolverPassMS[7] / samples:F4}.");
        Console.WriteLine($"Resident contact response: {count} circles, gravity 980, 4 substeps, 16 iterations, {warmup} warmup/{samples} samples; p50={times[samples / 2]:F4} ms, p95={times[(int)(samples * 0.95)]:F4} ms, p99={times[(int)(samples * 0.99)]:F4} ms, wait={(store.WaitMS - wait) / samples:F4} ms, solver={(store.SolverMS - solver) / samples:F4} ms, solver wait={(store.SolverWaitMS - solverWait) / samples:F4} ms; {allocation} B/tick, upload={(store.UploadBytes - upload) / samples}, readback={(store.ReadbackBytes - download) / samples}, uniforms={(store.UniformBytes - uniforms) / samples} B/tick; {store.Driver}, {store.DeviceName}, .NET {Environment.Version}.");
        var snapshots = new GPUPhysicsBodyStore.Snapshot[count]; store.Read(bodies, snapshots);
        var energy = snapshots.Sum(p => 0.5 * ((double)p.Velocity.X * p.Velocity.X + (double)p.Velocity.Y * p.Velocity.Y) + 0.25 * p.Velocity.Z * p.Velocity.Z - 980.0 * p.Position.Y);
        var initialEnergy = -980.0 * count * (side - 1);
        Check(energy <= initialEnergy + count * 98.0, "Contact response does not create unbounded mechanical energy.");
        var maximumY = snapshots.Max(p => p.Position.Y); var maximumSpeed = snapshots.Max(p => new Vector2(p.Velocity.X, p.Velocity.Y).Length());
        Console.WriteLine($"Resident pile: max Y={maximumY:F4}, floor={floorY}, max speed={maximumSpeed:F4}, contacts={store.ContactPointCount}, warm={store.WarmStartedPointCount}, energy change={energy - initialEnergy:F2}.");
        Check(snapshots.All(p => p.Position.IsFinite() && p.Position.X > -4 && p.Position.X < side * 2 + 2) && maximumY < floorY + 0.1f,
            "The complete gravity-loaded population remains inside the pit.");
    }
    private static void Near(float value, float expected, float tolerance, string message) => Check(MathF.Abs(value - expected) <= tolerance, $"{message}: {value} vs {expected}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
